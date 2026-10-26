using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string ControllerCase = "Networked controllers: human takeover retains authored decisions, physical plans and saved command identity";
        private static PlayerControllerFacts ControllerFacts(SimGame game)
        {
            var rows = new List<PlayerControllerFacts>(); game.Seat(0).View.Feature<PlayerControllersView>().CopyControllers(rows);
            if (rows.Count != 1) throw new InvalidOperationException("actual authored company controller missing");
            return rows[0];
        }
        private static List<PlanView> ControllerPlans(SimGame game)
        {
            var rows = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlanGroup("autopilot.field", rows); return rows;
        }
        private static Fix ControllerStock(SimGame game)
        {
            if (!game.Seat(0).View.TryFindResource("supply", out var supply)) throw new InvalidOperationException("authored supply missing");
            Code refusal = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(Entity(game, "source"), supply, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual source stock/" + refusal);
            return value.Stock;
        }
        private static string ControllerDiagnostics(SimGame game)
        {
            var facts = ControllerFacts(game);
            return "at=" + game.Clock.StepIndex + "/controller=" + game.Seat(0).Controller + "/decisions=" + facts.Decisions +
                "/awaiting=" + facts.AwaitingCommands + "/answers=" + facts.PendingAnswers + "/plans=" + ControllerPlans(game).Count + "/stock=" + ControllerStock(game);
        }
        private static void ControllerObserve(SimGame game)
        {
            var foreign = new List<PlayerControllerFacts>(); game.Seat(1).View.Feature<PlayerControllersView>().CopyControllers(foreign);
            if (foreign.Count != 0 || ControllerPlans(game).Count > 2 || ControllerStock(game) < Fix.FromInt(4))
                throw new InvalidOperationException("controller state leaked or duplicated actual plans/materials/" + ControllerDiagnostics(game));
        }
        private static Pair ControllerPair(SimGame a, SimGame b) => new Pair(a, b, ControllerObserve, ControllerDiagnostics, 2);
        private static bool ControllerPartial(SimGame game)
        {
            foreach (var plan in ControllerPlans(game)) {
                var nodes = new List<ActivityNodeView>();
                if (plan.Order > 0 && game.Seat(0).View.Feature<WorkView>().TryCopyNodes(plan.Order, nodes) &&
                    nodes.Any(n => n.Id == "build" && n.Progress.WholeTicks > 0 && n.Progress.WholeTicks < n.RequiredTicks)) return true;
            }
            return false;
        }
        private static void ControllersRun(Check c)
        {
            var factory = PlayerControllerTests.Factory();
            var pair = ControllerPair(ConstructionPlanTests.NewPoints(factory), ConstructionPlanTests.NewPoints(factory));
            Console.WriteLine("MODULES controllers=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            var limits = new ReplayLimits(8 * 1024 * 1024, 2000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int policy = Submit(c, pair, new ConfigureWorkPolicyCommand { Worker = Entity(pair.A, "machine"),
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") },
                    null, true, true, WorkAssignmentMode.All) });
            Reach(pair, g => Result(g, 0, policy)?.Accepted == true, "ordinary worker policy admitted before autonomous intent");
            int automatic = Submit(c, pair, new SetControllerCommand { Controller = Controller.Autopilot });
            Reach(pair, g => g.Seat(0).Controller == Controller.Autopilot && ControllerFacts(g).AwaitingCommands == 1,
                "networked handover earns first native pending template command");
            c.That(Result(pair.A, 0, automatic)?.Accepted == true && ControllerPlans(pair.A).Count == 0,
                "native decision queues an ordinary command without instant projects");
            var restored = ControllerPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var middleLeft = restored.A.RecordReplay(limits); var middleRight = restored.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), restored.A.Hash(), "pending authored controller inbox and cadence reattach exactly");
            Both(pair, restored, g => ControllerPlans(g).Count == 2 && ControllerFacts(g).PendingAnswers > 0,
                "actual template result waits for normal controller cadence");
            Both(pair, restored, ControllerPartial, "same actual machine earns partial supplied construction");
            c.That(pair.A.Seat(0).Snapshot.Entities.Count(e => e.Def == "point.tower") < 2,
                "productive work is not fabricated completed output");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new SetControllerCommand { Controller = Controller.Human });
            Both(pair, restored, g => g.Seat(0).Controller == Controller.Human, "normal network input takes a live autopilot seat back");
            long decisions = ControllerFacts(pair.A).Decisions; long target = pair.A.Clock.StepIndex + 5;
            Both(pair, restored, g => g.Clock.StepIndex >= target, "human takeover retains dormant controller memory");
            c.Equal(decisions, ControllerFacts(pair.A).Decisions, "human handover stops new autonomous decisions, not physical work");
            var results = new List<CommandResult>(); long cursor = 0; pair.A.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Any(r => r.Stream == CommandStreams.Player && r.Sequence == 1) &&
                results.Any(r => r.Stream == new Code(PlayerControllersModule.ModuleId) && r.Sequence == 1),
                "player and authored controller keep distinct actual same-number results");
            c.That(results.GroupBy(r => (r.Player, r.Stream, r.Sequence)).All(group => group.Count() == 1),
                "no duplicate full command identity survives the handover");
            ReplayPair(c, factory, middleLeft, middleRight, restored, limits, 1, "pending-controller to human-takeover replay");
            restored = ControllerPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var lateLeft = restored.A.RecordReplay(limits); var lateRight = restored.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), restored.A.Hash(), "saved human handover retains exact controller and work state");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new SetControllerCommand { Controller = Controller.Autopilot });
            Both(pair, restored, g => g.Seat(0).Controller == Controller.Autopilot && ControllerFacts(g).Decisions > decisions,
                "overdue authored strategy resumes without catch-up bursts or lost commands");
            Both(pair, restored, g => ControllerPlans(g).Count == 2 && ControllerPlans(g).All(p => p.State == PlanState.Done),
                "native physically supplied machine completes both authored structures");
            c.Equal(2, pair.A.Seat(0).Snapshot.Entities.Count(e => e.Def == "point.tower"), "native work alone creates exactly two actual towers");
            c.Equal(Fix.FromInt(4), ControllerStock(pair.A), "finite source pays exactly sixteen construction materials");
            target = pair.A.Clock.StepIndex + 8; Both(pair, restored, g => g.Clock.StepIndex >= target, "settled strategy does not duplicate plans");
            c.That(ControllerPlans(pair.A).Count == 2 && ControllerFacts(pair.A).AwaitingCommands == 0,
                "completed strategy retains native history without repeated work");
            ReplayPair(c, factory, lateLeft, lateRight, restored, limits, 1, "human-takeover to completed-autopilot replay");
            ReplayPair(c, factory, left, right, pair, limits, 4, "full networked controller handover replay");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && pair.A.Hash() == restored.A.Hash(),
                "real delayed transport, independently advanced sessions and final states agree");
        }
    }
}
