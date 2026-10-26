using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IFleetPolicy : IContentDefinition
    {
        IReadOnlyList<IContentDefinition> ContentDependencies { get; }
        IReadOnlyList<string> NavigationClasses { get; }
        void Validate(World world);
        Code CheckMember(World world, Entity member);
        Code CheckNavigation(string profile);
    }
    /// <summary>Authored admission only. Actual workers and movers retain all execution state.</summary>
    public sealed class AuthoredFleetPolicyDef : Def, IFleetPolicy
    {
        private FleetPolicyDeclaration _declaration;
        public IReadOnlyList<WorkerRoleId> AllowedRoles => _declaration.AllowedRoles;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        public IReadOnlyList<string> NavigationClasses => _declaration.NavigationClasses;
        protected override void ReadCore(JsonReader reader) => _declaration = FleetPolicyDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void Validate(World world) => _declaration.Validate(world);
        public Code CheckMember(World world, Entity member) => _declaration.CheckMember(world, member);
        public Code CheckNavigation(string profile) => _declaration.CheckNavigation(profile);
    }
    /// <summary>Sole native fleet JSON admission declaration, with no membership or execution state.</summary>
    public sealed class FleetPolicyDeclaration
    {
        private FleetPolicyDeclaration() { }
        private bool _resolved;
        private string[] _roles;
        private IReadOnlyList<IContentDefinition> _dependencies;
        public IReadOnlyList<WorkerRoleId> AllowedRoles { get; private set; }
        private IReadOnlyList<string> _navigation;
        public IReadOnlyList<string> NavigationClasses => _navigation;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _dependencies;
        public static FleetPolicyDeclaration Read(JsonReader reader)
        {
            var declaration = new FleetPolicyDeclaration(); declaration.ReadCore(reader); return declaration;
        }
        private void ReadCore(JsonReader reader)
        {
            _roles = ReadIds(reader, "allowed_roles");
            _navigation = Array.AsReadOnly(ReadIds(reader, "navigation_classes"));
        }
        private static string[] ReadIds(JsonReader reader, string name)
        {
            var values = reader.Array(name);
            if (values == null || values.Count == 0) throw reader.Error(name + " must be explicitly nonempty");
            var ids = new string[values.Count]; var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ids.Length; i++)
            {
                if (values[i]?.Kind != JsonKind.String) throw reader.Error(name + " requires string identities");
                ids[i] = values[i].Text;
                try { ContractIdentity.ValidateId(ids[i], name); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
                if (!seen.Add(ids[i])) throw reader.Error(name + " requires unique identities");
            }
            Array.Sort(ids, StringComparer.Ordinal); return ids;
        }
        public void Resolve(ContentResolver content)
        {
            if (_resolved) throw new InvalidOperationException("fleet policy declaration already resolved");
            var roles = new List<WorkerRoleId>(); var dependencies = new List<IContentDefinition>();
            foreach (var id in _roles)
            { var role = content.Def<IWorkerRoleDefinition>(id); roles.Add(role.Role); dependencies.Add(role); }
            AllowedRoles = roles.AsReadOnly(); _dependencies = dependencies.AsReadOnly(); _resolved = true;
        }
        public void Validate(World world)
        {
            if (AllowedRoles == null || AllowedRoles.Count == 0 || NavigationClasses == null || NavigationClasses.Count == 0)
                throw new ContentException("fleet needs explicit compatible roles/navigation identities");
        }
        public Code CheckMember(World world, Entity member)
        {
            if (member.Get<IMotionNavigation>() == null) return MotionNavigationCodes.Unsupported;
            if (member.Get<IWorker>() == null) return StandardCodes.NotWorker;
            bool compatible = false;
            foreach (var role in AllowedRoles) compatible |= WorkerExecutionContracts.Can(world, member, role);
            return compatible ? Code.None : FleetCodes.MemberNotAllowed;
        }
        public Code CheckNavigation(string profile)
        {
            foreach (var allowed in NavigationClasses) if (allowed == profile) return Code.None;
            return MotionNavigationCodes.NotAllowed;
        }
    }
    public static class FleetCodes
    {
        public static readonly Code MemberConflict = new Code("fleet.member-conflict"),
            MemberNotAllowed = new Code("fleet.member-not-allowed"), Empty = new Code("fleet.empty");
    }
    public sealed class FleetSettings
    {
        public string Profile { get; }
        public IReadOnlyList<EntityId> Members { get; }
        public FleetSettings(string profile, IReadOnlyList<EntityId> members)
        {
            ContractIdentity.ValidateId(profile, "fleet policy");
            if (members == null) throw new ArgumentNullException(nameof(members));
            var copy = new EntityId[members.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = members[i];
            Array.Sort(copy);
            for (int i = 0; i < copy.Length; i++)
                if (!copy[i].IsValid || i > 0 && copy[i] == copy[i - 1]) throw new ArgumentException("fleet members must be actual unique identities");
            Profile = profile; Members = Array.AsReadOnly(copy);
        }
        internal static FleetSettings Sync(IStateSync sync, FleetSettings current)
        {
            string profile = current?.Profile; sync.String(ref profile); int count = sync.Count(current?.Members.Count ?? 0);
            var ids = new EntityId[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = sync.Reading ? EntityId.None : current.Members[i]; sync.Entity(ref ids[i]);
                if (sync.Reading && (!ids[i].IsValid || i > 0 && ids[i].CompareTo(ids[i - 1]) <= 0))
                    throw new CorruptStateException("fleet members must have unique canonical identities");
            }
            if (!sync.Reading) return current;
            try { return new FleetSettings(profile, ids); }
            catch (ArgumentException error) { throw new CorruptStateException("invalid fleet settings: " + error.Message); }
        }
    }
    public sealed class FleetMemberSnapshot
    {
        public EntityId Entity { get; }
        public Code Blocker { get; }
        public MotionNavigationSnapshot Navigation { get; }
        internal FleetMemberSnapshot(EntityId entity, Code blocker, MotionNavigationSnapshot navigation)
        { Entity = entity; Blocker = blocker; Navigation = navigation; }
    }
    public sealed class FleetSnapshot
    {
        public PlayerId Company { get; }
        public string Id { get; }
        public FleetSettings Settings { get; }
        public Code Blocker { get; }
        public IReadOnlyList<FleetMemberSnapshot> Members { get; }
        internal FleetSnapshot(PlayerId company, string id, FleetSettings settings, Code blocker, List<FleetMemberSnapshot> members)
        { Company = company; Id = id; Settings = settings; Blocker = blocker; Members = members.AsReadOnly(); }
    }
    public sealed class FleetPolicySnapshot
    {
        public string Id { get; }
        public IReadOnlyList<string> NavigationClasses { get; }
        public IReadOnlyList<string> ContentDependencies { get; }
        internal FleetPolicySnapshot(IFleetPolicy policy)
        {
            Id = policy.Id; var classes = new List<string>(policy.NavigationClasses); classes.Sort(StringComparer.Ordinal);
            var dependencies = new List<string>(); foreach (var dependency in policy.ContentDependencies) dependencies.Add(dependency.Id);
            dependencies.Sort(StringComparer.Ordinal); NavigationClasses = classes.AsReadOnly(); ContentDependencies = dependencies.AsReadOnly();
        }
    }
    public interface IMachineFleets
    {
        World World { get; }
        Code PreviewConfigure(PlayerId company, string id, FleetSettings settings, out FleetSnapshot preview);
        void Configure(PlayerId company, string id, FleetSettings settings);
        Code CheckRemove(PlayerId company, string id);
        void Remove(PlayerId company, string id);
        bool TryRead(PlayerId company, string id, out FleetSnapshot snapshot);
        void CopyFleets(PlayerId viewer, IList<FleetSnapshot> destination);
        void CopyPolicies(PlayerId viewer, IList<FleetPolicySnapshot> destination);
        MotionNavigationSelectionPreview PreviewNavigation(PlayerId company, string id, string profile);
        int ConfigureNavigation(PlayerId company, string id, string profile);
        WorkPolicySelectionPreview PreviewWorkPolicy(PlayerId company, string id, WorkPolicySettings settings);
        int ConfigureWorkPolicy(PlayerId company, string id, WorkPolicySettings settings);
        WorkerHomeSelectionPreview PreviewHome(PlayerId company, string id, EntityId home);
        int ConfigureHome(PlayerId company, string id, EntityId home);
        HaulLoadSelectionPreview PreviewLoadPolicy(PlayerId company, string id, HaulLoadSettings settings);
        int ConfigureLoadPolicy(PlayerId company, string id, HaulLoadSettings settings);
        HaulTripSelectionPreview PreviewTripPolicy(PlayerId company, string id, HaulTripSettings settings);
        int ConfigureTripPolicy(PlayerId company, string id, HaulTripSettings settings);
        RecurringPlanSelectionPreview PreviewRecurringPlan(PlayerId company, string id, Code source, RecurringPlanSettings settings);
        int ConfigureRecurringPlan(PlayerId company, string id, Code source, RecurringPlanSettings settings);
        OutletPolicySelectionPreview PreviewOutletPolicy(PlayerId company, string id, OutletPolicyChange change);
        int ConfigureOutletPolicy(PlayerId company, string id, OutletPolicyChange change);
    }
    /// <summary>Sole named membership owner, not a scheduler or a second movement/worker policy.</summary>
    internal sealed class MachineFleetService : IMachineFleets, IPersistentService, ICompositionValidator
    {
        private readonly SortedDictionary<(int Company, string Id), FleetSettings> _fleets =
            new SortedDictionary<(int, string), FleetSettings>(Comparer<(int, string)>.Create((a, b) =>
                a.Item1 != b.Item1 ? a.Item1.CompareTo(b.Item1) : string.CompareOrdinal(a.Item2, b.Item2)));
        private bool _checking;
        public World World { get; }
        public string Id => MachineFleetsModule.ModuleId;
        public int StateSchemaVersion => 1;
        internal MachineFleetService(World world) { World = world; }
        private static bool ValidId(string id)
        { try { ContractIdentity.ValidateId(id, "fleet"); return true; } catch (ArgumentException) { return false; } }
        private IFleetPolicy Definition(FleetSettings settings) => settings == null ? null : World.Content.Find<IFleetPolicy>(settings.Profile);
        private bool VisibleTo(PlayerId viewer, IFleetPolicy policy)
        {
            if (policy == null || !World.Players.IsSeat(viewer)) return false;
            return Pure(policy, null, () => {
                var view = World.ViewFor(viewer); if (!view.CanSeeContent(policy)) return false;
                foreach (var dependency in policy.ContentDependencies) if (!view.CanSeeContent(dependency)) return false;
                return true;
            });
        }
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, World)) throw new ContentException("foreign fleet session");
            foreach (IFleetPolicy policy in world.Content.All<IFleetPolicy>())
            {
                Pure(policy, null, () =>
                {
                    if (policy.ContentDependencies == null) throw new ContentException(policy.Id + ": explicit fleet content dependencies required");
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var dependency in policy.ContentDependencies)
                        if (dependency == null || !world.Content.Owns(dependency) || !seen.Add(dependency.Id))
                            throw new ContentException(policy.Id + ": missing/foreign/duplicate fleet dependency");
                    if (policy.NavigationClasses == null || policy.NavigationClasses.Count == 0)
                        throw new ContentException(policy.Id + ": explicit fleet navigation identities required");
                    seen.Clear();
                    foreach (var profile in policy.NavigationClasses)
                    {
                        try { ContractIdentity.ValidateId(profile, "fleet navigation identity"); }
                        catch (ArgumentException error) { throw new ContentException(error.Message); }
                        if (!seen.Add(profile)) throw new ContentException(policy.Id + ": duplicate fleet navigation identity");
                    }
                    policy.Validate(world); return true;
                });
            }
        }
        private T Pure<T>(IFleetPolicy policy, Entity entity, Func<T> action)
        {
            if (_checking) throw new InvalidOperationException("fleet policy reentered admission");
            var owner = EntityObservation.CaptureProvider(World, this);
            var member = EntityObservation.CaptureProvider(World, entity,
                entity == null ? (Action<IStateSync>)null : entity.Sync, "fleet.policy-read");
            _checking = true;
            try
            {
                T value = EntityObservation.ReadProvider(World, policy, entity, () => {
                    if (!(policy is Def actual) || !actual.IsFrozen || policy is IPersistentService ||
                        string.IsNullOrWhiteSpace(actual.Id) || string.IsNullOrWhiteSpace(actual.Module) ||
                        policy.Id != actual.Id || policy.Module != actual.Module || !World.Content.Owns(policy) ||
                        !ReferenceEquals(World.Content.Find<IFleetPolicy>(actual.Id), policy))
                        throw new ContentException("fleet policy requires its actual immutable catalog alias");
                    return action();
                }, World.Space);
                member.Verify(); owner.Verify(); return value;
            }
            catch (Exception error) { World.LatchFault("fleet.policy-read", error); throw; }
            finally { _checking = false; }
        }
        private Code Member(PlayerId company, IFleetPolicy policy, EntityId id, out MotionNavigationSnapshot navigation)
        {
            navigation = null; var entity = World.Entities.Get(id);
            if (entity?.Alive != true) return Codes.NotFound;
            if (entity.Owner != company) return Codes.NotOwned;
            var motion = entity.Get<IMotionNavigation>();
            if (motion == null) return MotionNavigationCodes.Unsupported;
            Code refusal = Pure(policy, entity, () => policy.CheckMember(World, entity));
            navigation = MotionNavigationContracts.Read(World, entity);
            return refusal;
        }
        public Code PreviewConfigure(PlayerId company, string id, FleetSettings settings, out FleetSnapshot preview)
        {
            preview = null;
            World.RequireHealthyUpdate();
            if (!World.Players.IsSeat(company)) return Codes.NotSeated;
            if (!ValidId(id) || settings == null) return Codes.InvalidArgument;
            var policy = Definition(settings); if (!VisibleTo(company, policy)) return Codes.UnknownDef;
            var entries = new List<FleetMemberSnapshot>(); Code first = Code.None;
            foreach (var member in settings.Members)
            {
                Code refusal = Member(company, policy, member, out var navigation);
                if (refusal.IsNone)
                    foreach (var fleet in _fleets)
                        if ((fleet.Key.Company != company.Value || fleet.Key.Id != id) && Contains(fleet.Value.Members, member))
                        { refusal = FleetCodes.MemberConflict; break; }
                entries.Add(new FleetMemberSnapshot(member, refusal, navigation));
                if (first.IsNone && !refusal.IsNone) first = refusal;
            }
            preview = new FleetSnapshot(company, id, settings, first, entries); return first;
        }
        private static bool Contains(IReadOnlyList<EntityId> members, EntityId id)
        { foreach (var member in members) if (member == id) return true; return false; }
        public void Configure(PlayerId company, string id, FleetSettings settings)
        {
            if (_checking) throw new InvalidOperationException("fleet mutation during pure policy evaluation");
            Code refusal = PreviewConfigure(company, id, settings, out _);
            if (!refusal.IsNone) throw new InvalidOperationException("fleet membership refused: " + refusal);
            _fleets[(company.Value, id)] = settings;
        }
        public Code CheckRemove(PlayerId company, string id)
        {
            World.RequireHealthyUpdate();
            return !World.Players.IsSeat(company) ? Codes.NotSeated : !ValidId(id) || !_fleets.ContainsKey((company.Value, id)) ? Codes.NotFound : Code.None;
        }
        public void Remove(PlayerId company, string id)
        {
            if (_checking) throw new InvalidOperationException("fleet mutation during pure policy evaluation");
            Code refusal = CheckRemove(company, id); if (!refusal.IsNone) throw new InvalidOperationException("fleet removal refused: " + refusal);
            _fleets.Remove((company.Value, id));
        }
        public bool TryRead(PlayerId company, string id, out FleetSnapshot snapshot)
        {
            snapshot = null;
            World.RequireHealthyUpdate();
            if (!World.Players.IsSeat(company) || !ValidId(id) || !_fleets.TryGetValue((company.Value, id), out var settings)) return false;
            // Existing membership history is not a newly requested policy; actual unavailable members remain explicit.
            var policy = Definition(settings); var entries = new List<FleetMemberSnapshot>(); Code first = Code.None;
            foreach (var member in settings.Members)
            {
                var refusal = Member(company, policy, member, out var navigation);
                entries.Add(new FleetMemberSnapshot(member, refusal, navigation));
                if (first.IsNone && !refusal.IsNone) first = refusal;
            }
            snapshot = new FleetSnapshot(company, id, settings, first, entries); return true;
        }
        public void CopyFleets(PlayerId viewer, IList<FleetSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
            World.RequireHealthyUpdate();
            foreach (var fleet in _fleets)
                if (viewer == PlayerId.None || fleet.Key.Company == viewer.Value)
                    if (TryRead(new PlayerId(fleet.Key.Company), fleet.Key.Id, out var snapshot)) destination.Add(snapshot);
        }
        public void CopyPolicies(PlayerId viewer, IList<FleetPolicySnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
            World.RequireHealthyUpdate();
            var values = new List<FleetPolicySnapshot>();
            foreach (var policy in World.Content.All<IFleetPolicy>())
                if (VisibleTo(viewer, policy)) values.Add(Pure(policy, null, () => new FleetPolicySnapshot(policy)));
            values.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            foreach (var value in values) destination.Add(value);
        }
        private static MotionNavigationSelectionPreview Refused(Code blocker)
            => new MotionNavigationSelectionPreview(blocker, Array.Empty<MotionNavigationSelectionEntry>());
        private Code ControlledFleet(PlayerId company, string id, out FleetSnapshot fleet, out IFleetPolicy policy)
        {
            fleet = null; policy = null;
            World.RequireHealthyUpdate();
            if (!World.Players.IsSeat(company)) return Codes.NotSeated;
            if (!TryRead(company, id, out fleet)) return Codes.NotFound;
            if (!fleet.Blocker.IsNone) return fleet.Blocker;
            if (fleet.Settings.Members.Count == 0) return FleetCodes.Empty;
            policy = Definition(fleet.Settings);
            return VisibleTo(company, policy) ? Code.None : Codes.UnknownDef;
        }
        public MotionNavigationSelectionPreview PreviewNavigation(PlayerId company, string id, string profile)
        {
            Code admission = ControlledFleet(company, id, out var fleet, out var policy);
            if (!admission.IsNone) return Refused(admission);
            bool declared = Pure(policy, null, () => {
                foreach (var allowed in policy.NavigationClasses) if (allowed == profile) return true;
                return false;
            });
            if (!declared) return Refused(MotionNavigationCodes.NotAllowed);
            Code refusal = Pure(policy, null, () => policy.CheckNavigation(profile));
            return refusal.IsNone ? MotionNavigationContracts.PreviewGroup(World, company, fleet.Settings.Members, profile) : Refused(refusal);
        }
        public int ConfigureNavigation(PlayerId company, string id, string profile)
        {
            if (_checking) throw new InvalidOperationException("fleet navigation during pure policy evaluation");
            var preview = PreviewNavigation(company, id, profile);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("fleet navigation refused: " + preview.Blocker);
            return MotionNavigationContracts.ConfigureGroup(World, company, _fleets[(company.Value, id)].Members, profile);
        }
        public WorkPolicySelectionPreview PreviewWorkPolicy(PlayerId company, string id, WorkPolicySettings settings)
        {
            Code admission = ControlledFleet(company, id, out var fleet, out _);
            return admission.IsNone ? WorkPolicyRules.PreviewGroup(World, company, fleet.Settings.Members, settings)
                : new WorkPolicySelectionPreview(admission, Array.Empty<WorkPolicySelectionEntry>());
        }
        public int ConfigureWorkPolicy(PlayerId company, string id, WorkPolicySettings settings)
        {
            if (_checking) throw new InvalidOperationException("fleet work policy during pure policy evaluation");
            var preview = PreviewWorkPolicy(company, id, settings);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("fleet work policy refused: " + preview.Blocker);
            return WorkPolicyRules.ConfigureGroup(World, company, _fleets[(company.Value, id)].Members, settings);
        }
        public WorkerHomeSelectionPreview PreviewHome(PlayerId company, string id, EntityId home)
        {
            Code admission = ControlledFleet(company, id, out var fleet, out _);
            return admission.IsNone ? WorkerHomeContracts.PreviewGroup(World, company, fleet.Settings.Members, home)
                : new WorkerHomeSelectionPreview(admission, Array.Empty<WorkerHomeSelectionEntry>());
        }
        public int ConfigureHome(PlayerId company, string id, EntityId home)
        {
            if (_checking) throw new InvalidOperationException("fleet home during pure policy evaluation");
            var preview = PreviewHome(company, id, home);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("fleet home refused: " + preview.Blocker);
            return WorkerHomeContracts.ConfigureGroup(World, company, _fleets[(company.Value, id)].Members, home);
        }
        public OutletPolicySelectionPreview PreviewOutletPolicy(PlayerId company, string id, OutletPolicyChange change)
        {
            Code admission = ControlledFleet(company, id, out var fleet, out _);
            return admission.IsNone ? LogisticsPolicyRules.PreviewGroup(World, company, fleet.Settings.Members, change)
                : new OutletPolicySelectionPreview(admission, Array.Empty<OutletPolicySelectionEntry>());
        }
        public int ConfigureOutletPolicy(PlayerId company, string id, OutletPolicyChange change)
        {
            if (_checking) throw new InvalidOperationException("fleet outlet control during pure policy evaluation");
            var preview = PreviewOutletPolicy(company, id, change);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("fleet outlet control refused: " + preview.Blocker);
            return LogisticsPolicyRules.ConfigureGroup(World, company, _fleets[(company.Value, id)].Members, change);
        }
        public RecurringPlanSelectionPreview PreviewRecurringPlan(PlayerId company, string id, Code source, RecurringPlanSettings settings)
        {
            Code admission = ControlledFleet(company, id, out var fleet, out _);
            return admission.IsNone ? RecurringPlanContracts.PreviewGroup(World, company, fleet.Settings.Members, source, settings)
                : new RecurringPlanSelectionPreview(admission, Array.Empty<RecurringPlanSelectionEntry>());
        }
        public int ConfigureRecurringPlan(PlayerId company, string id, Code source, RecurringPlanSettings settings)
        {
            if (_checking) throw new InvalidOperationException("fleet recurring control during pure policy evaluation");
            var preview = PreviewRecurringPlan(company, id, source, settings);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("fleet recurring control refused: " + preview.Blocker);
            return RecurringPlanContracts.ConfigureGroup(World, company, _fleets[(company.Value, id)].Members, source, settings);
        }
        public HaulTripSelectionPreview PreviewTripPolicy(PlayerId company, string id, HaulTripSettings settings)
        {
            Code admission = ControlledFleet(company, id, out var fleet, out _);
            return admission.IsNone ? HaulTripPolicyContracts.PreviewGroup(World, company, fleet.Settings.Members, settings)
                : new HaulTripSelectionPreview(admission, Array.Empty<HaulTripSelectionEntry>());
        }
        public int ConfigureTripPolicy(PlayerId company, string id, HaulTripSettings settings)
        {
            if (_checking) throw new InvalidOperationException("fleet trip policy during pure policy evaluation");
            var preview = PreviewTripPolicy(company, id, settings);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("fleet trip policy refused: " + preview.Blocker);
            return HaulTripPolicyContracts.ConfigureGroup(World, company, _fleets[(company.Value, id)].Members, settings);
        }
        public HaulLoadSelectionPreview PreviewLoadPolicy(PlayerId company, string id, HaulLoadSettings settings)
        {
            Code admission = ControlledFleet(company, id, out var fleet, out _);
            return admission.IsNone ? HaulLoadPolicyContracts.PreviewGroup(World, company, fleet.Settings.Members, settings)
                : new HaulLoadSelectionPreview(admission, Array.Empty<HaulLoadSelectionEntry>());
        }
        public int ConfigureLoadPolicy(PlayerId company, string id, HaulLoadSettings settings)
        {
            if (_checking) throw new InvalidOperationException("fleet load policy during pure policy evaluation");
            var preview = PreviewLoadPolicy(company, id, settings);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("fleet load policy refused: " + preview.Blocker);
            return HaulLoadPolicyContracts.ConfigureGroup(World, company, _fleets[(company.Value, id)].Members, settings);
        }
        private void ValidateState()
        {
            var members = new HashSet<EntityId>();
            foreach (var fleet in _fleets)
            {
                if (!World.Players.IsSeat(new PlayerId(fleet.Key.Company)) || !ValidId(fleet.Key.Id) || Definition(fleet.Value) == null)
                    throw new CorruptStateException("invalid saved fleet identity/company/profile");
                foreach (var member in fleet.Value.Members)
                    if (member.Value >= World.Entities.NextIdentity.Value || !members.Add(member))
                        throw new CorruptStateException("invalid unallocated/conflicting saved fleet member");
            }
        }
        public void Sync(IStateSync sync)
        {
            if (!sync.Reading) ValidateState(); int count = sync.Count(_fleets.Count);
            if (sync.Reading)
            {
                _fleets.Clear(); (int, string)? previous = null;
                for (int i = 0; i < count; i++)
                {
                    int company = 0; string id = null; sync.Int(ref company); sync.String(ref id);
                    var key = (company, id);
                    if (previous.HasValue && _fleets.Comparer.Compare(previous.Value, key) >= 0) throw new CorruptStateException("noncanonical fleet identities");
                    _fleets.Add(key, FleetSettings.Sync(sync, null)); previous = key;
                }
                ValidateState();
            }
            else foreach (var fleet in _fleets)
            { int company = fleet.Key.Company; string id = fleet.Key.Id; sync.Int(ref company); sync.String(ref id); FleetSettings.Sync(sync, fleet.Value); }
        }
    }
}
