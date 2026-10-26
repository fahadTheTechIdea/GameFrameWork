using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ProofOperationPolicyDef : ComponentDef<ProofOperationPolicy>, IOperationPolicyDefinition
    {
        private OperationPolicyDeclaration _declaration;
        internal System.Action<string> Observe;
        public Code CapabilityId { get { Observe?.Invoke("policy.identity"); return _declaration.CapabilityId; } }
        public Code Operation { get { Observe?.Invoke("policy.operation"); return _declaration.Operation; } }
        public Code Condition => _declaration.Condition;
        public IUnitDefinition Unit { get { Observe?.Invoke("policy.unit"); return _declaration.Unit; } }
        public OperationPolicySettings InitialSettings { get { Observe?.Invoke("policy.settings"); return _declaration.InitialSettings; } }
        public int Fault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _declaration = OperationPolicyDeclaration.Read(reader); Fault = reader.Int("fault");
            if (Fault < 0 || Fault > 5) throw reader.Error("explicit policy proof fault required");
        }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    // Independent public-contract state owner. No policy rules, work, controls or scheduler copied here.
    public sealed class ProofOperationPolicy : Component<ProofOperationPolicyDef>, IOperationPolicy
    {
        private OperationPolicySettings _settings;
        private bool _latched;
        private long _changed = -1;
        internal System.Action<string> Observe;
        internal void MutateHiddenProbe() => _changed = _changed == -1 ? 0 : checked(_changed + 1);
        public Code CapabilityId => Def.CapabilityId;
        public override void OnCreate() { _settings = Def.InitialSettings; _latched = false; _changed = -1; }
        public OperationPolicyState ReadPolicy()
        { Observe?.Invoke("policy.state"); if (Def.Fault == 1) Entity.Touch(); return new OperationPolicyState(_settings, _latched, _changed); }
        public void Configure(OperationPolicySettings settings)
        {
            if (Def.Fault == 2) return;
            if (Def.Fault == 5) { var operation = Entity.Get<IControllableOperation>(Def.Operation); operation.Configure(false, operation.ReadControl().Target); }
            _settings = settings; Entity.Touch();
        }
        public void SetShutdownLatch(bool latched, long tick)
        { if (Def.Fault == 3) return; _latched = latched; _changed = Def.Fault == 4 ? checked(tick + 1) : tick; Entity.Touch(); }
        public override void Sync(IStateSync sync)
        {
            OperationPolicySettings.Sync(sync, ref _settings); sync.Bool(ref _latched); sync.Long(ref _changed);
        }
    }
}
