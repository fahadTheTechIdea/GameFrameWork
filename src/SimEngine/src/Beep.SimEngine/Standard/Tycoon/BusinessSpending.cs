using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Detached cumulative spending in native currency raw units, not a mutable ledger.</summary>
    public readonly struct BusinessExpenseSnapshot
    {
        public Code Reason { get; }
        public BigInteger AmountRaw { get; }
        public UnitId Currency { get; }
        internal BusinessExpenseSnapshot(Code reason, BigInteger amount, UnitId currency)
        { Reason = reason; AmountRaw = amount; Currency = currency; }
    }

    // Original financial history belongs to the existing account instance and codec only.
    internal sealed partial class BusinessAccounts
    {
        // Published maps are never mutated; their identities also guard prepared purchase admission.
        private readonly SortedDictionary<string, BigInteger>[] _spending;
        private SortedDictionary<string, BigInteger> PrepareSpending(int seat, Fix amount, Code reason)
        {
            if (!amount.IsPositive) return _spending[seat];
            if (reason.IsNone) throw new ArgumentException("a debit requires its actual ledger reason", nameof(reason));
            var next = new SortedDictionary<string, BigInteger>(_spending[seat], StringComparer.Ordinal);
            next.TryGetValue(reason.Id, out var previous); next[reason.Id] = previous + amount.Raw;
            return next;
        }
        private void Debit(PlayerId company, Fix amount, Code reason)
        {
            Amount(amount); RequireAccount(company);
            Fix balance = _balances[company.Value] - amount;
            var spending = PrepareSpending(company.Value, amount, reason);
            _balances[company.Value] = balance; _spending[company.Value] = spending;
        }
        public Code CopySpending(PlayerId company, IList<BusinessExpenseSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            if (!IsAccount(company)) return Codes.InvalidArgument;
            foreach (var row in _spending[company.Value])
                destination.Add(new BusinessExpenseSnapshot(new Code(row.Key), row.Value, Currency));
            return Code.None;
        }
        private void SyncSpending(IStateSync sync)
        {
            for (int seat = 0; seat < _spending.Length; seat++)
            {
                var rows = new List<KeyValuePair<string, BigInteger>>(_spending[seat]);
                int count = sync.Count(rows.Count); string previous = null;
                var restored = sync.Reading ? new SortedDictionary<string, BigInteger>(StringComparer.Ordinal) : null;
                for (int i = 0; i < count; i++)
                {
                    string reason = sync.Reading ? null : rows[i].Key;
                    string raw = sync.Reading ? null : rows[i].Value.ToString(CultureInfo.InvariantCulture);
                    sync.String(ref reason); sync.String(ref raw);
                    if (string.IsNullOrEmpty(reason) || previous != null && StringComparer.Ordinal.Compare(previous, reason) >= 0 ||
                        !BigInteger.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) || amount <= 0 ||
                        amount.ToString(CultureInfo.InvariantCulture) != raw)
                        throw new CorruptStateException("invalid saved account spending reason/order/amount");
                    previous = reason;
                    if (sync.Reading) restored.Add(reason, amount);
                }
                if (sync.Reading) _spending[seat] = restored;
            }
        }
    }
}
