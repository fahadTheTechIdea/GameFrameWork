using System.Text.Json;
namespace WorldCreator.Core.Operations;

public enum OperationSeverity { Debug, Information, Warning, Error, Critical }
public sealed record OperationEvent(
    string EventId, DateTimeOffset OccurredUtc, OperationSeverity Severity, string Source,
    string Operation, string CorrelationId, string Message, string? TargetId = null,
    string? Actor = null, string? Outcome = null);

public interface IOperationLog
{
    Task WriteAsync(OperationEvent entry, CancellationToken cancellationToken = default);
}
public interface IOperationAudit
{
    Task RecordAsync(OperationEvent entry, CancellationToken cancellationToken = default);
}
public interface IOperationNotifications
{
    Task PublishAsync(OperationEvent entry, CancellationToken cancellationToken = default);
}

/// <summary>Append-only JSONL writer; synchronizes operations within one process and reports I/O failures.</summary>
public sealed class JsonLineOperationStore(string filePath) : IOperationLog, IOperationAudit, IOperationNotifications
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _path = Path.GetFullPath(filePath);
    public async Task WriteAsync(OperationEvent entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var line = JsonSerializer.Serialize(entry) + Environment.NewLine;
            await File.AppendAllTextAsync(_path, line, cancellationToken);
        }
        finally { _mutex.Release(); }
    }
    public Task RecordAsync(OperationEvent entry, CancellationToken cancellationToken = default)
        => WriteAsync(entry, cancellationToken);
    public Task PublishAsync(OperationEvent entry, CancellationToken cancellationToken = default)
        => WriteAsync(entry, cancellationToken);
}
/// <summary>Routes notifications to a configured durable JSONL channel and to standard error for warnings/failures.</summary>
public sealed class ConsoleAndFileNotificationService(JsonLineOperationStore store) : IOperationNotifications
{
    public async Task PublishAsync(OperationEvent entry, CancellationToken cancellationToken = default)
    {
        await store.PublishAsync(entry, cancellationToken);
        if (entry.Severity >= OperationSeverity.Warning)
            Console.Error.WriteLine($"[{entry.EventId}] {entry.Message} (correlation: {entry.CorrelationId})");
    }
}
public sealed class WorldOperationReporter(IOperationLog log, IOperationAudit audit, IOperationNotifications notifications)
{
    public async Task ReportAsync(OperationEvent entry, bool notify = false, CancellationToken cancellationToken = default)
    {
        await log.WriteAsync(entry, cancellationToken);
        await audit.RecordAsync(entry, cancellationToken);
        if (notify) await notifications.PublishAsync(entry, cancellationToken);
    }
}
