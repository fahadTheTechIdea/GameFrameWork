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
    internal static class ConditionalPlanSiteTests
    {
        internal const string Case = "Player controllers: private conditional sites and physical strategic selection";
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(string document = null, string conditions = null, string conditionsDocument = "playbook-conditions.json", GameModule conditionModule = null)
            => ConstructionPlanTests.Points(
                extraModules: new GameModule[] { new PlayerControllerProofModule(), new InventorySignalsModule(), conditionModule ?? new GameConditionProofModule() },
                extraDeclarations: "[{\"id\":\"sim.player-controllers\",\"version\":\"1.0.0\",\"documents\":[\"conditional-plan-sites.json\"]},{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.conditions\",\"version\":\"1.0.0\",\"documents\":[\"playbook-conditions.json\"]}]".Replace("playbook-conditions.json", conditionsDocument),
                extraDocuments: new Dictionary<string, string> {
                    ["conditional-plan-sites.json"] = document ?? FrameworkProofContent.Read("conditional-plan-sites.json"),
                    [conditionsDocument] = conditions ?? FrameworkProofContent.Read(conditionsDocument) },
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(PlayerControllersModule.ModuleId));
                    scenario.Get("modules").Add(JsonValue.Of(InventorySignalsModule.ModuleId));
                    scenario.Get("modules").Add(JsonValue.Of(GameConditionsModule.ModuleId));
                    scenario.Get("settings").Set(PlayerControllersModule.ModuleId, Content.Json.Parse(
                        "{\"companies\":[{\"company\":0,\"profile\":\"choice.controller\"},{\"company\":1,\"profile\":\"choice.controller\"}]}"));
                }, startupProfiles: new[] { "conditional-plan-sites.start" });
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static List<PlanView> Plans(SimGame game, int company)
        { var values = new List<PlanView>(); game.Seat(company).View.Feature<WorkView>().CopyPlans(values); return values; }
        private static PlaybookStepSnapshot Claim(SimGame game, int company)
        {
            var values = new List<PlaybookStepSnapshot>();
            if (!game.Seat(company).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(company), values))
                throw new InvalidOperationException("missing private conditional-site diagnostics");
            return values.Single();
        }
        private static Fix Stock(SimGame game, int company)
        {
            Fix sum = Fix.Zero; var resource = game.World.Content.ResourceId("supply");
            foreach (var entity in game.World.Entities.All)
                if (entity.Alive && entity.Owner == new PlayerId(company) && entity.Get<IInletPort>() is IInletPort inventory)
                    sum += inventory.Amount(resource);
            return sum;
        }
        private static CommandResult Order(Check c, SimGame game, int company, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(company).Preview(command), "ordinary current admission");
            c.Equal(hash, game.Hash(), "private preview mutates no rule/resources/time");
            var answer = TestKit.Order(game, company, command); c.That(answer.Accepted, "ordinary answer: " + answer.Refusal);
            if (!answer.Accepted) throw new InvalidOperationException("unexpected command refusal " + answer.Refusal);
            return answer;
        }
        private static void Configure(Check c, SimGame game, int company)
        {
            var worker = game.Seat(company).Snapshot.Entities.Single(e => e.Def == "machine" && e.Owner == new PlayerId(company)).Id;
            Order(c, game, company, new ConfigureWorkPolicyCommand { Worker = worker,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") },
                    null, true, true, WorkAssignmentMode.All) });
        }
        private static void Risk(Check c, SimGame game, int company, bool enabled)
        {
            var beacon = game.Seat(company).Snapshot.Entities.Single(e => e.Def == "strategy.beacon" && e.Owner == new PlayerId(company)).Id;
            Order(c, game, company, new ConfigureOperationCommand { Entity = beacon, Operation = new Code("risk-dial"), Enabled = enabled, Target = Fix.Zero });
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.RunTicks(1); twin?.RunTicks(1);
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("conditional-site update/time=" + game.Tick + "/fault=" + game.Fault);
        }
        private static void Until(SimGame game, int company, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + "/time=" + game.Tick + "/plans=" +
                string.Join(";", Plans(game, company).Select(p => p.Id + "/" + p.State + "/" + p.Blocker)) + "/sites=" +
                string.Join(";", Claim(game, company).Sites.Select(s => s.Site.Id + "/" + s.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "conditional-site engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no extra persisted site-policy state");
            Physical(c); Blocked(c); Authoring(c);
        }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory);
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IBusinessAccounts>(), "strategy needs resources, not business/money");
            var retained = Claim(game, 0); ulong hash = game.Hash();
            c.That(retained.Sites.Select(s => s.Site.Id).SequenceEqual(new[] { "primary", "a.alternate", "b.alternate" }),
                "candidate priority then stable ID ignores authored row order");
            var primary = retained.Sites[0];
            c.That(!primary.Ready && primary.Admission.Ready && primary.Blocker.Id == "controller.risk-disabled",
                "policy refusal is distinct from physically ready normal admission");
            c.Equal("a.alternate", retained.SelectedSite.Id, "blocked high-priority site does not mask ready tied alternative");
            c.Throws<NotSupportedException>(() => ((IList<PlaybookSiteSnapshot>)retained.Sites).Clear(), "retained candidates immutable");
            c.Throws<InvalidOperationException>(() => retained.SelectedSite.Anchor.Set("position", JsonValue.Number("88")), "retained geometry immutable");
            c.Equal(hash, game.Hash(), "condition/site/private diagnostics pure");
            c.That(game.World.Content.Get<AuthoredPlanSitesDef>("choice.sites").AuthoringPath.Contains("conditional-plan-sites.json"),
                "actual master-selected site conditions are file content");
            Configure(c, game, 0); Configure(c, game, 1); Risk(c, game, 1, true);
            c.Equal("a.alternate", Claim(game, 0).SelectedSite.Id, "foreign enabled signal does not unlock own risky site");
            c.Equal("primary", Claim(game, 1).SelectedSite.Id, "same profile uses rival own private condition");
            c.That(retained.Sites[0].Blocker.Id == "controller.risk-disabled", "retained policy evidence is detached");
            var denied = new List<PlaybookStepSnapshot> { retained };
            c.That(!game.Seat(0).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(1), denied) && denied.Count == 0,
                "other company's strategy choices not leaked");
            using (var recorder = game.RecordReplay(Limits))
            {
                Order(c, game, 0, new SetControllerCommand { Controller = Controller.Autopilot });
                c.That(Claim(game, 0).PendingCommand > 0 && Plans(game, 0).Count == 0, "one normal next-update intent, no instant construction");
                var twin = factory.Load(game.Save()); Step(game, twin);
                c.That(Plans(game, 0).Single().Arguments.Get("placement").Get("position").Text == "24" &&
                    Plans(game, 0).Single().State == PlanState.Active, "actual physical alternative from fresh company policy");
                c.Equal(Fix.FromInt(52), Stock(game, 0), "intent/start has not granted or instantly consumed construction inputs");
                twin = factory.Load(game.Save());
                Until(game, 0, () => Claim(game, 0).Status == PlaybookStepStatus.Done, "own alternative physical construction", twin);
                c.Equal(Fix.FromInt(44), Stock(game, 0), "own hauling/build physically consumes eight units");
                Order(c, game, 1, new SetControllerCommand { Controller = Controller.Autopilot });
                twin = factory.Load(game.Save()); Step(game, twin);
                c.Equal("30", Plans(game, 1).Single().Arguments.Get("placement").Get("position").Text, "rival takes separately viable high-priority site");
                twin = factory.Load(game.Save());
                Until(game, 1, () => Claim(game, 1).Status == PlaybookStepStatus.Done, "rival private strategic construction", twin);
                c.Equal(Fix.FromInt(44), Stock(game, 1), "rival pays same physical material costs");
                c.That(game.Seat(0).Snapshot.Entities.Count(e => e.Def == "point.tower") == 2, "only two normal completed objects");
                var result = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(result.Success && result.Game.Hash() == game.Hash(), "canonical replay reproduces private choices and physical resources");
            }
        }
        private static void Blocked(Check c)
        {
            var json = Content.Json.Parse(FrameworkProofContent.Read("conditional-plan-sites.json"));
            var sites = json.Get("plan_site_sets")[0].Get("sites");
            for (int i = 0; i < sites.Count; i++)
                sites[i].Set("conditions", Content.Json.Parse("[\"strategy.risk\",\"strategy.wait\"]"));
            var factory = Factory(json.ToString()); var game = New(factory);
            Configure(c, game, 0); Risk(c, game, 1, true);
            Order(c, game, 0, new SetControllerCommand { Controller = Controller.Autopilot });
            var fact = Claim(game, 0);
            c.That(fact.Status == PlaybookStepStatus.Blocked && fact.SelectedSite == null &&
                fact.Sites.All(s => !s.Ready && s.Blocker.Id == "controller.risk-disabled"), "all gated alternatives preserve actual blockers with no fallback");
            var twin = factory.Load(game.Save()); for (int i = 0; i < 8; i++) Step(game, twin);
            c.That(Plans(game, 0).Count == 0 && Stock(game, 0) == Fix.FromInt(52), "saved native wait has no plans, free work or consumed stock");
            Risk(c, game, 0, true); fact = Claim(game, 0);
            c.That(fact.Status == PlaybookStepStatus.CommandPending && Plans(game, 0).Count == 0,
                "ordinary own signal command unlocks fresh engine next-update selection");
            twin = factory.Load(game.Save()); Step(game, twin);
            Until(game, 0, () => Claim(game, 0).Status == PlaybookStepStatus.Done, "signal-unlocked physical construction", twin);
            c.Equal(Fix.FromInt(44), Stock(game, 0), "unlocked work still physically consumes original stock");
            c.That(fact.Status != PlaybookStepStatus.Done, "held in-flight facts do not become completed");
        }
        private static void Authoring(Check c)
        {
            foreach (string fault in new[] { "missing", "null", "wrong-type", "unknown", "wrong-provider", "duplicate", "empty-id" })
            {
                var json = Content.Json.Parse(FrameworkProofContent.Read("conditional-plan-sites.json"));
                var site = json.Get("plan_site_sets")[0].Get("sites")[0];
                if (fault == "missing") site.Remove("conditions");
                if (fault == "null") site.Set("conditions", JsonValue.Null());
                if (fault == "wrong-type") site.Set("conditions", JsonValue.Of("strategy.risk"));
                if (fault == "unknown") site.Set("conditions", Content.Json.Parse("[\"missing\"]"));
                if (fault == "wrong-provider") site.Set("conditions", Content.Json.Parse("[\"choice.single\"]"));
                if (fault == "duplicate") site.Set("conditions", Content.Json.Parse("[\"strategy.risk\",\"strategy.risk\"]"));
                if (fault == "empty-id") site.Set("conditions", Content.Json.Parse("[\"\"]"));
                c.Throws<ContentException>(() => New(Factory(json.ToString())), "invalid candidate conditions fail before gameplay: " + fault);
            }
            c.Throws<ArgumentException>(() => new PlanSiteCandidate("valid", 1, JsonValue.NewObject(), new Code("!bad")), "invalid public candidate blocker refuses");
            var jsonReserve = Content.Json.Parse(FrameworkProofContent.Read("playbook-conditions.json"));
            jsonReserve.Get("signal_conditions")[1].Get("conditions")[0].Set("minimum", JsonValue.Number("33"));
            var waiting = New(Factory(conditions: jsonReserve.ToString()));
            c.That(Claim(waiting, 0).Sites.All(s => s.Blocker.Id == "controller.reserve") && Claim(waiting, 0).SelectedSite == null,
                "authored reserve profile gates every site without money dependency");
            c.Equal(waiting.Hash(), Factory(conditions: jsonReserve.ToString()).Load(waiting.Save()).Hash(), "resource-reserve wait saves through existing owners");
        }
    }
}
