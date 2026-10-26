using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;

namespace Beep.SimEngine.Tests
{
    public static class OptionalSpaceTests
    {
        private const string Definitions = "{" + TestGame.TimeDefinitions + @"
          ""entity_states"": [
            { ""id"": ""idle"", ""state"": ""station.idle"", ""priority"": 0, ""default"": true },
            { ""id"": ""working"", ""state"": ""station.processing"", ""priority"": 20 },
            { ""id"": ""loading"", ""state"": ""station.loading"", ""priority"": 30 },
            { ""id"": ""stopped"", ""state"": ""station.stopped"", ""priority"": 60 }
          ],
          ""worker_roles"": [{ ""id"": ""process"" }],
          ""entities"": [
            { ""id"": ""station"", ""kind"": ""structure"", ""components"": { ""worker"": {""work_rate"":1, ""roles"": [""process""], ""states"": { ""working"": ""working"", ""loading"": ""loading"", ""stopped"": ""stopped"" } } } },
            { ""id"": ""target"", ""kind"": ""marker"" }
          ],
          ""activities"": [{ ""id"": ""process"", ""player_orderable"": true, ""automatic_supply"": true, ""nodes"": [
            { ""id"": ""run"", ""executor"":""work.staffed"",""roles"": [""process""], ""duration"":{""numerator"":1,""denominator"":20,""unit"":""hour""} }
          ] }],
          ""game_modes"": [{ ""id"": ""optional-space.resource-only"", ""name"": ""Physical diagnostic game"", ""properties"": {} }],
          ""scenarios"": [{ ""id"": ""station"", ""game_mode"": ""optional-space.resource-only"", ""mode_overrides"": {}, ""time"": ""test.minute"", ""modules"": [""sim.work"",""sim.transformations"", ""station""], ""seats"": 1,
            ""settings"": {""sim.work"": {""retry"": ""proof.retry""}} }]
        }";

        private static GameFactory Factory()
        {
            var module = new ProbeModule("station", new ModuleVersion(1, 0, 0),
                new[] { new ModuleRequirement(WorkModule.ModuleId, new ModuleVersion(1, 0, 0)), new ModuleRequirement(TransformationModule.ModuleId, new ModuleVersion(1, 0, 0)) },
                compose: null);
            var definition = new GameDefinition("station-proof", new GameModule[] { new InventoryModule(), new OperationsModule(), new ContentAccessModule(), new TransformationModule(), new MotionModule(), new TileNavigationModule(), new WorkModule(), new LogisticsModule(), new ConstructionModule(), new InfrastructureModule(), new IncidentResponseModule(), module });
            var files = new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access)
                .Add("manifest.json", "{ \"schema\": 1, \"modules\": [{\"id\":\"sim.access\",\"version\":\"1.0.0\",\"documents\":[\"framework/access.json\"]}, {\"id\":\"sim.inventory\",\"version\":\"1.0.0\",\"documents\":[]}, {\"id\":\"sim.operations\",\"version\":\"1.0.0\",\"documents\":[]}, {\"id\":\"sim.transformations\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.motion\",\"version\":\"1.0.0\",\"documents\":[]}, {\"id\":\"sim.tile-navigation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.motion\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.work\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.motion\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.logistics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.construction\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.incidents\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]}, { \"id\": \"station\", \"version\": \"1.0.0\", \"requires\": [{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}], \"documents\": [\"station.json\"] }] }")
                .Add("station.json", Definitions);
            JsonValue master = Content.Json.Parse(files.ReadText("manifest.json"));
            for (int i = 0; i < master.Get("modules").Count; i++)
                if (master.Get("modules")[i].Get("id").Text == WorkModule.ModuleId)
                    master.Get("modules")[i].Get("documents").Add(JsonValue.Of("work-retry.json"));
            var text = new System.Text.StringBuilder(); master.WriteCanonical(text);
            files.Add("manifest.json", text.ToString()).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }

        public static void Register()
        {
            TestRunner.Add("Optional space: stationary standard work uses contracts and survives saves", Stationary);
            TestRunner.Add("Optional space: tile reach and travel validate providers without remote instant work", TileAccess);
        }

        private static void Stationary(Check c)
        {
            GameFactory factory = Factory();
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "station", Seed = 4 });
            World world = game.World;
            Entity machine = world.Spawn(world.Content.Get<EntityDef>("station"), new PlayerId(0));
            Entity remote = world.Spawn(world.Content.Get<EntityDef>("target"), new PlayerId(0));
            world.Settle();
            c.That(world.Space == null && game.Snapshot.Space == null, "standard simulation does not invent map dimensions or spatial state");
            c.That(!world.Services.TryGet<NavService>(out _) && !world.Services.TryGet<TileMap>(out _), "tile services are not composed in a nonspatial scenario");
            c.That(machine.Get<IWorker>() != null && machine.Get<IMobility>() == null, "stationary worker is not forced to declare movement");
            c.Equal(SpatialInteractionStatus.WithinReach, SpatialInteraction.Check(world, machine, machine), "nonspatial local work is actual self-interaction");
            c.Equal(SpatialProximityStatus.WithinReach, SpatialInteraction.CheckProximity(world, machine, machine), "nonspatial proximity supports actual self-work");
            c.Equal(SpatialProximityStatus.Unsupported, SpatialInteraction.CheckProximity(world, machine, remote), "nonspatial proximity does not invent shared geometry");
            c.Equal(SpatialInteractionStatus.Unsupported, SpatialInteraction.Check(world, machine, remote), "no-space entities are not treated as sharing an invented origin");
            c.That(!SpatialInteraction.TryEstimateCost(world, machine, remote, out _), "no distance is fabricated for unrelated nonspatial entities");
            game.Seat(0).Submit(new PostOrderCommand { Plan = "process", Target = machine.Id });
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException("stationary first Update fault: " + game.Fault.Detail);
            WorkBoard board = world.Services.Get<WorkBoard>();
            Order order = board.Orders[0];
            c.That(order.IsActive && order.Nodes[0].Progress.WholeTicks == 0, "submission does not finish authored work instantly");
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException("stationary second Update fault: " + game.Fault.Detail);
            c.Equal(new Code("station.processing"), machine.State, "entity Update settles the stationary worker's processing state");
            c.Equal(2, game.Snapshot.Entities.Count, "Update publishes both nonspatial entities through the existing snapshot");
            c.That(game.Snapshot.Entities[0].Spatial == null && game.Snapshot.Entities[0].State == new Code("station.processing"),
                "immutable entity facts carry settled state without inventing spatial coordinates");
            c.Equal(1L, order.Nodes[0].Progress.WholeTicks, "stationary work advances by the existing authoritative progress owner");
            SimGame loaded = factory.Load(game.Save());
            c.Equal(game.Hash(), loaded.Hash(), "local job, worker and progress save without tile state");
            int ticks = 0;
            while (order.IsActive && ticks < 8) { game.Update(); loaded.Update(); ticks++; }
            c.Equal(OrderState.Done, order.State, "bounded updates finish the physical-duration local job");
            c.Equal(3L, order.Nodes[0].Progress.WholeTicks, "all three required ticks are worked, not bypassed");
            c.Equal(new Code("station.idle"), machine.State, "entity returns to its authored idle state on completion");
            c.Equal(game.Hash(), loaded.Hash(), "restored stationary work continues identically");
            game.Seat(0).Submit(new PostOrderCommand { Plan = "process", Target = remote.Id });
            for (int i = 0; i < 5; i++) game.Update();
            Order remoteOrder = board.Orders[board.Orders.Count - 1];
            c.That(remoteOrder.IsActive && remoteOrder.Nodes[0].Progress.WholeTicks == 0 && remoteOrder.Blocker == WorkSystem.BlockerUnreachable,
                "unrelated no-space target is unreachable rather than receiving remote work; actual=" + remoteOrder.State + "/" + remoteOrder.Blocker);
            SimGame other = factory.NewGame(new NewGameRequest { Scenario = "station", Seed = 4 });
            Entity foreign = other.World.Spawn(other.World.Content.Get<EntityDef>("station"), new PlayerId(0));
            other.World.Settle();
            c.Equal(SpatialInteractionStatus.Unsupported, SpatialInteraction.Check(world, machine, foreign), "matching entity identities cannot cross session boundaries");
        }

        private static JsonValue EntityDefinition(JsonValue json, string id)
        {
            JsonValue entries = json.Get("entities");
            for (int i = 0; i < entries.Count; i++) if (entries[i].Get("id").Text == id) return entries[i];
            throw new InvalidOperationException("proof entity is missing");
        }

        private static void TileAccess(Check c)
        {
            JsonValue json = Json.Parse(Colony.Defs);
            EntityDefinition(json, "crane").Get("components").Remove("mover");
            json.Get("activities")[0].Set("target_tags", JsonValue.NewArray());
            GameFactory factory = Colony.Factory(json.ToString());
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 3 });
            World world = game.World; TileMap map = (TileMap)world.Space;
            Entity crane = world.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id);
            Entity nearby = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(9, 5));
            Entity distant = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(25, 20));
            world.Settle();
            c.Equal(SpatialInteractionStatus.WithinReach, SpatialInteraction.Check(world, crane, nearby), "tile provider allows stationary work within physical reach");
            c.Equal(SpatialInteractionStatus.Unreachable, SpatialInteraction.Check(world, crane, distant), "stationary machine cannot reach a distant target");
            c.That(SpatialInteraction.TryEstimateCost(world, crane, nearby, out long nearCost) && nearCost == 0, "cost and reach use the same provider-owned area");
            game.Seat(0).Submit(new PostOrderCommand { Plan = "inspect", Target = nearby.Id }); game.Update(); game.Update();
            Order local = world.Services.Get<WorkBoard>().Orders[0];
            c.Equal(1L, local.Nodes[0].Progress.WholeTicks, "stationary tile worker starts ordinary timed work without a mover");
            SimGame restored = factory.Load(game.Save());
            for (int i = 0; i < 64 && local.IsActive; i++) { game.Update(); restored.Update(); }
            c.Equal(OrderState.Done, local.State, "bounded physical work finishes through the unchanged order board");
            c.Equal(game.Hash(), restored.Hash(), "stationary tile job restores and continues identically");
            Entity truck = world.Entities.Get(Colony.Mine(game, 0, "truck")[0].Id);
            TileCoord origin = truck.Get<ITilePresence>().Tile;
            c.Equal(SpatialInteractionStatus.Reachable, SpatialInteraction.Check(world, truck, distant), "mobile carrier gets route eligibility from the selected provider");
            c.Equal(SpatialInteractionStatus.Reachable, SpatialInteraction.Approach(world, truck, distant), "approach requests physical movement");
            c.Equal(origin, truck.Get<ITilePresence>().Tile, "approach never teleports the carrier");
            game.Update();
            c.That(SpatialInteraction.Check(world, truck, distant) != SpatialInteractionStatus.WithinReach, "one Update does not bypass long travel");
            c.Throws<InvalidOperationException>(() => world.Services.Get<NavService>().Define("late", 10, _ => 10), "navigation registrations freeze before Update");
            JsonValue broken = Json.Parse(Colony.Defs);
            EntityDefinition(broken, "crane").Get("components").Get("mover").Set("class", JsonValue.Of("missing"));
            c.Throws<ContentException>(() => Colony.Factory(broken.ToString()).NewGame(new NewGameRequest { Scenario = "colony" }), "missing movement class refuses before any simulation updates");
            JsonValue absentPresence = Json.Parse(Colony.Defs);
            EntityDefinition(absentPresence, "crane").Get("components").Remove("tile_presence");
            c.Throws<ContentException>(() => Colony.Factory(absentPresence.ToString()).NewGame(new NewGameRequest { Scenario = "colony" }), "standard mover requires its explicit tile presence");
        }
    }
}
