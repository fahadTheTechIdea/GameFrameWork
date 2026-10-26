using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentRecoveryProfileDef : Def, ILogisticsRecoveryPolicy
    {
        private LogisticsRecoveryProfileDeclaration _declaration;
        protected override void ReadCore(JsonReader reader) => _declaration = LogisticsRecoveryProfileDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public LogisticsRecoverySettings Read(LogisticsRecoveryKind kind) => _declaration.ReadSettings(kind);
    }
    public sealed class IndependentRecoveryDestinationDef : Def, IRecoveryDestinationPolicy
    {
        private RecoveryDestinationDeclaration _declaration;
        protected override void ReadCore(JsonReader reader) => _declaration = RecoveryDestinationDeclaration.Read(reader);
        public RecoveryDestinationDecision Evaluate(RecoveryDestinationFacts facts) => _declaration.Evaluate(facts);
    }
    // Content and diagnostic Initialize only; the native coordinator executes every recovery.
    public sealed class RecoveryContractModule : GameModule
    {
        public const string ModuleId = "proof.recovery-contracts";
        private readonly InitializeProbe _initialize;
        public RecoveryContractModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(LogisticsRecoveryPolicyModule.ModuleId, Version) };
        public bool IndependentAliases { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentRecoveryProfileDef>("independent_recovery_profiles");
            types.Section<IndependentRecoveryDestinationDef>("independent_recovery_destinations");
        }
        public override void Compose(WorldComposer composer)
        {
            var profile = composer.Content.Get<ILogisticsRecoveryPolicy>("contract.recovery");
            var destination = composer.Content.Get<IRecoveryDestinationPolicy>("contract.demand-only");
            IndependentAliases = profile is IndependentRecoveryProfileDef && destination is IndependentRecoveryDestinationDef
                && composer.Content.Owns(profile) && composer.Content.Owns(destination)
                && ReferenceEquals(profile, composer.Services.Get<ILogisticsRecoveryPolicy>());
            composer.AddSystem(_initialize, 301);
        }
    }
}
