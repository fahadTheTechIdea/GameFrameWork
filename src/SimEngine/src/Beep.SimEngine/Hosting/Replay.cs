using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Hosting
{
    /// <summary>Caller-selected artifact safety budgets, never gameplay/world size limits.</summary>
    public sealed class ReplayLimits
    {
        public ReplayLimits(int maxBytes, int maxRecords)
        {
            if (maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxBytes));
            if (maxRecords < 1) throw new ArgumentOutOfRangeException(nameof(maxRecords));
            MaxBytes = maxBytes; MaxRecords = maxRecords;
        }
        public int MaxBytes { get; }
        public int MaxRecords { get; }
    }

    public interface IReplayRecorder : IDisposable
    {
        bool Active { get; }
        Code Failure { get; }
        long InitialStep { get; }
        int CommandCount { get; }
        int CheckpointCount { get; }
        byte[] Finish();
    }
    public interface IReplayVerifier
    {
        ReplayVerification Verify(GameFactory factory, byte[] replay, ReplayLimits limits);
    }
    public sealed class ReplayModuleInfo
    {
        internal ReplayModuleInfo(string id, ModuleVersion version) { Id = id; Version = version; }
        public string Id { get; }
        public ModuleVersion Version { get; }
    }
    /// <summary>Detached saved-session identity; this privileged diagnostic is not a player knowledge view.</summary>
    public sealed class ReplayDescription
    {
        internal ReplayDescription(ReplayData data)
        {
            SaveHeader h = SaveGame.Describe(data.Initial);
            Game = h.Game; Build = h.Build; SaveFormat = h.Format; ContentFingerprint = h.ContentFingerprint;
            Scenario = h.Settings.Scenario; Time = h.Settings.Time; Space = h.Settings.Space;
            InitialStep = h.Tick; FinalStep = data.FinalStep; InitialHash = data.InitialHash; FinalHash = data.FinalHash;
            Commands = data.Commands; Checkpoints = data.Checkpoints;
            var modules = new List<ReplayModuleInfo>();
            for (int i = 0; i < h.Settings.Modules.Length; i++) modules.Add(new ReplayModuleInfo(h.Settings.Modules[i], h.Settings.ModuleVersions[i]));
            Modules = modules.AsReadOnly();
        }
        public string Game { get; }
        public string Build { get; }
        public int SaveFormat { get; }
        public ulong ContentFingerprint { get; }
        public string Scenario { get; }
        public string Time { get; }
        public string Space { get; }
        public long InitialStep { get; }
        public long FinalStep { get; }
        public ulong InitialHash { get; }
        public ulong FinalHash { get; }
        public int Commands { get; }
        public int Checkpoints { get; }
        public IReadOnlyList<ReplayModuleInfo> Modules { get; }
    }
    /// <summary>Failure never exposes a partially verified live game.</summary>
    public sealed class ReplayVerification
    {
        internal ReplayVerification(Code failure, string detail, long step, int record, ulong expected, ulong actual, SimGame game = null)
        { Failure = failure; Detail = detail; Step = step; Record = record; ExpectedHash = expected; ActualHash = actual; Game = game; }
        public bool Success => Failure.IsNone;
        public Code Failure { get; }
        public string Detail { get; }
        public long Step { get; }
        public int Record { get; }
        public ulong ExpectedHash { get; }
        public ulong ActualHash { get; }
        public SimGame Game { get; }
    }

    /// <summary>One detached command/checkpoint observation stream; no authoritative state or extra clock.</summary>
    internal sealed class ReplayRecorder : IReplayRecorder
    {
        private readonly SimGame _game;
        private readonly ReplayLimits _limits;
        private readonly MemoryStream _stream = new MemoryStream();
        private bool _closed;
        internal Action<Command> Input { get; }
        internal ReplayRecorder(SimGame game, ReplayLimits limits)
        {
            _game = game; _limits = limits; Input = Admitted; InitialStep = game.Tick;
            var writer = new StateWriter(_stream); writer.Section(ReplayFormat.Magic);
            int version = ReplayFormat.Version; writer.Int(ref version);
            ReplayFormat.WriteBlock(writer, game.Save());
            ulong hash = game.Hash(); writer.ULong(ref hash);
            IReadOnlyList<CommandRegistry.Entry> commands = game.World.Commands.Entries;
            writer.Count(commands.Count);
            foreach (var entry in commands)
            {
                string id = entry.Id, owner = entry.Owner; int tag = entry.Tag;
                writer.String(ref id); writer.Int(ref tag); writer.String(ref owner);
            }
            writer.Flush();
            if (_stream.Length + ReplayFormat.FooterBytes > limits.MaxBytes)
            { _stream.Dispose(); throw new ArgumentException("initial replay identity exceeds the declared artifact budget", nameof(limits)); }
        }
        public bool Active => !_closed && Failure.IsNone;
        public Code Failure { get; private set; }
        public long InitialStep { get; }
        public int CommandCount { get; private set; }
        public int CheckpointCount { get; private set; }
        private bool Append(Action<StateWriter> encode)
        {
            if (!Active) return false;
            if ((long)CommandCount + CheckpointCount >= _limits.MaxRecords)
            { Failure = new Code("replay.recording-limit"); return false; }
            try
            {
                using (var temporary = new MemoryStream())
                {
                    var writer = new StateWriter(temporary); encode(writer); writer.Flush();
                    if (temporary.Length > _limits.MaxBytes - _stream.Length - ReplayFormat.FooterBytes)
                    { Failure = new Code("replay.recording-limit"); return false; }
                    temporary.WriteTo(_stream); return true;
                }
            }
            catch (Exception) { Failure = new Code("replay.recording-failed"); return false; }
        }
        internal void Admitted(Command command)
        {
            if (command.Stream != CommandStreams.Player) throw new InvalidOperationException("replay inputs must belong to the external player stream; autonomous inputs are recomputed");
            // Called with the original inbox lock, on its actual copied/stamped input before commit.
            if (Append(writer => {
                byte kind = ReplayFormat.Input; writer.Byte(ref kind);
                long admission = _game.Tick, issued = command.IssuedTick; writer.Long(ref admission);
                if (!_game.World.Commands.TryGet(command.GetType(), out var entry)) throw new InvalidOperationException("unknown replay input");
                string id = entry.Id; writer.String(ref id);
                PlayerId player = command.Player; writer.Player(ref player); writer.Long(ref issued);
                int sequence = command.Sequence; writer.Int(ref sequence);
                using (var payload = new MemoryStream())
                {
                    var codec = new StateWriter(payload); command.Sync(codec); codec.Flush();
                    ReplayFormat.WriteBlock(writer, payload.ToArray());
                }
            })) CommandCount++;
        }
        internal void CompletedStep()
        {
            if (Append(writer => {
                byte kind = ReplayFormat.Checkpoint; writer.Byte(ref kind);
                long step = _game.Tick; ulong hash = _game.Hash(); writer.Long(ref step); writer.ULong(ref hash);
                var results = _game.World.LastTickResults; writer.Count(results.Count);
                foreach (var result in results) { CommandResult copy = result; CommandResult.Sync(writer, ref copy); }
            })) CheckpointCount++;
        }
        public byte[] Finish()
        {
            _game.RequireReplayIdle();
            lock (_game.World.InputGate)
            {
                if (_closed) throw new InvalidOperationException("replay recorder is closed");
                try
                {
                    if (_game.Fault != null) Failure = new Code("replay.session-faulted");
                    if (!Failure.IsNone) throw new InvalidOperationException("incomplete replay: " + Failure);
                    var writer = new StateWriter(_stream); byte kind = ReplayFormat.Finish; writer.Byte(ref kind);
                    long step = _game.Tick; ulong hash = _game.Hash(); writer.Long(ref step); writer.ULong(ref hash);
                    int commands = CommandCount, checks = CheckpointCount; writer.Int(ref commands); writer.Int(ref checks); writer.Flush();
                    ulong checksum = ReplayFormat.Checksum(_stream.ToArray(), (int)_stream.Length); writer.ULong(ref checksum); writer.Flush();
                    return _stream.ToArray();
                }
                finally { _closed = true; _game.ReleaseReplay(this); _stream.Dispose(); }
            }
        }
        public void Dispose()
        {
            _game.RequireReplayIdle();
            lock (_game.World.InputGate)
            {
                if (_closed) return;
                _closed = true; _game.ReleaseReplay(this); _stream.Dispose();
            }
        }
    }

    /// <summary>Verifies through the original save, inbox, command codecs and Initialize/Update loop.</summary>
    public sealed class ReplayVerifier : IReplayVerifier
    {
        public static ReplayDescription Describe(byte[] replay, ReplayLimits limits) => new ReplayDescription(ReplayFormat.Read(replay, limits));
        public ReplayVerification Verify(GameFactory factory, byte[] replay, ReplayLimits limits)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (replay == null) throw new ArgumentNullException(nameof(replay));
            if (limits == null) throw new ArgumentNullException(nameof(limits));
            ReplayData data;
            try { data = ReplayFormat.Read(replay, limits); }
            catch (CorruptStateException error) { return Refuse("replay.invalid", error.Message, -1, -1); }
            SimGame game = null;
            int recordIndex = -1;
            string callback = "session.restore";
            long initialStep = SaveGame.Describe(data.Initial).Tick;
            try
            {
                try { game = factory.Load(data.Initial); }
                catch (CorruptStateException error) { return Refuse("replay.identity", error.Message, -1, -1); }
                var registry = game.World.Commands.Entries;
                if (registry.Count != data.Registry.Count) return Refuse("replay.identity", "command count differs", game.Tick, -1);
                for (int i = 0; i < registry.Count; i++)
                    if (registry[i].Id != data.Registry[i].Id || registry[i].Tag != data.Registry[i].Tag || registry[i].Owner != data.Registry[i].Owner)
                        return Refuse("replay.identity", "command ownership differs: " + data.Registry[i].Id, game.Tick, -1);
                callback = "initial.hash";
                ulong actual = game.Hash();
                if (actual != data.InitialHash) return Refuse("replay.state", "initial state differs", game.Tick, -1, data.InitialHash, actual);
                for (int i = 0; i < data.Records.Count; i++)
                {
                    recordIndex = i;
                    ReplayRecord record = data.Records[i];
                    if (record.Kind == ReplayFormat.Input)
                    {
                        if (record.Step != game.Tick || !game.World.Players.IsSeat(record.Player) || record.Issued > game.Tick ||
                            game.World.WasCommandIssued(record.Player, CommandStreams.Player, record.Sequence))
                            return Refuse("replay.order", "input admission/sequence differs", game.Tick, i);
                        if (!game.World.Commands.TryGet(record.Id, out var entry)) return Refuse("replay.identity", "unknown command " + record.Id, game.Tick, i);
                        try
                        {
                            callback = "input.decode:" + record.Id;
                            Command command = entry.Create(); var reader = new StateReader(record.Payload, 0, record.Payload.Length); command.Sync(reader);
                            if (!reader.AtEnd) return Refuse("replay.invalid", "trailing command payload", game.Tick, i);
                            command.Player = record.Player; command.Stream = CommandStreams.Player;
                            command.Sequence = record.Sequence; command.IssuedTick = record.Issued;
                            callback = "input.admit:" + record.Id;
                            game.World.SubmitStamped(command);
                        }
                        catch (CorruptStateException error) { return Refuse("replay.invalid", error.Message, game.Tick, i); }
                        catch (ArgumentException error) { return Refuse("replay.invalid", error.Message, game.Tick, i); }
                    }
                    else
                    {
                        if (record.Step != game.Tick + 1) return Refuse("replay.order", "checkpoint is not next game step", game.Tick, i);
                        callback = "step.update";
                        if (game.RunTicks(1) != 1 || game.Fault != null) return Refuse("replay.fault", "recorded step cannot complete: " + game.Fault, game.Tick, i);
                        callback = "checkpoint.hash";
                        actual = game.Hash();
                        if (actual != record.Hash) return Refuse("replay.state", "checkpoint differs", game.Tick, i, record.Hash, actual);
                        var results = game.World.LastTickResults;
                        if (results.Count != record.Results.Count) return Refuse("replay.results", "result count differs", game.Tick, i);
                        for (int j = 0; j < results.Count; j++)
                            if (!results[j].Equals(record.Results[j])) return Refuse("replay.results", "command result differs at " + j, game.Tick, i);
                    }
                }
                recordIndex = data.Records.Count;
                callback = "final.hash";
                actual = game.Hash();
                if (game.Tick != data.FinalStep || actual != data.FinalHash)
                    return Refuse("replay.state", "final state/pending input differs", game.Tick, data.Records.Count, data.FinalHash, actual);
                return new ReplayVerification(Code.None, null, game.Tick, data.Records.Count, data.FinalHash, actual, game);
            }
            catch (Exception error)
            {
                return Refuse("replay.provider", callback + ": " + error,
                    game == null ? initialStep : game.Tick, recordIndex);
            }
        }
        private static ReplayVerification Refuse(string id, string detail, long step, int record, ulong expected = 0, ulong actual = 0)
            => new ReplayVerification(new Code(id), detail, step, record, expected, actual);
    }

    // Detached artifact decoding only: these records never own live gameplay/pending input state.
    internal sealed class ReplayRecord
    {
        internal byte Kind; internal int Offset; internal long Step, Issued; internal ulong Hash;
        internal string Id; internal PlayerId Player; internal int Sequence; internal byte[] Payload;
        internal readonly List<CommandResult> Results = new List<CommandResult>();
    }
    internal sealed class ReplayCommandIdentity
    {
        internal string Id, Owner; internal int Tag;
    }
    internal sealed class ReplayData
    {
        internal byte[] Initial; internal ulong InitialHash, FinalHash; internal long FinalStep;
        internal int Commands, Checkpoints;
        internal readonly List<ReplayCommandIdentity> Registry = new List<ReplayCommandIdentity>();
        internal readonly List<ReplayRecord> Records = new List<ReplayRecord>();
    }
    internal static class ReplayFormat
    {
        internal const string Magic = "beep.simengine.replay";
        internal const int Version = 2, FooterBytes = 33;
        internal const byte Input = 1, Checkpoint = 2, Finish = 3;
        internal static void WriteBlock(StateWriter writer, byte[] bytes)
        { int length = bytes.Length; writer.Int(ref length); writer.Bytes(bytes); }
        private static byte[] ReadBlock(StateReader reader, byte[] artifact, ReplayLimits limits)
        {
            int length = 0; reader.Int(ref length);
            if (length < 0 || length > limits.MaxBytes || length > artifact.Length - reader.Position - 4)
                throw new CorruptStateException("invalid replay byte block");
            var bytes = new byte[length]; reader.Bytes(bytes); return bytes;
        }
        internal static ulong Checksum(byte[] data, int length)
        {
            var prefix = new byte[length]; Buffer.BlockCopy(data, 0, prefix, 0, length);
            var hasher = new StateHasher(); hasher.Bytes(prefix); return hasher.Value;
        }
        internal static ReplayData Read(byte[] artifact, ReplayLimits limits)
        {
            if (artifact == null) throw new ArgumentNullException(nameof(artifact));
            if (limits == null) throw new ArgumentNullException(nameof(limits));
            if (artifact.Length < 8 || artifact.Length > limits.MaxBytes) throw new CorruptStateException("replay artifact exceeds budget or ends early");
            int end = artifact.Length - 8; ulong stored = 0; new StateReader(artifact, end, 8).ULong(ref stored);
            if (stored != Checksum(artifact, end)) throw new CorruptStateException("replay checksum differs");
            var reader = new StateReader(artifact, 0, end); reader.Section(Magic);
            int version = 0; reader.Int(ref version);
            if (version != Version) throw new CorruptStateException("unsupported replay schema");
            var data = new ReplayData { Initial = ReadBlock(reader, artifact, limits) };
            SaveHeader header = SaveGame.Describe(data.Initial); long step = header.Tick;
            reader.ULong(ref data.InitialHash);
            int identities = reader.Count(0);
            if (identities > (end - reader.Position) / 12) throw new CorruptStateException("replay command identities end early");
            var identityIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < identities; i++)
            {
                var identity = new ReplayCommandIdentity(); reader.String(ref identity.Id); reader.Int(ref identity.Tag); reader.String(ref identity.Owner);
                if (string.IsNullOrEmpty(identity.Id) || string.IsNullOrEmpty(identity.Owner) || identity.Tag != i + 1 || !identityIds.Add(identity.Id))
                    throw new CorruptStateException("invalid replay command identity");
                data.Registry.Add(identity);
            }
            var inputs = new HashSet<(int, int)>();
            while (!reader.AtEnd)
            {
                int offset = reader.Position; byte kind = 0; reader.Byte(ref kind);
                if (kind == Finish)
                {
                    reader.Long(ref data.FinalStep); reader.ULong(ref data.FinalHash);
                    int commands = 0, checks = 0; reader.Int(ref commands); reader.Int(ref checks);
                    if (!reader.AtEnd || data.FinalStep != step || commands != data.Commands || checks != data.Checkpoints)
                        throw new CorruptStateException("replay final record differs or has trailing data");
                    return data;
                }
                if (data.Records.Count >= limits.MaxRecords) throw new CorruptStateException("replay record budget exceeded");
                var record = new ReplayRecord { Kind = kind, Offset = offset }; reader.Long(ref record.Step);
                if (kind == Input)
                {
                    reader.String(ref record.Id); reader.Player(ref record.Player); reader.Long(ref record.Issued); reader.Int(ref record.Sequence);
                    if (record.Step != step || record.Issued < 0 || record.Issued > step || record.Sequence <= 0 ||
                        record.Player.Value < 0 || record.Player.Value >= header.Settings.Seats.Length || !identityIds.Contains(record.Id) ||
                        !inputs.Add((record.Player.Value, record.Sequence))) throw new CorruptStateException("invalid or duplicate replay input order");
                    record.Payload = ReadBlock(reader, artifact, limits); data.Commands++;
                }
                else if (kind == Checkpoint)
                {
                    if (step == long.MaxValue || record.Step != step + 1) throw new CorruptStateException("replay checkpoint sequence differs");
                    reader.ULong(ref record.Hash); int count = reader.Count(0);
                    if (count > (end - reader.Position) / 45) throw new CorruptStateException("replay results end early");
                    for (int i = 0; i < count; i++) { var result = new CommandResult(); CommandResult.Sync(reader, ref result); record.Results.Add(result); }
                    step = record.Step; data.Checkpoints++;
                }
                else throw new CorruptStateException("unknown replay record kind");
                data.Records.Add(record);
            }
            throw new CorruptStateException("replay has no final record");
        }
    }
}
