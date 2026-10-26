using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Register an existing unrelated condition owner using only public engine contracts.</summary>
    public sealed class WorkerServiceProofModule : GameModule
    {
        public override string Id => "proof.worker-service";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(EquipmentConditionModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<ProofConditionDef>("proof_service_condition", 1);
            types.Section<ProofFacilityPolicyDef>("proof_facility_policies");
            types.Section<RoleFacilityPolicyDef>("proof_role_facility_policies");
            types.Component<ProofRecurringSourceDef>("proof_service_recurring", 5);
            types.Component<ProofOperationPolicyDef>("proof_service_safety", 1);
            types.Component<AutomationProbeDef>("proof_service_operation", 1);
            types.Component<ProofWorkPolicyDef>("proof_service_policy", 2);
        }
    }
}
