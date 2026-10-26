using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Another file of the sole transaction owner, not a service, queue, codec or stock ledger.
    public sealed partial class ResourceTransactions
    {
        // Single-leg calls enter the same complete-set owner; there is no second movement algorithm.
        public ResourceTransferBatchResult EvaluateMovement(ResourceMovementIntent intent)
        {
            _world.RequireHealthyUpdate();
            try
            {
                return EvaluateMovements(new[] { intent });
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }
        public ResourceTransferBatchResult CommitMovement(ResourceMovementIntent intent)
        {
            _world.RequireHealthyUpdate();
            try
            {
                using var operation = _operation.Enter();
                return CommitMovements(new[] { intent });
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        public ResourceTransferBatchResult EvaluateMovements(IReadOnlyList<ResourceMovementIntent> intents)
        {
            _world.RequireHealthyUpdate();
            try
            {
                return new ResourceTransferBatchResult(PrepareMovements(intents, out _, out _, out _, out _));
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        private ResourceTransferStatus PrepareMovements(IReadOnlyList<ResourceMovementIntent> intents,
            out List<ResourceMovementIntent> movements, out List<ResourceClaimIntent> claims, out List<ClaimEndpoint> endpoints,
            out Dictionary<ReservationOwner, PlayerId> companies)
        {
            movements = intents == null ? null : new List<ResourceMovementIntent>(intents);
            claims = new List<ResourceClaimIntent>(); endpoints = new List<ClaimEndpoint>();
            companies = new Dictionary<ReservationOwner, PlayerId>();
            if (movements == null || movements.Count == 0) return ResourceTransferStatus.InvalidRequest;
            var vectors = new Dictionary<(ReservationDirection Direction, ReservationOwner Owner, ResourceEndpointId Endpoint), List<ResourceAmount>>();
            var held = new Dictionary<ReservationToken, ResourceReservation>();
            var dispositions = new Dictionary<ReservationToken, ResourceMovementParticipantDisposition>();
            var spending = new Dictionary<ReservationToken, Fix>();
            var pairs = new HashSet<(ReservationOwner, ResourceEndpointId, ResourceEndpointId, ResourceId)>();
            // Capture/validate all caller data before any provider callback.
            foreach (ResourceMovementIntent intent in movements)
            {
                if (!intent.Owner.IsValid || !ValidClaimCompany(intent.Company) || !intent.Source.IsValid || !intent.Destination.IsValid ||
                    intent.Source == intent.Destination || intent.Quantities == null || intent.Quantities.IsEmpty || intent.SourceClaims == null || intent.DestinationClaims == null || intent.ParticipantClaims == null ||
                    intent.ParticipantDisposition != ResourceMovementParticipantDisposition.Retain && intent.ParticipantDisposition != ResourceMovementParticipantDisposition.Release)
                    return ResourceTransferStatus.InvalidRequest;
                if (companies.TryGetValue(intent.Owner, out var priorCompany) && priorCompany != intent.Company)
                    return ResourceTransferStatus.InvalidRequest;
                companies[intent.Owner] = intent.Company;
                foreach (ResourceAmount line in intent.Quantities.Lines)
                    if (!ValidClaimAmount(line.Resource, line.Amount) || !pairs.Add((intent.Owner, intent.Source, intent.Destination, line.Resource)))
                        return ResourceTransferStatus.InvalidRequest;
                var participants = new HashSet<ReservationToken>();
                foreach (ResourceReservation record in intent.ParticipantClaims)
                {
                    if (!record.Token.IsValid || !record.Owner.Equals(intent.Owner) || !participants.Add(record.Token) ||
                        record.Direction != ReservationDirection.Outbound && record.Direction != ReservationDirection.Inbound ||
                        !ValidClaimAmount(record.Resource, record.Amount)) return ResourceTransferStatus.InvalidReceipt;
                    if (held.TryGetValue(record.Token, out ResourceReservation previous) &&
                        (previous.Resource != record.Resource || previous.Amount != record.Amount ||
                         !previous.Owner.Equals(record.Owner) || previous.Direction != record.Direction)) return ResourceTransferStatus.InvalidReceipt;
                    held[record.Token] = record;
                    if (dispositions.TryGetValue(record.Token, out var disposition) && disposition != intent.ParticipantDisposition)
                        return ResourceTransferStatus.InvalidReceipt;
                    dispositions[record.Token] = intent.ParticipantDisposition;
                }
                foreach (ReservationDirection direction in new[] { ReservationDirection.Outbound, ReservationDirection.Inbound })
                {
                    IReadOnlyList<ResourceReservation> receipts = direction == ReservationDirection.Outbound ? intent.SourceClaims : intent.DestinationClaims;
                    ResourceEndpointId identity = direction == ReservationDirection.Outbound ? intent.Source : intent.Destination;
                    if (receipts.Count == 0)
                    { AddMovementClaim(vectors, direction, intent.Owner, identity, intent.Quantities); continue; }
                    if (receipts.Count != intent.Quantities.Count) return ResourceTransferStatus.InvalidReceipt;
                    var resources = new HashSet<ResourceId>();
                    foreach (ResourceReservation record in receipts)
                    {
                        Fix amount = intent.Quantities.AmountOf(record.Resource);
                        if (!record.Token.IsValid || record.Token.Endpoint != identity || !record.Owner.Equals(intent.Owner) ||
                            record.Direction != direction || !resources.Add(record.Resource) ||
                            !amount.IsPositive || !ValidClaimAmount(record.Resource, record.Amount))
                            return ResourceTransferStatus.InvalidReceipt;
                        if (held.TryGetValue(record.Token, out ResourceReservation previous) &&
                            (previous.Resource != record.Resource || previous.Amount != record.Amount ||
                             !previous.Owner.Equals(record.Owner) || previous.Direction != record.Direction))
                            return ResourceTransferStatus.InvalidReceipt;
                        held[record.Token] = record;
                        spending.TryGetValue(record.Token, out Fix spent);
                        try { spending[record.Token] = spent + amount; }
                        catch (OverflowException) { return ResourceTransferStatus.InvalidRequest; }
                        if (spending[record.Token] > record.Amount) return ResourceTransferStatus.InvalidReceipt;
                    }
                }
            }
            try
            {
                foreach (var vector in vectors)
                    claims.Add(new ResourceClaimIntent(vector.Key.Direction, vector.Key.Owner, vector.Key.Endpoint, new Bill(vector.Value)));
            }
            catch (OverflowException) { return ResourceTransferStatus.InvalidRequest; }
            // Capture the full participant set before invoking admission callbacks on its first member.
            foreach (ResourceMovementIntent intent in movements)
            {
                foreach (ResourceReservation participant in intent.ParticipantClaims)
                    if (TransferEndpoint(participant.Token.Endpoint, false, endpoints) == null) return ResourceTransferStatus.MissingEndpoint;
                if (TransferEndpoint(intent.Source, false, endpoints) == null || TransferEndpoint(intent.Destination, false, endpoints) == null)
                    return ResourceTransferStatus.MissingEndpoint;
                CheckClaimEndpoints(endpoints);
            }
            if (held.Count != 0)
            {
                var parties = new SortedDictionary<int, List<ResourceReservation>>();
                foreach (var record in held.Values)
                {
                    int party = companies[record.Owner].Value;
                    if (!parties.TryGetValue(party, out var group)) parties.Add(party, group = new List<ResourceReservation>());
                    group.Add(record);
                }
                foreach (var party in parties)
                {
                    ResourceTransferStatus status = PrepareClaimReceipts(new PlayerId(party.Key), party.Value, false, out _, out _, endpoints);
                    CheckClaimEndpoints(endpoints); if (status != ResourceTransferStatus.Ready) return status;
                }
            }
            var groups = MovementClaimGroups(claims, companies);
            foreach (var group in groups)
            {
                ResourceTransferStatus admission = PrepareClaims(new PlayerId(group.Key), group.Value, out _, out _, captured: endpoints);
                CheckClaimEndpoints(endpoints); if (admission != ResourceTransferStatus.Ready) return admission;
            }
            var outgoing = new Dictionary<(ResourceEndpointId, ResourceId), long>();
            var incoming = new Dictionary<(ResourceEndpointId, ResourceId), long>();
            foreach (var claim in claims) foreach (var line in claim.Quantities.Lines)
                if (!AddClaimTotal(claim.Direction == ReservationDirection.Outbound ? outgoing : incoming, claim.Endpoint, line.Resource, line.Amount))
                    return ResourceTransferStatus.InvalidRequest;
            ResourceTransferStatus aggregate = CheckClaimVectors(endpoints, outgoing, incoming);
            CheckClaimEndpoints(endpoints);
            if (aggregate != ResourceTransferStatus.Ready) return aggregate;
            foreach (ResourceMovementIntent intent in movements)
            {
                ClaimEndpoint source = endpoints.Find(endpoint => endpoint.Identity == intent.Source);
                ClaimEndpoint destination = endpoints.Find(endpoint => endpoint.Identity == intent.Destination);
                if (!MayUse(source.Provider, intent.Company) || !MayUse(destination.Provider, intent.Company)) return ResourceTransferStatus.InvalidRequest;
                if (source.Source == null || destination.Inlet == null) return ResourceTransferStatus.Incompatible;
                foreach (ResourceAmount line in intent.Quantities.Lines)
                {
                    if (!CanTransferLotMetadata(source.Source, destination.Inlet, line.Resource)) return ResourceTransferStatus.Incompatible;
                    CapacityQuantum quantum = CapacityQuantum.Combine(CapacityPolicyContracts.Quantum(source.Source.CapacityPolicy, line.Resource),
                        CapacityPolicyContracts.Quantum(destination.Inlet.CapacityPolicy, line.Resource));
                    CheckClaimEndpoints(endpoints);
                    if (quantum.Floor(line.Amount) != line.Amount) return ResourceTransferStatus.Incompatible;
                }
            }
            return ResourceTransferStatus.Ready;
        }
        private static SortedDictionary<int, List<ResourceClaimIntent>> MovementClaimGroups(IReadOnlyList<ResourceClaimIntent> claims,
            IReadOnlyDictionary<ReservationOwner, PlayerId> companies)
        {
            var groups = new SortedDictionary<int, List<ResourceClaimIntent>>();
            foreach (var claim in claims)
            {
                int party = companies[claim.Owner].Value;
                if (!groups.TryGetValue(party, out var group)) groups.Add(party, group = new List<ResourceClaimIntent>());
                group.Add(claim);
            }
            return groups;
        }

        private static void AddMovementClaim(
            Dictionary<(ReservationDirection Direction, ReservationOwner Owner, ResourceEndpointId Endpoint), List<ResourceAmount>> vectors,
            ReservationDirection direction, ReservationOwner owner, ResourceEndpointId endpoint, Bill bill)
        {
            var key = (direction, owner, endpoint);
            if (!vectors.TryGetValue(key, out var vector)) vectors.Add(key, vector = new List<ResourceAmount>());
            foreach (ResourceAmount line in bill.Lines) vector.Add(line);
        }

        public ResourceTransferBatchResult CommitMovements(IReadOnlyList<ResourceMovementIntent> intents)
        {
            _world.RequireHealthyUpdate();
            try
            {
                using var operation = _operation.Enter();
                ResourceTransferStatus status = PrepareMovements(intents, out List<ResourceMovementIntent> movements,
                    out List<ResourceClaimIntent> claims, out List<ClaimEndpoint> endpoints, out Dictionary<ReservationOwner, PlayerId> companies);
                CheckClaimEndpoints(endpoints);
                if (status != ResourceTransferStatus.Ready) return new ResourceTransferBatchResult(status);
                IReadOnlyList<ResourceReservation> acquired = Array.Empty<ResourceReservation>();
                if (claims.Count != 0)
                {
                    var all = new List<ResourceReservation>();
                    foreach (var group in MovementClaimGroups(claims, companies))
                    {
                        ResourceClaimResult acquisition = AcquireCapturedClaims(new PlayerId(group.Key), group.Value, endpoints);
                        if (!acquisition.Committed) throw new InvalidOperationException("movement envelope changed during complete claim acquisition");
                        all.AddRange(acquisition.Receipts);
                    }
                    acquired = all.AsReadOnly();
                }
                var records = new Dictionary<(ReservationDirection, ReservationOwner, ResourceEndpointId, ResourceId), ResourceReservation>();
                foreach (ResourceReservation record in acquired)
                {
                    records.Add((record.Direction, record.Owner, record.Token.Endpoint, record.Resource), record);
                }
                CheckClaimEndpoints(endpoints);
                var transfers = new List<ResourceTransferIntent>();
                foreach (ResourceMovementIntent intent in movements)
                    foreach (ResourceAmount line in intent.Quantities.Lines)
                    {
                        ResourceReservation source = intent.SourceClaims.Count == 0
                            ? records[(ReservationDirection.Outbound, intent.Owner, intent.Source, line.Resource)]
                            : FindMovementReceipt(intent.SourceClaims, line.Resource);
                        ResourceReservation destination = intent.DestinationClaims.Count == 0
                            ? records[(ReservationDirection.Inbound, intent.Owner, intent.Destination, line.Resource)]
                            : FindMovementReceipt(intent.DestinationClaims, line.Resource);
                        transfers.Add(new ResourceTransferIntent(intent.Owner, intent.Company, source.Token, destination.Token, line.Resource, line.Amount));
                    }
                var release = new List<ResourceReservation>(); var releasing = new HashSet<ReservationToken>();
                foreach (ResourceMovementIntent movement in movements)
                    if (movement.ParticipantDisposition == ResourceMovementParticipantDisposition.Release)
                        foreach (ResourceReservation record in movement.ParticipantClaims)
                            if (releasing.Add(record.Token)) release.Add(record);
                ResourceTransferBatchResult result = CommitCapturedTransfers(transfers, endpoints, release, companies);
                if (!result.Committed) throw new InvalidOperationException("movement envelope changed after complete claim acquisition: " + result.Status);
                return result;
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        private static ResourceReservation FindMovementReceipt(IReadOnlyList<ResourceReservation> records, ResourceId resource)
        {
            foreach (ResourceReservation record in records) if (record.Resource == resource) return record;
            throw new InvalidOperationException("admitted movement lost its source receipt");
        }

        private sealed class TransferLeg
        {
            public readonly ResourceTransferIntent Intent;
            public readonly ClaimEndpoint Source, Destination;
            public TransferLeg(ResourceTransferIntent intent, ClaimEndpoint source, ClaimEndpoint destination)
            { Intent = intent; Source = source; Destination = destination; }
        }

        public ResourceTransferBatchResult EvaluateTransfers(IReadOnlyList<ResourceTransferIntent> intents)
        {
            _world.RequireHealthyUpdate();
            try
            {
                return new ResourceTransferBatchResult(PrepareTransfers(intents, out _, out _, out _));
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        private ResourceTransferStatus PrepareTransfers(IReadOnlyList<ResourceTransferIntent> intents,
            out List<TransferLeg> legs, out List<ClaimEndpoint> endpoints, out IResourcePurchaseCommit purchaseCommit, bool committing = false,
            List<ClaimEndpoint> captured = null)
        {
            purchaseCommit = null;
            legs = new List<TransferLeg>(); endpoints = captured ?? new List<ClaimEndpoint>();
            CheckClaimEndpoints(endpoints);
            if (intents == null || intents.Count == 0) return ResourceTransferStatus.InvalidRequest;
            var ordered = new List<ResourceTransferIntent>(intents);
            var pairs = new HashSet<(ReservationToken, ReservationToken)>();
            var tokenUses = new Dictionary<ReservationToken, (ReservationDirection Direction, ReservationOwner Owner, PlayerId Company, ResourceId Resource, ResourceTransferKind Kind, Fix Amount)>();
            // Structural validation precedes provider callbacks and all mutations.
            foreach (ResourceTransferIntent intent in ordered)
            {
                if (!intent.Owner.IsValid || !intent.Source.IsValid || !intent.Destination.IsValid ||
                    intent.Source.Endpoint == intent.Destination.Endpoint || !ValidClaimAmount(intent.Resource, intent.Amount) ||
                    !ValidClaimCompany(intent.Company) ||
                    intent.Kind != ResourceTransferKind.Stock && intent.Kind != ResourceTransferKind.Supply)
                    return ResourceTransferStatus.InvalidRequest;
                if (!pairs.Add((intent.Source, intent.Destination))) return ResourceTransferStatus.InvalidReceipt;
                if (!AddTransferUse(tokenUses, intent.Source, ReservationDirection.Outbound, intent) ||
                    !AddTransferUse(tokenUses, intent.Destination, ReservationDirection.Inbound, intent))
                    return ResourceTransferStatus.InvalidReceipt;
            }
            ordered.Sort(CompareTransfers);
            foreach (ResourceTransferIntent intent in ordered)
            {
                ResourceTransferCheck check = EvaluateTransferLeg(intent, endpoints, out ClaimEndpoint source, out ClaimEndpoint destination);
                CheckClaimEndpoints(endpoints); // Even a refusing pure callback must leave the entire set unchanged.
                if (!check.Ready) return check.Status;
                if (!CanTransferLotMetadata(source.Source, destination.Inlet, intent.Resource)) return ResourceTransferStatus.Incompatible;
                legs.Add(new TransferLeg(intent, source, destination));
            }
            CheckClaimEndpoints(endpoints);
            foreach (var use in tokenUses)
            {
                ClaimEndpoint endpoint = endpoints.Find(candidate => candidate.Identity == use.Key.Endpoint);
                ResourceReservation actual = endpoint.Records.Find(record => record.Token.Equals(use.Key));
                if (!actual.Token.IsValid || actual.Amount < use.Value.Amount) return ResourceTransferStatus.InvalidReceipt;
            }
            var purchases = new List<ResourcePurchaseIntent>();
            foreach (TransferLeg leg in legs)
                if (leg.Intent.Kind == ResourceTransferKind.Supply)
                {
                    IResourceSupplyPrice pricing = leg.Source.Provider as IResourceSupplyPrice;
                    if (pricing == null) return ResourceTransferStatus.UnavailableCapability;
                    if (pricing.Endpoint != leg.Source.Identity) throw new InvalidOperationException("price provider differs from actual supply endpoint");
                    purchases.Add(new ResourcePurchaseIntent(leg.Intent.Company, leg.Intent.Resource, leg.Intent.Amount, pricing));
                }
            if (purchases.Count != 0)
            {
                if (!_world.Services.TryGet<IResourcePurchaseSettlement>(out var settlement)) return ResourceTransferStatus.UnavailableCapability;
                ResourceTransferStatus status = committing ? settlement.PreparePurchases(purchases.AsReadOnly(), out purchaseCommit)
                    : settlement.CheckPurchases(purchases.AsReadOnly());
                CheckClaimEndpoints(endpoints);
                if (status > ResourceTransferStatus.Ready || status != ResourceTransferStatus.Ready && purchaseCommit != null ||
                    committing && status == ResourceTransferStatus.Ready && purchaseCommit == null)
                    throw new InvalidOperationException("purchase settlement violated complete admission");
                if (status != ResourceTransferStatus.Ready) return status;
            }
            return ResourceTransferStatus.Ready;
        }

        private static bool AddTransferUse(
            Dictionary<ReservationToken, (ReservationDirection Direction, ReservationOwner Owner, PlayerId Company, ResourceId Resource, ResourceTransferKind Kind, Fix Amount)> uses,
            ReservationToken token, ReservationDirection direction, ResourceTransferIntent intent)
        {
            Fix amount = intent.Amount;
            if (uses.TryGetValue(token, out var previous))
            {
                if (previous.Direction != direction || !previous.Owner.Equals(intent.Owner) || previous.Company != intent.Company ||
                    previous.Resource != intent.Resource || previous.Kind != intent.Kind) return false;
                try { amount += previous.Amount; }
                catch (OverflowException) { return false; }
            }
            uses[token] = (direction, intent.Owner, intent.Company, intent.Resource, intent.Kind, amount);
            return true;
        }

        private ClaimEndpoint TransferEndpoint(ResourceEndpointId identity, bool supply, List<ClaimEndpoint> endpoints)
        {
            ClaimEndpoint existing = endpoints.Find(endpoint => endpoint.Identity == identity);
            if (existing != null) return existing;
            IResourceEndpoint provider = Resolve(_world, identity, supply);
            CheckClaimEndpoints(endpoints);
            if (provider == null) return null;
            if (provider.Endpoint != identity) throw new InvalidOperationException("resource provider returned a different transfer endpoint");
            var checkpoint = new ClaimEndpoint(provider, _world.Content.All<Content.IResourceContentDefinition>());
            endpoints.Add(checkpoint); CheckClaimEndpoints(endpoints);
            return checkpoint;
        }

        private static int CompareTransferEndpoints(ResourceEndpointId left, ResourceEndpointId right)
        {
            int order = string.CompareOrdinal(left.Scope, right.Scope);
            return order != 0 ? order : left.Identity.CompareTo(right.Identity);
        }
        private static int CompareTransfers(ResourceTransferIntent left, ResourceTransferIntent right)
        {
            int order = CompareTransferEndpoints(left.Source.Endpoint, right.Source.Endpoint);
            if (order != 0) return order;
            order = CompareTransferEndpoints(left.Destination.Endpoint, right.Destination.Endpoint);
            if (order != 0) return order;
            order = left.Resource.Value.CompareTo(right.Resource.Value);
            if (order != 0) return order;
            order = left.Source.Sequence.CompareTo(right.Source.Sequence);
            return order != 0 ? order : left.Destination.Sequence.CompareTo(right.Destination.Sequence);
        }
        private bool CanTransferLotMetadata(IResourceOutlet source, IResourceInlet destination, ResourceId resource)
        {
            if (!(source is IResourceLotEndpoint lots) || destination is IResourceLotEndpoint) return true;
            return EntityObservation.ReadProvider(_world, lots, null, () => {
                foreach (var lot in lots.Lots) if (lot.Resource == resource && !lot.Lot.IsNone) return false;
                return true;
            });
        }

        public ResourceTransferBatchResult CommitTransfers(IReadOnlyList<ResourceTransferIntent> intents)
        {
            _world.RequireHealthyUpdate();
            try
            {
                using var operation = _operation.Enter();
                return CommitCapturedTransfers(intents, null);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }
        private ResourceTransferBatchResult CommitCapturedTransfers(IReadOnlyList<ResourceTransferIntent> intents, List<ClaimEndpoint> captured,
            IReadOnlyList<ResourceReservation> release = null, IReadOnlyDictionary<ReservationOwner, PlayerId> companies = null)
        {
            ResourceTransferStatus status = PrepareTransfers(intents, out List<TransferLeg> legs, out List<ClaimEndpoint> endpoints,
                out IResourcePurchaseCommit purchaseCommit, true, captured);
            if (status != ResourceTransferStatus.Ready) return new ResourceTransferBatchResult(status);
            var facts = new List<ResourceFlowFact>();
            var executionIntents = new List<ResourceTransferIntent>();
            foreach (TransferLeg leg in legs) executionIntents.Add(leg.Intent);
            var execution = new ResourceSupplyExecution(executionIntents.AsReadOnly());
            try
            {
                foreach (TransferLeg leg in legs)
                {
                    ResourceTransferIntent intent = leg.Intent;
                    ClaimEndpoint source = leg.Source, destination = leg.Destination;
                    IResourceInlet inlet = (IResourceInlet)destination.Provider;
                    ISupplier supplier = source.Provider as ISupplier;
                    IResourceOutlet physicalSource = source.Source;
                    ResourceFlowKind flowKind = intent.Kind != ResourceTransferKind.Supply ? ResourceFlowKind.StockTransfer
                        : physicalSource != null ? ResourceFlowKind.StockPurchase : ResourceFlowKind.AuthoredCreation;
                    int beforeSourceVersion = source.Version, beforeDestinationVersion = destination.Version;
                    Fix beforeIn = destination.Amounts[intent.Resource];
                    Fix? beforeOut = physicalSource == null ? null : (Fix?)source.Amounts[intent.Resource];
                    CheckClaimEndpoints(endpoints);
                    IReadOnlyList<ResourceLotAmount> lotSelection = physicalSource is IResourceLotEndpoint lotSource
                        ? EntityObservation.ReadProvider(_world, lotSource, null, () => lotSource.SelectLots(intent.Owner, intent.Source, intent.Amount)) : null;
                    Code selection = Code.None;
                    foreach (var record in source.Records) if (record.Token.Equals(intent.Source)) { selection = record.Selection; break; }
                    if (physicalSource == null && inlet is IResourceLotEndpoint)
                        lotSelection = new[] { new ResourceLotAmount(intent.Resource, Code.None, intent.Amount) };
                    Fix taken = intent.Kind == ResourceTransferKind.Supply
                        ? supplier.SupplyReserved(_world, intent.Company, intent.Owner, intent.Source, intent.Amount, execution)
                        : physicalSource.TakeReserved(intent.Owner, intent.Source, intent.Amount);
                    if (taken != intent.Amount) throw new InvalidOperationException("resource source violated exact physical transfer receipt");
                    source.Reduce(intent.Source, intent.Amount);
                    if (physicalSource != null) source.Amounts[intent.Resource] = beforeOut.Value - intent.Amount;
                    source.AdvanceVersion(); CheckClaimEndpoints(endpoints);
                    Fix admitted = lotSelection != null && inlet is IResourceLotEndpoint lotDestination
                        ? lotDestination.ReceiveLotsReserved(_world.Content, intent.Owner, intent.Destination, lotSelection)
                        : inlet.ReceiveReserved(_world.Content, intent.Owner, intent.Destination, intent.Amount);
                    if (admitted != intent.Amount) throw new InvalidOperationException("resource destination violated exact physical transfer receipt");
                    destination.Reduce(intent.Destination, intent.Amount);
                    destination.Amounts[intent.Resource] = beforeIn + intent.Amount;
                    destination.AdvanceVersion(); CheckClaimEndpoints(endpoints);
                    facts.Add(new ResourceFlowFact(_world.Tick, intent,
                        _world.Content.Resource(intent.Resource).QuantityUnit.Unit, flowKind, beforeOut,
                        physicalSource == null ? null : (Fix?)source.Amounts[intent.Resource], beforeIn, destination.Amounts[intent.Resource],
                        beforeSourceVersion, source.Version, beforeDestinationVersion, destination.Version).WithLots(lotSelection, selection));
                }
                if (release != null && release.Count != 0)
                {
                    var remaining = new List<ResourceReservation>();
                    foreach (ResourceReservation record in release)
                    {
                        ClaimEndpoint endpoint = endpoints.Find(candidate => candidate.Identity == record.Token.Endpoint);
                        foreach (ResourceReservation current in endpoint.Records)
                            if (current.Token.Equals(record.Token)) { remaining.Add(current); break; }
                    }
                    var parties = new SortedDictionary<int, List<ResourceReservation>>();
                    foreach (var record in remaining)
                    {
                        int party = companies[record.Owner].Value;
                        if (!parties.TryGetValue(party, out var group)) parties.Add(party, group = new List<ResourceReservation>());
                        group.Add(record);
                    }
                    foreach (var party in parties)
                        if (!FinishCapturedClaims(new PlayerId(party.Key), party.Value, true, Code.None, endpoints).Committed)
                            throw new InvalidOperationException("movement participant release changed after complete admission");
                    CheckClaimEndpoints(endpoints);
                }
                execution.Complete();
                purchaseCommit?.Commit();
                CheckClaimEndpoints(endpoints);
                // No partially successful transfer history exists if any later provider violates its contract.
                foreach (ResourceFlowFact fact in facts) _world.Events.Publish(fact);
                return new ResourceTransferBatchResult(ResourceTransferStatus.Committed, legs.Count);
            }
            finally { execution.Close(); }
        }
    }
}
