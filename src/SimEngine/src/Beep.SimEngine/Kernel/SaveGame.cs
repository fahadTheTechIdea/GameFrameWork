using System;
using System.IO;

namespace Beep.SimEngine.Kernel
{
    /// <summary>The engine's identity, for saves and the lockstep handshake.</summary>
    public static class EngineInfo
    {
        /// <summary>Bumped with any change to simulation code or state layout; saves and peers must match exactly.</summary>
        public const string Build = "beep-simengine-2.782.0";
        public const int SaveFormat = 166;
    }

    /// <summary>What a save's header says, readable without building the world (for load menus).</summary>
    public sealed class SaveHeader
    {
        public int Format;
        public string Build;
        public string Game;
        public ulong ContentFingerprint;
        public long Tick;
        public WorldSettings Settings;
    }

    /// <summary>
    /// The save file: magic, header, the world's own Sync, then a checksum over everything before it.
    /// No migration: a save from a different build is refused, by design.
    /// </summary>
    public static class SaveGame
    {
        private static readonly byte[] Magic = { (byte)'S', (byte)'I', (byte)'M', (byte)'S', (byte)'A', (byte)'V', (byte)'E', 2 };

        public static byte[] Write(World world, string game)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.Fault != null) throw new InvalidOperationException("a faulted partial tick cannot be saved as resumable state");
            // The cheap original callback precondition precedes even header/settings encoding;
            // World.Sync still owns the complete lifecycle/operation/codec boundary below.
            world.RequireCompletedProviderCalls();
            using (var ms = new MemoryStream())
            {
                ms.Write(Magic, 0, Magic.Length);
                var w = new StateWriter(ms);
                int format = EngineInfo.SaveFormat; w.Int(ref format);
                string build = EngineInfo.Build; w.String(ref build);
                w.String(ref game);
                ulong fp = world.Content.Fingerprint; w.ULong(ref fp);
                long tick = world.Tick; w.Long(ref tick);
                world.Settings.Sync(w);
                world.Sync(w);
                w.Flush();
                ulong checksum = Fnv(ms.GetBuffer(), 0, (int)ms.Length);
                w.ULong(ref checksum);
                w.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>Reads and verifies the header. Throws <see cref="CorruptStateException"/> on a bad file.</summary>
        public static SaveHeader ReadHeader(byte[] data, out StateReader bodyReader)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length < Magic.Length + 8) throw new CorruptStateException("not a save file");
            for (int i = 0; i < Magic.Length; i++)
                if (data[i] != Magic[i]) throw new CorruptStateException("not a save file of this engine");
            int bodyEnd = data.Length - 8;
            ulong stored = new StateReader(data, bodyEnd, 8).ReadULong();
            if (Fnv(data, 0, bodyEnd) != stored) throw new CorruptStateException("save checksum does not match (the file is damaged)");
            var r = new StateReader(data, Magic.Length, bodyEnd - Magic.Length);
            var h = new SaveHeader { Settings = new WorldSettings() };
            r.Int(ref h.Format);
            r.String(ref h.Build);
            if (h.Format != EngineInfo.SaveFormat || h.Build != EngineInfo.Build)
                throw new CorruptStateException("unsupported engine build/save format");
            r.String(ref h.Game);
            r.ULong(ref h.ContentFingerprint);
            r.Long(ref h.Tick);
            if (h.Tick < 0) throw new CorruptStateException("save tick must be nonnegative");
            h.Settings.Sync(r);
            bodyReader = r;
            return h;
        }

        public static SaveHeader Describe(byte[] data) => ReadHeader(data, out _);

        /// <summary>Checks the header against the running engine and content, then loads the body into <paramref name="world"/>.</summary>
        public static void ReadBody(World world, SaveHeader header, StateReader body)
        {
            if (header.Format != EngineInfo.SaveFormat || header.Build != EngineInfo.Build)
                throw new CorruptStateException("save is from engine " + header.Build + " (format " + header.Format + "); this is " + EngineInfo.Build);
            if (header.ContentFingerprint != world.Content.Fingerprint)
                throw new CorruptStateException("save was made with different content");
            world.Sync(body);
            if (header.Tick != world.Tick) throw new CorruptStateException("save header and world ticks disagree");
            if (!body.AtEnd) throw new CorruptStateException("save has trailing data");
        }

        private static ulong Fnv(byte[] data, int offset, int length)
        {
            ulong h = 14695981039346656037UL;
            unchecked { for (int i = offset; i < offset + length; i++) { h ^= data[i]; h *= 1099511628211UL; } }
            return h;
        }

        private static ulong ReadULong(this StateReader r)
        {
            ulong v = 0;
            r.ULong(ref v);
            return v;
        }
    }
}
