using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Beep.SimEngine.Kernel
{
    public enum SyncMode : byte { Write, Read, Hash }

    /// <summary>Engine-owned purity fingerprint of actual current state, never authority for an in-flight checkpoint.</summary>
    internal interface IStateObservationSync
    {
        bool IsObservation { get; }
    }

    /// <summary>
    /// One pass over a piece of state that is either saving it, loading it or hashing it. Everything
    /// stateful implements a single <c>Sync(IStateSync)</c>, so save, load and hash can never disagree
    /// about which fields exist or in what order (Unreal's Serialize(Archive&amp;) pattern).
    /// </summary>
    public interface IStateSync
    {
        SyncMode Mode { get; }
        bool Reading { get; }
        void Int(ref int v);
        void Long(ref long v);
        void ULong(ref ulong v);
        void Bool(ref bool v);
        void Byte(ref byte v);
        void Fix(ref Fix v);
        void String(ref string v);
        void Entity(ref EntityId v);
        void Player(ref PlayerId v);
        void Resource(ref ResourceId v);
        void Tile(ref TileCoord v);
        void Vec(ref FixVec2 v);
        void Bytes(byte[] buffer);

        /// <summary>
        /// Opens a variable-length run: writes and hashes <paramref name="count"/>, or returns the count
        /// read. Counts are nonnegative signed 32-bit values, without a gameplay-volume ceiling.
        /// Every list is synced as Count then its elements, so runs never collide in the hash.
        /// </summary>
        int Count(int count);

        /// <summary>A section marker: checked on load (a mismatch names the section), folded into the hash.</summary>
        void Section(string name);
    }

    public static class StateSyncExtensions
    {
        internal static int NonnegativeCount(int count)
        {
            if (count < 0) throw new CorruptStateException("bad count " + count);
            return count;
        }

        public static void Work(this IStateSync s, ref WorkProgress progress)
        {
            long whole = progress.WholeTicks; int fraction = unchecked((int)progress.Fraction);
            s.Long(ref whole); s.Int(ref fraction);
            if (whole < 0) throw new CorruptStateException("elapsed work must be nonnegative");
            if (s.Reading) progress = new WorkProgress(whole, unchecked((uint)fraction));
        }

        public static void Enum<T>(this IStateSync s, ref T v) where T : struct, Enum
        {
            int i = Convert.ToInt32(v);
            s.Int(ref i);
            if (s.Reading) v = (T)System.Enum.ToObject(typeof(T), i);
        }

        /// <summary>Syncs a list of structs with a per-element sync.</summary>
        public static void List<T>(this IStateSync s, List<T> list, SyncElement<T> element) where T : new()
        {
            int n = s.Count(list.Count);
            if (s.Reading)
            {
                list.Clear();
                for (int i = 0; i < n; i++) { T resource = new T(); element(s, ref resource); list.Add(resource); }
            }
            else
            {
                for (int i = 0; i < n; i++) { T resource = list[i]; element(s, ref resource); }
            }
        }

        public static void Ints(this IStateSync s, List<int> list)
        {
            int n = s.Count(list.Count);
            if (s.Reading) { list.Clear(); for (int i = 0; i < n; i++) { int v = 0; s.Int(ref v); list.Add(v); } }
            else for (int i = 0; i < n; i++) { int v = list[i]; s.Int(ref v); }
        }

        public static void Entities(this IStateSync s, List<EntityId> list)
        {
            int n = s.Count(list.Count);
            if (s.Reading) { list.Clear(); for (int i = 0; i < n; i++) { EntityId v = default; s.Entity(ref v); list.Add(v); } }
            else for (int i = 0; i < n; i++) { EntityId v = list[i]; s.Entity(ref v); }
        }
    }

    public delegate void SyncElement<T>(IStateSync s, ref T value);

    /// <summary>Writes state to a byte stream (little-endian, length-prefixed strings).</summary>
    public sealed class StateWriter : IStateSync
    {
        private readonly BinaryWriter _w;
        public StateWriter(Stream stream) { _w = new BinaryWriter(stream, Encoding.UTF8, true); }
        public SyncMode Mode => SyncMode.Write;
        public bool Reading => false;
        public void Int(ref int v) => _w.Write(v);
        public void Long(ref long v) => _w.Write(v);
        public void ULong(ref ulong v) => _w.Write(v);
        public void Bool(ref bool v) => _w.Write(v);
        public void Byte(ref byte v) => _w.Write(v);
        public void Fix(ref Fix v) => _w.Write(v.Raw);
        public void String(ref string v)
        {
            if (v == null) { _w.Write(-1); return; }
            byte[] b = Encoding.UTF8.GetBytes(v);
            _w.Write(b.Length);
            _w.Write(b);
        }
        public void Entity(ref EntityId v) => _w.Write(v.Value);
        public void Player(ref PlayerId v) => _w.Write(v.Value);
        public void Resource(ref ResourceId v) => _w.Write(v.Value);
        public void Tile(ref TileCoord v) { _w.Write(v.X); _w.Write(v.Y); }
        public void Vec(ref FixVec2 v) { _w.Write(v.X.Raw); _w.Write(v.Y.Raw); }
        public void Bytes(byte[] buffer) { _w.Write(buffer.Length); _w.Write(buffer); }
        public int Count(int count) { StateSyncExtensions.NonnegativeCount(count); _w.Write(count); return count; }
        public void Section(string name) { string n = name; String(ref n); }
        public void Flush() => _w.Flush();
    }

    /// <summary>Reads state written by <see cref="StateWriter"/>. Every read is bounds-checked.</summary>
    public sealed class StateReader : IStateSync
    {
        private readonly byte[] _data;
        private int _pos;
        private readonly int _end;

        public StateReader(byte[] data, int offset, int length)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            if (offset < 0 || length < 0 || offset > data.Length || length > data.Length - offset) throw new ArgumentOutOfRangeException(nameof(length));
            _pos = offset;
            _end = offset + length;
        }

        public SyncMode Mode => SyncMode.Read;
        public bool Reading => true;
        public int Position => _pos;
        public bool AtEnd => _pos == _end;

        private void Need(int n) { if (n < 0 || n > _end - _pos) throw new CorruptStateException("state ends early at byte " + _pos); }
        // Explicit little-endian decode: BinaryWriter always writes little-endian, whatever the machine.
        private int I32()
        {
            Need(4);
            int v = _data[_pos] | _data[_pos + 1] << 8 | _data[_pos + 2] << 16 | _data[_pos + 3] << 24;
            _pos += 4;
            return v;
        }

        private long I64()
        {
            long lo = (uint)I32();
            long hi = (uint)I32();
            return lo | hi << 32;
        }

        public void Int(ref int v) => v = I32();
        public void Long(ref long v) => v = I64();
        public void ULong(ref ulong v) => v = (ulong)I64();
        public void Bool(ref bool v) { Need(1); byte b = _data[_pos++]; if (b > 1) throw new CorruptStateException("bad bool"); v = b == 1; }
        public void Byte(ref byte v) { Need(1); v = _data[_pos++]; }
        public void Fix(ref Fix v) => v = Beep.SimEngine.Fix.FromRaw(I64());
        public void String(ref string v)
        {
            int len = I32();
            if (len == -1) { v = null; return; }
            if (len < 0) throw new CorruptStateException("bad string length " + len);
            Need(len);
            v = Encoding.UTF8.GetString(_data, _pos, len);
            _pos += len;
        }
        public void Entity(ref EntityId v) => v = new EntityId(I32());
        public void Player(ref PlayerId v) => v = new PlayerId(I32());
        public void Resource(ref ResourceId v) => v = new ResourceId(I32());
        public void Tile(ref TileCoord v) { int x = I32(); int y = I32(); v = new TileCoord(x, y); }
        public void Vec(ref FixVec2 v) { long x = I64(); long y = I64(); v = new FixVec2(Beep.SimEngine.Fix.FromRaw(x), Beep.SimEngine.Fix.FromRaw(y)); }
        public void Bytes(byte[] buffer)
        {
            int len = I32();
            if (len != buffer.Length) throw new CorruptStateException("byte block is " + len + ", expected " + buffer.Length);
            Need(len);
            Buffer.BlockCopy(_data, _pos, buffer, 0, len);
            _pos += len;
        }
        public int Count(int count)
        {
            return StateSyncExtensions.NonnegativeCount(I32());
        }
        public void Section(string name)
        {
            string read = null;
            String(ref read);
            if (!string.Equals(read, name, StringComparison.Ordinal))
                throw new CorruptStateException("expected section '" + name + "' but found '" + read + "'");
        }
    }

    /// <summary>Deterministic typed-state fingerprint. Not a cryptographic security boundary.</summary>
    public sealed class StateHasher : IStateSync, IStateObservationSync
    {
        // The sole fingerprint owner: canonical eight-byte scalar mixing, plus a fresh
        // XXH64 digest of EVERY byte-array payload. No pattern tables or cached world state.
        private const ulong ScalarPrime = 1099511628211UL, ScalarWord = 2232315406967589409UL;
        private const ulong P1 = 0x9E3779B185EBCA87UL, P2 = 0xC2B2AE3D27D4EB4FUL,
            P3 = 0x165667B19E3779F9UL, P4 = 0x85EBCA77C2B2AE63UL, P5 = 0x27D4EB2F165667C5UL;
        private ulong _h = 14695981039346656037UL;
        private readonly bool _observation;
        public StateHasher() { }
        internal StateHasher(bool observation) { _observation = observation; }
        bool IStateObservationSync.IsObservation => _observation;
        public SyncMode Mode => SyncMode.Hash;
        public bool Reading => false;
        public ulong Value => _h;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static ulong Rotate(ulong value, int count) => (value << count) | (value >> (64 - count));
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static ulong Round(ulong accumulator, ulong lane)
        { unchecked { return Rotate(accumulator + lane * P2, 31) * P1; } }
        private static ulong Merge(ulong hash, ulong accumulator)
        { unchecked { return (hash ^ Round(0, accumulator)) * P1 + P4; } }

        // Seed-zero XXH64, specified by xxHash v0.8.3. Explicit little-endian loads,
        // unsigned modular arithmetic, local lanes, all stripes and all tail bytes.
        private static ulong Payload(ReadOnlySpan<byte> input)
        {
            unchecked
            {
                int offset = 0;
                ulong hash = P5;
                if (input.Length >= 32)
                {
                    ulong a = P1 + P2, b = P2, c = 0, d = 0UL - P1;
                    do
                    {
                        a = Round(a, BinaryPrimitives.ReadUInt64LittleEndian(input.Slice(offset)));
                        b = Round(b, BinaryPrimitives.ReadUInt64LittleEndian(input.Slice(offset + 8)));
                        c = Round(c, BinaryPrimitives.ReadUInt64LittleEndian(input.Slice(offset + 16)));
                        d = Round(d, BinaryPrimitives.ReadUInt64LittleEndian(input.Slice(offset + 24)));
                        offset += 32;
                    } while (offset <= input.Length - 32);
                    hash = Rotate(a, 1) + Rotate(b, 7) + Rotate(c, 12) + Rotate(d, 18);
                    hash = Merge(Merge(Merge(Merge(hash, a), b), c), d);
                }
                hash += (ulong)input.Length;
                for (; offset <= input.Length - 8; offset += 8)
                    hash = Rotate(hash ^ Round(0, BinaryPrimitives.ReadUInt64LittleEndian(input.Slice(offset))), 27) * P1 + P4;
                if (offset <= input.Length - 4)
                {
                    hash = Rotate(hash ^ (ulong)BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(offset)) * P1, 23) * P2 + P3;
                    offset += 4;
                }
                for (; offset < input.Length; offset++) hash = Rotate(hash ^ input[offset] * P5, 11) * P1;
                hash ^= hash >> 33; hash *= P2; hash ^= hash >> 29; hash *= P3; hash ^= hash >> 32;
                return hash;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void Add(long value)
        {
            unchecked
            {
                if ((ulong)value <= byte.MaxValue) { _h = (_h ^ (byte)value) * ScalarWord; return; }
                _h = (_h ^ (byte)value) * ScalarPrime;
                _h = (_h ^ (byte)(value >> 8)) * ScalarPrime;
                _h = (_h ^ (byte)(value >> 16)) * ScalarPrime;
                _h = (_h ^ (byte)(value >> 24)) * ScalarPrime;
                _h = (_h ^ (byte)(value >> 32)) * ScalarPrime;
                _h = (_h ^ (byte)(value >> 40)) * ScalarPrime;
                _h = (_h ^ (byte)(value >> 48)) * ScalarPrime;
                _h = (_h ^ (byte)(value >> 56)) * ScalarPrime;
            }
        }
        public void Int(ref int value) => Add(value);
        public void Long(ref long value) => Add(value);
        public void ULong(ref ulong value) => Add(unchecked((long)value));
        public void Bool(ref bool value) => Add(value ? 1 : 0);
        public void Byte(ref byte value) => Add(value);
        public void Fix(ref Fix value) => Add(value.Raw);
        public void String(ref string value)
        {
            if (value == null) { Add(-1); return; }
            Add(value.Length);
            for (int i = 0; i < value.Length; i++) Add(value[i]);
        }
        public void Entity(ref EntityId value) => Add(value.Value);
        public void Player(ref PlayerId value) => Add(value.Value);
        public void Resource(ref ResourceId value) => Add(value.Value);
        public void Tile(ref TileCoord value) { Add(value.X); Add(value.Y); }
        public void Vec(ref FixVec2 value) { Add(value.X.Raw); Add(value.Y.Raw); }
        public void Bytes(byte[] buffer)
        {
            Add(buffer.Length);
            Add(unchecked((long)Payload(buffer)));
        }
        public int Count(int count) { StateSyncExtensions.NonnegativeCount(count); Add(count); return count; }
        public void Section(string name) { string value = name; String(ref value); }
    }

    public sealed class CorruptStateException : Exception
    {
        public CorruptStateException(string message) : base(message) { }
    }
}
