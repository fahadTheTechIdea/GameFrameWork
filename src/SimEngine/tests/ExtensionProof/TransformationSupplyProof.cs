using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Owns explicit proof JSON only; existing engine contracts perform all gameplay.</summary>
    public sealed class TransformationSupplyProofModule : GameModule
    {
        public override string Id => "proof.transformation-supply";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TransformationModule.ModuleId, Version), new ModuleRequirement(LogisticsModule.ModuleId, Version) };
    }
}
