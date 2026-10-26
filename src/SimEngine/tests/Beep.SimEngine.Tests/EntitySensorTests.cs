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
    public static class EntitySensorTests
    {
        public static void Register() => TestRunner.Add("Entity sensors: physical sight, scoped snapshots and shared rule ownership", Run);
        private static readonly Code Sensor = new Code("survey.sensor");
        internal static GameFactory Factory(string policy = "survey.entity-sight", string sight = null, string world = null)
        {
            JsonValue authoredWorld = Content.Json.Parse(world ?? FrameworkProofContent.Read("native-sensor-world.json"));
            authoredWorld.Get("scenarios")[0].Get("settings").Get("sim.knowledge-visibility").Set("policy", JsonValue.Of(policy));
            authoredWorld.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("sight.start"));
            JsonValue master = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-master.json"));
            JsonValue modules = master.Get("modules");
            for (int i = 0; i < modules.Count; i++)
            {
                JsonValue module = modules[i]; string id = module.Get("id").Text;
                if (id == "sim.knowledge-visibility") module.Get("documents").Add(JsonValue.Of("native-entity-sight.json"));
                if (id == "sim.world-setup") module.Get("documents").Add(JsonValue.Of("native-entity-startup.json"));
                if (id == "sim.knowledge") module.Get("documents").Add(JsonValue.Of("native-entity-permission.json"));
            }
            return LiveSensorTests.Factory(world: authoredWorld.ToString(), master: master.ToString(), additionalDocuments: new Dictionary<string, string> {
                ["native-entity-sight.json"] = sight ?? FrameworkProofContent.Read("native-entity-sight.json"),
                ["native-entity-startup.json"] = FrameworkProofContent.Read("native-entity-startup.json"),
                ["native-entity-permission.json"] = FrameworkProofContent.Read("native-entity-permission.json") });
        }
        private static Entity Machine(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "survey.machine");
        private static Entity Site(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "survey.site");
        private static Entity Foreign(SimGame game, int x) => game.World.Entities.All.Single(e => e.Def.Id == "survey.foreign-sensor" && e.Get<ITilePresence>().Tile.X == x);
        private static bool Seen(SimGame game, EntityId id) => game.Seat(0).View.TryGetEntity(id, out _);
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("entity sensor time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " position=" + Machine(game).Get<ITilePresence>().Tile +
                " motion=" + Machine(game).Get<IMotion>().Status + " entity-count=" + game.Seat(0).View.EntityCount);
        }
        private static void Move(Check c, SimGame game, TileCoord to)
        {
            var command = new MoveVehicleCommand { Vehicle = Machine(game).Id, To = to };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal physical sight approach preview");
            c.That(TestKit.Order(game, 0, command).Accepted, "normal physical sight approach command");
        }
        private static void Control(Check c, SimGame game, int seat, EntityId entity, bool enabled, int distance)
        {
            var command = new ConfigureOperationCommand { Entity = entity, Operation = Sensor, Enabled = enabled, Target = Fix.FromInt(distance) };
            c.Equal(Code.None, game.Seat(seat).Preview(command), "normal owned sensor control preview");
            c.That(TestKit.Order(game, seat, command).Accepted, "existing operation owner commits sensor control");
        }
        private static KnowledgeObservation History(SimGame game)
        {
            if (!game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out var value)) throw new InvalidOperationException("missing completed survey");
            return value;
        }
        private static void VerifyHidden(Check c, SimGame game, EntityId id, string stage)
        {
            var mine = game.Seat(0).View; var entities = new List<EntityView>(); var facets = new List<Facet>();
            c.That(!mine.TryGetEntity(id, out _), stage + " direct lookup hides target");
            c.That(!mine.Inspect(id, facets) && facets.Count == 0, stage + " inspector hides target");
            mine.CopyEntities(new PlayerId(1), entities); c.Equal(0, entities.Count, stage + " owner-filtered list contains no hidden enemy");
            mine.Feature<TileMapView>().CopyEntities(new TileRect(29, 9, 3, 3), entities);
            c.Equal(0, entities.Count, stage + " spatial query contains no hidden enemy");
            c.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Id == id), stage + " scoped snapshot hides target");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current actual entity sight identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no new persistent sight state");
            var factory = Factory(); var game = LiveSensorTests.New(factory); var machine = Machine(game);
            EntityId near = Foreign(game, 30).Id, far = Foreign(game, 45).Id;
            var mine = game.Seat(0).View; var other = game.Seat(1).View; var map = mine.Feature<TileMapView>();
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = true }).Accepted, "normal initial hold leaves worker at actual authored origin");
            c.Equal(4, game.World.Entities.All.Count, "engine-only JSON startup includes two identical foreign prototypes");
            c.Equal(2, mine.EntityCount, "initial visible count includes only own entities");
            c.Equal(2, other.EntityCount, "other company initially sees only its actual own sources");
            VerifyHidden(c, game, near, "initial sight");
            c.That(!Seen(game, far), "same foreign prototype at another location stays hidden");
            c.That(game.World.Content.Get<EntitySensorVisibilityDef>("sight.foreign").AuthoringPath.Contains("native-entity-sight.json"), "actual master document owns entity rules");
            c.That(game.World.Content.Get<EntitySensorVisibilityDef>("sight.foreign") is SensorVisibilityDef<EntityDef> &&
                game.World.Content.Get<LayerSensorVisibilityDef>("survey.terrain.sight") is SensorVisibilityDef<ISpatialLayerDefinition>, "one generic owner serves both target types");
            c.That(game.Snapshot.Entities.Any(e => e.Id == near) && game.Snapshot.Entities.Any(e => e.Id == far), "explicit observer snapshot has deliberate whole-world authority");
            var visibility = game.World.Services.Get<IVisibility>();
            foreach (PlayerId invalid in new[] { new PlayerId(-2), new PlayerId(8) }) c.That(!visibility.CanSee(game.World, invalid, Foreign(game, 30)), "unseated viewer cannot borrow source coverage");
            c.That(visibility.CanSee(game.World, PlayerId.None, Foreign(game, 30)), "exact observer permission remains deliberate");
            ulong hash = game.Hash(); var entities = new List<EntityView>(); mine.CopyEntities(entities);
            c.Equal(2, entities.Count, "full entity view shares scoped count");
            c.Equal(hash, game.Hash(), "initial visibility/count/list/inspect reads are pure");
            int version = map.Version;
            Control(c, game, 1, far, false, 20);
            c.Equal(version, map.Version, "hidden other-company source change leaks no private layer cursor");
            c.Equal(2, mine.EntityCount, "hidden control change does not leak enemy count");
            c.That(!game.Seat(0).Snapshot.Spawned.Contains(far) && !game.Seat(0).Snapshot.Removed.Contains(far), "never-seen hidden changes emit no scoped identity delta");
            Control(c, game, 1, far, true, 20);

            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = false }).Accepted, "normal release restores work dispatch");
            var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "survey.sample", Target = Site(game).Id, Priority = 60 });
            c.That(result.Accepted, "normal physical survey plan");
            var order = game.World.Services.Get<WorkBoard>().Order((int)result.Arg0);
            var twin = factory.Load(game.Save());
            Reach(game, () => order.State == OrderState.Done, "actual physical survey", twin);
            c.Equal(game.Hash(), twin.Hash(), "survey and sensor rules continue identically");
            c.Equal(Fix.FromInt(12), Site(game).Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "physical work spends materials without Tycoon");
            var history = History(game);
            c.That(!Seen(game, near) && !Seen(game, far), "completed own-site work does not reveal distant enemies");
            c.That(other.TryGetEntity(machine.Id, out _), "other company's actual long sensor sees approaching foreign worker");
            c.That(!other.TryGetEntity(Site(game).Id, out _), "unlisted foreign site remains hidden despite another prototype rule");

            Move(c, game, new TileCoord(28, 10)); twin = factory.Load(game.Save());
            Reach(game, () => Seen(game, near), "actual foreign entity enters sensor coverage", twin);
            c.Equal(game.Hash(), twin.Hash(), "moving coverage and actual target sight restore deterministically");
            c.Equal(3, mine.EntityCount, "exact visible enemy increments scoped count");
            c.That(!Seen(game, far), "seeing one instance never reveals every matching prototype");
            c.That(game.Seat(0).Snapshot.Spawned.Contains(near) && !game.Seat(0).Snapshot.Spawned.Contains(far), "only actual entered-sight entity appears in delta");
            mine.CopyEntities(new PlayerId(1), entities); c.That(entities.Count == 1 && entities[0].Id == near, "owner-filtered list includes exact seen instance");
            map.CopyEntities(new TileRect(29, 9, 3, 3), entities); c.That(entities.Count == 1 && entities[0].Id == near, "spatial query uses exact same entity rule");
            c.That(mine.Inspect(near, new List<Facet>()), "public inspect can open seen entity");
            c.That(!mine.Feature<OperationsView>().TryGetControl(near, Sensor, out _), "seeing enemy never grants private controls");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(new ConfigureOperationCommand { Entity = near, Operation = Sensor, Enabled = false, Target = Fix.FromInt(20) }), "seen foreign operation remains unauthorized and its private control is not disclosed");
            c.Equal(history.ObservedTick, History(game).ObservedTick, "entity sight neither records nor refreshes geological history");
            var retained = game.Seat(0).Snapshot;
            Control(c, game, 0, machine.Id, false, 2);
            VerifyHidden(c, game, near, "disabled sight");
            c.Equal(2, mine.EntityCount, "explicit own-entity policy preserves own objects with sensors disabled");
            c.That(game.Seat(0).Snapshot.Removed.Contains(near), "lost sight publishes removal for previously seen entity only");
            c.That(retained.Entities.Any(e => e.Id == near), "retained immutable snapshot keeps its captured seen entity");
            c.Equal(history.ObservedTick, History(game).ObservedTick, "disabled sight preserves actual remembered work");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "disabled entity rule has no second visibility codec");
            Control(c, game, 0, machine.Id, true, 2);
            c.That(Seen(game, near) && game.Seat(0).Snapshot.Spawned.Contains(near), "ordinary re-enable reacquires current entity, not last-seen fiction");
            Move(c, game, new TileCoord(2, 10)); twin = factory.Load(game.Save());
            Reach(game, () => !Seen(game, near), "physical departure removes target", twin);
            c.Equal(game.Hash(), twin.Hash(), "physical departure saved continuation agrees");
            c.That(game.Seat(0).Snapshot.Removed.Contains(near), "moving away publishes actual lost-sight identity");
            VerifyHidden(c, game, near, "departed sight");
            c.That(!retained.Entities.Any(e => e.Id == far), "held snapshot never accumulated hidden prototype siblings");
            var fresh = LiveSensorTests.New(factory);
            c.That(!Seen(fresh, near) && fresh.Seat(0).View.EntityCount == 2, "interleaved new world borrows no seen identities or sensor positions");
            Invalid(c); Conjunctive(c);
        }
        private static void Conjunctive(Check c)
        {
            var game = LiveSensorTests.New(Factory(policy: "survey.locked-sight"));
            Move(c, game, new TileCoord(28, 10));
            Reach(game, () => Machine(game).Get<IMotion>().Status == MoveStatus.Arrived, "locked policy physical arrival");
            var values = new List<int>();
            c.Equal(SpatialLayerReadStatus.Success, game.Seat(0).View.Feature<TileMapView>().ReadLayer("survey.terrain", new TileRect(30, 10, 1, 1), values), "physical sensor really covers locked foreign location");
            c.That(!Seen(game, Foreign(game, 30).Id), "active sensor cannot bypass another matching knowledge restriction");
            c.Equal(2, game.Seat(0).View.EntityCount, "conjunctive refusal applies to count as well as lookup");
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-prototype", "missing-sensor", "nonspatial" })
            {
                var sight = Content.Json.Parse(FrameworkProofContent.Read("native-entity-sight.json"));
                var world = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-world.json"));
                if (mode == "missing-prototype") sight.Get("entity_sensor_visibility")[0].Set("definition", JsonValue.Of("missing"));
                if (mode == "missing-sensor") sight.Get("entity_sensor_visibility")[0].Set("sensors", Content.Json.Parse("[\"missing\"]"));
                if (mode == "nonspatial")
                {
                    sight.Get("entity_sensor_visibility")[0].Set("definition", JsonValue.Of("survey.site"));
                    world.Get("entities")[0].Get("components").Remove("tile_presence");
                }
                c.Throws<ContentException>(() => LiveSensorTests.New(Factory(sight: sight.ToString(), world: world.ToString())), "invalid authored entity sight " + mode);
            }
        }
    }
}
