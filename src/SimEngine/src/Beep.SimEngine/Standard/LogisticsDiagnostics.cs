using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface ILogisticsDiagnostics
    {
        Code ReadDiagnostics(PlayerId company, EntityId target, ResourceId resource, out LogisticsOperationalSnapshot value,
            Code port = default, ResourceEndpointId? endpoint = null);
        Code ReadIncomingThroughput(PlayerId company, EntityId target, ResourceId resource, long fromStep, long throughStep,
            out LogisticsThroughputSnapshot value, Code port = default, ResourceEndpointId? endpoint = null);
    }
    public static class LogisticsDiagnosticCodes
    {
        public static readonly Code NoHistory = new Code("logistics.history-unavailable"),
            ExpiredHistory = new Code("logistics.history-expired");
    }
    public readonly struct LogisticsShipmentDiagnostic
    {
        public int Job { get; }
        public EntityId Worker { get; }
        public EntityId Source { get; }
        public Code SourcePort { get; }
        public Code DestinationPort { get; }
        public ResourceEndpointId SourceEndpoint { get; }
        public ResourceEndpointId DestinationEndpoint { get; }
        public Fix Requested { get; }
        public Fix Loaded { get; }
        public JobState State { get; }
        public Code Blocker { get; }
        public int PhaseIndex { get; }
        public long RetryAt { get; }
        internal LogisticsShipmentDiagnostic(Job job)
        { Job = job.Id; Worker = job.Worker; Source = job.From; Requested = job.Amount; Loaded = job.Loaded;
            State = job.State; Blocker = job.Blocker; PhaseIndex = job.ToilIndex; RetryAt = job.RetryAt;
            SourcePort = job.SourcePort; DestinationPort = job.DestinationPort;
            SourceEndpoint = job.SourceEndpoint; DestinationEndpoint = job.DestinationEndpoint; }
    }
    public readonly struct LogisticsCarrierDiagnostic
    {
        public EntityId Worker { get; }
        public ResourceEndpointId CargoEndpoint { get; }
        public int Job { get; }
        public bool Held { get; }
        /// <summary>Unassigned, unheld and not moving; not a route/role/policy eligibility promise.</summary>
        public bool Unassigned { get; }
        public Fix IncomingCapacity { get; }
        public CargoDispositionSnapshot Cargo { get; }
        public HaulPreview? Return { get; }
        public bool ReturnBlocked => Return.HasValue && Return.Value.Status != HaulStatus.Ready;
        internal LogisticsCarrierDiagnostic(EntityId worker, ResourceEndpointId cargoEndpoint, int job, bool held,
            bool unassigned, Fix capacity, CargoDispositionSnapshot cargo, HaulPreview? returning)
        { Worker = worker; CargoEndpoint = cargoEndpoint; Job = job; Held = held; Unassigned = unassigned;
            IncomingCapacity = capacity; Cargo = cargo; Return = returning; }
    }
    public sealed class LogisticsOperationalSnapshot
    {
        public EntityId Target { get; }
        public Code Port { get; }
        public ResourceEndpointId Endpoint { get; }
        public ResourceId Resource { get; }
        public UnitId Unit { get; }
        public long ObservedStep { get; }
        public Fix Stock { get; }
        public Fix InboundPromised { get; }
        public Fix OutboundPromised { get; }
        public Fix UnmetDesired { get; }
        public Fix UnpromisedGap { get; }
        public IReadOnlyList<LogisticsDispatchOffer> Offers { get; }
        public IReadOnlyList<LogisticsShipmentDiagnostic> Shipments { get; }
        public IReadOnlyList<LogisticsCarrierDiagnostic> Carriers { get; }
        public int QueueLength => Offers.Count;
        public int BlockedOffers { get; }
        public int UnreachableOffers { get; }
        internal LogisticsOperationalSnapshot(EntityId target, Code port, ResourceEndpointId endpoint, ResourceId resource, UnitId unit, long step, Fix stock,
            Fix promised, Fix outbound, Fix unmet, Fix gap, List<LogisticsDispatchOffer> offers,
            List<LogisticsShipmentDiagnostic> shipments, List<LogisticsCarrierDiagnostic> carriers)
        {
            Target = target; Port = port; Endpoint = endpoint; Resource = resource; Unit = unit; ObservedStep = step; Stock = stock;
            InboundPromised = promised; OutboundPromised = outbound; UnmetDesired = unmet; UnpromisedGap = gap;
            Offers = new List<LogisticsDispatchOffer>(offers).AsReadOnly();
            Shipments = new List<LogisticsShipmentDiagnostic>(shipments).AsReadOnly();
            Carriers = new List<LogisticsCarrierDiagnostic>(carriers).AsReadOnly();
            foreach (var offer in Offers)
            { if (offer.Haul.Status != HaulStatus.Ready) BlockedOffers++;
                if (offer.Haul.Status == HaulStatus.Unreachable) UnreachableOffers++; }
        }
    }
    public sealed class LogisticsThroughputSnapshot
    {
        public ResourceEndpointId Endpoint { get; }
        public ResourceId Resource { get; }
        public UnitId Unit { get; }
        public long FromStep { get; }
        public long ThroughStep { get; }
        public GameDuration Elapsed { get; }
        public long TransferLegs { get; }
        /// <summary>Committed incoming native Q32 quantity, not current inventory.</summary>
        public BigInteger IncomingRaw { get; }
        /// <summary>Exact reduced incoming Q32 native quantity per elapsed game second.</summary>
        public BigInteger RateRawNumerator { get; }
        public BigInteger RateSecondsDenominator { get; }
        internal LogisticsThroughputSnapshot(ResourceEndpointId endpoint, ResourceId resource, UnitId unit, long from,
            long through, long legs, BigInteger raw, ISimulationTime time)
        {
            Endpoint = endpoint; Resource = resource; Unit = unit; FromStep = from; ThroughStep = through;
            Elapsed = GameDuration.FromSteps(through - from, time); TransferLegs = legs; IncomingRaw = raw;
            var rate = MeasuredRate.CalculateRaw(raw, through - from, time, DurationUnit.Second);
            RateRawNumerator = rate.Numerator; RateSecondsDenominator = rate.Denominator;
        }
    }
    // Derived facts only. The same service still owns selection and intent, endpoints own stock,
    // WorkBoard owns shipments, and optional ResourceFlowJournal owns committed history.
    public sealed partial class LogisticsService
    {
        private Code CheckDiagnosticTarget(PlayerId company, EntityId target, ResourceId resource, Code port,
            ResourceEndpointId? endpoint, out Entity entity, out IResourceInlet inlet)
        {
            entity = _world.Entities.Get(target); inlet = null;
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (entity == null || !entity.Alive) return Codes.NotFound;
            if (entity.Owner != company) return Codes.NotOwned;
            if (_world.Content.Resource(resource) == null || endpoint.HasValue && !endpoint.Value.IsValid) return Codes.InvalidArgument;
            Entity actual = entity;
            var binding = PhysicalPort(actual, port, resource, endpoint);
            if (!binding.Ready) return binding.Blocker;
            inlet = binding.Store;
            IResourceInlet store = inlet;
            return ObserveDiagnosticEndpoint(actual, store, () => store.Company != company ? Codes.NotOwned
                : store.Accepts(_world.Content, resource) ? Code.None : StandardCodes.NotAccepted);
        }
        private T ObserveDiagnosticEndpoint<T>(Entity entity, IResourceInlet inlet, Func<T> read)
        {
            int version = inlet.Version; var identity = inlet.Endpoint; var company = inlet.Company; var capacity = inlet.CapacityPolicy;
            return EntityObservation.Read(_world, entity, () =>
            {
                try { return read(); }
                finally
                {
                    if (inlet.Version != version || inlet.Endpoint != identity || inlet.Company != company ||
                        !ReferenceEquals(inlet.CapacityPolicy, capacity))
                        throw new InvalidOperationException("logistics diagnostic mutated its actual endpoint");
                }
            });
        }
        public Code ReadDiagnostics(PlayerId company, EntityId target, ResourceId resource, out LogisticsOperationalSnapshot value,
            Code port = default, ResourceEndpointId? endpoint = null)
        {
            value = null; Code check = CheckDiagnosticTarget(company, target, resource, port, endpoint, out Entity entity, out IResourceInlet inlet);
            if (!check.IsNone) return check;
            value = ObserveDiagnosticEndpoint(entity, inlet, () =>
            {
                var stock = (Amount: inlet.Amount(resource), Promised: inlet.ReservedIn(resource),
                    Outbound: inlet is IResourceOutlet outlet ? outlet.ReservedOut(resource) : Fix.Zero,
                    Unmet: HasQualityDemand(inlet.Endpoint, resource) ? OnHandQualityShortage(inlet, resource) : Fix.Max(Fix.Zero, DemandOf(inlet.Endpoint, resource, false) - inlet.Amount(resource)), Gap: DeliveryGap(inlet, resource, false));
                var all = new List<LogisticsDispatchOffer>(); BuildQueue(company, WorkCandidateMode.IdleOnly, all);
                var offers = all.FindAll(x => x.DestinationEndpoint == inlet.Endpoint && x.Resource == resource);
                var shipments = new List<LogisticsShipmentDiagnostic>();
                foreach (Job job in _world.Services.Get<WorkBoard>().Jobs)
                    if (job.IsLive && job.IsHaul && job.Owner == company && job.DestinationEndpoint == inlet.Endpoint && job.Resource == resource)
                        shipments.Add(new LogisticsShipmentDiagnostic(job));
                shipments.Sort((a, b) => a.Job.CompareTo(b.Job));
                var carriers = new List<LogisticsCarrierDiagnostic>();
                foreach (Entity worker in _world.Entities.All)
                    if (worker.Alive && worker.Owner == company && worker.Has<ITransporter>() && worker.Has<IWorker>())
                        carriers.Add(EntityObservation.Read(_world, worker, () =>
                        {
                            IInventory cargo = TransporterContracts.ReadCargo(_world, worker); var actor = worker.Get<IWorker>();
                            var disposition = ReadCargoDisposition(worker.Id);
                            Fix capacity = EntityObservation.Read(_world, cargo.Entity, () => cargo.Accepts(_world.Content, resource)
                                ? cargo.Offer(resource, Fix.MaxValue) : Fix.Zero);
                            HaulPreview? returning = disposition.Unassigned.AmountOf(resource).IsPositive
                                ? PreviewReturnCargo(worker.Id, resource) : (HaulPreview?)null;
                            bool unassigned = actor.Job == 0 && !actor.Held &&
                                (!worker.Has<IMotion>() || MotionExecutionContracts.ReadStatus(_world, worker) != MoveStatus.Moving);
                            return new LogisticsCarrierDiagnostic(worker.Id, cargo.Endpoint, actor.Job, actor.Held, unassigned, capacity, disposition, returning);
                        }));
                carriers.Sort((a, b) => a.Worker.Value.CompareTo(b.Worker.Value));
                return new LogisticsOperationalSnapshot(target, port, inlet.Endpoint, resource, _world.Content.Resource(resource).QuantityUnit.Unit,
                    _world.Tick, stock.Amount, stock.Promised, stock.Outbound, stock.Unmet, stock.Gap, offers, shipments, carriers);
            });
            return Code.None;
        }
        public Code ReadIncomingThroughput(PlayerId company, EntityId target, ResourceId resource, long fromStep, long throughStep,
            out LogisticsThroughputSnapshot value, Code port = default, ResourceEndpointId? endpoint = null)
        {
            value = null; Code check = CheckDiagnosticTarget(company, target, resource, port, endpoint, out Entity entity, out IResourceInlet inlet);
            if (!check.IsNone) return check;
            if (fromStep < 0 || throughStep <= fromStep || throughStep > _world.Tick) return Codes.InvalidArgument;
            if (!_world.Services.TryGet<IResourceFlowJournal>(out var journal)) return LogisticsDiagnosticCodes.NoHistory;
            LogisticsThroughputSnapshot snapshot = null;
            Code result = ObserveDiagnosticEndpoint(entity, inlet, () =>
            {
                var query = new ResourceFlowQuery(company,resource,new[]{ResourceFlowKind.StockTransfer,ResourceFlowKind.StockPurchase,ResourceFlowKind.AuthoredCreation},
                    fromStep,throughStep,DurationUnit.Second,destination:inlet.Endpoint,ownerScope:Job.HaulReservationScope);
                var measured = EntityObservation.ReadProvider(_world,journal,entity,()=>journal.ReadThroughput(query),observeTargetState:true);
                if (measured == null || !ReferenceEquals(measured.Query,query))
                    throw new InvalidOperationException("haul journal must return its actual query facts");
                if (measured.Status == ResourceFlowReadStatus.Expired) return LogisticsDiagnosticCodes.ExpiredHistory;
                if (!measured.Available) throw new InvalidOperationException("actual haul journal refused valid company/window: " + measured.Status);
                snapshot = new LogisticsThroughputSnapshot(inlet.Endpoint, resource, measured.Unit,
                    fromStep, throughStep, measured.Facts, measured.AmountRaw.Value, _world.Time);
                return Code.None;
            });
            value = snapshot; return result;
        }
    }
}
