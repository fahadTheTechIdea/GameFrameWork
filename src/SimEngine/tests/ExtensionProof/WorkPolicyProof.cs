using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ProofWorkPolicyDef : ComponentDef<ProofWorkPolicy>
    {
        private string _profile;
        public IWorkPolicyDefinition Profile { get; private set; }
        public ResolvedContent Content { get; private set; }
        public int Fault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _profile = reader.String("profile"); Fault = reader.Int("fault");
            if (Fault < 0 || Fault > 4) throw reader.Error("invalid explicit work policy fault");
        }
        protected override void ResolveCore(ContentResolver content)
        { Content = content.Content; Profile = content.Def<IWorkPolicyDefinition>(_profile); }
        protected override void ValidateCore(EntityDef owner)
        {
            if (!owner.HasCapability<IWorker>() || !WorkPolicyRules.CheckInitialSettings(Content,
                owner.Component<ISpatialPresenceDefinition>()?.Space, Profile.Initial).IsNone)
                throw new ContentException(owner.Id + ": invalid public work policy binding");
        }
    }
    // Independent policy owner; no native component wrapper, second fleet/job store or scheduler.
    public sealed class ProofWorkPolicy : Component<ProofWorkPolicyDef>, IWorkPolicy, IEntityStateSource
    {
        private WorkPolicySettings _settings;
        // Negative boundary instrumentation only. Like other proof callbacks,
        // it is excluded from gameplay persistence and never drives positive play.
        public string ReadProbe;
        public override void OnCreate() => _settings = Def.Profile.Initial;
        public WorkPolicySettings ReadPolicy()
        {
            if (Def.Fault == 3 || ReadProbe == "touch") Entity.Touch();
            if (ReadProbe == "payload") _settings = new WorkPolicySettings(_settings.Enabled, checked(_settings.Priority + 1),
                _settings.AllowedRoles, _settings.Area, _settings.Interruptible, _settings.EmergencyEligible, _settings.AssignmentMode);
            if (ReadProbe == "throw") throw new InvalidOperationException("proof.policy-read-fault");
            return _settings;
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context) => WorkPolicyRules.State(Entity, _settings, Def.Profile.States);
        public Code CheckPolicy(World world, WorkPolicySettings settings)
        { if (Def.Fault == 2) Entity.Touch(); return WorkPolicyRules.CheckSettings(world, Entity, settings); }
        public void Configure(World world, WorkPolicySettings settings)
        {
            _settings = Def.Fault == 1 ? new WorkPolicySettings(settings.Enabled, settings.Priority, settings.AllowedRoles,
                settings.Area, settings.Interruptible, settings.EmergencyEligible, WorkAssignmentMode.All) : settings;
            if (Def.Fault == 4) Entity.Get<IWorker>().Job = 0;
            Entity.Touch(); world.Services.Get<WorkBoard>().Dirty = true;
            world.Services.Get<IWorkerDispatch>().RefreshPolicy(world, Entity);
        }
        public override void Sync(IStateSync sync)
        {
            var settings = WorkPolicySettings.Sync(sync, _settings);
            if (!WorkPolicyRules.CheckRoles(Def.Content, settings).IsNone) throw new CorruptStateException("invalid public work roles");
            if (sync.Reading) _settings = settings;
        }
    }
}
