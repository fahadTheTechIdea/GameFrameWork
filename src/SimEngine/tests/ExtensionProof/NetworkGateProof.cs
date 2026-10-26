using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent runtime provider; the common authoring owner still resolves its connections.</summary>
    public sealed class ProofNetworkPortsDef : ComponentDef<ProofNetworkPorts>, INetworkPortsDefinition
    {
        private NetworkPortsDeclaration _declaration;
        public IReadOnlyList<PortSpec> Connections => _declaration.Connections;
        protected override void ReadCore(JsonReader reader) => _declaration = NetworkPortsDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }
    public sealed class ProofNetworkPorts : Component<ProofNetworkPortsDef>, INetworkPorts
    {
        public Action ReadProbe;
        public IReadOnlyList<PortSpec> Connections { get { ReadProbe?.Invoke(); return Def.Connections; } }
    }

    /// <summary>Fault injection only; no alternate flow, stock or incident rules.</summary>
    public sealed class NetworkGateProbeDef : ComponentDef<NetworkGateProbe>, INamedCapabilityDefinition
    {
        public Code CapabilityId { get; private set; }
        public int Mode { get; private set; }
        public INetworkDefinition Network { get; private set; }
        private string _network;
        protected override void ReadCore(JsonReader reader)
        { CapabilityId = new Code(reader.String("id")); _network = reader.String("network"); Mode = reader.Int("mode"); }
        protected override void ResolveCore(ContentResolver content) => Network = content.Def<INetworkDefinition>(_network);
    }
    public sealed class NetworkGateProbe : Component<NetworkGateProbeDef>, INetworkFlowGate
    {
        private bool _changedIdentity;
        public Code CapabilityId => _changedIdentity ? new Code("changed-identity") : Def.CapabilityId;
        public ExecutionGateSnapshot ReadGate(World world, INetworkDefinition network)
        {
            bool applies = ReferenceEquals(network, Def.Network);
            if (!applies) return new ExecutionGateSnapshot(CapabilityId, false, Code.None, Array.Empty<ExecutionGateConditionSnapshot>());
            if (Def.Mode == 1) Entity.Touch();
            if (Def.Mode == 2) return null;
            if (Def.Mode == 3) return new ExecutionGateSnapshot(new Code("forged"), true, Code.None, Array.Empty<ExecutionGateConditionSnapshot>());
            if (Def.Mode == 4) return new ExecutionGateSnapshot(CapabilityId, false, new Code("probe.invalid"), Array.Empty<ExecutionGateConditionSnapshot>());
            if (Def.Mode == 5)
            {
                AutomationSignalDef signal = world.Content.Get<AutomationSignalDef>("network.incident.active");
                var value = signal.ReadFrom(world, Entity.Get<IAutomationSignalSource>(new Code("sensor")));
                return new ExecutionGateSnapshot(CapabilityId, true, Code.None, new[] {
                    new ExecutionGateConditionSnapshot("bad", new Code("sensor"), value, false, new Code("probe.blocked")) });
            }
            if (Def.Mode == 6)
            {
                var snapshot = new ExecutionGateSnapshot(CapabilityId, true, Code.None, Array.Empty<ExecutionGateConditionSnapshot>());
                _changedIdentity = true; return snapshot;
            }
            return new ExecutionGateSnapshot(CapabilityId, true, Code.None, Array.Empty<ExecutionGateConditionSnapshot>());
        }
    }
}
