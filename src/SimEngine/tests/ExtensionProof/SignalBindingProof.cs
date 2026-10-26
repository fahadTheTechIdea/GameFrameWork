using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public enum OperationBindingFault { Exception, PauseClock, AllocateIdentity, HiddenState }
    // Authored source contract with transient read probes; schema/controls remain original owners.
    public sealed class MetadataSignalDef : ComponentDef<MetadataSignalSource>, IOperationDefinition, IAutomationSignalDefinition
    {
        private string _fault; private bool _renamed; private AutomationSignalProviderSchema _foreign;
        private string _operationFault; private OperationSettings _settings, _foreignControls; private OperationProviderSchema _foreignOperation;
        internal Action Observe;
        internal Action<string> ObserveOperation;
        public int OperationSchemaReads { get; private set; }
        public int SchemaReads { get; private set; }
        public int IdentityReads { get; private set; }
        public int OperationSchemaVersion => 1;
        public int SignalSchemaVersion => 1;
        public OperationSettings ControlSettings { get { ObserveOperation?.Invoke("settings"); return _operationFault == "controls-null" ? null : _foreignControls ?? _settings; } }
        public new OperationProviderSchema OperationSchema { get { OperationSchemaReads++; ObserveOperation?.Invoke("schema"); return _operationFault == "schema-null" ? null : _foreignOperation ?? base.OperationSchema; } }
        public Code CapabilityId { get { IdentityReads++; return _renamed ? new Code("changed.source") : _settings.Id; } }
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => Array.AsReadOnly(new[] {
            new AutomationMetricSchema(new Code("probe.level"), AutomationSignalKind.Number, new Code("unit.level")),
            new AutomationMetricSchema(new Code("operation.enabled"), AutomationSignalKind.Boolean, new Code("unit.boolean")) });
        public new AutomationSignalProviderSchema SignalSchema
        {
            get
            {
                SchemaReads++; Observe?.Invoke();
                return _fault == "null" ? null : _foreign ?? base.SignalSchema;
            }
        }
        protected override void ReadCore(JsonReader reader)
        { _fault = reader.String("schema_fault", "none"); _operationFault = reader.String("operation_fault", "none"); _settings = OperationSettings.Read(reader.Child("operation")); }
        protected override void ResolveCore(ContentResolver content)
        {
            if (_fault == "foreign") _foreign = content.Def<EntityDef>("contract.factory").Component<IAutomationSignalDefinition>(new Code("contract.process")).SignalSchema;
            if (_fault == "identity") _renamed = true;
            if (_operationFault == "schema-foreign") _foreignOperation = content.Def<EntityDef>("contract.factory").Component<IOperationDefinition>(new Code("contract.process")).OperationSchema;
            if (_operationFault == "controls-foreign") _foreignControls = content.Def<EntityDef>("contract.factory").Component<IOperationDefinition>(new Code("contract.process")).ControlSettings;
        }
    }
    public sealed class MetadataSignalSource : Component<MetadataSignalDef>, IControllableOperation, IAutomationSignalSource
    {
        private bool _enabled, _hiddenProbe; private Fix _target;
        internal Action<string> ObserveOperation;
        internal string Malformed;
        internal void MutateHiddenProbe() => _hiddenProbe = !_hiddenProbe;
        public int IdentityReads { get; private set; }
        public Code CapabilityId { get { IdentityReads++; ObserveOperation?.Invoke("identity"); return Malformed == "identity" ? Code.None : Def.CapabilityId; } }
        public override void OnCreate() { _enabled = Def.ControlSettings.Initial.Enabled; _target = Def.ControlSettings.Initial.Target; }
        public OperationControlSnapshot ReadControl() { ObserveOperation?.Invoke("control"); return Malformed == "control" ? default : Def.ControlSettings.Snapshot(_enabled, _target); }
        public OperationSnapshot ReadOperation(World world) { ObserveOperation?.Invoke("status"); return Malformed == "status" ? default : new OperationSnapshot(_enabled ? OperationStatus.Running : OperationStatus.Disabled,
            _enabled ? Code.None : OperationCodes.Disabled); }
        public Code CheckControl(bool enabled, Fix target) { ObserveOperation?.Invoke("check"); return Def.ControlSettings.Check(target); }
        public void Configure(bool enabled, Fix target)
        {
            if (!CheckControl(enabled, target).IsNone) throw new InvalidOperationException("invalid authored source control");
            _enabled = enabled; _target = target; Entity.Touch();
        }
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
            => signal.Metric == new Code("probe.level") ? signal.Number(_target, new Code("unit.level"))
                : signal.Metric == new Code("operation.enabled") ? signal.Boolean(_enabled) : signal.Unavailable(OperationCodes.SignalUnavailable);
        public override void Sync(IStateSync sync) { sync.Bool(ref _enabled); sync.Fix(ref _target); sync.Bool(ref _hiddenProbe); }
    }
    public sealed class SignalBindingModule : GameModule
    {
        private World _world; private MetadataSignalDef _definition;
        public int Reads { get; private set; }
        public int SchemaReads => _definition.SchemaReads;
        public override string Id => "proof.signal-bindings";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(OperationsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Component<MetadataSignalDef>("metadata_source", 2);
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            _definition = (MetadataSignalDef)composer.Content.Get<EntityDef>("contract.factory").Component<IAutomationSignalDefinition>(new Code("metadata.sensor"));
        }
        public int OperationSchemaReads => _definition.OperationSchemaReads;
        private MetadataSignalSource OperationSource()
        {
            foreach (var entity in _world.Entities.All) if (entity.Def.Id == "contract.factory")
                return entity.Get<MetadataSignalSource>(new Code("metadata.sensor"));
            throw new InvalidOperationException("missing actual proof operation");
        }
        public void ArmMalformed(string projection) => OperationSource().Malformed = projection;
        public void ArmOperation(string point, OperationBindingFault mode)
        {
            var source = OperationSource();
            Action<string> observe = read =>
            {
                if (read != point || Reads != 0) return; Reads++;
                if (mode == OperationBindingFault.Exception) throw new InvalidOperationException("proof.operation.metadata.failure");
                if (mode == OperationBindingFault.PauseClock) ((GameClock)_world.Clock).Pause();
                if (mode == OperationBindingFault.AllocateIdentity) _world.Spawn(_world.Content.Get<EntityDef>("source"), new PlayerId(0));
                if (mode == OperationBindingFault.HiddenState) source.MutateHiddenProbe();
            };
            _definition.ObserveOperation = observe; source.ObserveOperation = observe;
        }
        public bool PureNamedLookup()
        {
            foreach (var entity in _world.Entities.All)
                if (entity.Def.Id == "contract.factory")
                {
                    var source = entity.Get<MetadataSignalSource>(new Code("metadata.sensor"));
                    int nativeReads = source.IdentityReads, authoredReads = _definition.IdentityReads;
                    for (int i = 0; i < 32; i++)
                        if (!ReferenceEquals(source, entity.Get<IAutomationSignalSource>(new Code("metadata.sensor"))) ||
                            !ReferenceEquals(_definition, entity.Def.Component<IAutomationSignalDefinition>(new Code("metadata.sensor"))) ||
                            !entity.Def.HasCapability<IAutomationSignalSource>(new Code("metadata.sensor")) ||
                            entity.Get<ITransformer>(new Code("metadata.sensor")) != null ||
                            entity.Get<IAutomationSignalSource>(new Code("absent")) != null) return false;
                    return source.IdentityReads == nativeReads && _definition.IdentityReads == authoredReads;
                }
            return false;
        }
        public void Arm(ResourceSnapshotFault mode)
        {
            _definition.Observe = () =>
            {
                if (Reads != 0) return;
                Reads++;
                if (mode == ResourceSnapshotFault.Exception) throw new InvalidOperationException("proof.signal.schema.failure");
                if (mode == ResourceSnapshotFault.PauseClock) ((GameClock)_world.Clock).Pause();
                if (mode == ResourceSnapshotFault.AllocateIdentity) _world.Spawn(_world.Content.Get<EntityDef>("source"), new PlayerId(0));
            };
        }
    }
}
