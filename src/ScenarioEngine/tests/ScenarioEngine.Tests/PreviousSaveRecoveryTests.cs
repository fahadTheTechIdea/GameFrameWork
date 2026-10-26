using ScenarioEngine.Contracts;
using ScenarioEngine.Persistence;

namespace ScenarioEngine.Tests;

public sealed class PreviousSaveRecoveryTests
{
    [Fact]
    public async Task RecoveryIsExplicitAndDoesNotRewriteCorruptCurrentSlot()
    {
        var h = new Harness();
        using var simulation = h.Start();
        var root = Path.Combine(Path.GetTempPath(), "ScenarioEngineTests", Guid.NewGuid().ToString("N"));
        IScenarioSaveRecoveryService service = new ScenarioSaveService(new ScenarioSaveCodec(), new LocalFileSaveStore(root), h.Reporter);
        var ct = TestContext.Current.CancellationToken;
        Assert.True((await service.SaveAsync("slot", simulation.Snapshot, ct)).Committed);
        simulation.Enqueue(Harness.Increment());
        simulation.Step(Harness.Input(1));
        Assert.True((await service.SaveAsync("slot", simulation.Snapshot, ct)).Committed);
        var corruptBytes = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(Path.Combine(root, "slot.json"), corruptBytes, ct);
        Assert.Null((await service.LoadAsync("slot", ct)).Save);
        var recovered = await service.LoadPreviousAsync("slot", ct);
        Assert.NotNull(recovered.Save);
        Assert.Equal(0, recovered.Save.Snapshot.Tick);
        Assert.Equal(corruptBytes, await File.ReadAllBytesAsync(Path.Combine(root, "slot.json"), ct));
        Assert.Contains(h.Reporter.Audits, a => a.Operation == "PreviousSaveRead" && a.Result == "validated");
    }

    [Fact]
    public async Task MissingPreviousGenerationAndUnsupportedStoreAreDistinct()
    {
        var h = new Harness();
        var root = Path.Combine(Path.GetTempPath(), "ScenarioEngineTests", Guid.NewGuid().ToString("N"));
        IScenarioSaveRecoveryService service = new ScenarioSaveService(new ScenarioSaveCodec(), new LocalFileSaveStore(root), h.Reporter);
        Assert.Contains((await service.LoadPreviousAsync("slot", TestContext.Current.CancellationToken)).Report.Diagnostics,
            d => d.Code == "SAVE_PREVIOUS_MISSING");
        service = new ScenarioSaveService(new ScenarioSaveCodec(), new BaseStoreOnly(), h.Reporter);
        Assert.Contains((await service.LoadPreviousAsync("slot", TestContext.Current.CancellationToken)).Report.Diagnostics,
            d => d.Code == "SAVE_RECOVERY_UNAVAILABLE");
    }

    [Fact]
    public async Task RecoveryAuditFailureDoesNotReleaseSnapshot()
    {
        var h = new Harness();
        using var simulation = h.Start();
        var root = Path.Combine(Path.GetTempPath(), "ScenarioEngineTests", Guid.NewGuid().ToString("N"));
        IScenarioSaveRecoveryService service = new ScenarioSaveService(new ScenarioSaveCodec(), new LocalFileSaveStore(root), h.Reporter);
        var ct = TestContext.Current.CancellationToken;
        await service.SaveAsync("slot", simulation.Snapshot, ct);
        await service.SaveAsync("slot", simulation.Snapshot, ct);
        h.Reporter.FailAudit = true;
        var result = await service.LoadPreviousAsync("slot", ct);
        Assert.Null(result.Save);
        Assert.Contains(result.Report.Diagnostics, d => d.Code == "SAVE_RECOVERY_FAILED");
    }

    [Fact]
    public async Task CorruptPreviousGenerationIsAuditedAndPreserved()
    {
        var h = new Harness();
        using var simulation = h.Start();
        var root = Path.Combine(Path.GetTempPath(), "ScenarioEngineTests", Guid.NewGuid().ToString("N"));
        IScenarioSaveRecoveryService service = new ScenarioSaveService(new ScenarioSaveCodec(), new LocalFileSaveStore(root), h.Reporter);
        var ct = TestContext.Current.CancellationToken;
        await service.SaveAsync("slot", simulation.Snapshot, ct);
        await service.SaveAsync("slot", simulation.Snapshot, ct);
        var path = Path.Combine(root, "slot.json.previous");
        var corrupt = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(path, corrupt, ct);
        var result = await service.LoadPreviousAsync("slot", ct);
        Assert.Null(result.Save);
        Assert.False(result.Report.IsValid);
        Assert.Equal(corrupt, await File.ReadAllBytesAsync(path, ct));
        Assert.Contains(h.Reporter.Audits, a => a.Operation == "PreviousSaveRead" && a.Result == "rejected");
        Assert.NotNull((await service.LoadAsync("slot", ct)).Save);
    }

    [Fact]
    public async Task UnreadablePreviousGenerationIsNotReportedAsMissing()
    {
        var h = new Harness();
        var root = Path.Combine(Path.GetTempPath(), "ScenarioEngineTests", Guid.NewGuid().ToString("N"));
        IScenarioSaveRecoveryService service = new ScenarioSaveService(new ScenarioSaveCodec(), new LocalFileSaveStore(root), h.Reporter);
        Directory.CreateDirectory(Path.Combine(root, "slot.json.previous"));
        var result = await service.LoadPreviousAsync("slot", TestContext.Current.CancellationToken);
        Assert.Null(result.Save);
        Assert.Contains(result.Report.Diagnostics, d => d.Code == "SAVE_RECOVERY_FAILED");
    }

    private sealed class BaseStoreOnly : ISaveStore
    {
        public Task WriteAtomicAsync(string slotId, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ReadOnlyMemory<byte>?> ReadAsync(string slotId, CancellationToken cancellationToken) =>
            Task.FromResult<ReadOnlyMemory<byte>?>((ReadOnlyMemory<byte>?)null);
    }
}
