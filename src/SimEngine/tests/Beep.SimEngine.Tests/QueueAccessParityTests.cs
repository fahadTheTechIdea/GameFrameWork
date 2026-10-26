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
    // One authoritative queue evaluator; ordinary commands/Update/views/save only in this player driver.
    internal static class QueueAccessParityTests
    {
        internal const string Case = "Queue access parity: private missing hidden refusals preserve physical WIP and replay";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, DirectResourceContentModule resources)
            => ResourceContentContractTests.Factory(half, resources, worldEdit: root =>
            {
                var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
                scenario.Get("settings").Set(InventorySightModule.ModuleId, Json.Parse("{\"target_kind\":\"entity\",\"target\":\"destination\",\"initial_hidden\":false}"));
            }, extraModules: new GameModule[] { new InventorySightModule() },
                extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        private static EnqueueTransformationCommand Append(EntityId id, Code capability, string recipe = "direct.batch", int count = 1)
            => new EnqueueTransformationCommand { Transformer = id, Capability = capability, Transformation = recipe, Count = count };
        private static CancelTransformationCommand Cancel(EntityId id, Code capability, int index = 0)
            => new CancelTransformationCommand { Transformer = id, Capability = capability, Index = index };
        private static Code Parity(Check c, SimGame g, int seat, Command command)
        {
            ulong hash = g.Hash(); var view = g.Seat(seat).View.Feature<TransformationManagementView>();
            Code ordinary = g.Seat(seat).Preview(command);
            Code typed = command is EnqueueTransformationCommand e
                ? view.PreviewEnqueue(e.Transformer, e.Capability, e.Transformation, e.Count, e.Repeat, e.Home)
                : view.PreviewCancel(((CancelTransformationCommand)command).Transformer, ((CancelTransformationCommand)command).Capability, ((CancelTransformationCommand)command).Index);
            c.Equal(ordinary, typed, "typed/ordinary previews use identical current queue evaluator");
            c.Equal(hash, g.Hash(), "both queue preview surfaces preserve stock/receipts/identity/time");
            return typed;
        }
        private static void Hidden(Check c, SimGame g, EntityId actor, Code capability)
        {
            var view = g.Seat(0).View.Feature<TransformationManagementView>(); ulong hash = g.Hash();
            c.That(!view.TryGetTransformation(actor, capability, out _), "cached hidden processor cannot disclose queue/WIP/progress");
            var entries = new List<TransformationQueueEntry> { default };
            c.That(!view.CopyQueue(actor, capability, entries) && entries.Count == 0, "hidden queue clears caller seed without prefix");
            c.Equal(0, view.TransformationsOf(actor, capability).Count, "hidden processor recipe menu exposes no capabilities");
            c.Equal(Codes.NotFound, view.ReadExecutionPreview(actor, capability, "direct.batch", out var preview), "hidden execution projection is private");
            c.That(preview == null, "hidden execution preview returns no object");
            foreach (int seat in new[] { 0, 1 })
            {
                foreach (var command in new Command[] { Append(actor, capability), Cancel(actor, capability), Append(EntityId.None, capability), Cancel(EntityId.None, capability),
                    Append(actor, Code.None, "missing", 0), Cancel(actor, Code.None, -1) })
                    c.Equal(Codes.NotFound, Parity(c, g, seat, command), "hidden/foreign/missing access wins before revealing invalid queue arguments");
            }
            c.Equal(hash, g.Hash(), "all hidden metadata/queue observations are pure");
        }
        private static void Checkpoint(Check c, GameFactory f, SimGame g, EntityId actor, ResourceId resource)
        {
            var capability = new Code("process"); var owner = g.Seat(0).View.Feature<TransformationManagementView>();
            c.Equal(Code.None, Parity(c, g, 0, Append(actor, capability)), "visible owned append admission");
            c.Equal(Code.None, Parity(c, g, 0, Cancel(actor, capability)), "visible owned cancellation admission");
            c.Equal(Codes.InvalidArgument, Parity(c, g, 0, Append(actor, capability, count: 0)), "visible invalid count still uses same evaluator");
            c.Equal(Codes.InvalidArgument, Parity(c, g, 0, Cancel(actor, capability, -1)), "visible invalid index still uses same evaluator");
            c.Equal(Codes.NotFound, Parity(c, g, 1, Append(actor, capability)), "foreign append private refusal agrees");
            c.Equal(Codes.NotFound, Parity(c, g, 1, Cancel(actor, capability)), "foreign cancel private refusal agrees");
            c.That(owner.TryGetTransformation(actor, capability, out var retained), "actual physical partial state observed");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = true });
            Hidden(c, g, actor, capability);
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "hidden access rule restores with original WIP/receipts");
            g.Clock.Pause(); twin.Clock.Pause(); Hidden(c, twin, actor, capability);
            g.Update(); twin.Update(); c.Equal(g.Hash(), twin.Hash(), "equal local pacing keeps hidden saved twins identical");
            g.Clock.Resume();
            var issued = new List<(int Seat, int Sequence)>();
            foreach (int seat in new[] { 0, 1 }) foreach (Command command in new Command[] { Append(actor, capability), Cancel(actor, capability) })
                issued.Add((seat, g.Seat(seat).Submit(command)));
            g.Update();
            foreach (var item in issued)
            {
                long cursor = 0; var answers = new List<CommandResult>(); g.Seat(item.Seat).ReadResults(ref cursor, answers);
                var answer = answers.Single(x => x.Sequence == item.Sequence);
                c.That(!answer.Accepted && answer.Refusal == Codes.NotFound, "actual normal command execution shares private queue refusal");
            }
            c.That(g.Observer.Feature<TransformationManagementView>().TryGetTransformation(actor, capability, out var actual), "authorized observer still sees physical provider");
            c.That(actual.QueueCount == retained.QueueCount && actual.Wip.Stock.AmountOf(resource) == Fix.FromInt(2) &&
                actual.Tools.Receipts.Count == retained.Tools.Receipts.Count, "refused commands neither append/cancel nor disturb real WIP/tool receipts");
            c.Equal(Codes.NotFound, g.Observer.Feature<TransformationManagementView>().PreviewCancel(actor, capability, 0), "observer diagnosis retains actual owner's hidden queue refusal without command privilege");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = false });
            c.That(owner.TryGetTransformation(actor, capability, out _) && retained.Wip.Stock.AmountOf(resource) == Fix.FromInt(2), "reveal restores live facts without rewriting retained snapshot");
            c.Equal(Code.None, Parity(c, g, 0, Cancel(actor, capability)), "revealed actor resumes ordinary admissible control");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current shared queue private-admission identity");
            foreach (bool half in new[] { false, true }) foreach (bool cancel in new[] { false, true })
            {
                var resources = new DirectResourceContentModule(); var f = Factory(half, resources);
                ResourceContentContractTests.Healthy(c, half, cancel, f, resources, partialCheckpoint: Checkpoint);
            }
        }
    }
}
