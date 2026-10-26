using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Sole shared physical-only recovery fixture authoring; runtime rules stay with engine modules.
    internal static class PhysicalRecoveryTestContent
    {
        internal static GameFactory Factory(string additional = null, string infrastructure = null, bool cargo = false, bool supplier = false,
            Action<JsonValue> edit = null, IReadOnlyDictionary<string, string> additionalDocuments = null,
            IReadOnlyList<GameModule> additionalModules = null, IReadOnlyList<string> moduleDeclarations = null,
            string worldDefinitions = null, string flowDefinitions = null, bool automaticRecovery = false)
        {
            var authored = Json.Parse(worldDefinitions ?? FrameworkProofContent.Read("resource-recovery.json"));
            var entities = authored.Get("entities"); var physical = JsonValue.NewArray();
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].Get("id").Text == "market" && !supplier) continue;
                if (cargo && entities[i].Get("id").Text == "depot")
                    entities[i].Get("components").Get("storage").Set("public", JsonValue.Of(false));
                physical.Add(entities[i]);
            }
            authored.Set("entities", physical); WorkerExecutionTestContent.Add(authored);
            authored.Get("units").Add(Json.Parse("{\"id\":\"unit.work-rate\",\"dimension\":\"work-rate\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":false}"));
            var scenario = authored.Get("scenarios")[0];
            if (supplier) scenario.Get("modules").Add(JsonValue.Of(TycoonModule.ModuleId));
            scenario.Get("modules").Add(JsonValue.Of(WorldSetupModule.ModuleId));
            scenario.Get("settings").Set(WorldSetupModule.ModuleId, Json.Parse("{\"profile\":\"disposition.start\"}"));
            var documents = new Dictionary<string, string> {
                ["resource-disposition-navigation.json"] = FrameworkProofContent.Read("resource-disposition-navigation.json"),
                ["resource-disposition-startup.json"] = FrameworkProofContent.Read("resource-disposition-startup.json") };
            var selectedModules = new List<GameModule>();
            var declarations = new List<string>();
            if (cargo || automaticRecovery)
            {
                scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"recovery.high\"}"));
                documents.Add("logistics-recovery.json", FrameworkProofContent.Read("logistics-recovery.json"));
                selectedModules.Add(new LogisticsRecoveryPolicyModule());
                declarations.Add("{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}");
            }
            edit?.Invoke(authored);
            if (additionalModules != null) selectedModules.AddRange(additionalModules);
            if (moduleDeclarations != null) declarations.AddRange(moduleDeclarations);
            var text = new System.Text.StringBuilder(); authored.WriteCanonical(text);
            var master = Json.Parse(FrameworkProofContent.Read("resource-disposition-master.json"));
            if (supplier)
            {
                var modules = master.Get("modules");
                for (int i = 0; i < modules.Count; i++) if (modules[i].Get("id").Text == TycoonModule.ModuleId)
                    modules[i].Get("documents").Add(JsonValue.Of("game-mode-properties.json"));
            }
            return Colony.Factory(text.ToString(), flowDefinitions ?? FrameworkProofContent.Read("endpoints-flows.json"), infrastructureDefinitions: infrastructure,
                additionalDefinitions: additional == null ? null : WorkerExecutionTestContent.Author(additional),
                additionalDocuments: additionalDocuments, additionalModules: selectedModules,
                moduleDeclarations: declarations,
                moduleDocuments: documents, masterDocument: master.ToString());
        }
    }
}
