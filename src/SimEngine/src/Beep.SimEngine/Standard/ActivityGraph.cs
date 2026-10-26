using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>A native intent binding to the same selected authored workflow, not another execution owner.</summary>
    public interface IActivityWorkflowBinding
    { IActivityDefinition Workflow { get; } }

    public interface IActivityNodeDefinition : IDependencyNode
    {
        IActivityExecutor Executor { get; }
        IReadOnlyList<WorkerRoleId> Roles { get; }
        GameDuration Duration { get; }
        IWorkRequirement Requirement { get; }
        Fix RequirementMultiplier { get; }
        string Effect { get; }
    }
    public interface IActivityGraph
    {
        /// <summary>Canonical topological order with ordinal node identity tie-breaks.</summary>
        IReadOnlyList<IActivityNodeDefinition> Nodes { get; }
        IActivityNodeDefinition Find(string id);
        /// <summary>Includes the named node and dependency ancestry; null means any node in the graph.</summary>
        bool HasMachineWork(string id);
        /// <summary>Exact nominal Q32 work, not wall-time ETA; requires progress for every node.</summary>
        ActivityWorkAnalysis AnalyzeWork(ISimulationTime time, IReadOnlyDictionary<string, WorkProgress> progress);
    }

    public sealed class ActivityWorkAnalysis
    {
        public BigInteger TotalRequiredRaw { get; }
        public BigInteger RemainingRaw { get; }
        public DependencyPath CriticalWork { get; }
        internal ActivityWorkAnalysis(BigInteger total, BigInteger remaining, DependencyPath critical)
        { TotalRequiredRaw = total; RemainingRaw = remaining; CriticalWork = critical; }
    }

    /// <summary>Immutable typed activity data/ancestry over the sole dependency compiler; no runtime state.</summary>
    public sealed class ActivityGraph : IActivityGraph
    {
        private readonly DependencyGraph<IActivityNodeDefinition> _topology;
        private readonly Dictionary<string, bool> _physical = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly bool _anyPhysical;
        public IReadOnlyList<IActivityNodeDefinition> Nodes => _topology.Nodes;
        public IActivityNodeDefinition Find(string id) => _topology.Find(id);
        public bool HasMachineWork(string id) => id == null ? _anyPhysical : _physical.TryGetValue(id, out bool value) && value;

        public ActivityGraph(IEnumerable<IActivityNodeDefinition> definitions, string owner)
        {
            if (definitions == null) throw new ContentException(owner + ": activity nodes are required");
            var captured = new List<IActivityNodeDefinition>();
            foreach (IActivityNodeDefinition definition in definitions)
            {
                if (definition == null) throw new ContentException(owner + ": node requires a stable identity");
                captured.Add(new Node(definition, owner));
            }
            _topology = new DependencyGraph<IActivityNodeDefinition>(captured, owner);
            foreach (IActivityNodeDefinition node in Nodes)
            {
                ((Node)node).BindDependencies(_topology.Dependencies(node.Id));
                // Validators receive complete captured metadata, not a half-constructed node.
                node.Executor.ValidateDefinition(node);
                bool physical = ActivityExecutorContracts.RequiresWorker(node) && !node.Duration.IsZero;
                foreach (string dependency in _topology.Dependencies(node.Id)) physical |= _physical[dependency];
                _physical.Add(node.Id, physical); _anyPhysical |= physical;
            }
        }

        public ActivityWorkAnalysis AnalyzeWork(ISimulationTime time, IReadOnlyDictionary<string, WorkProgress> progress)
        {
            if (time == null) throw new ArgumentNullException(nameof(time));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (progress.Count != Nodes.Count) throw new ArgumentException("progress must cover exactly the activity graph", nameof(progress));
            var captured = new Dictionary<string, WorkProgress>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, WorkProgress> entry in progress)
            {
                if (Find(entry.Key) == null) throw new ArgumentException("unknown progress identity '" + entry.Key + "'", nameof(progress));
                captured.Add(entry.Key, entry.Value);
            }
            BigInteger total = BigInteger.Zero, remaining = BigInteger.Zero;
            var weights = new Dictionary<string, BigInteger>(StringComparer.Ordinal);
            foreach (IActivityNodeDefinition node in Nodes)
            {
                if (!captured.TryGetValue(node.Id, out WorkProgress earned))
                    throw new ArgumentException("missing progress for '" + node.Id + "'", nameof(progress));
                long required = time.Ticks(node.Duration);
                BigInteger weight = earned.RemainingRaw(required);
                total += (BigInteger)required << Fix.FractionalBits; remaining += weight;
                weights.Add(node.Id, weight);
            }
            return new ActivityWorkAnalysis(total, remaining, _topology.CriticalPath(weights));
        }

        private sealed class Node : IActivityNodeDefinition
        {
            public Node(IActivityNodeDefinition source, string owner)
            {
                Id = source.Id; Duration = source.Duration; RequirementMultiplier = source.RequirementMultiplier;
                if (source.Roles == null) throw new ContentException(owner + ": node roles are required");
                var roles = new List<WorkerRoleId>(source.Roles); roles.Sort((a, b) => string.CompareOrdinal(a.Value, b.Value));
                for (int i = 0; i < roles.Count; i++)
                    if (!roles[i].IsValid || i > 0 && roles[i] == roles[i - 1]) throw new ContentException(owner + ": node roles must be valid and unique");
                Roles = roles.AsReadOnly();
                Executor = source.Executor ?? throw new ContentException(owner + ": node executor is required");
                Effect = source.Effect;
                if (!RequirementMultiplier.IsPositive || source.Dependencies == null)
                    throw new ContentException(owner + ": node '" + Id + "' has invalid typed requirements");
                Dependencies = source.Dependencies; // Captured once by the topology compiler before publishing.
                Requirement = source.Requirement;
            }
            public string Id { get; }
            public IActivityExecutor Executor { get; }
            public IReadOnlyList<string> Dependencies { get; private set; }
            internal void BindDependencies(IReadOnlyList<string> dependencies) => Dependencies = dependencies;
            public IReadOnlyList<WorkerRoleId> Roles { get; }
            public GameDuration Duration { get; }
            public IWorkRequirement Requirement { get; }
            public Fix RequirementMultiplier { get; }
            public string Effect { get; }
        }
    }
}
