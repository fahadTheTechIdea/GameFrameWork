using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class LockstepPolicyDef : Def, ILockstepPolicy
    {
        private LockstepPolicyDeclaration _declaration;
        public long? MaximumAhead => _declaration.MaximumAhead;
        public long? MaximumCommandsPerFrame => _declaration.MaximumCommandsPerFrame;
        protected override void ReadCore(JsonReader reader) => _declaration = LockstepPolicyDeclaration.Read(reader);
    }
    public sealed class LockstepPolicyDeclaration
    {
        public long? MaximumAhead { get; }
        public long? MaximumCommandsPerFrame { get; }
        private LockstepPolicyDeclaration(long? ahead, long? commands)
        { MaximumAhead = ahead; MaximumCommandsPerFrame = commands; }
        public static LockstepPolicyDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit lockstep policy required");
            return new LockstepPolicyDeclaration(reader.PositiveLimit("maximum_ahead"), reader.PositiveLimit("maximum_commands_per_frame"));
        }
    }
    public sealed class LockstepPolicyModule : GameModule
    {
        public const string ModuleId = "sim.lockstep-policy";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Section<LockstepPolicyDef>("lockstep_policies");
        public override void Compose(WorldComposer composer)
        {
            foreach (ILockstepPolicy policy in composer.Content.All<ILockstepPolicy>())
                LockstepPolicyContracts.Read(composer.World, policy);
            JsonReader settings = composer.Scenario.SettingsFor(Id)
                ?? throw new ContentException("selected lockstep policy requires explicit profile settings");
            string id = settings.String("profile"); settings.End();
            var selected = composer.Content.Find<ILockstepPolicy>(id)
                ?? throw new ContentException("missing authored lockstep profile: " + id);
            composer.Services.Bind<ILockstepPolicy>(selected);
            composer.AddViewFeature(ModuleId, 1, (world, viewer) => new LockstepPolicyView(world));
        }
    }
}
