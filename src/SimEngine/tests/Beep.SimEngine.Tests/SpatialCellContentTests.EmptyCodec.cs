using System;
using System.IO;
using System.Linq;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    internal static partial class SpatialCellContentTests
    {
        private static void EmptyBindingCodec(Check c)
        {
            var factory=LayeredNavigationTests.Factory();var game=LiveSensorTests.New(factory);
            var layer=((ISpatialLayerProvider)game.World.Space).SpatialLayers.Single(item=>item.Id=="route.surface");
            // Isolated empty codec unit, never installed as an alternate live world index/book.
            var bindings=new SpatialResourceCellBindings(layer);var reads=new int[1];ulong before=game.Hash();
            Func<int[],long,Fix> quantity=(context,key)=>{context[0]++;throw new InvalidOperationException("empty bindings must not invoke a quantity sampler");};
            byte[] encoded;
            using(var stream=new MemoryStream())
            {
                var writer=new StateWriter(stream);
                c.That(bindings.StageSync(writer,reads,quantity)==null,"empty writer supplies no restore action");writer.Flush();encoded=stream.ToArray();
            }
            c.That(encoded.SequenceEqual(new byte[4]),"actual empty binding count remains exactly one zero Int32");
            var hash=new StateHasher();c.That(bindings.StageSync(hash,reads,quantity)==null,"empty hash supplies no restore action");
            var reference=new StateHasher();int zero=0;reference.Int(ref zero);
            c.Equal(reference.Value,hash.Value,"empty binding count contributes exact original canonical hash bytes");
            var reader=new StateReader(encoded,0,encoded.Length);var publish=bindings.StageSync(reader,reads,quantity);
            c.That(publish!=null&&reader.AtEnd,"empty reader stages a complete non-null zero-book publication");publish();
            c.Equal(0,reads[0],"no nonexistent cell callback is fabricated for writing, hashing or reading");
            foreach(int invalid in new[]{1,-1})
                c.Throws<CorruptStateException>(()=>bindings.StageSync(new StateReader(BitConverter.GetBytes(invalid),0,4),reads,quantity),
                    "encoded binding count is validated before the empty shortcut: "+invalid);
            c.Equal(0,reads[0],"malformed count cannot invoke quantity lookup or publish a partial book");
            c.Equal(before,game.Hash(),"isolated codec checks change no original game state");
            c.Equal(before,factory.Load(game.Save()).Hash(),"actual native zero-binding layers retain original saved continuation");
        }
    }
}
