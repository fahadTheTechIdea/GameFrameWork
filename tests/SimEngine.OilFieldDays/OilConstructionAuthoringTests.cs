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
    public static class OilConstructionAuthoringTests
    {
        public const string CaseName = "Oil construction: canonical supplied pad and timed removal through native DAG";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static JsonValue Clone(JsonValue value) => Json.Parse(value.ToString());
        private static void Fixture(JsonValue root)
        {
            // Explicit fixture handling allows a loaded-cargo checkpoint at the authored ten-update frame stride.
            root.Get("handling_profiles")[0].Get("resources")[0].Get("loading").Set("numerator", JsonValue.Number("10"));
            root.Get("handling_profiles")[0].Get("resources")[0].Get("unloading").Set("numerator", JsonValue.Number("10"));
            for (int i = 0; i < root.Get("entities").Count; i++)
            {
                var entity = root.Get("entities")[i]; var parts = entity.Get("components");
                if (entity.Get("id").Text == "proof.infrastructure.source")
                {
                    parts.Get("storage").Set("initial_stock", Json.Parse("{\"cement\":6}"));
                    parts.Get("inventory_port").Add(Json.Parse("{\"id\":\"oil.stock\"}"));
                }
                if (entity.Get("id").Text == "proof.infrastructure.builder")
                    parts.Get("worker").Set("roles", Json.Parse("[\"grader\",\"bulldozer\"]"));
                if (entity.Get("id").Text == "proof.infrastructure.truck")
                    parts.Set("inventory_port", Json.Parse("[{\"id\":\"oil.stock\"}]"));
            }
            for (int i = 0; i < root.Get("scenarios").Count; i++)
            {
                root.Get("scenarios")[i].Get("modules").Add(JsonValue.Of(ConstructionModule.ModuleId));
                root.Get("scenarios")[i].Get("settings").Set(ConstructionModule.ModuleId, Json.Parse("{\"recovery\":null}"));
            }
        }
        private static void Sources(PreloadedFileSource files, JsonValue master)
        {
            var raw = Json.Parse(OilFieldDaysProofContent.Read("oil-structures.json")).Get("entities");
            var targets = JsonValue.NewArray();
            for (int i = 0; i < raw.Count; i++)
            {
                var source = raw[i].Get("components");
                if (source.Get("construction") == null) continue;
                var parts = JsonValue.NewObject();
                foreach (string component in new[] { "tile_presence", "construction", "storage", "inventory_port" })
                    if (source.Get(component) != null) parts.Set(component, Clone(source.Get(component)));
                var entity = JsonValue.NewObject(); entity.Set("id", Clone(raw[i].Get("id"))); entity.Set("kind", Clone(raw[i].Get("kind"))); entity.Set("components", parts); targets.Add(entity);
            }
            var projection = JsonValue.NewObject(); projection.Set("entities", targets);
            var executor = JsonValue.NewObject();
            executor.Set("staffed_activity_executors", Clone(Json.Parse(OilFieldDaysProofContent.Read("oil-work.json")).Get("staffed_activity_executors")));
            var money = Json.Parse(files.ReadText("oil-business-costs.json")); var selected = JsonValue.NewArray();
            for (int i = 0; i < money.Get("activity_cost_bindings").Count; i++)
            {
                var binding = money.Get("activity_cost_bindings")[i]; string activity = binding.Get("activity").Text;
                if (activity.StartsWith("oil.line.") || activity.StartsWith("oil.build.")) selected.Add(binding);
            }
            money.Set("activity_cost_bindings", selected); files.Add("oil-business-costs.json", money.ToString());
            files.Add("oil-construction-targets.json", projection.ToString()).Add("oil-construction-executor.json", executor.ToString());
            foreach (string file in new[] { "oil-inventory.json", "oil-construction.json", "oil-construction-sites.json", "oil-construction-effects.json" })
                files.Add(file, OilFieldDaysProofContent.Read(file));
            for (int i = 0; i < master.Get("modules").Count; i++) if (master.Get("modules")[i].Get("id").Text == "proof.oil-infrastructure")
                foreach (string file in new[] { "oil-inventory.json", "oil-construction.json", "oil-construction-sites.json", "oil-construction-effects.json", "oil-construction-targets.json", "oil-construction-executor.json" })
                    master.Get("modules")[i].Get("documents").Add(JsonValue.Of(file));
        }
        private static Fix Stock(SimGame game, EntityId entity, ResourceId resource)
        {
            Code refusal = game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(entity, new Code("oil.stock"), resource, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("actual stock observation refused: " + refusal);
            return facts.Stock.AmountOf(resource);
        }
        private static void Run(Check check)
        {
            check.Equal(36, Json.Parse(OilFieldDaysProofContent.Read("oil-construction.json")).Get("activities").Count, "all18 native build/removal workflows");
            check.Equal(18, Json.Parse(OilFieldDaysProofContent.Read("oil-construction-sites.json")).Get("entities").Count, "all18 authored exact target sites");
            foreach (bool money in new[] { false, true })
            {
                var factory = OilAuthoredInfrastructureTests.Factory(authoredScenario: money ? "frontier" : "sandbox", editFixture: Fixture, editSources: Sources);
                var game = OilAuthoredInfrastructureTests.New(factory); game.Update();
                var view = game.Seat(0).View; var construction = view.Feature<ConstructionView>(); var work = view.Feature<WorkView>();
                var source = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.infrastructure.source").Id;
                var truck = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.infrastructure.truck").Id;
                check.That(view.TryFindResource("cement", out var cement), "actual native material identity");
                var command = new ConstructCommand { Def = "well_pad", Placement = TilePlacement.Arguments("oil.tiles", new TileCoord(9, 4)) };
                check.Equal(Code.None, construction.CheckPlacement(command.Def, command.Placement), "engine-owned site placement preview");
                check.Equal(Code.None, game.Seat(0).Preview(command), "same construct-command refusal semantics");
                game.Seat(0).Submit(command); game.Update();
                var site = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "oil.site.well_pad");
                check.That(construction.TryReadSite(site.Id, out var fact), "actual authored site/target/order view");
                check.Equal("well_pad", fact.TargetDefinition, "site binding, not client name parsing");
                var twin = factory.Load(game.Save()); bool savedCargo = false, savedWork = false; EntityId built = EntityId.None;
                for (int frame = 0; frame < 180; frame++)
                {
                    game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "supplied construction saved twin identity");
                    if (!savedCargo && Stock(game, truck, cement).IsPositive) { savedCargo = true; twin = factory.Load(game.Save()); }
                    var nodes = new List<ActivityNodeView>();
                    if (work.TryCopyNodes(fact.Order, nodes) && nodes.Single().State == NodeState.Working && !savedWork)
                    {
                        savedWork = true; twin = factory.Load(game.Save());
                        check.Equal(new Code("state.under-construction"), game.Seat(0).Snapshot.Entities.Single(e => e.Id == site.Id).State, "entity Update owns authored site state");
                    }
                    var complete = game.Seat(0).Snapshot.Entities.Where(e => e.Def == "well_pad").ToList();
                    if (complete.Count == 1) { built = complete[0].Id; break; }
                }
                check.That(built.IsValid && savedCargo && savedWork, "physical build checkpoints: result=" + built.IsValid + ", cargo=" + savedCargo + ", machine=" + savedWork);
                check.That(game.Clock.StepIndex >= 480, "eight authored machine-hours not instant placement");
                check.That(!construction.TryReadSite(site.Id, out _), "original site retired by native completion");
                check.Equal(Fix.Zero, Stock(game, source, cement), "original six cement consumed exactly once");
                check.Equal(Fix.Zero, Stock(game, truck, cement), "no stranded duplicated construction cargo");
                check.Equal(money, game.Modules.Contains(TycoonModule.ModuleId), "physical construction does not activate optional money");
                if (money)
                {
                    check.Equal(Code.None, view.Feature<BusinessView>().ReadAccount(new PlayerId(0), out var account), "actual optional native account");
                    check.Equal(Fix.FromInt(5980000), account.Balance, "original20000 construction cost once");
                }
                var demolition = new DemolishCommand { Target = built };
                check.Equal(Code.None, construction.CheckDemolition(built), "native empty-stock removal preview");
                check.Equal(Code.None, game.Seat(0).Preview(demolition), "matching demolition command");
                long started = game.Clock.StepIndex; game.Seat(0).Submit(demolition); game.Update(); twin = factory.Load(game.Save());
                bool savedRemoval = false;
                for (int frame = 0; frame < 90; frame++)
                {
                    game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "timed removal saved twin identity");
                    var orders = new List<OrderView>(); work.CopyOrders(orders);
                    foreach (var order in orders.Where(o => o.Plan == "demolish:well_pad"))
                    {
                        var nodes = new List<ActivityNodeView>();
                        if (!savedRemoval && work.TryCopyNodes(order.Id, nodes) && nodes.Any(n => n.State == NodeState.Working))
                        { savedRemoval = true; twin = factory.Load(game.Save()); }
                    }
                    if (!game.Seat(0).Snapshot.Entities.Any(e => e.Id == built)) break;
                }
                check.That(savedRemoval && !game.Seat(0).Snapshot.Entities.Any(e => e.Id == built), "actual machine removal completes and saves");
                check.That(game.Clock.StepIndex - started >= 120, "two authored removal hours, not instant despawn");
                check.That(game.Fault == null && twin.Fault == null, "healthy native build/removal sessions");
            }
        }
    }
}
