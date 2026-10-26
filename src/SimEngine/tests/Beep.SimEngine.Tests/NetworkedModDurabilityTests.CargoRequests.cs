using System;
using System.Collections.Generic;
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
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string CargoRequestCase = "Networked cargo requests: cancelled workflow retains acquired private delivery and ordinary reserve guards";
        private static OrderView? CargoRequestOrder(SimGame game, int id)
        {
            var rows = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(rows);
            foreach (var row in rows) if (row.Id == id) return row;
            return null;
        }
        private static WorkJobView? CargoRequestJob(SimGame game)
        {
            foreach (var job in AlertJobs(game))
                if (job.IsHaul && job.Worker == Entity(game, "machine") &&
                    (job.State == JobState.Assigned || job.State == JobState.Paused)) return job;
            return null;
        }
        private static void CargoRequestsRun(Check c)
        {
            var recovery = Json.Parse(FrameworkProofContent.Read("logistics-recovery.json"));
            var destination = recovery.Get("recovery_destination_policies")[0];
            if (destination.Get("id")?.Text != "recovery.nearest") throw new InvalidOperationException("explicit destination policy required");
            destination.Set("mode", JsonValue.Of("demand_only"));
            var factory = CompanyAlertAutomationTests.Factory(recovery: recovery.ToString(), additionalWork:
                new Dictionary<string, string> { ["cargo-demand-lifecycle.json"] = FrameworkProofContent.Read("cargo-demand-lifecycle.json") });
            void Observe(SimGame game)
            {
                if (AlertStock(game) + AlertStock(game, "source") + AlertStock(game, "machine") != Fix.FromInt(40)
                    || AlertIncident(game) || AlertState(game).Settings.Level != CompanyAlertLevel.Normal
                    || AlertState(game).Version > 2 || AlertState(game, 1).Version != 0)
                    throw new InvalidOperationException("ordinary cancelled request lost stock or gained emergency privilege/" + AlertDiagnostics(game));
            }
            Pair Create(SimGame a, SimGame b) => new Pair(a, b, Observe, AlertDiagnostics, 2);
            var pair = Create(ConstructionPlanTests.NewPoints(factory), ConstructionPlanTests.NewPoints(factory));
            Console.WriteLine("MODULES cargo-request=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(pair.A.Modules.Contains(LogisticsRecoveryPolicyModule.ModuleId) && !pair.A.Modules.Contains(TycoonModule.ModuleId),
                "explicit JSON workflow/recovery composition has no business");
            var limits = new ReplayLimits(8 * 1024 * 1024, 2000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            Submit(c, pair, new ConfigureWorkPolicyCommand { Worker = Entity(pair.A, "machine"), Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            int post = Submit(c, pair, new PostOrderCommand { Plan = "cargo.request", Target = Entity(pair.A, "field.plant") });
            Reach(pair, game => Result(game, 0, post)?.Accepted == true && CargoRequestJob(game)?.Loaded == Fix.FromInt(4)
                && AlertPosition(game, "machine") > AlertPosition(game, "source") && AlertPosition(game, "machine") < AlertPosition(game, "field.plant"),
                "ordinary authored activity physically loads and moves only its four-unit deficit");
            int order = checked((int)Result(pair.A, 0, post).Value.Arg0);
            var first = CargoRequestJob(pair.A).Value;
            c.That(first.HaulDemand.IsValid && !first.Emergency && first.RecoveryKind == null && AlertStock(pair.A) == Fix.FromInt(20)
                && AlertStock(pair.A, "source") == Fix.FromInt(16) && AlertStock(pair.A, "machine") == Fix.FromInt(4),
                "original requester and physical ordinary cargo have no emergency or instant material effect");
            var restored = Create(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var holdLeft = restored.A.RecordReplay(limits); var holdRight = restored.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), restored.A.Hash(), "loaded ordinary request and actual source claims save exactly");
            foreach (var current in new[] { pair, restored }) Submit(c, current,
                new SetWorkerHeldCommand { Worker = Entity(current.A, "machine"), Held = true });
            Both(pair, restored, game => AlertWorker(game).State == WorkerHomeState.Held, "ordinary loaded hold interrupts old commitment");
            Fix heldAt = AlertPosition(pair.A, "machine");
            c.That(CargoRequestJob(pair.A) == null && AlertStock(pair.A, "machine") == Fix.FromInt(4), "old job ends without losing retained cargo");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new ConfigureCompanyAlertCommand {
                Company = new PlayerId(0), Settings = new CompanyAlertSettings(AlertState(current.A).Settings.Profile,
                    CompanyAlertLevel.Normal, new[] { new WorkerReserveSetting(new WorkerRoleId("inspect"), 1) }) });
            Both(pair, restored, game => AlertState(game).Version == 1, "ordinary policy protects last actual inspect worker");
            ReplayPair(c, factory, holdLeft, holdRight, restored, limits, 2, "loaded request through real hold and normal reserve policy replay");
            restored = Create(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var releaseLeft = restored.A.RecordReplay(limits); var releaseRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current,
                new SetWorkerHeldCommand { Worker = Entity(current.A, "machine"), Held = false });
            long reserveWait = pair.A.Clock.StepIndex + 8;
            Both(pair, restored, game => game.Clock.StepIndex >= reserveWait && AlertWorker(game).State != WorkerHomeState.Held,
                "released ordinary cargo still waits through advancing native reserve policy");
            var queue = new List<LogisticsDispatchOffer>(); pair.A.Seat(0).View.Feature<LogisticsView>().CopyQueue(queue);
            c.That(CargoRequestJob(pair.A) == null && AlertPosition(pair.A, "machine") == heldAt
                && AlertStock(pair.A, "machine") == Fix.FromInt(4) && AlertStock(pair.A) == Fix.FromInt(20)
                && queue.Any(row => row.Kind == LogisticsDispatchKind.CargoReturn && !row.Emergency
                    && row.Haul.Blocker == CompanyWorkerReserveCodes.MinimumReady),
                "real ordinary request never acquires an emergency badge to evade the worker floor");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new ConfigureCompanyAlertCommand {
                Company = new PlayerId(0), Settings = new CompanyAlertSettings(AlertState(current.A).Settings.Profile,
                    CompanyAlertLevel.Normal, Array.Empty<WorkerReserveSetting>()) });
            bool Acquired(SimGame game) => CargoRequestJob(game) is WorkJobView job && job.Id != first.Id
                && job.RecoveryKind == LogisticsRecoveryKind.CargoReturn && job.HaulDemand.Equals(first.HaulDemand)
                && !job.Emergency && job.Destination == Entity(game, "field.plant") && job.Loaded == Fix.FromInt(4);
            Both(pair, restored, Acquired, "native recovery acquires actual residual stock for the private ordinary requester");
            var acquired = CargoRequestJob(pair.A).Value;
            c.That(AlertStock(pair.A, "source") == Fix.FromInt(16) && AlertStock(pair.A) == Fix.FromInt(20),
                "fresh recovery neither reloads source nor delivers before physical travel");
            ReplayPair(c, factory, releaseLeft, releaseRight, restored, limits, 2, "saved held cargo through ordinary reserve wait and fresh private recovery replay");
            restored = Create(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var cancelLeft = restored.A.RecordReplay(limits); var cancelRight = restored.B.RecordReplay(limits);
            c.That(Acquired(restored.A) && pair.A.Hash() == restored.A.Hash(), "private ordinary recovery commitment saves unchanged");
            int cancel = Submit(c, pair, new CancelOrderCommand { Order = order });
            c.Equal(cancel, Submit(c, restored, new CancelOrderCommand { Order = order }), "saved independent peers use the same ordinary cancellation identity");
            Both(pair, restored, game => Result(game, 0, cancel)?.Accepted == true && CargoRequestOrder(game, order)?.State == OrderState.Cancelled,
                "actual requester cancellation withdraws work, not acquired delivery promises");
            var requests = new List<LogisticsDemandView>(); pair.A.Seat(0).View.Feature<LogisticsView>().CopyDemands(requests);
            c.That(!requests.Any(row => row.Source.Equals(first.HaulDemand)) && Acquired(pair.A)
                && CargoRequestJob(pair.A).Value.Id == acquired.Id && AlertStock(pair.A, "machine") == Fix.FromInt(4)
                && AlertStock(pair.A) == Fix.FromInt(20), "expired source has no replacement demand or new emergency but retains original in-flight promise");
            var nodes = new List<ActivityNodeView>(); c.That(pair.A.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes), "cancelled graph remains observable");
            c.That(nodes.Count == 1 && nodes[0].Progress.WholeTicks == 0 && nodes[0].State == NodeState.Cancelled,
                "cancellation before real supply earns no work and spends no24-unit bill");
            var retainedOrder = CargoRequestOrder(pair.A, order).Value;
            var retainedNode = nodes[0];
            ReplayPair(c, factory, cancelLeft, cancelRight, restored, limits, 1, "acquired private return through real requester cancellation replay");
            restored = Create(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var deliveryLeft = restored.A.RecordReplay(limits); var deliveryRight = restored.B.RecordReplay(limits);
            c.That(Acquired(restored.A) && pair.A.Hash() == restored.A.Hash(), "expired requester and retained private promise restore exactly");
            Both(pair, restored, game => AlertStock(game) == Fix.FromInt(24) && AlertStock(game, "machine").IsZero && CargoRequestJob(game) == null,
                "original recovery physically delivers into private inlet after its requester expired");
            long settled = pair.A.Clock.StepIndex + 8;
            Both(pair, restored, game => game.Clock.StepIndex >= settled, "cancelled ordinary request cannot restart or consume delivered stock");
            c.That(AlertStock(pair.A, "source") == Fix.FromInt(16) && AlertStock(pair.A) == Fix.FromInt(24)
                && CargoRequestOrder(pair.A, order) == null && AlertState(pair.A).Version == 2,
                "finite24/16/0 stocks settle and ordinary cleanup drops unreferenced cancelled work without replacement");
            c.That(retainedOrder.State == OrderState.Cancelled && retainedNode.State == NodeState.Cancelled && retainedNode.Progress.WholeTicks == 0,
                "retained cancelled views stay detached after native live-reference cleanup");
            c.That(!pair.A.Seat(0).View.Feature<WorkView>().TryReadProgress(order, out var retired) && retired == null,
                "terminal ordinary request is not retained forever or replaced by a second history store");
            c.That(acquired.Loaded == Fix.FromInt(4) && !acquired.Emergency && acquired.State == JobState.Assigned,
                "retained acquired snapshot does not follow cancellation or physical completion");
            ReplayPair(c, factory, deliveryLeft, deliveryRight, restored, limits, 0, "saved expired requester through physical private delivery replay");
            ReplayPair(c, factory, left, right, pair, limits, 7, "full ordinary request hold reserve recovery cancellation and physical delivery replay");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && restored.Stalls > 0 && restored.ReversedBatches > 0
                && pair.A.Hash() == restored.A.Hash(), "four actual saved/independent peer worlds reorder, stall and converge");
            Console.WriteLine("networked cancelled cargo request replay: initial=" + left.InitialStep + ";final=" + pair.A.Clock.StepIndex
                + ";commands=" + left.CommandCount + ";checkpoints=" + left.CheckpointCount + ";live-hash=" + pair.A.Hash().ToString("X16")
                + ";plant=" + AlertStock(pair.A) + ";source=" + AlertStock(pair.A, "source") + ";cargo=" + AlertStock(pair.A, "machine"));
        }
    }
}
