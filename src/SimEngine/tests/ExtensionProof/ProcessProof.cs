using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ProcessAccessDef : Def, IContentAccessDefinition
    {
        public string Denied { get; private set; }
        protected override void ReadCore(JsonReader reader) { Denied = reader.String("deny"); }
        public Code Check(World world, PlayerId player, IContentDefinition definition)
            => definition.Id == Denied ? new Code("proof.content-denied") : Code.None;
    }
    public sealed class ProcessProofModule : GameModule
    {
        public int Initializations;
        public override string Id => "proof.process";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ProcessAccessDef>("process_access");
            types.Component<SingleResourcePortDef>("process_stock", 4);
            types.Component<CreationProbeDef>("creation_probe", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.AddSystem(new InitializationCounter(this), 300);
        }
        private sealed class InitializationCounter : GameSystem
        {
            private readonly ProcessProofModule _probe;
            public InitializationCounter(ProcessProofModule probe) { _probe = probe; }
            public override string Id => "proof.process-initialization";
            public override int StateSchemaVersion => 1;
            public override void Initialize(World world) { _probe.Initializations++; }
            public override void Update(World world, TickInfo tick) { }
        }
    }
}
