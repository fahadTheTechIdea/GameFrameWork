using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkBoard : IActivityFacilitySelection
    {
        public ActivityFacilitySelectionView ReadFacilities(IRecurringPlanSource source)
        {
            var schedule = RecurringPlanContracts.ReadSchedule(_world, source);
            Entity worker = source.Entity;
            var definition = worker.Def.Component<IRecurringPlanBindingDefinition>(source.CapabilityId);
            if (definition?.Binding.Target != RecurringPlanTarget.ServiceFacility ||
                !(RecurringPlanContracts.ResolvePlan(_world, definition) is IEntityTargetPlanDefinition plan) || worker.Get<IWorker>() == null ||
                !ReferenceEquals(ResolvePlan(plan.PlanKey), plan))
                throw new ArgumentException("facility query requires actual selected source, target-intent provider and policy");
            var policy = _world.Content.Get<IActivityFacilityPolicy>(schedule.Settings.FacilityPolicy.Id);
            string probe = ActivityFacilityPolicyContracts.Read(_world, policy, null, worker, () => policy.Node);
            var node = plan.Graph.Find(probe);
            if (node == null) throw new InvalidOperationException("facility policy lost its actual probe node");
            EntityId home = WorkerHomeContracts.ReadHome(_world, worker);
            int priority = RecurringPlanContracts.EffectivePriority(_world, source, schedule.Priority);
            ActivityWorkerSelection selection = definition.Binding.SelectWorkers(worker.Id);
            var offers = new List<ActivityFacilityCandidate>(); var workers = new List<WorkWorkerCandidate>();
            EntityId chosen = EntityId.None; PlanIntent selectedIntent = null; long rank = 0, cost = 0;
            foreach (Entity target in _world.Entities.All)
            {
                if (!target.Alive || target.Owner != worker.Owner || target.Get<IWorkDock>() == null || target.Id == worker.Id) continue;
                T Pure<T>(Func<T> observe) => EntityObservation.Read(_world, worker,
                    () => EntityObservation.Read(_world, target, observe));
                var intent = Pure(() => plan.CreateTargetIntent(target.Id, selection, definition.Group, priority));
                if (intent == null || intent.Definition != plan.PlanKey || intent.Priority != priority || intent.Group != definition.Group || intent.Paused)
                    throw new InvalidOperationException("target provider returned different facility intent metadata");
                Code admission = Pure(() => PreviewPlan(worker.Owner, intent));
                WorkRequirementResult readiness = WorkRequirementResult.Pending(admission.IsNone ? WorkSystem.BlockerNoWorker : admission);
                WorkerRoleId role = WorkerRoleId.None; long? travel = null, preference = null;
                if (admission.IsNone)
                {
                    Pure(() => { _world.Services.Get<IWorkerDispatch>().CopyCandidates(_world, worker.Owner, node.Roles,
                        target, node, 0, null, null, WorkCandidateMode.IdleOnly, workers, source); return true; });
                    readiness = WorkRequirementResult.Pending(WorkSystem.BlockerNoWorker);
                    bool observed = false, ready = false, excluded = false;
                    foreach (var candidate in workers)
                    {
                        if (candidate.Worker != worker.Id) continue;
                        if (!observed)
                        { readiness = candidate.Result; travel = candidate.TravelCost; role = candidate.Role; observed = true; }
                        if (candidate.Result.Status != WorkRequirementStatus.Satisfied) continue;
                        if (!candidate.TravelCost.HasValue || candidate.TravelCost.Value < 0 || !candidate.Role.IsValid)
                            throw new InvalidOperationException("ready facility offer has no actual route/role");
                        long? candidateRank = Pure(() => ActivityFacilityPolicyContracts.Read(_world, policy, target, worker, () => policy.Rank(new ActivityFacilityFacts(worker.Id, target.Id, home,
                            candidate.Role, candidate.TravelCost.Value))));
                        if (candidateRank < 0) throw new InvalidOperationException("facility preference must be nonnegative or explicit exclusion");
                        if (!candidateRank.HasValue)
                        {
                            if (!ready && !excluded)
                            { readiness = WorkRequirementResult.Blocked(ActivityFacilityCodes.Excluded); travel = candidate.TravelCost; role = candidate.Role; excluded = true; }
                            continue;
                        }
                        if (!ready || candidateRank.Value < preference.Value || candidateRank == preference &&
                            (candidate.TravelCost.Value < travel.Value || candidate.TravelCost == travel && candidate.Role.CompareTo(role) < 0))
                        { ready = true; readiness = candidate.Result; travel = candidate.TravelCost; role = candidate.Role; preference = candidateRank; }
                    }
                }
                else if (readiness.Blocker.IsNone) throw new InvalidOperationException("refused facility has no admission blocker");
                PlanIntent candidateIntent = null;
                if (readiness.Status == WorkRequirementStatus.Satisfied && preference.HasValue)
                {
                    var bindings = new List<ActivityWorkerBinding>();
                    foreach (var binding in selection.Bindings) bindings.Add(new ActivityWorkerBinding(binding.Node, binding.Worker,
                        binding.Node == node.Id ? role : binding.Role));
                    var exact = new ActivityWorkerSelection(bindings);
                    candidateIntent = Pure(() => plan.CreateTargetIntent(target.Id, exact, definition.Group, priority));
                    if (candidateIntent == null || candidateIntent.Definition != plan.PlanKey || candidateIntent.Priority != priority ||
                        candidateIntent.Group != definition.Group || candidateIntent.Paused)
                        throw new InvalidOperationException("target provider returned different selected-role intent metadata");
                    var captured = ActivityWorkerSelection.Read(candidateIntent.Arguments, plan.Graph, plan.PlanKey + ".facility", out EntityId capturedTarget);
                    if (capturedTarget != target.Id || captured.Identity(capturedTarget) != exact.Identity(target.Id))
                        throw new InvalidOperationException("target provider changed selected facility worker/role intent");
                    Code selectedAdmission = Pure(() => PreviewPlan(worker.Owner, candidateIntent));
                    if (!selectedAdmission.IsNone)
                    { readiness = WorkRequirementResult.Pending(selectedAdmission); preference = null; candidateIntent = null; }
                }
                offers.Add(new ActivityFacilityCandidate(target.Id, role, travel, readiness, preference));
                if (candidateIntent != null && (!chosen.IsValid || preference.Value < rank || preference.Value == rank &&
                    (travel.Value < cost || travel.Value == cost && target.Id.Value < chosen.Value)))
                {
                    selectedIntent = candidateIntent;
                    chosen = target.Id; rank = preference.Value; cost = travel.Value;
                }
            }
            offers.Sort((a, b) => a.Target.Value.CompareTo(b.Target.Value));
            return new ActivityFacilitySelectionView(schedule.Settings.FacilityPolicy, worker.Id, chosen, selectedIntent == null
                ? RecurringPlanRequest.Refused(ActivityFacilityCodes.NoFacility) : RecurringPlanRequest.Ready(selectedIntent), offers);
        }
    }
}
