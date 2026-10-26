using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Authored valve readiness, not a second network/stock/state owner.</summary>
    public sealed class SignalNetworkGateDef : ComponentDef<SignalNetworkGate>, ISignalNetworkGateDefinition
    {
        private SignalNetworkGateDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public IReadOnlyList<INetworkDefinition> Networks => _declaration.Networks;
        public IReadOnlyList<SignalGateCondition> Conditions => _declaration.Conditions;
        protected override void ReadCore(JsonReader reader) => _declaration = SignalNetworkGateDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }
    public sealed class SignalNetworkGate : Component, INetworkFlowGate
    {
        private ISignalNetworkGateDefinition Binding => (ISignalNetworkGateDefinition)Def;
        public Code CapabilityId => Binding.CapabilityId;
        public ExecutionGateSnapshot ReadGate(World world, INetworkDefinition network)
        {
            if (world == null || Entity == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                !ReferenceEquals(Entity.Get<INetworkFlowGate>(CapabilityId), this) || network == null ||
                !ReferenceEquals(world.Content.Find<INetworkDefinition>(network.Id), network))
                throw new ArgumentException("network gate requires its actual live entity and catalog network");
            return NetworkGateBindingContracts.Observe(world, Binding, Entity, (networks, conditions) => {
                bool applies = false;
                foreach (var definition in networks) if (ReferenceEquals(definition, network)) applies = true;
                return SignalGateCondition.ReadGate(world, Entity, CapabilityId, applies, conditions);
            });
        }
    }
    internal static class NetworkGateContracts
    {
        internal static ExecutionGateEvaluation Read(World world, Entity entity, INetworkDefinition network)
        {
            if (network == null || !ReferenceEquals(world.Content.Find<INetworkDefinition>(network.Id), network))
                throw new InvalidOperationException("foreign network gate definition");
            return NetworkDefinitionContracts.Read(world, network, entity,
                () => ExecutionGateContracts.Read<INetworkFlowGate>(world, entity, gate => gate.ReadGate(world, network)));
        }
    }
}
