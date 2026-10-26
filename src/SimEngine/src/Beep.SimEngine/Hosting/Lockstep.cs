using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Hosting
{
    /// <summary>How this machine takes part in a lockstep game.</summary>
    public sealed class LockstepOptions
    {
        /// <summary>Original human-owned session seats this machine plays; ownership survives autopilot handover.</summary>
        public List<int> LocalSeats = new List<int>();
        /// <summary>Nonnegative game updates between input and application; an optional authored policy bounds buffering.</summary>
        public int InputDelay = 4;
        /// <summary>Hash comparison interval; 0 disables comparisons. Negative intervals are invalid.</summary>
        public int ChecksumEvery = 60;
    }

    public enum NetState : byte
    {
        /// <summary>Every input for the next tick is here; the game runs.</summary>
        Running = 0,
        /// <summary>Waiting for a peer's input for the next tick; the game holds (and says so).</summary>
        Stalled = 1,
        /// <summary>A peer's state hash differs from ours: the games have diverged.</summary>
        Desynced = 2,
        /// <summary>A peer sent something that is not a message of this game.</summary>
        Broken = 3
    }

    /// <summary>
    /// Deterministic lockstep for one game (the Age of Empires / Factorio model). Every peer runs the whole
    /// simulation; only players' orders travel. Each human seat is played on exactly one machine: that
    /// machine seals the seat's orders for tick T + delay into a frame and sends it; a tick runs only when
    /// every originally human-owned seat's frame for it has arrived, including during temporary autopilot.
    /// Seats authored as autonomous at session creation need no input (every peer
    /// computes them). Peers exchange state hashes to catch a divergence early. The host only moves bytes:
    /// <see cref="DrainOutgoing"/> to send, <see cref="Receive"/> for what arrived — any transport will do.
    /// </summary>
    public sealed class NetSession
    {
        private const byte MsgFrame = 1, MsgChecksum = 2, MsgHello = 3, MsgResume = 4;
        private const byte ControlNone = 0, ControlPause = 1, ControlSpeed = 3;

        private readonly SimGame _game;
        private readonly int _inputDelay;
        private readonly int _checksumEvery;
        private readonly bool[] _local;
        private readonly Dictionary<(long Tick, int Seat), Frame> _frames = new Dictionary<(long Tick, int Seat), Frame>();
        private readonly List<Command>[] _pending;
        private readonly object _pendingGate = new object();
        private readonly int[] _nextSequence;
        private readonly List<byte[]> _outbox = new List<byte[]>();
        private readonly Dictionary<long, ulong> _localHashes = new Dictionary<long, ulong>();
        private readonly Dictionary<long, ulong> _remoteHashes = new Dictionary<long, ulong>();
        private byte _control;
        private int _controlSpeed;
        private long _sealedThrough;
        private readonly LockstepPolicySettings _policy;
        public long? MaximumAhead => _policy.MaximumAhead;
        public long? MaximumCommandsPerFrame => _policy.MaximumCommandsPerFrame;

        private sealed class Frame
        {
            public readonly List<Command> Commands = new List<Command>();
            public byte Control;
            public int Speed;
        }

        internal NetSession(SimGame game, LockstepOptions options)
        {
            _game = game;
            game.World.Services.TryGet<ILockstepPolicy>(out var authored);
            _policy = authored == null ? default : LockstepPolicyContracts.Read(game.World, authored);
            if (options.InputDelay < 0 || _policy.MaximumAhead.HasValue && options.InputDelay > _policy.MaximumAhead.Value)
                throw new ArgumentOutOfRangeException(nameof(options), "input delay must be nonnegative and within the selected authored buffering policy");
            if (options.ChecksumEvery < 0) throw new ArgumentOutOfRangeException(nameof(options), "checksum interval must be nonnegative");
            if (options.LocalSeats == null) throw new ArgumentException("local seats must be declared", nameof(options));
            _inputDelay = options.InputDelay;
            _checksumEvery = options.ChecksumEvery;
            int seats = game.SeatCount;
            _local = new bool[seats];
            _pending = new List<Command>[seats];
            _nextSequence = new int[seats];
            for (int i = 0; i < seats; i++)
            {
                _pending[i] = new List<Command>();
                // Continue the authoritative saved/admitted seat stream; proposals are not a new identity owner.
                _nextSequence[i] = game.World.LastCommandSequence(new PlayerId(i), CommandStreams.Player);
            }
            foreach (int s in options.LocalSeats)
            {
                if (s < 0 || s >= seats) throw new ArgumentOutOfRangeException(nameof(options), "seat " + s + " is not in this game");
                if (_local[s]) throw new ArgumentException("duplicate local seat " + s, nameof(options));
                if (!Needs(s)) throw new ArgumentException("a local transport seat must be human-owned in the original session setup", nameof(options));
                _local[s] = true;
            }
            // A game loaded at tick T has nothing to seal before T + delay: the frames for those ticks are empty.
            _sealedThrough = game.Tick - 1;
        }

        /// <summary>Transport ownership survives a human seat's temporary handover to its authored autopilot.</summary>
        private bool Needs(int seat) => _game.World.Settings.Seats[seat] == Controller.Human;

        public NetState State { get; private set; }
        /// <summary>The tick the game is waiting on while stalled.</summary>
        public long StalledOn { get; private set; } = -1;
        /// <summary>The tick whose hashes disagreed (for a desync report with the game's HashBreakdown).</summary>
        public long DesyncTick { get; private set; } = -1;
        public int InputDelay => _inputDelay;

        public bool IsLocal(PlayerId seat) => seat.IsValid && seat.Value < _local.Length && _local[seat.Value];

        // ------------------------------------------------------------------ joining

        /// <summary>What this machine is running: send it to a peer, who checks it with <see cref="CheckHello"/>.</summary>
        public byte[] Hello()
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(MsgHello);
                var w = new StateWriter(ms);
                string build = EngineInfo.Build; w.String(ref build);
                ulong fp = _game.World.Content.Fingerprint; w.ULong(ref fp);
                _game.World.Settings.Sync(w);
                int delay = _inputDelay; w.Int(ref delay);
                int checksums = _checksumEvery; w.Int(ref checksums);
                long tick = _game.Tick; w.Long(ref tick);
                ulong commands = CommandTableHash(); w.ULong(ref commands);
                ulong hash = _game.Hash(); w.ULong(ref hash);
                w.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>None when the peer runs the very same game (engine, content, settings, commands, state); otherwise why not.</summary>
        public Code CheckHello(byte[] hello)
        {
            if (hello == null || hello.Length < 2 || hello[0] != MsgHello) return new Code("net.not-a-hello");
            try
            {
                var r = new StateReader(hello, 1, hello.Length - 1);
                string build = null; r.String(ref build);
                if (build != EngineInfo.Build) return new Code("net.engine-differs");
                ulong fp = 0; r.ULong(ref fp);
                if (fp != _game.World.Content.Fingerprint) return new Code("net.content-differs");
                var settings = new WorldSettings(); settings.Sync(r);
                if (!SameSettings(settings, _game.World.Settings)) return new Code("net.settings-differ");
                int delay = 0; r.Int(ref delay);
                int checksums = 0; r.Int(ref checksums);
                if (delay != _inputDelay || checksums != _checksumEvery) return new Code("net.protocol-differs");
                long tick = 0; r.Long(ref tick);
                ulong commands = 0; r.ULong(ref commands);
                if (commands != CommandTableHash()) return new Code("net.commands-differ");
                ulong hash = 0; r.ULong(ref hash);
                if (tick != _game.Tick || hash != _game.Hash()) return new Code("net.state-differs");
                return r.AtEnd ? Code.None : new Code("net.not-a-hello");
            }
            catch (CorruptStateException) { return new Code("net.not-a-hello"); }
        }

        private static bool SameSettings(WorldSettings a, WorldSettings b)
        {
            if (a.Scenario != b.Scenario || a.Seed != b.Seed || a.Space != b.Space || a.Time != b.Time || a.HorizonTicks != b.HorizonTicks || a.GameMode != b.GameMode) return false;
            if (a.ModeOverrides.Count != b.ModeOverrides.Count) return false;
            for (int i = 0; i < a.ModeOverrides.Count; i++)
                if (a.ModeOverrides[i].Property != b.ModeOverrides[i].Property || !a.ModeOverrides[i].Value.Equals(b.ModeOverrides[i].Value)) return false;
            if (a.Modules.Length != b.Modules.Length || a.Seats.Length != b.Seats.Length) return false;
            if (a.ModuleVersions.Length != b.ModuleVersions.Length) return false;
            for (int i = 0; i < a.Modules.Length; i++)
                if (a.Modules[i] != b.Modules[i] || a.ModuleVersions[i] != b.ModuleVersions[i]) return false;
            for (int i = 0; i < a.Seats.Length; i++) if (a.Seats[i] != b.Seats[i]) return false;
            return true;
        }

        private ulong CommandTableHash()
        {
            var h = new StateHasher();
            IReadOnlyList<CommandRegistry.Entry> e = _game.World.Commands.Entries;
            int n = h.Count(e.Count);
            for (int i = 0; i < n; i++) { string id = e[i].Id; h.String(ref id); int tag = e[i].Tag; h.Int(ref tag); }
            return h.Value;
        }

        // ------------------------------------------------------------------ orders out

        /// <summary>Holds a local player's order for the next frame this machine seals. Returns its sequence.</summary>
        internal int Buffer(PlayerId seat, Command command)
        {
            if (!IsLocal(seat)) throw new InvalidOperationException("seat " + seat + " is played on another machine");
            Command copy = _game.World.Commands.Copy(command);
            lock (_pendingGate)
            {
                copy.Player = seat;
                copy.Stream = CommandStreams.Player;
                copy.Sequence = checked(_nextSequence[seat.Value] + 1);
                _pending[seat.Value].Add(copy);
                _nextSequence[seat.Value] = copy.Sequence;
                return copy.Sequence;
            }
        }

        /// <summary>Asks every peer to pause, resume or change speed at the same tick (sent with the next frame).</summary>
        public void RequestPause() => _control = ControlPause;

        /// <summary>
        /// Resumes every machine. A paused game runs no ticks, so the resume cannot ride in a tick's frame:
        /// it is sent at once, and every machine (all paused at the same tick) resumes when it hears it.
        /// </summary>
        public void RequestResume()
        {
            _game.Clock.ApplyResume();
            _outbox.Add(new[] { MsgResume });
        }
        public void RequestSpeed(int speed)
        {
            if (speed < 1 || speed > 64) throw new ArgumentOutOfRangeException(nameof(speed));
            _control = ControlSpeed; _controlSpeed = speed;
        }

        /// <summary>Messages to send to every peer since the last call.</summary>
        public int DrainOutgoing(IList<byte[]> dst)
        {
            for (int i = 0; i < _outbox.Count; i++) dst.Add(_outbox[i]);
            int n = _outbox.Count;
            _outbox.Clear();
            return n;
        }

        // ------------------------------------------------------------------ orders in

        /// <summary>A message from a peer (a frame of orders or a state hash).</summary>
        public void Receive(byte[] message)
        {
            if (State == NetState.Broken || State == NetState.Desynced) return;
            if (message == null || message.Length < 1) { State = NetState.Broken; return; }
            try
            {
                var r = new StateReader(message, 1, message.Length - 1);
                if (message[0] == MsgFrame) ReadFrame(r);
                else if (message[0] == MsgResume) _game.Clock.ApplyResume();
                else if (message[0] == MsgChecksum)
                {
                    long tick = 0; r.Long(ref tick);
                    ulong hash = 0; r.ULong(ref hash);
                    if (tick < 0 || !r.AtEnd) { State = NetState.Broken; return; }
                    _remoteHashes[tick] = hash;
                    Compare(tick);
                }
                else State = NetState.Broken;
            }
            catch (CorruptStateException) { State = NetState.Broken; }
        }

        private void ReadFrame(StateReader r)
        {
            long tick = 0; r.Long(ref tick);
            int seat = 0; r.Int(ref seat);
            // A frame must come from another machine's human seat, for a tick still to run, not one already held.
            if (seat < 0 || seat >= _local.Length || _local[seat] || !Needs(seat)) { State = NetState.Broken; return; }
            if (tick < _game.Tick || _policy.MaximumAhead.HasValue && tick - _game.Tick > _policy.MaximumAhead.Value)
            { State = NetState.Broken; return; }
            if (Has(tick, seat)) { State = NetState.Broken; return; }
            var f = new Frame();
            byte control = 0; r.Byte(ref control); f.Control = control;
            int speed = 0; r.Int(ref speed); f.Speed = speed;
            if ((control != ControlNone && control != ControlPause && control != ControlSpeed)
                || (control == ControlSpeed ? speed < 1 || speed > 64 : speed != 0))
            { State = NetState.Broken; return; }
            // Same signed-integer wire count, not an unrelated save collection ceiling.
            int n = 0; r.Int(ref n);
            if (n < 0 || _policy.MaximumCommandsPerFrame.HasValue && n > _policy.MaximumCommandsPerFrame.Value)
            { State = NetState.Broken; return; }
            int previousSequence = 0;
            for (int i = 0; i < n; i++)
            {
                int tag = 0; r.Int(ref tag);
                CommandRegistry.Entry entry = _game.World.Commands.ByTag(tag);
                if (entry == null) { State = NetState.Broken; return; }
                Command c = entry.Create();
                int seq = 0; r.Int(ref seq);
                if (seq <= previousSequence) { State = NetState.Broken; return; }
                previousSequence = seq;
                c.Player = new PlayerId(seat);
                c.Stream = CommandStreams.Player;
                c.Sequence = seq;
                c.Sync(r);
                f.Commands.Add(c);
            }
            if (!r.AtEnd) { State = NetState.Broken; return; }
            _frames.Add((tick, seat), f);
        }

        private Frame FrameFor(long tick, int seat)
        {
            var key = (tick, seat);
            if (!_frames.TryGetValue(key, out Frame f)) { f = new Frame(); _frames.Add(key, f); }
            return f;
        }

        private bool Has(long tick, int seat) => _frames.ContainsKey((tick, seat));

        // ------------------------------------------------------------------ the tick (called by SimGame)

        /// <summary>Seals this machine's frames up to <paramref name="tick"/> + delay (once each) and sends them.</summary>
        internal void Seal(long tick)
        {
            long target = checked(tick + _inputDelay);
            while (_sealedThrough < target)
            {
                long t = checked(_sealedThrough + 1);
                _sealedThrough = t;
                // Frames for the first ticks (before any input could arrive) are empty for every seat.
                bool first = t < target;
                for (int s = 0; s < _local.Length; s++)
                {
                    if (!_local[s]) continue;
                    Frame f = FrameFor(t, s);
                    if (!first)
                    {
                        lock (_pendingGate)
                        {
                            int count = _pending[s].Count;
                            if (_policy.MaximumCommandsPerFrame.HasValue && count > _policy.MaximumCommandsPerFrame.Value)
                                count = checked((int)_policy.MaximumCommandsPerFrame.Value);
                            for (int i = 0; i < count; i++) f.Commands.Add(_pending[s][i]);
                            _pending[s].RemoveRange(0, count);
                        }
                        f.Control = _control;
                        f.Speed = _control == ControlSpeed ? _controlSpeed : 0;
                        _control = ControlNone;
                    }
                    _outbox.Add(Encode(t, s, f));
                }
            }
        }

        private byte[] Encode(long tick, int seat, Frame f)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(MsgFrame);
                var w = new StateWriter(ms);
                w.Long(ref tick);
                w.Int(ref seat);
                byte control = f.Control; w.Byte(ref control);
                int speed = f.Speed; w.Int(ref speed);
                w.Count(f.Commands.Count);
                for (int i = 0; i < f.Commands.Count; i++)
                {
                    Command c = f.Commands[i];
                    if (!_game.World.Commands.TryGet(c.GetType(), out CommandRegistry.Entry entry)) throw new InvalidOperationException("unregistered command " + c.GetType().Name);
                    int tag = entry.Tag; w.Int(ref tag);
                    int seq = c.Sequence; w.Int(ref seq);
                    c.Sync(w);
                }
                w.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>True when every human seat's frame for this tick is here; then its orders go to the world.</summary>
        internal bool TryApply(long tick)
        {
            if (State == NetState.Desynced || State == NetState.Broken) return false;
            for (int s = 0; s < _local.Length; s++)
                if (Needs(s) && !Has(tick, s)) { State = NetState.Stalled; StalledOn = tick; return false; }
            // Admission is all-or-nothing across every seat in this epoch. A
            // delayed/regressing remote ticket must not commit an earlier seat's
            // commands or escape Update from the inbox's duplicate guard.
            for (int s = 0; s < _local.Length; s++)
            {
                if (!_frames.TryGetValue((tick, s), out Frame candidate)) continue;
                int last = _game.World.LastCommandSequence(new PlayerId(s), CommandStreams.Player);
                foreach (Command command in candidate.Commands)
                {
                    if (command.Stream != CommandStreams.Player || command.Sequence <= last) { State = NetState.Broken; return false; }
                    last = command.Sequence;
                }
            }
            State = NetState.Running;
            StalledOn = -1;
            for (int s = 0; s < _local.Length; s++)
            {
                var key = (tick, s);
                if (!_frames.TryGetValue(key, out Frame f)) continue;
                for (int i = 0; i < f.Commands.Count; i++)
                {
                    Command c = f.Commands[i];
                    c.IssuedTick = tick;
                    _game.World.SubmitStamped(c);
                }
                if (f.Control == ControlPause) _game.Clock.ApplyPause();
                else if (f.Control == ControlSpeed && f.Speed >= 1 && f.Speed <= 64) _game.Clock.ApplySpeed(f.Speed);
                _frames.Remove(key);
            }
            return true;
        }

        /// <summary>After a tick ran: every so often, share our state hash.</summary>
        internal void AfterTick(long tickRan)
        {
            if (_checksumEvery == 0 || tickRan % _checksumEvery != 0) return;
            ulong hash = _game.Hash();
            _localHashes[tickRan] = hash;
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(MsgChecksum);
                var w = new StateWriter(ms);
                w.Long(ref tickRan);
                w.ULong(ref hash);
                w.Flush();
                _outbox.Add(ms.ToArray());
            }
            Compare(tickRan);
        }

        private void Compare(long tick)
        {
            if (!_localHashes.TryGetValue(tick, out ulong mine) || !_remoteHashes.TryGetValue(tick, out ulong theirs)) return;
            if (mine != theirs) { State = NetState.Desynced; DesyncTick = tick; }
            _localHashes.Remove(tick);
            _remoteHashes.Remove(tick);
        }
    }
}
