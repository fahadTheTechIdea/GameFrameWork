using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>One reusable stock/promise owner embedded by an entity part or declared service endpoint.</summary>
    public sealed class ResourceInventoryState : IResourceConversionEndpoint, IResourceLotEndpoint
    {
        private readonly Func<PlayerId> _company;
        private readonly Func<int> _version;
        private readonly Action _changed;
        private readonly Func<ICapacityPolicy> _capacity;
        private readonly Func<ResourceId, bool> _accepts;
        private readonly ResolvedContent Content;
        public ResourceEndpointId Endpoint { get; }
        public PlayerId Company => _company();
        public int Version => _version();
        public ResourceInventoryState(ResourceEndpointId endpoint, Func<PlayerId> company, Func<int> version, Action changed,
            ResolvedContent content, Func<ICapacityPolicy> capacity, Func<ResourceId, bool> accepts, bool isPublic, bool evacuate)
        {
            if (!endpoint.IsValid) throw new ArgumentException("invalid inventory endpoint", nameof(endpoint));
            Endpoint = endpoint; _company = company ?? throw new ArgumentNullException(nameof(company));
            _version = version ?? throw new ArgumentNullException(nameof(version)); _changed = changed ?? throw new ArgumentNullException(nameof(changed));
            Content = content ?? throw new ArgumentNullException(nameof(content)); _capacity = capacity ?? throw new ArgumentNullException(nameof(capacity));
            _accepts = accepts ?? throw new ArgumentNullException(nameof(accepts)); Public = isPublic; Evacuate = evacuate;
        }
        private readonly ResourceLotBuffer _stock = new ResourceLotBuffer();
        private readonly ResourceReservationBook _reservations = new ResourceReservationBook();
        public IReadOnlyList<ResourceAmount> Stock => _stock.Aggregate().AsReadOnly();
        public IReadOnlyList<ResourceLotAmount> Lots => _stock.Lots;
        public IReadOnlyList<ResourceLotAmount> SelectLots(ReservationOwner owner, ReservationToken token, Fix amount)
        {
            var record = _reservations.Get(Endpoint, owner, token, ReservationDirection.Outbound);
            CheckReceiptAmount(record, amount);
            return ResourceLotSelection.Allocate(Content, record.Lots, record.Resource, amount, null)
                ?? throw new InvalidOperationException("receipt lost its actual lot partitions");
        }
        public void InitializeLot(Code lot, Fix amount)
        {
            var definition = Content.Find<IResourceLotDefinition>(lot.Id);
            if (definition == null || Reservations.Count != 0 || !ValidAmount(definition.Resource, amount) ||
                CapacityPolicyContracts.Quantum(CapacityPolicy, definition.Resource).Floor(amount) != amount)
                throw new InvalidOperationException("initial lot requires actual content and unclaimed native stock");
            _stock.Tag(new ResourceLotAmount(definition.Resource, lot, amount)); _changed();
        }
        public void InitializeLots(IReadOnlyList<ResourceLotAmount> lots)
        { foreach (var lot in lots) InitializeLot(lot.Lot, lot.Amount); }
        public Fix ReceiveLotsReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, IReadOnlyList<ResourceLotAmount> lots)
        {
            var check = CheckReservedIn(content, owner, token);
            if (!check.Ready || lots == null || lots.Count == 0) throw new InvalidOperationException("lot receipt requires actual capacity commitment");
            Fix total = Fix.Zero; string previous = null; bool first = true;
            foreach (var lot in lots)
            {
                if (lot.Resource != check.Record.Resource || !ValidLot(lot) ||
                    !first && string.CompareOrdinal(previous, lot.Lot.Id) >= 0)
                    throw new InvalidOperationException("invalid canonical lot receipt");
                first = false; previous = lot.Lot.Id; total += lot.Amount;
            }
            CheckReceiptAmount(check.Record, total);
            ReceiveReserved(content, owner, token, total);
            foreach (var lot in lots) if (!lot.Lot.IsNone) _stock.Tag(lot);
            return total;
        }
        private bool ValidLot(ResourceLotAmount lot)
        {
            if (!ValidAmount(lot.Resource, lot.Amount) || CapacityPolicyContracts.Quantum(CapacityPolicy, lot.Resource).Floor(lot.Amount) != lot.Amount) return false;
            return lot.ValidMetadata(Content);
        }
        public IReadOnlyList<ResourceAmount> InboundPromises => _reservations.Promises(ReservationDirection.Inbound);
        public IReadOnlyList<ResourceAmount> OutboundPromises => _reservations.Promises(ReservationDirection.Outbound);
        public IReadOnlyList<ResourceReservation> Reservations => _reservations.Records;
        public ICapacityPolicy CapacityPolicy => _capacity();
        public bool Public { get; }
        public bool Evacuate { get; }
        public bool Accepts(ResolvedContent content, ResourceId resource) => ReferenceEquals(content, Content) && _accepts(resource);

        public CapacityCheck CheckIncoming(IReadOnlyList<ResourceAmount> incoming)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            foreach (ResourceAmount entry in incoming)
                if (!Accepts(Content, entry.Resource)) return new CapacityCheck(CapacityStatus.ResourceRejected, entry.Resource);
            return CapacityPolicyContracts.Check(CapacityPolicy, _stock, InboundPromises, incoming);
        }

        public Fix Offer(ResourceId resource, Fix ceiling)
            => Accepts(Content, resource) ? CapacityPolicyContracts.Offer(CapacityPolicy, _stock, InboundPromises, resource, ceiling).Amount : Fix.Zero;

        public CapacityCheck CheckConversion(IReadOnlyList<ResourceAmount> consumed, IReadOnlyList<ResourceAmount> produced)
        {
            if (consumed == null || produced == null) return new CapacityCheck(CapacityStatus.InvalidRequest);
            var projected = new List<ResourceAmount>(_stock); var seen = new HashSet<ResourceId>();
            foreach (ResourceAmount line in consumed)
            {
                if (!seen.Add(line.Resource) || !ValidAmount(line.Resource, line.Amount) || !Accepts(Content, line.Resource) ||
                    CapacityPolicyContracts.Quantum(CapacityPolicy, line.Resource).Floor(line.Amount) != line.Amount)
                    return new CapacityCheck(CapacityStatus.InvalidAmount, line.Resource);
                if (Get(projected, line.Resource) < line.Amount) return new CapacityCheck(CapacityStatus.LimitExceeded, line.Resource);
                Set(projected, line.Resource, Get(projected, line.Resource) - line.Amount);
            }
            seen.Clear();
            foreach (ResourceAmount line in produced)
            {
                if (!seen.Add(line.Resource) || !ValidAmount(line.Resource, line.Amount) || !Accepts(Content, line.Resource) ||
                    CapacityPolicyContracts.Quantum(CapacityPolicy, line.Resource).Floor(line.Amount) != line.Amount)
                    return new CapacityCheck(CapacityStatus.ResourceRejected, line.Resource);
                try { _ = Get(projected, line.Resource) + line.Amount; }
                catch (OverflowException) { return new CapacityCheck(CapacityStatus.OutOfRange, line.Resource); }
            }
            return CapacityPolicyContracts.Check(CapacityPolicy, projected, InboundPromises, produced);
        }

        public void ConvertReserved(ReservationOwner owner, IReadOnlyList<ResourceConversionInput> inputs, Bill produced, IReadOnlyList<ResourceLotAmount> lots = null)
        {
            if (!owner.IsValid || inputs == null || inputs.Count == 0 || produced == null || produced.IsEmpty)
                throw new InvalidOperationException("whole coupled conversion requires explicit receipts and output");
            var seen = new HashSet<ReservationToken>(); var amounts = new List<ResourceAmount>();
            foreach (ResourceConversionInput input in inputs)
            {
                ResourceReservation receipt = input.Receipt;
                ResourceReceiptCheck check = CheckReservedOut(owner, receipt.Token);
                if (!check.Ready || !seen.Add(receipt.Token) || receipt.Token.Endpoint != Endpoint ||
                    !receipt.Owner.Equals(owner) || receipt.Direction != ReservationDirection.Outbound ||
                    check.Record.Resource != receipt.Resource || check.Record.Amount != receipt.Amount ||
                    input.Consumed < Fix.Zero || input.Consumed > receipt.Amount ||
                    input.Consumed == Fix.Zero && !input.ReleaseRemainder)
                    throw new InvalidOperationException("coupled conversion requires exact actual owned input records");
                if (input.Consumed.IsPositive) { CheckReceiptAmount(receipt, input.Consumed); amounts.Add(new ResourceAmount(receipt.Resource, input.Consumed)); }
            }
            Bill consumed = new Bill(amounts);
            if (!CheckConversion(consumed.Lines, produced.Lines).Fits)
                throw new InvalidOperationException("whole coupled conversion exceeds actual measured capacity");
            if (lots != null)
            {
                var generated = new List<ResourceAmount>();
                foreach (var lot in lots) { if (!ValidLot(lot)) throw new InvalidOperationException("invalid conversion output composition"); generated.Add(new ResourceAmount(lot.Resource, lot.Amount)); }
                var totals = new Bill(generated);
                if (totals.Count != produced.Count) throw new InvalidOperationException("conversion composition does not match output bill");
                foreach (var line in produced.Lines) if (totals.AmountOf(line.Resource) != line.Amount) throw new InvalidOperationException("conversion composition does not match output bill");
            }
            // No provider callback or externally visible prefix between these original book/stock mutations.
            foreach (ResourceConversionInput input in inputs)
            {
                ResourceReservation receipt = input.Receipt;
                if (input.Consumed.IsPositive)
                {
                    var selected = SelectLots(owner, receipt.Token, input.Consumed);
                    _reservations.Consume(Endpoint, owner, receipt.Token, ReservationDirection.Outbound, input.Consumed);
                    _stock.Remove(selected);
                }
                if (input.ReleaseRemainder && receipt.Amount > input.Consumed)
                    _reservations.Release(Endpoint, owner, receipt.Token, ReservationDirection.Outbound);
            }
            if (lots != null) foreach (var lot in lots) _stock.Add(lot);
            else foreach (ResourceAmount line in produced.Lines) _stock.Add(new ResourceLotAmount(line.Resource, Code.None, line.Amount));
            _changed();
        }

        public Fix Receive(ResolvedContent content, ResourceId resource, Fix amount)
            => Accepts(content, resource) ? Add(resource, amount) : Fix.Zero;
        public Fix Take(ResourceId resource, Fix amount) => Remove(resource, amount);
        public Fix Amount(ResourceId resource) => _stock.Amount(resource);
        public Fix ReservedIn(ResourceId resource) => _reservations.Amount(resource, ReservationDirection.Inbound);
        public Fix ReservedOut(ResourceId resource) => _reservations.Amount(resource, ReservationDirection.Outbound);
        public Fix Available(ResourceId resource) => Amount(resource) - ReservedOut(resource);

        public bool Holds(Bill bill)
        {
            foreach (ResourceAmount line in bill.Lines) if (Amount(line.Resource) < line.Amount) return false;
            return true;
        }
        public bool HoldsUnpromised(Bill bill)
        {
            foreach (ResourceAmount line in bill.Lines) if (Available(line.Resource) < line.Amount) return false;
            return true;
        }

        public Fix Add(ResourceId resource, Fix amount)
        {
            if (!ValidAmount(resource, amount) || !Accepts(Content, resource)) return Fix.Zero;
            Fix added = Offer(resource, amount);
            if (added.IsPositive) { _stock.Add(new ResourceLotAmount(resource, Code.None, added)); _changed(); }
            return added;
        }
        public Fix Remove(ResourceId resource, Fix amount)
        {
            if (!ValidAmount(resource, amount)) return Fix.Zero;
            Fix taken = CapacityPolicyContracts.Offer(CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(),
                resource, Fix.Min(Available(resource), amount)).Amount;
            if (taken.IsPositive)
            {
                var free = ResourceLotSelection.Unclaimed(Lots, Reservations);
                _stock.Remove(ResourceLotSelection.Allocate(Content, free.Lots, resource, taken, null)); _changed();
            }
            return taken;
        }

        private bool ValidAmount(ResourceId resource, Fix amount)
        {
            IResourceContentDefinition definition = Content.Resource(resource);
            return definition != null && amount.IsPositive &&
                UnitConversion.ValidateAmount(definition.QuantityUnit, amount) == UnitConversionStatus.Converted;
        }

        public bool CanReserveIn(ReservationOwner owner, ResourceId resource, Fix amount)
            => _reservations.CanReserve(owner, resource, ReservationDirection.Inbound) && ValidAmount(resource, amount)
                && CheckIncoming(new[] { new ResourceAmount(resource, amount) }).Fits;
        public ReservationToken ReserveIn(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (!CanReserveIn(owner, resource, amount)) throw new InvalidOperationException("invalid inbound capacity commitment");
            ReservationToken token = _reservations.Add(Endpoint, owner, resource, amount, ReservationDirection.Inbound);
            _changed(); return token;
        }
        public bool CanReserveOut(ReservationOwner owner, ResourceId resource, Fix amount)
            => _reservations.CanReserve(owner, resource, ReservationDirection.Outbound) && ValidAmount(resource, amount)
                && amount <= Available(resource) && CapacityPolicyContracts.Check(CapacityPolicy, Array.Empty<ResourceAmount>(),
                    Array.Empty<ResourceAmount>(), new[] { new ResourceAmount(resource, amount) }).Fits;
        public ReservationToken ReserveOut(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (!CanReserveOut(owner, resource, amount)) throw new InvalidOperationException("invalid outbound stock commitment");
            var free = ResourceLotSelection.Unclaimed(Lots, Reservations);
            return ReserveLotsOut(owner, resource, ResourceLotSelection.Allocate(Content, free.Lots, resource, amount, null), Code.None);
        }
        private IResourceLotSelectionDefinition Selection(ResourceId resource, Code code)
        {
            if (code.IsNone) return null;
            var selected = Content.Find<IResourceLotSelectionDefinition>(code.Id);
            if (selected == null || selected.Resource != resource) throw new InvalidOperationException("reservation requires actual resource selection");
            return selected;
        }
        private Fix ValidateFreeLots(ResourceId resource, IReadOnlyList<ResourceLotAmount> lots, IResourceLotSelectionDefinition selection)
        {
            if (lots == null || lots.Count == 0) throw new InvalidOperationException("reservation requires actual lot partitions");
            var free = ResourceLotSelection.Unclaimed(Lots, Reservations); Fix amount = Fix.Zero; string previous = null; bool first = true;
            foreach (var lot in lots)
            {
                if (lot.Resource != resource || !ValidLot(lot) || !ResourceLotSelection.Matches(Content, lot, selection) ||
                    !first && string.CompareOrdinal(previous, lot.Lot.Id) >= 0) throw new InvalidOperationException("reservation has invalid or ineligible lot partition");
                free.Remove(new[] { lot }); amount += lot.Amount; first = false; previous = lot.Lot.Id;
            }
            return amount;
        }
        public ReservationToken ReserveLotsOut(ReservationOwner owner, ResourceId resource, IReadOnlyList<ResourceLotAmount> lots, Code selection)
        {
            Fix amount = ValidateFreeLots(resource, lots, Selection(resource, selection));
            if (!CanReserveOut(owner, resource, amount)) throw new InvalidOperationException("invalid outbound lot commitment");
            ReservationToken token = _reservations.Add(Endpoint, owner, resource, amount, ReservationDirection.Outbound, selection, lots);
            _changed(); return token;
        }
        public void IncreaseReservedOut(ReservationOwner owner, ReservationToken token, Fix amount)
        {
            var record = _reservations.Get(Endpoint, owner, token, ReservationDirection.Outbound);
            var free = ResourceLotSelection.Unclaimed(Lots, Reservations);
            if (!ValidAmount(record.Resource, amount)) throw new InvalidOperationException("invalid receipt growth quantity");
            var lots = ResourceLotSelection.Allocate(Content, free.Lots, record.Resource, amount, Selection(record.Resource, record.Selection));
            IncreaseLotsReservedOut(owner, token, lots);
        }
        public void IncreaseLotsReservedOut(ReservationOwner owner, ReservationToken token, IReadOnlyList<ResourceLotAmount> lots)
        {
            ResourceReservation record = _reservations.Get(Endpoint, owner, token, ReservationDirection.Outbound);
            Fix amount = ValidateFreeLots(record.Resource, lots, Selection(record.Resource, record.Selection));
            Fix total = record.Amount + amount;
            if (!ValidAmount(record.Resource, amount) || amount > Available(record.Resource) ||
                CapacityPolicyContracts.Quantum(CapacityPolicy, record.Resource).Floor(total) != total)
                throw new InvalidOperationException("receipt growth exceeds actual unpromised native stock");
            _reservations.Increase(Endpoint, owner, token, amount, lots); _changed();
        }
        public void ReleaseIn(ReservationOwner owner, ReservationToken token)
        { _reservations.Release(Endpoint, owner, token, ReservationDirection.Inbound); _changed(); }
        public void ReleaseOut(ReservationOwner owner, ReservationToken token)
        { _reservations.Release(Endpoint, owner, token, ReservationDirection.Outbound); _changed(); }
        public ResourceReceiptCheck CheckReservedIn(ResolvedContent content, ReservationOwner owner, ReservationToken token)
        {
            ResourceReceiptCheck check = _reservations.Check(Endpoint, owner, token, ReservationDirection.Inbound);
            if (!check.Ready) return check;
            ResourceReservation record = check.Record;
            if (!Accepts(content, record.Resource)) return new ResourceReceiptCheck(ResourceTransferStatus.Incompatible, record);
            return CapacityPolicyContracts.Check(CapacityPolicy, _stock, _reservations.Promises(ReservationDirection.Inbound, token),
                new[] { new ResourceAmount(record.Resource, record.Amount) }).Fits
                ? check : new ResourceReceiptCheck(ResourceTransferStatus.NoCapacity, record);
        }
        public ResourceReceiptCheck CheckReservedOut(ReservationOwner owner, ReservationToken token)
        {
            ResourceReceiptCheck check = _reservations.Check(Endpoint, owner, token, ReservationDirection.Outbound);
            if (!check.Ready) return check;
            ResourceReservation record = check.Record;
            if (!Accepts(Content, record.Resource)) return new ResourceReceiptCheck(ResourceTransferStatus.Incompatible, record);
            if (record.Amount > Amount(record.Resource) || ReservedOut(record.Resource) > Amount(record.Resource))
                return new ResourceReceiptCheck(ResourceTransferStatus.NoStock, record);
            return CapacityPolicyContracts.Check(CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(),
                new[] { new ResourceAmount(record.Resource, record.Amount) }).Fits
                ? check : new ResourceReceiptCheck(ResourceTransferStatus.Incompatible, record);
        }
        public Fix ReceiveReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, Fix amount)
        {
            ResourceReceiptCheck check = CheckReservedIn(content, owner, token);
            if (!check.Ready) throw new InvalidOperationException("reserved inventory admission refused: " + check.Status);
            ResourceReservation record = check.Record;
            CheckReceiptAmount(record, amount);
            _reservations.Consume(Endpoint, owner, token, ReservationDirection.Inbound, amount);
            _stock.Add(new ResourceLotAmount(record.Resource, Code.None, amount)); _changed(); return amount;
        }
        public Fix TakeReserved(ReservationOwner owner, ReservationToken token, Fix amount)
        {
            ResourceReceiptCheck check = CheckReservedOut(owner, token);
            if (!check.Ready) throw new InvalidOperationException("reserved inventory pickup refused: " + check.Status);
            ResourceReservation record = check.Record;
            CheckReceiptAmount(record, amount);
            var selected = SelectLots(owner, token, amount);
            _reservations.Consume(Endpoint, owner, token, ReservationDirection.Outbound, amount);
            _stock.Remove(selected); _changed(); return amount;
        }

        private void CheckReceiptAmount(ResourceReservation record, Fix amount)
        {
            if (!ValidAmount(record.Resource, amount) || amount > record.Amount || CapacityPolicyContracts.Quantum(CapacityPolicy, record.Resource).Floor(amount) != amount)
                throw new InvalidOperationException("reserved quantity must be bounded and respect native endpoint quantum");
        }

        private static Fix Get(IReadOnlyList<ResourceAmount> list, ResourceId resource)
        {
            foreach (ResourceAmount line in list) if (line.Resource == resource) return line.Amount;
            return Fix.Zero;
        }
        private static void Set(List<ResourceAmount> list, ResourceId resource, Fix amount)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Resource != resource) continue;
                if (amount.IsPositive) list[i] = new ResourceAmount(resource, amount); else list.RemoveAt(i);
                return;
            }
            if (!amount.IsPositive) return;
            int at = 0; while (at < list.Count && list[at].Resource.Value < resource.Value) at++;
            list.Insert(at, new ResourceAmount(resource, amount));
        }

        public void WriteResourceState(IStateSync writer)
        { ResourceStateEncoding.RequireWriter(writer); Sync(writer); }
        public void Sync(IStateSync s)
        {
            SyncLots(s); _reservations.Sync(s, Endpoint);
            if (s.Reading)
            {
                foreach (ResourceAmount line in _stock) if (!Accepts(Content, line.Resource)) throw new CorruptStateException("incompatible saved stock");
                foreach (ResourceAmount line in InboundPromises) if (!Accepts(Content, line.Resource)) throw new CorruptStateException("incompatible saved capacity promise");
                if (!CapacityPolicyContracts.Check(CapacityPolicy, _stock, InboundPromises, Array.Empty<ResourceAmount>()).Fits)
                    throw new CorruptStateException("saved inventory exceeds its measured policy");
                foreach (ResourceReservation record in Reservations) if (!ValidAmount(record.Resource, record.Amount))
                    throw new CorruptStateException("invalid native owned reservation quantity");
                foreach (ResourceReservation record in Reservations)
                {
                    if (record.Direction != ReservationDirection.Outbound) continue;
                    IResourceLotSelectionDefinition selection = record.Selection.IsNone ? null : Content.Find<IResourceLotSelectionDefinition>(record.Selection.Id);
                    if (!record.Selection.IsNone && (selection == null || selection.Resource != record.Resource) || record.Lots.Count == 0)
                        throw new CorruptStateException("saved outbound promise lost its lot selection");
                    foreach (var lot in record.Lots) if (!ValidLot(lot) || !ResourceLotSelection.Matches(Content, lot, selection))
                        throw new CorruptStateException("saved promise holds invalid or ineligible lots");
                }
                try { ResourceLotSelection.Unclaimed(Lots, Reservations); }
                catch (InvalidOperationException error) { throw new CorruptStateException("saved lot promises exceed actual partitions: " + error.Message); }
                foreach (ResourceAmount line in OutboundPromises) if (line.Amount > Amount(line.Resource))
                    throw new CorruptStateException("saved pickup promises exceed stock");
                if (!CapacityPolicyContracts.Check(CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), OutboundPromises).Fits)
                    throw new CorruptStateException("invalid measured pickup promises");
            }
        }
        private void SyncLots(IStateSync s)
        {
            var lots = _stock.Lots; int n = s.Count(lots.Count);
            if (s.Reading) _stock.Clear();
            int previous = 0; string previousLot = null;
            for (int i = 0; i < n; i++)
            {
                var lot = ResourceLotAmount.Sync(s, s.Reading ? default : lots[i]);
                ResourceId resource = lot.Resource; string identity = lot.Lot.Id;
                if (s.Reading)
                {
                    if (resource.Value < previous || resource.Value == previous && string.CompareOrdinal(previousLot, identity) >= 0 || !ValidLot(lot))
                        throw new CorruptStateException("invalid saved lot inventory vector");
                    previous = resource.Value; previousLot = identity; _stock.Add(lot);
                }
            }
        }

    }
}

