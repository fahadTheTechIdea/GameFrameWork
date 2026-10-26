using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    public static class CapturedDependencyIdentityTests
    {
        public const string Case = "Dependency contracts: captured identity, canonical topology and external provider mutation";
        public static void Register() => TestRunner.Add(Case, Run);
        private static void Run(Check c)
        {
            var edges = new[] { "b", "a" };
            var a = new MutableDependencyProof("a");
            var b = new MutableDependencyProof("b");
            var join = new MutableDependencyProof("join", edges);
            var tail = new MutableDependencyProof("tail", "join");
            var source = new List<MutableDependencyProof> { tail, join, b, a };
            var graph = new DependencyGraph<MutableDependencyProof>(source, "external mutation proof");
            foreach (var node in source)
            {
                c.Equal(1, node.IdentityReads, "identity captured once");
                c.Equal(1, node.DependencyReads, "dependencies captured once");
                node.Replace("foreign", "missing"); node.RejectReads = true;
            }
            source.Clear(); edges[0] = "missing"; edges[1] = "join";
            c.Equal("a|b|join|tail", string.Join("|", graph.NodeIds), "captured canonical identities ignore live mutation");
            c.Equal("a|b", string.Join("|", graph.Dependencies("join")), "captured edges ignore array mutation");
            c.That(ReferenceEquals(join, graph.Find("join")) && ReferenceEquals(join, graph.Nodes[2]), "lookup retains sole provider alias");
            c.That(graph.Find("foreign") == null && graph.Find(null) == null, "changed identity cannot replace captured key");
            c.Throws<NotSupportedException>(() => ((IList<string>)graph.NodeIds).Clear(), "captured identities immutable");
            c.Throws<NotSupportedException>(() => ((IList<string>)graph.Dependencies("join")).Clear(), "captured edges immutable");
            c.Throws<ArgumentException>(() => graph.Dependencies("foreign"), "foreign edge query refuses");
            var weights = new Dictionary<string, BigInteger>(StringComparer.Ordinal) {
                ["a"] = (BigInteger)long.MaxValue + 1, ["b"] = (BigInteger)long.MaxValue + 1,
                ["join"] = 3, ["tail"] = 0 };
            var retained = graph.CriticalPath(weights);
            c.Equal((BigInteger)long.MaxValue + 4, retained.Weight, "exact weights beyond machine integer range");
            c.Equal("a|join", string.Join("|", retained.Nodes), "ordinal ties and zero suffix use captured keys");
            weights["b"] += 1;
            c.Equal("b|join", string.Join("|", graph.CriticalPath(weights).Nodes), "new weights recompute same captured topology");
            c.Equal("a|join", string.Join("|", retained.Nodes), "retained path is detached");
            c.Throws<NotSupportedException>(() => ((IList<string>)retained.Nodes).Clear(), "retained path immutable");
            weights.Remove("a"); weights.Add("foreign", 1);
            c.Throws<ArgumentException>(() => graph.CriticalPath(weights), "live alias cannot substitute original weight key");
            foreach (var node in graph.Nodes)
            {
                c.Equal(1, node.IdentityReads, "analysis never reads provider identity again");
                c.Equal(1, node.DependencyReads, "analysis never reads provider edges again");
            }
            var shuffled = new DependencyGraph<MutableDependencyProof>(new[] {
                new MutableDependencyProof("a"), new MutableDependencyProof("tail", "join"),
                new MutableDependencyProof("join", "a", "b"), new MutableDependencyProof("b") }, "shuffled");
            c.Equal(string.Join("|", graph.NodeIds), string.Join("|", shuffled.NodeIds), "declaration order independent");
            c.Throws<ContentException>(() => new DependencyGraph<MutableDependencyProof>(new[] {
                new MutableDependencyProof("a", "b"), new MutableDependencyProof("b", "a") }, "cycle"), "cycles still refuse");
            c.Throws<ContentException>(() => new DependencyGraph<MutableDependencyProof>(new[] {
                new MutableDependencyProof("a", "missing") }, "missing"), "missing references still refuse");
            c.Throws<ContentException>(() => new DependencyGraph<MutableDependencyProof>(new[] {
                new MutableDependencyProof("a"), new MutableDependencyProof("a") }, "duplicate"), "duplicate identities still refuse");
            var empty = new DependencyGraph<MutableDependencyProof>(Array.Empty<MutableDependencyProof>(), "empty");
            c.Equal(0, empty.NodeIds.Count, "explicit empty graph stays empty");
            c.Equal(BigInteger.Zero, empty.CriticalPath(new Dictionary<string, BigInteger>()).Weight, "empty graph has no fabricated work");
        }
    }
}
