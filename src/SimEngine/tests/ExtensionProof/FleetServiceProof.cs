using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Declaration only; existing independent recurring source, no new cadence/scheduler.</summary>
    public sealed class FleetServiceProofModule : GameModule
    {
        public override string Id => "proof.fleet-service";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(MachineFleetsModule.ModuleId, Version), new ModuleRequirement("proof.fleet-trip", Version) });
        public override void DeclareContent(ContentTypes types) => types.Component<ProofRecurringSourceDef>("proof_fleet_service", 5);
    }
}
