using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{

    public enum HaulStatus : byte { InvalidRequest, NotFound, NotOwned, NoSupply, NoCapacity, NoCarrier, Unreachable, Ready, NoDestination, PolicyBlocked }

    /// <summary>Detached engine decision; not a reservation, mutable job or host-side rule.</summary>
    public readonly struct HaulPreview
    {
        public HaulStatus Status { get; }
        public EntityId Source { get; }
        public EntityId Destination { get; }
        public EntityId Carrier { get; }
        public ResourceId Resource { get; }
        public Fix Amount { get; }
        /// <summary>Exact common source/cargo/destination native quantity step; valid only on Ready.</summary>
        public CapacityQuantum QuantityQuantum { get; }
        public Code SourcePort { get; }
        public Code DestinationPort { get; }
        public ResourceEndpointId SourceEndpoint { get; }
        public ResourceEndpointId DestinationEndpoint { get; }
        /// <summary>Actual selected requester provenance, including recovery serving existing demand.</summary>
        public LogisticsDemandId HaulDemand { get; }
        public Code Selection { get; }
        public bool Purchase { get; }
        public Code Blocker { get; }
        public long? TravelCost { get; }
        public Code SourceDockSlot { get; }
        public Code DestinationDockSlot { get; }
        public int InterruptedJob { get; }
        /// <summary>Selected complete conditional movement duration in the sole game clock's update intervals;
        /// excludes handling/service/queue delay. Null on refusal; an unsupported leg makes the whole duration unknown.</summary>
        public MotionTravelEstimate? Travel { get; }
        /// <summary>Selected authored trip admission; null when no trip policy exists or the haul refuses. No future fuel promise.</summary>
        public HaulTripEvaluation TripBudget { get; }
        // Exact immutable inputs consulted by this derived decision, not saved policy state.
        // Refusals may have no selected carrier, so rediscovering dependencies from Carrier is wrong.
        internal IReadOnlyList<HaulLoadSettings> LoadPolicyInputs { get; }
        internal IReadOnlyList<(EntityId Entity, Code Source)> ServiceInputs { get; }
        internal HaulPreview(HaulStatus status, EntityId source = default, EntityId destination = default,
            EntityId carrier = default, ResourceId resource = default, Fix amount = default, bool purchase = false, Code blocker = default, long? travelCost = null,
            Code sourceDockSlot = default, Code destinationDockSlot = default, int interruptedJob = 0, MotionTravelEstimate? travel = null,
            HaulTripEvaluation tripBudget = null, CapacityQuantum quantityQuantum = default,
            Code sourcePort = default, Code destinationPort = default, ResourceEndpointId sourceEndpoint = default, ResourceEndpointId destinationEndpoint = default,
            LogisticsDemandId haulDemand = default, Code selection = default)
        {
            if (status == HaulStatus.Ready && (!amount.IsPositive || quantityQuantum.Status != CapacityStatus.Fits ||
                quantityQuantum.Raw <= 0 || quantityQuantum.Floor(amount) != amount))
                throw new ArgumentException("ready haul requires its exact common native quantity quantum");
            if (status != HaulStatus.Ready && (quantityQuantum.Status != default || quantityQuantum.Raw != 0))
                throw new ArgumentException("refused haul cannot fabricate an admitted quantity quantum");
            SourcePort = sourcePort; DestinationPort = destinationPort; SourceEndpoint = sourceEndpoint; DestinationEndpoint = destinationEndpoint; HaulDemand = haulDemand; Selection = selection;
            LoadPolicyInputs = Array.Empty<HaulLoadSettings>();
            ServiceInputs = Array.Empty<(EntityId, Code)>();
            Status = status; Source = source; Destination = destination; Carrier = carrier; Resource = resource; Amount = amount; QuantityQuantum = quantityQuantum; Purchase = purchase; Blocker = blocker; TravelCost = travelCost;
            SourceDockSlot = sourceDockSlot; DestinationDockSlot = destinationDockSlot; InterruptedJob = interruptedJob; Travel = travel; TripBudget = tripBudget; }
        private HaulPreview(HaulPreview source, List<HaulLoadSettings> inputs)
        { this = source; LoadPolicyInputs = Array.AsReadOnly(inputs.ToArray()); }
        internal HaulPreview WithLoadPolicyInputs(List<HaulLoadSettings> inputs)
            => inputs == null || inputs.Count == 0 ? this : new HaulPreview(this, inputs);
        internal void CopyLoadPolicyInputs(ref List<HaulLoadSettings> destination)
        {
            if (LoadPolicyInputs == null || LoadPolicyInputs.Count == 0) return;
            destination ??= new List<HaulLoadSettings>();
            foreach (var input in LoadPolicyInputs) if (!destination.Contains(input)) destination.Add(input);
        }
        private HaulPreview(HaulPreview source, List<(EntityId Entity, Code Source)> inputs)
        { this = source; ServiceInputs = inputs.AsReadOnly(); }
        internal HaulPreview WithServiceInputs(List<(EntityId Entity, Code Source)> inputs)
            => inputs == null || inputs.Count == 0 ? this : new HaulPreview(this, new List<(EntityId Entity, Code Source)>(inputs));
        internal void CopyServiceInputs(ref List<(EntityId Entity, Code Source)> destination)
        {
            if (ServiceInputs == null || ServiceInputs.Count == 0) return;
            destination ??= new List<(EntityId Entity, Code Source)>();
            foreach (var input in ServiceInputs) if (!destination.Contains(input)) destination.Add(input);
        }
    }

    /// <summary>
    /// Everything that moves goods between stores: standing demands ("this store wants 40 steel"),
    /// standing routes ("carry crude from this tank to that terminal in loads of 20"), and the hauls in
    /// flight. Carriers are machines with the "haul" role and a cargo hold; every load is carried.
    /// </summary>
    public sealed partial class LogisticsService : IPersistentService, ITransformationSupplyRequests, IWorkSupplyRequests, IHaulExecution, IHaulPlayerControls, ICompositionValidator, ILogisticsDemandRequests, ILogisticsRoutes, ILogisticsDemandOwner, ILogisticsStockPolicies, ILogisticsDispatchQueue, ILogisticsDemandSources, IWorkerDispatchSource, ILogisticsDiagnostics, IEntityReplacementReferenceOwner
    {
        public int StateSchemaVersion => 27;
        public static readonly WorkerRoleId HaulRole = new WorkerRoleId("haul");

        public struct Demand : ILogisticsDemand
        {
            public EntityId Store;
            public Code Port;
            public ResourceEndpointId Endpoint;
            public ResourceId Resource;
            public LogisticsDemandSettings Settings;
            public bool Replenishing;
            public PlayerId Owner;
            public LogisticsDemandId Source;
            public long CreatedTick;
            public long LastDispatchTick;
            public LogisticsDemandSettings ReadDemand() => Settings;
        }

        public struct Route
        {
            public int Id;
            public PlayerId Owner;
            public EntityId From;
            public EntityId To;
            public Code SourcePort;
            public Code DestinationPort;
            public ResourceId Resource;
            public LogisticsRouteSettings Settings;
            public long CreatedTick;
            public long LastDispatchTick;
        }

        private readonly World _world;
        private readonly SimulationOperation _operation = new SimulationOperation();
        public bool OperationInProgress { get { _world.RequireHealthyUpdate(); return _operation.Active; } }
        private SimulationOperation.Scope BeginHaulOperation()
        {
            _world.RequireHealthyUpdate();
            if (_operation.Active) throw new InvalidOperationException("unfinished logistics operation cannot be reentered");
            return _operation.Enter();
        }
        private readonly List<Demand> _demands = new List<Demand>();
        private readonly List<Route> _routes = new List<Route>();
        private int _nextRoute = 1;

        public LogisticsService(World world) { _world = world; }

        public string Id => "sim.logistics";
        public void ValidateComposition(World world)
        {
            foreach (var profile in world.Content.All<ILogisticsPolicyDefinition>())
                EntityObservation.ReadProvider(world, profile, null, () => {
                    if (!(profile is Def actual) || !actual.IsFrozen) throw new ContentException("logistics profile must be actual frozen content");
                    LogisticsPolicyDeclaration.Validate(world.Content, profile); return true;
                });
            HaulHandling.ValidateDefinitions(world);
            ValidateRecoveryPolicy();
            ValidateDemandOwners();
            foreach (EntityDef entity in world.Content.All<EntityDef>())
            {
                ISpatialPresenceDefinition presence = entity.Component<ISpatialPresenceDefinition>();
                if (presence != null && presence.Space != world.Settings.Space) continue;
                var policy = entity.Component<IHaulLoadPolicyDefinition>();
                if (entity.HasCapability<IHaulLoadPolicy>() != (policy != null)) throw new ContentException(entity.Id + ": load policy metadata/provider must correspond");
                if (policy != null)
                {
                    if (policy.InitialProfile == null || !ReferenceEquals(world.Content.Find<IHaulLoadProfile>(policy.InitialProfile.Id), policy.InitialProfile))
                        throw new ContentException(entity.Id + ": load profile must alias actual content");
                    EntityObservation.ReadProvider(world, policy.InitialProfile, null, () => {
                        HaulLoadPolicyContracts.ValidateDefinition(world.Content, world.Time, entity, policy.InitialProfile); return true;
                    });
                }
                var trip = entity.Component<IHaulTripPolicyDefinition>();
                if (entity.HasCapability<IHaulTripPolicy>() != (trip != null)) throw new ContentException(entity.Id + ": trip metadata/provider must correspond");
                if (trip != null)
                {
                    if (trip.InitialProfile == null) throw new ContentException(entity.Id + ": explicit trip profile required");
                    EntityObservation.ReadProvider(world, trip.InitialProfile, null, () => {
                        HaulTripPolicyContracts.ValidateDefinition(world.Content, entity, trip.InitialProfile); return true;
                    });
                }
            }
        }
        public IReadOnlyList<Demand> Demands => _demands.AsReadOnly();
        public IReadOnlyList<Route> Routes => _routes.AsReadOnly();

        public void RequestInputs(ITransformer transformer, IReadOnlyList<LogisticsSupplyRequest> inputs, int priority)
        {
            LogisticsDemandId source = TransformationContracts.SupplySource(_world, transformer);
            Code refusal = ReplaceRequests(source, transformer.Entity.Owner, transformer.Entity.Id, inputs, priority);
            if (!refusal.IsNone) throw new InvalidOperationException("transformer request refused: " + refusal);
        }
        public void WithdrawInputs(ITransformer transformer) => WithdrawRequests(TransformationContracts.SupplySource(_world, transformer));
        public void RequestInputs(int order, Entity target, IReadOnlyList<LogisticsSupplyRequest> inputs, int priority)
        {
            Code refusal = ReplaceRequests(new LogisticsDemandId(WorkModule.ModuleId, order, Code.None), target.Owner, target.Id, inputs, priority);
            if (!refusal.IsNone) throw new InvalidOperationException("work request refused: " + refusal);
        }
        public void WithdrawInputs(int order) => WithdrawRequests(new LogisticsDemandId(WorkModule.ModuleId, order, Code.None));

        // ------------------------------------------------------------------ standing orders

        /// <summary>
        /// Sets how much of an resource one asker wants a store to hold (0 withdraws that asker's demand). The
        /// store's total want is the sum over its askers, so an order's materials and a player's stock
        /// target on the same store never overwrite each other.
        /// </summary>
        public Code SetRequest(LogisticsDemandId source, PlayerId company, EntityId destination, ResourceId resource, Fix level, int priority)
        {
            if (source.Scope == StockDemandScope) return LogisticsPolicyCodes.StockPolicyOwner;
            if (_world.Content.Resource(resource) == null || level < Fix.Zero) return Codes.InvalidArgument;
            Bill request = level.IsZero ? Bill.Empty : new Bill(new[] { new ResourceAmount(resource, level) });
            Code refusal = CheckRequests(source, company, destination, new[] { LogisticsSupplyRequest.Entity(destination, request) }, priority);
            if (!refusal.IsNone) return refusal;
            UpdateDemand(_world.Entities.Get(destination), resource, level, priority, source, Code.None, ResourceEndpointId.ForEntity(destination)); return Code.None;
        }
        private void UpdateDemand(Entity store, ResourceId resource, Fix level, int priority, LogisticsDemandId source, Code port, ResourceEndpointId endpoint, Code selection = default)
        {
            for (int i = 0; i < _demands.Count; i++)
            {
                if (_demands[i].Store != store.Id || _demands[i].Endpoint != endpoint || _demands[i].Resource != resource || _demands[i].Settings.Selection != selection || !_demands[i].Source.Equals(source)) continue;
                if (level <= Fix.Zero) _demands.RemoveAt(i);
                else { Demand current = _demands[i]; current.Port = port; current.Settings = new LogisticsDemandSettings(true, level, level, level, priority, LogisticsReserveAccess.Protected, selection); _demands[i] = current; }
                return;
            }
            if (level > Fix.Zero) _demands.Add(MakeDemand(store, resource, new LogisticsDemandSettings(true, level, level, level, priority, LogisticsReserveAccess.Protected, selection), source, _world.Tick, port, endpoint));
        }

        private static Demand MakeDemand(Entity store, ResourceId resource, LogisticsDemandSettings settings, LogisticsDemandId source, long created, Code port, ResourceEndpointId endpoint)
            => new Demand { Store = store.Id, Port = port, Endpoint = endpoint, Resource = resource, Settings = settings, Owner = store.Owner, Source = source, CreatedTick = created, LastDispatchTick = created };

        /// <summary>What a store wants to hold of an resource, over all askers.</summary>
        public Fix DemandOf(EntityId store, ResourceId resource)
            => DemandOf(ResourceEndpointId.ForEntity(store), resource, false);
        private Fix DemandOf(EntityId store, ResourceId resource, bool releaseReserve)
            => DemandOf(ResourceEndpointId.ForEntity(store), resource, releaseReserve);
        private Fix DemandOf(ResourceEndpointId endpoint, ResourceId resource, bool releaseReserve)
        {
            Fix total = Fix.Zero;
            for (int i = 0; i < _demands.Count; i++) if (_demands[i].Endpoint == endpoint && _demands[i].Resource == resource &&
                DemandActive(_demands[i]) && !(releaseReserve && _demands[i].Source.Scope == StockDemandScope &&
                    _demands[i].Settings.ReserveAccess == LogisticsReserveAccess.EmergencyOnly)) total += _demands[i].Settings.Desired;
            return total;
        }

        /// <summary>What a store may give away: what it holds, unpromised, above what it wants for itself.</summary>
        public Fix Spare(IOutletPort store, ResourceId resource)
            => Spare(store, resource, false);
        private Fix Spare(IOutletPort store, ResourceId resource, bool emergency)
        {
            OutletPolicySettings settings = LogisticsPolicyRules.Read(_world, store.Entity);
            if (settings != null && !settings.Enabled) return Fix.Zero;
            Fix protectedStock = ProtectedStock(store, resource, settings, emergency);
            return Fix.Max(Fix.Zero, store.Available(resource) - protectedStock);
        }

        public int AddRoute(PlayerId owner, EntityId from, EntityId to, ResourceId resource, LogisticsRouteSettings settings, Code sourcePort = default, Code destinationPort = default)
        {
            Code refusal = CheckRouteBinding(owner, from, to, resource, settings, sourcePort, destinationPort, true);
            if (!refusal.IsNone) throw new InvalidOperationException("route refused: " + refusal);
            int id = _nextRoute; _nextRoute = checked(_nextRoute + 1);
            var r = new Route { Id = id, Owner = owner, From = from, To = to, SourcePort = sourcePort, DestinationPort = destinationPort, Resource = resource, Settings = settings, CreatedTick = _world.Tick, LastDispatchTick = _world.Tick };
            _routes.Add(r);
            return r.Id;
        }

        public bool RemoveRoute(int id)
        {
            for (int i = 0; i < _routes.Count; i++) if (_routes[i].Id == id) { _routes.RemoveAt(i); return true; }
            return false;
        }

        public Route? FindRoute(int id)
        {
            for (int i = 0; i < _routes.Count; i++) if (_routes[i].Id == id) return _routes[i];
            return null;
        }

        // ------------------------------------------------------------------ intent maintenance before shared machine arbitration

        public void PrepareDispatch()
        {
            HaulLoadPolicyContracts.ValidateLive(_world);
            HaulTripPolicyContracts.ValidateLive(_world);
            // Demands and routes of removed stores lapse.
            _demands.RemoveAll(d =>
            {
                var check = CheckSource(d);
                if (check.Status == LogisticsDemandStatus.Invalid) throw new InvalidOperationException("invalid demand owner: " + check.Refusal);
                return check.Status == LogisticsDemandStatus.Expired || _world.Entities.Get(d.Store) == null;
            });
            _routes.RemoveAll(r => _world.Entities.Get(r.From) == null || _world.Entities.Get(r.To) == null);
            RefreshStockBands();
            RefreshRecoveries();
        }

        internal void RemoveEmptyEvacuationContainers(World world, WorkBoard board)
        {
            ComponentList<IInventory> stores = world.Entities.Components<IInventory>();
            for (int i = 0; i < stores.Count; i++)
            {
                IInventory pile = stores[i];
                if (!pile.Evacuate || !pile.Entity.Alive) continue;
                if (pile.Stock.Count == 0)
                {
                    // Emptied and nothing promised to it: a salvage pile is gone once carried away.
                    bool pending = false;
                    IReadOnlyList<Job> jobs = board.Jobs;
                    for (int h = 0; h < jobs.Count; h++)
                        if (jobs[h].IsLive && jobs[h].IsHaul && (jobs[h].From == pile.Entity.Id || jobs[h].To == pile.Entity.Id)) pending = true;
                    if (!pending && world.CheckDespawn(pile.Entity).IsNone) world.Despawn(pile.Entity);
                    continue;
                }
            }
        }

        public HaulPreview PreviewHaul(PlayerId owner, EntityId from, EntityId to, ResourceId resource, Fix ceiling, bool purchase = false, EntityId worker = default, Code sourcePort = default, Code destinationPort = default, Code selection = default)
            => Preview(owner, from, to, resource, ceiling, purchase, null, selectedWorker: worker, sourcePort: sourcePort, destinationPort: destinationPort, selection: selection);
        public HaulPreview PreviewPlayerHaul(PlayerId owner, EntityId from, EntityId to, ResourceId resource, Fix ceiling, bool purchase = false, EntityId worker = default, Code sourcePort = default, Code destinationPort = default, int priority = 0, Code selection = default)
        {
            _world.RequireHealthyUpdate();
            var view = _world.ViewFor(owner); Entity source = _world.Entities.Get(from), destination = _world.Entities.Get(to);
            if (!_world.Players.IsSeat(owner) || !view.CanObserveOwned(destination) ||
                (purchase ? source == null || !source.Alive || !view.CanSee(source) : !view.CanObserveOwned(source)) ||
                worker.IsValid && !view.CanObserveOwned(_world.Entities.Get(worker))) return new HaulPreview(HaulStatus.NotFound);
            var requested = _world.Content.Resource(resource);
            if (requested != null && !view.CanSeeContent(requested)) return new HaulPreview(HaulStatus.NotFound);
            if (!ResourceLotSelection.Visible(_world, owner, selection)) return new HaulPreview(HaulStatus.NotFound);
            if (priority < 0) return new HaulPreview(HaulStatus.InvalidRequest);
            var facts = EntityObservation.ReadProvider(_world, this, destination,
                () => Preview(owner, from, to, resource, ceiling, purchase, null, selectedWorker: worker, sourcePort: sourcePort, destinationPort: destinationPort, selection: selection),
                space: _world.Space, observeTargetState: true);
            return facts.Carrier.IsValid && !view.CanObserveOwned(_world.Entities.Get(facts.Carrier)) ||
                !HaulTripPolicyContracts.CanDisclose(_world, owner, facts.TripBudget) ||
                !RecurringPlanContracts.CanDisclose(_world, owner, facts.ServiceInputs) ? new HaulPreview(HaulStatus.NotFound) : facts;
        }

        private HaulPreview Preview(PlayerId owner, EntityId fromId, EntityId toId, ResourceId resource, Fix ceiling,
            bool purchase, Entity returning, LogisticsDemandId demand = default,
            WorkerDispatchOffer? intent = null, WorkCandidateMode mode = WorkCandidateMode.IdleOnly, long waitAge = 0, EntityId selectedWorker = default, Job continuing = null, Code sourcePort = default, Code destinationPort = default, ResourceEndpointId? destinationEndpoint = null, ResourceEndpointId? sourceEndpoint = null, Code selection = default)
        {
            World world = _world;
            if (!owner.IsValid || !ceiling.IsPositive || selectedWorker.Value < 0 || world.Content.Resource(resource) == null) return new HaulPreview(HaulStatus.InvalidRequest);
            if (!ValidSelection(world.Content, resource, selection)) return new HaulPreview(HaulStatus.InvalidRequest);
            if (purchase && !selection.IsNone) return new HaulPreview(HaulStatus.PolicyBlocked, blocker: QualityIncompatible);
            if (purchase && !sourcePort.IsNone || returning != null && !sourcePort.IsNone)
                return new HaulPreview(HaulStatus.InvalidRequest);
            bool emergency = false;
            if (demand.IsValid)
            {
                LogisticsDemandSourceCheck check = InspectSource(demand, owner, toId);
                if (check.Status == LogisticsDemandStatus.Invalid || continuing == null && check.Status != LogisticsDemandStatus.Active)
                    return new HaulPreview(HaulStatus.InvalidRequest, blocker: check.Refusal);
                emergency = check.Emergency;
                if (emergency && continuing == null)
                {
                    Demand? currentDemand = null;
                    foreach (Demand row in _demands) if (row.Source.Equals(demand) && row.Store == toId && row.Resource == resource &&
                        row.Settings.Selection == selection &&
                        (!destinationEndpoint.HasValue || row.Endpoint == destinationEndpoint.Value)) { currentDemand = row; break; }
                    ceiling = currentDemand.HasValue ? Fix.Min(ceiling, DeliveryGap(currentDemand.Value, true)) : Fix.Zero;
                    if (!ceiling.IsPositive) return new HaulPreview(HaulStatus.NoSupply);
                }
            }
            Entity from = world.Entities.Get(fromId), to = world.Entities.Get(toId);
            if (from == null || to == null) return new HaulPreview(HaulStatus.NotFound);
            if (to.Owner != owner || !purchase && from.Owner != owner) return new HaulPreview(HaulStatus.NotOwned);
            var destinationBinding = PhysicalPort(to, destinationPort, resource, destinationEndpoint);
            if (!destinationBinding.Ready) return new HaulPreview(HaulStatus.NoCapacity, blocker: destinationBinding.Blocker);
            IResourceInlet inlet = destinationBinding.Store;
            var sourceBinding = returning == null ? PhysicalPort(from, sourcePort, resource, sourceEndpoint) : default;
            if (returning == null && !purchase && !sourceBinding.Ready) return new HaulPreview(HaulStatus.NoSupply, blocker: sourceBinding.Blocker);
            IResourceOutlet outlet = returning == null ? sourceBinding.Store as IResourceOutlet : TransporterContracts.ReadCargo(_world, returning);
            if (returning != null && (outlet as IInventory)?.Entity != from) return new HaulPreview(HaulStatus.InvalidRequest);
            ISupplier supplier = purchase ? from.Get<ISupplier>() : null;
            int? inletVersion = inlet?.Version, outletVersion = outlet?.Version;
            ResourceEndpointId inletIdentity = inlet?.Endpoint ?? default, outletIdentity = outlet?.Endpoint ?? default;
            PlayerId inletCompany = inlet?.Company ?? default, outletCompany = outlet?.Company ?? default;
            try
            {
            if (inlet == null || !inlet.Accepts(world.Content, resource)) return new HaulPreview(HaulStatus.NoCapacity);
            bool retainedDestination = continuing != null && inlet.Endpoint == continuing.DestinationEndpoint;
            if (retainedDestination)
            {
                ResourceReceiptCheck receipt = inlet.CheckReservedIn(world.Content, continuing.ReservationOwner, continuing.InReservation);
                if (!receipt.Ready || receipt.Record.Resource != resource || receipt.Record.Amount != continuing.Loaded)
                    throw new InvalidOperationException("continuing delivery lost its original destination promise");
            }
            if (purchase && supplier == null || !purchase && outlet == null) return new HaulPreview(HaulStatus.NoSupply);
            if (!selection.IsNone && (!(outlet is IResourceLotEndpoint) || !(inlet is IResourceLotEndpoint))) return new HaulPreview(HaulStatus.PolicyBlocked, blocker: QualityIncompatible);
            if (!purchase && outlet.Endpoint == inlet.Endpoint) return new HaulPreview(HaulStatus.InvalidRequest);
            if (purchase && returning == null)
            {
                ResourceTransferStatus trading = ResourceTransactions.TradingAdmission(world);
                if (trading != ResourceTransferStatus.Ready)
                    return new HaulPreview(HaulStatus.PolicyBlocked, blocker: trading == ResourceTransferStatus.TradingDisabled
                        ? ResourceTransactions.TradingDisabled : ResourceTransactions.UnavailableTradingCapability);
            }
            Code export = ExportPermission(from, to, sourcePort);
            if (!export.IsNone) return new HaulPreview(HaulStatus.PolicyBlocked, blocker: export);
            Fix supplied = continuing != null ? Fix.Min(ceiling, continuing.Loaded)
                : purchase ? supplier.Offer(resource, ceiling) : Fix.Min(ceiling, PortSpare(from, outlet, resource, emergency, sourcePort));
            if (purchase && supplier.SupplyKind == ResourceSupplyKind.ExistingStock)
                supplied = Fix.Min(supplied, Spare(supplier.SupplyStock, resource, emergency));
            if (!selection.IsNone)
            {
                Fix eligible = continuing == null ? QualifiedAvailable(outlet, resource, selection)
                    : ResourceLotSelection.EligibleAmount(world.Content, ((IResourceLotEndpoint)outlet).SelectLots(continuing.ReservationOwner, continuing.CargoOutReservation, continuing.Loaded), resource, SelectedClaims(resource, selection)[0]);
                supplied = Fix.Min(supplied, eligible);
                if (continuing != null && supplied != continuing.Loaded) return new HaulPreview(HaulStatus.NoSupply, blocker: QualityIncompatible);
            }
            if (supplied <= Fix.Zero) return new HaulPreview(HaulStatus.NoSupply,
                blocker: outlet?.Available(resource).IsPositive == true ? LogisticsPolicyCodes.Reserve : Code.None);
            // A continuing original delivery already owns its capacity. Its own receipt is not
            // fresh demand, and a later stock-band change cannot withdraw an acquired promise.
            Fix receiving = retainedDestination ? supplied : ReceivingCeiling(inlet, resource, supplied);
            if (!receiving.IsPositive) return new HaulPreview(HaulStatus.PolicyBlocked, blocker: LogisticsPolicyCodes.Maximum);
            CapacityQuantum sourceQuantum = purchase ? supplier.SupplyQuantum(resource) : CapacityPolicyContracts.Quantum(outlet.CapacityPolicy, resource);
            var requester = continuing?.ReservationOwner ?? new ReservationOwner("sim.haul", world.Services.Get<WorkBoard>().NextJobId);
            IWorkerDispatch dispatch = world.Services.Get<IWorkerDispatch>();
            ComponentList<IWorker> workers = world.Entities.Components<IWorker>();
            IWorker explicitWorker = selectedWorker.IsValid ? world.Entities.Get(selectedWorker)?.Get<IWorker>() : null;
            int workerCount = selectedWorker.IsValid ? explicitWorker == null ? 0 : 1 : workers.Count;
            Entity best = null; Fix bestAmount = Fix.Zero; long bestCost = long.MaxValue; int bestPriority = 0, bestInterrupted = 0;
            MotionTravelEstimate? bestTravel = null;
            HaulTripEvaluation bestTripBudget = null;
            CapacityQuantum bestQuantum = default;
            Code tripBlocker = Code.None;
            HaulStatus blocker = HaulStatus.NoCarrier;
            Code policyBlocker = Code.None;
            Code handlingBlocker = Code.None;
            List<HaulLoadSettings> loadInputs = null;
            List<(EntityId Entity, Code Source)> serviceInputs = null;
            Code dockBlocker = Code.None, bestSourceSlot = Code.None, bestDestinationSlot = Code.None;
            WorkBoard board = world.Services.Get<WorkBoard>();
            var stops = returning == null ? new List<Entity> { from, to } : new List<Entity> { to };
            for (int i = 0; i < workerCount; i++)
            {
                IWorker worker = selectedWorker.IsValid ? explicitWorker : workers[i];
                Entity carrier = worker.Entity;
                if (returning != null && carrier != returning || carrier == to || returning == null && carrier == from) continue;
                int busy = continuing?.Id ?? (mode == WorkCandidateMode.EmergencyInterruption && emergency && intent.HasValue ? worker.Job : 0);
                Code eligibility = dispatch.CheckAssignment(world, carrier, owner, HaulRole, stops, emergency, busy);
                if (!eligibility.IsNone)
                {
                    if (WorkPolicyCodes.IsWait(eligibility) && policyBlocker.IsNone) policyBlocker = eligibility;
                    continue;
                }
                IInventory cargo = carrier.Get<ITransporter>() == null ? null : TransporterContracts.ReadCargo(_world, carrier);
                if (cargo == null || !cargo.Accepts(world.Content, resource) || returning == null && ReadCargoDisposition(carrier.Id).HasResidualCargo ||
                    !selection.IsNone && !(cargo is IResourceLotEndpoint) ||
                    cargo.Endpoint == inlet.Endpoint || returning == null && !purchase && cargo.Endpoint == outlet.Endpoint ||
                    cargo.Entity == to || returning == null && cargo.Entity == from) continue;
                long cost = 0; Code sourceSlot = Code.None, destinationSlot = Code.None;
                long destinationCost = 0;
                MotionTravelEstimate sourceTravel = new MotionTravelEstimate(0, Code.None);
                MotionTravelEstimate destinationTravel = new MotionTravelEstimate(null, MotionArrivalCodes.Unsupported);
                Code Continue(SpatialSnapshot origin) => board.PreviewDock(to, carrier, WorkDockRequest.Haul(resource, ResourceHandlingPhase.Unloading),
                    origin, out destinationSlot, out destinationCost, out _, out destinationTravel, current: continuing,
                    productiveOnly: retainedDestination && continuing.To == to.Id && continuing.DestinationPort == destinationPort);
                if (returning == null)
                {
                    Code sourceDock = board.PreviewDock(from, carrier, WorkDockRequest.Haul(resource, ResourceHandlingPhase.Loading), null,
                        out sourceSlot, out cost, out _, out sourceTravel, continuation: Continue);
                    if (!sourceDock.IsNone) { dockBlocker = sourceDock; if (sourceDock == WorkSystem.BlockerUnreachable) blocker = HaulStatus.Unreachable; continue; }
                }
                if (returning != null)
                {
                    Code destinationDock = Continue(null);
                    if (!destinationDock.IsNone) { dockBlocker = destinationDock; if (destinationDock == WorkSystem.BlockerUnreachable) blocker = HaulStatus.Unreachable; continue; }
                    cost = checked(cost + destinationCost);
                }
                MotionTravelEstimate tripTravel = MotionArrivalContracts.Combine(sourceTravel, destinationTravel);
                if (returning == null)
                {
                    if (!SpatialInteraction.TryEstimateCost(world, from, to, out long deliveryCost))
                    { blocker = HaulStatus.Unreachable; continue; }
                    cost = checked(cost + deliveryCost);
                }
                CapacityQuantum common = CapacityQuantum.Combine(sourceQuantum, CapacityPolicyContracts.Quantum(cargo.CapacityPolicy, resource), CapacityPolicyContracts.Quantum(inlet.CapacityPolicy, resource));
                Fix ceilingHere = retainedDestination ? receiving : Fix.Min(receiving, inlet.Offer(resource, receiving));
                if (returning == null) ceilingHere = Fix.Min(ceilingHere, cargo.Offer(resource, ceilingHere));
                Fix amount = common.Floor(ceilingHere);
                if (!amount.IsPositive) { blocker = HaulStatus.NoCapacity; continue; }
                if (purchase && returning == null)
                {
                    if (!(supplier is IResourceSupplyPrice pricing) || !world.Services.TryGet<IResourcePurchaseSettlement>(out var settlement))
                    { policyBlocker = ResourceTransactions.UnavailableTradingCapability; continue; }
                    int sourceVersion = from.Version, destinationVersion = to.Version, carrierVersion = carrier.Version;
                    ResourceTransferStatus payment = settlement.CheckPurchases(Array.AsReadOnly(new[] { new ResourcePurchaseIntent(owner, resource, amount, pricing) }));
                    if (from.Version != sourceVersion || to.Version != destinationVersion || carrier.Version != carrierVersion)
                        throw new InvalidOperationException("purchase settlement mutated physical haul preview");
                    if (payment > ResourceTransferStatus.Ready) throw new InvalidOperationException("purchase settlement returned an invalid preflight status");
                    if (payment != ResourceTransferStatus.Ready)
                    { policyBlocker = HaulHandlingCodes.PurchaseRefusal(payment); continue; }
                }
                if (intent.HasValue && returning == null)
                {
                    Code load = HaulLoadPolicyContracts.CheckLoad(world, carrier, resource, amount, waitAge, emergency, out var loadPolicy);
                    if (loadPolicy != null) (loadInputs ??= new List<HaulLoadSettings>()).Add(loadPolicy);
                    if (!load.IsNone) { handlingBlocker = load; continue; }
                }
                if (returning == null)
                {
                    ResourceHandlingCheck loading = HaulHandling.Check(world, carrier, resource, amount, ResourceHandlingPhase.Loading);
                    if (!loading.Ready) { handlingBlocker = loading.Blocker; continue; }
                }
                ResourceHandlingCheck unloading = HaulHandling.Check(world, carrier, resource, amount, ResourceHandlingPhase.Unloading);
                if (!unloading.Ready) { handlingBlocker = unloading.Blocker; continue; }
                var tripBudget = HaulTripPolicyContracts.CheckTrip(world, carrier, tripTravel,
                    returning == null ? default : resource, returning == null ? Fix.Zero : amount, emergency);
                if (tripBudget != null && !tripBudget.Blocker.IsNone) { if (tripBlocker.IsNone) tripBlocker = tripBudget.Blocker; continue; }
                Code serviceWindow = HaulTripPolicyContracts.CheckDispatch(world, carrier, tripTravel, out var service, emergency);
                if (service != null) foreach (var input in service.SourceInputs)
                {
                    serviceInputs ??= new List<(EntityId Entity, Code Source)>();
                    if (!serviceInputs.Contains(input)) serviceInputs.Add(input);
                }
                if (!serviceWindow.IsNone) { if (tripBlocker.IsNone) tripBlocker = serviceWindow; continue; }
                if (!amount.IsPositive || !retainedDestination && !inlet.CanReserveIn(requester, resource, amount) ||
                    continuing == null && (purchase ? !supplier.CanReserveSupply(requester, resource, amount) : !outlet.CanReserveOut(requester, resource, amount)) ||
                    returning == null && !cargo.CanReserveIn(requester, resource, amount))
                { blocker = HaulStatus.NoCapacity; continue; }
                int interrupted = busy == 0 || continuing != null ? 0 : dispatch.PreviewInterruption(world, intent.Value.WithCandidate(carrier.Id, cost));
                if (busy != 0 && continuing == null && interrupted == 0) continue;
                int priority = WorkPolicyRules.Priority(carrier);
                if (best == null || interrupted == 0 && bestInterrupted != 0 || (interrupted == 0) == (bestInterrupted == 0) &&
                    (priority > bestPriority || priority == bestPriority &&
                    (cost < bestCost || cost == bestCost && carrier.Id.Value < best.Id.Value)))
                { best = carrier; bestAmount = amount; bestCost = cost; bestPriority = priority; bestInterrupted = interrupted; bestSourceSlot = sourceSlot; bestDestinationSlot = destinationSlot; bestTravel = tripTravel; bestTripBudget = tripBudget; bestQuantum = common; }
            }
            var decision = best == null ? new HaulPreview(blocker == HaulStatus.NoCarrier && (!tripBlocker.IsNone || !policyBlocker.IsNone || !handlingBlocker.IsNone || !dockBlocker.IsNone) ? HaulStatus.PolicyBlocked : blocker,
                    blocker: blocker == HaulStatus.NoCarrier ? !tripBlocker.IsNone ? tripBlocker : !policyBlocker.IsNone ? policyBlocker : !handlingBlocker.IsNone ? handlingBlocker : dockBlocker : Code.None)
                : new HaulPreview(HaulStatus.Ready, from.Id, to.Id, best.Id, resource, bestAmount, purchase, travelCost: bestCost,
                    sourceDockSlot: bestSourceSlot, destinationDockSlot: bestDestinationSlot, interruptedJob: bestInterrupted, travel: bestTravel, tripBudget: bestTripBudget, quantityQuantum: bestQuantum, sourcePort: sourcePort, destinationPort: destinationPort,
                    sourceEndpoint: purchase ? ResourceEndpointId.ForEntity(from.Id) : outlet.Endpoint, destinationEndpoint: inlet.Endpoint, haulDemand: demand, selection: selection);
            return decision.WithLoadPolicyInputs(loadInputs).WithServiceInputs(serviceInputs);
            }
            finally
            {
                if (inlet != null && (inlet.Version != inletVersion || inlet.Endpoint != inletIdentity || inlet.Company != inletCompany) ||
                    outlet != null && (outlet.Version != outletVersion || outlet.Endpoint != outletIdentity || outlet.Company != outletCompany))
                    throw new InvalidOperationException("haul preview mutated its actual physical resource endpoint");
            }
        }

        public int AcquireHaul(PlayerId owner, EntityId from, EntityId to, ResourceId resource, Fix ceiling,
            int priority, bool purchase = false, EntityId worker = default, Code sourcePort = default, Code destinationPort = default, Code selection = default)
            => Acquire(owner, from, to, resource, ceiling, priority, 0, purchase, null, selectedWorker: worker, sourcePort: sourcePort, destinationPort: destinationPort, selection: selection);
        public int AcquirePlayerHaul(PlayerId owner, EntityId from, EntityId to, ResourceId resource, Fix ceiling,
            int priority, bool purchase = false, EntityId worker = default, Code sourcePort = default, Code destinationPort = default, Code selection = default)
            => Acquire(owner, from, to, resource, ceiling, priority, 0, purchase, null, selectedWorker: worker, sourcePort: sourcePort, destinationPort: destinationPort, playerIntent: true, selection: selection);

        private int Acquire(PlayerId owner, EntityId from, EntityId to, ResourceId resource, Fix ceiling,
            int priority, int route, bool purchase, Entity returning, LogisticsDemandId demand = default,
            WorkerDispatchOffer? intent = null, long waitAge = 0, EntityId selectedWorker = default, Code sourcePort = default, Code destinationPort = default, ResourceEndpointId? destinationEndpoint = null, ResourceEndpointId? sourceEndpoint = null, bool playerIntent = false, Code selection = default)
        {
            using var operation = BeginHaulOperation();
            try
            {
                // Re-evaluate current state, not a retained preview. Normal refusals precede every mutation.
                if (priority < 0) return 0;
                HaulPreview preview = playerIntent ? PreviewPlayerHaul(owner, from, to, resource, ceiling, purchase, selectedWorker, sourcePort, destinationPort, priority, selection)
                    : Preview(owner, from, to, resource, ceiling, purchase, returning, demand, intent, waitAge: waitAge, selectedWorker: selectedWorker, sourcePort: sourcePort, destinationPort: destinationPort, destinationEndpoint: destinationEndpoint, sourceEndpoint: sourceEndpoint, selection: selection);
                if (preview.Status != HaulStatus.Ready) return 0;
                World world = _world; WorkBoard board = world.Services.Get<WorkBoard>();
                Entity carrier = world.Entities.Get(preview.Carrier); IInventory cargo = TransporterContracts.ReadCargo(_world, carrier);
                var binding = PhysicalPort(world.Entities.Get(to), destinationPort, resource, preview.DestinationEndpoint);
                if (!binding.Ready) return 0;
                IResourceInlet destination = binding.Store;
                Entity source = world.Entities.Get(from); Fix amount = preview.Amount;
                Job job = board.AddJob(owner, HaulRole, priority, to);
                job.IsHaul = true; job.Purchase = purchase; job.Route = route;
                job.SourcePort = sourcePort; job.DestinationPort = destinationPort;
                job.SourceEndpoint = preview.SourceEndpoint; job.DestinationEndpoint = preview.DestinationEndpoint;
                job.HaulDemand = demand;
                job.HaulSelection = preview.Selection;
                if (intent.HasValue && intent.Value.Entry == RecoveryEntry(LogisticsRecoveryKind.Evacuation, resource, preview.SourceEndpoint)) job.RecoveryKind = LogisticsRecoveryKind.Evacuation;
                else if (intent.HasValue && intent.Value.Entry == RecoveryEntry(LogisticsRecoveryKind.CargoReturn, resource, preview.SourceEndpoint)) job.RecoveryKind = LogisticsRecoveryKind.CargoReturn;
                job.SourceDockSlot = preview.SourceDockSlot; job.DestinationDockSlot = preview.DestinationDockSlot;
                job.From = returning == null ? from : EntityId.None; job.To = to; job.Resource = resource; job.Amount = amount;
                if (returning == null)
                {
                    if (purchase)
                    {
                        job.OutReservation = source.Get<ISupplier>().ReserveSupply(job.ReservationOwner, resource, amount);
                        job.CargoInReservation = cargo.ReserveIn(job.ReservationOwner, resource, amount);
                        job.InReservation = destination.ReserveIn(job.ReservationOwner, resource, amount);
                    }
                    else
                    {
                        Bill quantities = new Bill(new[] { new ResourceAmount(resource, amount) });
                        ResourceClaimResult claims = world.Services.Get<IResourceTransaction>().AcquireClaims(owner, new[] {
                            new ResourceClaimIntent(ReservationDirection.Outbound, job.ReservationOwner, preview.SourceEndpoint, quantities, SelectedClaims(resource, job.HaulSelection)),
                            new ResourceClaimIntent(ReservationDirection.Inbound, job.ReservationOwner, cargo.Endpoint, quantities),
                            new ResourceClaimIntent(ReservationDirection.Inbound, job.ReservationOwner, preview.DestinationEndpoint, quantities) }, System.Array.Empty<ResourceReservation>());
                        if (!claims.Committed || claims.Receipts.Count != 3) throw new InvalidOperationException("whole haul acquisition changed after current admission");
                        foreach (ResourceReservation receipt in claims.Receipts)
                            if (receipt.Direction == ReservationDirection.Outbound) job.OutReservation = receipt.Token;
                            else if (receipt.Token.Endpoint == cargo.Endpoint) job.CargoInReservation = receipt.Token;
                            else job.InReservation = receipt.Token;
                    }
                    job.Toils.Add(Toil.GoTo(from)); job.Toils.Add(Toil.Load(from, resource, amount));
                }
                else
                {
                    var quantities = new Bill(new[] { new ResourceAmount(resource, amount) });
                    ResourceClaimResult claims = world.Services.Get<IResourceTransaction>().AcquireClaims(owner, new[] {
                        new ResourceClaimIntent(ReservationDirection.Outbound, job.ReservationOwner, cargo.Endpoint, quantities, SelectedClaims(resource, job.HaulSelection)),
                        new ResourceClaimIntent(ReservationDirection.Inbound, job.ReservationOwner, preview.DestinationEndpoint, quantities) }, System.Array.Empty<ResourceReservation>());
                    if (!claims.Committed || claims.Receipts.Count != 2)
                        throw new InvalidOperationException("whole cargo return acquisition changed after current admission");
                    foreach (ResourceReservation receipt in claims.Receipts)
                        if (receipt.Direction == ReservationDirection.Outbound) job.CargoOutReservation = receipt.Token;
                        else job.InReservation = receipt.Token;
                    job.Loaded = amount;
                }
                job.Toils.Add(Toil.GoTo(to)); job.Toils.Add(Toil.Unload(to, resource, amount));
                job.Toils.Add(Toil.LeaveDock(to));
                board.Assign(job, carrier.Get<IWorker>(), job.Role);
                board.ValidateHaul(job, false);
                if (intent.HasValue && intent.Value.Entry == RecoveryEntry(LogisticsRecoveryKind.Evacuation, resource, preview.SourceEndpoint)) RecordRecovery(intent.Value, resource, LogisticsRecoveryKind.Evacuation);
                else if (intent.HasValue && intent.Value.Entry == RecoveryEntry(LogisticsRecoveryKind.CargoReturn, resource, preview.SourceEndpoint)) RecordRecovery(intent.Value, resource, LogisticsRecoveryKind.CargoReturn);
                else if (intent.HasValue) RecordDispatch(route, preview.DestinationEndpoint, resource, job.HaulSelection);
                return job.Id;
            }
            catch (Exception error) { _world.LatchFault("logistics.acquire", error); throw; }
        }

        private static long SelectedCost(HaulPreview candidate)
        {
            if (candidate.Status != HaulStatus.Ready || !candidate.TravelCost.HasValue || candidate.TravelCost.Value < 0)
                throw new InvalidOperationException("successful haul candidate must supply its nonnegative selected travel cost");
            return candidate.TravelCost.Value;
        }

        /// <summary>Select actual public stock through local/named contracts before optional suppliers.</summary>
        private HaulPreview PreviewSource(Entity requester, ResourceId resource, PlayerId owner, Fix ceiling, LogisticsDemandId demand,
            WorkerDispatchOffer? intent = null, WorkCandidateMode mode = WorkCandidateMode.IdleOnly, long waitAge = 0, EntityId selectedWorker = default,
            Code destinationPort = default, ResourceEndpointId? destinationEndpoint = null, Code selection = default)
        {
            World world = _world;
            Entity best = null; HaulPreview selected = new HaulPreview(HaulStatus.NoSupply);
            List<HaulLoadSettings> loadInputs = null;
            List<(EntityId Entity, Code Source)> serviceInputs = null;
            bool hasWait = false; long bestCost = long.MaxValue; int bestPriority = 0;
            void Consider(Entity site, HaulPreview candidate)
            {
                candidate.CopyLoadPolicyInputs(ref loadInputs);
                candidate.CopyServiceInputs(ref serviceInputs);
                if (candidate.Status != HaulStatus.Ready)
                { if (best == null && !hasWait) { selected = candidate; hasWait = true; } return; }
                long cost = SelectedCost(candidate); int priority = ExportPriority(site, candidate.SourcePort);
                int portTie = string.CompareOrdinal(candidate.SourcePort.Id, selected.SourcePort.Id);
                if (best == null || candidate.InterruptedJob == 0 && selected.InterruptedJob != 0 ||
                    (candidate.InterruptedJob == 0) == (selected.InterruptedJob == 0) &&
                    (priority > bestPriority || priority == bestPriority &&
                    (cost < bestCost || cost == bestCost &&
                    (site.Id.Value < best.Id.Value || site.Id == best.Id && portTie < 0))))
                { best = site; bestCost = cost; bestPriority = priority; selected = candidate; }
            }
            var requestedBinding = PhysicalPort(requester, destinationPort, resource, destinationEndpoint);
            if (!requestedBinding.Ready) return new HaulPreview(HaulStatus.NoCapacity, blocker: requestedBinding.Blocker);
            IResourceInlet requestedStore = requestedBinding.Store;
            foreach (Entity site in world.Entities.All)
            {
                if (!site.Alive || site.Owner != owner || site.Has<ITransporter>()) continue;
                VisitPorts(site, resource, true, (name, inlet) =>
                {
                    if (inlet is not IResourceOutlet outlet || outlet.Endpoint == requestedStore.Endpoint) return;
                    Consider(site, Preview(owner, site.Id, requester.Id, resource, ceiling, false, null, demand, intent, mode,
                        waitAge, selectedWorker, sourcePort: name, destinationPort: destinationPort,
                        destinationEndpoint: destinationEndpoint, sourceEndpoint: outlet.Endpoint, selection: selection));
                });
            }
            if (best != null) return selected.WithLoadPolicyInputs(loadInputs).WithServiceInputs(serviceInputs);
            ComponentList<ISupplier> suppliers = world.Entities.Components<ISupplier>();
            for (int i = 0; i < suppliers.Count; i++)
            {
                Entity site = suppliers[i].Entity; if (!site.Alive) continue;
                Consider(site, Preview(owner, site.Id, requester.Id, resource, ceiling, true, null, demand, intent, mode,
                    waitAge, selectedWorker, destinationPort: destinationPort, destinationEndpoint: destinationEndpoint, selection: selection));
            }
            return selected.WithLoadPolicyInputs(loadInputs).WithServiceInputs(serviceInputs);
        }

        /// <summary>One authored recovery preference, then actual selected haul cost and stable identity.</summary>
        private static HaulPreview PreviewDestination(World world, Entity from, ResourceId resource, PlayerId owner, Entity returning,
            WorkerDispatchOffer? intent = null, WorkCandidateMode mode = WorkCandidateMode.IdleOnly, long waitAge = 0, EntityId selectedWorker = default, Job continuing = null,
            LogisticsRecoverySettings destinationSettings = null, Code sourcePort = default, ResourceEndpointId? sourceEndpoint = null, ResourceEndpointId? evacuatingEndpoint = null)
        {
            if (destinationSettings == null) throw new InvalidOperationException("recovery destination requires its actual authored settings");
            LogisticsService logistics = world.Services.Get<LogisticsService>();
            IRecoveryDestinationPolicy policy = world.Content.Get<IRecoveryDestinationPolicy>(destinationSettings.DestinationPolicy);
            HaulPreview best = new HaulPreview(HaulStatus.NoDestination);
            List<HaulLoadSettings> loadInputs = null;
            List<(EntityId Entity, Code Source)> serviceInputs = null;
            EntityId bestId = EntityId.None;
            long bestD = long.MaxValue;
            long bestPreference = -1;
            bool hasPolicyRefusal = false, refusalHasDemand = false;
            int refusalPriority = 0;
            long refusalCost = long.MaxValue;
            int CompareAccess(EntityId destination, Code destinationName, EntityId sourceSite, Code sourceName, HaulPreview chosen)
            {
                int result = sourceSite.Value.CompareTo(chosen.Source.Value);
                if (result == 0) result = string.CompareOrdinal(sourceName.Id, chosen.SourcePort.Id);
                if (result == 0) result = destination.Value.CompareTo(chosen.Destination.Value);
                return result != 0 ? result : string.CompareOrdinal(destinationName.Id, chosen.DestinationPort.Id);
            }
            void ConsiderSource(Entity accessSite, Code accessPort, ResourceEndpointId? accessEndpoint)
            {
                IResourceOutlet source = returning == null ? logistics.PhysicalPort(accessSite, accessPort, resource, accessEndpoint).Store as IResourceOutlet : TransporterContracts.ReadCargo(world, returning);
                if (source == null) return;
                foreach (Entity e in world.Entities.All)
                {
                    if (!e.Alive || e.Owner != owner || e.Has<ITransporter>()) continue;
                    logistics.VisitPorts(e, resource, false, (name, store) =>
                    {
                        if (store.Endpoint == source.Endpoint ||
                            store is IResourceOutlet { Evacuate: true }) return;
                        Demand? requested = logistics.RecoveryDemand(e.Id, store, resource, source, continuing);
                        LogisticsDemandId demand = continuing != null && store.Endpoint == continuing.DestinationEndpoint
                            ? continuing.HaulDemand : requested?.Source ?? default;
                        // Private receivers are explicit request destinations, never general spare-room discoveries.
                        // An acquired original promise remains valid after its requester expires.
                        if (!store.Public && !demand.IsValid) return;
                        HaulPreview candidate = logistics.Preview(owner, accessSite.Id, e.Id, resource,
                            continuing?.Loaded ?? source.Available(resource), false, returning,
                            demand: demand, intent: intent, mode: mode, waitAge: waitAge, selectedWorker: selectedWorker, continuing: continuing,
                            sourcePort: accessPort, sourceEndpoint: accessEndpoint, destinationPort: name, destinationEndpoint: store.Endpoint,
                            selection: requested?.Settings.Selection ?? continuing?.HaulSelection ?? Code.None);
                        candidate.CopyLoadPolicyInputs(ref loadInputs);
                        candidate.CopyServiceInputs(ref serviceInputs);
                        if (continuing != null && candidate.Status == HaulStatus.Ready && candidate.Amount != continuing.Loaded) return;
                        if (candidate.Status != HaulStatus.Ready)
                        {
                            if (!bestId.IsValid && best.Status == HaulStatus.NoDestination) best = candidate;
                            return;
                        }
                        var facts = new RecoveryDestinationFacts(owner, accessSite.Id, e.Id, candidate.Carrier, resource,
                            candidate.Amount, candidate.QuantityQuantum, logistics.DeliveryGap(store, resource, false),
                            requested?.Settings.Priority ?? 0, SelectedCost(candidate),
                            candidate.SourceEndpoint, name, candidate.DestinationEndpoint,
                            continuing != null && store.Endpoint == continuing.DestinationEndpoint ? continuing.Loaded : Fix.Zero);
                        var decision = EntityObservation.ReadProvider(world, policy, accessSite, () => EntityObservation.Read(world, e, () => policy.Evaluate(facts)));
                        if (!decision.Allowed)
                        {
                            if (decision.Blocker.IsNone || !decision.Ceiling.IsZero || decision.Preference != 0)
                                throw new InvalidOperationException("recovery policy returned invalid refusal facts");
                            int tie = CompareAccess(e.Id, name, accessSite.Id, accessPort, best);
                            if (!bestId.IsValid && (!hasPolicyRefusal || facts.HasDemand && !refusalHasDemand ||
                                facts.HasDemand == refusalHasDemand && (facts.DemandPriority > refusalPriority ||
                                facts.DemandPriority == refusalPriority && (facts.TravelCost < refusalCost ||
                                facts.TravelCost == refusalCost && tie < 0))))
                            {
                                best = new HaulPreview(HaulStatus.PolicyBlocked, accessSite.Id, e.Id, candidate.Carrier, resource,
                                    blocker: decision.Blocker, sourcePort: accessPort, destinationPort: name, sourceEndpoint: candidate.SourceEndpoint,
                                    destinationEndpoint: candidate.DestinationEndpoint);
                                hasPolicyRefusal = true; refusalHasDemand = facts.HasDemand; refusalPriority = facts.DemandPriority;
                                refusalCost = facts.TravelCost;
                            }
                            return;
                        }
                        if (!decision.Blocker.IsNone || !decision.Ceiling.IsPositive || decision.Ceiling > candidate.Amount || decision.Preference < 0 ||
                            candidate.QuantityQuantum.Floor(decision.Ceiling) != decision.Ceiling)
                            throw new InvalidOperationException("recovery policy exceeded its actual feasible native amount");
                        if (continuing != null && decision.Ceiling != continuing.Loaded) return;
                        if (decision.Ceiling != candidate.Amount)
                        {
                            candidate = logistics.Preview(owner, accessSite.Id, e.Id, resource, decision.Ceiling, false, returning,
                                demand: demand, intent: intent, mode: mode, waitAge: waitAge, selectedWorker: selectedWorker, continuing: continuing,
                                sourcePort: accessPort, sourceEndpoint: accessEndpoint, destinationPort: name, destinationEndpoint: store.Endpoint);
                            candidate.CopyLoadPolicyInputs(ref loadInputs);
                            candidate.CopyServiceInputs(ref serviceInputs);
                            if (candidate.Status != HaulStatus.Ready || candidate.Amount != decision.Ceiling)
                                throw new InvalidOperationException("recovery policy's native reduced amount lost its actual preflight");
                        }
                        long d = SelectedCost(candidate);
                        int stable = CompareAccess(e.Id, name, accessSite.Id, accessPort, best);
                        if (!bestId.IsValid || decision.Preference > bestPreference || decision.Preference == bestPreference &&
                            (d < bestD || d == bestD && stable < 0))
                        { best = candidate; bestId = e.Id; bestD = d; bestPreference = decision.Preference; }
                    });
                }
            }
            if (evacuatingEndpoint.HasValue)
            {
                if (returning != null || continuing != null) throw new InvalidOperationException("evacuation access selection requires unacquired stock");
                foreach (Entity site in world.Entities.All)
                {
                    if (!site.Alive || site.Owner != owner || site.Has<ITransporter>()) continue;
                    logistics.VisitPorts(site, resource, true, (name, inlet) =>
                    {
                        if (inlet is IResourceOutlet outlet && outlet.Evacuate && outlet.Endpoint == evacuatingEndpoint.Value)
                            ConsiderSource(site, name, outlet.Endpoint);
                    });
                }
            }
            else ConsiderSource(from, sourcePort, sourceEndpoint);
            return best.WithLoadPolicyInputs(loadInputs).WithServiceInputs(serviceInputs);
        }

        // ------------------------------------------------------------------ physical execution (WorkSystem)

        private void CheckPhysicalContext(World world, Job job, Entity worker, Entity at, ResourceId resource, Code kind)
        {
            if (!ReferenceEquals(world, _world) || job == null || worker == null || at == null)
                throw new InvalidOperationException("foreign or incomplete physical execution context");
            world.Services.Get<WorkBoard>().ValidateHaul(job, false);
            if (job.State != JobState.Assigned || job.RetryAt > world.Tick || job.Worker != worker.Id || !ReferenceEquals(world.Entities.Get(worker.Id), worker) ||
                !ReferenceEquals(world.Entities.Get(at.Id), at) || job.Resource != resource ||
                job.Toils[job.ToilIndex].Executor != kind || job.Toils[job.ToilIndex].At != at.Id ||
                world.Services.Get<WorkBoard>().CheckDockReach(job, worker, at) != SpatialInteractionStatus.WithinReach)
                throw new InvalidOperationException("physical execution differs from its current haul toil or arrival");
            IWorkerDispatch dispatch = world.Services.Get<IWorkerDispatch>();
            if (!dispatch.CheckJob(world, worker, job).IsNone || !dispatch.EffectiveWorkRate(world, worker).IsPositive)
                throw new InvalidOperationException("physical execution requires a currently eligible worker");
        }

        public ResourceTransferResult Load(World world, Job j, Entity worker, Entity at, ResourceId resource, Fix amount)
        {
            CheckPhysicalContext(world, j, worker, at, resource, WorkPhases.Pickup);
            if (j.Amount != amount || j.From != at.Id) throw new InvalidOperationException("physical pickup amount differs");
            using var operation = BeginHaulOperation();
            try
            {
                WorkBoard board = world.Services.Get<WorkBoard>();
                Fix chunk = board.EarnedHandling(j) - j.Loaded;
                if (!chunk.IsPositive) return new ResourceTransferResult(ResourceTransferStatus.Ready);
                IInventory hold = TransporterContracts.ReadCargo(_world, worker);
                IResourceInlet final = ReadHaulEndpoint(j, false).Store;
                if (final == null) return new ResourceTransferResult(ResourceTransferStatus.MissingEndpoint);
                int finalVersion = final.Version;
                ResourceReceiptCheck finalCheck = final.CheckReservedIn(world.Content, j.ReservationOwner, j.InReservation);
                if (final.Version != finalVersion) throw new InvalidOperationException("final destination mutated during pickup preflight");
                if (!finalCheck.Ready) return new ResourceTransferResult(finalCheck.Status);
                var intent = new ResourceTransferIntent(j.ReservationOwner, j.Owner, j.OutReservation, j.CargoInReservation,
                    resource, chunk, j.Purchase ? ResourceTransferKind.Supply : ResourceTransferKind.Stock);
                IResourceTransaction transaction = world.Services.Get<IResourceTransaction>();
                ResourceTransferResult result = transaction.Commit(intent);
                if (!result.Committed) return result;
                if (final.Version != finalVersion) throw new InvalidOperationException("pickup changed its final destination");
                ResourceClaimResult protectedCargo = !j.CargoOutReservation.IsValid
                    ? transaction.AcquireClaims(j.Owner, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, j.ReservationOwner, hold.Endpoint,
                        new Bill(new[] { new ResourceAmount(resource, result.Amount) }), SelectedClaims(resource, j.HaulSelection)) }, System.Array.Empty<ResourceReservation>())
                    : transaction.ExtendClaim(j.Owner, new ResourceReservation(j.CargoOutReservation, j.ReservationOwner,
                        resource, j.Loaded, ReservationDirection.Outbound), result.Amount);
                if (!protectedCargo.Committed || protectedCargo.Receipts.Count != 1)
                    throw new InvalidOperationException("actual received cargo could not retain its native outbound protection");
                j.CargoOutReservation = protectedCargo.Receipts[0].Token;
                j.Loaded += result.Amount;
                if (j.Loaded == j.Amount) { j.OutReservation = default; j.CargoInReservation = default; }
                if (final.Version != finalVersion) throw new InvalidOperationException("cargo protection changed its final destination");
                return result;
            }
            catch (Exception error) { _world.LatchFault("logistics.load", error); throw; }
        }

        public ResourceTransferResult Unload(World world, Job j, Entity worker, Entity at, ResourceId resource)
        {
            CheckPhysicalContext(world, j, worker, at, resource, WorkPhases.Delivery);
            using var operation = BeginHaulOperation();
            try
            {
                Fix chunk = world.Services.Get<WorkBoard>().EarnedHandling(j) - HaulHandling.Handled(j);
                if (!chunk.IsPositive) return new ResourceTransferResult(ResourceTransferStatus.Ready);
                var intent = new ResourceTransferIntent(j.ReservationOwner, j.Owner, j.CargoOutReservation, j.InReservation, resource, chunk);
                ResourceTransferResult result = world.Services.Get<IResourceTransaction>().Commit(intent);
                if (!result.Committed) return result;
                j.Loaded -= result.Amount;
                if (j.Loaded.IsZero) { j.InReservation = default; j.CargoOutReservation = default; }
                return result;
            }
            catch (Exception error) { _world.LatchFault("logistics.unload", error); throw; }
        }

        /// <summary>Hands back exactly what this job promised: the pickup and the room at the far end.</summary>
        public void EndHaul(World world, Job j)
        {
            if (!ReferenceEquals(world, _world)) throw new InvalidOperationException("foreign haul release session");
            if (j == null || !j.IsHaul || !ReferenceEquals(world.Services.Get<WorkBoard>().Job(j.Id), j))
                throw new InvalidOperationException("haul release requires its actual session job");
            using var operation = BeginHaulOperation();
            try
            {
                if (j.Purchase && j.OutReservation.IsValid)
                {
                    Entity source = world.Entities.Get(j.From); ISupplier supplier = source?.Get<ISupplier>();
                    if (supplier != null)
                    {
                        supplier.ReleaseSupply(j.ReservationOwner, j.OutReservation);
                        EntityObservation.Read(world, source, () => {
                            foreach (ResourceReservation record in supplier.Reservations)
                                if (record.Owner.Equals(j.ReservationOwner))
                                    throw new InvalidOperationException("supplier retained a cancelled haul claim");
                            return true;
                        });
                    }
                }
                ReleaseHaulClaims(j);
                j.OutReservation = default; j.InReservation = default; j.CargoInReservation = default; j.CargoOutReservation = default;
                j.HandlingProgress = WorkProgress.Zero;
                j.UnloadingBaseline = Fix.Zero;
                j.RecoveryCause = Code.None; j.RecoveryDockSlot = Code.None;
                j.SourceDockSlot = Code.None; j.DestinationDockSlot = Code.None;
            }
            catch (Exception error) { _world.LatchFault("logistics.release", error); throw; }
        }

        public void Sync(IStateSync s)
        {
            if (!s.Reading) { HaulLoadPolicyContracts.ValidateLive(_world); HaulTripPolicyContracts.ValidateLive(_world); }
            s.Int(ref _nextRoute);
            int nd = s.Count(_demands.Count);
            if (s.Reading) _demands.Clear();
            for (int i = 0; i < nd; i++)
            {
                Demand d = s.Reading ? default : _demands[i];
                s.Entity(ref d.Store); string port = d.Port.Id; s.String(ref port); if (s.Reading) d.Port = port == null ? Code.None : new Code(port);
                ResourceEndpointId.Sync(s, ref d.Endpoint); s.Resource(ref d.Resource); d.Settings = LogisticsDemandSettings.Sync(s, d.Settings); s.Bool(ref d.Replenishing); s.Player(ref d.Owner);
                LogisticsDemandId.Sync(s, ref d.Source); s.Long(ref d.CreatedTick); s.Long(ref d.LastDispatchTick);
                if (DemandOwner(d.Source) == null || !d.Store.IsValid || !d.Owner.IsValid || !d.Endpoint.IsValid ||
                    d.Port.IsNone && d.Endpoint != ResourceEndpointId.ForEntity(d.Store) ||
                    d.Source.Scope == StockDemandScope && d.Source.Entry != d.Port ||
                    d.CreatedTick < 0 || d.LastDispatchTick < d.CreatedTick || d.LastDispatchTick > _world.Tick ||
                    _world.Content.Resource(d.Resource) == null || d.Source.Scope != StockDemandScope && (d.Replenishing || !d.Settings.Enabled || !d.Settings.Desired.IsPositive ||
                    d.Settings.Minimum != d.Settings.Desired || d.Settings.Maximum != d.Settings.Desired) ||
                    d.Replenishing && (!d.Settings.Enabled || d.Settings.Minimum.IsZero || d.Settings.Desired.IsZero) || _world.Content.Resource(d.Resource).QuantityUnit.Discrete &&
                    (d.Settings.Minimum.Raw % Fix.One.Raw != 0 || d.Settings.Desired.Raw % Fix.One.Raw != 0 || d.Settings.Maximum.Raw % Fix.One.Raw != 0))
                    throw new CorruptStateException("invalid saved logistics demand");
                if (!s.Reading && CheckSource(d).Status == LogisticsDemandStatus.Invalid)
                    throw new CorruptStateException("saved demand does not belong to its declared source");
                for (int k = 0; k < (s.Reading ? _demands.Count : i); k++)
                    if ((_demands[k].Source.Equals(d.Source) || d.Source.Scope == StockDemandScope && _demands[k].Source.Scope == StockDemandScope) &&
                        _demands[k].Endpoint == d.Endpoint && _demands[k].Resource == d.Resource &&
                        (_demands[k].Settings.Selection == d.Settings.Selection || d.Source.Scope == StockDemandScope))
                        throw new CorruptStateException("duplicate saved logistics demand");
                if (s.Reading) _demands.Add(d);
            }
            int nr = s.Count(_routes.Count);
            if (s.Reading) _routes.Clear();
            for (int i = 0; i < nr; i++)
            {
                Route r = s.Reading ? default : _routes[i];
                s.Int(ref r.Id); s.Player(ref r.Owner); s.Entity(ref r.From); s.Entity(ref r.To);
                string sourceName = r.SourcePort.Id, destinationName = r.DestinationPort.Id; s.String(ref sourceName); s.String(ref destinationName);
                if (s.Reading) { r.SourcePort = sourceName == null ? Code.None : new Code(sourceName); r.DestinationPort = destinationName == null ? Code.None : new Code(destinationName); }
                s.Resource(ref r.Resource);
                r.Settings = LogisticsRouteSettings.Sync(s, r.Settings); s.Long(ref r.CreatedTick); s.Long(ref r.LastDispatchTick);
                if (_nextRoute < 1 || r.Id <= 0 || r.Id >= _nextRoute || !r.Owner.IsValid || !r.From.IsValid || !r.To.IsValid ||
                    r.CreatedTick < 0 || r.LastDispatchTick < r.CreatedTick || r.LastDispatchTick > _world.Tick || _world.Content.Resource(r.Resource) == null ||
                    _world.Content.Resource(r.Resource).QuantityUnit.Discrete && r.Settings.Batch.Raw % Fix.One.Raw != 0)
                    throw new CorruptStateException("invalid saved logistics route");
                if (!s.Reading && _world.Entities.Get(r.From) != null && _world.Entities.Get(r.To) != null &&
                    !CheckRouteBinding(r.Owner, r.From, r.To, r.Resource, r.Settings, r.SourcePort, r.DestinationPort, false).IsNone)
                    throw new CorruptStateException("route no longer belongs to its native endpoints");
                for (int k = 0; k < (s.Reading ? _routes.Count : i); k++)
                    if (_routes[k].Id == r.Id) throw new CorruptStateException("duplicate saved logistics route");
                if (s.Reading) _routes.Add(r);
            }
            if (_nextRoute < 1) throw new CorruptStateException("invalid next route identity");
            SyncRecoveries(s);
        }
    }

    /// <summary>Empty-container cleanup only. All evacuation assignments belong to WorkSystem.</summary>
    public sealed class LogisticsMaintenanceSystem : GameSystem
    {
        public override int StateSchemaVersion => 2;
        public override string Id => "sim.logistics";
        public override Schedule GetSchedule(ISimulationTime time) => Schedule.Every(time, Tuning.LogisticsPlanInterval);

        public override void Update(World world, TickInfo tick)
            => world.Services.Get<LogisticsService>().RemoveEmptyEvacuationContainers(world, world.Services.Get<WorkBoard>());
        public override void OnLoaded(World world)
        { var logistics = world.Services.Get<LogisticsService>(); logistics.ValidateLoadedDemands(); logistics.ValidateLoadedRoutes(); HaulLoadPolicyContracts.ValidateLive(world); HaulTripPolicyContracts.ValidateLive(world); }
    }
}
