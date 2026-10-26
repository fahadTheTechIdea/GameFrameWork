using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ProductionBlockerTests;

namespace Beep.SimEngine.Tests
{
    // Public player inputs/views/clock/save only. Existing engine owns every transition and refund.
    internal static class ProductionCancellationTests
    {
        internal const string Case = "Production cancellation: saved idle supply work blocked return and continuous stop retain original resources";
        private static readonly Code Operation = new Code("space.operation");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly ReplayLimits Limits = new ReplayLimits(4 * 1024 * 1024, 4000);
        private static void Queue(Check c, SimGame game, string recipe = "block.smelt", int count = 1)
            => Input(c, game, new EnqueueTransformationCommand { Transformer = Actor(game, "factory"),
                Capability = Operation, Transformation = recipe, Count = count });
        private static void Enabled(Check c, SimGame game, bool enabled)
            => Input(c, game, new ConfigureOperationCommand { Entity = Actor(game, "factory"),
                Operation = Operation, Enabled = enabled, Target = Fix.One });
        private static CancelTransformationCommand Cancel(SimGame game, int index = 0)
            => new CancelTransformationCommand { Transformer = Actor(game, "factory"), Capability = Operation, Index = index };
        private static SimGame CancelSaved(Check c, GameFactory factory, SimGame game, int index = 0)
        {
            var command = Cancel(game, index); ulong before = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal cancellation preview");
            c.Equal(before, game.Hash(), "cancellation preview returns no resources or tools");
            var original = State(game); game.Seat(0).Submit(command);
            c.That(State(game).QueueCount == original.QueueCount && State(game).Wip.Execution == original.Wip.Execution,
                "pending input never cancels outside Update");
            var twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(game.Hash(), twin.Hash(), "saved pending cancellation consumes one normal command once");
            return twin;
        }
        private static void WaitSame(Check c, SimGame game, SimGame twin, string stage)
        {
            var before = State(game); Fix fuel = Fuel(game);
            for (int i = 0; i < 5; i++) Step(game, twin);
            var after = State(game);
            c.That(before.Phase == after.Phase && before.Progress.Equals(after.Progress)
                && before.Wip.Execution == after.Wip.Execution
                && before.Wip.Stock.AmountOf(Resource(game, "space.planetx.ore")) == after.Wip.Stock.AmountOf(Resource(game, "space.planetx.ore"))
                && before.Tools.Held.AmountOf(Resource(game, "space.crucible")) == after.Tools.Held.AmountOf(Resource(game, "space.crucible"))
                && Fuel(game) == fuel, stage + " preserves exact phase/work/raw/tool and never repeats fuel cost");
            c.Equal(game.Hash(), twin.Hash(), stage + " saved twin identity");
        }
        private static Fix Fuel(SimGame game)
            => new[] { "source", "factory", "receiver", "carrier" }.Aggregate(Fix.Zero, (sum, actor) => sum + Stock(game, actor, "block.fuel"));
        private static void NoOutputs(Check c, SimGame game, string stage)
            => c.That(Output(game, "space.alloy") == Fix.Zero && Output(game, "space.slag") == Fix.Zero
                && Stock(game, "receiver", "space.alloy") == Fix.Zero && Stock(game, "receiver", "space.slag") == Fix.Zero, stage + " makes no result");
        private static void Run(Check c) => Physical(c);
        internal static void Physical(Check c, GameFactory selectedFactory = null, Action<Check, SimGame, string> costs = null)
        {
            var factory = selectedFactory ?? Factory("production-cancellation-master.json", "production-cancellation.json");
            var game = New(factory, "production-cancellation-return"); var record = game.RecordReplay(Limits);
            costs?.Invoke(c, game, "idle");
            c.That(State(game).Phase == TransformationPhase.Idle && State(game).Wip.Stock.IsEmpty, "fresh actual idle processor");
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(Cancel(game)), "empty idle cancellation refuses");
            Enabled(c, game, false); Queue(c, game, count:3);
            c.That(State(game).Phase == TransformationPhase.Idle && State(game).QueueCount == 3, "disabled idle queue earns no execution");
            var entries = State(game).Queue.Select(q => q.Execution).ToArray();
            var twin = CancelSaved(c, factory, game, 1);
            c.That(State(game).Queue.Select(q => q.Execution).SequenceEqual(new[] { entries[0], entries[2] }), "tail cancellation preserves original sibling identities");
            WaitSame(c, game, twin, "saved idle queue");
            costs?.Invoke(c, game, "idle-cancel"); costs?.Invoke(c, twin, "idle-cancel");
            twin = CancelSaved(c, factory, game);
            c.That(State(game).QueueCount == 1 && State(game).Queue[0].Execution == entries[2], "idle head cancellation allocates no replacement identity");
            Enabled(c, game, true);
            Reach(game, null, () => State(game).Phase == TransformationPhase.Supplying, "normal supplying admission");
            c.That(State(game).Wip.Stock.IsEmpty && State(game).Tools.Held.IsEmpty, "supplying never owns unearned input or tool");
            costs?.Invoke(c, game, "supplying");
            twin = CancelSaved(c, factory, game);
            c.That(State(game).QueueCount == 0 && State(game).Phase == TransformationPhase.Idle
                && Fuel(game) == Fix.FromInt(24), "supplying cancellation pays no fuel and creates no WIP");
            WaitSame(c, game, twin, "saved cancelled supply"); NoOutputs(c, game, "idle/supplying cancellation");
            costs?.Invoke(c, game, "supply-cancel"); costs?.Invoke(c, twin, "supply-cancel");

            Queue(c, game);
            Haul(c, factory, game, "source", "factory", "space.crucible", 1);
            Haul(c, factory, game, "source", "factory", "space.planetx.ore", 10);
            Haul(c, factory, game, "source", "factory", "block.fuel", 1);
            Reach(game, null, () => State(game).Phase == TransformationPhase.Working && State(game).Progress.WholeTicks == 1, "one physically fuelled productive update");
            var working = State(game);
            c.That(working.Wip.Stock.AmountOf(Resource(game, "space.planetx.ore")) == Fix.FromInt(10)
                && working.Operating.Consumed.AmountOf(Resource(game, "block.fuel")) == Fix.One && Fuel(game) == Fix.FromInt(23), "earned work has exact actual raw/tool/fuel ownership");
            Enabled(c, game, false);
            twin = factory.Load(game.Save()); WaitSame(c, game, twin, "saved partial work");
            costs?.Invoke(c, game, "working"); costs?.Invoke(c, twin, "working");
            Haul(c, factory, game, "source", "factory", "space.planetx.ore", 10);
            c.Equal(Fix.FromInt(10), Stock(game, "factory", "space.planetx.ore"), "physically delivered extra feed occupies original return room");
            twin = CancelSaved(c, factory, game);
            c.That(State(game).Phase == TransformationPhase.Returning && State(game).Blocker == TransformerManager.BlockerReturnFull
                && State(game).Wip.Execution == working.Wip.Execution
                && State(game).Progress.Equals(working.Progress), "partial cancellation enters real capacity-blocked return with original WIP/work");
            var returned = State(game); twin = factory.Load(game.Save());
            WaitSame(c, game, twin, "saved returning wait"); NoOutputs(c, game, "returning wait");
            costs?.Invoke(c, game, "returning"); costs?.Invoke(c, twin, "returning");
            ulong before = game.Hash();
            c.Equal(TransformerManager.BlockerReturning, game.Seat(0).Preview(Cancel(game)), "duplicate pending return cancellation refuses");
            c.Equal(before, game.Hash(), "refused duplicate return is pure");
            long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash();
            Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == paused, "sole clock pauses actual returning state");
            game.Clock.Resume(); twin.Clock.Resume();
            Haul(c, factory, game, "factory", "receiver", "space.planetx.ore", 10, "space.feed");
            Reach(game, null, () => State(game).QueueCount == 0 && State(game).Phase == TransformationPhase.Idle, "physical feed export permits original raw return");
            c.That(State(game).Wip.Stock.IsEmpty && State(game).Tools.Held.IsEmpty
                && Stock(game, "factory", "space.planetx.ore") == Fix.FromInt(10)
                && Stock(game, "receiver", "space.planetx.ore") == Fix.FromInt(10)
                && Stock(game, "factory", "space.crucible") == Fix.One && Fuel(game) == Fix.FromInt(23), "return refunds original ten raw once, releases tool and never refunds/repeats earned fuel");
            NoOutputs(c, game, "completed partial cancellation");
            twin = factory.Load(game.Save()); WaitSame(c, game, twin, "saved completed return");
            costs?.Invoke(c, game, "return-done"); costs?.Invoke(c, twin, "return-done");
            c.That(returned.Phase == TransformationPhase.Returning && returned.Wip.Stock.AmountOf(Resource(game, "space.planetx.ore")) == Fix.FromInt(10), "retained return facts are detached");

            Haul(c, factory, game, "source", "factory", "block.fuel", 1);
            Enabled(c, game, true); Queue(c, game, "block.flow");
            Reach(game, null, () => State(game).Mode == TransformationMode.Continuous && State(game).Progress.WholeTicks == 1, "one actual continuous conversion");
            c.That(State(game).Phase == TransformationPhase.Working && State(game).Wip.Stock.IsEmpty
                && Output(game, "space.alloy") == Fix.One && Output(game, "space.slag") == Fix.One && Fuel(game) == Fix.FromInt(22), "continuous stock conversion earns only one real result and one further fuel cost");
            twin = factory.Load(game.Save()); WaitSame(c, game, twin, "saved fuel-starved continuous work");
            costs?.Invoke(c, game, "continuous"); costs?.Invoke(c, twin, "continuous");
            twin = CancelSaved(c, factory, game);
            Reach(game, twin, () => State(game).QueueCount == 0, "saved continuous stop releases actual service/tool");
            c.That(State(game).Wip.Stock.IsEmpty && State(game).Tools.Held.IsEmpty
                && Stock(game, "factory", "space.planetx.ore") == Fix.FromInt(8)
                && Output(game, "space.alloy") == Fix.One && Output(game, "space.slag") == Fix.One && Fuel(game) == Fix.FromInt(22), "continuous stop neither refunds converted input nor duplicates result/fuel cost");
            WaitSame(c, game, twin, "saved completed continuous stop");
            costs?.Invoke(c, game, "continuous-stop"); costs?.Invoke(c, twin, "continuous-stop");
            c.That(new ReplayVerifier().Verify(factory, record.Finish(), Limits).Success, "idle/supply/partial/blocked-return/continuous commands and sole clock replay");

            var completed = New(factory); var completedRecord = completed.RecordReplay(Limits); Queue(c, completed, count:2);
            Haul(c, factory, completed, "source", "factory", "space.crucible", 1);
            Haul(c, factory, completed, "source", "factory", "space.planetx.ore", 10);
            Haul(c, factory, completed, "source", "factory", "block.fuel", 10);
            Haul(c, factory, completed, "source", "factory", "space.planetx.ore", 10);
            Haul(c, factory, completed, "source", "factory", "block.fuel", 10);
            Haul(c, factory, completed, "source", "factory", "block.fuel", 4);
            Reach(completed, null, () => State(completed).Phase == TransformationPhase.Blocked
                && State(completed).Blocker == TransformerManager.BlockerOutput, "second complete batch retains full-output WIP");
            var blocked = State(completed); twin = factory.Load(completed.Save());
            WaitSame(c, completed, twin, "saved completed-output wait");
            costs?.Invoke(c, completed, "output-wait"); costs?.Invoke(c, twin, "output-wait");
            c.That(blocked.Progress.Complete(blocked.Required)
                && blocked.Wip.Stock.AmountOf(Resource(completed, "space.planetx.ore")) == Fix.FromInt(10)
                && Fuel(completed) == Fix.Zero, "two funded batches spend exactly twenty-four real fuel units");
            twin = CancelSaved(c, factory, completed);
            Reach(completed, twin, () => State(completed).QueueCount == 0, "saved completed batch cancellation");
            c.That(State(completed).Wip.Stock.IsEmpty && State(completed).Tools.Held.IsEmpty
                && Stock(completed, "factory", "space.planetx.ore") == Fix.FromInt(10)
                && Output(completed, "space.alloy") == Fix.FromInt(6) && Output(completed, "space.slag") == Fix.FromInt(4)
                && Fuel(completed) == Fix.Zero, "completed-output cancellation returns second raw once, preserves first result, and cannot restore consumed fuel");
            WaitSame(c, completed, twin, "saved cancelled completed batch");
            costs?.Invoke(c, completed, "output-cancel"); costs?.Invoke(c, twin, "output-cancel");
            c.That(!View(completed, 1).TryGetTransformation(Actor(completed, "factory"), Operation, out _), "cancelled material commitments remain private");
            c.Equal(completed.Hash(), factory.Load(completed.Save()).Hash(), "final completed-cancellation checkpoint");
            c.That(new ReplayVerifier().Verify(factory, completedRecord.Finish(), Limits).Success, "completed-output cancellation replays without duplicate raw results or fuel");
        }
    }
}
