using System.Collections.Immutable;
using System.Text.Json;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Core;

/// <summary>Single writer for a generic scenario. All domain meanings are supplied by IGameLayer.</summary>
public sealed partial class ScenarioEngine : IScenarioSimulation
{
    private readonly object _gate = new();
    private readonly ValidatedScenario _scenario;
    private readonly IGameLayer _layer;
    private readonly ILayerRuntime _runtime;
    private readonly IContractValidator _validator;
    private readonly IEngineReporter _reporter;
    private SessionSnapshot _state;
    private bool _stepping;
    private readonly ImmutableArray<ObjectiveDefinition> _objectiveOrder;

    internal ScenarioEngine(ValidatedScenario scenario, ScenarioStartOptions options, WorldFrame world,
        IGameLayer layer, IContractValidator validator, IEngineReporter reporter, SessionSnapshot? restored)
    {
        _scenario = scenario;
        _layer = layer;
        _validator = validator;
        _reporter = reporter;
        if (string.IsNullOrWhiteSpace(options.SessionId) || options.SessionId.Length > 128 ||
            options.Controllers.Count is < 1 or > 16 ||
            options.Controllers.Any(string.IsNullOrWhiteSpace))
            throw new ScenarioContractException("START_OPTIONS", "Session and trusted controllers are required.");
        options = options with { Controllers = options.Controllers.WithComparer(StringComparer.Ordinal) };
        if (restored is not null) SessionSnapshotRules.Validate(restored);
        _objectiveOrder = ScenarioDefinitionRules.ValidateAndOrder(scenario.Definition, layer.Capabilities);
        foreach (var effect in scenario.Definition.Objectives.SelectMany(o => o.SuccessEffects.Concat(o.FailureEffects)))
            RequireValid(validator.Validate(layer.Capabilities.Effects[effect.Type], effect.Arguments), "EFFECT_ARGUMENTS");
        RequireValid(validator.Validate(layer.Capabilities.ContentSchema, scenario.Definition.LayerData), "LAYER_CONTENT");
        RequireValid(layer.Validate(scenario.Definition.LayerData), "LAYER_CONTENT");
        var shapeError = WorldFrameRules.ValidateShape(world);
        if (shapeError is not null) throw new ScenarioContractException(shapeError, "Invalid initial world frame.");
        if (restored is null && (world.ForTick != 0 || world.Generation != 0))
            throw new ScenarioContractException("WORLD_INITIAL", "Initial world must be tick zero and generation zero.");
        if (restored is not null && (world.ForTick != restored.Tick || world.Generation != restored.WorldGeneration))
            throw new ScenarioContractException("WORLD_INITIAL", "Restored world does not match the snapshot boundary.");
        _runtime = layer.Create(new(scenario.Definition, options, FreezeWorld(world)));
        _state = restored ?? new(scenario.Identity, options.SessionId, options.Seed, options.Controllers,
            SessionPhase.Ready, 0, world.Generation, 0, 0, 0,
            scenario.Definition.Objectives.ToImmutableDictionary(o => o.Id, _ => ObjectiveStatus.Locked, StringComparer.Ordinal),
            ImmutableHashSet<string>.Empty, null, null, Freeze(_runtime.InitialState), [],
            ImmutableDictionary<string, CommandReceipt>.Empty, ImmutableDictionary<string, RandomStreamState>.Empty);
        _state = _state with { Controllers = _state.Controllers.WithComparer(StringComparer.Ordinal) };
        RequireValid(_runtime.ValidateState(_state.Layer), "LAYER_STATE");
        if (restored is null)
        {
            var worldError = ValidateWorld(world, 0, world.Generation, 0);
            if (worldError is not null) throw new ScenarioContractException(worldError, "Invalid initial world.");
            _state = _state with { LastObservationSequence = world.Observations.IsEmpty ? 0 : world.Observations[^1].Sequence };
        }
        ValidateSnapshot(_state);
        if (!_state.Objectives.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(scenario.Definition.Objectives.Select(o => o.Id)) ||
            (_state.EndingId is not null && !scenario.Definition.Endings.Any(e => e.Id == _state.EndingId)))
            throw new ScenarioContractException("SNAPSHOT_SCENARIO", "Snapshot graph does not match scenario.");
        if (_state.PendingCommands.Length > 128 || _state.PendingCommands.Any(c =>
                c.Command.TargetTick <= _state.Tick || c.Command.TargetTick > _state.Tick + 8 ||
                c.Command.WorldGeneration != _state.WorldGeneration ||
                !_state.Receipts.TryGetValue(c.Command.CommandId, out var receipt) ||
                receipt.Status != CommandStatus.Queued || receipt.Sequence != c.Sequence))
            throw new ScenarioContractException("SNAPSHOT_COMMANDS", "Invalid restored queue.");
        foreach (var pending in _state.PendingCommands)
            if (!_layer.Capabilities.Operations.TryGetValue(pending.Command.Operation, out var schema) ||
                !_validator.Validate(schema, pending.Command.Payload).IsValid)
                throw new ScenarioContractException("SNAPSHOT_COMMANDS", "Restored command is not supported by the selected layer.");
        ProjectFacts(_state.Layer, _state.Tick, _state.Objectives);
    }

    public SessionSnapshot Snapshot => _state;

    public AdmissionResult Enqueue(CommandEnvelope command)
    {
        if (!Monitor.TryEnter(_gate)) return new(false, "ENGINE_BUSY", null);
        try
        {
            if (_stepping) return new(false, "ENGINE_BUSY", null);
            if (_state.Phase is not (SessionPhase.Running or SessionPhase.Paused or SessionPhase.Ready))
                return Denied("COMMAND_PHASE");
            if (string.IsNullOrWhiteSpace(command.CommandId) || command.CommandId.Length > 128 ||
                !_state.Controllers.Contains(command.ControllerId))
                return Denied("COMMAND_DENIED");
            if (string.IsNullOrWhiteSpace(command.Operation) || command.Operation.Length > 128 ||
                !WireJsonRules.Validate(command.Payload, requireObject: true).IsValid)
                return Denied("COMMAND_INVALID");
            string fingerprint;
            try { fingerprint = ContractJson.Hash(command); }
            catch (JsonException exception)
            {
                _reporter.Report(new("COMMAND_INVALID", "Invalid command payload.", Severity.Warning, _state.SessionId, exception));
                return Denied("COMMAND_INVALID");
            }
            if (_state.Receipts.TryGetValue(command.CommandId, out var existing))
                return existing.Fingerprint == fingerprint
                    ? new(true, existing.Code, existing.Sequence)
                    : Denied("COMMAND_ID_CONFLICT");
            if (command.WorldGeneration != _state.WorldGeneration) return Denied("WORLD_GENERATION");
            if (command.TargetTick <= _state.Tick) return Denied("COMMAND_LATE");
            if (command.TargetTick > _state.Tick + 8) return Denied("COMMAND_TOO_EARLY");
            if (_state.Receipts.Count >= 100000) return Denied("COMMAND_SESSION_LIMIT");
            if (_state.PendingCommands.Length >= 128 ||
                _state.PendingCommands.Count(c => c.Command.TargetTick == command.TargetTick) >= 32)
                return Denied("COMMAND_QUEUE_FULL");
            if (!_layer.Capabilities.Operations.TryGetValue(command.Operation, out var schema))
                return Denied("COMMAND_UNSUPPORTED");
            if (!_validator.Validate(schema, command.Payload).IsValid) return Denied("COMMAND_INVALID");
            var sequence = checked(_state.LastIngressSequence + 1);
            var frozen = command with { Payload = command.Payload.Clone() };
            var receipt = new CommandReceipt(command.CommandId, fingerprint, command.ControllerId, sequence, CommandStatus.Queued, "QUEUED");
            _state = _state with
            {
                LastIngressSequence = sequence,
                PendingCommands = _state.PendingCommands.Add(new(sequence, frozen)),
                Receipts = _state.Receipts.Add(command.CommandId, receipt)
            };
            return new(true, "QUEUED", sequence);
        }
        finally { Monitor.Exit(_gate); }
    }

    public AdmissionResult CancelQueued(string controllerId, string commandId)
    {
        if (!Monitor.TryEnter(_gate)) return new(false, "ENGINE_BUSY", null);
        try
        {
            if (_stepping) return new(false, "ENGINE_BUSY", null);
            if (_state.Phase is not (SessionPhase.Running or SessionPhase.Paused or SessionPhase.Ready)) return Denied("COMMAND_PHASE");
            if (!_state.Receipts.TryGetValue(commandId, out var receipt) || receipt.ControllerId != controllerId)
                return Denied("COMMAND_DENIED");
            if (receipt.Status == CommandStatus.Cancelled) return new(true, "CANCELLED", receipt.Sequence);
            if (receipt.Status != CommandStatus.Queued) return new(false, "COMMAND_ALREADY_RESOLVED", receipt.Sequence);
            _state = _state with
            {
                PendingCommands = [.. _state.PendingCommands.Where(c => c.Command.CommandId != commandId)],
                Receipts = _state.Receipts.SetItem(commandId, receipt with { Status = CommandStatus.Cancelled, Code = "CANCELLED" })
            };
            return new(true, "CANCELLED", receipt.Sequence);
        }
        finally { Monitor.Exit(_gate); }
    }

    private AdmissionResult Denied(string code)
    {
        _reporter.Report(new(code, "Command refused.", Severity.Warning, _state.SessionId));
        return new(false, code, null);
    }

    public void Start() => ChangePhase(SessionPhase.Ready, SessionPhase.Running);
    public void Pause() => ChangePhase(SessionPhase.Running, SessionPhase.Paused);
    public void Resume() => ChangePhase(SessionPhase.Paused, SessionPhase.Running);
    private void ChangePhase(SessionPhase expected, SessionPhase next)
    {
        if (!Monitor.TryEnter(_gate)) throw new ScenarioContractException("ENGINE_BUSY", "Concurrent lifecycle change.");
        try
        {
            if (_stepping) throw new ScenarioContractException("ENGINE_BUSY", "Reentrant lifecycle change.");
            if (_state.Phase != expected) throw new ScenarioContractException("SESSION_PHASE", "Illegal lifecycle transition.");
            _state = _state with { Phase = next };
            _reporter.Report(new("SESSION_" + next.ToString().ToUpperInvariant(), "Session phase changed.", Severity.Information, _state.SessionId));
        }
        finally { Monitor.Exit(_gate); }
    }

    public StepResult Step(TickInput input)
    {
        if (!Monitor.TryEnter(_gate)) return NotCommitted("ENGINE_BUSY");
        try
        {
            if (_stepping) return NotCommitted("ENGINE_BUSY");
            if (_state.Phase is not (SessionPhase.Running or SessionPhase.Transitioning)) return NotCommitted("SESSION_PHASE");
            _stepping = true;
            try { return Advance(input); }
            catch (Exception exception)
            {
                var code = exception is ScenarioContractException contract ? contract.Code : "ENGINE_FAULT";
                _state = _state with { Phase = SessionPhase.Faulted };
                _reporter.Report(new(code, "Tick discarded; the last committed state is retained.", Severity.Error, _state.SessionId, exception));
                return NotCommitted(code);
            }
            finally { _stepping = false; }
        }
        finally { Monitor.Exit(_gate); }
    }

    private StepResult Advance(TickInput input)
    {
        var before = _state;
        var generation = before.Phase == SessionPhase.Transitioning ? checked(before.WorldGeneration + 1) : before.WorldGeneration;
        if (input is null || input.World is null) return NotCommitted("WORLD_FRAME_INVALID");
        if (input.Tick != before.Tick + 1 || input.World.ForTick != input.Tick) return NotCommitted("WORLD_FRAME_GAP");
        var worldError = ValidateWorld(input.World, input.Tick, generation, before.LastObservationSequence);
        if (worldError is not null) return NotCommitted(worldError);
        var world = FreezeWorld(input.World);
        var commands = before.PendingCommands.Where(c => c.Command.TargetTick == input.Tick).OrderBy(c => c.Sequence).ToImmutableArray();
        var random = new PcgRandomStreams(before.Seed, before.RandomStreams);
        var context = new LayerTickContext(input.Tick, TimeSpan.FromMilliseconds(50), world, commands, random);
        var layerResult = _runtime.Resolve(Freeze(before.Layer), context);
        var candidate = Freeze(layerResult.State);
        RequireValid(_runtime.ValidateState(candidate), "INVARIANT_VIOLATION");
        ValidateOutcomes(commands, layerResult.Outcomes);
        var domainEvents = layerResult.Events.ToBuilder();
        var objectives = before.Objectives.ToBuilder();
        var applied = before.AppliedEffects.ToBuilder();
        foreach (var objective in _objectiveOrder)
        {
            var status = objectives[objective.Id];
            var facts = ProjectFacts(candidate, input.Tick, objectives);
            if (status == ObjectiveStatus.Locked &&
                objective.Dependencies.All(d => d.AcceptedStatuses.Contains(objectives[d.ObjectiveId])) &&
                ConditionLogic.Evaluate(objective.ActivateWhen, facts))
            {
                status = ObjectiveStatus.Active;
                objectives[objective.Id] = status;
                domainEvents.Add(new("core.objective_activated", ContractJson.Element(new { objectiveId = objective.Id })));
                facts = ProjectFacts(candidate, input.Tick, objectives);
            }
            if (status != ObjectiveStatus.Active) continue;
            ObjectiveStatus? terminal = ConditionLogic.Evaluate(objective.FailWhen, facts) ? ObjectiveStatus.Failed
                : ConditionLogic.Evaluate(objective.CompleteWhen, facts) ? ObjectiveStatus.Completed : null;
            if (terminal is null) continue;
            objectives[objective.Id] = terminal.Value;
            domainEvents.Add(new("core.objective_changed", ContractJson.Element(new { objectiveId = objective.Id, status = terminal.Value.ToString() })));
            var effects = terminal == ObjectiveStatus.Completed ? objective.SuccessEffects : objective.FailureEffects;
            var pendingEffects = effects.Select(e => new EffectEnvelope(e.Type, objective.Id + ":" + terminal + ":" + e.Key, e.Arguments.Clone()))
                .Where(e => !applied.Contains(e.TransitionKey)).ToImmutableArray();
            if (!pendingEffects.IsEmpty)
            {
                var effectResult = _runtime.ApplyEffects(candidate, pendingEffects, context);
                candidate = Freeze(effectResult.State);
                RequireValid(_runtime.ValidateState(candidate), "INVARIANT_VIOLATION");
                domainEvents.AddRange(effectResult.Events);
                foreach (var effect in pendingEffects) applied.Add(effect.TransitionKey);
            }
        }
        var finalFacts = ProjectFacts(candidate, input.Tick, objectives);
        string? endingId = before.EndingId;
        string? outcomeId = before.OutcomeId;
        var phase = layerResult.WorldRequests.Any(r => r.Kind == "core.change_zone") ? SessionPhase.Transitioning : SessionPhase.Running;
        if (ConditionLogic.Evaluate(_scenario.Definition.ClosingCondition, finalFacts))
        {
            var matching = _scenario.Definition.Endings.Where(e => ConditionLogic.Evaluate(e.Condition, finalFacts))
                .OrderByDescending(e => e.Priority).ThenBy(e => e.Id, StringComparer.Ordinal).ToArray();
            if (matching.Length == 0) throw new ScenarioContractException("ENDING_UNCOVERED", "Closing state has no ending.");
            if (matching.Length > 1 && matching[0].Priority == matching[1].Priority)
                throw new ScenarioContractException("ENDING_AMBIGUOUS", "Top ending priority is ambiguous.");
            endingId = matching[0].Id;
            outcomeId = ContractJson.Hash(new { before.SessionId, _scenario.Identity, endingId });
            phase = SessionPhase.Ended;
            domainEvents.Add(new("core.ending_reached", ContractJson.Element(new { endingId, outcomeId })));
        }
        var receipts = before.Receipts.ToBuilder();
        foreach (var result in layerResult.Outcomes)
            receipts[result.CommandId] = receipts[result.CommandId] with { Status = result.Applied ? CommandStatus.Applied : CommandStatus.Rejected, Code = result.Code };
        var pending = before.PendingCommands.Where(c => c.Command.TargetTick != input.Tick).ToImmutableArray();
        if (phase is SessionPhase.Ended or SessionPhase.Transitioning)
        {
            foreach (var command in pending)
                receipts[command.Command.CommandId] = receipts[command.Command.CommandId] with { Status = CommandStatus.Rejected, Code = "WORLD_CLOSED" };
            pending = [];
        }
        long sequence = before.LastEventSequence;
        var events = domainEvents.Select(e => new EventEnvelope(checked(++sequence), input.Tick, e.Kind, e.CommandId, e.Entity, e.Payload.Clone())).ToImmutableArray();
        var next = before with
        {
            Tick = input.Tick, Phase = phase, WorldGeneration = generation,
            LastObservationSequence = world.Observations.IsEmpty ? before.LastObservationSequence : world.Observations[^1].Sequence,
            LastEventSequence = sequence, Objectives = objectives.ToImmutable(), AppliedEffects = applied.ToImmutable(),
            Layer = candidate, PendingCommands = pending, Receipts = receipts.ToImmutable(),
            RandomStreams = random.Capture(), EndingId = endingId, OutcomeId = outcomeId
        };
        ValidateSnapshot(next);
        var hash = StateHash(next);
        // Freeze plugin outputs before the commit boundary: a bad payload must not commit half a tick.
        if (layerResult.WorldRequests.Length > 256 ||
            layerResult.WorldRequests.Select(r => r.RequestId).Distinct(StringComparer.Ordinal).Count() != layerResult.WorldRequests.Length)
            throw new ScenarioContractException("WORLD_REQUEST_CONTRACT", "Invalid world request batch.");
        var requests = layerResult.WorldRequests.Select(r => r with { Payload = r.Payload.Clone() }).ToImmutableArray();
        if (phase == SessionPhase.Ended)
            _reporter.Audit(new("ScenarioEnded", outcomeId!, "committed", before.SessionId));
        _state = next;
        return new(true, "COMMITTED", next.Tick, events, requests, next, hash);
    }

    private string? ValidateWorld(WorldFrame world, long tick, int generation, long lastSequence)
    {
        var shapeError = WorldFrameRules.ValidateShape(world);
        if (shapeError is not null) return shapeError;
        if (world.ForTick != tick) return "WORLD_FRAME_GAP";
        if (world.Generation != generation) return "WORLD_GENERATION";
        foreach (var observation in world.Observations)
        {
            if (observation.Sequence != ++lastSequence) return "WORLD_FRAME_GAP";
            if (!_layer.Capabilities.Observations.TryGetValue(observation.Kind, out var schema) ||
                !_validator.Validate(schema, observation.Payload).IsValid) return "WORLD_OBSERVATION_INVALID";
        }
        return null;
    }

    private ImmutableDictionary<string, FactValue> ProjectFacts(LayerSnapshot state, long tick,
        IReadOnlyDictionary<string, ObjectiveStatus> objectives)
    {
        var facts = _runtime.ProjectFacts(state).ToBuilder();
        if (facts.Keys.Any(k => !_layer.Capabilities.Facts.ContainsKey(k)) ||
            _layer.Capabilities.Facts.Keys.Any(k => !facts.ContainsKey(k)))
            throw new ScenarioContractException("CONDITION_CONTRACT", "Fact projection differs from registered capabilities.");
        foreach (var pair in facts) ConditionLogic.ValidateValue(pair.Key, pair.Value, _layer.Capabilities.Facts[pair.Key]);
        facts.Add("core.tick", FactValue.From(tick));
        foreach (var pair in objectives) facts.Add("core.objective." + pair.Key + ".status", FactValue.From(pair.Value.ToString()));
        return facts.ToImmutable();
    }

    private static void ValidateOutcomes(ImmutableArray<QueuedCommand> commands, ImmutableArray<CommandOutcome> outcomes)
    {
        if (outcomes.Length != commands.Length || outcomes.Select(o => o.CommandId).Distinct(StringComparer.Ordinal).Count() != outcomes.Length ||
            outcomes.Any(o => !commands.Any(c => c.Command.CommandId == o.CommandId)))
            throw new ScenarioContractException("COMMAND_OUTCOME_CONTRACT", "Every admitted command must have exactly one outcome.");
    }

    private static void ValidateSnapshot(SessionSnapshot state)
    {
        SessionSnapshotRules.Validate(state);
    }

    private static void RequireValid(ValidationReport report, string code)
    {
        if (!report.IsValid) throw new ScenarioContractException(code, string.Join("; ", report.Diagnostics.Select(d => d.Code + ": " + d.Message)));
    }
    private static LayerSnapshot Freeze(LayerSnapshot state) => new(state.Data.Clone());
    private static WorldFrame FreezeWorld(WorldFrame world) => world with
    {
        Observations = [.. world.Observations.Select(o => o with { Payload = o.Payload.Clone() })]
    };
    public static string StateHash(SessionSnapshot snapshot)
    {
        return ContractJson.StateHash(snapshot);
    }
    private StepResult NotCommitted(string code) => new(false, code, _state.Tick, [], [], _state, StateHash(_state));
    public void Dispose()
    {
        lock (_gate)
        {
            if (_stepping) throw new ScenarioContractException("ENGINE_BUSY", "Cannot dispose during Step.");
            _state = _state with { Phase = SessionPhase.Disposed };
        }
    }
}
