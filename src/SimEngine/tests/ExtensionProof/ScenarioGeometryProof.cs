using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent content registration only; all geometry/composition stays in the engine declaration.</summary>
    public sealed class ScenarioGeometryProofModule : GameModule
    {
        public override string Id => "proof.scenario-geometry";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Section<IndependentTileSpaceDef>("independent_tile_spaces");
    }
}
