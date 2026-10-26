using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class NetworkService
    {
        public static readonly Code ReplacementBinding = new Code("network.replacement-binding");
        public static readonly Code ReplacementEndpoint = new Code("network.replacement-endpoint");

        private static bool DeclaresFlow(IReadOnlyList<PortSpec> connections, INetworkDefinition network, FlowCursor cursor)
        {
            foreach (var spec in connections)
                if (ReferenceEquals(spec.Network, network))
                    foreach (var resource in cursor.Pull ? spec.Pull : spec.Push)
                        if (resource == cursor.Resource) return true;
            return false;
        }
        public Code CheckReferenceReplacement(World world, Entity source, EntityDef destination)
        {
            if (!ReferenceEquals(world, _world)) throw new ArgumentException("network references belong to another world", nameof(world));
            var endpoint = ResourceEndpointId.ForEntity(source.Id);
            foreach (NetState state in _netList)
                foreach (FlowCursor cursor in state.Flow)
                {
                    if (cursor.Entity != source.Id)
                    {
                        if (cursor.Source == endpoint || cursor.Destination == endpoint) return ReplacementEndpoint;
                        continue;
                    }
                    if ((cursor.Pull ? cursor.Destination : cursor.Source) != endpoint) return ReplacementEndpoint;
                    var current = source.Get<INetworkPorts>() as Component;
                    var next = destination.Component<INetworkPortsDefinition>();
                    if (current == null || next is not ComponentDef part || current.Def.Key != part.Key ||
                        !Upgrades.CanRetainState(current.Def, part) ||
                        !DeclaresFlow(NetworkPortsContracts.ValidateDefinition(world.Content, destination, next), state.Def, cursor))
                        return ReplacementBinding;
                    var inventory = destination.Component<IInventoryDefinition>();
                    if (inventory == null || !inventory.Accepts(world.Content, cursor.Resource) ||
                        !destination.HasCapability<IInletPort>() || !cursor.Pull && !destination.HasCapability<IOutletPort>())
                        return ReplacementBinding;
                    var pool = Resolve(cursor.Pull ? cursor.Source : cursor.Destination) as IResourceInlet;
                    if (pool == null) return ReplacementBinding;
                    var quantum = CapacityQuantum.Combine(CapacityPolicyContracts.Quantum(inventory.CapacityPolicy, cursor.Resource),
                        CapacityPolicyContracts.Quantum(pool.CapacityPolicy, cursor.Resource));
                    if (quantum.Status != CapacityStatus.Fits || quantum.Raw != cursor.Quantum) return ReplacementBinding;
                }
            return Code.None;
        }
        public void ReplaceEntityReferences(World world, Entity source, Entity destination)
        {
            if (!CheckReferenceReplacement(world, source, destination.Def).IsNone)
                throw new InvalidOperationException("network replacement binding changed after admission");
            var retained = new List<FlowCursor>(); var endpoint = ResourceEndpointId.ForEntity(destination.Id);
            foreach (NetState state in _netList)
                foreach (FlowCursor cursor in state.Flow)
                    if (cursor.Entity == source.Id)
                    {
                        var ports = destination.Get<INetworkPorts>();
                        if (ports == null || !DeclaresFlow(NetworkPortsContracts.ReadConnections(world, ports), state.Def, cursor))
                            throw new InvalidOperationException("replacement lost actual network flow connection");
                        IResourceInlet actor = cursor.Pull ? destination.Get<IInletPort>() : destination.Get<IOutletPort>();
                        var pool = Resolve(cursor.Pull ? cursor.Source : cursor.Destination) as IResourceInlet;
                        if (actor == null || actor.Endpoint != endpoint || actor.Company != destination.Owner ||
                            !actor.Accepts(world.Content, cursor.Resource) || pool == null)
                            throw new InvalidOperationException("replacement lost actual network flow endpoint");
                        var quantum = CapacityQuantum.Combine(CapacityPolicyContracts.Quantum(actor.CapacityPolicy, cursor.Resource),
                            CapacityPolicyContracts.Quantum(pool.CapacityPolicy, cursor.Resource));
                        if (quantum.Status != CapacityStatus.Fits || quantum.Raw != cursor.Quantum)
                            throw new InvalidOperationException("replacement changed actual network flow quantum");
                        retained.Add(cursor);
                    }
            foreach (var cursor in retained)
            {
                cursor.Entity = destination.Id;
                if (cursor.Pull) cursor.Destination = endpoint; else cursor.Source = endpoint;
            }
            foreach (NetState state in _netList) state.Flow.Sort(CompareFlow);
            // FlowTurn is successful-service history/order, not a live actor binding.
            // Elapsed, LastTick, Quantum and actual pool identities are deliberately unchanged.
        }
    }
}
