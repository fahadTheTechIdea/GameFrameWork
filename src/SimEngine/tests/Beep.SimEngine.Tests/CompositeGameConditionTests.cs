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
    internal static class CompositeGameConditionTests
    {
        private const string Case = "Conditions: authored all-any graphs, private strategy and physical replay";
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(string conditions = null, Action<JsonValue> editSites = null)
        {
            var sites = Content.Json.Parse(FrameworkProofContent.Read("conditional-plan-sites.json"));
            var rows = sites.Get("plan_site_sets")[0].Get("sites");
            for (int i = 0; i < rows.Count; i++) rows[i].Set("conditions", Content.Json.Parse("[\"policy.a.all\"]"));
            editSites?.Invoke(sites);
            return ConditionalPlanSiteTests.Factory(sites.ToString(), conditions, "composite-game-conditions.json");
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static GameConditionsView Conditions(SimGame game, int company = 0) => game.Seat(company).View.Feature<GameConditionsView>();
        private static PlaybookStepSnapshot Claim(SimGame game)
        {
            var rows = new List<PlaybookStepSnapshot>();
            game.Seat(0).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(0), rows); return rows.Single();
        }
        private static List<PlanView> Plans(SimGame game)
        { var rows = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(rows); return rows; }
        private static Fix Stock(SimGame game)
        {
            Fix stock = Fix.Zero; var supply = game.World.Content.ResourceId("supply");
            foreach (var entity in game.World.Entities.All)
                if (entity.Alive && entity.Owner == new PlayerId(0) && entity.Get<IInletPort>() is IInletPort inlet) stock += inlet.Amount(supply);
            return stock;
        }
        private static void Order(Check c, SimGame game, int company, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(company).Preview(command), "ordinary preview");
            c.Equal(hash, game.Hash(), "preview pure");
            c.That(TestKit.Order(game, company, command).Accepted, "ordinary engine command accepted");
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.RunTicks(1); twin?.RunTicks(1);
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("composite condition time=" + game.Tick + "/fault=" + game.Fault);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current shared-condition graph identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no persisted readiness or condition clock");
            Physical(c); Authoring(c);
        }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory); var view = Conditions(game); ulong hash = game.Hash();
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IBusinessAccounts>(), "nested strategy not money/business");
            c.Equal(Codes.UnknownDef, view.CheckCondition("missing"), "unknown condition query explicit");
            c.Equal(Codes.UnknownDef, view.CheckCondition(null), "null query does not select a fallback");
            c.Equal("controller.risk-disabled", view.CheckCondition("policy.b.any").Id, "any retains first actual stable-ID blocker while both alternatives fail");
            c.Equal("controller.risk-disabled", view.CheckCondition("policy.a.all").Id, "nested all propagates actual child blocker");
            c.That(Claim(game).SelectedSite == null && Claim(game).Sites.All(s => !s.Ready), "nested groups gate every ordinary candidate");
            c.Equal(hash, game.Hash(), "all/any/site/view observations mutate no state");
            var group = game.World.Content.Get<CompositeGameConditionDef>("policy.a.all");
            c.That(group.ConditionIds.SequenceEqual(new[] { "policy.b.any", "strategy.reserve" }) && group.AuthoringPath.Contains("composite-game-conditions.json"),
                "canonical graph belongs to actual master-listed file");
            c.Throws<NotSupportedException>(() => ((IList<string>)group.ConditionIds).Clear(), "immutable dependency metadata");
            var beacon = game.Seat(1).Snapshot.Entities.Single(e => e.Def == "strategy.beacon" && e.Owner == new PlayerId(1)).Id;
            Order(c, game, 1, new ConfigureOperationCommand { Entity = beacon, Operation = new Code("risk-dial"), Enabled = true, Target = Fix.Zero });
            c.Equal(Code.None, Conditions(game, 1).CheckCondition("policy.b.any"), "rival real ready branch satisfies same any profile");
            c.Equal("controller.risk-disabled", view.CheckCondition("policy.b.any").Id, "foreign signal does not satisfy own graph");
            var worker = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine" && e.Owner == new PlayerId(0)).Id;
            Order(c, game, 0, new ConfigureWorkPolicyCommand { Worker = worker, Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            using (var recorder = game.RecordReplay(Limits))
            {
                Order(c, game, 0, new SetControllerCommand { Controller = Controller.Autopilot });
                c.That(Plans(game).Count == 0 && Claim(game).SelectedSite == null, "native strategy waits with no fabricated intent");
                var twin = factory.Load(game.Save());
                for (int i = 0; i < 12 && Claim(game).PendingCommand == 0; i++) Step(game, twin);
                c.That(Claim(game).PendingCommand > 0 && Plans(game).Count == 0, "independent public clock leaf unlocks one actual next-update intent");
                c.Equal(Code.None, view.CheckCondition("policy.b.any"), "any true through elapsed alternative while own risk still false");
                c.Equal("controller.risk-disabled", view.CheckCondition("policy.z.strict").Id, "all does not turn alternate readiness into both readiness");
                c.Equal(Code.None, view.CheckCondition("policy.a.all"), "all also requires actual own reserve");
                twin = factory.Load(game.Save()); Step(game, twin);
                c.That(Plans(game).Single().State == PlanState.Active && Plans(game).Single().Arguments.Get("placement").Get("position").Text == "30",
                    "original priority selects physically viable primary after actual logical readiness");
                twin = factory.Load(game.Save());
                for (int i = 0; i < 2000 && Claim(game).Status != PlaybookStepStatus.Done; i++) Step(game, twin);
                if (Claim(game).Status != PlaybookStepStatus.Done) throw new InvalidOperationException("physical graph wait time=" + game.Tick + "/blocker=" + Claim(game).Blocker);
                c.Equal(Fix.FromInt(44), Stock(game), "normal machines haul/build and consume eight of52 supply");
                c.Equal(1, game.Seat(0).Snapshot.Entities.Count(e => e.Def == "point.tower"), "no instant or duplicate structure");
                var result = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(result.Success && result.Game != null && result.Game.Hash() == game.Hash(), "canonical replay preserves shared graph/physical results");
            }
            var low = Content.Json.Parse(FrameworkProofContent.Read("composite-game-conditions.json"));
            low.Get("signal_conditions")[1].Get("conditions")[0].Set("minimum", JsonValue.Number("33"));
            var waiting = New(Factory(low.ToString())); for (int i = 0; i < 6; i++) Step(waiting);
            c.Equal("controller.reserve", Conditions(waiting).CheckCondition("policy.a.all").Id, "all keeps actual reserve refusal despite elapsed any success");
            c.Equal(waiting.Hash(), Factory(low.ToString()).Load(waiting.Save()).Hash(), "existing save owners reproduce reserve graph wait");
        }
        private static void Authoring(Check c)
        {
            foreach (string fault in new[] { "mode", "missing", "empty", "null", "duplicate", "unknown", "wrong-kind", "self-cycle", "cycle", "unbound-cycle" })
            {
                var json = Content.Json.Parse(FrameworkProofContent.Read("composite-game-conditions.json")); var rows = json.Get("composite_conditions");
                var row = rows[0];
                if (fault == "mode") row.Set("mode", JsonValue.Of("not"));
                if (fault == "missing") row.Remove("conditions");
                if (fault == "empty") row.Set("conditions", JsonValue.NewArray());
                if (fault == "null") row.Set("conditions", JsonValue.Null());
                if (fault == "duplicate") row.Set("conditions", Content.Json.Parse("[\"strategy.risk\",\"strategy.risk\"]"));
                if (fault == "unknown") row.Set("conditions", Content.Json.Parse("[\"missing\"]"));
                if (fault == "wrong-kind") row.Set("conditions", Content.Json.Parse("[\"choice.single\"]"));
                if (fault == "self-cycle") row.Set("conditions", Content.Json.Parse("[\"policy.a.all\"]"));
                if (fault == "cycle") rows[1].Set("conditions", Content.Json.Parse("[\"policy.a.all\"]"));
                if (fault == "unbound-cycle") rows[2].Set("conditions", Content.Json.Parse("[\"policy.z.strict\"]"));
                c.Throws<ContentException>(() => New(Factory(json.ToString())), "malformed shared condition graph refuses before Initialize: " + fault);
            }
        }
    }
}
