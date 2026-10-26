using System.Linq;
using System;
using System.Collections.Generic;
using Beep.SimEngine;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class StandardTests
    {
        public static void Register()
        {
            TestRunner.Add("Standard: a structure is built by machines from hauled materials", Construction);
            TestRunner.Add("Standard: a stock target buys from the market, carried by truck", Procurement);
            TestRunner.Add("Standard: a factory makes a vehicle from delivered inputs", Production);
            TestRunner.Add("Standard: demolition and cancellation leave salvage that is carried away", Salvage);
            TestRunner.Add("Standard: unreachable work fails honestly", Unreachable);
            TestRunner.Add("Standard: askers' demands add up and a store keeps what it wants", Demands);
            TestRunner.Add("Standard: a colony mid-build saves and resumes identically", SaveResume);
            TestRunner.Add("Standard: lines are laid tile by tile and networks carry goods", Lines);
        }

        private static void Construction(Check c)
        {
            SimGame g = Colony.New();
            PlayerSeat me = g.Seat(0);
            IEconomy cash = g.World.Services.Get<IEconomy>();
            Fix before = cash.Cash(new PlayerId(0));
            c.Equal(new Code("colony.on-water"), me.View.Feature<ConstructionView>().CheckPlacement("tower", Beep.SimEngine.Maps.TilePlacement.Arguments(g.World.Space.Id, new TileCoord(35, 5))), "a game placement rule refuses water");
            c.Equal(Codes.Blocked, me.Preview(new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(g.World.Space.Id, new TileCoord(4, 4)), Priority = 50 }), "occupied ground is refused");
            CommandResult r = TestKit.Order(g, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(g.World.Space.Id, new TileCoord(10, 16)), Priority = 50 });
            c.That(r.Accepted && r.Entity.IsValid, "placing a tower is accepted and names its site");
            c.That(g.Observer.TryGetEntity(r.Entity, out EntityView site) && site.Def == "site:tower", "a site appears at once");
            c.Equal(before, cash.Cash(new PlayerId(0)), "placing is free (the ghost stage costs nothing)");
            var orders = new List<OrderView>();
            me.View.Feature<WorkView>().CopyOrders(orders);
            c.That(orders.Count == 1 && orders[0].Plan == "build:tower" && orders[0].NodeCount == 2, "the construction order is visible to its owner");
            var theirs = new List<OrderView>();
            g.Seat(1).View.Feature<WorkView>().CopyOrders(theirs);
            c.Equal(0, theirs.Count, "and not to a rival");

            bool sawCraneMoving = false;
            int ticks = TestKit.RunUntil(g, () =>
            {
                var cranes = Colony.Mine(g, 0, "crane");
                if (cranes.Count > 0 && cranes[0].State == g.World.Content.Get<IEntityStateDefinition>("moving").State) sawCraneMoving = true;
                return Colony.Mine(g, 0, "tower").Count == 1;
            }, checked((int)TestGame.Time.Days(3)));
            c.That(sawCraneMoving, "the crane drove to the site");
            c.Equal(1, Colony.Mine(g, 0, "tower").Count, "the tower was finished (in " + ticks + " ticks)");
            c.That(ticks >= checked((int)TestGame.Time.Hours(5)), "no faster than the crane's 5 hours of work");
            c.That(!g.Observer.TryGetEntity(r.Entity, out _), "the site is gone");
            c.Equal(before - Fix.FromInt(100), cash.Cash(new PlayerId(0)), "cash was taken when the materials step started");
            EntityId depot = Colony.Mine(g, 0, "depot")[0].Id;
            c.Equal(Fix.FromInt(20), Colony.Stock(g, depot, "steel"), "30 steel left the depot (carried by trucks)");
            var news = new List<Notification>();
            long cursor = 0;
            me.ReadNotifications(ref cursor, news);
            c.That(news.Exists(n => n.Kind == ConstructionService.NoteBuilt && n.Subject == "tower"), "the owner is told the structure is built");
        }

        private static void Procurement(Check c)
        {
            SimGame g = Colony.New();
            PlayerSeat me = g.Seat(0);
            EntityId depot = Colony.Mine(g, 0, "depot")[0].Id;
            me.View.TryFindResource("steel", out ResourceId steel);
            IEconomy cash = g.World.Services.Get<IEconomy>();
            Fix before = cash.Cash(new PlayerId(0));
            CommandResult r = TestKit.Order(g, 0, new ConfigureStockPolicyCommand { Store = depot, Resource = steel, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(90), Fix.FromInt(90), Fix.FromInt(90), 45, LogisticsReserveAccess.Protected)) });
            c.That(r.Accepted, "a stock target is accepted");
            c.Equal(Code.None, me.View.Feature<LogisticsView>().ReadStockTarget(depot, steel, out var stockTarget), "stock target available");
            c.Equal(Fix.FromInt(90), stockTarget, "and visible");
            Fix atOrder = Colony.Stock(g, depot, "steel");
            c.Equal(Fix.FromInt(50), atOrder, "nothing arrives by itself");
            TestKit.RunUntil(g, () => Colony.Stock(g, depot, "steel") >= Fix.FromInt(90), checked((int)TestGame.Time.Days(2)));
            c.Equal(Fix.FromInt(90), Colony.Stock(g, depot, "steel"), "trucks carried steel from the market up to the target");
            c.Equal(before - Fix.FromInt(400), cash.Cash(new PlayerId(0)), "40 steel was paid for at 10 each, when loaded");
            c.Equal(Codes.NotOwned, me.Preview(new ConfigureStockPolicyCommand { Store = Colony.Mine(g, 1, "depot")[0].Id, Resource = steel, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(10), Fix.FromInt(10), Fix.FromInt(10), 45, LogisticsReserveAccess.Protected)) }), "a rival's depot is not mine to stock");
        }

        private static void Production(Check c)
        {
            SimGame g = Colony.New();
            EntityId factory = Colony.Mine(g, 0, "factory")[0].Id;
            EntityId depot = Colony.Mine(g, 0, "depot")[0].Id;
            CommandResult r = TestKit.Order(g, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = factory, Transformation = "build_truck", Home = depot });
            c.That(r.Accepted, "a vehicle is queued at the factory");
            c.Equal(new Code("core.not-made-here"), g.Seat(0).Preview(new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = factory, Transformation = "nope" }), "an unknown transformation is refused");
            var queue = new List<TransformationQueueEntry>();
            g.Seat(0).View.Feature<TransformationManagementView>().CopyQueue(factory, new Code("transformation"), queue);
            c.Equal(1, queue.Count, "the queue is visible");
            TestKit.RunUntil(g, () => Colony.Mine(g, 0, "truck").Count == 3, checked((int)TestGame.Time.Days(2)));
            c.Equal(3, Colony.Mine(g, 0, "truck").Count, "a third truck rolled out");
            c.Equal(Fix.FromInt(40), Colony.Stock(g, depot, "steel"), "its 10 steel were carried to the factory");
        }

        private static void Demands(Check c)
        {
            SimGame g = Colony.New();
            World w = g.World;
            LogisticsService log = w.Services.Get<LogisticsService>();
            Entity depot = w.Entities.Get(Colony.Mine(g, 0, "depot")[0].Id);
            ResourceId steel = w.Content.ResourceId("steel");
            Order order = w.Services.Get<WorkBoard>().Post(w.Content.Get<ActivityDef>("inspect"), depot.Owner, depot.Id, 1);
            var orderSource = new LogisticsDemandId(WorkModule.ModuleId, order.Id, Code.None);
            log.ConfigureStockPolicy(depot.Owner, depot.Id, steel, LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(10), Fix.FromInt(10), Fix.FromInt(10), 1, LogisticsReserveAccess.Protected)));
            log.SetRequest(orderSource, depot.Owner, depot.Id, steel, Fix.FromInt(5), 1);
            c.Equal(Fix.FromInt(15), log.DemandOf(depot.Id, steel), "a player's stock target and an order's need on one store add up");
            log.SetRequest(orderSource, depot.Owner, depot.Id, steel, Fix.FromInt(3), 1);
            c.Equal(Fix.FromInt(13), log.DemandOf(depot.Id, steel), "an asker changing its want changes only its own share");
            log.WithdrawRequests(orderSource);
            c.Equal(Fix.FromInt(10), log.DemandOf(depot.Id, steel), "withdrawing an order leaves the player's target");
            Storage store = depot.Get<Storage>();
            Fix held = store.Amount(steel);
            c.Equal(Fix.Max(Fix.Zero, held - Fix.FromInt(10)), log.Spare(store, steel), "a store only gives away what is above its own want");
        }

        private static void Salvage(Check c)
        {
            SimGame g = Colony.New();
            CommandResult r = TestKit.Order(g, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(g.World.Space.Id, new TileCoord(10, 16)), Priority = 50 });
            TestKit.RunUntil(g, () => Colony.Mine(g, 0, "tower").Count == 1, checked((int)TestGame.Time.Days(3)));
            EntityId tower = Colony.Mine(g, 0, "tower")[0].Id;
            EntityId depot = Colony.Mine(g, 0, "depot")[0].Id;
            CommandResult d = TestKit.Order(g, 0, new DemolishCommand { Target = tower });
            c.That(d.Accepted, "demolition is ordered");
            TestKit.RunUntil(g, () => !g.Observer.TryGetEntity(tower, out _), checked((int)TestGame.Time.Days(1)));
            c.That(!g.Observer.TryGetEntity(tower, out _), "a machine took the tower down");
            TestKit.RunUntil(g, () => Colony.Stock(g, depot, "steel") >= Fix.FromInt(35), checked((int)TestGame.Time.Days(2)));
            c.Equal(Fix.FromInt(35), Colony.Stock(g, depot, "steel"), "half the steel came back as salvage, carried to the depot");
            c.Equal(0, Colony.Mine(g, 0, g.Seat(0).View.Feature<Beep.SimEngine.Views.ConstructionView>().RecoveryContainer).Count, "the salvage pile is cleared");

            CommandResult s2 = TestKit.Order(g, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(g.World.Space.Id, new TileCoord(20, 20)), Priority = 50 });
            TestKit.RunUntil(g, () => false, checked((int)TestGame.Time.Hours(4)));
            CommandResult cancel = TestKit.Order(g, 0, new CancelConstructionCommand { Site = s2.Entity });
            c.That(cancel.Accepted && !g.Observer.TryGetEntity(s2.Entity, out _), "a site can be cancelled");
        }

        private static void Unreachable(Check c)
        {
            SimGame g = Colony.New();
            CommandResult r = TestKit.Order(g, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(g.World.Space.Id, new TileCoord(38, 8)), Priority = 50 });
            c.That(r.Accepted, "the island is dry ground, so placing is allowed");
            TestKit.RunUntil(g, () => false, checked((int)TestGame.Time.Hours(6)));
            var orders = new List<OrderView>();
            g.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            c.That(orders.Count == 1 && orders[0].NodesDone == 0 && orders[0].Blocker == WorkSystem.BlockerNoWorker, "no machine can reach it, and the order says why");
            c.Equal(1, g.Seat(0).View.Feature<WorkView>().IdleWorkers(new WorkerRoleId("crane")), "the crane is not stuck on it");
        }

        private static void Lines(Check c)
        {
            SimGame g = Colony.New();
            var road = new List<TileCoord>();
            for (int x = 10; x <= 15; x++) road.Add(new TileCoord(x, 20));
            c.Equal(new Code("core.path-not-contiguous"), g.Seat(0).Preview(new LayLineCommand { Line = "road", Tiles = { new TileCoord(1, 1), new TileCoord(5, 5) } }), "a broken path is refused");
            CommandResult r = TestKit.Order(g, 0, new LayLineCommand { Line = "road", Tiles = road });
            c.That(r.Accepted && r.Arg0 > 0, "a road is staged as an ordinary persistent plan");
            var layer = new List<int>();
            c.Equal(SpatialLayerReadStatus.Success, g.Observer.Feature<global::Beep.SimEngine.Maps.TileMapView>().ReadLayer("road", new TileRect(10, 20, 6, 1), layer), "road query succeeds");
            c.That(layer.TrueForAll(v => v == 0), "nothing is laid at once");
            bool partial = false;
            TestKit.RunUntil(g, () =>
            {
                c.Equal(SpatialLayerReadStatus.Success, g.Observer.Feature<global::Beep.SimEngine.Maps.TileMapView>().ReadLayer("road", new TileRect(10, 20, 6, 1), layer), "road progress query succeeds");
                int laid = layer.FindAll(v => v > 0).Count;
                if (laid > 0 && laid < 6) partial = true;
                return laid == 6 && !g.Observer.TryGetEntity(r.Entity, out _);
            }, checked((int)TestGame.Time.Days(2)));
            c.That(partial, "the road grew tile by tile behind the machine");
            c.That(layer.TrueForAll(v => v == 1), "and was finished");
            c.Equal(Fix.FromInt(44), Colony.Stock(g, Colony.Mine(g, 0, "depot")[0].Id, "steel"), "a steel a tile was carried out and used");

            // Two tanks joined by a pipe: water moves from the one that pushes to the one that pulls.
            World w = g.World;
            Entity tank = ((global::Beep.SimEngine.Maps.TileMap)w.Space).SpawnAt(w, w.Content.Get<EntityDef>("tank"), new PlayerId(0), new TileCoord(20, 24));
            Entity sink = ((global::Beep.SimEngine.Maps.TileMap)w.Space).SpawnAt(w, w.Content.Get<EntityDef>("sink"), new PlayerId(0), new TileCoord(26, 24));
            tank.Get<Storage>().Add(w.Content.ResourceId("water"), Fix.FromInt(100));
            var pipe = new List<TileCoord>();
            for (int x = 22; x <= 25; x++) pipe.Add(new TileCoord(x, 25));
            TestKit.Order(g, 0, new LayLineCommand { Line = "pipe", Tiles = pipe });
            TestKit.RunUntil(g, () => sink.Get<Storage>().Amount(w.Content.ResourceId("water")) >= Fix.FromInt(100), checked((int)TestGame.Time.Days(2)));
            Fix inPipe = w.Services.Providers<NetworkService>().Single().Component("waternet", 0).Amount(w.Content.ResourceId("water"));
            c.Equal(Fix.FromInt(100), sink.Get<Storage>().Amount(w.Content.ResourceId("water")) + inPipe, "all the water reached the sink or is in the pipe (nothing made or lost)");
            c.Equal(Fix.FromInt(100), sink.Get<Storage>().Amount(w.Content.ResourceId("water")), "and it all arrived");
            GameFactory f = Colony.Factory();
            SimGame b = f.Load(g.Save());
            c.Equal(g.Hash(), b.Hash(), "a game with lines and networks loads identically");
        }

        private static void SaveResume(Check c)
        {
            GameFactory f = Colony.Factory();
            SimGame a = f.NewGame(new NewGameRequest { Scenario = "colony", Seed = 4 });
            TestKit.Order(a, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(a.World.Space.Id, new TileCoord(10, 16)), Priority = 50 });
            TestKit.Order(a, 1, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = Colony.Mine(a, 1, "factory")[0].Id, Transformation = "build_truck" });
            a.RunTicks(checked((int)TestGame.Time.Hours(2)) + 17);
            SimGame b = f.Load(a.Save());
            c.Equal(a.Hash(), b.Hash(), "the loaded colony matches");
            a.RunTicks(checked((int)TestGame.Time.Days(1)));
            b.RunTicks(checked((int)TestGame.Time.Days(1)));
            c.Equal(a.Hash(), b.Hash(), "and runs on identically (jobs, hauls, movement, transformation)");
            c.Equal(Colony.Mine(a, 0, "tower").Count, Colony.Mine(b, 0, "tower").Count, "both finished the same work");
        }
    }
}
