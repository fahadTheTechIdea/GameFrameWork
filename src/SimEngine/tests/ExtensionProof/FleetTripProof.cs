using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class FleetTripProofModule : GameModule
    {
        private World _world;
        public override string Id => "proof.fleet-trip";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(MachineFleetsModule.ModuleId, Version), new ModuleRequirement(LogisticsModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types) => types.Component<ContractHaulTripDef>("proof_fleet_trip", 1);
        public override void Compose(WorldComposer composer) => _world = composer.World;
        // Explicit negative instrumentation of the actual selected provider, never gameplay state.
        // Arm only after a fresh/restored session is admitted; this hook is not saved or replayed.
        public void ArmObservationFault(EntityId identity, string checkpointGame = null)
        {
            var policy = _world.Entities.Get(identity)?.Get<IHaulTripPolicy>() as ContractHaulTrip;
            if (policy == null || !(policy.Def.Fault >= 8 && policy.Def.Fault <= 12 ||
                policy.Def.Fault == 19 || policy.Def.Fault == 20 || policy.Def.Fault == 21 || policy.Def.Fault >= 23 && policy.Def.Fault <= 29))
                throw new InvalidOperationException("explicit authored trip observation fault required");
            if ((policy.Def.Fault == 24 || policy.Def.Fault == 25) && string.IsNullOrWhiteSpace(checkpointGame))
                throw new InvalidOperationException("actual checkpoint game identity required");
            policy.ArmObservationFault(_world, checkpointGame);
        }
        public (bool Captured, bool Swallowed) ReadNestedOperationProbe(EntityId identity)
            => (_world.Entities.Get(identity)?.Get<IHaulTripPolicy>() as ContractHaulTrip
                ?? throw new InvalidOperationException("actual trip operation probe required")).NestedOperationProbe;
        // Bind the already-declared point load provider for negative observation only;
        // this module introduces no load declaration, settings owner or dispatch logic.
        public void ArmLoadObservationFault(EntityId identity)
        {
            var policy = _world.Entities.Get(identity)?.Get<IHaulLoadPolicy>() as ContractHaulLoad
                ?? throw new InvalidOperationException("actual independent load provider required");
            policy.ArmObservationFault(_world);
        }
        // Direct engine-contract diagnostic, not a second command or policy implementation.
        public void ProbeConfigure(EntityId identity, HaulTripSettings settings, bool group)
        {
            var entity = _world.Entities.Get(identity) ?? throw new InvalidOperationException("actual trip diagnostic entity required");
            if (group) HaulTripPolicyContracts.ConfigureGroup(_world, entity.Owner, new[] { identity }, settings);
            else HaulTripPolicyContracts.Configure(_world, entity, settings);
        }
        public void ProbeLoadConfigure(EntityId identity, HaulLoadSettings settings, bool group)
        {
            var entity = _world.Entities.Get(identity) ?? throw new InvalidOperationException("actual load diagnostic entity required");
            if (group) HaulLoadPolicyContracts.ConfigureGroup(_world, entity.Owner, new[] { identity }, settings);
            else HaulLoadPolicyContracts.Configure(_world, entity, settings);
        }
        public void ObserveLoadRule(EntityId identity, Action query)
        {
            var entity = _world.Entities.Get(identity) ?? throw new InvalidOperationException("actual rule diagnostic worker required");
            var settings = HaulLoadPolicyContracts.Read(_world, entity);
            var profile = _world.Content.Get<IHaulLoadProfile>(settings.Profile) as OddHaulLoadProfileDef
                ?? throw new InvalidOperationException("actual authored independent rule required");
            if (profile.RuleFault < 1) throw new InvalidOperationException("explicit authored rule probe required");
            profile.Observe(() => {
                if (profile.RuleFault == 1 || profile.RuleFault == 4) { var worker = entity.Get<IWorker>(); worker.Held = !worker.Held; }
                if (profile.RuleFault == 2) ((GameClock)_world.Clock).Pause();
                if (profile.RuleFault == 3 || profile.RuleFault == 4) throw new InvalidOperationException("proof.load-rule-first-error");
                if (profile.RuleFault == 5) query();
                if (profile.RuleFault == 6) HaulLoadPolicyContracts.Read(_world, entity); // Valid rule -> current settings read.
            }, query);
        }
        public void ObserveTripRule(EntityId identity, Action query)
        {
            var entity = _world.Entities.Get(identity) ?? throw new InvalidOperationException("actual trip rule worker required");
            var settings = HaulTripPolicyContracts.Read(_world, entity);
            var profile = _world.Content.Get<IHaulTripProfile>(settings.Profile) as IndependentHaulTripProfileDef
                ?? throw new InvalidOperationException("actual authored independent trip rule required");
            if (profile.RuleFault < 1) throw new InvalidOperationException("explicit authored trip rule probe required");
            profile.Observe(() => {
                if (profile.RuleFault == 1 || profile.RuleFault == 4) { var worker = entity.Get<IWorker>(); worker.Held = !worker.Held; }
                if (profile.RuleFault == 2) ((GameClock)_world.Clock).Pause();
                if (profile.RuleFault == 3 || profile.RuleFault == 4) throw new InvalidOperationException("proof.trip-rule-first-error");
                if (profile.RuleFault == 5) query();
                if (profile.RuleFault == 6) HaulTripPolicyContracts.Read(_world, entity);
            }, query);
        }
        public void ProbeTripRuleBudget(EntityId identity, MotionTravelEstimate travel)
            => HaulTripPolicyContracts.CheckTrip(_world, _world.Entities.Get(identity)
                ?? throw new InvalidOperationException("actual trip budget worker required"), travel);
        public int ObserveTripSources(EntityId identity, Action query)
        {
            var entity = _world.Entities.Get(identity) ?? throw new InvalidOperationException("actual source diagnostic worker required");
            var settings = HaulTripPolicyContracts.Read(_world, entity);
            var profile = _world.Content.Get<IHaulTripProfile>(settings.Profile) as IndependentHaulTripProfileDef
                ?? throw new InvalidOperationException("actual independent source metadata required");
            if (profile.SourcesFault < 1) throw new InvalidOperationException("explicit authored source probe required");
            return profile.Observe(() => {
                if (profile.SourcesFault == 1 || profile.SourcesFault == 4) entity.Get<IWorker>().Held = true;
                if (profile.SourcesFault == 2) ((GameClock)_world.Clock).Pause();
                if (profile.SourcesFault == 3 || profile.SourcesFault == 4) throw new InvalidOperationException("proof.trip-sources-first-error");
                if (profile.SourcesFault == 5) query();
                if (profile.SourcesFault == 6) _ = _world.Clock.StepIndex;
            }, query, sources: true);
        }
        public Code ProbeTripSourceCheck(EntityId identity, HaulTripSettings settings)
            => HaulTripPolicyContracts.CheckSettings(_world, _world.Entities.Get(identity)
                ?? throw new InvalidOperationException("actual source-check worker required"), settings);
    }
    public sealed class ContractHaulTripDef : ComponentDef<ContractHaulTrip>, IHaulTripPolicyDefinition
    {
        private string _profile, _refuse, _mutationResource;
        public IHaulTripProfile Profile { get; private set; }
        public IHaulTripProfile InitialProfile => Profile;
        public string RefuseProfile { get; private set; }
        public int Fault { get; private set; }
        public ResourceId MutationResource { get; private set; }
        private ResolvedContent _content;
        protected override void ReadCore(JsonReader reader)
        {
            _profile = reader.String("profile"); _refuse = reader.String("refuse_profile"); Fault = reader.Int("fault");
            if (Fault < 0 || Fault > 29) throw reader.Error("unknown trip proof fault");
            if (Fault == 17 || Fault == 18) _mutationResource = reader.String("mutation_resource");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _content = content.Content; Profile = content.Def<IHaulTripProfile>(_profile); RefuseProfile = content.Def<IHaulTripProfile>(_refuse).Id;
            if (_mutationResource != null) MutationResource = content.Resource(_mutationResource);
        }
        protected override void ValidateCore(EntityDef entity) => HaulTripPolicyContracts.ValidateDefinition(_content, entity, Profile);
    }
    // Independent engine-side capability: settings only, original physical owners remain authoritative.
    public sealed class ContractHaulTrip : Component<ContractHaulTripDef>, IHaulTripPolicy
    {
        private HaulTripSettings _settings;
        private long _checkMutation;
        private World _observationWorld; // Transient negative hook, not another persistent state owner.
        private bool _reentering; // Bounds the negative probe on an unguarded baseline; not a runtime fallback.
        private string _checkpointGame;
        private bool _nestedCaptured, _nestedSwallowed; // Negative evidence only, never saved/hashed game state.
        internal (bool Captured, bool Swallowed) NestedOperationProbe => (_nestedCaptured, _nestedSwallowed);
        internal void ArmObservationFault(World world, string checkpointGame)
        { _observationWorld = world; _checkpointGame = checkpointGame; }
        private void ProbeNestedOperation()
        {
            try
            {
                if (Def.Fault == 24 || Def.Fault == 25) _ = SaveGame.Write(_observationWorld, _checkpointGame);
                else if (Def.Fault == 26 || Def.Fault == 27) _ = _observationWorld.Hash();
                else _observationWorld.Step();
                _nestedCaptured = true;
            }
            catch (InvalidOperationException) when (Def.Fault == 25 || Def.Fault == 27 || Def.Fault == 29)
            { _nestedSwallowed = true; } // Deliberate negative suppression; engine must already have latched the fault.
        }
        private void Reenter(Action callback)
        {
            if (_reentering) return;
            _reentering = true;
            try { callback(); } finally { _reentering = false; }
        }
        public override void OnCreate() => _settings = Def.Profile.Initial;
        public HaulTripSettings ReadTripPolicy()
        {
            if (_observationWorld != null && Def.Fault <= 12)
            {
                if (Def.Fault == 9) _checkMutation++;
                if (Def.Fault == 10) ((GameClock)_observationWorld.Clock).Pause();
                if (Def.Fault == 11 || Def.Fault == 12)
                { var worker = Entity.Get<IWorker>(); worker.Held = !worker.Held; }
                if (Def.Fault != 11) throw new InvalidOperationException("proof.trip-read-first-error");
            }
            if (_observationWorld != null && Def.Fault == 19)
                Reenter(() => HaulTripPolicyContracts.Read(_observationWorld, Entity));
            if (_observationWorld != null && Def.Fault == 20)
                Reenter(() => HaulTripPolicyContracts.CheckConfigure(_observationWorld, Entity.Owner, Entity, _settings));
            if (_observationWorld != null && Def.Fault >= 24 && Def.Fault <= 29)
                Reenter(ProbeNestedOperation);
            return _settings;
        }
        public Code CheckTripPolicy(World world, HaulTripSettings settings)
        {
            if (Def.Fault == 22) HaulTripPolicyContracts.Read(world, Entity); // Valid check -> current-settings read, not a cycle.
            if (_observationWorld != null && Def.Fault == 21)
                Reenter(() => HaulTripPolicyContracts.CheckConfigure(world, Entity.Owner, Entity, settings));
            if (Def.Fault == 1) _checkMutation++; // Deliberately no Touch: actual codec must detect it.
            // Explicit negative instrumentation only on the authored refusal request. Normal
            // configuration/hauling still uses the original capability and physical systems.
            if (settings.Profile == Def.RefuseProfile && Def.Fault >= 3 && Def.Fault <= 7)
            {
                if (Def.Fault == 4) _checkMutation++;
                if (Def.Fault == 5) ((GameClock)world.Clock).Pause();
                if (Def.Fault == 6 || Def.Fault == 7)
                { var worker = Entity.Get<IWorker>(); worker.Held = !worker.Held; }
                if (Def.Fault != 6) throw new InvalidOperationException("proof.trip-admission-first-error");
            }
            Code refusal = HaulTripPolicyContracts.CheckSettings(world, Entity, settings);
            return !refusal.IsNone ? refusal : settings.Profile == Def.RefuseProfile ? new Code("proof.trip-refused") : Code.None;
        }
        public void ConfigureTripPolicy(World world, HaulTripSettings settings)
        {
            if (_observationWorld != null && Def.Fault == 23)
                Reenter(() => HaulTripPolicyContracts.Configure(world, Entity, settings));
            if (Def.Fault == 16 && !settings.Enabled) throw new InvalidOperationException("proof.trip-commit-first-error");
            if (Def.Fault != 2) { _settings = settings; Entity.Touch(); }
            // Explicit negative modes: only disabling triggers them; normal authored hauling
            // remains in the native work/resource/motion owners and uses no injected state.
            if (!settings.Enabled)
            {
                if (Def.Fault == 13) ((GameClock)world.Clock).Pause();
                if (Def.Fault == 14) ((GameClock)world.Clock).Speed = 2;
                if (Def.Fault == 15) { var worker = Entity.Get<IWorker>(); worker.Held = !worker.Held; }
                if (Def.Fault == 17 || Def.Fault == 18)
                {
                    var stock = Entity.Get<IInventory>();
                    if (stock.Remove(Def.MutationResource, Fix.One) != Fix.One)
                        throw new InvalidOperationException("trip commit diagnostic requires actual finite stock");
                }
                if (Def.Fault == 18) throw new InvalidOperationException("proof.trip-commit-first-error");
            }
        }
        public override void Sync(IStateSync sync)
        { _settings = HaulTripSettings.Sync(sync, _settings); sync.Long(ref _checkMutation); }
    }
}
