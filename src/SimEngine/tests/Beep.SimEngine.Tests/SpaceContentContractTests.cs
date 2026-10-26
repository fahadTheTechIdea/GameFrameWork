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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Authored inputs and ordinary player commands/Update/immutable views only.
    internal static class SpaceContentContractTests
    {
        internal const string Case = "Space content contracts: independent JSON worlds retain physical layers and travel";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Create(SpaceContentContractsModule module, bool independent = true, int size = 48,
            Action<JsonValue, JsonValue, JsonValue> edit = null, IReadOnlyList<GameModule> extraModules = null, Action<JsonValue> editStartup = null)
        {
            var world = Json.Parse(FrameworkProofContent.Read("native-sensor-world.json"));
            var master = Json.Parse(FrameworkProofContent.Read("native-sensor-master.json"));
            var authored = Json.Parse("{\"independent_tile_spaces\":[]}");
            var row = world.Get("tile_spaces")[0]; row.Set("width", JsonValue.Number(size.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            if (size > 48) row.Set("height", JsonValue.Number(size.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            if (independent) { authored.Get("independent_tile_spaces").Add(row.Clone()); world.Remove("tile_spaces"); }
            world.Set("rectangular_tile_regions", Json.Parse("[{\"id\":\"space.rectangle\",\"space\":\"survey.tiles\",\"x\":0,\"y\":0,\"width\":32,\"height\":20}]"));
            world.Set("painted_tile_regions", Json.Parse("[{\"id\":\"space.paint\",\"space\":\"survey.tiles\",\"tiles\":[{\"x\":16,\"y\":10}]}]"));
            world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
            world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(TileConditionsModule.ModuleId));
            master.Get("modules").Add(Json.Parse("{\"id\":\"proof.space-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.tile-navigation\",\"version\":\"1.0.0\"}],\"documents\":[\"space-contracts.json\"]}"));
            master.Get("modules").Add(Json.Parse("{\"id\":\"sim.tile-conditions\",\"version\":\"1.0.0\",\"documents\":[\"space-conditions.json\"]}"));
            edit?.Invoke(world, authored, master);
            var modules = new List<GameModule> { module, new TileConditionsModule() };
            if (extraModules != null) modules.AddRange(extraModules);
            var startup = Json.Parse(FrameworkProofContent.Read("native-sensor-startup.json")); editStartup?.Invoke(startup);
            return LiveSensorTests.Factory(world: world.ToString(), master: master.ToString(),
                additionalModules: modules,
                additionalDocuments: new Dictionary<string, string> {
                    { "native-sensor-startup.json", startup.ToString() },
                    { "space-contracts.json", authored.ToString() },
                    { "space-conditions.json", "{\"tile_layer_band_conditions\":[{\"id\":\"space.band\",\"space\":\"survey.tiles\",\"layer\":\"survey.terrain\",\"minimum\":0,\"maximum\":43}]}" } });
        }
        private static TileSpatialSnapshot Position(SimGame game) => (TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Def == "survey.machine").Spatial;
        private static TileMapView Map(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<TileMapView>();
        private static bool Done(SimGame game) => game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:survey.sample", Entity(game, "survey.site"), out _);
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 480 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " tile=" + Position(game).Tile
                + " state=" + game.Seat(0).Snapshot.Entities.Single(e => e.Def == "survey.machine").State + " done=" + Done(game));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current space composition identity");
            c.Equal(154, EngineInfo.SaveFormat, "native map and position layouts unchanged");
            c.That(typeof(TileSpaceDef).BaseType == typeof(Def) && typeof(ITileSpaceDefinition).IsAssignableFrom(typeof(TileSpaceDef))
                && typeof(IndependentTileSpaceDef).BaseType == typeof(Def), "native/independent root contracts without compatibility base");
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Sparse(c); Invalid(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new SpaceContentContractsModule(); var factory = Create(module, independent); var game = LiveSensorTests.New(factory);
            c.That(module.Aliases && module.Independent == independent && module.Initialize.Initializations == 1, "actual master-listed aliases before Initialize");
            var map = Map(game); var other = Map(game, 1); var samples = new List<int>(); var quantities = new List<Fix>();
            var origin = new TileCoord(2, 10); var field = new TileRect(16, 10, 1, 1);
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            var fresh = LiveSensorTests.New(factory); ulong isolated = fresh.Hash();
            using (var recorder = game.RecordReplay(limits))
            {
                Order(c, game, new SetWorkerHeldCommand { Worker = Entity(game, "survey.machine"), Held = true });
                c.That(map.Width == 48 && map.Height == 32 && map.Space == "survey.tiles", "original authored geometry projected");
                c.Equal(SpatialLayerReadStatus.Unavailable, map.ReadLayer("survey.terrain", field, samples), "distant terrain not leaked");
                c.Equal(SpatialLayerReadStatus.Success, other.ReadLayer("survey.terrain", field, samples), "foreign sensor sees public actual layer");
                c.Equal(42, samples.Single(), "native startup wrote actual sparse public sample");
                c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer("survey.deposit", field, quantities), "private ownership not bypassed by range");
                c.That(map.Layers.Count == 3 && other.Layers.Count == 2, "immutable metadata obeys ownership and actual sensor coverage");
                var retained = Position(game); ulong hash = game.Hash();
                var page = map.ReadLayerPage("survey.terrain", new TileRect(origin, 1, 1), new SpatialLayerPageRequest(0, 1), samples);
                c.That(page.Status == SpatialLayerReadStatus.Success && game.Hash() == hash, "healthy layer reads do not mutate map/game time");
                var move = new MoveVehicleCommand { Vehicle = Entity(game, "survey.machine"), To = new TileCoord(10, 10) };
                Order(c, game, move);
                Reach(game, null, () => Position(game).Tile != origin, "ordinary physical travel starts");
                c.That(Position(game).Tile != move.To && game.Seat(0).Snapshot.Entities.Single(e => e.Def == "survey.machine").State == new Code("moving"),
                    "partial distance and authored state, no instant placement");
                c.Equal(origin, retained.Tile, "held pose is detached");
                c.Equal(SpatialLayerReadStatus.InvalidCursor, map.ReadLayerPage("survey.terrain", new TileRect(Position(game).Tile, 1, 1),
                    new SpatialLayerPageRequest(0, 1, page.Cursor), samples).Status, "physical coverage invalidates old cursor");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "in-travel original codecs restore exactly");
                long at = game.Clock.StepIndex; hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(game.Clock.StepIndex == at && game.Hash() == hash, "sole paused clock freezes physical map/motion");
                game.Clock.Resume(); twin.Clock.Resume(); Reach(game, twin, () => Position(game).Tile == move.To, "real travel arrival");
                c.That(game.Clock.StepIndex > at && !Done(game), "arrival takes game time and does not earn survey work");
                Order(c, game, new SetWorkerHeldCommand { Worker = Entity(game, "survey.machine"), Held = false });
                Order(c, twin, new SetWorkerHeldCommand { Worker = Entity(twin, "survey.machine"), Held = false });
                var work = new PostOrderCommand { Plan = "survey.sample", Target = Entity(game, "survey.site"), Priority = 60 };
                Order(c, game, work); Order(c, twin, new PostOrderCommand { Plan = work.Plan, Target = Entity(twin, "survey.site"), Priority = 60 });
                at = game.Clock.StepIndex; Reach(game, twin, () => Done(game), "physical material-paid survey completion");
                c.That(game.Clock.StepIndex >= at + 30, "half-hour work plus actual approach, not command completion");
                c.Equal(SpatialLayerReadStatus.Success, map.ReadLayer("survey.deposit", field, quantities), "arrived owned sensor sees actual private field");
                c.Equal(Fix.FromInt(31), quantities.Single(), "original resource-linked layer quantity");
                c.Equal(SpatialLayerReadStatus.Success, map.ReadLayer("survey.class", field, samples), "actual integer geology layer");
                c.Equal(-7, samples.Single(), "integer values retain native shape");
                c.That(game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out _),
                    "physical work earns existing durable knowledge");
                var facets = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, "survey.site"), facets);
                c.Equal(Fix.FromInt(12), facets.Single(f => f.Key == Storage.FacetStock && f.Subject == "steel").Value,
                    "original work and transaction consume exactly eight authored materials");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed layers/work/knowledge restore through original codecs");
                c.Equal(isolated, fresh.Hash(), "interleaved session retains original position/materials/layers");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "master-authored space and ordinary player controls replay");
            }
        }
        private static void Sparse(Check c)
        {
            var module = new SpaceContentContractsModule(); var factory = Create(module, size: 100000); var game = LiveSensorTests.New(factory);
            c.That(Map(game).Width == 100000 && Map(game).Height == 100000, "authored sparse geometry exceeds dense int cell count, no platform-size cap");
            Order(c, game, new SetWorkerHeldCommand { Worker = Entity(game, "survey.machine"), Held = true });
            var values = new List<int>();
            c.Equal(SpatialLayerReadStatus.Success, Map(game).ReadLayerPage("survey.terrain", new TileRect(2, 10, 1, 1), new SpatialLayerPageRequest(0, 1), values).Status,
                "small native page needs no full-world allocation");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "sparse authored geometry and actual allocated cells checkpoint");
        }
        private static void Invalid(Check c)
        {
            foreach (bool independent in new[] { false, true })
                foreach (string bad in new[] { "width", "height", "zero", "negative", "extra", "unused", "scenario", "layer", "region", "presence" })
                {
                    var module = new SpaceContentContractsModule();
                    c.Throws<ContentException>(() => LiveSensorTests.New(Create(module, independent, edit: (world, authored, master) => {
                        var rows = independent ? authored.Get("independent_tile_spaces") : world.Get("tile_spaces"); var row = rows[0];
                        if (bad == "width" || bad == "height") row.Remove(bad);
                        if (bad == "zero") row.Set("width", JsonValue.Number("0"));
                        if (bad == "negative") row.Set("height", JsonValue.Number("-1"));
                        if (bad == "extra") row.Set("renderer_limit", JsonValue.Number("1"));
                        if (bad == "unused") { row = row.Clone(); row.Set("id", JsonValue.Of("unused.space")); row.Set("width", JsonValue.Number("0")); rows.Add(row); }
                        if (bad == "scenario") world.Get("scenarios")[0].Set("space", JsonValue.Of("steel"));
                        if (bad == "layer") world.Get("spatial_layers")[0].Set("space", JsonValue.Of("missing.space"));
                        if (bad == "region") world.Get("rectangular_tile_regions")[0].Set("width", JsonValue.Number("49"));
                        if (bad == "presence") world.Get("entities")[1].Get("components").Get("tile_presence").Set("space", JsonValue.Of("steel"));
                    })), "space refuses authored " + independent + "/" + bad);
                    c.Equal(0, module.Initialize.Initializations, "invalid space graph before Initialize");
                }
            foreach (string bad in new[] { "width", "height", "mismatch", "unused", "duplicate", "missing-document" })
            {
                var module = new SpaceContentContractsModule();
                c.Throws<ContentException>(() => LiveSensorTests.New(Create(module, edit: (world, authored, master) => {
                    var row = authored.Get("independent_tile_spaces")[0];
                    if (bad == "width" || bad == "height" || bad == "mismatch") row.Set("proof_fault", JsonValue.Of(bad));
                    if (bad == "unused") { row = row.Clone(); row.Set("id", JsonValue.Of("unused.space")); row.Set("proof_fault", JsonValue.Of("width")); authored.Get("independent_tile_spaces").Add(row); }
                    if (bad == "duplicate") world.Set("tile_spaces", Json.Parse("[{\"id\":\"survey.tiles\",\"width\":48,\"height\":32}]"));
                    if (bad == "missing-document") master.Get("modules")[9].Get("documents").Add(JsonValue.Of("missing-space.json"));
                })), "actual space contract refuses " + bad);
                c.Equal(0, module.Initialize.Initializations, "invalid actual aliases/geometry before Initialize");
            }
            var missing = new SpaceContentContractsModule();
            c.Throws<InvalidOperationException>(() => LiveSensorTests.New(Create(missing, edit: (world, authored, master) =>
                world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("missing.module")))), "original composition refuses unknown module");
            c.Equal(0, missing.Initialize.Initializations, "unknown registered module before Initialize");
        }
    }
}
