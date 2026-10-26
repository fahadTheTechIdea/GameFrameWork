using System.Text.Json;
using Microsoft.Extensions.Logging;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Diagnostics;

/// <summary>One owner for platform logging, local audit and safe notification routing.</summary>
public sealed class ApplicationDiagnostics : IEngineReporter, IDisposable
{
    private readonly ILogger _logger;
    private readonly FileStream _audit;
    private readonly INotificationSink _notifications;
    private readonly object _gate = new();
    private readonly long _maxBytes;

    public ApplicationDiagnostics(ILogger logger, string auditPath, INotificationSink notifications, long maxBytes = 8 * 1024 * 1024)
    {
        _logger = logger; _notifications = notifications; _maxBytes = maxBytes;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(auditPath))!);
        _audit = new(auditPath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
    }
    public bool IsDegraded { get; private set; }
    public void Report(EngineNotice notice)
    {
        var level = notice.Severity switch { Severity.Error => LogLevel.Error, Severity.Warning => LogLevel.Warning, _ => LogLevel.Information };
        SafeLog(level, notice.Exception, notice.Code, notice.Message, notice.SessionId);
        try { _notifications.Publish(notice with { Exception = null }); }
        catch (Exception exception)
        {
            // Independent channel: never recursively notify through the failing sink.
            IsDegraded = true;
            SafeLog(LogLevel.Error, exception, "NOTIFICATION_FAILED", notice.Code, notice.SessionId);
            Console.Error.WriteLine("Notification delivery failed; consult local diagnostics.");
        }
    }
    private void SafeLog(LogLevel level, Exception? cause, string code, string message, string? session)
    {
        try { _logger.Log(level, cause, "{Code} {Message} Session={SessionId}", code, message, session); }
        catch (Exception loggingFailure)
        {
            IsDegraded = true;
            Console.Error.WriteLine("DIAGNOSTICS_DEGRADED: " + loggingFailure.GetType().Name + "; event " + code);
        }
    }
    public void Audit(AuditRecord record)
    {
        lock (_gate)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new { occurredAtUtc = DateTimeOffset.UtcNow, record }, ContractJson.Options);
            if (_audit.Length + bytes.Length + 1 > _maxBytes)
                throw new IOException("Audit storage limit reached; archive records before protected operations.");
            _audit.Write(bytes);
            _audit.WriteByte((byte)'\n');
            _audit.Flush(flushToDisk: true);
        }
    }
    public void Dispose() => _audit.Dispose();
}
public sealed class ConsoleNotificationSink : INotificationSink
{
    public void Publish(EngineNotice notice)
    {
        if (notice.Severity == Severity.Error)
            Console.Error.WriteLine($"{notice.Code}: {notice.Message}");
    }
}
