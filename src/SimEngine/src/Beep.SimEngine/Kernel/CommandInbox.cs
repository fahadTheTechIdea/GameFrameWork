using System;
using System.Collections.Generic;

namespace Beep.SimEngine.Kernel
{
    /// <summary>The single pending-command and per-company/producer sequence owner.</summary>
    internal sealed class CommandInbox
    {
        private readonly CommandRegistry _registry;
        private readonly List<Command> _pending = new List<Command>();
        private readonly SortedDictionary<string, int>[] _sequences;
        private readonly IReadOnlyList<string> _owners;
        internal object Gate { get; } = new object();

        internal CommandInbox(CommandRegistry registry, int seats, IReadOnlyList<string> owners)
        {
            _registry = registry;
            _owners = owners;
            _sequences = new SortedDictionary<string, int>[global::System.Math.Max(1, seats)];
            for (int i = 0; i < _sequences.Length; i++) _sequences[i] = new SortedDictionary<string, int>(StringComparer.Ordinal);
        }
        internal bool Supports(Code stream)
        {
            if (stream.IsNone) return false;
            if (stream == CommandStreams.Player) return true;
            for (int i = 0; i < _owners.Count; i++) if (_owners[i] == stream.Id) return true;
            return false;
        }
        private int Last(PlayerId player, Code stream)
        {
            if (!Supports(stream)) throw new ArgumentException("explicit selected command producer stream required", nameof(stream));
            return _sequences[player.Value].TryGetValue(stream.Id, out int value) ? value : 0;
        }
        internal int Submit(PlayerId player, Code stream, Command command, long tick, Action<Command> admitted = null)
        {
            Command copy = _registry.Copy(command);
            lock (Gate)
            {
                copy.Player = player; copy.Stream = stream; copy.IssuedTick = tick;
                copy.Sequence = checked(Last(player, stream) + 1);
                admitted?.Invoke(copy);
                _pending.Add(copy); _sequences[player.Value][stream.Id] = copy.Sequence;
                return copy.Sequence;
            }
        }
        internal void SubmitStamped(Command command, Action<Command> admitted = null)
        {
            Command copy = _registry.Copy(command);
            lock (Gate)
            {
                if (copy.Sequence <= Last(copy.Player, copy.Stream)) throw new ArgumentException("command producer sequence was already issued", nameof(command));
                admitted?.Invoke(copy);
                _pending.Add(copy); _sequences[copy.Player.Value][copy.Stream.Id] = copy.Sequence;
            }
        }
        internal Command[] Drain()
        {
            lock (Gate)
            {
                if (_pending.Count == 0) return Array.Empty<Command>();
                Command[] batch = _pending.ToArray(); _pending.Clear(); return batch;
            }
        }
        internal bool IsPending(PlayerId player, Code stream, int sequence)
        {
            lock (Gate)
            {
                int count = 0;
                foreach (Command command in _pending) if (command.Player == player && command.Stream == stream && command.Sequence == sequence) count++;
                return count == 1;
            }
        }
        internal bool WasIssued(PlayerId player, Code stream, int sequence)
        { lock (Gate) return sequence > 0 && sequence <= Last(player, stream); }
        internal int LastSequence(PlayerId player, Code stream)
        { lock (Gate) return Last(player, stream); }
        internal bool PendingMatches(PlayerId player, Code stream, int sequence, string identity, long issued)
        {
            lock (Gate)
            {
                int count = 0;
                foreach (Command command in _pending)
                    if (command.Player == player && command.Stream == stream && command.Sequence == sequence)
                    {
                        if (command.IssuedTick != issued || !_registry.TryGet(command.GetType(), out CommandRegistry.Entry entry) || entry.Id != identity) return false;
                        count++;
                    }
                return count == 1;
            }
        }
        // World holds Gate over sequences and pending input, forming one checkpoint.
        internal void SyncSequences(IStateSync sync)
        {
            if (sync.Count(_sequences.Length) != _sequences.Length) throw new CorruptStateException("seat count differs from the save");
            for (int i = 0; i < _sequences.Length; i++)
            {
                var sequences = _sequences[i]; int count = sync.Count(sequences.Count);
                if (sync.Reading)
                {
                    sequences.Clear(); string previous = null;
                    for (int j = 0; j < count; j++)
                    {
                        string stream = null; int sequence = 0; sync.String(ref stream); sync.Int(ref sequence);
                        if (string.IsNullOrEmpty(stream) || !Supports(new Code(stream)) || sequence <= 0 || previous != null && string.CompareOrdinal(previous, stream) >= 0)
                            throw new CorruptStateException("invalid command producer sequence identity/order");
                        sequences.Add(stream, sequence); previous = stream;
                    }
                }
                else foreach (var pair in sequences) { string stream = pair.Key; int sequence = pair.Value; sync.String(ref stream); sync.Int(ref sequence); }
            }
        }
        internal void SyncPending(IStateSync sync)
        {
            sync.Section("queue"); int count = sync.Count(_pending.Count);
            var identities = new HashSet<(int, string, int)>();
            if (sync.Reading) _pending.Clear();
            for (int i = 0; i < count; i++)
            {
                Command command; int tag;
                if (sync.Reading)
                {
                    tag = 0; sync.Int(ref tag);
                    var entry = _registry.ByTag(tag) ?? throw new CorruptStateException("unknown command tag " + tag);
                    command = entry.Create();
                }
                else
                {
                    command = _pending[i];
                    if (!_registry.TryGet(command.GetType(), out var entry)) throw new InvalidOperationException("unregistered command " + command.GetType().Name);
                    tag = entry.Tag; sync.Int(ref tag);
                }
                PlayerId player = command.Player; string stream = command.Stream.Id;
                long issued = command.IssuedTick; int sequence = command.Sequence;
                sync.Player(ref player); sync.String(ref stream); sync.Long(ref issued); sync.Int(ref sequence);
                if (player.Value < 0 || player.Value >= _sequences.Length || string.IsNullOrEmpty(stream) || !Supports(new Code(stream)) || issued < 0 ||
                    sequence <= 0 || sequence > Last(player, new Code(stream)) || !identities.Add((player.Value, stream, sequence)))
                    throw new CorruptStateException("invalid/duplicate queued command identity");
                command.Sync(sync);
                if (sync.Reading)
                {
                    command.Player = player; command.Stream = new Code(stream); command.IssuedTick = issued; command.Sequence = sequence;
                    _pending.Add(command);
                }
            }
        }
    }
}
