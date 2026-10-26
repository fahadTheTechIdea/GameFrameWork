using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Metadata registration only; existing engine declarations and WorkBoard own every rule and state.
    public sealed class RequirementAcquisitionModule : GameModule
    {
        public override string Id => "proof.requirement-acquisition";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentWorkerRoleDef>("independent_worker_roles");
            types.Section<IndependentStaffedExecutorDef>("independent_staffed_executors");
            types.Section<IndependentTimedExecutorDef>("independent_timed_executors");
            types.Section<IndependentRequirementGroupDef>("independent_requirement_groups");
            types.Section<IndependentTargetResourceDef>("independent_target_resources");
            types.Section<IndependentWorkerGuardDef>("independent_worker_guards");
        }
    }
}
