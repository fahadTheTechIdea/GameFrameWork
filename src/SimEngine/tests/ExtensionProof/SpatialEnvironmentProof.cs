using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentSpatialEnvironmentalMetricDef : Def, ISpatialResourceEnvironmentalMetricDefinition
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

    public sealed class SpatialEnvironmentProofModule : GameModule
    {
        public override string Id => "proof.spatial-environment";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(EnvironmentalMetricsModule.ModuleId, Version), new ModuleRequirement(SpatialResourcesModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
            => types.Section<IndependentSpatialEnvironmentalMetricDef>("independent_spatial_environmental_metrics");
    }
}
