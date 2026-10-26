using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using ScenarioEngine.Contracts;
using ScenarioEngine.Core;

namespace ScenarioEngine.Tests;

public sealed class PluginTests
{
    [Fact]
    public void ASecondDomainAndGameSubclassPlugInWithoutChangingCore()
    {
        var h = new Harness(new Counter.GameLayer.CounterLayer(), new SignalLayer());
        h.Scenario();
        var pack = Harness.Pack();
        pack["manifest"]!["packId"] = "examples.signal";
        pack["manifest"]!["gameLayerId"] = "test_signal";
        pack["documents"]!["scenario.json"]!["layerData"]!["initialValue"] = false;
        var predicate = JsonNode.Parse("""{"fact":"test_signal.enabled","op":"eq","value":true}""")!;
        pack["documents"]!["scenario.json"]!["closingCondition"] = predicate.DeepClone();
        pack["documents"]!["objective.json"]!["completeWhen"] = predicate.DeepClone();
        pack["documents"]!["objective.json"]!["failWhen"] = JsonNode.Parse("""{"fact":"core.tick","op":"lt","value":0}""");
        pack["documents"]!["ending.json"]!["condition"] = predicate.DeepClone();
        var scenario = h.Scenario(pack);
        Assert.Equal(2, h.Registry.ListInstalled().Length);
        Assert.Equal(2, h.Content.ListPlayable().Length);
        using var game = new SignalGame(h.Factory);
        game.Init(new(scenario, Harness.Options(), Harness.Frame(0)));
        Assert.True(game.Submit(new("toggle", "player", 1, 0, "test_signal.toggle", ContractJson.Element(new { }))).Accepted);
        var result = game.Update(Harness.Input(1));
        Assert.True(result.Committed);
        Assert.True(game.Snapshot.Layer.Data.GetProperty("enabled").GetBoolean());
        Assert.Equal(SessionPhase.Ended, game.Snapshot.Phase);
    }

    private sealed class SignalGame(IScenarioSessionFactory factory) : GameBase(factory)
    {
        protected override LayerIdentity GameLayer => new("test_signal", "1.0.0");
    }
    private sealed class SignalLayer : IGameLayer
    {
        private static JsonElement Json(string text)
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.Clone();
        }
        public string Id => "test_signal";
        public string Version => "1.0.0";
        public LayerCapabilities Capabilities { get; } = new(
            ImmutableDictionary<string, FactDefinition>.Empty.Add("test_signal.enabled", new(FactKind.Boolean)),
            ImmutableDictionary<string, JsonElement>.Empty.Add("test_signal.toggle", Json("""{"type":"object","additionalProperties":false}""")),
            ImmutableDictionary<string, JsonElement>.Empty,
            ImmutableDictionary<string, JsonElement>.Empty,
            Json("""{"type":"object","additionalProperties":false,"required":["initialValue"],"properties":{"initialValue":{"type":"boolean"}}}"""));
        public ValidationReport Validate(JsonElement content) => ValidationReport.Valid;
        public ILayerRuntime Create(LayerStartContext context) => new Runtime(context.Scenario.LayerData.GetProperty("initialValue").GetBoolean());
        private sealed class Runtime(bool initial) : ILayerRuntime
        {
            public LayerSnapshot InitialState => new(ContractJson.Element(new { enabled = initial }));
            public LayerStepResult Resolve(LayerSnapshot previous, LayerTickContext context)
            {
                var enabled = previous.Data.GetProperty("enabled").GetBoolean();
                foreach (var command in context.Commands) enabled = !enabled;
                return new(new(ContractJson.Element(new { enabled })),
                    [.. context.Commands.Select(c => new CommandOutcome(c.Command.CommandId, true, "APPLIED"))], [], []);
            }
            public LayerEffectResult ApplyEffects(LayerSnapshot state, ImmutableArray<EffectEnvelope> effects, LayerTickContext context) => new(state, []);
            public ImmutableDictionary<string, FactValue> ProjectFacts(LayerSnapshot state) =>
                ImmutableDictionary<string, FactValue>.Empty.Add("test_signal.enabled", FactValue.From(state.Data.GetProperty("enabled").GetBoolean()));
            public ValidationReport ValidateState(LayerSnapshot state) =>
                state.Data.GetProperty("enabled").ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? ValidationReport.Valid : new([new("SIGNAL_STATE", "Expected a boolean.")]);
        }
    }
}
