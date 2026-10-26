using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Public-only content provider; no dispatcher, stock, claims or chronology owner.
    public sealed class ProofLogisticsRecoveryProfileDef : Def, ILogisticsRecoveryPolicy
    {
        private LogisticsRecoveryProfileDeclaration _declaration;
        public LogisticsRecoverySettings Read(LogisticsRecoveryKind kind) => _declaration.ReadSettings(kind);
        protected override void ReadCore(JsonReader reader) => _declaration = LogisticsRecoveryProfileDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class LogisticsRecoveryPolicyProofModule : GameModule
    {
        public const string ModuleId = "proof.logistics-recovery";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(LogisticsRecoveryPolicyModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ProofLogisticsRecoveryProfileDef>("proof_logistics_recovery_profiles");
            types.Component<BoundCargoDef>("proof_bound_cargo", 1);
            types.Component<ProofWorkPolicyDef>("proof_recovery_work_policy", 2);
        }
        public override void Compose(WorldComposer composer) => composer.AddSystem(new BoundCargoContext(), 310);
    }
}
