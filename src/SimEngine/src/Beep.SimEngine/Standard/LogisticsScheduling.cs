using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface ILogisticsRoutes
    {
        Code CheckRoute(PlayerId company, EntityId from, EntityId to, ResourceId resource, LogisticsRouteSettings settings, Code sourcePort = default, Code destinationPort = default);
        int AddRoute(PlayerId company, EntityId from, EntityId to, ResourceId resource, LogisticsRouteSettings settings, Code sourcePort = default, Code destinationPort = default);
        Code CheckRouteSettings(PlayerId company, int route, LogisticsRouteSettings settings);
        Code ConfigureRoute(PlayerId company, int route, LogisticsRouteSettings settings);
        Code CheckRemoveRoute(PlayerId company, int route);
        bool RemoveRoute(int route);
    }
    public sealed class LogisticsRouteSettings
    {
        public bool Enabled { get; }
        public Fix Batch { get; }
        public int Priority { get; }
        public Code Selection { get; }
        public LogisticsRouteSettings(bool enabled, Fix batch, int priority, Code selection = default)
        {
            if (!batch.IsPositive || priority < 0) throw new ArgumentException("positive batch and nonnegative priority required");
            Enabled = enabled; Batch = batch; Priority = priority; Selection = selection;
        }
        public static LogisticsRouteSettings Sync(IStateSync sync, LogisticsRouteSettings settings)
        {
            if (!sync.Reading && settings == null) throw new CorruptStateException("missing route settings");
            bool enabled = settings?.Enabled ?? false; Fix batch = settings?.Batch ?? Fix.Zero; int priority = settings?.Priority ?? 0;
            sync.Bool(ref enabled); sync.Fix(ref batch); sync.Int(ref priority);
            string selection = settings?.Selection.Id; sync.String(ref selection);
            try { return new LogisticsRouteSettings(enabled, batch, priority, selection == null ? Code.None : new Code(selection)); }
            catch (ArgumentException ex) { throw new CorruptStateException("invalid route settings: " + ex.Message); }
        }
    }
    public enum LogisticsDispatchKind : byte { Demand, Route, Evacuation, CargoReturn }
    public readonly struct LogisticsDispatchOffer
    {
        public LogisticsDispatchKind Kind { get; }
        public EntityId RecoveryOrigin { get; }
        public LogisticsDemandId Source { get; }
        public int Route { get; }
        public PlayerId Company { get; }
        public EntityId Destination { get; }
        public Code SourcePort { get; }
        public ResourceEndpointId SourceEndpoint { get; }
        public Code DestinationPort { get; }
        public ResourceEndpointId DestinationEndpoint { get; }
        public ResourceId Resource { get; }
        public Code Selection { get; }
        /// <summary>Actual requested delivery gap, or null when the original physical stock is unavailable.</summary>
        public Fix? Requested { get; }
        public int Priority { get; }
        public long CreatedTick { get; }
        public long AgeTicks { get; }
        public long LastDispatchTick { get; }
        public long WaitAgeTicks { get; }
        public HaulPreview Haul { get; }
        public bool Emergency { get; }
        internal LogisticsDispatchOffer(LogisticsDemandId source, int route, PlayerId company, EntityId destination,
            ResourceId resource, Fix? requested, int priority, long created, long tick, HaulPreview haul, bool emergency, long lastDispatch,
            LogisticsDispatchKind kind = LogisticsDispatchKind.Demand, EntityId recoveryOrigin = default,
            Code destinationPort = default, ResourceEndpointId destinationEndpoint = default,
            Code sourcePort = default, ResourceEndpointId sourceEndpoint = default, Code selection = default)
        {
            if (created < 0 || lastDispatch < created || lastDispatch > tick) throw new InvalidOperationException("dispatch history is outside session age");
            if (!requested.HasValue && (route != 0 || kind != LogisticsDispatchKind.Demand || haul.Status == HaulStatus.Ready || haul.Blocker.IsNone) ||
                requested.HasValue && requested.Value < Fix.Zero || haul.Status == HaulStatus.Ready && !requested.Value.IsPositive)
                throw new InvalidOperationException("dispatch quantities distinguish known physical demand from unavailable observation");
            Source = source; Route = route; Company = company; Destination = destination;
            Selection = haul.Status == HaulStatus.Ready ? haul.Selection : selection;
            SourcePort = sourcePort; SourceEndpoint = sourceEndpoint;
            DestinationPort = destinationPort; DestinationEndpoint = destinationEndpoint; Resource = resource;
            Requested = requested; Priority = priority; CreatedTick = created; AgeTicks = checked(tick - created); Haul = haul; Emergency = emergency;
            LastDispatchTick = lastDispatch; WaitAgeTicks = checked(tick - lastDispatch);
            Kind = route != 0 ? LogisticsDispatchKind.Route : kind; RecoveryOrigin = recoveryOrigin;
        }
    }
    /// <summary>Derived offers from existing intent/receipt/worker owners; not another persistent queue.</summary>
    public interface ILogisticsDispatchQueue
    { void CopyQueue(PlayerId viewer, IList<LogisticsDispatchOffer> destination); }

    public sealed partial class LogisticsService
    {
        public string DispatchScope => LogisticsModule.ModuleId;
        public Schedule GetDispatchSchedule(ISimulationTime time) => Schedule.Every(time, Tuning.LogisticsPlanInterval);
        private static readonly Code DispatchUnavailable = new Code("logistics.dispatch-unavailable");

        private static bool IsRecovery(LogisticsDispatchOffer offer) => offer.Kind == LogisticsDispatchKind.Evacuation || offer.Kind == LogisticsDispatchKind.CargoReturn;
        private static LogisticsRecoveryKind RecoveryKind(LogisticsDispatchOffer offer)
            => offer.Kind == LogisticsDispatchKind.CargoReturn ? LogisticsRecoveryKind.CargoReturn : LogisticsRecoveryKind.Evacuation;
        private WorkerDispatchOffer Assignment(LogisticsDispatchOffer offer)
            => new WorkerDispatchOffer(DispatchScope, IsRecovery(offer) ? offer.SourceEndpoint.Identity : offer.Route == 0 ? offer.Destination.Value : offer.Route,
                IsRecovery(offer) ? RecoveryEntry(RecoveryKind(offer), offer.Resource, offer.SourceEndpoint) : offer.Route == 0 ? DemandEntry(offer.Resource, offer.DestinationPort, offer.DestinationEndpoint, offer.Selection) : "route",
                offer.Company, IsRecovery(offer) ? offer.RecoveryOrigin : offer.Destination, offer.Haul.Carrier, offer.Haul.Status == HaulStatus.Ready,
                offer.Haul.Status == HaulStatus.Ready ? Code.None : offer.Haul.Blocker.IsNone ? DispatchUnavailable : offer.Haul.Blocker,
                offer.Emergency, offer.Priority, offer.CreatedTick, _world.Tick, offer.Haul.TravelCost,
                IsRecovery(offer) ? LogisticsRecoveryContracts.Read(_world, RecoveryPolicy, RecoveryKind(offer)).AttemptsPerPass : offer.Route == 0 ? Tuning.LoadsPerDemandPass : 1, offer.Haul.InterruptedJob, true);
        public void CopyOffers(PlayerId viewer, WorkCandidateMode mode, IList<WorkerDispatchOffer> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); var offers = new List<LogisticsDispatchOffer>(); BuildQueue(viewer, mode, offers);
            foreach (LogisticsDispatchOffer offer in offers) destination.Add(Assignment(offer));
        }
        public void CopyCandidates(long identity, string entry, WorkCandidateMode mode, IList<WorkerDispatchOffer> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); var offers = new List<LogisticsDispatchOffer>(); var candidates = new List<WorkerDispatchOffer>();
            ComponentList<IWorker> workers = _world.Entities.Components<IWorker>();
            for (int i = 0; i < workers.Count; i++)
            {
                BuildQueue(PlayerId.None, mode, offers, identity, entry, workers[i].Entity.Id);
                foreach (LogisticsDispatchOffer offer in offers)
                    if (offer.Haul.Status == HaulStatus.Ready) candidates.Add(Assignment(offer));
            }
            candidates.Sort((a, b) => CompareCandidates(_world, a, b));
            foreach (WorkerDispatchOffer candidate in candidates) destination.Add(candidate);
        }
        internal static int CompareCandidates(World world, WorkerDispatchOffer a, WorkerDispatchOffer b)
            => CompareCandidates(world, a.Worker, a.InterruptedJob, a.TravelCost.Value, b.Worker, b.InterruptedJob, b.TravelCost.Value);
        private static int CompareCandidates(World world, EntityId a, int interruptedA, long costA, EntityId b, int interruptedB, long costB)
        {
            int compared = (interruptedA != 0).CompareTo(interruptedB != 0); if (compared != 0) return compared;
            int priorityB = WorkPolicyRules.ReadObserved(world, world.Entities.Get(b))?.Priority ?? 0;
            int priorityA = WorkPolicyRules.ReadObserved(world, world.Entities.Get(a))?.Priority ?? 0;
            compared = priorityB.CompareTo(priorityA);
            if (compared != 0) return compared;
            compared = costA.CompareTo(costB);
            return compared != 0 ? compared : a.Value.CompareTo(b.Value);
        }
        public bool TryAcquire(long identity, string entry, EntityId worker, WorkCandidateMode mode)
        {
            if (!worker.IsValid) return false;
            var offers = new List<LogisticsDispatchOffer>(); BuildQueue(PlayerId.None, mode, offers, identity, entry, worker);
            foreach (LogisticsDispatchOffer offer in offers)
            {
                WorkerDispatchOffer current = Assignment(offer);
                if (current.Identity != identity || current.Entry != entry || !current.Ready) continue;
                if (!offer.Requested.HasValue || !offer.Requested.Value.IsPositive)
                    throw new InvalidOperationException("ready dispatch requires its actual positive quantity");
                if (current.InterruptedJob != 0) _world.Services.Get<IWorkerDispatch>().InterruptForDispatch(_world, current);
                // The fresh recovery selection may deliberately serve only part of residual cargo.
                // Preserve its policy ceiling through native admission and reservation acquisition.
                bool acquired = Acquire(offer.Company, offer.Haul.Source, offer.Destination, offer.Resource,
                    IsRecovery(offer) ? offer.Haul.Amount : offer.Requested.Value,
                    offer.Priority, offer.Route, offer.Haul.Purchase,
                    offer.Kind == LogisticsDispatchKind.CargoReturn ? _world.Entities.Get(worker) : null,
                    offer.Source, current, offer.WaitAgeTicks, worker,
                    sourcePort: offer.SourcePort, destinationPort: offer.DestinationPort,
                    destinationEndpoint: offer.DestinationEndpoint.IsValid ? offer.DestinationEndpoint : (ResourceEndpointId?)null,
                    sourceEndpoint: offer.SourceEndpoint.IsValid ? offer.SourceEndpoint : (ResourceEndpointId?)null, selection: offer.Selection) != 0;
                if (!acquired && current.InterruptedJob != 0) throw new InvalidOperationException("emergency supply lost native admission after interruption");
                return acquired;
            }
            return false;
        }
        public static readonly Code RouteDisabled = new Code("logistics.route-disabled");
        public Code CheckRoute(PlayerId company, EntityId from, EntityId to, ResourceId resource, LogisticsRouteSettings settings, Code sourcePort = default, Code destinationPort = default)
        {
            _world.RequireHealthyUpdate();
            if (!_world.ViewFor(company).CanObserveOwned(_world.Entities.Get(from)) ||
                !_world.ViewFor(company).CanObserveOwned(_world.Entities.Get(to))) return Codes.NotFound;
            return CheckRouteBinding(company, from, to, resource, settings, sourcePort, destinationPort, true);
        }
        private Code CheckRouteBinding(PlayerId company, EntityId from, EntityId to, ResourceId resource, LogisticsRouteSettings settings,
            Code sourcePort, Code destinationPort, bool requireDistinct)
        {
            if (!company.IsValid || settings == null || _world.Content.Resource(resource) == null) return Codes.InvalidArgument;
            if (!ValidSelection(_world.Content, resource, settings.Selection)) return Codes.InvalidArgument;
            if (!ResourceLotSelection.Visible(_world, company, settings.Selection)) return Codes.NotFound;
            Entity source = _world.Entities.Get(from), destination = _world.Entities.Get(to);
            if (source == null || destination == null) return Codes.NotFound;
            if (source.Owner != company || destination.Owner != company) return Codes.NotOwned;
            var sourceBinding = PhysicalPort(source, sourcePort, resource);
            if (!sourceBinding.Ready) return sourceBinding.Blocker;
            var destinationBinding = PhysicalPort(destination, destinationPort, resource);
            if (!destinationBinding.Ready) return destinationBinding.Blocker;
            IResourceOutlet outlet = sourceBinding.Store as IResourceOutlet;
            IResourceInlet inlet = destinationBinding.Store;
            if (outlet == null) return StandardCodes.NoStorage;
            if (!settings.Selection.IsNone && (!(outlet is IResourceLotEndpoint) || !(inlet is IResourceLotEndpoint))) return QualityIncompatible;
            if (requireDistinct && outlet.Endpoint == inlet.Endpoint) return Codes.InvalidArgument;
            int sourceVersion = outlet.Version, destinationVersion = inlet.Version;
            ResourceEndpointId sourceIdentity = outlet.Endpoint, destinationIdentity = inlet.Endpoint;
            try
            {
            if (!outlet.Accepts(_world.Content, resource) || !inlet.Accepts(_world.Content, resource)) return StandardCodes.NotAccepted;
            CapacityQuantum native = CapacityQuantum.Combine(CapacityPolicyContracts.Quantum(outlet.CapacityPolicy, resource), CapacityPolicyContracts.Quantum(inlet.CapacityPolicy, resource));
            Code result = native.Floor(settings.Batch) == settings.Batch ? Code.None : Codes.InvalidArgument;
            return result;
            }
            finally
            {
                if (outlet.Version != sourceVersion || inlet.Version != destinationVersion || outlet.Endpoint != sourceIdentity || inlet.Endpoint != destinationIdentity ||
                    outlet.Company != company || inlet.Company != company) throw new InvalidOperationException("route admission mutated actual endpoints");
            }
        }
        public Code CheckRouteSettings(PlayerId company, int route, LogisticsRouteSettings settings)
        {
            _world.RequireHealthyUpdate();
            Route? current = FindRoute(route); if (!current.HasValue) return Codes.NotFound;
            if (company != current.Value.Owner ||
                !_world.ViewFor(company).CanObserveOwned(_world.Entities.Get(current.Value.From)) ||
                !_world.ViewFor(company).CanObserveOwned(_world.Entities.Get(current.Value.To))) return Codes.NotFound;
            return CheckRouteBinding(company, current.Value.From, current.Value.To, current.Value.Resource, settings, current.Value.SourcePort, current.Value.DestinationPort, false);
        }
        public Code CheckRemoveRoute(PlayerId company, int route)
        {
            _world.RequireHealthyUpdate();
            Route? current = FindRoute(route); return !current.HasValue || current.Value.Owner != company ? Codes.NotFound : Code.None;
        }
        public Code ConfigureRoute(PlayerId company, int route, LogisticsRouteSettings settings)
        {
            Code refusal = CheckRouteSettings(company, route, settings); if (!refusal.IsNone) return refusal;
            for (int i = 0; i < _routes.Count; i++)
                if (_routes[i].Id == route) { Route current = _routes[i]; current.Settings = settings; _routes[i] = current; return Code.None; }
            throw new InvalidOperationException("admitted route disappeared");
        }
        internal void ValidateLoadedRoutes()
        {
            foreach (Route route in _routes)
                if (_world.Entities.Get(route.From) != null && _world.Entities.Get(route.To) != null &&
                    !CheckRouteBinding(route.Owner, route.From, route.To, route.Resource, route.Settings, route.SourcePort, route.DestinationPort, false).IsNone)
                    throw new CorruptStateException("saved route is incompatible with its native endpoints");
        }
        public void CopyQueue(PlayerId viewer, IList<LogisticsDispatchOffer> destination)
            => BuildQueue(viewer, WorkCandidateMode.IdleOnly, destination);

        private void BuildQueue(PlayerId viewer, WorkCandidateMode mode, IList<LogisticsDispatchOffer> destination,
            long? identity = null, string entry = null, EntityId worker = default)
        {
            if (mode != WorkCandidateMode.IdleOnly && mode != WorkCandidateMode.EmergencyInterruption) throw new ArgumentOutOfRangeException(nameof(mode));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            // Aggregation is temporary. Existing demand rows retain policy, age and source ownership.
            var groups = new Dictionary<(ResourceEndpointId, ResourceId, Code), Demand>();
            foreach (Demand demand in _demands)
            {
                if (viewer.IsValid && demand.Owner != viewer || !DemandActive(demand) || !demand.Settings.Desired.IsPositive) continue;
                var binding = PhysicalPort(_world.Entities.Get(demand.Store), demand.Port, demand.Resource, demand.Endpoint);
                if (binding.Ready && (demand.Source.Scope == StockDemandScope && !demand.Replenishing ||
                    CheckSource(demand).Emergency && !DeliveryGap(binding.Store, demand.Resource, true).IsPositive)) continue;
                var key = (demand.Endpoint, demand.Resource, demand.Settings.Selection);
                if (!groups.TryGetValue(key, out Demand current) || CompareDemand(demand, current) < 0) groups[key] = demand;
            }
            var offers = new List<LogisticsDispatchOffer>();
            foreach (Demand demand in groups.Values)
            {
                string demandEntry = DemandEntry(demand.Resource, demand.Port, demand.Endpoint, demand.Settings.Selection);
                if (identity.HasValue && (identity.Value != demand.Store.Value || entry != demandEntry)) continue;
                Entity store = _world.Entities.Get(demand.Store);
                var binding = PhysicalPort(store, demand.Port, demand.Resource, demand.Endpoint);
                LogisticsDemandSourceCheck check = CheckSource(demand);
                if (!binding.Ready)
                {
                    offers.Add(new LogisticsDispatchOffer(demand.Source, 0, demand.Owner, demand.Store, demand.Resource,
                        null, demand.Settings.Priority, demand.CreatedTick, _world.Tick,
                        new HaulPreview(HaulStatus.PolicyBlocked, blocker: binding.Blocker), check.Emergency, demand.LastDispatchTick,
                        destinationPort: demand.Port, destinationEndpoint: demand.Endpoint, selection: demand.Settings.Selection));
                    continue;
                }
                Fix gap = DeliveryGap(demand, check.Emergency);
                if (!gap.IsPositive) continue;
                var intent = new WorkerDispatchOffer(DispatchScope, demand.Store.Value,
                    demandEntry,
                    demand.Owner, demand.Store, EntityId.None, false, WorkSystem.BlockerNoWorker, check.Emergency,
                    demand.Settings.Priority, demand.CreatedTick, _world.Tick, null, Tuning.LoadsPerDemandPass, 0, true);
                HaulPreview haul = PreviewSource(store, demand.Resource, demand.Owner, gap, demand.Source, intent, mode, checked(_world.Tick - demand.LastDispatchTick), worker, demand.Port, demand.Endpoint, demand.Settings.Selection);
                offers.Add(new LogisticsDispatchOffer(demand.Source, 0, demand.Owner, demand.Store, demand.Resource,
                    gap, demand.Settings.Priority, demand.CreatedTick, _world.Tick, haul, check.Emergency, demand.LastDispatchTick, destinationPort: demand.Port, destinationEndpoint: demand.Endpoint, sourcePort: haul.SourcePort, sourceEndpoint: haul.SourceEndpoint, selection: demand.Settings.Selection));
            }
            foreach (Route route in _routes)
            {
                if (identity.HasValue && (identity.Value != route.Id || entry != "route")) continue;
                if (viewer.IsValid && route.Owner != viewer) continue;
                var intent = new WorkerDispatchOffer(DispatchScope, route.Id, "route", route.Owner, route.To, EntityId.None,
                    false, DispatchUnavailable, false, route.Settings.Priority, route.CreatedTick, _world.Tick, null, 1, 0, true);
                HaulPreview haul = route.Settings.Enabled ? Preview(route.Owner, route.From, route.To, route.Resource, route.Settings.Batch,
                    false, null, intent: intent, mode: mode, waitAge: checked(_world.Tick - route.LastDispatchTick), selectedWorker: worker, sourcePort: route.SourcePort, destinationPort: route.DestinationPort, selection: route.Settings.Selection)
                    : new HaulPreview(HaulStatus.PolicyBlocked, blocker: RouteDisabled);
                offers.Add(new LogisticsDispatchOffer(default, route.Id, route.Owner, route.To, route.Resource,
                    route.Settings.Batch, route.Settings.Priority, route.CreatedTick, _world.Tick, haul, false, route.LastDispatchTick, destinationPort: route.DestinationPort,
                    destinationEndpoint: PhysicalPort(_world.Entities.Get(route.To), route.DestinationPort, route.Resource).Store?.Endpoint ?? default,
                    sourcePort: route.SourcePort, sourceEndpoint: PhysicalPort(_world.Entities.Get(route.From), route.SourcePort, route.Resource).Store?.Endpoint ?? default, selection: route.Settings.Selection));
            }
            AddRecoveries(viewer, mode, offers, identity, entry, worker);
            offers.Sort((a, b) => WorkSystem.CompareAssignments(Assignment(a), Assignment(b)));
            foreach (LogisticsDispatchOffer offer in offers) destination.Add(offer);
        }
        private static string DemandEntry(ResourceId resource, Code port, ResourceEndpointId endpoint, Code selection = default)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            string name = port.IsNone ? "" : port.Id, scope = endpoint.Scope;
            if (!endpoint.IsValid) throw new InvalidOperationException("demand dispatch requires an actual endpoint");
            return "demand.r-" + resource.Value.ToString(culture) + ".p-" + name.Length.ToString(culture) + "-" + name +
                ".s-" + scope.Length.ToString(culture) + "-" + scope + ".i-" + endpoint.Identity.ToString(culture) +
                (selection.IsNone ? "" : ".q-" + selection.Id.Length.ToString(culture) + "-" + selection.Id);
        }
        private int CompareDemand(Demand a, Demand b)
        {
            int result = CheckSource(b).Emergency.CompareTo(CheckSource(a).Emergency); if (result != 0) return result;
            result = b.Settings.Priority.CompareTo(a.Settings.Priority); if (result != 0) return result;
            result = a.CreatedTick.CompareTo(b.CreatedTick); if (result != 0) return result;
            result = string.CompareOrdinal(a.Source.Scope, b.Source.Scope);
            if (result != 0) return result;
            result = a.Source.Identity.CompareTo(b.Source.Identity);
            if (result != 0) return result;
            result = string.CompareOrdinal(a.Source.Entry.Id, b.Source.Entry.Id); if (result != 0) return result;
            result = a.Store.Value.CompareTo(b.Store.Value); if (result != 0) return result;
            result = string.CompareOrdinal(a.Port.Id, b.Port.Id); return result != 0 ? result : string.CompareOrdinal(a.Settings.Selection.Id, b.Settings.Selection.Id);
        }
        private void RecordDispatch(int route, ResourceEndpointId destination, ResourceId resource, Code selection)
        {
            if (route != 0)
            {
                for (int i = 0; i < _routes.Count; i++) if (_routes[i].Id == route)
                { Route current = _routes[i]; current.LastDispatchTick = _world.Tick; _routes[i] = current; return; }
                throw new InvalidOperationException("admitted standing route disappeared");
            }
            for (int i = 0; i < _demands.Count; i++)
                if (_demands[i].Endpoint == destination && _demands[i].Resource == resource && _demands[i].Settings.Selection == selection && DemandActive(_demands[i]))
                { Demand current = _demands[i]; current.LastDispatchTick = _world.Tick; _demands[i] = current; }
        }
    }
}
