using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Persistence;

/// <summary>.NET desktop adapter; Godot may replace it with a native FileAccess adapter.</summary>
public sealed class LocalFileSaveStore : IRecoverableSaveStore
{
    private readonly string _root;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _slotGates = new(StringComparer.Ordinal);
    public LocalFileSaveStore(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }
    private string PathFor(string slotId)
    {
        if (!Regex.IsMatch(slotId, "^[a-zA-Z0-9_-]{1,64}$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Invalid slot identifier.", nameof(slotId));
        return Path.Combine(_root, slotId + ".json");
    }
    public async Task WriteAtomicAsync(string slotId, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        if (bytes.Length > 16 * 1024 * 1024) throw new IOException("Save exceeds size limit.");
        var path = PathFor(slotId);
        var gate = _slotGates.GetOrAdd(slotId, _ => new(1, 1));
        await gate.WaitAsync(cancellationToken);
        var temporary = Path.Combine(_root, slotId + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path)) File.Replace(temporary, path, path + ".previous", ignoreMetadataErrors: false);
            else File.Move(temporary, path);
            // Cancellation after promotion must not turn an acknowledged write into "cancelled".
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            finally { gate.Release(); }
        }
    }
    public Task<ReadOnlyMemory<byte>?> ReadAsync(string slotId, CancellationToken cancellationToken) =>
        ReadFileAsync(PathFor(slotId), cancellationToken);
    public Task<ReadOnlyMemory<byte>?> ReadPreviousAsync(string slotId, CancellationToken cancellationToken) =>
        ReadFileAsync(PathFor(slotId) + ".previous", cancellationToken);
    private static async Task<ReadOnlyMemory<byte>?> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        FileStream opened;
        try
        {
            opened = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete,
                4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (FileNotFoundException) { return (ReadOnlyMemory<byte>?)null; } // Explicit absence, not permission or I/O failure.
        await using var stream = opened;
        if (stream.Length > 16 * 1024 * 1024) throw new IOException("Save exceeds size limit.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, cancellationToken);
        return bytes;
    }
}
