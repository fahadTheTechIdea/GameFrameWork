using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public readonly struct BusinessAccountSnapshot
    {
        public PlayerId Player { get; }
        public Fix Balance { get; }
        public UnitId Currency { get; }
        public string Policy { get; }
        public string IncomePolicy { get; }
        public long GenerationSettlements { get; }
        public BigInteger GenerationIncomeRaw { get; }
        public Fix Reserved { get; }
        public BigInteger AvailableRaw { get; }
        public int BudgetReservations { get; }
        internal BusinessAccountSnapshot(PlayerId player, Fix balance, UnitId currency, string policy,
            string incomePolicy, long settlements, BigInteger earned, Fix reserved, BigInteger available, int reservations)
        { Player = player; Balance = balance; Currency = currency; Policy = policy;
          IncomePolicy = incomePolicy; GenerationSettlements = settlements; GenerationIncomeRaw = earned;
          Reserved = reserved; AvailableRaw = available; BudgetReservations = reservations; }
    }
    public interface IBusinessAccounts : IEconomy
    {
        UnitId Currency { get; }
        string Policy { get; }
        Code ReadAccount(PlayerId player, out BusinessAccountSnapshot account);
        Code CopySpending(PlayerId player, IList<BusinessExpenseSnapshot> destination);
    }

    /// <summary>The selected module's sole mutable account owner; all economy aliases share this codec.</summary>
    internal sealed partial class BusinessAccounts : IBusinessAccounts, IPersistentService
    {
        private readonly Fix[] _balances;
        public string Id => "sim.tycoon-accounts";
        public int StateSchemaVersion => 7;
        public UnitId Currency { get; }
        public string Policy { get; }
        public BusinessAccounts(int seats, Fix opening, UnitId currency, string policy)
        {
            _balances = new Fix[seats];
            _generationCounts = new long[seats]; _generationEarnings = new BigInteger[seats];
            _spending = new SortedDictionary<string, BigInteger>[seats];
            for (int i = 0; i < seats; i++)
            { _balances[i] = opening; _spending[i] = new SortedDictionary<string, BigInteger>(StringComparer.Ordinal); }
            Currency = currency; Policy = policy;
        }
        private bool IsAccount(PlayerId player) => player.Value >= 0 && player.Value < _balances.Length;
        private static void Amount(Fix amount) { if (amount < Fix.Zero) throw new ArgumentOutOfRangeException(nameof(amount)); }
        private void RequireAccount(PlayerId player) { if (!IsAccount(player)) throw new ArgumentOutOfRangeException(nameof(player)); }
        public bool CanAfford(PlayerId player, Fix amount) { Amount(amount); return IsAccount(player) && AvailableRaw(player) >= amount.Raw; }
        public bool TryCharge(PlayerId player, Fix amount, Code reason)
        {
            if (!CanAfford(player, amount)) return false;
            Debit(player, amount, reason); return true;
        }
        public void Charge(PlayerId player, Fix amount, Code reason)
        { Debit(player, amount, reason); }
        public void Credit(PlayerId player, Fix amount, Code reason)
        { Amount(amount); RequireAccount(player); _balances[player.Value] += amount; }
        public Fix Cash(PlayerId player) { RequireAccount(player); return _balances[player.Value]; }
        public Code ReadAccount(PlayerId player, out BusinessAccountSnapshot account)
        {
            account = default;
            if (!IsAccount(player)) return Codes.InvalidArgument;
            int count = 0; foreach (var receipt in _budgets.Values) if (receipt.Company == player) count++;
            account = new BusinessAccountSnapshot(player, _balances[player.Value], Currency, Policy,
                _income?.Id, _generationCounts[player.Value], _generationEarnings[player.Value],
                Fix.FromRaw((long)ReservedRaw(player)), AvailableRaw(player), count); return Code.None;
        }
        public void Sync(IStateSync s)
        {
            string policy = Policy, currency = Currency.Id;
            int count = _balances.Length;
            s.String(ref policy); s.String(ref currency); s.Int(ref count);
            if (policy != Policy || currency != Currency.Id || count != _balances.Length)
                throw new CorruptStateException("business account definition, currency or seats disagree with selected content");
            string income = _income?.Id; s.String(ref income);
            if (income != _income?.Id) throw new CorruptStateException("saved income policy differs from selected content");
            long sequence = _generationSequence; s.Long(ref sequence);
            if (sequence < 0 || income == null && sequence != 0) throw new CorruptStateException("invalid generation settlement sequence");
            BigInteger total = BigInteger.Zero;
            for (int i = 0; i < _balances.Length; i++)
            {
                Fix value = _balances[i]; long settled = _generationCounts[i]; BigInteger earned = _generationEarnings[i];
                s.Fix(ref value); s.Long(ref settled); string raw = earned.ToString(System.Globalization.CultureInfo.InvariantCulture); s.String(ref raw);
                if (!BigInteger.TryParse(raw, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out earned) ||
                    settled < 0 || earned < 0 || settled == 0 && earned != 0 || settled > 0 && earned == 0 || income == null && settled != 0)
                    throw new CorruptStateException("invalid saved generation accounting");
                total += settled;
                if (s.Reading) { _balances[i] = value; _generationCounts[i] = settled; _generationEarnings[i] = earned; }
            }
            if (total != sequence) throw new CorruptStateException("generation accounting disagrees with settlement identity");
            if (s.Reading) _generationSequence = sequence;
            SyncBudgets(s);
            SyncActionCharges(s);
            SyncSales(s);
            SyncSpending(s);
        }
    }
}
