using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Existing route/demand/recovery records remain the only persistent owners.
    public sealed partial class LogisticsService
    {
        public static readonly Code ReplacementAssigned = new Code("logistics.replacement-assigned");
        public static readonly Code ReplacementDemandOwner = new Code("logistics.replacement-demand-owner");
        public static readonly Code ReplacementIndependentEndpoint = new Code("logistics.replacement-independent-endpoint");
        public static readonly Code ReplacementRecoverySource = new Code("logistics.replacement-recovery-source");
        private static bool RecoveryReferences(RecoveryIntent recovery, EntityId entity)
            => recovery.Origin == entity || recovery.SourceEndpoint == ResourceEndpointId.ForEntity(entity);
        public Code CheckReferenceReplacement(World world, Entity source, EntityDef destination)
        {
            if (!ReferenceEquals(world, _world)) throw new InvalidOperationException("logistics references belong to another session");
            foreach (Job job in world.Services.Get<WorkBoard>().Jobs)
                if (job.IsLive && (job.From == source.Id || job.To == source.Id || job.Worker == source.Id || job.Target == source.Id))
                    return ReplacementAssigned;
            foreach (Route route in _routes)
            {
                if (route.From != source.Id && route.To != source.Id) continue;
                if (route.Owner != source.Owner) return Codes.NotOwned;
                if (route.From == source.Id)
                {
                    Code check = LogisticsPolicyRules.CheckReplacementPort(world, destination, route.SourcePort, route.Resource, route.Settings.Batch, true, out _);
                    if (!check.IsNone) return check;
                }
                if (route.To == source.Id)
                {
                    Code check = LogisticsPolicyRules.CheckReplacementPort(world, destination, route.DestinationPort, route.Resource, route.Settings.Batch, false, out _);
                    if (!check.IsNone) return check;
                }
            }
            Code stockCheck = CheckStockReplacement(world, source, destination, out _);
            if (!stockCheck.IsNone) return stockCheck;
            foreach (RecoveryIntent recovery in _recoveries)
                if (RecoveryReferences(recovery, source.Id))
                {
                    if (recovery.Company != source.Owner) return Codes.NotOwned;
                    if (RecoveryStock(recovery) == null) return ReplacementRecoverySource;
                    if (recovery.Kind == LogisticsRecoveryKind.CargoReturn)
                    {
                        if (!recovery.SourcePort.IsNone) return ReplacementIndependentEndpoint;
                        // Replacing the carrier retains its provider-owned cargo binding, which
                        // may name an entirely different entity. Replacing that inventory instead
                        // changes only the source endpoint; the actual carrier remains the origin.
                        if (recovery.Origin == source.Id)
                        {
                            var current = source.Get<ITransporter>() as Component;
                            var next = destination.Component<ITransporterDefinition>() as ComponentDef;
                            if (current == null || next == null || !destination.HasCapability<ITransporter>() ||
                                current.Def.Key != next.Key || !Upgrades.CanRetainState(current.Def, next))
                                return ReplacementRecoverySource;
                        }
                        if (recovery.SourceEndpoint == ResourceEndpointId.ForEntity(source.Id))
                        {
                            if (!destination.HasCapability<IInventory>()) return ReplacementRecoverySource;
                            Code check = LogisticsPolicyRules.CheckReplacementPort(world, destination, Code.None, recovery.Resource,
                                Fix.Zero, true, out _, entityInventory: true);
                            if (!check.IsNone) return check;
                        }
                    }
                    else
                    {
                        Code check = CheckEvacuationReplacement(world, source, destination, recovery);
                        if (!check.IsNone) return check;
                    }
                }
            return Code.None;
        }
        public Code CheckStockTargetReplacement(Entity source, EntityDef destination, ResourceId resource,
            Code port, ResourceEndpointId original, Fix quantity)
            => PredictStockTarget(source, destination, resource, port, original, quantity, out _);

        public Code RebindStockTarget(Entity source, Entity destination, ResourceId resource,
            Code port, ResourceEndpointId original, out ResourceEndpointId endpoint)
        {
            endpoint = default; _world.RequireHealthyUpdate();
            if (destination?.Alive != true || !ReferenceEquals(_world.Entities.Get(destination.Id), destination)) return Codes.NotFound;
            if (ReferenceEquals(source, destination)) return Codes.InvalidArgument;
            Code check = PredictStockTarget(source, destination.Def, resource, port, original, Fix.Zero, out var predicted);
            if (!check.IsNone) return check;
            if (destination.Owner != source.Owner) return Codes.NotOwned;
            var binding = PhysicalPort(destination, port, resource, predicted ?? ResourceEndpointId.ForEntity(destination.Id));
            if (!binding.Ready) return binding.Blocker;
            if (binding.Store.Company != source.Owner) return Codes.NotOwned;
            endpoint = binding.Store.Endpoint;
            return Code.None;
        }
        private Code PredictStockTarget(Entity source, EntityDef destination, ResourceId resource,
            Code port, ResourceEndpointId original, Fix quantity, out ResourceEndpointId? endpoint)
        {
            endpoint = null; _world.RequireHealthyUpdate();
            if (source?.Alive != true || !ReferenceEquals(_world.Entities.Get(source.Id), source)) return Codes.NotFound;
            if (destination == null || !ReferenceEquals(_world.Content.Find<EntityDef>(destination.Id), destination)) return Codes.UnknownDef;
            if (!original.IsValid || _world.Content.Resource(resource) == null) return Codes.InvalidArgument;
            Code check = LogisticsPolicyRules.CheckReplacementPort(_world, destination, port, resource, quantity, false, out _);
            if (!check.IsNone) return check;
            check = PredictReplacementPort(_world, source, destination, port, original, out endpoint);
            return check.IsNone ? CheckReplacementStockSource(source, port, resource, original, endpoint) : check;
        }
        private Code CheckReplacementStockSource(Entity source, Code port, ResourceId resource,
            ResourceEndpointId original, ResourceEndpointId? replacement)
        {
            var current = PhysicalPort(source, port, resource, original);
            if (!current.Ready) return current.Blocker;
            if (current.Store.Company != source.Owner) return Codes.NotOwned;
            return current.Store.Reservations.Count != 0 && (!replacement.HasValue || replacement.Value != original)
                ? ReplacementAssigned : Code.None;
        }
        private Code CheckEvacuationReplacement(World world, Entity source, EntityDef destination, RecoveryIntent recovery)
        {
            if (recovery.Origin != source.Id) return ReplacementRecoverySource;
            Code check = LogisticsPolicyRules.CheckReplacementPort(world, destination, recovery.SourcePort, recovery.Resource,
                Fix.Zero, true, out _);
            if (!check.IsNone) return check;
            check = PredictReplacementPort(world, source, destination, recovery.SourcePort, null, out var endpoint);
            if (!check.IsNone) return check;
            if (recovery.SourceEndpoint == ResourceEndpointId.ForEntity(source.Id))
            {
                if (endpoint.HasValue) return ReplacementRecoverySource;
                var inventory = destination.Component<IInventoryDefinition>();
                return inventory?.Public == true && inventory.Evacuate ? Code.None : ReplacementRecoverySource;
            }
            // The independent provider still owns this stock. Replacing its access site is not permission
            // to move or redirect it, nor to pin an original that ordinary recovery cannot currently use.
            if (!endpoint.HasValue || endpoint.Value != recovery.SourceEndpoint) return ReplacementRecoverySource;
            foreach (var provider in world.Services.Providers<IResourceEndpointProvider>())
                if (provider.EndpointScope == endpoint.Value.Scope)
                    return EntityObservation.ReadProvider(world, provider, source, () => {
                        var actual = ResourceTransactions.Resolve(world, endpoint.Value);
                        if (actual == null) return Codes.NotFound;
                        if (actual.Company != recovery.Company) return Codes.NotOwned;
                        if (actual is not IResourceOutlet store || !store.Public || !store.Evacuate) return ReplacementRecoverySource;
                        if (!store.Accepts(world.Content, recovery.Resource)) return StandardCodes.NotAccepted;
                        ResourcePortContracts.ValidateStore(source, destination.Component<IResourcePortDefinition>(recovery.SourcePort), store, endpoint);
                        return Code.None;
                    }, world.Space, observeTargetState: true);
            return Codes.NotFound;
        }
        // A single prediction path for policies and recovery. A policy can retain an explicit original;
        // recovery passes null because future automatic evacuation uses the actual current binding.
        private Code PredictReplacementPort(World world, Entity source, EntityDef destination, Code identity,
            ResourceEndpointId? original, out ResourceEndpointId? endpoint)
        {
            endpoint = null;
            if (identity.IsNone) return Code.None;
            var port = destination.Component<IResourcePortDefinition>(identity);
            if (port == null) return Codes.NotFound;
            if (port.BindsEntityInventory) return Code.None;
            if (port is INetworkResourcePortDefinition connected)
            {
                NetworkResourcePortDeclaration.ValidateBinding(world, destination, connected);
                var access = world.Services.Get<INetworkResourceAccess>();
                var resolution = EntityObservation.ReadProvider(world, access, source,
                    () => access.ResolveStore(source.Id, destination, connected.Network, source.Owner, connected.MinimumMembers),
                    world.Space, observeTargetState: true);
                if (!resolution.Ready) return resolution.Blocker;
                ResourcePortContracts.ValidateStore(source, connected, resolution.Store, null);
                endpoint = resolution.Store.Endpoint;
            }
            else
            {
                if (port is not IResourcePortSelectionDefinition selection) return ReplacementIndependentEndpoint;
                var choices = ResourcePortSelectionContracts.ReadChoices(world, destination, selection);
                var currentPort = source.Get<IResourcePort>(identity) as Component;
                var nextPort = port as ComponentDef;
                if (currentPort is IResourcePortSelection retained && nextPort != null && currentPort.Def.Key == nextPort.Key &&
                    Upgrades.CanRetainState(currentPort.Def, nextPort))
                {
                    var selected = EntityObservation.ReadProvider(world, retained, source, () => retained.SelectedEndpoint, world.Space, observeTargetState: true);
                    var required = original ?? selected;
                    bool allowsSelected = false, allowsOriginal = false;
                    foreach (var choice in choices) { allowsSelected |= choice == selected; allowsOriginal |= choice == required; }
                    if (!allowsSelected || !allowsOriginal || selected != required &&
                        !EntityObservation.ReadProvider(world, selection, null, () => selection.RetainsOriginalAccess, world.Space))
                        return ResourcePortCodes.OriginalUnavailable;
                    endpoint = required;
                }
                else endpoint = EntityObservation.ReadProvider(world, selection, null, () => selection.InitialEndpoint, world.Space);
            }
            return endpoint.Value.Scope == ResourceEndpointId.EntityScope ? ReplacementIndependentEndpoint : Code.None;
        }
        // Transient prospective bindings only. Null explicitly denotes the future entity inventory;
        // independent identities must come from the existing authored selection contract, never an invented ID.
        private Code CheckStockReplacement(World world, Entity source, EntityDef destination,
            out SortedDictionary<int, ResourceEndpointId?> bindings)
        {
            bindings = new SortedDictionary<int, ResourceEndpointId?>();
            for (int i = 0; i < _demands.Count; i++)
            {
                Demand demand = _demands[i]; if (demand.Store != source.Id) continue;
                if (demand.Source.Scope != StockDemandScope) return ReplacementDemandOwner;
                if (demand.Owner != source.Owner) return Codes.NotOwned;
                Code check = LogisticsPolicyRules.CheckReplacementPort(world, destination, demand.Port, demand.Resource,
                    demand.Settings.Maximum, false, out _);
                if (!check.IsNone) return check;
                check = PredictReplacementPort(world, source, destination, demand.Port, demand.Endpoint, out var endpoint);
                if (!check.IsNone) return check;
                check = CheckReplacementStockSource(source, demand.Port, demand.Resource, demand.Endpoint, endpoint);
                if (!check.IsNone) return check;
                bindings.Add(i, endpoint);
            }
            foreach (var entry in bindings)
            {
                Demand demand = _demands[entry.Key]; var maxima = new List<ResourceAmount>(); var resources = new HashSet<ResourceId>();
                for (int i = 0; i < _demands.Count; i++)
                {
                    Demand other = _demands[i]; if (other.Source.Scope != StockDemandScope) continue;
                    bool same = bindings.TryGetValue(i, out var prospective) ? prospective == entry.Value : entry.Value.HasValue && other.Endpoint == entry.Value.Value;
                    if (!same) continue;
                    // Distinct policy identities must not silently collapse when two ports converge.
                    if (!resources.Add(other.Resource)) return Codes.InvalidArgument;
                    if (other.Settings.Maximum.IsPositive) maxima.Add(new ResourceAmount(other.Resource, other.Settings.Maximum));
                }
                Code check = LogisticsPolicyRules.CheckReplacementPort(world, destination, demand.Port, demand.Resource,
                    demand.Settings.Maximum, false, out var capacity);
                if (!check.IsNone) return check;
                check = CheckStockCapacity(demand.Endpoint, demand.Resource, capacity, demand.Settings, maxima);
                if (!check.IsNone) return check;
                if (demand.Settings.ReserveAccess == LogisticsReserveAccess.EmergencyOnly && !destination.HasCapability<IOutletPolicy>())
                    return LogisticsPolicyCodes.NoPolicy;
                if (!entry.Value.HasValue) continue;
                bool found = false;
                foreach (var provider in world.Services.Providers<IResourceEndpointProvider>())
                    if (provider.EndpointScope == entry.Value.Value.Scope)
                    {
                        found = true;
                        check = EntityObservation.ReadProvider(world, provider, null, () => {
                            var actual = ResourceTransactions.Resolve(world, entry.Value.Value);
                            if (actual == null) return Codes.NotFound;
                            if (actual.Company != source.Owner) return Codes.NotOwned;
                            if (actual is not IResourceOutlet store || !store.Accepts(world.Content, demand.Resource)) return StandardCodes.NotAccepted;
                            if (!CapacityPolicyContracts.Satisfies(store.CapacityPolicy, capacity)) return Codes.InvalidArgument;
                            return CheckStockCapacity(store.Endpoint, demand.Resource, store.CapacityPolicy, demand.Settings, maxima);
                        }, world.Space);
                        if (!check.IsNone) return check;
                    }
                if (!found) return Codes.NotFound;
            }
            return Code.None;
        }
        public void ReplaceEntityReferences(World world, Entity source, Entity destination)
        {
            if (!ReferenceEquals(world, _world)) throw new InvalidOperationException("logistics references belong to another session");
            // Resolve every actual new binding before editing any persistent intent.
            foreach (Route route in _routes)
                if (route.From == source.Id || route.To == source.Id)
                {
                    Code check = CheckRouteBinding(route.Owner, route.From == source.Id ? destination.Id : route.From,
                        route.To == source.Id ? destination.Id : route.To, route.Resource, route.Settings, route.SourcePort, route.DestinationPort, false);
                    if (!check.IsNone) throw new InvalidOperationException("replacement route binding violated admission: " + check);
                }
            ResourceEndpointId endpoint = ResourceEndpointId.ForEntity(destination.Id);
            Code stockCheck = CheckStockReplacement(world, source, destination.Def, out var stockBindings);
            if (!stockCheck.IsNone) throw new InvalidOperationException("replacement stock metadata violated admission: " + stockCheck);
            foreach (var entry in stockBindings)
            {
                Demand demand = _demands[entry.Key]; var expected = entry.Value ?? endpoint;
                var binding = PhysicalPort(destination, demand.Port, demand.Resource, expected);
                if (!binding.Ready) throw new InvalidOperationException("replacement stock policy binding violated admission: " + binding.Blocker);
                if (binding.Store.Company != demand.Owner || !CheckStockSettings(demand.Owner, destination, demand.Resource, binding.Store, demand.Settings, demand.Port).IsNone)
                    throw new InvalidOperationException("replacement stock policy lost actual ownership or settings");
            }
            foreach (RecoveryIntent recovery in _recoveries)
                if (RecoveryReferences(recovery, source.Id))
                {
                    ResourceEndpointId expected = recovery.SourceEndpoint == ResourceEndpointId.ForEntity(source.Id) ? endpoint : recovery.SourceEndpoint;
                    // Service callbacks precede component reference callbacks. An independent
                    // carrier still names the live old inventory here; validate the actual prepared
                    // replacement inventory, never require a component to commit out of order.
                    IResourceOutlet stock = recovery.Kind != LogisticsRecoveryKind.CargoReturn
                        ? PhysicalPort(destination, recovery.SourcePort, recovery.Resource).Store as IResourceOutlet
                        : recovery.Origin == source.Id ? TransporterContracts.ReadCargo(world, destination) : destination.Get<IInventory>();
                    if (stock == null || stock.Endpoint != expected || stock.Company != recovery.Company ||
                        !stock.Accepts(world.Content, recovery.Resource) || recovery.Kind == LogisticsRecoveryKind.Evacuation && (!stock.Public || !stock.Evacuate))
                        throw new InvalidOperationException("replacement recovery binding violated admission");
                }
            for (int i = 0; i < _routes.Count; i++)
            {
                Route route = _routes[i];
                if (route.From == source.Id) route.From = destination.Id;
                if (route.To == source.Id) route.To = destination.Id;
                _routes[i] = route;
            }
            for (int i = 0; i < _demands.Count; i++)
                if (_demands[i].Store == source.Id)
                {
                    Demand demand = _demands[i]; demand.Store = destination.Id; demand.Endpoint = stockBindings[i] ?? endpoint;
                    demand.Source = new LogisticsDemandId(StockDemandScope, destination.Id.Value, demand.Source.Entry);
                    _demands[i] = demand;
                }
            for (int i = 0; i < _recoveries.Count; i++)
                if (RecoveryReferences(_recoveries[i], source.Id))
                {
                    RecoveryIntent recovery = _recoveries[i];
                    if (recovery.Origin == source.Id) recovery.Origin = destination.Id;
                    if (recovery.SourceEndpoint == ResourceEndpointId.ForEntity(source.Id)) recovery.SourceEndpoint = endpoint;
                    _recoveries[i] = recovery;
                }
        }
    }
}

