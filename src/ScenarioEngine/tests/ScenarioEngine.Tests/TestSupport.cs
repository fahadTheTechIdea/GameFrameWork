using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Counter.GameLayer;
using ScenarioEngine.Content;
using ScenarioEngine.Contracts;
using ScenarioEngine.Core;

namespace ScenarioEngine.Tests;

internal sealed class Recorder : IEngineReporter
{
    public List<EngineNotice> Notices { get; } = [];
    public List<AuditRecord> Audits { get; } = [];
    public bool FailAudit { get; set; }
    public void Report(EngineNotice notice) => Notices.Add(notice);
    public void Audit(AuditRecord record)
    {
        if (FailAudit) throw new IOException("Injected audit storage outage.");
        Audits.Add(record);
    }
}
internal sealed class Harness
{
    public Recorder Reporter { get; } = new();
    public IGameLayerRegistry Registry { get; }
    public JsonSchemaContractValidator Validator { get; } = new();
    public ScenarioContentService Content { get; }
    public ScenarioSessionFactory Factory { get; }
    public Harness(params IGameLayer[] layers)
    {
        Registry = new GameLayerRegistry(layers.Length == 0 ? [new CounterLayer()] : layers);
        Content = new(Registry, Validator, Reporter);
        Factory = new(Registry, Validator, Reporter);
    }
    public static JsonNode Pack() => JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "counter.json")))!;
    public ContentLoadResult Load(JsonNode? node = null) =>
        Content.LoadNormalized(Encoding.UTF8.GetBytes((node ?? Pack()).ToJsonString()));
    public ValidatedScenario Scenario(JsonNode? node = null)
    {
        var loaded = Load(node);
        Assert.True(loaded.IsReady, string.Join("; ", loaded.Report.Diagnostics.Select(d => d.Code + ":" + d.Message)));
        return Assert.Single(loaded.Scenarios);
    }
    public IScenarioSimulation Start(ValidatedScenario? scenario = null, string sessionId = "test")
    {
        var simulation = Factory.Create(scenario ?? Scenario(), Options(sessionId), Frame(0));
        simulation.Start();
        return simulation;
    }
    public static ScenarioStartOptions Options(string id = "test") => new(id, 42, ImmutableHashSet.Create("player"));
    public static WorldFrame Frame(long tick, int generation = 0) => new(generation, tick, []);
    public static CommandEnvelope Increment(long tick = 1, string id = "one", int amount = 1, string controller = "player") =>
        new(id, controller, tick, 0, "test_counter.increment", ContractJson.Element(new { amount }));
    public static TickInput Input(long tick) => new(tick, Frame(tick));
    public static long Value(SessionSnapshot state) => state.Layer.Data.GetProperty("value").GetInt64();
}
internal sealed class DecoratingLayer(Func<LayerStepResult, LayerTickContext, LayerStepResult> onResolve) : IGameLayer
{
    private readonly CounterLayer _inner = new();
    public string Id => _inner.Id;
    public string Version => _inner.Version;
    public LayerCapabilities Capabilities => _inner.Capabilities;
    public ValidationReport Validate(JsonElement content) => _inner.Validate(content);
    public ILayerRuntime Create(LayerStartContext context) => new Runtime(_inner.Create(context), onResolve);
    private sealed class Runtime(ILayerRuntime inner, Func<LayerStepResult, LayerTickContext, LayerStepResult> hook) : ILayerRuntime
    {
        public LayerSnapshot InitialState => inner.InitialState;
        public LayerStepResult Resolve(LayerSnapshot previous, LayerTickContext context) => hook(inner.Resolve(previous, context), context);
        public LayerEffectResult ApplyEffects(LayerSnapshot state, ImmutableArray<EffectEnvelope> effects, LayerTickContext context) => inner.ApplyEffects(state, effects, context);
        public ImmutableDictionary<string, FactValue> ProjectFacts(LayerSnapshot state) => inner.ProjectFacts(state);
        public ValidationReport ValidateState(LayerSnapshot state) => inner.ValidateState(state);
    }
}
