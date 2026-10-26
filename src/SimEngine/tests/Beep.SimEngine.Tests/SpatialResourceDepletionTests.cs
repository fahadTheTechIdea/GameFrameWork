using System;
using System.Collections.Generic;
using System.Reflection;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    // Same authored fixtures; probes actual engine-side owner primitives, not extraction gameplay.
    internal static partial class SpatialResourceValueTests
    {
        private static void DepletionAdmission(Check c)
        {
            GameFactory factory = Tiles(); SimGame game = NewTile(factory); var map = (TileMap)game.World.Space;
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current exact-depletion execution identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no new stock/claim payload or legacy reader");
            c.Equal(6, map.StateSchemaVersion, "native sparse owner/checkpoint schema unchanged");
            c.That(!default(SpatialResourceChange).Ready && !default(SpatialResourceChange).Committed, "default withdrawal refuses");
            c.Throws<InvalidOperationException>(() => default(SpatialResourceChange).AfterCommit(), "default cannot invent a commit");
            c.Throws<ArgumentOutOfRangeException>(() => SpatialResourceChange.Refused(SpatialResourceChangeStatus.Committed), "refusal factory cannot invent success");
            c.Equal(SpatialResourceChangeStatus.NotQuantity, SpatialResourceValues.CheckWithdrawal(null, Fix.Zero, Fix.One).Status, "null cannot infer material");
            NativeDepletion(c, game, map.Layer<ByteLayer>("quantity.byte"), (byte)6, (byte)4, Fix.FromInt(3), Fix.One);
            var privateLayer = map.Layer<IntLayer>("quantity.int"); map.AssignOwner(game.World, privateLayer.Id, new PlayerId(0));
            NativeDepletion(c, game, privateLayer, 6, 4, Fix.FromInt(3), Fix.One);
            NativeDepletion(c, game, map.Layer<FixedLayer>("quantity.fixed"), Fix.FromInt(3), Fix.FromInt(2), Fix.FromInt(3), Fix.One);
            NativeDepletion(c, game, map.Layer<FixedLayer>("quantity.pairs"), Fix.FromInt(3), Fix.FromInt(2), Fix.FromInt(6), Fix.FromInt(2));
            NativeDepletion(c, game, map.Layer<FixedLayer>("quantity.mass"), Fix.FromInt(2), Fix.FromRatio(15, 8), Fix.FromInt(2000), Fix.FromInt(125));
            NativeDepletion(c, game, map.Layer<FixedLayer>("quantity.thirds"), Fix.FromRaw(9), Fix.FromRaw(6), Fix.FromRaw(3), Fix.FromRaw(1));

            var pairs = map.Layer<FixedLayer>("quantity.pairs"); var cell = new TileCoord(0, 0); pairs.Set(cell, Fix.One);
            ulong before = game.Hash(); SpatialResourceChange refusal = pairs.Withdraw(cell, Fix.One);
            c.Equal(SpatialResourceChangeStatus.Unrepresentable, refusal.Status, "one native part cannot leave half a discrete pair");
            c.Equal(UnitConversionStatus.FractionalDiscrete, refusal.Conversion, "original stored-quantum diagnostic survives");
            c.Equal(before, game.Hash(), "quantum refusal changes no material");
            c.Equal(Fix.Zero, refusal.Before, "refusals carry no fabricated stock facts");
            var mass = map.Layer<FixedLayer>("quantity.mass"); mass.Set(cell, Fix.One); before = game.Hash();
            refusal = mass.Withdraw(cell, Fix.FromRaw(1));
            c.Equal(SpatialResourceChangeStatus.Unrepresentable, refusal.Status, "small native debit cannot round a stored tonne remainder");
            c.Equal(UnitConversionStatus.Unrepresentable, refusal.Conversion, "inverse exact conversion reports its original failure");
            c.Equal(before, game.Hash(), "inexact inverse retains actual cell");
            foreach (MapLayer layer in new MapLayer[] { map.Layer<FixedLayer>("quantity.associated"), map.Layer<IntLayer>("quantity.signed") })
                c.Equal(SpatialResourceChangeStatus.NotQuantity, SpatialResourceValues.CheckWithdrawal(layer, -Fix.One, Fix.One).Status,
                    "signed associated/unlinked measurements are never material by inference");
            var associated = map.Layer<FixedLayer>("quantity.associated"); associated.Set(cell, -Fix.One);
            var signed = map.Layer<IntLayer>("quantity.signed"); signed.Set(cell, int.MinValue); before = game.Hash();
            c.Equal(SpatialResourceChangeStatus.NotQuantity, associated.Withdraw(cell, Fix.One).Status, "associated owner itself refuses material depletion");
            c.Equal(SpatialResourceChangeStatus.NotQuantity, signed.Withdraw(cell, Fix.One).Status, "unlinked owner itself refuses material depletion");
            c.Equal(before, game.Hash(), "nonquantity refusal retains signed actual measurements");
            c.Throws<InvalidOperationException>(() => associated.ReadQuantity(cell), "associated read cannot fabricate native stock");
            c.Throws<InvalidOperationException>(() => signed.ReadQuantity(cell), "unlinked read cannot fabricate native stock");

            var counts = map.Layer<FixedLayer>("quantity.fixed"); counts.Set(cell, Fix.FromInt(3)); before = game.Hash();
            refusal = counts.Withdraw(cell, Fix.FromRatio(1, 2));
            c.Equal(SpatialResourceChangeStatus.InvalidAmount, refusal.Status, "native discrete quantity rejects fractional withdrawal");
            c.Equal(UnitConversionStatus.FractionalDiscrete, refusal.Conversion, "native discrete diagnostic stays explicit");
            c.Equal(before, game.Hash(), "fractional native request retains stock");
            SpatialResourceChange retained = counts.CheckWithdrawal(cell, Fix.FromInt(2));
            c.That(retained.Ready && counts.Withdraw(cell, Fix.FromInt(2)).Committed, "actual first spend uses current stock");
            before = game.Hash(); c.Equal(SpatialResourceChangeStatus.InsufficientQuantity, counts.Withdraw(cell, retained.Amount).Status, "retained readiness cannot overspend current stock");
            c.Equal(before, game.Hash(), "stale readiness is not a resource receipt");

            // Whole byte/int shapes are mandatory even with continuous native/authored units.
            SimGame continuous = NewTile(Tiles(edit: root =>
            {
                for (int i = 0; i < 2; i++)
                { root.Get("spatial_layers")[i].Set("unit", JsonValue.Of("quantity.kg")); root.Get("spatial_layers")[i].Set("resource", JsonValue.Of("quantity.ore")); }
            }));
            var continuousMap = (TileMap)continuous.World.Space;
            var continuousByte = continuousMap.Layer<ByteLayer>("quantity.byte"); continuousByte.Set(cell, 2);
            var continuousInt = continuousMap.Layer<IntLayer>("quantity.int"); continuousInt.Set(cell, 2);
            before = continuous.Hash();
            c.Equal(UnitConversionStatus.FractionalDiscrete, continuousByte.Withdraw(cell, Fix.FromRatio(1, 2)).Conversion, "continuous catalog does not make byte cells fractional");
            c.Equal(UnitConversionStatus.FractionalDiscrete, continuousInt.Withdraw(cell, Fix.FromRatio(1, 2)).Conversion, "continuous catalog does not make int cells fractional");
            c.Equal(before, continuous.Hash(), "shape failures preserve both actual sparse stores");
            c.Equal(UnitConversionStatus.OutOfRange, SpatialResourceValues.CheckValue(continuousByte, Fix.FromInt(256)), "shared quantity checker enforces actual byte shape");
            c.Equal(UnitConversionStatus.Converted, SpatialResourceValues.CheckValue(continuousInt, Fix.FromInt(int.MaxValue)), "largest whole fixed-point quantity fits actual int storage");
            c.Equal(UnitConversionStatus.FractionalDiscrete, SpatialResourceValues.CheckValue(continuousInt, Fix.FromRaw(long.MaxValue)), "upper-range fraction cannot be truncated into int storage");
            c.Throws<CorruptStateException>(() => SpatialResourceValues.CheckWithdrawal(continuousByte, Fix.FromRatio(1, 2), Fix.FromRatio(1, 2)), "invalid existing shape is corrupt, not empty");
            continuousInt.Set(cell, int.MaxValue);
            c.That(continuousInt.Withdraw(cell, Fix.One).Committed, "actual largest whole int stock depletes without narrowing");
            c.Equal(int.MaxValue - 1, continuousInt[cell], "largest whole int debit is exact");

            GameFactory wideFactory = Tiles(int.MaxValue); SimGame wide = NewTile(wideFactory); var wideMap = (TileMap)wide.World.Space;
            var edge = new TileCoord(int.MaxValue - 1, 16); var wideLayer = wideMap.Layer<ByteLayer>("quantity.byte"); wideLayer.Set(edge, 4);
            c.That(wideLayer.Withdraw(edge, Fix.One).Committed, "actual far-edge depletion has no platform world cap");
            c.Equal((byte)2, wideLayer[edge], "far-edge exact stored halves retain sparse geometry");
            c.Equal(wide.Hash(), wideFactory.Load(wide.Save()).Hash(), "wide actual depletion saves without dense enumeration");
        }

        private static void NativeDepletion<T>(Check c, SimGame game, MapLayer<T> layer, T initial, T remainder,
            Fix nativeBefore, Fix amount) where T : struct
        {
            var tile = new TileCoord(17, 16); layer.Set(tile, initial);
            ISpatialResourceQuantity<TileCoord> contract = layer;
            ulong hash = game.Hash(); int version = layer.Version, mapVersion = layer.Map.Version;
            int ownerCursor = layer.Map.VersionFor(new PlayerId(0)), otherCursor = layer.Map.VersionFor(new PlayerId(1));
            SpatialResourceChange check = contract.CheckWithdrawal(tile, amount);
            c.Equal(nativeBefore, contract.ReadQuantity(tile).Amount, layer.Id + " native read derives current material directly from actual cell");
            c.Equal(layer.Resource.QuantityUnit.Unit, contract.ReadQuantity(tile).Unit, layer.Id + " native read carries actual unit identity");
            c.That(check.Ready && !check.Committed, layer.Id + " pure contract admits exact current amount");
            c.Equal(nativeBefore, check.Before, layer.Id + " current amount uses native catalog units");
            c.Equal(layer.Resource.QuantityUnit.Unit, check.NativeUnit, layer.Id + " actual resource unit alias");
            c.Equal(hash, game.Hash(), layer.Id + " readiness never changes state");
            foreach (Fix invalid in new[] { Fix.Zero, -Fix.One })
                c.Equal(SpatialResourceChangeStatus.InvalidAmount, contract.Withdraw(tile, invalid).Status, layer.Id + " nonpositive amounts refuse");
            c.Equal(SpatialResourceChangeStatus.InsufficientQuantity, contract.Withdraw(tile, nativeBefore + Fix.One).Status, layer.Id + " no hidden stock creation");
            c.Equal(SpatialResourceChangeStatus.InsufficientQuantity, contract.Withdraw(new TileCoord(0, 0), amount).Status, layer.Id + " sparse empty cell is actual zero");
            c.Equal(SpatialResourceChangeStatus.InvalidLocation, contract.Withdraw(new TileCoord(-1, 0), amount).Status, layer.Id + " negative coordinate refuses");
            c.Equal(SpatialResourceChangeStatus.InvalidLocation, contract.Withdraw(new TileCoord(18, 16), amount).Status, layer.Id + " beyond authored extent refuses");
            c.Equal(hash, game.Hash(), layer.Id + " every refusal retains values/hash");
            c.Equal(version, layer.Version, layer.Id + " every refusal retains layer cursor");
            c.Equal(mapVersion, layer.Map.Version, layer.Id + " every refusal retains map cursor");
            SpatialResourceChange committed = contract.Withdraw(tile, amount);
            c.That(committed.Committed && !committed.Ready, layer.Id + " actual commit has typed outcome");
            c.Equal(nativeBefore - amount, committed.After, layer.Id + " exact native remainder");
            c.Equal(remainder, layer[tile], layer.Id + " existing typed store contains exact remainder");
            c.Equal(version + 1, layer.Version, layer.Id + " one actual write advances one layer cursor");
            c.That(layer.Map.VersionFor(new PlayerId(0)) > ownerCursor, layer.Id + " authorized data invalidates");
            c.Equal(layer.Visibility == SpatialLayerVisibility.Public, layer.Map.VersionFor(new PlayerId(1)) > otherCursor, layer.Id + " private depletion never invalidates another company");
            hash = game.Hash();
            FieldInfo cursor = typeof(TileMap).GetField("_version", BindingFlags.Instance | BindingFlags.NonPublic);
            int actualCursor = layer.Map.Version; cursor.SetValue(layer.Map, int.MaxValue);
            c.Throws<OverflowException>(() => contract.Withdraw(tile, committed.After), layer.Id + " exhausted invalidation refuses before sparse debit");
            c.Equal(remainder, layer[tile], layer.Id + " counter refusal preserves actual material");
            c.Equal(hash, game.Hash(), layer.Id + " transient counter refusal preserves durable state"); cursor.SetValue(layer.Map, actualCursor);
            c.That(contract.Withdraw(tile, committed.After).Committed, layer.Id + " final exact depletion succeeds");
            c.Equal(default(T), layer[tile], layer.Id + " depletion removes actual cell value");
            c.Equal(0, BitConverter.ToInt32(Encode(layer), 0), layer.Id + " empty sparse chunk is removed by the existing owner");
            c.Equal(Fix.Zero, contract.ReadQuantity(tile).Amount, layer.Id + " depleted native read is actual empty stock");
            c.Throws<ArgumentOutOfRangeException>(() => contract.ReadQuantity(new TileCoord(-1, 0)), layer.Id + " invalid native read cannot fabricate zero");
        }

        private static void DepletionPersistence(Check c)
        {
            GameFactory factory = Points(); SimGame game = NewPoint(factory); var space = (PointSpace)game.World.Space;
            PointLayer deposit = Deposit(game); space.AssignOwner(game.World, deposit.Id, new PlayerId(0));
            Fix position = Fix.FromRatio(7, 2);
            c.That(space.WriteLayer(game.World, new PlayerId(0), deposit.Id, position, Fix.FromInt(8)).IsNone, "authored existing point samples use actual non-tile geometry");
            ISpatialResourceQuantity<Fix> contract = deposit;
            var own = game.Seat(0).View.Feature<PointLayerView>(); var other = game.Seat(1).View.Feature<PointLayerView>();
            ulong before = game.Hash(); int ownerCursor = own.Version, otherCursor = other.Version;
            c.Equal(SpatialResourceChangeStatus.InvalidLocation, contract.Withdraw(-Fix.One, Fix.One).Status, "point rejects negative actual coordinate");
            c.Equal(SpatialResourceChangeStatus.InvalidLocation, contract.Withdraw(Fix.FromInt(101), Fix.One).Status, "point rejects actual extent without tile limits");
            c.Equal(SpatialResourceChangeStatus.InvalidAmount, contract.Withdraw(position, Fix.FromRatio(1, 2)).Status, "independent provider shares native discrete admission");
            c.Equal(SpatialResourceChangeStatus.InsufficientQuantity, contract.Withdraw(position, Fix.FromInt(5)).Status, "independent provider cannot fabricate supply");
            c.Equal(before, game.Hash(), "public provider refusals retain actual saved owner/samples");
            c.Equal(ownerCursor, own.Version, "public refusal retains authorized cursor");
            c.Equal(otherCursor, other.Version, "public refusal retains foreign cursor");
            SpatialResourceChange preview = contract.CheckWithdrawal(position, Fix.One);
            c.That(preview.Ready && !preview.Committed, "point exposes pure typed primitive through public engine contracts");
            c.Equal(Fix.FromInt(4), preview.Before, "actual point sample halves convert to native supply");
            c.Equal(Fix.FromInt(4), contract.ReadQuantity(position).Amount, "point native stock read uses its actual sample owner");
            c.Equal(deposit.Resource.QuantityUnit.Unit, contract.ReadQuantity(position).Unit, "point native stock read identifies existing supply unit");
            c.Equal(before, game.Hash(), "point preview is pure");
            SpatialResourceChange moved = contract.Withdraw(position, Fix.One);
            c.That(moved.Committed, "public provider commits actual depletion");
            c.Equal(Fix.FromInt(6), moved.StoredAfter, "stored point sample stays in authored half units");
            c.Equal(Fix.FromInt(3), moved.After, "detached native remainder is exact");
            var values = new List<Fix>();
            c.Equal(SpatialLayerReadStatus.Success, own.ReadLayer(deposit.Id, new PointRegion(position, Fix.One, 1), values), "owner sees actual remaining material");
            c.Equal(Fix.FromInt(6), values[0], "view reports stored units rather than a copied native inventory");
            c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer(deposit.Id, new PointRegion(position, Fix.One, 1), values), "depleted private layer remains hidden");
            c.Equal(0, values.Count, "hidden query clears values");
            c.Equal(otherCursor, other.Version, "private actual debit does not expose foreign activity");
            c.That(own.Version > ownerCursor, "private owner sees actual invalidation");
            before = game.Hash();
            var cursor = typeof(PointSpace).GetField("_observerVersion", BindingFlags.Instance | BindingFlags.NonPublic);
            int actualCursor = (int)cursor.GetValue(space); cursor.SetValue(space, int.MaxValue);
            c.Throws<OverflowException>(() => contract.Withdraw(position, Fix.One), "point preflight refuses before sample deletion");
            c.Equal(before, game.Hash(), "public exhausted-cursor refusal retains durable stock"); cursor.SetValue(space, actualCursor);
            c.Equal(4, space.StateSchemaVersion, "point existing payload/schema stays authoritative");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial actual point depletion roundtrips");
            PointLayer loaded = Deposit(twin);
            for (int i = 0; i < 3; i++)
            {
                c.That(contract.Withdraw(position, Fix.One).Committed && loaded.Withdraw(position, Fix.One).Committed, "same actual saved remaining quantity is spendable");
                for (int tick = 0; tick < 3; tick++) { game.Update(); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "interleaved saved depletion continues deterministically"); }
            }
            c.Equal(SpatialResourceChangeStatus.InsufficientQuantity, contract.Withdraw(position, Fix.One).Status, "depleted actual point cannot be spent twice");
            c.Equal(Fix.Zero, contract.ReadQuantity(position).Amount, "depleted point native read is actual empty stock");
            c.Throws<ArgumentOutOfRangeException>(() => contract.ReadQuantity(-Fix.One), "invalid point native read cannot fabricate zero");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "empty sparse point remains durably empty");
            c.Equal(UnitConversionStatus.Converted, SpatialResourceValues.CheckValue(deposit, Fix.Zero), "empty material remains legal not replenished");

            // Native checkpoint continuation uses the same existing factory and Sync owner.
            GameFactory tiles = Tiles(); SimGame native = NewTile(tiles); var map = (TileMap)native.World.Space;
            var cell = new TileCoord(17, 16); var quantity = map.Layer<ByteLayer>("quantity.byte"); quantity.Set(cell, 8);
            c.That(quantity.Withdraw(cell, Fix.One).Committed, "native partial depletion commits");
            SimGame nativeTwin = tiles.Load(native.Save()); var loadedQuantity = ((TileMap)nativeTwin.World.Space).Layer<ByteLayer>(quantity.Id);
            c.Equal(native.Hash(), nativeTwin.Hash(), "native partial sparse depletion roundtrips");
            for (int i = 0; i < 3; i++)
            {
                c.That(quantity.Withdraw(cell, Fix.One).Committed && loadedQuantity.Withdraw(cell, Fix.One).Committed, "native actual saved remainder spends exactly");
                native.Update(); nativeTwin.Update(); c.Equal(native.Hash(), nativeTwin.Hash(), "native sessions retain independent identical depleted state");
            }
            c.Equal((byte)0, loadedQuantity[cell], "native loaded empty chunk reads actual zero");
            c.Equal(native.Hash(), tiles.Load(native.Save()).Hash(), "native depleted state remains resumable");
            // Poison the actual existing stores, never a test-side substitute for the engine's rules.
            quantity.Set(cell, 2);
            var chunks = (Dictionary<long, byte[]>)typeof(MapLayer<byte>).GetField("_chunks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(quantity);
            byte[] chunk = chunks[((long)1 << 32) | 1]; chunk[1] = 1;
            c.Throws<CorruptStateException>(() => quantity.CheckWithdrawal(cell, Fix.One), "native corrupt live material cannot become availability");
            c.Throws<CorruptStateException>(() => quantity.Withdraw(cell, Fix.One), "native corrupt live material cannot be spent");
            c.Throws<CorruptStateException>(() => quantity.ReadQuantity(cell), "native corrupt material read cannot fabricate stock");
            chunk[1] = 2;
            var samples = (SortedDictionary<long, Fix>)typeof(PointLayer).GetField("_samples", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(deposit);
            samples[position.Raw] = Fix.One;
            c.Throws<CorruptStateException>(() => contract.CheckWithdrawal(position, Fix.One), "point corrupt fractional native material cannot become availability");
            c.Throws<CorruptStateException>(() => contract.Withdraw(position, Fix.One), "point corrupt fractional native material cannot be spent");
            c.Throws<CorruptStateException>(() => contract.ReadQuantity(position), "point corrupt material read cannot fabricate stock");
            samples.Remove(position.Raw);
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "restored actual point store remains checkpointable after refused corruption");
            c.Throws<InvalidOperationException>(() => new PointLayer(game.World.Content.Get<ISpatialLayerDefinition>(deposit.Id), space).CheckWithdrawal(position, Fix.One), "public primitive rejects a layer not registered with its claimed owner");
        }
    }
}
