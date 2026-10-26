using System.Collections.Immutable;
using System.Text.Json;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Replay;

/// <summary>One owner for bounded observation-journal replay and its portable format.</summary>
public sealed partial class ReplayService(IScenarioSessionFactory sessions, IEngineReporter reporter,
    int maxRecordingEntries = ReplayService.MaxEntries) : IReplayService, IReplayCodec
{
    public const int MaxEntries = 10000;
    public const int MaxBytes = 16 * 1024 * 1024;
    public IReplayRecorder Record(IScenarioSimulation simulation, WorldFrame initialWorld)
    {
        if (maxRecordingEntries is < 2 or > MaxEntries)
            throw new ArgumentOutOfRangeException(nameof(maxRecordingEntries));
        var baseline = simulation.Snapshot;
        SessionSnapshotRules.Validate(baseline);
        ValidateBaseline(baseline, initialWorld);
        return new Recording(simulation, initialWorld, maxRecordingEntries);
    }

    public ReplayResult Verify(ValidatedScenario scenario, ReplayTape tape)
    {
        try
        {
            ValidateTape(tape);
            if (tape.InitialSnapshot.Identity != scenario.Identity)
                return Failed("REPLAY_CONTENT_MISMATCH", null, null, null);
            using var simulation = sessions.Restore(scenario, tape.InitialSnapshot, tape.InitialWorld);
            for (var i = 0; i < tape.Entries.Length; i++)
            {
                var entry = tape.Entries[i];
                object output = entry.Operation switch
                {
                    ReplayOperation.Submit => simulation.Enqueue(entry.Command!),
                    ReplayOperation.CancelQueued => simulation.CancelQueued(entry.ControllerId!, entry.CommandId!),
                    ReplayOperation.Step => simulation.Step(entry.Input!),
                    _ => ApplyLifecycle(simulation, entry.Operation)
                };
                var hash = ContractJson.StateHash(simulation.Snapshot);
                if (OutputCode(output) != entry.ExpectedCode || hash != entry.ExpectedStateHash || OutputHash(output) != entry.ExpectedOutputHash)
                    return Failed("REPLAY_DIVERGED", i, simulation.Snapshot, hash);
            }
            var final = simulation.Snapshot;
            return new(true, "REPLAY_MATCHED", null, final, ContractJson.StateHash(final));
        }
        catch (Exception exception)
        {
            var code = exception is ScenarioContractException contract ? contract.Code : "REPLAY_FAILED";
            reporter.Report(new(code, "Replay verification refused or failed.", Severity.Error, Exception: exception));
            return new(false, code, null, null, null);
        }
    }

    private ReplayResult Failed(string code, int? index, SessionSnapshot? state, string? hash)
    {
        reporter.Report(new(code, "Replay input or recorded result does not match.", Severity.Warning, state?.SessionId));
        return new(false, code, index, state, hash);
    }
    private static string OutputCode(object output) => output switch
    {
        StepResult step => step.Code,
        AdmissionResult admission => admission.Code,
        string code => code,
        _ => throw new ScenarioContractException("REPLAY_OPERATION", "Unsupported operation result.")
    };
    private static string OutputHash(object output) => output switch
    {
        StepResult step => ContractJson.HashJson(ContractJson.Element(new { step.Committed, step.Code, step.Tick, step.Events, step.WorldRequests })),
        _ => ContractJson.HashJson(ContractJson.Element(output))
    };

    private static string ApplyLifecycle(IScenarioSimulation simulation, ReplayOperation operation)
    {
        switch (operation)
        {
            case ReplayOperation.Start: simulation.Start(); break;
            case ReplayOperation.Pause: simulation.Pause(); break;
            case ReplayOperation.Resume: simulation.Resume(); break;
            case ReplayOperation.Dispose: simulation.Dispose(); break;
            default: throw new ScenarioContractException("REPLAY_OPERATION", "Unsupported replay operation.");
        }
        return "OK";
    }
    public byte[] Encode(ReplayTape tape)
    {
        ValidateTape(tape);
        var payload = ContractJson.Element(tape);
        var bytes = ContractJson.Canonical(ContractJson.Element(new
        {
            replayFormat = 1, hashAlgorithm = "canonical-session-sha256-v1",
            randomAlgorithm = "pcg32-xsh-rr-v1", payload, payloadDigest = ContractJson.HashJson(payload)
        }), integerOnly: false);
        if (bytes.Length > MaxBytes) throw new ScenarioContractException("REPLAY_SIZE", "Replay exceeds 16 MiB.");
        return bytes;
    }
    public ReplayDecodeResult Decode(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length > MaxBytes) throw new ScenarioContractException("REPLAY_SIZE", "Replay exceeds 16 MiB.");
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 64 });
            var root = document.RootElement;
            ContractJson.RejectDuplicateProperties(root);
            if (root.EnumerateObject().Count() != 5 || root.GetProperty("replayFormat").GetInt32() != 1 ||
                root.GetProperty("hashAlgorithm").GetString() != "canonical-session-sha256-v1" ||
                root.GetProperty("randomAlgorithm").GetString() != "pcg32-xsh-rr-v1")
                throw new ScenarioContractException("REPLAY_VERSION", "Unsupported replay format.");
            var payload = root.GetProperty("payload");
            if (ContractJson.HashJson(payload) != root.GetProperty("payloadDigest").GetString())
                throw new ScenarioContractException("REPLAY_CORRUPT", "Replay digest mismatch.");
            var tape = payload.Deserialize<ReplayTape>(ContractJson.Options);
            ValidateTape(tape!);
            return new(tape, ValidationReport.Valid);
        }
        catch (Exception exception) when (exception is JsonException or ScenarioContractException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            var code = exception is ScenarioContractException contract ? contract.Code : "REPLAY_CORRUPT";
            return new(null, new([new(code, "Invalid replay: " + exception.Message)]));
        }
    }
    private static void ValidateBaseline(SessionSnapshot state, WorldFrame world)
    {
        if (state.Phase is SessionPhase.Faulted or SessionPhase.Disposed or SessionPhase.Transitioning ||
            world is null || world.Observations.IsDefault || world.Observations.Length > 256 ||
            world.ForTick != state.Tick || world.Generation != state.WorldGeneration)
            throw new ScenarioContractException("REPLAY_BASELINE", "Recording must begin at a stable matching world boundary.");
    }
    private static void ValidateTape(ReplayTape tape)
    {
        if (tape is null || tape.Format != 1 || tape.Entries.IsDefault || tape.Entries.Length > MaxEntries)
            throw new ScenarioContractException("REPLAY_VERSION", "Invalid replay format or entry count.");
        SessionSnapshotRules.Validate(tape.InitialSnapshot);
        ValidateBaseline(tape.InitialSnapshot, tape.InitialWorld);
        var disposed = false;
        foreach (var entry in tape.Entries)
        {
            if (entry is null || !Enum.IsDefined(entry.Operation) || disposed ||
                string.IsNullOrWhiteSpace(entry.ExpectedCode) || entry.ExpectedCode.Length > 128 ||
                entry.ExpectedStateHash is null || entry.ExpectedStateHash.Length != 64 ||
                entry.ExpectedStateHash.Any(c => !Uri.IsHexDigit(c)) ||
                entry.ExpectedOutputHash is null || entry.ExpectedOutputHash.Length != 64 ||
                entry.ExpectedOutputHash.Any(c => !Uri.IsHexDigit(c)))
                throw new ScenarioContractException("REPLAY_OPERATION", "Invalid replay entry.");
            var valid = entry.Operation switch
            {
                ReplayOperation.Submit => entry.Command is not null && entry.Input is null && entry.ControllerId is null && entry.CommandId is null,
                ReplayOperation.Step => entry.Input is not null && entry.Input.World is not null && !entry.Input.World.Observations.IsDefault && entry.Command is null && entry.ControllerId is null && entry.CommandId is null,
                ReplayOperation.CancelQueued => !string.IsNullOrWhiteSpace(entry.ControllerId) && !string.IsNullOrWhiteSpace(entry.CommandId) && entry.Command is null && entry.Input is null,
                _ => entry.Command is null && entry.Input is null && entry.ControllerId is null && entry.CommandId is null
            };
            if (!valid) throw new ScenarioContractException("REPLAY_OPERATION", "Entry payload differs from operation.");
            disposed = entry.Operation == ReplayOperation.Dispose;
        }
    }

    private sealed class Recording : IReplayRecorder
    {
        private readonly IScenarioSimulation _simulation;
        private readonly SessionSnapshot _initial;
        private readonly WorldFrame _initialWorld;
        private readonly List<ReplayEntry> _entries = [];
        private readonly object _gate = new();
        private bool _inside;
        private bool _disposed;
        private int _bytes;
        private readonly int _maxEntries;
        public Recording(IScenarioSimulation simulation, WorldFrame initialWorld, int maxEntries)
        {
            _maxEntries = maxEntries;
            _simulation = simulation;
            _initial = Clone(simulation.Snapshot);
            _initialWorld = Clone(initialWorld);
            _bytes = ContractJson.Canonical(ContractJson.Element(new { _initial, _initialWorld })).Length;
            if (_bytes > MaxBytes / 2) throw new ScenarioContractException("REPLAY_SIZE", "Initial recording state exceeds budget.");
        }
        private static T Clone<T>(T value) => ContractJson.Element(value).Deserialize<T>(ContractJson.Options)!;
        public SessionSnapshot Snapshot => _simulation.Snapshot;
        public ReplayTape CaptureReplay()
        {
            lock (_gate)
            {
                if (_inside) throw new ScenarioContractException("REPLAY_BUSY", "Cannot capture during a replay operation.");
                return new(1, _initial, _initialWorld, [.. _entries]);
            }
        }
        private T Record<T>(ReplayEntry template, Func<ReplayEntry, T> operation, Func<T, string> resultCode)
        {
            lock (_gate)
            {
                if (_inside) throw new ScenarioContractException("REPLAY_BUSY", "Reentrant recorder call.");
                ObjectDisposedException.ThrowIf(_disposed, this);
                var frozen = Clone(template);
                var size = JsonSerializer.SerializeToUtf8Bytes(frozen, ContractJson.Options).Length + 320;
                var entryLimit = template.Operation == ReplayOperation.Dispose ? _maxEntries : _maxEntries - 1;
                var byteLimit = template.Operation == ReplayOperation.Dispose ? MaxBytes - 1024 : MaxBytes - 2048;
                if (_entries.Count >= entryLimit || size > 1024 * 1024 || _bytes + size > byteLimit)
                    throw new ScenarioContractException("REPLAY_SIZE", "Recording budget reached before executing operation.");
                _inside = true;
                try
                {
                    var result = operation(frozen);
                    _entries.Add(frozen with { ExpectedCode = resultCode(result), ExpectedStateHash = ContractJson.StateHash(_simulation.Snapshot), ExpectedOutputHash = OutputHash(result!) });
                    _bytes += size;
                    return result;
                }
                finally { _inside = false; }
            }
        }
        private void Lifecycle(ReplayOperation operation) => Record(new(operation, "", "", ""), _ => ApplyLifecycle(_simulation, operation), code => code);
        public void Start() => Lifecycle(ReplayOperation.Start);
        public void Pause() => Lifecycle(ReplayOperation.Pause);
        public void Resume() => Lifecycle(ReplayOperation.Resume);
        public AdmissionResult Enqueue(CommandEnvelope command) =>
            Record(new(ReplayOperation.Submit, "", "", "", Command: command), e => _simulation.Enqueue(e.Command!), result => result.Code);
        public AdmissionResult CancelQueued(string controllerId, string commandId) =>
            Record(new(ReplayOperation.CancelQueued, "", "", "", ControllerId: controllerId, CommandId: commandId),
                e => _simulation.CancelQueued(e.ControllerId!, e.CommandId!), result => result.Code);
        public StepResult Step(TickInput input) =>
            Record(new(ReplayOperation.Step, "", "", "", Input: input), e => _simulation.Step(e.Input!), result => result.Code);
        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                Lifecycle(ReplayOperation.Dispose);
                _disposed = true;
            }
        }
    }
}
