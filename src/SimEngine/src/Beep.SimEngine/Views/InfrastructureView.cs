using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    /// <summary>Detached engine facts; no host-owned pool, capacity or topology calculations.</summary>
    public sealed class NetworkPoolView
    {
        public ResourceEndpointId Endpoint { get; }
        public PlayerId Company { get; }
        public int Version { get; }
        public int MemberCount { get; }
        public bool TopologyPending { get; }
        public IReadOnlyList<ResourceAmount> Stock { get; }
        public IReadOnlyList<ResourceAmount> InboundPromises { get; }
        public IReadOnlyList<ResourceAmount> OutboundPromises { get; }
        public CapacityCheck Capacity { get; }
        public NetworkPoolView(ResourceEndpointId endpoint, PlayerId company, int version, int tiles, bool dirty,
            IReadOnlyList<ResourceAmount> stock, IReadOnlyList<ResourceAmount> incoming, IReadOnlyList<ResourceAmount> outgoing,
            CapacityCheck capacity)
        {
            if (!endpoint.IsValid || version < 0 || tiles < 1 || stock == null || incoming == null || outgoing == null ||
                !System.Enum.IsDefined(typeof(CapacityStatus), capacity.Status))
                throw new System.ArgumentException("network pool facts require actual identity, extent, stock and capacity");
            Endpoint = endpoint; Company = company; Version = version; MemberCount = tiles; TopologyPending = dirty;
            Stock = new List<ResourceAmount>(stock).AsReadOnly(); InboundPromises = new List<ResourceAmount>(incoming).AsReadOnly();
            OutboundPromises = new List<ResourceAmount>(outgoing).AsReadOnly(); Capacity = capacity;
        }
    }
    public sealed class InfrastructureView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal InfrastructureView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private bool Sees(PlayerId owner) => !_viewer.IsValid || owner == _viewer;

        /// <summary>Detached connection readiness using the same typed gate path as physical flow.</summary>
        public Code ReadGates(EntityId entityId, string networkId, out ExecutionGateEvaluation evaluation)
        {
            evaluation = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(entityId);
            if (entity == null || !entity.Alive || !(_viewer == PlayerId.None || entity.Owner == _viewer)) return Codes.NotFound;
            INetworkDefinition network = _world.Content.Find<INetworkDefinition>(networkId);
            if (network == null || !_world.ViewFor(_viewer).CanSeeContent(network)) return Codes.UnknownDef;
            bool supported = false;
            foreach (var component in entity.Components)
                if (component is INetworkPorts ports)
                    foreach (var connection in NetworkPortsContracts.ReadConnections(_world, ports)) if (ReferenceEquals(connection.Network, network)) supported = true;
            if (!supported) return Codes.InvalidArgument;
            evaluation = EntityObservation.ReadProvider(_world, entity, entity,
                () => NetworkGateContracts.Read(_world, entity, network), observeTargetState: true); return Code.None;
        }

        public Code CopyPools(string network, IList<NetworkPoolView> destination)
        {
            if (destination == null) throw new System.ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate();
            var provider = _world.Services.Get<INetworkDiagnostics>();
            var values = new List<NetworkPoolView>();
            Code result = EntityObservation.ReadProvider(_world, provider, null,
                () => provider.CopyPools(network, _viewer, values));
            foreach (var value in values) destination.Add(value); return result;
        }

        public Code ReadConnection(EntityId entity, string network, out NetworkConnectionSnapshot snapshot)
        {
            snapshot = default; _world.RequireHealthyUpdate();
            var provider = _world.Services.Get<INetworkDiagnostics>(); NetworkConnectionSnapshot value = default;
            Code result = EntityObservation.ReadProvider(_world, provider, _world.Entities.Get(entity),
                () => provider.ReadConnection(entity, network, _viewer, out value), observeTargetState: true);
            snapshot = value; return result;
        }

        /// <summary>Actual pure authored admission, not a host-calculated stock target or promised allowance.</summary>
        public Code ReadDemand(string network, EntityId entity, ResourceId resource, bool pull, out NetworkDemandSnapshot demand)
        {
            demand = default; _world.RequireHealthyUpdate();
            var provider = _world.Services.Get<INetworkDiagnostics>(); NetworkDemandSnapshot value = default;
            Code result = EntityObservation.ReadProvider(_world, provider, _world.Entities.Get(entity),
                () => provider.ReadDemand(network, _viewer, entity, resource, pull, out value), observeTargetState: true);
            demand = value; return result;
        }

        /// <summary>Detached private elapsed-service facts; an allowance is not a stock or readiness promise.</summary>
        public Code CopyFlow(string network, IList<NetworkFlowSnapshot> destination)
        {
            if (destination == null) throw new System.ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate();
            var provider = _world.Services.Get<INetworkDiagnostics>(); var values = new List<NetworkFlowSnapshot>();
            Code result = EntityObservation.ReadProvider(_world, provider, null,
                () => provider.CopyFlow(network, _viewer, values));
            foreach (var value in values) destination.Add(value); return result;
        }

        /// <summary>
        /// The path a line would take between two of the company's structures, from ground beside one to ground
        /// beside the other (the tiles nearest each other that the line may use): the whole plan, so a client
        /// never works out endpoints. Clears <paramref name="dst"/> and reports route-search status.
        /// </summary>
        public PathSearchResult SuggestLinkPath(string line, EntityId a, EntityId b, List<TileCoord> dst)
        {
            if (dst == null) throw new System.ArgumentNullException(nameof(dst));
            dst.Clear();
            _world.RequireHealthyUpdate();
            if (!(_world.Space is ITileSpatialSpace map) || !_world.Services.TryGet<ITileLineRouting>(out var lines)) return PathSearchResult.InvalidRequest;
            Entity ea = _world.Entities.Get(a), eb = _world.Entities.Get(b);
            ILineDefinition def = _world.Content.Find<ILineDefinition>(line);
            var view = _world.ViewFor(_viewer);
            if (ea == null || eb == null || !ea.Alive || !eb.Alive || def == null || !Sees(ea.Owner) || !Sees(eb.Owner) ||
                !view.CanSee(ea) || !view.CanSee(eb) || !view.CanSeeContent(def)) return PathSearchResult.InvalidRequest;
            return ReadRoute(lines, def, dst, path => EntityObservation.ReadProvider(_world, ea, ea, () =>
                EntityObservation.ReadProvider(_world, eb, eb, () => {
                    var pa = ea.Get<ITilePresence>(); var pb = eb.Get<ITilePresence>();
                    if (pa == null || pb == null || !pa.Placed || !pb.Placed || pa.Space != map.Id || pb.Space != map.Id)
                        return PathSearchResult.InvalidRequest;
                    List<TileCoord> starts = Beside(ea, eb), ends = Beside(eb, ea);
                    bool budgetExceeded = false;
                    for (int i = 0; i < starts.Count; i++)
                        for (int k = 0; k < ends.Count; k++)
                        {
                            var candidate = new List<TileCoord>();
                            PathSearchResult result = lines.PlanPath(_viewer, def, starts[i], ends[k], candidate);
                            ValidateRoute(result, candidate);
                            if (result == PathSearchResult.Found) { path.AddRange(candidate); return result; }
                            if (result == PathSearchResult.BudgetExceeded) budgetExceeded = true;
                        }
                    return budgetExceeded ? PathSearchResult.BudgetExceeded : PathSearchResult.Unreachable;
                }, observeTargetState: true), observeTargetState: true));
        }

        private PathSearchResult ReadRoute(ITileLineRouting routing, ILineDefinition line, List<TileCoord> destination,
            System.Func<List<TileCoord>, PathSearchResult> read)
        {
            var path = new List<TileCoord>();
            PathSearchResult result = LineDefinitionContracts.Read(_world, line, null, () =>
                EntityObservation.ReadProvider(_world, routing, null, () => {
                    PathSearchResult value = read(path); ValidateRoute(value, path); return value;
                }, space: _world.Space));
            if (result == PathSearchResult.Found) destination.AddRange(path);
            return result;
        }
        private static void ValidateRoute(PathSearchResult result, List<TileCoord> path)
        {
            if (!System.Enum.IsDefined(typeof(PathSearchResult), result) || result == PathSearchResult.Found && path.Count == 0 ||
                result != PathSearchResult.Found && path.Count != 0)
                throw new System.InvalidOperationException("routing provider returned an invalid complete path result");
        }

        /// <summary>The ground tiles around an entity's footprint nearest another entity (the four closest, ties by row then column).</summary>
        private List<TileCoord> Beside(Entity e, Entity toward)
        {
            var map = (ITileSpatialSpace)_world.Space;
            TileRect ring = TileSpatialGeometry.AdjacentGoal(map, e.Get<ITilePresence>().Footprint);
            var tiles = new List<TileCoord>();
            for (int y = ring.Y; y < ring.Bottom; y++)
                for (int x = ring.X; x < ring.Right; x++)
                {
                    var t = new TileCoord(x, y);
                    if (!e.Get<ITilePresence>().Footprint.Contains(t) && map.InBounds(t)) tiles.Add(t);
                }
            TileRect target = toward.Get<ITilePresence>().Footprint;
            tiles.Sort((p, q) =>
            {
                int c = target.StepsTo(p).CompareTo(target.StepsTo(q));
                if (c != 0) return c;
                return p.Y != q.Y ? p.Y.CompareTo(q.Y) : p.X.CompareTo(q.X);
            });
            if (tiles.Count > 4) tiles.RemoveRange(4, tiles.Count - 4);
            return tiles;
        }

        /// <summary>The path a line would be laid on between two tiles (the same rules the lay-line command checks).</summary>
        public PathSearchResult SuggestLinePath(string line, TileCoord from, TileCoord to, List<TileCoord> dst)
        {
            if (dst == null) throw new System.ArgumentNullException(nameof(dst));
            dst.Clear();_world.RequireHealthyUpdate();
            ILineDefinition definition = _world.Content.Find<ILineDefinition>(line);
            if (definition == null || !_world.ViewFor(_viewer).CanSeeContent(definition) ||
                !_world.Services.TryGet<ITileLineRouting>(out var routing)) return PathSearchResult.InvalidRequest;
            return ReadRoute(routing, definition, dst, path => routing.PlanPath(_viewer, definition, from, to, path));
        }

        /// <summary>Pure engine disposition check, not an instant removal command.</summary>
        public Code PreviewRemoval(string line, IReadOnlyList<SpatialPlacement> path)
        {
            _world.RequireHealthyUpdate();
            ILineDefinition definition = _world.Content.Find<ILineDefinition>(line);
            if (definition == null || !_world.ViewFor(_viewer).CanSeeContent(definition)) return Codes.UnknownDef;
            var topology = _world.Services.Get<INetworkTopology>();
            return LineDefinitionContracts.Read(_world, definition, null, () =>
                EntityObservation.ReadProvider(_world, topology, null,
                    () => topology.CheckRemoval(_viewer, definition, path), space: _world.Space));
        }

    }
}
