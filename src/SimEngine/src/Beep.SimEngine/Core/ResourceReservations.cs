using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine
{
    /// <summary>Deterministic requester identity, not a security credential or cross-session handle.</summary>
    public readonly struct ReservationOwner : IEquatable<ReservationOwner>
    {
        public readonly string Scope;
        public readonly long Identity;
        public ReservationOwner(string scope, long identity) { Scope = scope; Identity = identity; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Scope) && Identity > 0;
        public bool Equals(ReservationOwner other) => Scope == other.Scope && Identity == other.Identity;
        public override bool Equals(object obj) => obj is ReservationOwner other && Equals(other);
        public override int GetHashCode() => (Scope?.GetHashCode() ?? 0) ^ Identity.GetHashCode();
    }

    public readonly struct ReservationToken : IEquatable<ReservationToken>
    {
        public readonly ResourceEndpointId Endpoint;
        public readonly long Sequence;
        public ReservationToken(ResourceEndpointId endpoint, long sequence) { Endpoint = endpoint; Sequence = sequence; }
        public bool IsValid => Endpoint.IsValid && Sequence > 0;
        public bool Equals(ReservationToken other) => Endpoint == other.Endpoint && Sequence == other.Sequence;
        public override bool Equals(object obj) => obj is ReservationToken other && Equals(other);
        public override int GetHashCode() => Endpoint.GetHashCode() ^ Sequence.GetHashCode();
        public static void Sync(IStateSync sync, ref ReservationToken token)
        {
            ResourceEndpointId endpoint = token.Endpoint; long sequence = token.Sequence;
            ResourceEndpointId.Sync(sync, ref endpoint); sync.Long(ref sequence);
            if (sequence < 0 || (sequence == 0) != !endpoint.IsValid)
                throw new CorruptStateException("invalid reservation token");
            if (sync.Reading) token = new ReservationToken(endpoint, sequence);
        }
    }

    public enum ReservationDirection : byte { Inbound = 0, Outbound = 1 }

    public readonly struct ResourceReservation
    {
        public readonly ReservationToken Token;
        public readonly ReservationOwner Owner;
        public readonly ResourceId Resource;
        /// <summary>Current outstanding native promise, not an immutable initial shipment amount.</summary>
        public readonly Fix Amount;
        public readonly ReservationDirection Direction;
        public readonly Code Selection;
        private readonly IReadOnlyList<ResourceLotAmount> _lots;
        /// <summary>Exact physical partitions held by this record; never a second stock balance.</summary>
        public IReadOnlyList<ResourceLotAmount> Lots => _lots ?? Array.Empty<ResourceLotAmount>();
        public ResourceReservation(ReservationToken token, ReservationOwner owner, ResourceId resource,
            Fix amount, ReservationDirection direction, Code selection = default, IEnumerable<ResourceLotAmount> lots = null)
        { Token = token; Owner = owner; Resource = resource; Amount = amount; Direction = direction; Selection = selection;
            _lots = lots == null ? null : new List<ResourceLotAmount>(lots).AsReadOnly(); }
        internal ResourceReservation Reduced(Fix consumed)
        {
            var rest = new List<ResourceLotAmount>(); Fix remaining = consumed;
            foreach (var lot in Lots)
            {
                Fix take = Fix.Min(remaining, lot.Amount); remaining -= take;
                if (lot.Amount > take) rest.Add(lot.WithAmount(lot.Amount - take));
            }
            return new ResourceReservation(Token, Owner, Resource, Amount - consumed, Direction, Selection, rest);
        }
        internal ResourceReservation Increased(Fix amount, IReadOnlyList<ResourceLotAmount> added)
        {
            var merged = new SortedDictionary<string, ResourceLotAmount>(StringComparer.Ordinal);
            // Null/untracked identity has a deterministic key distinct from every valid content identity.
            foreach (var rows in new[] { Lots, added ?? Array.Empty<ResourceLotAmount>() }) foreach (var lot in rows)
            {
                string key = lot.Lot.Id ?? "";
                merged.TryGetValue(key, out var previous);
                merged[key] = lot.WithAmount(previous.Amount + lot.Amount);
            }
            return new ResourceReservation(Token, Owner, Resource, Amount + amount, Direction, Selection, merged.Values);
        }
        public static bool SameLots(ResourceReservation a, ResourceReservation b)
        {
            if (a.Selection != b.Selection || a.Lots.Count != b.Lots.Count) return false;
            for (int i = 0; i < a.Lots.Count; i++) if (a.Lots[i].Resource != b.Lots[i].Resource || a.Lots[i].Lot != b.Lots[i].Lot || a.Lots[i].Amount != b.Lots[i].Amount) return false;
            return true;
        }
    }

    /// <summary>Embedded endpoint-owned records. Derived promise vectors are facts, never cached balances.</summary>
    public sealed class ResourceReservationBook
    {
        private readonly List<ResourceReservation> _records = new List<ResourceReservation>();
        private long _next = 1;
        public IReadOnlyList<ResourceReservation> Records => Array.AsReadOnly(_records.ToArray());
        public static bool Matches(IReadOnlyList<ResourceReservation> records, ResourceEndpointId endpoint, ReservationOwner owner,
            ReservationToken token, ResourceId resource, Fix amount, ReservationDirection direction)
        {
            ResourceReceiptCheck check = Inspect(records, endpoint, owner, token, direction);
            return check.Ready && check.Record.Resource == resource && check.Record.Amount == amount;
        }
        public static ResourceReceiptCheck Inspect(IReadOnlyList<ResourceReservation> records, ResourceEndpointId endpoint,
            ReservationOwner owner, ReservationToken token, ReservationDirection direction)
        {
            if (!owner.IsValid || !token.IsValid || token.Endpoint != endpoint || records == null ||
                direction != ReservationDirection.Inbound && direction != ReservationDirection.Outbound)
                return new ResourceReceiptCheck(ResourceTransferStatus.InvalidReceipt);
            foreach (ResourceReservation record in records)
                if (record.Token.Equals(token))
                    return record.Owner.Equals(owner) && record.Direction == direction
                        ? new ResourceReceiptCheck(ResourceTransferStatus.Ready, record)
                        : new ResourceReceiptCheck(ResourceTransferStatus.InvalidReceipt);
            return new ResourceReceiptCheck(ResourceTransferStatus.InvalidReceipt);
        }
        public ResourceReceiptCheck Check(ResourceEndpointId endpoint, ReservationOwner owner, ReservationToken token, ReservationDirection direction)
            => Inspect(_records, endpoint, owner, token, direction);
        public static bool ContainsToken(IReadOnlyList<ResourceReservation> records, ReservationToken token)
        {
            foreach (ResourceReservation record in records) if (record.Token.Equals(token)) return true;
            return false;
        }
        public bool CanReserve(ReservationOwner owner, ResourceId resource, ReservationDirection direction)
        {
            if (!owner.IsValid || !resource.IsValid || _next == long.MaxValue ||
                direction != ReservationDirection.Inbound && direction != ReservationDirection.Outbound) return false;
            foreach (ResourceReservation record in _records)
                if (record.Owner.Equals(owner) && record.Resource == resource && record.Direction == direction) return false;
            return true;
        }
        public ReservationToken Add(ResourceEndpointId endpoint, ReservationOwner owner, ResourceId resource,
            Fix amount, ReservationDirection direction, Code selection = default, IReadOnlyList<ResourceLotAmount> lots = null)
        {
            if (!endpoint.IsValid || !amount.IsPositive || !CanReserve(owner, resource, direction))
                throw new InvalidOperationException("invalid or duplicate owned reservation");
            var token = new ReservationToken(endpoint, _next++);
            _records.Add(new ResourceReservation(token, owner, resource, amount, direction, selection, lots));
            return token;
        }
        public ResourceReservation Get(ResourceEndpointId endpoint, ReservationOwner owner, ReservationToken token,
            ReservationDirection direction)
        {
            ResourceReceiptCheck check = Check(endpoint, owner, token, direction);
            if (!check.Ready) throw new InvalidOperationException("foreign, released, consumed or invalid reservation receipt");
            return check.Record;
        }
        public void Release(ResourceEndpointId endpoint, ReservationOwner owner, ReservationToken token, ReservationDirection direction)
        {
            Get(endpoint, owner, token, direction);
            for (int i = 0; i < _records.Count; i++) if (_records[i].Token.Equals(token)) { _records.RemoveAt(i); return; }
        }
        /// <summary>Consume only this record's bounded quantity, preserving the token and original row order.</summary>
        public Fix Consume(ResourceEndpointId endpoint, ReservationOwner owner, ReservationToken token, ReservationDirection direction, Fix amount)
        {
            ResourceReservation record = Get(endpoint, owner, token, direction);
            if (!amount.IsPositive || amount > record.Amount) throw new InvalidOperationException("invalid bounded receipt consumption");
            Fix remaining = record.Amount - amount;
            for (int i = 0; i < _records.Count; i++)
                if (_records[i].Token.Equals(token))
                {
                    if (remaining.IsZero) _records.RemoveAt(i);
                    else _records[i] = record.Reduced(amount);
                    return remaining;
                }
            throw new InvalidOperationException("owned receipt disappeared during consumption");
        }
        /// <summary>Increase one existing promise in place; endpoint owns stock/quantum admission.</summary>
        public void Increase(ResourceEndpointId endpoint, ReservationOwner owner, ReservationToken token, Fix amount, IReadOnlyList<ResourceLotAmount> lots = null)
        {
            ResourceReservation record = Get(endpoint, owner, token, ReservationDirection.Outbound);
            if (!amount.IsPositive) throw new InvalidOperationException("receipt growth requires positive native quantity");
            Fix total = record.Amount + amount;
            for (int i = 0; i < _records.Count; i++)
                if (_records[i].Token.Equals(token))
                { _records[i] = record.Increased(amount, lots); return; }
            throw new InvalidOperationException("owned receipt disappeared during growth");
        }
        public Fix Amount(ResourceId resource, ReservationDirection direction)
        {
            long raw = 0;
            foreach (ResourceReservation record in _records)
                if (record.Resource == resource && record.Direction == direction) raw = checked(raw + record.Amount.Raw);
            return Fix.FromRaw(raw);
        }
        public IReadOnlyList<ResourceAmount> Promises(ReservationDirection direction, ReservationToken omit = default)
        {
            var totals = new SortedDictionary<int, long>();
            foreach (ResourceReservation record in _records)
                if (record.Direction == direction && !record.Token.Equals(omit))
                {
                    totals.TryGetValue(record.Resource.Value, out long raw);
                    totals[record.Resource.Value] = checked(raw + record.Amount.Raw);
                }
            var result = new ResourceAmount[totals.Count]; int at = 0;
            foreach (KeyValuePair<int, long> total in totals)
                result[at++] = new ResourceAmount(new ResourceId(total.Key), Fix.FromRaw(total.Value));
            return Array.AsReadOnly(result);
        }
        public void Sync(IStateSync sync, ResourceEndpointId endpoint)
        {
            sync.Long(ref _next);
            if (_next <= 0) throw new CorruptStateException("invalid reservation sequence");
            int count = sync.Count(_records.Count); if (sync.Reading) _records.Clear();
            long previous = 0;
            for (int i = 0; i < count; i++)
            {
                ResourceReservation record = sync.Reading ? default : _records[i];
                ReservationToken token = record.Token; ReservationToken.Sync(sync, ref token);
                string scope = record.Owner.Scope; long identity = record.Owner.Identity;
                ResourceId resource = record.Resource; Fix amount = record.Amount; ReservationDirection direction = record.Direction;
                sync.String(ref scope); sync.Long(ref identity); sync.Resource(ref resource); sync.Fix(ref amount); sync.Enum(ref direction);
                string selected = record.Selection.Id; sync.String(ref selected);
                int lotCount = sync.Count(record.Lots.Count); var lots = sync.Reading ? new List<ResourceLotAmount>() : null;
                string previousLot = null; Fix lotTotal = Fix.Zero;
                for (int j = 0; j < lotCount; j++)
                {
                    var partition = ResourceLotAmount.Sync(sync, sync.Reading ? default : record.Lots[j]);
                    string lot = partition.Lot.Id; ResourceId lotResource = partition.Resource; Fix quantity = partition.Amount;
                    if (sync.Reading)
                    {
                        if (direction != ReservationDirection.Outbound || lotResource != resource || !quantity.IsPositive || j > 0 && string.CompareOrdinal(previousLot, lot) >= 0)
                            throw new CorruptStateException("invalid reserved lot partition");
                        lots.Add(partition); previousLot = lot;
                        try { lotTotal += quantity; } catch (OverflowException) { throw new CorruptStateException("reserved lot total overflow"); }
                    }
                }
                if (!sync.Reading) continue;
                if (lotCount > 0 && lotTotal != amount || selected != null && lotCount == 0) throw new CorruptStateException("reserved lot partitions disagree with receipt");
                var owner = new ReservationOwner(scope, identity);
                if (token.Endpoint != endpoint || token.Sequence <= previous || token.Sequence >= _next ||
                    !owner.IsValid || !resource.IsValid || !amount.IsPositive ||
                    direction != ReservationDirection.Inbound && direction != ReservationDirection.Outbound)
                    throw new CorruptStateException("invalid owned reservation record");
                foreach (ResourceReservation existing in _records)
                    if (existing.Owner.Equals(owner) && existing.Resource == resource && existing.Direction == direction)
                        throw new CorruptStateException("duplicate owned reservation record");
                _records.Add(new ResourceReservation(token, owner, resource, amount, direction, selected == null ? Code.None : new Code(selected), lots)); previous = token.Sequence;
            }
            if (sync.Reading)
                try { Promises(ReservationDirection.Inbound); Promises(ReservationDirection.Outbound); }
                catch (OverflowException) { throw new CorruptStateException("owned reservation quantities overflow native units"); }
        }
    }
}
