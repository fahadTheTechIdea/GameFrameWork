using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentLockstepPolicyDef : Def, ILockstepPolicy
    {
        private LockstepPolicyDeclaration _declaration;
        public long? MaximumAhead => _declaration.MaximumAhead;
        public long? MaximumCommandsPerFrame => _declaration.MaximumCommandsPerFrame;
        protected override void ReadCore(JsonReader reader) => _declaration = LockstepPolicyDeclaration.Read(reader);
    }
    public sealed class LockstepPolicyProofModule : GameModule
    {
        public override string Id => "proof.lockstep-policy";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public InitializeProbe Initialize { get; } = new InitializeProbe("proof.lockstep-policy.initialize");
        public override void DeclareContent(ContentTypes types)
            => types.Section<IndependentLockstepPolicyDef>("independent_lockstep_policies");
        public override void Compose(WorldComposer composer) => composer.AddSystem(Initialize, 303);
    }
}
