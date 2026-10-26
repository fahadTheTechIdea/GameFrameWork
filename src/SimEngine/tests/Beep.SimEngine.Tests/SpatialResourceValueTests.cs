using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    // Author inputs and observe actual provider operations/checkpoints; no extraction or game rules here.
    internal static partial class SpatialResourceValueTests
    {
        internal static void Register()
        {
            TestRunner.Add("Spatial resource values: catalog legality and atomic native generation", Admission);
            TestRunner.Add("Spatial resource values: provider checkpoints, private queries and saved continuation", Persistence);
            TestRunner.Add("Spatial depletion: exact native admission and atomic stored remainder", DepletionAdmission);
            TestRunner.Add("Spatial depletion: independent geometry, private facts and saved continuation", DepletionPersistence);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Tiles(int? width = null, Action<JsonValue> edit = null)
        {
            var module = new ProbeModule("quantity.proof", new ModuleVersion(1, 0, 0));
            var definition = new GameDefinition("quantity.proof", new GameModule[] { module });
            var authored = Content.Json.Parse(FrameworkProofContent.Read("spatial-resource-values.json"));
            if (width.HasValue) authored.Get("tile_spaces")[0].Set("width", JsonValue.Number(width.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            edit?.Invoke(authored);
            return new GameFactory(definition, ContentSet.Load(new PreloadedFileSource()
                .Add("manifest.json", "{\"schema\":1,\"modules\":[{\"id\":\"quantity.proof\",\"version\":\"1.0.0\",\"documents\":[\"spatial-resource-values.json\"]}]}")
                .Add("spatial-resource-values.json", Text(authored)), definition.ContentTypes));
        }
        private static SimGame NewTile(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "quantities", Seed = 227 });
        private static GameFactory Points()
        {
            var definitions = Content.Json.Parse(PointGame.Definitions); definitions.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var layers = Content.Json.Parse(FrameworkProofContent.Read("point-layers.json"));
            var units = Content.Json.Parse(FrameworkProofContent.Read("spatial-resource-values.json")).Get("units");
            for (int i = 0; i < units.Count; i++) layers.Get("units").Add(units[i]);
            layers.Get("spatial_layers")[1].Set("unit", JsonValue.Of("quantity.half"));
            return PointGame.Factory(Text(definitions), Text(layers));
        }
        private static SimGame NewPoint(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 227 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static byte[] Encode(MapLayer layer)
        { using (var stream = new MemoryStream()) { layer.Sync(new StateWriter(stream)); return stream.ToArray(); } }
        private static void PointSync(PointLayer layer, IStateSync sync, SimGame game)
        {
            try { typeof(PointLayer).GetMethod("Sync", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(layer,
                new object[] { sync, game.World.Content.Get<PointSpaceDef>(game.World.Space.Id).Extent, game.World }); }
            catch (TargetInvocationException error) { throw error.InnerException; }
        }
        private static byte[] Encode(PointLayer layer, SimGame game)
        { using (var stream = new MemoryStream()) { PointSync(layer, new StateWriter(stream), game); return stream.ToArray(); } }
        private static PointLayer Deposit(SimGame game)
        { foreach (var layer in ((ISpatialLayerProvider)game.World.Space).SpatialLayers) if (layer.Id == "point.deposit") return (PointLayer)layer; throw new InvalidOperationException("deposit"); }
        private static void Admission(Check c)
        {
            var factory = Tiles(); var game = NewTile(factory); var map = (TileMap)game.World.Space;
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "resource-cell legality has current execution identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "quantity legality adds no stock or alternate value payload");
            c.Equal(6, map.StateSchemaVersion, "native value owner declares stricter checkpoint semantics");
            var bytes = map.Layer<ByteLayer>("quantity.byte"); var integers = map.Layer<IntLayer>("quantity.int");
            var counts = map.Layer<FixedLayer>("quantity.fixed"); var pairs = map.Layer<FixedLayer>("quantity.pairs");
            var mass = map.Layer<FixedLayer>("quantity.mass"); var thirds = map.Layer<FixedLayer>("quantity.thirds");
            c.Equal(UnitConversionStatus.InvalidUnit, SpatialResourceValues.CheckValue(null, Fix.One), "absent metadata never fabricates material legality");
            c.Equal(UnitConversionStatus.FractionalDiscrete, SpatialResourceValues.CheckValue(bytes, Fix.One), "stored whole half-unit is fractional native material");
            c.Equal(UnitConversionStatus.FractionalDiscrete, SpatialResourceValues.CheckValue(counts, Fix.FromRatio(3, 2)), "native discrete unit rejects fractional count");
            c.Equal(UnitConversionStatus.FractionalDiscrete, SpatialResourceValues.CheckValue(pairs, Fix.FromRatio(3, 2)), "stored discrete unit stays mandatory even when native result would be whole");
            c.Equal(UnitConversionStatus.InvalidAmount, SpatialResourceValues.CheckValue(integers, -Fix.One), "quantity metadata forbids negative material");
            c.Equal(UnitConversionStatus.Unrepresentable, SpatialResourceValues.CheckValue(thirds, Fix.FromRaw(1)), "inexact native raw conversion never rounds");
            c.Equal(UnitConversionStatus.OutOfRange, SpatialResourceValues.CheckValue(mass, Fix.FromRaw(long.MaxValue)), "exact native scaling cannot overflow its quantity representation");
            c.Equal(Fix.One, SpatialResourceValues.ConvertQuantity(bytes, Fix.FromInt(2)).Amount, "byte layer's authored two halves become one native part");
            c.Equal(Fix.FromInt(2000), SpatialResourceValues.ConvertQuantity(mass, Fix.FromInt(2)).Amount, "spatial quantities use same exact catalog scaling as resources");
            c.Equal(Fix.FromRaw(1), SpatialResourceValues.ConvertQuantity(thirds, Fix.FromRaw(3)).Amount, "representable small-scale quantity remains exact");
            c.Equal(Fix.Zero, SpatialResourceValues.ConvertQuantity(mass, Fix.Zero).Amount, "valid empty cell is actual zero availability, not stock creation");
            foreach (ISpatialLayer layer in ((ISpatialLayerProvider)map).SpatialLayers)
                if (layer.ResourceMeasure == SpatialResourceMeasure.Quantity)
                    c.That(ReferenceEquals(layer.Resource, game.World.Content.Resource(layer.Resource.Resource)) &&
                        ReferenceEquals(layer.Unit, game.World.Content.Find<UnitDef>(layer.Unit.Unit.Id)), "actual quantity metadata aliases sole immutable catalogs");
            ((ISpatialLayerProvider)map).AssignOwner(game.World, integers.Id, new PlayerId(0));
            NativeWrites(c, game, bytes, (byte)2, (byte)1);
            NativeWrites(c, game, integers, 2, -1);
            NativeWrites(c, game, counts, Fix.One, Fix.FromRatio(3, 2));
            NativeWrites(c, game, pairs, Fix.One, Fix.FromRatio(3, 2));
            NativeWrites(c, game, mass, Fix.One, Fix.FromRaw(long.MaxValue));
            NativeWrites(c, game, thirds, Fix.FromRaw(3), Fix.FromRaw(1));
            var associated = map.Layer<FixedLayer>("quantity.associated"); var signed = map.Layer<IntLayer>("quantity.signed");
            associated.Set(new TileCoord(2, 2), Fix.FromRatio(-5, 2)); signed.Set(new TileCoord(2, 2), int.MinValue);
            c.Equal(Fix.FromRatio(-5, 2), associated[2, 2], "associated below-datum elevation is not negative resource stock");
            c.Equal(int.MinValue, signed[2, 2], "unlinked signed semantic layer retains full storage range");
            c.Equal(UnitConversionStatus.Converted, SpatialResourceValues.CheckValue(associated, -Fix.One), "associated unit remains independent from material quantity unit");
            c.Equal(UnitConversionStatus.InvalidUnit, SpatialResourceValues.ConvertQuantity(associated, Fix.One).Status, "associated resource link never authorizes extraction by inference");
            c.Equal(UnitConversionStatus.InvalidUnit, SpatialResourceValues.ConvertQuantity(signed, Fix.One).Status, "unlinked layer has no fabricated native resource");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "all admitted native representations retain existing codecs");
            var wideFactory = Tiles(int.MaxValue); var wide = NewTile(wideFactory); var wideMap = (TileMap)wide.World.Space;
            var edge = new TileCoord(int.MaxValue - 1, 16); var wideLayer = wideMap.Layer<ByteLayer>("quantity.byte");
            wideLayer.Set(edge, 2); ulong wideHash = wide.Hash();
            c.Equal((byte)2, wideLayer[edge], "legal material value at actual int-max authored extent has no platform cap");
            c.Throws<ArgumentException>(() => wideLayer.Set(edge, 1), "wide sparse quantity admission uses same native quantum rule");
            c.Equal(wideHash, wide.Hash(), "wide quantity refusal preserves actual sparse world");
            c.Equal(wideHash, wideFactory.Load(wide.Save()).Hash(), "wide sparse material checkpoint retains exact geometry and value");
        }
        private static void NativeWrites<T>(Check c, SimGame game, MapLayer<T> layer, T valid, T invalid) where T : struct
        {
            var at = new TileCoord(1, 1); layer.Set(at, valid); ulong hash = game.Hash(); int version = layer.Version;
            int own = layer.Map.VersionFor(new PlayerId(0)), other = layer.Map.VersionFor(new PlayerId(1)); var original = Encode(layer);
            c.Throws<ArgumentException>(() => layer.Set(at, invalid), "invalid native quantity refuses existing cell: " + layer.Id);
            c.Throws<ArgumentException>(() => layer.Set(new TileCoord(17, 16), invalid), "invalid native quantity refuses before sparse allocation: " + layer.Id);
            c.Equal(valid, layer[at], "refused native writes retain actual value: " + layer.Id);
            c.Equal(hash, game.Hash(), "refused native writes change no session state: " + layer.Id);
            c.Throws<ArgumentException>(() => layer.Fill((x, y) => x == 17 && y == 16 ? invalid : valid), "late invalid generation refuses whole candidate: " + layer.Id);
            c.Equal(Convert.ToBase64String(original), Convert.ToBase64String(Encode(layer)), "failed generation retains authoritative sparse storage: " + layer.Id);
            c.Equal(version, layer.Version, "refusal retains layer version: " + layer.Id);
            c.Equal(own, layer.Map.VersionFor(new PlayerId(0)), "refusal retains owner cursor: " + layer.Id);
            c.Equal(other, layer.Map.VersionFor(new PlayerId(1)), "refusal retains foreign cursor: " + layer.Id);
            c.Equal(hash, game.Hash(), "failed generator creates no resource or map state: " + layer.Id);
            layer.Fill((x, y) => x == 17 && y == 16 ? valid : default);
            c.Equal(valid, layer[17, 16], "valid generator publishes exact quantity: " + layer.Id);
            c.Equal(default(T), layer[at], "generated layer replaces, not merges, previous cells: " + layer.Id);
            layer.Set(new TileCoord(17, 16), default);
            c.Equal(8, Encode(layer).Length, "empty quantity layer retains native sparse and resource-binding section counts: " + layer.Id);
        }
        private static void Persistence(Check c)
        {
            var factory = Tiles(); var game = NewTile(factory); var map = (TileMap)game.World.Space;
            ((ISpatialLayerProvider)map).AssignOwner(game.World, "quantity.int", new PlayerId(0));
            NativeCheckpoint(c, game, map.Layer<ByteLayer>("quantity.byte"), (byte)2, (byte)1, 1, new byte[] { 1 });
            NativeCheckpoint(c, game, map.Layer<IntLayer>("quantity.int"), 2, -1, 4, BitConverter.GetBytes(-1));
            NativeCheckpoint(c, game, map.Layer<FixedLayer>("quantity.fixed"), Fix.One, Fix.FromRatio(3, 2), 8, BitConverter.GetBytes(Fix.FromRatio(3, 2).Raw));
            NativeCheckpoint(c, game, map.Layer<FixedLayer>("quantity.mass"), Fix.One, Fix.FromRaw(long.MaxValue), 8, BitConverter.GetBytes(long.MaxValue));
            NativeCheckpoint(c, game, map.Layer<FixedLayer>("quantity.thirds"), Fix.FromRaw(3), Fix.FromRaw(1), 8, BitConverter.GetBytes(1L));
            var values = new List<int> { 99 }; var mine = game.Seat(0).View.Feature<TileMapView>(); var foreign = game.Seat(1).View.Feature<TileMapView>();
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("quantity.int", new TileRect(0, 0, 1, 1), values), "authorized query reads real validated quantity");
            c.Equal(2, values[0], "layer query retains stored unit rather than silently converting UI data");
            c.Equal(SpatialLayerReadStatus.Unavailable, foreign.ReadLayer("quantity.int", new TileRect(0, 0, 1, 1), values), "foreign queries expose no material availability");
            c.Equal(0, values.Count, "private refusal clears prior material samples");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "validated native layer checkpoint restores exact quantities");
            for (int i = 0; i < 8; i++) { game.Update(); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "interleaved native layer sessions remain deterministic"); }
            Points(c);
        }
        private static void NativeCheckpoint<T>(Check c, SimGame game, MapLayer<T> layer, T valid, T invalid, int width, byte[] invalidBytes) where T : struct
        {
            layer.Set(new TileCoord(0, 0), valid); layer.Set(new TileCoord(17, 16), valid);
            byte[] original = Encode(layer), bad = (byte[])original.Clone(); int record = 8 + (width == 1 ? 4 : 0) + 256 * width;
            int second = 4 + record + 8 + (width == 1 ? 4 : 0) + width; // Second edge chunk's actual x1/y0 cell.
            Array.Copy(invalidBytes, 0, bad, second, width);
            c.Throws<CorruptStateException>(() => layer.Sync(new StateReader(bad, 0, bad.Length)), "late corrupt resource quantity refuses restore: " + layer.Id);
            c.Equal(Convert.ToBase64String(original), Convert.ToBase64String(Encode(layer)), "failed native restore retains both original chunks: " + layer.Id);
            var chunks = (Dictionary<long, T[]>)typeof(MapLayer<T>).GetField("_chunks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(layer);
            chunks[0][0] = invalid;
            c.Throws<CorruptStateException>(() => { T ignored = layer[0, 0]; }, "live malformed material cannot produce a usable read: " + layer.Id);
            c.Throws<CorruptStateException>(() => Encode(layer), "live malformed material cannot save: " + layer.Id);
            c.Throws<CorruptStateException>(() => game.Hash(), "live malformed material cannot hash: " + layer.Id);
            chunks[0][0] = valid;
            c.Equal(Convert.ToBase64String(original), Convert.ToBase64String(Encode(layer)), "instrumented valid quantity restores sole authority: " + layer.Id);
        }
        private static void Points(Check c)
        {
            var factory = Points(); var game = NewPoint(factory); var layer = Deposit(game);
            c.Equal(4, game.World.Space.StateSchemaVersion, "public provider declares strict staged checkpoint semantics");
            c.That(TestKit.Order(game, 0, new AssignPointLayerCommand { Layer = layer.Id, Owner = new PlayerId(0) }).Accepted, "ordinary engine-side command assigns private resource owner");
            var mine = game.Seat(0).View.Feature<PointLayerView>(); var other = game.Seat(1).View.Feature<PointLayerView>();
            foreach (Fix value in new[] { -Fix.One, Fix.One, Fix.FromRatio(3, 2) })
            {
                ulong hash = game.Hash(); int cursor = mine.Version, hidden = other.Version;
                var command = new WritePointLayerCommand { Layer = layer.Id, Position = Fix.FromInt(3), Value = value };
                c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(command), "public provider applies canonical negative/native-discrete legality");
                c.Equal(hash, game.Hash(), "point preview allocates no material sample or resource");
                c.That(!TestKit.Order(game, 0, command).Accepted && game.Fault == null, "actual point command shares refusal without gameplay fault");
                c.Equal(cursor, mine.Version, "refused point write retains owner cursor"); c.Equal(hidden, other.Version, "refused point write leaks no private cursor change");
            }
            foreach (int point in new[] { 3, 5 }) c.That(TestKit.Order(game, 0, new WritePointLayerCommand { Layer = layer.Id, Position = Fix.FromInt(point), Value = Fix.FromInt(2) }).Accepted, "public commands store actual exact two-half quantities");
            c.Equal(Fix.One, SpatialResourceValues.ConvertQuantity(layer, Fix.FromInt(2)).Amount, "public provider and native layers share exact unit conversion");
            byte[] original = Encode(layer, game), bad = (byte[])original.Clone();
            // Change candidate ownership and second material sample; neither may partially commit.
            Array.Copy(BitConverter.GetBytes(1), 0, bad, 0, 4); Array.Copy(BitConverter.GetBytes(Fix.One.Raw), 0, bad, 32, 8);
            c.Throws<CorruptStateException>(() => PointSync(layer, new StateReader(bad, 0, bad.Length), game), "public staged restore refuses late fractional-native quantity");
            c.Equal(new PlayerId(0), layer.Owner, "failed public restore retains actual layer owner");
            c.Equal(Convert.ToBase64String(original), Convert.ToBase64String(Encode(layer, game)), "failed public restore retains all sample values");
            var samples = (SortedDictionary<long, Fix>)typeof(PointLayer).GetField("_samples", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(layer);
            samples[Fix.FromInt(3).Raw] = -Fix.One;
            c.Throws<CorruptStateException>(() => mine.ReadLayer(layer.Id, new PointRegion(Fix.FromInt(3), Fix.One, 1), new List<Fix>()), "public malformed material cannot publish query value");
            c.Throws<CorruptStateException>(() => Encode(layer, game), "public malformed material cannot save");
            c.Throws<CorruptStateException>(() => game.Hash(), "public malformed material cannot hash");
            samples[Fix.FromInt(3).Raw] = Fix.FromInt(2);
            var space = (PointSpace)game.World.Space;
            var observerCounter = typeof(PointSpace).GetField("_observerVersion", BindingFlags.Instance | BindingFlags.NonPublic);
            int counter = (int)observerCounter.GetValue(space); observerCounter.SetValue(space, int.MaxValue);
            ulong beforeNoop = game.Hash();
            c.Equal(Code.None, space.WriteLayer(game.World, new PlayerId(0), layer.Id, Fix.FromInt(3), Fix.FromInt(2)), "valid same-quantity write remains a no-op at cursor exhaustion");
            c.Equal(beforeNoop, game.Hash(), "same-quantity no-op changes no authoritative sample or owner state");
            c.Throws<OverflowException>(() => space.WriteLayer(game.World, new PlayerId(0), layer.Id, Fix.FromInt(3), Fix.FromInt(4)), "actual quantity change preflights exhausted public-provider counter");
            c.Equal(beforeNoop, game.Hash(), "counter refusal retains original exact material sample");
            observerCounter.SetValue(space, counter);
            var values = new List<Fix> { Fix.One };
            c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer(layer.Id, new PointRegion(Fix.FromInt(3), Fix.One, 1), values), "public private quantities keep existing authorization");
            c.Equal(0, values.Count, "public private refusal clears samples");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "shared public quantity contract survives saved provider state");
            for (int i = 0; i < 8; i++) { game.Update(); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "saved public quantity sessions remain isolated and deterministic"); }
        }
    }
}
