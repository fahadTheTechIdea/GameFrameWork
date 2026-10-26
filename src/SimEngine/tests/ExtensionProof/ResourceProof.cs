using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>A normal extension resource kind, not an additional catalog or inventory.</summary>
    public sealed class PlanetOreDef : ResourceDef
    {
        public string Composition { get; private set; }
        protected override void ReadProperties(JsonReader r)
        {
            Composition = r.String("composition");
            if (string.IsNullOrWhiteSpace(Composition)) throw r.Error("composition must be authored");
        }
    }
    public sealed class ResourceProofModule : GameModule
    {
        public int Initializations;
        public override string Id => "proof.resources";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<PlanetOreDef>("planet_ores");
            types.Component<SingleResourcePortDef>("ore_stock", 4);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.AddSystem(new InitializationCounter(this), 300);
        }
        private sealed class InitializationCounter : GameSystem
        {
            private readonly ResourceProofModule _module;
            public InitializationCounter(ResourceProofModule module) { _module = module; }
            public override string Id => "proof.resource-initialization";
            public override int StateSchemaVersion => 1;
            public override void Initialize(World world) { _module.Initializations++; }
            public override void Update(World world, TickInfo tick) { }
        }
    }
}
