using System;

namespace Beep.SimEngine
{
    public enum MonetaryCostCategory : byte { Unavailable = 0, Construction = 1, Manufacturing = 2, Running = 3 }
    public enum MonetaryCostApplicability : byte { Unavailable = 0, NotAuthored = 1, DisabledByMode = 2, Enabled = 3, UnavailableCapability = 4 }

    /// <summary>Detached monetary decision, never payment, stock, a balance or a reservation.</summary>
    public readonly struct MonetaryCostDecision
    {
        public MonetaryCostCategory Category { get; }
        public Fix AuthoredAmount { get; }
        public MonetaryCostApplicability Applicability { get; }
        public Fix? EffectiveAmount => Applicability == MonetaryCostApplicability.Enabled ? AuthoredAmount : (Fix?)null;
        public MonetaryCostDecision(MonetaryCostCategory category, Fix authoredAmount, MonetaryCostApplicability applicability)
        {
            if (category < MonetaryCostCategory.Construction || category > MonetaryCostCategory.Running || authoredAmount < Fix.Zero ||
                applicability < MonetaryCostApplicability.NotAuthored || applicability > MonetaryCostApplicability.UnavailableCapability ||
                (authoredAmount == Fix.Zero) != (applicability == MonetaryCostApplicability.NotAuthored))
                throw new ArgumentException("invalid monetary category, amount or applicability");
            Category = category; AuthoredAmount = authoredAmount; Applicability = applicability;
        }
    }
}
