using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Declaration only; original independent outlet owner, no new stock or scheduler.</summary>
    public sealed class FleetOutletProofModule : GameModule
    {
        public override string Id => "proof.fleet-outlet";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(MachineFleetsModule.ModuleId, Version), new ModuleRequirement("proof.fleet-trip", Version) });
        public override void DeclareContent(ContentTypes types) => types.Component<ContractOutletPolicyDef>("proof_fleet_outlet", 2);
    }
}
