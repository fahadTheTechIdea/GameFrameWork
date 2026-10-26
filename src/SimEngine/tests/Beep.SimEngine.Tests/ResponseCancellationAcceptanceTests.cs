using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.LogisticsPolicyContentTests;
using static Beep.SimEngine.Tests.EmergencyResponseAcceptanceTests;

namespace Beep.SimEngine.Tests
{
    // Thin player driver: native cancellation, enrollment, DAG and completion own every transition.
    internal static class ResponseCancellationAcceptanceTests
    {
        internal const string Case = "Incident durability: cancellation retained work and stale episode completion refusal";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static IncidentLifecycleView Lifecycle(SimGame game)
        {
            if (!game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(Id(game, "response.station"), new Code("failure"), out var facts))
                throw new InvalidOperationException("missing native response lifecycle");
            return facts;
        }
        private static void ReachStage(Check c, SimGame game, SimGame twin, Func<bool> ready, string stage)
            => Reach(c, game, twin, ready, stage, 2, 264);
        private static BigInteger Consumed(SimGame game, string resource)
            => game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                .Where(a => a.Resource == Resource(game, resource) && a.Kind == ResourceFlowKind.Consumed)
                .Aggregate(BigInteger.Zero, (sum, a) => sum + a.AmountRaw);
        private static SimGame Interrupt(Check c, GameFactory factory, SimGame game, string stage)
        {
            var before = Lifecycle(game); var earned = Phases(game).SelectMany(p => p.Nodes).ToList();
            var paid = new[] { "response.spare", "response.suppressant", "response.waste" }.ToDictionary(r => r, r => Consumed(game, r));
            _ = Partial(c, factory, game);
            var twin = Input(c, factory, game, new CancelOrderCommand { Order = before.OrderId });
            var after = Lifecycle(game);
            c.That(after.Snapshot.Active && after.Snapshot.Episode == before.Snapshot.Episode, stage + " never clears current damage");
            c.Equal(before.OrderId, after.OrderId, stage + " repost resumes same authoritative order");
            foreach (var node in earned) {
                var now = Node(game, node.Id);
                c.That(now.Progress.WholeTicks >= node.Progress.WholeTicks && now.ProductiveSteps >= node.ProductiveSteps, stage + " preserves earned " + node.Id);
                if (node.State == NodeState.Done) c.Equal(NodeState.Done, now.State, stage + " completed node not repeated " + node.Id);
            }
            foreach (var entry in paid) c.Equal(entry.Value, Consumed(game, entry.Key), stage + " paid materials not re-consumed " + entry.Key);
            Integrity(c, game, 2, 264); return Partial(c, factory, game);
        }
        private static void Run(Check c)
        {
            var factory = PhysicalRecoveryScenario.Create(false, responseCancellation: true);
            SimGame NewSession() => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 927,
                Mods = { ResourceFlowsModule.ModuleId }, Seats = { Controller.Human, Controller.Human } });
            var game = NewSession(); var isolated = NewSession(); ulong untouched = isolated.Hash();
            using var recording = game.RecordReplay(Limits);
            c.That(!game.Modules.Contains("sim.tycoon"), "response durability has no business dependency");
            var twin = Input(c, factory, game, new EnqueueTransformationCommand { Transformer = Id(game, "response.station"),
                Capability = new Code("process"), Transformation = "response.batch", Count = 1 });
            var execution = Process(game).Wip.Execution;
            RaiseIncidentCommand Raise() => new RaiseIncidentCommand { Target = Id(game, "response.station"), Source = new Code("failure"), Kind = new Code("response.failure") };
            twin = Input(c, factory, game, Raise()); int first = Lifecycle(game).OrderId;
            ReachStage(c, game, twin, () => Node(game, "detect").Progress.WholeTicks > 0 && !Complete(game, "detection"), "partial detection");
            twin = Interrupt(c, factory, game, "detection cancellation");
            ReachStage(c, game, twin, () => Node(game, "isolate").ProductiveSteps > 0 && !Complete(game, "isolation"), "partial isolation");
            c.Equal(NodeState.Done, Node(game, "guard").State, "native unfinished completion refused inside authored contract effect");
            twin = Interrupt(c, factory, game, "isolation cancellation");
            ReachStage(c, game, twin, () => Complete(game, "isolation") && Process(game).Blocker == new Code("response.isolated"), "shutdown after interrupted isolation");
            var shutdown = Process(game);
            twin = Input(c, factory, game, Dispatch(game, "recovery.carrier", "response.spare", 4, 100), Dispatch(game, "recovery.service", "response.suppressant", 4, 90));
            ReachStage(c, game, twin, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero && j.Loaded < Fix.FromInt(4)), "partial independent supply cargo");
            var cargo = Jobs(game).First(j => j.IsHaul && j.Loaded > Fix.Zero && j.Loaded < Fix.FromInt(4));
            twin = Interrupt(c, factory, game, "supply-wait cancellation");
            var retained = Jobs(game).Single(j => j.Id == cargo.Id);
            c.That(retained.Loaded >= cargo.Loaded && retained.Worker == cargo.Worker, "response cancellation preserves independent physical supply claim and cargo");
            ReachStage(c, game, twin, () => Node(game, "stabilize").ProductiveSteps > 0 && Node(game, "stabilize").State != NodeState.Done, "partial stabilization");
            twin = Interrupt(c, factory, game, "paid stabilization cancellation");
            ReachStage(c, game, twin, () => Node(game, "repair").ProductiveSteps > 0 && Node(game, "cleanup").ProductiveSteps > 0 && !Complete(game, "response") && !Complete(game, "cleanup"), "parallel partially paid response");
            twin = Interrupt(c, factory, game, "parallel repair-cleanup cancellation");
            ReachStage(c, game, twin, () => Complete(game, "response") && Complete(game, "cleanup"), "retained repairs finish");
            c.Equal(shutdown.Progress, Process(game).Progress, "interruptions never restart unsafe production");
            twin = Interrupt(c, factory, game, "readiness-blocked cancellation");
            ReachStage(c, game, twin, () => !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "initial supply departures settle");
            twin = Input(c, factory, game, Dispatch(game, "recovery.service", "response.kit", 1, 100));
            ReachStage(c, game, twin, () => Node(game, "restart").ProductiveSteps > 0 && Active(game), "partial verified restart");
            twin = Interrupt(c, factory, game, "restart cancellation");
            ReachStage(c, game, twin, () => !Active(game), "first interrupted episode completes");
            c.Equal(1, Lifecycle(game).Snapshot.Episode, "all cancellations retained one incident episode");
            c.Equal(first, Lifecycle(game).OrderId, "first episode completed same response order");
            c.Equal(execution, Process(game).Wip.Execution, "first restart retains original production WIP");
            twin = Partial(c, factory, game);
            twin = Input(c, factory, game, Raise()); var second = Lifecycle(game);
            c.Equal(2, second.Snapshot.Episode, "new damage creates next native episode"); c.That(second.OrderId != first, "new episode owns distinct work order");
            ReachStage(c, game, twin, () => Node(game, "guard").State == NodeState.Done, "stale prior-order completion refusal");
            c.That(Active(game) && !Complete(game, "response"), "actual old completed order cannot clear new unfinished damage");
            twin = Partial(c, factory, game);
            ReachStage(c, game, twin, () => Node(game, "repair").ProductiveSteps > 0 && !Complete(game, "response"), "second episode actual paid repair");
            twin = Interrupt(c, factory, game, "new-episode paid repair cancellation");
            ReachStage(c, game, twin, () => !Active(game), "second independently supplied episode completes");
            c.That(Phases(game).All(p => p.Complete), "all nodes required again for new episode");
            ReachStage(c, game, twin, () => Process(game).QueueCount == 0 && !Jobs(game).Any(j => j.Worker.IsValid), "once-only retained production and all departures");
            c.Equal(Fix.One, Stock(game, "response.station", "response.product"), "only original one batch produces one output");
            foreach (var item in new[] { ("steel", 2), ("response.spare", 4), ("response.suppressant", 4), ("response.waste", 2), ("response.kit", 0) })
                c.Equal(new BigInteger(Fix.FromInt(item.Item2).Raw), Consumed(game, item.Item1), "two episodes consume exactly once " + item.Item1);
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            c.Equal(2, orders.Count(o => o.Target == Id(game, "response.station")), "no duplicate response work across cancellation or repost");
            foreach (string entity in new[] { "response.station", "response.depot", "recovery.carrier", "recovery.service", "crane" })
                foreach (string resource in new[] { "steel", "response.spare", "response.suppressant", "response.kit", "recovery.fuel" }) {
                    c.Equal(Code.None, View(game).ReadDiagnostics(Id(game, entity), Resource(game, resource), out var facts), "terminal detached resource facts");
                    c.Equal(Fix.Zero, facts.InboundPromised + facts.OutboundPromised, "cancellation releases terminal claims");
                }
            for (int i = 0; i < 12; i++) { Step(c, game, twin); Integrity(c, game, 2, 264); }
            c.Equal(2, Lifecycle(game).Snapshot.Episode, "terminal updates never duplicate damage or repair");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal canceled-work state restores"); c.Equal(untouched, isolated.Hash(), "independent session untouched");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), Limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "all interruptions and episode refusals replay " + replay.Failure + "/" + replay.Detail);
        }
    }
}
