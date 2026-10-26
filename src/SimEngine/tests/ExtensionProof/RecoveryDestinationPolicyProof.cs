using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side content extension compiled using public contracts only. No world/job/stock owner.
    public sealed class MinimumPriorityRecoveryDestinationDef : Def, IRecoveryDestinationPolicy
    {
        private int _minimum;
        protected override void ReadCore(JsonReader reader)
        {
            _minimum = reader.Int("minimum_priority");
            if (_minimum < 0) throw reader.Error("minimum_priority must be nonnegative");
        }
        public RecoveryDestinationDecision Evaluate(RecoveryDestinationFacts facts)
        {
            if (!facts.HasDemand || facts.DemandPriority < _minimum)
                return new RecoveryDestinationDecision(new Code("proof.recovery-priority-required"));
            Fix amount = facts.QuantityQuantum.Floor(Fix.Min(facts.FeasibleAmount, facts.DemandGap));
            return amount.IsPositive ? new RecoveryDestinationDecision(amount, (long)facts.DemandPriority + 1)
                : new RecoveryDestinationDecision(new Code("proof.recovery-quantum-gap"));
        }
    }
}
