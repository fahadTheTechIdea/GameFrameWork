using System.Collections.Immutable;
using System.Text.Json;

namespace ScenarioEngine.Contracts;

public readonly record struct EntityId(long Value);
public readonly record struct PositionMm(int X, int Y, int Z);
public enum SessionPhase { Ready, Running, Paused, Transitioning, Ended, Faulted, Disposed }
public sealed record CommandEnvelope(string CommandId, string ControllerId, long TargetTick,
    int WorldGeneration, string Operation, JsonElement Payload);
public sealed record QueuedCommand(long Sequence, CommandEnvelope Command);
public enum CommandStatus { Queued, Cancelled, Applied, Rejected }
public sealed record CommandReceipt(string CommandId, string Fingerprint, string ControllerId,
    long Sequence, CommandStatus Status, string Code);
public sealed record AdmissionResult(bool Accepted, string Code, long? IngressSequence);
public sealed record WorldObservation(long Sequence, string Kind, EntityId? Entity, JsonElement Payload);
public sealed record WorldFrame(int Generation, long ForTick, ImmutableArray<WorldObservation> Observations);
public sealed record TickInput(long Tick, WorldFrame World);
public sealed record DomainEvent(string Kind, JsonElement Payload, string? CommandId = null, EntityId? Entity = null);
public sealed record EventEnvelope(long Sequence, long Tick, string Kind,
    string? CommandId, EntityId? Entity, JsonElement Payload);
public sealed record WorldRequest(string RequestId, int Generation, string Kind, JsonElement Payload);
public sealed record CommandOutcome(string CommandId, bool Applied, string Code);
public sealed record LayerSnapshot(JsonElement Data);
public sealed record RandomStreamState(ulong State, ulong Increment, long DrawCount);
public sealed record ScenarioStartOptions(string SessionId, ulong Seed, ImmutableHashSet<string> Controllers);
public sealed record LayerStartContext(ScenarioDefinition Scenario, ScenarioStartOptions Options, WorldFrame World);
public sealed record LayerTickContext(
    long Tick, TimeSpan FixedStep, WorldFrame World, ImmutableArray<QueuedCommand> Commands, IRandomStreams Random);
public sealed record LayerStepResult(LayerSnapshot State, ImmutableArray<CommandOutcome> Outcomes,
    ImmutableArray<DomainEvent> Events, ImmutableArray<WorldRequest> WorldRequests);
public sealed record EffectEnvelope(string Type, string TransitionKey, JsonElement Arguments);
public sealed record LayerEffectResult(LayerSnapshot State, ImmutableArray<DomainEvent> Events);
public interface IRandomStreams
{
    uint NextUInt32(string streamName, EntityId? entity = null);
}
public interface IGameLayer
{
    string Id { get; }
    string Version { get; }
    LayerCapabilities Capabilities { get; }
    ValidationReport Validate(JsonElement content);
    ILayerRuntime Create(LayerStartContext context);
}
public interface ILayerRuntime
{
    LayerSnapshot InitialState { get; }
    LayerStepResult Resolve(LayerSnapshot previous, LayerTickContext context);
    LayerEffectResult ApplyEffects(LayerSnapshot candidate, ImmutableArray<EffectEnvelope> effects, LayerTickContext context);
    ImmutableDictionary<string, FactValue> ProjectFacts(LayerSnapshot state);
    ValidationReport ValidateState(LayerSnapshot state);
}
public sealed record SessionSnapshot(
    ScenarioIdentity Identity, string SessionId, ulong Seed, ImmutableHashSet<string> Controllers,
    SessionPhase Phase, long Tick, int WorldGeneration, long LastObservationSequence,
    long LastEventSequence, long LastIngressSequence,
    ImmutableDictionary<string, ObjectiveStatus> Objectives,
    ImmutableHashSet<string> AppliedEffects, string? EndingId, string? OutcomeId,
    LayerSnapshot Layer, ImmutableArray<QueuedCommand> PendingCommands,
    ImmutableDictionary<string, CommandReceipt> Receipts,
    ImmutableDictionary<string, RandomStreamState> RandomStreams);
public sealed record StepResult(bool Committed, string Code, long Tick,
    ImmutableArray<EventEnvelope> Events, ImmutableArray<WorldRequest> WorldRequests,
    SessionSnapshot Snapshot, string RuleStateHash);
public interface IScenarioSimulation : IDisposable
{
    SessionSnapshot Snapshot { get; }
    AdmissionResult Enqueue(CommandEnvelope command);
    AdmissionResult CancelQueued(string controllerId, string commandId);
    void Start();
    void Pause();
    void Resume();
    StepResult Step(TickInput input);
}
public sealed record GameStart(ValidatedScenario Scenario, ScenarioStartOptions Options, WorldFrame InitialWorld);
public interface IGame : IDisposable
{
    SessionSnapshot Snapshot { get; }
    void Init(GameStart start);
    void Restore(ValidatedScenario scenario, SessionSnapshot saved, WorldFrame restoredWorld);
    StepResult Update(TickInput input);
    AdmissionResult Submit(CommandEnvelope command);
    AdmissionResult CancelQueued(string controllerId, string commandId);
    void Pause();
    void Resume();
}
public interface IGameFactory
{
    IGame Create(ValidatedScenario scenario);
}
