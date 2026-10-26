using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Authored intent, ordinary commands/clock and immutable engine facts; never worker/effect logic.
    internal static class WorkerEffectContractTests
    {
        internal const string Case = "Worker effect contracts: earned subject changes and scoped faults";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(WorkerEffectContractsModule module, bool independent = true, Action<JsonValue, JsonValue> edit = null)
        {
            var authored = Json.Parse(FrameworkProofContent.Read("worker-effect-contracts.json"));
            var external = JsonValue.NewObject();
            external.Set("independent_worker_condition_effects", independent ? authored.Get("worker_condition_effects").Clone() : JsonValue.NewArray());
            if (independent) authored.Set("worker_condition_effects", JsonValue.NewArray());
            edit?.Invoke(authored, external);
            return ConstructionPlanTests.Points(extraModules: new GameModule[] { new EquipmentConditionModule(), module },
                extraDeclarations: "[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\",\"documents\":[\"worker-effect-definitions.json\"]},{\"id\":\"proof.worker-effect-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\"},{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"worker-effect-contracts.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["worker-effect-definitions.json"] = authored.ToString(), ["worker-effect-contracts.json"] = external.ToString() },
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(EquipmentConditionModule.ModuleId)); scenario.Get("modules").Add(JsonValue.Of(module.Id));
                }, worldEdit: world => {
                    var condition = Json.Parse("{\"id\":\"integrity\",\"unit\":\"unit.condition\",\"maximum\":10,\"initial\":10,\"wear\":null,\"states\":{\"damaged\":\"blocked\",\"failed\":\"blocked\"}}");
                    world.Get("entities")[0].Get("components").Set("equipment_condition", condition.Clone());
                    var worker = world.Get("entities")[2].Get("components"); worker.Set("equipment_condition", condition);
                    worker.Get("storage").Set("initial_stock", Json.Parse("{\"supply\":3}"));
                    worker.Set("worker_effect_probe", Json.Parse("{\"id\":\"worker.effect-probe\"}"));
                    StartupContentAuthoring.Position(world, "machine", JsonValue.Number("4"));
                });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static EntityId Entity(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static ConditionSnapshot Condition(SimGame game, string definition)
        {
            Code refusal = game.Seat(0).View.Feature<EquipmentConditionView>().Read(Entity(game, definition), new Code("integrity"), out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("missing actual condition: " + refusal); return facts;
        }
        private static Fix Stock(SimGame game) => TransformationContentContractTests.Stock(game, "source") +
            TransformationContentContractTests.Stock(game, "destination") + TransformationContentContractTests.Stock(game, "machine");
        private static WorkView Work(SimGame game, int company = 0) => game.Seat(company).View.Feature<WorkView>();
        private static List<ActivityNodeView> Nodes(SimGame game, int order)
        { var rows = new List<ActivityNodeView>(); Work(game).TryCopyNodes(order, rows); return rows; }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("worker effect step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 480 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " worker=" + Condition(game, "machine").Value + " stock=" + Stock(game));
        }
        private static int Begin(Check c, SimGame game, string plan)
        {
            var policy = new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) };
            c.That(TestKit.Order(game, 0, policy).Accepted, "normal player enables physical machine");
            var command = new PostOrderCommand { Plan = plan, Target = Entity(game, "source"), Priority = 70 };
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal authored activity admission");
            c.Equal(before, game.Hash(), "activity preview pure"); var result = TestKit.Order(game, 0, command);
            c.That(result.Accepted, "normal worker activity starts"); return checked((int)result.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual direct effect build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same original state owners");
            c.That(typeof(IWorkEffect).Assembly.GetType("Beep.SimEngine.Standard.WorkEffectDef") == null &&
                typeof(IWorkEffect).Assembly.GetType("Beep.SimEngine.Standard.PhysicalWorkEffectDef") == null, "obsolete concrete dependency bases deleted");
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Cancellation(c); Invalid(c); Faults(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new WorkerEffectContractsModule(); var factory = Factory(module, independent); var game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash();
            c.That(module.ActualAliases && module.SharedOwners && module.Independent == independent && module.Initialize.Initializations > 0,
                "actual native/independent condition metadata shares original work owner");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "machine condition work does not require business");
            c.Equal(Fix.FromInt(23), Stock(game), "explicit source plus worker opening materials");
            c.Equal(Fix.FromInt(10), Condition(game, "machine").Value, "worker initially intact");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000);
            using (var recorder = game.RecordReplay(limits))
            {
                int order = Begin(c, game, "worker.damage-work");
                Until(game, null, () => Nodes(game, order).Any(n => n.Progress.WholeTicks > 0), "partial worker damage work");
                var retained = Nodes(game, order).Single();
                c.That(retained.Worker == Entity(game, "machine") && retained.RequiredTicks == 8 && retained.Progress.WholeTicks < 8, "real machine earns eight-minute work");
                c.That(((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == retained.Worker).Spatial).Position != Fix.FromInt(4), "machine physically moved to work target");
                c.Equal(Fix.FromInt(21), Stock(game), "two actual worker inputs consumed, tool retained");
                c.Equal(Fix.FromInt(10), Condition(game, "machine").Value, "partial work does not change condition");
                ulong before = game.Hash(); c.Equal(Codes.NotFound, Work(game).PreviewNodeEffect(order, "damage"), "no completed worker subject before earned stage");
                c.Equal(Codes.NotFound, Work(game, 1).PreviewNodeEffect(order, "damage"), "worker effect preview private");
                c.Equal(before, game.Hash(), "incomplete subject preview pure");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial work/resource/tool save");
                long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(at, game.Clock.StepIndex, "sole clock pauses worker work"); game.Clock.Resume(); twin.Clock.Resume();
                Until(game, twin, () => Condition(game, "machine").Value == Fix.FromInt(4), "earned completed-worker damage");
                c.That(Condition(game, "machine").ChangedTick >= at + 6, "worker change has original earned game timestamp");
                c.Equal(Fix.FromInt(10), Condition(game, "source").Value, "work-order target not mistaken for completed worker");
                c.Equal(Fix.FromInt(21), Stock(game), "damage effect creates/refunds no material");
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<EquipmentConditionView>().Read(Entity(game, "machine"), new Code("integrity"), out _), "actual worker condition company private");
                for (int i = 0; i < 12; i++) Step(game, twin);
                c.Equal(Fix.FromInt(4), Condition(game, "machine").Value, "once-only damage after subsequent updates");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "changed worker codec persists");
                int repair = Begin(c, game, "worker.repair-work");
                Until(game, null, () => Nodes(game, repair).Any(n => n.Progress.WholeTicks > 0), "partial physical worker repair");
                c.Equal(Fix.FromInt(4), Condition(game, "machine").Value, "partial repair never fabricates restoration");
                twin = factory.Load(game.Save());
                Until(game, twin, () => Condition(game, "machine").Value == Fix.FromInt(10), "actual worker repair completion");
                c.Equal(Fix.FromInt(10), Condition(game, "source").Value, "repair affects worker only"); c.Equal(Fix.FromInt(21), Stock(game), "held repair tool unchanged");
                c.That(retained.Progress.WholeTicks < 8, "retained partial node remains detached");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed worker repair save");
                c.Equal(idleHash, idle.Hash(), "interleaved session condition/work/materials isolated");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary earned worker damage/repair stream replays");
            }
        }
        private static void Cancellation(Check c)
        {
            var factory = Factory(new WorkerEffectContractsModule()); var game = New(factory); int order = Begin(c, game, "worker.damage-work");
            Until(game, null, () => Nodes(game, order).Any(n => n.Progress.WholeTicks > 0), "cancel partial worker work");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order }).Accepted, "ordinary cancel before earned condition effect");
            for (int i = 0; i < 12; i++) Step(game);
            c.Equal(Fix.FromInt(10), Condition(game, "machine").Value, "cancelled work cannot damage machine");
            c.Equal(Fix.FromInt(21), Stock(game), "spent inputs stay spent, no fake refund"); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancel saves native released commitments");
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-source", "invalid-source", "missing-kind", "unknown-kind", "unknown-unit", "wrong-unit-kind",
                "zero-amount", "quantum", "unknown-key", "unused-unknown-unit", "whole-order-worker", "timer-worker", "unrelated-machine" })
            {
                var module = new WorkerEffectContractsModule();
                c.Throws<ContentException>(() => New(Factory(module, edit: (authored, external) => {
                    var effect = external.Get("independent_worker_condition_effects")[0];
                    if (mode == "missing-source") effect.Remove("source");
                    if (mode == "invalid-source") effect.Set("source", JsonValue.Of("bad source"));
                    if (mode == "missing-kind") effect.Remove("kind");
                    if (mode == "unknown-kind") effect.Set("kind", JsonValue.Of("heal_instantly"));
                    if (mode == "unknown-unit") effect.Set("unit", JsonValue.Of("missing"));
                    if (mode == "wrong-unit-kind") effect.Set("unit", JsonValue.Of("inspect"));
                    if (mode == "zero-amount") effect.Set("amount", JsonValue.Number("0"));
                    if (mode == "quantum") { effect.Set("unit", JsonValue.Of("unit.supply")); effect.Set("amount", JsonValue.Number("0.5")); }
                    if (mode == "unknown-key") effect.Set("legacy_worker", JsonValue.Of(true));
                    if (mode == "unused-unknown-unit") { var unused = effect.Clone(); unused.Set("id", JsonValue.Of("unused.condition")); unused.Set("unit", JsonValue.Of("missing")); external.Get("independent_worker_condition_effects").Add(unused); }
                    var activity = authored.Get("activities")[0]; var node = activity.Get("nodes")[0];
                    if (mode == "whole-order-worker") { node.Remove("effect"); activity.Set("on_complete", JsonValue.Of("worker.damage")); }
                    if (mode == "timer-worker" || mode == "unrelated-machine") { node.Set("executor", JsonValue.Of("work.timed")); node.Remove("roles"); node.Remove("requirement"); }
                    if (mode == "unrelated-machine") activity.Get("nodes").Add(Json.Parse("{\"id\":\"unrelated\",\"executor\":\"work.staffed\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}}"));
                })), "invalid actual worker effect: " + mode);
                c.Equal(0, module.Initialize?.Initializations ?? 0, "invalid worker definition never initializes: " + mode);
            }
        }
        private static void Faults(Check c)
        {
            foreach (WorkerEffectFault fault in Enum.GetValues(typeof(WorkerEffectFault)))
            {
                var module = new WorkerEffectContractsModule(); var game = New(Factory(module)); var worker = Entity(game, "machine"); Begin(c, game, "worker.damage-work"); module.Arm(fault);
                long at = game.Clock.StepIndex;
                for (int i = 0; i < 480 && game.Fault == null; i++) { at = game.Clock.StepIndex; game.Update(); }
                c.That(game.Fault?.Phase == "work.effect" && module.FaultCallbacks == 1 && module.ActualSubject == worker,
                    "actual resolved worker callback faults once: " + fault);
                c.That(module.MutationCompleted == (fault != WorkerEffectFault.ThrowCommit), "actual worker mutation completed before guard");
                c.Equal(at, game.Clock.StepIndex, "failed worker completion withholds game time"); c.Equal(0, game.Update(), "terminal worker fault cannot resume");
                c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy worker fault hash"); c.Throws<InvalidOperationException>(() => game.Save(), "no worker fault save");
            }
        }
    }
}
