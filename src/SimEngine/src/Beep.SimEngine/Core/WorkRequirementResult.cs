using System;
using System.Collections.Generic;
namespace Beep.SimEngine
{
    public enum WorkRequirementStatus : byte { Invalid = 0, Satisfied = 1, Pending = 2, Blocked = 3 }

    /// <summary>An explicitly measured quantity; absent observations are not fabricated zero readings.</summary>
    public readonly struct WorkRequirementQuantity
    {
        public Fix Available { get; }
        public Fix Required { get; }
        public Code Unit { get; }
        public WorkRequirementQuantity(Fix available, Fix required, Code unit)
        {
            if (unit.IsNone) throw new ArgumentException("a requirement quantity needs a unit", nameof(unit));
            Available = available; Required = required; Unit = unit;
        }
    }

    /// <summary>A stable group occurrence and chosen entry; contains no runtime/provider handle.</summary>
    public sealed class WorkRequirementChoice
    {
        public IReadOnlyList<string> Path { get; }
        public string Entry { get; }
        public WorkRequirementChoice(IEnumerable<string> path, string entry)
        {
            Path = WorkRequirementPath.Copy(path);
            if (string.IsNullOrWhiteSpace(entry)) throw new ArgumentException("choice requires a group entry");
            Entry = entry;
        }
        internal bool At(IReadOnlyList<string> path)
        {
            return WorkRequirementPath.Matches(Path, path);
        }
    }

    /// <summary>Readiness, not a reservation or a second resource ledger.</summary>
    public readonly struct WorkRequirementResult
    {
        public WorkRequirementStatus Status { get; }
        public Code Blocker { get; }
        public WorkRequirementQuantity? Quantity { get; }
        /// <summary>Pure chosen entry for a satisfied any expression, not a committed provider.</summary>
        public string SelectedEntry { get; }
        public IReadOnlyList<WorkRequirementChoice> Choices => _choices ?? Array.Empty<WorkRequirementChoice>();
        private readonly IReadOnlyList<WorkRequirementChoice> _choices;
        private readonly IReadOnlyList<WorkResourceChoice> _resources;
        private readonly IReadOnlyList<WorkResourceCandidate> _resourceCandidates;
        private readonly IReadOnlyList<WorkCostChoice> _costs;
        public IReadOnlyList<WorkCostChoice> Costs => _costs ?? Array.Empty<WorkCostChoice>();
        public IReadOnlyList<WorkResourceChoice> Resources => _resources ?? Array.Empty<WorkResourceChoice>();
        public IReadOnlyList<WorkResourceCandidate> ResourceCandidates => _resourceCandidates ?? Array.Empty<WorkResourceCandidate>();
        private WorkRequirementResult(WorkRequirementStatus status, Code blocker, WorkRequirementQuantity? quantity, string selectedEntry = null,
            IReadOnlyList<WorkRequirementChoice> choices = null, IReadOnlyList<WorkResourceChoice> resources = null,
            IReadOnlyList<WorkResourceCandidate> resourceCandidates = null, IReadOnlyList<WorkCostChoice> costs = null)
        {
            if (status != WorkRequirementStatus.Satisfied && blocker.IsNone)
                throw new ArgumentException("an unsatisfied requirement needs a blocker", nameof(blocker));
            if (quantity.HasValue && quantity.Value.Unit.IsNone)
                throw new ArgumentException("a requirement quantity needs a unit", nameof(quantity));
            Status = status; Blocker = blocker; Quantity = quantity; SelectedEntry = selectedEntry; _choices = choices;
            _resources = resources; _resourceCandidates = resourceCandidates;
            _costs = costs;
        }
        internal WorkRequirementResult Select(string entry)
            => new WorkRequirementResult(Status, Blocker, Quantity, entry, _choices, _resources, _resourceCandidates, _costs);
        internal WorkRequirementResult WithChoices(IReadOnlyList<WorkRequirementChoice> choices)
            => new WorkRequirementResult(Status, Blocker, Quantity, SelectedEntry, choices, _resources, _resourceCandidates, _costs);
        internal WorkRequirementResult WithResources(IReadOnlyList<WorkResourceChoice> resources, IReadOnlyList<WorkResourceCandidate> candidates = null)
            => new WorkRequirementResult(Status, Blocker, Quantity, SelectedEntry, _choices, resources, candidates, _costs);
        internal WorkRequirementResult WithCosts(IReadOnlyList<WorkCostChoice> costs)
            => new WorkRequirementResult(Status, Blocker, Quantity, SelectedEntry, _choices, _resources, _resourceCandidates, costs);
        public static WorkRequirementResult Satisfied(WorkRequirementQuantity? quantity = null)
            => new WorkRequirementResult(WorkRequirementStatus.Satisfied, Code.None, quantity);
        public static WorkRequirementResult Pending(Code blocker, WorkRequirementQuantity? quantity = null)
            => new WorkRequirementResult(WorkRequirementStatus.Pending, blocker, quantity);
        public static WorkRequirementResult Blocked(Code blocker, WorkRequirementQuantity? quantity = null)
            => new WorkRequirementResult(WorkRequirementStatus.Blocked, blocker, quantity);
        public static WorkRequirementResult Invalid(Code blocker)
            => new WorkRequirementResult(WorkRequirementStatus.Invalid, blocker, null);
        internal bool IsWellFormed => Status == WorkRequirementStatus.Satisfied ? Blocker.IsNone
            : !Blocker.IsNone && (Status == WorkRequirementStatus.Pending || Status == WorkRequirementStatus.Blocked || Status == WorkRequirementStatus.Invalid);
    }

}
