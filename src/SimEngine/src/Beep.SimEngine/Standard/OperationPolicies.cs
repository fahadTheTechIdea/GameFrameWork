using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum OperationRestartPolicy : byte { Manual = 0, Automatic = 1 }
    public interface IOperationPolicyDefinition : INamedCapabilityDefinition
    {
        Code Operation { get; }
        Code Condition { get; }
        IUnitDefinition Unit { get; }
        OperationPolicySettings InitialSettings { get; }
    }
    public interface IOperationPolicy : INamedEntityCapability
    {
        OperationPolicyState ReadPolicy();
        void Configure(OperationPolicySettings settings);
        void SetShutdownLatch(bool latched, long tick);
    }
    /// <summary>Optional metadata link; scheduling and live priority remain with the existing board/source.</summary>
    public interface IRecurringOperationPolicyDefinition : IRecurringPlanDefinition
    {
        Code OperationPolicy { get; }
    }
    public sealed class OperationPolicySettings
    {
        public int Criticality { get; }
        public Fix MaintenanceThreshold { get; }
        public Fix ShutdownThreshold { get; }
        public Fix RestartThreshold { get; }
        public OperationRestartPolicy Restart { get; }
        public OperationPolicySettings(int criticality, Fix maintenance, Fix shutdown, Fix restart, OperationRestartPolicy policy)
        {
            Criticality = criticality; MaintenanceThreshold = maintenance; ShutdownThreshold = shutdown;
            RestartThreshold = restart; Restart = policy;
        }
        public static bool Same(OperationPolicySettings a, OperationPolicySettings b) => a != null && b != null &&
            a.Criticality == b.Criticality && a.MaintenanceThreshold == b.MaintenanceThreshold &&
            a.ShutdownThreshold == b.ShutdownThreshold && a.RestartThreshold == b.RestartThreshold && a.Restart == b.Restart;
        public static OperationPolicySettings Read(JsonReader reader)
        {
            int criticality = reader.Int("criticality"); Fix maintenance = reader.Fix("maintenance_threshold"), shutdown = reader.Fix("shutdown_threshold"), restart = reader.Fix("restart_threshold");
            string policy = reader.String("restart_policy");
            if (policy != "manual" && policy != "automatic") throw reader.Error("explicit manual/automatic restart policy required");
            return new OperationPolicySettings(criticality, maintenance, shutdown, restart,
                policy == "manual" ? OperationRestartPolicy.Manual : OperationRestartPolicy.Automatic);
        }
        public static void Sync(IStateSync sync, ref OperationPolicySettings settings)
        {
            // Decode locals are serialization scratch, never opening gameplay settings.
            int criticality = sync.Reading ? 0 : settings.Criticality;
            Fix maintenance = sync.Reading ? Fix.Zero : settings.MaintenanceThreshold;
            Fix shutdown = sync.Reading ? Fix.Zero : settings.ShutdownThreshold, restart = sync.Reading ? Fix.Zero : settings.RestartThreshold;
            byte policy = sync.Reading ? (byte)0 : (byte)settings.Restart;
            sync.Int(ref criticality); sync.Fix(ref maintenance); sync.Fix(ref shutdown); sync.Fix(ref restart); sync.Byte(ref policy);
            if (sync.Reading) settings = new OperationPolicySettings(criticality, maintenance, shutdown, restart, (OperationRestartPolicy)policy);
        }
    }
    public readonly struct OperationPolicyState
    {
        public OperationPolicySettings Settings { get; }
        public bool ShutdownLatched { get; }
        public long ChangedTick { get; }
        public OperationPolicyState(OperationPolicySettings settings, bool latched, long changedTick)
        { Settings = settings; ShutdownLatched = latched; ChangedTick = changedTick; }
    }
    public readonly struct OperationPolicyView
    {
        public Code Policy { get; }
        public Code Operation { get; }
        public Code Condition { get; }
        public OperationPolicyState State { get; }
        public OperationControlSnapshot Control { get; }
        public ConditionSnapshot Equipment { get; }
        public bool MaintenanceDue => Equipment.Value <= State.Settings.MaintenanceThreshold;
        public bool RestartReady => Equipment.Value >= State.Settings.RestartThreshold;
        public Code Blocker { get; }
        internal long Tick { get; }
        internal OperationPolicyView(Code policy, Code operation, Code condition, OperationPolicyState state,
            OperationControlSnapshot control, ConditionSnapshot equipment, Code blocker, long tick)
        { Policy = policy; Operation = operation; Condition = condition; State = state; Control = control; Equipment = equipment; Blocker = blocker; Tick = tick; }
    }
    public static class OperationPolicyCodes
    {
        public static readonly Code NotPolicy = new Code("operation.not-policy"), Unsafe = new Code("operation.policy-unsafe"),
            MaintenanceNotDue = new Code("operation.maintenance-not-due"), PriorityRange = new Code("operation.policy-priority-range");
    }
    /// <summary>Stateless policy rules and exact commits. Providers own state; operations own enabled/target.</summary>
    public static class OperationPolicyContracts
    {
        private static IOperationPolicyDefinition Actual(World world, IOperationPolicy provider, out Code identity)
        {
            Entity entity = provider?.Entity; identity = provider?.CapabilityId ?? Code.None;
            if (world == null || entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                identity.IsNone || !ReferenceEquals(entity.Get<IOperationPolicy>(identity), provider))
                throw new ArgumentException("operation policy must be the actual named live provider");
            var definition = entity.Def.Component<IOperationPolicyDefinition>(identity);
            if (definition?.Unit == null || !ReferenceEquals(world.Content.Find<IUnitContentDefinition>(definition.Unit.Unit.Id), definition.Unit))
                throw new InvalidOperationException("operation policy unit must alias actual content");
            return definition;
        }
        public static bool ValidSettings(IOperationPolicyDefinition definition, IConditionDefinition condition, OperationPolicySettings settings)
        {
            return definition?.Unit != null && condition?.Unit != null && ReferenceEquals(definition.Unit, condition.Unit) && settings != null &&
                settings.Criticality >= 0 && (byte)settings.Restart <= (byte)OperationRestartPolicy.Automatic &&
                settings.ShutdownThreshold >= Fix.Zero && settings.MaintenanceThreshold >= settings.ShutdownThreshold &&
                settings.RestartThreshold > settings.MaintenanceThreshold && settings.RestartThreshold <= condition.Maximum &&
                UnitConversion.ValidateAmount(definition.Unit, settings.ShutdownThreshold) == UnitConversionStatus.Converted &&
                UnitConversion.ValidateAmount(definition.Unit, settings.MaintenanceThreshold) == UnitConversionStatus.Converted &&
                UnitConversion.ValidateAmount(definition.Unit, settings.RestartThreshold) == UnitConversionStatus.Converted;
        }
        public static bool ValidState(IOperationPolicyDefinition definition, IConditionDefinition condition, OperationPolicyState state)
            => ValidSettings(definition, condition, state.Settings) && state.ChangedTick >= -1 && (!state.ShutdownLatched || state.ChangedTick >= 0);
        private static OperationPolicyState ReadState(World world, IOperationPolicy provider, out IOperationPolicyDefinition definition, out Code identity)
        {
            definition = Actual(world, provider, out identity);
            var state = provider.ReadPolicy();
            var metadata = definition; Entity entity = provider.Entity;
            var condition = entity.Get<IConditionProvider>(metadata.Condition)
                ?? throw new InvalidOperationException("operation policy condition binding is unavailable");
            bool valid = EntityObservation.ReadProvider(world, condition, entity, () =>
                ValidState(metadata, entity.Def.Component<IConditionDefinition>(metadata.Condition), state));
            if (!valid || state.ChangedTick > world.Tick)
                throw new CorruptStateException("invalid operation policy settings/latch/session chronology");
            return state;
        }
        internal static Code ReadIdentity(World world, IOperationPolicy provider)
            => EntityObservation.ReadProvider(world, provider, null, () => { Actual(world, provider, out Code identity); return identity; });
        public static OperationPolicyView Read(World world, IOperationPolicy provider)
            => EntityObservation.ReadProvider(world, provider, null, () =>
        {
            var state = ReadState(world, provider, out var definition, out Code identity); Entity entity = provider.Entity;
            IConditionProvider condition = entity.Get<IConditionProvider>(definition.Condition);
            IControllableOperation operation = entity.Get<IControllableOperation>(definition.Operation);
            if (condition == null || operation == null) throw new InvalidOperationException("operation policy binding is unavailable");
            var equipment = ConditionContracts.Read(world, condition);
            var control = OperationContracts.ReadControl(world, operation);
            if (state.ShutdownLatched && control.Enabled) throw new CorruptStateException("latched policy cannot own an enabled operation");
            Code blocker = equipment.Value <= state.Settings.ShutdownThreshold || !control.Enabled && equipment.Value < state.Settings.RestartThreshold
                ? OperationPolicyCodes.Unsafe : Code.None;
            if (equipment.Value <= state.Settings.ShutdownThreshold && control.Enabled || state.ShutdownLatched &&
                equipment.Value >= state.Settings.RestartThreshold && state.Settings.Restart == OperationRestartPolicy.Automatic)
            {
                bool enable = equipment.Value >= state.Settings.RestartThreshold;
                Code refusal = OperationContracts.Check(world, operation, enable, control.Target);
                if (!refusal.IsNone) blocker = refusal;
            }
            return new OperationPolicyView(identity, definition.Operation, definition.Condition, state, control, equipment, blocker, world.Tick);
        });
        public static Code CheckConfigure(World world, PlayerId player, Entity entity, Code policy, OperationPolicySettings settings)
        {
            if (world == null) return Codes.NotFound;
            world.RequireHealthyUpdate();
            if (entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                entity.Owner != player || !world.ViewFor(player).CanObserveOwned(entity)) return Codes.NotFound;
            return policy.IsNone ? Codes.InvalidArgument : CheckPolicyProvider(world, entity.Get<IOperationPolicy>(policy), settings);
        }
        private static Code CheckPolicyProvider(World world, IOperationPolicy provider, OperationPolicySettings settings)
        {
            if (provider == null) return OperationPolicyCodes.NotPolicy;
            return EntityObservation.ReadProvider(world, provider, null, () =>
            {
                Read(world, provider); var definition = Actual(world, provider, out Code identity);
                var condition = provider.Entity.Get<IConditionProvider>(definition.Condition);
                if (!EntityObservation.ReadProvider(world, condition, provider.Entity, () =>
                    ValidSettings(definition, provider.Entity.Def.Component<IConditionDefinition>(definition.Condition), settings))) return Codes.InvalidArgument;
                foreach (Component part in provider.Entity.Components)
                    if (part is IRecurringPlanSource source && !EntityObservation.ReadProvider(world, source, provider.Entity, () =>
                    {
                        var link = provider.Entity.Def.Component<IRecurringOperationPolicyDefinition>(source.CapabilityId);
                        return link == null || link.OperationPolicy != identity ||
                            RecurringPlanContracts.CheckSourcePriority(world, source, RecurringPlanContracts.ReadSchedule(world, source).Priority, settings.Criticality).IsNone;
                    })) return OperationPolicyCodes.PriorityRange;
                return Code.None;
            });
        }
        public static void Configure(World world, IOperationPolicy provider, OperationPolicySettings settings)
        {
            Code refusal = CheckPolicyProvider(world, provider, settings); if (!refusal.IsNone) throw new InvalidOperationException("operation policy refused: " + refusal);
            var before = Read(world, provider); Entity entity = provider.Entity;
            CommitState(world, provider, () => provider.Configure(settings), settings, before.State.ShutdownLatched, before.State.ChangedTick);
        }
        private static void CommitState(World world, IOperationPolicy provider, Action apply, OperationPolicySettings settings, bool latch, long changed)
        {
            Entity entity = provider.Entity; int version = entity.Version; PlayerId owner = entity.Owner; long tick = world.Tick; EntityId next = world.Entities.NextIdentity;
            var definition = EntityObservation.ReadProvider(world, provider, null, () => Actual(world, provider, out _));
            var condition = entity.Get<IConditionProvider>(definition.Condition); var operation = entity.Get<IControllableOperation>(definition.Operation);
            var equipment = ConditionContracts.Read(world, condition); var control = OperationContracts.ReadControl(world, operation);
            apply(); var after = Read(world, provider).State;
            var equipmentAfter = ConditionContracts.Read(world, condition); var controlAfter = OperationContracts.ReadControl(world, operation);
            if (!OperationPolicySettings.Same(after.Settings, settings) || after.ShutdownLatched != latch || after.ChangedTick != changed ||
                entity.Version == version || entity.Owner != owner || world.Tick != tick || world.Entities.NextIdentity != next ||
                !ReferenceEquals(world.Entities.Get(entity.Id), entity) || equipmentAfter.Value != equipment.Value || equipmentAfter.ChangedTick != equipment.ChangedTick ||
                controlAfter.Enabled != control.Enabled || controlAfter.Target != control.Target)
                throw new InvalidOperationException("policy provider did not commit exact admitted state without changing bound condition/control");
        }
        internal static void Observe(World world, IOperationPolicy provider, OperationPolicyView captured)
        {
            var current = Read(world, provider);
            if (captured.Tick != world.Tick || captured.Policy != current.Policy || !OperationPolicySettings.Same(captured.State.Settings, current.State.Settings) ||
                captured.State.ShutdownLatched != current.State.ShutdownLatched || captured.State.ChangedTick != current.State.ChangedTick ||
                captured.Equipment.Value != current.Equipment.Value || captured.Equipment.ChangedTick != current.Equipment.ChangedTick ||
                captured.Control.Enabled != current.Control.Enabled || captured.Control.Target != current.Control.Target)
                throw new InvalidOperationException("captured policy observation is stale");
            bool stop = current.Control.Enabled && current.Equipment.Value <= current.State.Settings.ShutdownThreshold;
            bool restart = current.State.ShutdownLatched && current.RestartReady && current.State.Settings.Restart == OperationRestartPolicy.Automatic;
            if (!stop && !restart) return;
            var operation = provider.Entity.Get<IControllableOperation>(current.Operation);
            Code refusal = OperationContracts.Commit(world, operation, restart, current.Control.Target);
            if (!refusal.IsNone) return;
            CommitState(world, provider, () => provider.SetShutdownLatch(stop, world.Tick), current.State.Settings, stop, world.Tick);
            Read(world, provider);
        }
        internal static Code CheckControl(World world, IControllableOperation operation, bool enabled)
        {
            Code operationId = OperationContracts.ReadIdentity(world, operation);
            foreach (Component part in operation.Entity.Components)
                if (part is IOperationPolicy policy)
                {
                    var value = Read(world, policy);
                    if (value.Operation == operationId && enabled &&
                        (value.Control.Enabled ? value.Equipment.Value <= value.State.Settings.ShutdownThreshold : !value.RestartReady)) return OperationPolicyCodes.Unsafe;
                }
            return Code.None;
        }
        internal static void AcknowledgeControl(World world, IControllableOperation operation)
        {
            Code operationId = OperationContracts.ReadIdentity(world, operation);
            foreach (Component part in operation.Entity.Components)
                if (part is IOperationPolicy policy)
                {
                    var observed = EntityObservation.ReadProvider(world, policy, null, () =>
                    {
                        var state = ReadState(world, policy, out var definition, out _);
                        return (State: state, Operation: definition.Operation);
                    });
                    if (observed.Operation != operationId) continue;
                    // Explicit intent clears the policy latch before validating the resulting control pair.
                    var state = observed.State;
                    if (state.ShutdownLatched) CommitState(world, policy, () => policy.SetShutdownLatch(false, world.Tick), state.Settings, false, world.Tick);
                    Read(world, policy);
                }
        }
        internal static void ValidateDefinition(World world, EntityDef entity, IComponentDefinition part, IOperationPolicyDefinition definition)
        {
            var condition = entity.Component<IConditionDefinition>(definition.Condition);
            var operationPart = entity.Component<IOperationDefinition>(definition.Operation);
            if (!typeof(IOperationPolicy).IsAssignableFrom(part.ComponentType) || !ValidSettings(definition, condition, definition.InitialSettings) ||
                !ReferenceEquals(entity.Component<IOperationPolicyDefinition>(definition.CapabilityId), definition) ||
                !entity.HasCapability<IConditionProvider>(definition.Condition) ||
                operationPart == null || !entity.HasCapability<IControllableOperation>(definition.Operation) ||
                !ReferenceEquals(world.Content.Find<IUnitContentDefinition>(definition.Unit.Unit.Id), definition.Unit))
                throw new ContentException(entity.Id + ": policy requires actual named controllable operation, condition and exact unit bands");
            var operations = new HashSet<Code>();
            foreach (ComponentDef candidate in entity.Components)
                if (candidate is IOperationPolicyDefinition policy && !operations.Add(policy.Operation))
                    throw new ContentException(entity.Id + ": multiple policies cannot control the same operation");
        }
    }
    /// <summary>Sole JSON policy metadata reader/resolver; no live policy state or evaluator.</summary>
    public sealed class OperationPolicyDeclaration
    {
        private readonly string _unit;
        public Code CapabilityId { get; }
        public Code Operation { get; }
        public Code Condition { get; }
        public IUnitDefinition Unit { get; private set; }
        public OperationPolicySettings InitialSettings { get; }
        private OperationPolicyDeclaration(JsonReader reader)
        {
            string id = reader.String("id"), operation = reader.String("operation"), condition = reader.String("condition");
            try { ContractIdentity.ValidateId(id, "operation policy"); ContractIdentity.ValidateId(operation, "policy operation"); ContractIdentity.ValidateId(condition, "policy condition"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id); Operation = new Code(operation); Condition = new Code(condition); _unit = reader.String("unit");
            InitialSettings = OperationPolicySettings.Read(reader);
        }
        public static OperationPolicyDeclaration Read(JsonReader reader)
            => new OperationPolicyDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Unit != null) throw new InvalidOperationException("policy declaration resolves once");
            Unit = content.Def<IUnitContentDefinition>(_unit);
        }
    }
    public sealed class OperationPolicyDef : ComponentDef<OperationPolicy>, IOperationPolicyDefinition
    {
        private OperationPolicyDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Operation => _declaration.Operation;
        public Code Condition => _declaration.Condition;
        public IUnitDefinition Unit => _declaration.Unit;
        public OperationPolicySettings InitialSettings => _declaration.InitialSettings;
        protected override void ReadCore(JsonReader reader) => _declaration = OperationPolicyDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class OperationPolicy : Component, IOperationPolicy
    {
        private OperationPolicySettings _settings;
        private bool _latched;
        private long _changed = -1;
        private IOperationPolicyDefinition Definition => (IOperationPolicyDefinition)base.Def;
        public Code CapabilityId => Definition.CapabilityId;
        public override void OnCreate() { _settings = Definition.InitialSettings; _latched = false; _changed = -1; }
        public OperationPolicyState ReadPolicy() => new OperationPolicyState(_settings, _latched, _changed);
        public void Configure(OperationPolicySettings settings) { _settings = settings; Entity.Touch(); }
        public void SetShutdownLatch(bool latched, long tick) { _latched = latched; _changed = tick; Entity.Touch(); }
        public override void Sync(IStateSync sync)
        {
            OperationPolicySettings settings = _settings; bool latched = _latched; long changed = _changed;
            OperationPolicySettings.Sync(sync, ref settings); sync.Bool(ref latched); sync.Long(ref changed);
            if (!OperationPolicyContracts.ValidState(Definition, Entity.Def.Component<IConditionDefinition>(Definition.Condition), new OperationPolicyState(settings, latched, changed)))
                throw new CorruptStateException("invalid saved operation policy");
            if (sync.Reading) { _settings = settings; _latched = latched; _changed = changed; }
        }
    }
}
