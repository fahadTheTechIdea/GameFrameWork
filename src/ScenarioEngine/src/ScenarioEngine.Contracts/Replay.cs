using System.Collections.Immutable;

namespace ScenarioEngine.Contracts;

public enum ReplayOperation { Start, Submit, CancelQueued, Pause, Resume, Step, Dispose }
public sealed record ReplayEntry(
    ReplayOperation Operation, string ExpectedCode, string ExpectedStateHash, string ExpectedOutputHash,
    CommandEnvelope? Command = null, string? ControllerId = null,
    string? CommandId = null, TickInput? Input = null);
public sealed record ReplayTape(int Format, SessionSnapshot InitialSnapshot, WorldFrame InitialWorld,
    ImmutableArray<ReplayEntry> Entries);
public sealed record ReplayResult(bool Matches, string Code, int? MismatchIndex,
    SessionSnapshot? FinalSnapshot, string? ActualHash);
public sealed record ReplayDecodeResult(ReplayTape? Tape, ValidationReport Report);
public interface IReplayRecorder : IScenarioSimulation
{
    ReplayTape CaptureReplay();
}
public interface IReplayService
{
    IReplayRecorder Record(IScenarioSimulation simulation, WorldFrame initialWorld);
    ReplayResult Verify(ValidatedScenario scenario, ReplayTape tape);
}
public interface IReplayCodec
{
    byte[] Encode(ReplayTape tape);
    ReplayDecodeResult Decode(ReadOnlyMemory<byte> bytes);
}
