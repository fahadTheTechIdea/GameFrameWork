using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent metadata delegates the one declaration; original runtime and network own execution/stock.
    public sealed class ConnectedPortDef : ComponentDef<NetworkResourcePort>, INetworkResourcePortDefinition, IComponentCompositionValidation
    {
        private NetworkResourcePortDeclaration _value;
        public bool BindsEntityInventory => false;
        public Code CapabilityId => _value.CapabilityId;
        public INetworkDefinition Network => _value.Network;
        public long MinimumMembers => _value.MinimumMembers;
        public ICapacityPolicy CapacityPolicy => _value.CapacityPolicy;
        public bool Accepts(ResolvedContent content, ResourceId resource) => _value.Accepts(content, resource);
        protected override void ReadCore(JsonReader reader) => _value = NetworkResourcePortDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _value.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _value.ValidateOwner(owner);
        public void ValidateComposition(World world, EntityDef owner) => NetworkResourcePortDeclaration.ValidateBinding(world, owner, this);
    }
    public sealed class ConnectedResourcePortModule : GameModule
    {
        public override string Id => "proof.connected-ports";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(InfrastructureModule.ModuleId, Version), new ModuleRequirement(TransformationModule.ModuleId, Version) });
        public bool SharedOwner { get; private set; }
        // Transient negative-proof event observation only; not simulation state or a resource authority.
        public int CoupledFacts { get; private set; }
        public override void DeclareContent(ContentTypes types)
        { types.Component<ConnectedPortDef>("independent_connected_port", 1); types.Component<AtomicInventoryDef>("atomic_inventory", 2); }
        public override void Compose(WorldComposer composer)
        {
            composer.AddSystem(new AtomicInventoryContext(), 1000);
            SharedOwner = ReferenceEquals(composer.Services.Get<INetworkResourceAccess>(), composer.Services.Get<INetworkTopology>()) &&
                ReferenceEquals(composer.Services.Get<INetworkResourceAccess>(), composer.Services.Get<INetworkDiagnostics>());
            CoupledFacts = 0;
            composer.Events.Subscribe<ResourceFlowFact>(Id + ".atomic-observation", fact => { if (fact.Cause == new Code("atomic.equal")) CoupledFacts++; });
        }
    }
    // Rebind transient callback context only. No inventory, gameplay update or saved progress.
    internal sealed class AtomicInventoryContext : GameSystem
    {
        public override string Id => "proof.connected-ports.inventory-context";
        public override int StateSchemaVersion => 1;
        public override void Update(World world, TickInfo tick) { }
        public override void OnLoaded(World world)
        {
            var components = world.Entities.Components<AtomicInventory>();
            for (int i = 0; i < components.Count; i++) components[i].BindSession(world);
        }
    }
}
