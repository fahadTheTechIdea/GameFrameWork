using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Authored classification over the original engine algorithm. Faults are explicit proof probes.
    public sealed class IndependentEnvironmentalMetricDef : Def, IEnvironmentalMetricDefinition, IContentDependencyDefinition
    {
        private ResourceEnvironmentalMetricDeclaration _declaration;
        private string _fault;
        public IUnitContentDefinition Unit => _fault == "unit-metadata" ? null : _declaration.Unit;
        public GameDuration? Window => _fault == "window-metadata" ? GameDuration.Zero : _declaration.Window;
        public EnvironmentalMeasure Measure => EnvironmentalMeasure.Flow;
        public DurationUnit? RateUnit => _fault == "rate-metadata" ? (DurationUnit)255 : _declaration.RateUnit;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.References();
        protected override void ReadCore(JsonReader reader)
        { _fault = reader.String("fault"); _declaration = ResourceEnvironmentalMetricDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(World world)
        {
            _declaration.ValidateBinding(world);
            if (_fault == "validation-throw") throw new InvalidOperationException("proof.environment.validation");
            if (_fault == "validation-pause") ((GameClock)world.Clock).Pause();
            if (_fault == "validation-reenter") world.Services.Get<IEnvironmentalMetricSource>().Read(new EnvironmentalQuery(new PlayerId(0), Id));
        }
        public EnvironmentalObservation Observe(EnvironmentalContext context)
        {
            if (_fault == "observe-null") return null;
            if (_fault == "observe-throw") throw new InvalidOperationException("proof.environment.observe");
            if (_fault == "observe-pause") ((GameClock)context.World.Clock).Pause();
            if (_fault == "observe-touch") foreach (var entity in context.World.Entities.All) { entity.Touch(); break; }
            if (_fault == "observe-reenter") return context.World.Services.Get<IEnvironmentalMetricSource>().Read(new EnvironmentalQuery(context.Company, Id));
            if (_fault == "observe-identity") return EnvironmentalObservation.Refused(new EnvironmentalQuery(context.Company, Id + ".wrong"), Codes.NotFound);
            return _declaration.Observe(context);
        }
    }
    public sealed class EnvironmentContentModule : ControlContractModule
    {
        public EnvironmentContentModule(InitializeProbe initialize) : base(initialize) { }
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(EnvironmentalMetricsModule.ModuleId, Version) };
        public bool ActualMetricAliases { get; private set; }
        public bool OriginalJournal { get; private set; }
        public override void DeclareContent(ContentTypes types)
        { base.DeclareContent(types); types.Section<IndependentEnvironmentalMetricDef>("independent_environment_metrics"); }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            var metric = composer.Content.Get<IEnvironmentalMetricDefinition>("contract.environment-cleaned");
            var predicate = composer.Content.Get<EnvironmentalObjectivePredicateDef>("environment.cleanup-target");
            ActualMetricAliases = metric.GetType().BaseType == typeof(Def) && composer.Content.Owns(metric) &&
                ReferenceEquals(predicate.Metric, metric) && composer.Content.Owns(metric.Unit);
            OriginalJournal = composer.Services.Get<IResourceFlowJournal>() is IPersistentService state &&
                state.Id == ResourceFlowsModule.ModuleId && state.StateSchemaVersion == 7;
        }
    }
}
