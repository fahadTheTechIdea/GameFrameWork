using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Read projection on the original owner, never another assignment store or reservation authority.
    public sealed partial class WorkBoard : IWorkAssignments
    {
        public Code ReadAssignment(PlayerId viewer, EntityId workerId, out WorkExecutionFacts? value)
        {
            value = null;
            Entity entity = _world.Entities.Get(workerId);
            if (entity?.Alive != true || viewer != PlayerId.None &&
                (!_world.Players.IsSeat(viewer) || viewer != entity.Owner)) return Codes.NotFound;
            IWorker worker = entity.Get<IWorker>();
            if (worker == null) return StandardCodes.NotWorker;
            int identity = EntityObservation.Read(_world, entity, () => worker.Job);
            if (identity == 0) return Code.None;
            Job job = Job(identity);
            if (identity < 0 || job == null || job.Worker != workerId || job.Owner != entity.Owner ||
                job.State != JobState.Assigned && job.State != JobState.Paused ||
                job.ToilIndex < 0 || job.ToilIndex > job.Toils.Count)
                throw new CorruptStateException("worker assignment is not its actual reciprocal live job");
            value = new WorkExecutionFacts(job, this);
            return Code.None;
        }

        public Code CheckProductiveAssignment(PlayerId viewer, EntityId workerId, EntityId targetId, out WorkExecutionFacts? value)
        {
            Code read = ReadAssignment(viewer, workerId, out value);
            if (!read.IsNone) return read;
            if (!value.HasValue) return WorkSystem.BlockerNoWorker;
            WorkExecutionFacts facts = value.Value;
            value = null;
            if (facts.IsHaul || facts.State != JobState.Assigned || facts.Target != targetId ||
                facts.PhaseTarget != targetId || facts.PhasePurpose != WorkPhasePurpose.Productive ||
                facts.Progress.Equals(WorkProgress.Zero)) return WorkSystem.BlockerWorkerUnavailable;
            Job job = Job(facts.Job); Order order = Order(facts.Order);
            ActivityNodeExecution node = order?.Node(facts.Node);
            if (order?.IsActive != true || node?.JobId != job.Id || node.State != NodeState.Working)
                return WorkSystem.BlockerWorkerUnavailable;
            Entity worker = _world.Entities.Get(workerId), target = _world.Entities.Get(targetId);
            if (target?.Alive != true || target.Owner != facts.Company) return Codes.NotFound;
            IWorkerDispatch dispatch = _world.Services.Get<IWorkerDispatch>();
            Code blocker = dispatch.CheckJob(_world, worker, job);
            if (!blocker.IsNone) return blocker;
            if (!dispatch.EffectiveWorkRate(_world, worker).IsPositive) return WorkSystem.BlockerWorkerUnavailable;
            if (ReadDockReach(job, worker, target, out Code dockBlocker) != SpatialInteractionStatus.WithinReach)
                return dockBlocker.IsNone ? WorkSystem.BlockerUnreachable : dockBlocker;
            WorkRequirementResult requirements = EvaluateRequirements(order.Owner, target, order.Plan.Graph.Find(facts.Node), node, null);
            if (requirements.Status != WorkRequirementStatus.Satisfied) return requirements.Blocker;
            value = facts; return Code.None;
        }
    }
}
