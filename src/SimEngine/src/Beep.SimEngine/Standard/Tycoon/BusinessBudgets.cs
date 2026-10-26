using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    internal sealed partial class BusinessAccounts : IMonetaryBudgets
    {
        private readonly SortedDictionary<long, MonetaryBudgetReceipt> _budgets = new SortedDictionary<long, MonetaryBudgetReceipt>();
        private long _nextBudget = 1, _budgetRevision;
        public long Revision => _budgetRevision;
        private BigInteger ReservedRaw(PlayerId company)
        {
            BigInteger amount = BigInteger.Zero;
            foreach (var receipt in _budgets.Values) if (receipt.Company == company) amount += receipt.Amount.Raw;
            return amount;
        }
        private BigInteger AvailableRaw(PlayerId company) => (BigInteger)_balances[company.Value].Raw - ReservedRaw(company);
        public MonetaryBudgetStatus EvaluateReservations(PlayerId company, IReadOnlyList<MonetaryBudgetRequest> requests)
        {
            if (!IsAccount(company) || requests == null || requests.Count == 0) return MonetaryBudgetStatus.InvalidRequest;
            var owners = new HashSet<ReservationOwner>(); BigInteger total = BigInteger.Zero;
            foreach (var request in requests)
            {
                if (!request.Owner.IsValid || !request.Amount.IsPositive || !owners.Add(request.Owner)) return MonetaryBudgetStatus.InvalidRequest;
                foreach (var receipt in _budgets.Values)
                    if (receipt.Company == company && receipt.Owner.Equals(request.Owner)) return MonetaryBudgetStatus.InvalidRequest;
                total += request.Amount.Raw;
            }
            if (total > long.MaxValue || _nextBudget > long.MaxValue - requests.Count || _budgetRevision == long.MaxValue)
                return MonetaryBudgetStatus.RangeExceeded;
            return AvailableRaw(company) >= total ? MonetaryBudgetStatus.Ready : MonetaryBudgetStatus.NoFunds;
        }
        public MonetaryBudgetStatus Reserve(PlayerId company, IReadOnlyList<MonetaryBudgetRequest> requests, out IReadOnlyList<MonetaryBudgetReceipt> receipts)
        {
            receipts = Array.Empty<MonetaryBudgetReceipt>();
            MonetaryBudgetStatus status = EvaluateReservations(company, requests);
            if (status != MonetaryBudgetStatus.Ready) return status;
            var acquired = new List<MonetaryBudgetReceipt>();
            foreach (var request in requests) acquired.Add(new MonetaryBudgetReceipt(_nextBudget++, company, request.Owner, request.Amount));
            foreach (var receipt in acquired) _budgets.Add(receipt.Id, receipt);
            _budgetRevision++; receipts = acquired.AsReadOnly(); return MonetaryBudgetStatus.Committed;
        }
        public MonetaryBudgetStatus EvaluateReceipts(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts)
        {
            if (!IsAccount(company) || receipts == null || receipts.Count == 0) return MonetaryBudgetStatus.InvalidRequest;
            var seen = new HashSet<long>();
            foreach (var receipt in receipts)
                if (!receipt.IsValid || receipt.Company != company || !seen.Add(receipt.Id) ||
                    !_budgets.TryGetValue(receipt.Id, out var actual) || !actual.Equals(receipt)) return MonetaryBudgetStatus.InvalidReceipt;
            return MonetaryBudgetStatus.Ready;
        }
        public MonetaryBudgetStatus EvaluateSpend(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts)
        {
            MonetaryBudgetStatus status = EvaluateReceipts(company, receipts); if (status != MonetaryBudgetStatus.Ready) return status;
            if (_budgetRevision == long.MaxValue) return MonetaryBudgetStatus.RangeExceeded;
            BigInteger total = BigInteger.Zero; foreach (var receipt in receipts) total += receipt.Amount.Raw;
            return _balances[company.Value].Raw >= total ? MonetaryBudgetStatus.Ready : MonetaryBudgetStatus.NoFunds;
        }
        public MonetaryBudgetStatus Spend(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts, Code reason)
        {
            MonetaryBudgetStatus status = EvaluateSpend(company, receipts); if (status != MonetaryBudgetStatus.Ready) return status;
            BigInteger total = BigInteger.Zero; foreach (var receipt in receipts) total += receipt.Amount.Raw;
            Debit(company, Fix.FromRaw((long)total), reason);
            Remove(receipts); return MonetaryBudgetStatus.Committed;
        }
        public MonetaryBudgetStatus Release(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts)
        {
            MonetaryBudgetStatus status = EvaluateReceipts(company, receipts); if (status != MonetaryBudgetStatus.Ready) return status;
            if (_budgetRevision == long.MaxValue) return MonetaryBudgetStatus.RangeExceeded;
            Remove(receipts); return MonetaryBudgetStatus.Committed;
        }
        private void Remove(IReadOnlyList<MonetaryBudgetReceipt> receipts)
        { foreach (var receipt in receipts) _budgets.Remove(receipt.Id); _budgetRevision++; }
        public void InspectBudgets(Action<IReadOnlyList<MonetaryBudgetReceipt>> inspect)
            => (inspect ?? throw new ArgumentNullException(nameof(inspect)))(new List<MonetaryBudgetReceipt>(_budgets.Values).AsReadOnly());
        private void SyncBudgets(IStateSync sync)
        {
            long next = _nextBudget, revision = _budgetRevision; sync.Long(ref next); sync.Long(ref revision);
            if (next < 1 || revision < 0 || (next == 1) != (revision == 0)) throw new CorruptStateException("invalid monetary budget identity/revision");
            var records = new List<MonetaryBudgetReceipt>(_budgets.Values); int count = sync.Count(records.Count);
            var restored = new SortedDictionary<long, MonetaryBudgetReceipt>(); var owners = new HashSet<(PlayerId, ReservationOwner)>();
            var totals = new BigInteger[_balances.Length]; long previous = 0;
            for (int i = 0; i < count; i++)
            {
                MonetaryBudgetReceipt receipt = sync.Reading ? default : records[i];
                long id = receipt.Id, identity = receipt.Owner.Identity; PlayerId company = receipt.Company; string scope = receipt.Owner.Scope; Fix amount = receipt.Amount;
                sync.Long(ref id); sync.Player(ref company); sync.String(ref scope); sync.Long(ref identity); sync.Fix(ref amount);
                receipt = new MonetaryBudgetReceipt(id, company, new ReservationOwner(scope, identity), amount);
                if (!receipt.IsValid || !IsAccount(company) || id <= previous || id >= next || !owners.Add((company, receipt.Owner)))
                    throw new CorruptStateException("invalid saved monetary hold owner/identity/amount");
                previous = id; totals[company.Value] += amount.Raw;
                if (totals[company.Value] > long.MaxValue) throw new CorruptStateException("saved monetary holds exceed native currency range");
                restored.Add(id, receipt);
            }
            if (sync.Reading) { _budgets.Clear(); foreach (var pair in restored) _budgets.Add(pair.Key, pair.Value); _nextBudget = next; _budgetRevision = revision; }
        }
    }
}
