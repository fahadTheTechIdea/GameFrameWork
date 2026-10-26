using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>One physical-transfer protocol over current providers. No balances, promises or persistent ledger.</summary>
    public sealed partial class ResourceTransactions : IResourceTransaction, ICompositionValidator
    {
        private readonly World _world;
        // This owner's instance alone tracks its nested commits; the scope primitive is shared.
        private readonly SimulationOperation _operation = new SimulationOperation();
        public bool OperationInProgress
        {
            get { _world.RequireHealthyUpdate(); return _operation.Active; }
        }
        public static readonly Code TradingDisabled = new Code("business.trading-disabled");
        public static readonly Code UnavailableTradingCapability = new Code("business.trading-capability-unavailable");
        internal static ResourceTransferStatus TradingAdmission(World world)
        {
            if (!world.Services.TryGet<IResourceTradingRules>(out var rules)) return ResourceTransferStatus.UnavailableCapability;
            ResourceTransferStatus status = rules.CheckTrading();
            if (status != ResourceTransferStatus.Ready && status != ResourceTransferStatus.TradingDisabled && status != ResourceTransferStatus.UnavailableCapability)
                throw new InvalidOperationException("trading rules returned an invalid capability admission");
            return status;
        }
        public ResourceTransactions(World world) { _world = world ?? throw new ArgumentNullException(nameof(world)); }
        // One fallback policy at the actual owner; earlier observation/commit faults retain precedence.
        private void LatchTransactionFault(Exception error, string phase = "resource.transaction")
            => _world.LatchFault(phase, error);
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(_world, world)) throw new Content.ContentException("resource transaction belongs to another session");
            StorageDeclaration.ValidateDefinitions(world);
            OperatingInputsContracts.ValidateDefinitions(world);
            ResourcePortSelectionContracts.ValidateDefinitions(world);
            if (TradingAdmission(world) == ResourceTransferStatus.Ready && !world.Services.Has<IResourcePurchaseSettlement>())
                throw new Content.ContentException("enabled trading requires the actual purchase settlement contract");
            var scopes = new HashSet<string>(StringComparer.Ordinal);
            foreach (IResourceEndpointProvider provider in world.Services.Providers<IResourceEndpointProvider>())
            {
                string scope = provider.EndpointScope, owner = world.Services.OwnerOf(provider);
                if (!ReferenceEquals(provider.World, world) || string.IsNullOrWhiteSpace(scope) || scope == ResourceEndpointId.EntityScope ||
                    scope != owner && !scope.StartsWith(owner + ".", StringComparison.Ordinal) || !scopes.Add(scope))
                    throw new Content.ContentException("invalid, foreign or duplicate resource endpoint provider: " + scope);
            }
        }

        internal static IResourceEndpoint Resolve(World world, ResourceEndpointId endpoint, bool supply = false)
        {
            if (endpoint.Scope == ResourceEndpointId.EntityScope)
            {
                Entity entity = world.Entities.Get(endpoint.ToEntity());
                return supply ? (IResourceEndpoint)entity?.Get<ISupplier>() : entity?.Get<IInletPort>();
            }
            foreach (IResourceEndpointProvider provider in world.Services.Providers<IResourceEndpointProvider>())
                if (provider.EndpointScope == endpoint.Scope)
                {
                    // Resolution precedes stock checkpoints: observe the actual owner even when
                    // it returns no endpoint. A malformed lookup must never become healthy absence.
                    return EntityObservation.ReadProvider(world, provider, null, () => {
                        IResourceEndpoint found = provider.Resolve(endpoint);
                        if (found != null && (found.Endpoint != endpoint || found.Version < 0 ||
                            found.Company != PlayerId.None && !world.Players.IsSeat(found.Company)))
                            throw new InvalidOperationException("resource endpoint provider returned an invalid identity/state");
                        return found;
                    }, observeTargetState: true, faultPhase: "resource.endpoint.resolve");
                }
            return null;
        }
        public ResourceClaimSnapshot ReadClaims(PlayerId company, ResourceEndpointId endpoint, ReservationOwner owner)
        {
            _world.RequireHealthyUpdate();
            try
            {
                if (!ValidClaimCompany(company) || !endpoint.IsValid || !owner.IsValid)
                    return new ResourceClaimSnapshot(ResourceTransferStatus.InvalidRequest, endpoint, 0, Array.Empty<ResourceReservation>());
                var endpoints = new List<ClaimEndpoint>();
                ResourceTransferStatus status = ClaimAccess(company, endpoint, ReservationDirection.Inbound, endpoints, out var captured);
                if (status != ResourceTransferStatus.Ready)
                    return new ResourceClaimSnapshot(status, endpoint, 0, Array.Empty<ResourceReservation>());
                var records = new List<ResourceReservation>();
                foreach (var record in captured.Records) if (record.Owner.Equals(owner)) records.Add(record);
                CheckClaimEndpoints(endpoints);
                return new ResourceClaimSnapshot(status, endpoint, captured.Version, records);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }
        public static readonly Code UndisposedStock = new Code("sim.resource.undisposed-stock");
        public static readonly Code ActivePromises = new Code("sim.resource.active-promises");
        public static readonly Code MissingRecovery = new Code("sim.resource.missing-recovery");

        public static Code CheckInventoryRemoval(IInventory inventory)
            => inventory.Reservations.Count != 0 ? ActivePromises : inventory.Stock.Count != 0 ? UndisposedStock : Code.None;

        /// <summary>Single initialization rule shared by built-in and external inventory providers.</summary>
        public static void InitializeStock(World world, Entity entity)
        {
            if (world == null || entity == null || !ReferenceEquals(world.Entities.Get(entity.Id), entity))
                throw new InvalidOperationException("initial inventory requires a registered live entity");
            world.RequireHealthyUpdate();
            IInventory stock = entity.Get<IInventory>();
            IInventoryDefinition definition = entity.Def.Component<IInventoryDefinition>();
            if (stock == null || definition?.InitialStock == null || stock.Stock.Count != 0 || stock.Reservations.Count != 0)
                throw new InvalidOperationException("inventory creation must begin empty with an explicit authored initial bill");
            // No resource mutation or fact exists for an empty authored bill. Other newly registered
            // capabilities may still be receiving their creation bindings; do not observe them here.
            if (definition.InitialStock.IsEmpty) return;
            var intent = new ResourceInventoryIntent(new ReservationOwner("sim.inventory.initial", entity.Id.Value),
                entity.Owner, entity.Id, definition.InitialStock, ResourceFlowKind.Initialized, new Code(entity.Def.Id),
                (definition as IStorageDefinition)?.InitialLots);
            ResourceInventoryResult result = world.Services.Get<IResourceTransaction>().CommitInventory(intent);
            if (!result.Committed) throw new InvalidOperationException("authored inventory initialization refused: " + result.Status);
        }

        public ResourceInventoryCheck EvaluateInventory(ResourceInventoryIntent intent)
        {
            _world.RequireHealthyUpdate();
            try
            {
                return PrepareInventory(intent, false, out _);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        public ResourceInventoryCheck EvaluateInitialInventory(Content.EntityDef definition, Bill additional)
        {
            _world.RequireHealthyUpdate();
            try
            {
                if (definition == null || !ReferenceEquals(_world.Content.Find<Content.EntityDef>(definition.Id), definition)
                    || additional == null) return new ResourceInventoryCheck(ResourceTransferStatus.InvalidRequest);
                return EntityObservation.ReadProvider(_world, definition, null, () => {
                    IInventoryDefinition inventory = definition.Component<IInventoryDefinition>();
                    if (additional.IsEmpty) return new ResourceInventoryCheck(ResourceTransferStatus.Ready);
                    if (inventory == null || !definition.HasCapability<IInventory>() || inventory.InitialStock == null)
                        return new ResourceInventoryCheck(ResourceTransferStatus.Incompatible);
                    foreach (ResourceAmount line in additional.Lines)
                    {
                        var resource = _world.Content.Resource(line.Resource);
                        if (resource == null || !line.Amount.IsPositive
                            || UnitConversion.ValidateAmount(resource.QuantityUnit, line.Amount) != UnitConversionStatus.Converted)
                            return new ResourceInventoryCheck(ResourceTransferStatus.InvalidRequest);
                        if (!inventory.Accepts(_world.Content, line.Resource)
                            || CapacityPolicyContracts.Quantum(inventory.CapacityPolicy, line.Resource).Floor(line.Amount) != line.Amount)
                            return new ResourceInventoryCheck(ResourceTransferStatus.Incompatible);
                    }
                    return new ResourceInventoryCheck(CapacityPolicyContracts.Check(inventory.CapacityPolicy,
                        inventory.InitialStock.Lines, Array.Empty<ResourceAmount>(), additional.Lines).Fits
                        ? ResourceTransferStatus.Ready : ResourceTransferStatus.NoCapacity);
                });
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        private ResourceInventoryCheck PrepareInventory(ResourceInventoryIntent intent, bool committing, out IResourceInventoryCommit settlement)
        {
            _world.RequireHealthyUpdate();
            settlement = null;
            if (!intent.Owner.IsValid || !intent.Endpoint.IsValid || intent.Cause.IsNone || intent.Quantities == null ||
                intent.Kind < ResourceFlowKind.Initialized || intent.Kind > ResourceFlowKind.Sold ||
                intent.Company != PlayerId.None && !_world.Players.IsSeat(intent.Company))
                return new ResourceInventoryCheck(ResourceTransferStatus.InvalidRequest);
            Entity entity = _world.Entities.Get(intent.Endpoint);
            if (entity == null) return new ResourceInventoryCheck(ResourceTransferStatus.MissingEndpoint);
            if (entity.Owner != intent.Company) return new ResourceInventoryCheck(ResourceTransferStatus.InvalidRequest);
            IInventory stock = entity.Get<IInventory>();
            if (stock == null) return new ResourceInventoryCheck(ResourceTransferStatus.Incompatible);
            int version = entity.Version;
            // Direct lifecycle callers need the same original codec/clock/space boundary as work previews.
            // Entity.Version alone cannot detect a provider changing private saved state without Touch.
            ResourceTransferStatus status = EntityObservation.ReadProvider(_world, stock, entity, () => {
                if (intent.InitialLots.Count != 0)
                {
                    if (intent.Kind != ResourceFlowKind.Initialized || !(stock is IResourceLotEndpoint) || stock.Stock.Count != 0 || stock.Reservations.Count != 0)
                        return ResourceTransferStatus.Incompatible;
                    var totals = new Dictionary<ResourceId, Fix>(); var identities = new HashSet<Code>();
                    foreach (var lot in intent.InitialLots)
                    {
                        if (lot.Lot.IsNone || lot.GeneratedQualities != null || !lot.ValidMetadata(_world.Content) || !identities.Add(lot.Lot) ||
                            !ValidClaimAmount(lot.Resource, lot.Amount)) return ResourceTransferStatus.InvalidRequest;
                        totals.TryGetValue(lot.Resource, out Fix amount); totals[lot.Resource] = amount + lot.Amount;
                        if (totals[lot.Resource] > intent.Quantities.AmountOf(lot.Resource)) return ResourceTransferStatus.InvalidRequest;
                        if (CapacityPolicyContracts.Quantum(stock.CapacityPolicy, lot.Resource).Floor(lot.Amount) != lot.Amount) return ResourceTransferStatus.Incompatible;
                    }
                }
                foreach (ResourceAmount line in intent.Quantities.Lines)
                {
                    var definition = _world.Content.Resource(line.Resource);
                    if (definition == null || !line.Amount.IsPositive ||
                        UnitConversion.ValidateAmount(definition.QuantityUnit, line.Amount) != UnitConversionStatus.Converted)
                        return ResourceTransferStatus.InvalidRequest;
                    if (!stock.Accepts(_world.Content, line.Resource) || CapacityPolicyContracts.Quantum(stock.CapacityPolicy, line.Resource).Floor(line.Amount) != line.Amount)
                        return ResourceTransferStatus.Incompatible;
                    if (!intent.AddsStock && stock.Available(line.Resource) < line.Amount)
                        return ResourceTransferStatus.NoStock;
                }
                return intent.AddsStock && !stock.CheckIncoming(intent.Quantities.Lines).Fits
                    ? ResourceTransferStatus.NoCapacity : ResourceTransferStatus.Ready;
            }, space: _world.Space, observeTargetState: true);
            if (status == ResourceTransferStatus.Ready && intent.Kind == ResourceFlowKind.Produced &&
                !intent.Quantities.IsEmpty && _world.Services.Has<IResourceProductionSettlement>())
            {
                var endpoints = new List<ClaimEndpoint>();
                TransferEndpoint(stock.Endpoint, false, endpoints);
                Code refusal = PrepareGeneration(intent.Owner, intent.Company, intent.Cause, Bill.Empty, intent.Quantities, committing, out var productionCommit);
                settlement = productionCommit;
                CheckClaimEndpoints(endpoints);
                if (!refusal.IsNone) return new ResourceInventoryCheck(ResourceTransferStatus.SettlementRefused, version, refusal);
            }
            if (status == ResourceTransferStatus.Ready && intent.Kind == ResourceFlowKind.Sold)
            {
                if (!_world.Services.TryGet<IResourceSaleSettlement>(out var sales))
                    return new ResourceInventoryCheck(ResourceTransferStatus.UnavailableCapability, version);
                var prepared = EntityObservation.ReadProvider(_world, sales, entity, () => {
                    Code code = sales.PrepareSale(intent, out var saleCommit); return (code, saleCommit);
                }, observeTargetState: true);
                if (!prepared.Item1.IsNone || prepared.Item2 == null)
                    return new ResourceInventoryCheck(ResourceTransferStatus.SettlementRefused, version, prepared.Item1);
                settlement = committing ? prepared.Item2 : null;
            }
            return new ResourceInventoryCheck(status, version);
        }

        public ResourceInventoryResult CommitInventory(ResourceInventoryIntent intent)
        {
            _world.RequireHealthyUpdate();
            try
            {
                using var operation = _operation.Enter();
                ResourceInventoryCheck check = PrepareInventory(intent, true, out IResourceInventoryCommit settlement);
                if (!check.Ready) return new ResourceInventoryResult(check.Status, blocker: check.Blocker);
                Entity entity = _world.Entities.Get(intent.Endpoint); IInventory stock = entity.Get<IInventory>();
                var envelope = EntityObservation.Capture(_world, entity, allowEntityVersionChange: true);
                var spatial = EntityObservation.CaptureSpace(_world, _world.Space);
                var checkpoint = EntityObservation.ReadProvider(_world, stock, entity, () => {
                    var stockVector = new List<ResourceAmount>(stock.Stock);
                    var records = new List<ResourceReservation>(stock.Reservations);
                    var amounts = new Fix[intent.Quantities.Count]; var units = new UnitId[amounts.Length];
                    for (int i = 0; i < amounts.Length; i++) {
                        ResourceId resource = intent.Quantities[i].Resource;
                        amounts[i] = stock.Amount(resource);
                        units[i] = _world.Content.Resource(resource).QuantityUnit.Unit;
                    }
                    var lots = stock is IResourceLotEndpoint tracked ? new List<ResourceLotAmount>(tracked.Lots).AsReadOnly() : null;
                    return (stockVector, records, amounts, units, lots);
                }, space: _world.Space, observeTargetState: true);
                Fix[] before = checkpoint.amounts;
                var versionsBefore = new int[before.Length]; var versionsAfter = new int[before.Length];
                if (entity.Version != check.EndpointVersion) throw new InvalidOperationException("inventory provider changed lifecycle checkpoint");
                for (int i = 0; i < before.Length; i++)
                {
                    ResourceAmount line = intent.Quantities[i]; versionsBefore[i] = entity.Version;
                    Fix changed = intent.AddsStock ? stock.Add(line.Resource, line.Amount) : stock.Remove(line.Resource, line.Amount);
                    if (intent.InitialLots.Count != 0)
                    {
                        var initialized = new List<ResourceLotAmount>();
                        foreach (var lot in intent.InitialLots) if (lot.Resource == line.Resource) initialized.Add(lot);
                        if (initialized.Count != 0) ((IResourceLotEndpoint)stock).InitializeLots(initialized.AsReadOnly());
                    }
                    envelope.Verify(); spatial.Verify(); _world.RequireHealthyUpdate();
                    Fix expected = intent.AddsStock ? before[i] + line.Amount : before[i] - line.Amount;
                    versionsAfter[i] = entity.Version;
                    Fix actual = EntityObservation.ReadProvider(_world, stock, entity, () => stock.Amount(line.Resource),
                        space: _world.Space, observeTargetState: true);
                    if (changed != line.Amount || actual != expected || versionsAfter[i] <= versionsBefore[i])
                        throw new InvalidOperationException("inventory provider violated exact lifecycle batch");
                }
                // Inspect the complete vector and original promises as pure reads before any success fact.
                EntityObservation.ReadProvider(_world, stock, entity, () => {
                    var expectedStock = new SortedDictionary<int, Fix>();
                    foreach (ResourceAmount line in checkpoint.stockVector) expectedStock.Add(line.Resource.Value, line.Amount);
                    foreach (ResourceAmount line in intent.Quantities.Lines)
                    {
                        expectedStock.TryGetValue(line.Resource.Value, out Fix held);
                        Fix changed = intent.AddsStock ? held + line.Amount : held - line.Amount;
                        if (changed.IsPositive) expectedStock[line.Resource.Value] = changed; else expectedStock.Remove(line.Resource.Value);
                    }
                    IReadOnlyList<ResourceAmount> afterStock = stock.Stock;
                    if (afterStock.Count != expectedStock.Count) throw new InvalidOperationException("inventory provider changed unrelated lifecycle stock");
                    int at = 0;
                    foreach (KeyValuePair<int, Fix> line in expectedStock)
                        if (afterStock[at].Resource.Value != line.Key || afterStock[at++].Amount != line.Value)
                            throw new InvalidOperationException("inventory provider changed unrelated lifecycle stock");
                    IReadOnlyList<ResourceReservation> records = stock.Reservations;
                    if (records.Count != checkpoint.records.Count) throw new InvalidOperationException("inventory provider changed lifecycle promises");
                    for (int i = 0; i < records.Count; i++)
                        if (!records[i].Token.Equals(checkpoint.records[i].Token) || !records[i].Owner.Equals(checkpoint.records[i].Owner) ||
                            records[i].Resource != checkpoint.records[i].Resource || records[i].Amount != checkpoint.records[i].Amount || records[i].Direction != checkpoint.records[i].Direction ||
                            stock is IResourceLotEndpoint && !ResourceReservation.SameLots(records[i], checkpoint.records[i]))
                            throw new InvalidOperationException("inventory provider changed lifecycle promises");
                    if (stock is IResourceLotEndpoint tracked) ResourceLotSelection.Unclaimed(tracked.Lots, records);
                    return true;
                }, space: _world.Space, observeTargetState: true);
                int committedVersion = before.Length == 0 ? check.EndpointVersion : versionsAfter[before.Length - 1];
                if (entity.Version != committedVersion) throw new InvalidOperationException("inventory provider mutated lifecycle post-inspection");
                if (settlement != null)
                    // Settlement may mutate its own state, not the already verified physical endpoint.
                    EntityObservation.ReadProvider(_world, entity, entity, () => { settlement.Commit(); return true; },
                        space: _world.Space, observeTargetState: true);
                envelope.Verify(); spatial.Verify(); _world.RequireHealthyUpdate();
                var attributed = EntityObservation.ReadProvider(_world, stock, entity, () => {
                    var values = new List<IReadOnlyList<ResourceLotAmount>>();
                    foreach (var line in intent.Quantities.Lines) values.Add(checkpoint.lots == null ? null :
                        ResourceLotBuffer.Difference(checkpoint.lots, ((IResourceLotEndpoint)stock).Lots, line.Resource, intent.AddsStock));
                    return values;
                }, space: _world.Space, observeTargetState: true);
                for (int i = 0; i < before.Length; i++)
                {
                    ResourceAmount line = intent.Quantities[i];
                    Fix after = intent.AddsStock ? before[i] + line.Amount : before[i] - line.Amount;
                    _world.Events.Publish(new ResourceFlowFact(_world.Tick, intent, line,
                        checkpoint.units[i], before[i], after, versionsBefore[i], versionsAfter[i]).WithLots(attributed[i]));
                }
                return new ResourceInventoryResult(ResourceTransferStatus.Committed, before.Length);
            }
            catch (Exception error) { LatchTransactionFault(error, "resource.inventory-commit"); throw; }
        }

        public ResourceTransferCheck Evaluate(ResourceTransferIntent intent)
        {
            _world.RequireHealthyUpdate();
            try
            {
                ResourceTransferStatus status = PrepareTransfers(new[] { intent }, out List<TransferLeg> legs, out _, out _);
                return status != ResourceTransferStatus.Ready ? new ResourceTransferCheck(status)
                    : new ResourceTransferCheck(status, legs[0].Source.Version, legs[0].Destination.Version);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        private ResourceTransferStatus CaptureClaimParticipants(PlayerId company, IReadOnlyList<ResourceReservation> participants,
            IReadOnlyList<ResourceClaimIntent> intents, IReadOnlyList<ResourceReservation> receipts, out List<ClaimEndpoint> endpoints,
            IReadOnlyList<ResourceEndpointId> protectedEndpoints = null, bool releaseInspection = false, bool allowAbsent = false)
        {
            endpoints = null;
            if (participants == null) return ResourceTransferStatus.InvalidRequest;
            if (participants.Count == 0 && !releaseInspection) return ResourceTransferStatus.Ready;
            if (!ValidClaimCompany(company)) return ResourceTransferStatus.InvalidRequest;
            var records = new List<ResourceReservation>(participants); var tokens = new HashSet<ReservationToken>();
            var identities = new HashSet<ResourceEndpointId>();
            foreach (var record in records)
            {
                if (!record.Owner.IsValid || !record.Token.IsValid || !tokens.Add(record.Token) ||
                    record.Direction != ReservationDirection.Outbound && record.Direction != ReservationDirection.Inbound ||
                    !ValidClaimAmount(record.Resource, record.Amount)) return ResourceTransferStatus.InvalidReceipt;
                identities.Add(record.Token.Endpoint);
            }
            if (intents != null) foreach (var intent in intents)
            {
                if (!intent.Endpoint.IsValid || !intent.Owner.IsValid || intent.Quantities == null ||
                    intent.Direction != ReservationDirection.Outbound && intent.Direction != ReservationDirection.Inbound)
                    return ResourceTransferStatus.InvalidRequest;
                identities.Add(intent.Endpoint);
            }
            if (receipts != null) foreach (var receipt in receipts)
            { if (!receipt.Token.IsValid) return ResourceTransferStatus.InvalidReceipt; identities.Add(receipt.Token.Endpoint); }
            if (protectedEndpoints != null) foreach (var identity in protectedEndpoints)
            { if (!identity.IsValid) return ResourceTransferStatus.InvalidRequest; identities.Add(identity); }
            var ordered = new List<ResourceEndpointId>(identities); ordered.Sort(CompareTransferEndpoints);
            endpoints = new List<ClaimEndpoint>();
            foreach (var identity in ordered)
            {
                var endpoint = TransferEndpoint(identity, false, endpoints);
                if (endpoint == null) { if (allowAbsent) continue; return ResourceTransferStatus.MissingEndpoint; }
                if (releaseInspection && !MayUse(endpoint.Provider, company)) return ResourceTransferStatus.InvalidRequest;
            }
            if (allowAbsent)
            {
                ResourceTransferStatus filter = SurvivingClaimRecords(records, endpoints, out records);
                if (filter != ResourceTransferStatus.Ready) return filter;
            }
            ResourceTransferStatus status = records.Count == 0 ? ResourceTransferStatus.Ready
                : PrepareClaimReceipts(company, records, releaseInspection, out _, out _, endpoints);
            CheckClaimEndpoints(endpoints); return status;
        }

        public ResourceClaimCheck EvaluateClaims(PlayerId company, IReadOnlyList<ResourceClaimIntent> intents,
            IReadOnlyList<ResourceReservation> participants)
        {
            _world.RequireHealthyUpdate();
            try
            {
                var requests = intents == null ? null : new List<ResourceClaimIntent>(intents);
                ResourceTransferStatus status = CaptureClaimParticipants(company, participants, requests, null, out List<ClaimEndpoint> captured);
                if (status != ResourceTransferStatus.Ready) return new ResourceClaimCheck(status);
                status = PrepareClaims(company, requests, out _, out List<ClaimEndpoint> endpoints, true, captured);
                var availability = new List<ResourceClaimAvailability>();
                if (status == ResourceTransferStatus.Ready || status == ResourceTransferStatus.NoStock ||
                    status == ResourceTransferStatus.NoCapacity || status == ResourceTransferStatus.Incompatible)
                    foreach (var intent in requests)
                    {
                        ClaimEndpoint endpoint = endpoints.Find(candidate => candidate.Identity == intent.Endpoint);
                        if (endpoint == null || intent.Quantities == null ||
                            intent.Direction == ReservationDirection.Outbound && endpoint.Source == null) continue;
                        foreach (var line in intent.Quantities.Lines)
                            if (ValidClaimAmount(line.Resource, line.Amount))
                            {
                                var selection = ClaimSelection(intent, line.Resource, endpoint);
                                Fix offer = intent.Direction == ReservationDirection.Outbound && selection != null
                                    ? endpoint.Source is IResourceLotEndpoint lotSource ? EligibleAmount(lotSource, endpoint, line.Resource, selection) : Fix.Zero
                                    : intent.Direction == ReservationDirection.Outbound ? endpoint.Available(line.Resource)
                                    : endpoint.Inlet.Offer(line.Resource, line.Amount);
                                CheckClaimEndpoints(endpoints);
                                if (offer < Fix.Zero || intent.Direction == ReservationDirection.Inbound && offer > line.Amount)
                                    throw new InvalidOperationException("resource provider returned an invalid inbound claim offer");
                                availability.Add(new ResourceClaimAvailability(intent.Direction, intent.Endpoint, line.Resource,
                                    _world.Content.Resource(line.Resource).QuantityUnit.Unit, offer, line.Amount));
                            }
                    }
                return new ResourceClaimCheck(status, availability);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        public ResourceClaimResult AcquireClaims(PlayerId company, IReadOnlyList<ResourceClaimIntent> intents,
            IReadOnlyList<ResourceReservation> participants)
        {
            _world.RequireHealthyUpdate();
            try
            {
                using var operation = _operation.Enter();
                ResourceTransferStatus status = CaptureClaimParticipants(company, participants, intents, null, out List<ClaimEndpoint> captured);
                return status == ResourceTransferStatus.Ready ? AcquireCapturedClaims(company, intents, captured) : new ResourceClaimResult(status);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }
        private ResourceClaimResult AcquireCapturedClaims(PlayerId company, IReadOnlyList<ResourceClaimIntent> intents, List<ClaimEndpoint> captured)
        {
            ResourceTransferStatus status = PrepareClaims(company, intents, out List<ClaimLeg> legs, out List<ClaimEndpoint> endpoints, captured: captured);
            if (status != ResourceTransferStatus.Ready) return new ResourceClaimResult(status);
            var acquired = new List<ResourceReservation>();
            foreach (ClaimLeg leg in legs)
            {
                if (leg.Token.IsValid)
                {
                    int index = leg.Endpoint.Records.FindIndex(record => record.Token.Equals(leg.Token));
                    ResourceReservation original = leg.Endpoint.Records[index];
                    if (leg.Endpoint.Source is IResourceLotEndpoint growthSource) growthSource.IncreaseLotsReservedOut(leg.Owner, leg.Token, leg.Lots);
                    else leg.Endpoint.Source.IncreaseReservedOut(leg.Owner, leg.Token, leg.Amount);
                    var increased = original.Increased(leg.Amount, leg.Lots);
                    leg.Endpoint.Records[index] = increased;
                    leg.Endpoint.AdvanceVersion(); CheckClaimEndpoints(endpoints); acquired.Add(increased);
                    continue;
                }
                ReservationToken token = leg.Direction == ReservationDirection.Outbound
                    ? leg.Endpoint.Source is IResourceLotEndpoint lotSource
                        ? lotSource.ReserveLotsOut(leg.Owner, leg.Resource, leg.Lots, leg.Selection == null ? Code.None : new Code(leg.Selection.Id))
                        : leg.Endpoint.Source.ReserveOut(leg.Owner, leg.Resource, leg.Amount)
                    : leg.Endpoint.Inlet.ReserveIn(leg.Owner, leg.Resource, leg.Amount);
                if (!token.IsValid || token.Endpoint != leg.Endpoint.Identity ||
                    ResourceReservationBook.ContainsToken(leg.Endpoint.Records, token))
                    throw new InvalidOperationException("resource provider returned an invalid batch claim");
                var record = new ResourceReservation(token, leg.Owner, leg.Resource, leg.Amount, leg.Direction,
                    leg.Selection == null ? Code.None : new Code(leg.Selection.Id), leg.Lots);
                leg.Endpoint.Records.Add(record); leg.Endpoint.AdvanceVersion(); CheckClaimEndpoints(endpoints);
                acquired.Add(record);
            }
            return new ResourceClaimResult(ResourceTransferStatus.Committed, acquired);
        }

        public ResourceClaimCheck EvaluateClaimReceipts(PlayerId company, IReadOnlyList<ResourceReservation> receipts)
        {
            _world.RequireHealthyUpdate();
            try
            {
                return new ResourceClaimCheck(PrepareClaimReceipts(company, receipts, false, out _, out _));
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        public ResourceClaimResult ExtendClaim(PlayerId company, ResourceReservation receipt, Fix amount)
        {
            _world.RequireHealthyUpdate();
            try
            {
                using var operation = _operation.Enter();
                if (!ValidClaimAmount(receipt.Resource, amount)) return new ResourceClaimResult(ResourceTransferStatus.InvalidRequest);
                return AcquireClaims(company, new[] { new ResourceClaimIntent(receipt,
                    new Bill(new[] { new ResourceAmount(receipt.Resource, amount) })) }, System.Array.Empty<ResourceReservation>());
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        public ResourceClaimCheck EvaluateClaimRelease(ResourceClaimReleaseIntent intent)
        {
            _world.RequireHealthyUpdate();
            try
            {
                return new ResourceClaimCheck(PrepareClaimRelease(intent, out _, out _));
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }
        public ResourceClaimResult ReleaseClaims(ResourceClaimReleaseIntent intent)
        {
            _world.RequireHealthyUpdate();
            try
            {
                using var operation = _operation.Enter();
                ResourceTransferStatus status = PrepareClaimRelease(intent, out List<ResourceReservation> records, out List<ClaimEndpoint> endpoints);
                if (status != ResourceTransferStatus.Ready) return new ResourceClaimResult(status);
                return records.Count == 0 ? new ResourceClaimResult(ResourceTransferStatus.Committed)
                    : FinishCapturedClaims(intent.Company, records, true, Code.None, endpoints);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }
        private static ResourceTransferStatus SurvivingClaimRecords(IReadOnlyList<ResourceReservation> records,
            List<ClaimEndpoint> endpoints, out List<ResourceReservation> surviving)
        {
            surviving = new List<ResourceReservation>();
            foreach (var record in records)
            {
                var endpoint = endpoints.Find(candidate => candidate.Identity == record.Token.Endpoint);
                if (endpoint == null) continue;
                bool found = false;
                foreach (var current in endpoint.Records) if (current.Token.Equals(record.Token)) { found = true; break; }
                if (!found) continue;
                if (!ResourceReservationBook.Matches(endpoint.Records, endpoint.Identity, record.Owner, record.Token,
                    record.Resource, record.Amount, record.Direction)) return ResourceTransferStatus.InvalidReceipt;
                surviving.Add(record);
            }
            return ResourceTransferStatus.Ready;
        }
        private ResourceTransferStatus PrepareClaimRelease(ResourceClaimReleaseIntent intent,
            out List<ResourceReservation> records, out List<ClaimEndpoint> endpoints)
        {
            records = null; endpoints = null;
            if (!ValidClaimCompany(intent.Company) || intent.Receipts == null || intent.Receipts.Count == 0 ||
                intent.Participants == null || intent.Endpoints == null ||
                intent.Policy != ResourceClaimReleasePolicy.Exact && intent.Policy != ResourceClaimReleasePolicy.Surviving)
                return ResourceTransferStatus.InvalidRequest;
            var tokens = new HashSet<ReservationToken>();
            foreach (var record in intent.Receipts)
                if (!record.Owner.IsValid || !record.Token.IsValid || !tokens.Add(record.Token) ||
                    record.Direction != ReservationDirection.Outbound && record.Direction != ReservationDirection.Inbound ||
                    !ValidClaimAmount(record.Resource, record.Amount)) return ResourceTransferStatus.InvalidReceipt;
            bool surviving = intent.Policy == ResourceClaimReleasePolicy.Surviving;
            ResourceTransferStatus status = CaptureClaimParticipants(intent.Company, intent.Participants, null, intent.Receipts,
                out endpoints, intent.Endpoints, releaseInspection: true, allowAbsent: surviving);
            if (status != ResourceTransferStatus.Ready) return status;
            records = new List<ResourceReservation>(intent.Receipts);
            if (surviving)
            {
                status = SurvivingClaimRecords(records, endpoints, out records);
                if (status != ResourceTransferStatus.Ready) return status;
            }
            return records.Count == 0 ? ResourceTransferStatus.Ready
                : PrepareClaimReceipts(intent.Company, records, true, out _, out _, endpoints);
        }
        public ResourceClaimResult ConsumeClaims(PlayerId company, IReadOnlyList<ResourceReservation> receipts, Code cause,
            IReadOnlyList<ResourceReservation> participants)
        {
            _world.RequireHealthyUpdate();
            try
            {
                using var operation = _operation.Enter();
                ResourceTransferStatus status = PrepareClaimConsumption(company, receipts, cause, participants, out List<ClaimEndpoint> captured);
                return status == ResourceTransferStatus.Ready ? FinishCapturedClaims(company, receipts, false, cause, captured) : new ResourceClaimResult(status);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }
        public ResourceClaimCheck EvaluateClaimConsumption(PlayerId company, IReadOnlyList<ResourceReservation> receipts, Code cause,
            IReadOnlyList<ResourceReservation> participants)
        {
            _world.RequireHealthyUpdate();
            try
            {
                return new ResourceClaimCheck(PrepareClaimConsumption(company, receipts, cause, participants, out _));
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }
        private ResourceTransferStatus PrepareClaimConsumption(PlayerId company, IReadOnlyList<ResourceReservation> receipts, Code cause,
            IReadOnlyList<ResourceReservation> participants, out List<ClaimEndpoint> captured)
        {
            captured = null;
            if (cause.IsNone) return ResourceTransferStatus.InvalidRequest;
            ResourceTransferStatus status = CaptureClaimParticipants(company, participants, null, receipts, out captured);
            return status == ResourceTransferStatus.Ready ? PrepareClaimReceipts(company, receipts, false, out _, out _, captured, outboundOnly: true) : status;
        }

        private ResourceClaimResult FinishCapturedClaims(PlayerId company, IReadOnlyList<ResourceReservation> receipts, bool release, Code cause,
            List<ClaimEndpoint> captured)
        {
            ResourceTransferStatus status = PrepareClaimReceipts(company, receipts, release, out List<ClaimLeg> legs,
                out List<ClaimEndpoint> endpoints, captured, outboundOnly: !release);
            if (status != ResourceTransferStatus.Ready) return new ResourceClaimResult(status);
            var facts = new List<ResourceFlowFact>();
            foreach (ClaimLeg leg in legs)
            {
                ClaimEndpoint endpoint = leg.Endpoint; int version = endpoint.Version;
                Fix before = endpoint.Amounts[leg.Resource];
                var lots = !release && endpoint.Source is IResourceLotEndpoint tracked
                    ? EntityObservation.ReadProvider(_world, tracked, null, () => tracked.SelectLots(leg.Owner, leg.Token, leg.Amount), observeTargetState: true) : null;
                if (release)
                {
                    if (leg.Direction == ReservationDirection.Outbound) endpoint.Source.ReleaseOut(leg.Owner, leg.Token);
                    else endpoint.Inlet.ReleaseIn(leg.Owner, leg.Token);
                }
                else
                {
                    Fix taken = endpoint.Source.TakeReserved(leg.Owner, leg.Token, leg.Amount);
                    if (taken != leg.Amount) throw new InvalidOperationException("resource provider violated exact owned consumption");
                    endpoint.Amounts[leg.Resource] = before - leg.Amount;
                }
                int removed = endpoint.Records.RemoveAll(record => record.Token.Equals(leg.Token));
                if (removed != 1) throw new InvalidOperationException("batch consumption lost its checkpoint receipt");
                endpoint.AdvanceVersion(); CheckClaimEndpoints(endpoints);
                if (!release) facts.Add(new ResourceFlowFact(_world.Tick, company,
                    new ResourceReservation(leg.Token, leg.Owner, leg.Resource, leg.Amount, ReservationDirection.Outbound), cause,
                    _world.Content.Resource(leg.Resource).QuantityUnit.Unit, before, endpoint.Amounts[leg.Resource], version, endpoint.Version, ResourceFlowKind.Consumed)
                    .WithLots(lots, leg.Selection == null ? Code.None : new Code(leg.Selection.Id)));
            }
            foreach (ResourceFlowFact fact in facts) _world.Events.Publish(fact);
            return new ResourceClaimResult(ResourceTransferStatus.Committed);
        }

        private ResourceTransferStatus PrepareClaims(PlayerId company, IReadOnlyList<ResourceClaimIntent> intents,
            out List<ClaimLeg> legs, out List<ClaimEndpoint> endpoints, bool accessProbe = false, List<ClaimEndpoint> captured = null)
        {
            legs = new List<ClaimLeg>(); endpoints = captured ?? new List<ClaimEndpoint>();
            if (!ValidClaimCompany(company) || intents == null || intents.Count == 0) return ResourceTransferStatus.InvalidRequest;
            var requests = new List<ResourceClaimIntent>(intents);
            var demands = new HashSet<(ResourceEndpointId, ReservationOwner, ReservationDirection, ResourceId)>();
            var outgoing = new Dictionary<(ResourceEndpointId, ResourceId), long>();
            var incoming = new Dictionary<(ResourceEndpointId, ResourceId), long>();
            var originals = new List<ResourceReservation>();
            // Validate the complete detached request shape before any endpoint callback.
            foreach (ResourceClaimIntent intent in requests)
            {
                if (intent.Kind != ResourceClaimKind.Fresh && intent.Kind != ResourceClaimKind.Increase)
                    return ResourceTransferStatus.InvalidRequest;
                if (intent.Kind == ResourceClaimKind.Increase)
                {
                    ResourceReservation original = intent.OriginalReceipt;
                    if (!original.Token.IsValid || !original.Owner.IsValid || original.Direction != ReservationDirection.Outbound ||
                        !ValidClaimAmount(original.Resource, original.Amount)) return ResourceTransferStatus.InvalidReceipt;
                    if (intent.Quantities == null || intent.Quantities.Count != 1 || intent.Quantities[0].Resource != original.Resource)
                        return ResourceTransferStatus.InvalidRequest;
                    try { _ = original.Amount + intent.Quantities[0].Amount; }
                    catch (OverflowException) { return ResourceTransferStatus.InvalidRequest; }
                    originals.Add(original);
                }
                if (!intent.Owner.IsValid || !intent.Endpoint.IsValid || intent.Quantities == null || !accessProbe && intent.Quantities.IsEmpty ||
                    intent.Direction != ReservationDirection.Outbound && intent.Direction != ReservationDirection.Inbound)
                    return ResourceTransferStatus.InvalidRequest;
                var selectedResources = new HashSet<ResourceId>();
                foreach (var selection in intent.Selections)
                    if (selection == null || intent.Direction != ReservationDirection.Outbound || intent.Kind != ResourceClaimKind.Fresh || !EntityObservation.ReadProvider(_world, selection, null, () =>
                        _world.Content.Owns(selection) && ReferenceEquals(_world.Content.Find<Content.IResourceLotSelectionDefinition>(selection.Id), selection) &&
                        Content.ResourceLotContentContracts.ValidSelection(_world.Content, selection.Resource, selection.Lot, selection.Qualities) &&
                        intent.Quantities.AmountOf(selection.Resource).IsPositive && selectedResources.Add(selection.Resource)))
                        return ResourceTransferStatus.InvalidRequest;
                foreach (ResourceAmount line in intent.Quantities.Lines)
                    if (!ValidClaimAmount(line.Resource, line.Amount) || !demands.Add((intent.Endpoint, intent.Owner, intent.Direction, line.Resource)))
                        return ResourceTransferStatus.InvalidRequest;
            }
            // Capture every participant before any admission callback can affect a later endpoint.
            foreach (ResourceClaimIntent intent in requests)
            {
                ResourceTransferStatus access = ClaimAccess(company, intent.Endpoint, intent.Direction, endpoints, out _);
                if (access != ResourceTransferStatus.Ready) return access;
            }
            if (originals.Count != 0)
            {
                ResourceTransferStatus originalStatus = PrepareClaimReceipts(company, originals, false,
                    out _, out _, endpoints, outboundOnly: true);
                if (originalStatus != ResourceTransferStatus.Ready) return originalStatus;
            }
            foreach (ResourceClaimIntent intent in requests)
            {
                ResourceTransferStatus endpointStatus = ClaimAccess(company, intent.Endpoint, intent.Direction, endpoints, out ClaimEndpoint endpoint);
                if (endpointStatus != ResourceTransferStatus.Ready) return endpointStatus;
                foreach (ResourceAmount line in intent.Quantities.Lines)
                {
                    bool accepts = endpoint.Inlet.Accepts(_world.Content, line.Resource); CheckClaimEndpoints(endpoints);
                    if (!accepts) return ResourceTransferStatus.Incompatible;
                    CapacityQuantum quantum = CapacityPolicyContracts.Quantum(endpoint.Inlet.CapacityPolicy, line.Resource); CheckClaimEndpoints(endpoints);
                    Fix protectedAmount = intent.Kind == ResourceClaimKind.Increase ? intent.OriginalReceipt.Amount + line.Amount : line.Amount;
                    if (quantum.Floor(line.Amount) != line.Amount || quantum.Floor(protectedAmount) != protectedAmount)
                        return ResourceTransferStatus.Incompatible;
                    if (intent.Kind == ResourceClaimKind.Fresh)
                        foreach (ResourceReservation held in endpoint.Records)
                            if (held.Owner.Equals(intent.Owner) && held.Resource == line.Resource && held.Direction == intent.Direction)
                                return ResourceTransferStatus.InvalidReceipt;
                    var totals = intent.Direction == ReservationDirection.Outbound ? outgoing : incoming;
                    if (!AddClaimTotal(totals, intent.Endpoint, line.Resource, line.Amount)) return ResourceTransferStatus.InvalidRequest;
                    var selection = ClaimSelection(intent, line.Resource, endpoint);
                    if (selection != null && !(endpoint.Source is IResourceLotEndpoint)) return ResourceTransferStatus.Incompatible;
                    bool can = intent.Kind == ResourceClaimKind.Increase || (intent.Direction == ReservationDirection.Outbound
                        ? endpoint.Source.CanReserveOut(intent.Owner, line.Resource, line.Amount)
                        : endpoint.Inlet.CanReserveIn(intent.Owner, line.Resource, line.Amount));
                    CheckClaimEndpoints(endpoints);
                    if (!can) return intent.Direction == ReservationDirection.Inbound ? ResourceTransferStatus.NoCapacity :
                        endpoint.Available(line.Resource) < line.Amount ? ResourceTransferStatus.NoStock : ResourceTransferStatus.Incompatible;
                    legs.Add(new ClaimLeg(endpoint, intent.Owner, line.Resource, line.Amount,
                        intent.Kind == ResourceClaimKind.Increase ? intent.OriginalReceipt.Token : default, intent.Direction, selection));
                }
            }
            ResourceTransferStatus status = CheckClaimVectors(endpoints, outgoing, incoming);
            if (status != ResourceTransferStatus.Ready) return status;
            CheckClaimEndpoints(endpoints); legs.Sort(CompareClaims);
            // One ephemeral free vector per original endpoint admits the entire set without a reservation prefix.
            var freeLots = new Dictionary<ResourceEndpointId, ResourceLotBuffer>();
            foreach (var leg in legs) if (leg.Direction == ReservationDirection.Outbound && leg.Endpoint.Source is IResourceLotEndpoint source)
            {
                if (!freeLots.TryGetValue(leg.Endpoint.Identity, out var free))
                {
                    free = EntityObservation.ReadProvider(_world, source, null, () => ResourceLotSelection.Unclaimed(source.Lots, leg.Endpoint.Records));
                    freeLots.Add(leg.Endpoint.Identity, free);
                }
                leg.Lots = EntityObservation.ReadProvider(_world, leg.Selection ?? (object)source, null,
                    () => ResourceLotSelection.Allocate(_world.Content, free.Lots, leg.Resource, leg.Amount, leg.Selection));
                CheckClaimEndpoints(endpoints);
                if (leg.Lots == null) return ResourceTransferStatus.NoStock;
                free.Remove(leg.Lots);
            }
            return ResourceTransferStatus.Ready;
        }
        private Content.IResourceLotSelectionDefinition ClaimSelection(ResourceClaimIntent intent, ResourceId resource, ClaimEndpoint endpoint)
        {
            if (intent.Kind == ResourceClaimKind.Increase)
            {
                foreach (var record in endpoint.Records) if (record.Token.Equals(intent.OriginalReceipt.Token))
                    return record.Selection.IsNone ? null : _world.Content.Find<Content.IResourceLotSelectionDefinition>(record.Selection.Id)
                        ?? throw new InvalidOperationException("receipt lost its authored lot selection");
            }
            foreach (var selection in intent.Selections) if (selection.Resource == resource) return selection;
            return null;
        }
        private Fix EligibleAmount(IResourceLotEndpoint source, ClaimEndpoint endpoint, ResourceId resource, Content.IResourceLotSelectionDefinition selection)
            => EntityObservation.ReadProvider(_world, source, null, () => {
                return ResourceLotSelection.EligibleAmount(_world.Content, ResourceLotSelection.Unclaimed(source.Lots, endpoint.Records).Lots, resource, selection);
            });
        private static bool AddClaimTotal(Dictionary<(ResourceEndpointId, ResourceId), long> totals,
            ResourceEndpointId endpoint, ResourceId resource, Fix amount)
        {
            var key = (endpoint, resource); totals.TryGetValue(key, out long total);
            try { totals[key] = checked(total + amount.Raw); return true; }
            catch (OverflowException) { return false; }
        }
        private static ResourceTransferStatus CheckClaimVectors(List<ClaimEndpoint> endpoints,
            Dictionary<(ResourceEndpointId, ResourceId), long> outgoing, Dictionary<(ResourceEndpointId, ResourceId), long> incoming)
        {
            ResourceTransferStatus status = CheckClaimCapacity(endpoints, outgoing);
            if (status != ResourceTransferStatus.Ready) return status;
            foreach (ClaimEndpoint endpoint in endpoints)
            {
                var vector = new List<ResourceAmount>();
                foreach (var total in incoming)
                    if (total.Key.Item1 == endpoint.Identity) vector.Add(new ResourceAmount(total.Key.Item2, Fix.FromRaw(total.Value)));
                if (vector.Count == 0) continue;
                vector.Sort((left, right) => left.Resource.Value.CompareTo(right.Resource.Value));
                CapacityCheck capacity = endpoint.Inlet.CheckIncoming(vector); CheckClaimEndpoints(endpoints);
                if (!capacity.Fits) return ResourceTransferStatus.NoCapacity;
            }
            CheckClaimEndpoints(endpoints); return ResourceTransferStatus.Ready;
        }

        // Both new and growing native holds pass the same complete outbound-vector admission.
        private static ResourceTransferStatus CheckClaimCapacity(List<ClaimEndpoint> endpoints,
            Dictionary<(ResourceEndpointId, ResourceId), long> totals)
        {
            foreach (ClaimEndpoint endpoint in endpoints)
            {
                bool requested = false;
                foreach (var total in totals)
                    if (total.Key.Item1 == endpoint.Identity)
                    {
                        requested = true;
                        if (endpoint.Available(total.Key.Item2).Raw < total.Value) return ResourceTransferStatus.NoStock;
                    }
                if (!requested) continue;
                var proposed = new List<ResourceAmount>();
                foreach (var amount in endpoint.Amounts)
                {
                    totals.TryGetValue((endpoint.Identity, amount.Key), out long requestedAmount);
                    long promised = amount.Value.Raw - endpoint.Available(amount.Key).Raw;
                    long combined = checked(promised + requestedAmount);
                    if (combined > 0) proposed.Add(new ResourceAmount(amount.Key, Fix.FromRaw(combined)));
                }
                proposed.Sort((a, b) => a.Resource.Value.CompareTo(b.Resource.Value));
                CapacityCheck capacity = CapacityPolicyContracts.Check(endpoint.Source.CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), proposed);
                CheckClaimEndpoints(endpoints);
                if (!capacity.Fits) return ResourceTransferStatus.Incompatible;
            }
            CheckClaimEndpoints(endpoints); return ResourceTransferStatus.Ready;
        }

        private ResourceTransferStatus PrepareClaimReceipts(PlayerId company, IReadOnlyList<ResourceReservation> receipts, bool release,
            out List<ClaimLeg> legs, out List<ClaimEndpoint> endpoints, List<ClaimEndpoint> captured = null, bool outboundOnly = false)
        {
            legs = new List<ClaimLeg>(); endpoints = captured ?? new List<ClaimEndpoint>();
            if (!ValidClaimCompany(company) || receipts == null || receipts.Count == 0) return ResourceTransferStatus.InvalidRequest;
            var tokens = new HashSet<ReservationToken>();
            foreach (ResourceReservation receipt in new List<ResourceReservation>(receipts))
            {
                if (!receipt.Owner.IsValid || !receipt.Token.IsValid ||
                    receipt.Direction != ReservationDirection.Outbound && receipt.Direction != ReservationDirection.Inbound ||
                    outboundOnly && receipt.Direction != ReservationDirection.Outbound ||
                    !ValidClaimAmount(receipt.Resource, receipt.Amount) || !tokens.Add(receipt.Token))
                    return ResourceTransferStatus.InvalidReceipt;
                ResourceTransferStatus endpointStatus = ClaimAccess(company, receipt.Token.Endpoint, receipt.Direction, endpoints, out ClaimEndpoint endpoint);
                if (endpointStatus != ResourceTransferStatus.Ready) return endpointStatus;
                if (!ResourceReservationBook.Matches(endpoint.Records, receipt.Token.Endpoint, receipt.Owner, receipt.Token,
                    receipt.Resource, receipt.Amount, receipt.Direction)) return ResourceTransferStatus.InvalidReceipt;
                if (!release)
                {
                    ResourceReceiptCheck check = receipt.Direction == ReservationDirection.Outbound
                        ? endpoint.Source.CheckReservedOut(receipt.Owner, receipt.Token)
                        : endpoint.Inlet.CheckReservedIn(_world.Content, receipt.Owner, receipt.Token);
                    CheckClaimEndpoints(endpoints);
                    if (check.Status > ResourceTransferStatus.Ready)
                        throw new InvalidOperationException("resource provider returned an invalid batch receipt status");
                    if (!check.Ready) return check.Status;
                    if (!check.Record.Token.Equals(receipt.Token) || !check.Record.Owner.Equals(receipt.Owner) ||
                        check.Record.Resource != receipt.Resource || check.Record.Amount != receipt.Amount || check.Record.Direction != receipt.Direction)
                        throw new InvalidOperationException("resource provider fabricated its batch receipt preflight");
                }
                var actual = endpoint.Records.Find(record => record.Token.Equals(receipt.Token));
                var selection = actual.Selection.IsNone ? null : _world.Content.Get<Content.IResourceLotSelectionDefinition>(actual.Selection.Id);
                legs.Add(new ClaimLeg(endpoint, receipt.Owner, receipt.Resource, receipt.Amount, receipt.Token, receipt.Direction, selection));
            }
            CheckClaimEndpoints(endpoints); legs.Sort(CompareClaims); return ResourceTransferStatus.Ready;
        }
        private bool ValidClaimCompany(PlayerId company) => company == PlayerId.None || _world.Players.IsSeat(company);
        private bool ValidClaimAmount(ResourceId resource, Fix amount)
            => _world.Content.Resource(resource) is Content.IResourceContentDefinition definition && amount.IsPositive &&
                UnitConversion.ValidateAmount(definition.QuantityUnit, amount) == UnitConversionStatus.Converted;
        private ResourceTransferStatus ClaimAccess(PlayerId company, ResourceEndpointId identity, ReservationDirection direction,
            List<ClaimEndpoint> endpoints, out ClaimEndpoint endpoint)
        {
            endpoint = endpoints.Find(candidate => candidate.Identity == identity);
            if (endpoint != null) return endpoint.Inlet == null || direction == ReservationDirection.Outbound && endpoint.Source == null
                ? ResourceTransferStatus.Incompatible : MayUse(endpoint.Provider, company) ? ResourceTransferStatus.Ready : ResourceTransferStatus.InvalidRequest;
            IResourceEndpoint resolved = Resolve(_world, identity);
            CheckClaimEndpoints(endpoints);
            if (resolved == null) return ResourceTransferStatus.MissingEndpoint;
            if (resolved.Endpoint != identity || resolved.Version < 0)
                throw new InvalidOperationException("resource provider returned an invalid batch endpoint");
            if (!MayUse(resolved, company)) return ResourceTransferStatus.InvalidRequest;
            if (!(resolved is IResourceInlet) || direction == ReservationDirection.Outbound && !(resolved is IResourceOutlet))
                return ResourceTransferStatus.Incompatible;
            endpoint = new ClaimEndpoint(resolved, _world.Content.All<Content.IResourceContentDefinition>()); endpoints.Add(endpoint);
            return ResourceTransferStatus.Ready;
        }
        private static int CompareClaims(ClaimLeg a, ClaimLeg b)
        {
            ResourceEndpointId left = a.Endpoint.Identity, right = b.Endpoint.Identity;
            int order = string.CompareOrdinal(left.Scope, right.Scope); if (order != 0) return order;
            order = left.Identity.CompareTo(right.Identity); if (order != 0) return order;
            order = (a.Selection == null ? 1 : 0).CompareTo(b.Selection == null ? 1 : 0); if (order != 0) return order;
            order = string.CompareOrdinal(a.Owner.Scope, b.Owner.Scope); if (order != 0) return order;
            order = a.Owner.Identity.CompareTo(b.Owner.Identity); if (order != 0) return order;
            order = a.Direction.CompareTo(b.Direction); if (order != 0) return order;
            order = a.Resource.Value.CompareTo(b.Resource.Value); return order != 0 ? order : a.Token.Sequence.CompareTo(b.Token.Sequence);
        }
        private static void CheckClaimEndpoints(List<ClaimEndpoint> endpoints)
        { foreach (ClaimEndpoint endpoint in endpoints) endpoint.Check(); }
        private sealed class ClaimLeg
        {
            public readonly ClaimEndpoint Endpoint; public readonly ReservationOwner Owner; public readonly ResourceId Resource;
            public readonly Fix Amount; public readonly ReservationToken Token;
            public readonly ReservationDirection Direction;
            public readonly Content.IResourceLotSelectionDefinition Selection;
            public IReadOnlyList<ResourceLotAmount> Lots;
            public bool ContributesQuality = true;
            public ClaimLeg(ClaimEndpoint endpoint, ReservationOwner owner, ResourceId resource, Fix amount, ReservationToken token,
                ReservationDirection direction, Content.IResourceLotSelectionDefinition selection = null)
            { Endpoint = endpoint; Owner = owner; Resource = resource; Amount = amount; Token = token; Direction = direction; Selection = selection; }
        }
        /// <summary>Ephemeral exact mutation checkpoint, never another stock/promise owner.</summary>
        private sealed class ClaimEndpoint
        {
            public readonly IResourceEndpoint Provider;
            public readonly IResourceOutlet Source;
            private readonly IResourceInlet _inlet;
            public IResourceInlet Inlet => _inlet;
            private readonly ISupplier _supplier;
            private readonly ResourceSupplyKind _origin;
            private readonly ResourceEndpointId _identity; private readonly PlayerId _company;
            public ResourceEndpointId Identity => _identity;
            public int Version { get; private set; }
            public readonly Dictionary<ResourceId, Fix> Amounts = new Dictionary<ResourceId, Fix>();
            public readonly List<ResourceReservation> Records;
            public ClaimEndpoint(IResourceEndpoint provider, IReadOnlyList<Content.IResourceContentDefinition> resources)
            {
                Provider = provider; _supplier = provider as ISupplier; _origin = _supplier?.SupplyKind ?? default;
                Source = provider as IResourceOutlet ?? _supplier?.SupplyStock;
                _inlet = provider as IResourceInlet ?? Source;
                _identity = provider.Endpoint; _company = provider.Company; Version = provider.Version;
                if (!_identity.IsValid || Version < 0) throw new InvalidOperationException("invalid resource checkpoint identity/version");
                Records = new List<ResourceReservation>(CurrentRecords());
                foreach (var resource in resources)
                {
                    if (_inlet == null) continue; // Authored creation has no physical stock facade.
                    Fix amount = _inlet.Amount(resource.Resource);
                    if (amount < Fix.Zero || UnitConversion.ValidateAmount(resource.QuantityUnit, amount) != UnitConversionStatus.Converted)
                        throw new InvalidOperationException("resource provider holds invalid native batch stock");
                    Amounts.Add(resource.Resource, amount);
                }
                var tokens = new HashSet<ReservationToken>();
                foreach (ResourceReservation record in Records)
                    if (!record.Owner.IsValid || !record.Token.IsValid || record.Token.Endpoint != _identity || !tokens.Add(record.Token) ||
                        !record.Amount.IsPositive ||
                        record.Direction != ReservationDirection.Outbound && record.Direction != ReservationDirection.Inbound ||
                        UnitConversion.ValidateAmount(FindUnit(resources, record.Resource), record.Amount) != UnitConversionStatus.Converted)
                        throw new InvalidOperationException("resource provider holds an invalid batch promise");
                Check();
            }
            private static IUnitDefinition FindUnit(IReadOnlyList<Content.IResourceContentDefinition> resources, ResourceId resource)
            {
                foreach (var definition in resources) if (definition.Resource == resource) return definition.QuantityUnit;
                throw new InvalidOperationException("unknown batch promise resource");
            }
            public Fix Available(ResourceId resource)
            {
                long promised = 0;
                foreach (var record in Records)
                    if (record.Resource == resource && record.Direction == ReservationDirection.Outbound)
                        promised = checked(promised + record.Amount.Raw);
                if (promised > Amounts[resource].Raw) throw new InvalidOperationException("resource provider overpromised batch stock");
                return Fix.FromRaw(Amounts[resource].Raw - promised);
            }
            public void AdvanceVersion()
            {
                int version = Provider.Version;
                if (version <= Version) throw new InvalidOperationException("resource provider did not advance its claimed mutation version");
                Version = version;
            }
            public void Check()
            {
                CheckIdentity();
                foreach (var amount in Amounts)
                    if (_inlet.Amount(amount.Key) != amount.Value || Source != null && Source.Available(amount.Key) != Available(amount.Key))
                        throw new InvalidOperationException("resource provider changed unexpected batch stock/availability");
                CheckRecords(CurrentRecords());
                if (_supplier != null && Source != null) CheckRecords(Source.Reservations);
                CheckIdentity();
            }
            private void CheckIdentity()
            {
                if (_supplier != null && (_supplier.SupplyKind != _origin || !ReferenceEquals(_supplier.SupplyStock, Source)))
                    throw new InvalidOperationException("supplier substituted its resource origin or physical stock owner");
                if (Provider.Endpoint != _identity || Provider.Company != _company || Provider.Version != Version ||
                    _inlet != null && (_inlet.Endpoint != _identity || _inlet.Company != _company || _inlet.Version != Version))
                    throw new InvalidOperationException("resource provider changed batch endpoint identity/version");
            }
            private IReadOnlyList<ResourceReservation> CurrentRecords()
                => _supplier != null ? _supplier.Reservations : _inlet.Reservations;
            private void CheckRecords(IReadOnlyList<ResourceReservation> actual)
            {
                if (actual.Count != Records.Count) throw new InvalidOperationException("resource provider changed unrelated batch promises");
                for (int i = 0; i < actual.Count; i++)
                    if (!actual[i].Token.Equals(Records[i].Token) || !actual[i].Owner.Equals(Records[i].Owner) ||
                        actual[i].Resource != Records[i].Resource || actual[i].Amount != Records[i].Amount || actual[i].Direction != Records[i].Direction ||
                        Source is IResourceLotEndpoint && !ResourceReservation.SameLots(actual[i], Records[i]))
                        throw new InvalidOperationException("resource provider changed unrelated batch receipt");
            }
            public void Reduce(ReservationToken token, Fix amount)
            {
                int index = Records.FindIndex(record => record.Token.Equals(token));
                if (index < 0 || !amount.IsPositive || amount > Records[index].Amount)
                    throw new InvalidOperationException("invalid expected receipt reduction");
                ResourceReservation record = Records[index]; Fix remaining = record.Amount - amount;
                if (remaining == Fix.Zero) Records.RemoveAt(index);
                else Records[index] = record.Reduced(amount);
            }
        }

        private static bool MayUse(IResourceEndpoint endpoint, PlayerId company)
            => endpoint.Company == company || endpoint.Company == PlayerId.None && endpoint is IResourceInlet inlet && inlet.Public;

        private ResourceTransferCheck EvaluateTransferLeg(ResourceTransferIntent intent, List<ClaimEndpoint> endpoints,
            out ClaimEndpoint sourceCheckpoint, out ClaimEndpoint destinationCheckpoint)
        {
            // Structural/native request validation is owned only by PrepareTransfers.
            sourceCheckpoint = TransferEndpoint(intent.Source.Endpoint, intent.Kind == ResourceTransferKind.Supply, endpoints);
            destinationCheckpoint = TransferEndpoint(intent.Destination.Endpoint, false, endpoints);
            IResourceEndpoint source = sourceCheckpoint?.Provider;
            IResourceEndpoint destination = destinationCheckpoint?.Provider;
            if (source == null || destination == null) return new ResourceTransferCheck(ResourceTransferStatus.MissingEndpoint);
            if (source.Endpoint != intent.Source.Endpoint || destination.Endpoint != intent.Destination.Endpoint)
                throw new InvalidOperationException("resource provider returned a different transfer endpoint");
            if (!MayUse(destination, intent.Company) || intent.Kind == ResourceTransferKind.Stock && !MayUse(source, intent.Company))
                return new ResourceTransferCheck(ResourceTransferStatus.InvalidRequest);
            IResourceInlet inlet = destination as IResourceInlet;
            IResourceOutlet outlet = source as IResourceOutlet;
            ISupplier supplier = source as ISupplier;
            if (inlet == null || (intent.Kind == ResourceTransferKind.Supply ? supplier == null : outlet == null))
                return new ResourceTransferCheck(ResourceTransferStatus.Incompatible);
            if (intent.Kind == ResourceTransferKind.Supply && supplier != null)
            {
                ResourceTransferStatus trading = TradingAdmission(_world);
                if (trading != ResourceTransferStatus.Ready) return new ResourceTransferCheck(trading);
                if (supplier.SupplyKind == ResourceSupplyKind.ExistingStock)
                {
                    IOutletPort stock = supplier.SupplyStock;
                    if (stock == null || stock.Endpoint != source.Endpoint)
                        throw new InvalidOperationException("finite supplier must expose its existing stock owner");
                }
                else if (supplier.SupplyKind != ResourceSupplyKind.AuthoredCreation || supplier.SupplyStock != null)
                    throw new InvalidOperationException("supplier must explicitly declare its physical resource origin");
            }
            CheckClaimEndpoints(endpoints);
            int sourceVersion = sourceCheckpoint.Version, destinationVersion = destinationCheckpoint.Version;
            ResourceReceiptCheck incoming = inlet.CheckReservedIn(_world.Content, intent.Owner, intent.Destination);
            CheckClaimEndpoints(endpoints);
            ResourceReceiptCheck outgoing = intent.Kind == ResourceTransferKind.Supply
                ? supplier.CheckSupply(_world, intent.Company, intent.Owner, intent.Source, intent.Amount)
                : outlet.CheckReservedOut(intent.Owner, intent.Source);
            CheckClaimEndpoints(endpoints);
            if (incoming.Status > ResourceTransferStatus.Ready || outgoing.Status > ResourceTransferStatus.Ready)
                throw new InvalidOperationException("resource provider returned an invalid preflight status");
            ResourceTransferStatus status = !incoming.Ready ? incoming.Status : !outgoing.Ready ? outgoing.Status
                : incoming.Record.Resource != intent.Resource || outgoing.Record.Resource != intent.Resource ||
                  incoming.Record.Amount < intent.Amount || outgoing.Record.Amount < intent.Amount
                    ? ResourceTransferStatus.InvalidReceipt : ResourceTransferStatus.Ready;
            // Readiness cannot be fabricated by returning a detached record that is absent from its provider.
            if (status == ResourceTransferStatus.Ready &&
                (!ResourceReservationBook.Matches(inlet.Reservations, destination.Endpoint, intent.Owner, intent.Destination,
                    intent.Resource, incoming.Record.Amount, ReservationDirection.Inbound) ||
                 !ResourceReservationBook.Matches(intent.Kind == ResourceTransferKind.Supply ? supplier.Reservations : outlet.Reservations,
                    source.Endpoint, intent.Owner, intent.Source, intent.Resource, outgoing.Record.Amount, ReservationDirection.Outbound) ||
                 !incoming.Record.Owner.Equals(intent.Owner) || !incoming.Record.Token.Equals(intent.Destination) ||
                 incoming.Record.Direction != ReservationDirection.Inbound ||
                 !outgoing.Record.Owner.Equals(intent.Owner) || !outgoing.Record.Token.Equals(intent.Source) ||
                 outgoing.Record.Direction != ReservationDirection.Outbound))
                throw new InvalidOperationException("resource provider fabricated successful receipt preflight");
            if (status == ResourceTransferStatus.Ready && intent.Kind == ResourceTransferKind.Supply &&
                supplier.SupplyKind == ResourceSupplyKind.ExistingStock &&
                !ResourceReservationBook.Matches(supplier.SupplyStock.Reservations, source.Endpoint, intent.Owner, intent.Source,
                    intent.Resource, outgoing.Record.Amount, ReservationDirection.Outbound))
                throw new InvalidOperationException("finite supplier fabricated its physical stock receipt");
            if (status == ResourceTransferStatus.Ready)
            {
                CapacityQuantum sourceQuantum = intent.Kind == ResourceTransferKind.Supply
                    ? supplier.SupplyQuantum(intent.Resource) : CapacityPolicyContracts.Quantum(outlet.CapacityPolicy, intent.Resource);
                CapacityQuantum quantum = CapacityQuantum.Combine(sourceQuantum, CapacityPolicyContracts.Quantum(inlet.CapacityPolicy, intent.Resource));
                if (quantum.Floor(intent.Amount) != intent.Amount) status = ResourceTransferStatus.Incompatible;
            }
            CheckClaimEndpoints(endpoints);
            return new ResourceTransferCheck(status, sourceVersion, destinationVersion);
        }

        public ResourceTransferResult Commit(ResourceTransferIntent intent)
        {
            _world.RequireHealthyUpdate();
            try
            {
                using var operation = _operation.Enter();
                ResourceTransferBatchResult result = CommitTransfers(new[] { intent });
                return new ResourceTransferResult(result.Status, result.Committed ? intent.Amount : Fix.Zero);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }
    }
}
