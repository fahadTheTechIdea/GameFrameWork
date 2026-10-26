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
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class PlaybookSiteTests
    {
        public static void Register()
            => TestRunner.Add("Player controllers: private site selection, ordinary rival competition and physical expansion", Run);
        private static GameFactory Factory(string document = null)
            => ConstructionPlanTests.Points(additionalModule: new PlayerControllerProofModule(),
                moduleDeclaration: "{\"id\":\"sim.player-controllers\",\"version\":\"1.0.0\",\"documents\":[\"playbook-sites.json\"]}",
                moduleDocument: document ?? FrameworkProofContent.Read("playbook-sites.json"),
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(PlayerControllersModule.ModuleId));
                    scenario.Get("settings").Set(PlayerControllersModule.ModuleId, Content.Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"expansion.player\"},{\"company\":1,\"profile\":\"expansion.rival\"}]}"));
                }, startupProfiles: new[] { "playbook-sites.start" });
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static WorkView Work(SimGame game, int company = 0) => game.Seat(company).View.Feature<WorkView>();
        private static List<PlaybookStepSnapshot> Steps(SimGame game, int company = 0)
        {
            var steps = new List<PlaybookStepSnapshot>();
            if (!game.Seat(company).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(company), steps)) throw new InvalidOperationException("missing site diagnostics");
            return steps;
        }
        private static PlaybookStepSnapshot Claim(SimGame game, int company = 0) => Steps(game, company).Single(s => s.Id == "claim");
        private static List<PlanView> Plans(SimGame game, int company = 0)
        { var values = new List<PlanView>(); Work(game, company).CopyPlans(values); return values; }
        private static PlayerControllerFacts Facts(SimGame game, int company = 0)
        { var values = new List<PlayerControllerFacts>(); game.Seat(company).View.Feature<PlayerControllersView>().CopyControllers(values); return values.Single(); }
        private static Fix Stock(SimGame game, int company)
        {
            Fix stock = Fix.Zero; ResourceId supply = game.World.Content.ResourceId("supply");
            foreach (Entity entity in game.World.Entities.All)
                if (entity.Alive && entity.Owner == new PlayerId(company) && entity.Get<IInletPort>() is IInletPort inlet) stock += inlet.Amount(supply);
            return stock;
        }
        private static void Configure(Check c, SimGame game, int company)
        {
            Entity worker = game.World.Entities.All.Single(e => e.Alive && e.Owner == new PlayerId(company) && e.Def.Id == "machine");
            c.That(TestKit.Order(game, company, new ConfigureWorkPolicyCommand { Worker = worker.Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) }).Accepted,
                "ordinary player/rival enables owned physical machine");
        }
        private static void Switch(Check c, SimGame game, int company)
            => c.That(TestKit.Order(game, company, new SetControllerCommand { Controller = Controller.Autopilot }).Accepted, "ordinary player/rival starts same controller pipeline");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1000 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " own=" +
                string.Join(";", Steps(game).Select(s => s.Id + "/" + s.Status + "/" + s.Blocker)) + " plans=" +
                string.Join(";", Plans(game).Concat(Plans(game, 1)).Select(p => p.Id + "/" + p.State + "/" + p.Blocker)));
        }
        private static PlacePlanTemplateCommand Place(SimGame game, int position, string group)
            => new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(Work(game).AuthoredPlanTemplate("expansion.tower")),
                Anchor = Content.Json.Parse("{\"space\":\"field.points\",\"position\":" + position + "}"), Group = group };
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "site-selection semantics have current engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no old fixed-anchor authoring/reader");
            Physical(c); Authoring(c); Race(c);
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IBusinessAccounts>(), "viable-site competition needs no money/Tycoon");
            c.Equal("proof.sites-undiscovered", Claim(game).Blocker.Id, "public site provider returns explicit private game-time wait");
            var initial = Claim(game, 1); ulong hash = game.Hash();
            c.That(initial.Sites.Select(s => s.Site.Id).SequenceEqual(new[] { "primary", "a.alternative", "b.alternative" }) && initial.SelectedSite.Id == "primary",
                "priority and stable ID, not JSON row order, choose same native candidate");
            c.Throws<NotSupportedException>(() => ((IList<PlaybookSiteSnapshot>)initial.Sites).Clear(), "candidate diagnostics immutable");
            c.Throws<InvalidOperationException>(() => initial.SelectedSite.Anchor.Set("position", JsonValue.Number("90")), "nested retained geometry frozen");
            c.Equal(hash, game.Hash(), "source/site/normal activation diagnostics consume no stock or placement");
            c.That(game.World.Content.Get<DelayedPlanSitesDef>("expansion.delayed") is IPlanSiteSource &&
                game.World.Content.Get<AuthoredPlanSitesDef>("expansion.sites").AuthoringPath.Contains("playbook-sites.json"), "public source and native sites use actual master document");
            Configure(c, game, 0); Configure(c, game, 1); Switch(c, game, 1);
            c.That(Facts(game, 1).AwaitingCommands == 1 && Plans(game, 1).Count == 0, "rival proposes ordinary next-Update command, not instant site");
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(Plans(game, 1).Single().State == PlanState.Active && Plans(game).Count == 0 && game.Hash() == twin.Hash(), "rival creates one real supplied physical project through saved normal admission");
            var attempted = Place(game, 30, "player.check");
            c.Equal(Code.None, game.Seat(0).Preview(attempted), "intent submission deliberately permits occupied physical placement");
            var actual = Work(game).PreviewPlanTemplateActivation(attempted.Selection, attempted.Anchor, attempted.Group);
            c.That(actual.Blocker == Codes.Blocked && actual.Entries.Single().Blocker == Codes.Blocked, "same provider activation owner reports genuine current occupied site");
            Reach(game, () => Claim(game).Status == PlaybookStepStatus.Ready, "authored public site wait ends", twin);
            var ready = Claim(game); hash = game.Hash();
            c.That(ready.SelectedSite.Id == "a.alternative" && ready.Sites[0].Admission.Blocker == Codes.Blocked && ready.Sites[1].Ready && ready.Sites[2].Ready,
                "private strategic readiness skips actually occupied rival site and selects stable tied alternative");
            c.That(initial.Sites.All(s => s.Ready) && initial.SelectedSite.Id == "primary", "retained old readiness never mutates or becomes current placement authority");
            c.Equal(hash, game.Hash(), "reselection is pure and grants no reservation");
            var foreign = new List<PlaybookStepSnapshot> { ready };
            c.That(!game.Seat(1).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(0), foreign) && foreign.Count == 0, "foreign company cannot inspect player site strategy");
            Switch(c, game, 0); twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(Claim(game).FirstPlan > 0 && Claim(game).Sites.Count == 0 && Claim(game).SelectedSite == null, "actual command/accepted native plan, not copied site-choice ledger, owns execution");
            c.Equal("24", Plans(game).Single().Arguments.Get("placement").Get("position").Text, "ordinary player template committed selected alternative intent");
            Reach(game, () => Steps(game).Single(s => s.Id == "expand").FirstPlan > 0, "physically completed claim unlocks expansion", twin);
            c.That(Claim(game).Status == PlaybookStepStatus.Done && Plans(game).Single(p => p.Group == "expansion.expand").Arguments.Get("placement").Get("position").Text == "42",
                "actual own and rival completed/active geometry advances next step to final viable alternative");
            twin = factory.Load(game.Save());
            Reach(game, () => Plans(game).All(p => p.State == PlanState.Done) && Plans(game, 1).Single().State == PlanState.Done &&
                Steps(game).Single(s => s.Id == "overflow").Status == PlaybookStepStatus.Blocked, "physical expansion completes and sites exhaust", twin);
            var blocked = Steps(game).Single(s => s.Id == "overflow");
            c.That(blocked.SelectedSite == null && blocked.Blocker == Codes.Blocked && blocked.Sites.Count == 3 && blocked.Sites.All(s => !s.Ready),
                "finite viable-site exhaustion is typed wait with exact per-site blockers, not a bonus or auto-demolition");
            c.That(game.World.Entities.All.Count(e => e.Alive && e.Def.Id == "point.tower" && e.Owner == new PlayerId(0)) == 2 &&
                game.World.Entities.All.Count(e => e.Alive && e.Def.Id == "point.tower" && e.Owner == new PlayerId(1)) == 1, "both companies construct real structures without privileged rival paths");
            c.Equal(Fix.FromInt(4), Stock(game, 0), "two player buildings physically consume16 from20");
            c.Equal(Fix.FromInt(12), Stock(game, 1), "one rival building physically consumes8 from20 without stealing own stock");
            c.That(game.Hash() == twin.Hash() && Facts(game).Decisions <= game.Tick, "saved geometry/work/site waits converge with bounded decisions");
            for (int i = 0; i < 7; i++) { Tick(game); Tick(twin); }
            c.That(Plans(game).Count == 2 && Plans(game, 1).Count == 1 && Facts(game).AwaitingCommands == 0 && game.Hash() == twin.Hash(), "exhausted sites never duplicate native intents or fabricate progress");
            c.That(Claim(New(factory), 1).SelectedSite.Id == "primary", "interleaved session has its own geometry, no shared chosen-site state");
        }
        private static void Authoring(Check c)
        {
            foreach (string fault in new[] { "missing-source", "missing-field", "legacy-anchor", "empty", "duplicate", "negative", "priority", "anchor", "space", "bounds" })
            {
                JsonValue json = Content.Json.Parse(FrameworkProofContent.Read("playbook-sites.json"));
                var step = json.Get("plan_playbooks")[0].Get("steps")[0]; var sites = json.Get("plan_site_sets")[0].Get("sites");
                if (fault == "missing-source") step.Set("sites", JsonValue.Of("missing"));
                if (fault == "missing-field") step.Remove("sites");
                if (fault == "legacy-anchor") { step.Remove("sites"); step.Set("anchor", sites[0].Get("anchor").Clone()); }
                if (fault == "empty") json.Get("plan_site_sets")[0].Set("sites", JsonValue.NewArray());
                if (fault == "duplicate") sites[0].Set("id", JsonValue.Of("primary"));
                if (fault == "negative") sites[0].Set("priority", JsonValue.Number("-1"));
                if (fault == "priority") sites[0].Remove("priority");
                if (fault == "anchor") sites[0].Remove("anchor");
                if (fault == "space") sites[0].Get("anchor").Set("space", JsonValue.Of("wrong"));
                if (fault == "bounds") sites[0].Get("anchor").Set("position", JsonValue.Number("1000"));
                c.Throws<ContentException>(() => New(Factory(json.ToString())), "invalid site authoring refuses before gameplay: " + fault);
            }
            c.Throws<ArgumentException>(() => new PlanSiteObservation(Code.None, new[] {
                new PlanSiteCandidate("same", 0, JsonValue.NewObject(), Code.None), new PlanSiteCandidate("same", 1, JsonValue.NewObject(), Code.None) }), "public source duplicate IDs refuse, no substitution");
            c.Throws<ArgumentException>(() => new PlanSiteObservation(Codes.Blocked, new[] {
                new PlanSiteCandidate("one", 0, JsonValue.NewObject(), Code.None) }), "blocked public source cannot publish usable candidates");
        }
        private static void Race(Check c)
        {
            // Normal player actions race a retained preview; no internal placement/project/resource writes.
            GameFactory factory = Factory(); SimGame game = New(factory);
            var command = Place(game, 30, "race.player");
            c.That(Work(game).PreviewPlanTemplateActivation(command.Selection, command.Anchor, command.Group).Ready, "prospective site is currently viable");
            var rival = new ConstructCommand { Def = "point.tower", Placement = command.Anchor, Priority = 70 };
            c.Equal(Code.None, game.Seat(1).Preview(rival), "ordinary rival direct construction has same current placement admission");
            c.That(TestKit.Order(game, 1, rival).Accepted, "rival actually claims site after retained player preview");
            c.Equal(Codes.Blocked, game.Seat(0).Preview(new ConstructCommand { Def = "point.tower", Placement = command.Anchor, Priority = 70 }), "current player direct admission sees actual race, not retained readiness");
            c.That(TestKit.Order(game, 0, command).Accepted && Plans(game).Single().State == PlanState.Pending && Plans(game).Single().Blocker == Codes.Blocked,
                "normal intent accepts race but physical execution waits; no implicit cancellation/replanning of accepted plans");
            SimGame twin = factory.Load(game.Save()); for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); }
            c.That(game.Hash() == twin.Hash() && Plans(game).Single().State == PlanState.Pending && Stock(game, 0) == Fix.FromInt(20),
                "saved blocked native intent conserves stock and real link; candidate preview was never a grant");
        }
    }
}

