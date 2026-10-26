using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    // Authoring and ordinary public lifecycle only; unused metadata is never executed by this driver.
    internal static class TransformationMetadataTests
    {
        internal const string Case = "Transformation metadata: canonical requirements and owned routed rates before initialization";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe initialize, out TransformationMetadataModule metadata, Action<JsonValue> edit = null)
        {
            metadata = new TransformationMetadataModule();
            var capacity = new CapacityContractModule(initialize); var recipes = new TransformationContractModule();
            var document = Json.Parse(FrameworkProofContent.Read("transformation-metadata.json")); edit?.Invoke(document);
            var setup = Json.Parse(FrameworkProofContent.Read("capacity-contracts.json")); setup.Set("transformations", JsonValue.NewArray());
            return ConstructionPlanTests.Points(extraModules: new GameModule[] { capacity, recipes, metadata },
                extraDeclarations: "[{\"id\":\"proof.capacity-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"capacity-contracts.json\"]},{\"id\":\"proof.transformation-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"transformations-contracts.json\"]},{\"id\":\"proof.transformation-metadata\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"transformation-metadata.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "capacity-contracts.json", setup.ToString() },
                    { "transformations-contracts.json", FrameworkProofContent.Read("transformations-contracts.json") }, { "transformation-metadata.json", document.ToString() } },
                startupProfiles: new[] { "contract.capacity-start" },
                scenarioEdit: scenario => { foreach (var id in new[] { capacity.Id, recipes.Id, "proof.transformation-metadata" }) scenario.Get("modules").Add(JsonValue.Of(id)); },
                worldEdit: root => root.Get("entities")[2].Get("components").Get("storage").Set("capacity_policy", JsonValue.Of("contract.carrier")));
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current metadata validation build");
            var initialize = new InitializeProbe(); var factory = Factory(initialize, out var metadata); var game = New(factory);
            c.That(metadata.ActualAliases, "healthy independent batch and continuous metadata resolves once with original aliases");
            c.Equal(1, initialize.Initializations, "all native and independent metadata validated before one Initialize");
            var twin = factory.Load(game.Save());
            for (int i = 0; i < 12; i++) { game.Update(); twin.Update(); c.That(game.Fault == null && twin.Fault == null && game.Hash() == twin.Hash(), "valid unused content deterministic step " + i); }
            long at = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); game.Update();
            c.That(at == game.Clock.StepIndex && hash == game.Hash(), "no extra content clock"); game.Clock.Resume();
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "healthy mixed metadata save/load");
            foreach (string fault in new[] { "startup-missing", "startup-extra", "startup-quantity", "tool-overdraw", "tool-unheld", "batch-rate",
                "continuous-bill", "tool-rate-unheld", "rate-null", "rate-foreign", "rate-zero", "rate-negative", "rate-fraction", "rate-numerator", "rate-denominator", "rate-unit",
                "input-duplicate-rate", "output-duplicate-rate", "tool-duplicate-rate", "operating-duplicate-rate",
                "input-invalid-rate", "output-invalid-rate", "tool-invalid-rate",
                "input-missing-route", "output-missing-route", "tool-missing-route", "operating-missing-route",
                "input-duplicate-route", "output-duplicate-route", "tool-duplicate-route", "operating-duplicate-route",
                "input-unrelated-route", "output-unrelated-route", "tool-unrelated-route", "operating-unrelated-route",
                "input-invalid-route", "output-invalid-route", "tool-invalid-route", "operating-invalid-route",
                "input-none-route", "output-none-route", "tool-none-route", "operating-none-route" })
            {
                var refused = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(refused, out _, root =>
                {
                    bool continuous = fault == "continuous-bill" || fault == "tool-rate-unheld" || fault.Contains("-rate") && !fault.StartsWith("rate-", StringComparison.Ordinal) && fault != "batch-rate";
                    var row = root.Get("metadata_transformations")[continuous ? 1 : 0]; row.Set("fault", JsonValue.Of(fault));
                })), "central unused provider refusal: " + fault);
                c.Equal(0, refused.Initializations, "no Initialize after inconsistent metadata: " + fault);
            }
            foreach (string fault in new[] { "native-tool", "native-route", "native-empty", "native-flow" })
            {
                var refused = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(refused, out _, root =>
                {
                    var row = root.Get("metadata_transformations")[fault == "native-flow" ? 1 : 0].Clone();
                    row.Remove("fault"); row.Set("id", JsonValue.Of("metadata.native-invalid"));
                    if (fault == "native-tool") row.Set("tool_consumption", Json.Parse("{\"supply\":2}"));
                    if (fault == "native-route") row.Set("output_ports", JsonValue.NewObject());
                    if (fault == "native-empty") { row.Set("outputs", JsonValue.NewObject()); row.Set("output_ports", JsonValue.NewObject()); }
                    if (fault == "native-flow") { row.Set("outputs", JsonValue.NewArray()); row.Set("output_ports", JsonValue.NewObject()); }
                    root.Set("transformations", JsonValue.NewArray()); root.Get("transformations").Add(row);
                })), "same central native refusal: " + fault);
                c.Equal(0, refused.Initializations, "native refusal before Initialize: " + fault);
            }
        }
    }
}
