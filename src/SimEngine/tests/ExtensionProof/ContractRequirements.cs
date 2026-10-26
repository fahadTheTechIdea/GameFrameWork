using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side proof content chooses retention; the work board remains the only execution store.
    public sealed class ActivityRecordDef : ComponentDef<ActivityRecord>
    {
        protected override void ReadCore(JsonReader reader) { }
    }
    public sealed class ActivityRecord : Component<ActivityRecordDef>, IWorkOrderRetention
    {
        // Instance-scoped negative read instrumentation, never serialized gameplay state.
        public System.Action<ActivityRecord, Order> ReadFault;
        public bool RetainsOrder(Order order)
        { ReadFault?.Invoke(this, order); return Entity.Alive && order.Target == Entity.Id; }
    }

    public interface IWorkPermit : IEntityCapability
    {
        bool Enabled { get; }
        void SetEnabled(bool enabled);
    }

    public sealed class WorkPermitDef : ComponentDef<WorkPermit>
    {
        public bool Enabled { get; private set; }
        public IEntityStateDefinition Stopped { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            JsonValue enabled = r.Value("enabled");
            if (enabled == null || enabled.Kind != JsonKind.Bool) throw r.Error("enabled must be authored as a boolean");
            Enabled = enabled.Bool;
        }
        protected override void ResolveCore(ContentResolver c) => Stopped = c.Def<IEntityStateDefinition>("stopped");
    }

    public sealed class WorkPermit : Component<WorkPermitDef>, IWorkPermit, IEntityStateSource
    {
        private bool _enabled;
        public bool Enabled => _enabled;
        public override void OnCreate() => _enabled = Def.Enabled;
        public void SetEnabled(bool enabled) { _enabled = enabled; Entity.Touch(); }
        // Negative observation probe: deliberately bypasses Touch, using the actual existing codec field.
        public void CorruptReadState() => _enabled = !_enabled;
        public IEntityStateDefinition GetState(EntityUpdateContext context) => _enabled ? null : Def.Stopped;
        public override void Sync(IStateSync sync) => sync.Bool(ref _enabled);
    }

    public sealed class PermitRequirementDef : WorkRequirementDef
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private Code _blocker, _missing;
        protected override void ReadCore(JsonReader r)
        { _blocker = new Code(r.String("blocker")); _missing = new Code(r.String("missing")); }
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        {
            IWorkPermit permit = context.Target.Get<IWorkPermit>();
            return permit == null ? WorkRequirementResult.Invalid(_missing)
                : permit.Enabled ? WorkRequirementResult.Satisfied() : WorkRequirementResult.Pending(_blocker);
        }
    }

    public sealed class ReserveRequirementDef : WorkRequirementDef
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private Fix _minimum;
        private Code _blocker, _unit;
        protected override void ReadCore(JsonReader r)
        {
            _minimum = r.Fix("minimum"); _blocker = new Code(r.String("blocker")); _unit = new Code(r.String("unit"));
            if (_minimum < Fix.Zero) throw r.Error("minimum cannot be negative");
        }
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        {
            Fix cash = context.World.Services.Get<IEconomy>().Cash(context.Owner);
            var quantity = new WorkRequirementQuantity(cash, _minimum, _unit);
            return cash >= _minimum ? WorkRequirementResult.Satisfied(quantity) : WorkRequirementResult.Blocked(_blocker, quantity);
        }
    }

    /// <summary>Ordinary engine-side mod predicate; no dispatch or reservation logic.</summary>
    public sealed class WorkerRateRequirementDef : WorkRequirementDef, IWorkerWorkRequirement
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private Fix _minimum; private Code _blocker;
        protected override void ReadCore(JsonReader reader)
        {
            _minimum = reader.Fix("minimum"); _blocker = new Code(reader.String("blocker"));
            if (!_minimum.IsPositive) throw reader.Error("minimum rate must be positive");
        }
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        {
            Fix rate = WorkerExecutionContracts.BaseRate(context.World, context.Worker);
            var quantity = new WorkRequirementQuantity(rate, _minimum, new Code("unit.work-rate"));
            return rate >= _minimum ? WorkRequirementResult.Satisfied(quantity) : WorkRequirementResult.Blocked(_blocker, quantity);
        }
    }

    // Explicit fault injection: malformed extension results must not turn into implicit readiness.
    public sealed class MalformedRequirementDef : WorkRequirementDef
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        protected override void ReadCore(JsonReader r) { }
        public override WorkRequirementResult Evaluate(WorkRequirementContext context) => default;
    }

    public sealed class SetWorkPermitCommand : Command
    {
        public EntityId Target;
        public bool Enabled;
        public override void Sync(IStateSync sync) { sync.Entity(ref Target); sync.Bool(ref Enabled); }
    }

    public sealed class SetWorkPermitHandler : CommandHandler<SetWorkPermitCommand>
    {
        protected override Code Check(World world, SetWorkPermitCommand command)
        {
            Entity target = world.Entities.Get(command.Target);
            if (target == null) return Codes.NotFound;
            if (target.Owner != command.Player) return Codes.NotOwned;
            return target.Has<IWorkPermit>() ? Code.None : Codes.InvalidArgument;
        }
        protected override void Apply(World world, SetWorkPermitCommand command, CommandContext result)
        {
            world.Entities.Get(command.Target).Get<IWorkPermit>().SetEnabled(command.Enabled);
            result.Entity = command.Target;
        }
    }
}
