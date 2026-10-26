using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Public authored input and shared thin player workflows only; original runtime owns all processing.
    internal static class ProcessorContentContractTests
    {
        internal const string Case = "Processor content contracts: independent declarations reuse native physical processing";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Select(JsonValue setup)
        {
            var components = setup.Get("entities")[1].Get("components"); var selected = components.Get("transformer").Clone();
            components.Remove("transformer"); components.Set("independent_processor", selected);
        }
        private static JsonValue Body(JsonValue setup) => setup.Get("entities")[1].Get("components").Get("independent_processor");
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual processor contract build");
            c.That(typeof(Transformer).BaseType == typeof(Component)
                && typeof(Transformer).GetProperty("Def", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).PropertyType == typeof(ITransformerWipDefinition), "runtime depends on metadata contract, not native def");
            c.That(typeof(TransformerDef).GetMethod("Makes") == null, "no duplicate native support lookup or compatibility helper");
            TransformationTimingTests.Batch(c, 60, 2, new ProcessorContractModule(), Select);
            TransformationTimingTests.Batch(c, 10, 9, new ProcessorContractModule(), Select);
            TransformationTimingTests.Continuous(c, new ProcessorContractModule(), Select);
            foreach (string fault in new[] { "supply-missing", "duplicate-authored", "wip-missing", "wip-unknown", "state-missing",
                "unknown-recipe", "missing-input-port", "missing-tool-port", "wip-too-small", "tool-capacity", "content", "capacity", "states",
                "duplicate", "unsupported-repeat", "missing-operating-port", "missing-output-port", "unselected-port" })
            {
                var initialize = new InitializeProbe(); var processor = new ProcessorContractModule();
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(TransformationContentContractTests.Factory(initialize, out _, root =>
                {
                    if (fault == "missing-operating-port" || fault == "missing-output-port")
                    {
                        var flow = Json.Parse(FrameworkProofContent.Read("transformation-metadata.json")).Get("metadata_transformations")[1].Clone();
                        flow.Remove("fault"); flow.Set("id", JsonValue.Of("contract.make"));
                        root.Set("independent_transformations", JsonValue.NewArray()); root.Get("independent_transformations").Add(flow);
                    }
                    if (fault == "unselected-port")
                    {
                        var unused = root.Get("independent_transformations")[1];
                        unused.Set("inputs", Json.Parse("{\"supply\":1}")); unused.Set("input_ports", Json.Parse("{\"supply\":\"missing.input\"}"));
                    }
                    if (fault == "missing-input-port") root.Get("independent_transformations")[0].Get("input_ports").Set("supply", JsonValue.Of("missing.input"));
                    if (fault == "missing-tool-port") root.Get("independent_transformations")[0].Get("tool_ports").Set("supply", JsonValue.Of("missing.tool"));
                    if (fault == "missing-operating-port") root.Get("independent_transformations")[0].Get("operating_ports").Set("supply", JsonValue.Of("missing.operating"));
                    if (fault == "missing-output-port") root.Get("independent_transformations")[0].Get("output_ports").Set("supply", JsonValue.Of("missing.output"));
                }, setupEdit: setup =>
                {
                    Select(setup); var body = Body(setup);
                    if (fault == "unselected-port")
                    {
                        var unused = setup.Get("entities")[1].Clone(); unused.Set("id", JsonValue.Of("unused.processor"));
                        unused.Get("components").Get("point_presence").Set("space", JsonValue.Of("other.points"));
                        unused.Get("components").Get("independent_processor").Set("transformations", Json.Parse("[\"contract.unused-recipe\"]"));
                        setup.Get("entities").Add(unused);
                    }
                    if (fault == "supply-missing") body.Remove("automatic_supply");
                    if (fault == "duplicate-authored") body.Get("transformations").Add(JsonValue.Of("contract.make"));
                    if (fault == "wip-missing") body.Remove("wip_capacity");
                    if (fault == "wip-unknown") body.Set("wip_capacity", JsonValue.Of("missing.policy"));
                    if (fault == "state-missing") body.Get("states").Remove("working");
                    if (fault == "unknown-recipe") body.Set("transformations", Json.Parse("[\"missing.recipe\"]"));
                    if (fault == "wip-too-small") body.Set("wip_capacity", JsonValue.Of("contract.closed"));
                    if (fault == "tool-capacity") setup.Get("entities")[1].Get("components").Get("storage").Set("capacity_policy", JsonValue.Of("contract.closed"));
                    if (fault == "content" || fault == "capacity" || fault == "states" || fault == "duplicate" || fault.EndsWith("-repeat"))
                        body.Set("fault", JsonValue.Of(fault));
                }, worldEdit: root =>
                {
                    if (fault == "unselected-port")
                    {
                        var space = root.Get("point_spaces")[0].Clone(); space.Set("id", JsonValue.Of("other.points")); root.Get("point_spaces").Add(space);
                    }
                }, extraModule: processor)), "independent processor refuses before Initialize: " + fault);
                c.Equal(0, initialize.Initializations, "no Initialize after invalid processor binding: " + fault);
            }
        }
    }
}
