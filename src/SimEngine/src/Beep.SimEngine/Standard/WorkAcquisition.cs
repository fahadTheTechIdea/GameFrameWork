using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Another file of WorkBoard, the existing native job/node acquisition owner.
    public sealed partial class WorkBoard
    {
        private sealed class TimedAdmission
        {
            public WorkRequirementContext Context;
            public WorkRequirementResult Result;
        }
        private bool PrepareTimedStarts(IReadOnlyList<(int Order, string Node)> intents, PlayerId company,
            out List<TimedAdmission> prepared, out Code blocker)
        {
            prepared = new List<TimedAdmission>(); blocker = Codes.InvalidArgument;
            var ids = new List<(int Order, string Node)>(intents);
            ids.Sort((a, b) => { int c = a.Order.CompareTo(b.Order); return c != 0 ? c : string.CompareOrdinal(a.Node, b.Node); });
            var seen = new HashSet<(int, string)>();
            foreach (var intent in ids)
            {
                Order order = Order(intent.Order); Entity target = _world.Entities.Get(order?.Target ?? EntityId.None);
                ActivityNodeExecution node = intent.Node == null ? null : order?.Node(intent.Node);
                IActivityNodeDefinition definition = intent.Node == null ? null : order?.Plan.Graph.Find(intent.Node);
                if (!seen.Add(intent) || order?.IsActive != true || order.Owner != company || target?.Alive != true ||
                    target.Owner != company || node == null || definition == null || definition.Duration.IsZero ||
                    ActivityExecutorContracts.RequiresWorker(definition) || node.JobId != 0 ||
                    node.State != NodeState.Waiting && node.State != NodeState.Ready) { blocker = Codes.InvalidArgument; return false; }
                blocker = PlanOrderBlocker(order.Id);
                if (blocker.IsNone) blocker = ResponseWork.CheckActiveOrder(_world, target, order);
                if (!blocker.IsNone) return false;
                if (!WorkSystem.DependenciesDone(order, definition)) { blocker = WorkSystem.BlockerDependencies; return false; }
                WorkRequirementResult result = EvaluateRequirements(company, target, definition, node, null);
                if (result.Status != WorkRequirementStatus.Satisfied) { blocker = result.Blocker; return false; }
                prepared.Add(new TimedAdmission { Result = result, Context = new WorkRequirementContext(_world,
                    company, target, definition, choices: node.Choices, execution: node) });
            }
            blocker = Code.None; return true;
        }
        private static void BindTimedStarts(IReadOnlyList<TimedAdmission> prepared)
        {
            foreach (var entry in prepared)
            {
                ActivityNodeExecution node = entry.Context.Execution;
                node.Choices = entry.Result.Choices; node.State = NodeState.Working; node.Blocker = Code.None;
            }
        }
        internal bool TryReadTimedAdmission(int orderId, string nodeId, out WorkRequirementResult result,
            out WorkRequirementContext context, out Code blocker)
        {
            result = default; context = default; blocker = Codes.InvalidArgument;
            Order order = Order(orderId); if (order == null) return false;
            if (!PrepareTimedStarts(Array.AsReadOnly(new[] { (orderId, nodeId) }), order.Owner, out var prepared, out blocker)) return false;
            result = prepared[0].Result; context = prepared[0].Context; return true;
        }
        internal bool StartWorkerless(Order order, IReadOnlyList<string> nodeIds, out Code blocker)
        {
            if (nodeIds == null || nodeIds.Count == 0) throw new ArgumentException("timed start requires a nonempty actual node set");
            blocker = Codes.InvalidArgument;
            if (order == null || !ReferenceEquals(Order(order.Id), order)) return false;
            var intents = new List<(int Order, string Node)>(); foreach (string id in nodeIds) intents.Add((order.Id, id));
            if (!PrepareTimedStarts(intents.AsReadOnly(), order.Owner, out var prepared, out blocker)) return false;
            var results = new List<WorkRequirementResult>(); var contexts = new List<WorkRequirementContext>();
            foreach (var entry in prepared) { results.Add(entry.Result); contexts.Add(entry.Context); }
            if (!CheckWorkSet(results.AsReadOnly(), contexts.AsReadOnly(), out var selected,
                out var resources, out Code dockBlocker, out _, out Code costBlocker))
            { blocker = !dockBlocker.IsNone ? dockBlocker : !resources.Ready ? ResourceRefusal(resources.Status).Blocker : costBlocker; return false; }
            if (!AcquireResources(selected, contexts.AsReadOnly(), out blocker)) return false;
            for (int i = 0; i < prepared.Count; i++) prepared[i].Result = selected[i];
            BindTimedStarts(prepared); return true;
        }

        private sealed class AssignmentAdmission
        {
            public Job Job;
            public IWorker Worker;
            public WorkWorkerCandidate Candidate;
            public WorkRequirementResult Result;
            public ActivityNodeExecution Node;
            public Code Slot;
        }
        internal bool Assign(IReadOnlyList<int> jobIds, IReadOnlyList<WorkWorkerCandidate> candidates,
            IReadOnlyList<Code?> selectedDocks)
            => Assign(jobIds, candidates, selectedDocks, Array.Empty<(int Order, string Node)>());
        internal bool Assign(IReadOnlyList<int> jobIds, IReadOnlyList<WorkWorkerCandidate> candidates,
            IReadOnlyList<Code?> selectedDocks, IReadOnlyList<(int Order, string Node)> timedNodes)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            var resources = new List<IReadOnlyList<WorkResourceChoice>>();
            foreach (var candidate in candidates) resources.Add(candidate.Result.Resources);
            return Assign(jobIds, candidates, selectedDocks, resources, timedNodes);
        }
        private bool PrepareNativeAdmission(IReadOnlyList<int> jobs, IReadOnlyList<WorkWorkerCandidate> machines,
            IReadOnlyList<Code?> docks, IReadOnlyList<IReadOnlyList<WorkResourceChoice>> sources,
            IReadOnlyList<(int Order, string Node)> timers, out List<AssignmentAdmission> prepared,
            out List<TimedAdmission> timed, out List<WorkRequirementResult> results, out List<WorkRequirementContext> contexts)
        {
            timed = new List<TimedAdmission>(); results = new List<WorkRequirementResult>(); contexts = new List<WorkRequirementContext>();
            if (!PrepareAssignments(jobs, machines, docks, sources, out prepared)) return false;
            PlayerId company = jobs.Count > 0 ? prepared[0].Job.Owner : Order(timers[0].Order)?.Owner ?? PlayerId.None;
            if (!PrepareTimedStarts(timers, company, out timed, out _)) return false;
            foreach (var entry in prepared) if (entry.Node != null)
            {
                Order order = Order(entry.Job.OrderId); results.Add(entry.Result);
                contexts.Add(new WorkRequirementContext(_world, company, _world.Entities.Get(entry.Job.Target),
                    order.Plan.Graph.Find(entry.Node.Id), entry.Worker.Entity, entry.Result.Choices, entry.Node, entry.Candidate.Role));
            }
            foreach (var entry in timed) { results.Add(entry.Result); contexts.Add(entry.Context); }
            IReadOnlyList<WorkRequirementResult> selected = results.AsReadOnly();
            if ((contexts.Count > 1 || timers.Count > 0) &&
                !CheckWorkSet(results.AsReadOnly(), contexts.AsReadOnly(), out selected, out _, out _, out _, out _)) return false;
            int index = 0;
            foreach (var entry in prepared) if (entry.Node != null) entry.Result = selected[index++];
            foreach (var entry in timed) entry.Result = selected[index++];
            results = new List<WorkRequirementResult>(selected);
            return PrepareResourceAcquisition(results.AsReadOnly(), contexts.AsReadOnly(), out _, out _, out _, out _);
        }
        private bool Assign(IReadOnlyList<int> jobIds, IReadOnlyList<WorkWorkerCandidate> candidates,
            IReadOnlyList<Code?> selectedDocks, IReadOnlyList<IReadOnlyList<WorkResourceChoice>> resources,
            IReadOnlyList<(int Order, string Node)> timedNodes)
        {
            if (jobIds == null || candidates == null || selectedDocks == null || resources == null || timedNodes == null ||
                jobIds.Count + timedNodes.Count == 0 || jobIds.Count != candidates.Count ||
                jobIds.Count != selectedDocks.Count || jobIds.Count != resources.Count)
                throw new ArgumentException("native assignment requires an aligned nonempty actual set");
            var jobs = new List<int>(jobIds); var machines = new List<WorkWorkerCandidate>(candidates);
            var docks = new List<Code?>(selectedDocks); var timers = new List<(int Order, string Node)>(timedNodes);
            var sourceChoices = new List<IReadOnlyList<WorkResourceChoice>>();
            foreach (var selection in resources) sourceChoices.Add(selection == null ? null : new List<WorkResourceChoice>(selection).AsReadOnly());
            if (!PrepareNativeAdmission(jobs, machines, docks, sourceChoices, timers, out var prepared, out var timed, out var results, out var contexts)) return false;
            bool interrupted = false;
            foreach (var entry in prepared)
                if (entry.Candidate.InterruptedJob != 0)
                {
                    var offer = WorkSystem.Assignment(new WorkDispatchOffer(entry.Job, Order(entry.Job.OrderId), _world.Tick,
                        entry.Candidate, WorkRequirementStatus.Satisfied, Code.None, IsEmergency(entry.Job)), this, _world.Tick);
                    _world.Services.Get<IWorkerDispatch>().InterruptForDispatch(_world, offer); interrupted = true;
                }
            if (interrupted)
            {
                for (int i = 0; i < machines.Count; i++)
                {
                    var before = machines[i];
                    machines[i] = new WorkWorkerCandidate(before.Worker, before.Role, before.TravelCost, before.Result, before.Priority, 0);
                }
                if (!PrepareNativeAdmission(jobs, machines, docks, sourceChoices, timers, out prepared, out timed, out results, out contexts))
                    throw new InvalidOperationException("emergency interruption lost complete native team admission");
            }
            if (!AcquireResources(results.AsReadOnly(), contexts.AsReadOnly(), out _))
            {
                if (interrupted) throw new InvalidOperationException("emergency team resource/budget admission changed");
                return false;
            }
            foreach (var entry in prepared)
            {
                Assign(entry.Job, entry.Worker, entry.Candidate.Role);
                entry.Job.DestinationDockSlot = entry.Slot;
                if (entry.Node != null) entry.Node.Choices = entry.Result.Choices;
            }
            BindTimedStarts(timed); return true;
        }
        private bool PrepareAssignments(IReadOnlyList<int> jobs, IReadOnlyList<WorkWorkerCandidate> candidates,
            IReadOnlyList<Code?> docks, IReadOnlyList<IReadOnlyList<WorkResourceChoice>> resources,
            out List<AssignmentAdmission> prepared)
        {
            prepared = new List<AssignmentAdmission>(); var identities = new HashSet<int>(); var workers = new HashSet<EntityId>();
            PlayerId company = PlayerId.None;
            // The complete shape/current ownership is validated before extension callbacks.
            for (int i = 0; i < jobs.Count; i++)
            {
                Job job = Job(jobs[i]); var candidate = candidates[i];
                Entity worker = _world.Entities.Get(candidate.Worker), target = _world.Entities.Get(job?.Target ?? EntityId.None);
                if (job == null || !identities.Add(job.Id) || !workers.Add(candidate.Worker) || job.IsHaul ||
                    job.State != JobState.Open || job.RetryAt > _world.Tick || worker?.Alive != true || target?.Alive != true ||
                    worker.Get<IWorker>() == null || worker.Owner != job.Owner || target.Owner != job.Owner ||
                    candidate.Result.Status != WorkRequirementStatus.Satisfied || worker.Get<IWorker>().Job != candidate.InterruptedJob ||
                    i > 0 && job.Owner != company) return false;
                company = job.Owner;
            }
            var proposals = new List<DockProposal>(); var results = new List<WorkRequirementResult>();
            var contexts = new List<WorkRequirementContext>(); var fresh = new List<WorkWorkerCandidate>();
            for (int i = 0; i < jobs.Count; i++)
            {
                Job job = Job(jobs[i]); var requested = candidates[i]; Entity worker = _world.Entities.Get(requested.Worker);
                if (!WorkerExecutionContracts.Can(_world, worker, requested.Role) || !CheckWorkerSelection(job, worker, requested.Role).IsNone)
                    throw new InvalidOperationException("candidate differs from actual selected machine or role");
                Order order = Order(job.OrderId); ActivityNodeExecution execution = order?.Node(job.NodeId);
                IActivityNodeDefinition definition = order?.Plan.Graph.Find(job.NodeId);
                if (order != null && (!order.IsActive || execution?.JobId != job.Id ||
                    !ResponseWork.CheckActiveOrder(_world, _world.Entities.Get(job.Target), order).IsNone)) return false;
                _world.Services.Get<IWorkerDispatch>().CopyCandidates(_world, job.Owner,
                    definition?.Roles ?? Array.AsReadOnly(new[] { job.Role }), _world.Entities.Get(job.Target), definition, job.Id,
                    execution != null && HasSpent(execution) ? execution.Choices : requested.Result.Choices,
                    execution, requested.InterruptedJob == 0 ? WorkCandidateMode.IdleOnly : WorkCandidateMode.EmergencyInterruption, fresh, null);
                int chosen = fresh.FindIndex(candidate => candidate.Worker == requested.Worker && candidate.Role == requested.Role &&
                    candidate.InterruptedJob == requested.InterruptedJob && candidate.Result.Status == WorkRequirementStatus.Satisfied);
                if (chosen < 0) return false;
                WorkWorkerCandidate current = fresh[chosen]; WorkRequirementResult result = current.Result;
                if (execution != null)
                {
                    var context = new WorkRequirementContext(_world, job.Owner, _world.Entities.Get(job.Target), definition,
                        worker, result.Choices, execution, current.Role);
                    if (resources[i] != null) result = BindResourceSelection(result, resources[i], context);
                    if (result.Status != WorkRequirementStatus.Satisfied) return false;
                    results.Add(result); contexts.Add(context);
                }
                Entity target = _world.Entities.Get(job.Target);
                if (!PreviewDock(target, worker, WorkDockRequest.Work(current.Role), null, out Code slot,
                    out _, out _, out _, productiveOnly: docks[i].HasValue || jobs.Count > 1,
                    requiredSlot: docks[i], proposals: proposals).IsNone) return false;
                IWorkDock dock = Dock(target); if (dock != null) proposals.Add(new DockProposal(dock, slot));
                prepared.Add(new AssignmentAdmission { Job = job, Worker = worker.Get<IWorker>(), Candidate = current,
                    Result = result, Node = execution, Slot = slot });
            }
            if (jobs.Count > 1 && !CheckWorkSet(results.AsReadOnly(), contexts.AsReadOnly(), out _, out _, out _, out _, out _)) return false;
            return PrepareResourceAcquisition(results.AsReadOnly(), contexts.AsReadOnly(), out _, out _, out _, out _);
        }
    }
}
