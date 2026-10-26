using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class LogisticsService
    {
        private bool TryReadStockPolicy(ResourceEndpointId endpoint, ResourceId resource, out LogisticsDemandSettings settings)
        {
            foreach (Demand demand in _demands)
                if (demand.Endpoint == endpoint && demand.Resource == resource && demand.Source.Scope == StockDemandScope)
                { settings = demand.Settings; return true; }
            settings = null; return false;
        }
        public bool TryReadStockPolicy(EntityId store, ResourceId resource, out LogisticsDemandSettings settings,
            Code port = default, ResourceEndpointId? endpoint = null)
        {
            IResourceInlet inlet = PhysicalPort(_world.Entities.Get(store), port, resource, endpoint).Store;
            settings = null;
            return inlet != null && TryReadStockPolicy(inlet.Endpoint, resource, out settings);
        }
        public void CopyStockPolicyTargets(EntityId store, ResourceId resource, IList<LogisticsStockPolicyTarget> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            foreach (Demand demand in _demands)
                if (demand.Store == store && demand.Resource == resource && demand.Source.Scope == StockDemandScope)
                    destination.Add(new LogisticsStockPolicyTarget(demand.Port, demand.Endpoint));
        }
        public Code CheckStockPolicy(PlayerId company, EntityId store, ResourceId resource, LogisticsDemandChange change,
            Code port = default, ResourceEndpointId? endpoint = null)
            => AdmitStockPolicy(company, store, resource, change, port, endpoint, out _, out _);
        private Code AdmitStockPolicy(PlayerId company, EntityId store, ResourceId resource, LogisticsDemandChange change,
            Code port, ResourceEndpointId? endpoint, out LogisticsDemandSettings settings, out ResourceEndpointId admitted)
        {
            settings = null; admitted = default;
            if (change == null || !store.IsValid || endpoint.HasValue && !endpoint.Value.IsValid) return Codes.InvalidArgument;
            Entity entity = _world.Entities.Get(store); if (entity == null) return Codes.NotFound;
            if (entity.Owner != company || !company.IsValid) return Codes.NotOwned;
            var result = EntityObservation.Read(_world, entity, () =>
            {
                if (_world.Content.Resource(resource) == null) return (Codes.InvalidArgument, (LogisticsDemandSettings)null, default(ResourceEndpointId));
                var binding = PhysicalPort(entity, port, resource, endpoint);
                if (!binding.Ready) return (binding.Blocker, (LogisticsDemandSettings)null, default(ResourceEndpointId));
                IResourceInlet inlet = binding.Store;
                int version = inlet.Version; var identity = inlet.Endpoint; var owner = inlet.Company;
                try
                {
                    if (owner != company || !inlet.Accepts(_world.Content, resource))
                        return (Codes.InvalidArgument, (LogisticsDemandSettings)null, default(ResourceEndpointId));
                    LogisticsDemandSettings candidate = change.Settings;
                    if (change.Kind == LogisticsDemandChange.ChangeKind.Level)
                    {
                        if (!TryReadStockPolicy(identity, resource, out var current))
                            return (LogisticsPolicyCodes.NoStockPolicy, (LogisticsDemandSettings)null, default(ResourceEndpointId));
                        try
                        {
                            candidate = new LogisticsDemandSettings(change.Enabled,
                                change.StockLevel == LogisticsStockLevel.Minimum ? change.Target : current.Minimum,
                                change.StockLevel == LogisticsStockLevel.Desired ? change.Target : current.Desired,
                                change.StockLevel == LogisticsStockLevel.Maximum ? change.Target : current.Maximum,
                                current.Priority, current.ReserveAccess, current.Selection);
                        }
                        catch (ArgumentException) { return (Codes.InvalidArgument, (LogisticsDemandSettings)null, default(ResourceEndpointId)); }
                    }
                    Code refusal = CheckStockSettings(company, entity, resource, inlet, candidate, port);
                    return (refusal, refusal.IsNone ? candidate : null, identity);
                }
                finally
                {
                    if (inlet.Version != version || inlet.Endpoint != identity || inlet.Company != owner)
                        throw new InvalidOperationException("stock admission mutated its actual endpoint");
                }
            });
            settings = result.Item2; admitted = result.Item3; return result.Item1;
        }
        private Code CheckStockSettings(PlayerId company, Entity store, ResourceId resource,
            IResourceInlet inlet, LogisticsDemandSettings settings, Code port)
        {
            if (settings == null) return Codes.InvalidArgument;
            if (!ValidSelection(_world.Content, resource, settings.Selection)) return Codes.InvalidArgument;
            if (!ResourceLotSelection.Visible(_world, company, settings.Selection)) return Codes.NotFound;
            if (!settings.Selection.IsNone && !(inlet is IResourceLotEndpoint)) return QualityIncompatible;
            if (settings.ReserveAccess == LogisticsReserveAccess.EmergencyOnly &&
                (inlet is not IResourceOutlet || LogisticsPolicyRules.Read(_world, store, port) == null))
                return LogisticsPolicyCodes.NoPolicy;
            Code capacity = CheckStockCapacity(inlet.Endpoint, resource, inlet.CapacityPolicy, settings);
            if (!capacity.IsNone) return capacity;
            if (!settings.Enabled || settings.Desired.IsZero) return Code.None;
            return CheckRequests(new LogisticsDemandId(StockDemandScope, store.Id.Value, port), company, store.Id,
                new[] { new LogisticsSupplyRequest(port, inlet.Endpoint, new Bill(new[] { new ResourceAmount(resource, settings.Desired) }), SelectedClaims(resource, settings.Selection)) },
                settings.Priority);
        }
        // One authored packing check shared by current admission and saved unavailable originals.
        private Code CheckStockCapacity(ResourceEndpointId endpoint, ResourceId resource,
            ICapacityPolicy policy, LogisticsDemandSettings settings, IReadOnlyList<ResourceAmount> replacementMaxima = null)
        {
            if (!LogisticsPolicyRules.Native(policy, resource, settings.Minimum) ||
                !LogisticsPolicyRules.Native(policy, resource, settings.Desired) ||
                !LogisticsPolicyRules.Native(policy, resource, settings.Maximum)) return Codes.InvalidArgument;
            var maxima = replacementMaxima;
            if (maxima == null)
            {
                var current = new List<ResourceAmount>();
                foreach (Demand demand in _demands)
                    if (demand.Endpoint == endpoint && demand.Source.Scope == StockDemandScope &&
                        demand.Resource != resource && demand.Settings.Maximum.IsPositive)
                        current.Add(new ResourceAmount(demand.Resource, demand.Settings.Maximum));
                if (settings.Maximum.IsPositive) current.Add(new ResourceAmount(resource, settings.Maximum));
                maxima = current;
            }
            if (!CapacityPolicyContracts.Check(policy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), maxima).Fits)
                return Codes.InvalidArgument;
            return Code.None;
        }
        public Code ConfigureStockPolicy(PlayerId company, EntityId store, ResourceId resource, LogisticsDemandChange change,
            Code port = default, ResourceEndpointId? endpoint = null)
        {
            Code refusal = AdmitStockPolicy(company, store, resource, change, port, endpoint, out var settings, out var admitted);
            if (!refusal.IsNone) return refusal;
            Entity entity = _world.Entities.Get(store);
            // Actual stores, not names or physical sites, own stock bands. An alias edits the original row.
            for (int i = 0; i < _demands.Count; i++)
                if (_demands[i].Source.Scope == StockDemandScope && _demands[i].Endpoint == admitted && _demands[i].Resource == resource)
                {
                    Demand demand = _demands[i]; demand.Settings = settings;
                    Fix available = QualifiedAvailable(PhysicalPort(entity, port, resource, admitted).Store, resource, settings.Selection);
                    demand.Replenishing = settings.Enabled && settings.Minimum.IsPositive && available < settings.Desired &&
                        (demand.Replenishing || available < settings.Minimum);
                    _demands[i] = demand; return Code.None;
                }
            var source = new LogisticsDemandId(StockDemandScope, store.Value, port);
            Demand added = MakeDemand(entity, resource, settings, source, _world.Tick, port, admitted);
            added.Replenishing = settings.Enabled && QualifiedAvailable(PhysicalPort(entity, port, resource, admitted).Store, resource, settings.Selection) < settings.Minimum;
            _demands.Add(added); return Code.None;
        }
        private static Fix Unpromised(IResourceInlet store, ResourceId resource)
            => store is IResourceOutlet outlet ? outlet.Available(resource) : store.Amount(resource);
        private void RefreshStockBands()
        {
            for (int i = 0; i < _demands.Count; i++)
            {
                Demand demand = _demands[i]; if (demand.Source.Scope != StockDemandScope) continue;
                IResourceInlet inlet = PhysicalPort(_world.Entities.Get(demand.Store), demand.Port, demand.Resource, demand.Endpoint).Store;
                if (inlet == null) continue; // Original maintenance owns expired destinations.
                int version = inlet.Version;
                Fix available = QualifiedAvailable(inlet, demand.Resource, demand.Settings.Selection);
                if (!demand.Settings.Enabled || available >= demand.Settings.Desired) demand.Replenishing = false;
                else if (available < demand.Settings.Minimum) demand.Replenishing = true;
                if (inlet.Version != version || inlet.Endpoint != demand.Endpoint)
                    throw new InvalidOperationException("stock refresh mutated its actual endpoint");
                _demands[i] = demand;
            }
        }
        private Fix DeliveryTarget(ResourceEndpointId endpoint, ResourceId resource, bool emergencyOnly)
        {
            Fix result = Fix.Zero;
            foreach (Demand demand in _demands)
                if (demand.Endpoint == endpoint && demand.Resource == resource && DemandActive(demand) &&
                    (demand.Source.Scope != StockDemandScope || demand.Replenishing) &&
                    (!emergencyOnly || CheckSource(demand).Emergency)) result += demand.Settings.Desired;
            return result;
        }
        private Fix DeliveryGap(IResourceInlet inlet, ResourceId resource, bool emergencyOnly)
        {
            if (HasQualityDemand(inlet.Endpoint, resource))
            { Fix result = Fix.Zero; foreach (var gap in QualityGaps(inlet, resource, emergencyOnly).Values) result += gap; return result; }
            int version = inlet.Version; var identity = inlet.Endpoint; var company = inlet.Company; var capacity = inlet.CapacityPolicy;
            try
            {
                return Fix.Max(Fix.Zero, DeliveryTarget(identity, resource, emergencyOnly)
                    - Unpromised(inlet, resource) - inlet.ReservedIn(resource));
            }
            finally
            {
                if (inlet.Version != version || inlet.Endpoint != identity || inlet.Company != company ||
                    !ReferenceEquals(inlet.CapacityPolicy, capacity))
                    throw new InvalidOperationException("delivery gap mutated its actual endpoint");
            }
        }
        private Demand? RecoveryDemand(EntityId destination, IResourceInlet inlet, ResourceId resource, IResourceOutlet cargo, Job continuing)
        {
            Demand? selected = null;
            foreach (Demand demand in _demands)
                if (demand.Store == destination && demand.Endpoint == inlet.Endpoint && demand.Resource == resource && DemandActive(demand) &&
                    (demand.Source.Scope != StockDemandScope || demand.Replenishing) &&
                    DeliveryGap(demand, CheckSource(demand).Emergency).IsPositive &&
                    (demand.Settings.Selection.IsNone || continuing == null && QualifiedAvailable(cargo, resource, demand.Settings.Selection).IsPositive ||
                        continuing != null && cargo is IResourceLotEndpoint lots && ResourceLotSelection.EligibleAmount(_world.Content,
                            lots.SelectLots(continuing.ReservationOwner, continuing.CargoOutReservation, continuing.Loaded), resource,
                            SelectedClaims(resource, demand.Settings.Selection)[0]) == continuing.Loaded) &&
                    (!selected.HasValue || CompareDemand(demand, selected.Value) < 0)) selected = demand;
            return selected;
        }
        private Fix ReceivingCeiling(IResourceInlet inlet, ResourceId resource, Fix ceiling)
        {
            if (TryReadStockPolicy(inlet.Endpoint, resource, out var settings) && settings.Enabled)
            {
                Fix room = Fix.Max(Fix.Zero, settings.Maximum - inlet.Amount(resource));
                room = Fix.Max(Fix.Zero, room - inlet.ReservedIn(resource));
                return Fix.Min(ceiling, room);
            }
            return ceiling;
        }
        private Code ExportPermission(Entity from, Entity to, Code port)
        {
            OutletPolicySettings settings = LogisticsPolicyRules.Read(_world, from, port);
            if (settings == null) return Code.None;
            return !settings.Enabled ? LogisticsPolicyCodes.Disabled : !settings.Allows(to.Def.Id) ? LogisticsPolicyCodes.Destination : Code.None;
        }
        private int ExportPriority(Entity from, Code port) => LogisticsPolicyRules.Read(_world, from, port)?.ExportPriority ?? 0;
    }
}
