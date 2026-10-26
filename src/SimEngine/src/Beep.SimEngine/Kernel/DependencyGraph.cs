using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Kernel
{
    public interface IDependencyNode
    {
        string Id { get; }
        IReadOnlyList<string> Dependencies { get; }
    }

    /// <summary>Detached exact weight and captured dependency path; units are supplied by the caller.</summary>
    public sealed class DependencyPath
    {
        public BigInteger Weight { get; }
        public IReadOnlyList<string> Nodes { get; }
        internal DependencyPath(BigInteger weight, List<string> nodes)
        { Weight = weight; Nodes = nodes.AsReadOnly(); }
    }

    /// <summary>Sole immutable topology compiler; values alias the caller's definitions, edges are captured.</summary>
    public sealed class DependencyGraph<T> where T : class, IDependencyNode
    {
        private readonly Dictionary<string, T> _nodes = new Dictionary<string, T>(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyList<string>> _edges = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        public IReadOnlyList<T> Nodes { get; }
        /// <summary>Captured stable identities in canonical topological order. Provider objects remain aliases;
        /// their later property changes cannot redefine this graph's validated topology.</summary>
        public IReadOnlyList<string> NodeIds { get; }
        public T Find(string id) => id != null && _nodes.TryGetValue(id, out T value) ? value : null;
        public IReadOnlyList<string> Dependencies(string id) => _edges.TryGetValue(id, out var value) ? value : throw new ArgumentException("unknown dependency identity", nameof(id));

        public DependencyGraph(IEnumerable<T> definitions, string owner)
        {
            if (definitions == null) throw new ContentException(owner + ": dependency nodes are required");
            foreach (T node in definitions)
            {
                string id = node?.Id;
                if (string.IsNullOrWhiteSpace(id)) throw new ContentException(owner + ": node requires a stable identity");
                if (_nodes.ContainsKey(id)) throw new ContentException(owner + ": duplicate node '" + id + "'");
                IReadOnlyList<string> dependencies = node.Dependencies;
                if (dependencies == null) throw new ContentException(owner + ": node '" + id + "' has no dependency declaration");
                var edges = new List<string>(dependencies); edges.Sort(StringComparer.Ordinal);
                for (int i = 0; i < edges.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(edges[i])) throw new ContentException(owner + ": node '" + id + "' has an empty dependency");
                    if (i > 0 && edges[i] == edges[i - 1]) throw new ContentException(owner + ": node '" + id + "' repeats dependency '" + edges[i] + "'");
                }
                _nodes.Add(id, node); _edges.Add(id, edges.AsReadOnly());
            }
            var pending = new SortedSet<string>(StringComparer.Ordinal);
            var incoming = new Dictionary<string, int>(StringComparer.Ordinal);
            var dependents = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (string id in new SortedSet<string>(_nodes.Keys, StringComparer.Ordinal))
            {
                incoming.Add(id, _edges[id].Count);
                if (_edges[id].Count == 0) pending.Add(id);
                foreach (string dependency in _edges[id])
                {
                    if (dependency == id) throw new ContentException(owner + ": node '" + id + "' depends on itself");
                    if (!_nodes.ContainsKey(dependency)) throw new ContentException(owner + ": node '" + id + "' references missing dependency '" + dependency + "'");
                    if (!dependents.TryGetValue(dependency, out var list)) { list = new List<string>(); dependents.Add(dependency, list); }
                    list.Add(id);
                }
            }
            var ordered = new List<T>(_nodes.Count);
            var identities = new List<string>(_nodes.Count);
            while (pending.Count > 0)
            {
                string id = pending.Min; pending.Remove(id); ordered.Add(_nodes[id]); identities.Add(id);
                if (dependents.TryGetValue(id, out var list))
                    foreach (string dependent in list) if (--incoming[dependent] == 0) pending.Add(dependent);
            }
            if (ordered.Count != _nodes.Count) throw new ContentException(owner + ": dependency cycle " + Cycle());
            Nodes = ordered.AsReadOnly();
            NodeIds = identities.AsReadOnly();
        }

        /// <summary>Exact longest weighted path. Equal predecessors/terminals use ordinal identity.
        /// Zero-weight connectors remain, but zero-work prefixes/suffixes are omitted.</summary>
        public DependencyPath CriticalPath(IReadOnlyDictionary<string, BigInteger> weights)
        {
            if (weights == null) throw new ArgumentNullException(nameof(weights));
            if (weights.Count != Nodes.Count) throw new ArgumentException("weights must cover exactly the captured graph", nameof(weights));
            var captured = new Dictionary<string, BigInteger>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, BigInteger> entry in weights)
            {
                if (!_nodes.ContainsKey(entry.Key) || entry.Value.Sign < 0)
                    throw new ArgumentException("unknown or negative weight for '" + entry.Key + "'", nameof(weights));
                captured.Add(entry.Key, entry.Value);
            }
            if (captured.Count != Nodes.Count) throw new ArgumentException("weights must cover exactly the captured graph", nameof(weights));
            var totals = new Dictionary<string, BigInteger>(StringComparer.Ordinal);
            var predecessors = new Dictionary<string, string>(StringComparer.Ordinal);
            var nonterminals = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in NodeIds)
            {
                BigInteger best = BigInteger.Zero; string predecessor = null;
                foreach (string dependency in _edges[id])
                {
                    nonterminals.Add(dependency);
                    BigInteger candidate = totals[dependency];
                    if (candidate > best || candidate == best && (predecessor == null || string.CompareOrdinal(dependency, predecessor) < 0))
                    { best = candidate; predecessor = dependency; }
                }
                totals.Add(id, best + captured[id]); predecessors.Add(id, predecessor);
            }
            BigInteger maximum = BigInteger.Zero; string terminal = null;
            foreach (string id in NodeIds)
            {
                if (nonterminals.Contains(id)) continue;
                BigInteger candidate = totals[id];
                if (candidate > maximum || candidate == maximum && (terminal == null || string.CompareOrdinal(id, terminal) < 0))
                { maximum = candidate; terminal = id; }
            }
            var path = new List<string>();
            if (maximum.IsZero) return new DependencyPath(maximum, path);
            for (string id = terminal; id != null; id = predecessors[id]) path.Add(id);
            path.Reverse();
            int first = 0, last = path.Count - 1;
            while (captured[path[first]].IsZero) first++;
            while (captured[path[last]].IsZero) last--;
            return new DependencyPath(maximum, path.GetRange(first, last - first + 1));
        }

        /// <summary>Uncapped iterative prerequisite closure in canonical dependency-first order.</summary>
        public IReadOnlyList<string> PrerequisiteOrder(string target)
        {
            if (target == null || !_nodes.ContainsKey(target)) throw new ArgumentException("unknown dependency identity", nameof(target));
            var included = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<string>(); pending.Push(target);
            while (pending.Count != 0)
            {
                string id = pending.Pop(); if (!included.Add(id)) continue;
                foreach (string dependency in _edges[id]) pending.Push(dependency);
            }
            var result = new List<string>();
            foreach (string id in NodeIds) if (included.Contains(id)) result.Add(id);
            return result.AsReadOnly();
        }

        private string Cycle()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var active = new Dictionary<string, int>(StringComparer.Ordinal);
            var stack = new List<Frame>();
            foreach (string root in new SortedSet<string>(_nodes.Keys, StringComparer.Ordinal))
            {
                if (seen.Contains(root)) continue;
                seen.Add(root); active.Add(root, 0); stack.Add(new Frame(root));
                while (stack.Count > 0)
                {
                    Frame frame = stack[stack.Count - 1];
                    IReadOnlyList<string> edges = _edges[frame.Id];
                    if (frame.Next == edges.Count) { active.Remove(frame.Id); stack.RemoveAt(stack.Count - 1); continue; }
                    string next = edges[frame.Next++];
                    if (active.TryGetValue(next, out int at))
                    {
                        var path = new List<string>();
                        for (int i = at; i < stack.Count; i++) path.Add(stack[i].Id);
                        path.Add(next); return string.Join(" -> ", path);
                    }
                    if (!seen.Add(next)) continue;
                    active.Add(next, stack.Count); stack.Add(new Frame(next));
                }
            }
            throw new InvalidOperationException("invalid unresolved dependency topology");
        }
        private sealed class Frame { public readonly string Id; public int Next; public Frame(string id) { Id = id; } }
    }
}
