using System;
using System.Collections.Generic;
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
    public static class OptionalWorkTests
    {
        public static void Register()
        {
            TestRunner.Add("Optional work: local timing, explicit dependencies and saved sessions", Local);
            TestRunner.Add("Optional work: physical owners, hauling, building and saved continuation", Physical);
        }

        internal static GameFactory Factory(LocalWorkProofModule module, string json = null, string[] documents = null)
        {
            var definition = new GameDefinition("local-work", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new WorkModule(), new WorldSetupModule(), module });
            var manifest = Json.Parse(FrameworkProofContent.Read("local-work-manifest.json"));
            if (documents != null) for (int i = 0; i < manifest.Get("modules").Count; i++)
                if (manifest.Get("modules")[i].Get("id").Text == module.Id)
                    foreach (string document in documents) manifest.Get("modules")[i].Get("documents").Add(JsonValue.Of(document));
            var files = new PreloadedFileSource().Add("manifest.json", manifest.ToString())
                .Add("framework/access.json", FrameworkProofContent.Access)
                .Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("local-work.json", json ?? FrameworkProofContent.Read("local-work.json"));
            if (documents != null) foreach (string document in documents) files.Add(document, FrameworkProofContent.Read(document));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "local-work", Seed = 29 });
        private static EntityId Target(SimGame game)
        {
            var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(new PlayerId(0), entities);
            return entities.Find(e => e.Def == "local").Id;
        }
        private static OrderView Active(SimGame game)
        {
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            if (orders.Count != 1) throw new InvalidOperationException("expected one active local order, got " + orders.Count);
            return orders[0];
        }
        private static Fix Stock(SimGame game, EntityId target)
            => game.World.Entities.Get(target).Get<IInventory>().Amount(game.World.Content.ResourceId("kit"));

        private static void Local(Check c)
        {
            var module = new LocalWorkProofModule(); GameFactory factory = Factory(module); SimGame game = New(factory);
            c.Equal(1, module.Initializations, "valid local session initializes once");
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<NavService>(), "unpriced local work needs neither money nor navigation");
            c.That(!game.World.Services.Has<IWorkSupplyRequests>() && !game.World.Services.Has<IHaulExecution>(), "no automatic supply or haul execution is fabricated");
            c.That(!game.World.Services.Has<IConstructionProjects>() && !game.World.Services.Has<ILineConstruction>(), "work does not install construction or infrastructure");
            c.That(game.World.Settings.Space == null, "local work is mapless");
            EntityId target = Target(game); ResourceId kit = game.World.Content.ResourceId("kit");
            c.That(game.Seat(0).View.Feature<InventoryView>().StoreAccepts("local", kit, Code.None), "inventory projection uses the authored port contract");
            Entity entity = game.World.Entities.Get(target);
            c.That(entity.Get<IOutletPort>() != null && entity.Get<IInletPort>() != null, "outlet also exposes inlet");
            c.Equal(Fix.FromInt(10), Stock(game, target), "initial inventory is authored");
            var post = new PostOrderCommand { Plan = "settle", Target = target };
            ulong before = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(post), "local work admits through the public command boundary");
            c.Equal(before, game.Hash(), "preview does not consume or progress");
            c.That(TestKit.Order(game, 0, post).Accepted, "unpriced local order executes without an economy");
            c.Equal(Fix.FromInt(8), Stock(game, target), "actual inputs are consumed once at start");
            c.Equal(6L, Active(game).CurrentNeeded, "authored duration resolves against session quantum");
            c.Equal(WorkProgress.Zero, Active(game).CurrentProgress, "starting is not instant completion");
            game.RunTicks(2);
            c.Equal(2L, Active(game).CurrentProgress.WholeTicks, "the engine advances timed work on updates");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "in-progress local work saves and restores");
            game.RunTicks(4); twin.RunTicks(4);
            c.Equal(0, game.Seat(0).View.Feature<WorkView>().ActiveOrders(target), "the required six updates complete work");
            c.Equal(Fix.FromInt(8), Stock(game, target), "completion does not consume twice");
            c.Equal(game.Hash(), twin.Hash(), "interleaved saved sessions finish identically");
            c.That(factory.Describe(game.Save()).Compatible, "save records the selected independent owners");
            c.That(TestKit.Order(game, 0, post).Accepted, "a later local activity can start");
            int id = Active(game).Id;
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = id }).Accepted, "ordinary work cancellation needs no construction provider");
            c.Equal(0, game.Seat(0).View.Feature<WorkView>().ActiveOrders(target), "cancellation removes active execution");
            c.Equal(Fix.FromInt(6), Stock(game, target), "cancelling consumed work does not invent a refund");

            string json = FrameworkProofContent.Read("local-work.json");
            GameFactory poorFactory = Factory(new LocalWorkProofModule(), json.Replace("\"initial\": 10", "\"initial\": 1"));
            SimGame poor = New(poorFactory); EntityId poorTarget = Target(poor);
            c.That(TestKit.Order(poor, 0, new PostOrderCommand { Plan = "settle", Target = poorTarget }).Accepted, "shortage remains a live physical wait");
            poor.RunTicks(20);
            c.Equal(WorkSystem.BlockerMaterials, Active(poor).Blocker, "no provider means a typed local material blocker");
            c.Equal(WorkProgress.Zero, Active(poor).CurrentProgress, "blocked work advances no progress");
            c.Equal(Fix.One, Stock(poor, poorTarget), "missing inputs are never fabricated");
            SimGame poorTwin = poorFactory.Load(poor.Save()); poor.RunTicks(5); poorTwin.RunTicks(5);
            c.Equal(poor.Hash(), poorTwin.Hash(), "blocked work survives save and continuation");
            c.That(TestKit.Order(poor, 0, new CancelOrderCommand { Order = Active(poor).Id }).Accepted, "blocked local work can be cancelled");

            var missingFlag = new LocalWorkProofModule();
            c.Throws<ContentException>(() => New(Factory(missingFlag, json.Replace("\"automatic_supply\": false, ", ""))), "supply policy must be authored explicitly");
            c.Equal(0, missingFlag.Initializations, "missing policy runs no initialization callback");
            var missingSupply = new LocalWorkProofModule();
            c.Throws<ContentException>(() => New(Factory(missingSupply, json.Replace("\"automatic_supply\": false", "\"automatic_supply\": true"))), "automatic input supply requires the actual selected contract");
            c.Equal(0, missingSupply.Initializations, "missing supply fails before Initialize");
            var missingMoney = new LocalWorkProofModule();
            string priced = json.Replace("\"requirement\": \"proof.local-work.settle.settling.inputs\"", "\"requirement\": \"local.paid\"")
                .Replace("\"activities\":", "\"work_cost_requirements\":[{\"id\":\"local.paid\",\"amount\":1,\"credit\":false, \"category\":\"construction\"}],\"activities\":");
            c.Throws<ContentException>(() => New(Factory(missingMoney, priced)), "authored monetary requirements need the actual optional economy");
            c.Equal(0, missingMoney.Initializations, "missing money fails before Initialize");
        }

        private static void Until(SimGame game, SimGame twin, Func<bool> done, int limit, string stage)
        {
            for (int tick = 0; tick < limit && !done(); tick++)
            {
                game.Update(); twin.Update();
                if (game.Fault != null || twin.Fault != null) break;
            }
            if (!done() || game.Fault != null || twin.Fault != null)
            {
                var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
                string diagnostics = "";
                foreach (OrderView order in orders) diagnostics += " [" + order.Plan + ":" + order.Blocker + "," + order.CurrentProgress + "/" + order.CurrentNeeded + "]";
                throw new InvalidOperationException(stage + " stalled at tick " + game.Tick + "; fault=" + game.Fault + diagnostics);
            }
        }
        internal static void Physical(Check c)
        {
            GameFactory factory = Colony.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 7 });
            foreach (string owner in new[] { WorkModule.ModuleId, LogisticsModule.ModuleId, ConstructionModule.ModuleId, InfrastructureModule.ModuleId, IncidentResponseModule.ModuleId })
                c.That(game.World.Content.HasModule(owner), "selected independent owner " + owner);
            LogisticsService logistics = game.World.Services.Get<LogisticsService>();
            c.That(ReferenceEquals(logistics, game.World.Services.Get<IWorkSupplyRequests>()), "work supply uses the existing demand ledger");
            c.That(ReferenceEquals(logistics, game.World.Services.Get<ITransformationSupplyRequests>()), "transformation supply uses that same ledger");
            c.That(ReferenceEquals(logistics, game.World.Services.Get<IHaulExecution>()), "haul execution uses the same coordinator");
            c.Equal(new Code("colony.on-water"), game.Seat(0).View.Feature<ConstructionView>().CheckPlacement("tower", Beep.SimEngine.Maps.TilePlacement.Arguments(game.World.Space.Id, new TileCoord(35, 5))), "construction projection owns placement validation");
            c.That(game.Seat(0).View.Feature<IncidentsView>() != null, "response projection has its own selected owner");
            EntityId depot = Colony.Mine(game, 0, "depot")[0].Id;
            EntityId transformer = Colony.Mine(game, 0, "factory")[0].Id;
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = transformer, Transformation = "build_truck", Home = depot }).Accepted, "transformation queues through the public command boundary");
            CommandResult construction = TestKit.Order(game, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(game.World.Space.Id, new TileCoord(10, 16)), Priority = 50 });
            c.That(construction.Accepted, "independent construction posts ordinary work");
            game.RunTicks(31); SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "active supply/build/transformation preserve module-owned state");
            bool moving = false;
            Until(game, twin, () => {
                moving |= Colony.Mine(game, 0, "crane").Exists(e => e.State == game.World.Content.Get<IEntityStateDefinition>("moving").State);
                return Colony.Mine(game, 0, "tower").Count == 1 && Colony.Mine(game, 0, "truck").Count == 3;
            }, 4320, "build and manufacture");
            c.That(moving, "machine construction includes physical travel");
            c.That(!game.Observer.TryGetEntity(construction.Entity, out _), "finished site is replaced by the actual structure");
            c.Equal(Fix.FromInt(10), Colony.Stock(game, depot, "steel"), "both workflows consume materials delivered by real hauling");
            c.Equal(game.Hash(), twin.Hash(), "saved physical workflows complete identically");
            var road = new List<TileCoord>(); for (int x = 10; x <= 15; x++) road.Add(new TileCoord(x, 20));
            c.That(TestKit.Order(game, 0, new LayLineCommand { Line = "road", Tiles = road }).Accepted, "infrastructure resolves its plan through the existing work board");
            game.RunTicks(17); twin = factory.Load(game.Save());
            var layer = new List<int>();
            Until(game, twin, () => {
                var status = game.Observer.Feature<TileMapView>().ReadLayer("road", new TileRect(10, 20, 6, 1), layer);
                return status == SpatialLayerReadStatus.Success && layer.Count == 6 && layer.TrueForAll(value => value == 1);
            }, 2880, "road construction");
            c.Equal(game.Hash(), twin.Hash(), "saved line plan resolver continues identically");
            c.Equal(Fix.FromInt(4), Colony.Stock(game, depot, "steel"), "road materials are physically supplied and consumed");
            CommandResult cancelled = TestKit.Order(game, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(game.World.Space.Id, new TileCoord(20, 20)), Priority = 50 });
            c.That(cancelled.Accepted, "a new site can wait on shortage");
            game.RunTicks(10);
            c.That(TestKit.Order(game, 0, new CancelConstructionCommand { Site = cancelled.Entity }).Accepted, "site-owned contract handles cancellation cleanup");
            c.That(!game.Observer.TryGetEntity(cancelled.Entity, out _), "cancelled site does not survive as an orphan");
            twin = factory.Load(game.Save()); game.RunTicks(30); twin.RunTicks(30);
            c.Equal(game.Hash(), twin.Hash(), "cancelled physical work and demand cleanup save consistently");
            c.That(game.Fault == null && twin.Fault == null, "bounded physical stages remain fault free");
        }
    }
}
