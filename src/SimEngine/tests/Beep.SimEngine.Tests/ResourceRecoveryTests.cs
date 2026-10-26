using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ResourceRecoveryTests
    {
        public static void Register()
        {
            TestRunner.Add("Resource recovery: guarded removal, live stock and cancelled-site continuation", Removal);
            TestRunner.Add("Resource recovery: timed construction, native salvage and saved physical delivery", Physical);
            TestRunner.Add(PointRecoveryTests.ObservationCase, PointRecoveryTests.ObservationBoundary);
            TestRunner.Add(PointRecoveryTests.OperationCase, PointRecoveryTests.OperationBoundary);
        }
        private static GameFactory Factory(string json = null, bool supplier = false)
        {
            var documents = new Dictionary<string, string> {
                ["resource-recovery-startup.json"] = FrameworkProofContent.Read("resource-recovery-startup.json") };
            if (supplier) documents.Add("resource-recovery-supplier-startup.json", FrameworkProofContent.Read("resource-recovery-supplier-startup.json"));
            return PhysicalRecoveryTestContent.Factory(supplier: supplier, additionalDocuments: documents,
                worldDefinitions: json, flowDefinitions: FrameworkProofContent.Read("recovery-flows.json"), automaticRecovery: !supplier,
                edit: root => root.Get("scenarios")[0].Get("settings").Set(WorldSetupModule.ModuleId,
                    Json.Parse("{\"profile\":\"recovery." + (supplier ? "supplier" : "physical") + "\"}")));
        }
        private static NewGameRequest Request()
        { var request = new NewGameRequest { Scenario = "colony", Seed = 83 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); request.Mods.Add(ResourceFlowsModule.ModuleId); return request; }
        private static Entity Find(SimGame game, string id)
        { foreach (Entity e in game.World.Entities.All) if (e.Alive && e.Def.Id == id) return e; return null; }
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("recovery tick " + game.Tick + ": " + game.Fault); }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, string stage, int limit = 1200)
        {
            for (int i = 0; i < limit && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) {
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException(stage + " stalled at " + game.Clock.StepIndex + ", fault=" + game.Fault +
                    ", jobs=" + string.Join(";", jobs.Select(job => job.Id + "/" + job.State + "/" + job.Worker + "/" + job.Loaded + "/" + job.Blocker)));
            }
        }
        private static ResourceFlowPage Read(SimGame game, long cursor = 0)
            => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor);
        private static Fix Total(SimGame game, ResourceId resource)
        {
            Fix amount = Fix.Zero;
            foreach (Entity e in game.World.Entities.All) if (e.Alive && e.Owner == new PlayerId(0))
                amount += e.Get<IInventory>()?.Amount(resource) ?? Fix.Zero;
            return amount;
        }
        private static void Effect(SimGame game, Entity entity, Bill bill, ResourceFlowKind kind = ResourceFlowKind.Produced)
        {
            var request = new ResourceInventoryIntent(new ReservationOwner("proof.recovery-fixture", entity.Id.Value), entity.Owner,
                entity.Id, bill, kind, new Code("proof.authored-test-effect"));
            if (!game.World.Services.Get<IResourceTransaction>().CommitInventory(request).Committed) throw new InvalidOperationException("fixture effect refused");
        }
        private static Bill Amount(ResourceId resource, Fix amount) => new Bill(new[] { new ResourceAmount(resource, amount) });
        private static void Removal(Check c)
        {
            // This protocol case explicitly checks optional supplier promises as well as recovery.
            GameFactory factory = Factory(supplier: true); SimGame game = factory.NewGame(Request()); World world = game.World;
            ResourceId steel = world.Content.ResourceId("steel"), water = world.Content.ResourceId("water"); Entity empty = Find(game, "factory");
            c.That(ReferenceEquals(world.Services.Providers<ConstructionService>().Single(), world.Services.Get<IResourceRecovery>()), "recovery aliases the sole existing construction service");
            var owner = new ReservationOwner("proof.foreign-claim", 1); IInventory inventory = empty.Get<IInventory>();
            ReservationToken inbound = inventory.ReserveIn(owner, steel, Fix.One); ulong before = game.Hash();
            c.Equal(ResourceTransactions.ActivePromises, world.CheckDespawn(empty), "empty inventory still protects inbound promises");
            c.Throws<InvalidOperationException>(() => world.Despawn(empty), "blind removal cannot erase commitments");
            c.Equal(before, game.Hash(), "typed removal refusal changes no stock, promises, IDs or hash"); inventory.ReleaseIn(owner, inbound);
            Effect(game, empty, Amount(steel, Fix.FromInt(3))); Tick(game); before = game.Hash();
            c.Equal(ResourceTransactions.UndisposedStock, world.CheckDespawn(empty), "actual stock requires explicit disposition");
            c.Throws<InvalidOperationException>(() => world.Despawn(empty), "blind removal cannot erase real stock");
            c.Equal(before, game.Hash(), "stock refusal is pure");
            Effect(game, empty, Amount(steel, Fix.FromInt(3)), ResourceFlowKind.Lost); Tick(game);
            c.That(world.CheckDespawn(empty).IsNone, "explicit verified loss permits subsequent removal"); world.Despawn(empty); Tick(game);
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), Read(game).Totals(steel).LostRaw, "real loss is accounted distinctly from salvage");
            Entity market = Find(game, "market"); ISupplier supplier = market.Get<ISupplier>();
            ReservationToken supplied = supplier.ReserveSupply(owner, steel, Fix.One); before = game.Hash();
            c.Equal(ResourceTransactions.ActivePromises, world.CheckDespawn(market), "authored suppliers protect their exact supply promises");
            c.Equal(before, game.Hash(), "supplier removal check is pure"); supplier.ReleaseSupply(owner, supplied);
            c.That(world.CheckDespawn(market).IsNone, "supplier removal permits no outstanding supply claims");
            Entity source = ((TileMap)world.Space).SpawnAt(world, world.Content.Get<EntityDef>("external"), new PlayerId(0), new TileCoord(12, 24)); world.Settle(); Tick(game);
            IResourceRecovery recovery = world.Services.Get<IResourceRecovery>(); IInventory original = source.Get<IInventory>();
            ReservationToken held = original.ReserveOut(owner, steel, Fix.One); before = game.Hash();
            c.Equal(ResourceTransactions.ActivePromises, recovery.Evacuate(world, source), "recovery cannot clear another owner's promised stock");
            c.Equal(before, game.Hash(), "normal recovery refusal creates no piles, tokens or facts"); original.ReleaseOut(owner, held);
            long cursor = Read(game).Latest; before = game.Hash();
            c.That(recovery.CheckEvacuation(world, source).IsNone, "external live stock has exact recovery capacity");
            c.Equal(before, game.Hash(), "complete recovery preflight creates nothing");
            c.That(recovery.Evacuate(world, source).IsNone, "external stock moves through existing receipts");
            c.Equal(Fix.Zero, original.Amount(steel), "source really becomes empty before removal");
            c.Equal(0, original.Reservations.Count, "actual movement consumes its source receipts");
            world.Settle(); c.Equal(Fix.FromInt(75), Total(game, steel), "all split pile stock is conserved, not recreated");
            c.Equal(3, Colony.Mine(game, 0, game.Seat(0).View.Feature<Beep.SimEngine.Views.ConstructionView>().RecoveryContainer).Count, "authored capacity ten splits 25 into physical piles");
            foreach (EntityView pile in Colony.Mine(game, 0, game.Seat(0).View.Feature<Beep.SimEngine.Views.ConstructionView>().RecoveryContainer))
                c.Equal(new TileCoord(12, 24), world.Entities.Get(pile.Id).Get<ITilePresence>().Tile, "disposition occurs at the source, not a remote depot");
            world.Despawn(source); Tick(game); ResourceFlowPage moved = Read(game, cursor);
            c.Equal(3, moved.Entries.Count, "one actual transfer fact per split physical receipt");
            c.Equal(BigInteger.Zero, moved.Totals(steel).CreatedRaw, "moving real stock never masquerades as creation");
            foreach (var entry in moved.Entries)
            {
                c.Equal(ResourceFlowKind.StockTransfer, entry.Item.Kind, "recovery uses existing exact physical transfer protocol");
                c.Equal(ResourceEndpointId.ForEntity(source.Id), entry.Item.Source.Endpoint, "historical source remains the removed actual entity");
                c.Equal(new ReservationOwner("sim.resource.recovery", source.Id.Value), entry.Item.Owner, "receipts identify the real recovery requester");
            }
            Cancelled(c, factory, game, steel, water); Line(c, factory);
            var invalid = Json.Parse(FrameworkProofContent.Read("resource-recovery.json"));
            var prototypes = invalid.Get("entities");
            for (int i = 0; i < prototypes.Count; i++) if (prototypes[i].Get("id").Text == "tower")
                prototypes[i].Get("components").Get("construction").Get("demolish").Get("salvage").Set("steel", JsonValue.Number("15.5"));
            c.Throws<ContentException>(() => Factory(invalid.ToString()).NewGame(Request()), "explicit structural salvage must respect its native discrete unit");
            PointRecoveryTests.Admission(c);
        }
        private static void Cancelled(Check c, GameFactory factory, SimGame game, ResourceId steel, ResourceId water)
        {
            CommandResult placed = TestKit.Order(game, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(game.World.Space.Id, new TileCoord(18, 20)), Priority = 50 });
            c.That(placed.Accepted, "player places a physical site"); Entity site = game.World.Entities.Get(placed.Entity);
            var leftovers = new Bill(new[] { new ResourceAmount(steel, Fix.FromInt(8)), new ResourceAmount(water, Fix.FromRatio(5, 2)) });
            Effect(game, site, leftovers); Tick(game); IInventory stock = site.Get<IInventory>();
            var owner = new ReservationOwner("proof.cancel-claim", 1); ReservationToken promise = stock.ReserveIn(owner, steel, Fix.One);
            c.That(TestKit.Order(game, 0, new CancelConstructionCommand { Site = site.Id }).Accepted, "player cancellation ends work without erasing another claim");
            Order order = game.World.Services.Get<WorkBoard>().Order(site.Get<Site>().Order);
            c.That(site.Alive && order.State == OrderState.Cancelled, "blocked cancelled site remains authoritative");
            c.Equal(ResourceTransactions.ActivePromises, order.Blocker, "existing order reports typed disposition blocker");
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            c.That(orders.Exists(o => o.Id == order.Id && o.State == OrderState.Cancelled && o.Blocker == ResourceTransactions.ActivePromises),
                "immutable work view exposes pending disposition through the retained ended order");
            c.Equal(Fix.FromInt(8), stock.Amount(steel), "blocked cancellation retains delivered stock");
            c.Equal(1, stock.Reservations.Count, "blocked cancellation retains the original promise");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "blocked site and exact owner claims are durable");
            stock.ReleaseIn(owner, promise); twin.World.Entities.Get(site.Id).Get<IInventory>().ReleaseIn(owner, promise);
            long cursor = Read(game).Latest; Fix totalSteel = Total(game, steel), totalWater = Total(game, water); Tick(game); Tick(twin);
            c.That(game.World.Entities.Get(site.Id) == null, "engine cleanup retries and removes the site after release");
            c.Equal(totalSteel, Total(game, steel), "cancelled-site recovery preserves all actual steel");
            c.Equal(totalWater, Total(game, water), "cancelled-site recovery preserves fractional native fluid");
            c.Equal(game.Hash(), twin.Hash(), "saved cancellation/recovery continues identically");
            ResourceFlowPage page = Read(game, cursor); int transferred = 0; Fix steelMoved = Fix.Zero, waterMoved = Fix.Zero;
            foreach (var entry in page.Entries) if (entry.Item.Source.Endpoint == ResourceEndpointId.ForEntity(site.Id))
            { transferred++; if (entry.Item.Resource == steel) steelMoved += entry.Item.Amount; if (entry.Item.Resource == water) waterMoved += entry.Item.Amount; }
            c.Equal(3, transferred, "combined authored capacity splits eight steel plus two-and-a-half water into three exact receipts");
            c.Equal(Fix.FromInt(8), steelMoved, "cancellation fact quantities preserve native count");
            c.Equal(Fix.FromRatio(5, 2), waterMoved, "cancellation fact quantities preserve native fluid");
            c.That(game.World.Services.Get<WorkBoard>().Order(order.Id) == null, "resolved disposition prunes only at the final engine tick boundary");
            c.Equal(BigInteger.Zero, page.Totals(steel).CreatedRaw + page.Totals(water).CreatedRaw, "cancellation fabricates no returned inputs");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed cancellation is a healthy checkpoint");
        }
        private static void Line(Check c, GameFactory factory)
        {
            SimGame game = factory.NewGame(Request()); var command = new LayLineCommand { Line = "road", Tiles = new List<TileCoord> { new TileCoord(8, 27), new TileCoord(9, 27) } };
            CommandResult posted = TestKit.Order(game, 0, command); c.That(posted.Accepted, "player creates the existing line order");
            var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(plans);
            Entity marker = game.World.Entities.Get(plans.Find(p => p.Id == posted.Arg0).Target); ResourceId steel = game.World.Content.ResourceId("steel");
            Effect(game, marker, Amount(steel, Fix.FromInt(5))); int order = marker.Get<LineWork>().Order;
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order }).Accepted, "line cancellation uses ordinary engine work commands");
            c.That(game.World.Entities.Get(marker.Id) == null, "line cleanup shares the optional recovery owner");
            c.Equal(Fix.FromInt(55), Total(game, steel), "line cleanup cannot erase supplied leftovers");
            c.Equal((byte)0, ((TileMap)game.World.Space).Layer<ByteLayer>("road")[8, 27], "cancel before work lays no instant road");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "line recovery uses the existing stock/order codecs");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = factory.NewGame(Request()); ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            CommandResult placed = TestKit.Order(game, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(game.World.Space.Id, new TileCoord(10, 16)), Priority = 50 }); c.That(placed.Accepted, "player construction creates the existing site/order");
            Entity site = game.World.Entities.Get(placed.Entity); Order order = game.World.Services.Get<WorkBoard>().Order(site.Get<Site>().Order);
            Until(game, null, () => !order.Nodes[1].Progress.Equals(WorkProgress.Zero), "physical reach and actual erection progress");
            c.Equal(Fix.FromInt(20), Total(game, steel), "physical inputs are spent once at the authored work stage");
            c.Equal(new BigInteger(Fix.FromInt(30).Raw), Read(game).Totals(steel).ConsumedRaw, "construction input has native lifecycle accounting");
            c.That(game.Tick > order.CreatedTick, "hauling, travel and site preparation consume real game time");
            SimGame twin = factory.Load(game.Save());
            var extras = new Bill(new[] { new ResourceAmount(steel, Fix.FromInt(5)), new ResourceAmount(water, Fix.FromInt(2)) });
            Effect(game, site, extras); Effect(twin, twin.World.Entities.Get(site.Id), extras);
            Until(game, twin, () => Find(game, "tower") != null, "site completion and leftover recovery");
            c.Equal(game.Hash(), twin.Hash(), "saved working site and leftover recovery remain identical");
            c.Equal(Fix.FromInt(25), Total(game, steel), "completed structure does not recreate consumed construction materials");
            c.Equal(Fix.FromInt(12), Total(game, water), "completion conserves unused fluids");
            c.That(!site.Alive && site.Get<IInventory>().Stock.Count == 0, "site is truly emptied before destruction");
            Entity tower = Find(game, "tower");
            var claimOwner = new ReservationOwner("proof.demolition-claim", 1);
            IInventory towerStock = tower.Get<IInventory>();
            ReservationToken claim = towerStock.ReserveIn(claimOwner, steel, Fix.One);
            var demolition = new DemolishCommand { Target = tower.Id }; ulong before = game.Hash();
            c.Equal(ResourceTransactions.ActivePromises, game.Seat(0).Preview(demolition), "demolition preview protects current inbound commitments");
            c.Equal(before, game.Hash(), "demolition preview does not change work, stock, receipts or hash");
            CommandResult refused = TestKit.Order(game, 0, new DemolishCommand { Target = tower.Id });
            c.That(!refused.Accepted && refused.Refusal == ResourceTransactions.ActivePromises, "command rechecks the same removal refusal");
            c.That(tower.Alive && game.World.Services.Get<WorkBoard>().ActiveOn(tower.Id, "demolish:tower") == null,
                "refused demolition starts no physical order or structural yield");
            towerStock.ReleaseIn(claimOwner, claim); long demolitionTick = game.Tick;
            c.That(TestKit.Order(game, 0, new DemolishCommand { Target = tower.Id }).Accepted, "player orders physical demolition");
            c.That(tower.Alive, "demolition cannot complete at command submission"); twin = factory.Load(game.Save());
            Until(game, twin, () => !tower.Alive, "physical demolition");
            c.That(game.Tick > demolitionTick + 1, "machine demolition takes authored elapsed time");
            c.Equal(game.Hash(), twin.Hash(), "saved demolition continues through normal engine work");
            c.Equal(Fix.FromInt(40), Total(game, steel), "authored half recovery yields fifteen, separate from leftovers");
            ResourceFlowPage history = Read(game); Fix produced = Fix.Zero; int yieldLines = 0;
            foreach (var entry in history.Entries) if (entry.Item.Cause == new Code("demolish:tower"))
            { produced += entry.Item.Amount; yieldLines++; c.Equal(ResourceFlowKind.Produced, entry.Item.Kind, "structural release is an explicit authored lifecycle yield"); }
            c.Equal(Fix.FromInt(15), produced, "structural recovery cause identifies its exact bill fraction");
            c.Equal(2, yieldLines, "structural salvage over capacity splits without silent loss");
            Until(game, null, () => Colony.Mine(game, 0, game.Seat(0).View.Feature<Beep.SimEngine.Views.ConstructionView>().RecoveryContainer).Count == 0, "physical salvage delivery");
            c.Equal(Fix.FromInt(40), Find(game, "depot").Get<IInventory>().Amount(steel), "carriers physically return recovered material to the depot");
            Fluid(c, factory);
            PointRecoveryTests.Physical(c);
        }
        private static void Fluid(Check c, GameFactory factory)
        {
            SimGame game = factory.NewGame(Request()); ResourceId water = game.World.Content.ResourceId("water");
            c.That(TestKit.Order(game, 0, new ConstructCommand { Def = "fluid_unit", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(game.World.Space.Id, new TileCoord(10, 16)), Priority = 50 }).Accepted, "fluid construction is ordinary authored content");
            Until(game, null, () => Find(game, "fluid_unit") != null, "fluid structure completion"); Entity unit = Find(game, "fluid_unit");
            c.Equal(Fix.FromInt(5), Total(game, water), "real fluid input is consumed during construction");
            c.That(TestKit.Order(game, 0, new DemolishCommand { Target = unit.Id }).Accepted, "fluid structure demolition is ordinary work");
            SimGame twin = factory.Load(game.Save()); Until(game, twin, () => !unit.Alive, "fractional fluid salvage");
            c.Equal(Fix.FromRatio(15, 2), Total(game, water), "native non-discrete salvage preserves half of five, not integer truncation");
            c.Equal(game.Hash(), twin.Hash(), "fractional salvage saves and continues identically");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed fluid recovery is durable");
        }
    }
}
