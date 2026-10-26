using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Detached earned-time facts, not stored resources or banked transfer credit.</summary>
    public sealed class NetworkFlowSnapshot
    {
        public EntityId Entity { get; }
        public ResourceId Resource { get; }
        public bool Pull { get; }
        public ResourceEndpointId Source { get; }
        public ResourceEndpointId Destination { get; }
        public long ElapsedTicks { get; }
        public long LastTick { get; }
        public Fix NextAllowance { get; }
        public Code Blocker { get; }
        public NetworkFlowSnapshot(EntityId entity, ResourceId resource, bool pull, ResourceEndpointId source,
            ResourceEndpointId destination, long elapsed, long lastTick, Fix next, Code blocker)
        {
            if (!entity.IsValid || !resource.IsValid || !source.IsValid || !destination.IsValid || source == destination ||
                elapsed < 0 || lastTick <= 0 || elapsed > lastTick || next < Fix.Zero || !blocker.IsNone && !next.IsZero)
                throw new ArgumentException("network flow facts require actual distinct endpoints, earned time and nonnegative unblocked allowance");
            Entity = entity; Resource = resource; Pull = pull; Source = source; Destination = destination;
            ElapsedTicks = elapsed; LastTick = lastTick; NextAllowance = next; Blocker = blocker; }
    }

    // Same graph/flow owner and Sync, split by responsibility for readability. No separate service.
    public sealed partial class NetworkService
    {
        public static readonly Code FlowRange = new Code("sim.network.flow-range");
        // Transient execution scope of this existing owner, never another elapsed-time counter.
        private readonly SimulationOperation _flowOperation = new SimulationOperation();
        internal sealed class FlowCursor
        {
            public EntityId Entity;
            public ResourceId Resource;
            public bool Pull;
            public ResourceEndpointId Source, Destination;
            public long Quantum, Elapsed, LastTick;
        }
        internal sealed class FlowTurn
        {
            public ResourceId Resource;
            public bool Pull;
            public EntityId Entity;
            public long Tick; // Last success timestamp from the sole game clock, never a running counter.
        }
        private sealed class FlowLeg
        {
            public NetState State;
            public NetComponent Component;
            public Entity Entity;
            public ResourceId Resource;
            public bool Pull;
            public IResourceOutlet Source;
            public IResourceInlet Destination;
            public CapacityQuantum Quantum;
            public Fix Budget;
            public long Elapsed, Rank;
        }
        private sealed class FlowBatch
        {
            public NetState State;
            public NetComponent Component;
            public ResourceId Resource;
            public bool Pull;
            public readonly List<FlowLeg> Legs = new List<FlowLeg>();
        }
        private sealed class FlowReadGuard
        {
            private readonly FlowLeg _leg;
            private readonly EntityObservation.ProviderScope _entityState, _poolState;
            private readonly int _entity, _source, _destination;
            private readonly ResourceEndpointId _from, _to;
            private readonly PlayerId _company, _fromCompany, _toCompany;
            public FlowReadGuard(World world, FlowLeg leg)
            {
                _entityState = EntityObservation.CaptureProvider(world, leg.Entity, leg.Entity.Sync, "network.policy-read");
                _poolState = EntityObservation.CaptureProvider(world, leg.Component.Inventory, leg.Component.Inventory.Sync, "network.policy-read");
                _leg = leg; _entity = leg.Entity.Version; _source = leg.Source.Version; _destination = leg.Destination.Version;
                _from = leg.Source.Endpoint; _to = leg.Destination.Endpoint;
                _company = leg.Entity.Owner; _fromCompany = leg.Source.Company; _toCompany = leg.Destination.Company;
            }
            public void Validate(World world, Entity changedEntity = null, IResourceEndpoint changedSource = null, IResourceEndpoint changedDestination = null)
            {
                if (!_leg.Entity.Alive || !ReferenceEquals(world.Entities.Get(_leg.Entity.Id), _leg.Entity) ||
                    _leg.Entity.Version != _entity && !ReferenceEquals(_leg.Entity, changedEntity) || _leg.Entity.Owner != _company ||
                    _leg.Source.Version != _source && !ReferenceEquals(_leg.Source, changedSource) && !ReferenceEquals(_leg.Source, changedDestination) ||
                    _leg.Destination.Version != _destination && !ReferenceEquals(_leg.Destination, changedSource) && !ReferenceEquals(_leg.Destination, changedDestination) ||
                    _leg.Source.Endpoint != _from || _leg.Destination.Endpoint != _to ||
                    _leg.Source.Company != _fromCompany || _leg.Destination.Company != _toCompany)
                    throw new InvalidOperationException("network arbitration mutated a participating entity/endpoint");
                if (!ReferenceEquals(_leg.Entity, changedEntity)) _entityState.Verify();
                if (!ReferenceEquals(_leg.Component.Inventory, changedSource) && !ReferenceEquals(_leg.Component.Inventory, changedDestination))
                    _poolState.Verify();
            }
        }
        private static int CompareTurns(FlowTurn a, FlowTurn b)
        { int value = a.Resource.Value.CompareTo(b.Resource.Value); return value != 0 ? value : a.Pull.CompareTo(b.Pull); }
        private static FlowTurn Turn(NetComponent component, ResourceId resource, bool pull)
        { foreach (FlowTurn turn in component.FlowTurns) if (turn.Resource == resource && turn.Pull == pull) return turn; return null; }
        private static void Served(FlowLeg leg, long tick)
        {
            FlowTurn turn = Turn(leg.Component, leg.Resource, leg.Pull);
            if (turn == null)
            {
                turn = new FlowTurn { Resource = leg.Resource, Pull = leg.Pull };
                leg.Component.FlowTurns.Add(turn); leg.Component.FlowTurns.Sort(CompareTurns);
            }
            turn.Entity = leg.Entity.Id; turn.Tick = tick;
        }
        private static int CompareFlow(FlowCursor a, FlowCursor b)
        {
            int value = a.Entity.Value.CompareTo(b.Entity.Value); if (value != 0) return value;
            value = a.Pull.CompareTo(b.Pull); return value != 0 ? value : a.Resource.Value.CompareTo(b.Resource.Value);
        }
        private static Code Allowance(IResourceRate rate, ISimulationTime time, long elapsed, long quantum, out Fix amount)
        {
            amount = Fix.Zero;
            try
            {
                long next = checked(elapsed + 1);
                BigInteger previous = QuantizedGameRates.AccumulatedRaw(rate.Quantity, rate.PeriodNumerator,
                    rate.PeriodDenominator, rate.PeriodUnit, time, new WorkProgress(elapsed), quantum, false);
                BigInteger following = QuantizedGameRates.AccumulatedRaw(rate.Quantity, rate.PeriodNumerator,
                    rate.PeriodDenominator, rate.PeriodUnit, time, new WorkProgress(next), quantum, false);
                amount = Fix.FromRaw(checked((long)(following - previous))); return Code.None;
            }
            catch (OverflowException) { return FlowRange; }
        }
        private Fix AdvanceFlow(FlowLeg leg, long endTick)
        {
            FlowCursor cursor = null;
            foreach (FlowCursor item in leg.State.Flow)
                if (item.Entity == leg.Entity.Id && item.Pull == leg.Pull && item.Resource == leg.Resource) { cursor = item; break; }
            if (cursor == null)
            {
                cursor = new FlowCursor { Entity = leg.Entity.Id, Resource = leg.Resource, Pull = leg.Pull };
                leg.State.Flow.Add(cursor);
            }
            if (cursor.LastTick != _world.Tick || cursor.Source != leg.Source.Endpoint ||
                cursor.Destination != leg.Destination.Endpoint || cursor.Quantum != leg.Quantum.Raw)
                cursor.Elapsed = 0;
            cursor.Source = leg.Source.Endpoint; cursor.Destination = leg.Destination.Endpoint; cursor.Quantum = leg.Quantum.Raw;
            Code refusal = Allowance(NetworkDefinitionContracts.Read(_world, leg.State.Def, leg.Entity,
                () => leg.State.Def.Rate(leg.Resource)), _world.Time, cursor.Elapsed, cursor.Quantum, out Fix amount);
            cursor.LastTick = endTick;
            if (refusal.IsNone) cursor.Elapsed = checked(cursor.Elapsed + 1);
            leg.Elapsed = cursor.Elapsed;
            return amount; // Range is exposed by the same derived snapshot; no invented quantity.
        }
        private static Fix ReadyAmount(World world, Entity entity, IResourceOutlet source, IResourceInlet destination,
            ResourceId resource, Fix ceiling, out CapacityQuantum quantum)
        {
            int entityVersion = entity.Version, sourceVersion = source.Version, destinationVersion = destination.Version;
            ResourceEndpointId from = source.Endpoint, to = destination.Endpoint;
            PlayerId sourceOwner = source.Company, destinationOwner = destination.Company;
            quantum = CapacityQuantum.Combine(CapacityPolicyContracts.Quantum(source.CapacityPolicy, resource), CapacityPolicyContracts.Quantum(destination.CapacityPolicy, resource));
            bool accepts = destination.Accepts(world.Content, resource);
            Fix available = source.Available(resource);
            Fix offered = accepts ? destination.Offer(resource, Fix.Min(ceiling, available)) : Fix.Zero;
            if (entity.Version != entityVersion || source.Version != sourceVersion || destination.Version != destinationVersion ||
                !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) || from != source.Endpoint || to != destination.Endpoint ||
                sourceOwner != source.Company || destinationOwner != destination.Company)
                throw new InvalidOperationException("network readiness mutated actual endpoint metadata/state");
            if (!Enum.IsDefined(typeof(CapacityStatus), quantum.Status) || available < Fix.Zero || offered < Fix.Zero ||
                offered > Fix.Min(ceiling, available) || UnitConversion.ValidateAmount(world.Content.Resource(resource).QuantityUnit, available) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(world.Content.Resource(resource).QuantityUnit, offered) != UnitConversionStatus.Converted)
                throw new InvalidOperationException("network readiness reported invalid native stock/capacity");
            return accepts && quantum.Status == CapacityStatus.Fits ? quantum.Floor(offered) : Fix.Zero;
        }
        private List<FlowLeg> CaptureFlowLegs(World world, string network = null, EntityId? entityId = null)
        {
            var legs = new List<FlowLeg>(); ComponentList<INetworkPorts> ports = world.Entities.Components<INetworkPorts>();
            foreach (NetState state in _netList)
            {
                if (network != null && state.Def.Id != network) continue;
                if (state.Components.Count == 0) continue;
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int k = 0; k < ports.Count; k++)
                    {
                        INetworkPorts port = ports[k]; Entity entity = port.Entity;
                        if (entityId.HasValue && entity.Id != entityId.Value) continue;
                        if (!entity.Alive) continue;
                        IReadOnlyList<PortSpec> connections = NetworkPortsContracts.ReadConnections(world, port);
                        foreach (PortSpec spec in connections)
                        {
                            if (!ReferenceEquals(spec.Network, state.Def)) continue;
                            int index = ComponentOf(state.Def.Id, entity, entity.Def); if (index < 0) continue;
                            NetComponent component = state.Components[index];
                            if (component.Owner == PlayerId.None && !component.Inventory.Public ||
                                !NetworkGateContracts.Read(world, entity, state.Def).Blocker.IsNone) continue;
                            IResourceOutlet source = pass == 0 ? entity.Get<IOutletPort>() : component.Inventory;
                            IResourceInlet destination = pass == 0 ? component.Inventory : entity.Get<IInletPort>();
                            if (source == null || destination == null) continue;
                            foreach (ResourceId resource in pass == 0 ? spec.Push : spec.Pull)
                            {
                                if (NetworkDefinitionContracts.Read(world, state.Def, entity, () => state.Def.Rate(resource)) == null)
                                    throw new InvalidOperationException("network connection has an unauthored resource rate");
                                CapacityQuantum quantum = default;
                                NetworkDefinitionContracts.Read(world, state.Def, entity,
                                    () => ReadyAmount(world, entity, source, destination, resource, Fix.FromRaw(long.MaxValue), out quantum));
                                if (quantum.Status != CapacityStatus.Fits) continue;
                                foreach (FlowLeg previous in legs)
                                    if (ReferenceEquals(previous.State, state) && previous.Entity.Id == entity.Id && previous.Pull == (pass != 0) && previous.Resource == resource)
                                        throw new InvalidOperationException("duplicate network flow capability/connection");
                                legs.Add(new FlowLeg { State = state, Component = component, Entity = entity, Resource = resource, Pull = pass != 0,
                                    Source = source, Destination = destination, Quantum = quantum });
                            }
                        }
                    }
                }
            }
            return legs;
        }
        /// <summary>Capture readiness before any transfer; newly pushed stock earns no same-step pull time.</summary>
        public void Flow()
        {
            World world = _world;
            world.RequireHealthyUpdate();
            if (_flowOperation.Active) throw new InvalidOperationException("network flow cannot recursively advance itself");
            using var operation = _flowOperation.Enter();
            long endTick = checked(world.Tick + 1);
            List<FlowLeg> legs = CaptureFlowLegs(world);
            var batches = new List<FlowBatch>();
            var guards = new List<FlowReadGuard>(); foreach (FlowLeg leg in legs) guards.Add(new FlowReadGuard(world, leg));
            foreach (FlowLeg leg in legs)
            {
                foreach (FlowReadGuard guard in guards) guard.Validate(world);
                NetworkDemandSnapshot demand = ReadDemand(leg, Fix.FromRaw(long.MaxValue));
                foreach (FlowReadGuard guard in guards) guard.Validate(world);
                if (!demand.Maximum.IsPositive) continue;
                leg.Budget = Fix.Min(AdvanceFlow(leg, endTick), demand.Maximum); if (!leg.Budget.IsPositive) continue;
                FlowBatch batch = null;
                foreach (FlowBatch candidate in batches)
                    if (ReferenceEquals(candidate.Component, leg.Component) && candidate.Resource == leg.Resource && candidate.Pull == leg.Pull)
                    { batch = candidate; break; }
                if (batch == null)
                {
                    batch = new FlowBatch { State = leg.State, Component = leg.Component, Resource = leg.Resource, Pull = leg.Pull };
                    batches.Add(batch);
                }
                batch.Legs.Add(leg);
            }
            // Rank all detached candidates before any transfer, guarding every participating owner.
            foreach (FlowBatch batch in batches)
            {
                EntityId last = Turn(batch.Component, batch.Resource, batch.Pull)?.Entity ?? EntityId.None;
                foreach (FlowLeg leg in batch.Legs)
                {
                    foreach (FlowReadGuard guard in guards) guard.Validate(world);
                    leg.Rank = NetworkDefinitionContracts.Read(world, batch.State.Def, leg.Entity,
                        () => NetworkPolicyContracts.Read(world, batch.State.Def.FlowPolicy, leg.Entity,
                            () => batch.State.Def.FlowPolicy.Rank(new NetworkFlowCandidate(leg.Entity.Id, leg.Entity.Owner,
                                leg.Resource, leg.Pull, leg.Budget, leg.Quantum.Raw, leg.Elapsed), last)));
                    foreach (FlowReadGuard guard in guards) guard.Validate(world);
                }
                batch.Legs.Sort((a, b) => { int value = a.Rank.CompareTo(b.Rank); return value != 0 ? value : a.Entity.Id.Value.CompareTo(b.Entity.Id.Value); });
            }
            batches.Sort((a, b) =>
            {
                int value = StringComparer.Ordinal.Compare(a.State.Def.Id, b.State.Def.Id); if (value != 0) return value;
                value = a.Component.Endpoint.Identity.CompareTo(b.Component.Endpoint.Identity); if (value != 0) return value;
                value = a.Pull.CompareTo(b.Pull); return value != 0 ? value : a.Resource.Value.CompareTo(b.Resource.Value);
            });
            Action validate = () => { foreach (FlowReadGuard guard in guards) guard.Validate(world); };
            var networks = new List<NetState>(_netList); networks.Sort((a, b) => StringComparer.Ordinal.Compare(a.Def.Id, b.Def.Id));
            foreach (NetState state in networks)
            {
                var requests = new List<NetworkFlowRequest>(); var binding = new Dictionary<NetworkFlowRequest, FlowLeg>();
                foreach (FlowBatch batch in batches)
                    if (ReferenceEquals(batch.State, state)) foreach (FlowLeg leg in batch.Legs)
                    {
                        var request = new NetworkFlowRequest(new NetworkFlowCandidate(leg.Entity.Id, leg.Entity.Owner, leg.Resource,
                            leg.Pull, leg.Budget, leg.Quantum.Raw, leg.Elapsed), leg.Source.Endpoint, leg.Destination.Endpoint);
                        requests.Add(request); binding.Add(request, leg);
                    }
                var context = new NetworkExecutionContext(state.Def.Id, endTick, world.Time, requests, validate, (request, maximum) =>
                {
                    FlowLeg current = binding[request]; NetworkFlowAttempt attempt = ExecuteFlow(current, maximum, endTick);
                    foreach (FlowReadGuard guard in guards)
                        if (attempt.Transferred) guard.Validate(world, current.Entity, current.Source, current.Destination);
                        else guard.Validate(world);
                    // Only this verified engine operation can mutate the observation baseline.
                    guards.Clear(); foreach (FlowLeg leg in legs) guards.Add(new FlowReadGuard(world, leg));
                    return attempt;
                });
                try
                {
                    validate();
                    NetworkPolicyContracts.Read(world, state.Def.Solver, null, () => { state.Def.Solver.Advance(context); return true; });
                    validate();
                }
                finally { context.Close(); }
            }
            foreach (NetState state in _netList)
            { state.Flow.RemoveAll(cursor => cursor.LastTick != endTick); state.Flow.Sort(CompareFlow); }
        }
        private NetworkFlowAttempt ExecuteFlow(FlowLeg leg, Fix maximum, long endTick)
        {
            NetworkDemandSnapshot demand = ReadDemand(leg, maximum);
            Fix amount = demand.Maximum;
            if (!amount.IsPositive) return new NetworkFlowAttempt(Fix.Zero,
                demand.Blocker.IsNone ? NetworkExecutionContext.Unavailable : demand.Blocker);
            if (!Transfer(leg.Source, leg.Destination, leg.Entity.Owner, leg.Resource, amount,
                new ReservationOwner("sim.infrastructure.flow", leg.Entity.Id.Value)))
                return new NetworkFlowAttempt(Fix.Zero, NetworkExecutionContext.Unavailable);
            Served(leg, endTick); return new NetworkFlowAttempt(amount, Code.None);
        }
        private NetworkDemandSnapshot ReadDemand(FlowLeg leg, Fix ceiling)
        {
            Fix ready = ReadyAmount(_world, leg.Entity, leg.Source, leg.Destination, leg.Resource, ceiling, out var quantum);
            if (quantum.Raw != leg.Quantum.Raw || quantum.Status != leg.Quantum.Status) throw new InvalidOperationException("network quantum changed during flow");
            if (!ready.IsPositive) return new NetworkDemandSnapshot(Fix.Zero, NetworkExecutionContext.Unavailable);
            var guard = new FlowReadGuard(_world, leg);
            Code ReadUtility(Code consumer, out UtilityDemandSnapshot facts, out ResourceQuantitySnapshot quantity)
            {
                facts = UtilityDemandSnapshot.None; quantity = default;
                if (!leg.Pull) return Codes.InvalidArgument;
                IUtilityConsumer provider = leg.Entity.Get<IUtilityConsumer>(consumer);
                if (provider == null) return Codes.NotFound;
                facts = UtilityContracts.Read(_world, provider);
                IResourcePort port = provider.ResolveUtilityPort(_world, leg.Resource);
                if (port == null) return ResourceSignalCodes.Incompatible;
                var binding=ResourcePortContracts.Bind(_world,leg.Entity,leg.Resource,port);
                if(!binding.Ready)return binding.Blocker;
                IResourceOutlet store = binding.Store;
                if (!ReferenceEquals(store, leg.Destination)) return ResourceSignalCodes.Incompatible;
                Code refusal = ResourceQuantitySignals.Capture(_world, store, leg.Resource, out quantity);
                guard.Validate(_world);
                return refusal;
            }
            var context = new NetworkDemandContext(leg.State.Def.Id, leg.Entity.Id, leg.Entity.Owner, leg.Resource,
                leg.Pull, ready, quantum, ReadUtility);
            try
            {
                NetworkDemandSnapshot demand = NetworkDefinitionContracts.Read(_world, leg.State.Def, leg.Entity,
                    () => NetworkPolicyContracts.Read(_world, leg.State.Def.DemandPolicy, leg.Entity,
                        () => leg.State.Def.DemandPolicy.Read(context)));
                guard.Validate(_world);
                if (demand.Maximum < Fix.Zero || demand.Maximum > ready ||
                    !demand.Blocker.IsNone && !demand.Maximum.IsZero || quantum.Floor(demand.Maximum) != demand.Maximum)
                    throw new InvalidOperationException("network demand exceeds actual ready quantity/native quantum");
                return demand;
            }
            finally { context.Close(); }
        }
        public Code CopyFlow(string network, PlayerId viewer, IList<NetworkFlowSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            _world.RequireHealthyUpdate();
            if (viewer != PlayerId.None && !_world.Players.IsSeat(viewer)) return Codes.NotSeated;
            if (network == null || !_nets.TryGetValue(network, out var state) || !_world.ViewFor(viewer).CanSeeContent(state.Def))
                return Codes.UnknownDef;
            var values = EntityObservation.ReadProvider(_world, this, null, () => {
                var flows = new List<NetworkFlowSnapshot>();
                foreach (FlowCursor cursor in state.Flow) {
                    Entity entity = _world.Entities.Get(cursor.Entity);
                    if (entity == null || viewer.IsValid && entity.Owner != viewer) continue;
                    IResourceRate rate = NetworkDefinitionContracts.Read(_world, state.Def, entity, () => state.Def.Rate(cursor.Resource));
                    Code blocker = Allowance(rate, _world.Time, cursor.Elapsed, cursor.Quantum, out Fix amount);
                    flows.Add(new NetworkFlowSnapshot(cursor.Entity, cursor.Resource, cursor.Pull, cursor.Source, cursor.Destination,
                        cursor.Elapsed, cursor.LastTick, amount, blocker));
                }
                return flows;
            });
            foreach (var value in values) destination.Add(value);
            return Code.None;
        }
        private long FlowTimeLimit(IStateSync sync)
        {
            // Flow completes before event/entity observers and before the SAME clock commits the
            // step. Observe that actual in-progress interval, never grant an extra earned tick.
            bool observation = sync is IStateObservationSync probe && probe.IsObservation && !sync.Reading;
            return observation && (_flowOperation.Active || _world.Stepping) ? checked(_world.Tick + 1) : _world.Tick;
        }
        private void SyncFlow(IStateSync sync, NetState state)
        {
            bool advancing = sync is IStateObservationSync probe && probe.IsObservation && _flowOperation.Active && !sync.Reading;
            long latest = FlowTimeLimit(sync);
            int count = sync.Count(state.Flow.Count); if (sync.Reading) state.Flow.Clear(); FlowCursor previous = null;
            for (int i = 0; i < count; i++)
            {
                FlowCursor cursor = sync.Reading ? new FlowCursor() : state.Flow[i];
                sync.Entity(ref cursor.Entity); sync.Resource(ref cursor.Resource); sync.Bool(ref cursor.Pull);
                ResourceEndpointId.Sync(sync, ref cursor.Source); ResourceEndpointId.Sync(sync, ref cursor.Destination);
                sync.Long(ref cursor.Quantum); sync.Long(ref cursor.Elapsed); sync.Long(ref cursor.LastTick);
                ResourceEndpointId pool = cursor.Pull ? cursor.Source : cursor.Destination;
                if (!cursor.Entity.IsValid || state.Def.Rate(cursor.Resource) == null || cursor.Quantum <= 0 ||
                    cursor.Elapsed < 0 || cursor.LastTick <= 0 || cursor.LastTick > latest || cursor.Elapsed > cursor.LastTick ||
                    !cursor.Source.IsValid || !cursor.Destination.IsValid || cursor.Source == cursor.Destination ||
                    pool.Scope != EndpointScope || pool.Identity >= _nextEndpoint ||
                    !advancing && previous != null && CompareFlow(previous, cursor) >= 0)
                    throw new CorruptStateException("invalid network elapsed flow cursor");
                if (sync.Reading) state.Flow.Add(cursor); previous = cursor;
            }
        }
        private void SyncFlowTurns(IStateSync sync, INetworkDefinition network, NetComponent component)
        {
            long latest = FlowTimeLimit(sync);
            int count = sync.Count(component.FlowTurns.Count); if (sync.Reading) component.FlowTurns.Clear(); FlowTurn previous = null;
            for (int i = 0; i < count; i++)
            {
                FlowTurn turn = sync.Reading ? new FlowTurn() : component.FlowTurns[i];
                sync.Resource(ref turn.Resource); sync.Bool(ref turn.Pull); sync.Entity(ref turn.Entity); sync.Long(ref turn.Tick);
                if (!network.Carries(turn.Resource) || !turn.Entity.IsValid || turn.Entity.Value >= _world.Entities.NextIdentity.Value || turn.Tick <= 0 || turn.Tick > latest ||
                    previous != null && CompareTurns(previous, turn) >= 0)
                    throw new CorruptStateException("invalid network successful-service history");
                if (sync.Reading) component.FlowTurns.Add(turn); previous = turn;
            }
        }
    }
}
