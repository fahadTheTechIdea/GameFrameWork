using System;
using System.Collections.Generic;
using System.Numerics;

namespace Beep.SimEngine.Standard
{
    // The existing account owner also settles purchases. No purchase ledger, scheduler or saved ticket.
    internal sealed partial class BusinessAccounts : IResourcePurchaseSettlement
    {
        private IResourceTradingRules _trading;
        internal void ConfigurePurchasing(IResourceTradingRules rules)
        {
            if (_trading != null || rules == null || rules.CheckTrading() != ResourceTransferStatus.Ready)
                throw new InvalidOperationException("purchase settlement requires explicitly enabled trading");
            _trading = rules;
        }
        public ResourceTransferStatus CheckPurchases(IReadOnlyList<ResourcePurchaseIntent> purchases)
            => PreparePurchases(purchases, out _);
        public ResourceTransferStatus PreparePurchases(IReadOnlyList<ResourcePurchaseIntent> purchases, out IResourcePurchaseCommit commit)
        {
            commit = null;
            if (_trading == null) return ResourceTransferStatus.UnavailableCapability;
            ResourceTransferStatus gate = _trading.CheckTrading();
            if (gate != ResourceTransferStatus.Ready) return gate;
            if (purchases == null || purchases.Count == 0) return ResourceTransferStatus.InvalidRequest;
            Fix[] before = (Fix[])_balances.Clone(), totals = new Fix[_balances.Length];
            var spendingBefore = (SortedDictionary<string, BigInteger>[])_spending.Clone();
            long generation = _generationSequence, budgetRevision = _budgetRevision;
            try
            {
                foreach (ResourcePurchaseIntent intent in purchases)
                {
                    if (!IsAccount(intent.Company) || !intent.Quantity.IsPositive || intent.Pricing == null ||
                        !intent.Resource.IsValid || !intent.Pricing.Endpoint.IsValid)
                        return ResourceTransferStatus.InvalidRequest;
                    Fix price = intent.Pricing.UnitPrice(intent.Resource);
                    Unchanged();
                    if (price < Fix.Zero) throw new InvalidOperationException("supplier returned a negative unit price");
                    if (price == Fix.Zero) return ResourceTransferStatus.Incompatible;
                    Fix cost = price * intent.Quantity;
                    if (!cost.IsPositive) return ResourceTransferStatus.SettlementRefused;
                    totals[intent.Company.Value] += cost;
                }
                Fix[] after = (Fix[])before.Clone();
                var spendingAfter = (SortedDictionary<string, BigInteger>[])spendingBefore.Clone();
                for (int i = 0; i < totals.Length; i++)
                {
                    if (!totals[i].IsPositive) continue;
                    if (AvailableRaw(new PlayerId(i)) < totals[i].Raw) return ResourceTransferStatus.NoPayment;
                    after[i] -= totals[i];
                    spendingAfter[i] = PrepareSpending(i, totals[i], BusinessModeRules.LedgerPurchase);
                }
                commit = new PurchaseCommit(this, before, after, spendingBefore, spendingAfter, generation, budgetRevision);
                return ResourceTransferStatus.Ready;
            }
            catch (OverflowException) { Unchanged(); return ResourceTransferStatus.SettlementRefused; }

            void Unchanged()
            {
                if (_generationSequence != generation || _budgetRevision != budgetRevision) throw new InvalidOperationException("pricing mutated account admission");
                for (int i = 0; i < before.Length; i++)
                    if (_balances[i] != before[i] || !ReferenceEquals(_spending[i], spendingBefore[i])) throw new InvalidOperationException("pricing mutated account admission");
            }
        }
        private sealed class PurchaseCommit : IResourcePurchaseCommit
        {
            private readonly BusinessAccounts _owner;
            private readonly Fix[] _before, _after;
            private readonly SortedDictionary<string, BigInteger>[] _spendingBefore, _spendingAfter;
            private readonly long _generation;
            private readonly long _budgets;
            private bool _committed;
            internal PurchaseCommit(BusinessAccounts owner, Fix[] before, Fix[] after,
                SortedDictionary<string, BigInteger>[] spendingBefore, SortedDictionary<string, BigInteger>[] spendingAfter, long generation, long budgets)
            { _owner = owner; _before = before; _after = after; _spendingBefore = spendingBefore; _spendingAfter = spendingAfter; _generation = generation; _budgets = budgets; }
            public void Commit()
            {
                if (_committed || _owner._generationSequence != _generation || _owner._budgetRevision != _budgets)
                    throw new InvalidOperationException("purchase settlement is duplicate or stale");
                for (int i = 0; i < _before.Length; i++)
                    if (_owner._balances[i] != _before[i] || !ReferenceEquals(_owner._spending[i], _spendingBefore[i]))
                        throw new InvalidOperationException("purchase admission changed before commit");
                for (int i = 0; i < _after.Length; i++) { _owner._balances[i] = _after[i]; _owner._spending[i] = _spendingAfter[i]; }
                _committed = true;
            }
        }
    }
}
