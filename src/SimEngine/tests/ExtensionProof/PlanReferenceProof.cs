using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class PlanReferenceProofModule : GameModule
    {
        public const string ModuleId = "proof.plan-references";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(WorkModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ProofPlanReferenceProfileDef>("plan_reference_profiles");
            types.Section<ProofPlanReferenceControllerDef>("plan_reference_controllers");
            types.Component<ExposurePrivateProbeDef>("plan_reference_private_probe", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            var settings = composer.Scenario.SettingsFor(Id) ?? throw new ContentException("reference profiles must be explicit");
            string serviceId = settings.String("service"), systemId = settings.String("system"); settings.End();
            var service = new ProofPlanReferenceService(composer.Content.Get<ProofPlanReferenceProfileDef>(serviceId));
            composer.Services.Bind<ProofPlanReferenceService>(service);
            composer.Services.Bind<IPlanHistoryRetention>(service);
            var system = new ProofPlanReferenceSystem(composer.Content.Get<ProofPlanReferenceProfileDef>(systemId));
            composer.Services.Bind<ProofPlanReferenceSystem>(system);
            composer.AddSystem(system, 750);
        }
    }
    public sealed class ProofPlanReferenceProfileDef : Def
    {
        public string Group { get; private set; }
        public bool Enabled { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Group = reader.String("group"); Enabled = reader.Bool("enabled");
            if (string.IsNullOrWhiteSpace(Group)) throw reader.Error("reference group required");
        }
    }
    public sealed class ProofPlanReferenceControllerDef : PlayerControllerDef
    {
        private string _group, _schemaFault;
        private bool _enabled;
        private World _schemaWorld;
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader); _group = reader.String("group"); _enabled = reader.Bool("enabled");
            if (string.IsNullOrWhiteSpace(_group)) throw reader.Error("reference group required");
            _schemaFault = reader.String("schema_fault", "none");
            if (_schemaFault != "none" && _schemaFault != "throw" && _schemaFault != "pause" && _schemaFault != "pause-throw"
                && _schemaFault != "corrupt" && _schemaFault != "corrupt-throw") throw reader.Error("explicit schema probe fault required");
        }
        internal void InstallSchemaProbe(World world) { _schemaWorld = world; }
        public override IPlayerController Create()
        {
            var controller = new ProofPlanReferenceController(_group, _enabled);
            if (_schemaFault != "none") controller.SchemaFault = () => {
                if (_schemaFault == "corrupt" || _schemaFault == "corrupt-throw") controller.References.CorruptReadState();
                if (_schemaFault == "pause" || _schemaFault == "pause-throw") ((GameClock)_schemaWorld.Clock).Pause();
                if (_schemaFault == "throw" || _schemaFault == "pause-throw" || _schemaFault == "corrupt-throw")
                    throw new ApplicationException("proof.reference-schema");
            };
            return controller;
        }
    }
    // Negative composition instrumentation only. Same native controller module/system and real provider.
    public sealed class ProofPlanReferenceControllersModule : PlayerControllersModule
    {
        private readonly InitializeProbe _initialize;
        private World _world;
        public ProofPlanReferenceControllersModule(InitializeProbe initialize = null) { _initialize = initialize; }
        public string ObservationFaultPhase => _world?.Fault?.Phase;
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            foreach (var definition in composer.Content.All<ProofPlanReferenceControllerDef>()) definition.InstallSchemaProbe(composer.World);
            if (_initialize != null) composer.AddSystem(_initialize, 301);
            base.Compose(composer);
        }
    }
    /// <summary>Actual provider policy state, not a second plan store or history pin index.</summary>
    public sealed class ProofPlanReferenceState
    {
        private readonly string _group;
        private bool _enabled;
        // Deliberately adversarial test instrumentation. Never serialized or used as gameplay intent.
        public Action ReadFault;
        public ProofPlanReferenceState(string group, bool enabled) { _group = group; _enabled = enabled; }
        public bool Retains(World world, PlayerId company, long plan)
        { ReadFault?.Invoke(); return _enabled && Matches(world, company, plan, _group); }
        public void Sync(IStateSync sync) => sync.Bool(ref _enabled);
        public void CorruptReadState() => _enabled = !_enabled;
        internal static bool Matches(World world, PlayerId company, long plan, string group)
        {
            var rows = new List<PlanView>(); world.Services.Get<IPlanBoard>().CopyPlanGroup(company, group, rows);
            foreach (var row in rows) if (row.Id == plan) return true;
            return false;
        }
    }
    public sealed class ProofPlanReferenceService : IPersistentService, IPlanHistoryRetention
    {
        public ProofPlanReferenceState References { get; }
        public ProofPlanReferenceService(ProofPlanReferenceProfileDef profile)
        { References = new ProofPlanReferenceState(profile.Group, profile.Enabled); }
        public string Id => "proof.plan-reference-service";
        public int StateSchemaVersion => 1;
        public void Sync(IStateSync sync) => References.Sync(sync);
        public bool RetainsPlan(World world, PlayerId company, long plan) => References.Retains(world, company, plan);
    }
    public sealed class ProofPlanReferenceSystem : GameSystem, IPlanHistoryRetention
    {
        public ProofPlanReferenceState References { get; }
        public ProofPlanReferenceSystem(ProofPlanReferenceProfileDef profile)
        { References = new ProofPlanReferenceState(profile.Group, profile.Enabled); }
        public override string Id => "proof.plan-reference-system";
        public override int StateSchemaVersion => 1;
        public override void Update(World world, TickInfo time) { }
        public override void Sync(IStateSync sync) => References.Sync(sync);
        public bool RetainsPlan(World world, PlayerId company, long plan) => References.Retains(world, company, plan);
    }
    public sealed class ProofPlanReferenceController : PlayerController, IPlayerControllerState, IPlanHistoryRetention
    {
        public ProofPlanReferenceState References { get; }
        // Deliberately adversarial metadata hook; never encoded or used as positive game intent.
        public Action SchemaFault;
        public ProofPlanReferenceController(string group, bool enabled)
        { References = new ProofPlanReferenceState(group, enabled); }
        public int StateSchemaVersion { get { SchemaFault?.Invoke(); return 1; } }
        public override void Decide(PlayerDecisionContext context) { }
        public void Sync(IStateSync sync) => References.Sync(sync);
        public bool RetainsPlan(World world, PlayerId company, long plan) => References.Retains(world, company, plan);
    }
}
