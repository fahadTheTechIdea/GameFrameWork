using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class EnvironmentalMetricsModule : GameModule
    {
        public const string ModuleId = "sim.environment-metrics";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(ResourceFlowsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ResourceEnvironmentalMetricDef>("environmental_metrics");
            types.Section<SpatialResourceEnvironmentalMetricDef>("spatial_environmental_metrics");
            types.Section<EnvironmentalObjectivePredicateDef>("environmental_objective_predicates");
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind<IEnvironmentalMetricSource>(new EnvironmentalMetrics(composer.World));
            composer.AddViewFeature(Id, 2, (world, viewer) => new EnvironmentalMetricsView(world, viewer));
        }
    }
}
