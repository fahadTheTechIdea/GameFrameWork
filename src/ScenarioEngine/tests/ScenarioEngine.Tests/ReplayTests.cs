using System.Text.Json;
using System.Text.Json.Nodes;
using ScenarioEngine.Contracts;
using ScenarioEngine.Replay;

namespace ScenarioEngine.Tests;

public sealed class ReplayTests
{
    [Fact]
    public void PortableReplayPreservesWorldInputsCommandsCancellationPauseAndEnding()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        var service = new ReplayService(h.Factory, h.Reporter);
        using var recording = service.Record(h.Factory.Create(scenario, Harness.Options(), Harness.Frame(0)), Harness.Frame(0));
        recording.Start();
        recording.Enqueue(Harness.Increment(id: "cancelled"));
        recording.CancelQueued("player", "cancelled");
        recording.Pause();
        recording.Resume();
        recording.Enqueue(Harness.Increment());
        var world = new WorldFrame(0, 1, [new(1, "test_counter.adjustment", null, ContractJson.Element(new { delta = 1 }))]);
        Assert.True(recording.Step(new(1, world)).Committed);
        recording.Enqueue(Harness.Increment(2, "next"));
        Assert.True(recording.Step(Harness.Input(2)).Committed);
        var decoded = service.Decode(service.Encode(recording.CaptureReplay()));
        Assert.True(decoded.Report.IsValid);
        var result = service.Verify(scenario, decoded.Tape!);
        Assert.True(result.Matches);
        Assert.Equal(recording.Snapshot.EndingId, result.FinalSnapshot!.EndingId);
        Assert.Equal(ContractJson.StateHash(recording.Snapshot), result.ActualHash);
    }

    [Fact]
    public void ChangedObservationIdentifiesFirstDivergentOperation()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        var service = new ReplayService(h.Factory, h.Reporter);
        using var recording = service.Record(h.Start(scenario), Harness.Frame(0));
        recording.Enqueue(Harness.Increment());
        var world = new WorldFrame(0, 1, [new(1, "test_counter.adjustment", null, ContractJson.Element(new { delta = 0 }))]);
        recording.Step(new(1, world));
        var tape = recording.CaptureReplay();
        var step = tape.Entries[1];
        var input = step.Input!;
        var altered = input with { World = input.World with
        {
            Observations = [input.World.Observations[0] with { Payload = ContractJson.Element(new { delta = 1 }) }]
        }};
        var result = service.Verify(scenario, tape with { Entries = tape.Entries.SetItem(1, step with { Input = altered }) });
        Assert.False(result.Matches);
        Assert.Equal("REPLAY_DIVERGED", result.Code);
        Assert.Equal(1, result.MismatchIndex);
    }

    [Fact]
    public void EventChangesAreDetectedEvenIfStateHashMatches()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        var service = new ReplayService(h.Factory, h.Reporter);
        using var recording = service.Record(h.Start(scenario), Harness.Frame(0));
        recording.Enqueue(Harness.Increment());
        recording.Step(Harness.Input(1));
        var changed = new Harness(new DecoratingLayer((result, _) => result with
        {
            Events = [.. result.Events.Select(e => e with { Kind = "test_counter.changed_event" })]
        }));
        var result = new ReplayService(changed.Factory, changed.Reporter).Verify(scenario, recording.CaptureReplay());
        Assert.False(result.Matches);
        Assert.Equal(1, result.MismatchIndex);
        Assert.Equal(ContractJson.StateHash(recording.Snapshot), result.ActualHash);
    }

    [Fact]
    public void ReplayDigestRejectsModifiedPayloadAndWrongContentIsRefused()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        var service = new ReplayService(h.Factory, h.Reporter);
        using var recording = service.Record(h.Start(scenario), Harness.Frame(0));
        var tape = recording.CaptureReplay();
        var envelope = JsonNode.Parse(service.Encode(tape))!;
        envelope["payload"]!["initialSnapshot"]!["seed"] = 999;
        var corrupted = service.Decode(JsonSerializer.SerializeToUtf8Bytes(envelope));
        Assert.Contains(corrupted.Report.Diagnostics, d => d.Code == "REPLAY_CORRUPT");
        var mismatch = service.Verify(scenario with { Identity = scenario.Identity with { ContentDigest = "different" } }, tape);
        Assert.Equal("REPLAY_CONTENT_MISMATCH", mismatch.Code);
    }

    [Fact]
    public void RecorderFreezesCallerPayloadBeforeDocumentDisposal()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        var service = new ReplayService(h.Factory, h.Reporter);
        using var recording = service.Record(h.Start(scenario), Harness.Frame(0));
        using (var json = JsonDocument.Parse("""{"amount":1}"""))
            recording.Enqueue(Harness.Increment() with { Payload = json.RootElement });
        recording.Step(Harness.Input(1));
        Assert.True(service.Verify(scenario, service.Decode(service.Encode(recording.CaptureReplay())).Tape!).Matches);
    }

    [Fact]
    public void RejectedFractionalCommandCanStillBeRecordedAndReplayed()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        var service = new ReplayService(h.Factory, h.Reporter);
        using var recording = service.Record(h.Start(scenario), Harness.Frame(0));
        using var payload = JsonDocument.Parse("""{"amount":1.5}""");
        Assert.False(recording.Enqueue(Harness.Increment() with { Payload = payload.RootElement }).Accepted);
        Assert.True(service.Verify(scenario, service.Decode(service.Encode(recording.CaptureReplay())).Tape!).Matches);
    }

    [Fact]
    public void RecordingDisposedStateCanBeExportedAndVerified()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        var service = new ReplayService(h.Factory, h.Reporter);
        var recording = service.Record(h.Start(scenario), Harness.Frame(0));
        recording.Dispose();
        var tape = recording.CaptureReplay();
        Assert.True(service.Verify(scenario, tape).Matches);
        Assert.Equal(SessionPhase.Disposed, service.Verify(scenario, tape).FinalSnapshot!.Phase);
    }

    [Fact]
    public void InvalidBaselineIsRefusedBeforeRecording()
    {
        var h = new Harness();
        var service = new ReplayService(h.Factory, h.Reporter);
        using var simulation = h.Start();
        Assert.Equal("REPLAY_BASELINE", Assert.Throws<ScenarioContractException>(() =>
            service.Record(simulation, Harness.Frame(999))).Code);
    }
}
