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
    public static class OptionalMotionTests
    {
        public static void Register()
            => TestRunner.Add("Optional motion: independent tile movement, refusals and saved sessions", Travel);
        private static GameFactory Factory(NavigationProofModule module, string json = null, string manifest = null)
        {
            var definition = new GameDefinition("navigation", new GameModule[] { new MotionModule(), new TileNavigationModule(), module });
            var files = new PreloadedFileSource().Add("manifest.json", manifest ?? FrameworkProofContent.Read("navigation-manifest.json"))
                .Add("navigation.json", json ?? FrameworkProofContent.Read("navigation.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "navigation", Seed = 23 });
        private static EntityView Cart(SimGame game)
        {
            var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(new PlayerId(0), entities);
            foreach (EntityView entity in entities) if (entity.Def == "cart") return entity;
            throw new InvalidOperationException("cart was not created by authored starts");
        }
        private static TileSpatialSnapshot Position(SimGame game) => (TileSpatialSnapshot)Cart(game).Spatial;
        private static void Travel(Check c)
        {
            var module = new NavigationProofModule(); GameFactory factory = Factory(module); SimGame game = New(factory);
            c.Equal(1, module.Initializations, "selected tile-motion session initializes normally");
            c.That(game.World.Content.HasModule(MotionModule.ModuleId) && game.World.Content.HasModule(TileNavigationModule.ModuleId), "two distinct motion/navigation owners are selected");
            c.That(!game.World.Content.HasModule(WorkModule.ModuleId), "physical work is not registered or selected");
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<WorkBoard>(), "movement needs no money or work board");
            c.That(!game.World.Services.Has<IConstructionProjects>() && !game.World.Services.Has<LogisticsService>(), "movement needs no construction/hauling coordinator");
            c.That(!game.World.Services.Has<IContentAccess>(), "movement admits through ownership without unrelated content policy");
            c.Equal(new TileCoord(1, 2), Position(game).Tile, "starting pose is authored in JSON");
            c.Equal(new Code("navigation.idle"), Cart(game).State, "initial state is authored");
            EntityId cart = Cart(game).Id; var command = new MoveVehicleCommand { Vehicle = cart, To = new TileCoord(7, 2) };
            ulong before = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary player preview admits a reachable route");
            c.Equal(before, game.Hash(), "preview does not move or change the session");
            c.Equal(MoveVehicleHandler.Unreachable, game.Seat(0).Preview(new MoveVehicleCommand { Vehicle = cart, To = new TileCoord(3, 2) }), "occupied destination refuses through the existing route rules");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary public player movement command executes");
            c.Equal(new Code("navigation.moving"), Cart(game).State, "entity settles its authored movement state after update");
            c.Equal(game.Tick - 1, Cart(game).StateSince, "state change records the executing tick index before completion");
            c.That(Position(game).Tile != command.To && Position(game).Position != FixVec2.Centre(new TileCoord(1, 2)), "travel takes game time rather than teleporting");
            game.Update(); SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "active route/interpolation saves under its new module owners");
            c.Equal(Position(game).Position, Position(restored).Position, "saved actual physical pose is preserved");
            bool hitObstacle = false; int ticks = 0;
            for (; ticks < 30 && Position(game).Position != FixVec2.Centre(command.To); ticks++)
            {
                game.Update(); restored.Update(); hitObstacle |= Position(game).Tile == new TileCoord(3, 2);
            }
            c.That(ticks < 30 && game.Fault == null && restored.Fault == null, "bounded movement completes around the obstacle without a physical fault");
            c.That(!hitObstacle, "physical route never enters the occupied obstacle");
            c.Equal(FixVec2.Centre(command.To), Position(game).Position, "existing mover reaches its exact target centre");
            c.Equal(new Code("navigation.idle"), Cart(game).State, "arrival returns to the authored default state");
            c.Equal(game.Hash(), restored.Hash(), "saved and interleaved motion twins finish identically");
            c.That(factory.Describe(game.Save()).Compatible, "save identity includes the independent motion/navigation providers");
            c.Throws<InvalidOperationException>(() => game.World.Services.Get<NavService>().Define("late", 10, _ => 10), "movement-class composition remains frozen");

            string json = FrameworkProofContent.Read("navigation.json");
            var wrongClass = new NavigationProofModule();
            c.Throws<ContentException>(() => New(Factory(wrongClass, json.Replace("\"class\": \"ground\"", "\"class\": \"missing\""))), "missing authored movement class refuses before initialization");
            c.Equal(0, wrongClass.Initializations, "class refusal runs no initialization callback");
            var noSpace = new NavigationProofModule();
            c.Throws<ContentException>(() => New(Factory(noSpace, json.Replace("\"space\": \"navigation.tiles\", \"modules\"", "\"modules\""))), "tile navigation does not fabricate a map for a mapless scenario");
            c.Equal(0, noSpace.Initializations, "absent tile-space refusal runs no initialization callback");
            var missingMotion = new NavigationProofModule();
            string incomplete = FrameworkProofContent.Read("navigation-manifest.json").Replace("\r\n", "\n")
                .Replace("    { \"id\": \"sim.motion\", \"version\": \"1.0.0\", \"documents\": [] },\n", "");
            c.Throws<ContentException>(() => New(Factory(missingMotion, manifest: incomplete)), "missing declared motion dependency refuses rather than running inert movers");
            c.Equal(0, missingMotion.Initializations, "missing module dependency runs no initialization callback");
        }
    }
}
