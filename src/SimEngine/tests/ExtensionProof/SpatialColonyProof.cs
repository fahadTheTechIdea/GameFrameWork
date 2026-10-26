using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Content composition only. Reuse the original independent worker declaration
    // and native Worker runtime, with no dispatch, progress or stock implementation.
    public sealed class SpatialColonyProofModule : GameModule
    {
        public override string Id => "proof.spatial-colony";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(WorkModule.ModuleId, Version),
            new ModuleRequirement(ConstructionModule.ModuleId, Version),
            new ModuleRequirement(TileNavigationModule.ModuleId, Version),
            new ModuleRequirement("proof.tile-sensor-space", Version) };
        public override void DeclareContent(ContentTypes types)
            => types.Component<IndependentWorkerDef>("colony_worker", 1);
    }
}
