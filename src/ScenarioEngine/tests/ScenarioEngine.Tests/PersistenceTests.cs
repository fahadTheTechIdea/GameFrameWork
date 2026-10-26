using System.Text.Json.Nodes;
using ScenarioEngine.Contracts;
using ScenarioEngine.Core;
using ScenarioEngine.Persistence;

namespace ScenarioEngine.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task SaveRestoreContinuesWithSameRuleStateHashes()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        using var original = h.Start(scenario);
        original.Enqueue(Harness.Increment());
        original.Step(Harness.Input(1));
        var store = new MemoryStore();
        var service = new ScenarioSaveService(new ScenarioSaveCodec(), store, h.Reporter);
        Assert.True((await service.SaveAsync("slot", original.Snapshot, TestContext.Current.CancellationToken)).Committed);
        var loaded = await service.LoadAsync("slot", TestContext.Current.CancellationToken);
        Assert.NotNull(loaded.Save);
        using var restored = h.Factory.Restore(scenario, loaded.Save.Snapshot, Harness.Frame(1));
        for (var tick = 2; tick <= 3; tick++)
        {
            original.Enqueue(Harness.Increment(tick, "next-" + tick));
            restored.Enqueue(Harness.Increment(tick, "next-" + tick));
            Assert.Equal(original.Step(Harness.Input(tick)).RuleStateHash, restored.Step(Harness.Input(tick)).RuleStateHash);
        }
    }

    [Fact]
    public void SaveChecksumDetectsModifiedState()
    {
        using var sim = new Harness().Start();
        var codec = new ScenarioSaveCodec();
        var bytes = codec.Encode(new(1, ScenarioSaveCodec.BuildId, DateTimeOffset.UtcNow, sim.Snapshot));
        var root = JsonNode.Parse(bytes)!;
        root["payload"]!["snapshot"]!["tick"] = 999;
        var corrupted = System.Text.Encoding.UTF8.GetBytes(root.ToJsonString());
        Assert.Contains(codec.Decode(corrupted).Report.Diagnostics, d => d.Code == "SAVE_CORRUPT");
    }

    [Fact]
    public void WrongContentIdentityIsRefusedOnRestore()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        using var sim = h.Start(scenario);
        var incompatible = scenario with { Identity = scenario.Identity with { ContentDigest = "other" } };
        Assert.Equal("SAVE_CONTENT_MISMATCH",
            Assert.Throws<ScenarioContractException>(() => h.Factory.Restore(incompatible, sim.Snapshot, Harness.Frame(0))).Code);
    }

    [Fact]
    public async Task StorageAndAuditFailureNeverReturnCommitted()
    {
        var h = new Harness();
        using var sim = h.Start();
        var failing = new MemoryStore { FailWrite = true };
        var service = new ScenarioSaveService(new ScenarioSaveCodec(), failing, h.Reporter);
        Assert.False((await service.SaveAsync("slot", sim.Snapshot, TestContext.Current.CancellationToken)).Committed);
        Assert.Contains(h.Reporter.Notices, n => n.Code == "SAVE_FAILED" && n.Exception is IOException);
        h.Reporter.FailAudit = true;
        var untouched = new MemoryStore();
        service = new(new ScenarioSaveCodec(), untouched, h.Reporter);
        Assert.False((await service.SaveAsync("slot", sim.Snapshot, TestContext.Current.CancellationToken)).Committed);
        Assert.Equal(0, untouched.Writes);
    }

    [Fact]
    public async Task CancellationAndMissingSlotsAreExplicit()
    {
        var h = new Harness();
        using var sim = h.Start();
        var store = new MemoryStore();
        var service = new ScenarioSaveService(new ScenarioSaveCodec(), store, h.Reporter);
        Assert.Equal("SAVE_CANCELLED", (await service.SaveAsync("slot", sim.Snapshot, new CancellationToken(true))).Code);
        Assert.Equal(0, store.Writes);
        Assert.Contains((await service.LoadAsync("missing", TestContext.Current.CancellationToken)).Report.Diagnostics, d => d.Code == "SAVE_MISSING");
    }

    [Fact]
    public async Task ActualFileStorePreservesPreviousGenerationAndRejectsUnsafeSlot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ScenarioEngineTests", Guid.NewGuid().ToString("N"));
        var h = new Harness();
        using var sim = h.Start();
        var codec = new ScenarioSaveCodec();
        var service = new ScenarioSaveService(codec, new LocalFileSaveStore(root), h.Reporter);
        Assert.True((await service.SaveAsync("slot", sim.Snapshot, TestContext.Current.CancellationToken)).Committed);
        sim.Enqueue(Harness.Increment());
        sim.Step(Harness.Input(1));
        Assert.True((await service.SaveAsync("slot", sim.Snapshot, TestContext.Current.CancellationToken)).Committed);
        Assert.Equal(0, codec.Decode(await File.ReadAllBytesAsync(Path.Combine(root, "slot.json.previous"), TestContext.Current.CancellationToken)).Save!.Snapshot.Tick);
        Assert.Equal(1, (await service.LoadAsync("slot", TestContext.Current.CancellationToken)).Save!.Snapshot.Tick);
        Assert.False((await service.SaveAsync("../escape", sim.Snapshot, TestContext.Current.CancellationToken)).Committed);
        // Leave the isolated files available for diagnosis; no broad temp-directory cleanup.
    }

    [Fact]
    public void RandomStreamContinuationSurvivesSerialization()
    {
        using var sim = new Harness().Start();
        var streams = new PcgRandomStreams(42, sim.Snapshot.RandomStreams);
        streams.NextUInt32("example");
        var snapshot = sim.Snapshot with { RandomStreams = streams.Capture() };
        var codec = new ScenarioSaveCodec();
        var decoded = codec.Decode(codec.Encode(new(1, ScenarioSaveCodec.BuildId, DateTimeOffset.UtcNow, snapshot)));
        Assert.NotNull(decoded.Save);
        var restored = new PcgRandomStreams(42, decoded.Save.Snapshot.RandomStreams);
        Assert.Equal(streams.NextUInt32("example"), restored.NextUInt32("example"));
    }

    private sealed class MemoryStore : ISaveStore
    {
        public byte[]? Bytes { get; private set; }
        public bool FailWrite { get; init; }
        public int Writes { get; private set; }
        public Task WriteAtomicAsync(string slotId, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailWrite) throw new IOException("Injected storage outage.");
            Bytes = bytes.ToArray(); Writes++;
            return Task.CompletedTask;
        }
        public Task<ReadOnlyMemory<byte>?> ReadAsync(string slotId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Bytes is null ? (ReadOnlyMemory<byte>?)null : new ReadOnlyMemory<byte>(Bytes));
        }
    }
}
