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
using static Beep.SimEngine.Tests.ControllerParityBoundaryTests;

namespace Beep.SimEngine.Tests
{
    // Pure JSON/master authoring and ordinary players. No feasibility, strategy, money or work implementation here.
    internal static class ControllerFeasibilityTests
    {
        internal const string Case = "Controller feasibility: typed route and optional budget waits retain physical recovery and bounded decisions";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, bool money, bool external, bool quota = false, bool fault = false)
        {
            var controller = Json.Parse(FrameworkProofContent.Read("playbook-recovery.json"));
            if (external || fault) controller.Get("plan_playbooks")[0].Get("steps")[0].Set("recovery", JsonValue.Of("recovery.external"));
            if (fault) controller.Get("recovery_relays")[0].Set("mode", JsonValue.Of("throw"));
            for (int i = 0; i < controller.Get("entity_setups").Count; i++)
                controller.Get("entity_setups")[i].Get("placement").Set("position", JsonValue.Number("70"));
            if (quota)
            {
                var prototype = controller.Get("plan_playbooks")[0].Get("steps")[0]; var steps = JsonValue.NewArray();
                foreach (string name in new[] { "a", "b", "c" }) {
                    var step = prototype.Clone(); step.Set("id", JsonValue.Of(name)); step.Set("group", JsonValue.Of("quota." + name));
                    step.Set("recovery", JsonValue.Null()); steps.Add(step);
                }
                controller.Get("plan_playbooks")[0].Set("steps", steps);
            }
            var docs = new Dictionary<string, string>();
            if (money) { docs.Add("controller-feasibility-economics.json", FrameworkProofContent.Read("controller-feasibility-economics.json"));
                docs.Add("game-mode-properties.json", FrameworkProofContent.Read("game-mode-properties.json")); }
            return ConstructionPlanTests.Points(additionalModule: new PlayerControllerProofModule(),
                moduleDeclaration: "{\"id\":\"sim.player-controllers\",\"version\":\"1.0.0\",\"documents\":[\"playbook-recovery.json\"]}", moduleDocument: controller.ToString(),
                extraModules: money ? new GameModule[] { new TycoonModule() } : null,
                extraDeclarations: money ? "[{\"id\":\"sim.tycoon\",\"version\":\"1.0.0\",\"documents\":[\"controller-feasibility-economics.json\",\"game-mode-properties.json\"]}]" : null,
                extraDocuments: docs, additionalDocuments: new Dictionary<string, string> { ["controller-feasibility.json"] = FrameworkProofContent.Read("controller-feasibility.json") },
                startupProfiles: new[] { "playbook-recovery.start", "recovery.feasibility.start" },
                edit: authored => { if (money) authored.Get("activities")[0].Get("nodes")[1].Set("requirement", JsonValue.Of("recovery.build-ready")); },
                worldEdit: world => { if (half) world.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); },
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(PlayerControllersModule.ModuleId));
                    scenario.Get("settings").Set(PlayerControllersModule.ModuleId, Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"recovery.controller\"},{\"company\":1,\"profile\":\"recovery.controller\"}]}"));
                    if (money) {
                        scenario.Get("modules").Add(JsonValue.Of(TycoonModule.ModuleId)); scenario.Set("game_mode", JsonValue.Of("recovery.funded"));
                        scenario.Get("settings").Set(TycoonModule.ModuleId, Json.Parse("{\"account_policy\":\"recovery.accounts\",\"valuation_policy\":\"recovery.values\",\"income_policy\":\"recovery.income\"}"));
                    }
                });
        }
        private static PlayerControllerFacts Facts(SimGame g)
        { var rows = new List<PlayerControllerFacts>(); g.Seat(1).View.Feature<PlayerControllersView>().CopyControllers(rows); return rows.Single(); }
        private static List<PlaybookStepSnapshot> Steps(SimGame g)
        { var rows = new List<PlaybookStepSnapshot>(); g.Seat(1).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(1), rows); return rows; }
        private static ActivityNodeView Node(SimGame g, string name)
        {
            var plan = Plans(g, 1).Single(x => x.Id == Claim(g).FirstPlan); var rows = new List<ActivityNodeView>();
            if (plan.Order <= 0 || !Work(g, 1).TryCopyNodes(plan.Order, rows)) throw new InvalidOperationException("actual rival work not yet active");
            return rows.Single(x => x.Id == name);
        }
        private static BusinessAccountSnapshot Account(SimGame g)
        {
            var code = g.Seat(1).View.Feature<BusinessView>().ReadAccount(new PlayerId(1), out var facts);
            if (!code.IsNone) throw new InvalidOperationException("actual optional account " + code); return facts;
        }
        private static void Monitor(Check c, SimGame g)
        {
            long previous = Facts(g).Decisions;
            g.Updated += _ => {
                if (g.Fault != null) return;
                var facts = Facts(g); c.That(facts.Decisions >= previous && facts.Decisions - previous <= 1, "declared native cadence permits at most one decision per ordinary Update");
                c.That(facts.AwaitingCommands <= 1, "authored one-command decisions keep at most one actual unresolved submission"); previous = facts.Decisions;
            };
        }
        private static void Tick(SimGame g, SimGame twin = null) => PermanentRemovalTests.Step(g, twin);
        private static void Reach(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            var trail = new Queue<string>();
            for (int i = 0; i < 512 && !done(); i++) {
                trail.Enqueue(g.Clock.StepIndex + ":stock=" + Stock(g, 1) + ":controller=" + Facts(g).Decisions + "/" + Facts(g).Blocker + ":steps=" +
                    string.Join(";", Steps(g).Select(x => x.Id + "/" + x.Status + "/" + x.Blocker + "/" + x.FirstPlan)));
                if (trail.Count > 12) trail.Dequeue(); Tick(g, twin);
            }
            if (!done()) throw new InvalidOperationException(stage + " exceeded512Updates " + string.Join(" | ", trail));
            Console.WriteLine("feasibility stage: " + stage + " at=" + g.Clock.StepIndex);
        }
        private static void Physical(Check c, bool half, bool money, bool external)
        {
            Console.WriteLine("feasibility variant: half=" + half + " money=" + money + " external=" + external);
            var f = Factory(half, money, external); var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong isolation = idle.Hash(); Monitor(c, g);
            var limits = new ReplayLimits(16 * 1024 * 1024, 12000); using var recording = g.RecordReplay(limits);
            c.Equal(money, g.Modules.Contains(TycoonModule.ModuleId), "only explicitly money-enabled mode composes optional business");
            for (int company = 0; company < 2; company++) Submit(c, g, company,
                new ConfigureWorkPolicyCommand { Worker = Entity(g, company, "machine"), Settings = Policy() });
            Submit(c, g, 1, new SetControllerCommand { Controller = Controller.Autopilot });
            Submit(c, g, 0, new ConstructCommand { Def = "point.tower", Placement = Json.Parse("{\"space\":\"field.points\",\"position\":30}"), Priority = 70 });
            long old = Plans(g, 1).Single().Id; var twin = PermanentRemovalTests.Save(c, f, g);
            Reach(g, twin, () => Claim(g).CancellationPlans.Count > 0, "ordinary pending-site recovery"); twin = PermanentRemovalTests.Save(c, f, g);
            Reach(g, twin, () => Claim(g).FirstPlan > old && Plans(g, 1).Single(x => x.Id == Claim(g).FirstPlan).State == PlanState.Active, "actual alternate site");
            Reach(g, twin, () => Node(g, "prepare").Worker.IsValid && Node(g, "prepare").Progress.Equals(WorkProgress.Zero), "real assigned traveling worker");
            c.That(((PointSnapshot)g.Seat(1).Snapshot.Entities.Single(x => x.Id == Entity(g, 1, "machine")).Spatial).Position > Fix.FromInt(42), "route closes before reaching actual alternate site");
            Submit(c, g, 1, new SetPointAccessCommand { Open = false }); twin = PermanentRemovalTests.Save(c, f, g);
            Reach(g, twin, () => Node(g, "prepare").Blocker == WorkSystem.BlockerUnreachable, "typed unreachable route");
            var stopped = Node(g, "prepare"); var retained = Claim(g); long first = retained.FirstPlan;
            for (int i = 0; i < 8; i++) Tick(g, twin);
            c.That(Node(g, "prepare").Progress.Equals(WorkProgress.Zero) && Claim(g).FirstPlan == first && Plans(g, 1).Count == 2 && Claim(g).RecoveryAttempts == 1,
                "closed route retains original active intent, no repeat cancellation/instant alternate or earned work");
            c.Equal(Fix.FromInt(20), Stock(g, 1), "closed route spends/grants no actual resources");
            c.That(!Claim(g).Recovery.Eligible && !Claim(g).Blocker.IsNone, "active recovery waits with actual typed engine blocker");
            if (money) c.That(Account(g).Balance.IsZero && Account(g).Reserved.IsZero && Account(g).GenerationSettlements == 0, "route waits never fabricate money/holds/income");
            twin = PermanentRemovalTests.Save(c, f, g); g.Clock.Pause(); twin.Clock.Pause(); long at = g.Clock.StepIndex; ulong hash = g.Hash();
            g.Update(20000000); twin.Update(20000000); c.That(g.Clock.StepIndex == at && g.Hash() == hash && twin.Hash() == hash, "paused wait earns no decisions/retry/work/income");
            g.Clock.Resume(); twin.Clock.Resume(); Submit(c, g, 1, new SetPointAccessCommand { Open = true }); twin = PermanentRemovalTests.Save(c, f, g);
            if (money)
            {
                Reach(g, twin, () => Node(g, "build").Blocker == WorkSystem.BlockerCash, "typed optional budget shortage");
                var waiting = Node(g, "build"); hash = g.Hash(); c.That(Claim(g).Blocker == WorkSystem.BlockerCash, "same authoritative budget refusal reaches controller diagnostics");
                c.Equal(hash, g.Hash(), "budget/controller observations pure");
                for (int i = 0; i < 8; i++) Tick(g, twin);
                c.That(Node(g, "build").Progress.Equals(WorkProgress.Zero) && Account(g).Balance.IsZero && Stock(g, 1) == Fix.FromInt(20), "unaffordable saved work waits without debt/material consumption");
                c.That(Claim(g).FirstPlan == first && Claim(g).RecoveryAttempts == 1 && waiting.Progress.Equals(WorkProgress.Zero), "budget wait preserves actual graph and detached prefix");
                Submit(c, g, 1, new EnqueueTransformationCommand { Transformer = Entity(g, 1, "recovery.generator"), Capability = new Code("transformation"), Transformation = "recovery.fund", Count = 1 });
                c.Equal(0L, Account(g).GenerationSettlements, "normal enqueue cannot instantly grant generation income"); twin = PermanentRemovalTests.Save(c, f, g);
                Reach(g, twin, () => Account(g).GenerationSettlements == 1, "timed primary generation settles native authored value"); twin = PermanentRemovalTests.Save(c, f, g);
            }
            Reach(g, twin, () => Claim(g).Status == PlaybookStepStatus.Done, "supplied affordable physical rival completion");
            c.Equal(Fix.FromInt(money ? 32 : 12), Stock(g, 1), "real finite stock plus optional20 generated minus8 once");
            c.That(Plans(g, 1).Count == 2 && Claim(g).RecoveryAttempts == 1 && g.Seat(1).Snapshot.Entities.Count(x => x.Owner == new PlayerId(1) && x.Def == "point.tower") == 1,
                "route/money wait produces exactly one physical rival structure");
            c.That(stopped.Progress.Equals(WorkProgress.Zero) && retained.FirstPlan == first, "retained blocked facts never mutate");
            if (money) c.That(Account(g).Balance == Fix.FromInt(20) && Account(g).Reserved.IsZero && Account(g).GenerationSettlements == 1,
                "actual20*5 primary income minus80 physical construction; no buyer/purchase/repeated payment");
            PermanentRemovalTests.Save(c, f, g); c.Equal(isolation, idle.Hash(), "route/budget/controller sessions stay isolated");
            var replay = new ReplayVerifier().Verify(f, recording.Finish(), limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == g.Hash(), "public recovery/route/funding input replay " + replay.Failure + "/" + replay.Detail);
        }
        private static void Quota(Check c, bool half)
        {
            var f = Factory(half, false, false, true); var g = ConstructionPlanTests.NewPoints(f); Monitor(c, g);
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            Submit(c, g, 1, new SetControllerCommand { Controller = Controller.Autopilot });
            c.That(Steps(g).Count(x => x.PendingCommand > 0) == 1 && Plans(g, 1).Count == 0, "three ready steps issue only one actual first command");
            var twin = PermanentRemovalTests.Save(c, f, g); long cursor = 0; int accepted = 0;
            Reach(g, twin, () => Plans(g, 1).Count == 3 && Steps(g).All(x => x.PendingCommand == 0), "bounded authored multi-step decisions");
            var answers = new List<CommandResult>(); g.Seat(1).ReadResults(ref cursor, answers);
            foreach (var answer in answers) if (answer.Command == "sim.place-plan-template" && answer.Accepted) accepted++;
            c.Equal(3, accepted, "exactly three registered placement answers, no scheduler fallback");
            c.That(Facts(g).Decisions >= 3 && Steps(g).All(x => x.FirstPlan > 0), "each original step owns an actual separately admitted intent");
            c.Equal(Fix.FromInt(20), Stock(g, 1), "disabled workers do not fabricate completion while controller plans");
            PermanentRemovalTests.Save(c, f, g); c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "multi-step cadence and real inbox replay");
        }
        private static void Failure(Check c)
        {
            var f = Factory(false, false, true, fault: true); var g = ConstructionPlanTests.NewPoints(f);
            var cached = g.Seat(1).View.Feature<PlayerControllersView>(); g.Seat(1).Submit(new SetControllerCommand { Controller = Controller.Autopilot }); g.Update();
            if (g.Fault == null) g.Seat(0).Submit(new ConstructCommand { Def = "point.tower", Placement = Json.Parse("{\"space\":\"field.points\",\"position\":30}"), Priority = 70 });
            for (int i = 0; i < 8 && g.Fault == null; i++) g.Update();
            c.That(g.Fault != null && g.Fault.ToString().Contains("proof.recovery-fault"), "shared recovery observation preserves original actual callback failure");
            if (g.Fault == null) throw new InvalidOperationException("missing actual recovery failure");
            var first = g.Fault; var rows = new List<PlaybookStepSnapshot> { null };
            c.Throws<InvalidOperationException>(() => cached.CopyPlaybook(new PlayerId(1), rows), "failed provider cannot be retried through cached diagnostics");
            c.Equal(0, rows.Count, "faulted callback publishes no partial facts"); c.Throws<InvalidOperationException>(() => g.Save(), "failed observations cannot checkpoint");
            c.Equal(0, g.Update(), "terminal recovery never retries"); c.That(ReferenceEquals(first, g.Fault), "first callback remains terminal cause");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current feasibility build");
            foreach (bool half in new[] { false, true }) foreach (bool money in new[] { false, true }) foreach (bool external in new[] { false, true }) Physical(c, half, money, external);
            Quota(c, false); Quota(c, true); Failure(c);
        }
    }
}
