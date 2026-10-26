using System;
using System.Collections.Generic;
using Beep.SimEngine;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>A cash ledger for the test colony.</summary>
    public sealed class TestLedger : IEconomy, IPersistentService
    {
        public int StateSchemaVersion => 1;
        private readonly List<Fix> _cash = new List<Fix>();
        public bool RefuseCharge, MutateAdmission;
        public TestLedger(int seats, Fix start) { for (int i = 0; i < seats; i++) _cash.Add(start); }
        public string Id => "test.ledger";
        public bool CanAfford(PlayerId p, Fix a) { if (MutateAdmission && p.IsValid) _cash[p.Value] -= Fix.One; return p.IsValid && _cash[p.Value] >= a; }
        public bool TryCharge(PlayerId p, Fix a, Code r) { if (RefuseCharge || !CanAfford(p, a)) return false; _cash[p.Value] -= a; return true; }
        public void Charge(PlayerId p, Fix a, Code r) => _cash[p.Value] -= a;
        public void Credit(PlayerId p, Fix a, Code r) => _cash[p.Value] += a;
        public Fix Cash(PlayerId p) => p.IsValid ? _cash[p.Value] : Fix.Zero;
        public void Sync(IStateSync s) { for (int i = 0; i < _cash.Count; i++) { Fix v = _cash[i]; s.Fix(ref v); _cash[i] = v; } }
    }

    public sealed class ColonyModule : GameModule
    {
        public override string Id => "colony";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, new ModuleVersion(1, 0, 0)), new ModuleRequirement(TransformationModule.ModuleId, new ModuleVersion(1, 0, 0)), new ModuleRequirement(TileNavigationModule.ModuleId, new ModuleVersion(1, 0, 0)), new ModuleRequirement(LogisticsModule.ModuleId, Version), new ModuleRequirement(ConstructionModule.ModuleId, Version), new ModuleRequirement(InfrastructureModule.ModuleId, Version), new ModuleRequirement(IncidentResponseModule.ModuleId, Version) };

        public override void DeclareContent(ContentTypes types)
        {
            types.Component<CheckpointProbeDef>("proof_checkpoint", 1);
            types.Component<TileConstraintProbeDef>("proof_tile_constraint", 1);
            types.Component<ContractWorkerDef>("contract_worker", 1);
            types.Component<SingleResourcePortDef>("single_resource_port", 4);
            types.Component<ReplacementProbeDef>("replacement_probe", 1);
            types.Component<InletDeclarationDef>("inlet_declaration", 1);
            types.Component<LaneMobilityDef>("lane_mover", 1);
            types.Component<FiniteSupplierDef>("finite_supplier", 3);
            types.Component<WorkPermitDef>("work_permit", 1);
            types.Component<ActivityRecordDef>("activity_record", 1);
            types.Component<DialOperationDef>("dial_operation", 1);
            types.Component<ContractIncidentDef>("contract_incident", 3);
            types.Component<AlarmIncidentDef>("alarm_incident", 3);
            types.Section<MarkEffectDef>("mark_effects");
            types.Section<PermitRequirementDef>("work_permits");
            types.Section<ReserveRequirementDef>("work_reserves");
            types.Section<MalformedRequirementDef>("fault_requirements");
            types.Section<WorkerRateRequirementDef>("work_worker_rates");
            types.Component<ResourceConnectionDef>("resource_connection", 1);
            types.Component<ProofNetworkPortsDef>("proof_network_ports", 1);
            types.Section<ProofNetworkFlowPolicyDef>("proof_network_flow_policies");
            types.Section<FaultNetworkFlowPolicyDef>("fault_network_flow_policies");
            types.Section<LimitedNetworkSolverDef>("limited_network_solvers");
            types.Section<SolverProbeDef>("probe_network_solvers");
            types.Section<NetworkDemandProbeDef>("probe_network_demands");
            types.Component<ProofUtilityConsumerDef>("proof_utility_consumer", 1);
            types.Component<NetworkGateProbeDef>("network_gate_probe", 1);
            types.Component<AutomationProbeDef>("network_sensor", 1);
            types.Section<PermitPropagationRuleDef>("permit_incident_propagation_rules");
            types.Component<ProofPropagationEmitterDef>("proof_incident_propagation", 1);
            types.Section<ProofEnvironmentalMetricDef>("proof_environment_metrics");
            types.Component<ProofConditionDef>("proof_condition", 1);
            types.Section<ProofConditionWearDef>("proof_condition_wear_profiles");
            types.Component<ProofRecurringSourceDef>("proof_recurring_activity", 5);
            types.Component<ProofOperationPolicyDef>("proof_operation_policy", 1);
            types.Component<ProofWorkerExecutionDef>("proof_worker_execution", 1);
            types.Component<ProofMotionExecutionDef>("proof_motion_execution", 1);
            types.Section<EndpointPoolDef>("resource_pools");
            types.Component<PoolPortDef>("pool_port", 4);
            types.Section<ConnectedResourceRequirementDef>("connected_resource_requirements");
        }

        public override void Compose(WorldComposer c)
        {
            c.Commands.Register("colony.set-work-permit", new SetWorkPermitHandler());
            c.Commands.Register("proof.set-network-signal", new SetAutomationProbeHandler());
            c.AddSystem(new DialOperationSystem(), 300);
            if (c.Content.All<EndpointPoolDef>().Count > 0)
            {
                var pools = new EndpointPools(c.World, "colony.pools");
                c.Services.Bind(pools);
            }
        }
    }

    public static class Colony
    {
        public static string Manifest => FrameworkProofContent.Read("colony-master.json");

        public static string Defs => FrameworkProofContent.Read("colony.json");

        public static GameFactory Factory(string definitions = null, string flowDefinitions = null, string infrastructureDefinitions = null, string additionalDefinitions = null,
            IReadOnlyDictionary<string, string> additionalDocuments = null, string retryProfiles = null,
            IReadOnlyList<GameModule> additionalModules = null, IReadOnlyList<string> moduleDeclarations = null,
            IReadOnlyDictionary<string, string> moduleDocuments = null, string masterDocument = null)
        {
            var modules = new List<GameModule> { new InventoryModule(), new ResourceFlowsModule(), new OperationsModule(), new ContentAccessModule(), new TransformationModule(), new MotionModule(), new TileNavigationModule(), new WorkModule(), new LogisticsModule(), new ConstructionModule(), new InfrastructureModule(), new IncidentResponseModule(), new IncidentPropagationModule(), new EnvironmentalMetricsModule(), new EquipmentConditionModule(), new ObjectiveModule(), new WorldSetupModule(), new TileConstructionRulesModule(), new TileConditionsModule(), new TycoonModule(), new ColonyModule() };
            if (additionalModules != null) modules.AddRange(additionalModules);
            var def = new GameDefinition("colony", modules);
            var master = Json.Parse(masterDocument ?? Manifest); JsonValue documents = null;
            JsonValue declarations = master.Get("modules");
            for (int i = 0; i < declarations.Count; i++)
                if (declarations[i].Get("id").Text == "colony") documents = declarations[i].Get("documents");
            if (documents == null) throw new ContentException("colony master has no colony document owner");
            if (moduleDeclarations != null) foreach (string declaration in moduleDeclarations) master.Get("modules").Add(Json.Parse(declaration));
            if (infrastructureDefinitions != null) documents.Add(JsonValue.Of("network-topology.json"));
            if (additionalDefinitions != null) documents.Add(JsonValue.Of("colony-extra.json"));
            if (additionalDocuments != null) foreach (var document in additionalDocuments) documents.Add(JsonValue.Of(document.Key));
            var authored = new System.Text.StringBuilder(); master.WriteCanonical(authored);
            var files = new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", authored.ToString()).Add("colony.json", definitions ?? Defs)
                .Add("work-retry.json", retryProfiles ?? FrameworkProofContent.Read("work-retry.json"))
                .Add("colony-startup.json", FrameworkProofContent.Read("colony-startup.json"))
                .Add("colony-navigation.json", FrameworkProofContent.Read("colony-navigation.json"))
                .Add("colony-placement.json", FrameworkProofContent.Read("colony-placement.json"))
                .Add("colony-conditions.json", FrameworkProofContent.Read("colony-conditions.json"))
                .Add("game-mode-properties.json", FrameworkProofContent.Read("game-mode-properties.json"))
                .Add("colony-game-modes.json", FrameworkProofContent.Read("colony-game-modes.json"))
                .Add("flows.json", flowDefinitions ?? FrameworkProofContent.Read("resource-flows.json"));
            if (infrastructureDefinitions != null) files.Add("network-topology.json", infrastructureDefinitions);
            if (additionalDefinitions != null) files.Add("colony-extra.json", additionalDefinitions);
            files.Add("colony-work.json", FrameworkProofContent.Read("colony-work.json"));
            if (additionalDocuments != null) foreach (var document in additionalDocuments) files.Add(document.Key, document.Value);
            if (moduleDocuments != null) foreach (var document in moduleDocuments) files.Add(document.Key, document.Value);
            return new GameFactory(def, ContentSet.Load(files, def.ContentTypes));
        }

        public static SimGame New(ulong seed = 1) => Factory().NewGame(new NewGameRequest { Scenario = "colony", Seed = seed });

        public static List<EntityView> Mine(SimGame g, int seat, string def)
        {
            var all = new List<EntityView>();
            g.Seat(seat).View.CopyEntities(new PlayerId(seat), all);
            all.RemoveAll(e => e.Def != def);
            return all;
        }

        public static Fix Stock(SimGame g, EntityId store, string resource)
        {
            var facets = new List<Facet>();
            g.Observer.Inspect(store, facets);
            for (int i = 0; i < facets.Count; i++) if (facets[i].Key == Storage.FacetStock && facets[i].Subject == resource) return facets[i].Value;
            return Fix.Zero;
        }

    }

}
