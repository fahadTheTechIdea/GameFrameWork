using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable binding for the original recurring state owner, not every custom source's model.</summary>
    public interface IRecurringActivitySourceDefinition : IRecurringPlanBindingDefinition, IRecurringOperationPolicyDefinition
    {
        IReadOnlyList<SignalGateCondition> Conditions { get; }
        void ValidateBinding(World world);
    }
    /// <summary>Sole JSON reader/resolver; no live schedule, plan history or work state.</summary>
    public sealed class RecurringActivityDeclaration
    {
        private readonly string _template;
        private bool _resolved;
        public Code CapabilityId { get; }
        public RecurringCadenceBasis Cadence { get; }
        public RecurringTriggerMode TriggerMode { get; }
        public RecurringWorkerDispatchMode WorkerDispatch { get; }
        public string PlanKey { get; }
        public IPlanTemplateContent Template { get; private set; }
        public RecurringPlanSettings Initial { get; }
        public RecurringPlanBinding Binding { get; }
        public string Group { get; }
        public Code OperationPolicy { get; }
        public IReadOnlyList<SignalGateCondition> Conditions { get; }
        private RecurringActivityDeclaration(JsonReader reader)
        {
            string id = reader.String("id"); RecurringPlanBinding.ReadSelection(reader, out var plan, out _template); PlanKey = plan;
            try { ContractIdentity.ValidateId(id, "recurring source"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id); Initial = RecurringPlanSettings.Read(reader);
            Cadence = RecurringPlanContracts.ReadCadence(reader);
            TriggerMode = RecurringPlanContracts.ReadTriggerMode(reader);
            WorkerDispatch = RecurringPlanContracts.ReadWorkerDispatch(reader);
            Binding = RecurringPlanBinding.Read(reader); Group = RecurringPlanBinding.ReadGroup(reader);
            OperationPolicy = RecurringPlanContracts.ReadOperationPolicy(reader);
            Conditions = SignalGateCondition.ReadAll(reader, "conditions", true);
        }
        public static RecurringActivityDeclaration Read(JsonReader reader)
            => new RecurringActivityDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (_resolved) throw new InvalidOperationException("recurring declaration resolves once");
            Template = content.OptionalDef<IPlanTemplateContent>(_template);
            foreach (var condition in Conditions) condition.Resolve(content);
            _resolved = true;
        }
        public void ValidateOwner(EntityDef owner)
        { foreach (var condition in Conditions) condition.Validate(owner); }
    }
    /// <summary>Actual bindings and existing codec observations only; the board remains the sole plan authority.</summary>
    public static class RecurringActivityBindingContracts
    {
        internal static T Observe<T>(World world, IRecurringActivitySourceDefinition definition, Entity entity,
            Func<IRecurringActivitySourceDefinition, T> read)
            => EntityObservation.ReadProvider(world, definition, entity, () => {
                var scope = entity == null ? default : EntityObservation.CaptureProvider(world, entity, entity.Sync, "work.recurring.metadata");
                try { ValidateCore(world, definition, entity); return read(definition); }
                finally { if (entity != null) scope.Verify(); }
            });
        public static void ValidateBinding(World world, IRecurringActivitySourceDefinition definition)
            => Observe(world, definition, null, _ => true);
        private static void ValidateCore(World world, IRecurringActivitySourceDefinition definition, Entity entity)
        {
            if (!(definition is ComponentDef part) || !part.IsFrozen || !world.Content.Owns(part.Owner) ||
                part.ComponentType != typeof(RecurringActivitySource) || part.StateCodec.SchemaVersion != 8 ||
                !ReferenceEquals(part.Owner.Component<IRecurringActivitySourceDefinition>(definition.CapabilityId), definition) ||
                entity != null && !ReferenceEquals(entity.Def, part.Owner))
                throw new ContentException("recurring binding requires actual immutable owner, original runtime and codec8");
            if (definition.Initial == null || definition.Binding == null || definition.Conditions == null ||
                (byte)definition.Cadence > (byte)RecurringCadenceBasis.SuccessfulCompletion ||
                (byte)definition.TriggerMode > (byte)RecurringTriggerMode.ScheduleOrMaintenance ||
                (byte)definition.WorkerDispatch > (byte)RecurringWorkerDispatchMode.ServiceFirst)
                throw new ContentException(part.Owner.Id + ": reusable recurring metadata is incomplete");
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var condition in definition.Conditions)
            {
                if (condition == null || !identities.Add(condition.Id))
                    throw new ContentException(part.Owner.Id + ": recurring conditions require actual unique declarations");
                condition.Validate(part.Owner);
            }
        }
    }
}
