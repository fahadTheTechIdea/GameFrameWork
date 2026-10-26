using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class AutomationProbeDef : ComponentDef<AutomationProbe>, IOperationDefinition, IAutomationSignalDefinition
    {
        internal Action ObserveIdentity; // Negative-only metadata hook, never normal gameplay.
        public int OperationSchemaVersion => 1;
        public int SignalSchemaVersion => 1;
        public OperationSettings ControlSettings { get; private set; }
        public Code CapabilityId { get { ObserveIdentity?.Invoke(); return ControlSettings.Id; } }
        public Fix InitialLevel { get; private set; }
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => Array.AsReadOnly(new[] {
            new AutomationMetricSchema(new Code("probe.level"), AutomationSignalKind.Number, new Code("unit.level")),
            new AutomationMetricSchema(new Code("operation.enabled"), AutomationSignalKind.Boolean, new Code("unit.boolean")) });
        protected override void ReadCore(JsonReader reader)
        { ControlSettings = OperationSettings.Read(reader.Child("operation")); InitialLevel = reader.Fix("level"); }
    }
    // Engine-side public-contract provider. Tests only submit controls/environment requests or inject faults.
    public sealed class AutomationProbe : Component<AutomationProbeDef>, IControllableOperation, IAutomationSignalSource
    {
        private bool _enabled, _available = true;
        private Fix _target, _level;
        public int FaultMode;
        public Code CapabilityId => Def.CapabilityId;
        public override void OnCreate()
        { _enabled = Def.ControlSettings.Initial.Enabled; _target = Def.ControlSettings.Initial.Target; _level = Def.InitialLevel; }
        public OperationControlSnapshot ReadControl() => Def.ControlSettings.Snapshot(_enabled, _target);
        public OperationSnapshot ReadOperation(World world) => _enabled
            ? new OperationSnapshot(OperationStatus.Running, Code.None) : new OperationSnapshot(OperationStatus.Disabled, OperationCodes.Disabled);
        public Code CheckControl(bool enabled, Fix target)
        { if (FaultMode == 4) Entity.Touch(); return Def.ControlSettings.Check(target); }
        public void Configure(bool enabled, Fix target)
        { if (FaultMode != 3) { _enabled = enabled; _target = target; Entity.Touch(); } }
        public void SetLevel(Fix level, bool available) { _level = level; _available = available; Entity.Touch(); }
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            if (FaultMode == 2) Entity.Touch();
            if (FaultMode == 5) _level += Fix.One; // Deliberately persisted mutation without a revision.
            if (FaultMode == 6) throw new InvalidOperationException("probe signal callback failed");
            if (FaultMode == 7) return new AutomationSignalSnapshot("probe.wrong-signal", signal.Kind, signal.Unit, Fix.One, Code.None);
            if (!_available) return signal.Unavailable(new Code("probe.sensor-offline"));
            if (FaultMode == 1) return new AutomationSignalSnapshot(signal.Id, signal.Kind, new Code("unit.wrong"), Fix.One, Code.None);
            return signal.Metric.Id == "probe.level" ? signal.Number(_level, new Code("unit.level"))
                : signal.Metric.Id == "operation.enabled" ? signal.Boolean(_enabled) : signal.Unavailable(OperationCodes.SignalUnavailable);
        }
        public override void Sync(IStateSync sync)
        { sync.Bool(ref _enabled); sync.Bool(ref _available); sync.Fix(ref _target); sync.Fix(ref _level); }
    }
    public sealed class SetAutomationProbeCommand : Command, IEntityControlCommand
    {
        public EntityId Entity;
        public EntityId TargetEntity => Entity;
        public Fix Level;
        public bool Available;
        public Action SyncProbe;
        public override void Sync(IStateSync sync) { SyncProbe?.Invoke(); sync.Entity(ref Entity); sync.Fix(ref Level); sync.Bool(ref Available); }
    }
    public sealed class SetAutomationProbeHandler : CommandHandler<SetAutomationProbeCommand>
    {
        public Action<SetAutomationProbeCommand> CheckProbe;
        public Action<World, SetAutomationProbeCommand> AppliedProbe;
        public Code RefusalProbe;
        protected override Code Check(World world, SetAutomationProbeCommand command)
        {
            CheckProbe?.Invoke(command);
            if (!RefusalProbe.IsNone) return RefusalProbe;
            Entity entity = world.Entities.Get(command.Entity);
            return entity == null ? Codes.NotFound : entity.Owner != command.Player ? Codes.NotOwned
                : entity.Get<AutomationProbe>() == null ? Codes.InvalidArgument : Code.None;
        }
        protected override void Apply(World world, SetAutomationProbeCommand command, CommandContext context)
        {
            world.Entities.Get(command.Entity).Get<AutomationProbe>().SetLevel(command.Level, command.Available);
            AppliedProbe?.Invoke(world, command);
        }
    }
    public sealed class InverseControlRuleDef : Def, IControlRule
    {
        private string _signal;
        private string _recovery;
        private IControlInputRecoveryPolicy _inputRecovery;
        public IControlInputRecoveryPolicy InputRecovery => _inputRecovery;
        private string _actionPolicy;
        private IControlActionPolicy _actions;
        public IControlActionPolicy ActionPolicy => _actions;
        private IReadOnlyList<ControlSignalInput> _inputs;
        private IReadOnlyDictionary<string, bool> _initial;
        private GameDuration _dwell, _cooldown;
        private ControlAction _true, _false;
        private bool _latch;
        public IReadOnlyList<ControlSignalInput> Inputs => _inputs;
        public IReadOnlyDictionary<string, IControlThresholdDefinition> Thresholds { get; }
            = new System.Collections.ObjectModel.ReadOnlyDictionary<string, IControlThresholdDefinition>(new Dictionary<string, IControlThresholdDefinition>());
        public IReadOnlyDictionary<string, bool> InitialState => _initial;
        public bool InitialLatch => _latch;
        public GameDuration Dwell => _dwell;
        public GameDuration Cooldown => _cooldown;
        public ControlAction WhenTrue => _true;
        public ControlAction WhenFalse => _false;
        public ControlRuleArbitration Arbitration => ControlRuleArbitration.Exclusive;
        protected override void ReadCore(JsonReader reader)
        {
            _signal = reader.String("signal");
            _recovery = reader.String("input_recovery");
            _actionPolicy = reader.String("action_policy");
            JsonValue initialValue = reader.Value("initial"), latch = reader.Value("initial_latch");
            if (initialValue == null || initialValue.Kind != JsonKind.Bool || latch == null || latch.Kind != JsonKind.Bool)
                throw reader.Error("explicit initial state and latch required");
            if (reader.String("arbitration") != "exclusive") throw reader.Error("explicit exclusive arbitration required");
            bool initial = initialValue.Bool; _latch = latch.Bool;
            _initial = new System.Collections.ObjectModel.ReadOnlyDictionary<string, bool>(new Dictionary<string, bool> { ["inverse"] = initial });
            _dwell = reader.Child("dwell").RationalDuration(); _cooldown = reader.Child("cooldown").RationalDuration();
            _true = ControlAction.Read(reader.Child("when_true")); _false = ControlAction.Read(reader.Child("when_false"));
        }
        protected override void ResolveCore(ContentResolver content)
        {
            AutomationSignalDef signal = content.Def<AutomationSignalDef>(_signal);
            _inputRecovery = content.Def<IControlInputRecoveryPolicy>(_recovery);
            _actions = content.Def<IControlActionPolicy>(_actionPolicy);
            if (signal.Kind != AutomationSignalKind.Boolean) throw content.Fail("inverse provider requires a boolean signal");
            _inputs = Array.AsReadOnly(new[] { new ControlSignalInput("sample", signal) });
        }
        public ControlEvaluation Evaluate(ControlEvaluationContext context)
        {
            bool condition = context.Signals["sample"].Value == Fix.Zero;
            return new ControlEvaluation(condition, new Dictionary<string, bool> { ["inverse"] = condition }, Code.None);
        }
    }
    public sealed class AutomationProofModule : GameModule
    {
        public override string Id => "proof.automation";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(AutomationModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<AutomationProbeDef>("automation_probe", 1); types.Section<InverseControlRuleDef>("inverse_control_rules");
            types.Section<ConditionalInputRecoveryPolicyDef>("conditional_input_recovery_policies");
            types.Section<BandControlRuleDef>("band_control_rules");
            types.Section<ProbeControlActionPolicyDef>("probe_control_actions");
        }
        public override void Compose(WorldComposer composer)
            => composer.Commands.Register("proof.set-automation-level", new SetAutomationProbeHandler());
    }
    // Independent content targeting extends the existing visibility owner; no second fog/knowledge store.
    public sealed class AutomationRuleVisibilityDef : DefinitionKnowledgeVisibilityDef<IControlRule> { }
    public sealed class AutomationSignalVisibilityDef : DefinitionKnowledgeVisibilityDef<AutomationSignalDef> { }
    public sealed class AutomationRecoveryVisibilityDef : DefinitionKnowledgeVisibilityDef<IControlInputRecoveryPolicy> { }
    public sealed class AutomationActionVisibilityDef : DefinitionKnowledgeVisibilityDef<IControlActionPolicy> { }
    public sealed class AutomationResourceVisibilityDef : DefinitionKnowledgeVisibilityDef<IResourceContentDefinition> { }
    public sealed class AutomationUnitVisibilityDef : DefinitionKnowledgeVisibilityDef<IUnitContentDefinition> { }
    public sealed class ActivityRequirementVisibilityDef : DefinitionKnowledgeVisibilityDef<IWorkRequirement> { }
    public sealed class ActivityEffectVisibilityDef : DefinitionKnowledgeVisibilityDef<IWorkEffect> { }
    public sealed class ActivitySubjectVisibilityDef : DefinitionKnowledgeVisibilityDef<IKnowledgeSubjectDefinition> { }
    public sealed class AutomationKnowledgeVisibilityModule : KnowledgeVisibilityModule
    {
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<AutomationRuleVisibilityDef>("control_rule_knowledge_visibility");
            types.Section<AutomationSignalVisibilityDef>("signal_knowledge_visibility");
            types.Section<AutomationRecoveryVisibilityDef>("input_recovery_knowledge_visibility");
            types.Section<AutomationActionVisibilityDef>("action_policy_knowledge_visibility");
            types.Section<AutomationResourceVisibilityDef>("resource_knowledge_visibility");
            types.Section<AutomationUnitVisibilityDef>("unit_knowledge_visibility");
            types.Section<ActivityRequirementVisibilityDef>("activity_requirement_knowledge_visibility");
            types.Section<ActivityEffectVisibilityDef>("activity_effect_knowledge_visibility");
            types.Section<ActivitySubjectVisibilityDef>("activity_subject_knowledge_visibility");
        }
    }
    /// <summary>Independent action interpretation; actual command handler remains the mutation owner.</summary>
    public sealed class ProbeControlActionPolicyDef : Def, IControlActionPolicy
    {
        public Action ValidateProbe;
        public ControlTargetKind TargetKind => ControlTargetKind.Entity;
        private string _command, _owner;
        public int FaultMode;
        public Action ReadProbe;
        public EntityId Redirect;
        public Command Retained;
        public Action<Command> CommandProbe;
        public string CommandId => _command;
        public string CommandOwner => _owner;
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
            => destination.HasCapability<IAutomationSignalSource>(target.Capability)
                ? action.Unit == new Code("unit.level") ? Code.None : AutomationCodes.UnitMismatch : AutomationCodes.NotSignalSource;
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => new ControlBinding(destination.Id, target.Capability);
        public IReadOnlyList<IContentDefinition> ContentDependencies => Array.Empty<IContentDefinition>();
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
        {
            ReadProbe?.Invoke(); destination.Clear();
            foreach (var component in world.Entities.Get(entity).Components)
                if (component is IAutomationSignalSource source) destination.Add(source.CapabilityId);
        }
        protected override void ReadCore(JsonReader reader)
        { _command = reader.String("command"); _owner = reader.String("owner"); }
        public void ValidateComposition(World world)
        {
            ValidateProbe?.Invoke();
            if (!world.Commands.TryGet(CommandId, out var entry) || entry.Type != typeof(SetAutomationProbeCommand)
                || entry.Owner != "proof.automation" || CommandOwner != entry.Owner)
                throw new ContentException(Id + ": probe action must select its actual normal environment command");
        }
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
        {
            ReadProbe?.Invoke();
            var source = world.Entities.Get(target.Entity)?.Get<IAutomationSignalSource>(target.Capability);
            if (FaultMode == 1) source?.Entity.Touch();
            return source == null ? AutomationCodes.NotSignalSource : action.Unit != new Code("unit.level") ? AutomationCodes.UnitMismatch : Code.None;
        }
        public Command CreateCommand(ControlActionRequest request)
        {
            ReadProbe?.Invoke();
            if (FaultMode == 2) return null;
            if (FaultMode == 3) return new ConfigureOperationCommand { Entity = request.Target.Entity };
            var command = new SetAutomationProbeCommand { Entity = FaultMode == 4 ? Redirect : request.Target.Entity,
                Level = request.Action.Target, Available = request.Action.Enabled };
            CommandProbe?.Invoke(command);
            Retained = command; return command;
        }
    }
    /// <summary>Public-contract numeric rule: the coordinator knows nothing about its comparison implementation.</summary>
    public sealed class BandControlRuleDef : Def, IControlRule
    {
        // Explicit per-proof negative hooks, never authored gameplay or persisted state.
        public Action EvaluateProbe, RecoveryMetadataProbe, TimingMetadataProbe, CooldownMetadataProbe,
            InitialStateMetadataProbe, InitialLatchMetadataProbe;
        private string _signal, _recovery;
        private Code _unit;
        private bool _above, _latch;
        private IReadOnlyList<ControlSignalInput> _inputs;
        private IReadOnlyDictionary<string, bool> _state;
        private IReadOnlyDictionary<string, IControlThresholdDefinition> _bands;
        private IControlInputRecoveryPolicy _policy;
        private string _actionPolicy;
        private IControlActionPolicy _actions;
        public IControlActionPolicy ActionPolicy => _actions;
        private GameDuration _dwell, _cooldown;
        private ControlAction _true, _false;
        public IReadOnlyList<ControlSignalInput> Inputs => _inputs;
        public IReadOnlyDictionary<string, IControlThresholdDefinition> Thresholds => _bands;
        public IReadOnlyDictionary<string, bool> InitialState { get { InitialStateMetadataProbe?.Invoke(); return _state; } }
        public IControlInputRecoveryPolicy InputRecovery { get { RecoveryMetadataProbe?.Invoke(); return _policy; } }
        public bool InitialLatch { get { InitialLatchMetadataProbe?.Invoke(); return _latch; } }
        public GameDuration Dwell { get { TimingMetadataProbe?.Invoke(); return _dwell; } }
        public GameDuration Cooldown { get { CooldownMetadataProbe?.Invoke(); return _cooldown; } }
        public ControlAction WhenTrue => _true;
        public ControlAction WhenFalse => _false;
        public ControlRuleArbitration Arbitration => ControlRuleArbitration.Exclusive;
        protected override void ReadCore(JsonReader reader)
        {
            _signal = reader.String("signal"); _recovery = reader.String("input_recovery");
            _actionPolicy = reader.String("action_policy");
            _unit = new Code(reader.String("unit"));
            if (reader.String("arbitration") != "exclusive") throw reader.Error("exclusive arbitration required");
            foreach (string key in new[] { "above", "initial", "initial_latch" })
                if (reader.Value(key)?.Kind != JsonKind.Bool) throw reader.Error("explicit boolean " + key + " required");
            _above = reader.Value("above").Bool; _latch = reader.Value("initial_latch").Bool;
            if (!reader.Has("on") || !reader.Has("off")) throw reader.Error("explicit bands required");
            _bands = new System.Collections.ObjectModel.ReadOnlyDictionary<string, IControlThresholdDefinition>(
                new Dictionary<string, IControlThresholdDefinition> { ["band"] = new ControlThresholdDefinition("level", _above,
                    new ControlThresholdBand(_unit, reader.Fix("on"), reader.Fix("off"))) });
            _state = new System.Collections.ObjectModel.ReadOnlyDictionary<string, bool>(new Dictionary<string, bool> { ["band"] = reader.Value("initial").Bool });
            _dwell = reader.Child("dwell").RationalDuration(); _cooldown = reader.Child("cooldown").RationalDuration();
            _true = ControlAction.Read(reader.Child("when_true")); _false = ControlAction.Read(reader.Child("when_false"));
        }
        protected override void ResolveCore(ContentResolver content)
        {
            var signal = content.Def<AutomationSignalDef>(_signal);
            if (signal.Kind != AutomationSignalKind.Number || signal.Unit != _unit) throw content.Fail("numeric signal unit mismatch");
            _inputs = Array.AsReadOnly(new[] { new ControlSignalInput("level", signal) });
            _policy = content.Def<IControlInputRecoveryPolicy>(_recovery);
            _actions = content.Def<IControlActionPolicy>(_actionPolicy);
        }
        public ControlEvaluation Evaluate(ControlEvaluationContext context)
        {
            EvaluateProbe?.Invoke();
            ControlThresholdBand band = context.Thresholds["band"];
            Fix value = context.Signals["level"].Value.Value, threshold = context.PreviousState["band"] ? band.Off : band.On;
            bool condition = _above ? value > threshold : value < threshold;
            return new ControlEvaluation(condition, new Dictionary<string, bool> { ["band"] = condition }, Code.None);
        }
    }
    /// <summary>Independent authored behavior plus fault instrumentation, not another scheduler or time owner.</summary>
    public sealed class ConditionalInputRecoveryPolicyDef : Def, IControlInputRecoveryPolicy
    {
        private string _input, _signal;
        private Code _blocker;
        private AutomationSignalDef _definition;
        public Action ReadProbe;
        public int Calls, FaultMode;
        protected override void ReadCore(JsonReader reader)
        {
            _input = reader.String("input"); _signal = reader.String("signal");
            try { _blocker = new Code(reader.String("blocker")); new ControlInputFailure(_input, new Code(_signal), _blocker); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        protected override void ResolveCore(ContentResolver content) => _definition = content.Def<AutomationSignalDef>(_signal);
        public ControlInputRecoveryAction Decide(ControlInputFailure failure)
        {
            Calls++; ReadProbe?.Invoke();
            if (FaultMode == 1) return (ControlInputRecoveryAction)99;
            return failure.Input == _input && failure.Signal.Id == _definition.Id && failure.Blocker == _blocker
                ? ControlInputRecoveryAction.Wait : ControlInputRecoveryAction.Suspend;
        }
    }
}
