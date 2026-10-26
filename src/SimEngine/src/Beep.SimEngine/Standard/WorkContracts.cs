using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum WorkCandidateMode : byte { IdleOnly = 0, EmergencyInterruption = 1 }
    public enum HaulRecoveryStage : byte { None = 0, Departing = 1, WaitingForDestination = 2 }
    public static class HaulRecoveryCodes
    {
        public static readonly Code DestinationUnavailable = new Code("haul.recovery-destination-unavailable");
        public static readonly Code DestinationBindingLost = new Code("haul.original-destination-binding-lost");
    }

    /// <summary>Actual reciprocal assignments from the existing work owner. Facts never authorize acquisition.</summary>
    public interface IWorkAssignments
    {
        /// <summary>None/null means an owned idle worker; private or absent entities expose no assignment.</summary>
        Code ReadAssignment(PlayerId viewer, EntityId worker, out WorkExecutionFacts? value);
        /// <summary>Fresh actual productive hold at this target; never idle eligibility or acquisition authority.</summary>
        Code CheckProductiveAssignment(PlayerId viewer, EntityId worker, EntityId target, out WorkExecutionFacts? value);
    }

    /// <summary>Detached incumbent facts; never an executable job, node or mutable world handle.</summary>
    public readonly struct WorkExecutionFacts
    {
        public int Job { get; }
        public PlayerId Company { get; }
        public EntityId Worker { get; }
        public EntityId Target { get; }
        public WorkerRoleId Role { get; }
        public JobState State { get; }
        public bool IsHaul { get; }
        public bool Emergency { get; }
        public long CreatedTick { get; }
        public WorkProgress Progress { get; }
        public int Order { get; }
        public ActivityExecutionBinding ExecutionBinding { get; }
        public string Node { get; }
        public string Plan { get; }
        public int ToilIndex { get; }
        public Code Phase { get; }
        public WorkPhasePurpose? PhasePurpose { get; }
        public EntityId PhaseTarget { get; }
        public Code Blocker { get; }
        public Code SourceDockSlot { get; }
        public Code DestinationDockSlot { get; }
        internal WorkExecutionFacts(Job job, WorkBoard board)
        {
            Job = job.Id; Company = job.Owner; Worker = job.Worker; Target = job.Target;
            Role = job.Role; State = job.State; IsHaul = job.IsHaul; Emergency = board.IsEmergency(job);
            CreatedTick = job.CreatedTick;
            Progress = board.Order(job.OrderId)?.Node(job.NodeId)?.Progress ?? job.HandlingProgress;
            Order = job.OrderId; ExecutionBinding = board.Order(job.OrderId)?.ExecutionBinding ?? default; Node = job.NodeId; Plan = board.Order(job.OrderId)?.Plan.PlanKey;
            ToilIndex = job.ToilIndex; Blocker = job.Blocker;
            SourceDockSlot = job.SourceDockSlot; DestinationDockSlot = job.DestinationDockSlot;
            bool hasPhase = job.ToilIndex >= 0 && job.ToilIndex < job.Toils.Count;
            Phase = hasPhase ? job.Toils[job.ToilIndex].Executor : Code.None;
            PhasePurpose = hasPhase ? board.World.Services.Get<IWorkPhaseCatalog>().Purpose(Phase) : (WorkPhasePurpose?)null;
            PhaseTarget = hasPhase ? job.Toils[job.ToilIndex].At : EntityId.None;
        }
    }
    /// <summary>Pure current queue projection, not a second job store or retained assignment authority.</summary>
    public interface IWorkDispatchQueue
    {
        void CopyQueue(World world, PlayerId viewer, IList<WorkDispatchOffer> destination);
        void CopyAssignments(World world, PlayerId viewer, IList<WorkerDispatchOffer> destination);
    }
    /// <summary>Detached ranking facts from an existing intent owner. Never a machine reservation.</summary>
    public readonly struct WorkerDispatchOffer
    {
        public string Scope { get; }
        public long Identity { get; }
        public string Entry { get; }
        public PlayerId Company { get; }
        public EntityId Target { get; }
        public EntityId Worker { get; }
        public bool RequiresWorker { get; }
        public bool Ready { get; }
        public Code Blocker { get; }
        public bool Emergency { get; }
        public int Priority { get; }
        public long EffectivePriority { get; }
        public long? StarvationAfterTicks { get; }
        public bool Starved { get; }
        /// <summary>Engine-stamped selected policy, never source-authored rank.</summary>
        public WorkerDispatchOrdering Ordering { get; }
        public int InterruptedJob { get; }
        public long CreatedTick { get; }
        public long AgeTicks { get; }
        public long? TravelCost { get; }
        public int AttemptsPerPass { get; }
        public WorkerDispatchOffer(string scope, long identity, string entry, PlayerId company,
            EntityId target, EntityId worker, bool ready, Code blocker, bool emergency, int priority,
            long createdTick, long tick, long? travelCost, int attemptsPerPass, int interruptedJob, bool requiresWorker)
        {
            ContractIdentity.ValidateId(scope, "worker dispatch source"); ContractIdentity.ValidateId(entry, "worker dispatch entry");
            if (identity <= 0 || !company.IsValid || !target.IsValid || priority < 0 || createdTick < 0 || createdTick > tick ||
                travelCost < 0 || attemptsPerPass < 1 || ready && (requiresWorker && !worker.IsValid || !blocker.IsNone || !travelCost.HasValue) ||
                !requiresWorker && (worker.IsValid || interruptedJob != 0 || travelCost != 0) ||
                !ready && blocker.IsNone || interruptedJob < 0 || interruptedJob != 0 && (!ready || !emergency))
                throw new System.ArgumentException("malformed worker dispatch facts");
            Scope = scope; Identity = identity; Entry = entry; Company = company; Target = target; Worker = worker;
            RequiresWorker = requiresWorker;
            Ready = ready; Blocker = blocker; Emergency = emergency; Priority = priority; CreatedTick = createdTick;
            EffectivePriority = priority;
            StarvationAfterTicks = null; Starved = false;
            Ordering = WorkerDispatchOrdering.PriorityFirst;
            InterruptedJob = interruptedJob;
            AgeTicks = checked(tick - createdTick); TravelCost = travelCost; AttemptsPerPass = attemptsPerPass;
        }
        internal WorkerDispatchOffer WithEffectivePriority(long priority) => new WorkerDispatchOffer(this, priority, StarvationAfterTicks, InterruptedJob);
        internal WorkerDispatchOffer WithStarvation(long? after) => new WorkerDispatchOffer(this, EffectivePriority, after, InterruptedJob);
        internal WorkerDispatchOffer WithInterruption(int job) => new WorkerDispatchOffer(this, EffectivePriority, StarvationAfterTicks, job);
        internal WorkerDispatchOffer WithOrdering(WorkerDispatchOrdering ordering)
            => new WorkerDispatchOffer(this, EffectivePriority, StarvationAfterTicks, InterruptedJob, ordering);
        internal WorkerDispatchOffer WithCandidate(EntityId worker, long cost)
        {
            if (!RequiresWorker) throw new InvalidOperationException("timed admission cannot acquire a machine candidate");
            return new WorkerDispatchOffer(Scope, Identity, Entry, Company, Target, worker, true, Code.None, Emergency,
                Priority, CreatedTick, checked(CreatedTick + AgeTicks), cost, AttemptsPerPass, 0, true)
                .WithEffectivePriority(EffectivePriority).WithStarvation(StarvationAfterTicks).WithOrdering(Ordering);
        }
        private WorkerDispatchOffer(WorkerDispatchOffer offer, long priority, long? starvationAfter, int interruptedJob,
            WorkerDispatchOrdering? ordering = null)
        {
            Scope = offer.Scope; Identity = offer.Identity; Entry = offer.Entry; Company = offer.Company; Target = offer.Target;
            Worker = offer.Worker; Ready = offer.Ready; Blocker = offer.Blocker; Emergency = offer.Emergency; Priority = offer.Priority;
            RequiresWorker = offer.RequiresWorker;
            EffectivePriority = priority; CreatedTick = offer.CreatedTick; AgeTicks = offer.AgeTicks; TravelCost = offer.TravelCost; AttemptsPerPass = offer.AttemptsPerPass;
            StarvationAfterTicks = starvationAfter; Starved = starvationAfter.HasValue && AgeTicks >= starvationAfter.Value;
            Ordering = ordering ?? offer.Ordering;
            InterruptedJob = interruptedJob;
        }
    }
    /// <summary>A declared engine-side service contributes its existing intent to WorkSystem's sole pass.</summary>
    public interface IWorkerDispatchSource
    {
        World World { get; }
        string DispatchScope { get; }
        Schedule GetDispatchSchedule(ISimulationTime time);
        void PrepareDispatch();
        void CopyOffers(PlayerId viewer, WorkCandidateMode mode, IList<WorkerDispatchOffer> destination);
        /// <summary>Pure ordered ready alternatives for one existing intent; no claims or replacement identities.</summary>
        void CopyCandidates(long identity, string entry, WorkCandidateMode mode, IList<WorkerDispatchOffer> destination);
        /// <summary>Re-resolve current intent and preflight its actual acquisition; retained offers never authorize mutation.</summary>
        bool TryAcquire(long identity, string entry, EntityId worker, WorkCandidateMode mode);
    }
    public readonly struct WorkDispatchOffer
    {
        public int Job { get; }
        public int Order { get; }
        public string Node { get; }
        public EntityId Target { get; }
        public EntityId Worker { get; }
        public WorkerRoleId Role { get; }
        public WorkRequirementStatus Status { get; }
        public Code Blocker { get; }
        public bool Emergency { get; }
        public int Priority { get; }
        public long CreatedTick { get; }
        public long AgeTicks { get; }
        public long RetryAt { get; }
        public long? TravelCost { get; }
        public int InterruptedJob { get; }
        internal WorkDispatchOffer(Job job, Order order, long tick, WorkWorkerCandidate? candidate,
            WorkRequirementStatus status, Code blocker, bool emergency)
        {
            Job = job.Id; Order = job.OrderId; Node = job.NodeId; Target = job.Target;
            Worker = candidate?.Worker ?? EntityId.None; Role = candidate?.Role ?? job.Role;
            Status = status; Blocker = blocker; Emergency = emergency;
            Priority = order?.Priority ?? job.Priority; CreatedTick = job.CreatedTick;
            AgeTicks = checked(tick - job.CreatedTick); RetryAt = job.RetryAt; TravelCost = candidate?.TravelCost;
            InterruptedJob = candidate?.InterruptedJob ?? 0;
        }
    }
    /// <summary>Detached offer, never a retained authorization or a machine claim.</summary>
    public readonly struct WorkWorkerCandidate
    {
        public EntityId Worker { get; }
        public WorkerRoleId Role { get; }
        public long? TravelCost { get; }
        public WorkRequirementResult Result { get; }
        public int Priority { get; }
        public int InterruptedJob { get; }
        public WorkWorkerCandidate(EntityId worker, WorkerRoleId role, long? travelCost, WorkRequirementResult result, int priority, int interruptedJob)
        { Worker = worker; Role = role; TravelCost = travelCost; Result = result; Priority = priority; InterruptedJob = interruptedJob; }
    }
    public interface IWorkSupplyRequests
    {
        void RequestInputs(int order, Entity target, IReadOnlyList<LogisticsSupplyRequest> inputs, int priority);
        void WithdrawInputs(int order);
    }
    /// <summary>The existing work owner evaluates eligibility without acquiring or mirroring worker state.</summary>
    /// <summary>Player control over the existing worker/job owner, independent of navigation or economics.</summary>
    public interface IWorkerControl
    {
        Code CheckHeld(World world, PlayerId company, EntityId worker, bool held);
        void SetHeld(World world, PlayerId company, EntityId worker, bool held);
    }
    public interface IWorkerDispatch
    {
        /// <summary>Settle current policy against existing jobs/home; never another claim or scheduler.</summary>
        void RefreshPolicy(World world, Entity worker);
        /// <summary>Current job eligibility, using the same remaining-stop and policy rules as execution.</summary>
        Code CheckJob(World world, Entity worker, Job job);
        /// <summary>Current idle role eligibility only; no target, route, material or assignment promise.</summary>
        Code CheckIdleWorker(World world, Entity worker, PlayerId owner, WorkerRoleId role, bool emergency);
        Code CheckAssignment(World world, Entity worker, PlayerId owner, WorkerRoleId role,
            IReadOnlyList<Entity> destinations, bool emergency, int ownedJob = 0);
        Code CheckInterruption(World world, Entity worker);
        /// <summary>Pure willingness and engine safety over current detached source facts, never a reservation.</summary>
        int PreviewInterruption(World world, WorkerDispatchOffer challenger);
        /// <summary>Recheck then interrupt the actual incumbent. Caller must freshly acquire or fault.</summary>
        void InterruptForDispatch(World world, WorkerDispatchOffer challenger);
        /// <summary>Pure effective work ticks per update at this checkpoint, shared by offers and execution.</summary>
        Fix EffectiveWorkRate(World world, Entity worker);
        /// <summary>All supported worker/role pairs in deterministic readiness order. Roles are
        /// alternatives, not distinct actors; a live assigned job exposes only its actual role.</summary>
        void CopyCandidates(World world, PlayerId owner, IReadOnlyList<WorkerRoleId> roles, Entity target,
            IActivityNodeDefinition node, int ownedJob, IReadOnlyList<WorkRequirementChoice> choices, ActivityNodeExecution execution,
            WorkCandidateMode mode, IList<WorkWorkerCandidate> destination, IRecurringPlanSource recurringSource);
        /// <summary>Pure prospective/actual response candidates from the actual target's named authored provider; never posts or acquires.</summary>
        void CopyResponseCandidates(World world, EntityId target, Code source, string nodeId, IList<WorkWorkerCandidate> destination);
    }
    /// <summary>Manual player authority over the same physical hauling owner; no independent state or selection.</summary>
    public interface IHaulPlayerControls
    {
        HaulPreview PreviewPlayerHaul(PlayerId owner, EntityId source, EntityId destination, ResourceId resource, Fix ceiling, bool purchase = false, EntityId worker = default, Code sourcePort = default, Code destinationPort = default, int priority = 0, Code selection = default);
        int AcquirePlayerHaul(PlayerId owner, EntityId source, EntityId destination, ResourceId resource, Fix ceiling, int priority, bool purchase = false, EntityId worker = default, Code sourcePort = default, Code destinationPort = default, Code selection = default);
    }
    /// <summary>The actual hauling owner; WorkSystem retains the sole job/toil execution ledger.</summary>
    public interface IHaulExecution : ISimulationOperationBoundary
    {
        HaulPreview PreviewHaul(PlayerId owner, EntityId source, EntityId destination, ResourceId resource, Fix ceiling, bool purchase = false, EntityId worker = default, Code sourcePort = default, Code destinationPort = default, Code selection = default);
        int AcquireHaul(PlayerId owner, EntityId source, EntityId destination, ResourceId resource, Fix ceiling, int priority, bool purchase = false, EntityId worker = default, Code sourcePort = default, Code destinationPort = default, Code selection = default);
        ResourceTransferResult Load(World world, Job job, Entity worker, Entity source, ResourceId resource, Fix amount);
        ResourceTransferResult Unload(World world, Job job, Entity worker, Entity destination, ResourceId resource);
        void EndHaul(World world, Job job);
        ResourcePortResolution ReadHaulEndpoint(Job job, bool pickup);
        /// <summary>Pure exact original delivery binding: distinguishes missing geometry from lost named access; never substitutes current selection.</summary>
        Code CheckDestinationBinding(Job job);
        /// <summary>Handle an authored recovery by beginning physical dock egress, replanning remaining delivery, or retaining a missing-destination wait. Preserves job/cargo and committed material; false means no recovery was handled.</summary>
        bool TryReplanDestination(World world, Job job, Code blocker);
        HaulPreview PreviewReturnCargo(EntityId worker, ResourceId resource);
        CargoDispositionSnapshot ReadCargoDisposition(EntityId worker);
        /// <summary>Pure current-job movement funding; null when no authored active haul applies.</summary>
        HaulTripEvaluation ReadHaulTripBudget(EntityId worker);
    }
    /// <summary>Component content or a declared module service supplies immutable plans to the same board catalog/validator.</summary>
    public interface IActivitySource
    {
        IReadOnlyList<IActivityDefinition> ActivityDefinitions { get; }
    }
    /// <summary>The target capability owns cleanup, not a concrete project cast in WorkSystem.</summary>
    public interface IWorkCancellationTarget : IEntityCapability
    {
        void OnWorkCancelled(World world, Order order);
    }
    /// <summary>Keep the existing ended order while its live target has unresolved physical disposition.</summary>
    public interface IWorkOrderRetention : IEntityCapability
    {
        bool RetainsOrder(Order order);
    }
}
