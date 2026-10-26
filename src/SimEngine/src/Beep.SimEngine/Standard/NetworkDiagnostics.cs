using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    /// <summary>Detached settled connection identity; never a pool or geometry handle.</summary>
    public readonly struct NetworkConnectionSnapshot
    {
        public ResourceEndpointId Endpoint { get; }
        public PlayerId Company { get; }
        public int Version { get; }
        public NetworkConnectionSnapshot(ResourceEndpointId endpoint, PlayerId company, int version)
        {
            if (!endpoint.IsValid || version < 0) throw new ArgumentException("connection facts require actual identity and version");
            Endpoint = endpoint; Company = company; Version = version;
        }
    }
    /// <summary>Pure engine-side facts, never mutable pools, topology acquisition or another stock owner.</summary>
    public interface INetworkDiagnostics
    {
        Code ReadConnection(EntityId connection, string network, PlayerId viewer, out NetworkConnectionSnapshot snapshot);
        Code ReadDemand(string network, PlayerId viewer, EntityId entity, ResourceId resource, bool pull, out NetworkDemandSnapshot demand);
        Code CopyPools(string network, PlayerId viewer, IList<NetworkPoolView> destination);
        Code CopyFlow(string network, PlayerId viewer, IList<NetworkFlowSnapshot> destination);
        Code ReadQuantity(EntityId connection, string network, PlayerId viewer, ResourceId resource,
            out ResourceQuantitySnapshot quantity);
    }

    public sealed partial class NetworkService : INetworkDiagnostics
    {
        public Code ReadDemand(string network, PlayerId viewer, EntityId entityId, ResourceId resource, bool pull, out NetworkDemandSnapshot demand)
        {
            demand = default;
            Code connection = ReadQuantity(entityId, network, viewer, resource, out _);
            if (!connection.IsNone) return connection;
            Entity entity = _world.Entities.Get(entityId);
            NetState state = _nets[network];
            if (!NetworkDefinitionContracts.Read(_world, state.Def, entity, () => state.Def.Carries(resource))) return Codes.InvalidArgument;
            ExecutionGateEvaluation gate = NetworkGateContracts.Read(_world, entity, state.Def);
            if (!gate.Blocker.IsNone) { demand = new NetworkDemandSnapshot(Fix.Zero, gate.Blocker); return Code.None; }
            foreach (FlowLeg leg in CaptureFlowLegs(_world, network, entityId))
                if (leg.Resource == resource && leg.Pull == pull)
                { demand = ReadDemand(leg, Fix.FromRaw(long.MaxValue)); return Code.None; }
            return Codes.InvalidArgument;
        }
        public static readonly Code TopologyPending = new Code("network.topology-pending"),
            Disconnected = new Code("network.disconnected");

        public Code CopyPools(string network, PlayerId viewer, IList<NetworkPoolView> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            _world.RequireHealthyUpdate();
            if (viewer != PlayerId.None && !_world.Players.IsSeat(viewer)) return Codes.NotSeated;
            if (network == null || !_nets.TryGetValue(network, out var state) || !_world.ViewFor(viewer).CanSeeContent(state.Def))
                return Codes.UnknownDef;
            var values = EntityObservation.ReadProvider(_world, this, null, () => {
                var pools = new List<NetworkPoolView>();
                foreach (NetComponent component in state.Components)
                    if (!viewer.IsValid || component.Owner == viewer || component.Owner == PlayerId.None && state.Def.Public)
                        pools.Add(CapturePool(state, component));
                return pools;
            });
            foreach (var value in values) destination.Add(value);
            return Code.None;
        }
        private NetworkPoolView CapturePool(NetState state, NetComponent component)
        {
            int version = component.Version; bool dirty = state.Dirty;
            var inventory = component.Inventory;
            var stock = inventory.Stock;
            var incoming = inventory.InboundPromises;
            var outgoing = inventory.OutboundPromises;
            CapacityCheck capacity = inventory.CheckIncoming(Array.Empty<ResourceAmount>());
            var result = new NetworkPoolView(component.Endpoint, component.Owner, version, component.MemberCount, dirty, stock, incoming, outgoing, capacity);
            if (component.Version != version || state.Dirty != dirty)
                throw new InvalidOperationException("network pool mutated or returned malformed capacity during observation");
            return result;
        }
        public Code ReadQuantity(EntityId connection, string network, PlayerId viewer, ResourceId resource,
            out ResourceQuantitySnapshot quantity)
        {
            quantity = default;
            ResourceQuantitySnapshot captured = default;
            Code refusal = ObserveConnection(connection, network, viewer, _world.Entities.Get(connection)?.Def,
                pool => ResourceQuantitySignals.Capture(_world, pool.Inventory, resource, out captured));
            if (refusal.IsNone) quantity = captured;
            return refusal;
        }
        public Code ReadConnection(EntityId connection, string network, PlayerId viewer, out NetworkConnectionSnapshot snapshot)
        {
            snapshot = default; NetworkConnectionSnapshot captured = default;
            Code refusal = ObserveConnection(connection, network, viewer, _world.Entities.Get(connection)?.Def, pool => {
                captured = new NetworkConnectionSnapshot(pool.Endpoint, pool.Owner, pool.Version); return Code.None;
            });
            if (refusal.IsNone) snapshot = captured;
            return refusal;
        }
        private Code ObserveConnection(EntityId connection, string network, PlayerId viewer, EntityDef footprint, Func<NetComponent, Code> observe)
        {
            _world.RequireHealthyUpdate();
            if (viewer != PlayerId.None && !_world.Players.IsSeat(viewer)) return Codes.NotSeated;
            Entity entity = _world.Entities.Get(connection);
            if (entity == null || !entity.Alive || viewer.IsValid && entity.Owner != viewer) return Codes.NotFound;
            if (footprint == null || !ReferenceEquals(_world.Content.Find<EntityDef>(footprint.Id), footprint)) return Codes.UnknownDef;
            if (network == null || !_nets.TryGetValue(network, out var state) || !_world.ViewFor(viewer).CanSeeContent(state.Def))
                return Codes.UnknownDef;
            int beforeGeometry = state.Version.Geometry, beforeOwnership = state.Version.Ownership;
            Code refusal = EntityObservation.ReadProvider(_world, this, entity, () =>
            {
                bool supported = false;
                var definition = footprint.Component<INetworkPortsDefinition>();
                if (definition == null) return Codes.InvalidArgument;
                var connections = ReferenceEquals(footprint, entity.Def)
                    ? NetworkPortsContracts.ReadConnections(_world, entity.Get<INetworkPorts>())
                    : EntityObservation.ReadProvider(_world, definition, entity,
                        () => NetworkPortsContracts.ValidateDefinition(_world.Content, footprint, definition), _world.Space, observeTargetState: true);
                foreach (PortSpec spec in connections)
                {
                    if (spec.Network == null || !ReferenceEquals(_world.Content.Find<INetworkDefinition>(spec.Network.Id), spec.Network))
                        throw new InvalidOperationException("network diagnostic received a foreign connection");
                    if (ReferenceEquals(spec.Network, state.Def)) supported = true;
                }
                if (!supported) return Codes.InvalidArgument;
                if (state.Dirty) return TopologyPending;
                int index = ComponentOf(network, entity, footprint);
                if (index < 0) return Disconnected;
                NetComponent pool = state.Components[index];
                if (pool.Owner != entity.Owner && !(pool.Owner == PlayerId.None && state.Def.Public)) return Codes.NotOwned;
                int geometry = state.Version.Geometry, ownership = state.Version.Ownership;
                Code result = observe(pool);
                if (state.Version.Geometry != geometry || state.Version.Ownership != ownership || state.Dirty ||
                    !ReferenceEquals(state.Components[index], pool))
                    throw new InvalidOperationException("network topology mutated a pure quantity observation");
                return result;
            }, observeTargetState: true);
            if (state.Version.Geometry != beforeGeometry || state.Version.Ownership != beforeOwnership)
                throw new InvalidOperationException("connection provider mutated network topology during observation");
            return refusal;
        }
    }
}
