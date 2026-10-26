using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class AuthoredStateTests
    {
        public static void Register()
        {
            TestRunner.Add("Authored states: arbitrary bindings settle physical capabilities and saved twins", Physical);
            TestRunner.Add("Authored states: strict references, immutable bindings and isolated content", Contracts);
        }

        private static JsonValue EntityJson(JsonValue json, string id)
        {
            for (int i = 0; i < json.Get("entities").Count; i++)
                if (json.Get("entities")[i].Get("id").Text == id) return json.Get("entities")[i];
            throw new InvalidOperationException("missing fixture entity " + id);
        }
        private static JsonValue Component(JsonValue json, string entity, string component)
            => EntityJson(json, entity).Get("components").Get(component);

        private static JsonValue Definitions()
        {
            JsonValue json = Json.Parse(Colony.Defs);
            JsonValue states = json.Get("entity_states");
            for (int i = 0; i < states.Count; i++)
            {
                string id = states[i].Get("id").Text;
                states[i].Set("id", JsonValue.Of("custom." + id));
                states[i].Set("state", JsonValue.Of("phase." + id));
            }
            JsonValue entities = json.Get("entities");
            for (int i = 0; i < entities.Count; i++)
            {
                JsonValue parts = entities[i].Get("components");
                for (int k = 0; k < parts.Count; k++)
                {
                    JsonValue bindings = parts.ValueAt(k).Get("states");
                    if (bindings == null) continue;
                    for (int n = 0; n < bindings.Count; n++)
                        bindings.Set(bindings.KeyAt(n), JsonValue.Of("custom." + bindings.ValueAt(n).Text));
                }
            }
            states.Add(Json.Parse("{ \"id\": \"plant.batch\", \"state\": \"phase.batch\", \"priority\": 25 }"));
            Component(json, "factory", "transformer").Get("states").Set("working", JsonValue.Of("plant.batch"));
            JsonValue factory = EntityJson(json, "factory").Get("components");
            factory.Remove("storage");
            factory.Set("single_resource_port", Json.Parse("{ \"resource\": \"steel\", \"capacity_policy\": \"fixture.count.40\", \"initial\": 30 }"));
            factory.Set("worker", Component(json, "crane", "worker").Clone());
            json.Get("transformations")[0].Set("duration", Json.Parse("{\"numerator\":1,\"denominator\":10,\"unit\":\"hour\"}"));
            json.Get("activities")[0].Set("target_tags", JsonValue.NewArray());
            json.Get("activities")[0].Get("nodes")[0].Set("duration", Content.Json.Parse("{\"numerator\":3,\"denominator\":10,\"unit\":\"hour\"}"));
            return json;
        }

        private static Entity Mine(SimGame game, string def)
            => game.World.Entities.Get(Colony.Mine(game, 0, def)[0].Id);
        private static void Reach(SimGame game, Func<bool> condition, int limit, string stage)
        {
            TestKit.RunUntil(game, condition, limit, 1);
            if (!condition()) throw new InvalidOperationException(stage + " stalled at tick " + game.Tick + "; fault=" + game.Fault);
        }

        private static void Physical(Check c)
        {
            GameFactory factory = Colony.Factory(Definitions().ToString());
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 9 });
            World world = game.World;
            foreach (string old in new[] { "idle", "working", "loading", "moving", "stopped", "under-construction" })
                c.That(world.Content.Find<IEntityStateDefinition>(old) == null, "no conventional state definition " + old);
            Entity plant = Mine(game, "factory");
            c.Equal(new Code("phase.idle"), plant.State, "Initialize uses the authored default identity");
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = plant.Id }).Accepted,
                "player submits ordinary timed self-work");
            Reach(game, () => plant.State == new Code("phase.working"), 8, "stationary work");
            Order order = world.Services.Get<WorkBoard>().Orders[0];
            c.That(order.IsActive && order.Nodes[0].Progress.WholeTicks < 18, "worker state does not bypass authored duration");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = plant.Id, Transformation = "build_truck" }).Accepted,
                "ordinary transformation queue accepts the same authored content");
            Reach(game, () => plant.State == new Code("phase.batch"), 8, "transformation");
            c.That(plant.Get<Transformer>().Progress.WholeTicks < 6 && Colony.Mine(game, 0, "truck").Count == 2,
                "transformation state requires physical progress before a vehicle exists");
            c.Equal(new Code("phase.working"), plant.Get<Worker>().GetState(new EntityUpdateContext(world, plant)).State,
                "the worker remains active while the transformer's authored priority wins");
            c.That(game.Snapshot.Entities.Exists(plant.Id, new Code("phase.batch")), "Update publishes the entity's arbitrated state");
            c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = plant.Id, Operation = new Code("transformation"), Enabled = false, Target = Fix.One }).Accepted,
                "ordinary control disables physical processing");
            c.Equal(new Code("phase.stopped"), plant.State, "authored stopped binding overrides active work");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "bindings require no duplicate save payload");
            c.Equal(plant.StateSince, twin.World.Entities.Get(plant.Id).StateSince, "settled transition timestamp restores");
            c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = plant.Id, Operation = new Code("transformation"), Enabled = true, Target = Fix.One }).Accepted,
                "player restarts through the existing engine operation");
            TestKit.Order(twin, 0, new ConfigureOperationCommand { Entity = plant.Id, Operation = new Code("transformation"), Enabled = true, Target = Fix.One });
            for (int i = 0; i < 24; i++) { game.Update(); twin.Update(); }
            c.Equal(OrderState.Done, order.State, "bounded Updates finish the real work order");
            c.Equal(3, Colony.Mine(game, 0, "truck").Count, "one physical transformation manufactures one machine");
            c.Equal(new Code("phase.idle"), plant.State, "inactive capabilities return to the authored default");
            c.Equal(game.Hash(), twin.Hash(), "interleaved restored sessions continue identically");

            Entity crane = Mine(game, "crane");
            TileCoord origin = crane.Get<ITilePresence>().Tile;
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = crane.Id, To = new TileCoord(15, 5) }).Accepted,
                "player movement uses the standard engine command");
            c.Equal(new Code("phase.moving"), crane.State, "mover resolves an arbitrary catalog ID");
            c.Equal(origin, crane.Get<ITilePresence>().Tile, "moving state never teleports the machine");
            twin = factory.Load(game.Save());
            int elapsed = TestKit.RunUntil(game, () => crane.Get<IMobility>().Status != MoveStatus.Moving, 120, 1);
            twin.RunTicks(elapsed);
            c.That(elapsed > 0 && crane.Get<IMobility>().Status == MoveStatus.Arrived, "bounded travel arrives over game time");
            c.Equal(game.Hash(), twin.Hash(), "in-motion save preserves authored state continuation");

            c.That(TestKit.Order(game, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(game.World.Space.Id, new TileCoord(12, 16)), Priority = 50 }).Accepted,
                "ordinary construction admits a physical site");
            Entity site = Mine(game, "site:tower");
            c.Equal(new Code("phase.under-construction"), site.State, "authored site's state comes from its target's JSON");
            c.That(ReferenceEquals(site.Def.Component<SiteDef>().ConstructionState, world.Content.Get<EntityDef>("tower").Component<ConstructibleDef>().States["site"]),
                "site references the same catalog object rather than inventing a state");
            c.Equal(0, Colony.Mine(game, 0, "tower").Count, "site state does not instantly create a completed structure");
            twin = factory.Load(game.Save());
            for (int i = 0; i < 10; i++) { game.Update(); twin.Update(); }
            c.Equal(game.Hash(), twin.Hash(), "active site and worker continuation restore through existing codecs");

            TileMap map = (TileMap)world.Space;
            Entity cart = map.SpawnMobile(world, world.Content.Get<EntityDef>("module_cart"), new PlayerId(0), FixVec2.Centre(new TileCoord(25, 9)));
            world.Settle();
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = cart.Id, To = new TileCoord(28, 9) }).Accepted,
                "independent non-friend mobility receives the same public command");
            c.Equal(new Code("phase.moving"), cart.State, "external provider uses the public binding reader and existing state-source contract");
            c.That(ReferenceEquals(world.Content.Get<EntityDef>("module_worker").Component<ContractWorkerDef>().States["working"], world.Content.Get<IEntityStateDefinition>("custom.working")),
                "external worker resolves through the same catalog, without a scheduler branch");
        }

        private static void Contracts(Check c)
        {
            foreach (var entry in new[] { ("crane", "worker", "working"), ("crane", "mover", "moving"), ("factory", "transformer", "working"), ("tower", "construction", "site") })
            {
                foreach (string flaw in new[] { "absent", "missing-mode", "empty", "non-string", "unknown-mode", "unknown-reference" })
                {
                    JsonValue json = Definitions(); JsonValue part = Component(json, entry.Item1, entry.Item2);
                    JsonValue bindings = part.Get("states");
                    switch (flaw)
                    {
                        case "absent": part.Remove("states"); break;
                        case "missing-mode": bindings.Remove(entry.Item3); break;
                        case "empty": bindings.Set(entry.Item3, JsonValue.Of(" ")); break;
                        case "non-string": bindings.Set(entry.Item3, JsonValue.Of(true)); break;
                        case "unknown-mode": bindings.Set("misspelled-mode", JsonValue.Of("custom.idle")); break;
                        case "unknown-reference": bindings.Set(entry.Item3, JsonValue.Of("missing.state")); break;
                    }
                    int composed = 0;
                    JsonValue manifest = Json.Parse(Colony.Manifest);
                    manifest.Get("modules").Add(Json.Parse("{ \"id\": \"state-probe\", \"version\": \"1.0.0\", \"documents\": [] }"));
                    json.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("state-probe"));
                    var definition = new GameDefinition("invalid-state", new GameModule[] { new InventoryModule(), new OperationsModule(), new ContentAccessModule(), new TransformationModule(), new MotionModule(), new TileNavigationModule(), new WorkModule(), new LogisticsModule(), new ConstructionModule(), new InfrastructureModule(), new IncidentResponseModule(), new ColonyModule(),
                        new ProbeModule("state-probe", new ModuleVersion(1, 0, 0), compose: _ => composed++) });
                    var files = new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", manifest.ToString()).Add("colony.json", json.ToString())
                        .Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
                    var factory = new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
                    c.Throws<ContentException>(() => factory.NewGame(new NewGameRequest { Scenario = "colony" }), entry.Item2 + " refuses " + flaw);
                    c.Equal(0, composed, "invalid states refuse before module composition and initialization");
                }
            }
            JsonValue good = Definitions(); GameFactory valid = Colony.Factory(good.ToString());
            SimGame a = valid.NewGame(new NewGameRequest { Scenario = "colony", Seed = 2 });
            SimGame b = valid.NewGame(new NewGameRequest { Scenario = "colony", Seed = 2 });
            WorkerDef worker = Mine(a, "crane").Def.Component<WorkerDef>();
            var dictionary = (IDictionary<string, IEntityStateDefinition>)worker.States.Definitions;
            c.Throws<NotSupportedException>(() => dictionary["working"] = a.World.Content.DefaultEntityState, "resolved bindings cannot be replaced");
            c.Throws<NotSupportedException>(() => dictionary.Clear(), "resolved bindings cannot be cleared");
            c.That(ReferenceEquals(worker.States["working"], a.World.Content.Get<IEntityStateDefinition>("custom.working")), "bindings are catalog references, not copies");
            c.That(ReferenceEquals(worker, Mine(b, "crane").Def.Component<WorkerDef>()), "sessions share immutable definitions only");
            var before = b.Snapshot;
            TestKit.Order(a, 0, new MoveVehicleCommand { Vehicle = Mine(a, "crane").Id, To = new TileCoord(15, 5) });
            c.Equal(new Code("phase.idle"), Mine(b, "crane").State, "one session's movement cannot change another's entity state");
            c.That(ReferenceEquals(before, b.Snapshot), "inactive session retains its immutable projection");
            Component(good, "crane", "worker").Get("states").Set("stopped", JsonValue.Of("custom.moving"));
            GameFactory changed = Colony.Factory(good.ToString());
            byte[] save = a.Save();
            c.That(!changed.Describe(save).Compatible, "binding changes participate in existing save content identity");
            c.Throws<CorruptStateException>(() => changed.Load(save), "changed bindings are not silently migrated on load");
            JsonValue shared = Definitions();
            Component(shared, "crane", "worker").Get("states").Set("loading", JsonValue.Of("custom.working"));
            SimGame alias = Colony.Factory(shared.ToString()).NewGame(new NewGameRequest { Scenario = "colony" });
            WorkerDef aliasDef = Mine(alias, "crane").Def.Component<WorkerDef>();
            c.That(ReferenceEquals(aliasDef.States["loading"], aliasDef.States["working"]), "different behavior modes may intentionally select the same state");
        }
    }
}
