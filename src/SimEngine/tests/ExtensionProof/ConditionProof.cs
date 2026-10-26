using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent game-side owner, compiled against public contracts only.</summary>
    public sealed class ProofConditionDef : ComponentDef<ProofCondition>, IConditionDefinition, IAutomationSignalDefinition
    {
        private ConditionDeclaration _declaration;
        internal Action<string> Observe;
        private Code _identity; private IUnitDefinition _resolvedUnit;
        public Code CapabilityId { get { Observe?.Invoke("condition.identity"); return _identity; } }
        public IUnitDefinition Unit { get { Observe?.Invoke("condition.unit"); return _resolvedUnit; } }
        public Fix Maximum => _declaration.Maximum;
        public Fix Initial => _declaration.Initial;
        public int Fault { get; private set; }
        public int SignalSchemaVersion => 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings { get; } = Array.AsReadOnly(new[] {
            new AutomationMetricSchema(new Code("condition.value"), AutomationSignalKind.Number, new Code("unit.condition")) });
        protected override void ReadCore(JsonReader reader)
        {
            _declaration = ConditionDeclaration.Read(reader); _identity = _declaration.CapabilityId; Fault = reader.Int("fault");
            if (Fault < 0 || Fault > 5) throw reader.Error("invalid proof condition fault");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _declaration.Resolve(content); _resolvedUnit = _declaration.Unit;
        }
    }
    public sealed class ProofCondition : Component<ProofConditionDef>, IConditionChange, IAutomationSignalSource
    {
        private Fix _value;
        private long _changed = -1;
        internal Action<string> Observe;
        internal void MutateHiddenProbe() => _changed = _changed == -1 ? 0 : checked(_changed + 1);
        public Code CapabilityId => Def.CapabilityId;
        public override void OnCreate() { _value = Def.Initial; _changed = -1; }
        public ConditionSnapshot ReadCondition()
        {
            Observe?.Invoke("condition.state"); if (Def.Fault == 1) Entity.Touch();
            return new ConditionSnapshot(CapabilityId, Def.Fault == 5 ? new UnitId("unit.wrong") : Def.Unit.Unit, _value, Def.Maximum, _changed);
        }
        public Code CheckChange(World world, ConditionChange change)
        {
            Observe?.Invoke("condition.check"); if (Def.Fault == 2) Entity.Touch();
            return ConditionContracts.CheckQuantity(Def, ReadCondition(), change);
        }
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            var condition = ConditionContracts.Read(world, this);
            return signal.Metric.Id == "condition.value" ? signal.Number(condition.Value, new Code(condition.Unit.Id))
                : signal.Unavailable(OperationCodes.SignalUnavailable);
        }
        public void ApplyChange(World world, ConditionChange change)
        {
            if (Def.Fault == 3) return;
            if (!CheckChange(world, change).IsNone) throw new InvalidOperationException("proof condition refused");
            _value = ConditionContracts.ValueAfter(ReadCondition(), change);
            _changed = Def.Fault == 4 ? checked(world.Tick + 1) : world.Tick; Entity.Touch();
        }
        public override void Sync(IStateSync sync)
        {
            Fix value = _value; long changed = _changed; sync.Fix(ref value); sync.Long(ref changed);
            if (value < Fix.Zero || value > Def.Maximum || changed < -1 || changed == -1 && value != Def.Initial)
                throw new CorruptStateException("invalid proof condition");
            if (sync.Reading) { _value = value; _changed = changed; }
        }
    }
}
