using System;
using System.Collections.Generic;

namespace Beep.SimEngine.Standard
{
    internal readonly struct DistinctWorkerMatch
    {
        internal EntityId Worker { get; }
        internal int Demand { get; }
        internal DistinctWorkerMatch(EntityId worker, int demand) { Worker = worker; Demand = demand; }
    }
    /// <summary>Stateless maximum capacity matching; caller supplies canonical demands and actual eligible identities.</summary>
    internal static class DistinctWorkerMatching
    {
        internal static IReadOnlyList<DistinctWorkerMatch> Solve(IReadOnlyList<int> capacities,
            IReadOnlyList<IReadOnlyList<EntityId>> compatible,
            Func<IReadOnlyList<DistinctWorkerMatch>, bool> feasible = null)
        {
            if (capacities == null || compatible == null || capacities.Count != compatible.Count) throw new ArgumentException("matching needs explicit aligned demands");
            var ids = new SortedSet<EntityId>(Comparer<EntityId>.Create((a, b) => a.Value.CompareTo(b.Value)));
            for (int demand = 0; demand < capacities.Count; demand++)
            {
                if (capacities[demand] < 0 || compatible[demand] == null) throw new ArgumentException("matching needs nonnegative capacities and explicit identities");
                var row = new HashSet<EntityId>();
                foreach (EntityId id in compatible[demand])
                { if (!id.IsValid || !row.Add(id)) throw new ArgumentException("matching requires distinct valid compatible identities"); ids.Add(id); }
            }
            var workers = new List<EntityId>(ids);
            var indices = new Dictionary<EntityId, int>();
            for (int i = 0; i < workers.Count; i++) indices.Add(workers[i], i);
            var assignment = new int[workers.Count]; for (int i = 0; i < assignment.Length; i++) assignment[i] = -1;
            var counts = new int[capacities.Count]; int covered = 0;
            for (int root = 0; root < capacities.Count; root++)
                while (counts[root] < capacities[root] && covered < workers.Count)
                {
                    var parents = new int[workers.Count]; for (int i = 0; i < parents.Length; i++) parents[i] = -1;
                    var previous = new int[capacities.Count]; for (int i = 0; i < previous.Length; i++) previous[i] = -1;
                    var seen = new bool[capacities.Count]; var queue = new List<int> { root }; seen[root] = true;
                    int free = -1;
                    for (int cursor = 0; cursor < queue.Count && free < 0; cursor++)
                    {
                        int demand = queue[cursor];
                        foreach (EntityId id in compatible[demand])
                        {
                            int worker = indices[id];
                            if (parents[worker] < 0)
                            {
                                parents[worker] = demand; int old = assignment[worker];
                                if (old < 0) { free = worker; break; }
                                if (!seen[old]) { seen[old] = true; previous[old] = worker; queue.Add(old); }
                            }
                        }
                    }
                    if (free < 0) break;
                    while (free >= 0)
                    {
                        int demand = parents[free], next = demand == root ? -1 : previous[demand];
                        if (assignment[free] >= 0) counts[assignment[free]]--;
                        assignment[free] = demand; counts[demand]++; free = next;
                    }
                    covered++;
                }
            var result = new List<DistinctWorkerMatch>();
            for (int i = 0; i < workers.Count; i++) if (assignment[i] >= 0) result.Add(new DistinctWorkerMatch(workers[i], assignment[i]));
            var original = result.AsReadOnly();
            if (feasible == null || feasible(original) || covered == 0) return original;
            // Preserve maximum coverage of EVERY priority-ranked demand. Expand only attained
            // counts, never possibly enormous requested capacities. Search owns no live claims.
            var slots = new List<int>();
            for (int demand = 0; demand < counts.Length; demand++)
                for (int n = 0; n < counts[demand]; n++) slots.Add(demand);
            var positions = new int[slots.Count];
            for (int i = 0; i < positions.Length; i++) positions[i] = -1;
            var chosen = new EntityId[slots.Count]; var used = new HashSet<EntityId>(); int depth = 0;
            while (depth >= 0)
            {
                if (chosen[depth].IsValid) { used.Remove(chosen[depth]); chosen[depth] = EntityId.None; }
                int demand = slots[depth]; var candidates = compatible[demand];
                int next = positions[depth] + 1;
                // Workers of the same demand are interchangeable. Canonical increasing edge
                // indices enumerate each set once, without suppressing distinct demand allocations.
                if (depth > 0 && slots[depth - 1] == demand) next = Math.Max(next, positions[depth - 1] + 1);
                while (next < candidates.Count && used.Contains(candidates[next])) next++;
                if (next == candidates.Count) { positions[depth] = -1; depth--; continue; }
                positions[depth] = next; chosen[depth] = candidates[next]; used.Add(chosen[depth]);
                if (depth + 1 < slots.Count) { depth++; positions[depth] = -1; continue; }
                var alternative = new List<DistinctWorkerMatch>();
                for (int i = 0; i < slots.Count; i++) alternative.Add(new DistinctWorkerMatch(chosen[i], slots[i]));
                alternative.Sort((a, b) => a.Worker.Value.CompareTo(b.Worker.Value));
                var detached = alternative.AsReadOnly();
                if (feasible(detached)) return detached;
            }
            // No jointly feasible set: retain the real priority coverage for shortage diagnostics
            // and ordinary physical sequencing. This is not successful joint admission.
            return original;
        }
    }
}
