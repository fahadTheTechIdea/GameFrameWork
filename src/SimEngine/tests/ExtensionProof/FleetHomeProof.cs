using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Declaration only; existing public worker/policy implementations own all behavior.</summary>
    public sealed class FleetHomeProofModule : GameModule
    {
        public override string Id => "proof.fleet-home";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(MachineFleetsModule.ModuleId, Version), new ModuleRequirement(LogisticsModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<ContractWorkerDef>("proof_fleet_home_worker", 1);
            types.Component<ProofWorkPolicyDef>("proof_fleet_work_policy", 2);
        }
    }
}
