using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine
{
    /// <summary>Stable producer identity; modules use their own module ID for autonomous input.</summary>
    public static class CommandStreams
    {
        public static readonly Code Player = new Code("engine.player-input");
    }
    /// <summary>
    /// A player's order: the only way anyone — a person, a rival, the autopilot, a network peer —
    /// changes a game. Fields are public so a client can fill them in; the game stamps who sent it and
    /// when. A command's <see cref="Sync"/> is its wire and replay format.
    /// </summary>
    public abstract class Command
    {
        public PlayerId Player { get; internal set; } = PlayerId.None;
        /// <summary>Sequence numbers are scoped to this producer and Player, never shared across producers.</summary>
        public Code Stream { get; internal set; }
        public long IssuedTick { get; internal set; }
        public int Sequence { get; internal set; }

        public virtual void Sync(IStateSync s) { }
    }

    /// <summary>What became of one command.</summary>
    public struct CommandResult
    {
        public PlayerId Player;
        public Code Stream;
        /// <summary>The sequence returned by this Player/Stream producer's Submit.</summary>
        public int Sequence;
        /// <summary>The tick the command was applied on.</summary>
        public long Tick;
        /// <summary>Stable command id ("oil.found-company").</summary>
        public string Command;
        public bool Accepted;
        /// <summary>Why it was refused (a content id a host localises); none when accepted.</summary>
        public Code Refusal;
        /// <summary>The entity the order created or acted on.</summary>
        public EntityId Entity;
        public long Arg0, Arg1;

        internal static void Sync(IStateSync sync, ref CommandResult value)
        {
            sync.Player(ref value.Player);
            string stream = value.Stream.Id; sync.String(ref stream);
            if (string.IsNullOrEmpty(stream)) throw new CorruptStateException("command result needs an explicit producer stream");
            if (sync.Reading) value.Stream = new Code(stream);
            sync.Int(ref value.Sequence); sync.Long(ref value.Tick);
            sync.String(ref value.Command); sync.Bool(ref value.Accepted);
            string refusal = value.Refusal.Id; sync.String(ref refusal);
            if (refusal != null && refusal.Length == 0) throw new CorruptStateException("empty command refusal identity");
            if (sync.Reading) value.Refusal = refusal == null ? Code.None : new Code(refusal);
            sync.Entity(ref value.Entity); sync.Long(ref value.Arg0); sync.Long(ref value.Arg1);
        }

        public override string ToString() => Command + "#" + Stream + "/" + Sequence + (Accepted ? " ok" : " refused " + Refusal);
    }

    /// <summary>The refusal codes the framework itself uses. Game modules declare their own the same way.</summary>
    public static class Codes
    {
        public static readonly Code NotOwned = new Code("core.not-owned");
        public static readonly Code NotFound = new Code("core.not-found");
        public static readonly Code InvalidArgument = new Code("core.invalid-argument");
        public static readonly Code UnknownDef = new Code("core.unknown-def");
        public static readonly Code NotAllowed = new Code("core.not-allowed");
        public static readonly Code Blocked = new Code("core.blocked");
        public static readonly Code OutOfBounds = new Code("core.out-of-bounds");
        public static readonly Code CannotAfford = new Code("core.cannot-afford");
        public static readonly Code NotSeated = new Code("core.not-seated");
        public static readonly Code UnknownCommand = new Code("core.unknown-command");
        public static readonly Code AlreadyDone = new Code("core.already-done");
    }
}

namespace Beep.SimEngine.Kernel
{
    /// <summary>
    /// Handles one command type. <see cref="Check"/> is pure (it may read, never write) and is exactly
    /// what a host's preview asks, so a preview can never disagree with the order. <see cref="Apply"/>
    /// runs only after Check passed, in the same tick.
    /// </summary>
    public abstract class CommandHandler<T> : ICommandHandler where T : Command
    {
        protected abstract Code Check(World world, T command);
        protected abstract void Apply(World world, T command, CommandContext context);

        Code ICommandHandler.Check(World world, Command command) => Check(world, (T)command);
        void ICommandHandler.Apply(World world, Command command, CommandContext context) => Apply(world, (T)command, context);
    }

    public interface ICommandHandler
    {
        Code Check(World world, Command command);
        void Apply(World world, Command command, CommandContext context);
    }

    /// <summary>What Apply reports back: the entity made or touched, and any numbers the order settled on.</summary>
    public sealed class CommandContext
    {
        public EntityId Entity { get; set; }
        public long Arg0 { get; set; }
        public long Arg1 { get; set; }

        internal void Reset()
        {
            Entity = EntityId.None;
            Arg0 = Arg1 = 0;
        }
    }

    /// <summary>
    /// Every command type a game understands: its stable id, its wire tag (its index, so the same
    /// modules give the same tags), a factory for decoding, and its handler.
    /// </summary>
    public sealed class CommandRegistry
    {
        public sealed class Entry
        {
            public string Id { get; }
            public int Tag { get; }
            public Type Type { get; }
            public Func<Command> Create { get; }
            public ICommandHandler Handler { get; }
            public string Owner { get; }
            internal Entry(string id, int tag, Type type, Func<Command> create, ICommandHandler handler, string owner)
            { Id = id; Tag = tag; Type = type; Create = create; Handler = handler; Owner = owner; }
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly Dictionary<Type, Entry> _byType = new Dictionary<Type, Entry>();
        private readonly Dictionary<string, Entry> _byId = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private bool _frozen;
        internal string ComposingModule { get; set; }

        public void Register<T>(string id, CommandHandler<T> handler) where T : Command, new()
        {
            if (_frozen) throw new InvalidOperationException("commands are registered while composing the game");
            if (ComposingModule == null) throw new InvalidOperationException("commands need a composing module owner");
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("a command needs an id");
            if (_byType.ContainsKey(typeof(T))) throw new InvalidOperationException(typeof(T).Name + " registered twice");
            if (_byId.ContainsKey(id)) throw new InvalidOperationException("command id '" + id + "' registered twice");
            var e = new Entry(id, _entries.Count + 1, typeof(T), () => new T(), handler ?? throw new ArgumentNullException(nameof(handler)), ComposingModule);
            _entries.Add(e);
            _byType.Add(e.Type, e);
            _byId.Add(id, e);
        }

        internal void Freeze() => _frozen = true;

        public IReadOnlyList<Entry> Entries => _entries.AsReadOnly();

        public bool TryGet(Type type, out Entry entry) => _byType.TryGetValue(type, out entry);
        public bool TryGet(string id, out Entry entry) => _byId.TryGetValue(id, out entry);

        public Entry ByTag(int tag) => tag >= 1 && tag <= _entries.Count ? _entries[tag - 1] : null;

        /// <summary>Snapshots submitted input through the command's sole wire/state contract.</summary>
        internal Command Copy(Command command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!TryGet(command.GetType(), out Entry entry))
                throw new ArgumentException("unregistered command " + command.GetType().Name, nameof(command));
            byte[] data;
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream);
                command.Sync(writer);
                writer.Flush();
                data = stream.ToArray();
            }
            Command copy = entry.Create();
            var reader = new StateReader(data, 0, data.Length);
            copy.Sync(reader);
            if (!reader.AtEnd) throw new InvalidOperationException("command Sync did not consume its complete payload: " + entry.Id);
            copy.Player = command.Player;
            copy.Stream = command.Stream;
            copy.IssuedTick = command.IssuedTick;
            copy.Sequence = command.Sequence;
            return copy;
        }
    }
}
