using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class ConnectedRolloutTests
    {
        internal const string Case = "Connected rollout: occupied launch cannot create on a disconnected remote tile";
        internal const string PhysicalCase = "Connected rollout: physical clearance resumes saved WIP without remote birth or duplication";
        internal const string PlanningCase = "Connected rollout: prospective footprints, budget and strict authored lane admission";
        internal const string PointCase = "Connected rollout: independent point launch waits for real clearance and replays";
        internal static void Register()
        { TestRunner.Add(Case, Disconnected); TestRunner.Add(PhysicalCase, Physical); TestRunner.Add(PlanningCase, Planning); TestRunner.Add(PointCase, Points); }
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == id && e.Owner == new PlayerId(0));
        private static TransformationSnapshot State(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Find(game, "berth.producer").Id, new Code("production"), out var state))
                throw new InvalidOperationException("missing original production facts");
            return state;
        }
        private static void Step(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("rollout fault/" + game.Fault); }
        private static void Twin(Check c, SimGame game, SimGame twin)
        { Step(game); Step(twin); c.Equal(game.Hash(), twin.Hash(), "original saved rollout/work/motion state matches every update"); }
        private static void Disconnected(Check c)
        {
            var factory = ProducerHomeBerthTests.Factory(data => {
                data.Get("tile_layer_setups")[0].Set("height", JsonValue.Number("1"));
                data.Get("entity_setups")[2].Get("placement").Set("y", JsonValue.Number("5"));
            });
            var game = LiveSensorTests.New(factory); var producer = Find(game, "berth.producer"); var carrier = Find(game, "berth.carrier");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = carrier.Id, Held = true }).Accepted, "ordinary player hold retains the actual launch occupant");
            var output = game.World.Content.Get<EntityDef>("berth.observer"); ulong hash = game.Hash();
            Code refusal = EntityRollout.Find(game.World, producer, output, out var placement);
            Console.WriteLine("ROLLOUT refusal=" + refusal + ";placement=" + (placement is TilePlacement tile ? tile.Origin.ToString() : "none"));
            c.Equal(Codes.Blocked, refusal, "occupied launch cannot be replaced by a free disconnected island");
            c.That(placement == null, "refused launch publishes no alternative remote placement");
            c.Equal(hash, game.Hash(), "rollout observation is pure and reserves no substitute location");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = producer.Id, Capability = new Code("production"), Transformation = "berth.worker", Count = 1 }).Accepted,
                "normal authored manufacturing can earn work while real rollout is blocked");
            for (int i = 0; i < 15; i++) Step(game);
            c.Equal(0, game.World.Entities.All.Count(e => e.Def.Id == "berth.observer"), "completed work never spawns past occupied producer access");
            c.Equal(1, State(game).QueueCount, "original queue retains completed blocked execution");
            c.Equal(TransformationPhase.Blocked, State(game).Phase, "engine reports physical result blockage, not false completion");
            c.Equal(new TileCoord(8, 5), carrier.Get<ITilePresence>().Tile, "no host move, teleport or automatic relocation clears the held occupant");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "blocked physical completion stays durable");
        }
        private static void Physical(Check c)
        {
            var factory = ProducerHomeBerthTests.Factory(data => {
                data.Get("entity_setups")[2].Get("placement").Set("y", JsonValue.Number("5"));
                data.Get("entities")[0].Get("components").Get("storage").Set("initial_stock", Json.Parse("{\"steel\":4}"));
                data.Get("entities")[1].Get("components").Get("storage").Set("initial_stock", Json.Parse("{}"));
                data.Get("entities")[0].Get("components").Get("transformer").Set("automatic_supply", JsonValue.Of(false));
                data.Get("transformations")[0].Set("inputs", Json.Parse("{\"steel\":4}"));
                data.Get("transformations")[0].Set("input_ports", Json.Parse("{\"steel\":\"stock\"}"));
            });
            var game = LiveSensorTests.New(factory); var carrier = Find(game, "berth.carrier"); var producer = Find(game, "berth.producer");
            var limits = new ReplayLimits(2 * 1024 * 1024, 1000); using var recorder = game.RecordReplay(limits);
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = carrier.Id, Held = true }).Accepted, "player holds the real launch occupant");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = producer.Id, Capability = new Code("production"), Transformation = "berth.worker", Count = 1 }).Accepted,
                "normal authored recipe acquires real input WIP");
            for (int i = 0; i < 10; i++) Step(game);
            c.Equal(TransformationPhase.Blocked, State(game).Phase, "completed work waits for physical launch capacity");
            c.Equal(Fix.FromInt(4), State(game).Wip.Stock.AmountOf(game.World.Content.ResourceId("steel")), "launch refusal retains all four actual WIP units");
            c.Equal(0, game.World.Entities.All.Count(e => e.Def.Id == "berth.observer"), "no result appears before clearance");
            var twin = factory.Load(game.Save());
            foreach (var current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new MoveVehicleCommand { Vehicle = carrier.Id, To = new TileCoord(8, 7) }).Accepted,
                "player issues ordinary movement; engine owns all travel and future creation");
            for (int i = 0; i < 25; i++)
            {
                Twin(c, game, twin);
                c.That(game.World.Entities.All.Where(e => e.Get<ITilePresence>()?.Movable == true).GroupBy(e => e.Get<ITilePresence>().Tile).All(g => g.Count() == 1), "launch/egress/parking never overlap real mobile occupants");
            }
            var output = Find(game, "berth.observer");
            c.Equal(1, game.World.Entities.All.Count(e => e.Def.Id == "berth.observer"), "exactly one completed result after real clearance");
            c.Equal(new TileCoord(7, 6), output.Get<ITilePresence>().Tile, "result physically clears launch access and reaches authored parking");
            c.Equal(0, State(game).QueueCount, "original production queue retires once");
            c.Equal(Fix.Zero, State(game).Wip.Stock.AmountOf(game.World.Content.ResourceId("steel")), "only successful conversion consumes WIP");
            c.Equal(Fix.Zero, producer.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "no input refunds, grants or duplicated stock");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "real launch wait and player movement replay identically/" + replay.Failure);
        }
        private static GameFactory PlannedFactory(int budget = 1024)
            => ProducerHomeBerthTests.Factory(data => {
                data.Get("tile_layer_setups").Add(Json.Parse("{\"id\":\"planned.channel\",\"after\":[],\"layer\":\"berth.channel\",\"x\":4,\"y\":5,\"width\":1,\"height\":3,\"value\":1,\"owner\":-1}"));
                data.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("planned.channel"));
                data.Get("entities").Add(Json.Parse("{\"id\":\"planned.wall\",\"kind\":\"structure\",\"components\":{\"tile_presence\":{\"space\":\"survey.tiles\",\"width\":2,\"height\":1,\"blocks\":true,\"movable\":false}}}"));
                var dock = data.Get("entities")[0].Get("components").Get("tile_work_dock");
                dock.Get("slots")[1].Set("y", JsonValue.Number("3")); dock.Get("rollouts")[0].Set("max_expanded", JsonValue.Number(budget.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            });
        private static void Planning(Check c)
        {
            foreach (int budget in new[] { 1, 1024 })
            {
                var factory = PlannedFactory(budget); var game = LiveSensorTests.New(factory); var world = game.World;
                var producer = Find(game, "berth.producer"); var output = world.Content.Get<EntityDef>("berth.observer");
                var origin = new TilePlacement(world.Space.Id, new TileCoord(4, 4), FixVec2.Centre(new TileCoord(4, 4)));
                ulong hash = game.Hash(); int count = world.Entities.All.Count;
                Code refusal = EntityRollout.Find(world, producer.Def, origin, output, Array.Empty<PlannedEntityPlacement>(), out var placement);
                c.Equal(budget == 1 ? EntityRollout.Pending : Code.None, refusal, "explicit search budget never invents a reachable launch");
                c.That(budget == 1 ? placement == null : placement is TilePlacement at && at.Origin == new TileCoord(4, 5), "only authored launch origin can be selected");
                var wall = new PlannedEntityPlacement(world.Content.Get<EntityDef>("planned.wall"), new TilePlacement(world.Space.Id, new TileCoord(3, 6), FixVec2.Centre(new TileCoord(3, 6))));
                c.Equal(Codes.Blocked, EntityRollout.Find(world, producer.Def, origin, output, new[] { wall }, out var blocked), "full two-cell planned wall prevents egress, not just its origin");
                c.That(blocked == null, "blocked planned prefix publishes no result");
                c.Equal(hash, game.Hash(), "prospective route/prefix/budget checks are pure");
                c.Equal(count, world.Entities.All.Count, "no temporary source or output is created for navigation");
            }
            foreach (string invalid in new[] { "missing", "start", "exit", "parking", "unknown-output", "stationary-output", "empty", "duplicate", "zero", "null" })
                c.Throws<ContentException>(() => LiveSensorTests.New(ProducerHomeBerthTests.Factory(data => {
                    var dock = data.Get("entities")[0].Get("components").Get("tile_work_dock"); var lane = dock.Get("rollouts")[0];
                    if (invalid == "missing") dock.Remove("rollouts");
                    if (invalid == "start") lane.Set("starts", Json.Parse("[\"missing\"]"));
                    if (invalid == "exit") lane.Set("exits", Json.Parse("[\"service\"]"));
                    if (invalid == "parking") lane.Set("starts", Json.Parse("[\"parking\"]"));
                    if (invalid == "unknown-output") lane.Set("outputs", Json.Parse("[\"missing\"]"));
                    if (invalid == "stationary-output") lane.Set("outputs", Json.Parse("[\"berth.source\"]"));
                    if (invalid == "empty") lane.Set("outputs", Json.Parse("[]"));
                    if (invalid == "duplicate") lane.Set("starts", Json.Parse("[\"service\",\"service\"]"));
                    if (invalid == "zero") lane.Set("max_expanded", JsonValue.Number("0"));
                    if (invalid == "null") dock.Set("rollouts", Json.Parse("null"));
                })), "invalid or missing actual launch metadata refuses before Initialize/" + invalid);
            var colony = Colony.Factory().NewGame(new NewGameRequest { Scenario = "colony", Seed = 1 });
            c.That(colony.Fault == null, "reference colony loads explicit material service, parking and launch geometry");
            c.Equal(Code.None, EntityRollout.CheckDefinition(colony.World, Find(colony, "factory").Def, colony.World.Content.Get<EntityDef>("truck")),
                "reference colony mobile producer uses the same declared rollout contract");
        }
        private static void Points(Check c)
        {
            var factory = TransformationHomePolicyTests.IndependentFactory(editWorld: root => StartupContentAuthoring.Position(root, "machine", JsonValue.Number("31")));
            var game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 509 }); var body = Find(game, "machine"); var producer = Find(game, "emitter");
            var limits = new ReplayLimits(2 * 1024 * 1024, 1000); using var recorder = game.RecordReplay(limits);
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = body.Id, Held = true }).Accepted, "independent real launch occupant is manually held");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = producer.Id, Capability = new Code("pulse"), Transformation = "pulse", Count = 1 }).Accepted,
                "independent processor earns ordinary timed production");
            for (int i = 0; i < 6; i++) Step(game);
            c.Equal(0, game.World.Entities.All.Count(e => e.Def.Id == "home.worker"), "independent provider cannot scan past its occupied launch");
            c.Equal(Codes.Blocked, EntityRollout.Find(game.World, producer, game.World.Content.Get<EntityDef>("home.worker"), out var placement), "independent geometry returns actual physical refusal");
            c.That(placement == null, "independent refusal exposes no remote placement");
            var twin = factory.Load(game.Save());
            foreach (var current in new[] { game, twin })
            {
                c.That(TestKit.Order(current, 0, new SetHomeCommand { Worker = body.Id, Home = Find(current, "destination").Id }).Accepted, "ordinary future home intent selects an engine-owned route");
                c.That(TestKit.Order(current, 0, new SetWorkerHeldCommand { Worker = body.Id, Held = false }).Accepted, "normal release permits physical exit");
            }
            for (int i = 0; i < 10; i++) Twin(c, game, twin);
            c.Equal(1, game.World.Entities.All.Count(e => e.Def.Id == "home.worker"), "one real independent output after clearance, no duplicate");
            c.That(body.Get<PointPresence>().Position < Fix.FromInt(30), "original occupant really travels rather than being relocated by a rollout query");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "independent blocked launch and physical release replay/" + replay.Failure);
        }
    }
}
