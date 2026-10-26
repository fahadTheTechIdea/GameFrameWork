using System;
using System.Diagnostics;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    public static partial class SpatialLayerTests
    {
        // Primitive spatial codec validation, NOT a player-workflow simulator.
        // Only native value mutation and codecs are under test; no stock/travel/work is injected.
        private static void NonemptyCodec(Check c)
        {
            var definitions=Json.Parse(Definitions);
            definitions.Get("tile_spaces")[0].Set("width",JsonValue.Number("18"));
            definitions.Get("tile_spaces")[0].Set("height",JsonValue.Number("17"));
            definitions.Get("units").Add(Json.Parse("{\"id\":\"unit.large_mass\",\"dimension\":\"mass\",\"scale\":{\"numerator\":1000000000,\"denominator\":1},\"discrete\":false}"));
            definitions.Get("spatial_layers").Add(Json.Parse("{\"id\":\"byte.quantity\",\"space\":\"layers.tiles\",\"type\":\"resource.quantity\",\"value_type\":\"byte\",\"visibility\":\"public\",\"unit\":\"unit.large_mass\",\"resource\":\"resource.ore\",\"resource_measure\":\"quantity\"}"));
            var factory=Factory(definitions.ToString());var game=New(factory);var map=(TileMap)game.World.Space;
            var layer=map.Layer<ByteLayer>("terrain");
            ulong Hash(MapLayer value,bool observation=false){var hash=new StateHasher(observation);value.Sync(hash);return hash.Value;}
            ulong empty=Hash(layer);c.Equal(8,Encode(layer).Length,"empty layer retains only its two native section counts");
            for(int position=0;position<256;position++)
            {
                var tile=new TileCoord(position%16,position/16);byte value=(byte)(position%255+1);
                layer.Set(tile,value);byte[] encoded=Encode(layer);
                c.Equal(1,new StateReader(encoded,0,encoded.Length).Count(0),"every nonzero byte position retains its native chunk/"+position);
                c.Equal(value,layer[tile],"native value survives occupancy validation/"+position);
                c.Equal(Hash(layer),Hash(layer,true),"normal and observation codecs validate identical current bytes/"+position);
                if(position==7||position==8||position==31||position==32||position==255)
                {
                    var twin=factory.Load(game.Save());
                    c.Equal(game.Hash(),twin.Hash(),"unaligned/word/final-byte chunk survives actual native save/"+position);
                }
                layer.Set(tile,0);
                c.Equal(8,Encode(layer).Length,"clearing the final occupied byte removes the actual chunk/"+position);
                c.Equal(empty,Hash(layer),"chunk reclamation preserves canonical empty state/"+position);
            }
            var last=new TileCoord(15,15);
            for(int value=1;value<=255;value++)
            {
                layer.Set(last,(byte)value);
                c.Equal(Hash(layer),Hash(layer,true),"all nonzero byte values at the last position remain observable/"+value);
                c.Equal((byte)value,layer[last],"last nonzero value is never normalized/"+value);
                layer.Set(last,0);c.Equal(8,Encode(layer).Length,"each last-byte value reclaims sparse storage/"+value);
            }
            layer.Set(new TileCoord(17,16),7);byte[] healthy=Encode(layer);
            foreach(int padding in new[]{2,16,255})
            {
                var bad=(byte[])healthy.Clone();bad[16+padding]=1;
                c.Throws<CorruptStateException>(()=>layer.Sync(new StateReader(bad,0,bad.Length)),"nonzero edge padding still refuses/"+padding);
                c.Equal(Convert.ToBase64String(healthy),Convert.ToBase64String(Encode(layer)),"failed edge restore publishes no prefix/"+padding);
            }
            var zero=(byte[])healthy.Clone();Array.Clear(zero,16,256);
            c.Throws<CorruptStateException>(()=>layer.Sync(new StateReader(zero,0,zero.Length)),"all-zero serialized sparse chunk still refuses");
            c.Equal(Convert.ToBase64String(healthy),Convert.ToBase64String(Encode(layer)),"empty candidate cannot replace committed values");

            var quantity=map.Layer<ByteLayer>("byte.quantity");quantity.Set(new TileCoord(0,0),1);
            c.That(!quantity.CheckSample(last,Fix.FromInt(3)).IsNone,"quantity conversion overflow remains an actual per-value refusal");
            byte[] validQuantity=Encode(quantity),invalidQuantity=(byte[])validQuantity.Clone();invalidQuantity[16+255]=3;
            c.Throws<CorruptStateException>(()=>quantity.Sync(new StateReader(invalidQuantity,0,invalidQuantity.Length)),"later nonzero material value is validated even after an earlier valid value");
            c.Equal(Convert.ToBase64String(validQuantity),Convert.ToBase64String(Encode(quantity)),"material validation refuses before publishing candidate stock");

            // Existing map/layer owners, late occupied cells and full current codecs.
            layer.Set(new TileCoord(17,16),0);
            foreach(int position in new[]{0,127,255})
            {
                var tile=new TileCoord(position%16,position/16);layer.Set(tile,1);
                var warming=Stopwatch.StartNew();do{_=Hash(layer,true);}while(warming.ElapsedMilliseconds<300);
                const int rounds=4096;ulong expected=Hash(layer,true),observed=0;
                long allocated=GC.GetAllocatedBytesForCurrentThread(),started=Stopwatch.GetTimestamp();
                for(int n=0;n<rounds;n++)observed=Hash(layer,true);
                long ticks=Stopwatch.GetTimestamp()-started,bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
                c.Equal(expected,observed,"measured original codec hashes every current value/"+position);
                Console.WriteLine(FormattableString.Invariant($"nonempty codec;build={EngineInfo.Build};position={position};calls={rounds};elapsed-ms={(double)ticks*1000/Stopwatch.Frequency:F3};allocated={bytes}"));
                layer.Set(tile,0);
            }
            c.That(game.Fault==null,"primitive validation leaves its session healthy");
        }
    }
}
