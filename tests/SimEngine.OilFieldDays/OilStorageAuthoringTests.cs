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
    public static class OilStorageAuthoringTests
    {
        public const string CaseName = "Oil storage: canonical capacity filters paired ports and physical cargo";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static JsonValue Clone(JsonValue value) => Json.Parse(value.ToString());
        private static List<JsonValue> Stores()
        {
            var values = new List<JsonValue>();
            foreach (string file in new[] { "oil-structures.json", "oil-vehicles.json" })
            {
                var entities = Json.Parse(OilFieldDaysProofContent.Read(file)).Get("entities");
                for (int i = 0; i < entities.Count; i++)
                    if (entities[i].Get("components").Get("storage") != null) values.Add(entities[i]);
            }
            return values;
        }
        private static JsonValue Store(string id) => Clone(Stores().Single(e => e.Get("id").Text == id).Get("components").Get("storage"));
        private static GameFactory Factory(string resource, string source, string destination, string carrier, int openingDestination, int malformed = 0)
            => OilAuthoredInfrastructureTests.Factory(editFixture: root => {
                foreach (string id in new[] { "proof.infrastructure.source", "proof.infrastructure.builder", "proof.infrastructure.truck" })
                {
                    JsonValue entity = null;
                    for (int i = 0; i < root.Get("entities").Count; i++) if (root.Get("entities")[i].Get("id").Text == id) entity = root.Get("entities")[i];
                    var parts = entity.Get("components");
                    var stock = Store(id == "proof.infrastructure.source" ? source : id == "proof.infrastructure.builder" ? destination : carrier);
                    stock.Set("initial_stock", Json.Parse(id == "proof.infrastructure.source" ? "{\"" + resource + "\":90}"
                        : id == "proof.infrastructure.builder" && openingDestination > 0 ? "{\"" + resource + "\":" + openingDestination + "}" : "{}"));
                    parts.Set("storage", stock);
                    var ports = parts.Get("inventory_port") ?? JsonValue.NewArray();
                    ports.Add(Json.Parse("{\"id\":\"oil.stock\"}")); parts.Set("inventory_port", ports);
                }
                root.Get("handling_profiles")[0].Set("resources", Json.Parse("[{\"resource\":\"" + resource + "\",\"quantity\":1,\"loading\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"},\"unloading\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}}]"));
            }, editSources: (files, master) => {
                var entities = JsonValue.NewArray();
                foreach (var original in Stores())
                {
                    var parts = JsonValue.NewObject(); parts.Set("storage", Clone(original.Get("components").Get("storage")));
                    parts.Set("inventory_port", Clone(original.Get("components").Get("inventory_port")));
                    var entity = JsonValue.NewObject(); entity.Set("id", Clone(original.Get("id"))); entity.Set("kind", Clone(original.Get("kind"))); entity.Set("components", parts);
                    entities.Add(entity);
                }
                if (malformed == 1) entities[entities.Count - 1].Get("components").Get("storage").Set("capacity_policy", JsonValue.Of("missing.policy"));
                if (malformed == 2) entities[0].Get("components").Get("storage").Set("accepts", Json.Parse("[\"crude\"]"));
                var projection = JsonValue.NewObject(); projection.Set("entities", entities);
                files.Add("oil-storage-projection.json", projection.ToString()).Add("oil-inventory.json", OilFieldDaysProofContent.Read("oil-inventory.json"));
                for (int i = 0; i < master.Get("modules").Count; i++) if (master.Get("modules")[i].Get("id").Text == "proof.oil-infrastructure")
                {
                    master.Get("modules")[i].Get("documents").Add(JsonValue.Of("oil-inventory.json"));
                    master.Get("modules")[i].Get("documents").Add(JsonValue.Of("oil-storage-projection.json"));
                }
            });
        private static Fix Amount(SimGame game, EntityId entity, ResourceId resource)
        {
            var refusal = game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(entity, new Code("oil.stock"), resource, out var fact);
            if (!refusal.IsNone) throw new InvalidOperationException("stock observation refused: " + refusal);
            return fact.Stock.AmountOf(resource);
        }
        private static void Run(Check check)
        {
            check.Equal(26, Stores().Count, "all canonical stores selected");
            check.Equal(16, Json.Parse(OilFieldDaysProofContent.Read("oil-inventory.json")).Get("capacity_policies").Count, "shared authored limits, not repeated stock owners");
            check.That(typeof(IInletPort).IsAssignableFrom(typeof(IOutletPort)), "every exporting port is receiving-capable by contract");
            ContentRefusal(check, 1, "missing.policy");
            ContentRefusal(check, 2, "storage cannot accept authored resource");
            foreach (string resource in new[] { "cement", "crude", "natural_gas" })
            {
                bool solid = resource == "cement", gas = resource == "natural_gas";
                string sourceDef = solid ? "company_base" : "export_terminal";
                string destinationDef = solid || gas ? (solid ? "maintenance_depot" : "gas_generator") : "storage_tank";
                int opening = solid ? 0 : gas ? 395 : 1995;
                int delivery = solid ? 40 : 5;
                var factory = Factory(resource, sourceDef, destinationDef, solid ? "haul_truck" : "tanker", opening);
                var game = OilAuthoredInfrastructureTests.New(factory); game.Update();
                var view = game.Seat(0).View; var inventory = view.Feature<InventoryView>();
                var items = Json.Parse(OilFieldDaysProofContent.Read("oil-items.json")).Get("resources");
                foreach (var entity in Stores())
                {
                    var storage = entity.Get("components").Get("storage");
                    var accepts = storage.Get("accepts"); var phases = storage.Get("phases");
                    for (int i = 0; i < items.Count; i++)
                    {
                        string id = items[i].Get("id").Text, phase = items[i].Get("phase").Text;
                        bool phaseAllowed = Enumerable.Range(0, phases.Count).Any(n => phases[n].Text == phase);
                        bool accepted = accepts.Count == 0 || Enumerable.Range(0, accepts.Count).Any(n => accepts[n].Text == id);
                        check.That(view.TryFindResource(id, out var item), "native resource identity");
                        check.Equal(phaseAllowed && accepted, inventory.StoreAccepts(entity.Get("id").Text, item, Code.None), "canonical filter contract");
                    }
                }
                var source = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.infrastructure.source").Id;
                var destination = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.infrastructure.builder").Id;
                var truck = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.infrastructure.truck").Id;
                check.That(view.TryFindResource(resource, out var quantity), "physical selected resource");
                check.That(inventory.TryGetCapacity(truck, Code.None, out var cargo), "native actual cargo capacity");
                check.Equal(Fix.FromInt(solid ? 40 : 120), cargo.Usage.Single().Limit, "original cargo limit retained");
                var preview = view.Feature<LogisticsView>().PreviewHaul(source, destination, quantity, Fix.FromInt(90),
                    sourcePort: new Code("oil.stock"), destinationPort: new Code("oil.stock"), worker: truck);
                check.Equal(HaulStatus.Ready, preview.Status, "native physical haul admitted");
                check.Equal(Fix.FromInt(delivery), preview.Amount, "cargo and destination policies bound the whole dispatch");
                var command = new HaulResourcesCommand { Source = source, Destination = destination, Worker = truck, Resource = quantity,
                    SourcePort = new Code("oil.stock"), DestinationPort = new Code("oil.stock"), Ceiling = Fix.FromInt(90) };
                check.Equal(Code.None, game.Seat(0).Preview(command), "command and management preview agree");
                game.Seat(0).Submit(command); game.Update();
                var twin = factory.Load(game.Save()); bool savedCargo = false, finished = false;
                for (int frame = 0; frame < 240; frame++)
                {
                    game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "partial cargo saved twin identity");
                    Fix held = Amount(game, truck, quantity), received = Amount(game, destination, quantity);
                    check.Equal(Fix.FromInt(90 + opening), Amount(game, source, quantity) + held + received, "no cargo duplication/loss");
                    if (!savedCargo && held.IsPositive && received == Fix.FromInt(opening)) { savedCargo = true; twin = factory.Load(game.Save()); }
                    if (received == Fix.FromInt(opening + delivery) && held.IsZero) { finished = true; break; }
                }
                check.That(savedCargo && finished, "bounded physical loading/travel/delivery and partial save");
                for (int frame = 0; frame < 120 && view.Feature<WorkView>().IdleWorkers(new WorkerRoleId("haul")) == 0; frame++)
                {
                    game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "physical return saved twin identity");
                    check.Equal(Fix.FromInt(90 + opening), Amount(game, source, quantity) + Amount(game, truck, quantity) + Amount(game, destination, quantity), "return preserves earned stock");
                }
                check.Equal(1, view.Feature<WorkView>().IdleWorkers(new WorkerRoleId("haul")), "carrier physically returns before a new dispatch");
                check.Equal(Fix.FromInt(90 - delivery), Amount(game, source, quantity), "finite source debited once");
                if (!solid)
                    check.Equal(HaulStatus.NoCapacity, view.Feature<LogisticsView>().PreviewHaul(source, destination, quantity, Fix.One,
                        sourcePort: new Code("oil.stock"), destinationPort: new Code("oil.stock"), worker: truck).Status, "full store refuses more cargo");
                check.That(game.Fault == null && twin.Fault == null, "healthy native storage sessions");
                check.That(!game.Modules.Contains(TycoonModule.ModuleId), "storage and hauling do not require money");
            }
        }
        private static void ContentRefusal(Check check, int malformed, string expected)
        {
            try
            {
                OilAuthoredInfrastructureTests.New(Factory("cement", "company_base", "maintenance_depot", "haul_truck", 0, malformed));
                check.That(false, "invalid storage declaration must refuse");
            }
            catch (ContentException error) { check.That(error.Message.Contains(expected), "actual storage refusal: " + error.Message); }
        }
    }
}
