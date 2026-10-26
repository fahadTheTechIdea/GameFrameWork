using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Player inputs and immutable facts only. Strategy, scheduling and physical work stay in engine owners.
    internal static class ControllerParityBoundaryTests
    {
        internal const string Case = "Controller parity: ordinary refusals physical rivals and terminal private diagnostics share engine boundaries";
        internal static void Register() => TestRunner.Add(Case, Run);
        private sealed class Input : PlayerController
        {
            private readonly Action<PlayerDecisionContext> _input;
            internal Input(Action<PlayerDecisionContext> input) { _input = input; }
            public override void Decide(PlayerDecisionContext context) => _input(context);
        }
        internal static WorkView Work(SimGame g, int company) => g.Seat(company).View.Feature<WorkView>();
        internal static EntityId Entity(SimGame g, int company, string definition)
            => g.Seat(company).Snapshot.Entities.Single(x => x.Owner == new PlayerId(company) && x.Def == definition).Id;
        internal static List<PlanView> Plans(SimGame g, int company)
        { var rows = new List<PlanView>(); Work(g, company).CopyPlans(rows); return rows; }
        internal static PlaybookStepSnapshot Claim(SimGame g)
        {
            var rows = new List<PlaybookStepSnapshot>();
            if (!g.Seat(1).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(1), rows))
                throw new InvalidOperationException("actual rival playbook unavailable");
            return rows.Single();
        }
        internal static Fix Stock(SimGame g, int company)
        {
            var view = g.Seat(company).View;
            if (!view.TryFindResource("supply", out var supply)) throw new InvalidOperationException("authored supply unavailable");
            Fix amount = Fix.Zero;
            foreach (var entity in g.Seat(company).Snapshot.Entities.Where(x => x.Owner == new PlayerId(company)))
                if (view.Feature<LogisticsView>().ReadDiagnostics(entity.Id, supply, out var facts).IsNone) amount += facts.Stock;
            return amount;
        }
        private static JsonValue At(int position) => Json.Parse("{\"space\":\"field.points\",\"position\":" + position + "}");
        internal static WorkPolicySettings Policy() => new WorkPolicySettings(true, 0,
            new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All);
        private static void Tick(SimGame g, SimGame twin = null) => PermanentRemovalTests.Step(g, twin);
        private static CommandResult Result(SimGame g, int company, int sequence)
        {
            long cursor = 0; var rows = new List<CommandResult>(); g.Seat(company).ReadResults(ref cursor, rows);
            return rows.Single(x => x.Sequence == sequence);
        }
        internal static CommandResult Submit(Check c, SimGame g, int company, Command command)
        {
            ulong before = g.Hash(); c.Equal(Code.None, g.Seat(company).Preview(command), "actual owned ordinary preview");
            c.Equal(before, g.Hash(), "ordinary preview is pure"); int sequence = g.Seat(company).Submit(command); Tick(g);
            var answer = Result(g, company, sequence); c.That(answer.Accepted, "actual ordinary answer " + answer.Refusal);
            if (!answer.Accepted) throw new InvalidOperationException("ordinary company command refused " + answer.Refusal);
            return answer;
        }
        private static void Refusals(Check c, SimGame g)
        {
            Func<int, Command>[] commands = {
                company => new SetControllerCommand { Controller = (Controller)200 },
                company => new SetWorkerHeldCommand { Worker = EntityId.None, Held = true },
                company => new ConfigureWorkPolicyCommand { Worker = Entity(g, 1-company, "machine"), Settings = Policy() },
                company => new HaulResourcesCommand { Source = Entity(g, company, "source"), Destination = EntityId.None,
                    Worker = Entity(g, company, "machine"), Resource = PermanentRemovalTests.Supply(g), Ceiling = Fix.One, Priority = 70 },
                company => new ConstructCommand { Def = "missing.tower", Placement = At(30), Priority = 70 },
                company => new ConstructCommand { Def = "point.tower", Placement = At(1000), Priority = 70 },
                company => new SubmitPlanCommand { Intent = new PlanIntent("missing.plan", JsonValue.NewObject(), "refusal", 70, false) },
                company => new ConfigurePlanCommand { Plan = long.MaxValue, Group = "refusal", Priority = 70, Paused = true },
                company => new CancelPlanCommand { Selection = null }
            };
            foreach (var make in commands)
            {
                Code? playerCode = null;
                for (int company = 0; company < 2; company++)
                {
                    Command command = make(company); ulong before = g.Hash(); Code refusal = g.Seat(company).Preview(command);
                    c.That(!refusal.IsNone, "malformed/missing/foreign intent has a typed ordinary refusal");
                    c.Equal(before, g.Hash(), "refusal preview changes no state");
                    if (playerCode.HasValue) c.Equal(playerCode.Value, refusal, "mapped rival/player permissions have identical refusal semantics");
                    playerCode = refusal;
                    int human = g.Seat(company).Submit(command); Tick(g);
                    c.That(!Result(g, company, human).Accepted && Result(g, company, human).Refusal == refusal, "actual seat answer matches preview");
                    int sequence = 0; bool sent = false;
                    var input = new Input(context => {
                        if (sent) return;
                        c.Equal(new PlayerId(company), context.Id, "decision input remains company scoped");
                        c.Equal(refusal, context.Preview(command), "same decision preview uses ordinary dispatcher");
                        sequence = context.Submit(command); sent = true;
                    });
                    g.Seat(company).Attach(input);
                    try { Tick(g); } finally { g.Seat(company).Detach(input); }
                    c.That(sent && sequence > 0, "decision uses actual registered input sequence");
                    var answer = Result(g, company, sequence);
                    c.That(!answer.Accepted && answer.Refusal == refusal, "decision actual answer has same refusal, no privileged handler");
                    c.Equal(Fix.FromInt(20), Stock(g, 0), "refused inputs never grant/spend player stock");
                    c.Equal(Fix.FromInt(20), Stock(g, 1), "refused inputs never grant/spend rival stock");
                    c.That(Plans(g, 0).Count == 0 && Plans(g, 1).Count == 0, "refused intents create no fallback project");
                }
            }
        }
        private static void Reach(Check c, SimGame g, SimGame twin, Func<bool> done, string stage, Action observe = null)
        {
            var trail = new Queue<string>();
            for (int i = 0; i < 512 && !done(); i++)
            {
                trail.Enqueue(g.Clock.StepIndex + ":stock=" + Stock(g, 0) + "/" + Stock(g, 1) + ":claim=" +
                    Claim(g).Status + "/" + Claim(g).FirstPlan + "/" + Claim(g).RecoveryAttempts + ":plans=" +
                    string.Join(";", Plans(g, 1).Select(x => x.Id + "/" + x.State + "/" + x.Blocker)) + ":work=" + Diagnostic(g));
                if (trail.Count > 12) trail.Dequeue(); Tick(g, twin); observe?.Invoke();
                for (int company = 0; company < 2; company++) c.That(Stock(g, company) >= Fix.FromInt(12) && Stock(g, company) <= Fix.FromInt(20),
                    "physical stocks remain finite and spend at most actual eight-input construction");
            }
            if (!done()) throw new InvalidOperationException(stage + " exceeded512Updates " + string.Join(" | ", trail));
            Console.WriteLine("controller stage: " + stage + " at=" + g.Clock.StepIndex);
        }
        private static string Diagnostic(SimGame g)
        {
            var facts = new List<string>();
            for (int company = 0; company < 2; company++)
            {
                var jobs = new List<WorkJobView>(); Work(g, company).CopyJobs(jobs);
                facts.Add(company + ":machine=" + ((PointSnapshot)g.Seat(company).Snapshot.Entities.Single(x => x.Id == Entity(g, company, "machine")).Spatial).Position +
                    ":jobs=" + string.Join(";", jobs.Select(x => x.Id + "/" + x.State + "/" + x.Worker + "/" + x.ToilIndex + "/" + x.Blocker)));
                foreach (var plan in Plans(g, company).Where(x => x.Order > 0))
                {
                    var nodes = new List<ActivityNodeView>(); Work(g, company).TryCopyNodes(plan.Order, nodes);
                    facts.Add("nodes=" + string.Join(";", nodes.Select(x => x.Id + "/" + x.State + "/" + x.Progress + "/" + x.Blocker)));
                }
            }
            return string.Join(" | ", facts);
        }
        private static void Physical(Check c, bool half, bool external)
        {
            Console.WriteLine("controller variant: half=" + half + " independentRecovery=" + external);
            var f = PlaybookRecoveryTests.Factory(json => {
                if (external) json.Get("plan_playbooks")[0].Get("steps")[0].Set("recovery", JsonValue.Of("recovery.external"));
                // Separate actual depots/carriers on opposite sides of the two sites: no overlapping startup or 1D crossing shortcut.
                for (int i = 0; i < json.Get("entity_setups").Count; i++)
                    json.Get("entity_setups")[i].Get("placement").Set("position", JsonValue.Number("70"));
            }, half);
            var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong isolation = idle.Hash();
            var limits = new ReplayLimits(16 * 1024 * 1024, 12000); using var recording = g.RecordReplay(limits);
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "resource-only colony workers do not require business"); Refusals(c, g);
            for (int company = 0; company < 2; company++) Submit(c, g, company,
                new ConfigureWorkPolicyCommand { Worker = Entity(g, company, "machine"), Settings = Policy() });
            Submit(c, g, 1, new SetControllerCommand { Controller = Controller.Autopilot });
            c.That(Claim(g).PendingCommand > 0 && Plans(g, 1).Count == 0, "native rival queues ordinary intent, not completed structure");
            Submit(c, g, 0, new ConstructCommand { Def = "point.tower", Placement = At(30), Priority = 70 });
            var old = Plans(g, 1).Single();
            var replyTwin = PermanentRemovalTests.Save(c, f, g);
            Reach(c, g, replyTwin, () => Claim(g).FirstPlan == old.Id, "native cadence consumes actual placement answer");
            var retained = Claim(g);
            c.That(old.State == PlanState.Pending && old.Blocker == Codes.Blocked && old.Target == EntityId.None,
                "actual occupied site preserves pending rival intent, no instant fallback");
            c.That(retained.FirstPlan == old.Id && retained.Recovery.Eligible && !retained.Recovery.Ready, "actual linked plan waits sole authored delay");
            c.Equal(Fix.FromInt(20), Stock(g, 1), "unstarted rival wait spends/grants nothing");
            ulong before = g.Hash(); var hidden = new List<PlaybookStepSnapshot> { retained };
            c.That(!g.Seat(0).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(1), hidden) && hidden.Count == 0, "foreign rival strategy remains private");
            c.Equal(before, g.Hash(), "private diagnostics are pure");
            c.Throws<NotSupportedException>(() => ((IList<PlaybookSiteSnapshot>)retained.Sites).Clear(), "retained site facts immutable");
            var twin = PermanentRemovalTests.Save(c, f, g); g.Clock.Pause(); twin.Clock.Pause(); before = g.Hash(); long time = g.Clock.StepIndex;
            g.Update(20000000); twin.Update(20000000);
            c.That(g.Clock.StepIndex == time && g.Hash() == before && twin.Hash() == before, "paused clock earns no decision/recovery/travel time");
            g.Clock.Resume(); twin.Clock.Resume();
            Reach(c, g, twin, () => Claim(g).CancellationPlans.Count > 0, "authored delayed cancellation");
            c.That(Claim(g).FirstPlan == old.Id && Claim(g).Recovering && Claim(g).RecoveryAttempts == 1, "actual cancellation awaits next-update answer and retains original link");
            twin = PermanentRemovalTests.Save(c, f, g);
            Reach(c, g, twin, () => Claim(g).FirstPlan > old.Id, "saved answer and alternate placement");
            var replacement = Plans(g, 1).Single(x => x.Id == Claim(g).FirstPlan);
            c.That(Plans(g, 1).Single(x => x.Id == old.Id).State == PlanState.Cancelled && replacement.Arguments.Get("placement").Get("position").Text == "42",
                "ordinary cancel commits before existing selector proposes different real site");
            c.That(replacement.Group == "recovery.claim.recovery.1" && retained.RecoveryAttempts == 0 && !retained.Recovering, "detached old strategy never changes");
            c.Equal(Fix.FromInt(20), Stock(g, 1), "replacement retains original finite stock");
            bool moved = false, carried = false, earned = false; twin = PermanentRemovalTests.Save(c, f, g);
            Reach(c, g, twin, () => Claim(g).Status == PlaybookStepStatus.Done && g.Seat(0).Snapshot.Entities.Any(x => x.Def == "point.tower" && x.Owner == new PlayerId(0)),
                "physical player and rival construction", () => {
                    var machine = g.Seat(1).Snapshot.Entities.Single(x => x.Id == Entity(g, 1, "machine"));
                    moved |= ((PointSnapshot)machine.Spatial).Position.IsPositive;
                    var jobs = new List<WorkJobView>(); Work(g, 1).CopyJobs(jobs); carried |= jobs.Any(x => x.IsHaul && x.Loaded.IsPositive);
                    foreach (var plan in Plans(g, 1).Where(x => x.Order > 0)) {
                        var nodes = new List<ActivityNodeView>(); Work(g, 1).TryCopyNodes(plan.Order, nodes);
                        earned |= nodes.Any(x => x.Progress.WholeTicks > 0 && x.State != NodeState.Done);
                    }
                });
            c.That(moved && carried && earned, "native rival physically travels, carries finite stock and earns nonterminal work");
            c.Equal(Fix.FromInt(12), Stock(g, 0), "player consumes exactly eight actual units");
            c.Equal(Fix.FromInt(12), Stock(g, 1), "rival consumes exactly eight actual units without grant/refund");
            c.That(Plans(g, 1).Count == 2 && Claim(g).RecoveryAttempts == 1 && g.Seat(0).Snapshot.Entities.Count(x => x.Def == "point.tower") == 2,
                "one cancelled intent and one real alternate; no duplicate/fallback completion");
            PermanentRemovalTests.Save(c, f, g);
            var replay = new ReplayVerifier().Verify(f, recording.Finish(), limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == g.Hash(), "ordinary player/controller command stream replays: " + replay.Failure + "/" + replay.Detail);
            c.Equal(isolation, idle.Hash(), "controller memory and physical state remain session-local");
        }
        private static void FaultBoundary(Check c, string mode)
        {
            var f = mode == "recovery" ? PlaybookRecoveryTests.Factory(json => {
                json.Get("plan_playbooks")[0].Get("steps")[0].Set("recovery", JsonValue.Of("recovery.external"));
                json.Get("recovery_relays")[0].Set("mode", JsonValue.Of("throw"));
            }) : PlayerControllerTests.Factory("reply.proof", json => json.Get("reply_controllers")[0].Set("mode", JsonValue.Of(mode)));
            var g = ConstructionPlanTests.NewPoints(f);
            var views = new[] { g.Seat(0).View.Feature<PlayerControllersView>(), g.Seat(1).View.Feature<PlayerControllersView>(), g.Observer.Feature<PlayerControllersView>() };
            var retained = new List<PlayerControllerFacts>(); views[0].CopyControllers(retained); var fact = retained.Single();
            g.Seat(mode == "recovery" ? 1 : 0).Submit(new SetControllerCommand { Controller = Controller.Autopilot });
            g.Update();
            if (mode == "recovery" && g.Fault == null) g.Seat(0).Submit(new ConstructCommand { Def = "point.tower", Placement = At(30), Priority = 70 });
            for (int i = 0; i < 10 && g.Fault == null; i++) g.Update();
            c.That(g.Fault != null, "actual authored " + mode + " callback creates terminal first fault");
            if (g.Fault == null) throw new InvalidOperationException("missing actual " + mode + " provider failure");
            var first = g.Fault;
            string expected = mode == "recovery" ? "proof.recovery-fault" : mode == "budget" ? "authored command budget" : "context has expired";
            c.That(first.ToString().Contains(expected), "original callback cause is not masked by observation verification");
            foreach (var view in views)
            {
                var rows = new List<PlayerControllerFacts> { fact };
                c.Throws<InvalidOperationException>(() => view.CopyControllers(rows), "cached controller query rejects first fault before private shortcut");
                c.Equal(0, rows.Count, "faulted controller destination cleared");
                foreach (int company in new[] { 0, 1, 99 })
                {
                    var steps = new List<PlaybookStepSnapshot> { null };
                    c.Throws<InvalidOperationException>(() => view.CopyPlaybook(new PlayerId(company), steps), "cached own/foreign/missing playbook rejects terminal state");
                    c.Equal(0, steps.Count, "faulted playbook publishes no partial facts");
                }
                foreach (var selection in new[] { Controller.Human, Controller.Autopilot, (Controller)200 })
                    c.Throws<InvalidOperationException>(() => view.PreviewSelection(selection), "cached valid/invalid selection rejects terminal state");
            }
            c.Throws<InvalidOperationException>(() => g.Save(), "faulted controller cannot checkpoint");
            c.Throws<InvalidOperationException>(() => g.Hash(), "faulted controller cannot publish healthy hash");
            c.Equal(0, g.Update(), "terminal Update never retries callback");
            c.That(ReferenceEquals(first, g.Fault) && fact.Decisions == 0, "original first failure and retained facts remain stable");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current controller-boundary build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "unchanged actual save format");
            foreach (bool half in new[] { false, true }) foreach (bool external in new[] { false, true }) Physical(c, half, external);
            foreach (string mode in new[] { "budget", "expired", "recovery" }) FaultBoundary(c, mode);
        }
    }
}
