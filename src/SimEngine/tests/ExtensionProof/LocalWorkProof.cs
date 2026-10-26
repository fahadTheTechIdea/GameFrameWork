using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class LocalWorkProofModule : GameModule
    {
        public int Initializations;
        public override string Id => "proof.local-work";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<SingleResourcePortDef>("local_stock", 4);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.AddSystem(new InitializationCounter(this), 300);
        }
        private sealed class InitializationCounter : GameSystem
        {
            private readonly LocalWorkProofModule _module;
            public InitializationCounter(LocalWorkProofModule module) { _module = module; }
            public override string Id => "proof.local-work-initialization";
            public override int StateSchemaVersion => 1;
            public override void Initialize(World world) { _module.Initializations++; }
            public override void Update(World world, TickInfo tick) { }
        }
    }
}
