using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Explicit authored connection intent; live pool IDs are never content.</summary>
    public sealed class NetworkSignalDef : ComponentDef<NetworkSignal>, INetworkSignalDefinition
    {
        private NetworkSignalDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public INetworkDefinition Network => _declaration.Network;
        public int SignalSchemaVersion => 2;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => NetworkSignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader) => _declaration = NetworkSignalDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }
    /// <summary>Stateless connected facts. Existing diagnostics, stock and automation keep sole ownership.</summary>
    public sealed class NetworkSignal : Component, IAutomationSignalSource
    {
        private INetworkSignalDefinition Binding => (INetworkSignalDefinition)Def;
        public Code CapabilityId => Binding.CapabilityId;
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            if (world == null || signal == null || Entity == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                !ReferenceEquals(Entity.Get<IAutomationSignalSource>(CapabilityId), this) ||
                !ReferenceEquals(world.Content.Find<IAutomationSignalContent>(signal.Id), signal))
                throw new ArgumentException("network signal requires its actual live session and authored signal");
            return NetworkSignalBindingContracts.Observe(world, Binding, Entity, network =>
            {
                var quantity = ResourceQuantitySignals.Definition(world, signal);
                return NetworkDefinitionContracts.Read(world, network, Entity, () =>
                {
                    Code refusal = world.Services.Get<INetworkDiagnostics>().ReadQuantity(Entity.Id, network.Id, Entity.Owner,
                        quantity.Resource.Resource, out var facts);
                    return refusal.IsNone ? ResourceQuantitySignals.Read(world, signal, facts, "network") : signal.Unavailable(refusal);
                });
            });
        }
    }
}
