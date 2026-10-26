using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Sole transaction owner: no conversion store, codec, scheduler or compensating balance.
    public sealed partial class ResourceTransactions
    {
        private sealed class ProductionLeg
        {
            public readonly ClaimEndpoint Endpoint;
            public readonly ResourceAmount Line;
            public readonly bool Coupled;
            public ReservationToken Token;
            public ResourceLotAmount Lot;
            public ProductionLeg(ClaimEndpoint endpoint, ResourceAmount line, bool coupled) { Endpoint = endpoint; Line = line; Coupled = coupled; }
        }

        public static readonly Code StockedCreation = new Code("resources.stocked-conversion-result");
        public static Code CheckResultDefinition(EntityDef definition)
        {
            if (definition == null) return Codes.UnknownDef;
            foreach (var component in definition.Components)
                if (component is IInventoryDefinition inventory && (inventory.InitialStock == null || !inventory.InitialStock.IsEmpty))
                    return StockedCreation;
            return Code.None;
        }

        public ResourceInventoryResult EvaluateConversion(ResourceConversionIntent intent, IEntityCreation creation)
        {
            _world.RequireHealthyUpdate();
            try
            {
                var status = PrepareConversion(intent, creation, out _, out _, out _, out _, out Code blocker, out _);
                return new ResourceInventoryResult(status, blocker: blocker);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        public ResourceInventoryResult EvaluateConversionClaims(PlayerId company, ReservationOwner owner,
            IReadOnlyList<ResourceClaimIntent> inputs, IReadOnlyList<ResourceProductionIntent> outputs, Code cause)
        {
            _world.RequireHealthyUpdate();
            try
            {
                if (!ValidClaimCompany(company) || !owner.IsValid || inputs == null || outputs == null || cause.IsNone)
                    return new ResourceInventoryResult(ResourceTransferStatus.InvalidRequest);
                var identities = new HashSet<ResourceEndpointId>(); var produced = new HashSet<ResourceEndpointId>();
                foreach (ResourceClaimIntent input in inputs)
                {
                    if (!input.Owner.Equals(owner) || input.Direction != ReservationDirection.Outbound || !input.Endpoint.IsValid ||
                        input.Quantities == null || input.Quantities.IsEmpty) return new ResourceInventoryResult(ResourceTransferStatus.InvalidRequest);
                    identities.Add(input.Endpoint);
                }
                foreach (ResourceProductionIntent output in outputs)
                {
                    if (!output.Endpoint.IsValid || output.Quantities == null || output.Quantities.IsEmpty || !produced.Add(output.Endpoint))
                        return new ResourceInventoryResult(ResourceTransferStatus.InvalidRequest);
                    foreach (ResourceAmount line in output.Quantities.Lines)
                        if (!ValidClaimAmount(line.Resource, line.Amount)) return new ResourceInventoryResult(ResourceTransferStatus.InvalidRequest);
                    identities.Add(output.Endpoint);
                }
                // Capture every input/output before the first claim or capacity callback, including empty-draw steps.
                var ordered = new List<ResourceEndpointId>(identities); ordered.Sort(CompareTransferEndpoints);
                var endpoints = new List<ClaimEndpoint>();
                foreach (ResourceEndpointId identity in ordered)
                {
                    ClaimEndpoint endpoint = TransferEndpoint(identity, false, endpoints);
                    if (endpoint == null) return new ResourceInventoryResult(ResourceTransferStatus.MissingEndpoint);
                    if (!MayUse(endpoint.Provider, company)) return new ResourceInventoryResult(ResourceTransferStatus.InvalidRequest);
                }
                var consumed = new List<ClaimLeg>();
                if (inputs.Count != 0)
                {
                    ResourceTransferStatus admission = PrepareClaims(company, inputs, out consumed, out _, captured: endpoints);
                    if (admission != ResourceTransferStatus.Ready) return new ResourceInventoryResult(admission);
                }
                foreach (var leg in consumed) if (leg.Token.IsValid && leg.Lots != null)
                {
                    var held = leg.Endpoint.Records.Find(record => record.Token.Equals(leg.Token));
                    var grown = held.Increased(leg.Amount, leg.Lots);
                    leg.Lots = ResourceLotSelection.Allocate(_world.Content, grown.Lots, leg.Resource, leg.Amount, null);
                }
                ResourceTransferStatus status = PrepareConversionOutputs(owner, outputs, consumed, endpoints, out _);
                CheckClaimEndpoints(endpoints);
                if (status == ResourceTransferStatus.Ready && outputs.Count != 0 && _world.Services.Has<IResourceProductionSettlement>())
                {
                    Bill drawn, generated;
                    try
                    {
                        var inputAmounts = new List<ResourceAmount>();
                        foreach (var input in inputs) inputAmounts.AddRange(input.Quantities.Lines);
                        var outputAmounts = new List<ResourceAmount>();
                        foreach (var output in outputs) outputAmounts.AddRange(output.Quantities.Lines);
                        drawn = new Bill(inputAmounts); generated = new Bill(outputAmounts);
                    }
                    catch (OverflowException) { return new ResourceInventoryResult(ResourceTransferStatus.SettlementRefused, blocker: GenerationRange); }
                    Code refusal = PrepareGeneration(owner, company, cause, drawn, generated, false, out _);
                    CheckClaimEndpoints(endpoints);
                    if (!refusal.IsNone) return new ResourceInventoryResult(ResourceTransferStatus.SettlementRefused, blocker: refusal);
                }
                return new ResourceInventoryResult(status);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }

        private ResourceTransferStatus PrepareConversion(ResourceConversionIntent intent, IEntityCreation creation, out List<ClaimLeg> consumed,
            out List<ClaimLeg> released, out List<ProductionLeg> outputs, out List<ClaimEndpoint> endpoints, out Code blocker,
            out IResourceProductionCommit settlement, bool committing = false)
        {
            settlement = null;
            blocker = Code.None;
            consumed = new List<ClaimLeg>(); released = new List<ClaimLeg>(); outputs = new List<ProductionLeg>();
            endpoints = new List<ClaimEndpoint>();
            if (!intent.Owner.IsValid || !ValidClaimCompany(intent.Company) || intent.Cause.IsNone ||
                intent.Inputs == null || intent.Outputs == null ||
                creation == null && intent.Inputs.Count == 0 && intent.Outputs.Count == 0)
                return ResourceTransferStatus.InvalidRequest;
            var tokens = new HashSet<ReservationToken>(); var identities = new HashSet<ResourceEndpointId>();
            var receipts = new List<ResourceReservation>();
            var dispositions = new Dictionary<ReservationToken, ResourceConversionInput>();
            foreach (var input in intent.Inputs)
            {
                var record = input.Receipt;
                if (!record.Owner.Equals(intent.Owner) || !record.Token.IsValid || record.Direction != ReservationDirection.Outbound ||
                    !ResourceConversionInput.IsDispositionKind(input.Kind) ||
                    !ValidClaimAmount(record.Resource, record.Amount) || !tokens.Add(record.Token) ||
                    input.Consumed < Fix.Zero || input.Consumed > record.Amount ||
                    input.Consumed.IsPositive && !ValidClaimAmount(record.Resource, input.Consumed) ||
                    input.Consumed == Fix.Zero && !input.ReleaseRemainder)
                    return ResourceTransferStatus.InvalidReceipt;
                receipts.Add(record); dispositions.Add(record.Token, input);
            }
            foreach (ResourceProductionIntent output in intent.Outputs)
            {
                if (!output.Endpoint.IsValid || output.Quantities == null || output.Quantities.IsEmpty || !identities.Add(output.Endpoint))
                    return ResourceTransferStatus.InvalidRequest;
                foreach (ResourceAmount line in output.Quantities.Lines)
                    if (!ValidClaimAmount(line.Resource, line.Amount)) return ResourceTransferStatus.InvalidRequest;
            }
            // Resolve/capture EVERY participant before the first pure receipt/capacity callback.
            var all = new HashSet<ResourceEndpointId>(identities);
            foreach (var record in receipts) all.Add(record.Token.Endpoint);
            var ordered = new List<ResourceEndpointId>(all); ordered.Sort(CompareTransferEndpoints);
            foreach (ResourceEndpointId identity in ordered)
            {
                ClaimEndpoint endpoint = TransferEndpoint(identity, false, endpoints);
                if (endpoint == null) return ResourceTransferStatus.MissingEndpoint;
                if (!MayUse(endpoint.Provider, intent.Company)) return ResourceTransferStatus.InvalidRequest;
            }
            if (creation != null)
            {
                blocker = _world.CheckCreation(creation);
                CheckClaimEndpoints(endpoints);
                if (!blocker.IsNone) return ResourceTransferStatus.InvalidRequest;
                if (creation.Owner != intent.Company) { blocker = Codes.NotOwned; return ResourceTransferStatus.InvalidRequest; }
                blocker = CheckResultDefinition(creation.Definition);
                if (!blocker.IsNone) return ResourceTransferStatus.InvalidRequest;
            }
            ResourceTransferStatus status;
            if (receipts.Count > 0)
            {
                // Validate original complete records, never fabricate reduced receipt authority.
                status = PrepareClaimReceipts(intent.Company, receipts, false, out var held, out _, endpoints, outboundOnly: true);
                if (status != ResourceTransferStatus.Ready) return status;
                foreach (var leg in held)
                {
                    var input = dispositions[leg.Token];
                    CapacityQuantum quantum = CapacityPolicyContracts.Quantum(leg.Endpoint.Source.CapacityPolicy, leg.Resource); CheckClaimEndpoints(endpoints);
                    if (quantum.Floor(input.Consumed) != input.Consumed) return ResourceTransferStatus.Incompatible;
                    if (input.Consumed.IsPositive)
                    {
                        var consumedLeg = new ClaimLeg(leg.Endpoint, leg.Owner, leg.Resource, input.Consumed, leg.Token, leg.Direction, leg.Selection);
                        consumedLeg.ContributesQuality = input.Kind == ResourceFlowKind.Consumed;
                        if (leg.Endpoint.Source is IResourceLotEndpoint lotSource)
                        {
                            consumedLeg.Lots = EntityObservation.ReadProvider(_world, lotSource, null,
                                () => lotSource.SelectLots(leg.Owner, leg.Token, input.Consumed), observeTargetState: true);
                            CheckClaimEndpoints(endpoints);
                        }
                        consumed.Add(consumedLeg);
                    }
                    Fix remaining = leg.Amount - input.Consumed;
                    if (input.ReleaseRemainder && remaining.IsPositive)
                        released.Add(new ClaimLeg(leg.Endpoint, leg.Owner, leg.Resource, remaining, leg.Token, leg.Direction));
                }
            }
            status = PrepareConversionOutputs(intent.Owner, intent.Outputs, consumed, endpoints, out outputs);
            if (status != ResourceTransferStatus.Ready) return status;
            CheckClaimEndpoints(endpoints);
            blocker = PrepareConversionGeneration(intent, committing, out settlement);
            CheckClaimEndpoints(endpoints);
            return blocker.IsNone ? ResourceTransferStatus.Ready : ResourceTransferStatus.SettlementRefused;
        }

        private ResourceTransferStatus PrepareConversionOutputs(ReservationOwner owner, IReadOnlyList<ResourceProductionIntent> requested,
            List<ClaimLeg> consumed, List<ClaimEndpoint> endpoints, out List<ProductionLeg> outputs)
        {
            outputs = new List<ProductionLeg>();
            var production = new List<ResourceProductionIntent>(requested);
            production.Sort((left, right) => CompareTransferEndpoints(left.Endpoint, right.Endpoint));
            foreach (ResourceProductionIntent output in production)
            {
                ClaimEndpoint endpoint = endpoints.Find(candidate => candidate.Identity == output.Endpoint);
                IResourceInlet inlet = endpoint.Inlet;
                if (inlet == null) return ResourceTransferStatus.Incompatible;
                var draw = new List<ResourceAmount>();
                foreach (ClaimLeg input in consumed)
                    if (ReferenceEquals(input.Endpoint, endpoint)) draw.Add(new ResourceAmount(input.Resource, input.Amount));
                bool coupled = draw.Count != 0;
                var converter = inlet as IResourceConversionEndpoint;
                if (coupled && converter == null) return ResourceTransferStatus.Incompatible;
                var axes = new HashSet<string>(StringComparer.Ordinal);
                var values = new Dictionary<ResourceId, SortedDictionary<string, Fix>>();
                var lots = new List<ResourceLotAmount>();
                foreach (var input in consumed)
                {
                    if (!input.ContributesQuality) continue;
                    if (input.Lots != null) lots.AddRange(input.Lots);
                    else lots.Add(new ResourceLotAmount(input.Resource, Code.None, input.Amount));
                }
                foreach (var rule in output.OutputQualities)
                {
                    var result = EntityObservation.ReadProvider(_world, rule ?? (object)this, null, () => {
                        if (!ResourceOutputQualityDeclaration.Valid(_world.Content, rule))
                            return (Status: ResourceTransferStatus.InvalidRequest, Resource: ResourceId.None, Axis: (string)null, Value: Fix.Zero);
                        bool ready = ResourceOutputQualityDeclaration.TryEvaluate(_world.Content, rule, lots, out Fix value);
                        return (Status: ready ? ResourceTransferStatus.Ready : ResourceTransferStatus.Incompatible, Resource: rule.Quality.Resource, Axis: rule.Quality.Id, Value: value);
                    }, observeTargetState: true);
                    CheckClaimEndpoints(endpoints);
                    if (result.Status != ResourceTransferStatus.Ready) return result.Status;
                    if (!axes.Add(result.Axis) || !output.Quantities.AmountOf(result.Resource).IsPositive) return ResourceTransferStatus.InvalidRequest;
                    if (!(inlet is IResourceLotEndpoint)) return ResourceTransferStatus.Incompatible;
                    if (!values.TryGetValue(result.Resource, out var vector)) values.Add(result.Resource, vector = new SortedDictionary<string, Fix>(StringComparer.Ordinal));
                    vector.Add(result.Axis, result.Value);
                }
                foreach (ResourceAmount line in output.Quantities.Lines)
                {
                    bool accepts = inlet.Accepts(_world.Content, line.Resource); CheckClaimEndpoints(endpoints);
                    if (!accepts) return ResourceTransferStatus.Incompatible;
                    CapacityQuantum quantum = CapacityPolicyContracts.Quantum(inlet.CapacityPolicy, line.Resource); CheckClaimEndpoints(endpoints);
                    if (quantum.Floor(line.Amount) != line.Amount) return ResourceTransferStatus.Incompatible;
                    foreach (ResourceReservation record in endpoint.Records)
                        if (!coupled && record.Owner.Equals(owner) && record.Resource == line.Resource && record.Direction == ReservationDirection.Inbound)
                            return ResourceTransferStatus.InvalidReceipt;
                }
                CapacityCheck capacity = EntityObservation.ReadProvider(_world, inlet, null, () =>
                {
                    CapacityCheck value = coupled ? converter.CheckConversion(new Bill(draw).Lines, output.Quantities.Lines)
                        : inlet.CheckIncoming(output.Quantities.Lines);
                    if (!Enum.IsDefined(typeof(CapacityStatus), value.Status))
                        throw new InvalidOperationException("conversion capacity provider returned an unknown status");
                    return value;
                }, observeTargetState: true);
                CheckClaimEndpoints(endpoints);
                if (!capacity.Fits) return ResourceTransferStatus.NoCapacity;
                foreach (ResourceAmount line in output.Quantities.Lines)
                {
                    bool can = coupled || inlet.CanReserveIn(owner, line.Resource, line.Amount); CheckClaimEndpoints(endpoints);
                    if (!can) return ResourceTransferStatus.NoCapacity;
                    outputs.Add(new ProductionLeg(endpoint, line, coupled) { Lot = values.TryGetValue(line.Resource, out var vector)
                        ? ResourceLotAmount.Produced(line.Resource, line.Amount, vector, ProductionIdentity(owner, endpoint.Identity, consumed)) : new ResourceLotAmount(line.Resource, Code.None, line.Amount) });
                }
            }
            CheckClaimEndpoints(endpoints);
            return ResourceTransferStatus.Ready;
        }

        private static string ProductionIdentity(ReservationOwner owner, ResourceEndpointId output, List<ClaimLeg> consumed)
        {
            var identity = new StringBuilder();
            void Text(string value) => identity.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('|');
            void Number(long value) => Text(value.ToString(CultureInfo.InvariantCulture));
            Text(owner.Scope); Number(owner.Identity); Text(output.Scope); Number(output.Identity);
            var ordered = new List<ClaimLeg>(consumed);
            ordered.Sort((a, b) => { int endpoint = CompareTransferEndpoints(a.Endpoint.Identity, b.Endpoint.Identity); if (endpoint != 0) return endpoint;
                int token = a.Token.Sequence.CompareTo(b.Token.Sequence); return token != 0 ? token : a.Resource.CompareTo(b.Resource); });
            foreach (var input in ordered)
            {
                Text(input.Endpoint.Identity.Scope); Number(input.Endpoint.Identity.Identity); Number(input.Token.Sequence); Number(input.Resource.Value); Number(input.Amount.Raw);
                Fix outstanding = Fix.Zero; foreach (var record in input.Endpoint.Records) if (record.Token.Equals(input.Token)) { outstanding = record.Amount; break; }
                Number(outstanding.Raw);
            }
            return identity.ToString();
        }

        public ResourceInventoryResult CommitConversion(ResourceConversionIntent intent, IEntityCreation creation)
        {
            _world.RequireHealthyUpdate();
            try
            {
                using var operation = _operation.Enter();
                ResourceTransferStatus status = PrepareConversion(intent, creation, out List<ClaimLeg> consumed, out List<ClaimLeg> released,
                    out List<ProductionLeg> outputs, out List<ClaimEndpoint> endpoints, out Code blocker, out IResourceProductionCommit settlement, committing: true);
                if (status != ResourceTransferStatus.Ready) return new ResourceInventoryResult(status, blocker: blocker);
                var expectedLots = new Dictionary<ResourceEndpointId, ResourceLotBuffer>();
                foreach (var endpoint in endpoints) if (endpoint.Inlet is IResourceLotEndpoint source)
                {
                    var buffer = EntityObservation.ReadProvider(_world, source, null, () => {
                        var projected = new ResourceLotBuffer(); foreach (var lot in source.Lots) projected.Add(lot); return projected;
                    }, observeTargetState: true);
                    expectedLots.Add(endpoint.Identity, buffer);
                }
                foreach (var input in consumed) if (expectedLots.TryGetValue(input.Endpoint.Identity, out var buffer))
                {
                    if (input.Lots == null) throw new InvalidOperationException("conversion lost actual consumed lot allocation");
                    buffer.Remove(input.Lots);
                }
                foreach (var output in outputs) if (expectedLots.TryGetValue(output.Endpoint.Identity, out var buffer)) buffer.Add(output.Lot);
                CheckClaimEndpoints(endpoints);
                var facts = new List<ResourceFlowFact>();
                var inputKinds = new Dictionary<ReservationToken, ResourceFlowKind>();
                foreach (ResourceConversionInput input in intent.Inputs) inputKinds.Add(input.Receipt.Token, input.Kind);
                foreach (ProductionLeg leg in outputs)
                {
                    if (leg.Coupled) continue;
                    ClaimEndpoint endpoint = leg.Endpoint;
                    leg.Token = endpoint.Inlet.ReserveIn(intent.Owner, leg.Line.Resource, leg.Line.Amount);
                    if (!leg.Token.IsValid || leg.Token.Endpoint != endpoint.Identity || ResourceReservationBook.ContainsToken(endpoint.Records, leg.Token))
                        throw new InvalidOperationException("conversion output returned an invalid capacity receipt");
                    endpoint.Records.Add(new ResourceReservation(leg.Token, intent.Owner, leg.Line.Resource, leg.Line.Amount, ReservationDirection.Inbound));
                    endpoint.AdvanceVersion(); CheckClaimEndpoints(endpoints);
                }
                // Newly acquired receipts must be actual and usable before any irreversible consumption.
                foreach (ProductionLeg leg in outputs)
                {
                    if (leg.Coupled) continue;
                    ResourceReceiptCheck check = leg.Endpoint.Inlet.CheckReservedIn(_world.Content, intent.Owner, leg.Token);
                    CheckClaimEndpoints(endpoints);
                    if (!check.Ready || !check.Record.Token.Equals(leg.Token) || !check.Record.Owner.Equals(intent.Owner) ||
                        check.Record.Resource != leg.Line.Resource || check.Record.Amount != leg.Line.Amount || check.Record.Direction != ReservationDirection.Inbound)
                        throw new InvalidOperationException("conversion output violated complete capacity admission");
                }
                var coupled = new HashSet<ResourceEndpointId>();
                foreach (ProductionLeg leg in outputs) if (leg.Coupled) coupled.Add(leg.Endpoint.Identity);
                foreach (ClaimEndpoint endpoint in endpoints)
                {
                    if (!coupled.Contains(endpoint.Identity)) continue;
                    var actualInputs = new List<ResourceConversionInput>(); var amounts = new List<ResourceAmount>(); var producedLots = new List<ResourceLotAmount>();
                    foreach (ResourceConversionInput input in intent.Inputs)
                        if (input.Receipt.Token.Endpoint == endpoint.Identity) actualInputs.Add(input);
                    foreach (ProductionLeg output in outputs)
                        if (ReferenceEquals(output.Endpoint, endpoint)) { amounts.Add(output.Line); producedLots.Add(output.Lot); }
                    int beforeVersion = endpoint.Version;
                    var envelope = EntityObservation.Capture(_world);
                    ((IResourceConversionEndpoint)endpoint.Inlet).ConvertReserved(intent.Owner, actualInputs.AsReadOnly(), new Bill(amounts), producedLots.AsReadOnly());
                    envelope.Verify(); _world.RequireHealthyUpdate();
                    endpoint.AdvanceVersion();
                    // Expected deltas describe the ordered internal consume/produce steps of one atomic stock mutation.
                    // No callback or success event observes/publishes that partial prefix.
                    foreach (ClaimLeg input in consumed)
                    {
                        if (!ReferenceEquals(input.Endpoint, endpoint)) continue;
                        Fix before = endpoint.Amounts[input.Resource];
                        endpoint.Amounts[input.Resource] = before - input.Amount; endpoint.Reduce(input.Token, input.Amount);
                        facts.Add(new ResourceFlowFact(_world.Tick, intent.Company,
                            new ResourceReservation(input.Token, input.Owner, input.Resource, input.Amount, ReservationDirection.Outbound), intent.Cause,
                            _world.Content.Resource(input.Resource).QuantityUnit.Unit, before, endpoint.Amounts[input.Resource], beforeVersion, endpoint.Version, inputKinds[input.Token])
                            .WithLots(input.Lots, input.Selection == null ? Code.None : new Code(input.Selection.Id)));
                    }
                    foreach (ClaimLeg input in released)
                        if (ReferenceEquals(input.Endpoint, endpoint)) endpoint.Reduce(input.Token, input.Amount);
                    foreach (ProductionLeg output in outputs)
                    {
                        if (!ReferenceEquals(output.Endpoint, endpoint)) continue;
                        Fix before = endpoint.Amounts[output.Line.Resource]; endpoint.Amounts[output.Line.Resource] = before + output.Line.Amount;
                        facts.Add(new ResourceFlowFact(_world.Tick, intent.Owner, intent.Company, endpoint.Identity, output.Line, intent.Cause,
                            _world.Content.Resource(output.Line.Resource).QuantityUnit.Unit, before, endpoint.Amounts[output.Line.Resource], beforeVersion, endpoint.Version)
                            .WithLots(endpoint.Inlet is IResourceLotEndpoint ? new[] { output.Lot } : null));
                    }
                    CheckClaimEndpoints(endpoints);
                }
                foreach (ClaimLeg leg in consumed)
                {
                    if (coupled.Contains(leg.Endpoint.Identity)) continue;
                    ClaimEndpoint endpoint = leg.Endpoint; Fix before = endpoint.Amounts[leg.Resource]; int version = endpoint.Version;
                    Fix taken = endpoint.Source.TakeReserved(leg.Owner, leg.Token, leg.Amount);
                    if (taken != leg.Amount) throw new InvalidOperationException("conversion input violated exact consumption");
                    endpoint.Amounts[leg.Resource] = before - leg.Amount; endpoint.Reduce(leg.Token, leg.Amount);
                    endpoint.AdvanceVersion(); CheckClaimEndpoints(endpoints);
                    facts.Add(new ResourceFlowFact(_world.Tick, intent.Company,
                        new ResourceReservation(leg.Token, leg.Owner, leg.Resource, leg.Amount, ReservationDirection.Outbound), intent.Cause,
                        _world.Content.Resource(leg.Resource).QuantityUnit.Unit, before, endpoint.Amounts[leg.Resource], version, endpoint.Version, inputKinds[leg.Token])
                        .WithLots(leg.Lots, leg.Selection == null ? Code.None : new Code(leg.Selection.Id)));
                }
                foreach (ClaimLeg leg in released)
                {
                    if (coupled.Contains(leg.Endpoint.Identity)) continue;
                    leg.Endpoint.Source.ReleaseOut(leg.Owner, leg.Token); leg.Endpoint.Reduce(leg.Token, leg.Amount);
                    leg.Endpoint.AdvanceVersion(); CheckClaimEndpoints(endpoints);
                }
                foreach (ProductionLeg leg in outputs)
                {
                    if (leg.Coupled) continue;
                    ClaimEndpoint endpoint = leg.Endpoint; Fix before = endpoint.Amounts[leg.Line.Resource]; int version = endpoint.Version;
                    Fix made = leg.Lot.Lot.IsNone ? endpoint.Inlet.ReceiveReserved(_world.Content, intent.Owner, leg.Token, leg.Line.Amount)
                        : ((IResourceLotEndpoint)endpoint.Inlet).ReceiveLotsReserved(_world.Content, intent.Owner, leg.Token, new[] { leg.Lot });
                    if (made != leg.Line.Amount) throw new InvalidOperationException("conversion output violated exact production");
                    endpoint.Amounts[leg.Line.Resource] = before + leg.Line.Amount; endpoint.Reduce(leg.Token, leg.Line.Amount);
                    endpoint.AdvanceVersion(); CheckClaimEndpoints(endpoints);
                    facts.Add(new ResourceFlowFact(_world.Tick, intent.Owner, intent.Company,
                        endpoint.Identity, leg.Line, intent.Cause,
                        _world.Content.Resource(leg.Line.Resource).QuantityUnit.Unit, before, endpoint.Amounts[leg.Line.Resource], version, endpoint.Version)
                        .WithLots(endpoint.Inlet is IResourceLotEndpoint ? new[] { leg.Lot } : null));
                }
                CheckClaimEndpoints(endpoints);
                EntityId created = EntityId.None;
                if (creation != null)
                {
                    // Resource failure can never create a result. Initialization must also preserve all
                    // participating endpoint state before the deferred conversion history is published.
                    created = _world.CommitCreation(creation).Id;
                    CheckClaimEndpoints(endpoints);
                }
                // Every physical output/result is verified before money changes. Event observers see the complete commit.
                foreach (var endpoint in endpoints) if (expectedLots.TryGetValue(endpoint.Identity, out var expected))
                    EntityObservation.ReadProvider(_world, endpoint.Inlet, null, () => {
                        var actual = ((IResourceLotEndpoint)endpoint.Inlet).Lots; var projected = expected.Lots;
                        if (actual.Count != projected.Count) throw new InvalidOperationException("conversion provider changed output lot partitions");
                        for (int i = 0; i < actual.Count; i++)
                            if (actual[i].Resource != projected[i].Resource || actual[i].Lot != projected[i].Lot || actual[i].Amount != projected[i].Amount ||
                                !actual[i].ValidMetadata(_world.Content)) throw new InvalidOperationException("conversion provider lost actual output composition");
                        return true;
                    }, observeTargetState: true);
                settlement?.Commit();
                CheckClaimEndpoints(endpoints);
                foreach (ResourceFlowFact fact in facts) _world.Events.Publish(fact);
                return new ResourceInventoryResult(ResourceTransferStatus.Committed, checked(consumed.Count + outputs.Count), created);
            }
            catch (Exception error) { LatchTransactionFault(error); throw; }
        }
    }
}
