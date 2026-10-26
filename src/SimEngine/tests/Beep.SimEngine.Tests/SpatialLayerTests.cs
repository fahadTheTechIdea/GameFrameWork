using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;

namespace Beep.SimEngine.Tests
{
    public static partial class SpatialLayerTests
    {
        private const string Definitions = "{" + TestGame.TimeDefinitions + @"
          ""entity_states"": [{ ""id"": ""idle"", ""state"": ""state.idle"", ""priority"": 0, ""default"": true }],
          ""tile_spaces"": [{ ""id"": ""layers.tiles"", ""width"": 70000, ""height"": 70000 }],
          ""units"": [
            { ""id"": ""unit.metre"", ""dimension"": ""length"", ""scale"": { ""numerator"": 1, ""denominator"": 1 }, ""discrete"": false },
            { ""id"": ""unit.kilogram"", ""dimension"": ""mass"", ""scale"": { ""numerator"": 1, ""denominator"": 1 }, ""discrete"": false },
            { ""id"": ""unit.tonne"", ""dimension"": ""mass"", ""scale"": { ""numerator"": 1000, ""denominator"": 1 }, ""discrete"": false },
            { ""id"": ""unit.litre"", ""dimension"": ""volume"", ""scale"": { ""numerator"": 1, ""denominator"": 1 }, ""discrete"": false },
            { ""id"": ""unit.classification"", ""dimension"": ""classification"", ""scale"": { ""numerator"": 1, ""denominator"": 1 }, ""discrete"": true }
          ],
          ""resources"": [
            { ""id"": ""resource.ore"", ""unit"": ""unit.kilogram"", ""category"": ""mineral"", ""phase"": ""solid"", ""traits"": [], ""capacity"": { ""unit.kilogram"": 1 } },
            { ""id"": ""resource.fresh_water"", ""unit"": ""unit.litre"", ""category"": ""fluid"", ""phase"": ""liquid"", ""traits"": [], ""capacity"": { ""unit.litre"": 1 } }
          ],
          ""spatial_layers"": [
            { ""id"": ""elevation"", ""space"": ""layers.tiles"", ""type"": ""terrain.elevation"", ""value_type"": ""fixed"", ""visibility"": ""public"", ""unit"": ""unit.metre"", ""vertical_domain"": ""surface"", ""vertical_unit"": ""unit.metre"", ""vertical_reference"": ""datum.sea_level"", ""metadata"": { ""model"": { ""id"": ""heightfield"" } } },
            { ""id"": ""deposit"", ""space"": ""layers.tiles"", ""type"": ""resource.deposit"", ""value_type"": ""fixed"", ""visibility"": ""company_private"", ""unit"": ""unit.tonne"", ""resource"": ""resource.ore"", ""resource_measure"": ""quantity"", ""vertical_domain"": ""subsurface"", ""vertical_unit"": ""unit.metre"", ""vertical_reference"": ""datum.surface"", ""vertical_offset"": -1200 },
            { ""id"": ""lake"", ""space"": ""layers.tiles"", ""type"": ""water.level"", ""value_type"": ""fixed"", ""visibility"": ""public"", ""unit"": ""unit.metre"", ""resource"": ""resource.fresh_water"", ""resource_measure"": ""associated"", ""vertical_domain"": ""water.surface"", ""vertical_unit"": ""unit.metre"", ""vertical_reference"": ""datum.sea_level"" },
            { ""id"": ""terrain"", ""space"": ""layers.tiles"", ""type"": ""terrain.classification"", ""value_type"": ""byte"", ""visibility"": ""public"", ""unit"": ""unit.classification"" },
            { ""id"": ""strata"", ""space"": ""layers.tiles"", ""type"": ""geology.stratum"", ""value_type"": ""int"", ""visibility"": ""public"", ""unit"": ""unit.classification"" }
          ],
          ""game_modes"": [{ ""id"": ""spatial-layer.resource-only"", ""name"": ""Physical diagnostic game"", ""properties"": {} }],
          ""scenarios"": [{ ""id"": ""layers"", ""game_mode"": ""spatial-layer.resource-only"", ""mode_overrides"": {}, ""time"": ""test.minute"", ""space"": ""layers.tiles"", ""modules"": [""layers""], ""seats"": 2, ""default_seats"": 2 }]
        }";

        internal static GameFactory Factory(string json = Definitions, string knowledge = null, string visibility = null, Action<WorldComposer> compose = null)
        {
            var module = new ProbeModule("layers", new ModuleVersion(1, 0, 0), compose: compose);
            var modules = new List<GameModule> { module };
            if (knowledge != null) modules.Add(new CompanyKnowledgeModule());
            if (visibility != null) modules.Add(new KnowledgeVisibilityModule());
            var definition = new GameDefinition("layer-proof", modules);
            var files = new PreloadedFileSource()
                .Add("manifest.json", "{ \"schema\": 1, \"modules\": [{ \"id\": \"layers\", \"version\": \"1.0.0\", \"documents\": [\"layers.json\"] }] }")
                .Add("layers.json", json);
            if (knowledge != null)
            {
                JsonValue manifest = Content.Json.Parse(files.ReadText("manifest.json"));
                manifest.Get("modules").Add(Content.Json.Parse("{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\",\"documents\":[\"knowledge.json\"]}"));
                if (visibility != null)
                    manifest.Get("modules").Add(Content.Json.Parse("{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"tile-layer-knowledge.json\"]}"));
                files.Add("manifest.json", manifest.ToString()).Add("knowledge.json", knowledge);
                if (visibility != null) files.Add("tile-layer-knowledge.json", visibility);
            }
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "layers", Seed = 19 });

        public static void Register()
        {
            TestRunner.Add("Spatial layer nonempty validation: every byte position native codecs and bounded cost", NonemptyCodec);
            TestRunner.Add(ExactGoalAdmissionTests.Case, ExactGoalAdmissionTests.Run);
            TestRunner.Add("Spatial layers: canonical sparse storage and generation", CanonicalStorage);
            TestRunner.Add("Spatial layers: catalog links, authored metadata and explicit regional reads", Queries);
            TestRunner.Add("Spatial layers: private ownership, detached views and saved sparse values", Persistence);
            TestRunner.Add("Spatial layers: paged tile and non-tile reads without area caps", Paging);
            TestRunner.Add("Spatial visibility: exact observer identity and private cursor isolation", ExactObserver);
        }

        private static void ExactObserver(Check c)
        {
            var invalids = new[] { new PlayerId(-2), new PlayerId(-7), new PlayerId(int.MinValue) };
            foreach (PlayerId invalid in invalids)
                foreach (SpatialLayerVisibility visibility in new[] { SpatialLayerVisibility.Public, SpatialLayerVisibility.CompanyPrivate })
                    c.That(!SpatialLayerAccess.CanRead(visibility, new PlayerId(0), invalid), "invalid negative identity is neither observer nor public-layer viewer");
            c.That(SpatialLayerAccess.CanRead(SpatialLayerVisibility.CompanyPrivate, PlayerId.None, PlayerId.None) &&
                SpatialLayerAccess.CanRead(SpatialLayerVisibility.CompanyPrivate, new PlayerId(0), new PlayerId(0)) &&
                !SpatialLayerAccess.CanRead(SpatialLayerVisibility.CompanyPrivate, new PlayerId(0), new PlayerId(1)),
                "exact observer and actual ownership remain distinct from another company");
            GameFactory factory = Factory(FrameworkProofContent.Read("spatial-observer.json")); var game = New(factory);
            var map = (TileMap)game.World.Space; var mine = game.Seat(0).View.Feature<TileMapView>();
            var other = game.Seat(1).View.Feature<TileMapView>(); var observer = game.Observer.Feature<TileMapView>();
            // Explicit native layer fixture hooks, NOT a player discovery/write feature or host gameplay.
            ((ISpatialLayerProvider)map).AssignOwner(game.World, "observer.private", new PlayerId(0));
            var tile = new TileCoord(69999, 69999); var region = new TileRect(tile, 1, 1);
            map.Layer<IntLayer>("observer.public").Set(tile, 17);
            map.Layer<FixedLayer>("observer.private").Set(tile, Fix.FromInt(4));
            var values = new List<Fix>(); var integers = new List<int>();
            c.That(mine.Layers.Count == 2 && other.Layers.Count == 1 && observer.Layers.Count == 2,
                "native metadata retains actual owner/public/exact-observer permissions");
            c.Equal(SpatialLayerReadStatus.Success, observer.ReadLayer("observer.private", region, values), "exact observer reads actual private layer");
            c.Equal(Fix.FromInt(4), values[0], "authorized observer receives actual stored quantity");
            int foreignVersion = other.Version; ulong hash = game.Hash();
            foreach (PlayerId invalid in invalids)
            {
                foreach (ISpatialLayer layer in map.Layers)
                {
                    c.That(!layer.CanRead(invalid),
                        "actual native layer capability denies malformed negative viewers");
                }
                c.That(map.VersionFor(invalid) == 0 && map.ChunkVersionFor(invalid, 4374, 4374) == 0 &&
                    ((TileWorldSnapshot)map.Capture(invalid)).Version == 0, "invalid negative viewer receives no native map/chunk/snapshot change counter");
                foreach (int cursor in new[] { -1, 0, int.MaxValue })
                    c.That(map.ChangedChunkKeys(invalid, cursor, out bool full).Length == 0 && !full,
                        "invalid negative cursor query enumerates neither private chunks nor full invalidation");
                c.Throws<ArgumentOutOfRangeException>(() => game.World.ViewFor(invalid), "normal module view guard rejects invalid negative identity");
            }
            c.Throws<ArgumentOutOfRangeException>(() => game.World.ViewFor(new PlayerId(2)), "normal view guard still rejects nonseated positive identity");
            values.Add(Fix.One);
            c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer("observer.private", new TileRect(-1, -1, -1, -1), values),
                "private denial precedes native region validation");
            c.Equal(0, values.Count, "native refused read clears old output");
            SpatialLayerPageResult denied = other.ReadLayerPage("observer.private", region, new SpatialLayerPageRequest(-1, 0), values);
            c.That(denied.Status == SpatialLayerReadStatus.Unavailable && denied.Cursor == null && denied.TotalCount == 0 && denied.Count == 0,
                "native hidden page reveals no type/region/page/count/version details");
            c.Equal(hash, game.Hash(), "native authorization and cursor inspection are pure");
            map.Layer<FixedLayer>("observer.private").Set(tile, Fix.FromInt(5));
            c.That(other.Version == foreignVersion && map.VersionFor(new PlayerId(-2)) == 0 && observer.Version > foreignVersion,
                "real private native write changes only actual owner/exact-observer cursors");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "native layer authorization saves through existing sparse codec");
            game.Update(); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "native layer twins continue under one engine Update");
            twin.Seat(0).View.Feature<TileMapView>().ReadLayer("observer.private", region, values);
            c.Equal(Fix.FromInt(5), values[0], "saved owner retains actual native private quantity");

            JsonValue pointDefinitions = Json.Parse(PointGame.Definitions);
            pointDefinitions.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            pointDefinitions.Get("scenarios")[0].Set("default_seats", JsonValue.Number("2"));
            var pointFactory = PointGame.Factory(pointDefinitions.ToString(), FrameworkProofContent.Read("point-layers.json"));
            var points = pointFactory.NewGame(new NewGameRequest { Scenario = "points", Seed = 43 });
            var space = (PointSpace)points.World.Space;
            c.That(TestKit.Order(points, 0, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(0) }).Accepted,
                "point-layer fixture uses ordinary owner assignment command");
            c.That(TestKit.Order(points, 0, new WritePointLayerCommand { Layer = "point.deposit", Position = Fix.One, Value = Fix.FromInt(6) }).Accepted,
                "point-layer fixture uses ordinary physical sample write command");
            var pointRegion = new PointRegion(Fix.One, Fix.One, 1);
            hash = points.Hash();
            foreach (PlayerId invalid in invalids)
            {
                var view = new PointLayerView(space, invalid);
                c.That(view.Layers.Count == 0 && view.Version == 0, "public point provider exposes no metadata/cursor to invalid negative identity");
                foreach (string id in new[] { "point.deposit", "point.terrain", "missing" })
                {
                    values.Add(Fix.One);
                    c.Equal(SpatialLayerReadStatus.Unavailable, view.ReadLayer(id, pointRegion, values), "point invalid viewer and missing layer share refusal");
                    c.Equal(0, values.Count, "point refused region clears old values");
                    values.Add(Fix.One);
                    denied = view.ReadLayerPage(id, pointRegion, new SpatialLayerPageRequest(-1, 0), values);
                    c.That(denied.Status == SpatialLayerReadStatus.Unavailable && denied.Cursor == null && denied.TotalCount == 0 &&
                        denied.Count == 0 && values.Count == 0, "point denied page discloses no sample count/cursor/page details");
                }
                foreach (ISpatialLayer layer in space.SpatialLayers)
                    c.That(!layer.CanRead(invalid), "actual point layer capability reuses exact observer identity");
                c.Equal(Codes.NotFound, space.WriteLayer(points.World, invalid, "point.deposit", Fix.One, Fix.FromInt(99)),
                    "malformed observer cannot mutate actual private samples through point provider");
            }
            c.Equal(hash, points.Hash(), "point invalid reads/writes change no samples, claims, ownership or simulation state");
            c.Equal(SpatialLayerReadStatus.Success, points.Observer.Feature<PointLayerView>().ReadLayer("point.deposit", pointRegion, values),
                "exact observer keeps public provider access"); c.Equal(Fix.FromInt(6), values[0], "point observer reads actual private quantity");
            var pointTwin = pointFactory.Load(points.Save()); c.Equal(points.Hash(), pointTwin.Hash(), "point private sample saves using sole spatial codec");
            points.Update(); pointTwin.Update(); c.Equal(points.Hash(), pointTwin.Hash(), "point twins continue identically without a privacy clock/table");
        }

        private static void CanonicalStorage(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "clean storage revision has a current build identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "unchanged value encoding adds no second reader");
            JsonValue definitions = Json.Parse(Definitions);
            definitions.Get("tile_spaces")[0].Set("width", JsonValue.Number("18"));
            definitions.Get("tile_spaces")[0].Set("height", JsonValue.Number("17"));
            GameFactory factory = Factory(definitions.ToString());
            SimGame game = New(factory); TileMap map = (TileMap)game.World.Space;
            Storage(c, map.Layer<ByteLayer>("terrain"), (byte)255, (byte)7, 1, true);
            Storage(c, map.Layer<IntLayer>("strata"), int.MinValue, int.MaxValue, 4, false);
            Storage(c, map.Layer<FixedLayer>("elevation"), Fix.FromRatio(-3, 8), Fix.FromRaw(1), 8, false);
            ((ISpatialLayerProvider)map).AssignOwner(game.World, "deposit", new PlayerId(0));
            int ownerVersion = map.VersionFor(new PlayerId(0)), foreignVersion = map.VersionFor(new PlayerId(1));
            map.Layer<FixedLayer>("deposit").Fill((x, y) => x == 17 && y == 16 ? Fix.FromInt(4) : Fix.Zero);
            c.That(map.VersionFor(new PlayerId(0)) > ownerVersion && map.VersionFor(new PlayerId(1)) == foreignVersion,
                "successful private generation invalidates only its actual owner's view");
            ownerVersion = map.VersionFor(new PlayerId(0));
            c.Throws<InvalidOperationException>(() => map.Layer<FixedLayer>("deposit").Fill((x, y) => throw new InvalidOperationException("failed")),
                "private failed generation is not committed");
            c.That(map.VersionFor(new PlayerId(0)) == ownerVersion && map.VersionFor(new PlayerId(1)) == foreignVersion,
                "failed private generation invalidates neither company");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "all revised native layers restore with one codec each");
            game.Update(); restored.Update(); c.Equal(game.Hash(), restored.Hash(), "canonical layer twins continue through engine Update");

            definitions.Get("tile_spaces")[0].Set("width", JsonValue.Number(int.MaxValue.ToString()));
            definitions.Get("tile_spaces")[0].Set("height", JsonValue.Number(int.MaxValue.ToString()));
            GameFactory wideFactory = Factory(definitions.ToString());
            SimGame wide = New(wideFactory); TileMap wideMap = (TileMap)wide.World.Space;
            var edge = new TileCoord(int.MaxValue - 1, int.MaxValue - 1);
            wideMap.Layer<ByteLayer>("terrain").Set(edge, 255);
            wideMap.Layer<IntLayer>("strata").Set(edge, int.MinValue);
            wideMap.Layer<FixedLayer>("elevation").Set(edge, Fix.FromRaw(-1));
            SimGame wideTwin = wideFactory.Load(wide.Save());
            c.Equal(wide.Hash(), wideTwin.Hash(), "int-max squared dimensions use only allocated sparse chunks");
            var integers = new List<int>(); var values = new List<Fix>();
            TileMapView view = wideTwin.Seat(0).View.Feature<TileMapView>();
            long offset = (long)int.MaxValue * int.MaxValue - 1;
            c.Equal(SpatialLayerReadStatus.Success, view.ReadLayerPage("terrain", wideMap.Bounds,
                new SpatialLayerPageRequest(offset, 1), integers).Status, "shared storage supports bounded last-cell queries without an area cap");
            c.Equal(255, integers[0], "byte edge value survives wide sparse restore");
            view.ReadLayer("strata", new TileRect(edge, 1, 1), integers);
            c.Equal(int.MinValue, integers[0], "signed integer edge value survives");
            view.ReadLayer("elevation", new TileRect(edge, 1, 1), values);
            c.Equal(Fix.FromRaw(-1), values[0], "raw fractional fixed edge value survives");
            wide.Update(); wideTwin.Update(); c.Equal(wide.Hash(), wideTwin.Hash(), "wide sparse twins continue without a platform dimension limit");
            CounterFailures(c, factory);
            DenseCheckpoints(c);
        }

        private static void DenseCheckpoints(Check c)
        {
            JsonValue definitions = Json.Parse(Definitions);
            definitions.Get("tile_spaces")[0].Set("width", JsonValue.Number("96"));
            definitions.Get("tile_spaces")[0].Set("height", JsonValue.Number("96"));
            GameFactory factory = Factory(definitions.ToString()); SimGame game = New(factory);
            var map = (TileMap)game.World.Space;
            map.Layer<ByteLayer>("terrain").Fill((x, y) => (byte)7);
            map.Layer<IntLayer>("strata").Fill((x, y) => -17);
            map.Layer<FixedLayer>("elevation").Fill((x, y) => Fix.FromRatio(-3, 2));
            map.Layer<FixedLayer>("lake").Fill((x, y) => Fix.FromInt(-2));
            foreach (string name in new[] { "terrain", "strata", "elevation", "lake" })
            {
                MapLayer layer = map.Layers.Single(value => value.Id == name);
                byte[] encoded = Encode(layer); var initial = new StateHasher(); layer.Sync(initial);
                for (int i = 0; i < 16; i++) layer.Sync(new StateHasher());
                ulong last = 0; var timer = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 256; i++) { var hash = new StateHasher(); layer.Sync(hash); last = hash.Value; }
                timer.Stop();
                c.Equal(initial.Value, last, "dense complete checkpoint hashing is deterministic: " + name);
                c.Equal(Convert.ToBase64String(encoded), Convert.ToBase64String(Encode(layer)), "dense hashing preserves every encoded value: " + name);
                Console.WriteLine("dense-layer-checkpoint;build=" + EngineInfo.Build + ";layer=" + name + ";hash=" + last.ToString("X16") + ";bytes=" + encoded.Length + ";iterations=256;elapsed-ms=" + timer.Elapsed.TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
                var cell = new TileCoord(95, 95); Fix before = layer.GetSample(cell);
                layer.SetSample(cell, before + Fix.One); var changed = new StateHasher(); layer.Sync(changed);
                c.That(changed.Value != initial.Value, "full dense hash still observes a last-cell change: " + name);
                layer.SetSample(cell, before);
            }
            var restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "all dense layer types restore through the one native codec");
            game.Update(); restored.Update();
            c.Equal(game.Hash(), restored.Hash(), "dense restored worlds retain identical normal Update state");
        }

        // Framework-only counter fault injection, never a module/host gameplay path.
        private static void InjectCounter(TileMap map, string name, int value, PlayerId? owner = null)
        {
            object target = map;
            if (owner.HasValue)
            {
                var records = (System.Collections.IDictionary)typeof(TileMap)
                    .GetField("_privateChanges", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(map);
                target = records[owner.Value];
            }
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static string Cursors(TileMap map, MapLayer layer)
            => layer.Version + ":" + map.Version + ":" + map.VersionFor(new PlayerId(0))
                + ":" + map.VersionFor(new PlayerId(1)) + ":" + map.ChunkVersion(0, 0)
                + ":" + map.ChunkVersionFor(new PlayerId(0), 0, 0) + ":" + map.ChunkVersionFor(new PlayerId(1), 0, 0);

        private static void CounterFailures(Check c, GameFactory factory)
        {
            foreach (string fault in new[] { "layer", "_version", "_publicVersion", "owner" })
            {
                SimGame game = New(factory); TileMap map = (TileMap)game.World.Space;
                map.AssignOwner(game.World, "deposit", new PlayerId(0));
                var layer = map.Layer<ByteLayer>("terrain"); var tile = new TileCoord(0, 0);
                layer.Set(tile, 7);
                if (fault == "layer") layer.Version = int.MaxValue;
                else if (fault == "owner") InjectCounter(map, "Version", int.MaxValue, new PlayerId(0));
                else InjectCounter(map, fault, int.MaxValue);
                string original = Convert.ToBase64String(Encode(layer)), cursors = Cursors(map, layer);
                ulong hash = game.Hash();
                layer.Set(tile, 7);
                c.Equal(cursors, Cursors(map, layer), fault + " counter does not refuse a same-value no-op");
                c.Throws<OverflowException>(() => layer.Set(tile, 8), fault + " refuses overwriting before mutation");
                c.Throws<OverflowException>(() => layer.Set(new TileCoord(17, 16), 9), fault + " refuses a new sparse chunk before mutation");
                c.Throws<OverflowException>(() => layer.Fill((x, y) => (byte)1), fault + " refuses generated replacement before commit");
                c.Equal(original, Convert.ToBase64String(Encode(layer)), fault + " refusal preserves complete sparse encoding");
                c.Equal(cursors, Cursors(map, layer), fault + " refusal preserves all map/layer/chunk cursors");
                c.Equal(hash, game.Hash(), fault + " refusal preserves authoritative hash");
                c.Equal(hash, factory.Load(game.Save()).Hash(), fault + " refusal leaves a valid resumable save");
            }
            SimGame callbackGame = New(factory); TileMap callbackMap = (TileMap)callbackGame.World.Space;
            var values = callbackMap.Layer<FixedLayer>("elevation"); values.Set(new TileCoord(0, 0), Fix.One);
            int version = values.Version;
            string before = Convert.ToBase64String(Encode(values));
            InjectCounter(callbackMap, "_version", int.MaxValue - 1);
            c.Throws<OverflowException>(() => values.Fill((x, y) =>
            {
                if (x == 0 && y == 0) callbackMap.Layer<FixedLayer>("lake").Set(new TileCoord(0, 0), Fix.FromInt(3));
                return Fix.FromInt(2);
            }), "generation rechecks counters after callbacks and before value commit");
            c.Equal(before, Convert.ToBase64String(Encode(values)), "post-generation refusal preserves all original values");
            c.Equal(version, values.Version, "post-generation refusal preserves layer cursor");
            c.Equal(int.MaxValue, callbackMap.Version, "callback side effects are not fabricated rollback");
            c.Equal(Fix.FromInt(3), callbackMap.Layer<FixedLayer>("lake")[0, 0], "actual other-layer callback write stays committed independently");

            SimGame privateGame = New(factory); TileMap privateMap = (TileMap)privateGame.World.Space;
            privateMap.AssignOwner(privateGame.World, "deposit", new PlayerId(0));
            InjectCounter(privateMap, "_publicVersion", int.MaxValue);
            int foreign = privateMap.VersionFor(new PlayerId(1));
            privateMap.Layer<FixedLayer>("deposit").Set(new TileCoord(0, 0), Fix.One);
            c.Equal(Fix.One, privateMap.Layer<FixedLayer>("deposit")[0, 0], "unaffected public counter cannot refuse a private write");
            c.Equal(foreign, privateMap.VersionFor(new PlayerId(1)), "private write preserves unrelated exhausted public cursor");
            InjectCounter(privateMap, "Version", int.MaxValue, new PlayerId(0));
            var privateLayer = privateMap.Layer<FixedLayer>("deposit");
            string privateBefore = Convert.ToBase64String(Encode(privateLayer)), privateCursors = Cursors(privateMap, privateLayer);
            c.Throws<OverflowException>(() => privateLayer.Set(new TileCoord(0, 0), Fix.FromInt(2)), "actual private-owner exhaustion refuses before stock-like layer values change");
            c.Throws<OverflowException>(() => privateLayer.Fill((x, y) => Fix.Zero), "private-owner exhaustion refuses before generated erasure");
            c.Equal(privateBefore, Convert.ToBase64String(Encode(privateLayer)), "private-owner refusals preserve exact values");
            c.Equal(privateCursors, Cursors(privateMap, privateLayer), "private-owner refusals preserve every cursor");
        }

        private static byte[] Encode(MapLayer layer)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); layer.Sync(writer); writer.Flush(); return stream.ToArray();
            }
        }

        private static void Storage<T>(Check c, MapLayer<T> layer, T initial, T generated, int valueBytes, bool byteRun) where T : struct
        {
            var first = new TileCoord(0, 0); var edge = new TileCoord(17, 16);
            layer.Set(first, initial); layer.Set(edge, generated);
            byte[] original = Encode(layer); int version = layer.Version; int visible = layer.Map.VersionFor(new PlayerId(0));
            c.That(layer is MapLayer<T>, typeof(T).Name + " uses the sole typed sparse owner");
            c.Throws<ArgumentOutOfRangeException>(() => { T ignored = layer[new TileCoord(-1, 0)]; }, "negative coordinates never manufacture zero");
            c.Throws<ArgumentOutOfRangeException>(() => { T ignored = layer[new TileCoord(18, 16)]; }, "right edge refuses explicitly");
            c.Throws<ArgumentOutOfRangeException>(() => { T ignored = layer[new TileCoord(17, 17)]; }, "bottom edge refuses explicitly");
            c.Throws<ArgumentOutOfRangeException>(() => layer.Set(new TileCoord(18, 0), generated), "invalid writes refuse before mutation");
            c.Equal(default(T), layer[new TileCoord(1, 1)], "only valid unallocated cells read zero");
            layer.Set(edge, generated); c.Equal(version, layer.Version, "same-value writes do not invalidate");
            c.Throws<InvalidOperationException>(() => layer.Fill((x, y) => x == 1 && y == 1
                ? throw new InvalidOperationException("generation failed") : initial), "failed generation does not erase the current layer");
            c.Equal(Convert.ToBase64String(original), Convert.ToBase64String(Encode(layer)), "failed generation preserves every sparse value");
            c.Equal(version, layer.Version, "failed generation preserves layer version");
            c.Equal(visible, layer.Map.VersionFor(new PlayerId(0)), "failed generation preserves viewer invalidation");
            c.Throws<InvalidOperationException>(() => layer.Fill((x, y) => { layer.Set(first, generated); return initial; }), "same-layer mutation inside generation refuses");
            c.Throws<InvalidOperationException>(() => layer.Fill((x, y) => { layer.Fill((a, b) => generated); return initial; }), "recursive generation refuses");
            c.Throws<InvalidOperationException>(() => layer.Fill((x, y) => { layer.Sync(new StateHasher()); return initial; }), "partial generation cannot be checkpointed or hashed");
            c.Equal(Convert.ToBase64String(original), Convert.ToBase64String(Encode(layer)), "all generation refusals leave original storage intact");

            int headerBytes = 8 + (byteRun ? 4 : 0);
            int chunkBytes = headerBytes + 256 * valueBytes;
            int second = 4 + chunkBytes;
            foreach (string fault in new[] { "padding-x", "padding-y", "empty", "duplicate", "unsorted", "outside", "truncated" })
            {
                byte[] bad = (byte[])original.Clone();
                if (fault == "padding-x" || fault == "padding-y")
                {
                    int target = fault == "padding-x" ? 2 : 16;
                    // Copy an actual nonzero representation into the unused edge-chunk padding.
                    Array.Copy(bad, second + headerBytes + valueBytes, bad, second + headerBytes + target * valueBytes, valueBytes);
                }
                else if (fault == "empty") Array.Clear(bad, second + headerBytes, 256 * valueBytes);
                else if (fault == "duplicate") Array.Copy(bad, 4, bad, second, 8);
                else if (fault == "unsorted")
                { Array.Copy(original, second, bad, 4, chunkBytes); Array.Copy(original, 4, bad, second, chunkBytes); }
                else if (fault == "outside") Array.Copy(BitConverter.GetBytes(2), 0, bad, second, 4);
                else Array.Resize(ref bad, bad.Length - 1);
                c.Throws<CorruptStateException>(() => layer.Sync(new StateReader(bad, 0, bad.Length)), "sole codec refuses " + fault + " for " + typeof(T).Name);
                c.Equal(Convert.ToBase64String(original), Convert.ToBase64String(Encode(layer)), "failed complete-layer restore retains original values");
            }
            // Framework-only fault injection; extension/game code never receives this backing store.
            var chunks = (Dictionary<long, T[]>)typeof(MapLayer<T>).GetField("_chunks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(layer);
            long edgeKey = ((long)1 << 32) | 1;
            chunks[edgeKey][2] = initial;
            c.Throws<CorruptStateException>(() => Encode(layer), "write validates live out-of-bounds padding rather than serializing it");
            c.Throws<CorruptStateException>(() => layer.Sync(new StateHasher()), "hash validates exactly the same live sparse invariants");
            chunks[edgeKey][2] = default;
            T[] retainedChunk = chunks[edgeKey]; chunks[edgeKey] = new T[256];
            c.Throws<CorruptStateException>(() => Encode(layer), "write rejects an injected empty sparse chunk");
            c.Throws<CorruptStateException>(() => layer.Sync(new StateHasher()), "hash rejects the same injected empty chunk");
            chunks[edgeKey] = retainedChunk;
            layer.Sync(new StateReader(original, 0, original.Length));
            c.Equal(initial, layer[first], "valid checkpoint retains first chunk");
            c.Equal(generated, layer[edge], "valid partial edge chunk retains exact value");
            layer.Set(first, default); layer.Set(edge, default);
            c.Equal(8, Encode(layer).Length, "zero writes retain only native sparse and resource-binding section counts");
            c.Equal(default(T), layer[edge], "removed sparse cell remains a valid in-bounds zero");
            int beforeFill = layer.Version;
            layer.Fill((x, y) => x == 17 && y == 16 ? generated : default);
            c.Equal(beforeFill + 1, layer.Version, "successful generation commits one layer invalidation");
            c.Equal(generated, layer[edge], "generated edge data uses the same sparse indexing");
            c.Equal(default(T), layer[first], "generation replaces rather than merges prior values");
            var tiles = new List<TileCoord>(); layer.CopyNonZeroTiles(tiles);
            c.That(tiles.Count == 1 && tiles[0] == edge, "network discovery enumerates only canonical nonzero cells");
            layer.Fill((x, y) => default); c.Equal(8, Encode(layer).Length, "all-zero generation retains only native section counts, not serialized chunks");
        }

        private static void Paging(Check c)
        {
            var pagingScope = new SpatialChangeScope();
            SpatialLayerPageResult envelope = SpatialLayerPaging.Check(long.MaxValue, 7, pagingScope, "probe", new SpatialLayerPageRequest(long.MaxValue - 2, int.MaxValue, pagingScope.Capture(7, "probe")));
            c.That(envelope.Status == SpatialLayerReadStatus.Success && envelope.Count == 2 && envelope.NextOffset == long.MaxValue,
                "generic envelope handles the full signed long range without overflowing offset plus capacity");
            c.Throws<ArgumentOutOfRangeException>(() => SpatialLayerPaging.Check(-1, 0, pagingScope, "probe", new SpatialLayerPageRequest(0, 1)), "invalid provider total is not fabricated empty data");
            c.Throws<ArgumentOutOfRangeException>(() => SpatialLayerPaging.Check(0, -1, pagingScope, "probe", new SpatialLayerPageRequest(0, 1)), "invalid provider version is not accepted");
            c.Throws<ArgumentOutOfRangeException>(() => SpatialLayerPageResult.Refused(SpatialLayerReadStatus.Success), "a refusal cannot claim successful output");
            c.Throws<ArgumentOutOfRangeException>(() => SpatialLayerPageResult.Refused((SpatialLayerReadStatus)255), "undefined refusal status is rejected");
            JsonValue json = Json.Parse(Definitions);
            json.Get("tile_spaces")[0].Set("width", JsonValue.Number(int.MaxValue.ToString()));
            json.Get("tile_spaces")[0].Set("height", JsonValue.Number(int.MaxValue.ToString()));
            GameFactory factory = Factory(json.ToString()); SimGame game = New(factory);
            TileMap map = (TileMap)game.World.Space;
            TileMapView view = game.Seat(0).View.Feature<TileMapView>();
            ISpatialLayerPagedQuery<TileRect, Fix> fixedQuery = view;
            ISpatialLayerPagedQuery<TileRect, int> integerQuery = view;
            var values = new List<Fix>(); var integers = new List<int>();
            TileRect region = map.Bounds;
            long total = (long)int.MaxValue * int.MaxValue;
            var last = new TileCoord(int.MaxValue - 1, int.MaxValue - 1);
            Fix final = Fix.FromRatio(-7, 4);
            map.Layer<FixedLayer>("elevation").Set(last, final);
            map.Layer<ByteLayer>("terrain").Set(last, 255);
            map.Layer<IntLayer>("strata").Set(last, -123456);
            map.Layer<FixedLayer>("lake").Set(new TileCoord(int.MaxValue - 1, 0), Fix.One);
            map.Layer<FixedLayer>("lake").Set(new TileCoord(0, 1), Fix.FromInt(2));
            map.Layer<FixedLayer>("lake").Set(new TileCoord(1, 1), Fix.FromInt(3));
            ulong hash = game.Hash();
            c.Equal(SpatialLayerReadStatus.BufferCapacityExceeded, view.ReadLayer("elevation", region, values), "whole result explicitly exceeds one IList, not world admission");
            SpatialLayerPageResult page = fixedQuery.ReadLayerPage("elevation", region, new SpatialLayerPageRequest(total - 2, 3), values);
            c.Equal(SpatialLayerReadStatus.Success, page.Status, "a bounded page reads the int-max squared world without area allocation");
            c.That(page.TotalCount == total && page.Offset == total - 2 && page.NextOffset == total
                && page.Count == 2 && !page.HasMore && page.Cursor.Version == view.Version, "long final offsets and actual delivered count are exact");
            c.That(values.Count == 2 && values[0] == Fix.Zero && values[1] == final, "final sparse fixed value retains exact sign and fraction");
            page = integerQuery.ReadLayerPage("terrain", region, new SpatialLayerPageRequest(total - 1, 1), integers);
            c.That(page.Status == SpatialLayerReadStatus.Success && integers.Count == 1 && integers[0] == 255, "byte page retains exact classification at the final coordinate");
            page = integerQuery.ReadLayerPage("strata", region, new SpatialLayerPageRequest(total - 1, 1), integers);
            c.That(page.Status == SpatialLayerReadStatus.Success && integers[0] == -123456, "integer layer shares the same bounded geometry and retains its type");
            page = fixedQuery.ReadLayerPage("lake", region, new SpatialLayerPageRequest(int.MaxValue - 1L, 3), values);
            c.That(page.Status == SpatialLayerReadStatus.Success && page.HasMore && page.NextOffset == int.MaxValue + 2L,
                "page crosses a row using wide linear arithmetic");
            c.That(values.Count == 3 && values[0] == Fix.One && values[1] == Fix.FromInt(2) && values[2] == Fix.FromInt(3), "provider row-major order crosses exactly once without duplication");

            var small = new TileRect(int.MaxValue - 2, int.MaxValue - 2, 2, 2);
            var collected = new List<Fix>(); long offset = 0; SpatialRegionCursor version = null;
            do
            {
                page = fixedQuery.ReadLayerPage("elevation", small, new SpatialLayerPageRequest(offset, 3, version), values);
                c.Equal(SpatialLayerReadStatus.Success, page.Status, "continued page preserves one visible version");
                collected.AddRange(values); offset = page.NextOffset; version = page.Cursor;
            } while (page.HasMore);
            view.ReadLayer("elevation", small, values);
            c.That(collected.Count == values.Count && collected[3] == final && collected[0] == values[0], "bounded traversal equals the sole whole-region values");
            page = fixedQuery.ReadLayerPage("elevation", region, new SpatialLayerPageRequest(total, 1), values);
            c.That(page.Status == SpatialLayerReadStatus.Success && page.Count == 0 && page.NextOffset == total && !page.HasMore && values.Count == 0, "offset equal to total is an empty terminal page");
            page = fixedQuery.ReadLayerPage("elevation", new TileRect(0, 0, 0, int.MaxValue), new SpatialLayerPageRequest(0, 1), values);
            c.That(page.Status == SpatialLayerReadStatus.Success && page.TotalCount == 0 && values.Count == 0, "zero-width region never divides by zero");
            page = fixedQuery.ReadLayerPage("elevation", new TileRect(0, 0, int.MaxValue, 0), new SpatialLayerPageRequest(0, int.MaxValue), values);
            c.That(page.Status == SpatialLayerReadStatus.Success && page.Count == 0 && page.TotalCount == 0, "zero-height region allocates no requested-capacity buffer");
            foreach (SpatialLayerPageRequest request in new[] {
                new SpatialLayerPageRequest(-1, 1), new SpatialLayerPageRequest(total + 1, 1),
                new SpatialLayerPageRequest(0, 0), new SpatialLayerPageRequest(0, -1),
                default(SpatialLayerPageRequest) })
            {
                values.Add(Fix.One);
                page = fixedQuery.ReadLayerPage("elevation", region, request, values);
                Refusal(c, page, values.Count, SpatialLayerReadStatus.InvalidPage, "invalid page envelope");
            }
            foreach (TileRect invalid in new[] { new TileRect(-1, 0, 1, 1), new TileRect(0, 0, -1, 1),
                new TileRect(int.MaxValue, 0, 1, 1), new TileRect(0, int.MaxValue, 1, 1) })
            {
                values.Add(Fix.One);
                page = fixedQuery.ReadLayerPage("elevation", invalid, new SpatialLayerPageRequest(0, 1), values);
                Refusal(c, page, values.Count, SpatialLayerReadStatus.InvalidRegion, "invalid region uses wide bounds");
            }
            values.Add(Fix.One);
            Refusal(c, fixedQuery.ReadLayerPage("terrain", region, new SpatialLayerPageRequest(0, 1), values), values.Count,
                SpatialLayerReadStatus.ValueTypeMismatch, "wrong value representation");
            foreach (string unavailable in new[] { "deposit", "missing" })
            {
                values.Add(Fix.One);
                Refusal(c, fixedQuery.ReadLayerPage(unavailable, new TileRect(-1, 0, 1, 1), default, values), values.Count,
                    SpatialLayerReadStatus.Unavailable, "hidden and missing layer share refusal before geometry/page validation");
            }
            c.Throws<ArgumentNullException>(() => fixedQuery.ReadLayerPage("elevation", region, new SpatialLayerPageRequest(0, 1), null), "null output remains a programmer error");
            c.Equal(hash, game.Hash(), "all successful and refused tile reads are pure and create no page state");

            page = fixedQuery.ReadLayerPage("elevation", region, new SpatialLayerPageRequest(0, 1), values);
            var continuation = new SpatialLayerPageRequest(1, 1, page.Cursor);
            map.Layer<FixedLayer>("elevation").Set(last, final);
            c.Equal(SpatialLayerReadStatus.Success, fixedQuery.ReadLayerPage("elevation", region, continuation, values).Status, "same-value write does not invalidate a continuation");
            map.Layer<FixedLayer>("elevation").Set(new TileCoord(0, 0), Fix.One);
            Refusal(c, fixedQuery.ReadLayerPage("elevation", region, continuation, values), values.Count,
                SpatialLayerReadStatus.Invalidated, "visible mutation explicitly invalidates, never silently mixes pages");
            var other = game.Seat(1).View.Feature<TileMapView>();
            ((ISpatialLayerProvider)map).AssignOwner(game.World, "deposit", new PlayerId(1));
            page = fixedQuery.ReadLayerPage("elevation", region, new SpatialLayerPageRequest(0, 1), values);
            SpatialLayerPageResult privatePage = other.ReadLayerPage("deposit", region, new SpatialLayerPageRequest(0, 1), values);
            map.Layer<FixedLayer>("deposit").Set(last, Fix.FromInt(8));
            c.Equal(SpatialLayerReadStatus.Success, fixedQuery.ReadLayerPage("elevation", region, new SpatialLayerPageRequest(1, 1, page.Cursor), values).Status,
                "another company's private write cannot invalidate my public cursor");
            Refusal(c, other.ReadLayerPage("deposit", region, new SpatialLayerPageRequest(1, 1, privatePage.Cursor), values), values.Count,
                SpatialLayerReadStatus.Invalidated, "private owner sees its real sample invalidation");
            privatePage = other.ReadLayerPage("deposit", region, new SpatialLayerPageRequest(0, 1), values);
            ((ISpatialLayerProvider)map).AssignOwner(game.World, "deposit", new PlayerId(0));
            Refusal(c, other.ReadLayerPage("deposit", region, new SpatialLayerPageRequest(1, 1, privatePage.Cursor), values), values.Count,
                SpatialLayerReadStatus.Unavailable, "revoked ownership hides values and version before cursor checks");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "paged reads add no save codec or stock authority");
            TileMapView loaded = restored.Seat(0).View.Feature<TileMapView>();
            page = loaded.ReadLayerPage("deposit", region, new SpatialLayerPageRequest(total - 1, 1), values);
            c.That(page.Status == SpatialLayerReadStatus.Success && values[0] == Fix.FromInt(8), "fresh cursor after restore reads saved private stock interpretation");
            c.That(loaded.Layers[1].Resource.Resource == game.World.Content.ResourceId("resource.ore")
                && loaded.Layers[1].VerticalOffset == Fix.FromInt(-1200), "resource and vertical metadata stay with the sole authored definition");
            game.Update(); restored.Update(); c.Equal(game.Hash(), restored.Hash(), "saved layer-only twins continue identically");
            SimGame independent = New(factory);
            page = independent.Seat(0).View.Feature<TileMapView>().ReadLayerPage("elevation", region, new SpatialLayerPageRequest(total - 1, 1), values);
            c.That(page.Status == SpatialLayerReadStatus.Success && values[0] == Fix.Zero, "shared immutable JSON does not share samples or live page state");
            PointPaging(c);
        }

        private static void Refusal(Check c, SpatialLayerPageResult page, int count, SpatialLayerReadStatus expected, string label)
        {
            c.Equal(expected, page.Status, label);
            c.That(count == 0 && page.Count == 0 && page.Offset == 0 && page.NextOffset == 0
                && page.TotalCount == 0 && page.Cursor == null && !page.HasMore, label + " clears output and publishes no region/version");
        }

        private static void PointPaging(Check c)
        {
            JsonValue json = Json.Parse(PointGame.Definitions);
            json.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            json.Get("scenarios")[0].Set("default_seats", JsonValue.Number("2"));
            GameFactory factory = PointGame.Factory(json.ToString(), FrameworkProofContent.Read("point-layers.json"));
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 19 });
            PointLayerView view = game.Seat(0).View.Feature<PointLayerView>();
            ISpatialLayerPagedQuery<PointRegion, Fix> query = view;
            var values = new List<Fix>(); var region = new PointRegion(Fix.Zero, Fix.FromRaw(1), Fix.FromInt(100).Raw + 1);
            c.That(TestKit.Order(game, 0, new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.FromInt(100), Value = Fix.FromRatio(5, 4) }).Accepted, "ordinary engine command writes a non-tile sample");
            ulong hash = game.Hash();
            c.Equal(SpatialLayerReadStatus.BufferCapacityExceeded, view.ReadLayer("point.terrain", region, values), "whole non-tile query also refuses only its single-buffer representation");
            SpatialLayerPageResult page = query.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(region.Count - 2, 4), values);
            c.That(page.Status == SpatialLayerReadStatus.Success && page.TotalCount == region.Count && page.Count == 2
                && page.NextOffset == region.Count && !page.HasMore, "non-tile provider pages a long sample count without raster conversion");
            c.That(values.Count == 2 && values[0] == Fix.Zero && values[1] == Fix.FromRatio(5, 4), "raw fractional geometry remains exact at its final long offset");
            page = query.ReadLayerPage("point.terrain", new PointRegion(Fix.FromInt(100), Fix.One, 0), new SpatialLayerPageRequest(0, 1), values);
            c.That(page.Status == SpatialLayerReadStatus.Success && values.Count == 0 && page.TotalCount == 0, "empty non-tile region at the provider boundary has no fabricated sample");
            Refusal(c, query.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(region.Count + 1, 1), values), values.Count,
                SpatialLayerReadStatus.InvalidPage, "non-tile envelope shares the engine's sole generic validator");
            foreach (PointRegion invalid in new[] { new PointRegion(Fix.Zero, Fix.Zero, 1), new PointRegion(Fix.Zero, Fix.One, -1),
                new PointRegion(Fix.Zero, Fix.MaxValue, long.MaxValue), new PointRegion(Fix.FromInt(100), Fix.One, 2) })
                Refusal(c, query.ReadLayerPage("point.terrain", invalid, new SpatialLayerPageRequest(0, 1), values), values.Count,
                    SpatialLayerReadStatus.InvalidRegion, "non-tile provider refuses invalid/overflowing geometry");
            Refusal(c, query.ReadLayerPage("point.deposit", region, default, values), values.Count,
                SpatialLayerReadStatus.Unavailable, "non-tile hidden metadata has the same private refusal");
            c.Equal(hash, game.Hash(), "non-tile reads never modify the provider's single state codec");
            page = query.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1), values);
            c.That(TestKit.Order(game, 0, new WritePointLayerCommand { Layer = "point.water", Position = Fix.FromRatio(1, 2), Value = Fix.One }).Accepted,
                "another visible channel changes through its engine provider");
            Refusal(c, query.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(1, 1, page.Cursor), values), values.Count,
                SpatialLayerReadStatus.Invalidated, "non-tile viewer version conservatively invalidates visible changes");
            c.That(TestKit.Order(game, 0, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(1) }).Accepted, "ordinary policy command assigns the private non-tile layer");
            page = query.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1), values);
            c.That(TestKit.Order(game, 1, new WritePointLayerCommand { Layer = "point.deposit", Position = Fix.One, Value = Fix.FromInt(3) }).Accepted,
                "private owner mutates its sample through engine command admission");
            c.Equal(SpatialLayerReadStatus.Success, query.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(1, 1, page.Cursor), values).Status,
                "foreign-private non-tile change cannot leak through cursor invalidation");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "non-tile page state adds no persistence owner");
            page = restored.Seat(0).View.Feature<PointLayerView>().ReadLayerPage("point.terrain", region,
                new SpatialLayerPageRequest(region.Count - 1, 1), values);
            c.That(page.Status == SpatialLayerReadStatus.Success && values[0] == Fix.FromRatio(5, 4), "fresh non-tile cursor sees exact saved sparse sample");
            game.Update(); restored.Update(); c.Equal(game.Hash(), restored.Hash(), "saved non-tile twins remain deterministic after paging");
        }

        private static void Queries(Check c)
        {
            SimGame game = New(Factory());
            TileMap map = (TileMap)game.World.Space;
            TileMapView view = game.Seat(0).View.Feature<TileMapView>();
            ISpatialLayerQuery<TileRect, Fix> fixedQuery = view;
            ISpatialLayerQuery<TileRect, int> integerQuery = view;
            var exact = new List<Fix> { Fix.One };
            var integers = new List<int> { 1 };
            var area = new TileRect(65536, 65536, 2, 2);
            map.Layer<FixedLayer>("elevation").Set(area.Origin, Fix.FromRatio(7, 2));
            map.Layer<FixedLayer>("lake").Set(area.Origin, Fix.FromRatio(23, 4));
            map.Layer<ByteLayer>("terrain").Set(area.Origin, 3);
            map.Layer<IntLayer>("strata").Set(area.Origin, 120000);
            c.Equal(SpatialLayerReadStatus.Success, fixedQuery.ReadLayer("elevation", area, exact), "generic contract supports exact regional reading at large coordinates");
            c.That(exact.Count == 4 && exact[0] == Fix.FromRatio(7, 2) && exact[1] == Fix.Zero, "sparse in-bounds defaults do not require whole-map allocation");
            c.Equal(SpatialLayerReadStatus.Success, fixedQuery.ReadLayer("lake", area, exact), "independent water level channel uses the same query contract");
            c.Equal(Fix.FromRatio(23, 4), exact[0], "lake level does not overwrite elevation");
            c.Equal(SpatialLayerReadStatus.Success, integerQuery.ReadLayer("terrain", area, integers), "byte channel projects integers");
            c.Equal(3, integers[0], "terrain channel remains independent");
            c.Equal(SpatialLayerReadStatus.Success, integerQuery.ReadLayer("strata", area, integers), "integer channel retains its range");
            c.Equal(120000, integers[0], "stratum value is not narrowed to a byte");
            c.Equal(SpatialLayerReadStatus.Unavailable, fixedQuery.ReadLayer("deposit", area, exact), "unassigned private layer is unavailable");
            c.Equal(0, exact.Count, "unavailable data clears old output without inventing values");
            c.Equal(SpatialLayerReadStatus.Unavailable, fixedQuery.ReadLayer("missing", area, exact), "unknown and hidden identities cannot be distinguished");
            c.Equal(SpatialLayerReadStatus.ValueTypeMismatch, fixedQuery.ReadLayer("terrain", area, exact), "wrong requested value shape is explicit");
            c.Equal(SpatialLayerReadStatus.InvalidRegion, fixedQuery.ReadLayer("elevation", new TileRect(int.MaxValue, 0, 2, 2), exact), "overflowing endpoint refuses before integer rectangle arithmetic");
            c.Equal(SpatialLayerReadStatus.InvalidRegion, fixedQuery.ReadLayer("elevation", new TileRect(0, 0, -1, 2), exact), "negative extent is invalid");
            c.Equal(SpatialLayerReadStatus.InvalidRegion, fixedQuery.ReadLayer("elevation", new TileRect(-1, 0, 2, 2), exact), "outside cells are not substituted with zero");
            c.Equal(SpatialLayerReadStatus.BufferCapacityExceeded, fixedQuery.ReadLayer("elevation", map.Bounds, exact), "one output buffer has an explicit representation bound, not a world-size cap");
            c.Equal(SpatialLayerReadStatus.Success, fixedQuery.ReadLayer("elevation", new TileRect(0, 0, 0, 70000), exact), "empty region is a bounded successful query");
            c.Equal(0, exact.Count, "empty region produces no values");
            c.Equal(4, view.Layers.Count, "private metadata is withheld before assignment");
            SpatialLayerSnapshot elevation = view.Layers[0];
            c.That(!typeof(ISpatialLayerProvider).IsAssignableFrom(elevation.GetType()), "metadata cannot be cast back to a storage provider");
            c.Throws<InvalidOperationException>(() => elevation.Metadata.Get("model").Set("id", JsonValue.Of("mutated")), "nested metadata is immutable");
            IResourceCatalog catalog = game.World.Content;
            ISpatialLayerDefinition deposit = game.World.Content.Get<ISpatialLayerDefinition>("deposit");
            c.That(ReferenceEquals(deposit.Unit, game.World.Content.Get<UnitDef>("unit.tonne")), "layer units alias the sole authored unit registry");
            c.That(ReferenceEquals(deposit.Resource, catalog.Get(game.World.Content.ResourceId("resource.ore"))), "layer resource aliases the sole resource catalog");
            c.Equal(SpatialResourceMeasure.Quantity, deposit.ResourceMeasure, "deposit explicitly declares quantity interpretation");
            c.Equal(Fix.FromInt(2500), UnitConversion.Convert(Fix.FromRatio(5, 2), deposit.Unit, deposit.Resource.QuantityUnit).Amount, "existing exact conversion interprets tonnes in native kilograms without modifying layer stock");
            ISpatialLayerDefinition lake = game.World.Content.Get<ISpatialLayerDefinition>("lake");
            c.That(lake.ResourceMeasure == SpatialResourceMeasure.Associated && lake.Unit.Dimension == "length" && lake.Resource.QuantityUnit.Dimension == "volume", "water-associated height is not mistaken for a quantity of water");
            c.That(deposit.VerticalUnit.Dimension == "length" && deposit.Unit.Dimension == "mass", "vertical offset has its own explicitly authored unit");
            c.That(elevation.Resource == null && elevation.ResourceMeasure == SpatialResourceMeasure.None, "nonresource layers do not invent resource identities");
            c.Throws<ContentException>(() => ((Def)deposit).Read(new JsonReader(Json.Parse("{}"), "repeat")), "shared layer definitions cannot be reauthored after resolution");
            c.Throws<ContentException>(() => ((Def)deposit).Resolve(new ContentResolver(game.World.Content, (Def)deposit)), "shared layer links resolve exactly once");
            InvalidContent(c);
        }

        private static void Persistence(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            TileMap map = (TileMap)game.World.Space;
            ISpatialLayerProvider provider = map;
            var owner = new PlayerId(0); var nextOwner = new PlayerId(1);
            TileMapView mine = game.Seat(0).View.Feature<TileMapView>(), other = game.Seat(1).View.Feature<TileMapView>();
            var tile = new TileCoord(65539, 65538);
            int cursor = other.Version;
            map.Layer<FixedLayer>("deposit").Set(tile, Fix.FromRatio(125, 2));
            c.Equal(cursor, other.Version, "unassigned private values change no company cursor");
            provider.AssignOwner(game.World, "deposit", owner);
            SpatialLayerSnapshot retained = mine.Layers[1];
            c.That(retained.Id == "deposit" && retained.Owner == owner && retained.Resource.Id == "resource.ore"
                && retained.VerticalOffset == Fix.FromInt(-1200), "owner sees resource identity and underground reference metadata");
            var authoredResource = game.World.Content.Get<ResourceDef>("resource.ore");
            c.That(retained.Resource.Resource == authoredResource.Resource && retained.Unit.Unit == new UnitId("unit.tonne")
                && retained.VerticalUnit.Unit == new UnitId("unit.metre") && retained.ResourceMeasure == SpatialResourceMeasure.Quantity,
                "detached metadata retains validated catalog identities and authored measures");
            c.That(!(retained.Resource is IContentDefinition) && !(retained.Unit is IContentDefinition)
                && !(retained.VerticalUnit is IContentDefinition), "detached metadata does not expose live content definitions");
            c.That(retained.Resource.QuantityUnit.Unit == authoredResource.QuantityUnit.Unit
                && retained.Resource.Category == authoredResource.Category && retained.Resource.Phase == authoredResource.Phase
                && retained.Resource.Traits.SequenceEqual(authoredResource.Traits), "detached resource copies actual unit/category/phase/traits");
            c.Equal(authoredResource.CapacityMeasures.Count, retained.Resource.CapacityMeasures.Count, "all authored capacity measures survive detachment");
            for (int i = 0; i < authoredResource.CapacityMeasures.Count; i++)
            {
                var actual = retained.Resource.CapacityMeasures[i]; var expected = authoredResource.CapacityMeasures[i];
                c.That(actual.Unit.Unit == expected.Unit.Unit && actual.PerQuantity == expected.PerQuantity
                    && !(actual.Unit is IContentDefinition), "capacity measure retains exact facts without live unit capability");
            }
            c.Throws<NotSupportedException>(() => ((IList<string>)retained.Resource.Traits).Clear(), "retained resource traits are immutable");
            c.Throws<NotSupportedException>(() => ((IList<ResourceCapacityMeasure>)retained.Resource.CapacityMeasures).Clear(), "retained capacity measures are immutable");
            c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer("deposit", new TileRect(tile, 1, 1), new List<Fix>()), "another company cannot read assigned values");
            c.Throws<ArgumentException>(() => provider.AssignOwner(game.World, "deposit", new PlayerId(2)), "owner must be a real session seat");
            c.Throws<InvalidOperationException>(() => provider.AssignOwner(game.World, "lake", owner), "public layer cannot acquire private ownership");
            c.Throws<InvalidOperationException>(() => provider.RegisterLayer(game.World.Content.Get<ISpatialLayerDefinition>("lake")), "composition registration is closed before ticking");
            map.Layer<FixedLayer>("elevation").Set(new TileCoord(1, 1), Fix.FromRatio(-3, 4));
            map.Layer<FixedLayer>("lake").Set(tile, Fix.FromRatio(9, 8));
            map.Layer<ByteLayer>("terrain").Set(tile, 8);
            map.Layer<IntLayer>("strata").Set(tile, -90000);
            map.Layer<FixedLayer>("elevation").Set(new TileCoord(31, 1), Fix.One);
            map.Layer<FixedLayer>("elevation").Set(new TileCoord(31, 1), Fix.Zero);
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "owners and all sparse value shapes have a single saved/hash codec");
            var values = new List<Fix>(); TileMapView restoredView = restored.Seat(0).View.Feature<TileMapView>();
            c.Equal(SpatialLayerReadStatus.Success, restoredView.ReadLayer("deposit", new TileRect(tile, 1, 1), values), "saved private ownership restores readability");
            c.Equal(Fix.FromRatio(125, 2), values[0], "sparse chunk coordinates and exact quantity restore");
            c.That(restoredView.Layers[1].Resource.Resource == restored.World.Content.ResourceId("resource.ore"), "saved layer resource remains the session catalog identity");
            c.Equal(SpatialLayerReadStatus.Success, restoredView.ReadLayer("elevation", new TileRect(1, 1, 1, 1), values), "terrain channel restores independently");
            c.Equal(Fix.FromRatio(-3, 4), values[0], "signed elevation is preserved");
            c.Equal(SpatialLayerReadStatus.Success, restoredView.ReadLayer("lake", new TileRect(tile, 1, 1), values), "associated water level restores independently");
            c.Equal(Fix.FromRatio(9, 8), values[0], "water level retains its exact value");
            provider.AssignOwner(game.World, "deposit", nextOwner);
            ((ISpatialLayerProvider)restored.World.Space).AssignOwner(restored.World, "deposit", nextOwner);
            c.Equal(owner, retained.Owner, "retained metadata snapshot does not change on transfer");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("deposit", new TileRect(tile, 1, 1), values), "old owner loses live access");
            c.Equal(SpatialLayerReadStatus.Success, other.ReadLayer("deposit", new TileRect(tile, 1, 1), values), "new owner gains access through engine authorization");
            game.Update(); restored.Update();
            c.Equal(game.Hash(), restored.Hash(), "saved twin continues identically after ownership change and Update");
            c.Equal(0, game.Snapshot.Entities.Count, "a layer-only world does not require fake entities");
            OwnershipInvalidation(c);
        }

        private static void OwnershipInvalidation(Check c)
        {
            JsonValue definitions = Json.Parse(Definitions);
            definitions.Get("scenarios")[0].Set("seats", JsonValue.Number("3"));
            definitions.Get("scenarios")[0].Set("default_seats", JsonValue.Number("3"));
            JsonValue unrelatedLayer = definitions.Get("spatial_layers")[1].Clone();
            unrelatedLayer.Set("id", JsonValue.Of("unrelated.deposit"));
            definitions.Get("spatial_layers").Add(unrelatedLayer);
            GameFactory factory = Factory(definitions.ToString()); SimGame game = New(factory);
            var map = (TileMap)game.World.Space; var layer = map.Layer<FixedLayer>("deposit");
            PlayerId oldOwner = new PlayerId(0), newOwner = new PlayerId(1), unrelated = new PlayerId(2);
            map.AssignOwner(game.World, "unrelated.deposit", unrelated);
            var other = game.Seat(2).View.Feature<TileMapView>(); var output = new List<Fix>();
            var area = new TileRect(0, 0, 2, 1);
            SpatialLayerPageResult page = other.ReadLayerPage("lake", area, new SpatialLayerPageRequest(0, 1), output);
            int observer = map.Version, publicCursor = map.VersionFor(unrelated);
            var publicBaseline = other.ReadChanges().Cursor;
            int initialOwnerCursor = map.VersionFor(oldOwner), prospectiveOwnerCursor = map.VersionFor(newOwner);
            map.AssignOwner(game.World, "deposit", oldOwner);
            c.That(map.Version > observer && map.VersionFor(oldOwner) > initialOwnerCursor, "private initial assignment invalidates observer and actual new owner");
            c.Equal(prospectiveOwnerCursor, map.VersionFor(newOwner), "initial assignment cannot leak to a prospective unrelated owner");
            c.Equal(publicCursor, other.Version, "initial assignment cannot leak to third-company cursor");
            c.Equal(SpatialLayerReadStatus.Success, other.ReadLayerPage("lake", area,
                new SpatialLayerPageRequest(1, 1, page.Cursor), output).Status, "unrelated public page continuation survives private assignment");
            int oldCursor = map.VersionFor(oldOwner), newCursor = map.VersionFor(newOwner); observer = map.Version;
            layer.Set(new TileCoord(0, 0), Fix.FromInt(4));
            oldCursor = map.VersionFor(oldOwner);
            map.AssignOwner(game.World, "deposit", newOwner);
            c.That(map.Version > observer && map.VersionFor(oldOwner) > oldCursor && map.VersionFor(newOwner) > newCursor,
                "transfer invalidates only observer plus both affected owners");
            c.Equal(publicCursor, other.Version, "private transfer cannot invalidate third-company cursor");
            c.That(!other.ReadChanges(publicBaseline).FullInvalidation
                && other.ReadChanges(publicBaseline).ChangedRegions.Count == 0, "private transfer cannot leak through unrelated chunk deltas");
            c.Equal(SpatialLayerReadStatus.Success, other.ReadLayerPage("lake", area,
                new SpatialLayerPageRequest(1, 1, page.Cursor), output).Status, "unrelated page continuation survives ownership transfer");
            c.Equal(SpatialLayerReadStatus.Unavailable, game.Seat(0).View.Feature<TileMapView>().ReadLayer("deposit", area, output), "previous owner loses values immediately");
            c.Equal(SpatialLayerReadStatus.Success, game.Seat(1).View.Feature<TileMapView>().ReadLayer("deposit", area, output), "new owner sees actual values immediately");
            c.Equal(Fix.FromInt(4), output[0], "ownership transfer never copies or resets sparse values");
            int layerCursor = layer.Version;
            InjectCounter(map, "_version", int.MaxValue);
            map.AssignOwner(game.World, "deposit", newOwner);
            c.Equal(layerCursor, layer.Version, "same-owner assignment remains a no-op at exhausted observer cursor");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "private transfer and transient exhaustion keep sole saved values/owner codec");
            game.Update(); restored.Update(); c.Equal(game.Hash(), restored.Hash(), "private ownership twins continue through real Update");

            foreach (string fault in new[] { "layer", "observer", "old-owner", "new-owner", "new-inherited-public" })
            {
                SimGame failed = New(factory); TileMap failedMap = (TileMap)failed.World.Space;
                failedMap.AssignOwner(failed.World, "deposit", oldOwner);
                var failedLayer = failedMap.Layer<FixedLayer>("deposit");
                if (fault == "layer") failedLayer.Version = int.MaxValue;
                else if (fault == "observer") InjectCounter(failedMap, "_version", int.MaxValue);
                else if (fault == "old-owner") InjectCounter(failedMap, "Version", int.MaxValue, oldOwner);
                else if (fault == "new-inherited-public") InjectCounter(failedMap, "_publicVersion", int.MaxValue);
                else
                {
                    failedMap.AssignOwner(failed.World, "deposit", newOwner);
                    failedMap.AssignOwner(failed.World, "deposit", oldOwner);
                    InjectCounter(failedMap, "Version", int.MaxValue, newOwner);
                }
                string before = Cursors(failedMap, failedLayer); ulong hash = failed.Hash();
                c.Throws<OverflowException>(() => failedMap.AssignOwner(failed.World, "deposit", newOwner), fault + " ownership counter refuses atomically");
                c.Equal(oldOwner, failedLayer.Owner, fault + " refusal retains actual private owner");
                c.Equal(before, Cursors(failedMap, failedLayer), fault + " refusal retains layer/map/view/chunk cursors");
                c.Equal(hash, failed.Hash(), fault + " refusal retains sole authoritative hash");
                c.Equal(hash, factory.Load(failed.Save()).Hash(), fault + " refusal remains resumable");
            }
        }

        private static void InvalidContent(Check c)
        {
            foreach (var change in new[]
            {
                new KeyValuePair<string, string>("space", "missing"),
                new KeyValuePair<string, string>("type", ""),
                new KeyValuePair<string, string>("unit", ""),
                new KeyValuePair<string, string>("unit", "unit.unknown"),
                new KeyValuePair<string, string>("vertical_unit", "unit.unknown"),
                new KeyValuePair<string, string>("value_type", "float"),
                new KeyValuePair<string, string>("visibility", "implicit")
            })
            {
                JsonValue json = Json.Parse(Definitions);
                json.Get("spatial_layers")[0].Set(change.Key, JsonValue.Of(change.Value));
                c.Throws<ContentException>(() => New(Factory(json.ToString())), "malformed layer " + change.Key + " refuses before Update");
            }
            JsonValue duplicate = Json.Parse(Definitions);
            duplicate.Get("spatial_layers").Add(duplicate.Get("spatial_layers")[0].Clone());
            c.Throws<ContentException>(() => New(Factory(duplicate.ToString())), "duplicate layer identity is rejected by the sole content index");
            JsonValue vertical = Json.Parse(Definitions);
            vertical.Get("spatial_layers")[0].Remove("vertical_domain");
            c.Throws<ContentException>(() => New(Factory(vertical.ToString())), "vertical reference cannot imply an undeclared vertical domain");
            JsonValue noVerticalUnit = Json.Parse(Definitions);
            noVerticalUnit.Get("spatial_layers")[1].Remove("vertical_unit");
            c.Throws<ContentException>(() => New(Factory(noVerticalUnit.ToString())), "mass-valued deposits cannot imply the unit of subsurface depth");
            foreach (var change in new[]
            {
                new KeyValuePair<string, string>("resource", "resource.missing"),
                new KeyValuePair<string, string>("resource", ""),
                new KeyValuePair<string, string>("resource_measure", ""),
                new KeyValuePair<string, string>("resource_measure", "automatic"),
                new KeyValuePair<string, string>("unit", "unit.metre")
            })
            {
                JsonValue json = Json.Parse(Definitions);
                json.Get("spatial_layers")[1].Set(change.Key, JsonValue.Of(change.Value));
                c.Throws<ContentException>(() => New(Factory(json.ToString())), "invalid deposit " + change.Key + " refuses before Initialize");
            }
            JsonValue implicitMeasure = Json.Parse(Definitions);
            implicitMeasure.Get("spatial_layers")[1].Remove("resource_measure");
            c.Throws<ContentException>(() => New(Factory(implicitMeasure.ToString())), "resource interpretation must be explicit, never inferred from a semantic name");
            JsonValue noResource = Json.Parse(Definitions);
            noResource.Get("spatial_layers")[1].Remove("resource");
            c.Throws<ContentException>(() => New(Factory(noResource.ToString())), "resource measure without a resource fails");
            JsonValue customType = Json.Parse(Definitions);
            customType.Get("spatial_layers")[0].Set("type", JsonValue.Of("planet.gravity_potential"));
            SimGame custom = New(Factory(customType.ToString()));
            c.Equal(new Code("planet.gravity_potential"), custom.World.Content.Get<ISpatialLayerDefinition>("elevation").Type, "new semantic types require authored content, not an engine enum update");
        }
    }
}
