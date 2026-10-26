using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class LogisticsService
    {
        public const string StockDemandScope = "sim.stock";
        public World World => _world;
        public string DemandScope => StockDemandScope;
        Code ILogisticsDemandOwner.CheckSourceDisclosure(PlayerId viewer, LogisticsDemandId source, PlayerId company, EntityId destination)
            => source.Scope == DemandScope && source.Identity == destination.Value && company == _world.Entities.Get(destination)?.Owner
                && _world.ViewFor(viewer).CanObserveOwned(_world.Entities.Get(destination)) ? Code.None : Codes.NotFound;
        public Code CheckSourceDisclosure(PlayerId viewer, LogisticsDemandId source, PlayerId company, EntityId destination)
        {
            _world.RequireHealthyUpdate(); if (viewer == PlayerId.None) return Code.None;
            Entity entity = _world.Entities.Get(destination);
            if (viewer != company || !source.IsValid || !_world.ViewFor(viewer).CanObserveOwned(entity)) return Codes.NotFound;
            ILogisticsDemandOwner owner = DemandOwner(source); if (owner == null) return Codes.NotFound;
            Code result = EntityObservation.ReadProvider(_world, owner, entity,
                () => owner.CheckSourceDisclosure(viewer, source, company, destination), space: _world.Space, observeTargetState: true);
            if (!result.IsNone && result != Codes.NotFound) throw new InvalidOperationException("demand disclosure must be whole available or private");
            return result;
        }
        public Code CheckDemandDisclosure(PlayerId viewer, EntityId destination, ResourceId resource, ResourceEndpointId? endpoint = null)
        {
            _world.RequireHealthyUpdate(); if (viewer == PlayerId.None) return Code.None;
            var view = _world.ViewFor(viewer); Entity entity = _world.Entities.Get(destination);
            if (!view.CanObserveOwned(entity)) return Codes.NotFound;
            var definition = _world.Content.Resource(resource);
            if (definition != null && !view.CanSeeContent(definition)) return Codes.NotFound;
            foreach (Demand demand in _demands)
                if (demand.Store == destination && demand.Resource == resource && (!endpoint.HasValue || demand.Endpoint == endpoint.Value) &&
                    (!CheckSourceDisclosure(viewer, demand.Source, demand.Owner, destination).IsNone || !ResourceLotSelection.Visible(_world, viewer, demand.Settings.Selection))) return Codes.NotFound;
            return Code.None;
        }
        public LogisticsDemandSourceCheck CheckSource(LogisticsDemandId source, PlayerId company, EntityId destination)
        {
            if (source.Scope != DemandScope || source.Identity != destination.Value)
                return LogisticsDemandSourceCheck.Invalid(LogisticsDemandCodes.InvalidSource);
            Entity store = _world.Entities.Get(destination);
            if (store == null) return LogisticsDemandSourceCheck.Expired(LogisticsDemandCodes.Expired);
            return store.Owner == company && (source.Entry.IsNone ? store.Get<IInletPort>() != null : store.Get<IResourcePort>(source.Entry) != null)
                ? LogisticsDemandSourceCheck.Active(false) : LogisticsDemandSourceCheck.Invalid(Codes.NotOwned);
        }
        private ILogisticsDemandOwner DemandOwner(LogisticsDemandId source)
        {
            foreach (ILogisticsDemandOwner owner in _world.Services.Providers<ILogisticsDemandOwner>())
                if (owner.DemandScope == source.Scope) return owner;
            return null;
        }
        internal LogisticsDemandSourceCheck CheckSource(Demand demand)
            => InspectSource(demand.Source, demand.Owner, demand.Store);
        public LogisticsDemandSourceCheck InspectSource(LogisticsDemandId source, PlayerId company, EntityId destination)
        {
            if (!source.IsValid || !company.IsValid || !destination.IsValid) return LogisticsDemandSourceCheck.Invalid(LogisticsDemandCodes.InvalidSource);
            ILogisticsDemandOwner owner = DemandOwner(source);
            if (owner == null) return LogisticsDemandSourceCheck.Invalid(LogisticsDemandCodes.UnknownOwner);
            LogisticsDemandSourceCheck check = owner.CheckSource(source, company, destination);
            if (!check.IsWellFormed) throw new InvalidOperationException("demand owner returned malformed source facts");
            Entity entity = _world.Entities.Get(destination);
            if (entity != null && entity.Owner != company) return LogisticsDemandSourceCheck.Invalid(Codes.NotOwned);
            return check;
        }
        private bool DemandActive(Demand demand)
        {
            var check = CheckSource(demand);
            if (check.Status == LogisticsDemandStatus.Invalid) throw new InvalidOperationException("invalid demand source: " + check.Refusal);
            return check.Status == LogisticsDemandStatus.Active && demand.Settings.Enabled;
        }
        internal void ValidateLoadedDemands()
        {
            var totals = new Dictionary<(ResourceEndpointId, ResourceId), long>();
            foreach (Demand demand in _demands)
            {
                if (!ValidSelection(_world.Content, demand.Resource, demand.Settings.Selection)) throw new CorruptStateException("saved demand lost actual quality selection");
                if (demand.Source.Scope != StockDemandScope && demand.Settings.ReserveAccess != LogisticsReserveAccess.Protected)
                    throw new CorruptStateException("workflow demands cannot carry stock-only reserve access");
                var check = CheckSource(demand);
                if (check.Status == LogisticsDemandStatus.Invalid) throw new CorruptStateException("saved demand does not belong to declared source: " + check.Refusal);
                IResourceInlet inlet = PhysicalPort(_world.Entities.Get(demand.Store), demand.Port, demand.Resource, demand.Endpoint).Store;
                if (inlet != null && !demand.Settings.Selection.IsNone && !(inlet is IResourceLotEndpoint)) throw new CorruptStateException("saved quality demand requires lot-capable destination");
                var definition = demand.Port.IsNone ? null : _world.Entities.Get(demand.Store)?.Def.Component<IResourcePortDefinition>(demand.Port);
                // Original stock intent survives a legal access disconnection. It still uses
                // exact authored packing; current selection never replaces its endpoint or age.
                ICapacityPolicy capacity = inlet?.CapacityPolicy ?? definition?.CapacityPolicy;
                if (check.Status == LogisticsDemandStatus.Active && (capacity == null ||
                    (inlet != null ? !inlet.Accepts(_world.Content, demand.Resource) : definition?.Accepts(_world.Content, demand.Resource) != true) ||
                    !LogisticsPolicyRules.Native(capacity, demand.Resource, demand.Settings.Minimum) ||
                    !LogisticsPolicyRules.Native(capacity, demand.Resource, demand.Settings.Desired) ||
                    !LogisticsPolicyRules.Native(capacity, demand.Resource, demand.Settings.Maximum)))
                    throw new CorruptStateException("saved demand is incompatible with its native destination");
                if (check.Status == LogisticsDemandStatus.Active && demand.Source.Scope == StockDemandScope &&
                    (inlet != null
                        ? !CheckStockPolicy(demand.Owner, demand.Store, demand.Resource, LogisticsDemandChange.Replace(demand.Settings), demand.Port, demand.Endpoint).IsNone
                        : !CheckStockCapacity(demand.Endpoint, demand.Resource, capacity, demand.Settings).IsNone ||
                            demand.Settings.ReserveAccess == LogisticsReserveAccess.EmergencyOnly &&
                            LogisticsPolicyRules.Read(_world, _world.Entities.Get(demand.Store), demand.Port) == null))
                    throw new CorruptStateException("invalid saved native stock policy");
                if (check.Status == LogisticsDemandStatus.Active && demand.Settings.Enabled)
                {
                    var key = (demand.Endpoint, demand.Resource); totals.TryGetValue(key, out long amount);
                    try { totals[key] = checked(amount + demand.Settings.Desired.Raw); }
                    catch (OverflowException) { throw new CorruptStateException("saved native demand total is out of range"); }
                }
            }
        }
        public Code CheckRequests(LogisticsDemandId source, PlayerId company, EntityId destination,
            IReadOnlyList<LogisticsSupplyRequest> requests, int priority)
        {
            if (!source.IsValid || !company.IsValid || requests == null || priority < 0) return Codes.InvalidArgument;
            ILogisticsDemandOwner owner = DemandOwner(source);
            if (owner == null) return LogisticsDemandCodes.UnknownOwner;
            var check = owner.CheckSource(source, company, destination);
            if (!check.IsWellFormed) throw new InvalidOperationException("demand owner returned malformed source facts");
            if (check.Status != LogisticsDemandStatus.Active) return check.Refusal;
            Entity entity = _world.Entities.Get(destination);
            if (entity == null) return Codes.NotFound;
            if (entity.Owner != company) return Codes.NotOwned;
            var seen = new HashSet<(ResourceEndpointId, ResourceId, Code)>();
            foreach (LogisticsSupplyRequest request in requests)
            {
                if (!request.Endpoint.IsValid || request.Quantities == null ||
                    request.Port.IsNone && request.Endpoint != ResourceEndpointId.ForEntity(destination)) return Codes.InvalidArgument;
                var selectionResources = new HashSet<ResourceId>();
                foreach (var selection in request.Selections)
                    if (selection == null || !_world.Content.Owns(selection) || !ReferenceEquals(_world.Content.Find<Content.IResourceLotSelectionDefinition>(selection.Id), selection) ||
                        !request.Quantities.AmountOf(selection.Resource).IsPositive || !selectionResources.Add(selection.Resource)) return Codes.InvalidArgument;
                foreach (ResourceAmount line in request.Quantities.Lines)
                {
                    var selection = request.SelectionFor(line.Resource); Code selected = selection == null ? Code.None : new Code(selection.Id);
                    if (_world.Content.Resource(line.Resource) == null || !line.Amount.IsPositive ||
                        !seen.Add((request.Endpoint, line.Resource, selected))) return Codes.InvalidArgument;
                    var binding = PhysicalPort(entity, request.Port, line.Resource, request.Endpoint);
                    if (!binding.Ready) return binding.Blocker;
                    IResourceInlet inlet = binding.Store;
                    if (!selected.IsNone && !(inlet is IResourceLotEndpoint)) return QualityIncompatible;
                    int version = inlet.Version;
                    bool valid = inlet.Company == company && inlet.Accepts(_world.Content, line.Resource) &&
                        CapacityPolicyContracts.Quantum(inlet.CapacityPolicy, line.Resource).Floor(line.Amount) == line.Amount &&
                        (source.Scope != StockDemandScope || CapacityPolicyContracts.Check(inlet.CapacityPolicy, Array.Empty<ResourceAmount>(),
                            Array.Empty<ResourceAmount>(), request.Quantities.Lines).Fits);
                    if (inlet.Version != version || inlet.Endpoint != request.Endpoint || inlet.Company != company)
                        throw new InvalidOperationException("demand admission mutated its actual port");
                    if (!valid) return Codes.InvalidArgument;
                    long total = line.Amount.Raw;
                    try
                    {
                        foreach (Demand demand in _demands)
                            if (demand.Endpoint == request.Endpoint && demand.Resource == line.Resource &&
                                !demand.Source.Equals(source) && !(source.Scope == StockDemandScope && demand.Source.Scope == StockDemandScope) && DemandActive(demand)) total = checked(total + demand.Settings.Desired.Raw);
                    }
                    catch (OverflowException) { return Codes.InvalidArgument; }
                }
            }
            return Code.None;
        }
        public Code ReplaceRequests(LogisticsDemandId source, PlayerId company, EntityId destination,
            IReadOnlyList<LogisticsSupplyRequest> requests, int priority)
        {
            if (source.Scope == StockDemandScope) return LogisticsPolicyCodes.StockPolicyOwner;
            Code refusal = CheckRequests(source, company, destination, requests, priority);
            if (!refusal.IsNone) return refusal;
            // Entire envelope admitted before any old row changes. Actual haul claims are independent.
            for (int i = _demands.Count - 1; i >= 0; i--)
            {
                Demand demand = _demands[i]; if (!demand.Source.Equals(source)) continue;
                bool retained = false;
                if (demand.Store == destination)
                    foreach (LogisticsSupplyRequest request in requests)
                        if (request.Endpoint == demand.Endpoint && request.Quantities.AmountOf(demand.Resource).IsPositive &&
                            (request.SelectionFor(demand.Resource)?.Id == demand.Settings.Selection.Id)) retained = true;
                if (!retained) _demands.RemoveAt(i);
            }
            Entity store = _world.Entities.Get(destination);
            foreach (LogisticsSupplyRequest request in requests) foreach (ResourceAmount line in request.Quantities.Lines)
                UpdateDemand(store, line.Resource, line.Amount, priority, source, request.Port, request.Endpoint,
                    request.SelectionFor(line.Resource) is Content.IResourceLotSelectionDefinition selection ? new Code(selection.Id) : Code.None);
            return Code.None;
        }
        public void WithdrawRequests(LogisticsDemandId source)
        {
            if (source.Scope == StockDemandScope) throw new ArgumentException("stock intent must be changed through ILogisticsStockPolicies", nameof(source));
            if (!source.IsValid || DemandOwner(source) == null) throw new ArgumentException("undeclared demand source", nameof(source));
            for (int i = _demands.Count - 1; i >= 0; i--) if (_demands[i].Source.Equals(source)) _demands.RemoveAt(i);
        }
        private void ValidateDemandOwners()
        {
            var scopes = new HashSet<string>(StringComparer.Ordinal);
            foreach (ILogisticsDemandOwner owner in _world.Services.Providers<ILogisticsDemandOwner>())
            {
                ContractIdentity.ValidateId(owner.DemandScope, "demand owner");
                if (!ReferenceEquals(owner.World, _world) || !scopes.Add(owner.DemandScope))
                    throw new Content.ContentException("duplicate or foreign logistics demand owner '" + owner.DemandScope + "'");
            }
        }
    }
}
