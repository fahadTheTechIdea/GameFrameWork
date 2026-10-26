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
    // Authored files, ordinary player commands, one game clock and immutable observations only.
    internal static class PhysicalDispatchContractTests
    {
        internal const string Case = "Physical dispatch contracts: authored effects earn timed manufacturing";
        private static readonly Code Capability = new Code("dispatch.production");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(PhysicalDispatchContractsModule module, bool independent = true, bool node = false,
            Action<JsonValue, JsonValue> edit = null, Action<JsonValue> workEdit = null)
        {
            var effects = Json.Parse(FrameworkProofContent.Read("physical-dispatch-contracts.json"));
            if (!independent) effects.Set("independent_dispatch_effects", JsonValue.NewArray());
            var authored = Json.Parse(FrameworkProofContent.Read("manufacturing-dispatch.json"));
            if (independent) authored.Set("transformation_dispatch_effects", JsonValue.NewArray());
            edit?.Invoke(authored, effects);
            return ManufacturingDispatchTests.Factory(manufacturing: root => {
                root.Set("transformation_dispatch_effects", authored.Get("transformation_dispatch_effects").Clone());
            }, work: root => {
                if (node)
                {
                    var activity = root.Get("entity_activities")[0]; activity.Remove("on_complete");
                    activity.Get("nodes")[0].Set("effect", JsonValue.Of("dispatch.enqueue"));
                }
                workEdit?.Invoke(root);
            }, extension: module, extensionDocument: "physical-dispatch-contracts.json", extensionText: effects.ToString());
        }
        private static SimGame New(GameFactory factory) => ManufacturingDispatchTests.New(factory);
        private static EntityId Target(SimGame game) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "dispatch.factory").Id;
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static TransformationSnapshot Production(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Target(game), Capability, out var result))
                throw new InvalidOperationException("missing actual factory facts");
            return result;
        }
        private static List<ActivityNodeView> Nodes(SimGame game, int order)
        {
            var result = new List<ActivityNodeView>(); Work(game).TryCopyNodes(order, result); return result;
        }
        private static Fix Stock(SimGame game)
        {
            Fix total = Fix.Zero;
            foreach (var entity in game.Seat(0).Snapshot.Entities)
            {
                var rows = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, rows);
                foreach (var row in rows) if (row.Key == Storage.FacetStock && row.Subject == "steel") total += row.Value;
            }
            foreach (var line in Production(game).Wip.Stock.Lines) total += line.Amount;
            return total;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("physical dispatch at=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 900 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " at=" + game.Clock.StepIndex + " phase=" + Production(game).Phase +
                " queue=" + Production(game).QueueCount + " blocker=" + Production(game).Blocker);
        }
        private static int Begin(Check c, SimGame game)
        {
            var command = new PostOrderCommand { Plan = "dispatch.prepare", Target = Target(game), Priority = 50 };
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal private physical preparation preview");
            c.Equal(before, game.Hash(), "prepare preview pure");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary player starts authored preparation");
            return checked((int)result.Arg0);
        }
        private static void Control(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Target(game), Operation = Capability,
                Enabled = true, Target = Fix.One }).Accepted, "player enables original production operation");
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual dispatch contract identity");
            c.Equal(154, EngineInfo.SaveFormat, "same native persistent owners");
            foreach (bool independent in new[] { false, true }) foreach (bool node in new[] { false, true }) Physical(c, independent, node);
            Cancellation(c); Invalid(c); Faults(c);
        }
        private static void Physical(Check c, bool independent, bool node)
        {
            var module = new PhysicalDispatchContractsModule(); var factory = Factory(module, independent, node);
            var game = New(factory); var idle = New(factory); ulong untouched = idle.Hash();
            c.That(module.ActualAliases && module.Direct && module.QueueOwner && module.Independent == independent,
                "direct native/independent actual aliases share sole queue");
            c.That(module.Initialize.Initializations > 0 && !game.Modules.Contains(TycoonModule.ModuleId), "validated initialization without business");
            c.Equal(Fix.FromInt(60), Stock(game), "actual authored opening material");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                int order = Begin(c, game);
                Until(game, null, () => Nodes(game, order).Any(n => n.Progress.WholeTicks > 0), "partial machine preparation");
                var retained = Nodes(game, order);
                c.That(retained.Any(n => n.Worker.IsValid && n.Progress.WholeTicks > 0), "real assigned machine earns physical work");
                c.Equal(0, Production(game).QueueCount, "partial preparation does not enqueue instant production");
                c.Equal(Fix.FromInt(60), Stock(game), "held tool is not consumed");
                ulong before = game.Hash(); Code preview = node ? Work(game).PreviewNodeEffect(order, "finish") : Work(game).PreviewOrderEffect(order);
                c.Equal(Code.None, preview, "engine effect readiness uses actual declared queue");
                c.Equal(Codes.NotOwned, node ? Work(game, 1).PreviewNodeEffect(order, "finish") : Work(game, 1).PreviewOrderEffect(order), "effect preview company private");
                c.Equal(before, game.Hash(), "effect preview never dispatches or consumes");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial preparation uses native saved graph");
                long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(at, game.Clock.StepIndex, "sole clock pauses physical preparation"); game.Clock.Resume(); twin.Clock.Resume();
                Until(game, twin, () => Production(game).QueueCount == 2, "earned final/node dispatch");
                var queued = Production(game);
                c.That(queued.Queue.All(q => q.Transformation == "dispatch.batch" && !q.Repeat && !q.Home.IsValid), "actual authored queue metadata");
                c.That(queued.Phase == TransformationPhase.Idle && queued.Progress.Equals(WorkProgress.Zero), "disabled operation does not inherit preparation progress");
                c.Equal(Fix.FromInt(58), Stock(game), "earned preparation consumes exactly two real materials");
                for (int i = 0; i < 8; i++) Step(game, twin);
                c.Equal(2, Production(game).QueueCount, "existing graph applies dispatch exactly once");
                c.Equal(independent ? 2 : 0, module.Commits, "independent callback runs once in each saved twin; native needs no probe");
                c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Target(game), Capability, out _), "native queue private");
                c.Throws<NotSupportedException>(() => ((IList<TransformationQueueEntry>)queued.Queue).Clear(), "detached queue immutable");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "earned queue reloads original manager");
                Control(c, game); Control(c, twin);
                Until(game, twin, () => Production(game).Phase == TransformationPhase.Working && Production(game).Progress.WholeTicks > 0, "actual material WIP");
                var working = Production(game); c.That(working.Required == 10 && working.Progress.WholeTicks < 10 && working.Wip.Stock.Lines.Single().Amount == Fix.FromInt(3), "real ten-minute native WIP");
                c.Equal(Fix.FromInt(58), Stock(game), "moving WIP conserves native material");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "active WIP/tool save");
                at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(at, game.Clock.StepIndex, "same clock pauses factory work"); game.Clock.Resume(); twin.Clock.Resume();
                Until(game, twin, () => Production(game).QueueCount == 1, "first actual production batch");
                c.Equal(Fix.FromInt(56), Stock(game), "first batch consumes three generates one");
                Until(game, twin, () => Production(game).QueueCount == 0, "second sequential batch");
                c.Equal(Fix.FromInt(54), Stock(game), "two earned batches conserve exact material");
                c.That(Production(game).Wip.Stock.IsEmpty && Production(game).Tools.Held.IsEmpty, "actual WIP/tools released");
                c.Equal(2, queued.QueueCount, "retained snapshot detached");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed native pipeline saves");
                c.Equal(untouched, idle.Hash(), "interleaved session queue/work/materials isolated");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "normal physical command stream replays");
            }
        }
        private static void Cancellation(Check c)
        {
            var factory = Factory(new PhysicalDispatchContractsModule()); var game = New(factory); int order = Begin(c, game);
            Until(game, null, () => Nodes(game, order).Any(n => n.Progress.WholeTicks > 0), "cancel partial preparation");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order }).Accepted, "ordinary cancel before earned dispatch");
            for (int i = 0; i < 8; i++) Step(game);
            c.Equal(0, Production(game).QueueCount, "cancellation cannot dispatch"); c.Equal(Fix.FromInt(60), Stock(game), "held tools unchanged");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled original graph saves");
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-count", "zero-count", "missing-repeat", "repeat-count", "missing-home", "unknown-home",
                "unknown-transformation", "wrong-kind", "unknown-key", "wrong-capability", "unused-invalid-metadata", "timer-only" })
            {
                var module = new PhysicalDispatchContractsModule();
                c.Throws<ContentException>(() => New(Factory(module, edit: (native, effects) => {
                    var row = effects.Get("independent_dispatch_effects")[0];
                    if (mode == "missing-count") row.Remove("count");
                    if (mode == "zero-count") row.Set("count", JsonValue.Number("0"));
                    if (mode == "missing-repeat") row.Remove("repeat");
                    if (mode == "repeat-count") row.Set("repeat", JsonValue.Of(true));
                    if (mode == "missing-home") row.Remove("home");
                    if (mode == "unknown-home") row.Set("home", JsonValue.Of("infer"));
                    if (mode == "unknown-transformation") row.Set("transformation", JsonValue.Of("missing"));
                    if (mode == "wrong-kind") row.Set("transformation", JsonValue.Of("dispatch.factory"));
                    if (mode == "unknown-key") row.Set("legacy_dispatch", JsonValue.Of(true));
                    if (mode == "wrong-capability") row.Set("capability", JsonValue.Of("missing"));
                    if (mode == "unused-invalid-metadata") { var invalid = row.Clone(); invalid.Set("id", JsonValue.Of("unused.invalid")); effects.Set("invalid_dispatch_effects", JsonValue.NewArray()); effects.Get("invalid_dispatch_effects").Add(invalid); }
                }, workEdit: root => {
                    if (mode != "timer-only") return;
                    var nodes = root.Get("entity_activities")[0].Get("nodes");
                    for (int i = 0; i < nodes.Count; i++) { nodes[i].Set("executor", JsonValue.Of("work.timed")); nodes[i].Remove("roles"); nodes[i].Remove("requirement"); }
                })), "invalid actual dispatch composition: " + mode);
                c.Equal(0, module.Initialize?.Initializations ?? 0, "invalid metadata never initializes: " + mode);
            }
        }
        private static void Faults(Check c)
        {
            foreach (AppliedDispatchFault fault in Enum.GetValues(typeof(AppliedDispatchFault)))
            {
                var module = new PhysicalDispatchContractsModule(); var game = New(Factory(module)); Begin(c, game); module.Arm(fault);
                long at = game.Clock.StepIndex;
                for (int i = 0; i < 900 && game.Fault == null; i++) { at = game.Clock.StepIndex; game.Update(); }
                c.That(game.Fault?.Phase == "work.effect" && module.Commits == 1 && module.CommittedQueueCount == 2, "actual Applied/throwing callback faults after real queue commit: " + fault);
                c.That(module.MutationCompleted == (fault != AppliedDispatchFault.Exception), "actual callback fault completed");
                c.Equal(at, game.Clock.StepIndex, "failed completion does not advance game time");
                c.Equal(0, game.Update(), "terminal callback cannot resume");
                c.Throws<InvalidOperationException>(() => game.Hash(), "faulted commit cannot hash"); c.Throws<InvalidOperationException>(() => game.Save(), "faulted commit cannot save");
            }
        }
    }
}
