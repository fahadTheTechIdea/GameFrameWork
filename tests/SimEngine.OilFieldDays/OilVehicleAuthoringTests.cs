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
    public static class OilVehicleAuthoringTests
    {
        public const string CaseName = "Oil vehicles: native land sea state roles and authored physical handling";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static JsonValue Clone(JsonValue value) => Json.Parse(value.ToString());
        private static IReadOnlyList<JsonValue> _vehicles;
        private static IReadOnlyList<JsonValue> Vehicles()
        {
            if (_vehicles != null) return _vehicles;
            var root = Json.Parse(OilFieldDaysProofContent.Read("oil-vehicles.json")).Get("entities");
            root.Freeze();
            return _vehicles = Array.AsReadOnly(Enumerable.Range(0, root.Count).Select(i => root[i]).ToArray());
        }
        private static TileCoord Start(JsonValue entity)
        {
            bool sea = entity.Get("components").Get("mover").Get("class").Text == "sea";
            var peers = Vehicles().Where(e => e.Get("components").Get("mover").Get("class").Text == (sea ? "sea" : "land")).ToList();
            int index = peers.FindIndex(e => e.Get("id").Text == entity.Get("id").Text);
            return new TileCoord(3 + index, sea ? 25 : 12);
        }
        private static void AddSetup(JsonValue root, string id, string entity, TileCoord at, bool sea)
        {
            root.Get("entity_setups").Add(Json.Parse("{\"id\":\"" + id + "\",\"after\":" + (sea ? "[\"proof.vehicle.water\"]" : "[]")
                + ",\"entity\":\"" + entity + "\",\"company\":0,\"placement\":{\"space\":\"oil.tiles\",\"x\":" + at.X + ",\"y\":" + at.Y + "},\"home\":null}"));
            root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of(id));
        }
        private static void Fixture(JsonValue root)
        {
            root.Get("tile_layer_setups").Add(Json.Parse("{\"id\":\"proof.vehicle.water\",\"after\":[],\"layer\":\"water\",\"x\":2,\"y\":22,\"width\":20,\"height\":12,\"value\":2,\"owner\":-1}"));
            root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("proof.vehicle.water"));
            foreach (var entity in Vehicles()) AddSetup(root, "proof.vehicle.start." + entity.Get("id").Text, entity.Get("id").Text, Start(entity),
                entity.Get("components").Get("mover").Get("class").Text == "sea");
            int pair = 0;
            foreach (var entity in Vehicles().Where(e => e.Get("components").Get("transporter") != null))
            {
                bool sea = entity.Get("components").Get("mover").Get("class").Text == "sea"; string id = entity.Get("id").Text;
                int row = sea ? 28 + pair % 2 : 19 + pair % 2;
                AddSetup(root, "proof.vehicle.source.start." + id, "proof.vehicle.source." + id, new TileCoord(4, row), sea);
                AddSetup(root, "proof.vehicle.destination.start." + id, "proof.vehicle.destination." + id, new TileCoord(8, row), sea); pair++;
            }
        }
        private static void Sources(PreloadedFileSource files, JsonValue master)
        {
            var entities = JsonValue.NewArray();
            var profiles = Json.Parse(OilFieldDaysProofContent.Read("oil-vehicle-handling.json")).Get("handling_profiles");
            foreach (var original in Vehicles())
            {
                var parts = JsonValue.NewObject();
                foreach (string key in new[] { "tile_presence", "mover", "worker", "storage", "inventory_port", "transporter" })
                    if (original.Get("components").Get(key) != null) parts.Set(key, Clone(original.Get("components").Get(key)));
                var entity = JsonValue.NewObject(); entity.Set("id", Clone(original.Get("id"))); entity.Set("kind", Clone(original.Get("kind"))); entity.Set("components", parts); entities.Add(entity);
                if (parts.Get("transporter") == null) continue;
                string id = original.Get("id").Text; var profile = Enumerable.Range(0, profiles.Count).Select(i => profiles[i]).Single(p => p.Get("id").Text == parts.Get("transporter").Get("handling").Text);
                string resource = parts.Get("storage").Get("phases")[0].Text == "solid" ? "cement" : "crude";
                var row = Enumerable.Range(0, profile.Get("resources").Count).Select(i => profile.Get("resources")[i]).Single(r => r.Get("resource").Text == resource);
                foreach (bool source in new[] { true, false })
                {
                    var store = Clone(parts.Get("storage"));
                    store.Set("initial_stock", Json.Parse(source ? "{\"" + resource + "\":" + row.Get("quantity").ToString() + "}" : "{}"));
                    var stockParts = JsonValue.NewObject(); stockParts.Set("storage", store); stockParts.Set("inventory_port", Clone(parts.Get("inventory_port")));
                    stockParts.Set("tile_presence", Json.Parse("{\"space\":\"oil.tiles\",\"width\":1,\"height\":1,\"blocks\":true,\"movable\":false}"));
                    var stock = JsonValue.NewObject(); stock.Set("id", JsonValue.Of("proof.vehicle." + (source ? "source." : "destination.") + id));
                    stock.Set("kind", JsonValue.Of("structure")); stock.Set("components", stockParts); entities.Add(stock);
                }
            }
            var document = JsonValue.NewObject(); document.Set("entities", entities);
            files.Add("oil-vehicle-projection.json", document.ToString()).Add("oil-inventory.json", OilFieldDaysProofContent.Read("oil-inventory.json"))
                .Add("oil-vehicle-handling.json", OilFieldDaysProofContent.Read("oil-vehicle-handling.json"));
            for (int i = 0; i < master.Get("modules").Count; i++) if (master.Get("modules")[i].Get("id").Text == "proof.oil-infrastructure")
                foreach (string file in new[] { "oil-vehicle-projection.json", "oil-inventory.json", "oil-vehicle-handling.json" }) master.Get("modules")[i].Get("documents").Add(JsonValue.Of(file));
        }
        private static Fix Stock(SimGame game, EntityId entity, ResourceId resource)
        {
            var refusal = game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(entity, new Code("oil.stock"), resource, out var fact);
            if (!refusal.IsNone) throw new InvalidOperationException("actual cargo observation refused: " + refusal);
            return fact.Stock.AmountOf(resource);
        }
        private static void Run(Check check)
        {
            check.Equal(17, Vehicles().Count, "all canonical vehicle identities selected");
            check.Equal(4, Json.Parse(OilFieldDaysProofContent.Read("oil-vehicle-handling.json")).Get("handling_profiles").Count, "four authored handling references");
            var modes = Json.Parse(OilFieldDaysProofContent.Read("oil-game-modes.json")).Get("time_definitions");
            var showcase = Enumerable.Range(0, modes.Count).Select(i => modes[i]).Single(t => t.Get("id").Text == "oil.time.showcase");
            check.Equal("1500", showcase.Get("horizon").Get("numerator").ToString(), "original showcase horizon remains exact authored1500");
            check.Equal("day", showcase.Get("horizon").Get("unit").Text, "horizon belongs to sole game time definition");
            var factory = OilAuthoredInfrastructureTests.Factory(authoredScenario: "sandbox", editFixture: Fixture, editSources: Sources);
            var game = OilAuthoredInfrastructureTests.New(factory); game.Update();
            var view = game.Seat(0).View; var motion = view.Feature<MotionView>(); var work = view.Feature<WorkView>();
            var actors = game.Seat(0).Snapshot.Entities.Where(e => Vehicles().Any(v => v.Get("id").Text == e.Def)).ToDictionary(e => e.Def, e => e.Id);
            check.Equal(17, actors.Count, "all native machines initialized");
            foreach (var entity in Vehicles())
            {
                string id = entity.Get("id").Text; var at = Start(entity); var to = new TileCoord(at.X, at.Y + 6);
                check.Equal(Code.None, motion.ReadNavigation(actors[id], out var navigation), "actual navigation metadata");
                check.Equal(entity.Get("components").Get("mover").Get("class").Text, navigation.Profile, "original land/sea intent");
                check.Equal(1, navigation.Allowed.Count, "only authored compatible navigation choice");
                check.Equal(Code.None, work.ReadWorkerExecution(actors[id], out var worker), "actual native worker execution");
                check.Equal(Fix.One, worker.BaseRate, "explicit neutral base work rate");
                check.Equal(new WorkerRoleId(entity.Get("components").Get("worker").Get("roles")[0].Text), worker.Roles.Single(), "original role through contract");
                var command = new MoveVehicleCommand { Vehicle = actors[id], To = to };
                check.Equal(Code.None, game.Seat(0).Preview(command), "engine route preview for actual land/sea vehicle");
                game.Seat(0).Submit(command);
            }
            game.Update(); var twin = factory.Load(game.Save()); var moving = new HashSet<EntityId>(); bool allArrived = false;
            for (int frame = 0; frame < 60; frame++)
            {
                game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "all seventeen partial motion saved twin identity");
                foreach (var actor in game.Seat(0).Snapshot.Entities.Where(e => actors.Values.Contains(e.Id)))
                    if (actor.State == new Code("state.moving")) moving.Add(actor.Id);
                allArrived = Vehicles().All(e => { var at = Start(e); return ((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(a => a.Id == actors[e.Get("id").Text]).Spatial).Tile == new TileCoord(at.X, at.Y + 6); });
                if (allArrived) break;
            }
            check.That(allArrived && moving.Count == 17, "all machines move physically through authored states, without teleporting");
            foreach (var entity in Vehicles().Where(e => e.Get("components").Get("transporter") != null))
            {
                string id = entity.Get("id").Text; EntityId carrier = actors[id];
                var release = new SetWorkerHeldCommand { Worker = carrier, Held = false };
                check.Equal(Code.None, game.Seat(0).Preview(release), "ordinary player release, no direct worker edit");
                game.Seat(0).Submit(release); game.Update(); twin = factory.Load(game.Save());
                var source = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.vehicle.source." + id).Id;
                var destination = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.vehicle.destination." + id).Id;
                string resource = entity.Get("components").Get("storage").Get("phases")[0].Text == "solid" ? "cement" : "crude";
                check.That(view.TryFindResource(resource, out var quantity), "actual native cargo resource");
                Fix total = Stock(game, source, quantity); var command = new HaulResourcesCommand { Source = source, Destination = destination, Worker = carrier,
                    SourcePort = new Code("oil.stock"), DestinationPort = new Code("oil.stock"), Resource = quantity, Ceiling = total };
                check.Equal(Code.None, game.Seat(0).Preview(command), "canonical transporter admits compatible physical cargo");
                long started = game.Clock.StepIndex; game.Seat(0).Submit(command); game.Update(); twin = factory.Load(game.Save());
                bool savedCargo = false, loading = false, complete = false;
                for (int frame = 0; frame < 120; frame++)
                {
                    game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "canonical handling saved twin identity");
                    Fix held = Stock(game, carrier, quantity), received = Stock(game, destination, quantity);
                    check.Equal(total, Stock(game, source, quantity) + held + received, "finite source/cargo/destination conservation");
                    if (game.Seat(0).Snapshot.Entities.Single(e => e.Id == carrier).State == new Code("state.loading")) loading = true;
                    if (!savedCargo && held.IsPositive && received.IsZero) { savedCargo = true; twin = factory.Load(game.Save()); }
                    if (received == total && held.IsZero) { complete = true; break; }
                }
                check.That(complete && savedCargo && loading, "native loading/travel/unloading states and partial cargo save for " + id);
                check.That(game.Clock.StepIndex - started >= 120, "one hour loading plus one hour unloading for a full nominal hold");
                check.Equal(Fix.Zero, Stock(game, source, quantity), "whole finite cargo delivered once");
            }
            check.That(game.Fault == null && twin.Fault == null, "healthy native vehicle/cargo sessions");
            check.That(!game.Modules.Contains(TycoonModule.ModuleId), "machines and handling require no money");
        }
    }
}
