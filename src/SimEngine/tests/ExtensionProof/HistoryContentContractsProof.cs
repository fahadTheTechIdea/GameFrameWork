using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentHistoryPolicyDef : Def, IPlanHistoryPolicy
    {
        private PlanHistoryPolicyDeclaration _declaration;
        public GameDuration MinimumAge => _declaration.MinimumAge;
        public int MinimumRetainedCohorts => _declaration.MinimumRetainedCohorts;
        public int CohortsPerUpdate => _declaration.CohortsPerUpdate;
        protected override void ReadCore(JsonReader reader) => _declaration = PlanHistoryPolicyDeclaration.Read(reader);
    }
    // Authored content and diagnostic Initialize only; no pruning, scheduler or copied history.
    public sealed class HistoryContractModule : GameModule
    {
        public const string ModuleId = "proof.history-contracts";
        private readonly InitializeProbe _initialize;
        public HistoryContractModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(PlanHistoryModule.ModuleId, Version) };
        public bool IndependentAlias { get; private set; }
        public override void DeclareContent(ContentTypes types) => types.Section<IndependentHistoryPolicyDef>("independent_history_policies");
        public override void Compose(WorldComposer composer)
        {
            var policy = composer.Content.Get<IPlanHistoryPolicy>("contract.history");
            IndependentAlias = !(policy is PlanHistoryPolicyDef) && composer.Content.Owns(policy)
                && composer.Services.Get<WorkBoard>().ReadPlanHistoryPolicy()?.Policy == policy.Id;
            composer.AddSystem(_initialize, 301);
        }
    }
}

