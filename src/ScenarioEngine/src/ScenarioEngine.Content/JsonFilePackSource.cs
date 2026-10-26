using ScenarioEngine.Contracts;

namespace ScenarioEngine.Content;

/// <summary>Headless .NET file adapter for a normalized JSON pack; Godot can supply its own source.</summary>
public sealed class JsonFilePackSource(string path) : IScenarioPackSource
{
    public async Task<ReadOnlyMemory<byte>> ReadAsync(CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > 1024 * 1024) throw new ScenarioContractException("PACK_SIZE", "Normalized pack exceeds 1 MiB.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, cancellationToken);
        return bytes;
    }
}
