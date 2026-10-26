using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>One detached policy value, never request age, cargo or reservation state.</summary>
    public sealed class HaulLoadSettings
    {
        public string Profile { get; }
        public bool Enabled { get; }
        public Bill MinimumLoad { get; }
        public GameDuration MaximumWait { get; }
        public bool EmergencyBypass { get; }
        public HaulLoadSettings(string profile, bool enabled, Bill minimumLoad, GameDuration maximumWait, bool emergencyBypass)
        {
            if (string.IsNullOrWhiteSpace(profile) || minimumLoad == null) throw new ArgumentException("explicit rule identity and native minimums required");
            Profile = profile; Enabled = enabled; MinimumLoad = minimumLoad; MaximumWait = maximumWait; EmergencyBypass = emergencyBypass;
        }
        internal bool Matches(HaulLoadSettings other)
        {
            if (other == null || Profile != other.Profile || Enabled != other.Enabled || EmergencyBypass != other.EmergencyBypass ||
                !MaximumWait.Equals(other.MaximumWait) || MinimumLoad.Count != other.MinimumLoad.Count) return false;
            for (int i = 0; i < MinimumLoad.Count; i++) if (!MinimumLoad[i].Equals(other.MinimumLoad[i])) return false;
            return true;
        }
        public static HaulLoadSettings Sync(IStateSync state, HaulLoadSettings current)
        {
            bool present = current != null; state.Bool(ref present); if (!present) return null;
            string profile = current?.Profile; bool enabled = current?.Enabled ?? false, emergency = current?.EmergencyBypass ?? false;
            state.String(ref profile); state.Bool(ref enabled); state.Bool(ref emergency);
            GameDuration duration = GameDuration.Sync(state, current?.MaximumWait ?? GameDuration.Zero);
            int count = state.Count(current?.MinimumLoad.Count ?? 0); var lines = state.Reading ? new List<ResourceAmount>() : null;
            ResourceId previous = default;
            for (int i = 0; i < count; i++)
            {
                ResourceId resource = state.Reading ? default : current.MinimumLoad[i].Resource;
                Fix amount = state.Reading ? Fix.Zero : current.MinimumLoad[i].Amount;
                state.Resource(ref resource); state.Fix(ref amount);
                if (!resource.IsValid || !amount.IsPositive || i > 0 && previous.CompareTo(resource) >= 0)
                    throw new CorruptStateException("noncanonical native haul minimum");
                previous = resource; lines?.Add(new ResourceAmount(resource, amount));
            }
            if (!state.Reading) return current;
            try { return new HaulLoadSettings(profile, enabled, new Bill(lines), duration, emergency); }
            catch (ArgumentException) { throw new CorruptStateException("invalid saved haul load settings"); }
        }
    }
    public sealed class HaulLoadPolicySnapshot
    {
        public HaulLoadSettings Settings { get; }
        public string Profile => Settings.Profile;
        public bool Enabled => Settings.Enabled;
        public Bill MinimumLoad => Settings.MinimumLoad;
        public bool EmergencyBypass => Settings.EmergencyBypass;
        public long MaximumWaitTicks { get; }
        internal HaulLoadPolicySnapshot(HaulLoadSettings settings, ISimulationTime time)
        { Settings = settings; MaximumWaitTicks = time.Ticks(settings.MaximumWait); }
    }
    public readonly struct HaulLoadContext
    {
        public ISimulationTime Time { get; }
        public ResourceId Resource { get; }
        public Fix Amount { get; }
        public long WaitAgeTicks { get; }
        public bool Emergency { get; }
        public HaulLoadContext(ISimulationTime time, ResourceId resource, Fix amount, long waitAgeTicks, bool emergency)
        {
            if (time == null || !resource.IsValid || !amount.IsPositive || waitAgeTicks < 0) throw new ArgumentException("actual positive load and request age required");
            Time = time; Resource = resource; Amount = amount; WaitAgeTicks = waitAgeTicks; Emergency = emergency;
        }
    }
    public interface IHaulLoadProfile : IContentDefinition
    {
        HaulLoadSettings Initial { get; }
        Code CheckLoad(HaulLoadContext context, HaulLoadSettings settings);
    }
    public sealed class AuthoredHaulLoadProfileDef : Def, IHaulLoadProfile
    {
        private HaulLoadProfileDeclaration _declaration;
        public HaulLoadSettings Initial => _declaration.Initial;
        protected override void ReadCore(JsonReader reader) => _declaration = HaulLoadProfileDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content, Id);
        public Code CheckLoad(HaulLoadContext context, HaulLoadSettings settings) => _declaration.CheckLoad(context, settings);
    }
    /// <summary>Sole native loading declaration; request age, cargo and claims remain coordinator-owned.</summary>
    public sealed class HaulLoadProfileDeclaration
    {
        private HaulLoadProfileDeclaration() { }
        public static HaulLoadProfileDeclaration Read(JsonReader reader)
        { var declaration = new HaulLoadProfileDeclaration(); declaration.ReadCore(reader); return declaration; }
        private BillSpec _minimum;
        private GameDuration _wait;
        private bool _enabled, _emergency;
        private HaulLoadSettings _initial;
        public HaulLoadSettings Initial => _initial;
        private void ReadCore(JsonReader reader)
        {
            _enabled = Boolean(reader, "enabled"); _emergency = Boolean(reader, "emergency_bypass");
            if (reader.Value("minimum_load")?.Kind != JsonKind.Object) throw reader.Error("minimum_load must be an explicit resource object");
            _minimum = BillSpec.Read(reader, "minimum_load");
            _wait = reader.Child("maximum_wait")?.RationalDuration() ?? throw reader.Error("maximum_wait must be explicit");
        }
        private static bool Boolean(JsonReader reader, string key)
        {
            JsonValue value = reader.Value(key);
            if (value?.Kind != JsonKind.Bool) throw reader.Error(key + " must be an explicit boolean");
            return value.Bool;
        }
        public void Resolve(ContentResolver content, string id)
        {
            if (_minimum == null) throw new InvalidOperationException("haul load declaration already resolved");
            _initial = new HaulLoadSettings(id, _enabled, _minimum.Resolve(content), _wait, _emergency); _minimum = null;
        }
        public Code CheckLoad(HaulLoadContext context, HaulLoadSettings settings)
            => !settings.Enabled ? HaulLoadPolicyCodes.Disabled : settings.EmergencyBypass && context.Emergency ||
                context.Amount >= settings.MinimumLoad.AmountOf(context.Resource) || context.WaitAgeTicks >= context.Time.Ticks(settings.MaximumWait)
                ? Code.None : HaulLoadPolicyCodes.Minimum;
    }
    public interface IHaulLoadPolicy : IEntityCapability
    {
        HaulLoadSettings ReadLoadPolicy();
        Code CheckLoadPolicy(World world, HaulLoadSettings settings);
        void ConfigureLoadPolicy(World world, HaulLoadSettings settings);
    }
    public interface IHaulLoadPolicyDefinition : IComponentDefinition { IHaulLoadProfile InitialProfile { get; } }
    public static class HaulLoadPolicyCodes
    {
        public static readonly Code NotPolicy = new Code("logistics.no-load-policy"), Invalid = new Code("logistics.invalid-load-profile"),
            Disabled = new Code("logistics.load-disabled"), Minimum = new Code("logistics.load-minimum");
    }
    public static partial class HaulLoadPolicyContracts
    {
        internal static bool CanDisclose(World world, PlayerId viewer, HaulLoadSettings settings, bool knownReferencesOnly = false)
        {
            world.RequireHealthyUpdate(); if (settings == null) return true;
            var profile = world.Content.Find<IHaulLoadProfile>(settings.Profile);
            if (profile == null) return knownReferencesOnly;
            var view = world.ViewFor(viewer);
            return view.CanSeeContent(profile) && view.CanSeeResources(settings.MinimumLoad.Lines, knownReferencesOnly);
        }
        public static Code CheckSettings(World world, Entity entity, HaulLoadSettings settings)
        {
            if (world == null || entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity)) return Codes.NotFound;
            var definition = settings == null ? null : world.Content.Find<IHaulLoadProfile>(settings.Profile);
            if (definition == null || !entity.Has<ITransporter>()) return HaulLoadPolicyCodes.Invalid;
            var cargo = TransporterContracts.ReadCargo(world, entity);
            return CheckCapacity(world.Content, world.Time, cargo.CapacityPolicy, resource => cargo.Accepts(world.Content, resource), settings);
        }
        private static Code CheckCapacity(ResolvedContent content, ISimulationTime time, ICapacityPolicy capacity, Func<ResourceId, bool> accepts, HaulLoadSettings settings)
        {
            if (settings == null) return HaulLoadPolicyCodes.Invalid;
            try { if (time.Ticks(settings.MaximumWait) < 0) return HaulLoadPolicyCodes.Invalid; }
            catch (ArgumentException) { return HaulLoadPolicyCodes.Invalid; }
            catch (OverflowException) { return HaulLoadPolicyCodes.Invalid; }
            foreach (ResourceAmount line in settings.MinimumLoad.Lines)
                if (content.Resource(line.Resource) == null || !accepts(line.Resource) || !LogisticsPolicyRules.Native(capacity, line.Resource, line.Amount) ||
                    !CapacityPolicyContracts.Check(capacity, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), new[] { line }).Fits) return HaulLoadPolicyCodes.Invalid;
            return Code.None;
        }
        public static void ValidateDefinition(ResolvedContent content, ISimulationTime time, EntityDef entity, IHaulLoadProfile profile)
        {
            var stock = entity.Component<IInventoryDefinition>();
            if (!content.Owns(profile) || !ReferenceEquals(content.Find<IHaulLoadProfile>(profile.Id), profile) || profile.Initial?.Profile != profile.Id || !entity.HasCapability<ITransporter>() || stock == null ||
                !CheckCapacity(content, time, stock.CapacityPolicy, resource => stock.Accepts(content, resource), profile.Initial).IsNone)
                throw new ContentException(entity.Id + ": load profile needs actual native cargo capacity/resources/time");
        }
        public static HaulLoadSettings Read(World world, Entity entity)
        {
            world.RequireHealthyUpdate();
            var policy = entity.Get<IHaulLoadPolicy>();
            if (policy == null) return null;
            using var providerCall = EntityObservation.EnterProviderCall(world, policy, "logistics.load-policy.read");
            return EntityObservation.ReadProvider(world, policy, entity, () =>
            {
                if (!ReferenceEquals(policy.Entity, entity)) throw new InvalidOperationException("load policy must belong to its actual carrier");
                var settings = policy.ReadLoadPolicy();
                if (settings == null || !CheckSettings(world, entity, settings).IsNone)
                    throw new InvalidOperationException("load policy must expose explicit compatible session settings");
                return settings;
            }, space: world.Space, observeTargetState: true);
        }
        internal static Code CheckLoad(World world, Entity entity, ResourceId resource, Fix amount, long waitAge, bool emergency, out HaulLoadSettings observed)
        {
            var settings = Read(world, entity);
            observed = settings;
            if (settings == null) return Code.None;
            if (!settings.Enabled) return HaulLoadPolicyCodes.Disabled;
            var profile = world.Content.Get<IHaulLoadProfile>(settings.Profile);
            using var providerCall = EntityObservation.EnterProviderCall(world, profile, "logistics.load-rule.check");
            return EntityObservation.ReadProvider(world, profile, entity,
                () => profile.CheckLoad(new HaulLoadContext(world.Time, resource, amount, waitAge, emergency), settings),
                space: world.Space, observeTargetState: true, faultPhase: "logistics.load-rule.check");
        }
        public static Code CheckConfigure(World world, PlayerId player, Entity entity, HaulLoadSettings settings)
        {
            if (world == null) return Codes.NotFound;
            world.RequireHealthyUpdate();
            if (entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                entity.Owner != player || !world.ViewFor(player).CanObserveOwned(entity)) return Codes.NotFound;
            if (!CanDisclose(world, player, settings, knownReferencesOnly: true)) return Codes.NotFound;
            return CheckPolicyProvider(world, entity, settings);
        }
        private static Code CheckPolicyProvider(World world, Entity entity, HaulLoadSettings settings)
        {
            var policy = entity?.Get<IHaulLoadPolicy>(); if (policy == null) return HaulLoadPolicyCodes.NotPolicy;
            using var providerCall = EntityObservation.EnterProviderCall(world, policy, "logistics.load-policy.check");
            Read(world, entity); var refusal = EntityObservation.Read(world, entity, () => CheckSettings(world, entity, settings));
            if (!refusal.IsNone) return refusal;
            return EntityObservation.ReadProvider(world, policy, entity,
                () => policy.CheckLoadPolicy(world, settings), space: world.Space, observeTargetState: true);
        }
        public static void Configure(World world, Entity entity, HaulLoadSettings settings)
        {
            var refusal = CheckPolicyProvider(world, entity, settings); if (!refusal.IsNone) throw new InvalidOperationException("load policy refused: " + refusal);
            if (Read(world, entity).Matches(settings)) return;
            int version = entity.Version; PlayerId owner = entity.Owner; long tick = world.Tick; EntityId next = world.Entities.NextIdentity;
            var worker = entity.Get<IWorker>(); int job = worker?.Job ?? 0;
            EntityId home = worker == null ? EntityId.None : WorkerHomeContracts.ReadHome(world, entity);
            bool held = worker?.Held ?? false, arrived = worker?.HeldArrived ?? false;
            var policy = entity.Get<IHaulLoadPolicy>();
            using var providerCall = EntityObservation.EnterProviderCall(world, policy, "logistics.load-policy.commit");
            try
            {
                var envelope = EntityObservation.Capture(world, entity, allowEntityVersionChange: true);
                var spatial = EntityObservation.CaptureSpace(world, world.Space);
                // Only this capability may change. Original sibling codecs retain authority
                // over cargo, promises, worker state and movement; no copied policy world.
                var siblings = EntityObservation.CaptureProvider(world, entity, sync => {
                    foreach (Component component in entity.Components)
                        if (!ReferenceEquals(component, policy)) component.Sync(sync);
                }, "logistics.configure-load", allowEntityVersionChange: true);
                envelope.Verify();
                policy.ConfigureLoadPolicy(world, settings);
                envelope.Verify(); siblings.Verify(); spatial.Verify();
                if (!Read(world, entity).Matches(settings) || entity.Version <= version || entity.Owner != owner || world.Tick != tick || world.Entities.NextIdentity != next ||
                    !ReferenceEquals(world.Entities.Get(entity.Id), entity) || !entity.Alive || worker != null &&
                    (worker.Job != job || WorkerHomeContracts.ReadHome(world, entity) != home || worker.Held != held || worker.HeldArrived != arrived))
                    throw new InvalidOperationException("load policy did not commit exact settings");
            }
            catch (Exception error) { world.LatchFault("logistics.configure-load", error); throw; }
        }
        internal static void ValidateLive(World world)
        {
            var policies = world.Entities.Components<IHaulLoadPolicy>();
            for (int i = 0; i < policies.Count; i++) if (policies[i].Entity.Alive) Read(world, policies[i].Entity);
        }
    }
    public sealed class HaulLoadPolicyDef : ComponentDef<HaulLoadPolicy>, IHaulLoadPolicyDefinition
    {
        private string _profile;
        public IHaulLoadProfile Profile { get; private set; }
        public IHaulLoadProfile InitialProfile => Profile;
        internal ResolvedContent Content { get; private set; }
        public EntityStateBindings States { get; private set; }
        protected override void ReadCore(JsonReader reader) { _profile = reader.String("profile"); States = EntityStateBindings.Read(reader, "disabled"); }
        protected override void ResolveCore(ContentResolver content) { Content = content.Content; Profile = content.Def<IHaulLoadProfile>(_profile); States = States.Resolve(content); }
        protected override void ValidateCore(EntityDef entity)
        { if (!entity.HasCapability<ITransporter>() || entity.Component<IInventoryDefinition>() == null) throw new ContentException(entity.Id + ": load policy requires authored transporter/inventory"); }
    }
    public sealed class HaulLoadPolicy : Component<HaulLoadPolicyDef>, IHaulLoadPolicy, IEntityStateSource
    {
        private HaulLoadSettings _settings;
        public override void OnCreate() => _settings = Def.Profile.Initial;
        public HaulLoadSettings ReadLoadPolicy() => _settings;
        public Code CheckLoadPolicy(World world, HaulLoadSettings settings) => HaulLoadPolicyContracts.CheckSettings(world, Entity, settings);
        public void ConfigureLoadPolicy(World world, HaulLoadSettings settings)
        {
            var refusal = CheckLoadPolicy(world, settings); if (!refusal.IsNone) throw new InvalidOperationException("invalid load settings: " + refusal);
            _settings = settings; Entity.Touch();
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context)
            => !_settings.Enabled && Entity.Get<IWorker>()?.Job == 0 && Entity.Get<IMotion>()?.Status != MoveStatus.Moving
                ? Def.States["disabled"] : null;
        public override void Sync(IStateSync sync)
        {
            HaulLoadSettings settings = HaulLoadSettings.Sync(sync, _settings);
            if (settings == null || Def.Content.Find<IHaulLoadProfile>(settings.Profile) == null)
                throw new CorruptStateException("missing saved load settings/rule");
            _settings = settings;
        }
    }
}
