using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Negative-only authored callback probes over the SAME independent declaration/parser.
    public sealed class ActivityIntentProbeDef : IndependentActivityContentDef
    {
        private string _fault, _stage;
        protected override void ReadCore(JsonReader reader)
        {
            _fault = reader.String("intent_fault");
            _stage = reader.String("intent_stage");
            if (_stage != "intent" && _stage != "identity" && _stage != "submission" && _stage != "activation" && _stage != "binding" && _stage != "execution")
                throw reader.Error("explicit plan admission probe stage required");
            if (_fault != "target" && _fault != "clock" && _fault != "space" && _fault != "throw" && _fault != "content" && _fault != "target-content" && _fault != "order")
                throw reader.Error("explicit activity intent probe fault required");
            if (_fault == "order" && _stage != "execution") throw reader.Error("order mutation requires execution identity probe");
            base.ReadCore(reader);
        }
        private void Attack(World world, string stage, JsonValue arguments, Entity target = null, Order order = null)
        {
            if (_stage == stage && (world.Clock.Paused || (_stage == "binding" || _stage == "execution") && world.Clock.StepIndex > 0))
            {
                if (_fault == "content") throw new ContentException("proof.plan-arguments.expected");
                target = target ?? world.Entities.Get(ReadEntityReferences(world, arguments)[0]);
                if (_fault == "target-content") { target.Touch(); throw new ContentException("proof.plan-arguments.expected"); }
                if (_fault == "target") target.Touch();
                else if (_fault == "order") order.Priority = checked(order.Priority + 1);
                else if (_fault == "clock") { if (world.Clock.Paused) ((GameClock)world.Clock).Resume(); else ((GameClock)world.Clock).Pause(); }
                else if (_fault == "space") ((PointSpace)world.Space).SetAccess(false);
                else throw new InvalidOperationException("proof.activity-intent.original");
            }
        }
        public override Code CheckIntent(World world, PlayerId company, Entity target)
        { Attack(world, "intent", null, target); return base.CheckIntent(world, company, target); }
        public override string IntentIdentity(World world, JsonValue arguments)
        { Attack(world, "identity", arguments); return base.IntentIdentity(world, arguments); }
        public override Code CheckSubmission(World world, PlayerId company, JsonValue arguments)
        { Attack(world, "submission", arguments); return base.CheckSubmission(world, company, arguments); }
        public override Code CheckActivation(World world, PlayerId company, JsonValue arguments)
        { Attack(world, "activation", arguments); return base.CheckActivation(world, company, arguments); }
        public override IActivityDefinition BindExecution(World world, EntityId target)
        { Attack(world, "binding", null, world.Entities.Get(target)); return base.BindExecution(world, target); }
        public override string ExecutionIdentity(World world, Order order)
        { Attack(world, "execution", null, world.Entities.Get(order.Target), order); return base.ExecutionIdentity(world, order); }
    }
    /// <summary>Negative engine-side observation probes; no positive game rules or state owner.</summary>
    public sealed class WorkObservationProbeModule : GameModule
    {
        public bool FailFactory, ArmWorker;
        public int FactoryCalls, WorkerCalls;
        public Action<ViewObservationVisibility> ConfigureVisibility;
        public override string Id => "proof.work-observation";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Section<ActivityIntentProbeDef>("intent_probe_activities");
        public override void Compose(WorldComposer composer)
        {
            if (ConfigureVisibility != null)
            {
                var visibility = new ViewObservationVisibility();
                ConfigureVisibility(visibility);
                composer.Services.Bind<IVisibility>(visibility);
            }
            composer.AddViewFeature<ProbeView>("proof.work-observation.arm", 1, (world, viewer) => {
                FactoryCalls++;
                if (FailFactory) throw new InvalidOperationException("proof.work-observation.factory");
                if (ArmWorker)
                {
                    var workers = world.Entities.Components<ContractWorker>();
                    for (int i = 0; i < workers.Count; i++)
                        workers[i].BeforeCan = () => {
                            WorkerCalls++;
                            throw new InvalidOperationException("proof.work-observation.can");
                        };
                }
                return new ProbeView(viewer);
            });
        }
    }
}
