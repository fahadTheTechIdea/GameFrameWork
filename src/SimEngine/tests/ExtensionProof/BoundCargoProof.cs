using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side capability proof: a worker's actual cargo inventory need not be on the worker.
    public sealed class BoundCargoDef : ComponentDef<BoundCargo>, ITransporterDefinition
    {
        private string _cargo, _handling;
        private string[] _replacements;
        public EntityDef CargoOwner { get; private set; }
        public IReadOnlyList<EntityDef> CargoReplacements { get; private set; }
        public bool AcceptsCargoOwner(EntityDef target)
        {
            if (target == CargoOwner) return true;
            foreach (EntityDef replacement in CargoReplacements) if (replacement == target) return true;
            return false;
        }
        public IResourceHandlingProfile Handling { get; private set; }
        public string Fault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _cargo = reader.String("cargo"); _handling = reader.String("handling"); Fault = reader.String("fault");
            JsonValue replacements = reader.Array("cargo_replacements");
            if (replacements == null) throw reader.Error("explicit cargo_replacements array required");
            _replacements = new string[replacements.Count];
            for (int i = 0; i < replacements.Count; i++)
            {
                if (replacements[i].Kind != JsonKind.String || string.IsNullOrWhiteSpace(replacements[i].Text))
                    throw reader.Error("cargo replacement identities must be nonempty strings");
                _replacements[i] = replacements[i].Text;
            }
            if (Fault != "none" && Fault != "mutate-null" && Fault != "mutate-state") throw reader.Error("unknown cargo proof fault");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            CargoOwner = content.Def<EntityDef>(_cargo); Handling = content.Def<IResourceHandlingProfile>(_handling);
            var replacements = new List<EntityDef>();
            foreach (string id in _replacements)
            {
                var target = content.Def<EntityDef>(id);
                if (target == CargoOwner || replacements.Contains(target) || !target.HasCapability<IInventory>() || target.Component<IInventoryDefinition>() == null)
                    throw content.Fail("cargo replacements must be distinct inventory prototypes other than the initial cargo owner");
                replacements.Add(target);
            }
            CargoReplacements = replacements.AsReadOnly();
        }
        public void ValidateBinding(EntityDef owner)
        {
            if (!owner.HasCapability<ISpatialPresence>() || !CargoOwner.HasCapability<IInventory>())
                throw new ContentException("bound transporter requires authored presence and actual cargo inventory owner");
        }
        protected override void ValidateCore(EntityDef owner)
            => ValidateBinding(owner);
    }
    public sealed class BoundCargo : Component<BoundCargoDef>, ITransporter, IEntityInitialization, IEntityReplacementReferenceOwner,
        IReplacementGuard, IEntityReplacementState
    {
        private World _world;
        private EntityId _cargo;
        private int _reads;
        internal void Bind(World world) => _world = world;
        public void Initialize(World world) => Bind(world);
        internal void BindActualCargo(World world)
        {
            Bind(world);
            foreach (var entity in world.Entities.All)
                if (entity.Alive && entity.Owner == Entity.Owner && entity.Def == Def.CargoOwner)
                {
                    if (_cargo.IsValid) throw new InvalidOperationException("ambiguous authored cargo binding");
                    _cargo = entity.Id;
                }
            if (!_cargo.IsValid) throw new InvalidOperationException("missing authored cargo binding");
        }
        public IInventory Cargo
        {
            get
            {
                if (Def.Fault == "mutate-null") { Entity.Touch(); return null; }
                if (Def.Fault == "mutate-state") _reads++;
                return _world.Entities.Get(_cargo)?.Get<IInventory>();
            }
        }
        public IResourceHandlingProfile Handling => Def.Handling;
        public Code CheckRemoval(World world) => TransporterContracts.CheckRemoval(world, this);
        public Code CheckReplacement(World world, EntityDef target)
        {
            var next = target.Component<BoundCargoDef>();
            if (next == null || !target.HasCapability<ITransporter>()) return CheckRemoval(world);
            return next.AcceptsCargoOwner(TransporterContracts.ReadCargo(world, Entity).Entity.Def)
                ? Code.None : new Code("proof.cargo-replacement-incompatible");
        }
        public void RestoreReplacementState(World world, Component source)
        {
            if (!(source is BoundCargo previous) || !Def.AcceptsCargoOwner(TransporterContracts.ReadCargo(world, source.Entity).Entity.Def))
                throw new InvalidOperationException("bound cargo replacement cannot retain an incompatible binding");
            Bind(world); _cargo = previous._cargo; _reads = previous._reads;
        }
        public Code CheckReferenceReplacement(World world, Entity source, EntityDef destination)
        {
            if (_cargo != source.Id) return Code.None;
            if (!ReferenceEquals(world, _world) || !source.Alive || source.Owner != Entity.Owner ||
                !ReferenceEquals(world.Entities.Get(source.Id), source) || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity))
                return Codes.NotAllowed;
            return Def.AcceptsCargoOwner(destination) && destination.HasCapability<IInventory>() && destination.Component<IInventoryDefinition>() != null
                ? Code.None : new Code("proof.cargo-replacement-incompatible");
        }
        public void ReplaceEntityReferences(World world, Entity source, Entity destination)
        {
            if (_cargo != source.Id) return;
            if (!CheckReferenceReplacement(world, source, destination.Def).IsNone || !destination.Alive ||
                destination.Owner != Entity.Owner || !ReferenceEquals(world.Entities.Get(destination.Id), destination) ||
                destination.Get<IInventory>() == null)
                throw new InvalidOperationException("bound cargo replacement requires actual compatible same-company inventory");
            _cargo = destination.Id; Entity.Touch();
        }
        public override void Sync(IStateSync sync) { sync.Entity(ref _cargo); sync.Int(ref _reads); }
    }
    // Rebind only transient session context after load. No gameplay update, dispatch or cargo store.
    internal sealed class BoundCargoContext : GameSystem
    {
        public override string Id => "proof.bound-cargo-context";
        public override int StateSchemaVersion => 1;
        public override void Update(World world, TickInfo tick) { }
        public override void OnNewGame(World world)
        {
            world.Settle();
            var providers = world.Entities.Components<BoundCargo>();
            for (int i = 0; i < providers.Count; i++) providers[i].BindActualCargo(world);
        }
        public override void OnLoaded(World world)
        {
            var providers = world.Entities.Components<BoundCargo>();
            for (int i = 0; i < providers.Count; i++) providers[i].Bind(world);
        }
    }
}
