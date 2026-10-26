using System;
using System.Collections.Generic;

namespace Beep.SimEngine
{
    public enum MonetaryBudgetStatus : byte { InvalidRequest, InvalidReceipt, NoFunds, RangeExceeded, Ready, Committed }
    public readonly struct MonetaryBudgetRequest
    {
        public ReservationOwner Owner { get; }
        public Fix Amount { get; }
        public MonetaryBudgetRequest(ReservationOwner owner, Fix amount) { Owner = owner; Amount = amount; }
    }
    /// <summary>Detached identity/provenance. The account service alone owns the live hold.</summary>
    public readonly struct MonetaryBudgetReceipt : IEquatable<MonetaryBudgetReceipt>
    {
        public long Id { get; }
        public PlayerId Company { get; }
        public ReservationOwner Owner { get; }
        public Fix Amount { get; }
        public bool IsValid => Id > 0 && Company.IsValid && Owner.IsValid && Amount.IsPositive;
        public MonetaryBudgetReceipt(long id, PlayerId company, ReservationOwner owner, Fix amount)
        { Id = id; Company = company; Owner = owner; Amount = amount; }
        public bool Equals(MonetaryBudgetReceipt other) => Id == other.Id && Company == other.Company && Owner.Equals(other.Owner) && Amount == other.Amount;
        public override bool Equals(object obj) => obj is MonetaryBudgetReceipt other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode() ^ Company.GetHashCode() ^ Owner.GetHashCode() ^ Amount.GetHashCode();
    }
    /// <summary>Optional account-owned whole-set holds, not physical resource reservations or a second ledger.</summary>
    public interface IMonetaryBudgets
    {
        /// <summary>Monotonic owner revision; pure reads never change it.</summary>
        long Revision { get; }
        MonetaryBudgetStatus EvaluateReservations(PlayerId company, IReadOnlyList<MonetaryBudgetRequest> requests);
        MonetaryBudgetStatus Reserve(PlayerId company, IReadOnlyList<MonetaryBudgetRequest> requests, out IReadOnlyList<MonetaryBudgetReceipt> receipts);
        MonetaryBudgetStatus EvaluateReceipts(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts);
        MonetaryBudgetStatus EvaluateSpend(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts);
        MonetaryBudgetStatus Spend(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts, Code reason);
        MonetaryBudgetStatus Release(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts);
        void InspectBudgets(Action<IReadOnlyList<MonetaryBudgetReceipt>> inspect);
    }
}
