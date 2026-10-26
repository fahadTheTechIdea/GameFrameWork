using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Public-only mod capability. It owns its outlet settings, never stock, demands or hauling.
    public sealed class ContractOutletPolicyDef : ComponentDef<ContractOutletPolicy>
    {
        private string _profile; public ILogisticsPolicyDefinition Profile { get; private set; }
        public ResolvedContent Content { get; private set; }
        public int Fault { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            _profile = r.String("profile"); Fault = r.Int("fault", 0);
            if (Fault < 0 || Fault > 2) throw r.Error("unknown outlet proof fault");
        }
        protected override void ResolveCore(ContentResolver c) { Content = c.Content; Profile = c.Def<ILogisticsPolicyDefinition>(_profile); }
        protected override void ValidateCore(EntityDef owner)
        {
            LogisticsPolicyRules.ValidateProfile(Content, owner, Profile);
        }
    }
    public sealed class ContractOutletPolicy : Component<ContractOutletPolicyDef>, IOutletPolicy, IEntityStateSource, IEntityInitialization
    {
        private OutletPolicySettings _settings;
        public override void OnCreate() => _settings = Def.Profile.Outlet;
        public void Initialize(World world) => LogisticsPolicyRules.InitializeStock(world, Entity, Def.Profile);
        public void CopyOutletPolicyPorts(System.Collections.Generic.IList<Code> destination) { destination.Clear(); destination.Add(Code.None); }
        public OutletPolicySettings ReadOutletPolicy(Code port = default) => port.IsNone ? _settings : null;
        public Code CheckOutletPolicy(World world, OutletPolicySettings settings, Code port = default)
        {
            if (!port.IsNone) return LogisticsPolicyCodes.NoPolicy;
            if (Def.Fault == 1)
                _settings = new OutletPolicySettings(!_settings.Enabled, _settings.Reserve, _settings.ExportPriority, _settings.AllowedDestinations, _settings.ReserveAccess);
            return !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ? Codes.NotFound : LogisticsPolicyRules.CheckOutlet(world.Content, Entity.Get<IOutletPort>(), settings);
        }
        public void ConfigureOutletPolicy(World world, OutletPolicySettings settings, Code port = default)
        {
            Code refusal = CheckOutletPolicy(world, settings, port); if (!refusal.IsNone) throw new InvalidOperationException("external outlet configuration refused: " + refusal);
            if (Def.Fault != 2) _settings = settings; Entity.Touch();
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context) => _settings.Enabled ? null : Def.Profile.States["disabled"];
        public override void Sync(IStateSync sync)
        {
            var next = OutletPolicySettings.Sync(sync, _settings);
            if (!LogisticsPolicyRules.CheckOutlet(Def.Content, Entity.Get<IOutletPort>(), next).IsNone)
                throw new CorruptStateException("external outlet has invalid native settings");
            _settings = next;
        }
    }
}
