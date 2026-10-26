using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Transient protocol assertions only. Financial policy and live state belong to the selected account.
    internal static class MonetaryBudgetExecution
    {
        internal static readonly Code AdmissionRefused = new Code("business.budget-admission-refused");
        internal static Code Blocker(MonetaryBudgetStatus status)
        {
            if (status == MonetaryBudgetStatus.NoFunds) return WorkSystem.BlockerCash;
            if (status == MonetaryBudgetStatus.RangeExceeded) return AdmissionRefused;
            throw new InvalidOperationException("actual work budget lost its valid owner/receipt: " + status);
        }
        internal static MonetaryBudgetStatus CheckReservations(World world, IMonetaryBudgets provider, PlayerId company, IReadOnlyList<MonetaryBudgetRequest> requests)
            => Check(world, provider, () => provider.EvaluateReservations(company, requests));
        internal static MonetaryBudgetStatus CheckReceipts(World world, IMonetaryBudgets provider, PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts)
            => Check(world, provider, () => provider.EvaluateReceipts(company, receipts));
        internal static MonetaryBudgetStatus CheckSpend(World world, IMonetaryBudgets provider, PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts)
            => Check(world, provider, () => provider.EvaluateSpend(company, receipts));
        private static MonetaryBudgetStatus Check(World world, IMonetaryBudgets provider, Func<MonetaryBudgetStatus> check)
        {
            Snapshot before = Capture(world, provider); MonetaryBudgetStatus result = check();
            before.Unchanged(Capture(world, provider));
            if (result < MonetaryBudgetStatus.InvalidRequest || result > MonetaryBudgetStatus.Ready)
                throw new InvalidOperationException("budget preflight returned a non-admission status");
            return result;
        }
        internal static IReadOnlyList<MonetaryBudgetReceipt> Reserve(World world, IMonetaryBudgets provider, PlayerId company, IReadOnlyList<MonetaryBudgetRequest> requests)
        {
            if (CheckReservations(world, provider, company, requests) != MonetaryBudgetStatus.Ready)
                throw new InvalidOperationException("whole work budget no longer matches its acquisition preflight");
            Snapshot before = Capture(world, provider);
            MonetaryBudgetStatus result = provider.Reserve(company, requests, out var receipts);
            Snapshot after = Capture(world, provider);
            if (result != MonetaryBudgetStatus.Committed || receipts == null || receipts.Count != requests.Count)
                throw new InvalidOperationException("budget provider did not commit its exact whole receipt set");
            var added = new Dictionary<long, MonetaryBudgetReceipt>();
            for (int i = 0; i < requests.Count; i++)
            {
                var receipt = receipts[i]; var request = requests[i];
                if (!receipt.IsValid || receipt.Company != company || !receipt.Owner.Equals(request.Owner) || receipt.Amount != request.Amount ||
                    before.Records.ContainsKey(receipt.Id) || added.ContainsKey(receipt.Id))
                    throw new InvalidOperationException("budget acquisition returned an invalid/foreign/reused receipt");
                added.Add(receipt.Id, receipt);
            }
            before.CashUnchanged(after); before.RequireRevision(after);
            if (after.Records.Count != before.Records.Count + added.Count) throw new InvalidOperationException("budget acquisition changed unrelated holds");
            foreach (var pair in before.Records) after.Require(pair.Value);
            foreach (var pair in added) after.Require(pair.Value);
            return new List<MonetaryBudgetReceipt>(receipts).AsReadOnly();
        }
        internal static void Spend(World world, IMonetaryBudgets provider, PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts, Code reason)
        {
            if (CheckSpend(world, provider, company, receipts) != MonetaryBudgetStatus.Ready)
                throw new InvalidOperationException("work budget changed before productive payment");
            Snapshot before = Capture(world, provider);
            if (provider.Spend(company, receipts, reason) != MonetaryBudgetStatus.Committed)
                throw new InvalidOperationException("budget provider violated productive payment preflight");
            Snapshot after = Capture(world, provider); before.Removed(after, receipts);
            BigInteger total = BigInteger.Zero; foreach (var receipt in receipts) total += receipt.Amount.Raw;
            for (int i = 0; i < before.Cash.Length; i++)
                if ((BigInteger)after.Cash[i].Raw != (BigInteger)before.Cash[i].Raw - (i == company.Value ? total : BigInteger.Zero))
                    throw new InvalidOperationException("budget payment did not debit only the exact receipt amount/company");
        }
        internal static void Release(World world, IMonetaryBudgets provider, PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts)
        {
            if (CheckReceipts(world, provider, company, receipts) != MonetaryBudgetStatus.Ready)
                throw new InvalidOperationException("work cannot release a missing or mismatched budget");
            Snapshot before = Capture(world, provider);
            if (provider.Release(company, receipts) != MonetaryBudgetStatus.Committed)
                throw new InvalidOperationException("budget provider failed exact receipt release");
            Snapshot after = Capture(world, provider); before.Removed(after, receipts); before.CashUnchanged(after);
        }
        internal static IReadOnlyList<MonetaryBudgetReceipt> Inspect(World world, IMonetaryBudgets provider)
            => new List<MonetaryBudgetReceipt>(Capture(world, provider).Records.Values).AsReadOnly();
        private static Snapshot Capture(World world, IMonetaryBudgets provider)
        {
            if (provider == null) throw new InvalidOperationException("missing actual budget provider");
            IEconomy account = world.Services.Get<IEconomy>(); long revision = provider.Revision;
            if (revision < 0) throw new InvalidOperationException("invalid budget owner revision");
            var cash = new Fix[world.Players.Count]; for (int i = 0; i < cash.Length; i++) cash[i] = account.Cash(new PlayerId(i));
            var records = new SortedDictionary<long, MonetaryBudgetReceipt>(); var owners = new HashSet<(PlayerId, ReservationOwner)>();
            int inspections = 0;
            provider.InspectBudgets(items =>
            {
                if (++inspections != 1 || items == null) throw new InvalidOperationException("budget inspection must report one actual receipt set");
                foreach (var receipt in items)
                {
                    if (!receipt.IsValid || receipt.Company.Value >= cash.Length || records.ContainsKey(receipt.Id) || !owners.Add((receipt.Company, receipt.Owner)))
                        throw new InvalidOperationException("budget inspection returned invalid/duplicate owner facts");
                    records.Add(receipt.Id, receipt);
                }
            });
            if (inspections != 1 || provider.Revision != revision) throw new InvalidOperationException("budget inspection mutated or omitted owner facts");
            for (int i = 0; i < cash.Length; i++) if (account.Cash(new PlayerId(i)) != cash[i]) throw new InvalidOperationException("budget inspection changed account money");
            return new Snapshot(revision, cash, records);
        }
        private sealed class Snapshot
        {
            internal readonly long Revision;
            internal readonly Fix[] Cash;
            internal readonly SortedDictionary<long, MonetaryBudgetReceipt> Records;
            internal Snapshot(long revision, Fix[] cash, SortedDictionary<long, MonetaryBudgetReceipt> records)
            { Revision = revision; Cash = cash; Records = records; }
            internal void CashUnchanged(Snapshot after)
            { for (int i = 0; i < Cash.Length; i++) if (after.Cash[i] != Cash[i]) throw new InvalidOperationException("budget operation changed unrelated account money"); }
            internal void RequireRevision(Snapshot after)
            { if (after.Revision <= Revision) throw new InvalidOperationException("budget mutation did not advance the actual owner revision"); }
            internal void Require(MonetaryBudgetReceipt receipt)
            { if (!Records.TryGetValue(receipt.Id, out var actual) || !actual.Equals(receipt)) throw new InvalidOperationException("budget operation changed its exact receipt facts"); }
            internal void Unchanged(Snapshot after)
            {
                if (after.Revision != Revision || after.Records.Count != Records.Count) throw new InvalidOperationException("budget preflight mutated owner holds");
                CashUnchanged(after); foreach (var receipt in Records.Values) after.Require(receipt);
            }
            internal void Removed(Snapshot after, IReadOnlyList<MonetaryBudgetReceipt> receipts)
            {
                RequireRevision(after); var removed = new HashSet<long>();
                foreach (var receipt in receipts) { Require(receipt); if (!removed.Add(receipt.Id) || after.Records.ContainsKey(receipt.Id)) throw new InvalidOperationException("budget commit retained/duplicated its selected hold"); }
                if (after.Records.Count != Records.Count - removed.Count) throw new InvalidOperationException("budget commit changed unrelated hold count");
                foreach (var receipt in Records.Values) if (!removed.Contains(receipt.Id)) after.Require(receipt);
            }
        }
    }
}
