using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkBoard
    {
        // Transient proposal search for the original complete-set admission owner. No claims here.
        private IEnumerable<WorkRequirementResult> RequirementAlternatives(WorkRequirementResult initial, WorkRequirementContext context)
        {
            yield return initial;
            ActivityNodeExecution execution = context.Execution;
            if (context.Choices?.Count > 0 || execution != null && (execution.Resources.Count > 0 || execution.Costs.Count > 0
                || execution.State == NodeState.Done
                || execution.State == NodeState.Completing || HasSpent(execution))) yield break;
            IWorkRequirement root = context.Definition.Requirement; if (root == null) yield break;
            var pending = new Stack<(IWorkRequirement Requirement, IReadOnlyList<string> Path)>();
            var groups = new List<(IReadOnlyList<string> Path, IReadOnlyList<WorkRequirementEntry> Entries)>();
            pending.Push((root, Array.AsReadOnly(new[] { root.Id })));
            while (pending.Count > 0)
            {
                var occurrence = pending.Pop(); if (!(occurrence.Requirement is IWorkRequirementGroup group)) continue;
                var metadata = EntityObservation.ReadProvider(_world, group, context.Target, () =>
                {
                    ValidateRequirementDefinition(group);
                    return (group.Mode, group.Entries);
                }, observeTargetState: true);
                if (metadata.Mode == WorkRequirementMode.Any) groups.Add((occurrence.Path, metadata.Entries));
                for (int i = metadata.Entries.Count - 1; i >= 0; i--)
                {
                    var entry = metadata.Entries[i]; var path = new List<string>(occurrence.Path) { entry.Id };
                    pending.Push((entry.Requirement, path.AsReadOnly()));
                }
            }
            if (groups.Count == 0) yield break;
            var positions = new int[groups.Count];
            while (true)
            {
                var choices = new List<WorkRequirementChoice>();
                for (int i = 0; i < groups.Count; i++) choices.Add(new WorkRequirementChoice(groups[i].Path, groups[i].Entries[positions[i]].Id));
                var proposal = new WorkRequirementContext(_world, context.Owner, context.Target, context.Definition,
                    context.Worker, choices.AsReadOnly(), execution, context.Role);
                WorkRequirementResult value = EntityObservation.ReadProvider(_world, root, context.Target, () =>
                    context.Worker == null ? EvaluateRequirement(root, proposal, null)
                        : EntityObservation.ReadProvider(_world, context.Worker, context.Worker,
                            () => EvaluateRequirement(root, proposal, null), observeTargetState: true), observeTargetState: true);
                if (value.Status == WorkRequirementStatus.Satisfied) yield return value;
                int next = positions.Length - 1;
                while (next >= 0)
                {
                    positions[next]++;
                    if (positions[next] < groups[next].Entries.Count) break;
                    positions[next] = 0; next--;
                }
                if (next < 0) yield break;
            }
        }

        private bool TryAlternativeWorkSet(IReadOnlyList<WorkRequirementResult> results, IReadOnlyList<WorkRequirementContext> contexts,
            out IReadOnlyList<WorkRequirementResult> selected, out ResourceClaimCheck resources, out IReadOnlyList<IncidentPreparednessDock> docks)
        {
            selected = results; resources = default; docks = Array.Empty<IncidentPreparednessDock>();
            if (contexts.Count == 0) return false;
            var iterators = new IEnumerator<WorkRequirementResult>[contexts.Count];
            var current = new WorkRequirementResult[contexts.Count]; int level = 0;
            try
            {
                while (level >= 0)
                {
                    if (iterators[level] == null) iterators[level] = RequirementAlternatives(results[level], contexts[level]).GetEnumerator();
                    if (!iterators[level].MoveNext())
                    { iterators[level].Dispose(); iterators[level] = null; level--; continue; }
                    current[level] = iterators[level].Current;
                    if (current[level].Status != WorkRequirementStatus.Satisfied) continue;
                    var prefix = new List<WorkRequirementResult>(); var actual = new List<WorkRequirementContext>();
                    for (int i = 0; i <= level; i++) { prefix.Add(current[i]); actual.Add(contexts[i]); }
                    if (!CheckDockSet(actual.AsReadOnly(), out var bays).IsNone) continue;
                    ResourceClaimCheck material = CheckResourceSet(prefix.AsReadOnly(), actual.AsReadOnly(), out var picked);
                    if (!material.Ready || !CheckCostSet(picked, actual.AsReadOnly()).Blocker.IsNone) continue;
                    if (level == contexts.Count - 1) { selected = picked; resources = material; docks = bays; return true; }
                    level++;
                }
                return false;
            }
            finally { foreach (var iterator in iterators) iterator?.Dispose(); }
        }
    }
}
