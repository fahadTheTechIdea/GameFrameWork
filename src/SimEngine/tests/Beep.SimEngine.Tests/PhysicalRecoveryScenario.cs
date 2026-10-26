using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Pure authored content composition. No session, tick, reservation, movement or stock decisions.
    internal static class PhysicalRecoveryScenario
    {
        internal static GameFactory Create(bool fuelShortage, bool endpointLoss = false, bool blockedOutput = false, bool durability = false, bool halfMinute = false, bool emergencyResponse = false, bool responseCancellation = false, bool environmentalResponse = false, bool externalResponse = false)
        {
            var root = Json.Parse(Colony.Defs);
            root.Get("scenarios")[0].Get("settings").Get("sim.world-setup")
                .Set("profile", JsonValue.Of("recovery.acceptance.start"));
            var activities = root.Get("activities");
            for (int i = 0; i < activities.Count; i++) if (activities[i].Get("id").Text == "road.segment-work")
            {
                // This scenario authors dry road work; no material or money fallback exists at runtime.
                var node = activities[i].Get("nodes")[0];
                node.Remove("requirement");
                node.Set("duration", Json.Parse("{\"numerator\":6,\"denominator\":1,\"unit\":\"minute\"}"));
            }
            var content = Json.Parse(FrameworkProofContent.Read("physical-recovery-acceptance.json"));
            if (fuelShortage) {
                var entities = content.Get("entities");
                for (int i = 0; i < entities.Count; i++) if (entities[i].Get("id").Text == "recovery.carrier")
                    entities[i].Get("components").Get("storage").Get("initial_stock").Set("recovery.fuel", JsonValue.Number("2"));
            }
            var retry = Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            retry.Set("work_retry_profiles", Json.Parse(FrameworkProofContent.Read("haul-retry.json")).Get("work_retry_profiles"));
            var documents = new Dictionary<string, string>();
            var externalFiles = new Dictionary<string, string>();
            documents.Add("physical-recovery-breakdown.json", FrameworkProofContent.Read("physical-recovery-breakdown.json"));
            if (emergencyResponse || responseCancellation || environmentalResponse || externalResponse) {
                // This scenario authors both carried-count and volume for every stored resource.
                var resources = root.Get("resources");
                for (int i = 0; i < resources.Count; i++) if (resources[i].Get("id").Text == "steel")
                    resources[i].Get("capacity").Set("unit.capacity.volume", JsonValue.Number("1"));
                content.Get("resources")[0].Get("capacity").Set("unit.capacity.volume", JsonValue.Number("1"));
                var breakdown = Json.Parse(documents["physical-recovery-breakdown.json"]);
                breakdown.Get("resources")[0].Get("capacity").Set("unit.capacity.volume", JsonValue.Number("1"));
                documents["physical-recovery-breakdown.json"] = breakdown.ToString();
                var response = Json.Parse(FrameworkProofContent.Read("emergency-response-acceptance.json"));
                if (environmentalResponse || externalResponse) {
                    var environment = Json.Parse(FrameworkProofContent.Read("environment-response-acceptance.json"));
                    if (externalResponse) {
                        var metrics = environment.Get("environmental_metrics");
                        for (int i = 0; i < metrics.Count; i++) metrics[i].Set("fault", JsonValue.Of("none"));
                        environment.Set("independent_environment_metrics", metrics); environment.Remove("environmental_metrics");
                        externalFiles.Add("external-environment.json", environment.ToString());
                    }
                    else documents.Add("environment-response-acceptance.json", environment.ToString());
                    response.Get("activities")[0].Get("nodes")[1].Set("effect", JsonValue.Of("environment.spill"));
                    var cleanup = response.Get("target_resource_requirements")[3];
                    cleanup.Set("use", JsonValue.Of("hold")); cleanup.Set("quantities", Json.Parse("{\"steel\":1}"));
                    var station = response.Get("entities")[0].Get("components");
                    station.Get("storage").Get("initial_stock").Set("response.waste", JsonValue.Number("2"));
                    station.Get("transformer").Set("transformations", Json.Parse("[\"environment.recovery\"]"));
                    station.Get("signal_transformation_gate").Set("transformations", Json.Parse("[\"environment.recovery\"]"));
                }
                if (responseCancellation) {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResponseCompletionGuardModule.ModuleId));
                    var nodes = response.Get("activities")[0].Get("nodes");
                    nodes.Add(Json.Parse("{\"id\":\"guard\",\"executor\":\"work.timed\",\"after\":[\"detect\"],\"duration\":{\"numerator\":0,\"denominator\":1,\"unit\":\"minute\"},\"effect\":\"response.completion-guard\"}"));
                    nodes[1].Set("after", Json.Parse("[\"guard\"]"));
                    response.Get("incident_workflows")[0].Get("phases")[0].Get("nodes").Add(JsonValue.Of("guard"));
                    response.Get("logistics_policies")[0].Get("outlet").Get("reserve").Set("recovery.fuel", JsonValue.Number("96"));
                    response.Get("entities")[0].Get("components").Get("storage").Get("initial_stock").Set("response.waste", JsonValue.Number("2"));
                    response.Get("entities")[0].Get("components").Set("activity_record", Json.Parse("{}"));
                    var depot = response.Get("entities")[1].Get("components").Get("storage").Get("initial_stock");
                    depot.Set("response.spare", JsonValue.Number("4")); depot.Set("response.suppressant", JsonValue.Number("4"));
                }
                if (externalResponse) {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ExternalResponseModule.ModuleId));
                    var station = response.Get("entities")[0].Get("components");
                    station.Set("external_incident", station.Get("incident")); station.Remove("incident");
                    response.Set("independent_incident_workflows", response.Get("incident_workflows")); response.Remove("incident_workflows");
                    response.Set("independent_incident_completions", response.Get("response_effects")); response.Remove("response_effects");
                    externalFiles.Add("external-response.json", response.ToString());
                }
                else documents.Add("emergency-response-acceptance.json", response.ToString());
                if (halfMinute) root.Get("scenarios")[0].Set("time", JsonValue.Of("response.half-minute"));
                var prototypes = root.Get("entities");
                for (int i = 0; i < prototypes.Count; i++) if (prototypes[i].Get("id").Text == "crane") {
                    var parts = prototypes[i].Get("components");
                    parts.Set("storage", Json.Parse("{\"capacity_policy\":\"response.store\",\"public\":false,\"initial_stock\":{\"recovery.fuel\":64}}"));
                    if (responseCancellation) parts.Get("storage").Get("initial_stock").Set("recovery.fuel", JsonValue.Number("96"));
                    parts.Set("operating_inputs", Json.Parse("{\"id\":\"propulsion\",\"inputs\":[{\"resource\":\"recovery.fuel\",\"quantity\":1,\"period\":{\"numerator\":2,\"denominator\":1,\"unit\":\"minute\"}}],\"phase\":\"movement\",\"states\":{\"disabled\":\"stopped\"}}"));
                }
                var entities = content.Get("entities");
                for (int i = 0; i < entities.Count; i++) {
                    var parts = entities[i].Get("components"); string id = entities[i].Get("id").Text;
                    if (id == "recovery.carrier" || id == "recovery.service") {
                        parts.Get("storage").Set("capacity_policy", JsonValue.Of("response.store"));
                        parts.Get("transporter").Set("handling", JsonValue.Of("response.handling"));
                    }
                    if (id == "recovery.service") {
                        parts.Get("storage").Get("initial_stock").Set("recovery.fuel", JsonValue.Number(responseCancellation ? "96" : "64"));
                        parts.Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"response.service-fuel\"}]}"));
                        parts.Set("operating_inputs", Json.Parse("{\"id\":\"propulsion\",\"inputs\":[{\"resource\":\"recovery.fuel\",\"quantity\":1,\"period\":{\"numerator\":2,\"denominator\":1,\"unit\":\"minute\"}}],\"phase\":\"movement\",\"states\":{\"disabled\":\"stopped\"}}"));
                    }
                }
                var entries = content.Get("world_setup_profiles")[0].Get("entries");
                entries.Add(JsonValue.Of("response.station-start")); entries.Add(JsonValue.Of("response.depot-start"));
            }
            if (durability) {
                documents.Add("physical-plan-durability.json", FrameworkProofContent.Read("physical-plan-durability.json"));
                if (halfMinute) root.Get("scenarios")[0].Set("time", JsonValue.Of("durability.half-minute"));
                var placements = content.Get("entity_setups");
                for (int i = 0; i < placements.Count; i++) if (placements[i].Get("id").Text == "recovery.destination-start")
                    placements[i].Set("entity", JsonValue.Of("durability.receiver"));
            }
            if (blockedOutput) {
                documents.Add("physical-recovery-output.json", FrameworkProofContent.Read("physical-recovery-output.json"));
                var entities = content.Get("entities");
                for (int i = 0; i < entities.Count; i++) {
                    var parts = entities[i].Get("components"); string id = entities[i].Get("id").Text;
                    if (id == "recovery.carrier" || id == "recovery.service")
                        parts.Get("transporter").Set("handling", JsonValue.Of("recovery.finished-handling"));
                    if (id == "recovery.destination") {
                        var resources = parts.Get("tile_work_dock").Get("resources");
                        resources.Add(JsonValue.Of("recovery.product")); resources.Add(JsonValue.Of("recovery.scrap"));
                    }
                }
                content.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("recovery.processor-start"));
            }
            if (endpointLoss) {
                var scenario = root.Get("scenarios")[0];
                scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"recovery.high\"}"));
                var entities = content.Get("entities");
                for (int i = 0; i < entities.Count; i++) {
                    var components = entities[i].Get("components"); string id = entities[i].Get("id").Text;
                    if (id == "recovery.destination") {
                        components.Remove("storage"); components.Remove("tile_work_dock");
                        components.Set("pool_port", Json.Parse("{\"id\":\"target\",\"pool\":\"recovery.original-book\",\"alternatives\":[],\"endpoint_scope\":\"colony.pools\",\"original_access\":\"retained\"}"));
                        components.Set("construction", Json.Parse("{\"site\":\"recovery.destination-site\",\"activity\":\"recovery.endpoint-build\",\"demolish\":{\"activity\":\"recovery.endpoint-remove\",\"salvage\":{}},\"states\":{\"site\":\"under-construction\"}}"));
                        components.Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":\"target\",\"profile\":\"recovery.nonrecipient\"}]}"));
                    }
                    if (id == "recovery.source" || id == "recovery.service")
                        components.Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"recovery.nonrecipient\"}]}"));
                }
                content.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("recovery.alternate-start"));
                content.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("recovery.funding-sink-start"));
                documents.Add("physical-recovery-endpoint.json", FrameworkProofContent.Read("physical-recovery-endpoint.json"));
            }
            documents.Add("physical-recovery-acceptance.json", content.ToString());
            return Colony.Factory(definitions: root.ToString(), retryProfiles: retry.ToString(),
                additionalDocuments: documents,
                additionalModules: externalResponse ? new GameModule[] { new ExternalResponseModule() } : responseCancellation ? new GameModule[] { new ResponseCompletionGuardModule() } : endpointLoss ? new GameModule[] { new LogisticsRecoveryPolicyModule() } : null,
                moduleDeclarations: externalResponse ? new[] { FrameworkProofContent.Read("external-response-module.json") } : responseCancellation ? new[] { "{\"id\":\"proof.response-completion-guard\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.incidents\",\"version\":\"1.0.0\"}],\"documents\":[\"response-cancellation.json\"]}" } : endpointLoss ? new[] { "{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}" } : null,
                moduleDocuments: externalResponse ? externalFiles : responseCancellation ? new Dictionary<string, string> { { "response-cancellation.json", FrameworkProofContent.Read("response-cancellation.json") } } : endpointLoss ? new Dictionary<string, string> { { "logistics-recovery.json", FrameworkProofContent.Read("logistics-recovery.json") } } : null);
        }
    }
}
