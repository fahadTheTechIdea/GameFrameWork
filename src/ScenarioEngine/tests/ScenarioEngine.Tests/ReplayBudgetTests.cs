using ScenarioEngine.Contracts;
using ScenarioEngine.Replay;

namespace ScenarioEngine.Tests;

public sealed class ReplayBudgetTests
{
    [Fact]
    public void RecordingBudgetAlwaysLeavesRoomForCleanup()
    {
        var h = new Harness();
        var service = new ReplayService(h.Factory, h.Reporter, maxRecordingEntries: 3);
        var simulation = h.Start();
        var recorder = service.Record(simulation, Harness.Frame(0));
        recorder.Enqueue(Harness.Increment());
        recorder.CancelQueued("player", "one");
        Assert.Throws<ScenarioContractException>(() => recorder.Step(Harness.Input(1)));
        recorder.Dispose();
        Assert.Equal(SessionPhase.Disposed, simulation.Snapshot.Phase);
        Assert.Equal(3, recorder.CaptureReplay().Entries.Length);
    }
}
