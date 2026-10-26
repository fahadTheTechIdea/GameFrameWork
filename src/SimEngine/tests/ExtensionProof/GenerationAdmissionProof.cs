using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class GenerationAdmissionProbeDef : Def
    {
        public int Marker { get; private set; }
        public Code Refusal { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Marker = reader.Int("initial_marker");
            if (Marker < 0 || !reader.Has("refusal")) throw reader.Error("explicit marker/refusal required");
            string refusal = reader.NullableString("refusal");
            if (refusal != null) ContractIdentity.ValidateId(refusal, "generation probe refusal");
            Refusal = refusal == null ? Code.None : new Code(refusal);
        }
    }
    // Engine-side optional participant proof, not accounts, money or another physical stock ledger.
    public sealed class GenerationAdmissionProbe : IResourceProductionSettlement, IPersistentService
    {
        private readonly World _world;
        private readonly Code _refusal;
        private int _marker, _commits;
        public string Id => "proof.generation-admission";
        public int StateSchemaVersion => 1;
        public int Marker => _marker;
        public int Commits => _commits;
        public int Calls;
        public ResourceGenerationContext Last;
        public Action<World> AdmissionProbe;
        internal GenerationAdmissionProbe(World world, GenerationAdmissionProbeDef definition)
        { _world = world; _marker = definition.Marker; _refusal = definition.Refusal; }
        public void MutatePersistedProbe() => _marker = checked(_marker + 1);
        public Code CheckGeneration(ResourceGenerationContext context)
        { Calls++; Last = context; AdmissionProbe?.Invoke(_world); return _refusal; }
        public Code PrepareGeneration(ResourceGenerationContext context, out IResourceProductionCommit commit)
        {
            Code refusal = CheckGeneration(context); commit = refusal.IsNone ? new CommitProbe(this) : null; return refusal;
        }
        private sealed class CommitProbe : IResourceProductionCommit
        {
            private readonly GenerationAdmissionProbe _owner;
            private bool _used;
            internal CommitProbe(GenerationAdmissionProbe owner) => _owner = owner;
            public void Commit() { if (_used) throw new InvalidOperationException("generation admission reused"); _used = true; _owner._commits = checked(_owner._commits + 1); }
        }
        public void Sync(IStateSync sync)
        { sync.Int(ref _marker); sync.Int(ref _commits); if (_marker < 0 || _commits < 0) throw new CorruptStateException("invalid generation probe state"); }
    }
    public sealed class GenerationAdmissionProofModule : GameModule
    {
        public override string Id => "proof.generation-admission";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.Empty<ModuleRequirement>();
        public override void DeclareContent(ContentTypes types) => types.Section<GenerationAdmissionProbeDef>("generation_admission_probes");
        public override void Compose(WorldComposer composer)
        {
            var profiles = composer.Content.All<GenerationAdmissionProbeDef>();
            if (profiles.Count != 1) throw new ContentException("generation admission proof requires exactly one explicit profile");
            var participant = new GenerationAdmissionProbe(composer.World, profiles[0]);
            composer.Services.Bind<GenerationAdmissionProbe>(participant);
            composer.Services.Bind<IResourceProductionSettlement>(participant);
        }
    }
}
