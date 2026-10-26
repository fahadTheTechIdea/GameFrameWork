using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine
{
    public enum ResourceTransferStatus : byte
    {
        InvalidRequest, MissingEndpoint, InvalidReceipt, Incompatible, NoStock, NoCapacity,
        NoPayment, TradingDisabled, UnavailableCapability, SettlementRefused, Ready, Committed
    }

    /// <summary>Side-effect-free validation of one existing owned receipt, never a second promise.</summary>
    public readonly struct ResourceReceiptCheck
    {
        public ResourceTransferStatus Status { get; }
        public ResourceReservation Record { get; }
        public bool Ready => Status == ResourceTransferStatus.Ready;
        public ResourceReceiptCheck(ResourceTransferStatus status, ResourceReservation record = default)
        { Status = status; Record = record; }
    }

    public enum ResourceTransferKind : byte { Stock, Supply }

    public enum ResourceSupplyKind : byte { Invalid, ExistingStock, AuthoredCreation }
    public enum ResourceFlowKind : byte { StockTransfer, StockPurchase, AuthoredCreation, Initialized, Produced, Consumed, Destroyed, Lost, Sold }

    /// <summary>Explicit immutable lifecycle request over the existing inventory; no parallel balances.</summary>
    public readonly struct ResourceInventoryIntent
    {
        public ReservationOwner Owner { get; }
        public PlayerId Company { get; }
        public EntityId Endpoint { get; }
        public Bill Quantities { get; }
        public ResourceFlowKind Kind { get; }
        public Code Cause { get; }
        public bool AddsStock => Kind == ResourceFlowKind.Initialized || Kind == ResourceFlowKind.Produced;
        private readonly IReadOnlyList<ResourceLotAmount> _initialLots;
        public IReadOnlyList<ResourceLotAmount> InitialLots => _initialLots ?? Array.Empty<ResourceLotAmount>();
        public ResourceInventoryIntent(ReservationOwner owner, PlayerId company, EntityId endpoint, Bill quantities,
            ResourceFlowKind kind, Code cause, IEnumerable<ResourceLotAmount> initialLots = null)
        { Owner = owner; Company = company; Endpoint = endpoint; Quantities = quantities; Kind = kind; Cause = cause;
            _initialLots = initialLots == null ? null : new List<ResourceLotAmount>(initialLots).AsReadOnly(); }
    }

    public readonly struct ResourceInventoryCheck
    {
        public ResourceTransferStatus Status { get; }
        public int EndpointVersion { get; }
        public Code Blocker { get; }
        public bool Ready => Status == ResourceTransferStatus.Ready;
        public ResourceInventoryCheck(ResourceTransferStatus status, int version = 0, Code blocker = default)
        { Status = status; EndpointVersion = version; Blocker = blocker; }
    }
    public readonly struct ResourceInventoryResult
    {
        public ResourceTransferStatus Status { get; }
        public int ChangedResources { get; }
        public EntityId CreatedEntity { get; }
        public Code Blocker { get; }
        public bool Ready => Status == ResourceTransferStatus.Ready;
        public bool Committed => Status == ResourceTransferStatus.Committed;
        public ResourceInventoryResult(ResourceTransferStatus status, int changedResources = 0, EntityId createdEntity = default, Code blocker = default)
        { Status = status; ChangedResources = changedResources; CreatedEntity = createdEntity; Blocker = blocker; }
    }

    /// <summary>Authored native output bill at an actual endpoint, not a new stock owner.</summary>
    public readonly struct ResourceProductionIntent
    {
        public ResourceEndpointId Endpoint { get; }
        public Bill Quantities { get; }
        private readonly IReadOnlyList<Content.IResourceOutputQualityRule> _qualities;
        public IReadOnlyList<Content.IResourceOutputQualityRule> OutputQualities => _qualities ?? Array.Empty<Content.IResourceOutputQualityRule>();
        public ResourceProductionIntent(ResourceEndpointId endpoint, Bill quantities, IEnumerable<Content.IResourceOutputQualityRule> qualities = null)
        { Endpoint = endpoint; Quantities = quantities; _qualities = qualities == null ? null : new List<Content.IResourceOutputQualityRule>(qualities).AsReadOnly(); }
    }
    /// <summary>Disposition of one exact actual receipt; the remainder stays protected unless explicitly released.</summary>
    public readonly struct ResourceConversionInput
    {
        public static bool IsDispositionKind(ResourceFlowKind kind)
            => kind == ResourceFlowKind.Consumed || kind == ResourceFlowKind.Destroyed || kind == ResourceFlowKind.Lost;
        public ResourceFlowKind Kind { get; }
        public ResourceReservation Receipt { get; }
        public Fix Consumed { get; }
        public bool ReleaseRemainder { get; }
        public ResourceConversionInput(ResourceFlowKind kind, ResourceReservation receipt, Fix consumed, bool releaseRemainder)
        { Kind = kind; Receipt = receipt; Consumed = consumed; ReleaseRemainder = releaseRemainder; }
    }
    /// <summary>Detached complete conversion request. Each actual receipt occurs once with explicit disposition.</summary>
    public readonly struct ResourceConversionIntent
    {
        public ReservationOwner Owner { get; }
        public PlayerId Company { get; }
        public IReadOnlyList<ResourceConversionInput> Inputs { get; }
        public IReadOnlyList<ResourceProductionIntent> Outputs { get; }
        public Code Cause { get; }
        public ResourceConversionIntent(ReservationOwner owner, PlayerId company,
            IEnumerable<ResourceConversionInput> inputs,
            IEnumerable<ResourceProductionIntent> outputs, Code cause)
        {
            Owner = owner; Company = company; Cause = cause;
            Inputs = inputs == null ? null : new List<ResourceConversionInput>(inputs).AsReadOnly();
            Outputs = outputs == null ? null : new List<ResourceProductionIntent>(outputs).AsReadOnly();
        }
    }

    public enum ResourceClaimKind : byte { Fresh, Increase }

    /// <summary>Explicit fresh demand or additional stock on an exact original outbound receipt; never implicit merging.</summary>
    public readonly struct ResourceClaimIntent
    {
        public ResourceClaimKind Kind { get; }
        public ResourceReservation OriginalReceipt { get; }
        public ReservationOwner Owner { get; }
        public ResourceEndpointId Endpoint { get; }
        public Bill Quantities { get; }
        public ReservationDirection Direction { get; }
        private readonly IReadOnlyList<Content.IResourceLotSelectionDefinition> _selections;
        public IReadOnlyList<Content.IResourceLotSelectionDefinition> Selections => _selections ?? Array.Empty<Content.IResourceLotSelectionDefinition>();
        public ResourceClaimIntent(ReservationDirection direction, ReservationOwner owner, ResourceEndpointId endpoint, Bill quantities,
            IEnumerable<Content.IResourceLotSelectionDefinition> selections = null)
        { Kind = ResourceClaimKind.Fresh; OriginalReceipt = default; Direction = direction; Owner = owner; Endpoint = endpoint; Quantities = quantities;
            _selections = selections == null ? null : new List<Content.IResourceLotSelectionDefinition>(selections).AsReadOnly(); }
        public ResourceClaimIntent(ResourceReservation receipt, Bill quantities)
        {
            Kind = ResourceClaimKind.Increase; OriginalReceipt = receipt; Direction = receipt.Direction;
            _selections = null;
            Owner = receipt.Owner; Endpoint = receipt.Token.Endpoint; Quantities = quantities;
        }
    }
    public readonly struct ResourceClaimCheck
    {
        public ResourceTransferStatus Status { get; }
        public bool Ready => Status == ResourceTransferStatus.Ready;
        private readonly IReadOnlyList<ResourceClaimAvailability> _availability;
        public IReadOnlyList<ResourceClaimAvailability> Availability => _availability ?? Array.Empty<ResourceClaimAvailability>();
        public ResourceClaimCheck(ResourceTransferStatus status, IEnumerable<ResourceClaimAvailability> availability = null)
        { Status = status; _availability = availability == null ? null : new List<ResourceClaimAvailability>(availability).AsReadOnly(); }
    }
    public readonly struct ResourceClaimAvailability
    {
        public ResourceEndpointId Endpoint { get; }
        public ResourceId Resource { get; }
        public UnitId Unit { get; }
        public Fix Available { get; }
        public Fix Required { get; }
        public ReservationDirection Direction { get; }
        /// <summary>Inbound Available is an admissible offer up to Required; outbound is actual unpromised stock.</summary>
        public ResourceClaimAvailability(ReservationDirection direction, ResourceEndpointId endpoint, ResourceId resource, UnitId unit, Fix available, Fix required)
        { Direction = direction; Endpoint = endpoint; Resource = resource; Unit = unit; Available = available; Required = required; }
    }
    /// <summary>Detached exact acquired records. Endpoint books alone own promises; spending/release returns no live receipts.</summary>
    public sealed class ResourceClaimResult
    {
        public ResourceTransferStatus Status { get; }
        public bool Committed => Status == ResourceTransferStatus.Committed;
        public IReadOnlyList<ResourceReservation> Receipts { get; }
        public ResourceClaimResult(ResourceTransferStatus status, IEnumerable<ResourceReservation> receipts = null)
        { Status = status; Receipts = receipts == null ? Array.Empty<ResourceReservation>() : new List<ResourceReservation>(receipts).AsReadOnly(); }
    }

    /// <summary>Verified historical transfer facts, never stock authority or permission to mutate.</summary>
    public readonly struct ResourceFlowFact
    {
        private readonly IReadOnlyList<ResourceLotAmount> _lots;
        /// <summary>Exact committed partitions. Empty means attribution unavailable, not an invented untracked grade.</summary>
        public IReadOnlyList<ResourceLotAmount> Lots => _lots ?? Array.Empty<ResourceLotAmount>();
        public Code Selection { get; }
        private ResourceFlowFact(ResourceFlowFact original, IEnumerable<ResourceLotAmount> lots, Code selection)
        { this = original; _lots = lots == null ? null : new List<ResourceLotAmount>(lots).AsReadOnly(); Selection = selection; }
        public ResourceFlowFact WithLots(IEnumerable<ResourceLotAmount> lots, Code selection = default)
            => new ResourceFlowFact(this, lots, selection);
        public const string EventId = "sim.resource-flow";
        public long Tick { get; }
        public ReservationOwner Owner { get; }
        public PlayerId Company { get; }
        public ResourceId Resource { get; }
        public Fix Amount { get; }
        public ReservationToken Source { get; }
        public ReservationToken Destination { get; }
        public ResourceEndpointId Endpoint { get; }
        public Code Cause { get; }
        public bool IsTransfer => Kind <= ResourceFlowKind.AuthoredCreation;
        public ResourceFlowAttribution Attribution => new ResourceFlowAttribution(Source.Endpoint, Destination.Endpoint, Endpoint, Owner.Scope);
        public UnitId Unit { get; }
        public ResourceFlowKind Kind { get; }
        public Fix? SourceBefore { get; }
        public Fix? SourceAfter { get; }
        public Fix DestinationBefore { get; }
        public Fix DestinationAfter { get; }
        public int SourceVersionBefore { get; }
        public int SourceVersionAfter { get; }
        public int DestinationVersionBefore { get; }
        public int DestinationVersionAfter { get; }
        public ResourceFlowFact(long tick, ResourceTransferIntent intent, UnitId unit, ResourceFlowKind kind,
            Fix? sourceBefore, Fix? sourceAfter, Fix destinationBefore, Fix destinationAfter,
            int sourceVersionBefore, int sourceVersionAfter, int destinationVersionBefore, int destinationVersionAfter)
        {
            Tick = tick; Owner = intent.Owner; Company = intent.Company; Resource = intent.Resource; Amount = intent.Amount;
            _lots = null; Selection = default;
            Source = intent.Source; Destination = intent.Destination; Endpoint = default; Cause = default;
            Unit = unit; Kind = kind;
            SourceBefore = sourceBefore; SourceAfter = sourceAfter;
            DestinationBefore = destinationBefore; DestinationAfter = destinationAfter;
            SourceVersionBefore = sourceVersionBefore; SourceVersionAfter = sourceVersionAfter;
            DestinationVersionBefore = destinationVersionBefore; DestinationVersionAfter = destinationVersionAfter;
        }
        public ResourceFlowFact(long tick, ResourceInventoryIntent intent, ResourceAmount line, UnitId unit,
            Fix before, Fix after, int versionBefore, int versionAfter)
        {
            Tick = tick; Owner = intent.Owner; Company = intent.Company; Resource = line.Resource; Amount = line.Amount;
            _lots = null; Selection = default;
            Source = default; Destination = default; Endpoint = ResourceEndpointId.ForEntity(intent.Endpoint); Cause = intent.Cause; Unit = unit; Kind = intent.Kind;
            SourceBefore = intent.AddsStock ? null : (Fix?)before; SourceAfter = intent.AddsStock ? null : (Fix?)after;
            DestinationBefore = intent.AddsStock ? before : Fix.Zero; DestinationAfter = intent.AddsStock ? after : Fix.Zero;
            SourceVersionBefore = intent.AddsStock ? 0 : versionBefore; SourceVersionAfter = intent.AddsStock ? 0 : versionAfter;
            DestinationVersionBefore = intent.AddsStock ? versionBefore : 0; DestinationVersionAfter = intent.AddsStock ? versionAfter : 0;
        }
        public ResourceFlowFact(long tick, PlayerId company, ResourceReservation receipt, Code cause, UnitId unit,
            Fix before, Fix after, int versionBefore, int versionAfter, ResourceFlowKind kind)
        {
            Tick = tick; Owner = receipt.Owner; Company = company; Resource = receipt.Resource; Amount = receipt.Amount;
            _lots = null; Selection = default;
            Source = receipt.Token; Destination = default; Endpoint = receipt.Token.Endpoint; Cause = cause; Unit = unit; Kind = kind;
            SourceBefore = before; SourceAfter = after; SourceVersionBefore = versionBefore; SourceVersionAfter = versionAfter;
            DestinationBefore = DestinationAfter = Fix.Zero; DestinationVersionBefore = DestinationVersionAfter = 0;
        }
        public ResourceFlowFact(long tick, ReservationOwner owner, PlayerId company, ResourceEndpointId endpoint,
            ResourceAmount line, Code cause, UnitId unit, Fix before, Fix after, int versionBefore, int versionAfter)
        {
            Tick = tick; Owner = owner; Company = company; Resource = line.Resource; Amount = line.Amount;
            _lots = null; Selection = default;
            Source = Destination = default; Endpoint = endpoint; Cause = cause; Unit = unit; Kind = ResourceFlowKind.Produced;
            SourceBefore = SourceAfter = null; SourceVersionBefore = SourceVersionAfter = 0;
            DestinationBefore = before; DestinationAfter = after;
            DestinationVersionBefore = versionBefore; DestinationVersionAfter = versionAfter;
        }
    }

    /// <summary>One exact physical transfer using endpoint-owned receipts; no copied live balances.</summary>
    public readonly struct ResourceTransferIntent
    {
        public ReservationOwner Owner { get; }
        public PlayerId Company { get; }
        public ReservationToken Source { get; }
        public ReservationToken Destination { get; }
        public ResourceId Resource { get; }
        public Fix Amount { get; }
        public ResourceTransferKind Kind { get; }
        public ResourceTransferIntent(ReservationOwner owner, PlayerId company, ReservationToken source,
            ReservationToken destination, ResourceId resource, Fix amount, ResourceTransferKind kind = ResourceTransferKind.Stock)
        { Owner = owner; Company = company; Source = source; Destination = destination; Resource = resource; Amount = amount; Kind = kind; }
    }

    /// <summary>Detached checkpoint facts, not permission to commit a retained preview.</summary>
    public readonly struct ResourceTransferCheck
    {
        public ResourceTransferStatus Status { get; }
        public int SourceVersion { get; }
        public int DestinationVersion { get; }
        public bool Ready => Status == ResourceTransferStatus.Ready;
        public ResourceTransferCheck(ResourceTransferStatus status, int sourceVersion = 0, int destinationVersion = 0)
        { Status = status; SourceVersion = sourceVersion; DestinationVersion = destinationVersion; }
    }

    public readonly struct ResourceTransferResult
    {
        public ResourceTransferStatus Status { get; }
        public Fix Amount { get; }
        public bool Committed => Status == ResourceTransferStatus.Committed;
        public ResourceTransferResult(ResourceTransferStatus status, Fix amount = default) { Status = status; Amount = amount; }
    }

    /// <summary>Detached all-leg admission/commit outcome; native quantities are never summed across units.</summary>
    public readonly struct ResourceTransferBatchResult
    {
        public ResourceTransferStatus Status { get; }
        public int CommittedTransfers { get; }
        public bool Ready => Status == ResourceTransferStatus.Ready;
        public bool Committed => Status == ResourceTransferStatus.Committed;
        public ResourceTransferBatchResult(ResourceTransferStatus status, int committedTransfers = 0)
        { Status = status; CommittedTransfers = committedTransfers; }
    }

    public enum ResourceMovementParticipantDisposition { Retain = 0, Release = 1 }

    /// <summary>One directed native Bill; empty source references request fresh claims, exact existing records may be boundedly spent.</summary>
    public readonly struct ResourceMovementIntent
    {
        public ReservationOwner Owner { get; }
        public PlayerId Company { get; }
        public ResourceEndpointId Source { get; }
        public ResourceEndpointId Destination { get; }
        public Bill Quantities { get; }
        public IReadOnlyList<ResourceReservation> SourceClaims { get; }
        public IReadOnlyList<ResourceReservation> DestinationClaims { get; }
        public IReadOnlyList<ResourceReservation> ParticipantClaims { get; }
        public ResourceMovementParticipantDisposition ParticipantDisposition { get; }
        public ResourceMovementIntent(ReservationOwner owner, PlayerId company, ResourceEndpointId source,
            ResourceEndpointId destination, Bill quantities, IReadOnlyList<ResourceReservation> sourceClaims,
            IReadOnlyList<ResourceReservation> destinationClaims, IReadOnlyList<ResourceReservation> participantClaims,
            ResourceMovementParticipantDisposition participantDisposition)
        {
            Owner = owner; Company = company; Source = source; Destination = destination; Quantities = quantities;
            SourceClaims = sourceClaims == null ? null : new List<ResourceReservation>(sourceClaims).AsReadOnly();
            DestinationClaims = destinationClaims == null ? null : new List<ResourceReservation>(destinationClaims).AsReadOnly();
            ParticipantClaims = participantClaims == null ? null : new List<ResourceReservation>(participantClaims).AsReadOnly();
            ParticipantDisposition = participantDisposition;
        }
    }

    /// <summary>Detached observation of actual endpoint-owned promises; never acquisition or stock authority.</summary>
    public readonly struct ResourceClaimSnapshot
    {
        public ResourceTransferStatus Status { get; }
        public ResourceEndpointId Endpoint { get; }
        public int Version { get; }
        public IReadOnlyList<ResourceReservation> Records { get; }
        public bool Ready => Status == ResourceTransferStatus.Ready;
        public ResourceClaimSnapshot(ResourceTransferStatus status, ResourceEndpointId endpoint, int version,
            IEnumerable<ResourceReservation> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            Status = status; Endpoint = endpoint; Version = version;
            Records = new List<ResourceReservation>(records).AsReadOnly();
        }
    }
    public enum ResourceClaimReleasePolicy { Exact = 0, Surviving = 1 }
    /// <summary>Detached claim and endpoint references for one guarded release; owns no stock or promise.</summary>
    public readonly struct ResourceClaimReleaseIntent
    {
        public PlayerId Company { get; }
        public IReadOnlyList<ResourceReservation> Receipts { get; }
        public IReadOnlyList<ResourceReservation> Participants { get; }
        public IReadOnlyList<ResourceEndpointId> Endpoints { get; }
        public ResourceClaimReleasePolicy Policy { get; }
        public ResourceClaimReleaseIntent(PlayerId company, IReadOnlyList<ResourceReservation> receipts,
            IReadOnlyList<ResourceReservation> participants, IReadOnlyList<ResourceEndpointId> endpoints, ResourceClaimReleasePolicy policy)
        {
            Company = company; Policy = policy;
            Receipts = receipts == null ? null : new List<ResourceReservation>(receipts).AsReadOnly();
            Participants = participants == null ? null : new List<ResourceReservation>(participants).AsReadOnly();
            Endpoints = endpoints == null ? null : new List<ResourceEndpointId>(endpoints).AsReadOnly();
        }
    }

    /// <summary>Engine-side physical mutation authority; pure previews, current-state commits, no rollback fiction.
    /// Every entry refuses a terminally faulted session before caller data, content or provider work.
    /// Unexpected execution errors latch the first session fault and rethrow; direct callers cannot checkpoint partial state.</summary>
    public interface IResourceTransaction : ISimulationOperationBoundary
    {
        ResourceClaimSnapshot ReadClaims(PlayerId company, ResourceEndpointId endpoint, ReservationOwner owner);
        ResourceTransferCheck Evaluate(ResourceTransferIntent intent);
        ResourceTransferResult Commit(ResourceTransferIntent intent);
        /// <summary>Pure whole-set admission over existing owned receipts; shared tokens cannot exceed their aggregate held quantity.</summary>
        ResourceTransferBatchResult EvaluateTransfers(IReadOnlyList<ResourceTransferIntent> intents);
        /// <summary>Fresh all-leg admission, exact receipt deltas and deferred success facts; provider faults cannot checkpoint.</summary>
        ResourceTransferBatchResult CommitTransfers(IReadOnlyList<ResourceTransferIntent> intents);
        /// <summary>Complete native source and inlet admission before creating any movement claims.</summary>
        ResourceTransferBatchResult EvaluateMovement(ResourceMovementIntent intent);
        /// <summary>Acquire exact source/inlet claims, then use the same whole-set physical transfer protocol.</summary>
        ResourceTransferBatchResult CommitMovement(ResourceMovementIntent intent);
        /// <summary>Pure aggregate admission for all directed bills, including shared source/inlet capacity.</summary>
        ResourceTransferBatchResult EvaluateMovements(IReadOnlyList<ResourceMovementIntent> intents);
        /// <summary>Acquire the entire movement envelope before transferring any resource; no partial ready prefix.</summary>
        ResourceTransferBatchResult CommitMovements(IReadOnlyList<ResourceMovementIntent> intents);
        ResourceInventoryCheck EvaluateInventory(ResourceInventoryIntent intent);
        /// <summary>Pure complete addition to an uncreated definition's authored initial stock.</summary>
        ResourceInventoryCheck EvaluateInitialInventory(Content.EntityDef definition, Bill additional);
        ResourceInventoryResult CommitInventory(ResourceInventoryIntent intent);
        /// <summary>Whole actual input/tool/output admission, no prefix and no retained preview authority.</summary>
        ResourceInventoryResult EvaluateConversion(ResourceConversionIntent intent, IEntityCreation creation);
        /// <summary>Pure whole outbound draw, output and optional generation admission for the explicit cause; creates no receipt.</summary>
        ResourceInventoryResult EvaluateConversionClaims(PlayerId company, ReservationOwner owner,
            IReadOnlyList<ResourceClaimIntent> inputs, IReadOnlyList<ResourceProductionIntent> outputs, Code cause);
        /// <summary>Fresh admission and verified complete conversion; publish facts only after all participants verify.</summary>
        ResourceInventoryResult CommitConversion(ResourceConversionIntent intent, IEntityCreation creation);
        /// <summary>Pure mixed stock/capacity set admission. An empty bill probes endpoint access only, never acquires a promise.</summary>
        ResourceClaimCheck EvaluateClaims(PlayerId company, IReadOnlyList<ResourceClaimIntent> intents, IReadOnlyList<ResourceReservation> participants);
        ResourceClaimResult AcquireClaims(PlayerId company, IReadOnlyList<ResourceClaimIntent> intents, IReadOnlyList<ResourceReservation> participants);
        /// <summary>Protect additional current stock under one actual native outbound receipt; changes no stock/history.</summary>
        ResourceClaimResult ExtendClaim(PlayerId company, ResourceReservation receipt, Fix amount);
        ResourceClaimCheck EvaluateClaimReceipts(PlayerId company, IReadOnlyList<ResourceReservation> receipts);
        ResourceClaimCheck EvaluateClaimConsumption(PlayerId company, IReadOnlyList<ResourceReservation> receipts, Code cause,
            IReadOnlyList<ResourceReservation> participants);
        ResourceClaimCheck EvaluateClaimRelease(ResourceClaimReleaseIntent intent);
        ResourceClaimResult ReleaseClaims(ResourceClaimReleaseIntent intent);
        ResourceClaimResult ConsumeClaims(PlayerId company, IReadOnlyList<ResourceReservation> receipts, Code cause, IReadOnlyList<ResourceReservation> participants);
    }
}
