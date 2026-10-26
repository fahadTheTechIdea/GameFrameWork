using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    /// <summary>Detached declaration facts from the selected optional policy; no provider or ranking cache.</summary>
    public sealed class WorkerDispatchPolicyView
    {
        public string Id { get; }
        public WorkerDispatchOrdering Ordering { get; }
        public IReadOnlyList<string> Scopes { get; }
        internal WorkerDispatchPolicyView(IWorkerDispatchPolicy policy)
        {
            Id = policy.Id; Ordering = policy.Ordering;
            var scopes = new string[policy.DispatchScopes.Count];
            for (int i = 0; i < scopes.Length; i++) scopes[i] = policy.DispatchScopes[i];
            Scopes = System.Array.AsReadOnly(scopes);
        }
    }
    public readonly struct WorkerPolicyView
    {
        public EntityId Worker { get; }
        public int EntityVersion { get; }
        public WorkPolicySettings Settings { get; }
        internal WorkerPolicyView(Entity entity, WorkPolicySettings settings)
        { Worker = entity.Id; EntityVersion = entity.Version; Settings = settings; }
    }
    public readonly struct WorkJobView
    {
        public int Id { get; }
        public EntityId Worker { get; }
        public EntityId Target { get; }
        public JobState State { get; }
        public Code Blocker { get; }
        public bool IsHaul { get; }
        public EntityId Source { get; }
        public EntityId Destination { get; }
        public Code SourcePort { get; }
        public Code DestinationPort { get; }
        public ResourceEndpointId SourceEndpoint { get; }
        public ResourceEndpointId DestinationEndpoint { get; }
        public bool Purchase { get; }
        public bool TripHeld { get; }
        public int ToilIndex { get; }
        public Fix Loaded { get; }
        public ResourceId Resource { get; }
        public Fix Amount { get; }
        public Fix HandledAmount { get; }
        public Fix HandlingRemaining { get; }
        public WorkProgress HandlingProgress { get; }
        public Fix UnloadingBaseline { get; }
        public Code RecoveryCause { get; }
        public Code RecoveryDockSlot { get; }
        public HaulRecoveryStage RecoveryStage { get; }
        public long? HandlingRequiredTicks { get; }
        public Code SourceDockSlot { get; }
        public Code DestinationDockSlot { get; }
        public bool DockWaiting { get; }
        public int Route { get; }
        public LogisticsDemandId HaulDemand { get; }
        public Code HaulSelection { get; }
        public LogisticsRecoveryKind? RecoveryKind { get; }
        public bool Emergency { get; }
        public int FailureCount { get; }
        public long RetryAt { get; }
        public long RetryRemainingTicks { get; }
        internal WorkJobView(Job job, WorkBoard board)
        { Id = job.Id; Worker = job.Worker; Target = job.Target; State = job.State; Blocker = job.Blocker; IsHaul = job.IsHaul; TripHeld = job.TripHeld;
            Source = job.From; Destination = job.To; Purchase = job.Purchase;
            SourcePort = job.SourcePort; DestinationPort = job.DestinationPort;
            SourceEndpoint = job.SourceEndpoint; DestinationEndpoint = job.DestinationEndpoint;
            ToilIndex = job.ToilIndex; Loaded = job.Loaded; Resource = job.Resource; Amount = job.Amount;
            HandledAmount = job.IsHaul ? HaulHandling.Handled(job) : Fix.Zero;
            HandlingRemaining = job.IsHaul ? job.Amount - HandledAmount : Fix.Zero;
            Route = job.Route; HaulDemand = job.HaulDemand; HaulSelection = job.HaulSelection; Emergency = board.IsEmergency(job);
            RecoveryKind = job.RecoveryKind;
            FailureCount = job.Failures; RetryAt = job.RetryAt;
            RetryRemainingTicks = job.RetryAt > board.World.Tick ? job.RetryAt - board.World.Tick : 0;
            HandlingProgress = job.HandlingProgress; ResourceHandlingCheck handling = board.ReadHandling(job);
            UnloadingBaseline = job.UnloadingBaseline;
            RecoveryCause = job.RecoveryCause; RecoveryDockSlot = job.RecoveryDockSlot; RecoveryStage = board.ReadRecoveryStage(job);
            SourceDockSlot = job.SourceDockSlot; DestinationDockSlot = job.DestinationDockSlot; DockWaiting = board.IsDockWaiting(job);
            HandlingRequiredTicks = handling.Ready ? handling.RequiredTicks : (long?)null; }
    }
    /// <summary>Detached authored readiness facts; no executable provider or mutable world reference.</summary>
    public readonly struct WorkRequirementView
    {
        public string Node { get; }
        public string Requirement { get; }
        public WorkRequirementResult Result { get; }
        public IReadOnlyList<string> Path { get; }
        internal WorkRequirementView(string node, string requirement, IReadOnlyList<string> path, WorkRequirementResult result)
        { Node = node; Requirement = requirement; Path = path; Result = result; }
    }

    /// <summary>Immutable host-facing node facts, never a provider or runtime record.</summary>
    public readonly struct ActivityNodeView
    {
        public string Id { get; }
        public IReadOnlyList<string> Dependencies { get; }
        public NodeState State { get; }
        public int JobId { get; }
        public EntityId Worker { get; }
        public WorkerRoleId SelectedRole { get; }
        public EntityId CompletedBy { get; }
        public EntityId RequestedWorker { get; }
        public WorkerRoleId RequestedRole { get; }
        public WorkProgress Progress { get; }
        public long ProductiveSteps { get; }
        public GameDuration ProductiveGameTime { get; }
        public long RequiredTicks { get; }
        public Code Blocker { get; }
        public IReadOnlyList<WorkRequirementChoice> Choices { get; }
        public int CompletionFailures { get; }
        public long CompletionRetryAt { get; }
        public long CompletionRetryRemainingTicks { get; }
        public IReadOnlyList<WorkResourceBinding> Resources { get; }
        public IReadOnlyList<WorkCostBinding> Costs { get; }
        public IReadOnlyList<WorkResourceUsage> RunningInputs { get; }
        public string Executor { get; }
        internal ActivityNodeView(IActivityNodeDefinition definition, ActivityNodeExecution execution, ISimulationTime time, Job job, WorkBoard board)
        {
            Executor = definition.Executor.Id;
            Id = execution.Id; State = execution.State; JobId = execution.JobId;
            Worker = job?.Worker ?? EntityId.None; SelectedRole = Worker.IsValid ? job.Role : WorkerRoleId.None;
            CompletedBy = execution.CompletedBy;
            RequestedWorker = board.Selection(execution).WorkerFor(definition.Id);
            RequestedRole = board.Selection(execution).RoleFor(definition.Id);
            Progress = execution.Progress; RequiredTicks = time.Ticks(definition.Duration); Blocker = execution.Blocker;
            ProductiveSteps = execution.ProductiveSteps;
            ProductiveGameTime = GameDuration.FromSteps(execution.ProductiveSteps, time);
            CompletionFailures = execution.CompletionFailures; CompletionRetryAt = execution.CompletionRetryAt;
            CompletionRetryRemainingTicks = execution.CompletionRetryAt > board.World.Tick ? execution.CompletionRetryAt - board.World.Tick : 0;
            Choices = execution.Choices; Resources = execution.Resources; Costs = execution.Costs;
            RunningInputs = board.RunningUsage(execution, definition);
            var dependencies = new string[definition.Dependencies.Count];
            for (int i = 0; i < dependencies.Length; i++) dependencies[i] = definition.Dependencies[i];
            Dependencies = System.Array.AsReadOnly(dependencies);
        }
    }

    /// <summary>Detached actual-order work diagnostics. Nominal work is not an ETA or a settlement guarantee.</summary>
    public sealed class ActivityProgressView
    {
        public int OrderId { get; }
        public string Plan { get; }
        public OrderState State { get; }
        public Code Blocker { get; }
        public long ReadAt { get; }
        public int CompletedNodes { get; }
        public int UnfinishedNodes { get; }
        /// <summary>Earned work whose departure/effect/state transition has not yet completed.</summary>
        public int EarnedUnsettledNodes { get; }
        public bool CompletionEffectApplied { get; }
        public ActivityWorkAnalysis Work { get; }
        internal ActivityProgressView(Order order, ISimulationTime time, long readAt)
        {
            OrderId = order.Id; Plan = order.PlanKey; State = order.State; Blocker = order.Blocker;
            ReadAt = readAt; CompletionEffectApplied = order.CompletionEffectApplied;
            var progress = new Dictionary<string, WorkProgress>(System.StringComparer.Ordinal);
            foreach (ActivityNodeExecution node in order.Nodes)
            {
                progress.Add(node.Id, node.Progress);
                if (node.State == NodeState.Done) CompletedNodes++;
                else
                {
                    UnfinishedNodes++;
                    if (node.Progress.Complete(time.Ticks(order.Plan.Graph.Find(node.Id).Duration))) EarnedUnsettledNodes++;
                }
            }
            Work = order.Plan.Graph.AnalyzeWork(time, progress);
        }
    }

    /// <summary>An order as its owner sees it.</summary>
    public struct OrderView
    {
        public int Id;
        public ActivityExecutionBinding ExecutionBinding;
        public string Plan;
        public EntityId Target;
        public OrderState State;
        public int Priority;
        public long CreatedTick;
        public int NodesDone;
        public int NodeCount;
        /// <summary>The declared role the current node needs (None for none or a timed node).</summary>
        public WorkerRoleId CurrentRole;
        public WorkProgress CurrentProgress;
        public long CurrentNeeded;
        public Code Blocker;
        public int CompletionFailures;
        public long CompletionRetryAt;
        public long CompletionRetryRemainingTicks;
        public bool CompletionEffectApplied;
    }

    /// <summary>The work a company has going: orders, readiness and worker availability.</summary>
    public sealed class WorkView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;

        internal WorkView(World world, PlayerId viewer)
        {
            _world = world;
            _viewer = viewer;
        }

        private bool Sees(PlayerId owner) => _viewer == PlayerId.None || _world.Players.IsSeat(_viewer) && owner == _viewer;
        private bool SeesTarget(EntityId target) => !target.IsValid || _world.ViewFor(_viewer).CanSee(_world.Entities.Get(target));
        private bool Discloses(Order order) => order != null && Sees(order.Owner) && SeesTarget(order.Target)
            && _world.Services.Get<IActivityCatalog>().CanDiscloseActivity(_viewer, order.PlanKey);
        private bool Discloses(Job job)
        {
            if (job == null || !Sees(job.Owner) || !SeesTarget(job.Target) || !SeesTarget(job.Worker)
                || !SeesTarget(job.From) || !SeesTarget(job.To)) return false;
            if (job.OrderId != 0 && !Discloses(_world.Services.Get<WorkBoard>().Order(job.OrderId))) return false;
            return !job.IsHaul || _world.ViewFor(_viewer).CanSeeContent(_world.Content.Resource(job.Resource)) && ResourceLotSelection.Visible(_world, _viewer, job.HaulSelection);
        }
        public Code ReadWorkerAssignment(EntityId worker, out WorkExecutionFacts? value)
        {
            value = null; _world.RequireHealthyUpdate();
            if (!_world.ViewFor(_viewer).CanObserveOwned(_world.Entities.Get(worker))) return Codes.NotFound;
            Code check = _world.Services.Get<IWorkAssignments>().ReadAssignment(_viewer, worker, out var facts);
            if (!check.IsNone) return check;
            if (!SeesTarget(worker) || facts.HasValue && !Discloses(_world.Services.Get<WorkBoard>().Job(facts.Value.Job))) return Codes.NotFound;
            _world.RequireHealthyUpdate(); value = facts; return Code.None;
        }
        public Code ReadWorkerCapacity(PlayerId company, WorkerRoleId role, EntityId subject, out WorkerCapacitySnapshot value)
        {
            value = default; _world.RequireHealthyUpdate();
            return (_viewer == PlayerId.None || _world.Players.IsSeat(_viewer) && _viewer == company)
                ? _world.Services.Get<IWorkLoad>().ReadWorkerCapacity(company, role, subject, out value) : Codes.NotFound;
        }
        public Code ReadOperatingInputs(EntityId id, OperatingPhase phase, out OperatingInputEvaluation value, long intervals = 1)
        {
            value = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(id);
            if (!_world.ViewFor(_viewer).CanObserveOwned(entity)) return Codes.NotFound;
            if (!entity.Has<IWorker>()) return StandardCodes.NotWorker;
            if (intervals < 0 || phase != OperatingPhase.Idle && phase != OperatingPhase.Standby) return Codes.InvalidArgument;
            var facts = OperatingInputsContracts.Read(_world, entity, phase, intervals);
            if (!OperatingInputsContracts.CanDisclose(_world, _viewer, facts)) return Codes.NotFound;
            value = facts; return Code.None;
        }
        public Code ReadWorkLoad(PlayerId company, WorkLoadScope scope, EntityId subject, out WorkLoadSnapshot value)
        {
            value = default; _world.RequireHealthyUpdate();
            return Sees(company) ? _world.Services.Get<IWorkLoad>().ReadWorkLoad(company, scope, subject, out value) : Codes.NotFound;
        }
        public Code ReadHome(EntityId id, out WorkerHomeView value)
        {
            _world.RequireHealthyUpdate(); return WorkerHomeContracts.ReadCurrent(_world, _viewer, _world.Entities.Get(id), out value);
        }
        public Code PreviewHome(EntityId worker, EntityId home)
        { _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(worker); return WorkerHomeContracts.CheckHome(_world, _viewer == PlayerId.None ? entity?.Owner ?? _viewer : _viewer, worker, home); }
        public WorkerHomeSelectionPreview PreviewHomeGroup(IReadOnlyList<EntityId> workers, EntityId home)
            => WorkerHomeContracts.PreviewGroup(_world, _viewer, workers, home);
        public Code PreviewWorkerHeld(EntityId worker, bool held)
        { _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(worker); return _world.Services.Get<IWorkerControl>().CheckHeld(_world, _viewer == PlayerId.None ? entity?.Owner ?? _viewer : _viewer, worker, held); }
        public void CopyPlans(IList<PlanView> destination)
            => _world.Services.Get<IPlanBoard>().CopyPlans(_viewer, destination);
        public Code ReadWorkerExecution(EntityId id, out WorkerExecutionView value)
        {
            value = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(id);
            if (!_world.ViewFor(_viewer).CanObserveOwned(entity)) return Codes.NotFound;
            if (entity.Get<IWorker>() == null) return Codes.InvalidArgument;
            Code assignment = ReadWorkerAssignment(id, out _); if (!assignment.IsNone) return assignment;
            value = WorkerExecutionContracts.Read(_world, entity); return Code.None;
        }
        public Code ReadRecurringPlan(EntityId id, Code source, out RecurringPlanView value)
        {
            value = default; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(id);
            if (entity == null || !Sees(entity.Owner)) return Codes.NotFound;
            if (source.IsNone) return Codes.InvalidArgument;
            var provider = entity.Get<IRecurringPlanSource>(source); if (provider == null) return RecurringPlanCodes.NotSource;
            if (!RecurringPlanContracts.CanDisclose(_world, _viewer, provider)) return Codes.NotFound;
            value = RecurringPlanContracts.Read(_world, provider); return Code.None;
        }
        public Code ReadRecurringFacilities(EntityId id, Code source, out ActivityFacilitySelectionView value)
        {
            value = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(id);
            if (entity == null || !Sees(entity.Owner)) return Codes.NotFound;
            if (source.IsNone) return Codes.InvalidArgument;
            var provider = entity.Get<IRecurringPlanSource>(source); if (provider == null) return RecurringPlanCodes.NotSource;
            if (!RecurringPlanContracts.CanDisclose(_world, _viewer, provider)) return Codes.NotFound;
            var definition = entity.Def.Component<IRecurringPlanBindingDefinition>(source);
            if (definition?.Binding.Target != RecurringPlanTarget.ServiceFacility) return ActivityFacilityCodes.NotSelected;
            value = _world.Services.Get<IActivityFacilitySelection>().ReadFacilities(provider); return Code.None;
        }
        public Code PreviewRecurringPlan(EntityId id, Code source, RecurringPlanSettings settings)
        {
            _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(id); if (entity == null) return Codes.NotFound;
            return RecurringPlanContracts.CheckPlayerConfigure(_world, _viewer == PlayerId.None ? entity.Owner : _viewer, id, source, settings);
        }
        public Code ReadRecurringTemplate(EntityId id, Code source, out RecurringTemplateSettings settings)
        {
            settings = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(id);
            if (entity == null || !Sees(entity.Owner)) return Codes.NotFound;
            if (source.IsNone) return Codes.InvalidArgument;
            var provider = entity.Get<IRecurringPlanSource>(source); if (provider == null) return RecurringPlanCodes.NotSource;
            if (!RecurringPlanContracts.CanDisclose(_world, _viewer, provider)) return Codes.NotFound;
            settings = RecurringPlanContracts.ReadTemplateSettings(_world, provider);
            return settings == null ? RecurringPlanCodes.NotTemplate : Code.None;
        }
        public PlanGroupCheck PreviewRecurringTemplate(EntityId id, Code source, RecurringTemplateSettings settings)
        {
            _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(id); if (entity == null) return new PlanGroupCheck(Codes.NotFound);
            return RecurringPlanContracts.CheckPlayerConfigureTemplate(_world, _viewer == PlayerId.None ? entity.Owner : _viewer, id, source, settings);
        }
        public PlanTemplate AuthoredPlanTemplate(string id)
        {
            _world.RequireHealthyUpdate(); IPlanTemplateContent definition = _world.Content.Find<IPlanTemplateContent>(id);
            if (definition == null) return null;
            return EntityObservation.ReadProvider(_world, definition, null, () =>
                _world.ViewFor(_viewer).CanSeeContent(definition) && _world.Services.Get<IPlanBoard>().CanDisclosePlanTemplate(_viewer, definition.Template)
                    ? definition.Template : null);
        }
        public Code ReadStoredPlanTemplate(long id, out StoredPlanTemplateView value)
            => _world.Services.Get<IPlanBoard>().ReadStoredPlanTemplate(_viewer, id, out value);
        public void CopyStoredPlanTemplates(IList<StoredPlanTemplateView> destination)
            => _world.Services.Get<IPlanBoard>().CopyStoredPlanTemplates(_viewer, destination);
        public PlanGroupCheck PreviewStorePlanTemplate(long id, long revision, string name, PlanTemplate template)
            => _world.Services.Get<IPlanBoard>().PreviewStorePlanTemplate(_viewer, id, revision, name, template);
        public Code PreviewRemovePlanTemplate(long id, long revision)
            => _world.Services.Get<IPlanBoard>().PreviewRemovePlanTemplate(_viewer, id, revision);
        public PlanGroupCheck PreviewPlanTemplateSelection(PlanTemplateSelection selection, JsonValue anchor, string group)
        {
            IPlanBoard board = _world.Services.Get<IPlanBoard>();
            Code check = board.ResolvePlanTemplate(_viewer, selection, out PlanTemplate template);
            return check.IsNone ? board.PreviewPlanTemplate(_viewer, template, anchor, group) : new PlanGroupCheck(check);
        }
        public PlanGroupCheck CapturePlanTemplate(string group, JsonValue origin, out PlanTemplate template)
            => _world.Services.Get<IPlanBoard>().CapturePlanTemplate(_viewer, group, origin, out template);
        public PlanGroupCheck PreviewPlanTemplateActivation(PlanTemplateSelection selection, JsonValue anchor, string group)
        {
            IPlanBoard board = _world.Services.Get<IPlanBoard>();
            Code check = board.ResolvePlanTemplate(_viewer, selection, out PlanTemplate template);
            return check.IsNone ? board.PreviewPlanTemplateActivation(_viewer, template, anchor, group) : new PlanGroupCheck(check);
        }
        public PlanGroupCheck PreviewPlanTemplate(PlanTemplate template, JsonValue anchor, string group)
            => _world.Services.Get<IPlanBoard>().PreviewPlanTemplate(_viewer, template, anchor, group);
        /// <summary>Provider-owned schema/anchor checks only; no live placement, resource or execution admission.</summary>
        public PlanGroupCheck PreviewPlanTemplateStructure(PlanTemplate template, JsonValue anchor)
        {
            _world.RequireHealthyUpdate(); IPlanBoard board = _world.Services.Get<IPlanBoard>();
            return template != null && !board.CanDisclosePlanTemplate(_viewer, template) ? new PlanGroupCheck(Codes.UnknownDef)
                : board.PreviewPlanTemplateStructure(template, anchor);
        }
        /// <summary>Detached engine-expanded intents only on whole-group admission; never a reservation or execution grant.</summary>
        public PlanGroupCheck ExpandPlanTemplate(PlanTemplate template, JsonValue anchor, string group, out PlanGroupIntent intent)
            => _world.Services.Get<IPlanBoard>().ExpandPlanTemplate(_viewer, template, anchor, group, out intent);
        public void CopyPlanGroup(string group, IList<PlanView> destination)
            => _world.Services.Get<IPlanBoard>().CopyPlanGroup(_viewer, group, destination);
        public PlanGroupCheck PreviewPlanGroup(PlanGroupIntent intent)
            => _world.Services.Get<IPlanBoard>().PreviewPlanGroup(_viewer, intent);
        public PlanGroupCheck PreviewPlanGroupEdit(string group, int priority, bool paused)
            => _world.Services.Get<IPlanBoard>().PreviewPlanGroupEdit(_viewer, group, priority, paused);
        public PlanGroupCheck PreviewPlanGroupMove(string group, long before)
            => _world.Services.Get<IPlanBoard>().PreviewPlanGroupMove(_viewer, group, before);
        public PlanGroupCheck PreviewPlanGroupCancel(string group)
            => _world.Services.Get<IPlanBoard>().PreviewPlanGroupCancel(_viewer, group);
        public PlanGroupCheck PreviewPlanGroupRepeat(string group, string newGroup)
            => _world.Services.Get<IPlanBoard>().PreviewPlanGroupRepeat(_viewer, group, newGroup);
        public Code PreviewPlan(PlanIntent intent)
            => _world.Services.Get<IPlanBoard>().PreviewPlan(_viewer, intent);
        public PlanGroupCheck PreviewRetirePlanHistory(PlanHistorySelection selection)
            => _world.Services.Get<IPlanBoard>().PreviewRetirePlanHistory(_viewer, selection);
        public PlanHistoryPolicyView? ReadPlanHistoryPolicy()
            => _world.Services.Get<IPlanBoard>().ReadPlanHistoryPolicy();
        public void CopyPlanHistory(IList<PlanHistoryCohortView> destination)
            => _world.Services.Get<IPlanBoard>().CopyPlanHistory(_viewer, destination);
        public PlanGroupCheck PreviewPlanDependencies(PlanDependencyChanges changes)
            => _world.Services.Get<IPlanBoard>().PreviewPlanDependencies(_viewer, changes);
        public Code PreviewPlanEdit(long plan, string group, int priority, bool paused)
            => _world.Services.Get<IPlanBoard>().PreviewPlanEdit(_viewer, plan, group, priority, paused);
        public Code PreviewPlanMove(long plan, long before)
            => _world.Services.Get<IPlanBoard>().PreviewPlanMove(_viewer, plan, before);
        public PlanGroupCheck PreviewPlanCancel(PlanCancellationSelection selection)
            => _world.Services.Get<IPlanBoard>().PreviewPlanCancel(_viewer, selection);
        public Code PreviewPlanRepeat(long plan)
            => _world.Services.Get<IPlanBoard>().PreviewPlanRepeat(_viewer, plan);
        public Code TryReadPolicy(EntityId worker, out WorkerPolicyView view)
        {
            view = default; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(worker);
            if (!_world.ViewFor(_viewer).CanObserveOwned(entity)) return Codes.NotFound;
            if (entity.Get<IWorkPolicy>() == null) return WorkPolicyCodes.NotPolicy;
            view = new WorkerPolicyView(entity, WorkPolicyRules.ReadObserved(_world, entity)); return Code.None;
        }
        public Code PreviewPolicy(EntityId worker, WorkPolicySettings settings)
        {
            _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(worker);
            return WorkPolicyRules.CheckConfigure(_world, _viewer == PlayerId.None ? entity?.Owner ?? _viewer : _viewer, entity, settings);
        }
        public WorkPolicySelectionPreview PreviewPolicyGroup(IReadOnlyList<EntityId> workers, WorkPolicySettings settings)
            => WorkPolicyRules.PreviewGroup(_world, _viewer, workers, settings);
        public OperatingAreaSnapshot ReadOperatingArea(string name) => _world.Services.Get<IOperatingAreas>().Read(_viewer, name);
        public void CopyOperatingAreas(IList<OperatingAreaSnapshot> destination) => _world.Services.Get<IOperatingAreas>().Copy(_viewer, destination);
        public Code PreviewOperatingArea(string name, string template, string shapeJson, long expectedRevision)
            => _world.Services.Get<IOperatingAreas>().PreviewSet(_viewer, name, template, shapeJson, expectedRevision);
        public Code PreviewRemoveOperatingArea(string name, long expectedRevision)
            => _world.Services.Get<IOperatingAreas>().PreviewRemove(_viewer, name, expectedRevision);
        public void CopyJobs(IList<WorkJobView> destination)
        {
            destination.Clear(); _world.RequireHealthyUpdate();
            WorkBoard board = _world.Services.Get<WorkBoard>();
            var facts = EntityObservation.ReadProvider(_world, board, null, () => {
                var rows = new List<WorkJobView>();
                foreach (Job job in board.Jobs)
                    if (job.IsLive && Discloses(job)) rows.Add(new WorkJobView(job, board));
                return rows;
            });
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) destination.Add(fact);
        }

        public void CopyDispatchQueue(IList<WorkDispatchOffer> destination)
        {
            destination.Clear(); _world.RequireHealthyUpdate();
            var queue = _world.Services.Get<IWorkDispatchQueue>();
            var facts = EntityObservation.ReadProvider(_world, queue, null, () => {
                var rows = new List<WorkDispatchOffer>(); queue.CopyQueue(_world, _viewer, rows);
                rows.RemoveAll(offer => !Discloses(_world.Services.Get<WorkBoard>().Job(offer.Job))); return rows;
            });
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) destination.Add(fact);
        }

        /// <summary>Actual cross-provider machine arbitration, derived by the same owner that assigns work.</summary>
        public bool TryReadDispatchPolicy(out WorkerDispatchPolicyView view)
        {
            view = null; _world.RequireHealthyUpdate();
            if (!_world.Services.TryGet<IWorkerDispatchPolicy>(out var policy)) return false;
            var access = _world.Services.Get<IContentAccess>();
            if (!EntityObservation.ReadProvider(_world, access, null, () => access.Check(_world, _viewer, policy)).IsNone) return false;
            view = EntityObservation.ReadProvider(_world, policy, null, () => new WorkerDispatchPolicyView(policy));
            return true;
        }
        public void CopyAssignments(IList<WorkerDispatchOffer> destination)
        {
            destination.Clear(); _world.RequireHealthyUpdate();
            var queue = _world.Services.Get<IWorkDispatchQueue>();
            var facts = EntityObservation.ReadProvider(_world, queue, null, () => {
                var rows = new List<WorkerDispatchOffer>(); queue.CopyAssignments(_world, _viewer, rows);
                rows.RemoveAll(offer => !Sees(offer.Company) || !SeesTarget(offer.Target) || !SeesTarget(offer.Worker) ||
                    offer.Scope == WorkModule.ModuleId && (offer.Entry == "job"
                        ? !Discloses(_world.Services.Get<WorkBoard>().Job(checked((int)offer.Identity)))
                        : !Discloses(_world.Services.Get<WorkBoard>().Order(checked((int)offer.Identity)))));
                return rows;
            });
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) destination.Add(fact);
        }

        /// <summary>Effect readiness through its optional provider contract, never inferred in a host.</summary>
        public Code PreviewNodeEffect(int orderId, string nodeId)
        {
            WorkBoard board = _world.Services.Get<WorkBoard>(); Order order = board.Order(orderId);
            _world.RequireHealthyUpdate();
            if (!Discloses(order)) return Codes.NotFound;
            IActivityNodeDefinition node = order.Plan.Graph.Find(nodeId);
            return node == null ? Codes.InvalidArgument : board.PreviewEffect(node.Effect, order, node);
        }

        /// <summary>The declared final effect, using the same pure engine boundary as node effects.</summary>
        public Code PreviewOrderEffect(int orderId)
        {
            _world.RequireHealthyUpdate();
            WorkBoard board = _world.Services.Get<WorkBoard>(); Order order = board.Order(orderId);
            if (order == null) return Codes.NotFound;
            return !Discloses(order) ? Codes.NotFound : board.PreviewEffect(order.Plan.OnComplete, order, null);
        }

        /// <summary>Detached earned completion for this seat only; None target explicitly selects company scope.</summary>
        public bool TryReadServiceCompletion(string plan, EntityId target, Code source, Code capability, out ActivityCompletionFact completion)
        {
            completion = default;
            _world.RequireHealthyUpdate();
            return _world.Players.IsSeat(_viewer) && SeesTarget(target) && _world.Services.Get<IActivityCatalog>().CanDiscloseActivity(_viewer, plan)
                && _world.Services.Get<IActivityCompletionHistory>()
                .TryReadServiceCompletion(_viewer, plan, target, source, capability, out completion);
        }
        public bool TryReadActivityCompletion(string plan, EntityId target, out ActivityCompletionFact completion)
            => TryReadCompanyActivityCompletion(_viewer, plan, target, out completion);
        /// <summary>Company-qualified completion diagnosis; ordinary viewers still require their own seat and complete metadata sight.</summary>
        public bool TryReadCompanyActivityCompletion(PlayerId company, string plan, EntityId target, out ActivityCompletionFact completion)
        {
            completion = default;
            _world.RequireHealthyUpdate();
            return _world.Players.IsSeat(company) && (_viewer == PlayerId.None || _viewer == company) && SeesTarget(target)
                && _world.Services.Get<IActivityCatalog>().CanDiscloseActivity(_viewer, plan)
                && _world.Services.Get<IActivityCompletionHistory>()
                .TryReadCompletion(company, plan, target, out completion);
        }

        /// <summary>Current criteria for a proposed order, without posting, reserving or consuming.</summary>
        public Code PreviewOrderRequirements(string plan, EntityId target, IList<WorkRequirementView> dst)
        {
            dst.Clear();
            _world.RequireHealthyUpdate();
            IAuthoredActivityDefinition definition = _world.Content.Find<IAuthoredActivityDefinition>(plan);
            if (definition == null) return Codes.UnknownDef;
            if (!_world.Services.Get<IActivityCatalog>().CanDiscloseActivity(_viewer, definition.PlanKey)) return Codes.UnknownDef;
            Entity entity = _world.Entities.Get(target);
            if (entity == null || !SeesTarget(target)) return Codes.NotFound;
            if (!Sees(entity.Owner)) return Codes.NotOwned;
            Code access = PlanObservation.Intent(definition, _world, entity.Owner, entity);
            if (!access.IsNone) return access;
            var facts = new List<WorkRequirementView>();
            CopyRequirements(definition, entity.Owner, entity, facts);
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) dst.Add(fact);
            return Code.None;
        }

        /// <summary>Current per-entry diagnostics for an order, using the same provider evaluator.</summary>
        public bool TryCopyOrderRequirements(int order, IList<WorkRequirementView> dst)
        {
            dst.Clear();
            _world.RequireHealthyUpdate();
            Order current = _world.Services.Get<WorkBoard>().Order(order);
            if (!Discloses(current)) return false;
            Entity target = _world.Entities.Get(current.Target);
            if (target == null) return false;
            var facts = new List<WorkRequirementView>();
            CopyRequirements(current.Plan, current.Owner, target, facts, current);
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) dst.Add(fact);
            return true;
        }

        private void CopyRequirements(IActivityDefinition plan, PlayerId owner, Entity target, IList<WorkRequirementView> dst, Order order = null)
        {
            WorkBoard board = _world.Services.Get<WorkBoard>();
            for (int i = 0; i < plan.Graph.Nodes.Count; i++)
            {
                IActivityNodeDefinition node = plan.Graph.Nodes[i];
                board.EvaluateRequirements(owner, target, node, order?.Node(node.Id),
                    (requirement, path, result) => dst.Add(new WorkRequirementView(node.Id, requirement, path, result)));
            }
        }

        /// <summary>Detached canonical node states; private orders disclose no partial data.</summary>
        public bool TryCopyNodes(int orderId, IList<ActivityNodeView> destination)
        {
            destination.Clear();
            _world.RequireHealthyUpdate();
            WorkBoard board = _world.Services.Get<WorkBoard>(); Order order = board.Order(orderId);
            if (!Discloses(order)) return false;
            var facts = new List<ActivityNodeView>(order.Nodes.Count);
            foreach (ActivityNodeExecution node in order.Nodes)
                facts.Add(new ActivityNodeView(order.Plan.Graph.Find(node.Id), node, _world.Time, board.Job(node.JobId), board));
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) destination.Add(fact);
            return true;
        }

        /// <summary>Private immutable nominal work and critical path from the actual authoritative graph.</summary>
        public bool TryReadProgress(int orderId, out ActivityProgressView value)
        {
            value = null; _world.RequireHealthyUpdate();
            Order order = _world.Services.Get<WorkBoard>().Order(orderId);
            if (!Discloses(order)) return false;
            var facts = new ActivityProgressView(order, _world.Time, _world.Tick);
            _world.RequireHealthyUpdate(); value = facts; return true;
        }

        public void CopyOrders(IList<OrderView> dst)
        {
            dst.Clear();
            _world.RequireHealthyUpdate();
            var facts = new List<OrderView>();
            WorkBoard board = _world.Services.Get<WorkBoard>();
            IReadOnlyList<Order> orders = board.Orders;
            for (int i = 0; i < orders.Count; i++)
            {
                Order o = orders[i];
                if (!Discloses(o) || !o.IsActive && !board.RetainsOrder(o)) continue;
                var v = new OrderView
                {
                    Id = o.Id, ExecutionBinding = o.ExecutionBinding, Plan = o.PlanKey, Target = o.Target, State = o.State, Priority = o.Priority,
                    CreatedTick = o.CreatedTick,
                    NodesDone = o.NodesDone, NodeCount = o.Nodes.Count, Blocker = o.Blocker,
                    CompletionFailures = o.CompletionFailures, CompletionRetryAt = o.CompletionRetryAt,
                    CompletionRetryRemainingTicks = o.CompletionRetryAt > _world.Tick ? o.CompletionRetryAt - _world.Tick : 0,
                    CompletionEffectApplied = o.CompletionEffectApplied
                };
                for (int s = 0; s < o.Nodes.Count; s++)
                {
                    if (o.Nodes[s].State == NodeState.Done) continue;
                    Job job = board.Job(o.Nodes[s].JobId);
                    v.CurrentRole = job?.Worker.IsValid == true ? job.Role : WorkerRoleId.None;
                    v.CurrentProgress = o.Nodes[s].Progress;
                    v.CurrentNeeded = _world.Time.Ticks(o.Plan.Graph.Nodes[s].Duration);
                    break;
                }
                facts.Add(v);
            }
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) dst.Add(fact);
        }

        public int ActiveOrders(EntityId target)
        {
            _world.RequireHealthyUpdate(); int n = 0;
            IReadOnlyList<Order> orders = _world.Services.Get<WorkBoard>().Orders;
            for (int i = 0; i < orders.Count; i++) if (orders[i].IsActive && orders[i].Target == target && Discloses(orders[i])) n++;
            return n;
        }

        /// <summary>Private current offers from the same selector used for actual assignment.</summary>
        public bool TryCopyWorkerCandidates(int orderId, string nodeId, IList<WorkWorkerCandidate> destination)
        {
            destination.Clear();
            Order order = _world.Services.Get<WorkBoard>().Order(orderId);
            _world.RequireHealthyUpdate();
            if (!Discloses(order)) return false;
            IActivityNodeDefinition node = order.Plan.Graph.Find(nodeId); if (node == null) return false;
            Entity target = _world.Entities.Get(order.Target); if (target == null) return false;
            ActivityNodeExecution execution = order.Node(nodeId);
            var dispatch = _world.Services.Get<IWorkerDispatch>();
            var facts = EntityObservation.ReadProvider(_world, dispatch, target, () => {
                var rows = new List<WorkWorkerCandidate>();
                dispatch.CopyCandidates(_world, order.Owner, node.Roles, target, node,
                    execution.JobId, execution.Choices, execution, WorkCandidateMode.EmergencyInterruption, rows, null);
                return rows;
            });
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) destination.Add(fact);
            return true;
        }

        public Code PreviewWorkerCandidates(string plan, string nodeId, EntityId target, IList<WorkWorkerCandidate> destination)
        {
            destination.Clear(); var criteria = new List<WorkRequirementView>();
            Code refusal = PreviewOrderRequirements(plan, target, criteria);
            if (!refusal.IsNone) return refusal;
            IAuthoredActivityDefinition definition = _world.Content.Get<IAuthoredActivityDefinition>(plan);
            IActivityNodeDefinition node = definition.Graph.Find(nodeId); if (node == null) return Codes.InvalidArgument;
            Entity entity = _world.Entities.Get(target);
            var dispatch = _world.Services.Get<IWorkerDispatch>();
            var facts = EntityObservation.ReadProvider(_world, dispatch, entity, () => {
                var rows = new List<WorkWorkerCandidate>();
                dispatch.CopyCandidates(_world, entity.Owner, node.Roles, entity, node, 0, null, null, WorkCandidateMode.IdleOnly, rows, null);
                return rows;
            });
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) destination.Add(fact);
            return Code.None;
        }

        /// <summary>Which machines of a role the company has idle right now.</summary>
        public int IdleWorkers(WorkerRoleId role)
        {
            _world.RequireHealthyUpdate();
            int count = EntityObservation.ReadProvider(_world, this, null, () => {
                int n = 0;
                ComponentList<IWorker> workers = _world.Entities.Components<IWorker>();
                for (int i = 0; i < workers.Count; i++)
                    if (workers[i].Entity.Alive && Sees(workers[i].Entity.Owner) && workers[i].Idle && WorkerExecutionContracts.Can(_world, workers[i].Entity, role)) n++;
                return n;
            });
            _world.RequireHealthyUpdate(); return count;
        }
    }

}
