using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public readonly struct ExecutionRateSnapshot
    {
        public ExecutionGateSnapshot Gate { get; }
        public Fix Multiplier { get; }
        public ExecutionRateSnapshot(ExecutionGateSnapshot gate, Fix multiplier) { Gate = gate; Multiplier = multiplier; }
    }
    public sealed class ExecutionRateEvaluation
    {
        public Fix Rate { get; }
        public ExecutionGateEvaluation Gates { get; }
        public IReadOnlyList<ExecutionRateSnapshot> Providers { get; }
        internal ExecutionRateEvaluation(Fix rate, ExecutionGateEvaluation gates, List<ExecutionRateSnapshot> providers)
        { Rate = rate; Gates = gates; Providers = providers.AsReadOnly(); }
    }
    internal static class ExecutionRateContracts
    {
        internal static ExecutionRateEvaluation Read<TProvider, TDefinition>(World world, Entity entity, Fix initial,
            Func<TProvider, ExecutionRateSnapshot> read)
            where TProvider : class, INamedEntityCapability where TDefinition : class, INamedCapabilityDefinition
        {
            if (initial < Fix.Zero) throw new InvalidOperationException("execution supplied negative base rate");
            Fix rate = initial; var providers = new List<ExecutionRateSnapshot>();
            var gates = ExecutionGateContracts.Read<TProvider>(world, entity, provider =>
            {
                if (entity.Def.Component<TDefinition>(provider.CapabilityId) == null)
                    throw new InvalidOperationException("execution has no declared named metadata");
                var value = read(provider);
                if (value.Multiplier < Fix.Zero) throw new InvalidOperationException("execution supplied negative multiplier");
                if (value.Gate != null && !value.Gate.Applies && value.Multiplier != Fix.One)
                    throw new InvalidOperationException("non-applicable execution must contribute the multiplier identity");
                rate *= value.Multiplier; providers.Add(value); return value.Gate;
            });
            return new ExecutionRateEvaluation(rate, gates, providers);
        }
        internal static void ValidateDefinitions<TProvider, TDefinition, TTarget>(World world)
            where TProvider : class, INamedEntityCapability where TDefinition : class, INamedCapabilityDefinition
            where TTarget : class, IEntityCapability
        {
            foreach (EntityDef entity in world.Content.All<EntityDef>()) foreach (ComponentDef part in entity.Components)
            {
                bool runtime = typeof(TProvider).IsAssignableFrom(part.ComponentType);
                if (runtime != (part is TDefinition) || runtime && !entity.HasCapability<TTarget>())
                    throw new ContentException(entity.Id + ": execution metadata/runtime must target actual " + typeof(TTarget).Name);
            }
        }
    }
    public interface IControlledExecutionDefinition : IOperationDefinition
    {
        IReadOnlyList<SignalGateCondition> Conditions { get; }
        EntityStateBindings States { get; }
    }
    /// <summary>One authoring protocol shared by independent work and motion specializations.</summary>
    public abstract class ControlledExecutionDef<T> : ComponentDef<T>, IControlledExecutionDefinition where T : Component, new()
    {
        protected abstract Code MultiplierUnit { get; }
        protected abstract bool HasTarget(EntityDef owner);
        public OperationSettings ControlSettings { get; private set; }
        public int OperationSchemaVersion => 1;
        public Code CapabilityId => ControlSettings.Id;
        public IReadOnlyList<SignalGateCondition> Conditions { get; private set; }
        public EntityStateBindings States { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            ControlSettings = OperationSettings.Read(reader.Child("operation"));
            if (ControlSettings.Initial.Unit != MultiplierUnit || ControlSettings.Initial.Minimum < Fix.Zero)
                throw reader.Error("execution requires explicit nonnegative " + MultiplierUnit + " multiplier controls");
            Conditions = SignalGateCondition.ReadAll(reader, "conditions", true);
            States = EntityStateBindings.Read(reader, "disabled");
        }
        protected override void ResolveCore(ContentResolver content)
        { foreach (var condition in Conditions) condition.Resolve(content); States = States.Resolve(content); }
        protected override void ValidateCore(EntityDef owner)
        {
            if (!HasTarget(owner)) throw new ContentException(owner.Id + ": execution requires its actual target capability");
            foreach (var condition in Conditions) condition.Validate(owner);
        }
    }
    /// <summary>One native enabled/multiplier state/control/codec owner per declared capability.</summary>
    public abstract class ControlledExecution<TDef> : Component<TDef>, IControllableOperation, IEntityStateSource
        where TDef : ComponentDef, IControlledExecutionDefinition
    {
        private bool _enabled;
        private Fix _multiplier;
        public Code CapabilityId => Def.CapabilityId;
        public override void OnCreate() { _enabled = Def.ControlSettings.Initial.Enabled; _multiplier = Def.ControlSettings.Initial.Target; }
        public ExecutionRateSnapshot ReadExecution(World world)
        {
            var gate = !_enabled ? new ExecutionGateSnapshot(CapabilityId, true, OperationCodes.Disabled, Array.Empty<ExecutionGateConditionSnapshot>())
                : SignalGateCondition.ReadGate(world, Entity, CapabilityId, true, Def.Conditions);
            return new ExecutionRateSnapshot(gate, _multiplier);
        }
        public OperationControlSnapshot ReadControl() => Def.ControlSettings.Snapshot(_enabled, _multiplier);
        public virtual Code CheckControl(bool enabled, Fix target) => Def.ControlSettings.Check(target);
        public void Configure(bool enabled, Fix target)
        {
            if (!CheckControl(enabled, target).IsNone) throw new InvalidOperationException("execution target refused");
            _enabled = enabled; _multiplier = target; Entity.Touch();
        }
        public OperationSnapshot ReadOperation(World world)
        {
            var value = ReadExecution(world);
            return !value.Gate.Blocker.IsNone ? new OperationSnapshot(OperationStatus.Disabled, value.Gate.Blocker) :
                new OperationSnapshot(value.Multiplier.IsPositive ? OperationStatus.Running : OperationStatus.Idle, Code.None);
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context)
        { var value = ReadExecution(context.World); return !value.Gate.Blocker.IsNone || !value.Multiplier.IsPositive ? Def.States["disabled"] : null; }
        public override void Sync(IStateSync sync)
        {
            bool enabled = _enabled; Fix multiplier = _multiplier; sync.Bool(ref enabled); sync.Fix(ref multiplier);
            if (!Def.ControlSettings.Check(multiplier).IsNone) throw new CorruptStateException("invalid saved execution multiplier");
            if (sync.Reading) { _enabled = enabled; _multiplier = multiplier; }
        }
    }
}
