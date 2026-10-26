using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class SystemReferenceSettingsDef : Def
    {
        public string Mode { get; private set; }
        public bool Alias { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Mode = reader.String("mode"); Alias = reader.Bool("alias");
            if (Mode != "normal" && Mode != "deny" && Mode != "mutate-check" && Mode != "throw-commit")
                throw reader.Error("explicit reference system mode required");
        }
    }
    internal interface IReferenceSystemAlias { }
    internal sealed class ReferenceOwnerSystem : GameSystem, IEntityReplacementReferenceOwner, IReferenceSystemAlias
    {
        private readonly World _world;
        private readonly string _id, _mode;
        internal IReadOnlyList<ReferenceOwnerSystem> Peers;
        internal EntityId Reference;
        internal PlayerId Company = PlayerId.None;
        internal int Commits, Ordinal;
        internal ReferenceOwnerSystem(World world, string id, string mode) { _world = world; _id = id; _mode = mode; }
        public override string Id => _id;
        public override int StateSchemaVersion => 1;
        public override void Update(World world, TickInfo tick) { }
        public Code CheckReferenceReplacement(World world, Entity source, EntityDef destination)
        {
            if (!ReferenceEquals(world, _world)) throw new InvalidOperationException("reference system belongs to another world");
            if (Reference != source.Id) return Code.None;
            if (Company != source.Owner) return Codes.NotOwned;
            if (_mode == "deny") return new Code("proof.system-reference-denied");
            if (_mode == "mutate-check") Commits++;
            return Code.None;
        }
        public void ReplaceEntityReferences(World world, Entity source, Entity destination)
        {
            if (!ReferenceEquals(world, _world)) throw new InvalidOperationException("reference system belongs to another world");
            if (Reference != source.Id) return;
            if (_mode == "throw-commit") throw new InvalidOperationException("authored system reference failure");
            if (!ReferenceEquals(world.Entities.Get(source.Id), source) || !ReferenceEquals(world.Entities.Get(destination.Id), destination) ||
                Company != destination.Owner) throw new InvalidOperationException("system callback lost actual replacement participants");
            int ordinal = 1; foreach (var peer in Peers) ordinal += peer.Commits;
            Reference = destination.Id; Ordinal = ordinal; Commits++;
        }
        public override void Sync(IStateSync sync)
        { sync.Entity(ref Reference); sync.Player(ref Company); sync.Int(ref Commits); sync.Int(ref Ordinal); }
        public override void OnLoaded(World world)
        {
            if (Reference.IsValid && world.Entities.Get(Reference)?.Owner != Company || Commits < 0 || Ordinal < 0)
                throw new CorruptStateException("invalid reference system state");
        }
    }
    public sealed class SetSystemReferencesCommand : Command
    {
        public EntityId Target;
        public override void Sync(IStateSync sync) => sync.Entity(ref Target);
    }
    internal sealed class SetSystemReferencesHandler : CommandHandler<SetSystemReferencesCommand>
    {
        private readonly IReadOnlyList<ReferenceOwnerSystem> _owners;
        internal SetSystemReferencesHandler(IReadOnlyList<ReferenceOwnerSystem> owners) { _owners = owners; }
        protected override Code Check(World world, SetSystemReferencesCommand command)
            => world.Entities.Get(command.Target)?.Owner == command.Player ? Code.None : Codes.NotFound;
        protected override void Apply(World world, SetSystemReferencesCommand command, CommandContext context)
        { foreach (var owner in _owners) { owner.Reference = command.Target; owner.Company = command.Player; } context.Entity = command.Target; }
    }
    public readonly struct SystemReferenceFact
    {
        public string Id { get; }
        public EntityId Reference { get; }
        public int Commits { get; }
        public int Ordinal { get; }
        internal SystemReferenceFact(ReferenceOwnerSystem owner) { Id = owner.Id; Reference = owner.Reference; Commits = owner.Commits; Ordinal = owner.Ordinal; }
    }
    public sealed class SystemReferenceView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        private readonly IReadOnlyList<ReferenceOwnerSystem> _owners;
        internal SystemReferenceView(World world, PlayerId viewer, IReadOnlyList<ReferenceOwnerSystem> owners)
        { _world = world; _viewer = viewer; _owners = owners; }
        public void Copy(IList<SystemReferenceFact> destination)
        {
            _world.RequireHealthyUpdate(); destination.Clear();
            foreach (var owner in _owners) if (owner.Company == _viewer) destination.Add(new SystemReferenceFact(owner));
        }
    }
    internal static class SystemReferenceProof
    {
        internal static void Compose(WorldComposer composer, SystemReferenceSettingsDef settings)
        {
            var first = new ReferenceOwnerSystem(composer.World, "proof.reference.first", settings.Mode);
            var alias = new ReferenceOwnerSystem(composer.World, "proof.reference.alias", "normal");
            var last = new ReferenceOwnerSystem(composer.World, "proof.reference.last", "normal");
            var owners = Array.AsReadOnly(new[] { first, alias, last });
            foreach (var owner in owners) owner.Peers = owners;
            if (settings.Alias) composer.Services.Bind<IReferenceSystemAlias>(alias);
            // Registration order differs from declared execution order.
            composer.AddSystem(last, 275); composer.AddSystem(first, 271); composer.AddSystem(alias, 273);
            composer.Commands.Register("proof.set-system-references", new SetSystemReferencesHandler(owners));
            composer.AddViewFeature("proof.system-references", 1, (world, viewer) => new SystemReferenceView(world, viewer, owners));
        }
    }
}
