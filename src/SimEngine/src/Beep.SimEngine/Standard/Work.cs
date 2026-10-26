using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // ======================================================================== content

    /// <summary>Sole JSON declaration for the reusable worker; no live assignment or rate store.</summary>
    public sealed class WorkerDeclaration
    {
        private readonly string[] _roleIds;
        public IReadOnlyList<WorkerRoleId> Roles { get; private set; }
        public Fix WorkRate { get; }
        public EntityStateBindings States { get; private set; }
        private WorkerDeclaration(JsonReader reader)
        {
            _roleIds = reader.Strings("roles");
            if (_roleIds.Length == 0) throw reader.Error("a worker needs at least one role");
            WorkRate = reader.Fix("work_rate");
            if (!WorkRate.IsPositive) throw reader.Error("work_rate must be positive");
            States = EntityStateBindings.Read(reader, "working", "loading", "stopped");
        }
        public static WorkerDeclaration Read(JsonReader reader)
            => new WorkerDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            var roles = new WorkerRoleId[_roleIds.Length];
            var distinct = new HashSet<WorkerRoleId>();
            for (int i = 0; i < roles.Length; i++)
            {
                roles[i] = content.Def<IWorkerRoleDefinition>(_roleIds[i]).Role;
                if (!distinct.Add(roles[i])) throw content.Fail("duplicate worker role '" + roles[i] + "'");
            }
            Roles = Array.AsReadOnly(roles); States = States.Resolve(content);
        }
    }

    /// <summary>A machine's authored roles, work rate and working/loading/stopped state bindings.</summary>
    public sealed class WorkerDef : ComponentDef<Worker>, IStatefulWorkerDefinition
    {
        private WorkerDeclaration _declaration;
        public IReadOnlyList<WorkerRoleId> Roles => _declaration.Roles;
        public Fix WorkRate => _declaration.WorkRate;
        public EntityStateBindings States => _declaration.States;
        protected override void ReadCore(JsonReader reader) => _declaration = WorkerDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }

    /// <summary>A machine's job and home. Assigned and driven by <see cref="WorkSystem"/>.</summary>
    public sealed class Worker : Component, IStatefulWorker
    {
        public new IStatefulWorkerDefinition Def => (IStatefulWorkerDefinition)base.Def;
        public int Job;
        private EntityId _home;
        public EntityId Home
        {
            get => _home;
            set
            {
                if (value.Value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                if (_home == value) return;
                Entity.Touch(); _home = value;
            }
        }
        /// <summary>Under player control: takes no automatic work until released.</summary>
        public bool Held;
        public bool HeldArrived;

        public bool Idle => Job == 0;
        public IReadOnlyList<WorkerRoleId> Roles => Def.Roles;
        public bool Can(WorkerRoleId role)
        {
            var roles = Def.Roles;
            for (int i = 0; i < roles.Count; i++) if (roles[i] == role) return true;
            return false;
        }
        int IWorker.Job { get => Job; set => Job = value; }
        EntityId IWorker.Home { get => Home; set => Home = value; }
        bool IWorker.Held { get => Held; set => Held = value; }
        bool IWorker.HeldArrived { get => HeldArrived; set => HeldArrived = value; }

        public IEntityStateDefinition GetState(EntityUpdateContext context)
        {
            if (Held && Entity.Get<IMotion>()?.Status != MoveStatus.Moving) return Def.States["stopped"];
            Code read = context.World.Services.Get<IWorkAssignments>().ReadAssignment(Entity.Owner, Entity.Id, out var assignment);
            if (!read.IsNone) throw new CorruptStateException("actual worker state cannot read its assignment");
            if (!assignment.HasValue) return null;
            WorkExecutionFacts job = assignment.Value;
            if (job.State == JobState.Paused) return Def.States["stopped"];
            if ((job.Blocker == WorkDockCodes.Waiting || job.Blocker == WorkDockCodes.Occupied || job.Blocker == WorkDockCodes.DepartureBlocked) &&
                Entity.Get<IMotion>()?.Status != MoveStatus.Moving) return Def.States["stopped"];
            if (job.State != JobState.Assigned || !job.PhasePurpose.HasValue) return null;
            WorkPhasePurpose purpose = job.PhasePurpose.Value;
            return purpose == WorkPhasePurpose.Productive ? Def.States["working"] :
                purpose == WorkPhasePurpose.Pickup || purpose == WorkPhasePurpose.Delivery ? Def.States["loading"] : null;
        }

        public override void Sync(IStateSync s)
        {
            int job = Job; EntityId home = _home; bool held = Held, arrived = HeldArrived;
            s.Int(ref job); s.Entity(ref home); s.Bool(ref held); s.Bool(ref arrived);
            if (job < 0 || home.Value < 0) throw new CorruptStateException("invalid saved worker assignment/home");
            if (s.Reading) { Job = job; _home = home; Held = held; HeldArrived = arrived; }
        }
    }

    /// <summary>One authored node: allowed worker roles, duration, named dependencies and physical inputs.</summary>
    public sealed class ActivityNodeDef : IActivityNodeDefinition
    {
        public string Id { get; private set; }
        private string _executorId;
        public IActivityExecutor Executor { get; private set; }
        /// <summary>Any compatible declared role; empty for timed work without a machine.</summary>
        public IReadOnlyList<WorkerRoleId> Roles { get; private set; }
        private string[] _roleIds;
        private string _requirementId;
        public IWorkRequirement Requirement { get; private set; }
        /// <summary>Exact authored work duration; the sole session time converter determines work units.</summary>
        public GameDuration Duration { get; private set; }
        public IReadOnlyList<string> Dependencies { get; private set; }
        public Fix RequirementMultiplier { get; private set; } = Fix.One;
        /// <summary>Effect id run when the node completes (registered by a module).</summary>
        public string Effect { get; private set; }

        public static ActivityNodeDef Read(JsonReader r)
        {
            var s = new ActivityNodeDef();
            s.Id = r.String("id");
            s._roleIds = r.Strings("roles");
            s._executorId = r.String("executor");
            s._requirementId = r.NullableString("requirement");
            s.Duration = new JsonReader(r.Object("duration") ?? throw r.Error("node duration is required"), r.Where + ".duration").RationalDuration();
            s.Dependencies = Array.AsReadOnly(r.Strings("after"));
            s.RequirementMultiplier = r.Fix("requirement_multiplier", Fix.One);
            s.Effect = r.NullableString("effect");
            r.End();
            return s;
        }

        /// <summary>A node built in code (for generated plans such as construction).</summary>
        public static ActivityNodeDef Make(string id, IActivityExecutor executor, IReadOnlyList<WorkerRoleId> roles, GameDuration duration, string[] after, string effect,
            IWorkRequirement requirement = null, Fix? requirementMultiplier = null)
            => new ActivityNodeDef { Id = id, Executor = executor ?? throw new ArgumentNullException(nameof(executor)), Roles = roles == null ? throw new ArgumentNullException(nameof(roles)) : new List<WorkerRoleId>(roles).AsReadOnly(), Duration = duration,
                Dependencies = Array.AsReadOnly(after == null ? Array.Empty<string>() : (string[])after.Clone()),
                Effect = effect, RequirementMultiplier = requirementMultiplier ?? Fix.One,
                Requirement = requirement };

        internal void Resolve(ContentResolver c, string owner)
        {
            Executor = c.Def<IActivityExecutor>(_executorId);
            var roles = new WorkerRoleId[_roleIds.Length];
            for (int i = 0; i < roles.Length; i++) roles[i] = c.Def<IWorkerRoleDefinition>(_roleIds[i]).Role;
            Roles = Array.AsReadOnly(roles); _roleIds = null;
            Requirement = c.OptionalDef<IWorkRequirement>(_requirementId);
        }

        internal void BindRequirement(IActivityRequirementBinding binding) => Requirement = binding;

    }

    /// <summary>An activity that can be ordered: a compiled dependency graph, and an effect when all are done.</summary>
    public interface IActivityDefinition
    {
        bool AutomaticSupply { get; }
        /// <summary>Actual immutable content required to disclose this activity; nonempty, including authored bindings.</summary>
        IReadOnlyList<IContentDefinition> DisclosureContent { get; }
        /// <summary>Stable key saved with orders: "activity:&lt;id&gt;" or a module's own prefix.</summary>
        string PlanKey { get; }
        IActivityGraph Graph { get; }
        /// <summary>Effect id run when the order completes, or null.</summary>
        string OnComplete { get; }
    }

    /// <summary>
    /// An order players (or systems) can place on a target: repair, drill, survey, decommission.
    /// Content, section "activities".
    /// </summary>
    public class ActivityDef : Def, IAuthoredActivityDefinition
    {
        private ActivityDeclaration _declaration;
        public bool AutomaticSupply => _declaration.AutomaticSupply;
        public string PlanKey => "activity:" + Id;
        public virtual IReadOnlyList<IContentDefinition> DisclosureContent => System.Array.AsReadOnly(new IContentDefinition[] { this });
        public virtual IActivityDefinition BindExecution(World world, EntityId target) => this;
        public IActivityGraph Graph => _declaration.Graph;
        public string OnComplete => _declaration.OnComplete;
        /// <summary>A player may place it with the post-order command (otherwise only the game posts it).</summary>
        public bool PlayerOrderable => _declaration.PlayerOrderable;
        /// <summary>The target must carry one of these entity tags (empty: any owned entity).</summary>
        public IReadOnlyList<string> TargetTags => _declaration.TargetTags;
        public int Priority => _declaration.Priority;

        protected override void ReadCore(JsonReader r) => _declaration = ActivityDeclaration.Read(r);

        protected override void ResolveCore(ContentResolver c) => _declaration.Resolve(c, this);

        public bool Targets(EntityDef def) => ActivityDefinitionContracts.Targets(this, def);

        public virtual Code CheckIntent(World world, PlayerId company, Entity target) => ActivityDefinitionContracts.CheckIntent(this, world, company, target);

        public virtual string IntentIdentity(World world, JsonValue arguments)
            => ActivityDefinitionContracts.IntentIdentity(this, arguments);
        public virtual IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments)
            => PlanEntityReferenceContracts.Read(this, arguments);
        public virtual Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination)
            => PlanEntityReferenceContracts.Check(this, world, company, arguments, source, destination);
        public virtual JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination)
            => PlanEntityReferenceContracts.Replace(this, arguments, source, destination);
        public virtual void ValidateTargetDefinition(EntityDef target) => ActivityDefinitionContracts.ValidateTargetDefinition(this, target);
        public virtual PlanIntent CreateTargetIntent(EntityId target, IActivityWorkerSelection workers, string group, int priority)
            => ActivityDefinitionContracts.CreateTargetIntent(this, target, workers, group, priority);
        public virtual string ExecutionIdentity(World world, Order order) => order.WorkerSelection.Identity(order.Target);
        public virtual Code CheckSubmission(World world, PlayerId company, JsonValue arguments)
            => ActivityDefinitionContracts.CheckSubmission(this, world, company, arguments);
        public virtual Code CheckActivation(World world, PlayerId company, JsonValue arguments)
            => ActivityDefinitionContracts.CheckActivation(this, world, company, arguments);
        public virtual Order Activate(World world, PlayerId company, JsonValue arguments, int priority)
            => ActivityDefinitionContracts.Activate(this, world, company, arguments, priority);
    }

    // ======================================================================== runtime





    /// <summary>A placed order: a plan applied to one target for one company.</summary>
    public sealed class Order
    {
        public long CompletedTick { get; internal set; } = -1;
        public int Id;
        public string PlanKey;
        public IActivityDefinition Plan;
        public PlayerId Owner;
        public EntityId Target;
        public ActivityExecutionBinding ExecutionBinding { get; internal set; }
        /// <summary>Immutable selected-node intent. Actual claims stay in Job.Worker/IWorker.Job.</summary>
        public ActivityWorkerSelection WorkerSelection { get; internal set; } = ActivityWorkerSelection.Unbound;
        public OrderState State;
        public int Priority;
        public long CreatedTick;
        /// <summary>A response binds the normal order to one incident owner and episode.</summary>
        public Code ResponseSource;
        public int ResponseEpisode;
        private IReadOnlyList<ActivityNodeExecution> _nodes = Array.AsReadOnly(Array.Empty<ActivityNodeExecution>());
        private readonly Dictionary<string, ActivityNodeExecution> _byNode = new Dictionary<string, ActivityNodeExecution>(StringComparer.Ordinal);
        public IReadOnlyList<ActivityNodeExecution> Nodes => _nodes;
        public ActivityNodeExecution Node(string id) => id != null && _byNode.TryGetValue(id, out ActivityNodeExecution node) ? node : null;
        internal void BindNodes()
        {
            var nodes = new List<ActivityNodeExecution>(); _byNode.Clear();
            foreach (IActivityNodeDefinition definition in Plan.Graph.Nodes)
            {
                var node = new ActivityNodeExecution(Id, definition.Id); nodes.Add(node); _byNode.Add(node.Id, node);
            }
            _nodes = nodes.AsReadOnly();
        }
        public Code Blocker;
        public int CompletionFailures { get; internal set; }
        public long CompletionRetryAt { get; internal set; }
        public bool CompletionEffectApplied { get; internal set; }
        public bool IsActive => State == OrderState.Active || State == OrderState.Completing;
        public int NodesDone
        {
            get { int done = 0; foreach (ActivityNodeExecution node in Nodes) if (node.State == NodeState.Done) done++; return done; }
        }
    }

    /// <summary>The sole saved node execution record; named lookup aliases these same records.</summary>
    public sealed class ActivityNodeExecution
    {
        internal ActivityNodeExecution(int order, string id) { OrderId = order; Id = id; }
        internal int OrderId { get; }
        public string Id { get; }
        public NodeState State { get; internal set; }
        public int JobId { get; internal set; }
        /// <summary>The actual final executing machine, captured only after earned work and physical departure.</summary>
        public EntityId CompletedBy { get; internal set; }
        public WorkProgress Progress { get; internal set; }
        /// <summary>Actual funded productive updates, not an independent clock. Waits/travel earn none.</summary>
        public long ProductiveSteps { get; internal set; }
        public Code Blocker { get; internal set; }
        public int CompletionFailures { get; internal set; }
        public long CompletionRetryAt { get; internal set; }
        public IReadOnlyList<WorkRequirementChoice> Choices { get; internal set; } = Array.Empty<WorkRequirementChoice>();
        public IReadOnlyList<WorkResourceBinding> Resources { get; internal set; } = Array.Empty<WorkResourceBinding>();
        public IReadOnlyList<WorkCostBinding> Costs { get; internal set; } = Array.Empty<WorkCostBinding>();
    }

    public enum JobState : byte { Open = 0, Assigned = 1, Done = 2, Failed = 3, Cancelled = 4, Paused = 5 }



    /// <summary>One thing a worker does in a job: go somewhere, work, load, unload.</summary>
    public struct Toil
    {
        public Code Executor;
        public EntityId At;
        public ResourceId Resource;
        public Fix Amount;

        public static Toil GoTo(EntityId at) => new Toil { Executor = WorkPhases.Travel, At = at };
        public static Toil Work(EntityId at) => new Toil { Executor = WorkPhases.Productive, At = at };
        public static Toil Load(EntityId at, ResourceId resource, Fix amount) => new Toil { Executor = WorkPhases.Pickup, At = at, Resource = resource, Amount = amount };
        public static Toil Unload(EntityId at, ResourceId resource, Fix amount) => new Toil { Executor = WorkPhases.Delivery, At = at, Resource = resource, Amount = amount };
        public static Toil LeaveDock(EntityId at) => new Toil { Executor = WorkPhases.Departure, At = at };
    }

    /// <summary>
    /// Work for one machine: a role, a queue of toils, and the reservation of the machine while it runs
    /// (RimWorld's Job). A job for an order node works that node; a haul job carries a load and holds the
    /// reservations that load needs (the pickup and the room), so ending the job frees exactly those.
    /// </summary>
    public sealed class Job
    {
        public int Id;
        public PlayerId Owner;
        public WorkerRoleId Role;
        /// <summary>Standalone-job intent only. Node jobs read their parent Order.Priority.</summary>
        public int Priority;
        public JobState State;
        /// <summary>The retained haul's trip-policy hold, not a copied budget or independent timer.</summary>
        public bool TripHeld;
        public EntityId Worker;
        public EntityId Target;
        public readonly List<Toil> Toils = new List<Toil>();
        public int ToilIndex;
        public int OrderId;
        public string NodeId;
        /// <summary>A haul: carry <see cref="Amount"/> of <see cref="Resource"/> from <see cref="From"/> (none: the carrier already holds it) to <see cref="To"/>.</summary>
        public bool IsHaul;
        /// <summary>This job buys from a supplier at pickup rather than taking an owned outlet's stock.</summary>
        public bool Purchase;
        /// <summary>The standing route that posted this haul (0: none).</summary>
        public int Route;
        /// <summary>Original requesting workflow. Current urgency stays with its declared owner.</summary>
        public LogisticsDemandId HaulDemand;
        public Code HaulSelection;
        /// <summary>Actual dispatch provenance, not another recovery intent/history owner.</summary>
        public LogisticsRecoveryKind? RecoveryKind;
        public EntityId From;
        public EntityId To;
        public Code SourcePort;
        public Code DestinationPort;
        /// <summary>Admitted physical bindings outlive spent receipts; never stock or a reservation.</summary>
        public ResourceEndpointId SourceEndpoint;
        public ResourceEndpointId DestinationEndpoint;
        public ResourceId Resource;
        public Fix Amount;
        /// <summary>The pickup at <see cref="From"/> is still reserved for this job.</summary>
        public ReservationToken OutReservation;
        /// <summary>The room at <see cref="To"/> is still reserved for this job.</summary>
        public ReservationToken InReservation;
        public ReservationToken CargoInReservation;
        public ReservationToken CargoOutReservation;
        internal const string HaulReservationScope = "sim.haul";
        public ReservationOwner ReservationOwner => new ReservationOwner(HaulReservationScope, Id);
        /// <summary>Attempts that failed since the job last made progress.</summary>
        public int Failures;
        public long RetryAt;
        public Fix Loaded;
        /// <summary>Elapsed handling of the current load/unload leg, never a copied stock quantity.</summary>
        public WorkProgress HandlingProgress { get; internal set; }
        public Fix UnloadingBaseline { get; internal set; }
        /// <summary>Authored cause of this job's retained, loaded recovery-departure phase.</summary>
        public Code RecoveryCause { get; internal set; }
        /// <summary>The old bay retained by this job until its actual departure geometry is reached.</summary>
        public Code RecoveryDockSlot { get; internal set; }
        public Code SourceDockSlot { get; internal set; }
        public Code DestinationDockSlot { get; internal set; }
        public long CreatedTick;

        public bool IsLive => State == JobState.Open || State == JobState.Assigned || State == JobState.Paused;
        public Code Blocker;
    }

    /// <summary>
    /// Every order and job in the game (saved and hashed). Systems and command handlers post orders
    /// here; <see cref="WorkSystem"/> turns them into jobs and runs the jobs.
    /// </summary>
    public sealed partial class WorkBoard : IPersistentService, ICompositionValidator, ILogisticsDemandOwner, IEntityReplacementReferenceOwner
    {
        public Code CheckReferenceReplacement(World world, Entity source, EntityDef destination)
        {
            if (!ReferenceEquals(world, _world)) throw new InvalidOperationException("reference owner belongs to another session");
            Code homes = WorkerHomeContracts.CheckHomeReferences(world, source, destination);
            return homes.IsNone ? CheckPlanReferenceReplacement(source, destination) : homes;
        }
        public void ReplaceEntityReferences(World world, Entity source, Entity destination)
        {
            if (!ReferenceEquals(world, _world)) throw new InvalidOperationException("reference owner belongs to another session");
            WorkerHomeContracts.ReplaceHomeReferences(world, source, destination);
            ReplacePlanReferences(source, destination);
        }
        public int StateSchemaVersion => 54;
        public IWorkRetryPolicy RetryPolicy { get; }
        private readonly World _world;
        private readonly List<Order> _orders = new List<Order>();
        private readonly List<Job> _jobs = new List<Job>();
        private readonly Dictionary<int, Order> _orderById = new Dictionary<int, Order>();
        private readonly Dictionary<int, Job> _jobById = new Dictionary<int, Job>();
        private int _nextOrder = 1, _nextJob = 1;

        public WorkBoard(World world, IWorkRetryPolicy retryPolicy)
        {
            _world = world;
            RetryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        }

        public string Id => "sim.work";
        public World World => _world;
        public string DemandScope => WorkModule.ModuleId;
        public Code CheckSourceDisclosure(PlayerId viewer, LogisticsDemandId source, PlayerId company, EntityId destination)
        {
            if (!_world.ViewFor(viewer).CanObserveOwned(_world.Entities.Get(destination))) return Codes.NotFound;
            if (source.Scope != DemandScope || source.Identity > int.MaxValue || !source.Entry.IsNone) return Codes.NotFound;
            Order order = Order((int)source.Identity);
            return order != null && order.Owner == company && order.Target == destination &&
                _world.Services.Get<IActivityCatalog>().CanDiscloseActivity(viewer, order.PlanKey) ? Code.None : Codes.NotFound;
        }
        public LogisticsDemandSourceCheck CheckSource(LogisticsDemandId source, PlayerId company, EntityId destination)
        {
            if (source.Scope != DemandScope || source.Identity > int.MaxValue || !source.Entry.IsNone)
                return LogisticsDemandSourceCheck.Invalid(LogisticsDemandCodes.InvalidSource);
            Order order = Order((int)source.Identity);
            if (order == null || !order.IsActive) return LogisticsDemandSourceCheck.Expired(LogisticsDemandCodes.Expired);
            if (order.Owner != company || order.Target != destination) return LogisticsDemandSourceCheck.Invalid(Codes.NotOwned);
            Code planBlocker = PlanOrderBlocker(order.Id);
            if (!planBlocker.IsNone) return LogisticsDemandSourceCheck.Expired(planBlocker);
            Code binding = ResponseWork.CheckActiveOrder(_world, _world.Entities.Get(destination), order);
            return binding.IsNone ? LogisticsDemandSourceCheck.Active(!order.ResponseSource.IsNone) : LogisticsDemandSourceCheck.Expired(binding);
        }

        public IReadOnlyList<Order> Orders => _orders.AsReadOnly();
        public IReadOnlyList<Job> Jobs => _jobs.AsReadOnly();
        /// <summary>Something changed that an assignment pass should look at. Saved: when a pass runs is part of the game's state.</summary>
        public bool Dirty { get; set; } = true;

        public Order Order(int id) => _orderById.TryGetValue(id, out Order o) ? o : null;
        public Job Job(int id) => _jobById.TryGetValue(id, out Job j) ? j : null;

        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world) || !ReferenceEquals(world.Content.Find<IWorkRetryPolicy>(RetryPolicy.Id), RetryPolicy) ||
                RetryPolicy.CanRetry(Code.None)) throw new ContentException("work retry policy must alias its selected immutable content");
            _ = RetryDelay(1); _ = RetryDelay(int.MaxValue);
            ValidatePlanHistoryPolicy();
            ValidateTemplates();
            ValidateRecurringDefinitions();
            try
            {
                world.Time.ExactTicks(Tuning.AssignFallbackInterval);
            }
            catch (ArgumentException ex) { throw new ContentException("standard time requirements: " + ex.Message); }
            catch (OverflowException ex) { throw new ContentException("standard duration range: " + ex.Message); }
            IReadOnlyList<IWorkRequirement> requirements = _world.Content.All<IWorkRequirement>();
            foreach (IWorkRequirement requirement in requirements)
                EntityObservation.ReadProvider(_world, requirement, null, () => { ValidateRequirementDefinition(requirement); return true; });
            // The same compiler used by ActivityGraph owns named reference and cycle validation.
            var requirementGraph = new DependencyGraph<IWorkRequirement>(requirements, "work requirements");
            foreach (IWorkEffect effect in _world.Content.All<IWorkEffect>()) WorkEffectContracts.ValidateDefinition(_world, effect);
            foreach (string key in PlanKeys) ValidatePlan(ResolvePlan(key));
            ValidateActivityPrerequisites(requirementGraph);
            IReadOnlyList<EntityDef> entities = _world.Content.All<EntityDef>();
            for (int i = 0; i < entities.Count; i++)
            {
                ISpatialPresenceDefinition presence = entities[i].Component<ISpatialPresenceDefinition>();
                if (presence != null && presence.Space != world.Settings.Space) continue;
                if (entities[i].HasCapability<IWorker>() && entities[i].HasCapability<ITransporter>() && !world.Services.Has<IHaulExecution>())
                    throw new ContentException(entities[i].Id + ": a cargo worker requires IHaulExecution");
                if (entities[i].HasCapability<IWorkDock>()) WorkDockRules.ValidateDefinition(entities[i]);
            }
        }

        internal long RetryDelay(int failures)
        {
            long delay = RetryPolicy.Delay(_world.Time, failures);
            if (failures < 1 || delay < 1) throw new InvalidOperationException("retry provider returned an invalid delay");
            return delay;
        }

        private IActivityDefinition ResolveExecutionPlan(string key, EntityId target)
        {
            IActivityDefinition definition = ResolvePlan(key);
            var execution = definition is IPlanDefinition provider ? PlanObservation.Binding(_world, provider, target) : definition;
            if (execution != null && execution.PlanKey != key) throw new CorruptStateException("bound execution has a different authored plan key");
            return execution;
        }

        internal WorkEffectResult RunEffect(string id, Order order, IActivityNodeDefinition node)
        {
            if (id == null) return WorkEffectResult.Applied;
            Code binding = ResponseWork.CheckActiveOrder(_world, _world.Entities.Get(order.Target), order);
            if (!binding.IsNone) return WorkEffectResult.Failed(binding);
            return WorkEffectContracts.Apply(_world, _world.Content.Get<IWorkEffect>(id), order, node);
        }

        internal bool DeferCompletion(WorkEffectResult result, int failures, out int next, out long deadline)
        {
            next = 0; deadline = 0;
            if (result.Status != WorkEffectStatus.Pending || !RetryPolicy.CanRetry(result.Blocker)) return false;
            next = checked(failures + 1); deadline = checked(_world.Tick + RetryDelay(next));
            return true;
        }

        internal Code PreviewEffect(string id, Order order, IActivityNodeDefinition node)
        {
            Code binding = ResponseWork.CheckActiveOrder(_world, _world.Entities.Get(order.Target), order);
            if (!binding.IsNone) return binding;
            if (id == null) return Code.None;
            IWorkEffect effect = _world.Content.Get<IWorkEffect>(id);
            return WorkEffectContracts.Preview(_world, effect, order, node);
        }

        public void ValidatePlan(IActivityDefinition plan)
        {
            if (plan == null || string.IsNullOrWhiteSpace(plan.PlanKey) || plan.Graph == null) throw new ContentException("an activity source must return a stable key and compiled graph");
            for (int i = 0; i < plan.Graph.Nodes.Count; i++)
            {
                var nodeExecutor = plan.Graph.Nodes[i].Executor;
                if (nodeExecutor == null || !ReferenceEquals(_world.Content.Find<IActivityExecutor>(nodeExecutor.Id), nodeExecutor))
                    throw new ContentException(plan.PlanKey + ": executor must alias selected authored content");
                ActivityExecutorContracts.Read(_world, nodeExecutor, null, null, () => {
                    nodeExecutor.ValidateDefinition(plan.Graph.Nodes[i]); return true;
                });
                if (plan.AutomaticSupply && HasSupplyRequirement(plan.Graph.Nodes[i].Requirement) && !_world.Services.Has<IWorkSupplyRequests>())
                    throw new ContentException(plan.PlanKey + ": automatic supply requires IWorkSupplyRequests");
                try { _world.Time.Ticks(plan.Graph.Nodes[i].Duration); }
                catch (OverflowException) { throw new ContentException(plan.PlanKey + ": duration exceeds this session's tick representation"); }
                foreach (WorkerRoleId role in plan.Graph.Nodes[i].Roles) _world.Content.Get<IWorkerRoleDefinition>(role.Value);
                IWorkRequirement requirement = plan.Graph.Nodes[i].Requirement;
                if (requirement != null && !ReferenceEquals(_world.Content.Find<IWorkRequirement>(requirement.Id), requirement))
                    throw new ContentException(plan.PlanKey + ": requirement must be an authored definition in this content set");
                if (plan.Graph.Nodes[i].Roles.Count == 0 && HasWorkerRequirement(requirement))
                    throw new ContentException(plan.PlanKey + ": worker-scoped requirements require declared node roles");
                if (plan.Graph.Nodes[i].Duration.IsZero && (HasResourceRequirement(requirement) || HasCostRequirement(requirement)))
                    throw new ContentException(plan.PlanKey + ": resource/tool requirements require positive physical work time");
                ValidateScaledRequirements(requirement, plan.Graph.Nodes[i].RequirementMultiplier);
                ValidateEffect(plan.Graph.Nodes[i].Effect, plan, plan.Graph.Nodes[i]);
            }
            ValidateEffect(plan.OnComplete, plan, null);
        }

        private void ValidateEffect(string id, IActivityDefinition plan, IActivityNodeDefinition node)
        {
            if (id == null) return;
            IWorkEffect effect = _world.Content.Get<IWorkEffect>(id);
            WorkEffectContracts.ValidatePlan(_world, effect, plan, node);
        }

        private static bool HasWorkerRequirement(IWorkRequirement root)
            => HasRequirement<IWorkerWorkRequirement>(root);

        private void ValidateRequirementDefinition(IWorkRequirement requirement)
        {
            if (!_world.Content.Owns(requirement) || !ReferenceEquals(_world.Content.Find<IWorkRequirement>(requirement.Id), requirement))
                throw new ContentException("work requirement must alias actual owned content");
            var expected = WorkRequirementContracts.Dependencies(requirement);
            var actual = requirement.Dependencies;
            if (actual == null || actual.Count != expected.Count) throw new ContentException(requirement.Id + ": requirement dependencies must match group entries");
            for (int i = 0; i < expected.Count; i++)
                if (actual[i] != expected[i]) throw new ContentException(requirement.Id + ": requirement dependencies must match actual entries");
            if (requirement is IPriorActivityWorkRequirement prior &&
                (requirement is IWorkRequirementGroup || !IsActivity(prior.Plan)))
                throw new ContentException(requirement.Id + ": prior completion requires an explicit selected plan and distinct leaf contract");
            if (requirement is IWorkRequirementBindingValidator binding) binding.ValidateBinding(_world);
            if (requirement is IResourceWorkRequirement && requirement is IWorkRequirementGroup)
                throw new ContentException(requirement.Id + ": a resource requirement is a leaf, not a group");
            if (requirement is IResourceWorkRequirement resource) ValidateResourceDefinition(resource);
            if (!(requirement is IWorkRequirementGroup group)) return;
            if (group.Mode != WorkRequirementMode.All && group.Mode != WorkRequirementMode.Any ||
                group.Entries == null || group.Entries.Count == 0)
                throw new ContentException(requirement.Id + ": invalid requirement group mode/entries");
            for (int i = 0; i < group.Entries.Count; i++)
            {
                WorkRequirementEntry entry = group.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || entry.Requirement == null ||
                    !ReferenceEquals(_world.Content.Find<IWorkRequirement>(entry.Requirement.Id), entry.Requirement))
                    throw new ContentException(requirement.Id + ": entry must reference an authored requirement");
                if (i > 0 && string.CompareOrdinal(group.Entries[i - 1].Id, entry.Id) >= 0)
                    throw new ContentException(requirement.Id + ": entry identities must be unique and canonical");
            }
        }

        /// <summary>One nonmutating iterative traversal for leaves, expressions, execution and diagnostics.</summary>
        internal WorkRequirementResult EvaluateRequirement(IWorkRequirement requirement, PlayerId owner, Entity target, IActivityNodeDefinition node)
            => EvaluateRequirement(requirement, new WorkRequirementContext(_world, owner, target, node), null);

        internal WorkRequirementResult EvaluateRequirement(IWorkRequirement requirement, WorkRequirementContext context,
            Action<string, IReadOnlyList<string>, WorkRequirementResult> observe)
        {
            _world.RequireHealthyUpdate();
            if (requirement == null) return WorkRequirementResult.Satisfied();
            return EntityObservation.ReadProvider(_world, requirement, context.Target,
                () => context.Worker == null ? EvaluateRequirementCore(requirement, context, observe)
                    : EntityObservation.ReadProvider(_world, context.Worker, context.Worker,
                        () => EvaluateRequirementCore(requirement, context, observe), observeTargetState: true),
                space: _world.Space, observeTargetState: true);
        }

        private WorkRequirementResult EvaluateRequirementCore(IWorkRequirement requirement, WorkRequirementContext context,
            Action<string, IReadOnlyList<string>, WorkRequirementResult> observe)
        {
            if (requirement == null) return WorkRequirementResult.Satisfied();
            if (!ReferenceEquals(context.World, _world) ||
                !ReferenceEquals(_world.Content.Find<IWorkRequirement>(requirement.Id), requirement))
                throw new InvalidOperationException("requirement evaluation needs this session's authored provider");
            var stack = new List<RequirementFrame> { new RequirementFrame(requirement,
                Array.AsReadOnly(new[] { requirement.Id }), context.Choices) };
            while (stack.Count > 0)
            {
                RequirementFrame frame = stack[stack.Count - 1];
                if (frame.Group != null && frame.Next < frame.Group.Entries.Count)
                {
                    WorkRequirementEntry entry = frame.Group.Entries[frame.Next++];
                    var segments = new string[frame.Path.Count + 1];
                    for (int i = 0; i < frame.Path.Count; i++) segments[i] = frame.Path[i];
                    segments[segments.Length - 1] = entry.Id;
                    stack.Add(new RequirementFrame(entry.Requirement, Array.AsReadOnly(segments), context.Choices));
                    continue;
                }
                WorkRequirementContext current = frame.Requirement is IResourceWorkRequirement
                    ? context.WithSource(Binding(context.Execution, frame.Path)?.Choice.Endpoint) : context;
                WorkRequirementResult result = frame.Group == null ? frame.Requirement is IWorkerWorkRequirement && current.Worker == null
                    ? WorkRequirementResult.Pending(WorkSystem.BlockerNoWorker) : frame.Requirement.Evaluate(current) : frame.Result();
                if (!result.IsWellFormed || frame.Group == null && (result.SelectedEntry != null || result.Choices.Count != 0 || result.Resources.Count != 0 || result.ResourceCandidates.Count != 0 || result.Costs.Count != 0))
                    throw new InvalidOperationException("requirement '" + frame.Requirement.Id + "' returned an invalid result");
                if (frame.Group == null && result.Status == WorkRequirementStatus.Satisfied && frame.Requirement is IResourceWorkRequirement resource)
                    result = EvaluateResource(resource, frame.Path, current, result);
                if (frame.Group == null && result.Status == WorkRequirementStatus.Satisfied && frame.Requirement is IWorkCostRequirement cost)
                    result = EvaluateCost(cost, frame.Path, context, result);
                if (frame.Group != null && result.Status == WorkRequirementStatus.Satisfied)
                { result = CheckResourceSet(result, context); if (result.Status == WorkRequirementStatus.Satisfied) result = CheckCostSet(result, context); }
                observe?.Invoke(frame.Requirement.Id, frame.Path, result);
                stack.RemoveAt(stack.Count - 1);
                if (stack.Count == 0) return result;
                RequirementFrame parent = stack[stack.Count - 1];
                parent.Accept(result, parent.Group.Entries[parent.Next - 1].Id);
            }
            throw new InvalidOperationException("empty requirement evaluation");
        }

        private sealed class RequirementFrame
        {
            public readonly IWorkRequirement Requirement;
            public readonly IWorkRequirementGroup Group;
            public readonly IReadOnlyList<string> Path;
            public int Next;
            private WorkRequirementResult? _invalid, _blocked, _pending;
            private string _selected;
            private WorkRequirementResult _selectedResult;
            private WorkRequirementResult? _committedResult;
            private readonly string _committed;
            private readonly List<WorkRequirementChoice> _allChoices = new List<WorkRequirementChoice>();
            private readonly List<WorkResourceChoice> _allResources = new List<WorkResourceChoice>();
            private readonly List<WorkResourceCandidate> _allResourceCandidates = new List<WorkResourceCandidate>();
            private readonly List<WorkCostChoice> _allCosts = new List<WorkCostChoice>();
            public RequirementFrame(IWorkRequirement requirement, IReadOnlyList<string> path, IReadOnlyList<WorkRequirementChoice> choices)
            {
                Requirement = requirement; Group = requirement as IWorkRequirementGroup; Path = path;
                if (choices != null) foreach (var choice in choices) if (choice.At(path)) { _committed = choice.Entry; break; }
            }
            public void Accept(WorkRequirementResult result, string entry)
            {
                if (entry == _committed) _committedResult = result;
                _allResources.AddRange(result.Resources); _allResourceCandidates.AddRange(result.ResourceCandidates); _allCosts.AddRange(result.Costs);
                if (result.Status == WorkRequirementStatus.Satisfied) _allChoices.AddRange(result.Choices);
                switch (result.Status)
                {
                    case WorkRequirementStatus.Invalid: if (!_invalid.HasValue) _invalid = result; break;
                    case WorkRequirementStatus.Blocked: if (!_blocked.HasValue) _blocked = result; break;
                    case WorkRequirementStatus.Pending: if (!_pending.HasValue) _pending = result; break;
                    case WorkRequirementStatus.Satisfied: if (_selected == null) { _selected = entry; _selectedResult = result; } break;
                }
            }
            public WorkRequirementResult Result()
            {
                if (_invalid.HasValue) return WorkRequirementResult.Invalid(_invalid.Value.Blocker);
                if (Group.Mode == WorkRequirementMode.Any)
                {
                    if (_committed != null)
                    {
                        if (!_committedResult.HasValue) throw new InvalidOperationException("committed requirement entry is absent");
                        if (_committedResult.Value.Status != WorkRequirementStatus.Satisfied) return _committedResult.Value.WithChoices(null);
                        _selected = _committed; _selectedResult = _committedResult.Value;
                    }
                    if (_selected != null)
                    {
                        var choices = new List<WorkRequirementChoice> { new WorkRequirementChoice(Path, _selected) };
                        choices.AddRange(_selectedResult.Choices);
                        return WorkRequirementResult.Satisfied().Select(_selected).WithChoices(choices.AsReadOnly())
                            .WithResources(_selectedResult.Resources, _selectedResult.ResourceCandidates).WithCosts(_selectedResult.Costs);
                    }
                    if (_pending.HasValue) return WorkRequirementResult.Pending(_pending.Value.Blocker)
                        .WithResources(_pending.Value.Resources, _pending.Value.ResourceCandidates).WithCosts(_pending.Value.Costs);
                    if (_blocked.HasValue) return WorkRequirementResult.Blocked(_blocked.Value.Blocker)
                        .WithResources(_blocked.Value.Resources, _blocked.Value.ResourceCandidates).WithCosts(_blocked.Value.Costs);
                }
                else
                {
                    if (_blocked.HasValue) return WorkRequirementResult.Blocked(_blocked.Value.Blocker)
                        .WithResources(_allResources.AsReadOnly(), _allResourceCandidates.AsReadOnly()).WithCosts(_allCosts.AsReadOnly());
                    if (_pending.HasValue) return WorkRequirementResult.Pending(_pending.Value.Blocker)
                        .WithResources(_allResources.AsReadOnly(), _allResourceCandidates.AsReadOnly()).WithCosts(_allCosts.AsReadOnly());
                    return WorkRequirementResult.Satisfied().WithChoices(_allChoices.AsReadOnly())
                        .WithResources(_allResources.AsReadOnly(), _allResourceCandidates.AsReadOnly()).WithCosts(_allCosts.AsReadOnly());
                }
                throw new InvalidOperationException("invalid empty requirement expression");
            }
        }

        internal WorkRequirementResult EvaluateRequirements(PlayerId owner, Entity target, IActivityNodeDefinition node)
            => EvaluateRequirements(owner, target, node, null, null);

        internal WorkRequirementResult EvaluateRequirements(PlayerId owner, Entity target, IActivityNodeDefinition node,
            ActivityNodeExecution execution, Action<string, IReadOnlyList<string>, WorkRequirementResult> observe)
        {
            Job job = Job(execution?.JobId ?? 0); Entity worker = _world.Entities.Get(job?.Worker ?? EntityId.None);
            WorkerRoleId role = worker == null ? WorkerRoleId.None : job.Role;
            WorkRequirementResult? unavailable = null;
            if (worker != null && job?.IsLive == true)
            {
                Code refusal = _world.Services.Get<IWorkerDispatch>().CheckJob(_world, worker, job);
                if (!refusal.IsNone) unavailable = WorkPolicyCodes.IsWait(refusal) ? WorkRequirementResult.Blocked(refusal) : WorkRequirementResult.Pending(refusal);
            }
            IReadOnlyList<WorkRequirementChoice> choices = execution != null &&
                (execution.State == NodeState.Working || execution.State == NodeState.Paused || execution.State == NodeState.Done || execution.State == NodeState.Completing || HasSpent(execution))
                ? execution.Choices : null;
            if (worker == null && (Selection(execution).WorkerFor(node.Id).IsValid || HasWorkerRequirement(node.Requirement) || node.Roles.Count > 0 && HasRunningResourceRequirement(node.Requirement)))
            {
                var candidates = new List<WorkWorkerCandidate>();
                _world.Services.Get<IWorkerDispatch>().CopyCandidates(_world, owner, node.Roles, target, node, job?.Id ?? 0, choices, execution, WorkCandidateMode.IdleOnly, candidates, null);
                foreach (var candidate in candidates)
                    if (candidate.Result.Status == WorkRequirementStatus.Satisfied) { worker = _world.Entities.Get(candidate.Worker); role = candidate.Role; break; }
                if (worker == null && candidates.Count > 0)
                { worker = _world.Entities.Get(candidates[0].Worker); role = candidates[0].Role; unavailable = candidates[0].Result; }
            }
            Action<string, IReadOnlyList<string>, WorkRequirementResult> project = observe == null ? null :
                (Action<string, IReadOnlyList<string>, WorkRequirementResult>)((id, path, result) => observe(id, path,
                    path.Count == 1 && unavailable.HasValue ? unavailable.Value : result));
            WorkRequirementResult evaluated = EvaluateRequirement(node.Requirement,
                new WorkRequirementContext(_world, owner, target, node, worker, choices, execution, role), project);
            return unavailable ?? evaluated;
        }

        /// <summary>Places an order. The plan's nodes become jobs as they come due.</summary>
        public Order Post(IActivityDefinition plan, PlayerId owner, EntityId target, int priority, ActivityWorkerSelection workerSelection = null)
        {
            _world.RequireStateMutation();
            if (priority < 0) throw new ArgumentOutOfRangeException(nameof(priority));
            if (plan == null || !ReferenceEquals(ResolveExecutionPlan(plan.PlanKey, target), plan))
                throw new InvalidOperationException("posting requires the actual declared or target-bound activity definition");
            ValidatePlan(plan);
            var selection = workerSelection ?? ActivityWorkerSelection.Unbound;
            Code refusal = selection.Check(_world, owner, plan.Graph);
            if (!refusal.IsNone) throw new InvalidOperationException("selected activity workers refused: " + refusal);
            ActivityStartRules.Commit(_world, plan, owner, _world.Entities.Get(target));

            var o = new Order
            {
                Id = _nextOrder++, PlanKey = plan.PlanKey, Plan = plan, Owner = owner, Target = target, Priority = priority,
                CreatedTick = _world.Tick, WorkerSelection = selection
            };
            o.BindNodes();
            _orders.Add(o);
            _orderById.Add(o.Id, o);
            Dirty = true;
            return o;
        }

        /// <summary>The active order of this plan on this target, if any.</summary>
        public Order ActiveOn(EntityId target, string planKey)
        {
            for (int i = 0; i < _orders.Count; i++)
                if (_orders[i].IsActive && _orders[i].Target == target && _orders[i].PlanKey == planKey) return _orders[i];
            return null;
        }

        /// <summary>Response execution identity is target plus named provider, not a possibly shared plan.</summary>
        public Order ActiveResponseOn(EntityId target, Code source)
        {
            if (source.IsNone) return null;
            foreach (Order order in _orders)
                if (order.IsActive && order.Target == target && order.ResponseSource == source) return order;
            return null;
        }

        /// <summary>Latest native response record for an exact episode, including terminal history.</summary>
        public Order ResponseOn(EntityId target, Code source, int episode)
        {
            if (source.IsNone || episode < 1) return null;
            for (int i = _orders.Count - 1; i >= 0; i--)
                if (_orders[i].Target == target && _orders[i].ResponseSource == source && _orders[i].ResponseEpisode == episode)
                    return _orders[i];
            return null;
        }

        /// <summary>Resume cancelled scheduling, never reset real work, spent inputs or completed effects.</summary>
        internal void ResumeResponse(Order order)
        {
            if (!ReferenceEquals(Order(order.Id), order) || order.State != OrderState.Cancelled ||
                !ResponseWork.CheckActiveOrder(_world, _world.Entities.Get(order.Target), order).IsNone)
                throw new InvalidOperationException("only the current cancelled response can resume");
            order.State = OrderState.Active; order.Blocker = Code.None;
            foreach (ActivityNodeExecution node in order.Nodes)
                if (node.State != NodeState.Done)
                { node.State = NodeState.Waiting; node.Blocker = Code.None; }
            Dirty = true;
        }

        public bool HasActiveOn(EntityId target)
        {
            for (int i = 0; i < _orders.Count; i++) if (_orders[i].IsActive && _orders[i].Target == target) return true;
            return false;
        }

        internal int NextJobId => _nextJob;

        internal void Assign(Job job, IWorker worker, WorkerRoleId role)
        {
            if (!ReferenceEquals(Job(job.Id), job) || job.State != JobState.Open || worker.Job != 0 || job.RetryAt > _world.Tick)
                throw new InvalidOperationException("job or worker already committed");
            if (!CheckWorkerSelection(job, worker.Entity, role).IsNone)
                throw new InvalidOperationException("job cannot acquire a different selected machine");
            // Acquired work takes motion ownership before the movement phase. An idle home
            // goal must not earn one more step while the new job awaits its first toil.
            MotionExecutionContracts.Stop(_world, worker.Entity);
            worker.Job = job.Id; job.Worker = worker.Entity.Id; job.Role = role; job.State = JobState.Assigned; job.ToilIndex = 0;
            // Assignment consumes the elapsed backoff. Reaching the site may then reset failures
            // without leaving an invalid zero-failure/nonzero-deadline checkpoint behind.
            job.RetryAt = 0;
        }

        internal bool Assign(Job job, WorkWorkerCandidate candidate, Code? selectedDock, IReadOnlyList<WorkResourceChoice> selectedResources)
            => Assign(new[] { job.Id }, new[] { candidate }, new[] { selectedDock },
                new IReadOnlyList<WorkResourceChoice>[] { selectedResources }, Array.Empty<(int Order, string Node)>());

        public Job AddJob(PlayerId owner, WorkerRoleId role, int priority, EntityId target)
        {
            if (priority < 0) throw new ArgumentOutOfRangeException(nameof(priority));
            if (!role.IsValid) throw new ArgumentException("a machine job needs a worker role", nameof(role));
            _world.Content.Get<IWorkerRoleDefinition>(role.Value);
            var j = new Job { Id = _nextJob++, Owner = owner, Role = role, Priority = priority, Target = target, CreatedTick = _world.Tick };
            _jobs.Add(j);
            _jobById.Add(j.Id, j);
            Dirty = true;
            return j;
        }

        public bool RetainsOrder(Order order)
        {
            _world.RequireHealthyUpdate();
            if (_readingOrderRetention)
            {
                var error = new InvalidOperationException("work order retention must not reenter its cleanup decision");
                _world.LatchFault("observation.provider", error); throw error;
            }
            if (order == null || !ReferenceEquals(Order(order.Id), order)) return false;
            // The original requester remains the disclosure/classification authority until
            // its acquired shipment ends. Cancellation withdraws demand, not that live reference.
            foreach (Job job in _jobs)
                if (job.IsLive && job.IsHaul && job.HaulDemand.Scope == DemandScope &&
                    job.HaulDemand.Identity == order.Id && job.HaulDemand.Entry.IsNone &&
                    job.Owner == order.Owner && job.To == order.Target) return true;
            Entity target = _world.Entities.Get(order.Target);
            if (target == null) return false;
            var retention = target.Get<IWorkOrderRetention>();
            var response = order.ResponseSource.IsNone ? null : target.Get<IIncidentResponse>(order.ResponseSource);
            if (retention == null && response == null) return false;
            _readingOrderRetention = true;
            try
            {
                return EntityObservation.ReadProvider(_world, (object)retention ?? response, target, () =>
                {
                    // Reuse the actual order codec, not a copied cleanup/progress model.
                    var original = EntityObservation.CaptureProvider(_world, order, sync => SyncOrder(sync, order));
                    bool retained = retention?.RetainsOrder(order) == true;
                    if (!retained && response != null)
                    {
                        // Named incident history uses its episode, not the singleton disposition capability.
                        if (response is IPhasedIncidentResponse phased)
                            retained = IncidentPhases.ReadLifecycle(_world, phased, out _).Episode == order.ResponseEpisode;
                        else
                        {
                            IncidentSnapshot snapshot = ResponseWork.Read(response);
                            retained = snapshot.Active && snapshot.Episode == order.ResponseEpisode;
                        }
                    }
                    original.Verify();
                    return retained;
                }, space: _world.Space, observeTargetState: true);
            }
            finally { _readingOrderRetention = false; }
        }
        private bool _readingOrderRetention;

        /// <summary>Drops finished jobs and resolved orders; pending target disposition keeps its existing owner.</summary>
        internal void Prune()
        {
            for (int i = _jobs.Count - 1; i >= 0; i--)
                if (!_jobs[i].IsLive) { _jobById.Remove(_jobs[i].Id); _jobs.RemoveAt(i); }
            for (int i = _orders.Count - 1; i >= 0; i--)
                if (!_orders[i].IsActive && !RetainsOrder(_orders[i]))
                { CompactCompletion(_orders[i]); _orderById.Remove(_orders[i].Id); _orders.RemoveAt(i); }
        }

        private static Exception HaulError(bool restoring, string message)
            => restoring ? (Exception)new CorruptStateException(message) : new InvalidOperationException(message);

        internal LogisticsDemandSourceCheck CheckHaulSource(Job job)
        {
            if (!job.HaulDemand.IsValid) return LogisticsDemandSourceCheck.Active(false);
            return !_world.Services.TryGet(out ILogisticsDemandSources sources)
                ? LogisticsDemandSourceCheck.Invalid(LogisticsDemandCodes.UnknownOwner) : sources.InspectSource(job.HaulDemand, job.Owner, job.To);
        }
        internal bool IsEmergency(Job job)
        {
            if (job.IsHaul)
            {
                LogisticsDemandSourceCheck check = CheckHaulSource(job);
                if (check.Status == LogisticsDemandStatus.Invalid) throw new InvalidOperationException("invalid haul requester: " + check.Refusal);
                return check.Emergency;
            }
            return IsEmergency(Order(job.OrderId));
        }
        internal bool IsEmergency(Order order)
        {
            return order?.IsActive == true && !order.ResponseSource.IsNone &&
                ResponseWork.CheckActiveOrder(_world, _world.Entities.Get(order.Target), order).IsNone;
        }

        /// <summary>One validator over existing toils, loaded quantity, worker relation and owned receipt phase.</summary>
        internal void ValidateHaul(Job job, bool restoring)
        {
            ValidateDocks(job, restoring);
            if (!LogisticsService.ValidSelection(_world.Content, job.Resource, job.HaulSelection) || job.Purchase && !job.HaulSelection.IsNone)
                throw HaulError(restoring, "invalid actual haul quality selection");
            if (!ReferenceEquals(Job(job.Id), job) || !job.IsHaul || !job.Owner.IsValid ||
                !job.Resource.IsValid || _world.Content.Resource(job.Resource) == null || !job.Amount.IsPositive ||
                !job.To.IsValid || job.Target != job.To || job.OrderId != 0 || job.NodeId != null ||
                job.Loaded < Fix.Zero || job.Loaded > job.Amount ||
                job.UnloadingBaseline < Fix.Zero || job.UnloadingBaseline >= job.Amount || job.UnloadingBaseline > job.Amount - job.Loaded ||
                !job.UnloadingBaseline.IsZero && (!job.RecoveryKind.HasValue || HaulHandling.Phase(job) != ResourceHandlingPhase.Unloading) ||
                UnitConversion.ValidateAmount(_world.Content.Resource(job.Resource).QuantityUnit, job.UnloadingBaseline) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(_world.Content.Resource(job.Resource).QuantityUnit, job.Loaded) != UnitConversionStatus.Converted)
                throw HaulError(restoring, "invalid physical haul identity");
            if (job.HaulDemand.IsValid && (job.Route != 0 || !job.From.IsValid && job.RecoveryKind != LogisticsRecoveryKind.CargoReturn ||
                CheckHaulSource(job).Status == LogisticsDemandStatus.Invalid))
                throw HaulError(restoring, "invalid haul requester provenance");
            bool returning = !job.From.IsValid;
            if (!job.SourceEndpoint.IsValid || !job.DestinationEndpoint.IsValid ||
                job.DestinationPort.IsNone && job.DestinationEndpoint != ResourceEndpointId.ForEntity(job.To) ||
                !returning && job.SourcePort.IsNone && job.SourceEndpoint != ResourceEndpointId.ForEntity(job.From))
                throw HaulError(restoring, "haul lost its original physical binding");
            if (job.RecoveryKind.HasValue && ((byte)job.RecoveryKind.Value > 1 || job.Purchase || job.Route != 0 ||
                (job.RecoveryKind.Value == LogisticsRecoveryKind.CargoReturn) != returning ||
                !_world.Services.TryGet<ILogisticsRecoveryPolicy>(out _)))
                throw HaulError(restoring, "recovery provenance differs from its actual hauling intent");
            int count = returning ? 3 : 5;
            if (job.Toils.Count != count || job.ToilIndex < 0 || job.ToilIndex > count ||
                returning && job.Purchase || job.SourceEndpoint == job.DestinationEndpoint)
                throw HaulError(restoring, "invalid physical haul itinerary");
            for (int i = 0; i < count; i++)
            {
                Toil toil = job.Toils[i];
                Code expected = i == count - 1 ? WorkPhases.Departure : i % 2 == 0 ? WorkPhases.Travel : !returning && i == 1 ? WorkPhases.Pickup : WorkPhases.Delivery;
                EntityId endpoint = !returning && i < 2 ? job.From : job.To;
                if (toil.Executor != expected || toil.At != endpoint ||
                    (expected == WorkPhases.Pickup || expected == WorkPhases.Delivery) && (toil.Resource != job.Resource || toil.Amount != job.Amount) ||
                    (expected == WorkPhases.Travel || expected == WorkPhases.Departure) && (toil.Resource.IsValid || !toil.Amount.IsZero))
                    throw HaulError(restoring, "haul toil differs from its exact intention");
            }
            if (!job.IsLive)
            {
                if (job.TripHeld || job.RetryAt != 0 || !job.RecoveryCause.IsNone || !job.RecoveryDockSlot.IsNone || !job.UnloadingBaseline.IsZero || !job.HandlingProgress.Equals(WorkProgress.Zero) || job.Worker.IsValid || job.OutReservation.IsValid || job.InReservation.IsValid ||
                    job.CargoInReservation.IsValid || job.CargoOutReservation.IsValid ||
                    job.State == JobState.Done && (job.ToilIndex != count || !job.Loaded.IsZero))
                    throw HaulError(restoring, "ended haul still owns live commitments");
                return;
            }
            bool pickup = !returning && job.ToilIndex < 2;
            bool departing = job.ToilIndex == count - 1;
            bool recoveryDeparture = !job.RecoveryCause.IsNone;
            bool missingDestinationWait = !recoveryDeparture && job.Blocker == HaulRecoveryCodes.DestinationUnavailable;
            Code destinationBinding = missingDestinationWait
                ? _world.Services.Get<IHaulExecution>().CheckDestinationBinding(job) : Code.None;
            if (missingDestinationWait && (job.State != JobState.Paused || !job.RecoveryKind.HasValue ||
                destinationBinding.IsNone || pickup || departing ||
                !_world.Services.TryGet<ILogisticsRecoveryPolicy>(out var missingPolicy) ||
                !LogisticsRecoveryContracts.Read(_world, missingPolicy, job.RecoveryKind.Value).Enabled ||
                !System.Linq.Enumerable.Contains(LogisticsRecoveryContracts.Read(_world, missingPolicy, job.RecoveryKind.Value).RerouteBlockers, destinationBinding)))
                throw HaulError(restoring, "unavailable destination wait has no actual loaded recovery and authored binding-loss permission");
            if (recoveryDeparture && (!departing || !job.RecoveryKind.HasValue ||
                !_world.Services.TryGet<ILogisticsRecoveryPolicy>(out var recoveryPolicy) ||
                !LogisticsRecoveryContracts.Read(_world, recoveryPolicy, job.RecoveryKind.Value).Enabled ||
                !System.Linq.Enumerable.Contains(LogisticsRecoveryContracts.Read(_world, recoveryPolicy, job.RecoveryKind.Value).RerouteBlockers, job.RecoveryCause)))
                throw HaulError(restoring, "loaded recovery departure has no actual authored cause");
            if (job.State != JobState.Assigned && job.State != JobState.Paused ||
                job.TripHeld && (job.State != JobState.Paused || job.Blocker.IsNone || job.RetryAt != 0) ||
                job.State == JobState.Paused && !job.TripHeld && !WorkPolicyCodes.IsWait(job.Blocker) && !WorkDockCodes.IsWait(job.Blocker) &&
                    !((recoveryDeparture || missingDestinationWait) && job.Blocker == HaulRecoveryCodes.DestinationUnavailable) &&
                    !(job.RetryAt > 0 && RetryPolicy.CanRetry(job.Blocker)) ||
                job.RetryAt > 0 && (job.State != JobState.Paused || job.Failures < 1 ||
                    job.RetryAt > _world.Tick && job.RetryAt - _world.Tick > RetryDelay(job.Failures)) ||
                job.ToilIndex >= count || !job.Worker.IsValid || (!departing || recoveryDeparture) && !job.InReservation.IsValid ||
                departing && !recoveryDeparture && (!job.Loaded.IsZero || job.OutReservation.IsValid || job.InReservation.IsValid || job.CargoInReservation.IsValid || job.CargoOutReservation.IsValid || !job.HandlingProgress.Equals(WorkProgress.Zero)) ||
                !pickup && (!departing || recoveryDeparture) && (!job.Loaded.IsPositive || job.OutReservation.IsValid || job.CargoInReservation.IsValid || !job.CargoOutReservation.IsValid) ||
                pickup && (job.Loaded == job.Amount || !job.OutReservation.IsValid || !job.CargoInReservation.IsValid || job.Loaded.IsPositive != job.CargoOutReservation.IsValid))
                throw HaulError(restoring, "haul phase differs from its source/cargo/destination commitments");
            Entity carrier = _world.Entities.Get(job.Worker);
            if (carrier != null)
            {
                if (job.TripHeld && carrier.Get<IHaulTripPolicy>() == null)
                    throw HaulError(restoring, "trip hold lost its actual authored policy");
                ResourceHandlingCheck handling = HaulHandling.Check(_world, carrier, job.Resource, HaulHandling.LegAmount(job), HaulHandling.Phase(job));
                if (!handling.Ready || job.HandlingProgress.WholeTicks > handling.RequiredTicks ||
                    job.HandlingProgress.WholeTicks == handling.RequiredTicks && job.HandlingProgress.Fraction != 0)
                    throw HaulError(restoring, "haul handling progress differs from its current authored phase");
                IInventory cargo = carrier.Has<ITransporter>() ? TransporterContracts.ReadCargo(_world, carrier) : null;
                if (carrier.Get<IWorker>()?.Job != job.Id || cargo == null ||
                    job.CargoOutReservation.IsValid && job.CargoOutReservation.Endpoint != cargo.Endpoint ||
                    job.CargoInReservation.IsValid && job.CargoInReservation.Endpoint != cargo.Endpoint)
                    throw HaulError(restoring, "haul selected carrier/cargo relation differs");
                if (!departing && _world.Entities.Get(pickup ? job.From : job.To) != null && HaulHandling.Handled(job) > EarnedHandling(job))
                    throw HaulError(restoring, "physical cargo exceeds actually earned authored handling");
            }
            CheckHaulReceipt(job, job.InReservation, ReservationDirection.Inbound, job.To, false, false, restoring);
            CheckHaulReceipt(job, job.OutReservation, ReservationDirection.Outbound, job.From, job.Purchase, false, restoring);
            CheckHaulReceipt(job, job.CargoInReservation, ReservationDirection.Inbound, default, false, true, restoring);
            CheckHaulReceipt(job, job.CargoOutReservation, ReservationDirection.Outbound, default, false, true, restoring);
        }

        internal Fix EarnedHandling(Job job)
        {
            Entity carrier = _world.Entities.Get(job.Worker);
            ResourceHandlingPhase phase = HaulHandling.Phase(job);
            Entity endpoint = _world.Entities.Get(phase == ResourceHandlingPhase.Loading ? job.From : job.To);
            IInventory cargo = carrier?.Has<ITransporter>() == true ? TransporterContracts.ReadCargo(_world, carrier) : null;
            if (endpoint == null || cargo == null) throw new InvalidOperationException("missing physical handling participant");
            int carrierVersion = carrier.Version, endpointVersion = endpoint.Version;
            CapacityQuantum endpointQuantum;
            if (phase == ResourceHandlingPhase.Loading && job.Purchase)
                endpointQuantum = endpoint.Get<ISupplier>().SupplyQuantum(job.Resource);
            else
            {
                IResourceInlet inlet = _world.Services.Get<IHaulExecution>().ReadHaulEndpoint(job, phase == ResourceHandlingPhase.Loading).Store;
                Code name = phase == ResourceHandlingPhase.Loading ? job.SourcePort : job.DestinationPort;
                // The exact named definition declares the immutable policy required of every
                // original/current binding. Losing access does not invent a quantity quantum.
                ICapacityPolicy policy = inlet?.CapacityPolicy ?? endpoint.Def.Component<IResourcePortDefinition>(name)?.CapacityPolicy;
                if (policy == null) throw new InvalidOperationException("missing authored original handling capacity policy");
                endpointQuantum = CapacityPolicyContracts.Quantum(policy, job.Resource);
            }
            CapacityQuantum quantum = CapacityQuantum.Combine(endpointQuantum, CapacityPolicyContracts.Quantum(cargo.CapacityPolicy, job.Resource));
            Fix earned = HaulHandling.Earned(_world, carrier, job.Resource, HaulHandling.LegAmount(job), phase, job.HandlingProgress, quantum);
            if (carrier.Version != carrierVersion || endpoint.Version != endpointVersion)
                throw new InvalidOperationException("physical handling prefix mutated a participant");
            return phase == ResourceHandlingPhase.Unloading ? job.UnloadingBaseline + earned : earned;
        }

        internal ResourceHandlingCheck ReadHandling(Job job)
            => job.IsHaul && job.IsLive && job.ToilIndex < job.Toils.Count - 1 ? HaulHandling.Check(_world, _world.Entities.Get(job.Worker), job.Resource, HaulHandling.LegAmount(job), HaulHandling.Phase(job))
                : new ResourceHandlingCheck(ResourceHandlingStatus.InvalidRequest);

        private static Fix HaulReceiptAmount(Job job, ReservationToken token)
        {
            if (token.Equals(job.CargoOutReservation)) return job.Loaded;
            if (token.Equals(job.OutReservation) || token.Equals(job.CargoInReservation)) return job.Amount - job.Loaded;
            if (token.Equals(job.InReservation)) return HaulHandling.Phase(job) == ResourceHandlingPhase.Loading ? job.Amount : job.Loaded;
            throw new InvalidOperationException("receipt does not belong to its current haul");
        }
        private void CheckHaulReceipt(Job job, ReservationToken token, ReservationDirection direction,
            EntityId expected, bool supply, bool cargo, bool restoring)
        {
            if (!token.IsValid) return;
            IReadOnlyList<ResourceReservation> records;
            if (supply || cargo)
            {
                if (token.Endpoint.Scope != ResourceEndpointId.EntityScope || expected.IsValid && token.Endpoint != ResourceEndpointId.ForEntity(expected))
                    throw HaulError(restoring, "haul supplier/cargo receipt differs from its intention");
                Entity entity = _world.Entities.Get(token.Endpoint.ToEntity());
                if (entity == null) return;
                records = supply ? entity.Get<ISupplier>()?.Reservations : entity.Get<IInventory>()?.Reservations;
            }
            else
            {
                ResourceEndpointId original = direction == ReservationDirection.Outbound ? job.SourceEndpoint : job.DestinationEndpoint;
                if (token.Endpoint != original)
                    throw HaulError(restoring, "haul receipt differs from its original endpoint identity");
                if (_world.Entities.Get(expected) != null)
                {
                    IResourceInlet endpoint = _world.Services.Get<IHaulExecution>().ReadHaulEndpoint(job, direction == ReservationDirection.Outbound).Store;
                    if (endpoint != null && (endpoint.Endpoint != token.Endpoint ||
                        endpoint.Company != job.Owner && !(endpoint.Company == PlayerId.None && endpoint.Public)))
                        throw HaulError(restoring, "haul receipt differs from its exact physical port binding");
                }
                // Named endpoint providers may outlive the physical participant. Validate the actual
                // surviving receipt, not the continued existence/access of its former site.
                ResourceClaimSnapshot claims = _world.Services.Get<IResourceTransaction>().ReadClaims(job.Owner, token.Endpoint, job.ReservationOwner);
                if (claims.Status == ResourceTransferStatus.MissingEndpoint) return;
                if (claims.Status != ResourceTransferStatus.Ready)
                    throw HaulError(restoring, "haul receipt's actual endpoint is inaccessible");
                records = claims.Records;
            }
            if (!ResourceReservationBook.Matches(records, token.Endpoint, job.ReservationOwner, token, job.Resource, HaulReceiptAmount(job, token), direction))
                throw HaulError(restoring, "haul receipt differs from its surviving endpoint");
            if (direction == ReservationDirection.Outbound)
                foreach (var record in records) if (record.Token.Equals(token) && record.Selection != job.HaulSelection)
                    throw HaulError(restoring, "haul quality intent differs from its actual outbound commitment");
        }

        internal IActivityWorkerSelection Selection(ActivityNodeExecution node)
        {
            if (node == null) return ActivityWorkerSelection.Unbound;
            Order order = Order(node.OrderId);
            if (order == null || !ReferenceEquals(order.Node(node.Id), node))
                throw new InvalidOperationException("worker selection requires the actual node execution");
            return order.WorkerSelection;
        }
        internal Code CheckWorkerSelection(Job job, Entity worker, WorkerRoleId? proposedRole = null)
        {
            var selection = Order(job.OrderId)?.WorkerSelection;
            EntityId selected = selection?.WorkerFor(job.NodeId) ?? EntityId.None;
            WorkerRoleId role = selection?.RoleFor(job.NodeId) ?? WorkerRoleId.None;
            return (!selected.IsValid || worker?.Id == selected) && (!role.IsValid || role == (proposedRole ?? job.Role))
                ? Code.None : ActivityWorkerSelection.Mismatch;
        }
        private void ValidateWorkerSelection(Order order)
        {
            if (order.WorkerSelection == null) throw new CorruptStateException("missing activity worker selection");
            try { order.WorkerSelection.ValidateGraph(order.Plan.Graph); }
            catch (ContentException error) { throw new CorruptStateException(error.Message); }
            foreach (var binding in order.WorkerSelection.Bindings)
            {
                if (binding.Worker.Value >= _world.Entities.NextIdentity.Value)
                    throw new CorruptStateException("selected worker identity was never allocated");
                var node = order.Node(binding.Node); var job = Job(node.JobId);
                if (node.CompletedBy.IsValid && node.CompletedBy != binding.Worker ||
                    job?.IsLive == true && job.Worker.IsValid && (job.Worker != binding.Worker ||
                    binding.Role.IsValid && job.Role != binding.Role) ||
                    job?.Worker.IsValid == true && binding.Role.IsValid && job.Role != binding.Role)
                    throw new CorruptStateException("selected machine differs from actual assignment or completion");
            }
        }
        private void ValidateCompletedWorker(ActivityNodeExecution node, IActivityNodeDefinition definition)
        {
            Entity actor = _world.Entities.Get(node.CompletedBy);
            bool machine = definition.Roles.Count > 0 && !definition.Duration.IsZero;
            bool complete = node.Progress.Complete(_world.Time.Ticks(definition.Duration));
            bool settled = node.State == NodeState.Completing || node.State == NodeState.Done;
            if (node.CompletedBy.Value < 0 || node.CompletedBy.Value >= _world.Entities.NextIdentity.Value ||
                node.CompletedBy.IsValid && (!machine || !complete || node.State != NodeState.Completing &&
                    node.State != NodeState.Done && node.State != NodeState.Failed && node.State != NodeState.Cancelled) ||
                machine && settled && !node.CompletedBy.IsValid || node.CompletedBy.IsValid && actor != null && actor.Get<IWorker>() == null)
                throw new CorruptStateException("activity completion worker differs from earned machine work");
        }

        internal void ValidateLoadedCommitments()
        {
            ValidatePlanExecutions();
            ValidateRecurringSources();
            ValidateOwnedBudgets();
            foreach (Order order in _orders)
            {
                ValidateWorkerSelection(order);
                ValidateExecutionBinding(order);
                if (order.State < OrderState.Active || order.State > OrderState.Completing)
                    throw new CorruptStateException("saved activity has invalid aggregate state");
                ValidateCompletion(order);
                foreach (ActivityNodeExecution node in order.Nodes)
                {
                    IActivityNodeDefinition definition = order.Plan.Graph.Find(node.Id);
                    long required = _world.Time.Ticks(definition.Duration);
                    ValidateCompletedWorker(node, definition);
                    if (node.ProductiveSteps < 0 || node.ProductiveSteps > _world.Tick ||
                        node.Progress.Equals(WorkProgress.Zero) != (node.ProductiveSteps == 0))
                        throw new CorruptStateException("saved productive exposure differs from actual node work/session time");
                    if (node.Progress.WholeTicks > required || node.Progress.WholeTicks == required && node.Progress.Fraction != 0)
                        throw new CorruptStateException("saved activity progress exceeds its named node duration");
                    if ((node.State == NodeState.Done || node.State == NodeState.Completing) && !node.Progress.Complete(required) ||
                        order.IsActive && (node.State == NodeState.Working || node.State == NodeState.Paused) &&
                        definition.Roles.Count > 0 && node.JobId == 0)
                        throw new CorruptStateException("saved activity node is missing its real work or assigned job");
                    if ((node.State == NodeState.Ready || node.State == NodeState.Working ||
                         node.State == NodeState.Paused || node.State == NodeState.Done || node.State == NodeState.Completing) && !LoadedDependenciesDone(order, definition))
                        throw new CorruptStateException("saved activity runs before its dependencies");
                    if (node.JobId != 0)
                    {
                        Job job = Job(node.JobId);
                        if (!order.IsActive || node.State != NodeState.Working && node.State != NodeState.Paused ||
                            job == null || !job.IsLive || job.OrderId != order.Id || job.NodeId != node.Id ||
                            job.Owner != order.Owner || job.Target != order.Target || !HasRole(definition.Roles, job.Role) || job.IsHaul)
                            throw new CorruptStateException("saved activity node refers to an absent or different job");
                        Entity machine = _world.Entities.Get(job.Worker);
                        if (machine != null && (machine.Owner != order.Owner || machine.Get<IWorker>() == null || !WorkerExecutionContracts.Can(_world, machine, job.Role)))
                            throw new CorruptStateException("saved selected worker has a different owner or role");
                    }
                    Job selectedJob = Job(node.JobId);
                    bool committed = HasSpent(node) || node.State == NodeState.Done || node.State == NodeState.Completing || order.IsActive &&
                        (node.State == NodeState.Working || node.State == NodeState.Paused) &&
                        (definition.Roles.Count == 0 || selectedJob?.Worker.IsValid == true);
                    ValidateChoices(definition.Requirement, node.Choices, committed);
                    ValidateResourceBindings(order, node, definition, selectedJob);
                    ValidateCostBindings(order, node, definition, selectedJob);
                    if (!order.IsActive && node.JobId != 0 || order.State == OrderState.Done && node.State != NodeState.Done ||
                        order.IsActive && (node.State == NodeState.Cancelled || node.State == NodeState.Failed))
                        throw new CorruptStateException("saved activity/node terminal states differ");
                }
            }
            foreach (Job job in _jobs)
            {
                if (job.OrderId == 0)
                { if (job.NodeId != null) throw new CorruptStateException("standalone job names an activity node"); }
                else
                {
                    Order order = Order(job.OrderId);
                    ActivityNodeExecution node = order?.Node(job.NodeId);
                    if (job.NodeId == null || job.IsHaul || job.IsLive && (order == null || !order.IsActive ||
                        node == null || node.JobId != job.Id))
                        throw new CorruptStateException("live job has no matching named activity node");
                }
                if (job.IsHaul) ValidateHaul(job, true);
                else ValidateActivityItinerary(job, true);
                if (!job.IsHaul && (!job.SourcePort.IsNone || !job.DestinationPort.IsNone || job.SourceEndpoint.IsValid || job.DestinationEndpoint.IsValid || job.HaulDemand.IsValid || job.OutReservation.IsValid || job.InReservation.IsValid || job.CargoInReservation.IsValid || job.CargoOutReservation.IsValid))
                    throw new CorruptStateException("non-haul job owns haul receipts");
            }
            ComponentList<IInletPort> ports = _world.Entities.Components<IInletPort>();
            for (int i = 0; i < ports.Count; i++) ValidateOwnedRecords(ports[i].Reservations);
            ComponentList<ISupplier> suppliers = _world.Entities.Components<ISupplier>();
            for (int i = 0; i < suppliers.Count; i++) ValidateOwnedRecords(suppliers[i].Reservations);
            foreach (IResourceEndpointProvider provider in _world.Services.Providers<IResourceEndpointProvider>())
            {
                var identities = new HashSet<ResourceEndpointId>();
                provider.InspectReservations((endpoint, records) =>
                {
                    if (!endpoint.IsValid || endpoint.Scope != provider.EndpointScope || !identities.Add(endpoint) || records == null)
                        throw new CorruptStateException("invalid scoped reservation-book inspection");
                    foreach (var record in records) if (record.Token.Endpoint != endpoint)
                        throw new CorruptStateException("scoped book contains another endpoint's promise");
                    ValidateOwnedRecords(records);
                });
            }
            ComponentList<IWorker> workers = _world.Entities.Components<IWorker>();
            for (int i = 0; i < workers.Count; i++)
            {
                IWorker worker = workers[i];
                if (worker.Job == 0) continue;
                Job job = Job(worker.Job);
                if (job == null || !job.IsLive || job.Worker != worker.Entity.Id ||
                    job.State != JobState.Assigned && job.State != JobState.Paused)
                    throw new CorruptStateException("saved worker refers to an absent or different assignment");
            }
        }
        private static bool HasRole(IReadOnlyList<WorkerRoleId> roles, WorkerRoleId role)
        { foreach (var allowed in roles) if (allowed == role) return true; return false; }
        private static void ValidateChoices(IWorkRequirement root, IReadOnlyList<WorkRequirementChoice> choices, bool committed)
        {
            if (!committed && choices.Count != 0) throw new CorruptStateException("uncommitted node retains selected alternatives");
            if (!committed) return;
            var pending = new Stack<(IWorkRequirement Requirement, IReadOnlyList<string> Path)>();
            if (root != null) pending.Push((root, Array.AsReadOnly(new[] { root.Id })));
            var used = new HashSet<int>();
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (!(current.Requirement is IWorkRequirementGroup group)) continue;
                string selected = null;
                if (group.Mode == WorkRequirementMode.Any)
                {
                    for (int i = 0; i < choices.Count; i++) if (choices[i].At(current.Path))
                    {
                        if (selected != null) throw new CorruptStateException("duplicate committed requirement choice");
                        selected = choices[i].Entry; used.Add(i);
                    }
                    if (selected == null) throw new CorruptStateException("committed node is missing an alternative choice");
                }
                bool found = selected == null;
                foreach (var entry in group.Entries)
                {
                    if (selected != null && selected != entry.Id) continue;
                    found = true;
                    var path = new List<string>(current.Path) { entry.Id };
                    pending.Push((entry.Requirement, path.AsReadOnly()));
                }
                if (!found) throw new CorruptStateException("committed requirement entry no longer exists");
            }
            if (used.Count != choices.Count) throw new CorruptStateException("choice does not belong to the committed requirement path");
        }
        private static bool LoadedDependenciesDone(Order order, IActivityNodeDefinition definition)
        {
            foreach (string dependency in definition.Dependencies)
                if (order.Node(dependency).State != NodeState.Done) return false;
            return true;
        }
        private void ValidateOwnedRecords(IReadOnlyList<ResourceReservation> records)
        {
            foreach (ResourceReservation record in records)
            {
                if (record.Owner.Scope?.StartsWith("sim.work.resources.", StringComparison.Ordinal) == true)
                {
                    Order order = record.Owner.Identity <= int.MaxValue ? Order((int)record.Owner.Identity) : null;
                    bool found = false;
                    if (order != null) foreach (var node in order.Nodes) foreach (var binding in node.Resources)
                        if (!binding.Spent) foreach (var held in binding.Receipts)
                            if (held.Token.Equals(record.Token) && held.Owner.Equals(record.Owner) && held.Resource == record.Resource &&
                                held.Amount == record.Amount && held.Direction == record.Direction) found = true;
                    if (!found) throw new CorruptStateException("orphan or mismatched saved activity resource promise");
                    continue;
                }
                if (record.Owner.Scope != "sim.haul") continue; // Other module responsibilities retain their own promises.
                Job job = record.Owner.Identity <= int.MaxValue ? Job((int)record.Owner.Identity) : null;
                bool reference = job != null && (record.Direction == ReservationDirection.Inbound
                    ? record.Token.Equals(job.InReservation) || record.Token.Equals(job.CargoInReservation)
                    : record.Token.Equals(job.OutReservation) || record.Token.Equals(job.CargoOutReservation));
                if (!reference || !job.IsHaul || !job.IsLive || job.Resource != record.Resource || HaulReceiptAmount(job, record.Token) != record.Amount)
                    throw new CorruptStateException("orphan or mismatched saved haul commitment");
            }
        }

        private void ValidateCompletionRetry(int failures, long deadline, Code blocker, bool completing)
        {
            if (failures < 0 || deadline < 0 || completing != (failures > 0 && deadline > 0) ||
                !completing && (failures != 0 || deadline != 0) ||
                completing && (!RetryPolicy.CanRetry(blocker) || deadline > _world.Tick && deadline - _world.Tick > RetryDelay(failures)))
                throw new CorruptStateException("completion retry differs from its authored policy/current state");
        }

        private void ValidateCompletion(Order order, bool observation = false)
        {
            ValidateCompletionTime(order);
            if (order.State < OrderState.Active || order.State > OrderState.Completing)
                throw new CorruptStateException("invalid completion aggregate state");
            // A purity fingerprint can run inside the sole effect evaluator before a retry exists.
            // Normal hashes/writes/loads still require a fully settled completion checkpoint.
            if (!(observation && order.State == OrderState.Completing && order.CompletionFailures == 0 && order.CompletionRetryAt == 0))
                ValidateCompletionRetry(order.CompletionFailures, order.CompletionRetryAt, order.Blocker, order.State == OrderState.Completing);
            if ((order.State == OrderState.Completing || order.CompletionEffectApplied) && order.NodesDone != order.Nodes.Count ||
                order.State == OrderState.Active && order.CompletionEffectApplied ||
                order.State == OrderState.Done && !order.CompletionEffectApplied)
                throw new CorruptStateException("order completion phase differs from its actual completed graph");
            foreach (ActivityNodeExecution node in order.Nodes)
            {
                if (node.State < NodeState.Waiting || node.State > NodeState.Completing)
                    throw new CorruptStateException("invalid completion node state");
                if (!(observation && node.State == NodeState.Completing && node.CompletionFailures == 0 && node.CompletionRetryAt == 0))
                    ValidateCompletionRetry(node.CompletionFailures, node.CompletionRetryAt, node.Blocker, node.State == NodeState.Completing);
                IActivityNodeDefinition definition = order.Plan.Graph.Find(node.Id);
                if ((node.State == NodeState.Done || node.State == NodeState.Completing) &&
                    (!node.Progress.Complete(_world.Time.Ticks(definition.Duration)) || !LoadedDependenciesDone(order, definition) || node.JobId != 0))
                    throw new CorruptStateException("completed node differs from its earned graph work/job lifetime");
                if (node.State == NodeState.Completing && (!order.IsActive || node.JobId != 0 ||
                    definition.Effect == null))
                    throw new CorruptStateException("pending node effect has no completed physical work/dependencies");
            }
        }

        // Sole native order codec, shared by board persistence and observed execution identity.
        internal void SyncOrder(IStateSync s, Order o)
        {
            if (!s.Reading) ValidateCompletion(o, s is IStateObservationSync observation && observation.IsObservation);
            s.Int(ref o.Id);
            s.String(ref o.PlanKey);
            s.Player(ref o.Owner);
            s.Entity(ref o.Target);
            o.WorkerSelection = ActivityWorkerSelection.Sync(s, o.WorkerSelection);
            o.ExecutionBinding = ActivityExecutionBinding.Sync(s, o.ExecutionBinding);
            s.Enum(ref o.State);
            s.Int(ref o.Priority);
            s.Long(ref o.CreatedTick);
            long completed = o.CompletedTick; s.Long(ref completed);
            if (s.Reading) o.CompletedTick = completed;
            ValidateCompletionTime(o);
            if (o.CreatedTick < 0 || o.CreatedTick > _world.Tick || o.Priority < 0)
                throw new CorruptStateException("order creation tick or priority is invalid");
            string response = o.ResponseSource.Id; s.String(ref response);
            if (s.Reading) o.ResponseSource = response == null ? Code.None : new Code(response);
            s.Int(ref o.ResponseEpisode);
            if (s.Reading && (o.ResponseEpisode < 0 || o.ResponseSource.IsNone != (o.ResponseEpisode == 0)))
                throw new CorruptStateException("response order has invalid source/episode identity");
            string blocker = o.Blocker.Id; s.String(ref blocker); o.Blocker = blocker == null ? Code.None : new Code(blocker);
            int failures = o.CompletionFailures; long retryAt = o.CompletionRetryAt; bool effectApplied = o.CompletionEffectApplied;
            s.Int(ref failures); s.Long(ref retryAt); s.Bool(ref effectApplied);
            if (s.Reading) { o.CompletionFailures = failures; o.CompletionRetryAt = retryAt; o.CompletionEffectApplied = effectApplied; }
            if (s.Reading)
            {
                o.Plan = ResolveExecutionPlan(o.PlanKey, o.Target) ?? throw new CorruptStateException("save names unknown activity '" + o.PlanKey + "'");
                ValidatePlan(o.Plan); o.BindNodes();
            }
            if (!s.Reading) ValidateWorkerSelection(o);
            int count = s.Count(o.Nodes.Count);
            if (count != o.Nodes.Count) throw new CorruptStateException("saved activity node count differs");
            for (int k = 0; k < count; k++)
            {
                ActivityNodeExecution node = o.Nodes[k];
                string id = node.Id; s.String(ref id);
                if (id != node.Id) throw new CorruptStateException("saved activity node identity/order differs");
                NodeState state = node.State; int job = node.JobId; WorkProgress progress = node.Progress;
                s.Enum(ref state); s.Int(ref job); s.Work(ref progress);
                EntityId completedBy = node.CompletedBy; s.Entity(ref completedBy);
                if (s.Reading) node.CompletedBy = completedBy;
                else ValidateCompletedWorker(node, o.Plan.Graph.Find(node.Id));
                long productiveSteps = node.ProductiveSteps; s.Long(ref productiveSteps);
                if (productiveSteps < 0) throw new CorruptStateException("negative productive activity exposure");
                if (s.Reading) node.ProductiveSteps = productiveSteps;
                string reason = node.Blocker.Id; s.String(ref reason);
                int nodeFailures = node.CompletionFailures; long nodeRetryAt = node.CompletionRetryAt;
                s.Int(ref nodeFailures); s.Long(ref nodeRetryAt);
                if (s.Reading) { node.CompletionFailures = nodeFailures; node.CompletionRetryAt = nodeRetryAt; }
                int choices = s.Count(node.Choices.Count);
                var selected = s.Reading ? new List<WorkRequirementChoice>() : null;
                for (int choice = 0; choice < choices; choice++)
                {
                    WorkRequirementChoice fact = s.Reading ? null : node.Choices[choice];
                    int segments = s.Count(fact?.Path.Count ?? 0); var path = s.Reading ? new List<string>() : null;
                    for (int segment = 0; segment < segments; segment++)
                    { string part = s.Reading ? null : fact.Path[segment]; s.String(ref part); path?.Add(part); }
                    string entry = fact?.Entry; s.String(ref entry);
                    if (s.Reading)
                    {
                        try { selected.Add(new WorkRequirementChoice(path, entry)); }
                        catch (ArgumentException) { throw new CorruptStateException("invalid saved requirement choice"); }
                    }
                }
                if (state < NodeState.Waiting || state > NodeState.Completing || job < 0)
                    throw new CorruptStateException("invalid saved activity node state");
                if (s.Reading) { node.State = state; node.JobId = job; node.Progress = progress; node.Blocker = reason == null ? Code.None : new Code(reason); node.Choices = selected.AsReadOnly(); }
                SyncResources(s, node, o.Plan.Graph.Find(node.Id)); SyncCosts(s, node);
            }
            if (s.Reading)
            {
                ValidateCompletion(o);
                if (o.Id <= 0 || o.Id >= _nextOrder || _orderById.ContainsKey(o.Id) || !_world.Players.IsSeat(o.Owner))
                    throw new CorruptStateException("invalid saved activity execution identity");
                _orders.Add(o); _orderById.Add(o.Id, o);
            }
        }

        public void Sync(IStateSync s)
        {
            if (s.Reading) _world.RequireStateMutation();
            string retry = RetryPolicy.Id; s.String(ref retry);
            if (retry != RetryPolicy.Id) throw new CorruptStateException("saved work retry profile differs from selected content");
            s.Int(ref _nextOrder);
            s.Int(ref _nextJob);
            int no = s.Count(_orders.Count);
            if (s.Reading) { _orders.Clear(); _orderById.Clear(); }
            for (int i = 0; i < no; i++) SyncOrder(s, s.Reading ? new Order() : _orders[i]);
            int nj = s.Count(_jobs.Count);
            if (s.Reading) { _jobs.Clear(); _jobById.Clear(); }
            for (int i = 0; i < nj; i++)
            {
                Job j = s.Reading ? new Job() : _jobs[i];
                if (!s.Reading && j.IsHaul) ValidateHaul(j, false);
                s.Int(ref j.Id);
                s.Player(ref j.Owner);
                string role = j.Role.Value;
                s.String(ref role);
                if (s.Reading)
                    j.Role = _world.Content.Find<IWorkerRoleDefinition>(role)?.Role
                        ?? throw new CorruptStateException("save names unknown worker role '" + role + "'");
                s.Int(ref j.OrderId);
                if (j.OrderId == 0) s.Int(ref j.Priority);
                if (j.Priority < 0 || j.OrderId != 0 && j.Priority != 0)
                    throw new CorruptStateException("node jobs cannot duplicate parent order priority");
                s.Enum(ref j.State);
                s.Bool(ref j.TripHeld);
                string jobBlocker = j.Blocker.Id; s.String(ref jobBlocker);
                if (s.Reading) j.Blocker = jobBlocker == null ? Code.None : new Code(jobBlocker);
                s.Entity(ref j.Worker);
                s.Entity(ref j.Target);
                int nt = s.Count(j.Toils.Count);
                if (s.Reading) j.Toils.Clear();
                for (int k = 0; k < nt; k++)
                {
                    Toil t = s.Reading ? default : j.Toils[k];
                    string executor = t.Executor.Id; s.String(ref executor);
                    t.Executor = executor == null ? Code.None : new Code(executor);
                    if (!_world.Services.Get<IWorkPhaseCatalog>().Contains(t.Executor))
                        throw new CorruptStateException("save names unknown physical work phase executor '" + executor + "'");
                    s.Entity(ref t.At);
                    s.Resource(ref t.Resource);
                    s.Fix(ref t.Amount);
                    if (s.Reading) j.Toils.Add(t);
                }
                s.Int(ref j.ToilIndex);
                s.String(ref j.NodeId);
                s.Bool(ref j.IsHaul);
                if (j.RecoveryKind.HasValue && (byte)j.RecoveryKind.Value > 1) throw new CorruptStateException("invalid recovery kind");
                byte recovery = j.RecoveryKind.HasValue ? (byte)((byte)j.RecoveryKind.Value + 1) : (byte)0;
                s.Byte(ref recovery);
                if (recovery > 2 || recovery != 0 && (!j.IsHaul || j.OrderId != 0)) throw new CorruptStateException("invalid recovery haul provenance");
                if (s.Reading) j.RecoveryKind = recovery == 0 ? (LogisticsRecoveryKind?)null : (LogisticsRecoveryKind)(recovery - 1);
                if (j.TripHeld && (!j.IsHaul || j.State != JobState.Paused || j.Blocker.IsNone))
                    throw new CorruptStateException("trip hold belongs only to a blocked retained haul");
                s.Bool(ref j.Purchase);
                s.Int(ref j.Route);
                bool hasDemand = j.HaulDemand.IsValid; s.Bool(ref hasDemand);
                string selection = j.HaulSelection.Id; s.String(ref selection); if (s.Reading) j.HaulSelection = selection == null ? Code.None : new Code(selection);
                if (!j.IsHaul && !j.HaulSelection.IsNone) throw new CorruptStateException("quality haul intent belongs only to haul jobs");
                if (hasDemand) LogisticsDemandId.Sync(s, ref j.HaulDemand);
                else if (s.Reading) j.HaulDemand = default;
                if (j.HaulDemand.IsValid && (!j.IsHaul || j.Route != 0)) throw new CorruptStateException("requester provenance belongs only to demand hauls");
                if (!s.Reading && j.HaulDemand.IsValid && CheckHaulSource(j).Status == LogisticsDemandStatus.Invalid)
                    throw new CorruptStateException("invalid haul requester provenance");
                s.Entity(ref j.From);
                s.Entity(ref j.To);
                string sourcePort = j.SourcePort.Id, destinationPort = j.DestinationPort.Id;
                s.String(ref sourcePort); s.String(ref destinationPort);
                if (s.Reading) { j.SourcePort = sourcePort == null ? Code.None : new Code(sourcePort); j.DestinationPort = destinationPort == null ? Code.None : new Code(destinationPort); }
                if (j.Purchase && !j.SourcePort.IsNone || !j.IsHaul && (!j.SourcePort.IsNone || !j.DestinationPort.IsNone))
                    throw new CorruptStateException("named haul ports do not belong to this job");
                ResourceEndpointId.Sync(s, ref j.SourceEndpoint); ResourceEndpointId.Sync(s, ref j.DestinationEndpoint);
                if (!j.IsHaul && (j.SourceEndpoint.IsValid || j.DestinationEndpoint.IsValid)) throw new CorruptStateException("non-haul job cannot own physical port bindings");
                s.Resource(ref j.Resource);
                s.Fix(ref j.Amount);
                ReservationToken.Sync(s, ref j.OutReservation);
                ReservationToken.Sync(s, ref j.InReservation);
                ReservationToken.Sync(s, ref j.CargoInReservation);
                ReservationToken.Sync(s, ref j.CargoOutReservation);
                s.Int(ref j.Failures);
                s.Long(ref j.RetryAt);
                s.Fix(ref j.Loaded);
                WorkProgress handlingProgress = j.HandlingProgress; s.Work(ref handlingProgress);
                if (!j.IsHaul && !handlingProgress.Equals(WorkProgress.Zero)) throw new CorruptStateException("ordinary work cannot carry haul handling progress");
                if (s.Reading) j.HandlingProgress = handlingProgress;
                Fix baseline = j.UnloadingBaseline; s.Fix(ref baseline);
                if (!j.IsHaul && !baseline.IsZero) throw new CorruptStateException("ordinary work cannot carry a haul unloading baseline");
                if (s.Reading) j.UnloadingBaseline = baseline;
                string recoveryCause = j.RecoveryCause.Id, recoverySlot = j.RecoveryDockSlot.Id;
                s.String(ref recoveryCause); s.String(ref recoverySlot);
                if (s.Reading)
                { j.RecoveryCause = recoveryCause == null ? Code.None : new Code(recoveryCause); j.RecoveryDockSlot = recoverySlot == null ? Code.None : new Code(recoverySlot); }
                if (!j.IsHaul && (!j.RecoveryCause.IsNone || !j.RecoveryDockSlot.IsNone))
                    throw new CorruptStateException("ordinary work cannot carry haul recovery departure");
                string sourceSlot = j.SourceDockSlot.Id, destinationSlot = j.DestinationDockSlot.Id;
                s.String(ref sourceSlot); s.String(ref destinationSlot);
                if (s.Reading)
                { j.SourceDockSlot = sourceSlot == null ? Code.None : new Code(sourceSlot); j.DestinationDockSlot = destinationSlot == null ? Code.None : new Code(destinationSlot); }
                if (!j.IsHaul && !j.SourceDockSlot.IsNone)
                    throw new CorruptStateException("ordinary work cannot own pickup dock slots");
                if (!s.Reading && !j.IsHaul) ValidateActivityItinerary(j, false);
                s.Long(ref j.CreatedTick);
                if (j.Failures < 0 || j.RetryAt < 0 || j.RetryAt > 0 && j.Failures == 0 ||
                    j.RetryAt > _world.Tick && j.RetryAt - _world.Tick > RetryDelay(j.Failures) ||
                    j.CreatedTick < 0 || j.CreatedTick > _world.Tick)
                    throw new CorruptStateException("job timestamps are outside the session age");
                if (s.Reading)
                {
                    if (j.Id <= 0 || j.Id >= _nextJob || _jobById.ContainsKey(j.Id) ||
                        j.State < JobState.Open || j.State > JobState.Paused)
                        throw new CorruptStateException("invalid saved job identity or state");
                    _jobs.Add(j); _jobById.Add(j.Id, j);
                }
            }
            bool dirty = Dirty; s.Bool(ref dirty); Dirty = dirty;
            SyncPlans(s);
            SyncStoredPlanTemplates(s);
            SyncActivityCompletions(s);
            if (!s.Reading) ValidateRecurringSources();
        }
    }

    // ======================================================================== the system

    /// <summary>
    /// Runs all work, every tick, in a fixed order:
    ///
    ///     1. jobs in progress advance their toils (go, work, load, unload)
    ///     2. orders advance their steps (wait, supply, post a job, finish, run effects)
    ///     3. open work and declared standing-haul sources share one current dispatch pass: eligible
    ///        response, player priority, original age, travel cost and stable identity; native owners acquire
    ///     4. idle machines with cargo drop it at the nearest store; other idle machines go home
    ///
    /// Everything a machine does, it does by travelling and working. Nothing is instant.
    /// </summary>
    public sealed partial class WorkSystem : GameSystem, IWorkerDispatch, IWorkDispatchQueue, ICompositionValidator
    {
        public override int StateSchemaVersion => 45;
        public static readonly Code BlockerMaterials = new Code("blocker.materials");
        public static readonly Code BlockerCash = new Code("blocker.cash");
        public static readonly Code BlockerNoWorker = new Code("blocker.no-worker");
        public static readonly Code BlockerUnreachable = new Code("blocker.unreachable");
        public static readonly Code BlockerWorkerBusy = new Code("work.worker-busy");
        public static readonly Code BlockerWorkerHeld = new Code("work.worker-held");
        public static readonly Code BlockerWorkerUnavailable = new Code("work.worker-unavailable");
        public static readonly Code NoteOrderDone = new Code("note.order-done");
        public static readonly Code NoteOrderFailed = new Code("note.order-failed");
        public static readonly Code NoteArrived = new Code("note.vehicle-arrived");
        public static readonly Code NoteStranded = new Code("note.vehicle-stranded");

        private WorkBoard _board;
        private IWorkSupplyRequests _supply;
        private World _world;
        // Current-call proposals only. Original nodes/jobs remain the sole progress/binding owners.
        private List<(int Order, string Node)> _productive;

        public override string Id => "sim.work";

        public Code CheckIdleWorker(World world, Entity entity, PlayerId owner, WorkerRoleId role, bool emergency)
        {
            if (!ReferenceEquals(world, _board.World) || entity == null || !ReferenceEquals(world.Entities.Get(entity.Id), entity)) return Codes.NotFound;
            return EntityObservation.Read(world, entity, () =>
            {
                Code refusal = CheckWorker(world, entity, owner, role, 0);
                if (!refusal.IsNone) return refusal;
                refusal = WorkPolicyRules.CheckReadiness(WorkPolicyRules.Read(entity), role, emergency, false);
                if (refusal.IsNone && !emergency) refusal = _board.CheckRecurringDispatch(entity, null, null, null);
                return refusal.IsNone && !emergency ? CheckOrdinaryReserve(world, entity) : refusal;
            });
        }
        private static Code CheckOrdinaryReserve(World world, Entity entity)
        {
            if (!world.Services.TryGet<ICompanyWorkerReservePolicy>(out var reserves)) return Code.None;
            if (!ReferenceEquals(reserves.World, world)) throw new InvalidOperationException("company reserves belong to another session");
            return EntityObservation.Read(world, entity, () => reserves.CheckOrdinaryAssignment(entity));
        }

        public Code CheckAssignment(World world, Entity entity, PlayerId owner, WorkerRoleId role,
            IReadOnlyList<Entity> destinations, bool emergency, int ownedJob = 0)
        {
            Job job = world.Services.Get<WorkBoard>().Job(ownedJob);
            Order order = world.Services.Get<WorkBoard>().Order(job?.OrderId ?? 0);
            ActivityNodeExecution execution = order?.Node(job.NodeId);
            return CheckAssignmentCore(world, entity, owner, role, destinations, emergency, ownedJob,
                execution == null ? null : order.Plan.Graph.Find(execution.Id), execution, null);
        }
        private Code CheckAssignmentCore(World world, Entity entity, PlayerId owner, WorkerRoleId role,
            IReadOnlyList<Entity> destinations, bool emergency, int ownedJob,
            IActivityNodeDefinition node, ActivityNodeExecution execution, IRecurringPlanSource recurringSource)
        {
            if (!ReferenceEquals(world, _board.World) || entity == null || !ReferenceEquals(world.Entities.Get(entity.Id), entity) || destinations == null)
                return Codes.InvalidArgument;
            // No service destination exists while this actual incumbent leaves its old dock.
            // Fresh assignments and other jobs still require explicit destinations.
            if (destinations.Count == 0 && (_board.Job(ownedJob)?.RecoveryCause.IsNone != false ||
                !WorkPolicyRules.IsContinuation(world, entity, role, destinations, ownedJob))) return Codes.InvalidArgument;
            Code eligibility = CheckWorker(world, entity, owner, role, ownedJob);
            if (!eligibility.IsNone && eligibility != BlockerWorkerUnavailable) return eligibility;
            foreach (Entity stop in destinations)
                if (stop == null || !stop.Alive || !ReferenceEquals(world.Entities.Get(stop.Id), stop)) return Codes.NotFound;
            if (eligibility.IsNone && entity.Get<IMotion>() != null)
                foreach (Entity stop in destinations)
                    if (SpatialInteraction.CheckProximity(world, entity, stop) != SpatialProximityStatus.WithinReach)
                    {
                        Code blocker = MotionExecutionContracts.ReadPhysical(world, entity).Blocker;
                        // Route admission describes current intent, not a proposed task's new goal.
                        // CheckJob and the actual approach phase gate the active job's route.
                        if (MotionAreaCodes.IsWait(blocker)) continue;
                        if (!blocker.IsNone) { eligibility = BlockerWorkerUnavailable; break; }
                    }
            bool selfService = WorkPolicyRules.Read(entity)?.AssignmentMode == WorkAssignmentMode.EmergencyAndSelfService &&
                world.Services.Get<WorkBoard>().IsSelfService(entity, node, execution, recurringSource);
            Code policy = WorkPolicyRules.CheckAssignment(world, entity, role, destinations, emergency, selfService, ownedJob);
            if (policy.IsNone && eligibility.IsNone && !emergency && !WorkPolicyRules.IsContinuation(world, entity, role, destinations, ownedJob))
                policy = _board.CheckRecurringDispatch(entity, node, execution, recurringSource);
            if (policy.IsNone && eligibility.IsNone && !emergency && !selfService && !WorkPolicyRules.IsContinuation(world, entity, role, destinations, ownedJob))
                policy = CheckOrdinaryReserve(world, entity);
            return policy.IsNone ? eligibility : policy;
        }
        public Code CheckInterruption(World world, Entity worker)
        {
            if (worker == null || !ReferenceEquals(world.Entities.Get(worker.Id), worker)) return Codes.NotFound;
            Job job = world.Services.Get<WorkBoard>().Job(worker.Get<IWorker>()?.Job ?? 0);
            return job?.IsLive == true && WorkPolicyRules.Read(worker)?.Interruptible == false ? WorkPolicyCodes.NotInterruptible : Code.None;
        }
        private List<Entity> RemainingStops(World world, Job job)
        {
            var stops = new List<Entity>();
            // Recovery is leaving, not requesting service at the old endpoint. Selected exit geometry
            // and continuous travel still pass WorkBoard's existing dock/area/route rules.
            if (!job.RecoveryCause.IsNone) return stops;
            for (int i = job.ToilIndex; i < job.Toils.Count; i++)
            { Entity stop = world.Entities.Get(job.Toils[i].At); if (!stops.Contains(stop)) stops.Add(stop); }
            return stops;
        }
        public Code CheckJob(World world, Entity worker, Job job)
        {
            Code planBlocker = _board.PlanOrderBlocker(job.OrderId);
            if (!planBlocker.IsNone) return planBlocker;
            Code selected = _board.CheckWorkerSelection(job, worker);
            if (!selected.IsNone) return selected;
            Code refusal = CheckAssignment(world, worker, job.Owner, job.Role, RemainingStops(world, job), _board.IsEmergency(job), job.Id);
            if (refusal.IsNone) refusal = _board.CheckAssignedDockPolicy(job, worker);
            return refusal.IsNone && worker.Get<IMotion>() != null ? MotionAreaContracts.Read(world, worker).Blocker : refusal;
        }
        private Code ReplanJobDocks(World world, Entity worker, Job job)
        {
            Code planBlocker = _board.PlanOrderBlocker(job.OrderId);
            if (!planBlocker.IsNone) return planBlocker;
            Code refusal = CheckAssignment(world, worker, job.Owner, job.Role, RemainingStops(world, job), _board.IsEmergency(job), job.Id);
            return refusal.IsNone ? _board.ReplanDockBindings(job, worker) : refusal;
        }
        /// <summary>Policy edits settle the existing job immediately; never release cargo or owned claims.</summary>
        public void RefreshPolicy(World world, Entity worker)
        {
            Job job = world.Services.Get<WorkBoard>().Job(worker.Get<IWorker>()?.Job ?? 0);
            if (job?.IsLive == true)
            {
                Code refusal = ReplanJobDocks(world, worker, job);
                if (refusal.IsNone) refusal = CheckJob(world, worker, job);
                if (HoldTrip(world, worker, job, refusal)) return;
                if (WorkPolicyCodes.IsWait(refusal) || WorkDockCodes.IsWait(refusal) || PlanCodes.IsWait(refusal)) PausePolicy(world, worker, job, refusal);
            }
            else
            {
                WorkerHomeContracts.UpdateIdle(world, worker);
            }
        }
        private void PausePolicy(World world, Entity worker, Job job, Code refusal)
        {
            job.TripHeld = false;
            job.State = JobState.Paused;
            if (!job.IsHaul || job.RetryAt == 0) job.Blocker = refusal;
            // Route-area waits retain the actual intent so pure admission can reopen it after an edit.
            if (!MotionAreaCodes.IsWait(refusal)) MotionExecutionContracts.Stop(world, worker);
            Order order = _board.Order(job.OrderId); ActivityNodeExecution node = order?.Node(job.NodeId);
            if (node != null) { node.State = NodeState.Paused; node.Blocker = refusal; order.Blocker = refusal; }
        }
        private bool HoldTrip(World world, Entity worker, Job job, Code refusal)
        {
            if (!job.IsHaul || worker.Get<IHaulTripPolicy>() == null ||
                !refusal.IsNone && refusal != BlockerWorkerUnavailable && refusal != BlockerUnreachable &&
                !MotionAreaCodes.IsWait(refusal) && !WorkDockCodes.IsWait(refusal)) return false;
            HaulTripEvaluation budget = world.Services.Get<IHaulExecution>().ReadHaulTripBudget(worker.Id);
            if (budget == null || budget.Blocker.IsNone) return false;
            job.RetryAt = 0; PausePolicy(world, worker, job, budget.Blocker); job.TripHeld = true;
            // Keep actual cargo/receipts/progress, but release obsolete movement intent so the
            // stationary recipient can be physically serviced. Next admission uses actual position.
            MotionExecutionContracts.Stop(world, worker);
            return true;
        }

        internal Code CheckPlanDependencyInterruption(World world, Order order)
        {
            foreach (ActivityNodeExecution node in order.Nodes)
            {
                Job job = _board.Job(node.JobId);
                if (job?.IsLive != true || !job.Worker.IsValid) continue;
                Code refusal = CheckInterruption(world, world.Entities.Get(job.Worker));
                if (!refusal.IsNone) return refusal;
            }
            return Code.None;
        }

        internal void PausePlannedOrder(World world, Order order, Code blocker)
        {
            if (!PlanCodes.IsWait(blocker)) throw new ArgumentException("planned work needs its actual pause/dependency blocker");
            if (order.State != OrderState.Completing) order.Blocker = blocker;
            foreach (ActivityNodeExecution node in order.Nodes)
            {
                Job job = _board.Job(node.JobId); Entity worker = world.Entities.Get(job?.Worker ?? EntityId.None);
                if (blocker != PlanCodes.Paused && worker != null && job?.IsLive == true)
                {
                    Code refusal = Interrupt(world, job);
                    if (!refusal.IsNone) throw new InvalidOperationException("admitted plan dependency interruption refused: " + refusal);
                }
                if (node.State == NodeState.Working || node.State == NodeState.Paused)
                {
                    if (blocker != PlanCodes.Paused)
                    {
                        _board.ReleaseResources(node, order.Owner);
                        if (!WorkBoard.HasSpent(node)) node.Choices = Array.Empty<WorkRequirementChoice>();
                    }
                    node.State = NodeState.Paused; node.Blocker = blocker;
                }
                if (blocker == PlanCodes.Paused && worker != null && job?.IsLive == true) PausePolicy(world, worker, job, blocker);
            }
        }

        public Fix EffectiveWorkRate(World world, Entity entity)
        {
            return WorkerExecutionContracts.Read(world, entity).EffectiveRate;
        }

        private Code CheckWorker(World world, Entity entity, PlayerId owner, WorkerRoleId role, int ownedJob)
        {
            IWorker worker = entity.Get<IWorker>();
            if (!entity.Alive || entity.Owner != owner || worker == null || !WorkerExecutionContracts.Can(world, entity, role)) return BlockerNoWorker;
            if (worker.Job != 0 && worker.Job != ownedJob) return BlockerWorkerBusy;
            if (worker.Held) return BlockerWorkerHeld;
            var execution = WorkerExecutionContracts.Read(world, entity);
            if (!execution.EffectiveRate.IsPositive || !execution.Gates.Blocker.IsNone) return BlockerWorkerUnavailable;
            return Code.None;
        }

        /// <summary>Sole pure worker selection path for proposed/current activity and standalone jobs.</summary>
        public void CopyCandidates(World world, PlayerId owner, IReadOnlyList<WorkerRoleId> roles, Entity target,
            IActivityNodeDefinition node, int ownedJob, IReadOnlyList<WorkRequirementChoice> choices, ActivityNodeExecution execution,
            WorkCandidateMode mode, IList<WorkWorkerCandidate> destination, IRecurringPlanSource recurringSource)
            => CopyCandidatesCore(world, owner, roles, target, node, ownedJob, choices, execution, mode, destination, recurringSource, false);

        public void CopyResponseCandidates(World world, EntityId targetId, Code source, string nodeId, IList<WorkWorkerCandidate> destination)
        {
            if (!ReferenceEquals(world.Services.Get<IWorkerDispatch>(), this)) throw new InvalidOperationException("response selector belongs to another session");
            if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
            Entity target = world.Entities.Get(targetId); var response = target?.Get<IIncidentResponse>(source);
            if (target == null || !target.Alive || source.IsNone || response == null) throw new ArgumentException("response candidates need actual target/source");
            EntityObservation.Read(world, target, () =>
            {
                IActivityDefinition plan = response.ResponsePlan; ResponseWork.ValidatePlan(plan);
                if (!ReferenceEquals(_board.ResolvePlan(plan.PlanKey), plan)) throw new InvalidOperationException("response candidates need actual composed definition");
                IActivityNodeDefinition node = plan.Graph.Find(nodeId);
                if (node == null) throw new ArgumentException("response candidates need authored node identity");
                IncidentSnapshot episode = ResponseWork.Read(response);
                Order order = episode.Active ? _board.ResponseOn(target.Id, source, episode.Episode) : null;
                if (order != null && !ResponseWork.CheckActiveOrder(world, target, order).IsNone) throw new InvalidOperationException("response candidates have stale episode/order binding");
                ActivityNodeExecution execution = order?.Node(node.Id); Job job = _board.Job(execution?.JobId ?? 0);
                CopyCandidatesCore(world, target.Owner, node.Roles, target, node, job?.Id ?? 0, execution?.Choices, execution,
                    order == null ? WorkCandidateMode.IdleOnly : WorkCandidateMode.EmergencyInterruption, destination, null, true);
                return true;
            });
        }

        private void CopyCandidatesCore(World world, PlayerId owner, IReadOnlyList<WorkerRoleId> roles, Entity target,
            IActivityNodeDefinition node, int ownedJob, IReadOnlyList<WorkRequirementChoice> choices, ActivityNodeExecution execution,
            WorkCandidateMode mode, IList<WorkWorkerCandidate> destination, IRecurringPlanSource recurringSource, bool responseContext)
        {
            if (mode != WorkCandidateMode.IdleOnly && mode != WorkCandidateMode.EmergencyInterruption) throw new ArgumentOutOfRangeException(nameof(mode));
            destination.Clear();
            if (target == null || !target.Alive || target.Owner != owner) return;
            Job job = world.Services.Get<WorkBoard>().Job(ownedJob);
            var stops = new List<Entity> { target };
            if (job != null)
            {
                if (job.Owner != owner || job.Target != target.Id) throw new InvalidOperationException("candidate query names a different job");
                for (int stopIndex = job.ToilIndex; stopIndex < job.Toils.Count; stopIndex++)
                {
                    Toil toil = job.Toils[stopIndex];
                    Entity stop = world.Entities.Get(toil.At);
                    if (stop == null) return;
                    if (!stops.Contains(stop)) stops.Add(stop);
                }
            }
            Entity first = job?.Toils.Count > 0 ? world.Entities.Get(job.Toils[0].At) : target;
            var candidates = new List<WorkWorkerCandidate>();
            EntityId selected = _board.Selection(execution).WorkerFor(node?.Id);
            WorkerRoleId selectedRole = _board.Selection(execution).RoleFor(node?.Id);
            if (selected.IsValid)
            {
                Entity actual = world.Entities.Get(selected);
                Code refusal = actual == null || !actual.Alive ? Codes.NotFound : actual.Owner != owner ? Codes.NotOwned :
                    actual.Get<IWorker>() == null ? BlockerNoWorker : Code.None;
                if (!refusal.IsNone)
                {
                    destination.Add(new WorkWorkerCandidate(selected, WorkerRoleId.None, null, WorkRequirementResult.Pending(refusal), 0, 0));
                    return;
                }
            }
            ComponentList<IWorker> workers = world.Entities.Components<IWorker>();
            for (int i = 0; i < workers.Count; i++)
            {
                IWorker worker = workers[i]; Entity entity = worker.Entity;
                if (!entity.Alive || entity.Owner != owner) continue;
                if (selected.IsValid && entity.Id != selected) continue;
                bool supported = false;
                foreach (WorkerRoleId role in roles)
                {
                    if (selectedRole.IsValid && selectedRole != role) continue;
                    if (!WorkerExecutionContracts.Can(world, entity, role)) continue;
                    supported = true;
                    // Live execution owns its actual role; alternatives are unacquired intent only.
                    if (job?.IsLive == true && job.Worker == entity.Id && job.Role != role) continue;
                    WorkRequirementResult result; long? cost = null; int interrupted = 0;
                    Job incumbent = world.Services.Get<WorkBoard>().Job(worker.Job);
                    bool mayInterrupt = mode == WorkCandidateMode.EmergencyInterruption && job != null &&
                        CanConsiderInterruption(world, owner, WorkModule.ModuleId, world.Services.Get<WorkBoard>().IsEmergency(job), entity);
                    Code eligibility = _board.PlanOrderBlocker(execution?.OrderId ?? job?.OrderId ?? 0);
                    if (eligibility.IsNone) eligibility = CheckAssignmentCore(world, entity, owner, role, stops,
                        responseContext || job != null && world.Services.Get<WorkBoard>().IsEmergency(job), mayInterrupt ? incumbent.Id : ownedJob, node, execution, recurringSource);
                    if (!eligibility.IsNone) result = eligibility == BlockerWorkerUnavailable || WorkPolicyCodes.IsWait(eligibility) || PlanCodes.IsWait(eligibility)
                        ? WorkRequirementResult.Blocked(eligibility) : WorkRequirementResult.Pending(eligibility);
                    else
                    {
                        Code facility = node == null ? Code.None : _board.CheckExecutorWorker(
                            new WorkRequirementContext(world, owner, target, node, entity, choices, execution, role)); long estimate = 0;
                        bool reachable;
                        if (!facility.IsNone) reachable = false;
                        else if (job?.IsHaul != true)
                        {
                            facility = world.Services.Get<WorkBoard>().PreviewDock(target, entity, WorkDockRequest.Work(role), null, out _, out estimate, out _, out _, job);
                            reachable = facility.IsNone;
                        }
                        else reachable = SpatialInteraction.TryEstimateCost(world, entity, first, out estimate);
                        if (reachable) foreach (Entity stop in stops)
                            if (!SpatialInteraction.CanApproach(SpatialInteraction.Check(world, entity, stop))) { reachable = false; break; }
                        if (!reachable) result = WorkRequirementResult.Blocked(facility.IsNone ? BlockerUnreachable : facility);
                        else
                        {
                            cost = estimate;
                            result = node == null ? WorkRequirementResult.Satisfied() : world.Services.Get<WorkBoard>().EvaluateRequirement(
                                node.Requirement, new WorkRequirementContext(world, owner, target, node, entity, choices, execution, role), null);
                            if (mayInterrupt && result.Status == WorkRequirementStatus.Satisfied)
                            {
                                var offer = new WorkerDispatchOffer(WorkModule.ModuleId, job.Id, "job", owner,
                                    target.Id, entity.Id, true, Code.None, true, world.Services.Get<WorkBoard>().Order(job.OrderId)?.Priority ?? 0,
                                    job.CreatedTick, world.Tick, cost, 1, 0, true);
                                interrupted = PreviewInterruption(world, offer);
                                if (interrupted == 0) result = WorkRequirementResult.Pending(BlockerWorkerBusy);
                            }
                        }
                    }
                    candidates.Add(new WorkWorkerCandidate(entity.Id, role, cost, result, WorkPolicyRules.Priority(entity), interrupted));
                }
                if (!supported && selected.IsValid)
                    candidates.Add(new WorkWorkerCandidate(selected, WorkerRoleId.None, null,
                        WorkRequirementResult.Pending(BlockerNoWorker), 0, 0));
            }
            candidates.Sort((a, b) =>
            {
                int rank = CandidateRank(a.Result.Status).CompareTo(CandidateRank(b.Result.Status));
                if (rank != 0) return rank;
                int interruption = (a.InterruptedJob != 0).CompareTo(b.InterruptedJob != 0);
                if (interruption != 0) return interruption;
                int preference = b.Priority.CompareTo(a.Priority);
                if (preference != 0) return preference;
                int cost = (a.TravelCost ?? long.MaxValue).CompareTo(b.TravelCost ?? long.MaxValue);
                if (cost != 0) return cost;
                int role = string.CompareOrdinal(a.Role.Value, b.Role.Value);
                return role != 0 ? role : a.Worker.Value.CompareTo(b.Worker.Value);
            });
            foreach (var candidate in candidates) destination.Add(candidate);
        }
        private static int CandidateRank(WorkRequirementStatus status)
            => status == WorkRequirementStatus.Satisfied ? 0 : status == WorkRequirementStatus.Pending ? 1 : status == WorkRequirementStatus.Blocked ? 2 : 3;

        private bool CanConsiderInterruption(World world, PlayerId company, string scope, bool emergency, Entity worker)
        {
            WorkBoard board = world.Services.Get<WorkBoard>();
            Job incumbent = board.Job(worker?.Get<IWorker>()?.Job ?? 0);
            if (worker == null || !worker.Alive || !ReferenceEquals(world.Entities.Get(worker.Id), worker) || !emergency ||
                incumbent == null || incumbent.State != JobState.Assigned && incumbent.State != JobState.Paused ||
                incumbent.IsHaul || board.IsEmergency(incumbent) ||
                worker.Owner != company || incumbent.Owner != company || incumbent.Worker != worker.Id ||
                worker.Get<IWorker>()?.Job != incumbent.Id || worker.Get<IWorker>().Held || !CheckInterruption(world, worker).IsNone ||
                worker.Get<ITransporter>() != null && world.Services.Get<IHaulExecution>().ReadCargoDisposition(worker.Id).HasResidualCargo ||
                !world.Services.TryGet<IWorkerDispatchPreemptionPolicy>(out _) ||
                !world.Services.TryGet<IWorkerDispatchPolicy>(out var policy)) return false;
            foreach (string declared in policy.DispatchScopes) if (declared == scope) return true;
            return false;
        }

        public int PreviewInterruption(World world, WorkerDispatchOffer challenger)
        {
            Entity worker = world.Entities.Get(challenger.Worker), target = world.Entities.Get(challenger.Target);
            if (!challenger.Ready || target == null || !target.Alive || target.Owner != challenger.Company ||
                !CanConsiderInterruption(world, challenger.Company, challenger.Scope, challenger.Emergency, worker)) return 0;
            Job incumbent = world.Services.Get<WorkBoard>().Job(worker.Get<IWorker>().Job);
            return world.Services.Get<IWorkerDispatchPreemptionPolicy>().CanPreempt(RankOffer(world, challenger.WithInterruption(0)),
                new WorkExecutionFacts(incumbent, world.Services.Get<WorkBoard>())) ? incumbent.Id : 0;
        }

        public void InterruptForDispatch(World world, WorkerDispatchOffer challenger)
        {
            int incumbent = PreviewInterruption(world, challenger);
            if (incumbent == 0 || incumbent != challenger.InterruptedJob ||
                !Interrupt(world, world.Services.Get<WorkBoard>().Job(incumbent)).IsNone)
                throw new InvalidOperationException("dispatch interruption eligibility changed during admission");
        }

        public override void Initialize(World world)
        {
            _world = world;
            _board = world.Services.Get<WorkBoard>();
            world.Services.TryGet(out _supply);
        }

        public override void OnLoaded(World world)
        {
            try { WorkPolicyRules.ValidateLive(world); WorkerHomeContracts.ValidateLive(world); WorkerExecutionContracts.ValidateLive(world); OperatingInputsContracts.ValidateWorkers(world); _board.ValidateLoadedCommitments(); }
            catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
            catch (InvalidOperationException error) { throw new CorruptStateException(error.Message); }
        }
        public override void Sync(IStateSync sync)
        { if (!sync.Reading) { WorkPolicyRules.ValidateLive(_world); WorkerHomeContracts.ValidateLive(_world); WorkerExecutionContracts.ValidateLive(_world); OperatingInputsContracts.ValidateWorkers(_world); } }

        public override void Update(World world, TickInfo tick)
        {
            if (_productive != null) throw new InvalidOperationException("work update cannot be re-entered");
            _productive = new List<(int Order, string Node)>();
            try { UpdateWork(world, tick); }
            finally { _productive = null; _board.EndCompanyActivityRates(); }
        }

        private void UpdateWork(World world, TickInfo tick)
        {
            WorkPolicyRules.ValidateLive(world);
            WorkerExecutionContracts.ValidateLive(world);
            var inactive = CaptureInactiveInputs(world);
            _board.ObserveRecurringPlans();
            _board.PromotePlans();
            _board.BeginCompanyActivityRates();
            IReadOnlyList<Job> jobs = _board.Jobs;
            for (int i = 0; i < jobs.Count; i++)
                if (jobs[i].State == JobState.Assigned || jobs[i].State == JobState.Paused) RunJob(world, jobs[i]);
            IReadOnlyList<Order> orders = _board.Orders;
            var advanced = AdvanceProductiveFrontier(world);
            for (int i = 0; i < orders.Count; i++)
                if (orders[i].IsActive) AdvanceOrder(world, orders[i], advanced);
            RefreshSupplies(world);
            bool assign = _board.Dirty || tick.Tick % world.Time.ExactTicks(Tuning.AssignFallbackInterval) == 0;
            IReadOnlyList<IWorkerDispatchSource> dispatchSources = DispatchSources(world);
            foreach (IWorkerDispatchSource source in dispatchSources)
                if (source.GetDispatchSchedule(world.Time).IsDue(tick.Tick)) assign = true;
            _board.Dirty = false;
            if (assign)
            {
                foreach (IWorkerDispatchSource source in dispatchSources) source.PrepareDispatch();
                Assign(world);
            }
            IdleBehaviour(world);
            foreach (var item in inactive)
                if (item.Entity.Alive && ReferenceEquals(world.Entities.Get(item.Entity.Id), item.Entity) &&
                    OperatingInputsContracts.CurrentWorkerPhase(world, item.Entity) == item.Phase)
                    OperatingInputsContracts.Commit(world, item.Entity, item.Phase);
        }

        private static List<(Entity Entity, OperatingPhase Phase)> CaptureInactiveInputs(World world)
        {
            var inactive = new List<(Entity Entity, OperatingPhase Phase)>();
            foreach (var entity in world.Entities.All)
                if (entity.Alive && entity.Has<IWorker>() && OperatingInputsContracts.HasInputs(entity))
                {
                    var phase = OperatingInputsContracts.CurrentWorkerPhase(world, entity);
                    if (phase.HasValue) inactive.Add((entity, phase.Value));
                }
            return inactive;
        }

        public override void CompleteUpdate(World world) { _board.SettlePlans(); _board.Prune(); _board.RetireAutomaticPlanHistory(); }

        // ------------------------------------------------------------------ orders

        private void AdvanceOrder(World world, Order o, HashSet<(int Order, string Node)> advanced)
        {
            Entity target = world.Entities.Get(o.Target);
            if (target == null) { EndOrder(world, o, OrderState.Cancelled); return; }
            if (FailStaleResponse(world, o)) return;
            Code planBlocker = _board.PlanOrderBlocker(o.Id);
            if (!planBlocker.IsNone) { PausePlannedOrder(world, o, planBlocker); return; }
            if (o.State == OrderState.Completing) { CompleteOrder(world, o); return; }
            o.Blocker = Code.None;
            for (int i = 0; i < o.Plan.Graph.Nodes.Count; i++)
            {
                if (!o.IsActive) return;
                IActivityNodeDefinition definition = o.Plan.Graph.Nodes[i];
                ActivityNodeExecution node = o.Node(definition.Id);
                if (advanced.Contains((o.Id, node.Id))) { SetBlocker(o, node); continue; }
                if (node.State == NodeState.Completing)
                { ApplyNodeEffect(world, o, definition); SetBlocker(o, node); continue; }
                node.Blocker = Code.None;
                if (node.State == NodeState.Done) continue;
                if (node.State == NodeState.Failed) { EndOrder(world, o, OrderState.Failed); return; }
                if (!DependenciesDone(o, definition))
                { node.Blocker = BlockerDependencies; SetBlocker(o, node); continue; }
                WorkRequirementResult result = _board.EvaluateRequirements(o.Owner, target, definition, node, null);
                if (result.Status != WorkRequirementStatus.Satisfied)
                {
                    node.Blocker = result.Blocker;
                    if (result.Blocker == WorkResourceCodes.Lost)
                    {
                        Job lost = _board.Job(node.JobId);
                        if (lost?.IsLive == true) Reopen(world, lost, false);
                        else
                        {
                            _board.ReleaseResources(node, o.Owner);
                            if (!WorkBoard.HasSpent(node)) node.Choices = Array.Empty<WorkRequirementChoice>();
                            node.State = NodeState.Ready;
                        }
                        SetBlocker(o, node); continue;
                    }
                    if (result.Status == WorkRequirementStatus.Invalid)
                    { node.State = NodeState.Failed; SetBlocker(o, node); EndOrder(world, o, OrderState.Failed); return; }
                    if (node.State == NodeState.Working || node.State == NodeState.Paused)
                    { node.State = NodeState.Paused; Job paused = _board.Job(node.JobId); if (paused?.IsLive == true) paused.State = JobState.Paused; }
                    SetBlocker(o, node);
                    continue;
                }
                switch (node.State)
                {
                    case NodeState.Waiting:
                        node.State = NodeState.Ready;
                        goto case NodeState.Ready;
                    case NodeState.Ready:
                        if (definition.Duration.IsZero) { node.Choices = result.Choices; CompleteNode(world, o, definition); continue; }
                        if (!ActivityExecutorContracts.RequiresWorker(definition))
                        {
                            _board.Dirty = true; continue; // Timed and staffed starts share the final admission pass.
                        }
                        var context = _board.ExecutorContext(o, definition, null);
                        var itinerary = ActivityExecutorContracts.Read(world, definition.Executor, target, null, () => definition.Executor.CreateItinerary(context));
                        if (itinerary == null) throw new InvalidOperationException("activity executor returned no itinerary");
                        var phases = new List<Toil>(itinerary).AsReadOnly();
                        if (phases.Count == 0 || !ActivityExecutorContracts.Read(world, definition.Executor, target, null, () => definition.Executor.MatchesItinerary(context, phases)))
                            throw new InvalidOperationException("activity executor returned an invalid itinerary");
                        foreach (Toil phase in phases) if (!world.Services.Get<IWorkPhaseCatalog>().Contains(phase.Executor))
                            throw new InvalidOperationException("activity itinerary references unknown phase executor");
                        Job j = _board.AddJob(o.Owner, definition.Roles[0], 0, o.Target);
                        j.OrderId = o.Id;
                        j.NodeId = definition.Id;
                        j.Toils.AddRange(phases);
                        node.JobId = j.Id; node.State = NodeState.Working;
                        continue;
                    case NodeState.Paused:
                    case NodeState.Working:
                        if (!ActivityExecutorContracts.RequiresWorker(definition))
                        {
                            // Already acquired timed nodes were considered once before this graph pass.
                            // Waiting starts remain in the sole final dispatch phase.
                            continue;
                        }
                        Job job = _board.Job(node.JobId);
                        if (job == null || job.State == JobState.Cancelled)
                        { node.JobId = 0; node.State = NodeState.Ready; continue; }
                        if (job.State == JobState.Failed)
                        { node.State = NodeState.Failed; EndOrder(world, o, OrderState.Failed); return; }
                        if (job.State == JobState.Open) node.Blocker = !job.Blocker.IsNone ? job.Blocker : job.Failures > 0 ? BlockerUnreachable : BlockerNoWorker;
                        else if (job.State == JobState.Paused) node.State = NodeState.Paused;
                        else if (job.State == JobState.Assigned) node.State = NodeState.Working;
                        if (!job.Blocker.IsNone) node.Blocker = job.Blocker;
                        SetBlocker(o, node); continue;
                }
            }
            if (!o.IsActive) return;
            if (o.NodesDone == o.Nodes.Count) { o.State = OrderState.Completing; CompleteOrder(world, o); }
        }

        private HashSet<(int Order, string Node)> AdvanceProductiveFrontier(World world)
        {
            foreach (Order order in _board.Orders)
            {
                Entity target = world.Entities.Get(order.Target);
                if (!order.IsActive || order.State == OrderState.Completing || target?.Alive != true ||
                    FailStaleResponse(world, order) || !_board.PlanOrderBlocker(order.Id).IsNone) continue;
                foreach (IActivityNodeDefinition definition in order.Plan.Graph.Nodes)
                {
                    ActivityNodeExecution node = order.Node(definition.Id);
                    if (definition.Duration.IsZero || ActivityExecutorContracts.RequiresWorker(definition) ||
                        node.State != NodeState.Working && node.State != NodeState.Paused || !DependenciesDone(order, definition)) continue;
                    WorkRequirementResult readiness = _board.EvaluateRequirements(order.Owner, target, definition, node, null);
                    // The original graph walk still owns invalid requirements and lost bindings.
                    if (readiness.Status == WorkRequirementStatus.Satisfied) _productive.Add((order.Id, node.Id));
                }
            }
            var attempted = new HashSet<(int Order, string Node)>();
            var ranked = new List<(WorkerDispatchOffer Offer, int Order, string Node)>();
            foreach (var identity in _productive)
            {
                Order order = _board.Order(identity.Order);
                if (order?.IsActive != true) continue;
                ActivityNodeExecution node = order.Node(identity.Node);
                IActivityNodeDefinition definition = order.Plan.Graph.Find(identity.Node);
                if (!attempted.Add(identity)) throw new InvalidOperationException("duplicate productive proposal in one work update");
                bool requiresWorker = ActivityExecutorContracts.RequiresWorker(definition);
                Job job = requiresWorker ? _board.Job(node.JobId) : null;
                var offer = new WorkerDispatchOffer(WorkModule.ModuleId, order.Id, "node." + node.Id, order.Owner,
                    order.Target, job?.Worker ?? EntityId.None, true, Code.None, _board.IsEmergency(order),
                    order.Priority, order.CreatedTick, world.Tick, 0, 1, 0, requiresWorker);
                ranked.Add((RankOffer(world, offer), order.Id, node.Id));
            }
            ranked.Sort((a, b) => CompareAssignments(a.Offer, b.Offer));
            var companies = new HashSet<PlayerId>();
            foreach (var first in ranked)
            {
                PlayerId company = first.Offer.Company;
                if (!companies.Add(company)) continue;
                var group = new List<(int Order, string Node)>();
                foreach (var member in ranked) if (member.Offer.Company == company) group.Add((member.Order, member.Node));
                bool together = _board.AdvanceWork(group.AsReadOnly(), company, out Code groupBlocker);
                foreach (var identity in group)
                {
                    Order order = _board.Order(identity.Order);
                    if (!order.IsActive) continue;
                    ActivityNodeExecution node = order.Node(identity.Node);
                    IActivityNodeDefinition definition = order.Plan.Graph.Find(identity.Node);
                    Code blocker = groupBlocker;
                    bool committed = together || group.Count > 1 &&
                        _board.AdvanceWork(Array.AsReadOnly(new[] { identity }), company, out blocker);
                    node.Blocker = committed ? Code.None : blocker;
                    node.State = committed ? NodeState.Working : NodeState.Paused;
                    if (ActivityExecutorContracts.RequiresWorker(definition))
                    {
                        Job job = _board.Job(node.JobId);
                        job.Blocker = node.Blocker; job.State = committed ? JobState.Assigned : JobState.Paused;
                        if (committed && node.Progress.Complete(world.Time.Ticks(definition.Duration)) && !_board.RetainProductiveWork(order, definition))
                        { job.ToilIndex++; if (job.ToilIndex >= job.Toils.Count) Finish(world, job); }
                    }
                    else if (committed && node.Progress.Complete(world.Time.Ticks(definition.Duration)) && !_board.RetainProductiveWork(order, definition)) CompleteNode(world, order, definition);
                }
            }
            return attempted;
        }

        private void RefreshSupplies(World world)
        {
            foreach (Order order in _board.Orders) if (order.IsActive && order.Plan.AutomaticSupply) RefreshSupply(world, order);
        }
        private void RefreshSupply(World world, Order order)
        {
            var inputs = new List<LogisticsSupplyRequest>(); Entity target = world.Entities.Get(order.Target);
            if (target != null && order.State == OrderState.Active && _board.PlanOrderBlocker(order.Id).IsNone)
                foreach (IActivityNodeDefinition definition in order.Plan.Graph.Nodes)
                {
                    ActivityNodeExecution node = order.Node(definition.Id);
                    if (node.State == NodeState.Done || node.State == NodeState.Completing || node.State == NodeState.Failed || node.State == NodeState.Cancelled ||
                        !DependenciesDone(order, definition)) continue;
                    WorkRequirementResult result = _board.EvaluateRequirements(order.Owner, target, definition, node, null);
                    // Unclaimed but supplied stock remains an obligation until a real actor acquires it.
                    if (result.Status != WorkRequirementStatus.Invalid) _board.CopySupply(result, node, target, inputs);
                }
            // One existing demand identity aggregates every eligible node, never a new supply ledger.
            // A material-free plan may explicitly run without the optional logistics module.
            // There is no demand to withdraw in that composition, and no invented supplier.
            if (_supply == null)
            {
                if (inputs.Count != 0) throw new InvalidOperationException("automatic material inputs require the composed IWorkSupplyRequests owner");
                return;
            }
            if (inputs.Count == 0) _supply.WithdrawInputs(order.Id);
            else _supply.RequestInputs(order.Id, target, inputs.AsReadOnly(), order.Priority);
        }

        internal static readonly Code BlockerDependencies = new Code("work.dependencies");
        private static void SetBlocker(Order order, ActivityNodeExecution node)
        { if (order.Blocker.IsNone && !node.Blocker.IsNone) order.Blocker = node.Blocker; }

        internal static bool DependenciesDone(Order order, IActivityNodeDefinition definition)
        {
            for (int k = 0; k < definition.Dependencies.Count; k++)
                if (order.Node(definition.Dependencies[k]).State != NodeState.Done) return false;
            return true;
        }

        private void CompleteNode(World world, Order o, IActivityNodeDefinition definition)
        {
            if (FailStaleResponse(world, o)) return;
            ActivityNodeExecution node = o.Node(definition.Id);
            _board.ReleaseResources(node, o.Owner);
            Job job = _board.Job(node.JobId);
            if (definition.Roles.Count > 0 && !definition.Duration.IsZero)
            {
                Entity actor = world.Entities.Get(job?.Worker ?? EntityId.None);
                if (job == null || !job.IsLive || job.IsHaul || job.OrderId != o.Id || job.NodeId != definition.Id ||
                    job.ToilIndex != job.Toils.Count || !node.Progress.Complete(world.Time.Ticks(definition.Duration)) ||
                    actor == null || !actor.Alive || actor.Owner != o.Owner || actor.Get<IWorker>()?.Job != job.Id ||
                    !_board.CheckWorkerSelection(job, actor).IsNone)
                    throw new InvalidOperationException("completed machine work requires its actual earned and departed job actor");
                node.CompletedBy = actor.Id;
            }
            if (job?.IsLive == true) Release(world, job, JobState.Done);
            node.JobId = 0; node.State = NodeState.Completing;
            ApplyNodeEffect(world, o, definition);
            SetBlocker(o, node);
        }

        private void ApplyNodeEffect(World world, Order order, IActivityNodeDefinition definition)
        {
            ActivityNodeExecution node = order.Node(definition.Id);
            if (node.CompletionRetryAt > world.Tick) return;
            WorkEffectResult result = _board.RunEffect(definition.Effect, order, definition);
            if (result.Status == WorkEffectStatus.Applied)
            { node.State = NodeState.Done; node.Blocker = Code.None; node.CompletionFailures = 0; node.CompletionRetryAt = 0; }
            else if (_board.DeferCompletion(result, node.CompletionFailures, out int failures, out long deadline))
            { node.CompletionFailures = failures; node.CompletionRetryAt = deadline; node.Blocker = result.Blocker; }
            else
            { order.Blocker = node.Blocker = result.Blocker; node.State = NodeState.Failed; EndOrder(world, order, OrderState.Failed); }
        }

        private void CompleteOrder(World world, Order order)
        {
            if (order.CompletionRetryAt > world.Tick) return;
            WorkEffectResult result = WorkEffectResult.Applied;
            if (!order.CompletionEffectApplied)
            {
                result = _board.RunEffect(order.Plan.OnComplete, order, null);
                if (result.Status == WorkEffectStatus.Applied) order.CompletionEffectApplied = true;
            }
            if (result.Status == WorkEffectStatus.Applied) { order.Blocker = Code.None; EndOrder(world, order, OrderState.Done); }
            else if (_board.DeferCompletion(result, order.CompletionFailures, out int failures, out long deadline))
            { order.CompletionFailures = failures; order.CompletionRetryAt = deadline; order.Blocker = result.Blocker; }
            else { order.Blocker = result.Blocker; EndOrder(world, order, OrderState.Failed); }
        }

        /// <summary>Ends an order and every live job it posted.</summary>
        internal void EndOrder(World world, Order o, OrderState state)
        {
            if (!o.IsActive) return;
            o.State = state;
            if (state == OrderState.Done) o.CompletedTick = world.Tick;
            o.CompletionFailures = 0; o.CompletionRetryAt = 0;
            for (int i = 0; i < o.Nodes.Count; i++)
            {
                ActivityNodeExecution node = o.Nodes[i];
                Job j = _board.Job(node.JobId);
                if (j != null && j.IsLive) Release(world, j, JobState.Cancelled);
                node.JobId = 0;
                node.CompletionFailures = 0; node.CompletionRetryAt = 0;
                _board.ReleaseResources(node, o.Owner);
                if (node.State != NodeState.Done && !WorkBoard.HasSpent(node)) node.Choices = Array.Empty<WorkRequirementChoice>();
                if (node.State != NodeState.Done)
                    node.State = state == OrderState.Cancelled ? NodeState.Cancelled : NodeState.Failed;
            }
            Entity target = world.Entities.Get(o.Target);
            // Publish terminal intent together with its sole execution before any further provider observation.
            _board.SettlePlanExecution(o);
            if (o.Plan.AutomaticSupply) _supply?.WithdrawInputs(o.Id);
            if (state == OrderState.Done)
            {
                world.Notify(NoteOrderDone, o.Owner, o.Target, o.PlanKey);
            }
            if (state != OrderState.Done)
            {
                if (state == OrderState.Failed) world.Notify(NoteOrderFailed, o.Owner, o.Target, o.PlanKey);
                if (state == OrderState.Failed && !o.Blocker.IsNone) world.Notify(o.Blocker, o.Owner, o.Target, o.PlanKey);
                target?.Get<IWorkCancellationTarget>()?.OnWorkCancelled(world, o);
            }
            _board.Dirty = true;
        }

        public void Cancel(World world, Order o) => EndOrder(world, o, OrderState.Cancelled);

        private bool FailStaleResponse(World world, Order order)
        {
            Code refusal = ResponseWork.CheckActiveOrder(world, world.Entities.Get(order.Target), order);
            if (refusal.IsNone) return false;
            order.Blocker = refusal;
            EndOrder(world, order, OrderState.Failed);
            return true;
        }

        // ------------------------------------------------------------------ jobs

        private void RunJob(World world, Job j)
        {
            if (j.OrderId != 0 && _board.Order(j.OrderId) is Order order && FailStaleResponse(world, order)) return;
            Entity worker = world.Entities.Get(j.Worker);
            if (worker == null)
            { Reopen(world, j, false); return; }
            Code planBlocker = _board.PlanOrderBlocker(j.OrderId);
            if (!planBlocker.IsNone) { PausePolicy(world, worker, j, planBlocker); return; }
            if (j.ToilIndex >= j.Toils.Count) { Finish(world, j); return; }
            Code policy = j.IsHaul && j.Loaded.IsPositive
                ? world.Services.Get<IHaulExecution>().CheckDestinationBinding(j) : Code.None;
            // The same admitted access-loss cause already owns physical egress. It is not a
            // new failed service request; current worker/dock/motion gates still govern movement.
            // A different loss (including removed geometry) still requires its own admission.
            if (policy == HaulRecoveryCodes.DestinationBindingLost && j.RecoveryCause == policy)
                policy = Code.None;
            if (policy.IsNone) policy = ReplanJobDocks(world, worker, j);
            // A retained dock recovery has no service stops. Loss still needs explicit recovery
            // admission before executing a toil against geometry that no longer exists.
            if (policy.IsNone && j.IsHaul && world.Entities.Get(j.To) == null) policy = Codes.NotFound;
            if (policy.IsNone) policy = CheckJob(world, worker, j);
            if (HoldTrip(world, worker, j, policy)) return;
            j.TripHeld = false;
            if (!policy.IsNone && j.IsHaul && world.Services.Get<IHaulExecution>().TryReplanDestination(world, j, policy)) return;
            if (WorkPolicyCodes.IsWait(policy) || WorkDockCodes.IsWait(policy) || PlanCodes.IsWait(policy)) { PausePolicy(world, worker, j, policy); return; }
            if (!policy.IsNone)
            {
                if (j.IsHaul && (policy == BlockerWorkerUnavailable || _board.RetryPolicy.CanRetry(policy)))
                {
                    if (j.RetryAt <= world.Tick) DeferHaul(world, j, policy);
                    else { j.State = JobState.Paused; worker.Get<IMotion>()?.Stop(); }
                }
                else if (policy == Codes.NotFound) Release(world, j, JobState.Cancelled);
                else Reopen(world, j, false);
                return;
            }
            if (j.IsHaul && j.RetryAt > world.Tick)
            { j.State = JobState.Paused; worker.Get<IMotion>()?.Stop(); return; }
            bool transferWait = j.IsHaul && HaulHandlingCodes.IsWait(j.Blocker);
            if (j.IsHaul) j.RetryAt = 0;
            j.Blocker = Code.None;
            if (j.State == JobState.Paused) j.State = JobState.Assigned;
            if (j.IsHaul) _board.ReleaseDepartedDock(j, worker);
            Toil t = j.Toils[j.ToilIndex];
            Entity at = world.Entities.Get(t.At);
            if (at == null) { Release(world, j, JobState.Cancelled); return; }
            ExecutePhase(world, j, worker, at, transferWait);
        }

        internal void ExecuteTravel(WorkPhaseContext context)
        {
            World world = context.World; Job j = context.Job; Entity worker = context.Worker, at = context.Target;
            SpatialInteractionStatus approach = _board.ApproachDock(j, worker, at);
            if (j.Blocker.IsNone && worker.Get<IMotion>() != null)
                j.Blocker = MotionAreaContracts.Read(world, worker).Blocker;
            if (WorkPolicyCodes.IsWait(j.Blocker)) { PausePolicy(world, worker, j, j.Blocker); return; }
            if (!j.IsHaul && _board.Order(j.OrderId)?.Node(j.NodeId) is ActivityNodeExecution approaching) approaching.Blocker = j.Blocker;
            if (approach == SpatialInteractionStatus.WithinReach) { j.Failures = 0; j.ToilIndex++; RunJob(world, j); return; }
            if (!SpatialInteraction.CanApproach(approach))
            {
                if (j.IsHaul) DeferHaul(world, j, !j.Blocker.IsNone ? j.Blocker :
                    approach == SpatialInteractionStatus.Unreachable ? BlockerUnreachable : Codes.InvalidArgument);
                else Reopen(world, j, true);
            }
            else if (j.IsHaul) j.Failures = 0;
            return;
        }

        internal void ExecuteProductive(WorkPhaseContext context)
        {
            World world = context.World; Job j = context.Job; Entity worker = context.Worker, at = context.Target;
            if (_board.CheckDockReach(j, worker, at) != SpatialInteractionStatus.WithinReach)
            { Code wait = _board.ReapproachActivity(j); if (!wait.IsNone) PausePolicy(world, worker, j, wait); return; }
            Order o = _board.Order(j.OrderId);
            if (o == null || !o.IsActive) { Release(world, j, JobState.Cancelled); return; }
            IActivityNodeDefinition definition = o.Plan.Graph.Find(j.NodeId);
            ActivityNodeExecution node = o.Node(j.NodeId);
            WorkRequirementResult readiness = _board.EvaluateRequirements(o.Owner, at, definition, node, null);
            if (readiness.Status != WorkRequirementStatus.Satisfied)
            {
                node.Blocker = readiness.Blocker;
                if (readiness.Blocker == WorkResourceCodes.Lost) { Reopen(world, j, false); return; }
                if (readiness.Status == WorkRequirementStatus.Invalid)
                { node.State = NodeState.Failed; o.Blocker = readiness.Blocker; EndOrder(world, o, OrderState.Failed); }
                else { j.State = JobState.Paused; node.State = NodeState.Paused; }
                return;
            }
            if (_productive == null) throw new InvalidOperationException("productive proposal requires the active work update");
            _productive.Add((o.Id, node.Id));
        }

        internal void ExecutePickup(WorkPhaseContext context)
        {
            World world = context.World; Job j = context.Job; Entity worker = context.Worker, at = context.Target;
            Toil t = context.Phase; bool transferWait = context.TransferWait;
            if (_board.CheckDockReach(j, worker, at) != SpatialInteractionStatus.WithinReach) { j.ToilIndex--; return; }
            if (!transferWait && !AdvanceHandling(world, j, worker)) return;
            ResourceTransferResult pickup = world.Services.Get<IHaulExecution>().Load(world, j, worker, at, t.Resource, t.Amount);
            if (!AcceptHandling(world, j, pickup)) return;
            if (j.Loaded != j.Amount) return;
            j.HandlingProgress = WorkProgress.Zero;
            j.ToilIndex++;
            return;
        }

        internal void ExecuteDelivery(WorkPhaseContext context)
        {
            World world = context.World; Job j = context.Job; Entity worker = context.Worker, at = context.Target;
            Toil t = context.Phase; bool transferWait = context.TransferWait;
            if (_board.CheckDockReach(j, worker, at) != SpatialInteractionStatus.WithinReach) { j.ToilIndex--; return; }
            if (!transferWait && !AdvanceHandling(world, j, worker)) return;
            ResourceTransferResult delivery = world.Services.Get<IHaulExecution>().Unload(world, j, worker, at, t.Resource);
            if (!AcceptHandling(world, j, delivery)) return;
            if (j.Loaded.IsPositive) return;
            j.HandlingProgress = WorkProgress.Zero;
            j.ToilIndex++;
            if (j.ToilIndex >= j.Toils.Count) Finish(world, j);
            return;
        }

        internal void ExecuteDeparture(WorkPhaseContext context)
        {
            World world = context.World; Job j = context.Job; Entity worker = context.Worker, at = context.Target;
            SpatialInteractionStatus departure = _board.DepartDock(j, worker, at);
            if (j.Blocker.IsNone && worker.Get<IMotion>() != null)
                j.Blocker = MotionAreaContracts.Read(world, worker).Blocker;
            if (WorkPolicyCodes.IsWait(j.Blocker)) { PausePolicy(world, worker, j, j.Blocker); return; }
            if (departure == SpatialInteractionStatus.WithinReach && !j.RecoveryCause.IsNone)
            {
                _board.CompleteRecoveryDeparture(j, worker);
                if (!world.Services.Get<IHaulExecution>().TryReplanDestination(world, j, j.RecoveryCause))
                    PausePolicy(world, worker, j, HaulRecoveryCodes.DestinationUnavailable);
            }
            else if (departure == SpatialInteractionStatus.WithinReach) { j.Failures = 0; j.ToilIndex++; Finish(world, j); }
            else if (!SpatialInteraction.CanApproach(departure))
            {
                if (j.IsHaul) DeferHaul(world, j, !j.Blocker.IsNone ? j.Blocker : departure == SpatialInteractionStatus.Unreachable ? BlockerUnreachable : Codes.InvalidArgument);
                else Reopen(world, j, true);
            }
            else j.Failures = 0;
            return;
        }

        private bool AdvanceHandling(World world, Job job, Entity worker)
        {
            ResourceHandlingCheck handling = _board.ReadHandling(job);
            if (!handling.Ready) { job.Blocker = handling.Blocker; Release(world, job, JobState.Failed); return false; }
            Fix rate = EffectiveWorkRate(world, worker);
            if (!rate.IsPositive) { DeferHaul(world, job, BlockerWorkerUnavailable); return false; }
            job.HandlingProgress = job.HandlingProgress.Advance(rate, handling.RequiredTicks);
            return true;
        }

        private bool AcceptHandling(World world, Job job, ResourceTransferResult result)
        {
            if (result.Status == ResourceTransferStatus.Ready || result.Committed)
            { job.Failures = 0; job.RetryAt = 0; return true; }
            Code wait = HaulHandlingCodes.Wait(result.Status);
            if (!wait.IsNone) { DeferHaul(world, job, wait); return false; }
            Release(world, job, JobState.Failed); return false;
        }

        private void DeferHaul(World world, Job job, Code blocker)
        {
            job.TripHeld = false;
            if (world.Services.Get<IHaulExecution>().TryReplanDestination(world, job, blocker)) return;
            if (!_board.RetryPolicy.CanRetry(blocker))
            { job.Blocker = blocker; Release(world, job, JobState.Failed); return; }
            int failures = checked(job.Failures + 1);
            long deadline = checked(world.Tick + _board.RetryDelay(failures));
            job.Failures = failures; job.RetryAt = deadline; job.Blocker = blocker; job.State = JobState.Paused;
            world.Entities.Get(job.Worker)?.Get<IMotion>()?.Stop();
        }

        private void Finish(World world, Job j)
        {
            if (j.OrderId != 0)
            {
                Order o = _board.Order(j.OrderId);
                ActivityNodeExecution node = o?.Node(j.NodeId);
                if (o != null && o.IsActive && node != null && (node.State == NodeState.Working || node.State == NodeState.Paused))
                {
                    IActivityNodeDefinition definition = o.Plan.Graph.Find(j.NodeId);
                    if (!node.Progress.Complete(world.Time.Ticks(definition.Duration)))
                        throw new InvalidOperationException("activity cannot finish before earning its authored work");
                    CompleteNode(world, o, definition);
                }
            }
            if (j.IsLive) Release(world, j, JobState.Done);
        }

        /// <summary>The player took the machine off its job: the job goes back on the board with its progress.</summary>
        public Code Interrupt(World world, Job j)
        {
            Code refusal = CheckInterruption(world, world.Entities.Get(j.Worker));
            if (!refusal.IsNone) return refusal;
            world.Entities.Get(j.Worker)?.Get<IMotion>()?.Stop();
            Reopen(world, j, false); return Code.None;
        }

        /// <summary>End a complete haul commitment; other work reopens with its retained progress.</summary>
        private void Reopen(World world, Job j, bool failure)
        {
            j.TripHeld = false;
            if (j.IsHaul) { Release(world, j, JobState.Failed); return; }
            FreeWorker(world, j);
            j.State = JobState.Open;
            j.Blocker = Code.None;
            ActivityNodeExecution node = _board.Order(j.OrderId)?.Node(j.NodeId);
            if (node != null)
            {
                _board.ReleaseResources(node, j.Owner);
                if (!WorkBoard.HasSpent(node)) node.Choices = Array.Empty<WorkRequirementChoice>();
            }
            j.ToilIndex = 0;
            if (failure)
            {
                j.Failures = checked(j.Failures + 1);
                // Work on an order waits with bounded backoff; the order retains progress and reports its blocker.
                j.RetryAt = checked(world.Tick + _board.RetryDelay(j.Failures));
            }
            _board.Dirty = true;
        }

        /// <summary>Ends a job, frees its machine and hands back what it had reserved.</summary>
        internal void Release(World world, Job j, JobState state)
        {
            j.TripHeld = false;
            j.RetryAt = 0;
            if (j.IsHaul) world.Services.Get<IHaulExecution>().EndHaul(world, j);
            FreeWorker(world, j);
            j.State = state;
            Order order = _board.Order(j.OrderId);
            ActivityNodeExecution node = order?.Node(j.NodeId);
            if (order?.IsActive == true && node?.JobId == j.Id && state != JobState.Done)
            {
                _board.ReleaseResources(node, order.Owner);
                node.JobId = 0; if (!WorkBoard.HasSpent(node)) node.Choices = Array.Empty<WorkRequirementChoice>();
                if (state == JobState.Failed)
                { node.State = NodeState.Failed; EndOrder(world, order, OrderState.Failed); }
                else node.State = NodeState.Ready;
            }
            _board.Dirty = true;
        }

        private static void FreeWorker(World world, Job j)
        {
            Entity w = world.Entities.Get(j.Worker);
            if (w != null)
            {
                IWorker wk = w.Get<IWorker>();
                if (wk != null && wk.Job == j.Id) wk.Job = 0;
            }
            j.Worker = EntityId.None;
            if (!j.IsHaul) { j.SourceDockSlot = Code.None; j.DestinationDockSlot = Code.None; }
        }

        // ------------------------------------------------------------------ assignment

        private void Assign(World world)
        {
            var offers = new List<WorkerDispatchOffer>();
            var attempted = new Dictionary<(string, long, string), (int Count, int Limit)>();
            while (true)
            {
                CopyAssignments(world, PlayerId.None, offers);
                var active = new List<WorkerDispatchOffer>(); var capacities = new List<int>();
                var edges = new List<IReadOnlyList<EntityId>>(); var candidates = new List<WorkerDispatchOffer>();
                foreach (WorkerDispatchOffer offer in offers)
                {
                    var key = (offer.Scope, offer.Identity, offer.Entry);
                    if (!attempted.TryGetValue(key, out var budget)) budget = (0, offer.AttemptsPerPass);
                    if (budget.Limit != offer.AttemptsPerPass) throw new InvalidOperationException("dispatch attempt budget changed within one pass");
                    if (budget.Count >= budget.Limit) continue;
                    var compatible = new List<EntityId>();
                    if (offer.Ready && offer.RequiresWorker)
                    {
                        CopyAssignmentCandidates(world, offer, candidates);
                        foreach (WorkerDispatchOffer candidate in candidates) compatible.Add(candidate.Worker);
                    }
                    active.Add(offer); capacities.Add(offer.RequiresWorker ? 1 : 0); edges.Add(compatible.AsReadOnly());
                }
                var selections = new Dictionary<int, (WorkWorkerCandidate Candidate, Code? Dock)>();
                bool joint = MatchNativeFrontier(world, active, capacities, edges, selections, out var matches, out var nativeFrontier);
                var selected = new Dictionary<int, EntityId>();
                foreach (DistinctWorkerMatch match in matches) selected.Add(match.Demand, match.Worker);
                bool changed = false;
                for (int i = 0; i < active.Count; i++)
                {
                    WorkerDispatchOffer offer = active[i];
                    var key = (offer.Scope, offer.Identity, offer.Entry);
                    if (!attempted.TryGetValue(key, out var budget)) budget = (0, offer.AttemptsPerPass);
                    if (budget.Limit != offer.AttemptsPerPass) throw new InvalidOperationException("dispatch attempt budget changed within one pass");
                    if (budget.Count >= budget.Limit) continue;
                    if (offer.Ready && offer.RequiresWorker && !selected.ContainsKey(i)) continue;
                    attempted[key] = (checked(budget.Count + 1), budget.Limit);
                    selected.TryGetValue(i, out EntityId worker);
                    bool hasSelection = selections.TryGetValue(i, out var selection);
                    if (joint && nativeFrontier[0] == i && offer.Scope == WorkModule.ModuleId && (hasSelection || !offer.RequiresWorker && offer.Ready))
                    {
                        var jobIds = new List<int>(); var team = new List<WorkWorkerCandidate>(); var bays = new List<Code?>();
                        var timers = new List<(int Order, string Node)>(); var members = new List<int>();
                        foreach (int j in nativeFrontier)
                        {
                            if (selections.TryGetValue(j, out var member))
                            { jobIds.Add(checked((int)active[j].Identity)); team.Add(member.Candidate); bays.Add(member.Dock); members.Add(j); }
                            else if (!active[j].RequiresWorker && active[j].Ready)
                            { timers.Add((checked((int)active[j].Identity), TimedNode(active[j]))); members.Add(j); }
                            else throw new InvalidOperationException("native frontier lost its actual activity selection");
                        }
                        if (jobIds.Count + timers.Count > 1)
                        {
                            foreach (int j in members)
                                if (j != i)
                                {
                                    var memberKey = (active[j].Scope, active[j].Identity, active[j].Entry);
                                    if (!attempted.TryGetValue(memberKey, out var memberBudget)) memberBudget = (0, active[j].AttemptsPerPass);
                                    attempted[memberKey] = (checked(memberBudget.Count + 1), memberBudget.Limit);
                                }
                            changed = _board.Assign(jobIds.AsReadOnly(), team.AsReadOnly(), bays.AsReadOnly(), timers.AsReadOnly());
                            if (changed) break;
                            // A whole-set ordinary refusal must not fall through to a prefix acquisition.
                            continue;
                        }
                    }
                    if (offer.Scope == WorkModule.ModuleId && !offer.RequiresWorker)
                    {
                        Order order = _board.Order(checked((int)offer.Identity));
                        if (order != null)
                        {
                            changed = _board.StartWorkerless(order, Array.AsReadOnly(new[] { TimedNode(offer) }), out Code blocker);
                            ActivityNodeExecution node = order.Node(TimedNode(offer));
                            if (!changed && node != null) { node.Blocker = blocker; SetBlocker(order, node); }
                        }
                    }
                    else if (offer.Scope == WorkModule.ModuleId) changed = TryAssign(world, checked((int)offer.Identity), worker,
                        hasSelection ? selection.Candidate : (WorkWorkerCandidate?)null, hasSelection ? selection.Dock : null);
                    else if (offer.Ready)
                        foreach (IWorkerDispatchSource source in DispatchSources(world))
                            if (source.DispatchScope == offer.Scope) { changed = source.TryAcquire(offer.Identity, offer.Entry, worker, WorkCandidateMode.EmergencyInterruption); break; }
                    if (changed) break; // Current claims invalidate every remaining candidate and stock/capacity offer.
                }
                if (!changed) return;
                // Actual acquisition changes material holds as well as machine availability. Refresh
                // existing supply obligations before rebuilding mixed work/haul offers in this pass.
                RefreshSupplies(world);
            }
        }

        private bool TryAssign(World world, int jobId, EntityId selectedWorker, WorkWorkerCandidate? selection, Code? selectedDock)
        {
            Job job = _board.Job(jobId);
            if (job == null || job.State != JobState.Open || job.RetryAt > world.Tick) return false;
            Entity target = world.Entities.Get(job.Target);
            if (target == null) { Release(world, job, JobState.Cancelled); return true; }
            Order order = _board.Order(job.OrderId);
            if (order != null && FailStaleResponse(world, order)) return true;
            IActivityNodeDefinition node = order?.Plan.Graph.Find(job.NodeId);
            ActivityNodeExecution execution = order?.Node(job.NodeId); var candidates = new List<WorkWorkerCandidate>();
            var choices = execution != null && WorkBoard.HasSpent(execution) ? execution.Choices : selection?.Result.Choices;
            CopyCandidates(world, job.Owner, node?.Roles ?? Array.AsReadOnly(new[] { job.Role }), target, node, job.Id,
                choices, execution, WorkCandidateMode.EmergencyInterruption, candidates, null);
            if (candidates.Count == 0 || candidates[0].Result.Status != WorkRequirementStatus.Satisfied)
            {
                job.Blocker = candidates.Count == 0 ? BlockerNoWorker : candidates[0].Result.Blocker;
                if (execution != null) { execution.Blocker = job.Blocker; order.Blocker = job.Blocker; }
                return false;
            }
            int chosen = candidates.FindIndex(candidate => candidate.Worker == selectedWorker &&
                (!selection.HasValue || candidate.Role == selection.Value.Role) && candidate.Result.Status == WorkRequirementStatus.Satisfied);
            if (chosen < 0) return false;
            job.Blocker = Code.None;
            bool preempted = candidates[chosen].InterruptedJob != 0;
            if (preempted)
            {
                InterruptForDispatch(world, Assignment(new WorkDispatchOffer(job, order, world.Tick, candidates[chosen],
                    WorkRequirementStatus.Satisfied, Code.None, _board.IsEmergency(job)), _board, world.Tick));
                CopyCandidates(world, job.Owner, node.Roles, target, node, job.Id,
                    choices, execution, WorkCandidateMode.IdleOnly, candidates, null);
                chosen = candidates.FindIndex(candidate => candidate.Worker == selectedWorker &&
                    (!selection.HasValue || candidate.Role == selection.Value.Role) && candidate.Result.Status == WorkRequirementStatus.Satisfied);
                if (chosen < 0)
                    throw new InvalidOperationException("emergency interruption could not reacquire a real idle worker");
            }
            bool assigned = _board.Assign(job, candidates[chosen], selectedDock, selection?.Result.Resources);
            if (preempted && !assigned) throw new InvalidOperationException("emergency interruption lost native admission");
            return assigned;
        }

        // ------------------------------------------------------------------ idle machines

        private void IdleBehaviour(World world)
        {
            ComponentList<IWorker> workers = world.Entities.Components<IWorker>();
            for (int w = 0; w < workers.Count; w++)
            {
                IWorker wk = workers[w];
                Entity e = wk.Entity;
                if (!e.Alive || !wk.Idle) continue;
                if (!WorkerExecutionContracts.Read(world, e).Gates.Blocker.IsNone) continue;
                IMotion m = e.Get<IMotion>();
                if (wk.Held)
                {
                    if (m == null) continue;
                    if (!wk.HeldArrived && (m.Status == MoveStatus.Arrived || m.Status == MoveStatus.Unreachable))
                    {
                        wk.HeldArrived = true;
                        world.Notify(m.Status == MoveStatus.Arrived ? NoteArrived : NoteStranded, e.Owner, e.Id);
                    }
                    continue;
                }
                WorkerHomeContracts.UpdateIdle(world, e);
            }
        }
    }
}
