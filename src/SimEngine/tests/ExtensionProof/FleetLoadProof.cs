using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Declaration only; existing independent batching provider/rule, no new scheduler.</summary>
    public sealed class FleetLoadProofModule : GameModule
    {
        public override string Id => "proof.fleet-load";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(MachineFleetsModule.ModuleId, Version), new ModuleRequirement(LogisticsModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<ContractHaulLoadDef>("proof_fleet_load", 2);
            types.Section<OddHaulLoadProfileDef>("proof_fleet_load_profiles");
        }
    }
}
