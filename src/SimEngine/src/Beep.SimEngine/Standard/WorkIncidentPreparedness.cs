using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IIncidentPreparedness
    {
        /// <summary>Current target/DAG facts only. Does not raise, post, preempt, acquire or guarantee future response.</summary>
        Code ReadPreparedness(EntityId target, Code source, out IncidentPreparednessSnapshot snapshot);
    }
    public sealed class IncidentPreparednessNode
    {
        public string Id { get; }
        public IReadOnlyList<string> Dependencies { get; }
        public NodeState? State { get; }
        public bool Frontier { get; }
        public bool NeedsWorker { get; }
        public WorkRequirementResult Criteria { get; }
        public IReadOnlyList<WorkWorkerCandidate> Candidates { get; }
        /// <summary>Illustrative current distinct coverage, not acquired assignment authority.</summary>
        public EntityId CoveredWorker { get; }
        /// <summary>Illustrative selected role for CoveredWorker, never a live assignment.</summary>
        public WorkerRoleId CoveredRole { get; }
        internal IncidentPreparednessNode(IActivityNodeDefinition node, ActivityNodeExecution execution,
            bool frontier, bool needsWorker, WorkRequirementResult criteria, IEnumerable<WorkWorkerCandidate> candidates,
            EntityId covered, WorkerRoleId role)
        {
            Id = node.Id; Dependencies = new List<string>(node.Dependencies).AsReadOnly(); State = execution?.State;
            Frontier = frontier; NeedsWorker = needsWorker; Criteria = criteria;
            Candidates = new List<WorkWorkerCandidate>(candidates).AsReadOnly(); CoveredWorker = covered; CoveredRole = role;
        }
    }
    /// <summary>Detached current service-slot intent, not an acquired claim or provider handle.</summary>
    public readonly struct IncidentPreparednessDock
    {
        public string Node { get; }
        public EntityId Worker { get; }
        public EntityId Endpoint { get; }
        public Code Slot { get; }
        internal IncidentPreparednessDock(string node, EntityId worker, EntityId endpoint, Code slot)
        { Node = node; Worker = worker; Endpoint = endpoint; Slot = slot; }
    }
    public sealed class IncidentPreparednessSnapshot
    {
        public EntityId Target { get; }
        public Code Source { get; }
        public string Plan { get; }
        public IncidentSnapshot Incident { get; }
        public int OrderId { get; }
        public OrderState? OrderState { get; }
        public long ObservedUpdate { get; }
        public bool Prospective => !Incident.Active;
        public Code Blocker { get; }
        public IReadOnlyList<IncidentPreparednessNode> Nodes { get; }
        public int FrontierNodes { get; }
        public int FrontierWorkersRequired { get; }
        public int FrontierWorkersCovered { get; }
        public int FrontierWorkerShortfall => FrontierWorkersRequired - FrontierWorkersCovered;
        /// <summary>Whole native material set for selected covered workers; null when criteria/coverage prevents evaluation.</summary>
        public ResourceClaimCheck? FrontierResources { get; }
        /// <summary>Null means unevaluated; None means joint current productive slots feasible, not acquired.</summary>
        public Code? FrontierDockBlocker { get; }
        public IReadOnlyList<IncidentPreparednessDock> FrontierDocks { get; }
        /// <summary>Null means unevaluated; None means combined current optional obligations fit, not reserved.</summary>
        public Code? FrontierCostBlocker { get; }
        /// <summary>Current criteria, distinct coverage, joint materials/docks/optional costs; no team/future acquisition guarantee.</summary>
        public bool ReadyForFrontier { get; }
        internal IncidentPreparednessSnapshot(Entity target, Code source, IActivityDefinition plan, IncidentSnapshot incident,
            Order order, long observed, Code blocker, IEnumerable<IncidentPreparednessNode> nodes, ResourceClaimCheck? resources,
            Code? dockBlocker, IReadOnlyList<IncidentPreparednessDock> docks, Code? costBlocker)
        {
            Target = target.Id; Source = source; Plan = plan.PlanKey; Incident = incident; OrderId = order?.Id ?? 0;
            OrderState = order?.State; ObservedUpdate = observed; Blocker = blocker;
            var rows = new List<IncidentPreparednessNode>(nodes); bool criteria = true; int frontier = 0, required = 0, covered = 0;
            foreach (var row in rows) if (row.Frontier)
            {
                frontier++; criteria &= row.Criteria.Status == WorkRequirementStatus.Satisfied;
                if (row.NeedsWorker) { required++; if (row.CoveredWorker.IsValid) covered++; }
            }
            Nodes = rows.AsReadOnly(); FrontierNodes = frontier; FrontierWorkersRequired = required; FrontierWorkersCovered = covered;
            FrontierResources = resources;
            FrontierDockBlocker = dockBlocker; FrontierDocks = new List<IncidentPreparednessDock>(docks).AsReadOnly();
            FrontierCostBlocker = costBlocker;
            ReadyForFrontier = blocker.IsNone && frontier > 0 && criteria && required == covered && resources.HasValue && resources.Value.Ready &&
                dockBlocker.HasValue && dockBlocker.Value.IsNone && costBlocker.HasValue && costBlocker.Value.IsNone;
        }
    }
    public sealed partial class WorkBoard : IIncidentPreparedness
    {
        public Code ReadPreparedness(EntityId targetId, Code source, out IncidentPreparednessSnapshot snapshot)
        {
            snapshot = null; Entity target = _world.Entities.Get(targetId);
            if (target == null || !target.Alive) return Codes.NotFound;
            if (source.IsNone) return Codes.InvalidArgument;
            var response = target.Get<IIncidentResponse>(source); if (response == null) return IncidentCodes.NotResponse;
            var result = EntityObservation.Read(_world, target, () => CapturePreparedness(target, response));
            snapshot = result; return Code.None;
        }
        private IncidentPreparednessSnapshot CapturePreparedness(Entity target, IIncidentResponse response)
        {
            IActivityDefinition plan = response.ResponsePlan; ResponseWork.ValidatePlan(plan);
            if (!ReferenceEquals(ResolvePlan(plan.PlanKey), plan)) throw new InvalidOperationException("preparedness requires actual composed response definition");
            IncidentSnapshot incident = ResponseWork.Read(response);
            Order order = incident.Active ? ResponseOn(target.Id, response.CapabilityId, incident.Episode) : null;
            Code binding = order == null ? Code.None : ResponseWork.CheckActiveOrder(_world, target, order);
            if (binding.IsNone && order != null && !order.IsActive) binding = order.Blocker.IsNone ? IncidentCodes.StaleResponse : order.Blocker;
            if (binding.IsNone && order?.State == OrderState.Completing && !order.Blocker.IsNone) binding = order.Blocker;
            var definitions = plan.Graph.Nodes; var frontier = new bool[definitions.Count]; var needs = new bool[definitions.Count];
            var criteria = new WorkRequirementResult[definitions.Count]; var offers = new List<IReadOnlyList<WorkWorkerCandidate>>();
            var indices = new List<int>(); var capacities = new List<int>(); var edges = new List<IReadOnlyList<EntityId>>();
            for (int i = 0; i < definitions.Count; i++)
            {
                var node = definitions[i]; var execution = order?.Node(node.Id); bool done = execution?.State == NodeState.Done;
                bool dependencies = order == null ? node.Dependencies.Count == 0 : WorkSystem.DependenciesDone(order, node);
                frontier[i] = binding.IsNone && !done && dependencies;
                needs[i] = node.Roles.Count > 0 && !done && execution?.State != NodeState.Completing;
                var candidates = new List<WorkWorkerCandidate>(); var eligible = new List<EntityId>();
                if (!binding.IsNone) criteria[i] = WorkRequirementResult.Blocked(binding);
                else if (done) criteria[i] = WorkRequirementResult.Satisfied();
                else if (execution?.State == NodeState.Completing)
                    criteria[i] = execution.Blocker.IsNone ? WorkRequirementResult.Satisfied() : WorkRequirementResult.Blocked(execution.Blocker);
                else if (node.Roles.Count == 0)
                    criteria[i] = EvaluateRequirements(target.Owner, target, node, execution, null);
                else
                {
                    _world.Services.Get<IWorkerDispatch>().CopyResponseCandidates(_world, target.Id, response.CapabilityId, node.Id, candidates);
                    criteria[i] = candidates.Count == 0 ? WorkRequirementResult.Pending(WorkSystem.BlockerNoWorker) : candidates[0].Result;
                    EntityId incumbent = Job(execution?.JobId ?? 0)?.Worker ?? EntityId.None;
                    if (incumbent.IsValid)
                    {
                        criteria[i] = WorkRequirementResult.Pending(WorkSystem.BlockerNoWorker);
                        foreach (var candidate in candidates) if (candidate.Worker == incumbent) { criteria[i] = candidate.Result; break; }
                    }
                    var distinct = new HashSet<EntityId>();
                    foreach (var candidate in candidates)
                        if (candidate.Result.Status == WorkRequirementStatus.Satisfied && (!incumbent.IsValid || incumbent == candidate.Worker) &&
                            distinct.Add(candidate.Worker)) eligible.Add(candidate.Worker);
                }
                offers.Add(candidates.AsReadOnly());
                if (frontier[i] && needs[i]) { indices.Add(i); capacities.Add(1); edges.Add(eligible.AsReadOnly()); }
            }
            EntityId[] Coverage(IReadOnlyList<DistinctWorkerMatch> matches)
            {
                var workers = new EntityId[definitions.Count];
                foreach (var match in matches) workers[indices[match.Demand]] = match.Worker;
                return workers;
            }
            bool Evaluate(EntityId[] workers, out WorkRequirementResult[] projected, out ResourceClaimCheck? resources,
                out Code? dockBlocker, out IReadOnlyList<IncidentPreparednessDock> docks, out Code? costBlocker,
                out WorkerRoleId[] roles)
            {
                projected = (WorkRequirementResult[])criteria.Clone();
                roles = new WorkerRoleId[definitions.Count];
                var resourceIndices = new List<int>(); var resourceResults = new List<WorkRequirementResult>();
                var alternatives = new List<IReadOnlyList<WorkWorkerCandidate>>();
                var contexts = new List<WorkRequirementContext>(); bool canCheck = binding.IsNone;
                for (int i = 0; i < definitions.Count; i++) if (frontier[i])
                {
                    var node = definitions[i]; var execution = order?.Node(node.Id); WorkerRoleId role = WorkerRoleId.None;
                    var options = new List<WorkWorkerCandidate>();
                    if (needs[i])
                    {
                        if (!workers[i].IsValid) { canCheck = false; continue; }
                        foreach (var candidate in offers[i]) if (candidate.Worker == workers[i] && candidate.Result.Status == WorkRequirementStatus.Satisfied)
                            options.Add(candidate);
                        if (options.Count == 0) { canCheck = false; continue; }
                        projected[i] = options[0].Result; role = options[0].Role; roles[i] = role;
                    }
                    if (projected[i].Status != WorkRequirementStatus.Satisfied) { canCheck = false; continue; }
                    // Completing nodes already earned/released their materials; effects own their wait.
                    if (execution?.State == NodeState.Completing) continue;
                    resourceIndices.Add(i); resourceResults.Add(projected[i]);
                    alternatives.Add(options.AsReadOnly());
                    contexts.Add(new WorkRequirementContext(_world, target.Owner, target, node,
                        _world.Entities.Get(workers[i]), execution?.Choices, execution, role));
                }
                resources = null; dockBlocker = null; docks = Array.Empty<IncidentPreparednessDock>(); costBlocker = null;
                if (!canCheck) return false;
                bool ready = CheckWorkSet(resourceResults, contexts, alternatives, out var chosen, out var selected, out var material,
                    out var dock, out docks, out var cost);
                resources = material; dockBlocker = dock; costBlocker = cost;
                for (int i = 0; i < selected.Count; i++)
                { projected[resourceIndices[i]] = selected[i]; if (chosen[i].HasValue) roles[resourceIndices[i]] = chosen[i].Value.Role; }
                return ready;
            }
            var covered = Coverage(DistinctWorkerMatching.Solve(capacities, edges, matches =>
                matches.Count < indices.Count || Evaluate(Coverage(matches), out _, out _, out _, out _, out _, out _)));
            Evaluate(covered, out var finalCriteria, out var resources, out var dockBlocker, out var docks, out var costBlocker, out var finalRoles);
            var rows = new List<IncidentPreparednessNode>();
            for (int i = 0; i < definitions.Count; i++) rows.Add(new IncidentPreparednessNode(definitions[i], order?.Node(definitions[i].Id),
                frontier[i], needs[i], finalCriteria[i], offers[i], covered[i], finalRoles[i]));
            return new IncidentPreparednessSnapshot(target, response.CapabilityId, plan, incident, order, _world.Clock.StepIndex, binding, rows, resources, dockBlocker, docks, costBlocker);
        }
    }
}
