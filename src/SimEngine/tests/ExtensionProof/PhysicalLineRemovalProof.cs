using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Content registration only. Existing engine contracts execute all normal gameplay.
    public sealed class PhysicalLineRemovalProofModule : GameModule
    {
        public override string Id => "proof.physical-line-removal";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InfrastructureModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<IndependentLineEffectDef>("independent_line_effects");
        public override void Compose(WorldComposer composer) { }
    }
}
