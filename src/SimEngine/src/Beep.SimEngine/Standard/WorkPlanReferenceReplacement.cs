using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkBoard
    {
        public static readonly Code ReplacementActiveExecution = new Code("work.replacement-active-execution");
        public static readonly Code ReplacementRecurringSource = new Code("work.replacement-recurring-source");

        private IReadOnlyList<EntityId> ReadPlanReferences(IPlanDefinition definition, JsonValue arguments)
            => PlanObservation.References(_world, definition, arguments);
        private static bool ContainsReference(IReadOnlyList<EntityId> references, EntityId entity)
        { foreach (var reference in references) if (reference == entity) return true; return false; }

        private JsonValue RebindPlanArguments(PlanEntry entry, EntityId source, EntityId destination)
        {
            var definition = Definition(entry);
            var original = ReadPlanReferences(definition, entry.Arguments);
            var rewritten = EntityObservation.ReadProvider(_world, definition, null,
                () => definition.ReplaceEntityReference(_world, entry.Arguments, source, destination));
            if (rewritten == null || !rewritten.IsObject) throw new InvalidOperationException("plan reference rewrite requires object arguments");
            // Capture returned content before persisting it; the provider owns no mutable alias of plan intent.
            var arguments = rewritten.Clone(); arguments.Freeze();
            var expected = new List<EntityId>();
            foreach (var reference in original) expected.Add(reference == source ? destination : reference);
            expected.Sort();
            var actual = ReadPlanReferences(definition, arguments);
            if (actual.Count != expected.Count) throw new InvalidOperationException("plan rewrite changed unrelated references");
            for (int i = 0; i < actual.Count; i++)
                if (actual[i] != expected[i]) throw new InvalidOperationException("plan rewrite did not exactly rebind its requested reference");
            return arguments;
        }

        private Code CheckPlanReferenceReplacement(Entity source, EntityDef destination)
        {
            Code recurring = CheckRecurringReferenceReplacement(source, destination);
            if (!recurring.IsNone) return recurring;
            foreach (Order order in _orders)
                if (order.IsActive)
                {
                    bool referenced = order.Target == source.Id;
                    foreach (var worker in order.WorkerSelection.Bindings) referenced |= worker.Worker == source.Id;
                    if (!referenced) continue;
                    // The earned replacement's own execution becomes terminal history; it must not be rewritten.
                    bool completingReplacement = order.State == OrderState.Completing && order.NodesDone == order.Nodes.Count && order.Target == source.Id &&
                        _world.Content.Find<IWorkEffect>(order.Plan.OnComplete) is IEntityReplacementEffect effect &&
                        EntityObservation.ReadProvider(_world, effect, source,
                            () => effect.AcceptsSourceDefinition(source.Def) && effect.CheckReplacement(_world, source, destination).IsNone,
                            space: _world.Space, observeTargetState: true);
                    if (!completingReplacement) return ReplacementActiveExecution;
                }
            var projected = new Dictionary<PlanEntry, JsonValue>();
            foreach (PlanEntry entry in _plans)
            {
                if (!entry.Live) continue;
                if (entry.State != PlanState.Pending) continue;
                var definition = Definition(entry);
                if (!ContainsReference(ReadPlanReferences(definition, entry.Arguments), source.Id)) continue;
                // This exact pending replacement will become its native execution,
                // not a surviving intent whose old-prototype target needs rebinding.
                // Every OTHER pending plan and every unearned active order still
                // passes the original full reference/metadata/identity guards.
                if (IsActivatingReplacement(entry, definition, source, destination)) continue;
                Code check = EntityObservation.ReadProvider(_world, definition, null,
                    () => definition.CheckEntityReplacement(_world, entry.Company, entry.Arguments, source, destination));
                if (!check.IsNone) return check;
                projected.Add(entry, RebindPlanArguments(entry, source.Id, _world.Entities.NextIdentity));
            }
            return CheckProjectedPlanIdentities(projected);
        }
        private bool IsActivatingReplacement(PlanEntry entry, IPlanDefinition definition, Entity source, EntityDef destination)
        {
            if (!ReferenceEquals(entry, _activatingPlan) || entry.Company != source.Owner ||
                definition is not IAuthoredActivityDefinition activity ||
                _world.Content.Find<IWorkEffect>(activity.OnComplete) is not IEntityReplacementEffect effect) return false;
            ActivityWorkerSelection.Read(entry.Arguments, activity.Graph, activity.PlanKey + ".intent", out EntityId target);
            return target == source.Id && EntityObservation.ReadProvider(_world, effect, source,
                () => effect.AcceptsSourceDefinition(source.Def) && effect.CheckReplacement(_world, source, destination).IsNone,
                space: _world.Space, observeTargetState: true);
        }
        private Code CheckProjectedPlanIdentities(IReadOnlyDictionary<PlanEntry, JsonValue> projected)
        {
            var identities = new HashSet<(PlayerId Company, string Definition, string Identity)>();
            foreach (var entry in _plans)
                if (entry.Live)
                {
                    var definition = Definition(entry);
                    var arguments = projected.TryGetValue(entry, out var proposed) ? proposed : entry.Arguments;
                    string identity = PlanObservation.Identity(_world, definition, arguments);
                    if (string.IsNullOrWhiteSpace(identity)) throw new InvalidOperationException("plan reference rewrite has no intent identity");
                    if (!identities.Add((entry.Company, entry.Definition, identity))) return PlanCodes.Duplicate;
                }
            return Code.None;
        }
        private void ReplacePlanReferences(Entity source, Entity destination)
        {
            var projected = new Dictionary<PlanEntry, JsonValue>();
            foreach (PlanEntry entry in _plans)
            {
                if (entry.State != PlanState.Pending) continue;
                var definition = Definition(entry);
                if (!ContainsReference(ReadPlanReferences(definition, entry.Arguments), source.Id)) continue;
                var arguments = RebindPlanArguments(entry, source.Id, destination.Id);
                Code check = PlanObservation.Submission(_world, definition, entry.Company, arguments);
                if (!check.IsNone) throw new InvalidOperationException("replacement plan binding violated admission: " + check);
                projected.Add(entry, arguments);
            }
            if (!CheckProjectedPlanIdentities(projected).IsNone) throw new InvalidOperationException("replacement created duplicate plan intent");
            foreach (var item in projected) item.Key.Arguments = item.Value;
            ReplaceRecurringReferences(source, destination);
            if (projected.Count > 0) Dirty = true;
        }
    }
}
