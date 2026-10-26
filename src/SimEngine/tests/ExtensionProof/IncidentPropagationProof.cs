using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent authored eligibility: no tile API, scheduler, incident state or exposure book.
    public sealed class PermitPropagationRuleDef : Def, IIncidentPropagationRule
    {
        private IncidentPropagationPolicyDeclaration _policy;
        public GameDuration Interval => _policy.Interval;
        public GameDuration Delay => _policy.Delay;
        public Code TargetSource => _policy.TargetSource;
        public Code TargetKind => _policy.TargetKind;
        public bool CrossCompany => _policy.CrossCompany;
        public int FaultMode { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _policy = IncidentPropagationPolicyDeclaration.Read(reader); FaultMode = reader.Int("fault_mode");
            if (FaultMode < 0 || FaultMode > 2) throw reader.Error("unknown proof fault mode");
        }
        protected override void ResolveCore(ContentResolver content) => _policy.Resolve(content);
        public void ValidateSource(EntityDef source) => _policy.ValidateSource(source);
        public void ValidateBinding(World world) => _policy.ValidateBinding(world, Id);
        public IncidentExposureObservation Observe(IncidentPropagationContext context, Entity target)
        {
            if (FaultMode == 1) return default;
            if (FaultMode == 2) { target.Touch(); return IncidentExposureObservation.Ready; }
            Code blocker = _policy.CheckConditions(context, target, Id);
            if (!blocker.IsNone) return IncidentExposureObservation.Refused(blocker);
            return target.Get<IWorkPermit>()?.Enabled == true ? IncidentExposureObservation.Ready
                : IncidentExposureObservation.Refused(new Code("proof.propagation.permit"));
        }
    }
    public sealed class ProofPropagationEmitterDef : ComponentDef<ProofPropagationEmitter>, IIncidentPropagationDefinition
    {
        private IncidentPropagationBindingDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Source => _declaration.Source;
        public IIncidentPropagationRule Rule => _declaration.Rule;
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentPropagationBindingDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public void ValidateBinding(World world) => IncidentPropagationContracts.ValidateBinding(world, this);
    }
    public sealed class ProofPropagationEmitter : Component<ProofPropagationEmitterDef>, IIncidentPropagationEmitter
    {
        public Code CapabilityId => Def.CapabilityId;
        public Code Source => Def.Source;
        public IIncidentPropagationRule Rule => Def.Rule;
    }
}
