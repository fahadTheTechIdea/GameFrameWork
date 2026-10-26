using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static class IndependentTileNavigationTests
    {
        internal const string Case = "Independent tile world: native layered routing, physical movement and public commands";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory()
        {
            var definition = new GameDefinition("independent-navigation", new GameModule[] { new IndependentTileSensorModule(),
                new WorldSetupModule(), new OperationsModule(), new CompanyKnowledgeModule(), new KnowledgeVisibilityModule(),
                new MotionModule(), new TileNavigationModule(), new TileConditionsModule() });
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("independent-tile-navigation-master.json"));
            var world = Json.Parse(FrameworkProofContent.Read("independent-tile-sensor-world.json"));
            var scenario = world.Get("scenarios")[0];
            foreach (string id in new[] { "sim.motion", "sim.tile-navigation", "sim.tile-conditions" }) scenario.Get("modules").Add(JsonValue.Of(id));
            scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("sensor.navigation-start"));
            files.Add("independent-tile-sensor-world.json", world.ToString());
            foreach (string file in new[] { "independent-tile-sensor-startup.json", "independent-tile-sensor-visibility.json",
                "independent-tile-navigation-startup.json", "independent-tile-navigation.json", "independent-tile-navigation-conditions.json" })
                files.Add(file, FrameworkProofContent.Read(file));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "independent-sensor", Seed = 723, Seats = { Controller.Human, Controller.Human } });
        private static EntityView Cart(SimGame game) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "sensor.cart");
        private static TileSpatialSnapshot Geometry(SimGame game) => (TileSpatialSnapshot)Cart(game).Spatial;
        private static void Update(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
        private static bool Visible(SimGame game, EntityId target) => game.Seat(0).View.TryGetEntity(target, out _);
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory);
            c.That(game.World.Space is SensorTileSpace && !game.World.Services.Has<TileMap>(), "actual independent provider, no native map service or wrapper");
            c.That(!game.World.Services.Has<IEconomy>(), "native routing/time needs no business module");
            var target = game.Seat(1).Snapshot.Entities.Single(e => e.Def == "sensor.target").Id;
            var origin = Geometry(game); var destination = new TileCoord(12, 14);
            c.That(!Visible(game, target), "JSON-created density screen occludes both actual company sensors");
            ulong before = game.Hash(); long step = game.Clock.StepIndex;
            var command = new MoveVehicleCommand { Vehicle = Cart(game).Id, To = destination };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal move preview uses native A* over actual independent layers/occupancy");
            c.Equal(before, game.Hash(), "route preview mutates no authoritative state");
            c.Equal(step, game.Clock.StepIndex, "route preview drives no clock");
            c.Equal(Codes.OutOfBounds, game.Seat(0).Preview(new MoveVehicleCommand { Vehicle = Cart(game).Id, To = new TileCoord(32, 14) }), "preview refuses actual selected bounds");
            c.That(!game.Seat(0).Preview(new MoveVehicleCommand { Vehicle = Cart(game).Id, To = new TileCoord(8, 10) }).IsNone, "authored layered cost blocks exact opaque destination");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary engine command starts native timed movement");
            c.That(Geometry(game).Position != origin.Position && Geometry(game).Tile != destination, "movement earns partial physical progress, never instant arrival");
            c.That(Cart(game).State == new Code("moving"), "normal entity update settles authored moving state");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending native path/presence saves on independent provider");
            step = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Update(game); Update(twin);
            c.Equal(step, game.Clock.StepIndex, "paused sole clock advances no travel");
            c.That(Geometry(game).Matches(Geometry(twin)), "paused saved geometry matches");
            game.Clock.Resume(); twin.Clock.Resume();
            int updates = 0; bool midSaved = false;
            while (Geometry(game).Position != FixVec2.Centre(destination) && updates++ < 160)
            {
                Update(game); Update(twin);
                c.Equal(game.Hash(), twin.Hash(), "native layered path/presence advances identically after save");
                var geometry = Geometry(game);
                c.That(geometry.Tile.X != 8 || geometry.Tile.Y < 8 || geometry.Tile.Y >= 13, "physical path avoids JSON-authored blocked density strip");
                if (!midSaved && updates >= 5) {
                    twin = factory.Load(game.Save()); midSaved = true;
                    c.Equal(game.Hash(), twin.Hash(), "partially travelled route restores its original native cursor");
                }
            }
            c.That(updates < 160 && midSaved, "bounded physical completion includes interrupted travel save");
            c.Equal(destination, Geometry(game).Tile, "native command reaches exact selected-space destination");
            c.Equal(FixVec2.Centre(destination), Geometry(game).Position, "actual position reaches centre through elapsed game time");
            c.That(Cart(game).State == new Code("idle"), "entity update settles arrival state");
            c.That(Visible(game, target), "physical relocation opens sight through original engine disclosure");
            var fresh = New(factory); c.That(!Visible(fresh, target), "interleaved session retains its own authored initial screen/source positions");
            c.Equal(new TileCoord(2, 11), Geometry(fresh).Tile, "fresh session has no route/coordinate contamination");
            foreach (string mode in new[] { "clock", "layer", "throw" }) OccupancyFault(c, factory, mode);
        }
        private static void OccupancyFault(Check c, GameFactory factory, string mode)
        {
            var game = New(factory); var space = (SensorTileSpace)game.World.Space; int callbacks = 0;
            space.BeforeOccupant = world => {
                callbacks++;
                if (mode == "clock") ((GameClock)world.Clock).Speed = 2;
                if (mode == "layer") ((SensorSampleLayer)space.SpatialLayers.Single()).SetSample(new TileCoord(5, 5), Fix.One);
                if (mode == "throw") throw new InvalidOperationException("proof.occupancy-original-error");
            };
            bool returned = false;
            try { game.Seat(0).Preview(new MoveVehicleCommand { Vehicle = Cart(game).Id, To = new TileCoord(12, 14) }); returned = true; }
            catch (InvalidOperationException) { }
            c.That(!returned && game.Fault?.Phase == "observation.provider", "unsafe occupancy callback produces no healthy route preview: " + mode);
            if (mode == "throw") c.That(game.Fault.Message.Contains("proof.occupancy-original-error"), "original occupancy callback error retained");
            int reads = callbacks;
            c.Throws<InvalidOperationException>(() => game.World.Services.Get<NavService>().Grid("sensor.route").Cost(new TileCoord(-1, -1)),
                "faulted navigation refuses before out-of-bounds shortcut: " + mode);
            c.Equal(reads, callbacks, "faulted navigation does not retry occupancy callback: " + mode);
            c.Equal(0, game.Update(), "faulted route cannot advance: " + mode);
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted route cannot checkpoint: " + mode);
        }
    }
}
