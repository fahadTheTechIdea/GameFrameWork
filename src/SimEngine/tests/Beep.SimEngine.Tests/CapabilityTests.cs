using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class CapabilityTests
    {
        public static void Register()
        {
            TestRunner.Add("Capabilities: one owner and live indexed projections", Indexes);
            TestRunner.Add("Capabilities: port compatibility and protected reservations", Reservations);
            TestRunner.Add("Capabilities: independent worker executes standard physical construction", ExternalWorker);
            TestRunner.Add("Capabilities: independent endpoints haul and restore without Storage", ExternalPorts);
            TestRunner.Add("Capabilities: independent inventory transformation reserves output and restores", ExternalProduction);
            TestRunner.Add("Capabilities: incompatible outputs commit neither resources nor vehicle", IncompatibleProduction);
            TestRunner.Add("Capabilities: independent inventory survives physical demolition as salvage", ExternalSalvage);
            TestRunner.Add("Capabilities: independent mobility rolls out and moves through standard phases", ExternalMobility);
            TestRunner.Add("Capabilities: independent finite supplier buys only physically available cargo", ExternalSupplier);
            TestRunner.Add("Capabilities: owned supplier stock is transferred without charging its owner", OwnedSupplier);
            TestRunner.Add("Capabilities: invalid or ambiguous owners fail content resolution", Invalid);
        }

        private static void Indexes(Check c)
        {
            SimGame g = Colony.New();
            World w = g.World;
            ComponentList<IWorker> workers = w.Entities.Components<IWorker>();
            c.Equal(6, workers.Count, "interface index includes initial workers");
            var bucket = workers.Bucket(3, 0);
            Entity e = ((global::Beep.SimEngine.Maps.TileMap)w.Space).SpawnMobile(w, w.Content.Get<EntityDef>("module_worker"), new PlayerId(0), FixVec2.Centre(new TileCoord(12, 5)));
            c.That(e.Get<IWorker>() == e.Get<ContractWorker>(), "contract lookup returns the sole component, not a proxy");
            c.That(e.Get<IWorkerRoles>() == e.Get<IWorker>(), "inherited capability has the same owner");
            c.That(e.Get<Worker>() == null, "custom worker has no standard Worker component");
            w.Entities.Flush();
            c.Equal(7, workers.Count, "previously created contract index accepts new implementations");
            int bucketCount = bucket.Count;
            if (e.Id.Value % 3 == 0) c.That(bucketCount > 0 && bucket[bucketCount - 1].Entity == e, "cached spread bucket accepts the new worker");
            else c.Equal(0, CountEntity(bucket, e.Id), "spread bucket does not receive another bucket's worker");
            w.Despawn(e); w.Entities.Flush();
            c.Equal(6, workers.Count, "despawn updates the existing index");
            c.Equal(0, CountEntity(bucket, e.Id), "despawn updates cached buckets");
            c.Throws<ArgumentException>(() => w.Entities.Components<IEntityCapability>(), "ambiguous common root cannot be queried");
            c.Throws<ArgumentException>(() => workers[0].Entity.Get<IEntityCapability>(), "entity common-root query is also refused");
        }

        private static int CountEntity(System.Collections.Generic.IReadOnlyList<IWorker> parts, EntityId id)
        {
            int n = 0;
            for (int i = 0; i < parts.Count; i++) if (parts[i].Entity.Id == id) n++;
            return n;
        }

        private static void Reservations(Check c)
        {
            SimGame g = Colony.New();
            World w = g.World;
            Entity truck = w.Entities.Get(Colony.Mine(g, 0, "truck")[0].Id);
            IInventory inventory = truck.Get<IInventory>();
            IOutletPort outlet = truck.Get<IOutletPort>();
            IInletPort inlet = truck.Get<IInletPort>();
            ResourceId steel = w.Content.ResourceId("steel"), water = w.Content.ResourceId("water");
            c.That(outlet == inlet && inlet == inventory, "outlet and inlet resolve to the existing inventory");
            c.That(truck.Get<ITransporter>().Cargo == inventory, "transporter has no second cargo store");
            c.Equal(Fix.Zero, inlet.Receive(w.Content, water, Fix.FromInt(10)), "solid cargo refuses fluid through the inlet");
            var portOwner = new ReservationOwner("proof.ports", 1);
            ReservationToken inbound = inlet.ReserveIn(portOwner, steel, Fix.FromInt(8));
            c.Equal(Fix.FromInt(12), inlet.Receive(w.Content, steel, Fix.FromInt(20)), "receiving protects another delivery's reserved space");
            inlet.ReleaseIn(portOwner, inbound);
            c.Equal(Fix.FromInt(8), inlet.Receive(w.Content, steel, Fix.FromInt(20)), "only remaining capacity is accepted");
            outlet.ReserveOut(portOwner, steel, Fix.FromInt(15));
            c.Equal(Fix.FromInt(5), outlet.Take(steel, Fix.FromInt(20)), "export protects another pickup's reserved stock");
            c.Equal(Fix.FromInt(15), outlet.Amount(steel), "promised stock remains present");
            c.Equal(Fix.Zero, outlet.Take(steel, -Fix.One), "negative export cannot manufacture stock");
            c.Equal(Fix.Zero, inlet.Receive(w.Content, steel, -Fix.One), "negative receive cannot remove stock");
            g.RunTicks(1);
            SimGame restored = Colony.Factory().Load(g.Save());
            c.Equal(g.Hash(), restored.Hash(), "port stock and reservations have one save/hash path");
            c.Equal(Fix.FromInt(15), restored.World.Entities.Get(truck.Id).Get<IOutletPort>().ReservedOut(steel), "promises survive load");
        }

        private static void ExternalWorker(Check c)
        {
            SimGame g = Colony.New();
            World w = g.World;
            foreach (EntityView crane in Colony.Mine(g, 0, "crane")) w.Despawn(w.Entities.Get(crane.Id));
            Entity custom = ((global::Beep.SimEngine.Maps.TileMap)w.Space).SpawnMobile(w, w.Content.Get<EntityDef>("module_worker"), new PlayerId(0), FixVec2.Centre(new TileCoord(12, 5)));
            g.RunTicks(1);
            c.Equal(1, g.Seat(0).View.Feature<WorkView>().IdleWorkers(new WorkerRoleId("crane")), "management view recognizes the independent worker");
            CommandResult build = TestKit.Order(g, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(g.World.Space.Id, new TileCoord(10, 16)), Priority = 50 });
            c.That(build.Accepted, "ordinary public construction command accepts the plan");
            g.RunTicks(checked((int)TestGame.Time.Hours(1)));
            SimGame restored = Colony.Factory().Load(g.Save());
            c.Equal(g.Hash(), restored.Hash(), "independent worker's active job/state restore identically");
            bool sawWorking = false;
            int ticks = TestKit.RunUntil(g, () =>
            {
                if (g.Observer.TryGetEntity(custom.Id, out EntityView view) && view.State == w.Content.Get<IEntityStateDefinition>("working").State)
                    sawWorking = true;
                return Colony.Mine(g, 0, "tower").Count == 1;
            }, checked((int)TestGame.Time.Days(2)));
            c.Equal(1, Colony.Mine(g, 0, "tower").Count, "standard scheduler completes construction with no standard Worker");
            c.That(custom.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position != FixVec2.Centre(new TileCoord(12, 5)), "independent worker physically travelled");
            c.That(ticks + checked((int)TestGame.Time.Hours(1)) >= checked((int)TestGame.Time.Hours(2)), "authored work rate still consumes game time");
            c.That(sawWorking, "entity Update settles the extension's state source for public views");
            c.Equal(Fix.FromInt(20), Colony.Stock(g, Colony.Mine(g, 0, "depot")[0].Id, "steel"), "materials were physically supplied and consumed");
            restored.RunTicks(ticks);
            c.Equal(g.Hash(), restored.Hash(), "restored custom worker finishes through the same deterministic scheduler");
        }

        private static void ExternalPorts(Check c)
        {
            GameFactory f = Colony.Factory();
            SimGame a = f.NewGame(new NewGameRequest { Scenario = "colony", Seed = 8 });
            World w = a.World;
            Entity source = ((global::Beep.SimEngine.Maps.TileMap)w.Space).SpawnAt(w, w.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(12, 18));
            Entity dest = ((global::Beep.SimEngine.Maps.TileMap)w.Space).SpawnAt(w, w.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(28, 18));
            IOutletPort sp = source.Get<IOutletPort>();
            dest.Get<IOutletPort>().Take(w.Content.ResourceId("steel"), Fix.FromInt(30));
            a.RunTicks(1);
            c.That(source.Get<Storage>() == null && dest.Get<Storage>() == null, "endpoints are compiled outside framework and have no Storage");
            ResourceId steel = w.Content.ResourceId("steel");
            CommandResult route = TestKit.Order(a, 0, new AddRouteCommand { From = source.Id, To = dest.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(10), 40) });
            c.That(route.Accepted, "ordinary route command accepts contract-only endpoints");
            a.RunTicks(30);
            c.Equal(Fix.Zero, Colony.Stock(a, dest.Id, "steel"), "cargo cannot teleport to the far endpoint");
            SimGame b = f.Load(a.Save());
            c.Equal(a.Hash(), b.Hash(), "in-flight endpoint promises and custom state restore identically");
            a.RunTicks(checked((int)TestGame.Time.Days(1))); b.RunTicks(checked((int)TestGame.Time.Days(1)));
            c.Equal(a.Hash(), b.Hash(), "restored contract-based hauls continue identically");
            c.Equal(Fix.FromInt(30), Colony.Stock(a, dest.Id, "steel"), "all material is physically delivered");
            c.Equal(Fix.Zero, sp.Amount(steel), "no source material duplicated");
            c.Equal(Fix.Zero, sp.ReservedOut(steel), "finished exports release all promises");

            TestKit.Order(a, 0, new RemoveRouteCommand { Route = checked((int)route.Arg0) });
            EntityId depot = Colony.Mine(a, 0, "depot")[0].Id;
            TestKit.Order(a, 0, new ConfigureStockPolicyCommand { Store = depot, Resource = steel, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(50), Fix.FromInt(50), Fix.FromInt(50), 45, LogisticsReserveAccess.Protected)) });
            CommandResult stock = TestKit.Order(a, 0, new ConfigureStockPolicyCommand { Store = source.Id, Resource = steel, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(15), Fix.FromInt(15), Fix.FromInt(15), 45, LogisticsReserveAccess.Protected)) });
            c.That(stock.Accepted, "stock targets also accept a contract-only inlet");
            TestKit.RunUntil(a, () => Colony.Stock(a, source.Id, "steel") == Fix.FromInt(15), checked((int)TestGame.Time.Days(1)));
            c.Equal(Fix.FromInt(15), Colony.Stock(a, source.Id, "steel"), "automatic matching finds the independent outlet, not a concrete store");
            c.Equal(Fix.FromInt(15), Colony.Stock(a, dest.Id, "steel"), "supply was taken from the independent endpoint");
            c.Equal(Fix.FromInt(50), Colony.Stock(a, depot, "steel"), "the depot's own stock target is respected");
        }

        private static void Invalid(Check c)
        {
            string ambiguous = Colony.Defs.Replace("\"single_resource_port\": { \"resource\": \"steel\", \"capacity_policy\": \"fixture.count.80\", \"initial\": 30 }",
                "\"single_resource_port\": { \"resource\": \"steel\", \"capacity_policy\": \"fixture.count.80\", \"initial\": 30 }, \"storage\": { \"capacity_policy\": \"fixture.count.10\" }");
            c.Throws<ContentException>(() => Colony.Factory(ambiguous).NewGame(new NewGameRequest { Scenario = "colony" }), "two owners of the same endpoint fail before play");
            string noCargo = Colony.Defs.Replace("\"storage\": { \"capacity_policy\": \"fixture.count.20\", \"public\": false }, ", "");
            c.Throws<ContentException>(() => Colony.Factory(noCargo).NewGame(new NewGameRequest { Scenario = "colony" }), "transporter without cargo inventory fails before play");
            string conflictingDefinition = Colony.Defs.Replace("\"single_resource_port\": { \"resource\": \"steel\", \"capacity_policy\": \"fixture.count.80\", \"initial\": 30 }",
                "\"single_resource_port\": { \"resource\": \"steel\", \"capacity_policy\": \"fixture.count.80\", \"initial\": 30 }, \"inlet_declaration\": {}");
            c.Throws<ContentException>(() => Colony.Factory(conflictingDefinition).NewGame(new NewGameRequest { Scenario = "colony" }), "two definition-contract owners fail even without competing runtime ports");
            string noInventory = Colony.Defs.Replace("\"storage\": { \"capacity_policy\": \"fixture.count.100\", \"public\": false }, ", "");
            c.Throws<ContentException>(() => Colony.Factory(noInventory).NewGame(new NewGameRequest { Scenario = "colony" }), "transformer with no inventory refuses content resolution");
            string unknownOutput = Colony.Defs.Replace("\"entity_output\": \"truck\"", "\"entity_output\": \"missing\"");
            c.Throws<ContentException>(() => Colony.Factory(unknownOutput).NewGame(new NewGameRequest { Scenario = "colony" }), "unknown entity output refuses content resolution");
            string missingMobility = Colony.Defs.Replace(", \"lane_mover\": { \"class\": \"lane\", \"speed\": 6, \"states\": { \"moving\": \"moving\", \"stopped\": \"stopped\" } }", "")
                .Replace("\"entity_output\": \"truck\"", "\"entity_output\": \"module_cart\"");
            c.Throws<ContentException>(() => Colony.Factory(missingMobility).NewGame(new NewGameRequest { Scenario = "colony" }), "vehicle transformation cannot silently substitute land movement when its mobility is missing");
            string noSupplierStock = Colony.Defs.Replace("\"single_resource_port\": { \"resource\": \"steel\", \"capacity_policy\": \"fixture.count.40\", \"initial\": 25 }, ", "");
            c.Throws<ContentException>(() => Colony.Factory(noSupplierStock).NewGame(new NewGameRequest { Scenario = "colony" }), "finite supplier without its inventory refuses content resolution");
        }

        private static void ExternalMobility(Check c)
        {
            string definitions = Colony.Defs.Replace("\"entity_output\": \"truck\"", "\"entity_output\": \"module_cart\"");
            GameFactory factory = Colony.Factory(definitions);
            SimGame g = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            Entity transformer = g.World.Entities.Get(Colony.Mine(g, 0, "factory")[0].Id);
            c.That(TestKit.Order(g, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = transformer.Id, Transformation = "build_truck", Count = 1 }).Accepted, "transformation accepts an independently defined vehicle");
            g.RunTicks(checked((int)TestGame.Time.Hours(1)));
            c.Equal(0, Colony.Mine(g, 0, "module_cart").Count, "independent vehicle still needs physical transformation time");
            TestKit.RunUntil(g, () => Colony.Mine(g, 0, "module_cart").Count == 1, checked((int)TestGame.Time.Days(1)));
            Entity cart = g.World.Entities.Get(Colony.Mine(g, 0, "module_cart")[0].Id);
            c.That(cart.Get<Mover>() == null && cart.Get<IMobility>() == cart.Get<LaneMobility>(), "vehicle has only its external mobility implementation");
            c.Equal("lane", cart.Def.Component<IMobilityDefinition>().MovementClass, "rollout reads authored navigation contract");
            c.Equal(9, cart.Get<global::Beep.SimEngine.Maps.ITilePresence>().Tile.Y, "rollout respects lane navigation, not the old land fallback");
            FixVec2 start = cart.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position;
            c.That(TestKit.Order(g, 0, new MoveVehicleCommand { Vehicle = cart.Id, To = new TileCoord(15, 9) }).Accepted, "ordinary move command drives independent mobility");
            c.That(cart.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position != start && cart.Get<global::Beep.SimEngine.Maps.ITilePresence>().Tile.X != 15, "first Update advances physical travel but cannot teleport to the destination");
            c.That(g.Observer.TryGetEntity(cart.Id, out EntityView moving) && moving.State == g.World.Content.Get<IEntityStateDefinition>("moving").State, "entity Update owns the independent mobility's public moving state");
            SimGame restored = factory.Load(g.Save());
            c.Equal(g.Hash(), restored.Hash(), "independent mobility position/goal/state restore identically");
            int elapsed = TestKit.RunUntil(g, () => cart.Get<IMobility>().Status == MoveStatus.Arrived, checked((int)TestGame.Time.Hours(3)), 1);
            c.That(elapsed >= checked((int)TestGame.Time.Hours(1)), "authored travel speed consumes game time");
            c.Equal(new TileCoord(15, 9), cart.Get<global::Beep.SimEngine.Maps.ITilePresence>().Tile, "standard phase invokes independent movement to its destination");
            restored.RunTicks(elapsed);
            c.Equal(g.Hash(), restored.Hash(), "save/load resumes independent movement identically");
            c.That(!TestKit.Order(g, 0, new MoveVehicleCommand { Vehicle = cart.Id, To = new TileCoord(15, 10) }).Accepted, "command preview refuses a destination outside the authored lane");
            c.That(TestKit.Order(g, 0, new MoveVehicleCommand { Vehicle = cart.Id, To = new TileCoord(20, 9) }).Accepted, "a second reachable destination uses the same command path");
            ((global::Beep.SimEngine.Maps.TileMap)g.World.Space).Layer<Beep.SimEngine.Maps.ByteLayer>("water").Set(new TileCoord(20, 9), 1);
            g.RunTicks(1);
            c.Equal(MoveStatus.Unreachable, cart.Get<IMobility>().Status, "provider reports a destination outside its operating lane as unreachable");
            c.That(g.Observer.TryGetEntity(cart.Id, out EntityView stopped) && stopped.State == g.World.Content.Get<IEntityStateDefinition>("stopped").State, "entity Update projects stopped state without host logic");
        }

        private static void ExternalSupplier(Check c)
        {
            GameFactory factory = Colony.Factory();
            SimGame g = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            World w = g.World;
            // The market is neutral; remove it explicitly through the engine fixture's neutral lookup.
            foreach (Entity entity in w.Entities.All) if (entity.Def.Id == "market") w.Despawn(entity);
            ResourceId steel = w.Content.ResourceId("steel");
            w.Entities.Get(Colony.Mine(g, 0, "depot")[0].Id).Get<IInventory>().Remove(steel, Fix.FromInt(50));
            Entity source = ((global::Beep.SimEngine.Maps.TileMap)w.Space).SpawnAt(w, w.Content.Get<EntityDef>("module_supplier"), PlayerId.None, new TileCoord(28, 18));
            Entity dest = ((global::Beep.SimEngine.Maps.TileMap)w.Space).SpawnAt(w, w.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(12, 18));
            dest.Get<IInventory>().Remove(steel, Fix.FromInt(30));
            var supplierOwner = new ReservationOwner("proof.supplier", 1);
            ReservationToken protectedStock = source.Get<IOutletPort>().ReserveOut(supplierOwner, steel, Fix.FromInt(10));
            c.That(source.Get<Supplier>() == null && source.Get<Storage>() == null, "supplier policy and its sole inventory are independent implementations");
            c.Equal(Fix.FromInt(15), source.Get<ISupplier>().Offer(steel, Fix.FromInt(100)), "finite offer protects promised stock and cannot fabricate cargo");
            c.Equal(Fix.Zero, source.Get<ISupplier>().Offer(w.Content.ResourceId("water"), Fix.One), "supplier refuses incompatible resource");
            Fix before = w.Services.Get<IEconomy>().Cash(new PlayerId(0));
            IEconomy economy = w.Services.Get<IEconomy>();
            c.That(economy.TryCharge(new PlayerId(0), before, BusinessModeRules.LedgerPurchase), "fixture can exhaust the buyer's declared economy");
            ReservationToken unpaid = source.Get<ISupplier>().ReserveSupply(new ReservationOwner("proof.unpaid", 1), steel, Fix.One);
            ulong refusedHash = g.Hash();
            c.Throws<InvalidOperationException>(() => source.Get<ISupplier>().SupplyReserved(w, new PlayerId(0), new ReservationOwner("proof.unpaid", 1), unpaid, Fix.One, null), "raw endpoint fulfillment is not a purchase API");
            c.Equal(Fix.Zero, economy.Cash(new PlayerId(0)), "refused supply cannot debit an insolvent buyer");
            c.Equal(Fix.FromInt(25), source.Get<IInventory>().Amount(steel), "refused supply leaves the sole stock authority unchanged");
            c.Equal(refusedHash, g.Hash(), "refused purchase changes no authoritative state");
            source.Get<ISupplier>().ReleaseSupply(new ReservationOwner("proof.unpaid", 1), unpaid);
            economy.Credit(new PlayerId(0), before, BusinessModeRules.LedgerPurchase);
            c.That(TestKit.Order(g, 0, new ConfigureStockPolicyCommand { Store = dest.Id, Resource = steel, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(25), Fix.FromInt(25), Fix.FromInt(25), 45, LogisticsReserveAccess.Protected)) }).Accepted, "public stock policy requests paid supply");
            g.RunTicks(30);
            c.Equal(Fix.Zero, Colony.Stock(g, dest.Id, "steel"), "purchased material has not teleported to its destination");
            c.Equal(before, w.Services.Get<IEconomy>().Cash(new PlayerId(0)), "planning and travel do not pay before physical pickup");
            c.That(HasPurchase(w), "existing job authority records paid-source intent");
            byte[] save = g.Save();
            c.Equal(EngineInfo.SaveFormat, SaveGame.Describe(save).Format, "jobs, controls and codec declarations use the current breaking save layout");
            SimGame restored = factory.Load(save);
            c.Equal(g.Hash(), restored.Hash(), "in-flight purchase and inventory promises have one restoration path");
            int elapsed = TestKit.RunUntil(g, () => Colony.Stock(g, dest.Id, "steel") == Fix.FromInt(15), checked((int)TestGame.Time.Days(1)));
            restored.RunTicks(elapsed);
            c.Equal(g.Hash(), restored.Hash(), "finite paid hauls continue identically after load");
            c.Equal(Fix.FromInt(15), Colony.Stock(g, dest.Id, "steel"), "only unpromised finite material arrives");
            c.Equal(before - Fix.FromInt(105), w.Services.Get<IEconomy>().Cash(new PlayerId(0)), "payment matches actual physical supply, not requested demand");
            c.Equal(Fix.FromInt(10), source.Get<IInventory>().Amount(steel), "protected outgoing stock remains at the source");
            source.Get<IOutletPort>().ReleaseOut(supplierOwner, protectedStock);
            TestKit.RunUntil(g, () => Colony.Stock(g, dest.Id, "steel") == Fix.FromInt(25), checked((int)TestGame.Time.Days(1)));
            c.Equal(Fix.FromInt(25), Colony.Stock(g, dest.Id, "steel"), "existing matching retries supply after reservation release");
            c.Equal(before - Fix.FromInt(175), w.Services.Get<IEconomy>().Cash(new PlayerId(0)), "all 25 resources were paid once");
            c.Equal(Fix.Zero, source.Get<IInventory>().Amount(steel), "source cannot duplicate finite stock");
            c.Equal(Fix.Zero, dest.Get<IInletPort>().ReservedIn(steel), "finished/failed purchases release destination promises");
        }

        private static bool HasPurchase(World world)
        {
            foreach (Job job in world.Services.Get<WorkBoard>().Jobs) if (job.IsLive && job.Purchase) return true;
            return false;
        }

        private static void OwnedSupplier(Check c)
        {
            SimGame g = Colony.New();
            World w = g.World;
            ResourceId steel = w.Content.ResourceId("steel");
            w.Entities.Get(Colony.Mine(g, 0, "depot")[0].Id).Get<IInventory>().Remove(steel, Fix.FromInt(50));
            Entity source = ((global::Beep.SimEngine.Maps.TileMap)w.Space).SpawnAt(w, w.Content.Get<EntityDef>("module_supplier"), new PlayerId(0), new TileCoord(12, 18));
            Entity dest = ((global::Beep.SimEngine.Maps.TileMap)w.Space).SpawnAt(w, w.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(28, 18));
            dest.Get<IInventory>().Remove(steel, Fix.FromInt(30));
            Fix before = w.Services.Get<IEconomy>().Cash(new PlayerId(0));
            c.That(TestKit.Order(g, 0, new ConfigureStockPolicyCommand { Store = dest.Id, Resource = steel, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(20), Fix.FromInt(20), Fix.FromInt(20), 45, LogisticsReserveAccess.Protected)) }).Accepted, "owned supply is requested through standard stock command");
            TestKit.RunUntil(g, () => Colony.Stock(g, dest.Id, "steel") == Fix.FromInt(20), checked((int)TestGame.Time.Days(1)));
            c.Equal(Fix.FromInt(20), Colony.Stock(g, dest.Id, "steel"), "owned supply is physically transferred");
            c.Equal(before, w.Services.Get<IEconomy>().Cash(new PlayerId(0)), "supplier policy cannot turn an owned-stock haul into a paid purchase");
            c.Equal(Fix.FromInt(5), source.Get<IInventory>().Amount(steel), "one shared source inventory loses exactly the delivered stock");
            c.Equal(Fix.Zero, source.Get<IOutletPort>().ReservedOut(steel), "owned stock reservations are released normally");
            c.Equal(Fix.Zero, dest.Get<IInletPort>().ReservedIn(steel), "owned haul destination promises are released normally");
            c.That(!HasPurchase(w), "owned transfer never becomes a purchase job");
        }

        private static string IndependentFactory(string outputs)
        {
            JsonValue root = Json.Parse(Colony.Defs), factory = null, recipe = null;
            var entities = root.Get("entities"); var recipes = root.Get("transformations");
            for (int i = 0; i < entities.Count; i++)
                if (entities[i].Get("id").Text == "factory")
                {
                    if (factory != null) throw new InvalidOperationException("independent fixture has duplicate factory");
                    factory = entities[i];
                }
            for (int i = 0; i < recipes.Count; i++)
                if (recipes[i].Get("id").Text == "build_truck")
                {
                    if (recipe != null) throw new InvalidOperationException("independent fixture has duplicate build_truck");
                    recipe = recipes[i];
                }
            if (factory == null || recipe == null) throw new InvalidOperationException("independent fixture requires its actual factory and build_truck");
            var components = factory.Get("components");
            if (components.Get("storage") == null || components.Get("single_resource_port") != null)
                throw new InvalidOperationException("independent fixture requires exactly the original storage owner before authoring");
            components.Remove("storage");
            components.Set("single_resource_port", Json.Parse("{\"resource\":\"steel\",\"capacity_policy\":\"fixture.count.40\",\"initial\":30,\"public\":false,\"evacuate\":false}"));
            JsonValue outputBill = Json.Parse("{" + outputs + "}"), outputPorts = JsonValue.NewObject();
            for (int i = 0; i < outputBill.Count; i++) outputPorts.Set(outputBill.KeyAt(i), JsonValue.Of("material.output"));
            recipe.Set("outputs", outputBill); recipe.Set("output_ports", outputPorts);
            var text = new System.Text.StringBuilder(); root.WriteCanonical(text); return text.ToString();
        }

        internal static void ExternalProduction(Check c)
        {
            GameFactory factory = Colony.Factory(IndependentFactory("\"steel\": 12"));
            SimGame g = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            World w = g.World;
            Entity e = w.Entities.Get(Colony.Mine(g, 0, "factory")[0].Id);
            IInventory inventory = e.Get<IInventory>();
            ResourceId steel = w.Content.ResourceId("steel");
            c.That(e.Get<Storage>() == null && inventory == e.Get<SingleResourcePort>(), "transformer uses one independently compiled inventory, no Storage or proxy");
            WorkView view = g.Seat(0).View.Feature<WorkView>();
            c.That(g.Seat(0).View.Feature<InventoryView>().StoreAccepts(e.Def.Id, steel, Code.None), "prototype preview recognizes independent inlet definition");
            c.That(!g.Seat(0).View.Feature<InventoryView>().StoreAccepts(e.Def.Id, w.Content.ResourceId("water"), Code.None), "prototype preview preserves authored compatibility");
            c.Throws<ArgumentException>(() => e.Def.Component<IComponentDefinition>(), "ambiguous definition root cannot be queried");
            var outputOwner = new ReservationOwner("proof.transformation", 1);
            ReservationToken outputSpace = inventory.ReserveIn(outputOwner, steel, Fix.FromInt(9));
            c.That(TestKit.Order(g, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = e.Id, Transformation = "build_truck", Count = 1 }).Accepted, "ordinary transformation command accepts independent inventory");
            g.RunTicks(checked((int)TestGame.Time.Hours(1)));
            c.That(g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(e.Id, new Code("transformation"), out TransformationSnapshot working) && working.Phase == TransformationPhase.Working, "engine reports timed transformation, not instantaneous output");
            c.Equal(Fix.FromInt(20), inventory.Amount(steel), "input bill is consumed exactly once");
            SimGame restored = factory.Load(g.Save());
            c.Equal(g.Hash(), restored.Hash(), "custom inventory, promises and active transformation restore identically");
            g.RunTicks(checked((int)TestGame.Time.Hours(3))); restored.RunTicks(checked((int)TestGame.Time.Hours(3)));
            c.Equal(g.Hash(), restored.Hash(), "restored transformation reaches the same reservation blocker");
            c.That(g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(e.Id, new Code("transformation"), out TransformationSnapshot blocked), "owner sees transformation facts");
            c.Equal(TransformerManager.BlockerOutput, blocked.Blocker, "inbound promise protects room from transformation");
            c.Equal(TransformationPhase.Blocked, blocked.Phase, "complete transformation waits without losing its output");
            c.Equal(Fix.FromInt(20), inventory.Amount(steel), "blocked delivery inserts no partial output");
            c.Equal(2, Colony.Mine(g, 0, "truck").Count, "blocked resource output also withholds vehicle");
            c.That(!g.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(e.Id, new Code("transformation"), out _), "rival cannot inspect private transformation");
            ulong retained = g.Hash(); SimGame cancelled = factory.Load(g.Save());
            c.That(TestKit.Order(cancelled, 0, new CancelTransformationCommand { Transformer = e.Id, Capability = new Code("transformation"), Index = 0 }).Accepted,
                "completed capacity-blocked work remains cancellable through ordinary commands");
            TestKit.RunUntil(cancelled, () => TransformerTests.State(cancelled, "factory").QueueCount == 0, checked((int)TestGame.Time.Hours(1)));
            c.Equal(Fix.FromInt(30), cancelled.World.Entities.Get(e.Id).Get<IInventory>().Amount(steel), "cancelled blocked conversion physically returns only its original raw inputs");
            c.Equal(2, Colony.Mine(cancelled, 0, "truck").Count, "cancelled blocked conversion never publishes its vehicle");
            c.Equal(cancelled.Hash(), factory.Load(cancelled.Save()).Hash(), "cancelled actual WIP and original inventory save consistently");
            c.Equal(retained, g.Hash(), "cancellation in another saved session leaves original blocked execution unchanged");
            inventory.ReleaseIn(outputOwner, outputSpace);
            restored.World.Entities.Get(e.Id).Get<IInventory>().ReleaseIn(outputOwner, outputSpace);
            g.RunTicks(1); restored.RunTicks(1);
            c.Equal(g.Hash(), restored.Hash(), "reservation release completes identically after load");
            c.Equal(Fix.FromInt(32), inventory.Amount(steel), "whole transformation output commits after room becomes available");
            c.Equal(3, Colony.Mine(g, 0, "truck").Count, "same commit rolls out exactly one vehicle");
            c.That(g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(e.Id, new Code("transformation"), out TransformationSnapshot done) && done.QueueCount == 0 && done.Phase == TransformationPhase.Idle, "engine reports finished queue");
            c.Equal(TransformationPhase.Blocked, blocked.Phase, "previous transformation snapshot remains detached after Update");
            g.RunTicks(checked((int)TestGame.Time.Hours(1)));
            c.Equal(Fix.FromInt(32), inventory.Amount(steel), "finished output is not delivered again");
        }

        internal static void IncompatibleProduction(Check c)
        {
            GameFactory Authored(string outputs, InitializeProbe initialize)
            {
                JsonValue authored = Json.Parse(IndependentFactory(outputs));
                authored.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.output-validation"));
                var module = new ProbeModule("proof.output-validation", new ModuleVersion(1,0,0), compose:composer=>composer.AddSystem(initialize,9000));
                return Colony.Factory(authored.ToString(), additionalModules:new GameModule[]{module},
                    moduleDeclarations:new[]{"{\"id\":\"proof.output-validation\",\"version\":\"1.0.0\",\"documents\":[\"output-validation.json\"]}"},
                    moduleDocuments:new System.Collections.Generic.Dictionary<string,string>{{"output-validation.json","{}"}});
            }
            var ready = new InitializeProbe("proof.output-validation.initialize");
            var factory = Authored("\"steel\":5",ready); var healthy = factory.NewGame(new NewGameRequest{Scenario="colony"});
            c.Equal(1,ready.Initializations,"valid named output really activates the declared initialization witness");
            ulong retained = healthy.Hash(); var denied = new InitializeProbe("proof.output-validation.initialize");
            var invalid = Authored("\"steel\":5,\"water\":1",denied);
            for(int attempt=0;attempt<2;attempt++)
            {
                ContentException refusal=null;
                try{invalid.NewGame(new NewGameRequest{Scenario="colony"});}catch(ContentException error){refusal=error;}
                c.That(refusal!=null&&refusal.Message.Contains("authored resource port binding refused: " + ResourcePortCodes.DefinitionMissing.Id),"permanently incompatible output refuses at the actual typed port validator: " + refusal?.Message);
                c.Equal(0,denied.Initializations,"incompatible output admits no initialized world or partial compatible result");
                c.Equal(retained,healthy.Hash(),"repeated invalid content cannot mutate a separate valid execution");
            }
            c.Equal(Fix.FromInt(30),FindFactoryInventory(healthy).Amount(healthy.World.Content.ResourceId("steel")),"invalid output selection inserts no compatible subset into actual independent buffer");
            c.Equal(2,Colony.Mine(healthy,0,"truck").Count,"invalid output selection creates no extra vehicle");
            c.Equal(0L,healthy.Tick,"content compatibility validation never advances game time");
            c.Equal(healthy.Hash(),factory.Load(healthy.Save()).Hash(),"valid independent inventory remains durable after rejected composition");
            IInventory FindFactoryInventory(SimGame game)=>game.World.Entities.Get(Colony.Mine(game,0,"factory")[0].Id).Get<IInventory>();
        }

        private static void ExternalSalvage(Check c)
        {
            JsonValue authored = Json.Parse(IndependentFactory("\"steel\":12"));
            JsonValue entities = authored.Get("entities"), prototype = null;
            for (int i = 0; i < entities.Count; i++)
            {
                string id = entities[i].Get("id").Text;
                if (id == "site:factory") throw new InvalidOperationException("salvage fixture already has its site");
                if (id != "factory") continue;
                if (prototype != null) throw new InvalidOperationException("salvage fixture has duplicate factory");
                prototype = entities[i];
            }
            if (prototype == null || prototype.Get("components").Get("construction") != null)
                throw new InvalidOperationException("salvage fixture requires its original non-constructible factory");
            prototype.Get("components").Set("construction",Json.Parse("{\"site\":\"site:factory\",\"activity\":\"factory.build-work\",\"demolish\":{\"activity\":\"factory.removal-work\",\"salvage\":{\"steel\":3}},\"states\":{\"site\":\"under-construction\"}}"));
            if (authored.Get("target_resource_requirements") != null) throw new InvalidOperationException("salvage fixture requires its explicit additional input declaration");
            authored.Set("target_resource_requirements",Json.Parse("[{\"id\":\"module.assemble.inputs\",\"port\":null,\"use\":\"consume\",\"quantities\":{\"steel\":6}}]"));
            JsonValue workflows = authored.Get("activities") ?? throw new InvalidOperationException("salvage fixture requires existing authored activities");
            workflows.Add(Json.Parse("{\"id\":\"factory.build-work\",\"automatic_supply\":true,\"player_orderable\":false,\"priority\":50,\"nodes\":[{\"id\":\"assemble\",\"executor\":\"work.staffed\",\"roles\":[\"crane\"],\"duration\":{\"numerator\":1,\"denominator\":1,\"unit\":\"hour\"},\"requirement\":\"module.assemble.inputs\"}]}"));
            workflows.Add(Json.Parse("{\"id\":\"factory.removal-work\",\"automatic_supply\":false,\"player_orderable\":false,\"priority\":40,\"nodes\":[{\"id\":\"demolish\",\"executor\":\"work.staffed\",\"roles\":[\"crane\"],\"duration\":{\"numerator\":1,\"denominator\":1,\"unit\":\"hour\"}}]}"));
            JsonValue factorySite = Json.Parse("{\"id\":\"site:factory\",\"kind\":\"structure\",\"components\":{\"storage\":{\"capacity_policy\":\"fixture.count.1000\",\"initial_stock\":{},\"public\":false},\"site\":{\"target\":\"factory\"}}}");
            factorySite.Get("components").Set("tile_presence",prototype.Get("components").Get("tile_presence").Clone());
            entities.Add(factorySite);
            // Retain this case's actual movement/yield facts; no alternate stock accounting.
            var flows = Json.Parse(FrameworkProofContent.Read("resource-flows.json"));
            flows.Get("resource_flow_journals")[0].Set("retained_records_per_company",JsonValue.Number("128"));
            authored.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
            GameFactory factory = Colony.Factory(authored.ToString(),flowDefinitions:flows.ToString());
            SimGame g = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            SimGame idle = factory.NewGame(new NewGameRequest { Scenario = "colony" }); ulong isolated = idle.Hash();
            Entity e = g.World.Entities.Get(Colony.Mine(g, 0, "factory")[0].Id);
            var steel = g.World.Content.ResourceId("steel"); var inventory = e.Get<IInventory>();
            EntityId depot = Colony.Mine(g,0,"depot")[0].Id; var at = e.Get<Beep.SimEngine.Maps.ITilePresence>().Tile;
            string container = g.Seat(0).View.Feature<ConstructionView>().RecoveryContainer;
            c.That(e.Get<Storage>() == null && ReferenceEquals(inventory,e.Get<SingleResourcePort>()),"demolition uses the actual independent sole inventory");
            c.That(e.Def.Component<IConstructionDefinition>().Demolition != null,"actual factory has the authored physical removal contract");
            var privateRows=new List<Facet>();g.Seat(1).View.Inspect(e.Id,privateRows);
            c.That(!privateRows.Any(facet=>facet.Key==Storage.FacetStock),"foreign company cannot inspect the actual private source inventory");
            Fix Total(SimGame game) => game.World.Entities.All.Where(item=>item.Alive&&item.Owner==new PlayerId(0))
                .Aggregate(Fix.Zero,(sum,item)=>sum+(item.Get<IInventory>()?.Amount(steel)??Fix.Zero));
            c.Equal(Fix.FromInt(80),Total(g),"only authored thirty stored and fifty depot steel exist");
            var limits = new ReplayLimits(4*1024*1024,6000); using var recording = g.RecordReplay(limits);
            var command = new DemolishCommand {Target=e.Id}; ulong before = g.Hash(); long started = g.Tick;
            c.Equal(Code.None,g.Seat(0).Preview(command),"valid independent inventory demolition preview");
            c.Equal(before,g.Hash(),"preview creates no work, pile or resource prefix");
            c.That(TestKit.Order(g,0,command).Accepted,"demolition of independent inventory uses normal work command");
            c.That(e.Alive&&inventory.Amount(steel)==Fix.FromInt(30),"submission cannot instantly remove structure or cargo");
            Order order = g.World.Services.Get<WorkBoard>().ActiveOn(e.Id,"demolish:factory");
            if(order==null)throw new InvalidOperationException("actual demolition order missing");
            void Step(SimGame twin=null)
            {
                g.Update();twin?.Update();
                if(g.Fault!=null||twin?.Fault!=null)throw new InvalidOperationException("independent salvage fault "+g.Fault+"/"+twin?.Fault);
                c.Equal(Fix.FromInt(e.Alive?80:83),Total(g),"every Update conserves stored cargo plus only earned structural yield");
                if(twin!=null)c.Equal(g.Hash(),twin.Hash(),"saved actual work, stock, receipts and hauling advance identically");
            }
            void Until(Func<bool> done,string stage,SimGame twin=null)
            {
                var elapsed=System.Diagnostics.Stopwatch.StartNew();int updates=0;
                while(!done()&&updates<checked((int)TestGame.Time.Days(1))&&elapsed.Elapsed<TimeSpan.FromSeconds(20)){Step(twin);updates++;}
                if(!done())throw new InvalidOperationException(stage+" clock="+g.Tick+" updates="+updates+" elapsed-ms="+elapsed.ElapsedMilliseconds+
                    " order="+order.State+" nodes="+string.Join(";",order.Nodes.Select(node=>node.Id+"/"+node.State+"/"+node.Blocker))+" stock="+Total(g));
            }
            Until(()=>order.Nodes[0].Progress.WholeTicks>0,"actual crane travel and partial removal");
            c.That(e.Alive&&order.Nodes[0].Progress.WholeTicks<checked((int)TestGame.Time.Hours(1)),"checkpoint is genuinely inside positive-time removal");
            SimGame restored = factory.Load(g.Save());c.Equal(g.Hash(),restored.Hash(),"partial physical demolition checkpoint");
            g.Clock.Pause();restored.Clock.Pause();before=g.Hash();
            c.Equal(0,g.Update(),"pause stops physical demolition");c.Equal(0,restored.Update(),"saved twin uses the same paused clock");
            c.Equal(before,g.Hash(),"paused removal moves no resources or machines");c.Equal(g.Hash(),restored.Hash(),"paused saved actual owners agree");
            g.Clock.Resume();restored.Clock.Resume();
            Until(()=>!e.Alive,"earned machine demolition",restored);
            c.That(g.Tick-started>=checked((int)TestGame.Time.Hours(1)),"physical worker earns the entire authored hour");
            c.Equal(Fix.Zero,inventory.Amount(steel),"removed independent source is emptied, not cloned");
            c.Equal(0,inventory.Reservations.Count,"removed source retains no dangling stock promises");
            var piles=Colony.Mine(g,0,container);
            c.Equal(2,piles.Count,"existing stock evacuation and authored structural yield have separate native packing receipts");
            c.That(piles.Select(pile=>Colony.Stock(g,pile.Id,"steel")).OrderBy(value=>value).SequenceEqual(new[]{Fix.FromInt(3),Fix.FromInt(30)}),
                "all thirty stored steel and exactly three structural steel remain in original containers");
            foreach(var pile in piles)c.Equal(at,g.World.Entities.Get(pile.Id).Get<Beep.SimEngine.Maps.ITilePresence>().Tile,"recovery is physically at the demolished site, never the remote depot");
            c.Equal(Fix.FromInt(50),Colony.Stock(g,depot,"steel"),"earned demolition does not instantly deliver salvage");
            var history=g.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0);
            var evacuated=history.Entries.Where(entry=>entry.Item.Source.Endpoint==ResourceEndpointId.ForEntity(e.Id)).ToArray();
            c.That(evacuated.Length==1&&evacuated[0].Item.Kind==ResourceFlowKind.StockTransfer&&evacuated[0].Item.Amount==Fix.FromInt(30),
                "actual independent stock is one transfer, not fabricated production");
            var yields=history.Entries.Where(entry=>entry.Item.Cause==new Code("demolish:factory")).ToArray();
            c.That(yields.Length==1&&yields[0].Item.Kind==ResourceFlowKind.Produced&&yields[0].Item.Amount==Fix.FromInt(3),"only authored structural salvage is created once");
            // This scenario has no automatic-evacuation policy. The player requests recovery through
            // the existing stock policy; matching, carrier choice and all movement remain engine-owned.
            ConfigureStockPolicyCommand Demand()=>new ConfigureStockPolicyCommand {Store=depot,Resource=steel,
                Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,Fix.FromInt(83),Fix.FromInt(83),Fix.FromInt(83),45,LogisticsReserveAccess.Protected))};
            before=g.Hash();c.Equal(Code.None,g.Seat(0).Preview(Demand()),"ordinary depot stock policy requests the actual recovered material");
            c.Equal(before,g.Hash(),"recovery demand preview moves no stock or cargo");
            c.That(TestKit.Order(g,0,Demand()).Accepted&&TestKit.Order(restored,0,Demand()).Accepted,"both saved sessions accept the same player stock request");
            c.Equal(g.Hash(),restored.Hash(),"saved stock demand and physical dispatch match");
            var jobs=new List<WorkJobView>();
            bool Loaded(){g.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);return jobs.Any(job=>job.Loaded.IsPositive);}
            Until(Loaded,"real transporter pickup of recovered stock",restored);
            restored=factory.Load(g.Save());c.Equal(g.Hash(),restored.Hash(),"saved loaded salvage preserves real cargo and endpoint receipts");
            Until(()=>Colony.Stock(g,depot,"steel")==Fix.FromInt(83),"physical salvage delivery to original depot",restored);
            Until(()=>Colony.Mine(g,0,container).Count==0,"empty recovery container cleanup",restored);
            for(int i=0;i<10;i++)Step(restored);
            c.Equal(Fix.FromInt(83),Colony.Stock(g,depot,"steel"),"stored cargo and structural yield are delivered exactly once");
            c.Equal(g.Hash(),factory.Load(g.Save()).Hash(),"completed original recovery state remains durable");
            c.Equal(ResourceFlowReadStatus.NotOwned,g.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0).Status,
                "foreign company receives no private company recovery accounting");
            c.Equal(isolated,idle.Hash(),"other session receives no demolition, salvage or delivery");
            var replay=new ReplayVerifier().Verify(factory,recording.Finish(),limits);
            c.That(replay.Success&&replay.Game?.Hash()==g.Hash(),"ordinary demolition, pause and physical salvage replay: "+replay.Failure+"/"+replay.Detail);
        }
    }
}
