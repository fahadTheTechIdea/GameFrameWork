using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IResourceFlowQueryDefinition : IContentDefinition
    {
        ResourceId Resource { get; }
        IReadOnlyList<ResourceFlowKind> Kinds { get; }
        GameDuration Window { get; }
        DurationUnit Per { get; }
        IResourceLotSelectionDefinition Selection { get; }
    }
    /// <summary>One shared native/mod authoring path, not another flow journal or selection model.</summary>
    public sealed class ResourceFlowQueryDeclaration
    {
        private readonly string _resource, _selection;
        public ResourceId Resource { get; private set; }
        public IReadOnlyList<ResourceFlowKind> Kinds { get; }
        public GameDuration Window { get; }
        public DurationUnit Per { get; }
        public IResourceLotSelectionDefinition Selection { get; private set; }
        private ResourceFlowQueryDeclaration(JsonReader reader)
        {
            _resource = reader.String("resource"); _selection = reader.NullableString("selection");
            Window = (reader.Child("window") ?? throw reader.Error("explicit positive flow window required")).RationalDuration();
            if (Window.IsZero) throw reader.Error("flow window must be positive");
            Per = JsonReader.TimeUnit(reader.String("per"), reader);
            var kinds = new List<ResourceFlowKind>();
            foreach (string kind in reader.Strings("kinds"))
            {
                switch (kind)
                {
                    case "stock_transfer": kinds.Add(ResourceFlowKind.StockTransfer); break;
                    case "stock_purchase": kinds.Add(ResourceFlowKind.StockPurchase); break;
                    case "authored_creation": kinds.Add(ResourceFlowKind.AuthoredCreation); break;
                    case "initialized": kinds.Add(ResourceFlowKind.Initialized); break;
                    case "produced": kinds.Add(ResourceFlowKind.Produced); break;
                    case "consumed": kinds.Add(ResourceFlowKind.Consumed); break;
                    case "destroyed": kinds.Add(ResourceFlowKind.Destroyed); break;
                    case "lost": kinds.Add(ResourceFlowKind.Lost); break;
                    case "sold": kinds.Add(ResourceFlowKind.Sold); break;
                    default: throw reader.Error("unknown resource flow kind");
                }
            }
            kinds.Sort();
            if (kinds.Count == 0) throw reader.Error("explicit nonempty flow kinds required");
            for (int i = 1; i < kinds.Count; i++) if (kinds[i] == kinds[i - 1]) throw reader.Error("duplicate flow kind");
            Kinds = kinds.AsReadOnly();
        }
        public static ResourceFlowQueryDeclaration Read(JsonReader reader) => new ResourceFlowQueryDeclaration(reader);
        public void Resolve(ContentResolver content)
        { Resource = content.Resource(_resource); Selection = content.OptionalDef<IResourceLotSelectionDefinition>(_selection); }
        public static void Validate(ResolvedContent content, IResourceFlowQueryDefinition definition)
        {
            if (!(definition is Def actual) || !actual.IsFrozen || definition is IPersistentService || definition.Id != actual.Id || definition.Module != actual.Module ||
                !content.Owns(definition) || !ReferenceEquals(content.Find<IResourceFlowQueryDefinition>(definition.Id), definition) ||
                content.Resource(definition.Resource) == null || definition.Window.IsZero ||
                definition.Selection != null && (!ReferenceEquals(content.Find<IResourceLotSelectionDefinition>(definition.Selection.Id), definition.Selection) || definition.Selection.Resource != definition.Resource))
                throw new ContentException("flow query requires actual owned resource/selection and positive window");
            try { _ = new ResourceFlowQuery(PlayerId.None, definition.Resource, definition.Kinds, 0, 0, definition.Per); }
            catch (ArgumentException error) { throw new ContentException("invalid flow query: " + error.Message); }
        }
        public static ResourceFlowQuery Create(IResourceFlowQueryDefinition definition, PlayerId company, ISimulationTime time, long throughTick)
        {
            long window = time.Ticks(definition.Window);
            return new ResourceFlowQuery(company, definition.Resource, definition.Kinds, throughTick > window ? throughTick - window : 0,
                throughTick, definition.Per, selection: definition.Selection == null ? Code.None : new Code(definition.Selection.Id));
        }
    }
    public sealed class ResourceFlowQueryDef : Def, IResourceFlowQueryDefinition
    {
        private ResourceFlowQueryDeclaration _declaration;
        public ResourceId Resource => _declaration.Resource;
        public IReadOnlyList<ResourceFlowKind> Kinds => _declaration.Kinds;
        public GameDuration Window => _declaration.Window;
        public DurationUnit Per => _declaration.Per;
        public IResourceLotSelectionDefinition Selection => _declaration.Selection;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceFlowQueryDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
}
