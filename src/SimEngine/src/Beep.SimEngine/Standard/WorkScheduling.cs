using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkSystem
    {
        public static readonly Code BlockerRetry = new Code("work.retry-deferred");

        private static IReadOnlyList<IWorkerDispatchSource> DispatchSources(World world)
        {
            var sources = new List<IWorkerDispatchSource>(world.Services.Providers<IWorkerDispatchSource>());
            var scopes = new HashSet<string>(StringComparer.Ordinal) { WorkModule.ModuleId };
            foreach (IWorkerDispatchSource source in sources)
            {
                ContractIdentity.ValidateId(source.DispatchScope, "worker dispatch source");
                if (!ReferenceEquals(source.World, world) || !scopes.Add(source.DispatchScope))
                    throw new InvalidOperationException("duplicate or foreign worker dispatch source");
                Schedule schedule = source.GetDispatchSchedule(world.Time);
                if (schedule.Kind == ScheduleKind.Spread || schedule.Period < 1 || schedule.Phase < 0 || schedule.Phase >= schedule.Period)
                    throw new InvalidOperationException("worker dispatch sources require a valid whole-pass schedule");
            }
            sources.Sort((a, b) => string.CompareOrdinal(a.DispatchScope, b.DispatchScope));
            return sources.AsReadOnly();
        }
        public void ValidateComposition(World world)
        {
            ValidateWorkPhases(world);
            foreach (IActivityExecutor executor in world.Content.All<IActivityExecutor>())
                ActivityExecutorContracts.Read(world, executor, null, null, () => {
                    executor.ValidateComposition(world); return true;
                });
            WorkerExecutionContracts.ValidateDefinitions(world);
            foreach (EntityDef entity in world.Content.All<EntityDef>()) foreach (ComponentDef part in entity.Components)
            {
                if (typeof(IWorkDock).IsAssignableFrom(part.ComponentType) != (part is IWorkDockDefinition))
                    throw new ContentException(entity.Id + ": physical dock must declare matching definition/runtime capability");
                if (part is IWorkDockDefinition) WorkDockRules.ValidateComposition(world, entity);
            }
            IReadOnlyList<IWorkerDispatchSource> sources = DispatchSources(world);
            if (world.Services.TryGet<ICompanyAlertPolicy>(out var alerts) && !ReferenceEquals(alerts.World, world))
                throw new InvalidOperationException("company alert policy belongs to another session");
            if (world.Services.TryGet<ICompanyWorkerReservePolicy>(out var reserves) && !ReferenceEquals(reserves.World, world))
                throw new InvalidOperationException("company worker reserve policy belongs to another session");
            if (world.Services.TryGet<IWorkerDispatchPolicy>(out var policy))
            {
                if (!ReferenceEquals(world.Content.Find<IWorkerDispatchPolicy>(policy.Id), policy) || policy.DispatchScopes == null)
                    throw new ContentException("dispatch policy must alias its selected immutable content");
                if (policy.Ordering != WorkerDispatchOrdering.PriorityFirst && policy.Ordering != WorkerDispatchOrdering.StarvationFirst)
                    throw new ContentException("dispatch policy must declare valid ordering");
                var scopes = new HashSet<string>(StringComparer.Ordinal);
                foreach (string scope in policy.DispatchScopes)
                {
                    try { ContractIdentity.ValidateId(scope, "dispatch policy scope"); }
                    catch (ArgumentException ex) { throw new ContentException(ex.Message); }
                    if (!scopes.Add(scope)) throw new ContentException("duplicate dispatch policy scope");
                    bool found = scope == WorkModule.ModuleId;
                    foreach (IWorkerDispatchSource source in sources) if (source.DispatchScope == scope) found = true;
                    if (!found) throw new ContentException("unknown dispatch policy scope '" + scope + "'");
                }
                policy.ValidateTime(world.Time);
            }
            if (world.Services.TryGet<IWorkerDispatchPreemptionPolicy>(out var preemption) &&
                (!world.Services.TryGet<IWorkerDispatchPolicy>(out var selected) || !ReferenceEquals(selected, preemption)))
                throw new ContentException("preemption policy must alias the selected immutable dispatch content");
        }

        internal static WorkerDispatchOffer Assignment(WorkDispatchOffer offer, WorkBoard board, long tick)
            => new WorkerDispatchOffer(WorkModule.ModuleId, offer.Job, "job", board.Job(offer.Job).Owner,
                offer.Target, offer.Worker, offer.Status == WorkRequirementStatus.Satisfied, offer.Blocker,
                offer.Emergency, offer.Priority, offer.CreatedTick, tick, offer.TravelCost, 1, offer.InterruptedJob, true);

        private static WorkerDispatchOffer RankOffer(World world, WorkerDispatchOffer offer)
        {
            if (world.Services.TryGet<ICompanyAlertPolicy>(out var alerts)) offer = offer.WithEffectivePriority(alerts.EffectivePriority(offer));
            if (world.Services.TryGet<IWorkerDispatchPolicy>(out var policy))
            {
                if (policy.Ordering != WorkerDispatchOrdering.PriorityFirst && policy.Ordering != WorkerDispatchOrdering.StarvationFirst)
                    throw new InvalidOperationException("dispatch policy returned invalid ordering");
                offer = offer.WithOrdering(policy.Ordering);
                long? threshold = policy.StarvationAfter(world.Time, offer);
                bool declared = false;
                foreach (string scope in policy.DispatchScopes) if (scope == offer.Scope) declared = true;
                if (threshold.HasValue && (threshold.Value <= 0 || !declared))
                    throw new InvalidOperationException("dispatch policy returned a nonpositive or undeclared starvation threshold");
                offer = offer.WithStarvation(threshold);
            }
            return offer;
        }

        public void CopyAssignments(World world, PlayerId viewer, IList<WorkerDispatchOffer> destination)
        {
            if (!ReferenceEquals(world.Services.Get<IWorkDispatchQueue>(), this)) throw new InvalidOperationException("assignment queue belongs to another session");
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); var offers = new List<WorkerDispatchOffer>(); var work = new List<WorkDispatchOffer>();
            WorkBoard board = world.Services.Get<WorkBoard>(); BuildQueue(world, viewer, work, WorkCandidateMode.EmergencyInterruption);
            foreach (WorkDispatchOffer offer in work) offers.Add(Assignment(offer, board, world.Tick));
            foreach (Order order in board.Orders)
                if (order.IsActive && (!viewer.IsValid || viewer == order.Owner))
                    foreach (IActivityNodeDefinition definition in order.Plan.Graph.Nodes)
                    {
                        ActivityNodeExecution node = order.Node(definition.Id);
                        if (definition.Duration.IsZero || ActivityExecutorContracts.RequiresWorker(definition) ||
                            node.State != NodeState.Waiting && node.State != NodeState.Ready || !DependenciesDone(order, definition)) continue;
                        bool ready = board.TryReadTimedAdmission(order.Id, node.Id, out _, out _, out Code blocker);
                        offers.Add(new WorkerDispatchOffer(WorkModule.ModuleId, order.Id, "node." + node.Id, order.Owner,
                            order.Target, EntityId.None, ready, blocker, board.IsEmergency(order), order.Priority,
                            order.CreatedTick, world.Tick, 0, 1, 0, false));
                    }
            foreach (IWorkerDispatchSource source in DispatchSources(world))
            {
                var current = new List<WorkerDispatchOffer>(); source.CopyOffers(viewer, WorkCandidateMode.EmergencyInterruption, current);
                foreach (WorkerDispatchOffer offer in current)
                {
                    Entity target = world.Entities.Get(offer.Target), worker = world.Entities.Get(offer.Worker);
                    if (!offer.RequiresWorker || offer.Scope != source.DispatchScope || offer.Identity <= 0 || offer.Entry == null || offer.CreatedTick < 0 ||
                        offer.StarvationAfterTicks.HasValue || offer.Starved || offer.EffectivePriority != offer.Priority || offer.Ordering != WorkerDispatchOrdering.PriorityFirst ||
                        offer.CreatedTick > world.Tick || offer.AgeTicks != world.Tick - offer.CreatedTick ||
                        !world.Players.IsSeat(offer.Company) || viewer.IsValid && offer.Company != viewer ||
                        target != null && target.Owner != offer.Company || offer.Ready && (target == null || worker?.Owner != offer.Company ||
                            worker.Get<IWorker>() == null || worker.Get<IWorker>().Job != offer.InterruptedJob || worker.Get<IWorker>().Held) ||
                        offer.InterruptedJob != 0 && PreviewInterruption(world, offer) != offer.InterruptedJob || offer.AttemptsPerPass < 1)
                        throw new InvalidOperationException("invalid/private worker dispatch source offer");
                    offers.Add(offer);
                }
            }
            var identities = new HashSet<(string, long, string)>();
            foreach (WorkerDispatchOffer offer in offers)
                if (!identities.Add((offer.Scope, offer.Identity, offer.Entry))) throw new InvalidOperationException("duplicate worker dispatch offer identity");
            for (int i = 0; i < offers.Count; i++) offers[i] = RankOffer(world, offers[i]);
            offers.Sort(CompareAssignments); foreach (WorkerDispatchOffer offer in offers) destination.Add(offer);
        }

        internal static int CompareAssignments(WorkerDispatchOffer a, WorkerDispatchOffer b)
        {
            if (a.Ordering != b.Ordering || a.Ordering != WorkerDispatchOrdering.PriorityFirst && a.Ordering != WorkerDispatchOrdering.StarvationFirst)
                throw new InvalidOperationException("dispatch comparison requires common engine-stamped ordering");
            int comparison = b.Ready.CompareTo(a.Ready); if (comparison != 0) return comparison;
            comparison = b.Emergency.CompareTo(a.Emergency); if (comparison != 0) return comparison;
            if (a.Ordering == WorkerDispatchOrdering.PriorityFirst)
            { comparison = b.EffectivePriority.CompareTo(a.EffectivePriority); if (comparison != 0) return comparison; }
            comparison = b.Starved.CompareTo(a.Starved); if (comparison != 0) return comparison;
            if (a.Starved) { comparison = a.CreatedTick.CompareTo(b.CreatedTick); if (comparison != 0) return comparison; }
            if (a.Ordering == WorkerDispatchOrdering.StarvationFirst)
            { comparison = b.EffectivePriority.CompareTo(a.EffectivePriority); if (comparison != 0) return comparison; }
            comparison = a.CreatedTick.CompareTo(b.CreatedTick); if (comparison != 0) return comparison;
            comparison = (a.TravelCost ?? long.MaxValue).CompareTo(b.TravelCost ?? long.MaxValue); if (comparison != 0) return comparison;
            comparison = a.Target.Value.CompareTo(b.Target.Value); if (comparison != 0) return comparison;
            comparison = a.Company.Value.CompareTo(b.Company.Value); if (comparison != 0) return comparison;
            comparison = string.CompareOrdinal(a.Scope, b.Scope); if (comparison != 0) return comparison;
            comparison = a.Identity.CompareTo(b.Identity);
            return comparison != 0 ? comparison : string.CompareOrdinal(a.Entry, b.Entry);
        }

        public void CopyQueue(World world, PlayerId viewer, IList<WorkDispatchOffer> destination)
            => BuildQueue(world, viewer, destination, WorkCandidateMode.IdleOnly);

        private void ReadJobCandidates(World world, Job job, WorkCandidateMode mode, IList<WorkWorkerCandidate> candidates,
            out Order order, out WorkRequirementStatus status, out Code blocker, out bool emergency)
        {
            candidates.Clear(); order = _board.Order(job.OrderId); emergency = _board.IsEmergency(job);
            status = WorkRequirementStatus.Pending; blocker = BlockerNoWorker;
            if (job.State != JobState.Open) return;
            if (job.IsHaul) throw new InvalidOperationException("a haul owns its actual carrier before dispatch");
            if (job.CreatedTick < 0 || job.CreatedTick > world.Tick) throw new InvalidOperationException("job creation tick is outside the current session age");
            Entity target = world.Entities.Get(job.Target);
            Code binding = order == null ? Code.None : ResponseWork.CheckActiveOrder(world, target, order);
            if (target == null || !binding.IsNone)
            { status = WorkRequirementStatus.Invalid; blocker = target == null ? Codes.NotFound : binding; emergency = false; return; }
            if (job.RetryAt > world.Tick) { blocker = BlockerRetry; return; }
            IActivityNodeDefinition node = order?.Plan.Graph.Find(job.NodeId);
            ActivityNodeExecution execution = order?.Node(job.NodeId);
            CopyCandidates(world, job.Owner, node?.Roles ?? Array.AsReadOnly(new[] { job.Role }), target,
                node, job.Id, execution == null || !WorkBoard.HasSpent(execution) ? null : execution.Choices,
                execution, mode, candidates, null);
            if (candidates.Count != 0) { status = candidates[0].Result.Status; blocker = candidates[0].Result.Blocker; }
        }

        internal void CopyAssignmentCandidates(World world, WorkerDispatchOffer intent, IList<WorkerDispatchOffer> destination)
        {
            destination.Clear();
            if (!intent.RequiresWorker)
            {
                if (intent.Scope != WorkModule.ModuleId || !intent.Entry.StartsWith("node.", StringComparison.Ordinal))
                    throw new InvalidOperationException("unknown timed admission owner or node identity");
                return;
            }
            if (intent.Scope == WorkModule.ModuleId)
            {
                Job job = _board.Job(checked((int)intent.Identity)); if (job == null) return;
                var candidates = new List<WorkWorkerCandidate>();
                ReadJobCandidates(world, job, WorkCandidateMode.EmergencyInterruption, candidates,
                    out Order order, out _, out _, out bool emergency);
                var workers = new HashSet<EntityId>();
                foreach (WorkWorkerCandidate candidate in candidates)
                    if (candidate.Result.Status == WorkRequirementStatus.Satisfied && workers.Add(candidate.Worker))
                        destination.Add(Assignment(new WorkDispatchOffer(job, order, world.Tick, candidate,
                            WorkRequirementStatus.Satisfied, Code.None, emergency), _board, world.Tick));
            }
            else foreach (IWorkerDispatchSource source in DispatchSources(world))
                if (source.DispatchScope == intent.Scope)
                { source.CopyCandidates(intent.Identity, intent.Entry, WorkCandidateMode.EmergencyInterruption, destination); break; }
            var identities = new HashSet<EntityId>(); bool advertised = false, idle = false;
            foreach (WorkerDispatchOffer candidate in destination)
            {
                Entity worker = world.Entities.Get(candidate.Worker);
                if (!candidate.RequiresWorker || !candidate.Ready || candidate.Scope != intent.Scope || candidate.Identity != intent.Identity || candidate.Entry != intent.Entry ||
                    candidate.Company != intent.Company || candidate.Target != intent.Target || candidate.Emergency != intent.Emergency ||
                    candidate.Priority != intent.Priority || candidate.EffectivePriority != candidate.Priority || candidate.Starved || candidate.StarvationAfterTicks.HasValue ||
                    candidate.CreatedTick != intent.CreatedTick || candidate.AgeTicks != intent.AgeTicks || candidate.AttemptsPerPass != intent.AttemptsPerPass ||
                    worker?.Owner != candidate.Company || worker.Get<IWorker>() == null || worker.Get<IWorker>().Held ||
                    worker.Get<IWorker>().Job != candidate.InterruptedJob || !identities.Add(candidate.Worker) ||
                    candidate.InterruptedJob != 0 && PreviewInterruption(world, candidate) != candidate.InterruptedJob)
                    throw new InvalidOperationException("invalid or mismatched worker dispatch candidate");
                if (candidate.Worker == intent.Worker && candidate.TravelCost == intent.TravelCost && candidate.InterruptedJob == intent.InterruptedJob) advertised = true;
                if (candidate.InterruptedJob == 0) idle = true;
            }
            if (intent.Ready && !advertised) throw new InvalidOperationException("ready dispatch offer must include its advertised candidate");
            // Do not interrupt an incumbent merely to improve lower-ranked work's coverage.
            // After an idle acquisition the sole pass rebuilds and may then authorize genuinely needed interruption.
            if (idle) for (int i = destination.Count - 1; i >= 0; i--)
                if (destination[i].InterruptedJob != 0) destination.RemoveAt(i);
        }

        private bool MatchNativeFrontier(World world, IReadOnlyList<WorkerDispatchOffer> offers,
            IReadOnlyList<int> capacities, IReadOnlyList<IReadOnlyList<EntityId>> edges,
            IDictionary<int, (WorkWorkerCandidate Candidate, Code? Dock)> selections,
            out IReadOnlyList<DistinctWorkerMatch> matches, out IReadOnlyList<int> nativeFrontier)
        {
            selections.Clear();
            matches = DistinctWorkerMatching.Solve(capacities, edges);
            nativeFrontier = Array.Empty<int>();
            var matched = new HashSet<int>(); foreach (var match in matches) matched.Add(match.Demand);
            var frontier = new List<int>(); PlayerId company = PlayerId.None;
            for (int i = 0; i < offers.Count; i++)
            {
                WorkerDispatchOffer offer = offers[i];
                if (!offer.Ready || offer.RequiresWorker && !matched.Contains(i)) continue;
                // Only the first runnable native activity frontier may commit in this pass.
                // A source/company/standalone boundary is never crossed for lower work.
                if (offer.Scope != WorkModule.ModuleId || company.IsValid && offer.Company != company) break;
                if (offer.RequiresWorker)
                { Job job = _board.Job(checked((int)offer.Identity)); if (job == null || job.OrderId <= 0) break; }
                if (!company.IsValid) company = offer.Company;
                frontier.Add(i);
            }
            while (frontier.Count > 0)
            {
                var members = new HashSet<int>(frontier);
                // Excluded lower work owns no machine claim. Its hypothetical coverage must
                // not veto a resource-feasible higher team. Actual acquisition rebuilds all
                // remaining offers against real worker/endpoint commitments in Assign.
                var prefixCapacities = new int[capacities.Count];
                foreach (int member in frontier) prefixCapacities[member] = capacities[member];
                var candidate = DistinctWorkerMatching.Solve(prefixCapacities, edges,
                    team => FitsAssignmentSet(world, offers, team, members));
                if (FitsAssignmentSet(world, offers, candidate, members, selections))
                { matches = candidate; nativeFrontier = frontier.AsReadOnly(); return true; }
                // Pure complete-set refusal: no partial reservation or execution occurred.
                // Exclude only the final ranked member, preserving every earlier intent.
                frontier.RemoveAt(frontier.Count - 1);
            }
            selections.Clear(); return false;
        }

        private bool FitsAssignmentSet(World world, IReadOnlyList<WorkerDispatchOffer> offers, IReadOnlyList<DistinctWorkerMatch> matches,
            ISet<int> nativeFrontier,
            IDictionary<int, (WorkWorkerCandidate Candidate, Code? Dock)> selections = null)
        {
            selections?.Clear();
            var companies = new SortedDictionary<int, (List<WorkRequirementResult> Results, List<WorkRequirementContext> Contexts,
                List<int> Rows, List<IReadOnlyList<WorkWorkerCandidate>> Alternatives)>();
            var candidates = new List<WorkWorkerCandidate>();
            foreach (var match in matches)
            {
                if (!nativeFrontier.Contains(match.Demand)) continue;
                WorkerDispatchOffer offer = offers[match.Demand];
                // External sources still participate in distinct coverage and own their actual
                // acquisition protocol. Do not invent native activity obligations for them.
                if (offer.Scope != WorkModule.ModuleId) continue;
                Job job = _board.Job(checked((int)offer.Identity));
                if (job == null) return false;
                ReadJobCandidates(world, job, WorkCandidateMode.EmergencyInterruption, candidates,
                    out Order order, out _, out _, out _);
                IActivityNodeDefinition node = order?.Plan.Graph.Find(job.NodeId);
                if (node == null) continue;
                var options = candidates.FindAll(c => c.Worker == match.Worker && c.Result.Status == WorkRequirementStatus.Satisfied);
                if (options.Count == 0) return false;
                WorkWorkerCandidate candidate = options[0];
                if (!companies.TryGetValue(job.Owner.Value, out var group))
                {
                    group = (new List<WorkRequirementResult>(), new List<WorkRequirementContext>(),
                        new List<int>(), new List<IReadOnlyList<WorkWorkerCandidate>>());
                    companies.Add(job.Owner.Value, group);
                }
                var execution = order.Node(node.Id);
                group.Results.Add(candidate.Result);
                group.Rows.Add(match.Demand); group.Alternatives.Add(options.AsReadOnly());
                group.Contexts.Add(new WorkRequirementContext(world, job.Owner, world.Entities.Get(job.Target), node,
                    world.Entities.Get(match.Worker), execution?.Choices, execution, candidate.Role));
            }
            for (int i = 0; i < offers.Count; i++)
            {
                WorkerDispatchOffer offer = offers[i];
                if (!nativeFrontier.Contains(i) || offer.Scope != WorkModule.ModuleId || offer.RequiresWorker || !offer.Ready) continue;
                if (!_board.TryReadTimedAdmission(checked((int)offer.Identity), TimedNode(offer), out var result, out var context, out _)) return false;
                if (!companies.TryGetValue(offer.Company.Value, out var group))
                {
                    group = (new List<WorkRequirementResult>(), new List<WorkRequirementContext>(),
                        new List<int>(), new List<IReadOnlyList<WorkWorkerCandidate>>());
                    companies.Add(offer.Company.Value, group);
                }
                group.Results.Add(result); group.Contexts.Add(context); group.Rows.Add(i);
                group.Alternatives.Add(Array.Empty<WorkWorkerCandidate>());
            }
            foreach (var group in companies.Values)
            {
                if (!_board.CheckWorkSet(group.Results, group.Contexts, group.Alternatives,
                    out var chosen, out var selected, out _, out _, out var docks, out _))
                { selections?.Clear(); return false; }
                if (selections == null) continue;
                for (int i = 0; i < group.Rows.Count; i++)
                {
                    int demand = group.Rows[i]; if (!chosen[i].HasValue) continue;
                    var candidate = chosen[i].Value; Code? slot = null;
                    foreach (var dock in docks) if (candidate.Worker == dock.Worker)
                    {
                        if (offers[demand].Target != dock.Endpoint || slot.HasValue)
                            throw new InvalidOperationException("joint dock selection differs from actual native matched demand");
                        slot = dock.Slot;
                    }
                    selections.Add(demand, (new WorkWorkerCandidate(candidate.Worker, candidate.Role, candidate.TravelCost,
                        selected[i], candidate.Priority, candidate.InterruptedJob), slot));
                }
            }
            return true;
        }
        private static string TimedNode(WorkerDispatchOffer offer)
        {
            if (offer.RequiresWorker || offer.Scope != WorkModule.ModuleId || !offer.Entry.StartsWith("node.", StringComparison.Ordinal))
                throw new InvalidOperationException("timed admission requires its actual native node identity");
            return offer.Entry.Substring(5);
        }

        private void BuildQueue(World world, PlayerId viewer, IList<WorkDispatchOffer> destination, WorkCandidateMode mode)
        {
            if (!ReferenceEquals(world.Services.Get<IWorkDispatchQueue>(), this))
                throw new InvalidOperationException("dispatch queue belongs to a different session");
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            var offers = new List<WorkDispatchOffer>();
            var candidates = new List<WorkWorkerCandidate>();
            WorkBoard board = world.Services.Get<WorkBoard>();
            foreach (Job job in board.Jobs)
            {
                if (job.State != JobState.Open || viewer.IsValid && viewer != job.Owner) continue;
                ReadJobCandidates(world, job, mode, candidates, out Order order, out WorkRequirementStatus status, out Code blocker, out bool emergency);
                WorkWorkerCandidate? best = candidates.Count == 0 ? (WorkWorkerCandidate?)null : candidates[0];
                offers.Add(new WorkDispatchOffer(job, order, world.Tick, best,
                    status, blocker, emergency));
            }
            offers.Sort((a, b) => CompareAssignments(Assignment(a, board, world.Tick), Assignment(b, board, world.Tick)));
            foreach (WorkDispatchOffer offer in offers) destination.Add(offer);
        }

    }
}
