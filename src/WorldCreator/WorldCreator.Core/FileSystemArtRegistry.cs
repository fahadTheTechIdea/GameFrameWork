using System.Text.Json;
namespace WorldCreator.Core;
/// <summary>Reads pre-approved versioned manifests without mutating GameArts' approval lifecycle.</summary>
public sealed class FileSystemArtRegistry(string root) : IApprovedArtRegistry
{
    public async Task<ApprovedArtManifest?> ResolveAsync(string assetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetId) || assetId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || assetId.Contains('/') || assetId.Contains('\\') || assetId is "." or "..")
            throw new ArgumentException("Asset ID must be one safe filename component.", nameof(assetId));
        var file = Path.Combine(root, assetId + ".json");
        if (!File.Exists(file)) return null;
        await using var stream = File.OpenRead(file);
        return await JsonSerializer.DeserializeAsync<ApprovedArtManifest>(stream, cancellationToken: cancellationToken);
    }
}
