using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface ISpatialResourceEnvironmentalMetricDefinition : IEnvironmentalMetricDefinition, IContentDependencyDefinition
    {
        IReadOnlyList<ISpatialResourceCellDefinition> Cells { get; }
    }

    /// <summary>Authored observations of original material cells; no copied balance or history.</summary>
    public sealed class SpatialResourceEnvironmentalMetricDeclaration
    {
        private readonly string _unit;
        private readonly string[] _cells;
        public IUnitContentDefinition Unit { get; private set; }
        public IReadOnlyList<ISpatialResourceCellDefinition> Cells { get; private set; }
        private SpatialResourceEnvironmentalMetricDeclaration(JsonReader reader)
        {
            _unit = reader.String("unit");
            if (reader.Array("cells") == null) throw reader.Error("explicit cells array required");
            _cells = reader.Strings("cells");
            if (_cells.Length == 0) throw reader.Error("current stock requires at least one actual cell");
            Array.Sort(_cells, StringComparer.Ordinal);
            for (int i = 0; i < _cells.Length; i++)
                if (i > 0 && _cells[i] == _cells[i - 1]) throw reader.Error("duplicate current-stock cell");
        }
        public static SpatialResourceEnvironmentalMetricDeclaration Read(JsonReader reader)
            => new SpatialResourceEnvironmentalMetricDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Unit != null) throw new InvalidOperationException("spatial metric resolves once");
            Unit = content.Def<IUnitContentDefinition>(_unit);
            var cells = new List<ISpatialResourceCellDefinition>();
            foreach (string id in _cells) cells.Add(content.Def<ISpatialResourceCellDefinition>(id));
            Cells = cells.AsReadOnly();
        }
        public IReadOnlyList<IContentDefinition> References()
        {
            var references = new List<IContentDefinition> { Unit };
            foreach (var cell in Cells) references.Add(cell);
            return references.AsReadOnly();
        }
    }

    /// <summary>One actual-contract admission and observation algorithm, shared by native and external definitions.</summary>
    public static class SpatialResourceEnvironmentalMetricContracts
    {
        public static void ValidateBinding(World world, ISpatialResourceEnvironmentalMetricDefinition metric)
        {
            EnvironmentalMetricContracts.ValidateAlias(world, metric);
            if (!ReferenceEquals(world.Content.Find<ISpatialResourceEnvironmentalMetricDefinition>(metric.Id), metric) ||
                metric.Measure != EnvironmentalMeasure.Stock || metric.Cells == null || metric.Cells.Count == 0)
                throw new ContentException("spatial metric requires its actual stock/cell metadata alias");
            if (!world.Services.Has<ISpatialResourceEndpoints>()) throw new ContentException("spatial metric requires selected actual cell endpoints");
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in metric.Cells)
                EntityObservation.ReadProvider(world, definition, null, () => {
                    if (definition is not Def actual || !actual.IsFrozen || !world.Content.Owns(definition) ||
                        !ReferenceEquals(world.Content.Find<ISpatialResourceCellDefinition>(definition.Id), definition) ||
                        !identities.Add(definition.Id) || definition.Layer?.Resource?.QuantityUnit.Unit != metric.Unit.Unit)
                        throw new ContentException("spatial metric requires actual cells with the exact same native unit");
                    SpatialResourceCellDeclaration.ValidateMetadata(world.Content, definition.Identity, definition.Layer, definition.Capacity, definition.Public);
                    if (world.Services.Get<ISpatialResourceEndpoints>().ResolveCell(definition) == null)
                        throw new ContentException("spatial metric cell has no actual selected endpoint");
                    return true;
                }, space: world.Space);
        }
        public static EnvironmentalObservation Observe(EnvironmentalContext context, ISpatialResourceEnvironmentalMetricDefinition metric)
        {
            if (!ReferenceEquals(context.Definition, metric)) throw new InvalidOperationException("spatial stock observation requires its actual metric context");
            ValidateBinding(context.World, metric);
            var world = context.World; var endpoints = world.Services.Get<ISpatialResourceEndpoints>();
            BigInteger raw = BigInteger.Zero; long samples = 0;
            foreach (var definition in metric.Cells)
            {
                if (!world.ViewFor(context.Company).CanSeeContent(definition))
                    return EnvironmentalObservation.Refused(new EnvironmentalQuery(context.Company, context.Definition.Id), Codes.UnknownDef);
                var cell = endpoints.ResolveCell(definition);
                if (cell == null) return EnvironmentalObservation.Refused(new EnvironmentalQuery(context.Company, context.Definition.Id), Codes.NotFound);
                if (cell.Company != context.Company && !cell.Public)
                    return EnvironmentalObservation.Refused(new EnvironmentalQuery(context.Company, context.Definition.Id), Codes.NotOwned);
                Fix quantity = cell.Amount(definition.Layer.Resource.Resource);
                if (UnitConversion.ValidateAmount(definition.Layer.Resource.QuantityUnit, quantity) != UnitConversionStatus.Converted)
                    throw new InvalidOperationException("spatial stock provider returned invalid native material");
                raw += quantity.Raw;
                if (quantity.IsPositive) samples = checked(samples + 1);
            }
            return EnvironmentalObservation.Measured(context, raw, samples);
        }
    }

    public sealed class SpatialResourceEnvironmentalMetricDef : Def, ISpatialResourceEnvironmentalMetricDefinition
    {
        private SpatialResourceEnvironmentalMetricDeclaration _declaration;
        public IUnitContentDefinition Unit => _declaration.Unit;
        public EnvironmentalMeasure Measure => EnvironmentalMeasure.Stock;
        public GameDuration? Window => null;
        public DurationUnit? RateUnit => null;
        public IReadOnlyList<ISpatialResourceCellDefinition> Cells => _declaration.Cells;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.References();
        protected override void ReadCore(JsonReader reader) => _declaration = SpatialResourceEnvironmentalMetricDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(World world) => SpatialResourceEnvironmentalMetricContracts.ValidateBinding(world, this);
        public EnvironmentalObservation Observe(EnvironmentalContext context) => SpatialResourceEnvironmentalMetricContracts.Observe(context, this);
    }
}
