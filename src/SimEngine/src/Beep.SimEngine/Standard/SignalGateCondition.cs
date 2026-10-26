using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    /// <summary>One immutable signal condition owner shared by typed execution gates.</summary>
    public sealed class SignalGateCondition
    {
        private readonly string _signal;
        public string Id { get; }
        public Code Source { get; }
        public IAutomationSignalContent Signal { get; private set; }
        public Code Blocker { get; }
        public bool? Expected { get; }
        public Fix Minimum { get; }
        public Fix Maximum { get; }
        public Code Unit { get; }
        private SignalGateCondition(JsonReader reader)
        {
            Id = reader.String("id"); string source = reader.String("source"); _signal = reader.String("signal");
            string blocker = reader.String("blocker");
            try
            {
                foreach (string id in new[] { Id, source, _signal, blocker }) ContractIdentity.ValidateId(id, "gate condition identity");
            }
            catch (ArgumentException ex) { throw reader.Error(ex.Message); }
            Source = new Code(source); Blocker = new Code(blocker);
            if (reader.Has("expected")) Expected = ControlRuleDeclaration.Boolean(reader, "expected");
            else
            {
                if (!reader.Has("minimum") || !reader.Has("maximum")) throw reader.Error("numeric gate requires explicit minimum/maximum");
                Minimum = reader.Fix("minimum"); Maximum = reader.Fix("maximum"); string unit = reader.String("unit");
                try { ContractIdentity.ValidateId(unit, "gate range unit"); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
                Unit = new Code(unit);
                if (Minimum > Maximum || Unit.IsNone) throw reader.Error("gate range/unit is invalid");
            }
            reader.End();
        }
        internal static IReadOnlyList<SignalGateCondition> ReadAll(JsonReader reader, string key = "conditions", bool allowEmpty = false)
        {
            JsonValue rows = reader.Array(key);
            if (rows == null || !allowEmpty && rows.Count == 0) throw reader.Error("explicit " + key + (allowEmpty ? " array required" : " must be nonempty"));
            var conditions = new List<SignalGateCondition>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                var condition = new SignalGateCondition(new JsonReader(rows[i], reader.Where + "." + key + "[" + i + "]"));
                if (!ids.Add(condition.Id)) throw reader.Error("duplicate condition identity"); conditions.Add(condition);
            }
            conditions.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); return conditions.AsReadOnly();
        }
        internal void Resolve(ContentResolver content)
        {
            Signal = content.Def<IAutomationSignalContent>(_signal);
            if (Expected.HasValue ? Signal.Kind != AutomationSignalKind.Boolean || Signal.Unit != new Code("unit.boolean")
                : Signal.Kind != AutomationSignalKind.Number || Signal.Unit != Unit)
                throw content.Fail("gate condition '" + Id + "' disagrees with its authored signal kind/unit");
        }
        internal bool Accepts(EntityDef owner)
        {
            var schema = AutomationSignalContracts.Schema(owner, Source);
            return schema != null && Signal.Accepts(schema);
        }
        internal void Validate(EntityDef owner)
        {
            if (!Accepts(owner))
                throw new ContentException(owner.Id + ": gate condition '" + Id + "' requires its actual named compatible signal source");
        }
        private bool Ready(AutomationSignalSnapshot value) => value.Available &&
            (Expected.HasValue ? value.Value.Value == (Expected.Value ? Fix.One : Fix.Zero)
                : value.Value.Value >= Minimum && value.Value.Value <= Maximum);

        /// <summary>One condition interpretation over detached, exactly typed signal facts.</summary>
        public ExecutionGateConditionSnapshot Evaluate(AutomationSignalSnapshot value)
        {
            if (Signal == null) throw new InvalidOperationException("gate condition has not been resolved");
            Signal.Validate(value);
            bool ready = Ready(value);
            Code refusal = ready ? Code.None : !value.Available ? value.Blocker : Blocker;
            return new ExecutionGateConditionSnapshot(Id, Source, value, ready, refusal);
        }

        internal static IReadOnlyList<ExecutionGateConditionSnapshot> ReadFromView(OperationsView view, EntityId entity,
            IReadOnlyList<SignalGateCondition> definitions)
        {
            var facts = new List<ExecutionGateConditionSnapshot>();
            foreach (var condition in definitions)
            {
                Code refusal = view.ReadSignal(entity, condition.Source, condition.Signal.Id, out AutomationSignalSnapshot value);
                facts.Add(condition.Evaluate(refusal.IsNone ? value : condition.Signal.Unavailable(refusal)));
            }
            return facts.AsReadOnly();
        }
        internal static ExecutionGateSnapshot ReadGate(World world, Entity entity, Code capability,
            bool applies, IReadOnlyList<SignalGateCondition> definitions)
        {
            var conditions = new List<ExecutionGateConditionSnapshot>(); Code blocker = Code.None;
            if (applies)
                foreach (var condition in definitions)
                {
                    var source = entity.Get<IAutomationSignalSource>(condition.Source);
                    AutomationSignalSnapshot value = condition.Signal.ReadFrom(world, source);
                    ExecutionGateConditionSnapshot fact = condition.Evaluate(value);
                    conditions.Add(fact);
                    if (blocker.IsNone && !fact.Blocker.IsNone) blocker = fact.Blocker;
                }
            return new ExecutionGateSnapshot(capability, applies, blocker, conditions);
        }
    }
}
