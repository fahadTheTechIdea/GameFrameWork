using System.Text.Json;
using Counter.GameLayer;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Tests;

public sealed class BoundaryPassTests
{
    [Fact]
    public void NullObservationIsRefusedWithoutFaultingSession()
    {
        using var simulation = new Harness().Start();
        var result = simulation.Step(new(1, new(0, 1, [null!])));
        Assert.False(result.Committed);
        Assert.Equal("WORLD_OBSERVATION_INVALID", result.Code);
        Assert.Equal(SessionPhase.Running, simulation.Snapshot.Phase);
        Assert.Equal(0, simulation.Snapshot.Tick);
    }

    [Fact]
    public void InvalidInitialWorldProducesContractDiagnostic()
    {
        var h = new Harness();
        var exception = Assert.Throws<ScenarioContractException>(() =>
            h.Factory.Create(h.Scenario(), Harness.Options(), new(0, 0, [null!])));
        Assert.Equal("WORLD_OBSERVATION_INVALID", exception.Code);
    }

    [Fact]
    public void NullOperationIsAnExplicitCommandRefusal()
    {
        using var simulation = new Harness().Start();
        var result = simulation.Enqueue(Harness.Increment() with { Operation = null! });
        Assert.False(result.Accepted);
        Assert.Equal("COMMAND_INVALID", result.Code);
    }

    [Fact]
    public void DuplicateCommandPropertiesAreRefused()
    {
        using var simulation = new Harness().Start();
        using var payload = JsonDocument.Parse("""{"amount":1,"amount":2}""");
        var result = simulation.Enqueue(Harness.Increment() with { Payload = payload.RootElement });
        Assert.False(result.Accepted);
        Assert.Equal("COMMAND_INVALID", result.Code);
    }

    [Fact]
    public void CommandBudgetCountsUtf8BytesRatherThanUtf16Characters()
    {
        using var simulation = new Harness(new OpenPayloadLayer()).Start();
        using var payload = JsonDocument.Parse("{\"amount\":1,\"padding\":\"" + new string('界', 30000) + "\"}");
        Assert.True(payload.RootElement.GetRawText().Length < 65536);
        Assert.Equal("COMMAND_INVALID", simulation.Enqueue(Harness.Increment() with { Payload = payload.RootElement }).Code);
    }

    [Fact]
    public void ObservationPayloadBudgetIsEnforcedBeforeLayerExecution()
    {
        using var simulation = new Harness(new OpenPayloadLayer()).Start();
        using var payload = JsonDocument.Parse("{\"delta\":1,\"padding\":\"" + new string('界', 30000) + "\"}");
        var result = simulation.Step(new(1, new(0, 1, [new(1, "test_counter.adjustment", null, payload.RootElement)])));
        Assert.False(result.Committed);
        Assert.Equal("WORLD_OBSERVATION_INVALID", result.Code);
        Assert.Equal(0, Harness.Value(simulation.Snapshot));
        Assert.Equal(SessionPhase.Running, simulation.Snapshot.Phase);
    }

    [Theory]
    [InlineData("{\"delta\":1,\"delta\":2}")]
    [InlineData("{\"nested\":{\"delta\":1,\"delta\":2}}")]
    public void DuplicateObservationPropertiesPreserveThePreviousCommit(string json)
    {
        using var simulation = new Harness(new OpenPayloadLayer()).Start();
        var before = simulation.Snapshot;
        using var payload = JsonDocument.Parse(json);
        var result = simulation.Step(new(1, new(0, 1, [new(1, "test_counter.adjustment", null, payload.RootElement)])));
        Assert.Equal("WORLD_OBSERVATION_INVALID", result.Code);
        Assert.Same(before, result.Snapshot);
    }

    [Fact]
    public void AggregateFrameBudgetRefusesIndividuallyLegalObservations()
    {
        using var simulation = new Harness(new OpenPayloadLayer()).Start();
        var before = simulation.Snapshot;
        using var payload = JsonDocument.Parse("{\"delta\":1,\"padding\":\"" + new string('a', 60000) + "\"}");
        var observations = Enumerable.Range(1, 18)
            .Select(i => new WorldObservation(i, "test_counter.adjustment", null, payload.RootElement));
        var result = simulation.Step(new(1, new(0, 1, [.. observations])));
        Assert.Equal("WORLD_FRAME_LIMIT", result.Code);
        Assert.Same(before, result.Snapshot);
    }

    [Fact]
    public void DisposedObservationPayloadIsRefusedAndTheNextValidFrameCanCommit()
    {
        using var simulation = new Harness().Start();
        var document = JsonDocument.Parse("{\"delta\":1}");
        var payload = document.RootElement;
        document.Dispose();
        var refused = simulation.Step(new(1, new(0, 1, [new(1, "test_counter.adjustment", null, payload)])));
        Assert.Equal("WORLD_OBSERVATION_INVALID", refused.Code);
        Assert.True(simulation.Step(Harness.Input(1)).Committed);
    }

    private sealed class OpenPayloadLayer : IGameLayer
    {
        private readonly CounterLayer _inner = new();
        public string Id => _inner.Id;
        public string Version => _inner.Version;
        public LayerCapabilities Capabilities
        {
            get
            {
                using var schema = JsonDocument.Parse("""{"type":"object"}""");
                var open = schema.RootElement.Clone();
                return _inner.Capabilities with
                {
                    Operations = _inner.Capabilities.Operations.SetItem("test_counter.increment", open),
                    Observations = _inner.Capabilities.Observations.SetItem("test_counter.adjustment", open)
                };
            }
        }
        public ValidationReport Validate(JsonElement content) => _inner.Validate(content);
        public ILayerRuntime Create(LayerStartContext context) => _inner.Create(context);
    }
}
