using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class PlanHistoryPolicyProofModule : PlanHistoryModule
    {
        public override void DeclareContent(ContentTypes types)
        { base.DeclareContent(types); types.Section<PublicPlanHistoryPolicyDef>("public_plan_history_policies"); }
    }
    /// <summary>Independent public-only authored provider; no scheduler, state store or time loop.</summary>
    public sealed class PublicPlanHistoryPolicyDef : PlanHistoryPolicyDef
    {
        private PlanHistoryPolicyDeclaration _declaration;
        public override GameDuration MinimumAge => _declaration.MinimumAge;
        public override int MinimumRetainedCohorts => _declaration.MinimumRetainedCohorts;
        public override int CohortsPerUpdate => _declaration.CohortsPerUpdate;
        protected override void ReadCore(JsonReader reader) => _declaration = PlanHistoryPolicyDeclaration.Read(reader);
    }
}
