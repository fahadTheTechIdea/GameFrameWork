using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ContractHaulLoadDef : ComponentDef<ContractHaulLoad>, IHaulLoadPolicyDefinition
    {
        private string _profile, _mutationResource;
        public IHaulLoadProfile Profile { get; private set; }
        public IHaulLoadProfile InitialProfile => Profile;
        public ResolvedContent Content { get; private set; }
        public int Fault { get; private set; }
        public ResourceId MutationResource { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _profile = reader.String("profile"); Fault = reader.Int("fault");
            if (Fault < 0 || Fault > 20) throw reader.Error("unknown load proof fault");
            if (Fault == 18 || Fault == 19) _mutationResource = reader.String("mutation_resource");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            Content = content.Content; Profile = content.Def<IHaulLoadProfile>(_profile);
            if (_mutationResource != null) MutationResource = content.Resource(_mutationResource);
        }
    }
    // Independent public capability owns only current policy settings, never request age/cargo.
    public sealed class ContractHaulLoad : Component<ContractHaulLoadDef>, IHaulLoadPolicy
    {
        private HaulLoadSettings _settings;
        private World _observationWorld;
        private bool _reentering; // Bounds negative baseline probes, never saved gameplay or a runtime fallback.
        internal void ArmObservationFault(World world) => _observationWorld = world;
        private void Reenter(Action callback)
        {
            if (_reentering) return;
            _reentering = true; try { callback(); } finally { _reentering = false; }
        }
        public override void OnCreate() => _settings = Def.Profile.Initial;
        public HaulLoadSettings ReadLoadPolicy()
        {
            if (Def.Fault == 1) Entity.Touch();
            if (_observationWorld != null && Def.Fault >= 5 && Def.Fault <= 7)
            {
                if (Def.Fault == 6 || Def.Fault == 7) { var worker = Entity.Get<IWorker>(); worker.Held = !worker.Held; }
                if (Def.Fault != 6) throw new InvalidOperationException("proof.load-read-first-error");
            }
            if (_observationWorld != null && Def.Fault == 11)
                Reenter(() => HaulLoadPolicyContracts.Read(_observationWorld, Entity));
            return Def.Fault == 2 ? null : _settings;
        }
        public Code CheckLoadPolicy(World world, HaulLoadSettings settings)
        {
            if (Def.Fault == 4) Entity.Touch();
            if (Def.Fault == 13) HaulLoadPolicyContracts.Read(world, Entity); // Legitimate check -> current read.
            if (_observationWorld != null && Def.Fault >= 8 && Def.Fault <= 10)
            {
                if (Def.Fault == 9 || Def.Fault == 10) { var worker = Entity.Get<IWorker>(); worker.Held = !worker.Held; }
                if (Def.Fault != 9) throw new InvalidOperationException("proof.load-check-first-error");
            }
            if (_observationWorld != null && Def.Fault == 12)
                Reenter(() => HaulLoadPolicyContracts.CheckConfigure(world, Entity.Owner, Entity, settings));
            return HaulLoadPolicyContracts.CheckSettings(world, Entity, settings);
        }
        public void ConfigureLoadPolicy(World world, HaulLoadSettings settings)
        {
            // Deliberate negative callback faults only on explicitly armed diagnostic clones.
            bool probe = _observationWorld != null && !settings.Enabled;
            if (probe && Def.Fault == 20)
                Reenter(() => HaulLoadPolicyContracts.Configure(world, Entity, settings));
            if (probe && Def.Fault == 17) throw new InvalidOperationException("proof.load-commit-first-error");
            if (Def.Fault != 3) { _settings = settings; Entity.Touch(); }
            if (!probe) return;
            if (Def.Fault == 14) ((GameClock)world.Clock).Pause();
            if (Def.Fault == 15) ((GameClock)world.Clock).Speed = 2;
            if (Def.Fault == 16) { var worker = Entity.Get<IWorker>(); worker.Held = !worker.Held; }
            if (Def.Fault == 18 || Def.Fault == 19)
            {
                if (Entity.Get<IInventory>().Remove(Def.MutationResource, Fix.One) != Fix.One)
                    throw new InvalidOperationException("load commit diagnostic requires actual finite stock");
            }
            if (Def.Fault == 19) throw new InvalidOperationException("proof.load-commit-first-error");
        }
        public override void Sync(IStateSync sync) => _settings = HaulLoadSettings.Sync(sync, _settings);
    }
    // Alternate authored engine-side rule, without editing the existing dispatch owner.
    public sealed class OddHaulLoadProfileDef : Def, IHaulLoadProfile
    {
        private Action _observationProbe;
        private bool _probing;
        public int RuleFault { get; private set; }
        // Scoped negative instrumentation only; no world handle, session map or saved rule state.
        internal void Observe(Action probe, Action query)
        {
            if (_observationProbe != null) throw new InvalidOperationException("load rule probe already active");
            _observationProbe = probe; try { query(); } finally { _observationProbe = null; }
        }
        private HaulLoadProfileDeclaration _declaration;
        public HaulLoadSettings Initial => _declaration.Initial;
        protected override void ReadCore(JsonReader reader)
        {
            RuleFault = reader.Int("rule_fault", 0); if (RuleFault < 0 || RuleFault > 6) throw reader.Error("unknown load rule probe");
            _declaration = HaulLoadProfileDeclaration.Read(reader);
        }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content, Id);
        public Code CheckLoad(HaulLoadContext context, HaulLoadSettings settings)
        {
            if (_observationProbe != null && !_probing)
            { _probing = true; try { _observationProbe(); } finally { _probing = false; } }
            return !settings.Enabled ? HaulLoadPolicyCodes.Disabled : settings.EmergencyBypass && context.Emergency || context.WaitAgeTicks >= context.Time.Ticks(settings.MaximumWait) ||
                context.Amount >= settings.MinimumLoad.AmountOf(context.Resource) && context.Amount.Raw % Fix.FromInt(2).Raw != 0
                ? Code.None : new Code("proof.odd-load");
        }
    }
}
