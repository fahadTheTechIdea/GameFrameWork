using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Engine-side content module: a scheduled productive phase, no second clock/state owner.</summary>
    public sealed class AuthoredExecutorProofModule : GameModule
    {
        public override string Id => "proof.activity-executor";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(WorkModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types) => types.Section<DelayedWorkPhaseDef>("delayed_work_phases");
        public override void Compose(WorldComposer composer) => composer.Services.Bind(new DelayedWorkPhase(composer.Content.Get<DelayedWorkPhaseDef>("proof.work.delayed")));
    }
    public sealed class DelayedWorkPhaseDef : Def
    {
        public GameDuration Delay { get; private set; }
        public Code Blocker { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Delay = new JsonReader(reader.Object("delay") ?? throw reader.Error("delay is required"), reader.Where + ".delay").RationalDuration();
            Blocker = new Code(reader.String("blocker"));
        }
    }
    public sealed class DelayedWorkPhase : IWorkPhaseExecutor
    {
        private readonly DelayedWorkPhaseDef _definition;
        public DelayedWorkPhase(DelayedWorkPhaseDef definition) { _definition = definition; }
        public Code Id => new Code(_definition.Id);
        public WorkPhasePurpose Purpose => WorkPhasePurpose.Productive;
        public void Execute(WorkPhaseContext context)
        {
            Order order = context.World.Services.Get<WorkBoard>().Order(context.Job.OrderId)
                ?? throw new InvalidOperationException("scheduled phase requires an actual activity order");
            long start = checked(order.CreatedTick + context.World.Time.Ticks(_definition.Delay));
            if (context.World.Tick < start) context.Pause(_definition.Blocker);
            else context.AdvanceProductiveWork();
        }
    }
}
