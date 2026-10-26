using System.Text.Json;
using Counter.GameLayer;
using Microsoft.Extensions.Logging.Abstractions;
using ScenarioEngine.Contracts;
using ScenarioEngine.Diagnostics;

namespace ScenarioEngine.Tests;

public sealed class OperationalTests
{
    [Fact]
    public void MotherClassCanRestoreAReadySnapshotAndUpdate()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        using var ready = h.Factory.Create(scenario, Harness.Options(), Harness.Frame(0));
        using IGame restored = new CounterGame(h.Factory);
        restored.Restore(scenario, ready.Snapshot, Harness.Frame(0));
        restored.Submit(Harness.Increment());
        Assert.True(restored.Update(Harness.Input(1)).Committed);
    }

    [Fact]
    public void BadWorldOutputCannotCommitHalfATick()
    {
        var h = new Harness(new DecoratingLayer((result, _) => result with
        {
            WorldRequests = [new("bad", 0, "example", default(JsonElement))]
        }));
        using var sim = h.Start();
        sim.Enqueue(Harness.Increment());
        var step = sim.Step(Harness.Input(1));
        Assert.False(step.Committed);
        Assert.Equal(0, sim.Snapshot.Tick);
        Assert.Equal(0, Harness.Value(sim.Snapshot));
        Assert.Empty(step.Events);
    }

    [Fact]
    public void EveryGameCanRestoreThroughTheMotherClassContract()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        using var first = new CounterGame(h.Factory);
        first.Init(new(scenario, Harness.Options(), Harness.Frame(0)));
        first.Submit(Harness.Increment());
        first.Update(Harness.Input(1));
        first.Pause();
        using IGame restored = new CounterGame(h.Factory);
        restored.Restore(scenario, first.Snapshot, Harness.Frame(1));
        Assert.Equal(SessionPhase.Paused, restored.Snapshot.Phase);
        restored.Resume();
        restored.Submit(Harness.Increment(2, "two"));
        Assert.True(restored.Update(Harness.Input(2)).Committed);
        Assert.Equal(2, Harness.Value(restored.Snapshot));
    }

    [Fact]
    public void UiChannelFailureIsReportedAsDegradedWithoutRecursion()
    {
        var path = Path.Combine(Path.GetTempPath(), "ScenarioEngineTests", Guid.NewGuid().ToString("N"), "audit.jsonl");
        var sink = new BrokenSink();
        using var diagnostics = new ApplicationDiagnostics(NullLogger.Instance, path, sink);
        diagnostics.Report(new("EXAMPLE", "Safe message.", Severity.Error, Exception: new InvalidOperationException("Private stack")));
        Assert.True(diagnostics.IsDegraded);
        Assert.Equal(1, sink.Calls);
        Assert.Null(sink.Last!.Exception);
        diagnostics.Audit(new("ExampleAction", "target", "denied"));
        using var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        Assert.Contains("denied", reader.ReadToEnd());
    }

    private sealed class BrokenSink : INotificationSink
    {
        public int Calls { get; private set; }
        public EngineNotice? Last { get; private set; }
        public void Publish(EngineNotice notice)
        {
            Last = notice; Calls++;
            throw new IOException("Injected notification outage.");
        }
    }
}
