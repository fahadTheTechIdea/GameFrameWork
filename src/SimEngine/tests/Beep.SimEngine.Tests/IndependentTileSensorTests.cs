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
    internal static class IndependentTileSensorTests
    {
        internal const string Case = "Tile sensor contracts: independent sparse space, native control and guarded samples";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory()
        {
            var definition = new GameDefinition("independent-sensor", new GameModule[] {
                new IndependentTileSensorModule(), new WorldSetupModule(), new OperationsModule(), new CompanyKnowledgeModule(), new KnowledgeVisibilityModule() });
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("independent-tile-sensor-master.json"));
            foreach (string file in new[] { "independent-tile-sensor-world.json", "independent-tile-sensor-visibility.json", "independent-tile-sensor-startup.json" })
                files.Add(file, FrameworkProofContent.Read(file));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "independent-sensor", Seed = 723 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Source(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "sensor.source");
        private static Entity Target(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "sensor.target");
        private static SensorSampleLayer Layer(SimGame game) => (SensorSampleLayer)((ISpatialLayerProvider)game.World.Space).SpatialLayers.Single();
        private static ISpatialSensor Sensor(SimGame game) => Source(game).Get<ISpatialSensor>(new Code("sensor.sight"));
        private static void Update(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory); var layer = Layer(game); var sensor = Sensor(game);
            var target = Target(game); var sample = target.Get<ISpatialPresence>().Capture();
            c.That(game.World.Space is SensorTileSpace && !(game.World.Space is TileMap), "selected independent provider, not native map delegation");
            c.That(layer is ITileSampleLayer && !((object)layer is MapLayer), "independent sample storage, not native layer delegation");
            c.That(game.Seat(0).Snapshot.Space is TileWorldSnapshot geometry && geometry.Width == 32 && geometry.Height == 32,
                "independent provider publishes the existing detached world geometry shape");
            c.That(!game.World.Services.Has<IEconomy>(), "native sensor/control/save need no Tycoon");
            c.That(Source(game).Get<ITilePresence>() is TilePresence && target.Get<ITilePresence>() is TilePresence,
                "JSON setup uses native presence/coordinate/codec ownership on the independent provider");
            c.That(Source(game).Get<IControllableOperation>(new Code("sensor.sight")) is TileSensor, "native operation and sensor share one runtime owner");
            ulong before = game.Hash(); long step = game.Clock.StepIndex;
            c.That(sensor.CanObserve(game.World, sample), "native optical algorithm accepts independent samples");
            c.That(game.Seat(0).View.TryGetEntity(target.Id, out _), "original player disclosure admits clear foreign target");
            c.That(layer.SampleReads > 0, "native sensor actually sampled independent provider");
            c.Equal(before, game.Hash(), "sample reads leave authoritative state unchanged");
            c.Equal(step, game.Clock.StepIndex, "observations never drive clock");
            string oldKey = sensor.ReadCoverage(game.World).Key;
            // Provider protocol exercise, not a player fallback or a test-authored visibility formula.
            var obstruction = new TileCoord(8, 10); layer.SetSample(obstruction, Fix.FromInt(2)); Update(game);
            c.That(!sensor.CanObserve(game.World, sample), "native threshold blocks independent density");
            c.That(!game.Seat(0).View.TryGetEntity(target.Id, out _), "same engine disclosure hides occluded entity");
            c.That(oldKey != sensor.ReadCoverage(game.World).Key, "native scope key reads actual independent revision");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "independent sole provider codec restores exact state");
            c.That(!Sensor(twin).CanObserve(twin.World, Target(twin).Get<ISpatialPresence>().Capture()), "restored native sensor uses restored density, not saved visibility");
            c.Equal(sensor.ReadCoverage(game.World).Key, Sensor(twin).ReadCoverage(twin.World).Key, "restored revision preserves current scope identity");
            layer.SetSample(obstruction, Fix.One); Layer(twin).SetSample(obstruction, Fix.One); Update(game); Update(twin);
            c.That(game.Seat(0).View.TryGetEntity(target.Id, out _), "below-threshold sample reopens native disclosure");
            c.Equal(game.Hash(), twin.Hash(), "saved interleaved continuation stays deterministic");
            var command = new ConfigureOperationCommand { Entity = Source(game).Id, Operation = new Code("sensor.sight"), Enabled = false, Target = Fix.FromInt(32) };
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary engine operation preview admits shutdown");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary engine command shuts down native sensor");
            c.That(!game.Seat(0).View.TryGetEntity(target.Id, out _), "shutdown has no provider-specific sight fallback");
            var disabled = factory.Load(game.Save()); c.Equal(game.Hash(), disabled.Hash(), "control state and independent space save together");
            c.That(!disabled.Seat(0).View.TryGetEntity(Target(disabled).Id, out _), "restored shutdown remains authoritative");
            var fresh = New(factory);
            c.That(fresh.Seat(0).View.TryGetEntity(Target(fresh).Id, out _), "interleaved fresh session has its own native control and sample owners");
            Movement(c, factory, fresh);
            foreach (string mode in new[] { "entity", "clock" }) RelocationFault(c, factory, mode);
            foreach (string mode in new[] { "state", "restore", "clock", "throw" }) Fault(c, factory, mode);
        }
        private static void Movement(Check c, GameFactory factory, SimGame game)
        {
            var presence = Source(game).Get<ITilePresence>(); var target = Target(game).Get<ITilePresence>();
            var origin = presence.Tile; var position = presence.Position; ulong before = game.Hash(); long step = game.Clock.StepIndex;
            c.Throws<InvalidOperationException>(() => presence.Move(game.World, target.Position), "independent occupancy refuses blocked relocation before coordinates publish");
            c.Equal(origin, presence.Tile, "blocked relocation retains original tile");
            c.Equal(before, game.Hash(), "blocked relocation retains original coordinates and occupancy state");
            c.Throws<ArgumentException>(() => presence.Move(game.World, FixVec2.Centre(new TileCoord(32, 10))), "native presence bounds check uses selected contract geometry");
            var other = New(factory);
            c.Throws<ArgumentException>(() => presence.Move(other.World, FixVec2.Centre(new TileCoord(3, 10))), "foreign session cannot relocate native presence");
            c.Equal(before, game.Hash(), "invalid and foreign relocation publish no prefix");
            var destination = new TileCoord(12, 14);
            // Coordinate/occupancy protocol test only. No substitute clock, pathfinding or player teleport command.
            presence.Move(game.World, FixVec2.Centre(destination));
            c.Equal(destination, presence.Tile, "native coordinate owner accepts independent occupancy relocation");
            c.Equal(position, presence.PreviousPosition, "native interpolation history is retained until game update");
            c.Equal(FixVec2.Centre(destination), presence.Position, "native position publishes after relocation admission");
            c.Equal(step, game.Clock.StepIndex, "coordinate protocol does not advance the sole clock");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "native presence codec restores movement on independent space");
            var lastPublished = (TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Source(game).Id).Spatial;
            c.Equal(origin, lastPublished.Tile, "retained player snapshot stays at its last completed update before publication");
            var moved = (TileSpatialSnapshot)presence.Capture();
            var restored = (TileSpatialSnapshot)Source(twin).Get<ISpatialPresence>().Capture();
            c.That(moved.Matches(restored), "restored detached movement/history facts match");
            Update(game); Update(twin);
            c.Equal(presence.Position, presence.PreviousPosition, "normal game update owns interpolation-history settlement");
            c.Equal(destination, ((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Source(game).Id).Spatial).Tile,
                "normal game update publishes moved geometry to player snapshot");
            c.Equal(game.Hash(), twin.Hash(), "saved native coordinate owners continue identically");
            c.Equal(step + 1, game.Clock.StepIndex, "normal update advances only the existing game clock");
        }
        private static void Fault(Check c, GameFactory factory, string mode)
        {
            var game = New(factory); var layer = Layer(game); int callbacks = 0;
            layer.BeforeSample = () => {
                callbacks++;
                if (mode == "state" || mode == "restore") {
                    layer.SetSample(new TileCoord(8, 10), Fix.FromInt(2));
                    if (mode == "restore") layer.SetSample(new TileCoord(8, 10), Fix.Zero);
                }
                if (mode == "clock") game.Clock.Speed = 2;
                if (mode == "throw") throw new InvalidOperationException("proof.independent-sample-error");
            };
            bool returned = false;
            try { Sensor(game).CanObserve(game.World, Target(game).Get<ISpatialPresence>().Capture()); returned = true; }
            catch (InvalidOperationException) { }
            c.That(!returned && game.Fault?.Phase == "observation.provider", "unsafe sample callback latches original fault: " + mode);
            if (mode == "throw") c.That(game.Fault.Message.Contains("proof.independent-sample-error"), "first callback error retained");
            int reads = callbacks;
            c.Throws<InvalidOperationException>(() => Sensor(game).CanObserve(game.World, Target(game).Get<ISpatialPresence>().Capture()), "faulted sensor refuses without callback retry: " + mode);
            c.Equal(reads, callbacks, "faulted observation does not reenter sample provider: " + mode);
            c.Equal(0, game.Update(), "faulted session cannot update: " + mode);
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted session cannot checkpoint: " + mode);
        }
        private static void RelocationFault(Check c, GameFactory factory, string mode)
        {
            var game = New(factory); var presence = Source(game).Get<ITilePresence>(); var origin = presence.Tile;
            ((SensorTileSpace)game.World.Space).BeforeRelocate = (world, actual) => {
                if (mode == "entity") actual.Entity.Touch(); else ((GameClock)world.Clock).Speed = 2;
            };
            c.Throws<InvalidOperationException>(() => presence.Move(game.World, FixVec2.Centre(new TileCoord(3, 10))), "relocation provider cannot mutate native source/clock: " + mode);
            c.Equal(origin, presence.Tile, "invalid relocation callback publishes no coordinate prefix: " + mode);
            c.That(game.Fault?.Phase == "observation.provider", "original observation owner latches relocation contract violation: " + mode);
            c.Equal(0, game.Update(), "faulted relocation cannot resume update: " + mode);
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted relocation cannot checkpoint: " + mode);
        }
    }
}
