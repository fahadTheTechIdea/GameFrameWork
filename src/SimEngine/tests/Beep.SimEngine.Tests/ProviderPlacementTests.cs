using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class ProviderPlacementTests
    {
        public static void Register()
        {
            TestRunner.Add("Provider placement: non-tile timed replacement and saved continuation", Timed);
            TestRunner.Add("Provider placement: refusal parity, changed geometry and initialization rollback", Refusals);
        }
        private static string Insert(string json, string key, string entries)
        {
            string anchor = "\"" + key + "\": [";
            if (!json.Contains(anchor)) throw new InvalidOperationException("missing fixture anchor " + key);
            return json.Replace(anchor, anchor + entries + ",");
        }
        private static string Effects(string json, string entries)
        {
            JsonValue root = Content.Json.Parse(json);
            root.Set("entity_replacement_effects", Content.Json.Parse(entries)); return root.ToString();
        }
        private static Entity Find(SimGame game, string id)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity;
            throw new InvalidOperationException("missing proof entity " + id);
        }
        private static void Reach(SimGame game, Func<bool> ready, int limit, SimGame twin = null)
        {
            for (int i = 0; i < limit && !ready(); i++) { game.Update(); twin?.Update(); }
            if (!ready()) throw new InvalidOperationException("replacement stalled at tick " + game.Tick + "; fault=" + game.Fault);
        }
        internal static void Timed(Check c)
        {
            string json = Insert(PointGame.Definitions, "entities", @"
              { ""id"": ""expanded"", ""kind"": ""entity"", ""components"": {
                ""point_presence"": { ""space"": ""field.points"", ""position"": 77 },
                ""point_stock"": { ""resource"": ""supply"", ""capacity_policy"": ""fixture.count.60"", ""initial"": 1 } } },
              { ""id"": ""plain"", ""kind"": ""entity"", ""components"": {} },
              { ""id"": ""plain.new"", ""kind"": ""entity"", ""components"": {} }");
            json = Insert(json, "activities", @"{ ""id"": ""expand"", ""player_orderable"": true,
              ""on_complete"": ""expand.effect"", ""automatic_supply"": true, ""nodes"": [{ ""id"": ""upgrade"", ""executor"":""work.staffed"",""roles"": [""inspect""], ""duration"":{""numerator"":1,""denominator"":10,""unit"":""hour""} }] }");
            json = Effects(json, "[{\"id\":\"expand.effect\",\"source\":\"source\",\"target\":\"expanded\"}]");
            GameFactory factory = PointGame.Factory(json);
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 9 });
            Entity source = Find(game, "source"); ResourceId supply = game.World.Content.ResourceId("supply");
            var command = new PostOrderCommand { Plan = "expand", Target = source.Id };
            ulong before = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary engine preview accepts non-tile replacement");
            c.Equal(before, game.Hash(), "preview changes neither stock nor provider position");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary player command uses the same admission rule");
            Order order = game.World.Services.Get<WorkBoard>().Orders[0];
            Reach(game, () => order.Nodes[0].Progress.WholeTicks > 0, 12);
            c.That(source.Alive && order.IsActive && order.Nodes[0].Progress.WholeTicks < 6, "replacement waits for authored physical work");
            Entity busyMachine = Find(game, "machine");
            c.Equal(Upgrades.WorkerBusy, Upgrades.Check(game.World, busyMachine, busyMachine.Def), "replacement cannot orphan another entity's active worker job");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "save preserves the active order with existing codecs");
            Reach(game, () => order.State == OrderState.Done, 16, twin);
            Entity made = Find(game, "expanded");
            c.That(!source.Alive && made.Id != source.Id && made.Owner == source.Owner, "replacement changes identity but retains owner");
            c.Equal(Fix.Zero, made.Get<PointPresence>().Position, "provider keeps live position instead of prototype position 77");
            c.Equal(Fix.Zero, ((PointSnapshot)made.Get<ISpatialPresence>().Capture()).Previous, "new presence has consistent interpolation origin");
            c.Equal(Fix.FromInt(20), made.Get<IInventory>().Amount(supply), "verified relocation carries actual stock, not authored initial stock");
            c.Equal(Fix.FromInt(60), made.Get<IInventory>().CapacityPolicy.Limits[0].Amount, "capacity belongs to the new authored definition");
            c.Equal(6L, order.Nodes[0].Progress.WholeTicks, "completion executes every authored work tick");
            c.Equal(new Code("point.ready"), made.State, "new entity settles through its own update lifecycle");
            c.That(!game.World.Services.TryGet<TileMap>(out _), "replacement composes no tile authority");
            c.Equal(game.Hash(), twin.Hash(), "replacement continuation matches saved twin");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "completed replacement restores provider position and inventory");

            // Framework instrumentation of the public engine-side execution boundary: no host rule.
            Entity plain = game.World.Spawn(game.World.Content.Get<EntityDef>("plain"), source.Owner);
            c.Equal(Code.None, Upgrades.Check(game.World, plain, game.World.Content.Get<EntityDef>("plain.new")), "non-spatial replacement needs no invented location");
            c.Equal(Code.None, Upgrades.TryReplace(game.World, plain, game.World.Content.Get<EntityDef>("plain.new"), out Entity newPlain), "same sole coordinator replaces a non-spatial entity");
            c.That(!plain.Alive && newPlain.Get<ISpatialPresence>() == null, "non-spatial result remains non-spatial");
            c.Equal(Codes.NotAllowed, Upgrades.Check(game.World, newPlain, made.Def), "spatial/non-spatial conversion requires explicit support, not fallback");
            SimGame foreign = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 9 });
            c.Equal(Codes.NotFound, Upgrades.Check(game.World, Find(foreign, "source"), made.Def), "foreign entity identity refuses before provider execution");
            c.Equal(Code.None, Upgrades.Check(game.World, made, foreign.World.Content.Get<EntityDef>("expanded")), "sessions may safely share the factory's immutable definitions");
            SimGame foreignContent = PointGame.Factory(json).NewGame(new NewGameRequest { Scenario = "points", Seed = 9 });
            c.Equal(Codes.UnknownDef, Upgrades.Check(game.World, made, foreignContent.World.Content.Get<EntityDef>("expanded")), "separately resolved definitions refuse even with equal IDs");
            c.That(game.Fault == null && twin.Fault == null, "bounded work finishes without engine faults");
        }

        private static string TileEntity(string id, int width, string components = "", int height = 2) =>
            "{ \"id\": \"" + id + "\", \"kind\": \"entity\", \"components\": { \"tile_presence\": { \"space\": \"colony.tiles\", \"width\": "
            + width + ", \"height\": " + height + ", \"blocks\": true, \"movable\": false }" + components + " } }";
        private static void Refusals(Check c)
        {
            string json = Insert(Colony.Defs, "entities",
                TileEntity("wide", 4, ", \"storage\": { \"capacity_policy\": \"fixture.count.200\" }") + "," +
                TileEntity("blocker", 1, "", 1) + "," +
                TileEntity("small", 2, ", \"storage\": { \"capacity_policy\": \"fixture.count.1\" }") + "," +
                TileEntity("filtered", 2, ", \"storage\": { \"capacity_policy\": \"fixture.volume.200\", \"phases\": [\"liquid\", \"gas\"], \"accepts\": [\"water\"] }") + "," +
                TileEntity("empty", 2) + "," +
                TileEntity("faulty", 4, ", \"storage\": { \"capacity_policy\": \"fixture.count.200\" }, \"replacement_probe\": { \"allowed_targets\": [], \"fail_initialization\": true }") + "," +
                TileEntity("guarded", 2, ", \"replacement_probe\": { \"allowed_targets\": [\"wide\"], \"fail_initialization\": false }") + "," +
                TileEntity("huge", int.MaxValue));
            json = Insert(json, "activities", @"
                { ""id"": ""expand"", ""player_orderable"": true, ""on_complete"": ""expand.effect"",
                  ""automatic_supply"": true, ""nodes"": [{ ""id"": ""change"", ""executor"":""work.staffed"",""roles"": [""crane""], ""duration"":{""numerator"":1,""denominator"":10,""unit"":""hour""} }] },
                { ""id"": ""shrink"", ""player_orderable"": true, ""on_complete"": ""shrink.effect"",
                  ""automatic_supply"": true, ""nodes"": [{ ""id"": ""change"", ""roles"": [""crane""], ""hours"": 0.1 }] }");
            json = Effects(json, "[{\"id\":\"expand.effect\",\"source\":\"depot\",\"target\":\"wide\"},{\"id\":\"shrink.effect\",\"source\":\"depot\",\"target\":\"small\"}]");
            GameFactory factory = Colony.Factory(json);
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 1 });
            World world = game.World; TileMap map = (TileMap)world.Space;
            Entity depot = Find(game, "depot"); TileCoord origin = depot.Get<ITilePresence>().Tile;
            EntityDef wide = world.Content.Get<EntityDef>("wide"); ResourceId steel = world.Content.ResourceId("steel");
            ulong before = game.Hash();
            c.Equal(Code.None, Upgrades.Check(world, depot, wide), "replacement geometry ignores only its original footprint");
            c.Equal(Upgrades.StockWouldBeLost, Upgrades.Check(world, depot, world.Content.Get<EntityDef>("small")), "insufficient authored capacity refuses stock loss");
            c.Equal(Upgrades.StockWouldBeLost, game.Seat(0).Preview(new PostOrderCommand { Plan = "shrink", Target = depot.Id }), "command preview uses replacement capacity rule");
            c.Equal(before, game.Hash(), "refused stock/placement checks are pure");
            c.Equal(Upgrades.StockWouldBeLost, TestKit.Order(game, 0, new PostOrderCommand { Plan = "shrink", Target = depot.Id }).Refusal, "submitted command returns the same typed refusal");
            c.Equal(0, world.Services.Get<WorkBoard>().Orders.Count, "refused replacement does not create a work order");
            c.Equal(Upgrades.StockWouldBeLost, Upgrades.Check(world, depot, world.Content.Get<EntityDef>("filtered")), "changed receiving compatibility refuses existing stock");
            c.Equal(Upgrades.StockWouldBeLost, Upgrades.Check(world, depot, world.Content.Get<EntityDef>("empty")), "removing an inventory with stock refuses rather than discarding it");
            c.Equal(Codes.OutOfBounds, Upgrades.Check(world, depot, world.Content.Get<EntityDef>("huge")), "large authored footprint uses checked geometry without integer wrap");

            var expand = new PostOrderCommand { Plan = "expand", Target = depot.Id };
            c.Equal(Code.None, game.Seat(0).Preview(expand), "currently free expanded geometry is admitted");
            c.That(TestKit.Order(game, 0, expand).Accepted, "timed replacement order starts");
            Entity blocker = map.SpawnAt(world, world.Content.Get<EntityDef>("blocker"), depot.Owner, new TileCoord(origin.X + 3, origin.Y));
            Order order = world.Services.Get<WorkBoard>().Orders[0];
            Reach(game, () => !order.IsActive, 120);
            c.Equal(OrderState.Failed, order.State, "geometry changing during work fails completion deterministically");
            c.Equal(Codes.Blocked, order.Blocker, "failure exposes provider's actual placement blocker");
            c.That(depot.Alive && ReferenceEquals(map.OccupantAt(origin), depot), "failed completion keeps original entity and occupancy");
            c.Equal(Fix.FromInt(50), depot.Get<IInventory>().Amount(steel), "failed replacement preserves physical stock");
            world.Despawn(blocker); world.Entities.Flush();

            Entity guarded = map.SpawnAt(world, world.Content.Get<EntityDef>("guarded"), depot.Owner, new TileCoord(10, 25));
            c.Equal(ReplacementProbe.Denied, Upgrades.Check(world, guarded, world.Content.Get<EntityDef>("empty")), "independent capability can refuse replacement without coordinator changes");
            c.Equal(Code.None, Upgrades.Check(world, guarded, wide), "JSON-authored alternative allowed by the same extension policy");
            // The completing crane still occupies its real approach tile beside the old depot.
            // Use an isolated stocked fixture for startup fault injection rather than teleporting it.
            depot = map.SpawnAt(world, world.Content.Get<EntityDef>("depot"), depot.Owner, new TileCoord(24, 25));
            depot.Get<IInventory>().Add(steel, Fix.FromInt(50));
            origin = depot.Get<ITilePresence>().Tile;
            world.Entities.Flush(); before = game.Hash();
            EntityDef faulty = world.Content.Get<EntityDef>("faulty");
            c.Throws<InvalidOperationException>(() => Upgrades.TryReplace(world, depot, faulty, out _), "startup failure propagates, not a fabricated success");
            c.That(depot.Alive && ReferenceEquals(world.Entities.Get(depot.Id), depot), "initialization failure restores original identity");
            c.That(ReferenceEquals(map.OccupantAt(origin), depot) && map.OccupantAt(new TileCoord(origin.X + 3, origin.Y)) == null, "rollback releases temporary enlarged occupancy and restores original footprint");
            c.Equal(before, game.Hash(), "failed initialization consumes no entity ID or persisted state");
            var requester = new ReservationOwner("proof.replacement", 1);
            ReservationToken heldStock = depot.Get<IOutletPort>().ReserveOut(requester, steel, Fix.One);
            c.Equal(Upgrades.InventoryReserved, Upgrades.Check(world, depot, wide), "owned commitments block replacement until released");
            depot.Get<IOutletPort>().ReleaseOut(requester, heldStock);
            c.Equal(Code.None, Upgrades.TryReplace(world, depot, wide, out Entity made), "one coordinator replaces generic-kind tile entity after blockers clear");
            world.Entities.Flush();
            c.Equal(4, made.Get<ITilePresence>().Footprint.W, "new footprint belongs to target definition");
            c.Equal(new FixVec2(Fix.FromInt(origin.X) + Fix.FromInt(2), Fix.FromInt(origin.Y) + Fix.One), made.Get<ITilePresence>().Position, "stationary position matches new footprint center");
            c.That(ReferenceEquals(map.OccupantAt(new TileCoord(origin.X + 3, origin.Y)), made), "expanded occupancy is owned by the new identity");
            c.Equal(Fix.FromInt(50), made.Get<IInventory>().Amount(steel), "verified relocation preserves stock during resize");
            c.Equal(Fix.Zero, made.Get<IOutletPort>().ReservedOut(steel), "released reservations do not migrate");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "resized placement restores and reclaims matching provider geometry");
            c.That(game.Fault == null, "typed replacement refusal is not an engine fault");
        }
    }
}
