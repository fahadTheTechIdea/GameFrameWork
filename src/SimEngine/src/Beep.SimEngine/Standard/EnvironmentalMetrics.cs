using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum EnvironmentalMeasure : byte { Flow = 1, Stock = 2 }
    public static class EnvironmentalCodes
    {
        public static readonly Code HistoryExpired = new Code("environment.history-expired");
        public static readonly Code NoElapsedTime = new Code("environment.no-elapsed-time");
        public static readonly Code Threshold = new Code("environment.threshold");
        public static readonly Code StockHasNoRate = new Code("environment.stock-has-no-rate");
    }
    public readonly struct EnvironmentalQuery
    {
        public PlayerId Company { get; }
        public string Metric { get; }
        public EnvironmentalQuery(PlayerId company, string metric) { Company = company; Metric = metric; }
    }
    public sealed class EnvironmentalObservation
    {
        public string Metric { get; }
        public PlayerId Company { get; }
        public UnitId Unit { get; }
        public EnvironmentalMeasure? Measure { get; }
        public BigInteger? AmountRaw { get; }
        public bool Available => AmountRaw.HasValue;
        public long Facts { get; }
        public long FromTick { get; }
        public long ThroughTick { get; }
        public Code Blocker { get; }
        public MeasuredRate? Rate { get; }
        public Code RateBlocker { get; }
        private EnvironmentalObservation(string metric, PlayerId company, UnitId unit, BigInteger? amount, long facts,
            long from, long through, Code blocker, MeasuredRate? rate, Code rateBlocker, EnvironmentalMeasure? measure = null)
        { Metric = metric; Company = company; Unit = unit; AmountRaw = amount; Facts = facts; FromTick = from; ThroughTick = through;
            Blocker = blocker; Rate = rate; RateBlocker = rateBlocker; Measure = measure; }
        public static EnvironmentalObservation Refused(EnvironmentalQuery query, Code blocker)
        {
            if (blocker.IsNone) throw new ArgumentException("unavailable environmental metric needs a blocker");
            return new EnvironmentalObservation(query.Metric, query.Company, default, null, 0, 0, 0, blocker, null, blocker);
        }
        public static EnvironmentalObservation Measured(EnvironmentalContext context, BigInteger amountRaw, long facts)
        {
            if (amountRaw < 0 || facts < 0 || (amountRaw.IsZero != (facts == 0))) throw new ArgumentException("invalid environmental amount/count");
            long elapsed = context.ThroughTick - context.FromTick;
            bool stock = context.Definition.Measure == EnvironmentalMeasure.Stock;
            MeasuredRate? rate = stock || elapsed == 0 ? (MeasuredRate?)null : MeasuredRate.Calculate(amountRaw, elapsed, context.World.Time, context.Definition.RateUnit.Value);
            return new EnvironmentalObservation(context.Definition.Id, context.Company, context.Definition.Unit.Unit, amountRaw,
                facts, context.FromTick, context.ThroughTick, Code.None, rate,
                stock ? EnvironmentalCodes.StockHasNoRate : elapsed == 0 ? EnvironmentalCodes.NoElapsedTime : Code.None, context.Definition.Measure);
        }
    }
    public readonly struct EnvironmentalContext
    {
        public World World { get; }
        public PlayerId Company { get; }
        public IEnvironmentalMetricDefinition Definition { get; }
        public long FromTick { get; }
        public long ThroughTick { get; }
        internal EnvironmentalContext(World world, PlayerId company, IEnvironmentalMetricDefinition definition)
        { World = world; Company = company; Definition = definition; ThroughTick = world.Tick;
            FromTick = definition.Measure == EnvironmentalMeasure.Stock ? world.Tick :
                definition.Window.HasValue ? Math.Max(0, world.Tick - world.Time.ExactTicks(definition.Window.Value)) : 0; }
    }
    public interface IEnvironmentalMetricDefinition : IContentDefinition
    {
        IUnitContentDefinition Unit { get; }
        EnvironmentalMeasure Measure { get; }
        GameDuration? Window { get; }
        DurationUnit? RateUnit { get; }
        void ValidateBinding(World world);
        EnvironmentalObservation Observe(EnvironmentalContext context);
    }
    public interface IEnvironmentalMetricSource
    { EnvironmentalObservation Read(EnvironmentalQuery query); }
    /// <summary>Sole strict authored unit/window/rate declaration; no metric state or history.</summary>
    public sealed class EnvironmentalMetricDeclaration
    {
        private readonly string _unit;
        public IUnitContentDefinition Unit { get; private set; }
        public GameDuration? Window { get; }
        public DurationUnit RateUnit { get; }
        private EnvironmentalMetricDeclaration(JsonReader reader)
        {
            _unit = reader.String("unit"); RateUnit = JsonReader.TimeUnit(reader.String("rate_unit"), reader);
            JsonValue window = reader.Value("window") ?? throw reader.Error("explicit window duration or null required");
            if (window.Kind != JsonKind.Null)
            {
                Window = new JsonReader(window, reader.Where + ".window").RationalDuration();
                if (Window.Value.IsZero) throw reader.Error("window must be positive");
            }
        }
        public static EnvironmentalMetricDeclaration Read(JsonReader reader)
            => new EnvironmentalMetricDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver resolver)
        {
            if (Unit != null) throw new InvalidOperationException("environmental declaration resolves once");
            Unit = resolver.Def<IUnitContentDefinition>(_unit);
        }
    }
    public static class EnvironmentalMetricContracts
    {
        public static void ValidateAlias(World world, IEnvironmentalMetricDefinition definition)
            => EntityObservation.ReadProvider(world, definition, null, () => {
                if (!(definition is Def actual) || !actual.IsFrozen || definition is IPersistentService ||
                    !world.Content.Owns(definition) || !ReferenceEquals(world.Content.Find<IEnvironmentalMetricDefinition>(definition.Id), definition))
                    throw new ContentException("environmental metric needs its actual frozen immutable content alias");
                var unit = definition.Unit;
                if (unit == null || !world.Content.Owns(unit) ||
                    !ReferenceEquals(world.Content.Find<IUnitContentDefinition>(unit.Id), unit) ||
                    definition.Measure != EnvironmentalMeasure.Flow && definition.Measure != EnvironmentalMeasure.Stock)
                    throw new ContentException(definition.Id + ": actual unit and explicit measurement kind required");
                if (definition.Measure == EnvironmentalMeasure.Flow ?
                    !definition.RateUnit.HasValue || definition.RateUnit < DurationUnit.Second || definition.RateUnit > DurationUnit.Day :
                    definition.RateUnit.HasValue || definition.Window.HasValue)
                    throw new ContentException(definition.Id + ": flows require rate units; current stock has no rate or history window");
                if (definition.Window.HasValue)
                {
                    if (definition.Window.Value.IsZero) throw new ContentException(definition.Id + ": window must be positive");
                    try { _ = world.Time.ExactTicks(definition.Window.Value); }
                    catch (Exception error) when (error is ArgumentException || error is OverflowException)
                    { throw new ContentException(definition.Id + ": window does not fit the session quantum: " + error.Message); }
                }
                return true;
            });
    }
    /// <summary>Authored classification of committed physical facts, not a pollution stock owner.</summary>
    public sealed class ResourceEnvironmentalMetricDeclaration
    {
        private string[] _resources;
        public IReadOnlyList<IResourceDefinition> Resources { get; private set; }
        public IReadOnlyList<ResourceFlowKind> Kinds { get; private set; }
        public IReadOnlyList<Code> Causes { get; private set; }
        private string _transferCell;
        public ISpatialResourceCellDefinition TransferCell { get; private set; }
        public SpatialResourceTransferDirection TransferDirection { get; private set; }
        private readonly EnvironmentalMetricDeclaration _policy;
        public IUnitContentDefinition Unit => _policy.Unit;
        public GameDuration? Window => _policy.Window;
        public DurationUnit RateUnit => _policy.RateUnit;
        private ResourceEnvironmentalMetricDeclaration(JsonReader reader)
        {
            _policy = EnvironmentalMetricDeclaration.Read(reader); _resources = RequiredIds(reader, "resources");
            string[] kinds = RequiredIds(reader, "kinds");
            var selected = new List<ResourceFlowKind>(); var causeCodes = new List<Code>();
            foreach (string kind in kinds)
                switch (kind)
                {
                    case "produced": selected.Add(ResourceFlowKind.Produced); break;
                    case "consumed": selected.Add(ResourceFlowKind.Consumed); break;
                    case "destroyed": selected.Add(ResourceFlowKind.Destroyed); break;
                    case "lost": selected.Add(ResourceFlowKind.Lost); break;
                    case "stock_transfer": selected.Add(ResourceFlowKind.StockTransfer); break;
                    default: throw reader.Error("environmental kind must select explicit physical lifecycle or scoped stock_transfer facts");
                }
            bool transfer = selected.Contains(ResourceFlowKind.StockTransfer);
            string[] causes = RequiredIds(reader, "causes", allowEmpty: transfer);
            if (transfer)
            {
                if (selected.Count != 1 || causes.Length != 0) throw reader.Error("scoped stock transfers require only stock_transfer and explicitly empty lifecycle causes");
                var selector = new JsonReader(reader.Object("transfer") ?? throw reader.Error("explicit transfer selector required"), reader.Where + ".transfer");
                _transferCell = selector.String("cell"); string direction = selector.String("direction");
                TransferDirection = direction == "deposit" ? SpatialResourceTransferDirection.Deposit : direction == "withdraw" ? SpatialResourceTransferDirection.Withdraw :
                    throw selector.Error("transfer direction must be deposit or withdraw");
                selector.End();
            }
            selected.Sort(); Kinds = selected.AsReadOnly(); foreach (string cause in causes) causeCodes.Add(new Code(cause)); Causes = causeCodes.AsReadOnly();
        }
        private static string[] RequiredIds(JsonReader reader, string key, bool allowEmpty = false)
        {
            if (reader.Array(key) == null) throw reader.Error("explicit " + key + " required");
            string[] ids = reader.Strings(key); if (!allowEmpty && ids.Length == 0) throw reader.Error(key + " cannot be empty");
            Array.Sort(ids, StringComparer.Ordinal);
            for (int i = 0; i < ids.Length; i++)
            {
                try
                {
                    if (key == "causes") _ = new Code(ids[i]);
                    else ContractIdentity.ValidateId(ids[i], key);
                }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
                if (i > 0 && ids[i] == ids[i - 1]) throw reader.Error("duplicate " + key);
            }
            return ids;
        }
        public static ResourceEnvironmentalMetricDeclaration Read(JsonReader reader)
            => new ResourceEnvironmentalMetricDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver resolver)
        {
            _policy.Resolve(resolver); var resources = new List<IResourceDefinition>();
            foreach (string id in _resources)
            { IResourceContentDefinition resource = resolver.Def<IResourceContentDefinition>(id); if (resource.QuantityUnit.Unit != Unit.Unit) throw resolver.Fail("metric resources require the exact same native quantity unit"); resources.Add(resource); }
            Resources = resources.AsReadOnly();
            if (_transferCell != null)
                TransferCell = resolver.Def<ISpatialResourceCellDefinition>(_transferCell);
        }
        public IReadOnlyList<IContentDefinition> References()
        {
            var references = new List<IContentDefinition> { Unit };
            foreach (var resource in Resources) references.Add((IContentDefinition)resource);
            if (TransferCell != null) references.Add(TransferCell);
            return references.AsReadOnly();
        }
        public void ValidateBinding(World world)
        {
            if (!world.Services.Has<IResourceFlowJournal>()) throw new ContentException("physical metrics require selected IResourceFlowJournal");
            if (TransferCell == null) return;
            if (!world.Services.Has<ISpatialResourceEndpoints>() || !world.Content.Owns(TransferCell) ||
                !ReferenceEquals(world.Content.Find<ISpatialResourceCellDefinition>(TransferCell.Id), TransferCell))
                throw new ContentException("scoped transfer metric requires actual selected cell metadata/endpoints");
            if (TransferCell.Layer?.Resource == null || Resources.Count != 1 || Resources[0].Resource != TransferCell.Layer.Resource.Resource)
                throw new ContentException("scoped transfer metric must select its cell's exact native resource");
            SpatialResourceCellDeclaration.ValidateMetadata(world.Content, TransferCell.Identity, TransferCell.Layer, TransferCell.Capacity, TransferCell.Public);
            if (world.Services.Get<ISpatialResourceEndpoints>().ResolveCell(TransferCell) == null)
                throw new ContentException("scoped transfer metric cell has no actual endpoint");
        }
        private bool Matches(ResourceId resource, ResourceFlowKind kind, Code cause, ResourceFlowAttribution attribution)
        {
            bool hasResource = false, hasKind = false, hasCause = false;
            foreach (var value in Resources) if (value.Resource == resource) hasResource = true;
            foreach (var value in Kinds) if (value == kind) hasKind = true;
            foreach (var value in Causes) if (value == cause) hasCause = true;
            if (TransferCell == null) return hasResource && hasKind && hasCause;
            var endpoint = new ResourceEndpointId(SpatialResourcesModule.ModuleId, TransferCell.Identity);
            return hasResource && hasKind && cause.IsNone &&
                (TransferDirection == SpatialResourceTransferDirection.Deposit ? attribution.Destination : attribution.Source) == endpoint;
        }
        public EnvironmentalObservation Observe(EnvironmentalContext context)
        {
            IResourceFlowJournal journal = context.World.Services.Get<IResourceFlowJournal>(); BigInteger total = 0; long facts = 0;
            if (!Window.HasValue)
            {
                ResourceFlowSummary summary = journal.ReadSummary(context.Company);
                if (summary.Status != ResourceFlowReadStatus.Ready) return EnvironmentalObservation.Refused(new EnvironmentalQuery(context.Company, context.Definition.Id), Codes.NotSeated);
                foreach (var row in summary.Aggregates) if (Matches(row.Resource, row.Kind, row.Cause, row.Attribution)) { total += row.AmountRaw; facts = checked(facts + row.Facts); }
            }
            else
            {
                ResourceFlowPage page = journal.ReadWindow(context.Company, context.FromTick, context.ThroughTick);
                if (page.Status != ResourceFlowReadStatus.Ready) return EnvironmentalObservation.Refused(new EnvironmentalQuery(context.Company, context.Definition.Id),
                    page.Status == ResourceFlowReadStatus.Expired ? EnvironmentalCodes.HistoryExpired : Codes.InvalidArgument);
                foreach (var entry in page.Entries) if (Matches(entry.Item.Resource, entry.Item.Kind, entry.Item.Cause, entry.Item.Attribution)) { total += entry.Item.Amount.Raw; facts = checked(facts + 1); }
            }
            return EnvironmentalObservation.Measured(context, total, facts);
        }
    }
    public sealed class ResourceEnvironmentalMetricDef : Def, IEnvironmentalMetricDefinition, IContentDependencyDefinition
    {
        private ResourceEnvironmentalMetricDeclaration _declaration;
        public IUnitContentDefinition Unit => _declaration.Unit;
        public EnvironmentalMeasure Measure => EnvironmentalMeasure.Flow;
        public GameDuration? Window => _declaration.Window;
        public DurationUnit? RateUnit => _declaration.RateUnit;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.References();
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceEnvironmentalMetricDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver resolver) => _declaration.Resolve(resolver);
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world);
        public EnvironmentalObservation Observe(EnvironmentalContext context) => _declaration.Observe(context);
    }
    /// <summary>Stateless catalog facade. Journal/providers retain all actual history and gameplay.</summary>
    internal sealed class EnvironmentalMetrics : IEnvironmentalMetricSource, ICompositionValidator
    {
        private readonly World _world;
        internal EnvironmentalMetrics(World world) { _world = world; }
        private bool _observing;
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("foreign environmental owner");
            if (_observing) throw new InvalidOperationException("environmental observations cannot reenter");
            _observing = true;
            try
            {
                foreach (var definition in world.Content.All<IEnvironmentalMetricDefinition>())
                    EntityObservation.ReadProvider(world, definition, null, () => {
                        EnvironmentalMetricContracts.ValidateAlias(world, definition); definition.ValidateBinding(world); return true;
                    });
            }
            finally { _observing = false; }
        }
        public EnvironmentalObservation Read(EnvironmentalQuery query)
        {
            _world.RequireHealthyUpdate();
            if (_observing) throw new InvalidOperationException("environmental observations cannot reenter");
            if (query.Company != PlayerId.None && !_world.Players.IsSeat(query.Company)) return EnvironmentalObservation.Refused(query, Codes.NotSeated);
            var definition = query.Metric == null ? null : _world.Content.Find<IEnvironmentalMetricDefinition>(query.Metric);
            if (definition == null) return EnvironmentalObservation.Refused(query, Codes.UnknownDef);
            _observing = true;
            try
            {
                return EntityObservation.ReadProvider(_world, definition, null, () => {
                    EnvironmentalMetricContracts.ValidateAlias(_world, definition);
                    if (!_world.ViewFor(query.Company).CanSeeContent(definition)) return EnvironmentalObservation.Refused(query, Codes.UnknownDef);
                    var context = new EnvironmentalContext(_world, query.Company, definition);
                    var entities = new List<EntityObservation.Scope>();
                    foreach (Entity entity in _world.Entities.All) entities.Add(EntityObservation.Capture(_world, entity));
                    EnvironmentalObservation observation = definition.Observe(context);
                    foreach (var entity in entities) entity.Verify();
                    if (observation == null || observation.Company != query.Company || observation.Metric != definition.Id ||
                        observation.Available == !observation.Blocker.IsNone) throw new InvalidOperationException("metric provider returned invalid identity/readiness");
                    if (observation.Available && (observation.Measure != definition.Measure || observation.Unit != definition.Unit.Unit || observation.FromTick != context.FromTick || observation.ThroughTick != context.ThroughTick ||
                        observation.AmountRaw.Value < 0 || observation.Facts < 0 || observation.AmountRaw.Value.IsZero != (observation.Facts == 0) ||
                        definition.Measure == EnvironmentalMeasure.Stock && (observation.Rate.HasValue || observation.RateBlocker != EnvironmentalCodes.StockHasNoRate) ||
                        definition.Measure == EnvironmentalMeasure.Flow && context.ThroughTick > context.FromTick && (!observation.Rate.HasValue || !observation.Rate.Value.Equals(MeasuredRate.Calculate(observation.AmountRaw.Value,
                            context.ThroughTick - context.FromTick, _world.Time, definition.RateUnit.Value)) || !observation.RateBlocker.IsNone) ||
                        definition.Measure == EnvironmentalMeasure.Flow && context.ThroughTick == context.FromTick && (observation.Rate.HasValue || observation.RateBlocker != EnvironmentalCodes.NoElapsedTime)))
                        throw new InvalidOperationException("metric provider returned malformed quantity/rate/coverage facts");
                    return observation;
                }, space: _world.Space);
            }
            finally { _observing = false; }
        }
    }
    public sealed class EnvironmentalObjectivePredicateDef : ObjectivePredicateDef
    {
        public override IReadOnlyList<IContentDefinition> ObservationContent => Array.AsReadOnly(new IContentDefinition[] { Metric, (IUnitContentDefinition)Unit });
        private string _metric, _unit;
        private bool _minimum;
        public IEnvironmentalMetricDefinition Metric { get; private set; }
        public Fix Amount { get; private set; }
        public IUnitDefinition Unit { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _metric = reader.String("metric"); _unit = reader.String("unit"); string relation = reader.String("relation");
            if (relation != "at_least" && relation != "at_most") throw reader.Error("explicit metric relation required");
            _minimum = relation == "at_least"; Amount = reader.Fix("amount"); if (Amount < Fix.Zero) throw reader.Error("threshold cannot be negative");
        }
        protected override void ResolveCore(ContentResolver resolver)
        {
            Metric = resolver.Def<IEnvironmentalMetricDefinition>(_metric); Unit = resolver.Def<IUnitContentDefinition>(_unit);
            if (UnitConversion.ValidateAmount(Unit, Amount) != UnitConversionStatus.Converted) throw resolver.Fail("objective threshold must use exact authored native units");
        }
        public override void ValidateBinding(World world)
        {
            if (!world.Services.Has<IEnvironmentalMetricSource>()) throw new ContentException(Id + ": objective needs selected environmental metrics");
            EnvironmentalMetricContracts.ValidateAlias(world, Metric);
            if (Metric.Unit.Unit != Unit.Unit) throw new ContentException(Id + ": objective threshold unit differs from its actual metric");
        }
        public override ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children)
        {
            EnvironmentalObservation observation = context.World.Services.Get<IEnvironmentalMetricSource>().Read(new EnvironmentalQuery(context.Company, Metric.Id));
            if (!observation.Available) return ObjectiveObservation.Invalid(observation.Blocker);
            BigInteger amount = observation.AmountRaw.Value;
            if (_minimum ? amount >= Amount.Raw : amount <= Amount.Raw) return ObjectiveObservation.Complete;
            return ObjectiveObservation.Wait(EnvironmentalCodes.Threshold, _minimum && Amount.IsPositive ? Fix.FromRaw((long)(amount * Fix.One.Raw / Amount.Raw)) : Fix.Zero);
        }
    }
}
