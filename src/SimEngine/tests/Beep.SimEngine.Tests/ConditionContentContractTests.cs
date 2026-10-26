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
    // Explicit authored files and normal player commands/views; no World or readiness/work mutations.
    internal static class ConditionContentContractTests
    {
        internal const string Case = "Condition content contracts: independent readiness drives private objectives, strategic sites and physical saved replay";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe init, out ConditionContractModule proof,
            Action<JsonValue, JsonValue, JsonValue> edit = null)
        {
            var sites = Json.Parse(FrameworkProofContent.Read("conditional-plan-sites.json"));
            // Explicit viable opening positions: an idle foreign machine must not occupy the loading destination.
            StartupContentAuthoring.Position(sites, "choice.rival.machine", JsonValue.Number("80"));
            var external = Json.Parse(FrameworkProofContent.Read("condition-contracts.json"));
            var objectives = Json.Parse(FrameworkProofContent.Read("condition-contract-objectives.json"));
            foreach (var index in new[] { 0, 1, 2 }) sites.Get("plan_site_sets")[0].Get("sites")[index].Set("conditions", Json.Parse("[\"contract.allowed\"]"));
            sites.Get("plan_playbooks")[0].Get("steps")[0].Set("conditions", Json.Parse("[\"contract.either\"]"));
            edit?.Invoke(sites, external, objectives); proof = new ConditionContractModule(init);
            return ConstructionPlanTests.Points(extraModules: new GameModule[] { new PlayerControllerProofModule(), new InventorySignalsModule(), proof, new ObjectiveModule() },
                extraDeclarations: "[{\"id\":\"sim.player-controllers\",\"version\":\"1.0.0\",\"documents\":[\"conditional-plan-sites.json\"]},{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.conditions\",\"version\":\"1.0.0\",\"documents\":[\"playbook-conditions.json\",\"condition-contracts.json\"]},{\"id\":\"sim.objectives\",\"version\":\"1.0.0\",\"documents\":[\"condition-contract-objectives.json\"]}]",
                extraDocuments: new Dictionary<string, string> {
                    ["conditional-plan-sites.json"] = sites.ToString(), ["playbook-conditions.json"] = FrameworkProofContent.Read("playbook-conditions.json"),
                    ["condition-contracts.json"] = external.ToString(), ["condition-contract-objectives.json"] = objectives.ToString() },
                scenarioEdit: scenario => {
                    foreach (var id in new[] { PlayerControllersModule.ModuleId, InventorySignalsModule.ModuleId, GameConditionsModule.ModuleId, ObjectiveModule.ModuleId }) scenario.Get("modules").Add(JsonValue.Of(id));
                    scenario.Get("settings").Set(PlayerControllersModule.ModuleId, Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"choice.controller\"},{\"company\":1,\"profile\":\"choice.controller\"}]}"));
                    scenario.Get("settings").Set(ObjectiveModule.ModuleId, Json.Parse("{\"companies\":[{\"company\":0,\"objectives\":[\"contract.built\"]},{\"company\":1,\"objectives\":[\"contract.built\"]}]}"));
                }, startupProfiles: new[] { "conditional-plan-sites.start" },
                worldEdit: world => StartupContentAuthoring.Position(world, "machine", JsonValue.Number("4")));
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static GameConditionsView Conditions(SimGame game, int company = 0) => game.Seat(company).View.Feature<GameConditionsView>();
        private static List<PlanView> Plans(SimGame game)
        { var rows = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(rows); return rows; }
        private static PlaybookStepSnapshot Claim(SimGame game)
        { var rows = new List<PlaybookStepSnapshot>(); game.Seat(0).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(0), rows); return rows.Single(); }
        private static ObjectiveSnapshot Objective(SimGame game, int company = 0)
        { var rows = new List<ObjectiveSnapshot>(); game.Seat(company).View.Feature<ObjectiveView>().CopyObjectives(rows); return rows.Single(); }
        private static EntityView Entity(SimGame game, string def, int company = 0)
            => game.Seat(company).Snapshot.Entities.Single(e => e.Def == def && e.Owner == new PlayerId(company));
        private static Fix Stock(SimGame game, int company = 0)
        {
            Fix total = Fix.Zero;
            foreach (var entity in game.Seat(company).Snapshot.Entities.Where(e => e.Owner == new PlayerId(company)))
            {
                var facets = new List<Facet>(); game.Seat(company).View.Inspect(entity.Id, facets);
                foreach (var facet in facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply")) total += facet.Value;
            }
            return total;
        }
        private static bool Working(SimGame game)
        { var rows = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(rows); return rows.Any(o => o.NodesDone > 0 && o.CurrentProgress.WholeTicks > 0); }
        private static string Diagnostics(SimGame game)
        {
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            return " stock=" + Stock(game) + " machine=" + ((PointSnapshot)Entity(game, "machine").Spatial).Position + " jobs=" +
                string.Join(";", jobs.Select(j => j.Id + "/" + j.IsHaul + "/" + j.State + "/" + j.Blocker + "/worker=" + j.Worker + "/from=" + j.Source + "/to=" + j.Destination + "/load=" + j.Loaded));
        }
        private static void Order(Check c, SimGame game, int company, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(company).Preview(command), "original public command preview");
            c.Equal(hash, game.Hash(), "public preview is pure"); c.That(TestKit.Order(game, company, command).Accepted, "original command executes");
        }
        private static void Risk(Check c, SimGame game, int company, bool enabled)
            => Order(c, game, company, new ConfigureOperationCommand { Entity = Entity(game, "strategy.beacon", company).Id, Operation = new Code("risk-dial"), Enabled = enabled, Target = Fix.Zero });
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin != null && (twin.Fault != null || twin.Hash() != game.Hash()))
                throw new InvalidOperationException("contract conditions step=" + game.Clock.StepIndex + "/fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            int steps = 0; while (!done() && steps++ < 1200) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " condition=" + Conditions(game).CheckCondition("contract.allowed") +
                " claim=" + Claim(game).Status + "/" + Claim(game).Blocker + " plans=" + string.Join(";", Plans(game).Select(p => p.Id + "/" + p.State + "/" + p.Blocker)) + Diagnostics(game));
        }
        private static void Run(Check c)
        {
            var init = new InitializeProbe(); var factory = Factory(init, out var proof); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current condition contract protocol");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current authored format, no new readiness/progress state");
            c.That(proof.IndependentAliases && proof.NativeLinks, "direct providers alias native composite and objective content");
            c.Equal(1, init.Initializations, "valid graph initializes once");
            var view = Conditions(game); ulong hash = game.Hash(); var retained = Claim(game);
            c.Equal(Codes.UnknownDef, view.CheckCondition("missing"), "unknown condition never substitutes another");
            c.Equal(Codes.UnknownDef, view.CheckCondition(null), "null condition selects no default");
            c.Equal("controller.risk-disabled", view.CheckCondition("contract.risk").Id, "public query reads independent condition alias");
            c.Equal("controller.risk-disabled", view.CheckCondition("contract.allowed").Id, "native all group reads independent child");
            c.Equal("controller.risk-disabled", view.CheckCondition("contract.either").Id, "native any retains stable actual refusal");
            c.Equal(Code.None, view.CheckCondition("contract.repeat"), "existing external call probe repeats independent ready child");
            c.Equal("controller.risk-disabled", view.CheckCondition("contract.alias").Id, "cross-view call shares independent child invocation");
            var context = proof.AliasProbe.Last;
            c.Throws<InvalidOperationException>(() => { var unused = context.View; }, "successful condition context expires");
            c.Equal("condition.plan-incomplete", view.CheckCondition("contract.completed").Id, "independent plan condition observes original unearned state");
            c.Equal("condition.plan-incomplete", game.Seat(0).View.Feature<ObjectiveView>().Observe("contract.built").Blocker.Id, "native objective shares same independent blocker");
            c.That(retained.SelectedSite == null && retained.Sites.All(s => !s.Ready), "native site policy refuses independent blocked conditions");
            c.Equal(hash, game.Hash(), "shared conditions/site/objective queries are pure");
            c.Equal(Fix.FromInt(52), Stock(game), "authored resources not granted by conditions");
            c.Throws<NotSupportedException>(() => ((IList<PlaybookSiteSnapshot>)retained.Sites).Clear(), "site diagnostics remain immutable");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2400); using var recorder = game.RecordReplay(limits);
            Risk(c, game, 1, true);
            c.Equal(Code.None, Conditions(game, 1).CheckCondition("contract.allowed"), "rival uses own actual policy through same contract");
            c.Equal("controller.risk-disabled", view.CheckCondition("contract.allowed").Id, "foreign policy never unlocks own site");
            Order(c, game, 0, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine").Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            Order(c, game, 0, new SetControllerCommand { Controller = Controller.Autopilot });
            c.Equal(0, Plans(game).Count, "blocked native strategy creates no synthetic plan");
            Risk(c, game, 0, true); var twin = factory.Load(game.Save());
            Until(game, twin, () => Claim(game).PendingCommand > 0, "native contract-gated strategy intent");
            c.That(Claim(game).PendingCommand > 0 && Plans(game).Count == 0, "original strategy queues intent without an instant physical project");
            c.Equal(Code.None, view.CheckCondition("contract.allowed"), "same independent gate becomes ready from actual operation command");
            twin = factory.Load(game.Save());
            Until(game, twin, () => Plans(game).Count > 0, "saved queued strategy intent");
            c.Equal(PlanState.Active, Plans(game).Single().State, "ordinary engine command admits one physical project");
            c.Equal("30", Plans(game).Single().Arguments.Get("placement").Get("position").Text, "authored site geometry remains engine-owned");
            Until(game, twin, () => Working(game), "machine travels and earns elapsed construction work");
            c.That(((PointSnapshot)Entity(game, "machine").Spatial).Position != Fix.FromInt(4), "actual machine moves from authored position");
            c.Equal(Fix.FromInt(44), Stock(game), "native hauling/work consumes eight physical inputs");
            c.Equal("condition.plan-incomplete", view.CheckCondition("contract.completed").Id, "elapsed partial work never pretends plan completed");
            c.That(Objective(game).State != ObjectiveState.Completed, "native objective cannot award partial build");
            twin = factory.Load(game.Save());
            Until(game, twin, () => Claim(game).Status == PlaybookStepStatus.Done && Objective(game).State == ObjectiveState.Completed, "native physical completion and objective settlement");
            c.Equal(1, game.Seat(0).Snapshot.Entities.Count(e => e.Def == "point.tower"), "one actual finished structure");
            c.Equal(Code.None, view.CheckCondition("contract.completed"), "independent readiness sees native earned plan completion");
            c.Equal("condition.plan-incomplete", Conditions(game, 1).CheckCondition("contract.completed").Id, "foreign company cannot borrow own completed plan");
            c.That(Objective(game, 1).State != ObjectiveState.Completed, "private objective lifecycle shares no foreign completion");
            c.Equal(Fix.FromInt(44), Stock(game), "condition/objective settlement gives no material refund/grant");
            c.Equal(Fix.FromInt(52), Stock(game, 1), "foreign inventory is untouched");
            c.That(retained.SelectedSite == null && retained.Sites.All(s => !s.Ready), "retained blocked decisions stay detached");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed mixed content restores same native chronology");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "complete policies/queued intent/physical work/private objective replay reaches final live hash");
            Console.WriteLine("condition content physical replay: initial=" + recorder.InitialStep + ";final=" + game.Tick
                + ";commands=" + recorder.CommandCount + ";checkpoints=" + recorder.CheckpointCount
                + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
            var isolated = New(factory);
            c.Equal("controller.risk-disabled", Conditions(isolated).CheckCondition("contract.allowed").Id, "interleaved session owns separate policy");
            c.Equal("condition.plan-incomplete", Conditions(isolated).CheckCondition("contract.completed").Id, "interleaved session retains no cached ready result");

            foreach (var invalid in new[] { "missing-child", "wrong-child", "self-cycle", "mixed-cycle", "duplicate-child", "missing-array", "empty-array", "missing-group-child", "wrong-objective", "missing-site", "wrong-step",
                "observation-stage", "observation-fault", "observation-missing-stage", "observation-missing-fault" })
            {
                var refused = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(refused, out _, (sites, external, objectives) => {
                    var forward = external.Get("forward_game_conditions")[0];
                    if (invalid == "missing-child" || invalid == "wrong-child") forward.Set("conditions", Json.Parse(invalid == "missing-child" ? "[\"missing\"]" : "[\"source\"]"));
                    if (invalid == "self-cycle" || invalid == "mixed-cycle") forward.Set("conditions", Json.Parse(invalid == "self-cycle" ? "[\"contract.risk\"]" : "[\"contract.allowed\"]"));
                    if (invalid == "duplicate-child") forward.Set("conditions", Json.Parse("[\"strategy.risk\",\"strategy.risk\"]"));
                    if (invalid == "missing-array") forward.Remove("conditions");
                    if (invalid == "empty-array") forward.Set("conditions", JsonValue.NewArray());
                    if (invalid == "missing-group-child") external.Get("composite_conditions")[0].Set("conditions", Json.Parse("[\"missing\"]"));
                    if (invalid == "wrong-objective") objectives.Get("condition_objective_predicates")[0].Set("condition", JsonValue.Of("source"));
                    if (invalid == "missing-site") sites.Get("plan_site_sets")[0].Get("sites")[0].Set("conditions", Json.Parse("[\"missing\"]"));
                    if (invalid == "wrong-step") sites.Get("plan_playbooks")[0].Get("steps")[0].Set("conditions", Json.Parse("[\"source\"]"));
                    if (invalid.StartsWith("observation", StringComparison.Ordinal))
                    {
                        if (invalid != "observation-missing-stage") forward.Set("observation_stage", JsonValue.Of(invalid == "observation-stage" ? "missing" : "check"));
                        if (invalid != "observation-missing-fault") forward.Set("observation_fault", JsonValue.Of(invalid == "observation-fault" ? "missing" : "pause-throw"));
                    }
                })), "contract condition refuses " + invalid);
                c.Equal(0, refused.Initializations, invalid + " refuses before Initialize");
            }
            foreach (var bad in new[] { "alias-loop", "bad-code" })
            {
                var faulted = New(Factory(new InitializeProbe(), out var faultProof, (sites, external, objectives) => {
                    var probe = external.Get("condition_call_probes")[0];
                    if (bad == "alias-loop") probe.Set("target", JsonValue.Of("contract.alias"));
                    else probe.Set("mode", JsonValue.Of("bad-code"));
                }));
                if (bad == "alias-loop") c.Throws<InvalidOperationException>(() => Conditions(faulted).CheckCondition("contract.alias"), "undeclared cross-view recursion retains original guard");
                else c.Throws<ArgumentException>(() => Conditions(faulted).CheckCondition("contract.alias"), "invalid external blocker never substitutes readiness");
                c.That(faulted.Fault != null, bad + " latches actual engine fault");
                c.Throws<InvalidOperationException>(() => { var unused = faultProof.AliasProbe.Last.View; }, bad + " invocation expires");
                c.Throws<InvalidOperationException>(() => faulted.Save(), bad + " cannot checkpoint faulty state");
                c.Throws<InvalidOperationException>(() => faulted.Hash(), bad + " cannot present a healthy hash");
            }
            ObservationFaults(c);
        }
        private static GameFactory ProbeFactory(InitializeProbe init, string stage, string fault)
            => Factory(init, out _, (sites, external, objectives) => {
                var forward = external.Get("forward_game_conditions")[0];
                forward.Set("observation_stage", JsonValue.Of(stage)); forward.Set("observation_fault", JsonValue.Of(fault));
            });
        private static void ObservationFaults(Check c)
        {
            foreach (string stage in new[] { "dependencies", "enumeration", "binding", "check" })
            {
                var factory = ProbeFactory(new InitializeProbe(), stage, "none"); var game = New(factory);
                ulong hash = game.Hash();
                c.Equal("controller.risk-disabled", Conditions(game).CheckCondition("contract.risk").Id, stage + " healthy probe delegates actual readiness");
                c.Equal(hash, game.Hash(), stage + " healthy graph/binding/check observation is pure");
                c.Equal(hash, factory.Load(game.Save()).Hash(), stage + " healthy native condition state restores");
            }
            foreach (string stage in new[] { "dependencies", "enumeration", "binding" })
            foreach (string fault in new[] { "throw", "pause", "pause-throw" })
            {
                var init = new InitializeProbe(); var factory = ProbeFactory(init, stage, fault); Exception caught = null;
                try { New(factory); } catch (Exception error) { caught = error; }
                c.That(caught != null, stage + "/" + fault + " cannot create a healthy world from impure condition metadata");
                c.Equal(0, init.Initializations, stage + "/" + fault + " refuses before selected Initialize");
                if (fault != "pause") c.That(caught?.ToString().Contains("proof.condition-observation." + stage) == true,
                    stage + "/" + fault + " original callback error survives startup verification");
            }
            var isolated = New(Factory(new InitializeProbe(), out _)); ulong untouched = isolated.Hash();
            foreach (string fault in new[] { "throw", "pause", "pause-throw" })
            foreach (bool restored in new[] { false, true })
            {
                var factory = ProbeFactory(new InitializeProbe(), "check", fault); var game = New(factory);
                if (restored) game = factory.Load(game.Save());
                var condition = game.World.Content.Get<ForwardGameConditionDef>("contract.risk"); var view = Conditions(game); Exception caught = null;
                try { view.CheckCondition("contract.risk"); } catch (Exception error) { caught = error; }
                c.That(caught != null && game.Fault != null, fault + " actual private condition query rejects provider mutation/error");
                c.Equal("condition.observation", game.Fault?.Phase, fault + " one condition owner reports first callback/verification failure");
                if (fault != "pause") c.That(caught?.ToString().Contains("proof.condition-observation.check") == true
                    && game.Fault?.Message.Contains("proof.condition-observation.check") == true, fault + " first runtime callback cause remains original");
                c.Equal(1, condition.ObservationCalls, fault + " failing readiness callback runs once");
                c.Throws<InvalidOperationException>(() => { var unused = condition.LastObservedContext.View; }, fault + " failed context expires");
                c.Throws<InvalidOperationException>(() => condition.LastObservedContext.Check(condition), fault + " expired context cannot run another condition");
                var first = game.Fault; long at = game.Tick;
                foreach (string id in new[] { "contract.risk", "missing", null })
                    c.Throws<InvalidOperationException>(() => view.CheckCondition(id), fault + " retained query stops before healthy shortcuts");
                c.Throws<InvalidOperationException>(() => game.Save(), fault + " terminal condition cannot checkpoint");
                c.Throws<InvalidOperationException>(() => game.Hash(), fault + " terminal condition cannot publish healthy hash");
                c.Equal(0, game.Update(), fault + " terminal condition cannot advance");
                c.That(ReferenceEquals(first, game.Fault) && game.Tick == at && condition.ObservationCalls == 1,
                    fault + " first fault and sole clock remain authoritative");
                c.Equal(untouched, isolated.Hash(), fault + " failed condition leaves an interleaved session unchanged");
            }
        }
    }
}
