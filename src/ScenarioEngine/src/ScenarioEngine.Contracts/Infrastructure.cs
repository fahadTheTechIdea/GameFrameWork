using System.Collections.Immutable;

namespace ScenarioEngine.Contracts;

public sealed record EngineNotice(string Code, string Message, Severity Severity,
    string? SessionId = null, Exception? Exception = null);
public sealed record AuditRecord(string Operation, string Target, string Result, string? SessionId = null);
public interface IEngineReporter
{
    void Report(EngineNotice notice);
    void Audit(AuditRecord record);
}
public interface INotificationSink { void Publish(EngineNotice notice); }
public sealed record ContentLoadResult(ImmutableArray<ValidatedScenario> Scenarios, ValidationReport Report)
{
    public bool IsReady => Report.IsValid && !Scenarios.IsEmpty;
}
public interface IScenarioContentService : IScenarioCatalog
{
    ContentLoadResult LoadNormalized(ReadOnlyMemory<byte> json, CancellationToken cancellationToken = default);
    Task<ContentLoadResult> LoadAsync(IScenarioPackSource source, CancellationToken cancellationToken);
    ContentRemovalResult RemoveFromCatalog(string packId, string contentDigest);
}
public sealed record ContentRemovalResult(bool Removed, string Code, int ScenarioCount);
public interface IScenarioPackSource
{
    Task<ReadOnlyMemory<byte>> ReadAsync(CancellationToken cancellationToken);
}
public sealed record SaveDocument(int SaveFormat, string EngineBuildId, DateTimeOffset CreatedAtUtc, SessionSnapshot Snapshot);
public sealed record SaveDecodeResult(SaveDocument? Save, ValidationReport Report);
public interface IScenarioSaveCodec
{
    byte[] Encode(SaveDocument document);
    SaveDecodeResult Decode(ReadOnlyMemory<byte> bytes);
}
public interface ISaveStore
{
    Task WriteAtomicAsync(string slotId, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken);
    Task<ReadOnlyMemory<byte>?> ReadAsync(string slotId, CancellationToken cancellationToken);
}
public interface IRecoverableSaveStore : ISaveStore
{
    /// <summary>Reads an earlier generation without promoting it or changing the current slot.</summary>
    Task<ReadOnlyMemory<byte>?> ReadPreviousAsync(string slotId, CancellationToken cancellationToken);
}
public sealed record SaveResult(bool Committed, string Code);
public interface IScenarioSaveService
{
    Task<SaveResult> SaveAsync(string slotId, SessionSnapshot snapshot, CancellationToken cancellationToken);
    Task<SaveDecodeResult> LoadAsync(string slotId, CancellationToken cancellationToken);
}
public interface IScenarioSaveRecoveryService : IScenarioSaveService
{
    /// <summary>Explicit, validated recovery. Never selected automatically by LoadAsync.</summary>
    Task<SaveDecodeResult> LoadPreviousAsync(string slotId, CancellationToken cancellationToken);
}
