using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class ConnectedResourcePortTests
    {
        internal const string TopologyCase = "Connected topology continuation: real tool claims protect saved cuts and physical reconnect rebinds original owners";
        private static void TopologyContinuation(Check c)
        {
            foreach (bool half in new[] { false, true }) foreach (bool independent in new[] { false, true })
                Physical(c, half, independent, true, ContinueTopology);
        }
        // Same original JSON/player workflow, now continued through a live-claim refusal and real cut/rebuild.
        // No raw World, stock, token, geometry, worker or progress mutation.
        private static void ContinueTopology(Check c, GameFactory factory, SimGame earned, bool half, bool independent)
        {
            var game = factory.Load(earned.Save()); var twin = factory.Load(game.Save()); var idle = factory.Load(game.Save()); ulong isolated = idle.Hash();
            string context = "half=" + half + "/independent-port=" + independent;
            var site = Site(game); var resource = Supply(game); var worker = Entity(game, "machine");
            var removal = Entity(game, "corridor.removal-site");
            Fix initial = Total(game); bool rebuilding = false;
            var old = Pools(game).Single();
            var path = new SpatialPlacement[] { new PointPlacement("field.points", Fix.FromInt(5)) };
            InfrastructureView Infrastructure(SimGame session) => session.Seat(0).View.Feature<InfrastructureView>();
            InventoryView Inventory(SimGame session) => session.Seat(0).View.Feature<InventoryView>();
            void Observe() {
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("connected topology fault " + context + "/" + game.Fault + "/" + twin.Fault);
                c.Equal(game.Hash(), twin.Hash(), "saved actual topology/claims/work agree each update: " + context);
                if (!rebuilding) c.Equal(initial, Total(game), "claims and physical split conserve complete original stock: " + context);
                else c.That(Total(game) <= initial && Total(game) >= initial - Fix.FromInt(2), "only authored two-unit reconstruction bill can leave physical stock: " + context);
            }
            void Advance() { game.Update(); twin.Update(); Observe(); }
            void Wait(Func<bool> ready, string stage) {
                var elapsed = Stopwatch.StartNew(); int updates = 0;
                while (updates < 512 && elapsed.ElapsedMilliseconds < 20000 && !ready()) { Advance(); updates++; }
                if (!ready()) throw new InvalidOperationException(stage + " exceeds512Updates/20000ms at " + game.Clock.StepIndex + "/" + context + "/elapsed=" + elapsed.ElapsedMilliseconds +
                    "/port=" + Port(game).Blocker + "/pools=" + Pools(game).Count + "/jobs=" + string.Join(";", PermanentRemovalTests.Jobs(game).Select(row => row.Id + "/" + row.State + "/" + row.Blocker)));
            }
            CommandResult Send(Command command) { var row = PermanentRemovalTests.Submit(c, game, command); twin = factory.Load(game.Save()); Observe(); return row; }
            void Save() { c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "actual connected topology checkpoint canonical: " + context); twin = factory.Load(game.Save()); }
            var limits = new ReplayLimits(6 * 1024 * 1024, 6000);
            using (var recording = game.RecordReplay(limits)) {
                int inspect = (int)Send(new PostOrderCommand { Plan = "connected.inspect", Target = site, Priority = 90 }).Arg0;
                Wait(() => PermanentRemovalTests.Node(game, inspect, "check").Progress.WholeTicks > 0, "machine earns actual tool-backed connected work");
                c.That(Inventory(game).TryGetReservations(site, Shared, out var receipts) && receipts.Count == 1, "ordinary connected work acquires one real original tool receipt: " + context);
                var original = receipts.Single();
                c.That(original.Amount == Fix.FromInt(2) && original.Token.Endpoint == old.Endpoint, "tool authority addresses the actual complete connected pool: " + context);
                ulong pure = game.Hash();
                c.Equal(ResourceTransactions.ActivePromises, Infrastructure(game).PreviewRemoval("corridor.point-line", path), "same actual promise blocks public destructive topology preview: " + context);
                c.Equal(pure, game.Hash(), "live-claim removal preview is pure: " + context);
                long cut = Send(new SubmitPlanCommand { Intent = new PlanIntent("activity:corridor.remove", ActivityWorkerSelection.CreateArguments(removal, ActivityWorkerSelection.Unbound), "connected-cut", 100, false) }).Arg0;
                var pending = PermanentRemovalTests.Plan(game, cut);
                c.That(pending.State == PlanState.Pending && pending.Order == 0 && pending.Blocker == ResourceTransactions.ActivePromises, "persistent cut intent waits without a phantom physical execution: " + context);
                Save(); for (int i = 0; i < 3; i++) Advance();
                c.That(PermanentRemovalTests.Plan(game, cut).Order == 0 && Pools(game).Single().Endpoint == old.Endpoint && SpatialCorridorExecutionTests.Segment(game, true, 1).IsPositive,
                    "saved pending cut cannot erase geometry or replace claimed endpoint: " + context);
                c.That(Inventory(twin).TryGetReservations(site, Shared, out var saved) && saved.Any(row => row.Token.Equals(original.Token) && row.Owner.Equals(original.Owner) && row.Amount == original.Amount), "saved pending intent retains exact original receipt authority: " + context);
                Send(new CancelOrderCommand { Order = inspect });
                Wait(() => PermanentRemovalTests.Plan(game, cut).Order > 0 && PermanentRemovalTests.Node(game, PermanentRemovalTests.Plan(game, cut).Order, "cut").Progress.WholeTicks > 0, "released original tool permits normal positive-time cut");
                c.That(SpatialCorridorExecutionTests.Segment(game, true, 1).IsPositive && Pools(game).Single().Endpoint == old.Endpoint, "partially earned cut has not committed deletion: " + context);
                Save(); Wait(() => PermanentRemovalTests.Plan(game, cut).State == PlanState.Done, "earned physical deletion commits admitted split");
                var split = Pools(game);
                c.That(split.Count == 2 && split.All(row => row.Endpoint != old.Endpoint && row.MemberCount == 1 && row.OutboundPromises.Count == 0), "two new actual pool owners replace only released original endpoint: " + context);
                c.Equal(Fix.FromInt(6), split.SelectMany(row => row.Stock).Where(row => row.Resource == resource).Aggregate(Fix.Zero, (sum, row) => sum + row.Amount), "exact original six pooled units redistribute without loss: " + context);
                c.That(!Inventory(game).TryGetReservations(site, Shared, out _, old.Endpoint), "retired original cannot resolve to substitute receipt authority: " + context);
                var unavailable = Port(game);
                c.That(!unavailable.Ready && unavailable.Blocker == NetworkService.InsufficientMembers && unavailable.Stock == null && unavailable.Version == -1, "authored minimum membership returns unavailable, never invented empty usable inventory: " + context);
                Send(new EnqueueTransformationCommand { Transformer = site, Capability = Process, Transformation = "connected.batch" });
                c.That(ProcessFacts(game).Blocker == NetworkService.InsufficientMembers && ProcessFacts(game).Wip.Stock.IsEmpty && split.All(row => row.OutboundPromises.Count == 0), "queued processor observes same unavailable binding before claiming any child pool: " + context);
                Save(); long stopped = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Advance();
                c.Equal(stopped, game.Clock.StepIndex, "sole paused clock freezes split topology and pending work: " + context); game.Clock.Resume(); twin.Clock.Resume();
                Send(new CancelTransformationCommand { Transformer = site, Capability = Process, Index = 0 });
                rebuilding = true;
                long rebuild = Send(new SubmitPlanCommand { Intent = new PlanIntent("line:corridor.point-line", Json.Parse("{\"space\":\"field.points\",\"path\":[{\"space\":\"field.points\",\"position\":5}]}"), "reconnect", 60, false) }).Arg0;
                var marker = Entity(game, "corridor.cursor");
                var supply = new HaulResourcesCommand { Source = Entity(game, "destination"), Destination = marker, Worker = worker, Resource = resource, Ceiling = Fix.FromInt(2), Priority = 100 };
                Wait(() => PermanentRemovalTests.Plan(game, rebuild).State == PlanState.Done || game.Seat(0).Preview(supply).IsNone, "actual carrier ready to supply reconnect from finite delivered stock");
                if (PermanentRemovalTests.Plan(game, rebuild).State != PlanState.Done) Send(supply);
                Save(); Wait(() => PermanentRemovalTests.Plan(game, rebuild).State == PlanState.Done, "real supplied machine construction reconnects graph");
                var merged = Pools(game).Single(); var rebound = Port(game);
                c.That(rebound.Ready && rebound.Endpoint == merged.Endpoint && merged.Endpoint != old.Endpoint && split.All(row => row.Endpoint != merged.Endpoint) && merged.MemberCount == 3, "same capability rebinds to one genuinely new complete owner without identity recycling: " + context);
                c.Equal(initial - Fix.FromInt(2), Total(game), "only exact authored rebuild material is consumed: " + context);
                c.That(!Inventory(game).TryGetReservations(site, Shared, out _, old.Endpoint), "reconnection does not resurrect retired receipt identities: " + context);
                int fresh = (int)Send(new PostOrderCommand { Plan = "connected.inspect", Target = site, Priority = 90 }).Arg0;
                Wait(() => PermanentRemovalTests.Node(game, fresh, "check").Progress.WholeTicks > 0, "same authored work binds real merged endpoint");
                c.That(Inventory(game).TryGetReservations(site, Shared, out var next) && next.Single().Token.Endpoint == merged.Endpoint && !next.Single().Token.Equals(original.Token), "new execution owns a new actual receipt, not a remapped old token: " + context);
                Send(new CancelOrderCommand { Order = fresh });
                Wait(() => !PermanentRemovalTests.Jobs(game).Any(Live) && Pools(game).Single().OutboundPromises.Count == 0, "new tool cancellation releases only current authority");
                Save(); var hidden = new List<PlanView>(); game.Seat(1).View.Feature<WorkView>().CopyPlans(hidden); c.Equal(0, hidden.Count, "physical topology plans remain company-private: " + context);
                c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "actual saved connected-claim pending cut split refill and merge commands replay: " + context);
            }
            c.Equal(isolated, idle.Hash(), "independent loaded session retains its own original topology and stock: " + context);
        }
    }
}
