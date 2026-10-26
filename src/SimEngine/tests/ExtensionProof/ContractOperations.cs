using System;
using System.Collections.Generic;
using Beep.SimEngine;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class DialOperationDef : ComponentDef<DialOperation>, IOperationDefinition, IAutomationSignalDefinition
    {
        public int OperationSchemaVersion => 1;
        public int SignalSchemaVersion => 1;
        public OperationSettings ControlSettings => Operation;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => Array.AsReadOnly(new[]
        {
            new AutomationMetricSchema(new Code("dial.accumulated"), AutomationSignalKind.Number, new Code("unit.units")),
            new AutomationMetricSchema(new Code("operation.enabled"), AutomationSignalKind.Boolean, new Code("unit.boolean"))
        });
        public OperationSettings Operation { get; private set; }
        public Code CapabilityId => Operation.Id;
        public IEntityStateDefinition Working { get; private set; }
        public IEntityStateDefinition Stopped { get; private set; }
        protected override void ReadCore(JsonReader r) => Operation = OperationSettings.Read(r.Child("operation"));
        protected override void ResolveCore(ContentResolver c)
        { Working = c.Def<IEntityStateDefinition>("working"); Stopped = c.Def<IEntityStateDefinition>("stopped"); }
    }

    /// <summary>An unrelated engine-side operation, not a producer wrapper or client simulation.</summary>
    public sealed class DialOperation : Component<DialOperationDef>, IControllableOperation, IAutomationSignalSource, IEntityStateSource
    {
        private bool _enabled;
        private Fix _target, _accumulated;
        public Code CapabilityId => Def.Operation.Id;
        public override void OnCreate() { _enabled = Def.Operation.Initial.Enabled; _target = Def.Operation.Initial.Target; }
        public OperationControlSnapshot ReadControl() => Def.Operation.Snapshot(_enabled, _target);
        public Code CheckControl(bool enabled, Fix target) => Def.Operation.Check(target);
        public void Configure(bool enabled, Fix target)
        {
            if (!CheckControl(enabled, target).IsNone) throw new ArgumentOutOfRangeException(nameof(target));
            _enabled = enabled; _target = target; Entity.Touch();
        }
        public void Advance() { if (_enabled) _accumulated += _target; }
        public OperationSnapshot ReadOperation(World world)
            => !_enabled ? new OperationSnapshot(OperationStatus.Disabled, OperationCodes.Disabled)
                : new OperationSnapshot(_target.IsZero ? OperationStatus.Idle : OperationStatus.Running, Code.None);
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
            => signal.Metric.Id == "dial.accumulated" ? signal.Number(_accumulated, new Code("unit.units"))
                : signal.Metric.Id == "operation.enabled" ? signal.Boolean(_enabled)
                : signal.Unavailable(OperationCodes.SignalUnavailable);
        public IEntityStateDefinition GetState(EntityUpdateContext context)
            => !_enabled ? Def.Stopped : _target.IsZero ? null : Def.Working;
        public override void Sync(IStateSync sync)
        {
            sync.Bool(ref _enabled); sync.Fix(ref _target); sync.Fix(ref _accumulated);
            if (sync.Reading && !CheckControl(_enabled, _target).IsNone) throw new CorruptStateException("dial target violates authored bounds");
        }
    }

    public sealed class DialOperationSystem : GameSystem
    {
        public override int StateSchemaVersion => 1;
        public override string Id => "colony.dial-operations";
        public override void Update(World world, TickInfo tick)
        {
            ComponentList<DialOperation> operations = world.Entities.Components<DialOperation>();
            for (int i = 0; i < operations.Count; i++) if (operations[i].Entity.Alive) operations[i].Advance();
        }
    }
}
