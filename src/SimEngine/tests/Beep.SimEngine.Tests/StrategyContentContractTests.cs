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
    // Normal authored files/player actions only; no scheduler, World or raw inventory/progress writes.
    internal static class StrategyContentContractTests
    {
        internal const string Case = "Strategy content contracts: independent profiles, templates, sites and recovery use physical rival play and saved replay";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe init, out StrategyContractModule proof,
            Action<JsonValue, JsonValue> edit = null)
        {
            var native = Json.Parse(FrameworkProofContent.Read("playbook-recovery.json"));
            var external = Json.Parse(FrameworkProofContent.Read("strategy-contracts.json"));
            native.Remove("plan_templates");
            var step = native.Get("plan_playbooks")[0].Get("steps")[0];
            step.Set("sites", JsonValue.Of("contract.sites")); step.Set("recovery", JsonValue.Of("recovery.external"));
            // Separate viable physical routes, not an overlap/instant-transfer fallback.
            StartupContentAuthoring.Position(native, "rival.source", JsonValue.Number("60"));
            StartupContentAuthoring.Position(native, "rival.machine", JsonValue.Number("60"));
            edit?.Invoke(native, external); proof = new StrategyContractModule(init);
            return ConstructionPlanTests.Points(additionalModule: proof,
                moduleDeclaration: "{\"id\":\"sim.player-controllers\",\"version\":\"1.0.0\",\"documents\":[\"playbook-recovery.json\",\"strategy-contracts.json\"]}",
                moduleDocument: native.ToString(), extraDocuments: new Dictionary<string, string> { ["strategy-contracts.json"] = external.ToString() },
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(PlayerControllersModule.ModuleId));
                    scenario.Get("settings").Set(PlayerControllersModule.ModuleId, Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"contract.controller\"},{\"company\":1,\"profile\":\"contract.controller\"}]}"));
                }, startupProfiles: new[] { "playbook-recovery.start" },
                worldEdit: world => StartupContentAuthoring.Position(world, "machine", JsonValue.Number("4")));
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static List<PlanView> Plans(SimGame game, int company = 1)
        { var rows = new List<PlanView>(); game.Seat(company).View.Feature<WorkView>().CopyPlans(rows); return rows; }
        private static PlaybookStepSnapshot Claim(SimGame game, int company = 1)
        { var rows = new List<PlaybookStepSnapshot>(); game.Seat(company).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(company), rows); return rows.Single(); }
        private static PlayerControllerFacts ControllerFacts(SimGame game, int company = 1)
        { var rows = new List<PlayerControllerFacts>(); game.Seat(company).View.Feature<PlayerControllersView>().CopyControllers(rows); return rows.Single(); }
        private static EntityView Entity(SimGame game, string def, int company)
            => game.Seat(company).Snapshot.Entities.Single(e => e.Def == def && e.Owner == new PlayerId(company));
        private static Fix Stock(SimGame game, int company)
        {
            Fix total = Fix.Zero;
            foreach (var entity in game.Seat(company).Snapshot.Entities.Where(e => e.Owner == new PlayerId(company)))
            {
                var facets = new List<Facet>(); game.Seat(company).View.Inspect(entity.Id, facets);
                foreach (var facet in facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply")) total += facet.Value;
            }
            return total;
        }
        private static void Order(Check c, SimGame game, int company, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(company).Preview(command), "original command preview");
            c.Equal(hash, game.Hash(), "preview stays pure"); c.That(TestKit.Order(game, company, command).Accepted, "original command accepted");
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin != null && (twin.Fault != null || twin.Hash() != game.Hash()))
                throw new InvalidOperationException("strategy contracts step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            int steps = 0; while (!done() && steps++ < 1200) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " claim=" + Claim(game).Status + "/" + Claim(game).Blocker +
                " stock=" + Stock(game, 0) + "/" + Stock(game, 1) + " plans=" + string.Join(";", Plans(game).Select(p => p.Id + "/" + p.State + "/" + p.Blocker)));
        }
        private static void Run(Check c)
        {
            var init = new InitializeProbe(); var factory = Factory(init, out var proof); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current strategy contract protocol");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current controller/plan saved layout");
            c.That(proof.IndependentAliases && proof.NativeLinks, "direct profile/template/site/recovery providers alias native execution inputs");
            c.Equal(1, init.Initializations, "valid strategic composition initializes once");
            var dormant = ControllerFacts(game);
            c.That(dormant.Profile == "contract.controller" && dormant.Selection == Controller.Human && dormant.Decisions == 0, "independent authored profile is dormant, not implicit AI");
            c.Equal("contract.controller", ControllerFacts(game, 0).Profile, "same immutable profile binds a separate ordinary company");
            ulong hash = game.Hash(); var template = game.Seat(1).View.Feature<WorkView>().AuthoredPlanTemplate("recovery.tower");
            c.That(template != null && template.Entries.Count == 1 && template.Entries[0].Definition == "build:point.tower", "private template view accepts independent catalog contract");
            c.That(game.Seat(1).View.Feature<WorkView>().AuthoredPlanTemplate("missing") == null, "unknown authored template does not fabricate a design");
            c.Throws<NotSupportedException>(() => ((IList<PlanTemplateEntry>)template.Entries).Clear(), "authored template value is immutable");
            c.Throws<InvalidOperationException>(() => template.Entries[0].Arguments.Get("placement").Set("offset", JsonValue.Number("10")), "nested template intent is frozen");
            c.Equal(hash, game.Hash(), "template/private strategy observations are pure");
            var hidden = new List<PlaybookStepSnapshot>();
            c.That(!game.Seat(0).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(1), hidden) && hidden.Count == 0, "foreign company cannot read rival strategy state");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2400); using var recorder = game.RecordReplay(limits);
            for (int company = 0; company < 2; company++) Order(c, game, company, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine", company).Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            Order(c, game, 1, new SetControllerCommand { Controller = Controller.Autopilot });
            c.That(Claim(game).PendingCommand > 0 && Plans(game).Count == 0, "independent profile factory queues native next-update placement only");
            var started = ControllerFacts(game);
            c.Equal(started.LastDecision + 2, started.NextDecision, "external authored cadence controls original scheduler");
            Order(c, game, 0, new ConstructCommand { Def = "point.tower", Placement = Json.Parse("{\"space\":\"field.points\",\"position\":30}"), Priority = 70 });
            var twin = factory.Load(game.Save());
            Until(game, twin, () => Plans(game).Count == 1 && Claim(game).BlockedSince >= 0, "actual raced site and recovery observation");
            long old = Plans(game).Single().Id;
            c.That(Plans(game).Single().State == PlanState.Pending && Plans(game).Single().Target == EntityId.None && Plans(game).Single().Blocker == Codes.Blocked,
                "native site race leaves real pending intent, no ghost project");
            c.Equal(Fix.FromInt(20), Stock(game, 1), "blocked rival spends no materials");
            var retained = Claim(game);
            c.That(retained.Recovery.Eligible && !retained.Recovery.Ready, "direct recovery policy preserves actual elapsed delay");
            game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long step = game.Clock.StepIndex;
            game.Update(); twin.Update();
            c.That(game.Clock.StepIndex == step && game.Hash() == hash && twin.Hash() == hash, "paused one clock earns no recovery/build time");
            game.Clock.Resume(); twin.Clock.Resume();
            Until(game, twin, () => Claim(game).Recovering, "native policy delay and pending-only cancellation");
            c.That(Claim(game).RecoveryAttempts == 1 && Plans(game).Single().State == PlanState.Pending, "normal cancellation queues before changing actual plan");
            twin = factory.Load(game.Save());
            Until(game, twin, () => Claim(game).FirstPlan > old, "saved cancellation answer and native alternate intent");
            c.Equal(PlanState.Cancelled, Plans(game).Single(p => p.Id == old).State, "old plan is cancelled, never relocated");
            var replacement = Plans(game).Single(p => p.Id == Claim(game).FirstPlan);
            c.That(replacement.Arguments.Get("placement").Get("position").Text == "42" && replacement.Group == "recovery.claim.recovery.1", "independent site source feeds original alternative ranking/group history");
            c.Equal(Fix.FromInt(20), Stock(game, 1), "replacement gives no resources or refund");
            twin = factory.Load(game.Save());
            Until(game, twin, () => Claim(game).Status == PlaybookStepStatus.Done && game.Seat(0).Snapshot.Entities.Count(e => e.Def == "point.tower") == 2, "two separate physical company constructions");
            c.Equal(Fix.FromInt(12), Stock(game, 0), "player build consumes eight actual inputs");
            c.Equal(Fix.FromInt(12), Stock(game, 1), "rival build consumes the same eight actual inputs");
            c.That(((PointSnapshot)Entity(game, "machine", 1).Spatial).Position != Fix.FromInt(60), "rival machine physically travels from its source");
            c.Equal(0L, ControllerFacts(game, 0).Decisions, "same profile factory never shares rival decisions into human company");
            c.That(retained.RecoveryAttempts == 0 && !retained.Recovering, "retained private recovery facts stay detached");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "contract profile/native provider state and completed plans load identically");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "whole physical rival recovery/normal cancellation replays to final live hash");
            Console.WriteLine("strategy physical replay: initial=" + recorder.InitialStep + ";final=" + game.Tick + ";commands=" + recorder.CommandCount
                + ";checkpoints=" + recorder.CheckpointCount + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
            var isolated = New(factory);
            c.That(ControllerFacts(isolated).Decisions == 0 && Claim(isolated).RecoveryAttempts == 0 && Plans(isolated).Count == 0, "new interleaved session owns fresh native controller state");

            foreach (var invalid in new[] { "missing-profile", "wrong-profile", "self-cycle", "mixed-cycle", "zero-cadence", "zero-budget", "null-factory", "missing-template", "wrong-template", "bad-template-entry", "template-cycle", "missing-site", "wrong-site", "missing-recovery", "wrong-recovery" })
            {
                var refused = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(refused, out _, (native, external) => {
                    var profile = external.Get("independent_controller_profiles")[0];
                    var stepDef = native.Get("plan_playbooks")[0].Get("steps")[0];
                    if (invalid == "missing-profile" || invalid == "wrong-profile" || invalid == "self-cycle") profile.Set("profile", JsonValue.Of(invalid == "missing-profile" ? "missing" : invalid == "wrong-profile" ? "source" : "contract.controller"));
                    if (invalid == "mixed-cycle") {
                        var second = profile.Clone(); second.Set("id", JsonValue.Of("contract.second")); second.Set("profile", JsonValue.Of("contract.controller"));
                        profile.Set("profile", JsonValue.Of("contract.second")); external.Get("independent_controller_profiles").Add(second);
                    }
                    if (invalid == "zero-cadence") profile.Get("cadence").Set("numerator", JsonValue.Number("0"));
                    if (invalid == "zero-budget") profile.Set("commands_per_decision", JsonValue.Number("0"));
                    if (invalid == "null-factory") profile.Set("factory", JsonValue.Of("null"));
                    if (invalid == "missing-template" || invalid == "wrong-template") stepDef.Set("template", JsonValue.Of(invalid == "missing-template" ? "missing" : "source"));
                    if (invalid == "bad-template-entry") external.Get("independent_plan_templates")[0].Get("entries")[0].Set("definition", JsonValue.Of("build:missing"));
                    if (invalid == "template-cycle") external.Get("independent_plan_templates")[0].Get("entries")[0].Set("after", Json.Parse("[\"tower\"]"));
                    if (invalid == "missing-site" || invalid == "wrong-site") stepDef.Set("sites", JsonValue.Of(invalid == "missing-site" ? "missing" : "source"));
                    if (invalid == "missing-recovery" || invalid == "wrong-recovery") stepDef.Set("recovery", JsonValue.Of(invalid == "missing-recovery" ? "missing" : "source"));
                })), "strategic content refuses " + invalid);
                c.Equal(0, refused.Initializations, invalid + " refuses before Initialize");
            }
            ObservationFaults(c);
        }
        private static GameFactory ProbeFactory(InitializeProbe init, string stage, string fault, bool unused, out StrategyContractModule proof)
            => Factory(init, out proof, (native, external) => {
                var rows = external.Get("independent_controller_profiles"); var profile = rows[0];
                if (unused) { profile = profile.Clone(); profile.Set("id", JsonValue.Of("contract.unused")); rows.Add(profile); }
                profile.Set("observation_stage", JsonValue.Of(stage)); profile.Set("observation_fault", JsonValue.Of(fault));
            });
        private static void ObservationFaults(Check c)
        {
            foreach (bool unused in new[] { false, true })
            foreach (string stage in new[] { "cadence", "budget", "dependencies", "enumeration", "factory", "binding" })
            {
                if (unused && (stage == "factory" || stage == "binding")) continue;
                var healthyInit = new InitializeProbe(); var healthyFactory = ProbeFactory(healthyInit, stage, "none", unused, out _);
                var healthy = New(healthyFactory); ulong hash = healthy.Hash();
                var profile = healthy.World.Content.Get<IndependentControllerProfileDef>(unused ? "contract.unused" : "contract.controller");
                c.That(healthyInit.Initializations == 1 && profile.ObservationCalls > 0 && healthy.Fault == null, stage + " healthy selected/unselected metadata remains observable");
                c.Equal(hash, healthyFactory.Load(healthy.Save()).Hash(), stage + " pure metadata preserves ordinary saved controller state");
                foreach (string fault in stage == "cadence" ? new[] { "throw", "pause", "pause-throw", "overflow" } : new[] { "throw", "pause", "pause-throw" })
                {
                    var init = new InitializeProbe(); var factory = ProbeFactory(init, stage, fault, unused, out var proof); Exception caught = null;
                    try { New(factory); } catch (Exception error) { caught = error; }
                    c.That(caught != null, stage + "/" + fault + " selected/unselected impure metadata cannot create a healthy session");
                    c.Equal(0, init.Initializations, stage + "/" + fault + " fails before any selected Initialize");
                    c.Equal("observation.provider", proof.ObservationFaultPhase, stage + "/" + fault + " original provider observation owns startup fault");
                    if (fault != "pause") c.That((fault == "overflow" ? caught is OverflowException : caught is ApplicationException)
                        && caught.Message.Contains("proof.controller-observation." + stage),
                        stage + "/" + fault + " startup retains first callback error");
                }
            }
            var isolated = New(Factory(new InitializeProbe(), out _)); ulong untouched = isolated.Hash();
            foreach (bool restored in new[] { false, true })
            foreach (string fault in new[] { "throw", "pause", "pause-throw" })
            {
                var factory = ProbeFactory(new InitializeProbe(), "budget", "none", false, out _); var game = New(factory);
                if (restored) game = factory.Load(game.Save());
                var profile = game.World.Content.Get<IndependentControllerProfileDef>("contract.controller"); profile.ArmObservationFault(fault);
                game.Seat(1).Submit(new SetControllerCommand { Controller = Controller.Autopilot }); long at = game.Tick;
                c.Equal(0, game.Update(), fault + " actual first decision cannot complete with an impure command budget");
                c.That(game.Fault?.Phase == "observation.provider" && game.Tick == at && profile.ObservationCalls == 1,
                    fault + " fresh/restored budget callback faults once through original clock/observation");
                if (fault != "pause") c.That(game.Fault?.Message.Contains("proof.controller-observation.budget") == true, fault + " original decision metadata cause survives");
                if (game.Fault == null) continue; // Record real baseline failure; never fabricate a terminal context.
                c.Throws<InvalidOperationException>(() => game.Save(), fault + " failed decision budget cannot save");
                c.Throws<InvalidOperationException>(() => game.Hash(), fault + " failed decision budget cannot hash as healthy");
                var first = game.Fault; c.Equal(0, game.Update(), fault + " failed budget cannot resume");
                c.That(ReferenceEquals(first, game.Fault) && game.Tick == at && profile.ObservationCalls == 1, fault + " first failure remains authoritative");
                c.Equal(untouched, isolated.Hash(), fault + " budget fault leaves an independent session unchanged");
            }
        }
    }
}
