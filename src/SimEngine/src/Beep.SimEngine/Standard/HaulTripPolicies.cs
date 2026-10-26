using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Trip rules only; native stock/reserves, routes and game time keep their existing owners.</summary>
    public sealed class HaulTripSettings
    {
        public string Profile { get; }
        public bool Enabled { get; }
        public bool PreserveOutletReserve { get; }
        public HaulTripSettings(string profile, bool enabled, bool preserveOutletReserve)
        {
            if (string.IsNullOrWhiteSpace(profile)) throw new ArgumentException("explicit trip profile required");
            Profile = profile; Enabled = enabled; PreserveOutletReserve = preserveOutletReserve;
        }
        internal bool Matches(HaulTripSettings other) => other != null && Profile == other.Profile &&
            Enabled == other.Enabled && PreserveOutletReserve == other.PreserveOutletReserve;
        public static HaulTripSettings Sync(IStateSync state, HaulTripSettings current)
        {
            bool present = current != null; state.Bool(ref present); if (!present) return null;
            string profile = current?.Profile; bool enabled = current?.Enabled ?? false, preserve = current?.PreserveOutletReserve ?? false;
            state.String(ref profile); state.Bool(ref enabled); state.Bool(ref preserve);
            if (!state.Reading) return current;
            try { return new HaulTripSettings(profile, enabled, preserve); }
            catch (ArgumentException) { throw new CorruptStateException("invalid saved trip settings"); }
        }
    }
    public sealed class HaulTripEvaluation
    {
        public HaulTripSettings Settings { get; }
        public MotionTravelEstimate Travel { get; }
        public OperatingInputEvaluation Operating { get; }
        public Bill ReserveFloor { get; }
        public Bill ReturnCommitment { get; }
        public Bill RequiredStock { get; }
        public Code Blocker { get; }
        internal HaulTripEvaluation(HaulTripSettings settings, MotionTravelEstimate travel,
            OperatingInputEvaluation operating, Bill reserve, Bill returning, Bill required, Code blocker)
        { Settings = settings; Travel = travel; Operating = operating; ReserveFloor = reserve;
            ReturnCommitment = returning; RequiredStock = required; Blocker = blocker; }
    }
    public interface IHaulTripProfile : IContentDefinition
    {
        HaulTripSettings Initial { get; }
        IReadOnlyList<Code> ServiceSources { get; }
        Code CheckTrip(HaulTripEvaluation context);
    }
    public sealed class AuthoredHaulTripProfileDef : Def, IHaulTripProfile
    {
        private HaulTripProfileDeclaration _declaration;
        public HaulTripSettings Initial => _declaration.Initial;
        public IReadOnlyList<Code> ServiceSources => _declaration.ServiceSources;
        protected override void ReadCore(JsonReader reader) => _declaration = HaulTripProfileDeclaration.Read(reader, Id);
        public Code CheckTrip(HaulTripEvaluation context) => _declaration.CheckTrip(context);
    }
    /// <summary>Sole native trip declaration; funding/exposure/route/service state keep existing owners.</summary>
    public sealed class HaulTripProfileDeclaration
    {
        private HaulTripProfileDeclaration() { }
        public static HaulTripProfileDeclaration Read(JsonReader reader, string id)
        { var declaration = new HaulTripProfileDeclaration(); declaration.ReadCore(reader, id); return declaration; }
        private HaulTripSettings _initial;
        private IReadOnlyList<Code> _serviceSources;
        public HaulTripSettings Initial => _initial;
        public IReadOnlyList<Code> ServiceSources => _serviceSources;
        private void ReadCore(JsonReader reader, string id)
        {
            bool Boolean(string key)
            { var value = reader.Value(key); if (value?.Kind != JsonKind.Bool) throw reader.Error(key + " must be an explicit boolean"); return value.Bool; }
            _initial = new HaulTripSettings(id, Boolean("enabled"), Boolean("preserve_outlet_reserve"));
            var sources = reader.Array("service_sources") ?? throw reader.Error("explicit service_sources array required");
            var ids = new HashSet<string>(StringComparer.Ordinal); var values = new List<Code>();
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i].Kind != JsonKind.String) throw reader.Error("service source must be a named capability");
                try { ContractIdentity.ValidateId(sources[i].Text, "trip service source"); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
                if (!ids.Add(sources[i].Text)) throw reader.Error("duplicate trip service source");
                values.Add(new Code(sources[i].Text));
            }
            _serviceSources = values.AsReadOnly();
        }
        public Code CheckTrip(HaulTripEvaluation context) => context.Blocker;
    }
    public interface IHaulTripPolicy : IEntityCapability
    {
        HaulTripSettings ReadTripPolicy();
        Code CheckTripPolicy(World world, HaulTripSettings settings);
        void ConfigureTripPolicy(World world, HaulTripSettings settings);
    }
    public interface IHaulTripPolicyDefinition : IComponentDefinition { IHaulTripProfile InitialProfile { get; } }
    public static class HaulTripPolicyCodes
    {
        public static readonly Code NotPolicy = new Code("logistics.no-trip-policy"), Invalid = new Code("logistics.invalid-trip-policy"),
            Disabled = new Code("logistics.trip-disabled"), Reserve = new Code("logistics.trip-reserve"), Cause = new Code("logistics.trip-budget"),
            NotHauling = new Code("logistics.no-active-haul");
    }
    public static partial class HaulTripPolicyContracts
    {
        internal static bool CanDisclose(World world, PlayerId viewer, HaulTripSettings settings)
        {
            world.RequireHealthyUpdate();
            return settings == null || world.ViewFor(viewer).CanSeeContent(world.Content.Get<IHaulTripProfile>(settings.Profile));
        }

        internal static bool CanDisclose(World world, PlayerId viewer, HaulTripEvaluation value)
        {
            world.RequireHealthyUpdate(); if (value == null) return true;
            var view = world.ViewFor(viewer);
            return CanDisclose(world, viewer, value.Settings)
                && view.CanSeeResources(value.ReserveFloor.Lines) && view.CanSeeResources(value.ReturnCommitment.Lines)
                && view.CanSeeResources(value.RequiredStock.Lines)
                && (value.Operating == null || OperatingInputsContracts.CanDisclose(world, viewer, value.Operating));
        }

        public static Code CheckSettings(World world, Entity entity, HaulTripSettings settings)
        {
            if (world == null) return Codes.NotFound;
            world.RequireHealthyUpdate();
            if (entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity)) return Codes.NotFound;
            var profile = settings == null ? null : world.Content.Find<IHaulTripProfile>(settings.Profile);
            if (profile == null || !entity.Has<IWorker>() || !entity.Has<IMotion>() || !entity.Has<ITransporter>() ||
                entity.Get<IInventory>() == null || settings.PreserveOutletReserve && !entity.Has<IOutletPolicy>()) return HaulTripPolicyCodes.Invalid;
            TransporterContracts.ReadCargo(world, entity);
            ReadServiceSources(world, entity, profile, requireCompatible: false, out bool compatible);
            return compatible ? Code.None : HaulTripPolicyCodes.Invalid;
        }
        // One runtime capture for validation, management and service-window consumers. No
        // cached policy copy: each caller receives its actual detached, observed metadata.
        internal static IReadOnlyList<Code> ReadServiceSources(World world, Entity entity, IHaulTripProfile profile,
            bool requireCompatible, out bool compatible)
        {
            compatible = false;
            using var providerCall = EntityObservation.EnterProviderCall(world, profile, "logistics.trip-sources.read");
            var captured = EntityObservation.ReadProvider(world, profile, entity, () => {
                if (!world.Content.Owns(profile) || !ReferenceEquals(world.Content.Find<IHaulTripProfile>(profile.Id), profile))
                    throw new InvalidOperationException("trip service metadata requires its actual session catalog alias");
                var declared = profile.ServiceSources;
                IReadOnlyList<Code> sources = declared == null ? null : new List<Code>(declared).AsReadOnly();
                bool valid = ValidServiceSources(entity.Def, sources);
                if (requireCompatible && !valid) throw new InvalidOperationException("actual trip service sources must match this worker's scheduled bindings");
                return (Sources: sources, Compatible: valid);
            }, space: world.Space, observeTargetState: true, faultPhase: "logistics.trip-sources.read");
            compatible = captured.Compatible; return captured.Sources;
        }
        private static bool ValidServiceSources(EntityDef entity, IReadOnlyList<Code> sources)
        {
            if (sources == null) return false;
            var seen = new HashSet<Code>();
            foreach (Code id in sources)
            {
                if (id.IsNone || !seen.Add(id)) return false;
                var source = entity.Component<IRecurringPlanBindingDefinition>(id);
                if (source?.Binding == null || source.Template != null || source.Binding.WorkerNodes.Count == 0 ||
                    source.Cadence != RecurringCadenceBasis.SuccessfulCompletion || source.TriggerMode == RecurringTriggerMode.Maintenance ||
                    !typeof(IRecurringPlanSource).IsAssignableFrom(source.ComponentType)) return false;
            }
            return true;
        }
        public static void ValidateDefinition(ResolvedContent content, EntityDef entity, IHaulTripProfile profile)
        {
            if (profile == null || !content.Owns(profile) || !ReferenceEquals(content.Find<IHaulTripProfile>(profile.Id), profile))
                throw new ContentException(entity.Id + ": trip profile must alias its actual catalog definition");
            var initial = profile.Initial;
            if (initial == null || initial.Profile != profile.Id ||
                !entity.HasCapability<IWorker>() || !entity.HasCapability<IMotion>() || !entity.HasCapability<ITransporter>() ||
                entity.Component<IInventoryDefinition>() == null || initial.PreserveOutletReserve && !entity.HasCapability<IOutletPolicy>() ||
                !ValidServiceSources(entity, profile.ServiceSources))
                throw new ContentException(entity.Id + ": trip policy requires actual worker/motion/transporter/inventory, selected reserve provider and compatible scheduled service sources");
        }
        public static HaulTripSettings Read(World world, Entity entity)
        {
            world.RequireHealthyUpdate();
            var policy = entity.Get<IHaulTripPolicy>(); if (policy == null) return null;
            using var providerCall = EntityObservation.EnterProviderCall(world, policy, "logistics.trip-policy.read");
            return EntityObservation.ReadProvider(world, policy, entity, () =>
            {
                if (!ReferenceEquals(policy.Entity, entity)) throw new InvalidOperationException("trip policy must alias actual entity");
                var settings = policy.ReadTripPolicy();
                if (!CheckSettings(world, entity, settings).IsNone) throw new InvalidOperationException("invalid actual trip policy settings");
                return settings;
            }, space: world.Space, observeTargetState: true);
        }
        public static HaulTripEvaluation CheckTrip(World world, Entity entity, MotionTravelEstimate travel,
            ResourceId returningResource = default, Fix returningAmount = default, bool emergency = false)
        {
            if (returningAmount < Fix.Zero || returningAmount.IsPositive && !returningResource.IsValid)
                throw new ArgumentException("return commitment requires actual nonnegative native cargo");
            var settings = Read(world, entity); if (settings == null) return null;
            // Validate estimate shape with the same sole checked-duration protocol.
            MotionArrivalContracts.Combine(travel, new MotionTravelEstimate(0, Code.None));
            return EntityObservation.Read(world, entity, () =>
            {
                OperatingInputEvaluation operating = null; Bill reserve = Bill.Empty, returning = Bill.Empty, required = Bill.Empty;
                Code blocker = !settings.Enabled ? HaulTripPolicyCodes.Disabled : !travel.RemainingTicks.HasValue ? travel.Blocker : Code.None;
                if (blocker.IsNone)
                {
                    operating = OperatingInputsContracts.Read(world, entity, OperatingPhase.Movement, travel.RemainingTicks.Value);
                    blocker = operating.Blocker;
                    var store = entity.Get<IInventory>();
                    if (settings.PreserveOutletReserve)
                    {
                        var outlet = LogisticsPolicyRules.Read(world, store.Entity);
                        if (outlet == null) throw new InvalidOperationException("trip reserve lost actual outlet settings");
                        reserve = emergency && outlet.ReserveAccess == LogisticsReserveAccess.EmergencyOnly ? Bill.Empty : outlet.Reserve;
                    }
                    if (returningAmount.IsPositive && ReferenceEquals(TransporterContracts.ReadCargo(world, entity), store))
                        returning = new Bill(new[] { new ResourceAmount(returningResource, returningAmount) });
                    if (blocker != OperatingInputCodes.Range)
                    {
                        try
                        {
                            var lines = new List<ResourceAmount>(); lines.AddRange(operating.RequiredInputs.Lines);
                            lines.AddRange(reserve.Lines); lines.AddRange(returning.Lines); required = new Bill(lines);
                            if (blocker.IsNone)
                            {
                                var status = world.Services.Get<IResourceTransaction>().EvaluateInventory(new ResourceInventoryIntent(
                                    new ReservationOwner("haul.trip", entity.Id.Value), entity.Owner, entity.Id, required,
                                    ResourceFlowKind.Consumed, HaulTripPolicyCodes.Cause)).Status;
                                if (status == ResourceTransferStatus.NoStock) blocker = HaulTripPolicyCodes.Reserve;
                                else if (status != ResourceTransferStatus.Ready) throw new InvalidOperationException("trip budget inventory refused: " + status);
                            }
                        }
                        catch (OverflowException) { required = Bill.Empty; blocker = OperatingInputCodes.Range; }
                    }
                }
                var value = new HaulTripEvaluation(settings, travel, operating, reserve, returning, required, blocker);
                if (!blocker.IsNone) return value;
                var profile = world.Content.Get<IHaulTripProfile>(settings.Profile);
                using var providerCall = EntityObservation.EnterProviderCall(world, profile, "logistics.trip-rule.check");
                Code rule = EntityObservation.ReadProvider(world, profile, entity, () => profile.CheckTrip(value),
                    space: world.Space, observeTargetState: true, faultPhase: "logistics.trip-rule.check");
                return rule.IsNone ? value : new HaulTripEvaluation(settings, travel, operating, reserve, returning, required, rule);
            });
        }
        public static Code CheckConfigure(World world, PlayerId player, Entity entity, HaulTripSettings settings)
        {
            if (world == null) return Codes.NotFound;
            world.RequireHealthyUpdate();
            if (entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                entity.Owner != player || !world.ViewFor(player).CanObserveOwned(entity)) return Codes.NotFound;
            if (settings != null && world.Content.Find<IHaulTripProfile>(settings.Profile) != null &&
                !CanDisclose(world, player, settings)) return Codes.NotFound;
            return CheckPolicyProvider(world, entity, settings);
        }
        private static Code CheckPolicyProvider(World world, Entity entity, HaulTripSettings settings)
        {
            var policy = entity?.Get<IHaulTripPolicy>(); if (policy == null) return HaulTripPolicyCodes.NotPolicy;
            using var providerCall = EntityObservation.EnterProviderCall(world, policy, "logistics.trip-policy.check");
            Read(world, entity); var refusal = EntityObservation.Read(world, entity, () => CheckSettings(world, entity, settings));
            if (!refusal.IsNone) return refusal;
            return EntityObservation.ReadProvider(world, policy, entity,
                () => policy.CheckTripPolicy(world, settings), space: world.Space, observeTargetState: true);
        }
        /// <summary>Fresh selected movement only. Retained cargo continues through the existing physical trip budget.</summary>
        public static Code CheckDispatch(World world, Entity entity, MotionTravelEstimate travel, out ServiceMovementWindow observed, bool emergency = false)
        {
            observed = null;
            var settings = Read(world, entity); if (settings == null) return Code.None;
            MotionArrivalContracts.Combine(travel, new MotionTravelEstimate(0, Code.None));
            if (!settings.Enabled) return HaulTripPolicyCodes.Disabled;
            if (!travel.RemainingTicks.HasValue) return travel.Blocker;
            if (emergency) return Code.None;
            observed = ReadServiceWindow(world, entity);
            return observed.CheckMovement(travel.RemainingTicks.Value);
        }
        /// <summary>Same canonical multi-source readiness used by private views and fresh dispatch.</summary>
        public static ServiceMovementWindow ReadServiceWindow(World world, Entity entity)
        {
            var settings = Read(world, entity); if (settings == null) return null;
            if (!settings.Enabled) return new ServiceMovementWindow(world, Code.None, false, null, HaulTripPolicyCodes.Disabled);
            return EntityObservation.Read(world, entity, () =>
            {
                var profile = world.Content.Get<IHaulTripProfile>(settings.Profile);
                var ids = new List<Code>(ReadServiceSources(world, entity, profile, requireCompatible: true, out _));
                ids.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
                ServiceMovementWindow selected = null;
                var inputs = new List<(EntityId Entity, Code Source)>();
                foreach (Code id in ids)
                {
                    var window = RecurringPlanContracts.ReadServiceWindow(world, entity.Get<IRecurringPlanSource>(id));
                    if (window.Unrestricted) continue;
                    foreach (var input in window.SourceInputs) if (!inputs.Contains(input)) inputs.Add(input);
                    if (selected == null || selected.Blocker.IsNone &&
                        (!window.Blocker.IsNone || window.DueUpdateStep.Value < selected.DueUpdateStep.Value)) selected = window;
                }
                return selected == null ? new ServiceMovementWindow(world, Code.None, true, null, Code.None)
                    : new ServiceMovementWindow(world, selected.Source, false, selected.DueUpdateStep, selected.Blocker, inputs);
            });
        }
        public static void Configure(World world, Entity entity, HaulTripSettings settings)
        {
            var refusal = CheckPolicyProvider(world, entity, settings); if (!refusal.IsNone) throw new InvalidOperationException("trip settings refused: " + refusal);
            if (Read(world, entity).Matches(settings)) return;
            int version = entity.Version; var owner = entity.Owner; long time = world.Tick; var next = world.Entities.NextIdentity;
            var worker = entity.Get<IWorker>(); int job = worker.Job;
            EntityId home = WorkerHomeContracts.ReadHome(world, entity);
            bool held = worker.Held, arrived = worker.HeldArrived;
            var policy = entity.Get<IHaulTripPolicy>();
            using var providerCall = EntityObservation.EnterProviderCall(world, policy, "logistics.trip-policy.commit");
            try
            {
                var envelope = EntityObservation.Capture(world, entity, allowEntityVersionChange: true);
                var spatial = EntityObservation.CaptureSpace(world, world.Space);
                // Only this capability may change. Observe every sibling through its original
                // codec; do not copy inventories, promises or worker state into another model.
                var siblings = EntityObservation.CaptureProvider(world, entity, sync => {
                    foreach (Component component in entity.Components)
                        if (!ReferenceEquals(component, policy)) component.Sync(sync);
                }, "logistics.configure-trip", allowEntityVersionChange: true);
                envelope.Verify();
                policy.ConfigureTripPolicy(world, settings);
                envelope.Verify(); siblings.Verify(); spatial.Verify();
                if (!Read(world, entity).Matches(settings) || entity.Version <= version || entity.Owner != owner || world.Tick != time || world.Entities.NextIdentity != next ||
                    !ReferenceEquals(world.Entities.Get(entity.Id), entity) || !entity.Alive || worker.Job != job ||
                    WorkerHomeContracts.ReadHome(world, entity) != home || worker.Held != held || worker.HeldArrived != arrived)
                    throw new InvalidOperationException("trip policy did not commit exact settings");
                world.Services.Get<IWorkerDispatch>().RefreshPolicy(world, entity);
            }
            catch (Exception error) { world.LatchFault("logistics.configure-trip", error); throw; }
        }
        internal static void ValidateLive(World world)
        { var all = world.Entities.Components<IHaulTripPolicy>(); for (int i = 0; i < all.Count; i++) if (all[i].Entity.Alive) Read(world, all[i].Entity); }
    }
    public sealed class HaulTripPolicyDef : ComponentDef<HaulTripPolicy>, IHaulTripPolicyDefinition
    {
        private string _profile;
        internal ResolvedContent Content { get; private set; }
        public IHaulTripProfile Profile { get; private set; }
        public IHaulTripProfile InitialProfile => Profile;
        protected override void ReadCore(JsonReader reader) => _profile = reader.String("profile");
        protected override void ResolveCore(ContentResolver content) { Content = content.Content; Profile = content.Def<IHaulTripProfile>(_profile); }
        protected override void ValidateCore(EntityDef entity) => HaulTripPolicyContracts.ValidateDefinition(Content, entity, Profile);
    }
    public sealed class HaulTripPolicy : Component<HaulTripPolicyDef>, IHaulTripPolicy
    {
        private HaulTripSettings _settings;
        public override void OnCreate() => _settings = Def.Profile.Initial;
        public HaulTripSettings ReadTripPolicy() => _settings;
        public Code CheckTripPolicy(World world, HaulTripSettings settings) => HaulTripPolicyContracts.CheckSettings(world, Entity, settings);
        public void ConfigureTripPolicy(World world, HaulTripSettings settings)
        { if (!CheckTripPolicy(world, settings).IsNone) throw new InvalidOperationException("invalid trip settings"); _settings = settings; Entity.Touch(); }
        public override void Sync(IStateSync state)
        {
            var settings = HaulTripSettings.Sync(state, _settings);
            if (settings == null || Def.Content.Find<IHaulTripProfile>(settings.Profile) == null) throw new CorruptStateException("missing saved trip profile/settings");
            _settings = settings;
        }
    }
}
