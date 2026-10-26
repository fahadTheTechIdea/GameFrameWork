using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable authored stock/export intent; balances and execution belong to original logistics.</summary>
    public interface ILogisticsPolicyDefinition : IContentDefinition
    {
        IReadOnlyList<StockPolicyEntry> Stock { get; }
        OutletPolicySettings Outlet { get; }
        EntityStateBindings States { get; }
    }

    /// <summary>Sole native/independent authoring and actual-alias validation; no live stock or defaults.</summary>
    public sealed class LogisticsPolicyDeclaration
    {
        private readonly List<(string Resource, LogisticsDemandSettings Settings)> _stocks = new List<(string, LogisticsDemandSettings)>();
        private BillSpec _reserve;
        private bool _enabled;
        private int _priority;
        private string[] _destinations;
        private LogisticsReserveAccess _reserveAccess;
        public IReadOnlyList<StockPolicyEntry> Stock { get; private set; }
        public OutletPolicySettings Outlet { get; private set; }
        public EntityStateBindings States { get; private set; }
        private LogisticsPolicyDeclaration() { }

        public static LogisticsPolicyDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit logistics policy declaration required");
            var declaration = new LogisticsPolicyDeclaration();
            JsonValue stocks = reader.Object("stock") ?? throw reader.Error("stock must be explicit");
            var entries = new JsonReader(stocks, reader.Where + ".stock");
            for (int i = 0; i < stocks.Count; i++) {
                var entry = entries.Child(stocks.KeyAt(i));
                declaration._stocks.Add((stocks.KeyAt(i), LogisticsDemandSettings.Read(entry))); entry.End();
            }
            entries.End(); var outlet = reader.Child("outlet") ?? throw reader.Error("outlet must be explicit");
            declaration._enabled = outlet.Bool("enabled"); declaration._priority = outlet.Int("export_priority");
            declaration._reserveAccess = LogisticsPolicyRules.ReadReserveAccess(outlet);
            if (!outlet.Has("reserve")) throw outlet.Error("reserve must be explicit");
            declaration._reserve = BillSpec.Read(outlet, "reserve");
            JsonValue filter = outlet.Value("allowed_destinations");
            if (filter == null || filter.Kind != JsonKind.Null && !filter.IsArray) throw outlet.Error("allowed_destinations must be explicit null or an array");
            if (filter.IsArray) {
                declaration._destinations = new string[filter.Count];
                for (int i = 0; i < filter.Count; i++) {
                    if (filter[i].Kind != JsonKind.String) throw outlet.Error("destinations are prototype identities");
                    declaration._destinations[i] = filter[i].Text;
                }
            }
            outlet.End(); declaration.States = EntityStateBindings.Read(reader, "disabled"); return declaration;
        }

        public void Resolve(ContentResolver content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (_reserve == null || Stock != null) throw new InvalidOperationException("logistics declaration already resolved");
            var entries = new List<StockPolicyEntry>();
            foreach (var entry in _stocks)
            {
                ResourceId resource = content.Resource(entry.Resource);
                if (!entry.Settings.Selection.IsNone)
                {
                    var selected = content.Def<IResourceLotSelectionDefinition>(entry.Settings.Selection.Id);
                    if (selected.Resource != resource) throw content.Fail("stock quality selection must match actual resource");
                }
                entries.Add(new StockPolicyEntry(resource, entry.Settings));
            }
            entries.Sort((a, b) => a.Resource.Value.CompareTo(b.Resource.Value)); Stock = entries.AsReadOnly();
            try { Outlet = new OutletPolicySettings(_enabled, _reserve.Resolve(content), _priority, _destinations, _reserveAccess); }
            catch (ArgumentException error) { throw content.Fail(error.Message); }
            if (Outlet.AllowedDestinations != null) foreach (string id in Outlet.AllowedDestinations) content.Def<EntityDef>(id);
            States = States.Resolve(content); _reserve = null;
        }

        public static void Validate(ResolvedContent content, ILogisticsPolicyDefinition profile)
        {
            if (content == null || profile == null || !content.Owns(profile) ||
                !ReferenceEquals(content.Find<ILogisticsPolicyDefinition>(profile.Id), profile))
                throw new ContentException("logistics policy requires actual catalog identity");
            var stock = profile.Stock; var outlet = profile.Outlet; var states = profile.States;
            if (stock == null || outlet == null || states == null || states.Definitions.Count != 1 ||
                !states.Definitions.TryGetValue("disabled", out var disabled) || disabled == null ||
                !ReferenceEquals(content.Find<IEntityStateDefinition>(disabled.Id), disabled))
                throw new ContentException(profile.Id + ": explicit stock/outlet/disabled state aliases required");
            int previous = -1;
            foreach (var entry in stock) {
                var resource = content.Resource(entry.Resource);
                if (resource == null || entry.Resource.Value <= previous || entry.Settings == null)
                    throw new ContentException(profile.Id + ": stock entries require unique canonical actual resources/settings");
                if (!LogisticsService.ValidSelection(content, entry.Resource, entry.Settings.Selection)) throw new ContentException(profile.Id + ": invalid stock quality selection");
                Exact(resource.QuantityUnit.Discrete, entry.Settings.Minimum, profile.Id);
                Exact(resource.QuantityUnit.Discrete, entry.Settings.Desired, profile.Id);
                Exact(resource.QuantityUnit.Discrete, entry.Settings.Maximum, profile.Id);
                previous = entry.Resource.Value;
            }
            foreach (var line in outlet.Reserve.Lines) {
                var resource = content.Resource(line.Resource);
                if (resource == null) throw new ContentException(profile.Id + ": reserve needs actual resource");
                Exact(resource.QuantityUnit.Discrete, line.Amount, profile.Id);
            }
            if (outlet.AllowedDestinations != null) foreach (string destination in outlet.AllowedDestinations)
                if (content.Find<EntityDef>(destination) == null) throw new ContentException(profile.Id + ": unknown export destination");
        }
        private static void Exact(bool discrete, Fix value, string id)
        {
            if (value < Fix.Zero || discrete && value.Raw % Fix.One.Raw != 0)
                throw new ContentException(id + ": policy quantities must use exact nonnegative native resource units");
        }
    }
}
