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
    internal static class PlaybookRecoveryTests
    {
        internal const string Case = "Player controllers: authored pending-plan recovery, physical rivals and durable cancellation";
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(Action<JsonValue> edit = null, bool half = false)
        {
            JsonValue json = Content.Json.Parse(FrameworkProofContent.Read("playbook-recovery.json")); edit?.Invoke(json);
            return ConstructionPlanTests.Points(additionalModule: new PlayerControllerProofModule(),
                moduleDeclaration: "{\"id\":\"sim.player-controllers\",\"version\":\"1.0.0\",\"documents\":[\"playbook-recovery.json\"]}",
                moduleDocument: json.ToString(), scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(PlayerControllersModule.ModuleId));
                    scenario.Get("settings").Set(PlayerControllersModule.ModuleId, Content.Json.Parse(
                        "{\"companies\":[{\"company\":0,\"profile\":\"recovery.controller\"},{\"company\":1,\"profile\":\"recovery.controller\"}]}"));
                }, startupProfiles: new[] { "playbook-recovery.start" }, worldEdit: world => {
                    if (half) world.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static WorkView Work(SimGame game, int company = 1) => game.Seat(company).View.Feature<WorkView>();
        private static List<PlanView> Plans(SimGame game, int company = 1)
        { var plans = new List<PlanView>(); Work(game, company).CopyPlans(plans); return plans; }
        private static PlaybookStepSnapshot Claim(SimGame game, int company = 1)
        {
            var steps = new List<PlaybookStepSnapshot>();
            if (!game.Seat(company).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(company), steps))
                throw new InvalidOperationException("missing recovery diagnostics");
            return steps.Single();
        }
        private static Fix Stock(SimGame game, int company)
        {
            Fix amount = Fix.Zero; ResourceId supply = game.World.Content.ResourceId("supply");
            foreach (var entity in game.World.Entities.All)
                if (entity.Alive && entity.Owner == new PlayerId(company) && entity.Get<IInletPort>() is IInletPort inlet)
                    amount += inlet.Amount(supply);
            return amount;
        }
        private static JsonValue At(int position) => Content.Json.Parse("{\"space\":\"field.points\",\"position\":" + position + "}");
        private static ConstructCommand Build(int position) => new ConstructCommand { Def = "point.tower", Placement = At(position), Priority = 70 };
        private static CommandResult Order(Check c, SimGame game, int company, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(company).Preview(command), "ordinary owned command previews");
            c.Equal(hash, game.Hash(), "ordinary preview is pure");
            var result = TestKit.Order(game, company, command); c.That(result.Accepted, "ordinary command accepted: " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("command refused: " + result.Refusal);
            return result;
        }
        private static void Configure(Check c, SimGame game)
        {
            for (int company = 0; company < 2; company++)
            {
                var id = game.Seat(company).Snapshot.Entities.Single(e => e.Owner == new PlayerId(company) && e.Def == "machine").Id;
                Order(c, game, company, new ConfigureWorkPolicyCommand { Worker = id, Settings = new WorkPolicySettings(
                    true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            }
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.RunTicks(1); twin?.RunTicks(1);
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("recovery time=" + game.Tick + "/fault=" + game.Fault + "/twin=" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + "/time=" + game.Tick + "/plans=" +
                string.Join(";", Plans(game).Select(p => p.Id + "/" + p.State + "/" + p.Blocker)) +
                "/claim=" + Claim(game).Status + "/" + Claim(game).BlockedSince + "/" + Claim(game).RecoveryAttempts);
        }
        private static long Race(Check c, SimGame game, bool recovery = true)
        {
            Order(c, game, 1, new SetControllerCommand { Controller = Controller.Autopilot });
            c.That(Claim(game).PendingCommand > 0 && Plans(game).Count == 0, "rival queued normal placement, not instant building");
            Order(c, game, 0, Build(30));
            var plan = Plans(game).Single();
            c.That(plan.State == PlanState.Pending && plan.Blocker == Codes.Blocked && plan.Target == EntityId.None,
                "player claimed site after proposal; accepted rival intent remains unstarted");
            c.That(Claim(game).FirstPlan == plan.Id && (recovery ? Claim(game).BlockedSince >= 0 : Claim(game).BlockedSince == -1) && Claim(game).RecoveryAttempts == 0,
                "native controller links actual accepted plan and starts sole-clock observed wait");
            c.Equal(Fix.FromInt(20), Stock(game, 1), "blocked rival intent consumes no physical stock");
            return plan.Id;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current recovery engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "breaking command/playbook wire has one current reader");
            Physical(c, false); Physical(c, true); Controls(c); Exhausted(c); NoSites(c); Authoring(c); ProviderFault(c);
        }
        private static void Physical(Check c, bool external)
        {
            var factory = Factory(external ? json => json.Get("plan_playbooks")[0].Get("steps")[0].Set("recovery", JsonValue.Of("recovery.external")) : null);
            var game = New(factory); Configure(c, game);
            using (var recorder = game.RecordReplay(Limits))
            {
                long old = Race(c, game); var initial = Claim(game); ulong hash = game.Hash();
                c.That(initial.Recovery.Eligible && !initial.Recovery.Ready, "authored delay blocks immediate recovery");
                c.Equal(hash, game.Hash(), "recovery/private plan diagnostics are pure");
                c.Throws<NotSupportedException>(() => ((IList<PlaybookSiteSnapshot>)initial.Sites).Clear(), "retained recovery facts are immutable");
                var hidden = new List<PlaybookStepSnapshot> { initial };
                c.That(!game.Seat(0).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(1), hidden) && hidden.Count == 0,
                    "foreign rival strategy remains private");
                var twin = factory.Load(game.Save()); Step(game, twin);
                game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long time = game.Tick;
                game.Update(20000000); twin.Update(20000000);
                c.That(game.Tick == time && game.Hash() == hash && twin.Hash() == hash, "paused sole clock earns no recovery time");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, () => Claim(game).CancellationPlans.Count > 0, "authored cancellation issued", twin);
                var cancelling = Claim(game);
                c.That(cancelling.FirstPlan == old && cancelling.Recovering && cancelling.RecoveryAttempts == 1 &&
                    Plans(game).Single().State == PlanState.Pending, "actual original link retained until next-update cancellation");
                c.That(initial.RecoveryAttempts == 0 && !initial.Recovering, "old detached recovery facts do not mutate");
                twin = factory.Load(game.Save());
                Until(game, () => Claim(game).FirstPlan > old, "saved cancellation answer and current alternate placement", twin);
                var replacement = Plans(game).Single(p => p.Id == Claim(game).FirstPlan);
                c.That(replacement.Arguments.Get("placement").Get("position").Text == "42" &&
                    replacement.Group == "recovery.claim.recovery.1", "same current site selector chooses genuine alternate; old group history preserved");
                c.That(Plans(game).Single(p => p.Id == old).State == PlanState.Cancelled, "old native intent really cancelled, not relocated");
                c.Equal(Fix.FromInt(20), Stock(game, 1), "replacement begins with existing stock only");
                twin = factory.Load(game.Save());
                Until(game, () => Claim(game).Status == PlaybookStepStatus.Done &&
                    game.Seat(0).Snapshot.Entities.Any(e => e.Def == "point.tower" && e.Owner == new PlayerId(0)), "real rival/player hauling and construction", twin);
                c.Equal(Fix.FromInt(12), Stock(game, 0), "player physical build consumes8");
                c.Equal(Fix.FromInt(12), Stock(game, 1), "rival physical alternate build consumes8; no refund/grant");
                c.That(Plans(game).Count == 2 && Claim(game).RecoveryAttempts == 1, "no duplicate automatic projects or retries");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(),
                    "physical recovery and cancellation replay identically: " + replay.Failure + "/" + replay.Detail);
            }
            c.That(Claim(New(factory)).RecoveryAttempts == 0 && Plans(New(factory)).Count == 0, "interleaved session owns no copied recovery state");
        }
        private static void Controls(Check c)
        {
            var factory = Factory(); var game = New(factory); Configure(c, game); long plan = Race(c, game);
            Order(c, game, 1, new ConfigurePlanCommand { Plan = plan, Group = "player.paused", Priority = 70, Paused = true });
            var twin = factory.Load(game.Save()); for (int i = 0; i < 8; i++) Step(game, twin);
            c.That(Claim(game).BlockedSince == -1 && !Claim(game).Recovery.Eligible && Claim(game).RecoveryAttempts == 0 &&
                Plans(game).Single().Paused, "player pause resets observed delay and prevents automatic cancellation");
            Order(c, game, 1, new ConfigurePlanCommand { Plan = plan, Group = "player.paused", Priority = 70, Paused = false });
            c.That(Claim(game).BlockedSince >= 0 && Claim(game).CancellationPlans.Count == 0, "unpause earns a fresh delay rather than past paused time");
            var active = New(factory); Configure(c, active);
            var arguments = JsonValue.NewObject(); arguments.Set("placement", At(60));
            long pending = Order(c, active, 0, new SubmitPlanCommand { Intent = new PlanIntent("build:point.tower", arguments, "guard", 70, true) }).Arg0;
            var cancel = new CancelPlanCommand { Selection = new PlanCancellationSelection(true, new[] { pending }) };
            c.Equal(Code.None, active.Seat(0).Preview(cancel), "current pending-only preview is ready");
            Order(c, active, 0, new ConfigurePlanCommand { Plan = pending, Group = "guard", Priority = 70, Paused = false });
            c.That(Plans(active, 0).Single().State == PlanState.Active, "normal update physically activates original plan");
            ulong hash = active.Hash();
            c.Equal(PlanCodes.NotPending, active.Seat(0).Preview(cancel), "stale pending preview cannot cancel activated work");
            c.Equal(PlanCodes.NotPending, Work(active, 0).PreviewPlanCancel(new PlanCancellationSelection(true, new[] { pending })).Blocker, "view and normal command share fresh cancellation guard");
            c.Equal(hash, active.Hash(), "active refusal preserves resources/project/state");
            var refused = TestKit.Order(active, 0, cancel);
            c.That(!refused.Accepted && refused.Refusal == PlanCodes.NotPending && Plans(active, 0).Single().State == PlanState.Active,
                "actual pending-only cancellation refusal cannot stop physical work");
        }
        private static void Exhausted(Check c)
        {
            var factory = Factory(); var game = New(factory); Configure(c, game); Race(c, game);
            Until(game, () => Claim(game).Recovering, "first recovery");
            Until(game, () => Claim(game).PendingCommand > 0 && !Claim(game).Recovering && Claim(game).FirstPlan == 0, "replacement proposed");
            Order(c, game, 0, Build(42)); var twin = factory.Load(game.Save());
            for (int i = 0; i < 12; i++) Step(game, twin);
            c.That(Claim(game).RecoveryAttempts == 1 && Claim(game).Recovery.Blocker.Id == "controller.recovery-exhausted" &&
                !Claim(game).Recovering && Plans(game).Count == 2 && Plans(game).Last().State == PlanState.Pending,
                "explicit attempt cap retains second real blocked intent; does not steal last site");
            c.Equal(Fix.FromInt(20), Stock(game, 1), "exhausted recovery spends no materials or money");
        }
        private static void NoSites(Check c)
        {
            var factory = Factory(json => {
                var old = json.Get("plan_site_sets")[0].Get("sites"); var sites = JsonValue.NewArray();
                sites.Add(old[0].Clone()); sites.Add(old[1].Clone()); json.Get("plan_site_sets")[0].Set("sites", sites);
            }); var game = New(factory); Configure(c, game);
            Race(c, game); Order(c, game, 0, Build(42)); var twin = factory.Load(game.Save());
            Until(game, () => Claim(game).FirstPlan == 0 && Claim(game).RecoveryAttempts == 1 && Claim(game).Status == PlaybookStepStatus.Blocked,
                "all real sites exhausted after cancellation", twin);
            c.That(Claim(game).Sites.Count == 2 && Claim(game).Sites.All(s => !s.Ready) && Plans(game).Count == 1,
                "no viable alternate means typed wait without fabricated placement");
            c.Equal(Fix.FromInt(20), Stock(game, 1), "no-sites wait conserves all rival resources");
        }
        private static void Authoring(Check c)
        {
            foreach (string fault in new[] { "missing", "unknown", "type", "zero-delay", "maximum", "empty", "duplicate", "delay-missing" })
                c.Throws<ContentException>(() => New(Factory(json => {
                    var step = json.Get("plan_playbooks")[0].Get("steps")[0]; var policy = json.Get("pending_plan_recovery_policies")[0];
                    if (fault == "missing") step.Remove("recovery");
                    if (fault == "unknown") step.Set("recovery", JsonValue.Of("missing.policy"));
                    if (fault == "type") step.Set("recovery", JsonValue.Number("1"));
                    if (fault == "zero-delay") policy.Get("delay").Set("numerator", JsonValue.Number("0"));
                    if (fault == "maximum") policy.Set("maximum_recoveries", JsonValue.Number("0"));
                    if (fault == "empty") policy.Set("blockers", JsonValue.NewArray());
                    if (fault == "duplicate") policy.Get("blockers").Add(JsonValue.Of("core.blocked"));
                    if (fault == "delay-missing") policy.Remove("delay");
                })), "invalid explicit recovery authoring refuses: " + fault);
            var factory = Factory(json => json.Get("plan_playbooks")[0].Get("steps")[0].Set("recovery", JsonValue.Null()));
            var game = New(factory); Configure(c, game); Race(c, game, false);
            for (int i = 0; i < 8; i++) Step(game);
            c.That(Claim(game).RecoveryAttempts == 0 && !Claim(game).Recovery.Eligible && Plans(game).Single().State == PlanState.Pending,
                "explicit null disables automatic recovery without fallback");
        }
        private static void ProviderFault(Check c)
        {
            var factory = Factory(json => {
                json.Get("plan_playbooks")[0].Get("steps")[0].Set("recovery", JsonValue.Of("recovery.external"));
                json.Get("recovery_relays")[0].Set("mode", JsonValue.Of("throw"));
            });
            var game = New(factory); Configure(c, game);
            Order(c, game, 1, new SetControllerCommand { Controller = Controller.Autopilot });
            game.Seat(0).Submit(Build(30)); game.RunTicks(1);
            c.That(game.Fault != null && game.Fault.ToString().Contains("proof.recovery-fault"), "independent policy failure faults original update");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted recovery cannot checkpoint");
            c.Throws<InvalidOperationException>(() => game.Hash(), "faulted recovery cannot hash as valid");
        }
    }
}
