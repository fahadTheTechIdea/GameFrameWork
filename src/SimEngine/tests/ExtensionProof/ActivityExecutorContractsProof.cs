using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentFixedExecutorDef : Def, IActivityExecutor
    {
        private FixedActivityExecution _execution;
        protected override void ReadCore(JsonReader reader) => _execution = FixedActivityExecution.Read(reader);
        public void ValidateDefinition(IActivityNodeDefinition definition) => _execution.ValidateDefinition(definition);
        public void ValidateComposition(World world) => _execution.ValidateComposition(world);
        public IReadOnlyList<Toil> CreateItinerary(ActivityExecutorContext context) => _execution.CreateItinerary(context);
        public bool MatchesItinerary(ActivityExecutorContext context, IReadOnlyList<Toil> itinerary) => _execution.MatchesItinerary(context, itinerary);
        public Code CheckWorker(WorkRequirementContext context) => _execution.CheckWorker(context);
        public ActivityApproachResult Reapproach(ActivityExecutorContext context, int current) => _execution.Reapproach(context, current);
        public Fix ReadRate(ActivityExecutorContext context) => _execution.ReadRate(context);
    }
    public sealed class IndependentTimedExecutorDef : Def, IActivityExecutor
    {
        private TimedActivityExecution _execution;
        protected override void ReadCore(JsonReader reader) => _execution = TimedActivityExecution.Read(reader);
        public void ValidateDefinition(IActivityNodeDefinition definition) => _execution.ValidateDefinition(definition);
        public void ValidateComposition(World world) => _execution.ValidateComposition(world);
        public IReadOnlyList<Toil> CreateItinerary(ActivityExecutorContext context) => _execution.CreateItinerary(context);
        public bool MatchesItinerary(ActivityExecutorContext context, IReadOnlyList<Toil> itinerary) => _execution.MatchesItinerary(context, itinerary);
        public Code CheckWorker(WorkRequirementContext context) => _execution.CheckWorker(context);
        public ActivityApproachResult Reapproach(ActivityExecutorContext context, int current) => _execution.Reapproach(context, current);
        public Fix ReadRate(ActivityExecutorContext context) => _execution.ReadRate(context);
    }

    public sealed class ActivityExecutorContractsModule : GameModule
    {
        public override string Id => "proof.activity-executor-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public readonly InitializeProbe Initialize = new InitializeProbe("proof.activity-executor-init");
        public bool ActualAliases { get; private set; }
        public IActivityGraph AuthoredGraph { get; private set; }
        public bool Independent { get; private set; }
        private bool _armed;
        public void ArmObservationFault() => _armed = true;
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentStaffedExecutorDef>("independent_staffed_executors");
            types.Section<IndependentFixedExecutorDef>("independent_fixed_executors");
            types.Section<IndependentTimedExecutorDef>("independent_timed_executors");
            types.Component<ExposurePrivateProbeDef>("executor_private_probe", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            AuthoredGraph = composer.Content.Get<IAuthoredActivityDefinition>("executor.chain").Graph;
            var staffed = composer.Content.Get<IActivityExecutor>("executor.staffed");
            var fixedWork = composer.Content.Get<IActivityExecutor>("executor.fixed");
            var timed = composer.Content.Get<IActivityExecutor>("executor.timed");
            ActualAliases = composer.Content.Owns(staffed) && composer.Content.Owns(fixedWork) && composer.Content.Owns(timed);
            Independent = staffed is IndependentStaffedExecutorDef && fixedWork is IndependentFixedExecutorDef && timed is IndependentTimedExecutorDef;
            foreach (var definition in composer.Content.All<IndependentStaffedExecutorDef>())
            {
                definition.Observer = (phase, world, target, worker) =>
                {
                    // Intentionally invalid callback behavior, never normal work/gameplay implementation.
                    if ((!_armed && phase != "composition") || !definition.Fault.StartsWith(phase + "-", StringComparison.Ordinal)) return;
                    if (definition.Fault.EndsWith("pause", StringComparison.Ordinal)) ((GameClock)world.Clock).Pause();
                    if (definition.Fault.EndsWith("throw", StringComparison.Ordinal)) throw new InvalidOperationException("proof.executor." + phase);
                    if (definition.Fault.EndsWith("target-private", StringComparison.Ordinal)) target.Get<ExposurePrivateProbe>().Corrupt();
                    if (definition.Fault.EndsWith("worker-private", StringComparison.Ordinal)) worker.Get<ExposurePrivateProbe>().Corrupt();
                };
            }
            composer.AddSystem(Initialize, 305);
        }
    }
}
