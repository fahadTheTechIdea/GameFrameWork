using System;
using System.Collections.Generic;

namespace Beep.SimEngine
{
    /// <summary>Authored monetary readiness fact, not a balance, payment or reserved budget.</summary>
    public sealed class WorkCostChoice
    {
        public IReadOnlyList<string> Path { get; }
        public string Requirement { get; }
        public Fix Amount { get; }
        public bool Credit { get; }
        public MonetaryCostDecision Decision { get; }
        public MonetaryCostCategory Category => Decision.Category;
        public MonetaryCostApplicability Applicability => Decision.Applicability;
        public WorkCostChoice(IEnumerable<string> path, string requirement, MonetaryCostDecision decision, bool credit)
        {
            Path = WorkRequirementPath.Copy(path);
            if (string.IsNullOrWhiteSpace(requirement) || !decision.AuthoredAmount.IsPositive || decision.Applicability == MonetaryCostApplicability.Unavailable)
                throw new ArgumentException("cost choice requires a named positive authored decision");
            Requirement = requirement; Amount = decision.AuthoredAmount; Credit = credit; Decision = decision;
        }
        internal bool At(IReadOnlyList<string> path) => WorkRequirementPath.Matches(Path, path);
        internal ReservationOwner BudgetOwner(int order, string node) => WorkRequirementPath.Owner("sim.work.budgets.", order, node, Path);
    }
    /// <summary>Node-owned obligation/spent fact; IEconomy alone owns actual money.</summary>
    public sealed class WorkCostBinding
    {
        public WorkCostChoice Choice { get; }
        public bool Spent { get; }
        public MonetaryBudgetReceipt? Receipt { get; }
        public WorkCostBinding(WorkCostChoice choice, bool spent, MonetaryBudgetReceipt? receipt)
        {
            Choice = choice ?? throw new ArgumentNullException(nameof(choice));
            if (choice.Applicability != MonetaryCostApplicability.Enabled) throw new ArgumentException("only an enabled monetary cost can become an obligation");
            if (choice.Credit ? receipt.HasValue : !receipt.HasValue || !receipt.Value.IsValid || receipt.Value.Amount != choice.Amount)
                throw new ArgumentException("ordinary cost requires its exact budget receipt; debt-authorized cost has none");
            Spent = spent; Receipt = receipt;
        }
    }
}
