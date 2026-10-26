using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentStorageDef : ComponentDef<Storage>, IStorageDefinition
    {
        private StorageDeclaration _declaration;
        private string _fault;
        internal Action<string> Observe;
        public ResolvedContent Content { get { Observe?.Invoke("context"); return _fault == "context" ? null : _declaration.Content; } }
        public ICapacityPolicy CapacityPolicy { get { Observe?.Invoke("capacity"); return _fault == "capacity" ? null : _declaration.CapacityPolicy; } }
        public bool Public => _declaration.Public;
        public bool Evacuate => _declaration.Evacuate;
        public Bill InitialStock => _fault == "initial" ? null : _declaration.InitialStock;
        public IReadOnlyList<ResourceLotAmount> InitialLots => _declaration.InitialLots;
        public bool Accepts(ResolvedContent content, ResourceId resource) { Observe?.Invoke("accepts"); return _declaration.Accepts(content, resource); }
        protected override void ReadCore(JsonReader reader) { _declaration = StorageDeclaration.Read(reader); _fault = reader.String("proof_fault", "none"); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class UndeclaredStorageDef : ComponentDef<Storage>, IInventoryDefinition
    {
        private StorageDeclaration _declaration;
        public ICapacityPolicy CapacityPolicy => _declaration.CapacityPolicy;
        public Bill InitialStock => _declaration.InitialStock;
        public bool Public => _declaration.Public;
        public bool Evacuate => _declaration.Evacuate;
        public bool Accepts(ResolvedContent content, ResourceId resource) => _declaration.Accepts(content, resource);
        protected override void ReadCore(JsonReader reader) => _declaration = StorageDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class NonStorageDef : ComponentDef<NonStorage>, IStorageDefinition
    {
        private StorageDeclaration _declaration;
        public ResolvedContent Content => _declaration.Content;
        public ICapacityPolicy CapacityPolicy => _declaration.CapacityPolicy;
        public Bill InitialStock => _declaration.InitialStock;
        public IReadOnlyList<ResourceLotAmount> InitialLots => _declaration.InitialLots;
        public bool Public => _declaration.Public;
        public bool Evacuate => _declaration.Evacuate;
        public bool Accepts(ResolvedContent content, ResourceId resource) => _declaration.Accepts(content, resource);
        protected override void ReadCore(JsonReader reader) => _declaration = StorageDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class NonStorage : Component<NonStorageDef> { }
    public enum StorageBindingFault { Exception, HiddenStock, Touch, PauseClock, AllocateIdentity }
    public sealed class StorageBindingModule : GameModule
    {
        private World _world;
        public bool NativeBinding { get; private set; }
        public int Reads { get; private set; }
        public override string Id => "proof.storage-bindings";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InventoryModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<IndependentStorageDef>("independent_storage", 4);
            types.Component<UndeclaredStorageDef>("undeclared_storage", 4);
            types.Component<NonStorageDef>("non_storage", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            var machine = composer.Content.Get<EntityDef>("machine").Component<IInventoryDefinition>();
            var factory = composer.Content.Get<EntityDef>("contract.factory").Component<IInventoryDefinition>();
            NativeBinding = machine is IndependentStorageDef && factory is IndependentStorageDef &&
                machine.ComponentType == typeof(Storage) && factory.ComponentType == typeof(Storage);
        }
        public void Arm(StorageBindingFault mode)
        {
            foreach (var entity in _world.Entities.All)
                if (entity.Def.Id == "storage.stocked")
                {
                    var inventory = entity.Get<IInventory>();
                    ((IndependentStorageDef)entity.Def.Component<IInventoryDefinition>()).Observe = point => {
                        if (point != "capacity" || Reads != 0) return; Reads++;
                        if (mode == StorageBindingFault.Exception) throw new InvalidOperationException("proof.storage.observation");
                        if (mode == StorageBindingFault.Touch) entity.Touch();
                        if (mode == StorageBindingFault.PauseClock) ((GameClock)_world.Clock).Pause();
                        if (mode == StorageBindingFault.AllocateIdentity) _world.Spawn(_world.Content.Get<EntityDef>("source"), new PlayerId(0));
                        if (mode == StorageBindingFault.HiddenStock)
                        {
                            // Preserve the ACTUAL original reservation codec; rewrite only its stock prefix.
                            using (var original = new MemoryStream())
                            using (var prefix = new MemoryStream())
                            {
                                ((Component)inventory).Sync(new StateWriter(original));
                                var lots = ((IResourceLotEndpoint)inventory).Lots;
                                var writer = new StateWriter(prefix); int n = lots.Count; writer.Count(n);
                                for (int i = 0; i < n; i++)
                                {
                                    var line = lots[i]; ResourceId resource = line.Resource; string lot = line.Lot.Id;
                                    Fix amount = line.Amount + (i == 0 ? Fix.One : Fix.Zero);
                                    writer.Resource(ref resource); writer.String(ref lot); writer.Fix(ref amount);
                                }
                                var bytes = original.ToArray(); prefix.ToArray().CopyTo(bytes, 0);
                                ((Component)inventory).Sync(new StateReader(bytes, 0, bytes.Length));
                            }
                        }
                    };
                    return;
                }
            throw new InvalidOperationException("missing actual stocked inventory probe");
        }
    }
}
