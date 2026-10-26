using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Named financial-contract probes; existing engine owners perform all accounting and persistence.
    internal static class BusinessSpendingTests
    {
        internal const string Case = "Tycoon spending: original reason totals sole owner saved privacy and prepared settlement";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static List<BusinessExpenseSnapshot> Read(Check c, Hosting.SimGame game, int seat = 0)
        {
            var rows = new List<BusinessExpenseSnapshot> { default }; ulong before = game.Hash();
            c.Equal(Code.None, game.Seat(seat).View.Feature<BusinessView>().CopySpending(new PlayerId(seat), rows), "actual private spending snapshot");
            c.Equal(before, game.Hash(), "spending read is pure");
            c.That(rows.Select(row => row.Reason.Id).SequenceEqual(rows.Select(row => row.Reason.Id).OrderBy(id => id, StringComparer.Ordinal)), "canonical ordinal reasons");
            return rows;
        }
        private static void Run(Check c)
        {
            var factory = PurchasingTests.Factory(); var game = PurchasingTests.New(factory); var idle = PurchasingTests.New(factory);
            ulong isolated = idle.Hash(); var company = new PlayerId(0); var other = new PlayerId(1);
            var accounts = game.World.Services.Get<IBusinessAccounts>(); var money = game.World.Services.Get<IEconomy>();
            c.That(ReferenceEquals(accounts, money), "history and balance use the original account owner");
            c.Equal(0, Read(c, game).Count, "opening cash is not an expense");
            c.That(money.TryCharge(company, Fix.FromInt(3), new Code("proof.alpha")), "actual successful debit");
            money.Charge(company, Fix.FromInt(4), new Code("proof.beta"));
            var retained = Read(c, game);
            c.Equal((BigInteger)Fix.FromInt(3).Raw, retained.Single(row => row.Reason.Id == "proof.alpha").AmountRaw, "first reason exact");
            c.Equal((BigInteger)Fix.FromInt(4).Raw, retained.Single(row => row.Reason.Id == "proof.beta").AmountRaw, "second reason exact");
            c.That(retained.All(row => row.Currency == accounts.Currency), "actual authored currency on every detached expense");
            ulong before = game.Hash();
            c.That(!money.TryCharge(company, Fix.FromInt(1000), new Code("proof.refused")), "unaffordable debit refused");
            c.That(money.TryCharge(company, Fix.Zero, Code.None), "zero debit creates no invented category");
            c.Equal(before, game.Hash(), "refused and zero debit have no expense or balance mutation");
            money.Credit(company, Fix.FromInt(7), new Code("proof.credit"));
            c.Equal(2, Read(c, game).Count, "credits neither erase expenses nor become expenses");
            var hidden = new List<BusinessExpenseSnapshot>(retained);
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<BusinessView>().CopySpending(company, hidden), "foreign private history refused");
            c.Equal(0, hidden.Count, "foreign refusal clears retained caller data");
            hidden.Add(default);
            c.Equal(Codes.InvalidArgument, game.Observer.Feature<BusinessView>().CopySpending(new PlayerId(99), hidden), "nonexistent account refused");
            c.Equal(0, hidden.Count, "missing account clears caller data");

            var budgets = game.World.Services.Get<IMonetaryBudgets>();
            var request = new[] { new MonetaryBudgetRequest(new ReservationOwner("proof.expense", 1), Fix.FromInt(12)) };
            c.Equal(MonetaryBudgetStatus.Committed, budgets.Reserve(company, request, out var hold), "reserve real account budget");
            c.Equal(2, Read(c, game).Count, "holding money is not spending");
            c.Equal(MonetaryBudgetStatus.Committed, budgets.Release(company, hold), "release actual hold");
            c.Equal(2, Read(c, game).Count, "released holds are not expenses or refunds");
            c.Equal(MonetaryBudgetStatus.Committed, budgets.Reserve(company, request, out hold), "reacquire original budget owner");
            c.Equal(MonetaryBudgetStatus.Committed, budgets.Spend(company, hold, new Code("ledger.work")), "actual budget spend");
            c.Equal((BigInteger)Fix.FromInt(12).Raw, Read(c, game).Single(row => row.Reason.Id == "ledger.work").AmountRaw, "budget spend records exactly once");
            before = game.Hash();
            c.Equal(MonetaryBudgetStatus.InvalidReceipt, budgets.Spend(company, hold, new Code("ledger.work")), "spent receipt cannot spend twice");
            c.Equal(before, game.Hash(), "duplicate budget debit leaves financial history unchanged");

            var settlement = game.World.Services.Get<IResourcePurchaseSettlement>();
            var vendor = game.World.Entities.All.Single(entity => entity.Def.Id == "vendor-a");
            var intent = new[] { new ResourcePurchaseIntent(company, game.World.Content.ResourceId("kit"), Fix.FromInt(2), (IResourceSupplyPrice)vendor.Get<ISupplier>()) };
            before = game.Hash();
            c.Equal(ResourceTransferStatus.Ready, settlement.PreparePurchases(intent, out var purchase), "prepare actual native purchase participant");
            c.Equal(before, game.Hash(), "prepared purchase does not publish expenses");
            purchase.Commit();
            c.Equal((BigInteger)Fix.FromInt(6).Raw, Read(c, game).Single(row => row.Reason == BusinessModeRules.LedgerPurchase).AmountRaw, "purchase records actual quantity times native price");
            before = game.Hash(); c.Throws<InvalidOperationException>(() => purchase.Commit(), "duplicate purchase commit refused");
            c.Equal(before, game.Hash(), "duplicate purchase has no expense prefix");
            c.Equal(ResourceTransferStatus.Ready, settlement.PreparePurchases(intent, out var stale), "prepare a second transient purchase");
            Fix balance = money.Cash(other);
            money.Charge(other, Fix.One, new Code("proof.intervening")); money.Credit(other, Fix.One, new Code("proof.credit"));
            c.Equal(balance, money.Cash(other), "intervening debit and credit restore balance but not real spending history");
            before = game.Hash(); c.Throws<InvalidOperationException>(() => stale.Commit(), "net-zero balance mutation cannot replace prepared spending state");
            c.Equal(before, game.Hash(), "stale purchase preserves both accounts' actual history");

            money.Charge(company, Fix.MaxValue, new Code("proof.large"));
            money.Credit(company, Fix.MaxValue, new Code("proof.credit"));
            money.Charge(company, Fix.MaxValue, new Code("proof.large"));
            c.Equal((BigInteger)Fix.MaxValue.Raw * 2, Read(c, game).Single(row => row.Reason.Id == "proof.large").AmountRaw,
                "cumulative spending remains exact beyond a single balance's range");
            c.Equal((BigInteger)Fix.FromInt(3).Raw, retained.Single(row => row.Reason.Id == "proof.alpha").AmountRaw, "retained rows remain immutable");
            using (var payload = new MemoryStream())
            {
                var writer = new StateWriter(payload); ((IPersistentService)accounts).Sync(writer); writer.Flush();
                byte[] saved = payload.ToArray(); string raw = Fix.FromInt(3).Raw.ToString(System.Globalization.CultureInfo.InvariantCulture);
                foreach (var edit in new[] { ("proof.alpha", "proof.large"), (raw, new string('0', raw.Length)),
                    (raw, "0" + raw.Substring(1)), (raw, "-" + raw.Substring(1)) })
                {
                    byte[] bad = (byte[])saved.Clone(), needle = Encoding.UTF8.GetBytes(edit.Item1), replacement = Encoding.UTF8.GetBytes(edit.Item2);
                    var offsets = Enumerable.Range(0, bad.Length - needle.Length + 1).Where(i => bad.AsSpan(i, needle.Length).SequenceEqual(needle)).ToArray();
                    c.Equal(1, offsets.Length, "exact original account field selected for codec corruption");
                    Buffer.BlockCopy(replacement, 0, bad, offsets.Single(), replacement.Length);
                    var candidate = PurchasingTests.New(factory).World.Services.Get<IBusinessAccounts>();
                    c.Throws<CorruptStateException>(() => ((IPersistentService)candidate).Sync(new StateReader(bad, 0, bad.Length)),
                        "original account codec rejects noncanonical reason order or amount");
                }
            }
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "account codec restores expense history and debt");
            c.That(Read(c, game).Select(row => (row.Reason, row.AmountRaw)).SequenceEqual(Read(c, twin).Select(row => (row.Reason, row.AmountRaw))), "restored exact reason totals");
            for (int i = 0; i < 3; i++) { game.Update(); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "saved ordinary update continuation"); }
            c.Equal(isolated, idle.Hash(), "interleaved session has no shared spending state");
            c.Equal(0, Read(c, idle).Count, "other session retains authored empty history");
            var dormant = PurchasingTests.New(PurchasingTests.Factory("purchase-dormant")); hidden.Add(default);
            c.Equal(new Code("business.no-account"), dormant.Seat(0).View.Feature<BusinessView>().CopySpending(company, hidden), "money-off is absent history, not fake zero accounts");
            c.Equal(0, hidden.Count, "money-off clears caller data");
        }
    }
}
