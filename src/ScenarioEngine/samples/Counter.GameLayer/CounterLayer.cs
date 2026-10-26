using System.Collections.Immutable;
using System.Text.Json;
using ScenarioEngine.Contracts;
using ScenarioEngine.Core;

namespace Counter.GameLayer;

/// <summary>A deliberately unrelated domain proving the engine is reusable.</summary>
public sealed class CounterLayer : IGameLayer
{
    public string Id => "test_counter";
    public string Version => "1.0.0";
    private static JsonElement Schema(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
    private static readonly JsonElement Empty = Schema("""{"type":"object","additionalProperties":false}""");
    private static readonly JsonElement Amount = Schema("""{"type":"object","required":["amount"],"additionalProperties":false,"properties":{"amount":{"type":"integer","minimum":1,"maximum":10}}}""");
    private static readonly JsonElement Observation = Schema("""{"type":"object","required":["delta"],"additionalProperties":false,"properties":{"delta":{"type":"integer","minimum":-100,"maximum":100}}}""");
    public LayerCapabilities Capabilities { get; } = new(
        ImmutableDictionary<string, FactDefinition>.Empty.Add("test_counter.value", new(FactKind.Integer, 0, 1000)),
        ImmutableDictionary<string, JsonElement>.Empty.Add("test_counter.increment", Amount).Add("test_counter.roll", Empty),
        ImmutableDictionary<string, JsonElement>.Empty.Add("test_counter.bonus", Amount),
        ImmutableDictionary<string, JsonElement>.Empty.Add("test_counter.adjustment", Observation),
        Schema("""{"type":"object","required":["initialValue"],"additionalProperties":false,"properties":{"initialValue":{"type":"integer","minimum":0,"maximum":100}}}"""));
    public ValidationReport Validate(JsonElement content) => ValidationReport.Valid;
    public ILayerRuntime Create(LayerStartContext context) => new Runtime(context);
    private sealed class Runtime(LayerStartContext start) : ILayerRuntime
    {
        public LayerSnapshot InitialState { get; } = new(ContractJson.Element(new { value = start.Scenario.LayerData.GetProperty("initialValue").GetInt64() }));
        public LayerStepResult Resolve(LayerSnapshot previous, LayerTickContext context)
        {
            var value = previous.Data.GetProperty("value").GetInt64();
            var events = ImmutableArray.CreateBuilder<DomainEvent>();
            foreach (var observation in context.World.Observations)
            {
                value = checked(value + observation.Payload.GetProperty("delta").GetInt64());
                events.Add(new("test_counter.adjusted", ContractJson.Element(new { value })));
            }
            var results = ImmutableArray.CreateBuilder<CommandOutcome>();
            foreach (var queued in context.Commands)
            {
                var command = queued.Command;
                var amount = command.Operation == "test_counter.roll"
                    ? context.Random.NextUInt32("die") % 6 + 1
                    : command.Payload.GetProperty("amount").GetInt64();
                if (value + amount > 1000) { results.Add(new(command.CommandId, false, "COUNTER_LIMIT")); continue; }
                value += amount;
                results.Add(new(command.CommandId, true, "APPLIED"));
                events.Add(new("test_counter.incremented", ContractJson.Element(new { value }), command.CommandId));
            }
            return new(new(ContractJson.Element(new { value })), results.ToImmutable(), events.ToImmutable(), []);
        }
        public LayerEffectResult ApplyEffects(LayerSnapshot candidate, ImmutableArray<EffectEnvelope> effects, LayerTickContext context)
        {
            var value = candidate.Data.GetProperty("value").GetInt64();
            foreach (var effect in effects) value = checked(value + effect.Arguments.GetProperty("amount").GetInt64());
            return new(new(ContractJson.Element(new { value })), []);
        }
        public ImmutableDictionary<string, FactValue> ProjectFacts(LayerSnapshot state) =>
            ImmutableDictionary<string, FactValue>.Empty.Add("test_counter.value", FactValue.From(state.Data.GetProperty("value").GetInt64()));
        public ValidationReport ValidateState(LayerSnapshot state)
        {
            if (!state.Data.TryGetProperty("value", out var value) || !value.TryGetInt64(out var number) || number is < 0 or > 1000)
                return new([new("COUNTER_STATE", "Counter value must be an integer in 0..1000.")]);
            return ValidationReport.Valid;
        }
    }
}
public sealed class CounterGame(IScenarioSessionFactory sessions) : GameBase(sessions)
{
    protected override LayerIdentity GameLayer => new("test_counter", "1.0.0");
}
