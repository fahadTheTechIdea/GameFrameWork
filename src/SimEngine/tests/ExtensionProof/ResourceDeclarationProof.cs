using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Content registration and lifecycle instrumentation only; existing engine owners execute gameplay.
    public sealed class ResourceDeclarationModule : GameModule
    {
        private readonly InitializeProbe _initialize;
        public ResourceDeclarationModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => "proof.resource-declarations";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires
            => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<PlanetOreDef>("planet_ores");
        public override void Compose(WorldComposer composer) => composer.AddSystem(_initialize, 300);
    }
}
