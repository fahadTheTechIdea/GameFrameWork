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

namespace Beep.SimEngine.Tests
{
    internal static class SensorOcclusionTests
    {
        internal static void Register() => TestRunner.Add("Sensor occlusion: authored screens, physical sight and saved continuation", Run);
        private static readonly Code Sensor = new Code("survey.sensor");
        private static JsonValue Authoring()
        {
            var world = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-world.json"));
            world.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("occlusion.start"));
            world.Get("scenarios")[0].Get("settings").Get("sim.knowledge-visibility").Set("policy", JsonValue.Of("occlusion.sight"));
            for (int i = 1; i < 3; i++)
            {
                var sensor = world.Get("entities")[i].Get("components").Get("tile_sensor");
                sensor.Set("occlusion", Content.Json.Parse("[{\"layer\":\"survey.screen\",\"threshold\":2}]"));
                sensor.Get("operation").Set("target", JsonValue.Number("32"));
            }
            return world;
        }
        private static GameFactory Factory(JsonValue world = null)
        {
            var master = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-master.json"));
            var modules = master.Get("modules");
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i]; string id = module.Get("id").Text;
                if (id == "sim.world-setup") module.Get("documents").Add(JsonValue.Of("native-sensor-occlusion.json"));
                if (id == "sim.knowledge-visibility") module.Get("documents").Add(JsonValue.Of("native-sensor-occlusion-visibility.json"));
            }
            return LiveSensorTests.Factory(world: (world ?? Authoring()).ToString(), master: master.ToString(), additionalDocuments: new Dictionary<string, string> {
                ["native-sensor-occlusion.json"] = FrameworkProofContent.Read("native-sensor-occlusion.json"),
                ["native-sensor-occlusion-visibility.json"] = FrameworkProofContent.Read("native-sensor-occlusion-visibility.json") });
        }
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("occlusion step=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, SimGame twin)
        {
            for (int i = 0; i < 400 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException("occlusion travel timeout step=" + game.Tick + " position=" +
                Find(game, "survey.machine").Get<ITilePresence>().Tile + " motion=" + Find(game, "survey.machine").Get<IMotion>().Status);
        }
        private static TileSpatialSnapshot Sample(TileRect bounds) => new TileSpatialSnapshot("survey.tiles", new TileCoord(bounds.X, bounds.Y), bounds, default, default, default, false, false);
        private static void Control(Check c, SimGame game, bool enabled, int target)
        {
            var command = new ConfigureOperationCommand { Entity = Find(game, "survey.machine").Id, Operation = Sensor, Enabled = enabled, Target = Fix.FromInt(target) };
            c.Equal(Code.None, game.Seat(0).Preview(command), "same operation preview controls native sight");
            c.That(TestKit.Order(game, 0, command).Accepted, "same operation owner commits native sight control");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current occlusion engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "occlusion uses existing component/layer payloads");
            var factory = Factory(); var game = LiveSensorTests.New(factory);
            Entity machine = Find(game, "survey.machine"), foreign = Find(game, "survey.foreign-sensor");
            var sensor = machine.Get<ISpatialSensor>(Sensor); var map = game.World.Services.Get<TileMap>();
            var screen = map.Layer<ByteLayer>("survey.screen"); var view = game.Seat(0).View.Feature<TileMapView>();
            c.That(map is ITileSpatialSpace && ReferenceEquals(game.World.Space, map), "original selected map implements geometry contract without a second owner");
            c.That(screen is ITileSampleLayer && ((ITileSampleLayer)screen).Version == screen.Version, "original layer implements sample/revision contract without a copied obstacle buffer");
            var values = new List<int>(); var quantities = new List<Fix>(); var field = new TileRect(16, 10, 1, 1);
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = true }).Accepted, "player holds actual source at its authored position");
            c.That(!game.World.Services.Has<IEconomy>(), "physical sight has no Tycoon dependency");
            var definition = machine.Def.Component<TileSensorDef>(Sensor);
            c.Equal("survey.screen", definition.Occlusion.Single().Layer.Id, "actual JSON layer binding");
            c.Equal(Fix.FromInt(2), definition.Occlusion.Single().Threshold, "actual JSON native threshold");
            c.That(((Def)definition.Occlusion.Single().Layer).AuthoringPath.Contains("native-sensor-occlusion.json"), "master-selected document owns density layer");
            ulong hash = game.Hash(); var detached = sensor.ReadCoverage(game.World);
            c.That(detached.Covers(Sample(field)), "detached range geometry alone covers hidden field, not observation authority");
            c.That(!sensor.CanObserve(game.World, Sample(field)), "live native provider refuses field behind density screen");
            c.Equal(SpatialLayerReadStatus.Unavailable, view.ReadLayer("survey.terrain", field, values), "same layer visibility refuses hidden current sample");
            c.Equal(0, values.Count, "occluded sample leaks no retained values");
            c.That(!game.Seat(0).View.TryGetEntity(foreign.Id, out _), "same provider hides actual foreign entity behind screen");
            c.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Id == foreign.Id), "scoped snapshot hides occluded entity");
            c.Equal(SpatialLayerReadStatus.Success, game.Seat(1).View.Feature<TileMapView>().ReadLayer("survey.terrain", field, values), "other company's actual sensor has a clear line");
            c.Equal(42, values[0], "clear line returns actual sample");
            c.Equal(SpatialLayerReadStatus.Unavailable, game.Seat(1).View.Feature<TileMapView>().ReadLayer("survey.deposit", field, quantities), "clear sight never bypasses private layer ownership");
            c.Equal(SpatialLayerReadStatus.Success, view.ReadLayer("survey.screen", new TileRect(8, 10, 1, 1), values), "opaque endpoint itself remains visible");
            c.Equal(2, values[0], "visible screen reads actual density");
            c.That(!sensor.CanObserve(game.World, Sample(new TileRect(15, 11, 1, 2))), "whole footprint refuses when one of its cell rays is occluded");
            c.That(!sensor.CanObserve(game.World, Sample(new TileRect(-1, 10, 1, 1))), "out-of-map samples refuse rather than querying invalid cells");
            c.Equal(hash, game.Hash(), "coverage, occlusion, views and snapshots are pure");

            // Native provider protocol checks only: tests do not define a player layer-edit command or optical rules.
            int version = view.Version; var oldKey = detached.Key;
            screen.Set(new TileCoord(8, 10), 1); Update(game);
            c.That(view.Version > version, "threshold-crossing layer change invalidates stationary sensor scope");
            c.That(oldKey != sensor.ReadCoverage(game.World).Key && detached.Key == oldKey, "new key tracks actual layer version while retained metadata stays detached");
            c.Equal(SpatialLayerReadStatus.Success, view.ReadLayer("survey.terrain", field, values), "below-threshold layer no longer blocks sight");
            c.That(game.Seat(0).View.TryGetEntity(foreign.Id, out _), "layer change admits entity through same contract");
            var page = view.ReadLayerPage("survey.terrain", field, new SpatialLayerPageRequest(0, 1), values);
            c.Equal(SpatialLayerReadStatus.Success, page.Status, "clear sample page captures current cursor");
            screen.Set(new TileCoord(8, 10), 2); Update(game);
            c.Equal(SpatialLayerReadStatus.Unavailable, view.ReadLayerPage("survey.terrain", field, new SpatialLayerPageRequest(0, 1, page.Cursor), values).Status, "hidden page refuses before cursor disclosure");
            c.Equal(0, values.Count, "hidden stale page leaks no retained sample");
            c.Equal(SpatialLayerReadStatus.InvalidCursor, view.ReadLayerPage("survey.terrain", new TileRect(2, 10, 1, 1), new SpatialLayerPageRequest(0, 1, page.Cursor), values).Status, "obstruction change invalidates old cursor on still-visible page");
            c.That(!game.Seat(0).View.TryGetEntity(foreign.Id, out _), "new obstruction removes foreign entity visibility without moving source");
            var restored = factory.Load(game.Save()); c.Equal(game.Hash(), restored.Hash(), "native density/operation save keeps canonical state only");
            c.Equal(SpatialLayerReadStatus.Unavailable, restored.Seat(0).View.Feature<TileMapView>().ReadLayer("survey.terrain", field, values), "restored policy derives blocked sight, not saved fog");

            c.That(sensor.CanObserve(game.World, Sample(new TileRect(3, 11, 1, 1))), "initial diagonal ray is clear");
            screen.Set(new TileCoord(3, 10), 2); Update(game);
            c.That(!sensor.CanObserve(game.World, Sample(new TileRect(3, 11, 1, 1))), "supercover corner tie does not leak through adjacent opaque tile");
            c.That(sensor.CanObserve(game.World, Sample(new TileRect(3, 10, 1, 1))), "corner obstruction endpoint remains visible");
            screen.Set(new TileCoord(3, 10), 0); screen.Set(new TileCoord(2, 10), 2); Update(game);
            c.That(sensor.CanObserve(game.World, Sample(new TileRect(2, 12, 1, 1))), "source footprint does not self-occlude");
            screen.Set(new TileCoord(2, 10), 0); Update(game);
            Control(c, game, false, 32); c.That(!sensor.CanObserve(game.World, Sample(new TileRect(2, 10, 1, 1))), "disabled operation provides no fallback sight");
            Control(c, game, true, 2); c.That(!sensor.CanObserve(game.World, Sample(field)), "optical query still applies authored range control");
            Control(c, game, true, 32);

            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = false }).Accepted, "player release permits native physical travel");
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = machine.Id, To = new TileCoord(12, 14) }).Accepted, "normal player move travels past optical screen");
            var twin = factory.Load(game.Save()); long step = game.Clock.StepIndex;
            game.Clock.Pause(); twin.Clock.Pause(); Update(game, twin);
            c.Equal(step, game.Clock.StepIndex, "sole paused clock advances neither sight source nor travel");
            c.Equal(game.Hash(), twin.Hash(), "paused moving sources have identical canonical state");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, () => machine.Get<ITilePresence>().Tile == new TileCoord(12, 14) && machine.Get<IMotion>().Status == MoveStatus.Arrived, twin);
            c.Equal(game.Hash(), twin.Hash(), "saved native moving sensor continues identically");
            c.Equal(SpatialLayerReadStatus.Success, view.ReadLayer("survey.terrain", field, values), "physical relocation gains actual clear field sight");
            c.Equal(42, values[0], "relocated sight reads same canonical sample");
            c.That(game.Seat(0).View.TryGetEntity(foreign.Id, out _), "physical relocation gains actual foreign entity sight");
            c.That(!game.Seat(0).View.Feature<Beep.SimEngine.Views.KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out _), "seeing past screen grants no remembered survey");
            var fresh = LiveSensorTests.New(factory);
            c.Equal(SpatialLayerReadStatus.Unavailable, fresh.Seat(0).View.Feature<TileMapView>().ReadLayer("survey.terrain", field, values), "interleaved session keeps its own source and density");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing", "duplicate", "unknown", "private", "zero", "fractional", "range", "space" })
            {
                var world = Authoring(); var sensor = world.Get("entities")[1].Get("components").Get("tile_sensor");
                var entries = sensor.Get("occlusion");
                if (mode == "missing") sensor.Remove("occlusion");
                if (mode == "duplicate") entries.Add(entries[0].Clone());
                if (mode == "unknown") entries[0].Set("layer", JsonValue.Of("unknown.screen"));
                if (mode == "private") entries[0].Set("layer", JsonValue.Of("survey.deposit"));
                if (mode == "zero") entries[0].Set("threshold", JsonValue.Number("0"));
                if (mode == "fractional") entries[0].Set("threshold", JsonValue.Number("0.5"));
                if (mode == "range") entries[0].Set("threshold", JsonValue.Number("256"));
                if (mode == "space")
                {
                    world.Get("tile_spaces").Add(Content.Json.Parse("{\"id\":\"other.tiles\",\"width\":1,\"height\":1}"));
                    world.Get("spatial_layers").Add(Content.Json.Parse("{\"id\":\"other.screen\",\"space\":\"other.tiles\",\"type\":\"optical.density\",\"value_type\":\"byte\",\"visibility\":\"public\",\"unit\":\"survey.count\"}"));
                    entries[0].Set("layer", JsonValue.Of("other.screen"));
                }
                c.Throws<ContentException>(() => LiveSensorTests.New(Factory(world)), "invalid occlusion authoring " + mode);
            }
        }
    }
}
