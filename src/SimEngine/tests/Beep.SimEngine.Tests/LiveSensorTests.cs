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
    public static class LiveSensorTests
    {
        public static void Register() => TestRunner.Add("Live sensors: physical coverage, controls and remembered survey separation", Run);
        private static readonly Code Sensor = new Code("survey.sensor");
        internal static GameFactory Factory(string world = null, string visibility = null, string master = null, IDictionary<string, string> additionalDocuments = null,
            IReadOnlyList<GameModule> additionalModules = null)
        {
            var modules = new List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new TileNavigationModule(),
                new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule(), new OperationsModule(), new KnowledgeVisibilityModule() };
            if (additionalModules != null) modules.AddRange(additionalModules);
            var definition = new GameDefinition("native-sensor", modules);
            var files = new PreloadedFileSource().Add("manifest.json", master ?? FrameworkProofContent.Read("native-sensor-master.json"))
                .Add("native-sensor-world.json", world ?? FrameworkProofContent.Read("native-sensor-world.json"))
                .Add("native-sensor-visibility.json", visibility ?? FrameworkProofContent.Read("native-sensor-visibility.json"))
                .Add("native-sensor-startup.json", FrameworkProofContent.Read("native-sensor-startup.json"))
                .Add("native-survey-navigation.json", FrameworkProofContent.Read("native-survey-navigation.json"))
                .Add("native-survey-work.json", FrameworkProofContent.Read("native-survey-work.json"))
                .Add("native-survey-knowledge.json", FrameworkProofContent.Read("native-survey-knowledge.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            if (additionalDocuments != null) foreach (var document in additionalDocuments) files.Add(document.Key, document.Value);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 723 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        private static TileMapView View(SimGame game, int seat) => game.Seat(seat).View.Feature<TileMapView>();
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("sensor time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " position=" + Find(game, "survey.machine").Get<ITilePresence>().Tile +
                " orders=" + string.Join(";", game.World.Services.Get<WorkBoard>().Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static bool Known(SimGame game) => game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out _);
        private static KnowledgeObservation Reading(SimGame game)
        {
            if (!game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out var value)) throw new InvalidOperationException("no survey history");
            return value;
        }
        private static void Control(Check c, SimGame game, bool enabled, int range)
        {
            var command = new ConfigureOperationCommand { Entity = Find(game, "survey.machine").Id, Operation = Sensor, Enabled = enabled, Target = Fix.FromInt(range) };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal sensor command preview");
            c.That(TestKit.Order(game, 0, command).Accepted, "normal operation owner commits sensor control");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current sensor simulation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "new component schema needs no parallel save layout");
            var factory = Factory(); var game = New(factory); var mine = View(game, 0); var other = View(game, 1);
            var machine = Find(game, "survey.machine"); var site = Find(game, "survey.site");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = true }).Accepted, "normal player hold prevents automatic home travel during stationary read checks");
            var origin = new TileCoord(2, 10); var field = new TileRect(16, 10, 1, 1);
            var values = new List<int>(); var quantities = new List<Fix>();
            c.Equal(3, game.World.Entities.All.Count, "only actual JSON startup entities");
            c.That(game.World.Content.Get<LayerSensorVisibilityDef>("survey.terrain.sight").AuthoringPath.Contains("native-sensor-visibility.json"), "actual master-listed sensor rules");
            c.That(machine.Get<ISpatialSensor>(Sensor) is TileSensor && machine.Get<IControllableOperation>(Sensor) is TileSensor, "same provider owns coverage and existing controls");
            c.That(!game.World.Services.Has<IEconomy>(), "current sight and physical survey need no Tycoon");
            c.Equal(3, mine.Layers.Count, "owned active sensor permits layer metadata");
            c.Equal(2, other.Layers.Count, "foreign active sensor does not bypass private deposit ownership");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("survey.terrain", new TileRect(origin, 1, 1), values), "current source tile is visible");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("survey.terrain", field, values), "distant field initially outside current sight");
            c.Equal(0, values.Count, "refused sample clears retained output");
            c.Equal(SpatialLayerReadStatus.Success, other.ReadLayer("survey.terrain", field, values), "other company's actual sensor has its own longer coverage");
            c.Equal(42, values[0], "current sight reads actual native sample");
            c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer("survey.deposit", field, quantities), "coverage AND ownership required for deposit");
            c.That(!game.Seat(0).View.TryGetEntity(Find(game, "survey.foreign-sensor").Id, out _), "sensor does not leak unlisted foreign entities");
            c.That(!Known(game), "live sight is not automatically remembered survey knowledge");
            var page = mine.ReadLayerPage("survey.terrain", new TileRect(origin, 1, 1), new SpatialLayerPageRequest(0, 1), values);
            c.Equal(SpatialLayerReadStatus.Success, page.Status, "initial live page");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("survey.terrain", new TileRect(2, 10, 4, 1), values), "whole mixed seen/unseen read refuses atomically");
            c.Equal(0, values.Count, "mixed samples leak no prefix");
            int baseline = mine.Version;
            ulong hash = game.Hash();
            var bad = new ConfigureOperationCommand { Entity = machine.Id, Operation = Sensor, Enabled = true, Target = Fix.Half };
            c.Equal(OperationCodes.TargetBounds, game.Seat(0).Preview(bad), "native distance refuses fractional target");
            c.Equal(OperationCodes.TargetBounds, game.Seat(0).View.Feature<OperationsView>().PreviewConfigure(machine.Id, Sensor, true, Fix.Half), "management preview shares target quantum");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(new ConfigureOperationCommand { Entity = machine.Id, Operation = Sensor, Enabled = false, Target = Fix.FromInt(2) }), "foreign sensor control stays private");
            c.Equal(hash, game.Hash(), "sample, coverage and control previews are pure");
            for (int i = 0; i < 3; i++) Update(game);
            c.Equal(baseline, mine.Version, "stable coverage does not repeatedly invalidate");

            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = false }).Accepted, "normal player release restores physical work dispatch");
            var post = new PostOrderCommand { Plan = "survey.sample", Target = site.Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(post), "work need not pre-own survey knowledge or distant sight");
            var result = TestKit.Order(game, 0, post); c.That(result.Accepted, "normal physical survey order");
            var order = game.World.Services.Get<WorkBoard>().Order((int)result.Arg0);
            Reach(game, () => machine.Get<ITilePresence>().Tile != origin, "moving sensor first tile");
            c.That(mine.Version > baseline, "actual movement invalidates coverage through existing map cursor");
            c.Equal(SpatialLayerReadStatus.InvalidCursor, mine.ReadLayerPage("survey.terrain", new TileRect(machine.Get<ITilePresence>().Tile, 1, 1), new SpatialLayerPageRequest(0, 1, page.Cursor), values).Status, "changed coverage invalidates old page version");
            c.That(!Known(game) && order.Node("sample").Progress.WholeTicks == 0, "movement gains sight but no completed survey");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "moving sensor checkpoint has actual source/operation state only");
            Reach(game, () => mine.ReadLayer("survey.deposit", field, quantities) == SpatialLayerReadStatus.Success, "machine reaches field sight", twin);
            c.Equal(game.Hash(), twin.Hash(), "restored moving coverage derives identically");
            c.Equal(Fix.FromInt(31), quantities[0], "live field sight reads actual deposit");
            c.That(!Known(game), "seeing field does not manufacture a completed observation");
            Reach(game, () => order.Node("sample").Progress.WholeTicks > 0, "actual physical survey work", twin);
            c.Equal(Fix.FromInt(12), site.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "existing work consumes eight physical materials");
            c.That(!Known(game), "partial survey remains unearned");
            twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "completed survey capture", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial native survey with live sensor resumes identically");
            var history = Reading(game); c.Equal(Fix.FromInt(31).Raw, new JsonReader(history.Data, "history").Long("value_raw"), "physical completion captures durable actual sample");

            int foreign = other.Version;
            Control(c, game, false, 2);
            c.Equal(0, mine.Layers.Count, "disabled sensor removes current layer metadata");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("survey.deposit", field, quantities), "historical survey does not substitute current sight");
            c.Equal(history.ObservedTick, Reading(game).ObservedTick, "disable leaves remembered observation chronology intact");
            c.Equal(foreign, other.Version, "one company's operation does not invalidate another's coverage");
            c.Equal(new Code("stopped"), machine.State, "entity Update settles authored sensor disabled state");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "disabled controls and history survive exact checkpoint");
            c.Equal(0, View(twin, 0).Layers.Count, "restore derives disabled visibility without fog codec");
            Control(c, game, true, 0); c.Equal(0, mine.Layers.Count, "zero execution range has explicit inactive coverage");
            Control(c, game, true, 2);
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("survey.deposit", field, quantities), "re-enabled current coverage restored");
            var oldCoverage = machine.Get<ISpatialSensor>(Sensor).ReadCoverage(game.World);
            var leave = new MoveVehicleCommand { Vehicle = machine.Id, To = origin };
            c.Equal(Code.None, game.Seat(0).Preview(leave), "normal manual physical move preview");
            c.That(TestKit.Order(game, 0, leave).Accepted, "normal move leaves completed survey site");
            twin = factory.Load(game.Save());
            Reach(game, () => machine.Get<ITilePresence>().Tile == origin && machine.Get<IMotion>().Status == MoveStatus.Arrived, "physical move away", twin);
            c.Equal(game.Hash(), twin.Hash(), "moving away saved continuation is deterministic");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("survey.deposit", field, quantities), "departed field loses live coverage");
            c.Equal(history.ObservedTick, Reading(game).ObservedTick, "departure does not forget or refresh completed history");
            c.That(oldCoverage.Key != machine.Get<ISpatialSensor>(Sensor).ReadCoverage(game.World).Key, "held geometry remains detached from moving source");
            var nextPage = mine.ReadLayerPage("survey.terrain", new TileRect(5, 10, 1, 1), new SpatialLayerPageRequest(0, 1), values);
            c.Equal(SpatialLayerReadStatus.Unavailable, nextPage.Status, "current narrow radius refuses distant tile");
            Control(c, game, true, 4);
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("survey.terrain", new TileRect(5, 10, 1, 1), values), "existing operation target expands actual native sight");
            c.Equal(history.ObservedTick, Reading(game).ObservedTick, "range controls never record new geological observations");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "range and held movement state persist in existing codecs");
            var fresh = New(factory);
            c.That(!Known(fresh) && Find(fresh, "survey.machine").Get<ITilePresence>().Tile == origin, "interleaved session borrows no knowledge or position");
            c.Equal(SpatialLayerReadStatus.Unavailable, View(fresh, 0).ReadLayer("survey.terrain", new TileRect(5, 10, 1, 1), values), "new session keeps its independently authored sensor range");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing", "duplicate", "empty" })
            {
                var visibility = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-visibility.json"));
                visibility.Get("layer_sensor_visibility")[0].Set("sensors", Content.Json.Parse(mode == "missing" ? "[\"unknown\"]" : mode == "duplicate" ? "[\"survey.sensor\",\"survey.sensor\"]" : "[]"));
                c.Throws<ContentException>(() => New(Factory(visibility: visibility.ToString())), "invalid sensor binding " + mode);
            }
            foreach (string mode in new[] { "fractional", "negative", "unit", "space" })
            {
                var world = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-world.json"));
                var sensor = world.Get("entities")[1].Get("components").Get("tile_sensor");
                if (mode == "fractional") sensor.Get("operation").Set("target", JsonValue.Number("0.5"));
                if (mode == "negative") sensor.Get("operation").Set("minimum", JsonValue.Number("-1"));
                if (mode == "unit") sensor.Get("operation").Set("unit", JsonValue.Of("unit.speed-multiplier"));
                if (mode == "space") sensor.Set("space", JsonValue.Of("missing.space"));
                c.Throws<ContentException>(() => New(Factory(world: world.ToString())), "invalid authored sensor geometry " + mode);
            }
        }
    }
}
