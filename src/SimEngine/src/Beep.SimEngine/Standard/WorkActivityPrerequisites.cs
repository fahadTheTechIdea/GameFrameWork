using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkBoard
    {
        private sealed class CompletionDependency : IDependencyNode
        {
            public string Id { get; }
            public IReadOnlyList<string> Dependencies { get; }
            internal CompletionDependency(string id, SortedSet<string> dependencies)
            { Id = id; Dependencies = new List<string>(dependencies).AsReadOnly(); }
        }
        /// <summary>Composition-only structural possibility; never earned history or runtime eligibility.</summary>
        private void ValidateActivityPrerequisites(DependencyGraph<IWorkRequirement> requirements)
        {
            var possible = new HashSet<string>(StringComparer.Ordinal);
            var pending = new SortedSet<string>(PlanKeys, StringComparer.Ordinal);
            var readiness = new Dictionary<string, bool>(StringComparer.Ordinal);
            void ReadRequirements()
            {
                readiness.Clear();
                foreach (var requirement in requirements.Nodes)
                {
                    bool ready = true;
                    if (requirement is IPriorActivityWorkRequirement prior) ready = possible.Contains(prior.Plan);
                    else if (requirement is IWorkRequirementGroup group)
                    {
                        ready = group.Mode == WorkRequirementMode.All;
                        foreach (var entry in group.Entries)
                            if (group.Mode == WorkRequirementMode.All) ready &= readiness[entry.Requirement.Id];
                            else ready |= readiness[entry.Requirement.Id];
                    }
                    readiness.Add(requirement.Id, ready);
                }
            }
            while (pending.Count != 0)
            {
                ReadRequirements(); var newlyPossible = new List<string>();
                foreach (var key in pending)
                {
                    bool ready = true;
                    foreach (var node in ResolvePlan(key).Graph.Nodes)
                        if (node.Requirement != null && !readiness[node.Requirement.Id]) { ready = false; break; }
                    if (ready) newlyPossible.Add(key);
                }
                if (newlyPossible.Count == 0) break;
                foreach (var key in newlyPossible) { pending.Remove(key); possible.Add(key); }
            }
            if (pending.Count == 0) return;

            // Only genuinely blocking branches contribute edges. A viable ANY branch removes its
            // other branches from this diagnostic, not from the authored or executing graph.
            var blocked = new List<CompletionDependency>();
            foreach (var key in pending)
            {
                var references = new SortedSet<string>(StringComparer.Ordinal);
                var visited = new HashSet<IWorkRequirement>(); var stack = new Stack<IWorkRequirement>();
                foreach (var node in ResolvePlan(key).Graph.Nodes) if (node.Requirement != null) stack.Push(node.Requirement);
                while (stack.Count != 0)
                {
                    var requirement = stack.Pop();
                    if (!visited.Add(requirement) || readiness[requirement.Id]) continue;
                    if (requirement is IPriorActivityWorkRequirement prior) references.Add(prior.Plan);
                    else if (requirement is IWorkRequirementGroup group)
                        foreach (var entry in group.Entries) stack.Push(entry.Requirement);
                }
                blocked.Add(new CompletionDependency(key, references));
            }
            _ = new DependencyGraph<CompletionDependency>(blocked, "activity completion prerequisites");
            throw new ContentException("unresolved activity completion prerequisites");
        }
    }
}
