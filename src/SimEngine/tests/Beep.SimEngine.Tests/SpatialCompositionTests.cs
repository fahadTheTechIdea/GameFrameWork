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
    // Explicit JSON composition and normal player commands/views/clock/saves/assertions only.
    internal static class SpatialCompositionTests
    {
        internal const string Case = "Spatial composition: selected tile motion requires actual providers before initialization without map fallbacks";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(SpatialCompositionModule probe, bool independentSpace = false, bool independentMover = false,
            Action<JsonValue, JsonValue> edit = null)
        {
            var definition = new GameDefinition("spatial-composition", new GameModule[] { new MotionModule(), new TileNavigationModule(), new WorldSetupModule(), probe });
            var world = Json.Parse(FrameworkProofContent.Read("spatial-composition-world.json"));
            var routes = Json.Parse(FrameworkProofContent.Read("spatial-composition-routes.json"));
            if (independentSpace) { world.Set("independent_tile_spaces", world.Get("tile_spaces")); world.Remove("tile_spaces"); }
            if (independentMover)
            { var parts = world.Get("entities")[0].Get("components"); parts.Set("independent_mover", parts.Get("mover")); parts.Remove("mover"); }
            edit?.Invoke(world, routes);
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("spatial-composition-master.json"))
                .Add("spatial-composition-world.json", world.ToString()).Add("spatial-composition-routes.json", routes.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame Session(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "spatial-composition", Seed = 927 }; request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        private static JsonValue Scenario(JsonValue world) => world.Get("scenarios")[0];
        private static void NoNavigation(JsonValue world)
            => Scenario(world).Set("modules", Json.Parse("[\"sim.world-setup\",\"proof.spatial-composition\"]"));
        private static void Profile(JsonValue world, string id)
            => Scenario(world).Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of(id));
        private static EntityView Cart(SimGame game) => game.Seat(0).Snapshot.Entities.Single(x => x.Def == "spatial.cart");
        private static TileSpatialSnapshot Position(SimGame game) => (TileSpatialSnapshot)Cart(game).Spatial;
        private static MotionView Motion(SimGame game) => game.Seat(0).View.Feature<MotionView>();
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("spatial composition update=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static SimGame Input(Check c, GameFactory factory, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary movement input admits through original rules");
            c.Equal(hash, game.Hash(), "preview changes no authoritative state/RNG");
            game.Seat(0).Submit(command); return factory.Load(game.Save());
        }
        private static void Run(Check c)
        {
            foreach (bool independentSpace in new[] { false, true })
                foreach (bool independentMover in new[] { false, true })
                {
                    foreach (string invalid in new[] { "absent-space", "unselected-space", "absent-navigation", "missing-initial-class", "missing-allowed-class", "no-instances", "foreign-navigation", "malformed-choices" })
                    {
                        var probe = new SpatialCompositionModule();
                        c.Throws<ContentException>(() => Session(Factory(probe, independentSpace, independentMover, (world, routes) => {
                            if (invalid == "absent-space") { Scenario(world).Set("space", JsonValue.Of("spatial.absent")); NoNavigation(world); Profile(world, "spatial.empty"); }
                            if (invalid == "unselected-space") { Scenario(world).Set("space", JsonValue.Null()); Profile(world, "spatial.empty"); }
                            if (invalid == "absent-navigation") NoNavigation(world);
                            if (invalid == "missing-initial-class") routes.Get("uniform_tile_navigation")[0].Set("id", JsonValue.Of("spatial.other"));
                            if (invalid == "missing-allowed-class") routes.Set("uniform_tile_navigation", Json.Parse("[{\"id\":\"spatial.ground\",\"space\":\"spatial.tiles\",\"cost\":10}]"));
                            if (invalid == "no-instances") { NoNavigation(world); Profile(world, "spatial.empty"); }
                            if (invalid == "foreign-navigation") { NoNavigation(world); world.Get("spatial_composition_probes")[0].Set("mode", JsonValue.Of(invalid)); }
                            if (invalid == "malformed-choices")
                            {
                                var mover = world.Get("entities")[0].Get("components").Get(independentMover ? "independent_mover" : "mover");
                                if (independentMover) mover.Set("proof_fault", JsonValue.Of("classes"));
                                else mover.Set("classes", JsonValue.NewArray());
                            }
                        })), "native/independent space=" + independentSpace + " mover=" + independentMover + " rejects " + invalid);
                        c.Equal(0, probe.Initialize.Initializations, "invalid spatial composition never initializes");
                    }
                    Physical(c, independentSpace, independentMover);
                }
            var localProbe = new SpatialCompositionModule();
            var localFactory = Factory(localProbe, independentSpace: true, independentMover: true,
                edit: (world, routes) => { NoNavigation(world); Scenario(world).Set("space", JsonValue.Null()); Profile(world, "spatial.local-only"); });
            var local = Session(localFactory);
            c.That(localProbe.Initialize.Initializations == 1 && local.Modules.Count == 3 && local.Observer.Space == null, "nonspatial selected motion has no invented map or navigation module");
            c.That(local.Seat(0).Snapshot.Entities.Count == 1 && local.Seat(0).Snapshot.Entities.Single().Spatial == null, "unselected-space content does not impose tile providers on a local entity");
            var localTwin = localFactory.Load(local.Save()); Step(local, localTwin);
            c.Equal(local.Hash(), localFactory.Load(local.Save()).Hash(), "nonspatial selected motion saves without tile state");

            // Existing independent point motion executes through the same motion phase, never tile navigation.
            var pointFactory = PlanetXProductionTests.Factory(); var point = PlanetXProductionTests.New(pointFactory);
            var limits = new ReplayLimits(4 * 1024 * 1024, 1000); var replay = point.RecordReplay(limits);
            c.That(point.Modules.All(x => x != TileNavigationModule.ModuleId) && point.Observer.Space is PointWorldSnapshot, "non-tile physical session composes without native tile router");
            var command = PlanetXProductionTests.Queue(point, "smelter", "space.smelt.planetx");
            ulong before = point.Hash(); c.Equal(Code.None, point.Seat(0).Preview(command), "non-tile ordinary physical production preview remains admitted");
            c.Equal(before, point.Hash(), "non-tile preview remains pure"); point.Seat(0).Submit(command); var pointTwin = pointFactory.Load(point.Save());
            for (int i = 0; i < 256 && PlanetXProductionTests.Stock(point, "smelter", "alloy") != Fix.FromInt(6); i++) PlanetXProductionTests.Step(point, pointTwin);
            c.Equal(Fix.FromInt(6), PlanetXProductionTests.Stock(point, "smelter", "alloy"), "actual non-tile motion physically supplies production without a tile fallback");
            c.That(new ReplayVerifier().Verify(pointFactory, replay.Finish(), limits).Success, "non-tile normal production/physical motion replay");
        }
        private static void Physical(Check c, bool independentSpace, bool independentMover)
        {
            var probe = new SpatialCompositionModule(); var factory = Factory(probe, independentSpace, independentMover); var game = Session(factory);
            c.That(probe.Initialize.Initializations == 1 && game.Modules.Count == 4, "valid explicit four-module composition initializes");
            c.Equal(new TileCoord(1, 2), Position(game).Tile, "native world setup uses exact authored placement");
            c.Equal(new Code("spatial.idle"), Cart(game).State, "authored initial entity state");
            c.That(game.Seat(0).Snapshot.Entities.Single(x => x.Def == "spatial.local").Spatial == null, "nonspatial entity can coexist with selected map");
            c.Equal(Code.None, Motion(game).ReadNavigation(Cart(game).Id, out var navigation), "original motion navigation view");
            c.That(navigation.Profile == "spatial.ground" && navigation.Allowed.SequenceEqual(new[] { "spatial.ground", "spatial.heavy" }), "actual initial and alternate classes are explicit");
            var blocked = new MoveVehicleCommand { Vehicle = Cart(game).Id, To = new TileCoord(3, 2) };
            ulong before = game.Hash(); c.Equal(MoveVehicleHandler.Unreachable, game.Seat(0).Preview(blocked), "occupied destination remains typed route refusal");
            c.Equal(before, game.Hash(), "refused route changes no state");
            var limits = new ReplayLimits(2 * 1024 * 1024, 512); var replay = game.RecordReplay(limits);
            var move = new MoveVehicleCommand { Vehicle = Cart(game).Id, To = new TileCoord(7, 2) }; var twin = Input(c, factory, game, move); Step(game, twin);
            c.That(Position(game).Position != FixVec2.Centre(new TileCoord(1, 2)) && Position(game).Tile != move.To, "accepted command advances actual travel over game time, never teleport");
            c.Equal(new Code("spatial.moving"), Cart(game).State, "Game.Update resolves movement entity state");
            c.Equal(Code.None, Motion(game).ReadArrival(Cart(game).Id, out var arrival), "actual physical arrival estimate");
            c.That(arrival.Status == MoveStatus.Moving && arrival.RemainingTicks > 0, "route has remaining game time");
            var retained = Position(game); twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial route restores its original motion owner");
            twin = Input(c, factory, game, new ConfigureMotionNavigationCommand { Entity = Cart(game).Id, Profile = "spatial.heavy" }); Step(game, twin);
            bool enteredObstacle = false; int updates = 0;
            for (; updates < 96 && Position(game).Position != FixVec2.Centre(move.To); updates++)
            { Step(game, twin); enteredObstacle |= Position(game).Tile == new TileCoord(3, 2); }
            c.That(updates < 96 && !enteredObstacle, "bounded physical replan respects original obstacle geometry");
            c.Equal(FixVec2.Centre(move.To), Position(game).Position, "original route reaches exact authored target");
            c.Equal(new Code("spatial.idle"), Cart(game).State, "entity Update settles authored idle state after arrival");
            c.That(retained.Position != Position(game).Position, "retained interpolation facts remain detached");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal motion restores");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "normal move/navigation command stream replays");
            var isolated = Session(factory); Step(isolated); Step(game, twin);
            c.Equal(new TileCoord(1, 2), Position(isolated).Tile, "interleaved new session has no route/position leakage");
        }
    }
}
