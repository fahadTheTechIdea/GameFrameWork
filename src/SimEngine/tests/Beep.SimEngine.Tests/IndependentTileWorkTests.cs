using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class IndependentTileWorkTests
    {
        internal const string Case = "Independent tile work: native machine travels, docks and earns authored work";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(bool construction = false, bool extraction = false, bool nativeTiles = false, bool incidents = false,
            Action<JsonValue> authorIncidents = null, bool lines = false, Action<JsonValue> authorLines = null, bool stockedRemoval = false,
            bool recoverCargo = false, bool independentRecovery = false, bool terrainCleanup = false,
            Action<JsonValue> authorTerrainCleanup = null, bool terrainDeposition = false, Action<JsonValue> authorTerrainDeposition = null,
            bool spillResources = false, Action<JsonValue> authorSpillResources = null,
            bool spillEnvironment = false, Action<JsonValue> authorSpillEnvironment = null,
            bool spillHistory = false, Action<JsonValue> authorSpillHistory = null,
            bool spillDisposal = false, bool independentDisposal = false, Action<JsonValue> authorSpillDisposal = null,
            bool incidentPressure = false, bool independentPressure = false, bool halfMinute = false, Action<JsonValue> authorIncidentPressure = null,
            GameModule additionalModule = null, string additionalDeclaration = null, string additionalDocument = null,
            Action<JsonValue> authorVisibility = null, Action<JsonValue> authorScenario = null)
        {
            if(additionalModule==null&&(additionalDeclaration!=null||additionalDocument!=null)||additionalModule!=null&&(additionalDeclaration==null||additionalDocument==null))
                throw new ArgumentException("additional module requires its explicit declaration and document together");
            JsonValue extra=additionalModule==null?null:Json.Parse(additionalDeclaration);
            if(extra!=null&&extra.Get("documents")?.Count!=1)throw new ArgumentException("exactly one explicit additional document required");
            if (independentPressure && !incidentPressure) throw new ArgumentException("independent process requires explicit incident-pressure content");
            if (independentDisposal && !spillDisposal) throw new ArgumentException("independent disposal requires explicit disposal content");
            spillEnvironment |= spillHistory || spillDisposal;
            spillResources |= spillEnvironment || incidentPressure;
            terrainCleanup |= terrainDeposition || spillResources;
            incidents |= terrainCleanup;
            if (independentRecovery && !recoverCargo) throw new ArgumentException("independent recovery requires explicit recovery content");
            stockedRemoval |= recoverCargo;
            lines |= stockedRemoval;
            construction |= extraction || incidents || lines;
            var selectedModules = new List<GameModule> { new IndependentTileSensorModule(),
                new WorldSetupModule(), new OperationsModule(), new CompanyKnowledgeModule(), new KnowledgeVisibilityModule(),
                new MotionModule(), new TileNavigationModule(), new TileConditionsModule(), new ContentAccessModule(), new InventoryModule(), new WorkModule() };
            if (construction) { selectedModules.Add(new LogisticsModule()); selectedModules.Add(new ConstructionModule()); }
            if (extraction || spillResources) selectedModules.Add(new SpatialResourcesModule());
            if (spillResources) { selectedModules.Add(new TransformationModule()); selectedModules.Add(new ResourceFlowsModule()); }
            if (spillEnvironment) { selectedModules.Add(new EnvironmentalMetricsModule()); selectedModules.Add(new ObjectiveModule()); selectedModules.Add(new SpatialEnvironmentProofModule()); }
            if (spillDisposal) selectedModules.Add(new InventoryWorkProofModule());
            if (incidentPressure) { selectedModules.Add(new IncidentProcessModule()); if (independentPressure) selectedModules.Add(new IncidentProcessProofModule()); }
            if (incidents) { selectedModules.Add(new IncidentResponseModule()); selectedModules.Add(new IncidentPropagationModule()); }
            if (lines) selectedModules.Add(new InfrastructureModule());
            if (recoverCargo) selectedModules.Add(new LogisticsRecoveryPolicyModule());
            if (independentRecovery) selectedModules.Add(new RecoveryContractModule(new InitializeProbe()));
            if (terrainCleanup) selectedModules.Add(new TileTerrainWorkModule());
            if(additionalModule!=null)selectedModules.Add(additionalModule);
            var definition = new GameDefinition("independent-work", selectedModules);
            var master = Json.Parse(FrameworkProofContent.Read("independent-tile-navigation-master.json"));
            var modules = master.Get("modules");
            modules.Add(Json.Parse("{\"id\":\"sim.access\",\"version\":\"1.0.0\",\"documents\":[\"open-access.json\"]}"));
            modules.Add(Json.Parse("{\"id\":\"sim.inventory\",\"version\":\"1.0.0\",\"documents\":[]}"));
            modules.Add(Json.Parse("{\"id\":\"sim.work\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.motion\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-tile-work.json\",\"work-retry.json\"]}"));
            var world = Json.Parse(FrameworkProofContent.Read("independent-tile-sensor-world.json")); var scenario = world.Get("scenarios")[0];
            if(extra!=null){modules.Add(extra);scenario.Get("modules").Add(JsonValue.Of(additionalModule.Id));}
            if (halfMinute) world.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
            if (nativeTiles) {
                world.Set("tile_spaces", world.Get("sensor_tile_spaces")); world.Remove("sensor_tile_spaces");
            }
            foreach (string id in new[] { "sim.motion", "sim.tile-navigation", "sim.tile-conditions", "sim.access", "sim.inventory", "sim.work" })
                scenario.Get("modules").Add(JsonValue.Of(id));
            scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("sensor.work-start"));
            scenario.Get("settings").Set("sim.work", Json.Parse("{\"retry\":\"proof.retry\"}"));
            var startup = Json.Parse(FrameworkProofContent.Read("independent-tile-navigation-startup.json"));
            var cart = startup.Get("entities")[0].Get("components");
            cart.Set("worker", Json.Parse("{\"roles\":[\"sensor.inspect\"],\"work_rate\":1,\"states\":{\"working\":\"sensor.working\",\"loading\":\"sensor.loading\",\"stopped\":\"sensor.stopped\"}}"));
            cart.Set("work_policy", Json.Parse("{\"profile\":\"sensor.worker-policy\"}"));
            if (construction) {
                modules.Add(Json.Parse("{\"id\":\"sim.logistics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
                modules.Add(Json.Parse("{\"id\":\"sim.construction\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-tile-construction.json\"]}"));
                scenario.Get("modules").Add(JsonValue.Of("sim.logistics")); scenario.Get("modules").Add(JsonValue.Of("sim.construction"));
                scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("sensor.construction-start"));
                scenario.Get("settings").Set("sim.construction", Json.Parse("{\"recovery\":null}"));
                cart.Get("worker").Get("roles").Add(JsonValue.Of("haul"));
                cart.Set("storage", Json.Parse("{\"capacity_policy\":\"sensor.stock\",\"public\":false,\"initial_stock\":{}}"));
                cart.Set("transporter", Json.Parse("{\"handling\":\"sensor.handling\"}"));
            }
            var work = Json.Parse(FrameworkProofContent.Read("independent-tile-work.json"));
            if (construction) work.Get("work_policies")[0].Get("allowed_roles").Add(JsonValue.Of("haul"));
            if (extraction) {
                modules.Add(Json.Parse("{\"id\":\"sim.spatial-resources\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-tile-extraction.json\"]}"));
                for (int i = 0; i < modules.Count; i++)
                    if (modules[i].Get("id").Text == CompanyKnowledgeModule.ModuleId)
                        modules[i].Get("documents").Add(JsonValue.Of("independent-tile-readings.json"));
                scenario.Get("modules").Add(JsonValue.Of(SpatialResourcesModule.ModuleId));
                scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("sensor.extraction-start"));
                work.Get("entities")[0].Get("components").Set("storage", Json.Parse("{\"capacity_policy\":\"sensor.stock\",\"public\":false,\"initial_stock\":{}}"));
            }
            JsonValue incidentContent = null;
            if (incidents) {
                cart.Get("work_policy").Set("profile", JsonValue.Of("sensor.response-worker-policy"));
                modules.Add(Json.Parse("{\"id\":\"sim.incidents\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
                modules.Add(Json.Parse("{\"id\":\"sim.incident-propagation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.incidents\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-tile-incidents.json\"]}"));
                scenario.Get("modules").Add(JsonValue.Of(IncidentResponseModule.ModuleId));
                scenario.Get("modules").Add(JsonValue.Of(IncidentPropagationModule.ModuleId));
                scenario.Get("settings").Set(IncidentPropagationModule.ModuleId, Json.Parse("{\"budget\":16}"));
                scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("sensor.incident-start"));
                var site = work.Get("entities")[0].Get("components");
                site.Get("tile_presence").Set("width", JsonValue.Number("2"));
                site.Get("tile_work_dock").Get("resources").Add(JsonValue.Of("steel"));
                site.Set("storage", Json.Parse("{\"capacity_policy\":\"sensor.stock\",\"public\":false,\"initial_stock\":{}}"));
                site.Set("incident", Json.Parse("{\"id\":\"leak\",\"workflow\":\"sensor.fire\",\"states\":{\"active\":\"sensor.stopped\"}}"));
                incidentContent = Json.Parse(FrameworkProofContent.Read("independent-tile-incidents.json"));
                if (terrainCleanup) {
                    modules.Add(Json.Parse("{\"id\":\"sim.tile-terrain-work\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-tile-cleanup.json\"]}"));
                    scenario.Get("modules").Add(JsonValue.Of(TileTerrainWorkModule.ModuleId));
                    scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("sensor.cleanup-start"));
                    incidentContent.Get("entities")[0].Get("components").Get("incident").Set("workflow",JsonValue.Of("sensor.cleanup-workflow"));
                    site.Get("incident").Set("workflow",JsonValue.Of("sensor.cleanup-workflow"));
                    if(terrainDeposition){
                        modules[modules.Count-1].Get("documents").Add(JsonValue.Of("independent-tile-deposition.json"));
                        scenario.Get("settings").Get("sim.world-setup").Set("profile",JsonValue.Of("sensor.deposition-start"));
                        incidentContent.Get("tile_incident_propagation_rules")[0].Set("exposure_effect",JsonValue.Of("sensor.deposition-terrain"));
                    }
                }
                authorIncidents?.Invoke(incidentContent);
            }
            if(spillResources){
                bool listed=false;for(int i=0;i<modules.Count;i++)if(modules[i].Get("id").Text==SpatialResourcesModule.ModuleId){
                    modules[i].Get("documents").Add(JsonValue.Of("independent-tile-spill.json"));listed=true;
                }
                if(!listed)modules.Add(Json.Parse("{\"id\":\"sim.spatial-resources\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-tile-spill.json\"]}"));
                modules.Add(Json.Parse("{\"id\":\"sim.transformations\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
                modules.Add(Json.Parse("{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
                foreach(string id in new[]{SpatialResourcesModule.ModuleId,TransformationModule.ModuleId,ResourceFlowsModule.ModuleId})
                    scenario.Get("modules").Add(JsonValue.Of(id));
                scenario.Get("settings").Get("sim.world-setup").Set("profile",JsonValue.Of("sensor.spill-start"));
                cart.Get("transporter").Set("handling",JsonValue.Of("sensor.spill-handling"));
                incidentContent.Get("tile_incident_propagation_rules")[0].Set("exposure_effect",JsonValue.Of("sensor.spill"));
            }
            JsonValue lineContent = null;
            if(incidentPressure){
                modules.Add(Json.Parse("{\"id\":\"sim.incident-processes\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.incidents\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-tile-incident-process.json\"]}"));
                scenario.Get("modules").Add(JsonValue.Of(IncidentProcessModule.ModuleId));
                if(independentPressure){modules.Add(Json.Parse("{\"id\":\"proof.incident-process\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.incident-processes\",\"version\":\"1.0.0\"}],\"documents\":[]}"));scenario.Get("modules").Add(JsonValue.Of("proof.incident-process"));}
                scenario.Get("settings").Get("sim.world-setup").Set("profile",JsonValue.Of("sensor.pressure-start"));
            }
            if (spillEnvironment) {
                var environmentalModule=Json.Parse("{\"id\":\"sim.environment-metrics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-tile-spill-environment.json\"]}");
                if(spillHistory)environmentalModule.Get("documents").Add(JsonValue.Of("independent-tile-spill-history.json"));
                if(spillDisposal)environmentalModule.Get("documents").Add(JsonValue.Of("independent-tile-disposal.json"));
                modules.Add(environmentalModule);
                modules.Add(Json.Parse("{\"id\":\"sim.objectives\",\"version\":\"1.0.0\",\"documents\":[]}"));
                modules.Add(Json.Parse("{\"id\":\"proof.spatial-environment\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.environment-metrics\",\"version\":\"1.0.0\"},{\"id\":\"sim.spatial-resources\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-spill-observation.json\"]}"));
                scenario.Get("modules").Add(JsonValue.Of(EnvironmentalMetricsModule.ModuleId));
                scenario.Get("modules").Add(JsonValue.Of(ObjectiveModule.ModuleId));
                scenario.Get("modules").Add(JsonValue.Of("proof.spatial-environment"));
                scenario.Get("settings").Set(ObjectiveModule.ModuleId,Json.Parse("{\"companies\":[{\"company\":0,\"objectives\":[\"sensor.spill-detected\",\"sensor.spill-recovered\",\"sensor.spill-treated\"]}]}"));
                if(spillHistory)scenario.Get("settings").Get(ObjectiveModule.ModuleId).Get("companies")[0].Get("objectives").Add(JsonValue.Of("sensor.spill-accounted"));
            }
            if(spillDisposal){
                modules.Add(Json.Parse("{\"id\":\"proof.inventory-work\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-inventory-disposal.json\"]}"));
                scenario.Get("modules").Add(JsonValue.Of("proof.inventory-work"));
                scenario.Get("settings").Get("sim.world-setup").Set("profile",JsonValue.Of("sensor.disposal-start"));
                scenario.Get("settings").Get(ObjectiveModule.ModuleId).Get("companies")[0].Set("objectives",Json.Parse("[\"sensor.spill-detected\",\"sensor.spill-recovered\",\"sensor.spill-disposed\"]"));
            }
            if (lines) {
                modules.Add(Json.Parse("{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-tile-lines.json\"]}"));
                scenario.Get("modules").Add(JsonValue.Of(InfrastructureModule.ModuleId));
                scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("sensor.line-startup"));
                lineContent = Json.Parse(FrameworkProofContent.Read("independent-tile-lines.json")); authorLines?.Invoke(lineContent);
                if (stockedRemoval) {
                    modules[modules.Count - 1].Get("documents").Add(JsonValue.Of("independent-tile-stocked-removal.json"));
                    scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("sensor.stocked-removal-start"));
                    var endpoints = lineContent.Get("entities");
                    foreach (int index in new[] { 2,3 }) {
                        var parts = endpoints[index].Get("components");
                        parts.Set("ports", Json.Parse("{\"list\":[{\"network\":\"sensor.network\",\"push\":[],\"pull\":[]}]}"));
                        parts.Set("network_resource_port", Json.Parse("{\"id\":\"network-stock\",\"network\":\"sensor.network\",\"minimum_members\":1}"));
                    }
                }
            }
            JsonValue recoveryContent = null;
            if (recoverCargo) {
                modules.Add(Json.Parse("{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
                scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"contract.recovery\"}"));
                recoveryContent = Json.Parse(FrameworkProofContent.Read("independent-tile-cargo-recovery.json"));
                if (independentRecovery) {
                    modules.Add(Json.Parse("{\"id\":\"proof.recovery-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\"}],\"documents\":[\"independent-tile-cargo-recovery.json\"]}"));
                    scenario.Get("modules").Add(JsonValue.Of(RecoveryContractModule.ModuleId));
                } else {
                    modules[modules.Count-1].Get("documents").Add(JsonValue.Of("independent-tile-cargo-recovery.json"));
                    recoveryContent.Set("logistics_recovery_profiles", recoveryContent.Get("independent_recovery_profiles"));
                    recoveryContent.Set("recovery_destination_policies", recoveryContent.Get("independent_recovery_destinations"));
                    recoveryContent.Remove("independent_recovery_profiles"); recoveryContent.Remove("independent_recovery_destinations");
                }
            }
            authorScenario?.Invoke(scenario);
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString()).Add("independent-tile-sensor-world.json", world.ToString())
                .Add("independent-tile-navigation-startup.json", startup.ToString()).Add("open-access.json", FrameworkProofContent.Access)
                .Add("independent-tile-work.json", work.ToString());
            foreach (string file in new[] { "independent-tile-sensor-startup.json", "independent-tile-sensor-visibility.json",
                "independent-tile-navigation.json", "independent-tile-navigation-conditions.json", "work-retry.json" })
            {
                string document = FrameworkProofContent.Read(file);
                if (file == "independent-tile-sensor-visibility.json" && authorVisibility != null) {
                    var policy = Json.Parse(document); authorVisibility(policy); document = policy.ToString();
                }
                files.Add(file, document);
            }
            if (construction) files.Add("independent-tile-construction.json", FrameworkProofContent.Read("independent-tile-construction.json"));
            if (extraction) foreach (string file in new[] { "independent-tile-extraction.json", "independent-tile-readings.json" })
                files.Add(file, FrameworkProofContent.Read(file));
            if (incidents) files.Add("independent-tile-incidents.json", incidentContent.ToString());
            if (lines) files.Add("independent-tile-lines.json", lineContent.ToString());
            if (stockedRemoval) files.Add("independent-tile-stocked-removal.json", FrameworkProofContent.Read("independent-tile-stocked-removal.json"));
            if (recoverCargo) files.Add("independent-tile-cargo-recovery.json", recoveryContent.ToString());
            if (terrainCleanup) {
                var cleanup = Json.Parse(FrameworkProofContent.Read("independent-tile-cleanup.json")); authorTerrainCleanup?.Invoke(cleanup);
                files.Add("independent-tile-cleanup.json", cleanup.ToString());
            }
            if(terrainDeposition){
                var deposition=Json.Parse(FrameworkProofContent.Read("independent-tile-deposition.json"));authorTerrainDeposition?.Invoke(deposition);
                files.Add("independent-tile-deposition.json",deposition.ToString());
            }
            if(spillResources){var spill=Json.Parse(FrameworkProofContent.Read("independent-tile-spill.json"));authorSpillResources?.Invoke(spill);
                if(spillHistory)spill.Remove("resource_flow_journals");
                files.Add("independent-tile-spill.json",spill.ToString());}
            if(spillEnvironment){var environment=Json.Parse(FrameworkProofContent.Read("independent-tile-spill-environment.json"));authorSpillEnvironment?.Invoke(environment);
                files.Add("independent-tile-spill-environment.json",environment.ToString());
                files.Add("independent-spill-observation.json",FrameworkProofContent.Read("independent-spill-observation.json"));}
            if(spillHistory){var history=Json.Parse(FrameworkProofContent.Read("independent-tile-spill-history.json"));authorSpillHistory?.Invoke(history);
                files.Add("independent-tile-spill-history.json",history.ToString());}
            if(spillDisposal){var disposal=Json.Parse(FrameworkProofContent.Read("independent-tile-disposal.json"));
                if(independentDisposal)disposal.Get("entity_activities")[0].Set("on_complete",JsonValue.Of("sensor.dispose-oil-independent"));
                authorSpillDisposal?.Invoke(disposal);files.Add("independent-tile-disposal.json",disposal.ToString());
                files.Add("independent-inventory-disposal.json",FrameworkProofContent.Read("independent-inventory-disposal.json"));}
            if(incidentPressure){var pressure=Json.Parse(FrameworkProofContent.Read("independent-tile-incident-process.json"));
                if(independentPressure){var parts=pressure.Get("entities")[0].Get("components");parts.Set("independent_incident_process",parts.Get("incident_process"));parts.Remove("incident_process");}
                authorIncidentPressure?.Invoke(pressure);files.Add("independent-tile-incident-process.json",pressure.ToString());}
            if(extra!=null)files.Add(extra.Get("documents")[0].Text,additionalDocument);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "independent-sensor", Seed = 723, Seats = { Controller.Human, Controller.Human } });
        private static EntityView Entity(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == id);
        private static ActivityNodeView Node(SimGame game, int order)
        {
            var nodes = new List<ActivityNodeView>();
            if (!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes)) throw new InvalidOperationException("missing disclosed work nodes");
            return nodes.Single();
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString());
            if (twin != null) Update(twin);
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory);
            c.That(game.World.Space is SensorTileSpace && !game.World.Services.Has<TileMap>(), "native work uses actual independent provider only");
            c.That(!game.World.Services.Has<IEconomy>(), "machine work requires no business module");
            var command = new PostOrderCommand { Target = Entity(game, "sensor.worksite").Id, Plan = "sensor.inspection" };
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary authored work preview admits actual dock target");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary player command posts native work DAG");
            int order = (int)result.Arg0; var origin = (TileSpatialSnapshot)Entity(game, "sensor.cart").Spatial;
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending native work/dock state saves without another scheduler");
            long step = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Update(game, twin);
            c.Equal(step, game.Clock.StepIndex, "paused sole clock advances neither work nor travel");
            game.Clock.Resume(); twin.Clock.Resume(); bool travelled = false, partialSaved = false;
            int updates = 0; long earned = 0;
            bool Completed() => game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:sensor.inspection", command.Target, out _);
            while (!Completed() && updates++ < 160)
            {
                Update(game, twin); c.Equal(game.Hash(), twin.Hash(), "saved native job/dock/position owners continue identically");
                if (Completed()) break;
                var position = (TileSpatialSnapshot)Entity(game, "sensor.cart").Spatial; var node = Node(game, order);
                earned = Math.Max(earned, node.Progress.WholeTicks);
                travelled |= position.Position != origin.Position;
                if (node.Progress.WholeTicks > 0 && node.Progress.WholeTicks < node.RequiredTicks) {
                    c.Equal(new TileCoord(13, 20), position.Tile, "productive work requires actual authored service bay arrival");
                    if (!partialSaved && node.State != NodeState.Done) {
                        twin = factory.Load(game.Save()); partialSaved = true;
                        c.Equal(game.Hash(), twin.Hash(), "earned partial work and actual dock claim restore together");
                    }
                }
            }
            c.That(updates < 160 && travelled && partialSaved, "bounded work requires physical travel and interrupted productive save");
            c.That(Completed(), "native DAG completes authored docked inspection");
            c.Equal(6L, earned, "exact six authored game minutes earned before physical departure, no instant completion");
            c.Equal(new TileCoord(13, 21), ((TileSpatialSnapshot)Entity(game, "sensor.cart").Spatial).Tile,
                "completion includes physical exit through the authored departure slot");
            c.That(game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:sensor.inspection", command.Target, out _), "original engine completion view reports actual work fact");
            var hidden = new List<ActivityNodeView>();
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order, hidden) && hidden.Count == 0, "foreign company learns no private workflow progress");
            var fresh = New(factory); var orders = new List<OrderView>(); fresh.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            c.Equal(0, orders.Count, "interleaved fresh session receives no workflow state");
        }
    }
}
