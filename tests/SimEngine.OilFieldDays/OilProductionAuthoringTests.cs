using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class OilProductionAuthoringTests
    {
        public const string CaseName = "Oil production: canonical timed manufacturing and optional cost annotations";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static void Sources(PreloadedFileSource files, JsonValue master)
        {
            var physical = Json.Parse(OilFieldDaysProofContent.Read("oil-production.json"));
            var rows = JsonValue.NewArray();
            for (int i = 0; i < physical.Get("transformations").Count; i++)
                if (physical.Get("transformations")[i].Get("id").Text == "build_haul_truck")
                { var row = physical.Get("transformations")[i]; row.Set("entity_output", JsonValue.Of("proof.infrastructure.truck")); row.Set("output_home", JsonValue.Of("producer")); rows.Add(row); }
            physical.Set("transformations", rows);
            var money = Json.Parse(OilFieldDaysProofContent.Read("oil-business-transformation-costs.json"));
            var selected = JsonValue.NewArray();
            for (int i = 0; i < money.Get("transformation_cost_bindings").Count; i++)
                if (money.Get("transformation_cost_bindings")[i].Get("transformation").Text == "build_haul_truck") selected.Add(money.Get("transformation_cost_bindings")[i]);
            money.Set("transformation_cost_bindings", selected);
            files.Add("oil-production.json", physical.ToString()).Add("oil-business-transformation-costs.json", money.ToString());
            for (int i = 0; i < master.Get("modules").Count; i++)
            {
                var module = master.Get("modules")[i];
                if (module.Get("id").Text == "proof.oil-infrastructure") module.Get("documents").Add(JsonValue.Of("oil-production.json"));
                if (module.Get("id").Text == TycoonModule.ModuleId) module.Get("documents").Add(JsonValue.Of("oil-business-transformation-costs.json"));
            }
        }
        private static void Fixture(JsonValue root)
        {
            root.Set("units", Json.Parse("[{\"id\":\"unit.work-rate\",\"dimension\":\"work-rate\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":false}]"));
            root.Get("time_definitions")[0].Set("quantum", Json.Parse("{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}"));
            root.Get("time_definitions")[0].Set("frame_ticks", JsonValue.Number("10"));
            for (int i = 0; i < root.Get("entities").Count; i++)
            {
                if (root.Get("entities")[i].Get("id").Text == "proof.infrastructure.truck")
                    root.Get("entities")[i].Get("components").Get("tile_presence").Set("blocks", JsonValue.Of(true));
                if (root.Get("entities")[i].Get("id").Text == "proof.infrastructure.source")
                {
                    var parts = root.Get("entities")[i].Get("components");
                    parts.Get("storage").Set("initial_stock", Json.Parse("{\"structural_steel\":20,\"machinery\":6,\"electrical_parts\":4}"));
                    parts.Get("inventory_port").Add(Json.Parse("{\"id\":\"oil.stock\"}"));
                    parts.Set("tile_work_dock", Json.Parse("{\"work_roles\":[],\"resources\":[],\"slots\":[{\"id\":\"launch\",\"kind\":\"rollout\",\"x\":0,\"y\":1},{\"id\":\"exit\",\"kind\":\"departure\",\"x\":0,\"y\":2},{\"id\":\"park.a\",\"kind\":\"parking\",\"x\":2,\"y\":2,\"work_role\":\"haul\"},{\"id\":\"park.b\",\"kind\":\"parking\",\"x\":3,\"y\":2,\"work_role\":\"haul\"},{\"id\":\"park.c\",\"kind\":\"parking\",\"x\":4,\"y\":2,\"work_role\":\"haul\"}],\"rollouts\":[{\"id\":\"trucks\",\"outputs\":[\"proof.infrastructure.truck\"],\"starts\":[\"launch\"],\"exits\":[\"exit\"],\"max_expanded\":4096}]}"));
                    parts.Set("transformer", Json.Parse("{\"automatic_supply\":false,\"wip_capacity\":\"proof.infrastructure.capacity\",\"transformations\":[\"build_haul_truck\"],\"states\":{\"working\":\"working\",\"stopped\":\"stopped\"},\"operation\":{\"id\":\"production\",\"enabled\":true,\"target\":1,\"minimum\":1,\"maximum\":1,\"unit\":\"unit.work-rate\"}}"));
                }
            }
            for (int i = 0; i < root.Get("scenarios").Count; i++)
            {
                root.Get("scenarios")[i].Get("modules").Add(JsonValue.Of(TransformationModule.ModuleId));
                root.Get("scenarios")[i].Set("time", JsonValue.Of("proof.infrastructure.time"));
            }
        }
        private static void Run(Check check)
        {
            var definitions = Json.Parse(OilFieldDaysProofContent.Read("oil-production.json")).Get("transformations");
            check.Equal(22, definitions.Count, "canonical seventeen vehicle and five process definitions authored");
            check.That(Enumerable.Range(0, definitions.Count).Any(i => definitions[i].Get("id").Text == "dispose_produced_water"),
                "canonical catalog includes explicitly authored produced-water disposal, not a discarded entry");
            check.Equal(17, Json.Parse(OilFieldDaysProofContent.Read("oil-business-transformation-costs.json")).Get("transformation_cost_bindings").Count, "all original monetary amounts owned by optional module");
            foreach (bool money in new[] { false, true })
            {
                var factory = OilAuthoredInfrastructureTests.Factory(authoredScenario: money ? "frontier" : "sandbox",
                    editFixture: Fixture, editSources: Sources, editBindings: OilAuthoredInfrastructureTests.SelectLineCosts);
                var game = OilAuthoredInfrastructureTests.New(factory);
                var source = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.infrastructure.source").Id;
                check.Equal(money, game.Modules.Contains(TycoonModule.ModuleId), "manufacturing does not activate money implicitly");
                game.Seat(0).Submit(new EnqueueTransformationCommand { Transformer = source, Capability = new Code("production"), Transformation = "build_haul_truck", Count = 2 });
                game.Update(); var twin = factory.Load(game.Save());
                var business = money ? game.Seat(0).View.Feature<BusinessView>() : null;
                long first = -1, second = -1;
                for (int i = 0; i < 460; i++)
                {
                    game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "canonical physical manufacture saved twin identity");
                    int trucks = game.Seat(0).Snapshot.Entities.Count(e => e.Def == "proof.infrastructure.truck");
                    if (trucks >= 2 && first < 0)
                    {
                        first = game.Clock.StepIndex; CheckRelease(check, game); twin = factory.Load(game.Save());
                    }
                    if (trucks >= 3) { second = game.Clock.StepIndex; break; }
                }
                check.That(first >= 2160, "first truck earns original36hours before release");
                check.That(second - first >= 2160, "second truck cannot skip its own36hour production");
                check.Equal(3, game.Seat(0).Snapshot.Entities.Count(e => e.Def == "proof.infrastructure.truck"), "two real entities manufactured, plus initial truck");
                CheckRelease(check, game); // Idle machines may subsequently return to the same depot.
                if (business != null)
                {
                    check.Equal(Code.None, business.ReadAccount(new PlayerId(0), out var account), "actual native account result");
                    check.Equal(Fix.FromInt(5964000), account.Balance, "original18000 charged exactly twice");
                    check.Equal(0, account.BudgetReservations, "original budget receipts retired");
                }
                check.That(game.Fault == null && twin.Fault == null, "healthy native physical manufacturing sessions");
            }
        }
        private static void CheckRelease(Check check, SimGame game)
        {
            var trucks = game.Seat(0).Snapshot.Entities.Where(e => e.Def == "proof.infrastructure.truck").OrderBy(e => e.Id.Value).ToList();
            var newest = ((TileSpatialSnapshot)trucks.Last().Spatial).Footprint;
            check.That(trucks.Take(trucks.Count - 1).All(e => !((TileSpatialSnapshot)e.Spatial).Footprint.Overlaps(newest)),
                "new truck clear: " + string.Join(";", trucks.Select(e => e.Id.Value + "=" + ((TileSpatialSnapshot)e.Spatial).Tile + ":" + e.State)));
        }
    }
}
