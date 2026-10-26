using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum WorkAssignmentMode : byte { All = 0, EmergencyOnly = 1, EmergencyAndSelfService = 2 }
    /// <summary>Detached immutable settings, never job, region or receipt handles.</summary>
    public sealed class WorkPolicySettings
    {
        public bool Enabled { get; }
        public int Priority { get; }
        public IReadOnlyList<WorkerRoleId> AllowedRoles { get; }
        public string Area { get; }
        public bool Interruptible { get; }
        public bool EmergencyEligible { get; }
        public WorkAssignmentMode AssignmentMode { get; }
        public WorkPolicySettings(bool enabled, int priority, IEnumerable<WorkerRoleId> allowedRoles, string area, bool interruptible, bool emergencyEligible, WorkAssignmentMode assignmentMode)
        {
            if (allowedRoles == null || priority < 0) throw new ArgumentException("policy needs explicit roles and nonnegative preference");
            if (area != null && string.IsNullOrWhiteSpace(area)) throw new ArgumentException("area is a stable region ID or explicit null");
            if (!ValidMode(assignmentMode)) throw new ArgumentException("invalid work assignment mode");
            var roles = new List<WorkerRoleId>(allowedRoles); roles.Sort((a, b) => string.CompareOrdinal(a.Value, b.Value));
            for (int i = 0; i < roles.Count; i++)
                if (!roles[i].IsValid || i > 0 && roles[i] == roles[i - 1]) throw new ArgumentException("policy roles must be valid and unique");
            Enabled = enabled; Priority = priority; AllowedRoles = roles.AsReadOnly(); Area = area; Interruptible = interruptible; EmergencyEligible = emergencyEligible;
            AssignmentMode = assignmentMode;
        }
        internal bool Matches(WorkPolicySettings other)
        {
            if (other == null || Enabled != other.Enabled || Priority != other.Priority || Area != other.Area || Interruptible != other.Interruptible ||
                EmergencyEligible != other.EmergencyEligible || AssignmentMode != other.AssignmentMode || AllowedRoles.Count != other.AllowedRoles.Count) return false;
            for (int i = 0; i < AllowedRoles.Count; i++) if (AllowedRoles[i] != other.AllowedRoles[i]) return false;
            return true;
        }
        public bool Allows(WorkerRoleId role) { foreach (var allowed in AllowedRoles) if (allowed == role) return true; return false; }
        public static WorkPolicySettings Read(JsonReader reader)
        {
            bool enabled = Boolean(reader, "enabled"), interruptible = Boolean(reader, "interruptible"), emergency = Boolean(reader, "emergency_eligible");
            string mode = reader.String("assignment_mode");
            if (mode != "all" && mode != "emergency_only" && mode != "emergency_and_self_service") throw reader.Error("assignment_mode must explicitly be all, emergency_only or emergency_and_self_service");
            int priority = reader.Int("priority"); JsonValue area = reader.Value("area");
            if (area == null || area.Kind != JsonKind.Null && area.Kind != JsonKind.String) throw reader.Error("area must be an explicit region ID or null");
            JsonValue values = reader.Array("allowed_roles") ?? throw reader.Error("allowed_roles must be explicit");
            var roles = new List<WorkerRoleId>();
            try
            {
                for (int i = 0; i < values.Count; i++)
                { if (values[i].Kind != JsonKind.String) throw reader.Error("roles must be string identities"); roles.Add(new WorkerRoleId(values[i].Text)); }
                return new WorkPolicySettings(enabled, priority, roles, area.Kind == JsonKind.Null ? null : area.Text, interruptible, emergency,
                    mode == "all" ? WorkAssignmentMode.All : mode == "emergency_only" ? WorkAssignmentMode.EmergencyOnly : WorkAssignmentMode.EmergencyAndSelfService);
            }
            catch (ArgumentException e) { throw reader.Error(e.Message); }
        }
        private static bool ValidMode(WorkAssignmentMode mode) => mode == WorkAssignmentMode.All || mode == WorkAssignmentMode.EmergencyOnly || mode == WorkAssignmentMode.EmergencyAndSelfService;
        private static bool Boolean(JsonReader reader, string name)
        {
            JsonValue value = reader.Value(name);
            if (value == null || value.Kind != JsonKind.Bool) throw reader.Error(name + " must be an explicit boolean");
            return value.Bool;
        }
        public static WorkPolicySettings Sync(IStateSync sync, WorkPolicySettings current)
        {
            if (!sync.Reading && current == null) throw new CorruptStateException("missing policy settings");
            bool enabled = current?.Enabled ?? false, interruptible = current?.Interruptible ?? false, emergency = current?.EmergencyEligible ?? false;
            int priority = current?.Priority ?? 0; string area = current?.Area;
            sync.Bool(ref enabled); sync.Int(ref priority); sync.String(ref area); sync.Bool(ref interruptible); sync.Bool(ref emergency);
            int mode = current == null ? 0 : (int)current.AssignmentMode; sync.Int(ref mode);
            if (!ValidMode((WorkAssignmentMode)mode) || mode < 0 || mode > byte.MaxValue) throw new CorruptStateException("invalid saved work assignment mode");
            int count = sync.Count(current?.AllowedRoles.Count ?? 0); var roles = sync.Reading ? new List<WorkerRoleId>() : null; string previous = null;
            for (int i = 0; i < count; i++)
            {
                string id = sync.Reading ? null : current.AllowedRoles[i].Value; sync.String(ref id);
                if (string.IsNullOrWhiteSpace(id) || previous != null && string.CompareOrdinal(previous, id) >= 0) throw new CorruptStateException("noncanonical policy roles");
                previous = id;
                if (roles != null)
                {
                    try { roles.Add(new WorkerRoleId(id)); }
                    catch (ArgumentException) { throw new CorruptStateException("invalid saved policy role identity"); }
                }
            }
            if (!sync.Reading) return current;
            try { return new WorkPolicySettings(enabled, priority, roles, area, interruptible, emergency, (WorkAssignmentMode)mode); }
            catch (ArgumentException) { throw new CorruptStateException("invalid policy settings"); }
        }
    }
    public interface IWorkPolicy : IEntityCapability
    {
        WorkPolicySettings ReadPolicy();
        Code CheckPolicy(World world, WorkPolicySettings settings);
        void Configure(World world, WorkPolicySettings settings);
    }
    public static class WorkPolicyCodes
    {
        public static readonly Code NotPolicy = new Code("work.not-policy"), InvalidSettings = new Code("work.invalid-policy-settings"),
            Disabled = new Code("work.policy-disabled"), Role = new Code("work.policy-role"), OutsideArea = new Code("work.policy-outside-area"),
            UnsupportedArea = new Code("work.policy-area-unsupported"), Emergency = new Code("work.policy-emergency-ineligible"),
            NotInterruptible = new Code("work.policy-not-interruptible"), Standby = new Code("work.policy-standby");
        public static bool IsWait(Code code) => code == Disabled || code == Role || code == OutsideArea || code == UnsupportedArea || code == Emergency || code == Standby || code == CompanyWorkerReserveCodes.MinimumReady || MotionAreaCodes.IsWait(code);
    }
    /// <summary>One stateless rule owner shared by work, hauling, home return and commands.</summary>
    public static partial class WorkPolicyRules
    {
        public static WorkPolicySettings Read(Entity worker)
        {
            IWorkPolicy policy = worker.Get<IWorkPolicy>();
            return policy == null ? null : policy.ReadPolicy() ?? throw new InvalidOperationException("missing provider policy settings");
        }
        public static int Priority(Entity worker) => Read(worker)?.Priority ?? 0;
        public static IEntityStateDefinition State(Entity worker, WorkPolicySettings settings, EntityStateBindings states)
            => !settings.Enabled ? states["disabled"] : settings.AssignmentMode != WorkAssignmentMode.All &&
                worker.Get<IWorker>() is IWorker actor && actor.Job == 0 && !actor.Held ? states["standby"] : null;
        public static Code CheckConfigure(World world, PlayerId player, Entity worker, WorkPolicySettings settings)
        {
            if (world == null) return Codes.NotFound;
            world.RequireHealthyUpdate();
            if (worker == null || !worker.Alive || !ReferenceEquals(world.Entities.Get(worker.Id), worker) ||
                worker.Owner != player || !world.ViewFor(player).CanObserveOwned(worker)) return Codes.NotFound;
            return CheckPolicyProvider(world, worker, settings);
        }
        private static Code CheckPolicyProvider(World world, Entity worker, WorkPolicySettings settings)
        {
            if (worker == null || !worker.Alive || !ReferenceEquals(world.Entities.Get(worker.Id), worker)) return Codes.NotFound;
            var policy = worker.Get<IWorkPolicy>(); if (policy == null) return WorkPolicyCodes.NotPolicy;
            if (!ReferenceEquals(policy.Entity, worker)) throw new InvalidOperationException("work policy must belong to its actual worker");
            Code refusal = CheckSettings(world, worker, settings); if (!refusal.IsNone) return refusal;
            return EntityObservation.ReadProvider(world, policy, worker, () => policy.CheckPolicy(world, settings));
        }
        public static void Configure(World world, Entity worker, WorkPolicySettings settings)
        {
            Code refusal = CheckPolicyProvider(world, worker, settings); if (!refusal.IsNone) throw new InvalidOperationException("work policy control refused: " + refusal);
            if (settings.Matches(ReadObserved(world, worker))) return;
            int version = worker.Version, job = worker.Get<IWorker>()?.Job ?? 0; PlayerId owner = worker.Owner;
            long update = world.Clock.StepIndex; EntityId next = world.Entities.NextIdentity;
            worker.Get<IWorkPolicy>().Configure(world, settings);
            var after = ReadObserved(world, worker);
            if (!settings.Matches(after) || worker.Version == version || worker.Owner != owner || world.Clock.StepIndex != update || world.Entities.NextIdentity != next ||
                !ReferenceEquals(world.Entities.Get(worker.Id), worker) || (worker.Get<IWorker>()?.Job ?? 0) != job)
                throw new InvalidOperationException("work policy provider did not commit exact settings without changing its worker/job identity");
        }
        /// <summary>Automatic intent only; explicit held player movement does not become an automatic trip.</summary>
        public static ISpatialRegion MotionRegion(World world, Entity worker)
        {
            if (worker.Get<IWorker>()?.Held == true) return null;
            var settings = EntityObservation.Read(world, worker, () => Read(worker));
            if (settings?.Area == null) return null;
            var region = world.Services.Get<IOperatingAreas>().Resolve(worker.Owner, settings.Area);
            if (region == null || region.Space != world.Settings.Space) throw new InvalidOperationException("invalid actual motion operating area");
            return region;
        }
        public static Code CheckRoles(ResolvedContent content, WorkPolicySettings settings)
        {
            if (settings == null) return WorkPolicyCodes.InvalidSettings;
            foreach (var role in settings.AllowedRoles) if (content.Find<IWorkerRoleDefinition>(role.Value) == null) return WorkPolicyCodes.InvalidSettings;
            return Code.None;
        }
        public static Code CheckInitialSettings(ResolvedContent content, string space, WorkPolicySettings settings)
        {
            Code refusal = CheckRoles(content, settings); if (!refusal.IsNone) return refusal;
            if (settings.Area != null && (content.Find<ISpatialRegionDefinition>(settings.Area) is not ISpatialRegionDefinition region || region.Space != space)) return WorkPolicyCodes.InvalidSettings;
            return Code.None;
        }
        public static Code CheckSettings(World world, Entity worker, WorkPolicySettings settings)
        {
            Code refusal = CheckRoles(world.Content, settings); if (!refusal.IsNone) return refusal;
            if (settings.Area != null && world.Services.Get<IOperatingAreas>().Resolve(worker.Owner, settings.Area)?.Space != world.Settings.Space)
                return WorkPolicyCodes.InvalidSettings;
            return Code.None;
        }
        internal static void ValidateLive(World world)
        {
            foreach (Entity worker in world.Entities.All)
                if (worker.Alive && worker.Has<IWorkPolicy>())
                {
                    WorkPolicySettings settings = EntityObservation.Read(world, worker, () => Read(worker));
                    if (!CheckSettings(world, worker, settings).IsNone) throw new InvalidOperationException("invalid actual work policy or operating-area reference");
                }
        }
        public static Code CheckDestination(World world, Entity worker, Entity destination)
        {
            WorkPolicySettings settings = Read(worker);
            return CheckGeometry(world, worker.Owner, settings, settings?.Enabled == true && settings.Area != null ? destination?.Get<ISpatialPresence>()?.Capture() : null);
        }
        public static Code CheckGeometry(World world, Entity worker, SpatialSnapshot geometry)
            => CheckGeometry(world, worker.Owner, Read(worker), geometry);
        private static Code CheckGeometry(World world, PlayerId owner, WorkPolicySettings settings, SpatialSnapshot geometry)
        {
            if (settings == null) return Code.None; // Explicit absence of a policy capability, not an inferred profile.
            if (!settings.Enabled) return WorkPolicyCodes.Disabled;
            if (settings.Area == null) return Code.None;
            ISpatialRegion region = world.Services.Get<IOperatingAreas>().Resolve(owner, settings.Area);
            if (region == null || region.Space != world.Settings.Space) return WorkPolicyCodes.UnsupportedArea;
            SpatialRegionStatus status = region.Check(geometry);
            return status == SpatialRegionStatus.Inside ? Code.None : status == SpatialRegionStatus.Outside ? WorkPolicyCodes.OutsideArea : WorkPolicyCodes.UnsupportedArea;
        }
        internal static Code CheckAssignment(World world, Entity worker, WorkerRoleId role, IReadOnlyList<Entity> destinations, bool emergency, bool selfService, int ownedJob)
        {
            WorkPolicySettings settings = Read(worker);
            bool continuation = settings != null && !emergency && !selfService && settings.AssignmentMode != WorkAssignmentMode.All &&
                IsContinuation(world, worker, role, destinations, ownedJob);
            Code eligibility = CheckReadiness(settings, role, emergency, selfService || continuation);
            if (!eligibility.IsNone) return eligibility;
            foreach (Entity destination in destinations) { Code refusal = CheckDestination(world, worker, destination); if (!refusal.IsNone) return refusal; }
            return Code.None;
        }
        internal static Code CheckReadiness(WorkPolicySettings settings, WorkerRoleId role, bool emergency, bool continuationOrSelfService)
        {
            if (settings == null) return Code.None;
            if (!settings.Enabled) return WorkPolicyCodes.Disabled;
            if (!settings.Allows(role)) return WorkPolicyCodes.Role;
            if (emergency && !settings.EmergencyEligible) return WorkPolicyCodes.Emergency;
            if (!emergency && settings.AssignmentMode != WorkAssignmentMode.All && !continuationOrSelfService) return WorkPolicyCodes.Standby;
            return Code.None;
        }
        internal static bool IsContinuation(World world, Entity worker, WorkerRoleId role, IReadOnlyList<Entity> destinations, int ownedJob)
        {
            Job job = world.Services.Get<WorkBoard>().Job(ownedJob);
            if (job?.IsLive != true || job.Worker != worker.Id || worker.Get<IWorker>()?.Job != ownedJob || job.Owner != worker.Owner || job.Role != role) return false;
            foreach (Entity destination in destinations)
            {
                bool actual = false;
                for (int i = job.ToilIndex; i < job.Toils.Count; i++) if (job.Toils[i].At == destination.Id) { actual = true; break; }
                if (!actual) return false;
            }
            return true;
        }
    }
    /// <summary>Actual owned immutable initial policy and entity-state bindings, independent of native definition inheritance.</summary>
    public interface IWorkPolicyDefinition : IContentDefinition
    {
        WorkPolicySettings Initial { get; }
        EntityStateBindings States { get; }
    }
    /// <summary>Sole reader/resolver for authored initial worker policies; no runtime policy or worker state.</summary>
    public sealed class WorkPolicyDeclaration
    {
        private WorkPolicyDeclaration() { }
        public WorkPolicySettings Initial { get; private set; }
        public EntityStateBindings States { get; private set; }
        private bool _resolved;
        public static WorkPolicyDeclaration Read(JsonReader reader)
            => new WorkPolicyDeclaration { Initial = WorkPolicySettings.Read(reader), States = EntityStateBindings.Read(reader, "disabled", "standby") };
        public void Resolve(ContentResolver content)
        {
            if (_resolved) throw new InvalidOperationException("work policy declaration already resolved");
            foreach (var role in Initial.AllowedRoles) content.Def<IWorkerRoleDefinition>(role.Value);
            if (Initial.Area != null) content.Def<ISpatialRegionDefinition>(Initial.Area);
            States = States.Resolve(content);
            _resolved = true;
        }
        internal static void ValidateComposition(World world)
        {
            foreach (var policy in world.Content.All<IWorkPolicyDefinition>())
                EntityObservation.ReadProvider(world, policy, null, () => {
                    if (!world.Content.Owns(policy) || !ReferenceEquals(world.Content.Find<IWorkPolicyDefinition>(policy.Id), policy) ||
                        !WorkPolicyRules.CheckRoles(world.Content, policy.Initial).IsNone || policy.States == null)
                        throw new ContentException("work policy must alias valid actual owned immutable content");
                    if (policy.Initial.Area != null && world.Content.Find<ISpatialRegionDefinition>(policy.Initial.Area) == null)
                        throw new ContentException(policy.Id + ": unknown initial operating area");
                    var states = policy.States.Definitions;
                    if (states.Count != 2 || !states.ContainsKey("disabled") || !states.ContainsKey("standby"))
                        throw new ContentException(policy.Id + ": work policy requires exactly disabled and standby states");
                    foreach (var state in states.Values)
                        if (state == null || !ReferenceEquals(world.Content.Find<IEntityStateDefinition>(state.Id), state))
                            throw new ContentException(policy.Id + ": work policy state must alias actual owned content");
                    return true;
                });
        }
    }
    public sealed class WorkPolicyDef : Def, IWorkPolicyDefinition
    {
        private WorkPolicyDeclaration _declaration;
        public WorkPolicySettings Initial => _declaration.Initial;
        public EntityStateBindings States => _declaration.States;
        protected override void ReadCore(JsonReader reader) => _declaration = WorkPolicyDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class WorkPolicyComponentDef : ComponentDef<WorkPolicyComponent>
    {
        private string _profile;
        internal ResolvedContent Content { get; private set; }
        public IWorkPolicyDefinition Profile { get; private set; }
        protected override void ReadCore(JsonReader reader) => _profile = reader.String("profile");
        protected override void ResolveCore(ContentResolver content)
        { Content = content.Content; Profile = content.Def<IWorkPolicyDefinition>(_profile); }
        protected override void ValidateCore(EntityDef owner)
        {
            if (!owner.HasCapability<IWorker>()) throw new ContentException(owner.Id + ": work policy requires IWorker");
            if (!WorkPolicyRules.CheckInitialSettings(Content, owner.Component<ISpatialPresenceDefinition>()?.Space, Profile.Initial).IsNone) throw new ContentException(owner.Id + ": incompatible initial work policy");
        }
    }
    /// <summary>Settings only. Worker/job/receipt/cargo/progress owners are unchanged.</summary>
    public sealed class WorkPolicyComponent : Component<WorkPolicyComponentDef>, IWorkPolicy, IEntityStateSource
    {
        private WorkPolicySettings _settings;
        public override void OnCreate() => _settings = Def.Profile.Initial;
        public WorkPolicySettings ReadPolicy() => _settings;
        public IEntityStateDefinition GetState(EntityUpdateContext context) => WorkPolicyRules.State(Entity, _settings, Def.Profile.States);
        public Code CheckPolicy(World world, WorkPolicySettings settings)
            => !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ? Codes.NotFound : WorkPolicyRules.CheckSettings(world, Entity, settings);
        public void Configure(World world, WorkPolicySettings settings)
        {
            Code refusal = CheckPolicy(world, settings); if (!refusal.IsNone) throw new InvalidOperationException("policy configuration refused: " + refusal);
            _settings = settings; Entity.Touch(); world.Services.Get<WorkBoard>().Dirty = true; world.Services.Get<IWorkerDispatch>().RefreshPolicy(world, Entity);
        }
        public override void Sync(IStateSync sync)
        {
            WorkPolicySettings settings = WorkPolicySettings.Sync(sync, _settings);
            if (!WorkPolicyRules.CheckRoles(Def.Content, settings).IsNone) throw new CorruptStateException("unknown saved policy roles");
            // Runtime areas restore after entities. WorkSystem validates references after all services decode.
            _settings = settings;
        }
    }
}
