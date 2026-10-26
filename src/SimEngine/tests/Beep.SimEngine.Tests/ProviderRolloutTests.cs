using System;
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
    public static class ProviderRolloutTests
    {
        private const string Transformer = @"""transformer"": { ""automatic_supply"": true, ""wip_capacity"": ""fixture.count.20"", ""transformations"": [""build""],
          ""states"": { ""working"": ""processing"", ""stopped"": ""blocked"" },
          ""operation"": { ""id"": ""transformation"", ""enabled"": true, ""target"": 1,
            ""minimum"": 1, ""maximum"": 1, ""unit"": ""unit.work-rate"" } }";
        private const string Nonspatial = "{" + TestGame.TimeDefinitions + @"
          ""entity_states"": [
            { ""id"": ""ready"", ""state"": ""plain.ready"", ""priority"": 0, ""default"": true },
            { ""id"": ""processing"", ""state"": ""plain.working"", ""priority"": 20 },
            { ""id"": ""blocked"", ""state"": ""plain.blocked"", ""priority"": 60 }],
          ""entities"": [
            { ""id"": ""assembler"", ""kind"": ""entity"", ""components"": {
              ""storage"": { ""capacity_policy"": ""fixture.count.4"" }, " + Transformer + @" } },
            { ""id"": ""token"", ""kind"": ""entity"", ""components"": {} }],
          ""transformations"": [{ ""id"": ""build"", ""mode"": ""batch"", ""cancel_policy"": ""return_inputs"", ""operating_inputs"": [], ""tools"": {}, ""entity_output"": ""token"", ""output_home"": ""unassigned"", ""duration"": { ""numerator"": 5, ""denominator"": 100, ""unit"": ""hour"" } }],
          ""entity_setups"": [{ ""id"": ""assembler"", ""after"": [], ""entity"": ""assembler"", ""company"": 0, ""placement"": null, ""home"": null }],
          ""world_setup_profiles"": [{ ""id"": ""rollout.start"", ""entries"": [""assembler""], ""includes"": [] }],
          ""game_modes"": [{ ""id"": ""provider-rollout.resource-only"", ""name"": ""Physical diagnostic game"", ""properties"": {} }],
          ""scenarios"": [{ ""id"": ""points"", ""game_mode"": ""provider-rollout.resource-only"", ""mode_overrides"": {}, ""time"": ""test.minute"", ""modules"": [""sim.work"",""sim.transformations"", ""points"", ""sim.world-setup""], ""seats"": 1,
            ""settings"": {""sim.work"": {""retry"": ""proof.retry""}, ""sim.world-setup"": {""profile"": ""rollout.start""}} }]
        }";

        public static void Register()
        {
            TestRunner.Add("Provider rollout: timed non-tile outputs, congestion and non-spatial saves", NonTile);
            TestRunner.Add("Provider rollout: tile result contracts, sequential output and fault isolation", Tiles);
        }
        private static string Insert(string json, string key, string entries)
        {
            string anchor = "\"" + key + "\": [";
            if (!json.Contains(anchor)) throw new InvalidOperationException("missing fixture anchor " + key);
            return json.Replace(anchor, anchor + entries + ",");
        }
        private static Entity Find(SimGame game, string id)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity;
            throw new InvalidOperationException("missing proof entity " + id);
        }
        private static int Count(SimGame game, string id)
        {
            int count = 0;
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) count++;
            return count;
        }
        private static void Reach(SimGame game, Func<bool> ready, int limit, SimGame twin = null)
        {
            for (int i = 0; i < limit && !ready(); i++) { game.Update(); twin?.Update(); }
            if (!ready()) throw new InvalidOperationException("rollout stalled at tick " + game.Tick + "; fault=" + game.Fault);
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 11 });

        private static void NonTile(Check c)
        {
            string json = Insert(PointGame.Definitions, "entities", @"
              { ""id"": ""assembler"", ""kind"": ""entity"", ""components"": {
                ""point_presence"": { ""space"": ""field.points"", ""position"": 10 },
                ""point_stock"": { ""resource"": ""supply"", ""capacity_policy"": ""fixture.count.40"", ""initial"": 20 }, " + Transformer + @" } },
              { ""id"": ""built"", ""kind"": ""entity"", ""components"": {
                ""point_presence"": { ""space"": ""field.points"", ""position"": 77 },
                ""point_motion"": { ""rate"": 60, ""states"": { ""moving"": ""travel"", ""stopped"": ""blocked"" } } } }");
            JsonValue startup = Content.Json.Parse(json);
            StartupContentAuthoring.Add(startup, "assembler", "assembler", 0, Content.Json.Parse("{\"space\":\"field.points\",\"position\":10}"));
            json = json.Replace("\"activities\": [", @"""transformations"": [{ ""id"": ""build"", ""mode"": ""batch"", ""cancel_policy"": ""return_inputs"", ""operating_inputs"": [], ""tools"": {}, ""inputs"": { ""supply"": 2 },
                ""outputs"": { ""supply"": 1 }, ""entity_output"": ""built"", ""output_home"": ""unassigned"", ""duration"": { ""numerator"": 1, ""denominator"": 10, ""unit"": ""hour"" } }], ""activities"": [");
            JsonValue completed = Content.Json.Parse(json); completed.Set("entity_setups", startup.Get("entity_setups")); completed.Set("world_setup_profiles", startup.Get("world_setup_profiles"));
            GameFactory factory = PointGame.Factory(completed.ToString()); SimGame game = New(factory);
            Entity assembler = Find(game, "assembler"); Transformer part = assembler.Get<Transformer>();
            TransformationManagementView view = game.Seat(0).View.Feature<TransformationManagementView>(); ResourceId supply = game.World.Content.ResourceId("supply");
            ulong before = game.Hash();
            EntityOutputPreview preview = view.PreviewEntityOutput(assembler.Id, new Code("transformation"), "build");
            c.Equal(Code.None, preview.Refusal, "engine preview finds non-tile output placement");
            c.Equal("built", preview.Output, "detached preview identifies the authored result");
            c.Equal(Fix.FromInt(11), ((PointPlacement)preview.Placement).Position, "provider applies JSON rollout offset to transformer's actual position");
            c.Equal(before, game.Hash(), "rollout preview does not reserve, move, consume or sample randomness");
            SimGame foreign = New(factory);
            c.Equal(Codes.NotFound, EntityRollout.Find(game.World, Find(foreign, "assembler"), game.World.Content.Get<EntityDef>("built"), out _),
                "foreign live transformer refuses before provider decisions");
            c.Equal("built", view.TransformationEntityOutput("build"), "transformation view is not vehicle-only");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = assembler.Id, Transformation = "build", Count = 2 }).Accepted,
                "ordinary queue command starts two timed entity results");
            c.That(part.Progress.WholeTicks > 0 && part.Progress.WholeTicks < 6 && Count(game, "built") == 0, "manufacturing is not an instant spawn");
            c.Equal(Fix.FromInt(18), assembler.Get<IInventory>().Amount(supply), "only first queued batch consumes its inputs");
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "player closes actual provider access");
            Reach(game, () => part.Phase == TransformationPhase.Blocked, 10);
            c.Equal(Codes.Blocked, part.Blocker, "completed batch waits on typed provider congestion");
            c.Equal(6L, part.Progress.WholeTicks, "blocked output retains completed work");
            c.Equal(2, part.Queue.Count, "unreleased result remains the same queue head");
            c.Equal(0, Count(game, "built"), "blocked rollout creates no entity");
            c.Equal(Fix.FromInt(18), assembler.Get<IInventory>().Amount(supply), "blocked mixed output publishes no resource output");
            before = game.Hash(); EntityOutputPreview blocked = view.PreviewEntityOutput(assembler.Id, new Code("transformation"), "build");
            c.Equal(Codes.Blocked, blocked.Refusal, "view reports current provider blocker");
            c.That(blocked.Placement == null, "blocked preview contains no fabricated coordinate");
            c.Equal(Code.None, game.Seat(0).Preview(new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = assembler.Id, Transformation = "build", Count = 1 }),
                "temporary congestion does not reject a valid persistent queue request");
            c.Equal(before, game.Hash(), "both blocked placement and queue previews are pure");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "blocked queue/progress/access restore with existing owners");
            for (int i = 0; i < 4; i++) { game.Update(); twin.Update(); }
            c.Equal(Fix.FromInt(18), assembler.Get<IInventory>().Amount(supply), "waiting never reconsumes input or emits repeated byproduct");
            TestKit.Order(game, 0, new SetPointAccessCommand { Open = true });
            TestKit.Order(twin, 0, new SetPointAccessCommand { Open = true });
            c.Equal(1, Count(game, "built"), "opening access releases exactly one completed entity");
            Entity built = Find(game, "built");
            c.Equal(Fix.FromInt(11), built.Get<PointPresence>().Position, "new presence uses legal rollout instead of prototype position 77");
            c.Equal(built.Get<PointPresence>().Position, ((PointSnapshot)built.Get<ISpatialPresence>().Capture()).Previous, "new entity starts with consistent interpolation coordinates");
            c.Equal(new Code("point.ready"), built.State, "newborn state settles through entity Update");
            c.That(built.Kind == EntityKind.Generic && built.Get<IMobility>() == null && built.Get<IMotion>() != null, "generic result uses an independent non-tile motion capability");
            c.Equal(Fix.FromInt(19), assembler.Get<IInventory>().Amount(supply), "resource output commits once alongside the released entity");
            c.Equal(1, part.Queue.Count, "first release does not complete the next queued entry");
            Reach(game, () => part.Queue.Count == 0, 10, twin);
            c.Equal(2, Count(game, "built"), "sequential queue creates exactly two results");
            c.Equal(Fix.FromInt(18), assembler.Get<IInventory>().Amount(supply), "each batch consumes two and returns one exactly once");
            c.Equal(game.Hash(), twin.Hash(), "saved blocked twins continue through both releases identically");
            c.Equal(Fix.FromInt(11), ((PointPlacement)preview.Placement).Position, "retained placement preview remains detached and immutable");
            c.That(!game.World.Services.TryGet<TileMap>(out _) && !game.World.Services.TryGet<NavService>(out _), "no tile service is composed for non-tile manufacturing");
            c.That(game.Fault == null && twin.Fault == null, "bounded scenario finishes without an engine fault");

            GameFactory plainFactory = PointGame.Factory(Nonspatial); SimGame plain = New(plainFactory);
            Entity plainAssembler = Find(plain, "assembler"); TransformationManagementView plainView = plain.Seat(0).View.Feature<TransformationManagementView>();
            c.That(plain.World.Space == null && plain.Snapshot.Space == null, "non-spatial transformation invents no space");
            EntityOutputPreview plainPreview = plainView.PreviewEntityOutput(plainAssembler.Id, new Code("transformation"), "build");
            c.That(plainPreview.Refusal.IsNone && plainPreview.Placement == null, "non-spatial output has no invented position");
            c.That(TestKit.Order(plain, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = plainAssembler.Id, Transformation = "build", Count = 1 }).Accepted, "same queue command manufactures non-spatial result");
            SimGame plainTwin = plainFactory.Load(plain.Save());
            Reach(plain, () => Count(plain, "token") == 1, 6, plainTwin);
            c.That(Find(plain, "token").Get<ISpatialPresence>() == null, "result stays non-spatial");
            c.Equal(plain.Hash(), plainTwin.Hash(), "non-spatial queued save continues identically");
            c.Throws<ContentException>(() => New(PointGame.Factory(Nonspatial.Replace("\"entity_output\"", "\"vehicle\""))), "removed vehicle JSON field is not a compatibility alias");
            string unsupported = Nonspatial.Replace("\"entity_states\": [", @"""point_spaces"": [{ ""id"": ""unused.points"", ""extent"": 100, ""access_open"": true, ""rollout_offset"": 1 }], ""entity_states"": [")
                .Replace("\"id\": \"token\", \"kind\": \"entity\", \"components\": {}",
                    @"""id"": ""token"", ""kind"": ""entity"", ""components"": { ""point_presence"": { ""space"": ""unused.points"", ""position"": 0 } }")
                .Replace("\"storage\": { \"capacity_policy\": \"fixture.count.4\" },", @"""replacement_probe"": { ""allowed_targets"": [], ""fail_initialization"": true }, ""storage"": { ""capacity_policy"": ""fixture.count.4"" },");
            c.Throws<ContentException>(() => New(PointGame.Factory(unsupported)), "missing selected rollout space refuses composition before a startup callback can throw");
        }

        private static void Tiles(Check c)
        {
            GameFactory factory = Colony.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 1 });
            World world = game.World; Entity transformer = Find(game, "factory"), home = Find(game, "depot");
            TransformationManagementView view = game.Seat(0).View.Feature<TransformationManagementView>(); Transformer part = transformer.Get<Transformer>();
            ulong before = game.Hash(); EntityOutputPreview preview = view.PreviewEntityOutput(transformer.Id, new Code("transformation"), "build_truck");
            c.Equal(Code.None, preview.Refusal, "ordinary truck transformation resolves through optional tile rollout provider");
            c.That(preview.Placement is TilePlacement, "tile provider returns its typed immutable intent");
            c.Equal(before, game.Hash(), "tile preview does not claim ground or consume inputs");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().PreviewEntityOutput(transformer.Id, new Code("transformation"), "build_truck").Refusal, "private transformer preview does not disclose enemy placement");
            transformer.Get<IInventory>().Add(world.Content.ResourceId("steel"), Fix.FromInt(20)); // supply fixture, not execution logic
            int initial = Count(game, "truck");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = transformer.Id, Transformation = "build_truck", Count = 2, Home = home.Id }).Accepted,
                "same engine queue accepts sequential tile results");
            c.That(part.Progress.WholeTicks < 180 && Count(game, "truck") == initial, "tile output waits for real manufacturing time");
            Reach(game, () => Count(game, "truck") == initial + 1, 185);
            c.Equal(1, part.Queue.Count, "first result releases while the second still requires a complete batch");
            c.That(view.TryGetTransformation(transformer.Id, new Code("transformation"), out TransformationSnapshot facts) && facts.EntityOutput == "truck" && facts.Required == 180,
                "immutable transformation view exposes authored result and actual remaining batch duration");
            SimGame twin = factory.Load(game.Save());
            Reach(game, () => part.Queue.Count == 0, 185, twin);
            c.Equal(initial + 2, Count(game, "truck"), "tile queue emits no duplicates");
            c.Equal(game.Hash(), twin.Hash(), "saved sequential tile transformation continues identically");
            int based = 0;
            foreach (Entity entity in world.Entities.All) if (entity.Owner == transformer.Owner && entity.Def.Id == "truck" && entity.Get<IWorker>().Home == home.Id) based++;
            c.Equal(4, based, "produced workers are based through the existing home authority");

            string stationary = Colony.Defs.Replace("\"entity_output\": \"truck\"", "\"entity_output\": \"depot\"")
                .Replace("\"output_home\": \"producer\"", "\"output_home\": \"unassigned\"")
                .Replace("\"initial_stock\": { \"steel\": 50 }", "\"initial_stock\": {}"); // Authored result has no starter-stock grant.
            SimGame structures = Colony.Factory(stationary).NewGame(new NewGameRequest { Scenario = "colony", Seed = 1 });
            Entity structureFactory = Find(structures, "factory");
            EntityOutputPreview structurePreview = structures.Seat(0).View.Feature<TransformationManagementView>().PreviewEntityOutput(structureFactory.Id, new Code("transformation"), "build_truck");
            c.Equal(Code.None, structurePreview.Refusal, "stationary output is not rejected by EntityKind or mandatory mobility");
            c.Equal(Code.None, EntityRollout.Find(structures.World, structureFactory, structures.World.Content.Get<EntityDef>("depot"), out SpatialPlacement place), "provider uses stationary target footprint");
            TilePlacement tile = (TilePlacement)place;
            Entity originalDepot = Find(structures, "depot");
            structureFactory.Get<IInventory>().Add(structures.World.Content.ResourceId("steel"), Fix.FromInt(10));
            c.That(TestKit.Order(structures, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = structureFactory.Id, Transformation = "build_truck", Count = 1 }).Accepted,
                "stationary result enters the same real-time transformation queue");
            Reach(structures, () => Count(structures, "depot") == 3, 185);
            Entity result = null;
            foreach (Entity entity in structures.World.Entities.All)
                if (entity.Def.Id == "depot" && entity.Owner == structureFactory.Owner && entity.Id != originalDepot.Id) result = entity;
            if (result == null) throw new InvalidOperationException("missing produced stationary entity");
            c.Equal(new TileRect(tile.Origin, 3, 3), result.Get<ITilePresence>().Footprint, "stationary output keeps authored footprint geometry");
            c.That(ReferenceEquals(((TileMap)structures.World.Space).OccupantAt(tile.Origin), result), "stationary result claims actual provider occupancy");
            c.Equal(FixVec2.Centre(new TileCoord(tile.Origin.X + 1, tile.Origin.Y + 1)), result.Get<ITilePresence>().Position, "stationary center follows its three-tile footprint");

            string missingMotion = Colony.Defs.Replace(", \"lane_mover\": { \"class\": \"lane\", \"speed\": 6, \"states\": { \"moving\": \"moving\", \"stopped\": \"stopped\" } }", "")
                .Replace("\"entity_output\": \"truck\"", "\"entity_output\": \"module_cart\"");
            c.Throws<ContentException>(() => Colony.Factory(missingMotion).NewGame(new NewGameRequest { Scenario = "colony" }), "mobile tile output cannot substitute a fabricated navigation class");
            string unknown = Colony.Defs.Replace("\"entity_output\": \"truck\"", "\"entity_output\": \"missing\"");
            c.Throws<ContentException>(() => Colony.Factory(unknown).NewGame(new NewGameRequest { Scenario = "colony" }), "missing authored output reference refuses before play");

            string faulty = Insert(Colony.Defs, "entities", @"{ ""id"": ""faulty"", ""kind"": ""entity"", ""components"": {
              ""tile_presence"": { ""space"": ""colony.tiles"", ""width"": 1, ""height"": 1, ""blocks"": true, ""movable"": false },
              ""replacement_probe"": { ""allowed_targets"": [], ""fail_initialization"": true } } }")
                .Replace("\"duration\": { \"numerator\": 3, \"denominator\": 1, \"unit\": \"hour\" }, \"entity_output\": \"truck\"", "\"duration\": { \"numerator\": 5, \"denominator\": 100, \"unit\": \"hour\" }, \"outputs\": { \"steel\": 1 }, \"entity_output\": \"faulty\"");
            SimGame failure = Colony.Factory(faulty).NewGame(new NewGameRequest { Scenario = "colony", Seed = 1 });
            Entity failingProducer = Find(failure, "factory");
            failingProducer.Get<IInventory>().Add(failure.World.Content.ResourceId("steel"), Fix.FromInt(10));
            EntityOutputPreview faultPreview = failure.Seat(0).View.Feature<TransformationManagementView>().PreviewEntityOutput(failingProducer.Id, new Code("transformation"), "build_truck");
            c.Equal(Code.None, faultPreview.Refusal, "pure placement does not invoke result initialization");
            c.That(TestKit.Order(failure, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = failingProducer.Id, Transformation = "build_truck", Count = 1 }).Accepted, "fault probe still enters ordinary timed transformation");
            Reach(failure, () => failure.Fault != null, 6);
            c.Equal(0, Count(failure, "faulty"), "failed initialization registers no partial entity");
            c.Equal(Fix.One, failingProducer.Get<IInventory>().Amount(failure.World.Content.ResourceId("steel")), "actual resource mutation precedes result lifecycle; faulted mutation is not rolled back or published as success");
            c.That(((TileMap)failure.World.Space).OccupantAt(((TilePlacement)faultPreview.Placement).Origin) == null, "spawn cleanup releases attempted result footprint");
            c.That(failure.Fault != null && game.Fault == null, "extension failure is an explicit engine fault, isolated from other sessions");
        }
    }
}
