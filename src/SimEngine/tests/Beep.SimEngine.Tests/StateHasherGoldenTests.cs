using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    internal static class StateHasherGoldenTests
    {
        internal const string Case = "State hashing: canonical typed fingerprint official payload vectors and every codec";
        internal const string BytesCase = "State hashing: field boundaries mutations and independent owners";
        internal const string ShapesCase = "State hashing: byte codec shapes canonical stream and allocation costs";
        internal static void Register()
        { TestRunner.Add(Case, Run); TestRunner.Add(BytesCase, Streams); TestRunner.Add(ShapesCase, CodecShapes); }

        // Independent one-shot oracle: manual byte assembly and lane-array iteration,
        // unlike the production span-based payload owner. Official external vectors
        // below certify this reference too; no engine hash helper computes expectations.
        private static ulong Reference(byte[] bytes)
        {
            const ulong p1=0x9E3779B185EBCA87UL,p2=0xC2B2AE3D27D4EB4FUL,p3=0x165667B19E3779F9UL,
                p4=0x85EBCA77C2B2AE63UL,p5=0x27D4EB2F165667C5UL;
            ulong Read(int at,int count) { ulong value=0;for(int n=0;n<count;n++)value|=(ulong)bytes[at+n]<<(8*n);return value; }
            ulong Rot(ulong value,int count)=>(value<<count)|(value>>(64-count));
            unchecked
            {
                ulong Fold(ulong value,ulong input)=>Rot(value+input*p2,31)*p1;
                int offset=0;ulong hash=p5;
                if(bytes.Length>=32)
                {
                    var lanes=new[]{p1+p2,p2,0UL,0UL-p1};
                    do { for(int lane=0;lane<4;lane++)lanes[lane]=Fold(lanes[lane],Read(offset+lane*8,8));offset+=32; }
                    while(offset<=bytes.Length-32);
                    hash=Rot(lanes[0],1)+Rot(lanes[1],7)+Rot(lanes[2],12)+Rot(lanes[3],18);
                    foreach(ulong lane in lanes)hash=(hash^Fold(0,lane))*p1+p4;
                }
                hash+=(ulong)bytes.Length;
                while(offset<=bytes.Length-8){hash=Rot(hash^Fold(0,Read(offset,8)),27)*p1+p4;offset+=8;}
                if(offset<=bytes.Length-4){hash=Rot(hash^Read(offset,4)*p1,23)*p2+p3;offset+=4;}
                while(offset<bytes.Length)hash=Rot(hash^bytes[offset++]*p5,11)*p1;
                hash^=hash>>33;hash*=p2;hash^=hash>>29;hash*=p3;return hash^(hash>>32);
            }
        }

        private static ulong Fingerprint(byte[] bytes)
        {
            ulong hash=14695981039346656037UL;
            unchecked { foreach(byte value in bytes)hash=(hash^value)*1099511628211UL; }
            return hash;
        }
        // Test-only typed fingerprint recorder. Every raw payload first passes the
        // independently certified XXH64 oracle; no World or gameplay rules here.
        private sealed class Canonical : IStateSync, IDisposable
        {
            private readonly MemoryStream _bytes=new MemoryStream();
            private readonly BinaryWriter _writer;
            internal Canonical(){_writer=new BinaryWriter(_bytes,Encoding.UTF8,true);}
            internal byte[] BytesNow(){_writer.Flush();return _bytes.ToArray();}
            internal ulong Digest()=>Fingerprint(BytesNow());
            public SyncMode Mode=>SyncMode.Hash;
            public bool Reading=>false;
            public void Int(ref int v)=>_writer.Write((long)v);
            public void Long(ref long v)=>_writer.Write(v);
            public void ULong(ref ulong v)=>_writer.Write(v);
            public void Bool(ref bool v)=>_writer.Write(v?1L:0L);
            public void Byte(ref byte v)=>_writer.Write((long)v);
            public void Fix(ref Fix v)=>_writer.Write(v.Raw);
            public void String(ref string v){_writer.Write(v==null?-1L:v.Length);if(v!=null)foreach(char ch in v)_writer.Write((long)ch);}
            public void Entity(ref EntityId v)=>_writer.Write((long)v.Value);
            public void Player(ref PlayerId v)=>_writer.Write((long)v.Value);
            public void Resource(ref ResourceId v)=>_writer.Write((long)v.Value);
            public void Tile(ref TileCoord v){_writer.Write((long)v.X);_writer.Write((long)v.Y);}
            public void Vec(ref FixVec2 v){_writer.Write(v.X.Raw);_writer.Write(v.Y.Raw);}
            public void Bytes(byte[] data){_writer.Write((long)data.Length);_writer.Write(Reference(data));}
            public int Count(int count){if(count<0)throw new CorruptStateException("negative test count");_writer.Write((long)count);return count;}
            public void Section(string name){String(ref name);}
            public void Dispose(){_writer.Dispose();_bytes.Dispose();}
        }
        private static void Compare(Check c,StateHasher actual,Canonical expected,string label)
        {
            ulong before=actual.Value;
            c.Equal(expected.Digest(),before,label);
            c.Equal(before,actual.Value,"digest read never finalizes/resets/mutates/"+label);
        }
        private static byte[] Payload(int length,int pattern)
        {
            var data=new byte[length];
            for(int i=0;i<length;i++)data[i]=pattern==0?(byte)((i*73+i/11)&255):pattern==1?(byte)(i%16<6+(i%256)/64?2:0):
                pattern==2?(byte)3:(byte)(i&255);
            return data;
        }
        private static void AllCodecs(IStateSync state)
        {
            int i=int.MinValue;long n=long.MinValue;ulong u=ulong.MaxValue;bool yes=true,no=false;byte b=255;
            Fix f=Fix.FromRatio(-17,3);string text="a\0Ω🚀",empty="",missing=null;
            var entity=new EntityId(37);var player=new PlayerId(-1);var resource=new ResourceId(3);
            var tile=new TileCoord(-11,33);var point=new FixVec2(Fix.Half,-Fix.One);
            state.Section("canonical.κ");state.Int(ref i);state.Long(ref n);state.ULong(ref u);
            state.Bool(ref yes);state.Bool(ref no);state.Byte(ref b);state.Fix(ref f);
            state.String(ref text);state.String(ref missing);state.String(ref empty);
            state.Entity(ref entity);state.Player(ref player);state.Resource(ref resource);state.Tile(ref tile);state.Vec(ref point);
            state.Count(0);state.Count(3);state.Bytes(new byte[]{0,1,3,255,0});state.Long(ref n);
        }
        private static void ScalarVectors(Check c)
        {
            // Independently calculated byte-sequence expectations, not read back from the engine.
            long[] inputs = { 0, 1, -1, int.MinValue, int.MaxValue, long.MinValue, long.MaxValue, 0x0123456789abcdef };
            ulong[] singles = { 12161962213042174405UL, 9929646806074584996UL, 10157053723145373757UL,
                10154101631709266849UL, 17818800096642449761UL, 12161821475553763397UL, 10157194460633784765UL, 4029383781087845461UL };
            ulong[] prefixes = { 12161962213042174405UL, 7576559462502111812UL, 14522085584902735164UL,
                5899596053217119128UL, 5409026947373731892UL, 16456276617425582132UL, 2329854737031942316UL, 13922929949929852300UL };
            var sequence = new StateHasher();
            c.Equal(14695981039346656037UL, sequence.Value, "canonical FNV offset");
            for (int i = 0; i < inputs.Length; i++)
            {
                long value = inputs[i]; var single = new StateHasher(); single.Long(ref value); sequence.Long(ref value);
                c.Equal(singles[i], single.Value, "canonical signed eight-byte scalar " + i);
                c.Equal(prefixes[i], sequence.Value, "canonical accumulated scalar prefix " + i);
                c.Equal(inputs[i], value, "hash codec never changes scalar input " + i);
                if (value >= int.MinValue && value <= int.MaxValue) {
                    int integer = (int)value; var intHash = new StateHasher(); intHash.Int(ref integer);
                    c.Equal(singles[i], intHash.Value, "int hashing retains sign-extended eight-byte encoding " + i);
                }
            }
        }
        private static void Run(Check c)
        {
            ScalarVectors(c);
            c.Equal(0xEF46DB3751D8E999UL,Reference(Array.Empty<byte>()),"external known XXH64 empty vector");
            c.Equal(0x44BC2CF5AD770999UL,Reference(Encoding.ASCII.GetBytes("abc")),"external known XXH64 ASCII vector");
            c.Equal(14695981039346656037UL,new StateHasher().Value,"empty typed-state scalar mixer offset");
            using var stream=typeof(Program).Assembly.GetManifestResourceStream("tests.state-hash-vectors.json")
                ??throw new InvalidOperationException("missing independent state hash vectors");
            using var text=new StreamReader(stream);
            var root=new JsonReader(Json.Parse(text.ReadToEnd()),"state-hash-vectors");
            c.Equal(1,root.Int("schema"),"explicit oracle schema");
            c.Equal("xxh64-payload-oracle-v1",root.String("algorithm"),"explicit independently certified payload algorithm");
            c.Equal("Microsoft System.IO.Hashing 10.0.1",root.String("oracle"),"independent pre-existing reference implementation");
            c.Equal("2679A6164163C1F4B7B5FADA83A55EB48B0A85840E04F00E8921B8D495A66B84",root.String("oracle_sha256"),"oracle assembly provenance");
            var rows=root.Array("vectors");root.End();c.Equal(312,rows.Count,"all independently generated golden vectors retained");
            for(int index=0;index<rows.Count;index++)
            {
                var row=new JsonReader(rows[index],"hash.vector["+index+"]");
                int length=row.Int("length"),pattern=row.Int("pattern");
                long prefix=long.Parse(row.String("prefix"),CultureInfo.InvariantCulture);
                ulong expected=ulong.Parse(row.String("hash"),NumberStyles.HexNumber,CultureInfo.InvariantCulture);row.End();
                var data=Payload(length,pattern);using var canonical=new Canonical();
                canonical.Long(ref prefix);canonical.Bytes(data);
                c.Equal(expected,Reference(data),"independent one-shot oracle agrees with official payload implementation/"+index);
                foreach(bool observation in new[]{false,true})
                {
                    var actual=new StateHasher(observation);actual.Long(ref prefix);actual.Bytes(data);
                    c.Equal(canonical.Digest(),actual.Value,"typed canonical prefix/length/payload agrees with official vector/"+observation+"/"+index);
                    long suffix=unchecked((long)0xFEDCBA9876543210UL);
                    using var extended=new Canonical();extended.Long(ref prefix);extended.Bytes(data);extended.Long(ref suffix);
                    actual.Long(ref suffix);Compare(c,actual,extended,"read then append after official vector/"+observation+"/"+index);
                }
            }
            foreach(bool observation in new[]{false,true})
            {
                var actual=new StateHasher(observation);using var expected=new Canonical();
                AllCodecs(actual);AllCodecs(expected);Compare(c,actual,expected,"all scalar/ID/coordinate/string/count/array encodings/"+observation);
                c.That(((IStateObservationSync)actual).IsObservation==observation,"normal/observation identity remains explicit");
                c.Throws<CorruptStateException>(()=>actual.Count(-1),"negative counts still refuse, never become a hidden length");
            }
        }
        private static void Streams(Check c)
        {
            for(int length=0;length<=96;length++)
            {
                var actual=new StateHasher();using var expected=new Canonical();long prefix=long.MinValue+length;
                actual.Long(ref prefix);expected.Long(ref prefix);
                for(int n=0;n<9;n++)
                {
                    var bytes=Payload((length+n*37)%97,n%4);
                    actual.Bytes(bytes);expected.Bytes(bytes);Compare(c,actual,expected,"unaligned stripe/tail before scalar/"+length+"/"+n);
                    long value=unchecked((long)0x0123456789ABCDEFUL+n);actual.Long(ref value);expected.Long(ref value);
                    Compare(c,actual,expected,"unaligned stripe/tail after scalar/"+length+"/"+n);
                }
            }
            foreach(byte value in new byte[]{0,1,3,255})
            {
                var data=new byte[256];Array.Fill(data,value);
                for(int bit=0;bit<2048;bit++)
                {
                    data[bit/8]^=(byte)(1<<(bit%8));
                    var actual=new StateHasher();using var expected=new Canonical();
                    actual.Bytes(data);expected.Bytes(data);Compare(c,actual,expected,"every current bit participates/"+value+"/"+bit);
                    data[bit/8]^=(byte)(1<<(bit%8));
                }
            }
            for(int value=0;value<256;value++)for(int length=0;length<=33;length++)
            {
                var bytes=new byte[length];Array.Fill(bytes,(byte)value);
                var actual=new StateHasher();using var expected=new Canonical();
                actual.Bytes(bytes);expected.Bytes(bytes);Compare(c,actual,expected,"all byte values and short boundaries/"+value+"/"+length);
                c.That(Array.TrueForAll(bytes,x=>x==(byte)value),"hashing never edits current input/"+value+"/"+length);
            }
            var hashes=new ulong[8];var answers=new ulong[8];
            Parallel.For(0,8,owner=>{
                var actual=new StateHasher(owner%2==0);using var expected=new Canonical();
                for(int round=0;round<64;round++)
                {
                    long value=((long)owner<<32)|(uint)round;var bytes=Payload(1027+round%3,(owner+round)%4);
                    actual.Long(ref value);actual.Bytes(bytes);expected.Long(ref value);expected.Bytes(bytes);
                    _=actual.Value;
                }
                hashes[owner]=actual.Value;answers[owner]=expected.Digest();
            });
            for(int owner=0;owner<8;owner++)c.Equal(answers[owner],hashes[owner],"independent concurrent streaming owners/"+owner);
        }
        private static byte[] Shape(string shape)
        {
            var data=new byte[256];
            for(int i=0;i<data.Length;i++)
            {
                int x=i%16,y=i/16;
                data[i]=shape=="zero"?(byte)0:shape=="uniform"?(byte)3:
                    shape=="water-edge"?(byte)(x<6+y/4?2:0):
                    shape=="terrain-edge"?(byte)(x<5+y/3?(x==0?y%5:3):0):
                    shape=="small-palette"?(byte)((i*3+i/11)&15):(byte)((i*73+i/11)&255);
            }
            return data;
        }
        private static void CodecShapes(Check c)
        {
            foreach(string shape in new[]{"zero","uniform","water-edge","terrain-edge","small-palette","noise"})
            {
                var data=Shape(shape);var warm=Stopwatch.StartNew();var warming=new StateHasher();
                do{warming.Bytes(data);}while(warm.ElapsedMilliseconds<500);
                var actual=new StateHasher();var timer=new Stopwatch();const int rounds=65536;
                long allocated=GC.GetAllocatedBytesForCurrentThread();timer.Start();
                for(int n=0;n<rounds;n++)actual.Bytes(data);
                long elapsed=timer.ElapsedTicks,bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
                using var expected=new Canonical();for(int n=0;n<rounds;n++)expected.Bytes(data);
                Compare(c,actual,expected,"complete canonical16MiB byte shape/"+shape);
                c.Equal(0L,bytes,"warmed shape append allocates no per-call state/"+shape);
                Console.WriteLine(FormattableString.Invariant($"codec shape16MiB;shape={shape};engine-ms={(double)elapsed*1000/Stopwatch.Frequency:F3};allocated={bytes}"));
            }
        }
    }
}
