using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum RecurringCadenceBasis : byte { Submission = 0, SuccessfulCompletion = 1 }
    public enum RecurringTriggerMode : byte { Schedule = 0, Maintenance = 1, ScheduleOrMaintenance = 2 }
    public enum RecurringWorkerDispatchMode : byte { Compete = 0, ServiceFirst = 1 }
    public sealed class RecurringPlanSettings
    {
        public bool Enabled { get; }
        public int Priority { get; }
        public GameDuration Interval { get; }
        public Code FacilityPolicy { get; }
        public RecurringPlanSettings(bool enabled, int priority, GameDuration interval, Code facilityPolicy)
        {
            if (priority < 0 || interval.IsZero) throw new ArgumentException("recurring settings require nonnegative priority and positive interval");
            if (!facilityPolicy.IsNone) ContractIdentity.ValidateId(facilityPolicy.Id, "facility policy");
            Enabled = enabled; Priority = priority; Interval = interval; FacilityPolicy = facilityPolicy;
        }
        internal bool Matches(RecurringPlanSettings other) => other != null && Enabled == other.Enabled && Priority == other.Priority &&
            Interval.Literal == other.Interval.Literal && Interval.Scale == other.Interval.Scale && Interval.Divisor == other.Interval.Divisor && FacilityPolicy == other.FacilityPolicy;
        public static RecurringPlanSettings Read(JsonReader reader)
        {
            bool enabled = reader.Bool("enabled"); int priority = reader.Int("priority");
            GameDuration interval = reader.Child("interval")?.RationalDuration() ?? throw reader.Error("explicit recurring interval required");
            JsonValue policy = reader.Value("facility_policy");
            if (policy == null || policy.Kind != JsonKind.Null && policy.Kind != JsonKind.String) throw reader.Error("explicit facility_policy:null or named identity required");
            try { return new RecurringPlanSettings(enabled, priority, interval, policy.Kind == JsonKind.Null ? Code.None : new Code(policy.Text)); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        public static RecurringPlanSettings Sync(IStateSync sync, RecurringPlanSettings current)
        {
            bool present = current != null; sync.Bool(ref present); if (!present) return null;
            bool enabled = current?.Enabled ?? false; int priority = current?.Priority ?? 0;
            sync.Bool(ref enabled); sync.Int(ref priority); GameDuration interval = GameDuration.Sync(sync, current?.Interval ?? GameDuration.Zero);
            string policy = current?.FacilityPolicy.Id; sync.String(ref policy);
            if (!sync.Reading) return current;
            try { return new RecurringPlanSettings(enabled, priority, interval, policy == null ? Code.None : new Code(policy)); }
            catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
        }
    }
    public interface IRecurringPlanDefinition : INamedCapabilityDefinition
    {
        RecurringCadenceBasis Cadence { get; }
        RecurringTriggerMode TriggerMode { get; }
        RecurringWorkerDispatchMode WorkerDispatch { get; }
        string PlanKey { get; }
        IPlanTemplateContent Template { get; }
        RecurringPlanSettings Initial { get; }
    }
    public interface IRecurringPlanBindingDefinition : IRecurringPlanDefinition
    {
        RecurringPlanBinding Binding { get; }
        string Group { get; }
    }
    /// <summary>A pure request is either actual intent or an explicit typed refusal, never a fake target.</summary>
    public readonly struct RecurringPlanRequest
    {
        public PlanIntent Intent { get; }
        public PlanGroupIntent GroupIntent { get; }
        public Code Blocker { get; }
        public bool IsWellFormed => (Intent != null || GroupIntent != null) ? Blocker.IsNone && !(Intent != null && GroupIntent != null) : !Blocker.IsNone;
        private RecurringPlanRequest(PlanIntent intent, PlanGroupIntent group, Code blocker) { Intent = intent; GroupIntent = group; Blocker = blocker; }
        public static RecurringPlanRequest Ready(PlanIntent intent)
            => new RecurringPlanRequest(intent ?? throw new ArgumentNullException(nameof(intent)), null, Code.None);
        public static RecurringPlanRequest Ready(PlanGroupIntent group)
            => new RecurringPlanRequest(null, group ?? throw new ArgumentNullException(nameof(group)), Code.None);
        public static RecurringPlanRequest Refused(Code blocker)
        {
            if (blocker.IsNone) throw new ArgumentException("a refused recurring request needs a typed blocker");
            ContractIdentity.ValidateId(blocker.Id, "recurring request blocker"); return new RecurringPlanRequest(null, null, blocker);
        }
    }
    public enum RecurringPlanTarget : byte { Self = 0, WorkerHome = 1, ServiceFacility = 2, AuthoredIntent = 3, Template = 4 }
    public enum RecurringReserveUse : byte { None = 0, SelfService = 1 }
    /// <summary>Immutable authored binding; the actual worker owns home, and the existing plan owns execution.</summary>
    public sealed class RecurringPlanBinding
    {
        public RecurringPlanTarget Target { get; }
        public IReadOnlyList<string> WorkerNodes { get; }
        public RecurringReserveUse ReserveUse { get; }
        public JsonValue Arguments { get; }
        public RecurringPlanBinding(RecurringPlanTarget target, IEnumerable<string> workerNodes, RecurringReserveUse reserveUse, JsonValue arguments)
        {
            if (target != RecurringPlanTarget.Self && target != RecurringPlanTarget.WorkerHome && target != RecurringPlanTarget.ServiceFacility && target != RecurringPlanTarget.AuthoredIntent && target != RecurringPlanTarget.Template)
                throw new ArgumentException("invalid recurring target");
            if (reserveUse != RecurringReserveUse.None && reserveUse != RecurringReserveUse.SelfService) throw new ArgumentException("invalid recurring reserve use");
            var nodes = workerNodes == null ? throw new ArgumentNullException(nameof(workerNodes)) : new List<string>(workerNodes);
            nodes.Sort(StringComparer.Ordinal);
            for (int i = 0; i < nodes.Count; i++)
            {
                ContractIdentity.ValidateId(nodes[i], "recurring worker node");
                if (i > 0 && nodes[i - 1] == nodes[i]) throw new ArgumentException("duplicate recurring worker node");
            }
            if (target == RecurringPlanTarget.AuthoredIntent || target == RecurringPlanTarget.Template)
            {
                if (arguments?.Kind != JsonKind.Object || nodes.Count != 0 || reserveUse != RecurringReserveUse.None)
                    throw new ArgumentException("authored intent requires an argument object, no source worker bindings and no reserve exemption");
            }
            else
            {
                if (arguments != null) throw new ArgumentException("entity targeting requires arguments:null");
                if ((target != RecurringPlanTarget.Self || reserveUse == RecurringReserveUse.SelfService) && nodes.Count == 0)
                    throw new ArgumentException("worker target/reserve self-service requires explicit machine nodes");
            }
            Target = target; WorkerNodes = nodes.AsReadOnly(); ReserveUse = reserveUse;
            Arguments = arguments?.Clone(); Arguments?.Freeze();
        }
        public static RecurringPlanBinding Read(JsonReader reader)
        {
            string target = reader.String("target"); var rows = reader.Array("worker_nodes") ?? throw reader.Error("explicit worker_nodes array required");
            string reserve = reader.String("reserve_use");
            if (reserve != "none" && reserve != "self_service") throw reader.Error("reserve_use must be none or self_service");
            if (target != "self" && target != "worker_home" && target != "service_facility" && target != "authored_intent" && target != "template")
                throw reader.Error("target must be self, worker_home, service_facility, authored_intent or template");
            JsonValue arguments = reader.Value("arguments") ?? throw reader.Error("explicit arguments object or null required");
            var nodes = new List<string>();
            for (int i = 0; i < rows.Count; i++)
            { var row = rows[i]; if (row.Kind != JsonKind.String) throw reader.Error("worker_nodes requires node identities"); nodes.Add(row.Text); }
            try { return new RecurringPlanBinding(target == "self" ? RecurringPlanTarget.Self : target == "worker_home" ? RecurringPlanTarget.WorkerHome :
                target == "service_facility" ? RecurringPlanTarget.ServiceFacility : target == "template" ? RecurringPlanTarget.Template : RecurringPlanTarget.AuthoredIntent,
                nodes, reserve == "none" ? RecurringReserveUse.None : RecurringReserveUse.SelfService, arguments.Kind == JsonKind.Null ? null : arguments); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        public static string ReadGroup(JsonReader reader)
        {
            JsonValue group = reader.Value("group");
            if (group == null || group.Kind != JsonKind.Null && group.Kind != JsonKind.String)
                throw reader.Error("explicit group:null/identity required");
            string value = group.Kind == JsonKind.Null ? null : group.Text;
            try { if (value != null) ContractIdentity.ValidateId(value, "recurring group"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            return value;
        }
        public static void ReadSelection(JsonReader reader, out string plan, out string template)
        {
            string Read(string key)
            {
                var value = reader.Value(key);
                if (value == null || value.Kind != JsonKind.Null && value.Kind != JsonKind.String || value.Kind == JsonKind.String && string.IsNullOrWhiteSpace(value.Text))
                    throw reader.Error("explicit " + key + " identity or null required");
                return value.Kind == JsonKind.Null ? null : value.Text;
            }
            plan = Read("plan"); template = Read("template");
            if ((plan == null) == (template == null)) throw reader.Error("select exactly one plan or template");
            if (template != null)
                try { ContractIdentity.ValidateId(template, "recurring template"); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        public static string CycleGroup(IRecurringPlanSource source, string group, RecurringPlanSchedule schedule)
        {
            ContractIdentity.ValidateId(group, "recurring template group");
            return group + "." + source.Entity.Id.Value.ToString(CultureInfo.InvariantCulture) + "." + source.CapabilityId.Id + "." + schedule.LastPlan.ToString(CultureInfo.InvariantCulture);
        }
        public void Validate(EntityDef owner, IActivityGraph graph)
        {
            if ((Target == RecurringPlanTarget.WorkerHome || Target == RecurringPlanTarget.ServiceFacility || WorkerNodes.Count != 0) && !owner.HasCapability<IWorker>())
                throw new ContentException(owner.Id + ": recurring machine binding requires a worker capability");
            foreach (string node in WorkerNodes) ActivityWorkerSelection.ValidateMachineNode(graph, node);
        }
        public ActivityWorkerSelection SelectWorkers(EntityId worker)
        {
            var bindings = new List<ActivityWorkerBinding>(); foreach (string node in WorkerNodes) bindings.Add(new ActivityWorkerBinding(node, worker));
            return new ActivityWorkerSelection(bindings);
        }
        public RecurringPlanRequest ReadRequest(World world, IRecurringPlanSource source, string group, int priority)
        {
            Entity entity = source.Entity; EntityId target = entity.Id;
            var definition = entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId);
            if (Target == RecurringPlanTarget.Template)
            {
                RecurringTemplateSettings settings = RecurringPlanContracts.ReadTemplateSettings(world, source);
                IPlanBoard board = world.Services.Get<IPlanBoard>();
                Code resolved = board.ResolvePlanTemplate(entity.Owner, settings.Selection, out PlanTemplate template);
                if (!resolved.IsNone) return RecurringPlanRequest.Refused(resolved);
                Code range = RecurringPlanContracts.CheckTemplatePriority(template, priority, 0);
                if (!range.IsNone) return RecurringPlanRequest.Refused(range);
                string cycle = CycleGroup(source, settings.Group, RecurringPlanContracts.ReadSchedule(world, source));
                var check = board.ExpandPlanTemplate(entity.Owner, template, settings.Anchor, cycle, out var expanded);
                if (expanded == null) return RecurringPlanRequest.Refused(check.Blocker);
                var entries = new List<PlanIntent>();
                foreach (var entry in expanded.Entries) entries.Add(new PlanIntent(entry.Definition, entry.Arguments, cycle, checked(priority + entry.Priority), entry.Paused, entry.Dependencies));
                return RecurringPlanRequest.Ready(new PlanGroupIntent(cycle, entries, expanded.Dependencies));
            }
            var plan = RecurringPlanContracts.ResolvePlan(world, definition);
            if (Target == RecurringPlanTarget.AuthoredIntent)
                return RecurringPlanRequest.Ready(new PlanIntent(plan.PlanKey, Arguments, group, priority, false));
            if (Target == RecurringPlanTarget.ServiceFacility)
                return world.Services.Get<IActivityFacilitySelection>().ReadFacilities(source).Request;
            if (Target == RecurringPlanTarget.WorkerHome)
            {
                target = WorkerHomeContracts.ReadHome(world, entity);
                if (!target.IsValid) return RecurringPlanRequest.Refused(RecurringPlanCodes.NoTarget);
                Code refusal = WorkerHomeContracts.CheckBinding(world, entity, target);
                if (!refusal.IsNone) return RecurringPlanRequest.Refused(refusal);
            }
            return RecurringPlanRequest.Ready(((IEntityTargetPlanDefinition)plan).CreateTargetIntent(target, SelectWorkers(entity.Id), group, priority));
        }
    }
    public interface IRecurringPlanSource : INamedEntityCapability
    {
        RecurringPlanSchedule ReadSchedule();
        RecurringPlanRequest ReadRequest(World world);
        Code CheckTrigger(World world);
        void Configure(World world, RecurringPlanSettings settings);
        void RecordSubmission(World world, long plan);
        void RecordCompletion(World world, long plan);
    }
    public readonly struct RecurringPlanSchedule
    {
        public RecurringPlanSettings Settings { get; }
        public bool Enabled => Settings.Enabled;
        public int Priority => Settings.Priority;
        public GameDuration Interval => Settings.Interval;
        public long LastPostedTick { get; }
        public long LastPlan { get; }
        public long LastCompletedPlan { get; }
        public RecurringPlanSchedule(RecurringPlanSettings settings, long lastPostedTick, long lastPlan, long lastCompletedPlan)
        { Settings = settings; LastPostedTick = lastPostedTick; LastPlan = lastPlan; LastCompletedPlan = lastCompletedPlan; }
    }
    public readonly struct RecurringPlanView
    {
        public EntityId Entity { get; }
        public Code Source { get; }
        public PlanIntent Intent { get; }
        public PlanGroupIntent GroupIntent { get; }
        public PlanGroupCheck GroupAdmission { get; }
        public RecurringPlanSchedule Schedule { get; }
        public RecurringTemplateSettings TemplateSettings { get; }
        public long? DueTick { get; }
        public GameDuration? DueGameTime { get; }
        /// <summary>Actual completed-service deadline, distinct from retry admission. Null for submission cadence.</summary>
        public long? ServiceDueTick { get; }
        public GameDuration? ServiceDueGameTime { get; }
        public RecurringCadenceBasis Cadence { get; }
        public RecurringTriggerMode TriggerMode { get; }
        public RecurringWorkerDispatchMode WorkerDispatch { get; }
        public bool MaintenanceDue { get; }
        public long? LastCompletedTick { get; }
        public Code Blocker { get; }
        public Code RequestBlocker { get; }
        internal long ObservedTick { get; }
        internal RecurringPlanView(EntityId entity, Code source, RecurringPlanRequest request, RecurringPlanSchedule schedule, long? due, Code blocker, World world, PlanGroupCheck admission, RecurringCadenceBasis cadence, long? completed, RecurringTriggerMode trigger, bool maintenance, long? serviceDue)
        { Entity = entity; Source = source; Intent = request.Intent; GroupIntent = request.GroupIntent; GroupAdmission = admission;
            RequestBlocker = request.Blocker; Schedule = schedule; DueTick = due; Blocker = blocker; ObservedTick = world.Clock.StepIndex;
            Cadence = cadence; LastCompletedTick = completed;
            TriggerMode = trigger; MaintenanceDue = maintenance;
            WorkerDispatch = world.Entities.Get(entity).Def.Component<IRecurringPlanDefinition>(source).WorkerDispatch;
            TemplateSettings = RecurringPlanContracts.ReadTemplateSettings(world, world.Entities.Get(entity).Get<IRecurringPlanSource>(source));
            DueGameTime = due.HasValue ? GameDuration.FromSteps(due.Value, world.Clock.Time) : (GameDuration?)null;
            ServiceDueTick = serviceDue;
            ServiceDueGameTime = serviceDue.HasValue ? GameDuration.FromSteps(serviceDue.Value, world.Clock.Time) : (GameDuration?)null; }
    }
    public static class RecurringPlanCodes
    {
        public static readonly Code NotSource = new Code("work.not-recurring-source"), Disabled = new Code("work.recurring-disabled"),
            Cadence = new Code("work.recurring-cadence"), TimeRange = new Code("work.recurring-time-range"),
            Interval = new Code("work.recurring-invalid-interval"), NoTarget = new Code("work.recurring-no-target"),
            RequestChanged = new Code("work.recurring-request-changed"), Outstanding = new Code("work.recurring-outstanding"),
            CompletionPending = new Code("work.recurring-completion-pending");
        public static readonly Code ServicePending = new Code("work.recurring-service-pending");
        public static readonly Code MovementWindow = new Code("work.recurring-movement-window");
        public static readonly Code NotTemplate = new Code("work.recurring-not-template");
    }
    /// <summary>One control/admission protocol. Posting timestamps derive only from the session's GameClock.</summary>
    public static partial class RecurringPlanContracts
    {
        // Disclosure follows the existing captured activity graph/current template, never a second knowledge store.
        internal static bool CanDisclose(World world, PlayerId viewer, IRecurringPlanSource source)
        {
            world.RequireHealthyUpdate();
            if (source == null) return false;
            Entity entity = Actual(world, source);
            if (!world.ViewFor(viewer).CanObserveOwned(entity)) return false;
            if (viewer == PlayerId.None) return true;
            using var call = EntityObservation.EnterProviderCall(world, source, "work.recurring.disclosure");
            return EntityObservation.ReadProvider(world, source, entity, () => {
                var definition = entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId);
                if (definition == null) throw new InvalidOperationException("recurring source requires its actual definition");
                if (definition.Template == null)
                    return world.Services.Get<IActivityCatalog>().CanDiscloseActivity(viewer, definition.PlanKey);
                var settings = ReadTemplateSettings(world, source);
                var board = world.Services.Get<IPlanBoard>();
                if (settings == null) return false;
                Code resolution = board.ResolvePlanTemplate(viewer, settings.Selection, out var template);
                // The library reports revision conflict only after the actual owned current design passes disclosure.
                // The request remains invalid; do not substitute that newer design into the player's old selection.
                return resolution == PlanCodes.TemplateRevisionConflict || resolution.IsNone && board.CanDisclosePlanTemplate(viewer, template);
            }, space: world.Space, observeTargetState: true);
        }
        internal static bool CanDisclose(World world, PlayerId viewer, IReadOnlyList<(EntityId Entity, Code Source)> inputs)
        {
            world.RequireHealthyUpdate();
            if (inputs != null) foreach (var input in inputs)
                if (!CanDisclose(world, viewer, world.Entities.Get(input.Entity)?.Get<IRecurringPlanSource>(input.Source))) return false;
            return true;
        }
        internal static Code CheckPlayerConfigure(World world, PlayerId player, EntityId id, Code source, RecurringPlanSettings settings)
        {
            Code refusal = CheckPlayerSource(world, player, id, source, out var provider);
            if (!refusal.IsNone) return refusal;
            // Safe disabling and known template replacement must not require disclosure of broken/hidden old intent.
            if (settings != null && settings.Enabled && !CanDisclose(world, player, provider)) return Codes.NotFound;
            return CheckConfigure(world, provider, settings);
        }
        private static Code CheckPlayerSource(World world, PlayerId player, EntityId id, Code source, out IRecurringPlanSource provider)
        {
            provider = null;
            world.RequireHealthyUpdate(); Entity entity = world.Entities.Get(id);
            if (entity == null) return Codes.NotFound;
            if (entity.Owner != player) return Codes.NotOwned;
            if (!world.ViewFor(player).CanObserveOwned(entity)) return Codes.NotFound;
            if (source.IsNone) return Codes.InvalidArgument;
            provider = entity.Get<IRecurringPlanSource>(source);
            return provider == null ? RecurringPlanCodes.NotSource : Code.None;
        }
        /// <summary>The existing board is the sole plan resolver for every source and plan kind.</summary>
        public static IPlanDefinition ResolvePlan(World world, IRecurringPlanDefinition definition)
        {
            var plan = world.Services.Get<WorkBoard>().ResolvePlan(definition?.PlanKey) as IPlanDefinition;
            if (plan == null || plan.PlanKey != definition.PlanKey)
                throw new ContentException("recurring source requires its registered intent plan: " + definition?.PlanKey);
            return plan;
        }
        public static bool IsStructurallyValid(RecurringPlanSchedule value) => value.Settings != null && value.Priority >= 0 && !value.Interval.IsZero && value.LastPostedTick >= -1 &&
            value.LastPlan >= 0 && value.LastCompletedPlan >= 0 && value.LastCompletedPlan <= value.LastPlan &&
            (value.LastPlan == 0) == (value.LastPostedTick == -1);
        internal static bool Same(RecurringPlanSchedule a, RecurringPlanSchedule b) => a.Settings.Matches(b.Settings) &&
            a.LastPostedTick == b.LastPostedTick && a.LastPlan == b.LastPlan && a.LastCompletedPlan == b.LastCompletedPlan;
        public static RecurringCadenceBasis ReadCadence(JsonReader reader)
        {
            string value = reader.String("cadence");
            if (value != "submission" && value != "successful_completion") throw reader.Error("explicit submission/successful_completion cadence required");
            return value == "submission" ? RecurringCadenceBasis.Submission : RecurringCadenceBasis.SuccessfulCompletion;
        }
        public static RecurringTriggerMode ReadTriggerMode(JsonReader reader)
        {
            string value = reader.String("trigger");
            if (value != "schedule" && value != "maintenance" && value != "schedule_or_maintenance")
                throw reader.Error("explicit schedule/maintenance/schedule_or_maintenance trigger required");
            return value == "schedule" ? RecurringTriggerMode.Schedule : value == "maintenance" ? RecurringTriggerMode.Maintenance : RecurringTriggerMode.ScheduleOrMaintenance;
        }
        public static RecurringWorkerDispatchMode ReadWorkerDispatch(JsonReader reader)
        {
            string value = reader.String("dispatch_mode");
            if (value != "compete" && value != "service_first") throw reader.Error("explicit compete/service_first dispatch_mode required");
            return value == "compete" ? RecurringWorkerDispatchMode.Compete : RecurringWorkerDispatchMode.ServiceFirst;
        }
        public static Code ReadOperationPolicy(JsonReader reader)
        {
            JsonValue value = reader.Value("operation_policy");
            if (value == null || value.Kind != JsonKind.Null && value.Kind != JsonKind.String)
                throw reader.Error("explicit operation_policy:null or named policy required");
            if (value.Kind == JsonKind.Null) return Code.None;
            try { ContractIdentity.ValidateId(value.Text, "recurring operation policy"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            return new Code(value.Text);
        }
        private static Entity Actual(World world, IRecurringPlanSource source)
        {
            Entity entity = source?.Entity;
            if (world == null || entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                !ReferenceEquals(entity.Get<IRecurringPlanSource>(source.CapabilityId), source))
                throw new ArgumentException("recurring source must be the actual named live provider");
            return entity;
        }
        public static RecurringPlanSchedule ReadSchedule(World world, IRecurringPlanSource source)
            => EntityObservation.ReadProvider(world, source, null, () => {
            Entity entity = Actual(world, source); RecurringPlanSchedule value = source.ReadSchedule();
            var definition = entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId);
            if (definition is IRecurringActivitySourceDefinition reusable)
            {
                RecurringActivityBindingContracts.Observe(world, reusable, entity, _ => true);
            }
            if (!IsStructurallyValid(value) || value.LastPostedTick > world.Clock.StepIndex || !CheckInterval(world, value.Interval).IsNone ||
                definition == null || (byte)definition.Cadence > (byte)RecurringCadenceBasis.SuccessfulCompletion ||
                (byte)definition.TriggerMode > (byte)RecurringTriggerMode.ScheduleOrMaintenance ||
                (byte)definition.WorkerDispatch > (byte)RecurringWorkerDispatchMode.ServiceFirst ||
                definition.Cadence == RecurringCadenceBasis.Submission && value.LastCompletedPlan != 0 ||
                !CheckFacilityPolicy(world, definition, value.Settings.FacilityPolicy, source.Entity).IsNone)
                throw new CorruptStateException("invalid recurring source schedule or session chronology");
            _ = EffectivePriority(world, source, value.Priority);
            return value;
        });
        internal static Code CheckFacilityPolicy(World world, IRecurringPlanDefinition definition, Code profile, Entity worker = null)
        {
            var binding = definition as IRecurringPlanBindingDefinition;
            if (definition?.Template != null)
                return binding?.Binding.Target == RecurringPlanTarget.Template && definition.PlanKey == null
                    ? profile.IsNone ? Code.None : ActivityFacilityCodes.NotSelected : ActivityFacilityCodes.Incompatible;
            var plan = ResolvePlan(world, definition);
            if (binding != null && (binding.Binding == null || binding.Binding.Target == RecurringPlanTarget.Template ||
                binding.Binding.Target != RecurringPlanTarget.AuthoredIntent && !(plan is IEntityTargetPlanDefinition)))
                return ActivityFacilityCodes.Incompatible;
            if (binding?.Binding.Target != RecurringPlanTarget.ServiceFacility)
                return profile.IsNone ? Code.None : ActivityFacilityCodes.NotSelected;
            if (profile.IsNone) return ActivityFacilityCodes.NotSelected;
            var policy = world.Content.Find<IActivityFacilityPolicy>(profile.Id);
            if (policy == null) return ActivityFacilityCodes.Unknown;
            string probe = ActivityFacilityPolicyContracts.Read(world, policy, null, worker, () => policy.Node);
            try
            {
                ContractIdentity.ValidateId(probe, "facility probe node");
                ActivityWorkerSelection.ValidateMachineNode(plan.Graph, probe);
            }
            catch (ArgumentException) { return ActivityFacilityCodes.Incompatible; }
            catch (ContentException) { return ActivityFacilityCodes.Incompatible; }
            foreach (string node in binding.Binding.WorkerNodes) if (node == probe) return Code.None;
            return ActivityFacilityCodes.Incompatible;
        }
        public static int EffectivePriority(World world, IRecurringPlanSource source, int priority)
        {
            Entity entity = Actual(world, source);
            var link = entity.Def.Component<IRecurringOperationPolicyDefinition>(source.CapabilityId);
            int criticality = link == null || link.OperationPolicy.IsNone ? 0 :
                OperationPolicyContracts.Read(world, entity.Get<IOperationPolicy>(link.OperationPolicy)).State.Settings.Criticality;
            if (!CheckTemplatePriority(null, priority, criticality).IsNone)
                throw new InvalidOperationException("recurring priority exceeds actual representation");
            return priority + criticality;
        }
        internal static Code CheckPriority(IRecurringPlanDefinition definition, int priority, int criticality)
            => CheckTemplatePriority(definition?.Template?.Template, priority, criticality);
        private static bool SameIntent(World world, Entity entity, PlanIntent a, PlanIntent b)
        {
            if (a == null || b == null) return a == b;
            if (a.Definition != b.Definition || a.Group != b.Group || a.Priority != b.Priority || a.Paused != b.Paused) return false;
            if (a.Dependencies.Count != b.Dependencies.Count) return false;
            for (int i = 0; i < a.Dependencies.Count; i++) if (a.Dependencies[i] != b.Dependencies[i]) return false;
            var plan = world.Services.Get<WorkBoard>().ResolvePlan(a.Definition) as IPlanDefinition
                ?? throw new InvalidOperationException("recurring entry must resolve an actual intent plan");
            return EntityObservation.Read(world, entity, () => PlanObservation.Identity(world, plan, a.Arguments) == PlanObservation.Identity(world, plan, b.Arguments));
        }
        private static bool SameRequest(World world, Entity entity, RecurringPlanRequest a, RecurringPlanRequest b)
        {
            if (a.Blocker != b.Blocker || !SameIntent(world, entity, a.Intent, b.Intent)) return false;
            if (a.GroupIntent == null || b.GroupIntent == null) return a.GroupIntent == b.GroupIntent;
            if (a.GroupIntent.Group != b.GroupIntent.Group || a.GroupIntent.Entries.Count != b.GroupIntent.Entries.Count) return false;
            for (int i = 0; i < a.GroupIntent.Entries.Count; i++)
                if (!SameIntent(world, entity, a.GroupIntent.Entries[i], b.GroupIntent.Entries[i])) return false;
            if (a.GroupIntent.Dependencies.Count != b.GroupIntent.Dependencies.Count) return false;
            for (int i = 0; i < a.GroupIntent.Dependencies.Count; i++)
                if (a.GroupIntent.Dependencies[i].Entry != b.GroupIntent.Dependencies[i].Entry ||
                    a.GroupIntent.Dependencies[i].Prerequisite != b.GroupIntent.Dependencies[i].Prerequisite) return false;
            return true;
        }
        private static RecurringPlanRequest Request(World world, IRecurringPlanSource source, RecurringPlanSchedule schedule, IRecurringPlanDefinition definition)
        {
            Entity entity = Actual(world, source); var request = EntityObservation.Read(world, entity, () => source.ReadRequest(world));
            if (!request.IsWellFormed) throw new InvalidOperationException("recurring source returned neither intent nor typed refusal");
            if (definition.Template != null)
            {
                var binding = (IRecurringPlanBindingDefinition)definition;
                var expected = EntityObservation.Read(world, entity, () => binding.Binding.ReadRequest(world, source, binding.Group, EffectivePriority(world, source, schedule.Priority)));
                if (!SameRequest(world, entity, request, expected)) throw new InvalidOperationException("recurring source returned a different authored template group");
                return request;
            }
            if (request.GroupIntent != null) throw new InvalidOperationException("single-plan source returned a grouped intent");
            var plan = ResolvePlan(world, definition);
            PlanIntent intent = request.Intent;
            if (intent != null)
            {
                if (intent.Definition != plan.PlanKey || intent.Priority != EffectivePriority(world, source, schedule.Priority) || intent.Paused)
                    throw new InvalidOperationException("recurring source returned a different activity/settings or paused intent");
                string identity = EntityObservation.Read(world, entity, () => PlanObservation.Identity(world, plan, intent.Arguments));
                if (string.IsNullOrWhiteSpace(identity)) throw new InvalidOperationException("recurring intent has no stable actual identity");
                if (definition is IRecurringPlanBindingDefinition binding &&
                    (intent.Group != binding.Group || binding.Binding.Target == RecurringPlanTarget.AuthoredIntent &&
                    identity != EntityObservation.Read(world, entity, () => PlanObservation.Identity(world, plan, binding.Binding.Arguments))))
                    throw new InvalidOperationException("recurring source returned different authored intent metadata");
            }
            return request;
        }
        private static Code ResolveCadence(World world, Entity entity, IRecurringPlanSource source,
            IRecurringPlanDefinition definition, RecurringPlanSchedule schedule,
            out bool outstanding, out bool retry, out long? completed, out long? due, out long? serviceDue)
        {
            var board = world.Services.Get<WorkBoard>();
            EntityObservation.Read(world, entity, () => { board.ValidateRecurringLink(source, schedule); return true; });
            long interval = world.Time.ExactTicks(schedule.Interval);
            if (interval <= 0) throw new InvalidOperationException("recurring cadence must be positive and aligned");
            outstanding = EntityObservation.Read(world, entity, () => board.HasOutstandingRecurringPlan(source, schedule));
            long? latest = schedule.LastPlan == 0 || outstanding ? null : EntityObservation.Read(world, entity,
                () => board.SuccessfulRecurringCompletion(source, schedule.LastPlan));
            completed = schedule.LastCompletedPlan == 0 ? null : schedule.LastCompletedPlan == schedule.LastPlan ? latest :
                EntityObservation.Read(world, entity, () => board.SuccessfulRecurringCompletion(source, schedule.LastCompletedPlan));
            due = serviceDue = null; Code blocker = Code.None;
            retry = schedule.LastPlan != 0 && !outstanding && !latest.HasValue;
            try
            {
                if (definition.Cadence == RecurringCadenceBasis.Submission)
                    due = schedule.LastPostedTick < 0 ? 0 : checked(schedule.LastPostedTick + interval);
                else
                {
                    // Earned service age is actual whole-cycle success, never an attempted posting.
                    serviceDue = completed.HasValue ? checked(completed.Value + interval) : 0;
                    if (outstanding) blocker = RecurringPlanCodes.Outstanding;
                    else
                    {
                        due = serviceDue;
                        if (schedule.LastPlan != 0 && schedule.LastPlan != schedule.LastCompletedPlan)
                        {
                            if (latest.HasValue)
                            {
                                // Actual complete work has known timing, but this pure read cannot acknowledge it.
                                due = serviceDue = checked(latest.Value + interval);
                                blocker = RecurringPlanCodes.CompletionPending;
                            }
                            else
                                // Retry admission is delayed without making earned service younger.
                                due = Math.Max(due.Value, checked(schedule.LastPostedTick + interval));
                        }
                    }
                }
            }
            catch (OverflowException) { blocker = RecurringPlanCodes.TimeRange; }
            return blocker;
        }
        /// <summary>Pure prospective movement against actual service chronology, never another deadline owner or plan preview.</summary>
        public static Code CheckMovementWindow(World world, IRecurringPlanSource source, long updateIntervals)
        {
            if (updateIntervals < 0) throw new ArgumentOutOfRangeException(nameof(updateIntervals));
            return ReadServiceWindow(world, source).CheckMovement(updateIntervals);
        }
        /// <summary>Pure service facts derived from the existing cadence/history owners and sole game clock.</summary>
        public static ServiceMovementWindow ReadServiceWindow(World world, IRecurringPlanSource source)
        {
            Entity entity = Actual(world, source); var schedule = ReadSchedule(world, source);
            var definition = entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId);
            if (definition.Cadence != RecurringCadenceBasis.SuccessfulCompletion || definition.TriggerMode == RecurringTriggerMode.Maintenance)
                throw new ArgumentException("movement window requires successful-completion scheduled service");
            Code blocker = ResolveCadence(world, entity, source, definition, schedule, out bool outstanding, out _, out _, out _, out long? serviceDue);
            if (!schedule.Enabled) return new ServiceMovementWindow(world, source.CapabilityId, true, null, Code.None);
            if (outstanding) blocker = RecurringPlanCodes.ServicePending;
            var policy = definition as IRecurringOperationPolicyDefinition;
            if (blocker.IsNone && definition.TriggerMode == RecurringTriggerMode.ScheduleOrMaintenance && policy != null && !policy.OperationPolicy.IsNone &&
                OperationPolicyContracts.Read(world, entity.Get<IOperationPolicy>(policy.OperationPolicy)).MaintenanceDue)
                blocker = RecurringPlanCodes.MovementWindow;
            return new ServiceMovementWindow(world, source.CapabilityId, false, serviceDue, blocker,
                new[] { (entity.Id, source.CapabilityId) });
        }
        public static RecurringPlanView Read(World world, IRecurringPlanSource source)
        {
            Entity entity = Actual(world, source); var schedule = ReadSchedule(world, source);
            var definition = entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId);
            var board = world.Services.Get<WorkBoard>();
            var request = Request(world, source, schedule, definition);
            Code blocker = ResolveCadence(world, entity, source, definition, schedule, out bool outstanding, out bool retry, out long? completed, out long? due, out long? serviceDue);
            var policy = definition as IRecurringOperationPolicyDefinition;
            bool maintenance = policy != null && !policy.OperationPolicy.IsNone &&
                OperationPolicyContracts.Read(world, entity.Get<IOperationPolicy>(policy.OperationPolicy)).MaintenanceDue;
            if (!schedule.Enabled) blocker = RecurringPlanCodes.Disabled;
            else if (blocker.IsNone)
            {
                bool early = definition.TriggerMode != RecurringTriggerMode.Schedule && maintenance;
                if (definition.TriggerMode == RecurringTriggerMode.Maintenance && !maintenance)
                    blocker = OperationPolicyCodes.MaintenanceNotDue;
                else if (world.Tick < due && (!early || retry)) blocker = RecurringPlanCodes.Cadence;
            }
            if (blocker.IsNone && outstanding)
                blocker = RecurringPlanCodes.Outstanding;
            if (blocker.IsNone) blocker = request.Blocker;
            if (blocker.IsNone) blocker = EntityObservation.Read(world, entity, () => source.CheckTrigger(world));
            if (blocker.IsNone) blocker = PreviewAdmission(world, source, request);
            var admission = request.GroupIntent == null ? null : EntityObservation.Read(world, entity, () => board.PreviewPlanGroup(entity.Owner, request.GroupIntent));
            return new RecurringPlanView(entity.Id, source.CapabilityId, request, schedule, due, blocker, world, admission, definition.Cadence, completed, definition.TriggerMode, maintenance, serviceDue);
        }
        internal static Code PreviewAdmission(World world, IRecurringPlanSource source, RecurringPlanRequest offered)
        {
            Entity entity = Actual(world, source); var schedule = ReadSchedule(world, source);
            var definition = entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId);
            var request = Request(world, source, schedule, definition);
            var board = world.Services.Get<WorkBoard>();
            board.ValidateRecurringLink(source, schedule);
            if (board.HasOutstandingRecurringPlan(source, schedule)) return RecurringPlanCodes.Outstanding;
            if (!request.Blocker.IsNone) return request.Blocker;
            if (!SameRequest(world, entity, offered, request)) return RecurringPlanCodes.RequestChanged;
            return EntityObservation.Read(world, entity, () => request.GroupIntent != null
                ? board.PreviewPlanGroup(entity.Owner, request.GroupIntent).Blocker : board.PreviewPlan(entity.Owner, request.Intent));
        }
        internal static Code CheckInterval(World world, GameDuration interval)
        {
            if (interval.IsZero) return RecurringPlanCodes.Interval;
            try { return world.Clock.Time.ExactTicks(interval) > 0 ? Code.None : RecurringPlanCodes.Interval; }
            catch (ArgumentException) { return RecurringPlanCodes.Interval; }
            catch (OverflowException) { return RecurringPlanCodes.TimeRange; }
        }
        public static Code CheckConfigure(World world, IRecurringPlanSource source, RecurringPlanSettings settings)
        {
            if (source == null) return RecurringPlanCodes.NotSource;
            var current = ReadSchedule(world, source); if (settings == null) return Codes.InvalidArgument;
            Code facility = CheckFacilityPolicy(world, source.Entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId), settings.FacilityPolicy, source.Entity);
            if (!facility.IsNone) return facility;
            Code interval = CheckInterval(world, settings.Interval); if (!interval.IsNone) return interval;
            try
            {
                long duration = world.Time.ExactTicks(settings.Interval);
                if (current.LastPostedTick >= 0) _ = checked(current.LastPostedTick + duration);
                if (current.LastCompletedPlan != 0)
                    _ = checked(world.Services.Get<WorkBoard>().SuccessfulRecurringCompletion(source, current.LastCompletedPlan).Value + duration);
            }
            catch (OverflowException) { return RecurringPlanCodes.TimeRange; }
            var link = source.Entity.Def.Component<IRecurringOperationPolicyDefinition>(source.CapabilityId);
            int criticality = link == null || link.OperationPolicy.IsNone ? 0 :
                OperationPolicyContracts.Read(world, source.Entity.Get<IOperationPolicy>(link.OperationPolicy)).State.Settings.Criticality;
            return CheckSourcePriority(world, source, settings.Priority, criticality);
        }
        public static void Configure(World world, IRecurringPlanSource source, RecurringPlanSettings settings)
        {
            var refusal = CheckConfigure(world, source, settings); if (!refusal.IsNone) throw new InvalidOperationException("recurring control refused: " + refusal);
            var before = ReadSchedule(world, source); Entity entity = source.Entity; int version = entity.Version;
            if (before.Settings.Matches(settings)) return;
            var beforeTemplate = ReadTemplateSettings(world, source);
            PlayerId owner = entity.Owner; long tick = world.Tick; EntityId next = world.Entities.NextIdentity;
            var worker = entity.Get<IWorker>(); int job = worker?.Job ?? 0;
            EntityId home = worker == null ? EntityId.None : WorkerHomeContracts.ReadHome(world, entity);
            bool held = worker?.Held ?? false, arrived = worker?.HeldArrived ?? false;
            source.Configure(world, settings); var after = ReadSchedule(world, source);
            if (!after.Settings.Matches(settings) || after.LastPostedTick != before.LastPostedTick || after.LastPlan != before.LastPlan || after.LastCompletedPlan != before.LastCompletedPlan ||
                !SameTemplateSettings(beforeTemplate, ReadTemplateSettings(world, source)) ||
                entity.Version <= version || entity.Owner != owner || world.Tick != tick || world.Entities.NextIdentity != next ||
                !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) || worker != null &&
                (worker.Job != job || WorkerHomeContracts.ReadHome(world, entity) != home || worker.Held != held || worker.HeldArrived != arrived))
                throw new InvalidOperationException("recurring source did not commit exact settings without changing its posting history");
        }
        internal static void Record(World world, IRecurringPlanSource source, RecurringPlanView before, long plan)
        {
            if (before.ObservedTick != world.Tick || before.Entity != source.Entity.Id || before.Source != source.CapabilityId || !Same(ReadSchedule(world, source), before.Schedule))
                throw new InvalidOperationException("recurring admission is stale or belongs to another source");
            Entity entity = source.Entity; int version = entity.Version; PlayerId owner = entity.Owner; EntityId next = world.Entities.NextIdentity; long tick = world.Tick;
            source.RecordSubmission(world, plan); var after = ReadSchedule(world, source);
            world.Services.Get<WorkBoard>().ValidateRecurringLink(source, after);
            if (after.LastPlan != plan || after.LastPostedTick != tick || after.LastCompletedPlan != before.Schedule.LastCompletedPlan || !after.Settings.Matches(before.Schedule.Settings) ||
                !SameTemplateSettings(before.TemplateSettings, ReadTemplateSettings(world, source)) ||
                entity.Version == version || entity.Owner != owner || world.Tick != tick || world.Entities.NextIdentity != next)
                throw new InvalidOperationException("recurring source did not acknowledge exactly the admitted native plan");
        }
        internal static void RecordCompletion(World world, IRecurringPlanSource source, long plan)
        {
            var before = ReadSchedule(world, source); Entity entity = source.Entity;
            if (entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId).Cadence != RecurringCadenceBasis.SuccessfulCompletion ||
                plan != before.LastPlan || plan <= before.LastCompletedPlan ||
                !world.Services.Get<WorkBoard>().SuccessfulRecurringCompletion(source, plan).HasValue)
                throw new InvalidOperationException("completion acknowledgement requires the actual successful whole source cycle");
            var template = ReadTemplateSettings(world, source); int version = entity.Version;
            var owner = entity.Owner; var next = world.Entities.NextIdentity; long update = world.Clock.StepIndex;
            source.RecordCompletion(world, plan); var after = ReadSchedule(world, source);
            world.Services.Get<WorkBoard>().ValidateRecurringLink(source, after);
            if (after.LastCompletedPlan != plan || after.LastPlan != before.LastPlan || after.LastPostedTick != before.LastPostedTick ||
                !after.Settings.Matches(before.Settings) || !SameTemplateSettings(template, ReadTemplateSettings(world, source)) ||
                entity.Version == version || entity.Owner != owner || world.Entities.NextIdentity != next || world.Clock.StepIndex != update)
                throw new InvalidOperationException("recurring source did not acknowledge exact completion without altering posting/settings/time");
        }
    }
    public sealed class RecurringActivitySourceDef : ComponentDef<RecurringActivitySource>, IRecurringActivitySourceDefinition
    {
        private RecurringActivityDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public RecurringCadenceBasis Cadence => _declaration.Cadence;
        public RecurringTriggerMode TriggerMode => _declaration.TriggerMode;
        public RecurringWorkerDispatchMode WorkerDispatch => _declaration.WorkerDispatch;
        public string PlanKey => _declaration.PlanKey;
        public IPlanTemplateContent Template => _declaration.Template;
        public RecurringPlanSettings Initial => _declaration.Initial;
        public RecurringPlanBinding Binding => _declaration.Binding;
        public string Group => _declaration.Group;
        public Code OperationPolicy => _declaration.OperationPolicy;
        public IReadOnlyList<SignalGateCondition> Conditions => _declaration.Conditions;
        protected override void ReadCore(JsonReader reader) => _declaration = RecurringActivityDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public void ValidateBinding(World world) => RecurringActivityBindingContracts.ValidateBinding(world, this);
    }
    /// <summary>Posting policy and last admitted reference only. WorkBoard alone owns plans and execution.</summary>
    public sealed class RecurringActivitySource : Component, IRecurringTemplateSource
    {
        public new IRecurringActivitySourceDefinition Def => (IRecurringActivitySourceDefinition)base.Def;
        private RecurringPlanSettings _settings;
        private RecurringTemplateSettings _templateSettings;
        private long _lastPosted = -1, _lastPlan, _lastCompleted;
        public Code CapabilityId => Def.CapabilityId;
        public override void OnCreate()
        {
            _settings = Def.Initial; _lastPosted = -1; _lastPlan = 0; _lastCompleted = 0;
            _templateSettings = Def.Template == null ? null : new RecurringTemplateSettings(PlanTemplateSelection.Snapshot(Def.Template.Template), Def.Binding.Arguments, Def.Group);
        }
        public RecurringTemplateSettings ReadTemplateSettings() => _templateSettings;
        public void ConfigureTemplate(World world, RecurringTemplateSettings settings)
        {
            Code check = RecurringPlanContracts.CheckConfigureTemplate(world, this, settings).Blocker;
            if (!check.IsNone) throw new InvalidOperationException("recurring template control refused: " + check);
            _templateSettings = settings; Entity.Touch();
        }
        public RecurringPlanSchedule ReadSchedule() => new RecurringPlanSchedule(_settings, _lastPosted, _lastPlan, _lastCompleted);
        public RecurringPlanRequest ReadRequest(World world)
            => RecurringActivityBindingContracts.Observe(world, Def, Entity, definition => definition.Binding.ReadRequest(world, this, definition.Group,
                RecurringPlanContracts.EffectivePriority(world, this, _settings.Priority)));
        public Code CheckTrigger(World world)
        {
            return RecurringActivityBindingContracts.Observe(world, Def, Entity,
                definition => SignalGateCondition.ReadGate(world, Entity, definition.CapabilityId, true, definition.Conditions).Blocker);
        }
        public void Configure(World world, RecurringPlanSettings settings)
        {
            Code refusal = RecurringPlanContracts.CheckConfigure(world, this, settings);
            if (!refusal.IsNone) throw new InvalidOperationException("recurring control refused: " + refusal);
            _settings = settings; Entity.Touch();
        }
        public void RecordSubmission(World world, long plan)
        { if (plan <= 0 || world.Tick < _lastPosted) throw new InvalidOperationException("invalid recurring acknowledgement"); _lastPosted = world.Tick; _lastPlan = plan; Entity.Touch(); }
        public void RecordCompletion(World world, long plan)
        { if (plan != _lastPlan || plan <= _lastCompleted) throw new InvalidOperationException("invalid recurring completion acknowledgement"); _lastCompleted = plan; Entity.Touch(); }
        public override void Sync(IStateSync sync)
        {
            RecurringPlanSettings settings = RecurringPlanSettings.Sync(sync, _settings); long posted = _lastPosted, plan = _lastPlan, completed = _lastCompleted;
            sync.Long(ref posted); sync.Long(ref plan); sync.Long(ref completed);
            RecurringTemplateSettings template = RecurringTemplateSettings.Sync(sync, _templateSettings);
            if ((template != null) != (Def.Template != null)) throw new CorruptStateException("invalid recurring template binding state");
            if (!RecurringPlanContracts.IsStructurallyValid(new RecurringPlanSchedule(settings, posted, plan, completed)))
                throw new CorruptStateException("invalid saved recurring schedule");
            if (sync.Reading) { _settings = settings; _lastPosted = posted; _lastPlan = plan; _lastCompleted = completed; _templateSettings = template; }
        }
    }
}
