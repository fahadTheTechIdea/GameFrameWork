using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Composition only: existing independent/native providers and file own all behavior.
    public sealed class QueueContextProofModule : GameModule
    {
        public override string Id => "proof.queue-context";
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement("proof.transformer",Version),new ModuleRequirement(WorldSetupModule.ModuleId,Version) };
    }
}
