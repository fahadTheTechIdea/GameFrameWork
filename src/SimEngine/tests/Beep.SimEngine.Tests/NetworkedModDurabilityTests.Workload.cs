using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Physical = Beep.SimEngine.Tests.PermanentRemovalTests;
using Load = Beep.SimEngine.Tests.WorkLoadSignalTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string WorkBacklogCase = "Networked workload dispatch: saved open jobs, independent worker pause and deferred physical restart";
        internal const string WorkQueueCase = "Networked workload queues: actual arrived docks, paused service and saved partial cargo controls";
        private static readonly Code LoadSource = new Code("workload"), LoadAvailability = new Code("availability");
        private static readonly string[] LoadWorkers = { "machine.a", "machine.b", "machine.c", "machine.d" };
        private static WorkView LoadWork(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static WorkLoadSnapshot LoadFacts(SimGame game, WorkLoadScope scope = WorkLoadScope.Company, EntityId subject = default)
        {
            Code refusal = LoadWork(game).ReadWorkLoad(new PlayerId(0), scope, subject, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("actual workload refused/" + scope + "/" + refusal); return facts;
        }
        private static WorkerCapacitySnapshot LoadCapacity(SimGame game, WorkerRoleId role = default)
        {
            Code refusal = LoadWork(game).ReadWorkerCapacity(new PlayerId(0), role, EntityId.None, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("actual worker capacity refused/" + refusal); return facts;
        }
        private static AutomationSignalSnapshot LoadSignal(SimGame game, string signal, string source = "destination")
        {
            Code refusal = game.Seat(0).View.Feature<OperationsView>().ReadSignal(FleetEntity(game, source), LoadSource, signal, out var facts);
            if (!refusal.IsNone || !facts.Available || !facts.Value.HasValue || facts.Unit != new Code("unit.count"))
                throw new InvalidOperationException("actual typed workload signal/" + signal + "/" + refusal + "/" + facts.Blocker); return facts;
        }
        private static AutomationRuleSnapshot LoadRule(SimGame game)
            => game.Seat(0).View.Feature<OperationsView>().TryGetAutomationRule(new PlayerId(0), "workload", out var rule) ? rule : null;
        private static bool LoadEnabled(SimGame game, string worker)
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetControl(FleetEntity(game, worker), LoadAvailability, out var control))
                throw new InvalidOperationException("original availability control missing"); return control.Enabled;
        }
        private static ConfigureOperationCommand LoadControl(SimGame game, string worker, bool enabled)
            => new ConfigureOperationCommand { Entity = FleetEntity(game, worker), Operation = LoadAvailability, Enabled = enabled, Target = Fix.One };
        private static ConfigureAutomationCommand LoadConfigure(SimGame game, string profile, string source, string worker)
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetAutomationProfile(profile, out var authored)) throw new InvalidOperationException("original workload profile missing");
            return new ConfigureAutomationCommand { Rule = "workload", Settings = new AutomationSettings(profile, true,
                new ControlBinding(FleetEntity(game, worker), LoadAvailability),
                new Dictionary<string, ControlBinding> { ["jobs"] = new ControlBinding(FleetEntity(game, source), LoadSource) },
                authored.Thresholds.ToDictionary(p => p.Key, p => p.Value.Initial, StringComparer.Ordinal), authored.WhenTrue, authored.WhenFalse) };
        }
        private static ConfigureWorkPolicyCommand LoadPolicy(SimGame game, EntityId worker, bool enabled)
        {
            var policy = HomePolicy(game, worker).Settings;
            return new ConfigureWorkPolicyCommand { Worker = worker, Settings = new WorkPolicySettings(enabled, policy.Priority,
                policy.AllowedRoles, policy.Area, policy.Interruptible, policy.EmergencyEligible, policy.AssignmentMode) };
        }
        private static List<ActivityNodeView> LoadNodes(SimGame game, int order)
        {
            var rows = new List<ActivityNodeView>(); LoadWork(game).TryCopyNodes(order, rows); return rows;
        }
        private static bool LoadProductive(SimGame game, int order) => LoadNodes(game, order).Any(n => n.ProductiveSteps > 0 && n.State != NodeState.Done);
        private static bool LoadLive(WorkJobView job) => job.State == JobState.Open || job.State == JobState.Assigned || job.State == JobState.Paused;
        private static string LoadDiagnostics(SimGame game)
        {
            var facts = LoadFacts(game); var capacity = LoadCapacity(game); var rule = LoadRule(game);
            var orders = new List<OrderView>(); LoadWork(game).CopyOrders(orders);
            return "at=" + game.Clock.StepIndex + "/jobs=" + facts.LiveJobs + "/open=" + facts.OpenJobs + "/assigned=" + facts.AssignedJobs +
                "/paused=" + facts.PausedJobs + "/waiting=" + facts.DockWaiting + "/arrived=" + facts.DockArrivedWaiting +
                "/busy=" + capacity.BusyWorkers + "/ready=" + capacity.OrdinaryReady + "/pending=" + rule?.PendingDecision +
                "/workers=" + string.Join(";", LoadWorkers.Select(w => w + "=" + Physical.At(game, FleetEntity(game, w)))) +
                "/nodes=" + string.Join(";", orders.SelectMany(o => LoadNodes(game, o.Id).Select(n => o.Id + "/" + n.Id + "/" + n.Progress.WholeTicks + "/" + n.Blocker))) +
                "/jobs-detail=" + string.Join(";", Physical.Jobs(game).Select(j => j.Id + "/" + j.Worker + "/" + j.State + "/" + j.ToilIndex + "/" + j.Loaded + "/" + j.Blocker));
        }
        private static Pair LoadPair(SimGame a, SimGame b)
        {
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long prior) && prior == game.Clock.StepIndex) return;
                var facts = LoadFacts(game); var jobs = Physical.Jobs(game).Where(LoadLive).ToList(); var capacity = LoadCapacity(game);
                if (Physical.Total(game) != Fix.FromInt(32) || facts.LiveJobs != jobs.Count || facts.ActivityJobs != jobs.Count(j => !j.IsHaul) ||
                    facts.HaulJobs != jobs.Count(j => j.IsHaul) || facts.OpenJobs + facts.AssignedJobs + facts.PausedJobs != facts.LiveJobs ||
                    facts.DockArrivedWaiting > facts.DockWaiting || facts.DockWaiting > facts.LiveJobs || capacity.TotalWorkers != 4 ||
                    capacity.BusyWorkers < 0 || capacity.IdleWorkers < 0 || capacity.HeldWorkers < 0 || capacity.OrdinaryReady > capacity.IdleWorkers ||
                    jobs.Where(j => j.Worker.IsValid).GroupBy(j => j.Worker).Any(g => g.Count() != 1))
                    throw new InvalidOperationException("original finite stock/distinct job/worker count invariant/" + LoadDiagnostics(game));
                var expected = new[] { facts.LiveJobs, facts.ActivityJobs, facts.HaulJobs, facts.OpenJobs, facts.AssignedJobs, facts.PausedJobs, facts.DockWaiting, facts.DockArrivedWaiting };
                var signals = new[] { "load.live", "load.activity", "load.haul", "load.open", "load.assigned", "load.paused", "load.waiting", "load.arrived" };
                for (int i = 0; i < signals.Length; i++) if (LoadSignal(game, signals[i]).Value != Fix.FromInt(expected[i]))
                    throw new InvalidOperationException("native workload signal differs from actual immutable board facts/" + signals[i]);
                foreach (var row in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates)
                    if (row.Kind != ResourceFlowKind.Initialized && row.Kind != ResourceFlowKind.StockTransfer)
                        throw new InvalidOperationException("workload observation/control fabricated, consumed or discarded supplies");
                if (game.Seat(1).View.Feature<WorkView>().ReadWorkLoad(new PlayerId(0), WorkLoadScope.Company, EntityId.None, out var hidden) != Codes.NotFound ||
                    hidden.LiveJobs != 0 || game.Seat(1).View.Feature<OperationsView>().TryGetAutomationRule(new PlayerId(0), "workload", out _) ||
                    game.Seat(1).View.Feature<OperationsView>().ReadSignal(FleetEntity(game, "destination"), LoadSource, "load.live", out var signal) != Codes.NotOwned || signal.Value.HasValue)
                    throw new InvalidOperationException("private workload/rule disclosed to another company");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, LoadDiagnostics, 0);
        }
        private static Pair LoadRestore(Check c, GameFactory factory, Pair source)
        {
            var restored = LoadPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()));
            c.Equal(source.A.Hash(), restored.A.Hash(), "actual work/claims/controls restore with no workload count state");
            c.Equal(source.B.Hash(), restored.B.Hash(), "saved peer restores original state"); return restored;
        }
        private static void WorkBacklogRun(Check c)
        {
            var factory = Load.Factory(root => {
                // Explicit startup parking, outside the source's actual departure corridor.
                StartupContentAuthoring.Position(root, "machine.c", JsonValue.Number("20"));
                StartupContentAuthoring.Position(root, "machine.d", JsonValue.Number("21"));
            }, recordFlows: true); var pair = LoadPair(Load.New(factory), Load.New(factory));
            Console.WriteLine("MODULES workload-backlog=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(pair.A.Modules.Contains(WorkSignalsModule.ModuleId) && !pair.A.Modules.Contains(TycoonModule.ModuleId), "actual workload controls have no business dependency");
            c.That(LoadCapacity(pair.A, new WorkerRoleId("inspect")).TotalWorkers == 4 && LoadCapacity(pair.A, new WorkerRoleId("haul")).TotalWorkers == 3,
                "original native and independent workers retain declared roles");
            var initial = LoadFacts(pair.A); var limits = new ReplayLimits(8 * 1024 * 1024, 3000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            foreach (string worker in LoadWorkers) Submit(c, pair, LoadControl(pair.A, worker, false));
            Submit(c, pair, LoadConfigure(pair.A, "load.dispatch", "destination", "machine.b"));
            int posted = Submit(c, pair, new PostOrderCommand { Plan = "inspect", Target = FleetEntity(pair.A, "source") });
            int foreign = Submit(c, pair, new PostOrderCommand { Plan = "inspect", Target = FleetEntity(pair.B, "source") }, Codes.NotOwned, seat: 1);
            Reach(pair, g => Result(g, 0, posted)?.Accepted == true && LoadRule(g)?.PendingDecision == true, "actual two open jobs earn deferred availability intent");
            int order = checked((int)Result(pair.A, 0, posted).Value.Arg0); EntityId actor = FleetEntity(pair.A, "machine.b");
            c.That(LoadFacts(pair.A).OpenJobs == 2 && LoadFacts(pair.A).AssignedJobs == 0 && !LoadEnabled(pair.A, "machine.b") &&
                Result(pair.A, 1, foreign)?.Refusal == Codes.NotOwned, "new backlog is not immediate worker availability, assignment or foreign work");
            var restored = LoadRestore(c, factory, pair); var pendingLeft = restored.A.RecordReplay(limits); var pendingRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => LoadProductive(g, order), "next-update availability enables independent worker to physically earn service work");
            var working = LoadNodes(pair.A, order).Single(n => n.ProductiveSteps > 0 && n.State != NodeState.Done);
            c.That(working.Worker == actor && working.RequiredTicks == 12 && Physical.At(pair.A, actor) == Fix.One &&
                LoadFacts(pair.A).AssignedJobs == 1 && LoadFacts(pair.A).OpenJobs == 1, "same real independent machine works at service bay while second node remains open");
            ReplayPair(c, factory, pendingLeft, pendingRight, restored, limits, 0, "saved backlog decision to real assigned work replay");
            restored = LoadRestore(c, factory, pair); var workingLeft = restored.A.RecordReplay(limits); var workingRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native clock pause during productive workload");
            long paused = pair.A.Clock.StepIndex; ulong held = pair.A.Hash();
            Reach(restored, g => g.Clock.StepIndex >= paused + 4, "separate saved work peers earn four real updates while originals pause"); pair.Round();
            c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == held && restored.A.Clock.StepIndex > paused,
                "original workload borrows neither assignment age nor productive work from saved peers");
            pair.A.Net.RequestResume(); Both(pair, restored, g => g.Clock.StepIndex >= paused + 4, "native resumed backlog reaches common real work epoch");
            var beforePolicy = Physical.Node(pair.A, order, working.Id).Progress;
            foreach (var current in new[] { pair, restored }) Submit(c, current, LoadPolicy(current.A, actor, false));
            Both(pair, restored, g => LoadFacts(g, WorkLoadScope.Worker, actor).PausedJobs == 1, "normal independent-worker policy pauses its acquired job");
            c.Equal(beforePolicy, Physical.Node(pair.A, order, working.Id).Progress, "policy pause earns no extra work in command update");
            c.That(LoadFacts(pair.A).OpenJobs == 1 && LoadCapacity(pair.A).BusyWorkers == 1 && LoadCapacity(pair.A).OrdinaryReady == 0 &&
                LoadSignal(pair.A, "load.paused", "machine.b").Value == Fix.One, "paused assignment is busy, not new open work or ready capacity");
            ReplayPair(c, factory, workingLeft, workingRight, restored, limits, 1, "saved productive independent work to policy hold replay");
            restored = LoadRestore(c, factory, pair); var holdLeft = restored.A.RecordReplay(limits); var holdRight = restored.B.RecordReplay(limits);
            long target = pair.A.Clock.StepIndex + 4; Both(pair, restored, g => g.Clock.StepIndex >= target, "saved policy hold retains real unfinished work");
            c.Equal(beforePolicy, Physical.Node(pair.A, order, working.Id).Progress, "quiet held job cannot earn work through a derived signal");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new CancelOrderCommand { Order = order });
            Both(pair, restored, g => LoadFacts(g).LiveJobs == 0 && LoadRule(g)?.PendingDecision == false, "actual cancelled jobs earn deferred empty-backlog disable");
            c.That(LoadEnabled(pair.A, "machine.b") && !LoadWork(pair.A).TryReadActivityCompletion("activity:inspect", FleetEntity(pair.A, "source"), out _),
                "cancelled productive prefix is not completed work or same-update operation shutdown");
            ReplayPair(c, factory, holdLeft, holdRight, restored, limits, 1, "saved paused work to cancelled backlog decision replay");
            restored = LoadRestore(c, factory, pair); var emptyLeft = restored.A.RecordReplay(limits); var emptyRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => !LoadEnabled(g, "machine.b"), "saved pending false applies normal availability command");
            c.That(LoadCapacity(pair.A).BusyWorkers == 0 && LoadCapacity(pair.A).OrdinaryReady == 0 && initial.LiveJobs == 0,
                "idle disabled workers remain in original capacity and retained initial facts stay detached");
            ReplayPair(c, factory, emptyLeft, emptyRight, restored, limits, 0, "saved empty decision to idle disabled work replay");
            restored = LoadRestore(c, factory, pair); var idleLeft = restored.A.RecordReplay(limits); var idleRight = restored.B.RecordReplay(limits);
            int fresh = 0;
            foreach (var current in new[] { pair, restored }) {
                Submit(c, current, LoadPolicy(current.A, actor, true));
                int sequence = Submit(c, current, new PostOrderCommand { Plan = "inspect", Target = FleetEntity(current.A, "source") });
                if (fresh == 0) fresh = sequence; else c.Equal(fresh, sequence, "fresh actual work retains saved input sequence");
            }
            Both(pair, restored, g => Result(g, 0, fresh)?.Accepted == true && LoadRule(g)?.PendingDecision == true, "new original backlog earns another deferred enable");
            int freshOrder = checked((int)Result(pair.A, 0, fresh).Value.Arg0);
            c.That(freshOrder != order && !LoadEnabled(pair.A, "machine.b") && LoadNodes(pair.A, freshOrder).All(n => n.Progress.Equals(WorkProgress.Zero)),
                "fresh work keeps original duration and inherits no cancelled progress");
            ReplayPair(c, factory, idleLeft, idleRight, restored, limits, 2, "saved idle work to fresh deferred dispatch replay");
            restored = LoadRestore(c, factory, pair); var freshLeft = restored.A.RecordReplay(limits); var freshRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => LoadProductive(g, freshOrder), "saved fresh workload decision produces real partial work");
            c.That(LoadNodes(pair.A, freshOrder).Where(n => n.State != NodeState.Done).All(n => n.RequiredTicks == 12), "both authored nodes retain twelve game minutes");
            ReplayPair(c, factory, freshLeft, freshRight, restored, limits, 0, "saved restart to fresh productive service replay");
            restored = LoadRestore(c, factory, pair); var completionLeft = restored.A.RecordReplay(limits); var completionRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => LoadFacts(g).LiveJobs == 0 && LoadRule(g)?.PendingDecision == false &&
                LoadWork(g).TryReadActivityCompletion("activity:inspect", FleetEntity(g, "source"), out var done) && done.Order == freshOrder,
                "two full real nodes and physical departures earn original completion then empty-backlog intent");
            c.That(LoadEnabled(pair.A, "machine.b") && Physical.Facts(pair.A, FleetEntity(pair.A, "source")).Stock == Fix.FromInt(32),
                "earned work neither fabricates material nor applies new shutdown recursively");
            ReplayPair(c, factory, completionLeft, completionRight, restored, limits, 0, "saved partial work to earned whole activity replay");
            restored = LoadRestore(c, factory, pair); var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => !LoadEnabled(g, "machine.b"), "second saved pending false disables actual idle worker");
            target = pair.A.Clock.StepIndex + 4; Both(pair, restored, g => g.Clock.StepIndex >= target, "quiet completed workload does not repeat work or controls");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, 0, "saved earned completion to settled availability replay");
            ReplayPair(c, factory, left, right, pair, limits, 11, "full original independent-worker workload dispatch replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four actually advanced backlog worlds converge");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "settled actual workload saves without a second count codec");
        }
        private static void WorkQueueRun(Check c)
        {
            var factory = Load.Factory(root => {
                // A line has no passing lane. Incoming waiters stand BEHIND the
                // loading bay, oldest nearest; the idle spare is outside that lane.
                StartupContentAuthoring.Position(root, "source", JsonValue.Number("5"));
                root.Get("entities")[0].Get("components").Get("point_presence").Set("position", JsonValue.Number("5"));
                StartupContentAuthoring.Position(root, "machine.a", JsonValue.Number("5"));
                StartupContentAuthoring.Position(root, "machine.b", JsonValue.Number("4"));
                StartupContentAuthoring.Position(root, "machine.c", JsonValue.Number("3"));
                StartupContentAuthoring.Position(root, "machine.d", JsonValue.Number("0"));
                var slots = root.Get("entities")[0].Get("components").Get("point_dock").Get("slots");
                for (int i = 0; i < slots.Count; i++) {
                    if (slots[i].Get("id").Text == "load.wait.a") slots[i].Set("offset", JsonValue.Number("-1"));
                    if (slots[i].Get("id").Text == "load.wait.b") slots[i].Set("offset", JsonValue.Number("-2"));
                }
            }, recordFlows: true); var pair = LoadPair(Load.New(factory), Load.New(factory));
            Console.WriteLine("MODULES workload-queue=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            var limits = new ReplayLimits(8 * 1024 * 1024, 3000); var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            Submit(c, pair, LoadConfigure(pair.A, "load.queue", "source", "machine.d"));
            Reach(pair, g => LoadRule(g)?.HasApplied == true && !LoadEnabled(g, "machine.d"), "original empty-queue dwell and next-update availability disable");
            var restored = LoadRestore(c, factory, pair); var idleLeft = restored.A.RecordReplay(limits); var idleRight = restored.B.RecordReplay(limits);
            int posted = 0;
            foreach (var current in new[] { pair, restored }) {
                Submit(c, current, new HaulResourcesCommand { Source = FleetEntity(current.A, "source"), Destination = FleetEntity(current.A, "destination"),
                    Worker = FleetEntity(current.A, "machine.a"), Resource = Physical.Supply(current.A), Ceiling = Fix.FromInt(8), Priority = 100 });
                int sequence = Submit(c, current, new PostOrderCommand { Plan = "inspect", Target = FleetEntity(current.A, "source") });
                if (posted == 0) posted = sequence; else c.Equal(posted, sequence, "same actual mixed-queue command identity");
            }
            bool Arrived(SimGame game) => LoadFacts(game, WorkLoadScope.Endpoint, FleetEntity(game, "source")).DockArrivedWaiting == 2;
            Both(pair, restored, Arrived, "two distinct service workers physically arrive behind original occupied loading bay");
            int order = checked((int)Result(pair.A, 0, posted).Value.Arg0);
            var queued = LoadFacts(pair.A, WorkLoadScope.Endpoint, FleetEntity(pair.A, "source"));
            c.That(LoadFacts(pair.A).LiveJobs == 3 && LoadFacts(pair.A).HaulJobs == 1 && LoadFacts(pair.A).ActivityJobs == 2 && queued.DockWaiting == 2 &&
                LoadSignal(pair.A, "load.arrived", "source").Value == Fix.FromInt(2) && LoadNodes(pair.A, order).All(n => n.Progress.Equals(WorkProgress.Zero)),
                "actual arrival/queue facts count three original jobs without earning remote work");
            ulong readHash = pair.A.Hash(); LoadFacts(pair.A); LoadSignal(pair.A, "load.arrived", "source"); LoadCapacity(pair.A);
            c.Equal(readHash, pair.A.Hash(), "actual workload/signal/capacity reads do not mutate claims, time or counts");
            ReplayPair(c, factory, idleLeft, idleRight, restored, limits, 2, "saved idle queue to actual arrived mixed workers replay");
            restored = LoadRestore(c, factory, pair); var queuedLeft = restored.A.RecordReplay(limits); var queuedRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => LoadRule(g)?.PendingDecision == true, "arrived queue earns original one-minute dwell");
            c.That(!LoadEnabled(pair.A, "machine.d"), "queue intent is pending, not same-step availability feedback");
            ReplayPair(c, factory, queuedLeft, queuedRight, restored, limits, 0, "saved arrived queue to deferred extra-worker decision replay");
            restored = LoadRestore(c, factory, pair); var pendingLeft = restored.A.RecordReplay(limits); var pendingRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => LoadProductive(g, order), "same queued worker physically enters released bay and earns service work");
            var working = LoadNodes(pair.A, order).First(n => n.ProductiveSteps > 0 && n.State != NodeState.Done); EntityId actor = working.Worker;
            c.That(LoadEnabled(pair.A, "machine.d") && working.RequiredTicks == 12 && queued.DockArrivedWaiting == 2 &&
                LoadFacts(pair.A, WorkLoadScope.Endpoint, FleetEntity(pair.A, "source")).DockWaiting < queued.DockWaiting,
                "deferred action enables actual spare worker while promotion removes only a real waiting claim");
            ReplayPair(c, factory, pendingLeft, pendingRight, restored, limits, 0, "saved queue decision to actual service promotion replay");
            restored = LoadRestore(c, factory, pair); var workLeft = restored.A.RecordReplay(limits); var workRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, LoadPolicy(current.A, actor, false));
            Both(pair, restored, g => LoadFacts(g, WorkLoadScope.Worker, actor).PausedJobs == 1, "normal servicing-worker policy pause retains acquired physical work");
            var pausedProgress = Physical.Node(pair.A, order, working.Id).Progress;
            c.That(LoadFacts(pair.A).PausedJobs == 1 && LoadSignal(pair.A, "load.paused", pair.A.Seat(0).Snapshot.Entities.Single(e => e.Id == actor).Def).Value == Fix.One,
                "current worker source publishes actual paused job, not open backlog");
            ReplayPair(c, factory, workLeft, workRight, restored, limits, 1, "saved mixed service work to native policy pause replay");
            restored = LoadRestore(c, factory, pair); var holdLeft = restored.A.RecordReplay(limits); var holdRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native clock pause while original service policy and cargo are retained");
            long paused = pair.A.Clock.StepIndex; ulong held = pair.A.Hash();
            Reach(restored, g => g.Clock.StepIndex >= paused + 4, "saved mixed queue/cargo advances on its own game clock"); pair.Round();
            c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == held && restored.A.Clock.StepIndex > paused,
                "paused original does not borrow queue age, handling or work from another session");
            pair.A.Net.RequestResume(); Both(pair, restored, g => g.Clock.StepIndex >= paused + 4, "native resumed mixed work reaches common epoch");
            c.Equal(pausedProgress, Physical.Node(pair.A, order, working.Id).Progress, "disabled servicing worker remains paused while unrelated cargo advances");
            int haulId = Physical.Jobs(pair.A).Single(j => j.IsHaul && LoadLive(j)).Id;
            foreach (var current in new[] { pair, restored }) Submit(c, current, new CancelOrderCommand { Order = order });
            Both(pair, restored, g => LoadFacts(g).ActivityJobs == 0 && LoadRule(g)?.PendingDecision == false, "ordinary service cancellation clears actual arrived queue and earns deferred false");
            c.That(LoadEnabled(pair.A, "machine.d") && Physical.Jobs(pair.A).Any(j => j.Id == haulId && j.IsHaul) && queued.DockArrivedWaiting == 2,
                "clearing service work does not cancel unrelated original haul or mutate retained snapshot");
            ReplayPair(c, factory, holdLeft, holdRight, restored, limits, 1, "saved paused service through actual queue cancellation replay");
            restored = LoadRestore(c, factory, pair); var falseLeft = restored.A.RecordReplay(limits); var falseRight = restored.B.RecordReplay(limits);
            bool PartialDelivery(SimGame game) {
                Fix stock = Physical.Facts(game, FleetEntity(game, "destination")).Stock;
                return !LoadEnabled(game, "machine.d") && stock > Fix.Zero && stock < Fix.FromInt(8) &&
                    Physical.Jobs(game).Any(j => j.Id == haulId && j.Loaded.IsPositive && j.ToilIndex == 3);
            }
            Both(pair, restored, PartialDelivery, "saved cleared-queue decision shuts spare availability while original cargo physically unloads");
            c.That(LoadFacts(pair.A).ActivityJobs == 0 && LoadFacts(pair.A).HaulJobs == 1 && LoadFacts(pair.A).DockWaiting == 0,
                "only real retained haul remains; future receiving promises are not false current queue counts");
            ReplayPair(c, factory, falseLeft, falseRight, restored, limits, 0, "saved empty queue intent to actual partial delivery replay");
            restored = LoadRestore(c, factory, pair); var deliveryLeft = restored.A.RecordReplay(limits); var deliveryRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => LoadFacts(g).LiveJobs == 0 && Physical.Facts(g, FleetEntity(g, "destination")).Stock == Fix.FromInt(8),
                "same retained haul earns full delivery and physical departure");
            long quiet = pair.A.Clock.StepIndex + 4; Both(pair, restored, g => g.Clock.StepIndex >= quiet, "quiet settled mixed workload");
            c.That(Physical.Facts(pair.A, FleetEntity(pair.A, "source")).Stock == Fix.FromInt(24) && LoadCapacity(pair.A).BusyWorkers == 0 &&
                LoadCapacity(pair.A).OrdinaryReady == 2 && !LoadEnabled(pair.A, "machine.d"), "actual32 supplies finish as24 source/eight destination, with truthful remaining capacity");
            BigInteger moved = pair.A.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                .Where(r => r.Kind == ResourceFlowKind.StockTransfer && r.Resource == Physical.Supply(pair.A)).Aggregate(BigInteger.Zero, (sum, r) => sum + r.AmountRaw);
            c.Equal(new BigInteger(Fix.FromInt(16).Raw), moved, "native cumulative history records eight source-to-carrier plus eight carrier-to-destination units");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && restored.Stalls > 0 && restored.ReversedBatches > 0, "actual workload transports stall and shuffle");
            ReplayPair(c, factory, deliveryLeft, deliveryRight, restored, limits, 0, "saved partial delivery to settled mixed queue replay");
            ReplayPair(c, factory, left, right, pair, limits, 5, "full original physical dock/workload control replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "four actually advanced queue/cargo worlds converge");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "settled queue/cargo/work controls save through original codecs");
        }
    }
}
