namespace GameFrameWork.Core;

/// <summary>Evidence references embedded in exported worlds. Hashes refer to approved
/// GameArts artifacts, not a claim that this record itself proves lifecycle approval.</summary>
public sealed record WorldAssetFileEvidence(string SheetId, string ImageSha256,
    string AtlasSha256, string ComponentId);
public sealed record WorldAssetDependency(string AssetId, string GameArtsRevision,
    string StandardVersion, IReadOnlyList<WorldAssetFileEvidence> Files);
public sealed record WorldAssetDependencyResult(IReadOnlyList<WorldAssetDependency> Dependencies,
    IReadOnlyList<string> Issues)
{
    public bool Valid => Issues.Count == 0;
}

/// <summary>Produces stable revision-pinned dependency records after production validation.</summary>
public sealed class WorldAssetDependencyManifestBuilder(IApprovedArtRegistry registry)
{
    public async Task<WorldAssetDependencyResult> BuildAsync(
        IEnumerable<string> requiredAssets, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(requiredAssets);
        var result = new List<WorldAssetDependency>();
        var errors = new List<string>();
        foreach (var id in requiredAssets.Distinct(StringComparer.Ordinal)
                     .OrderBy(x => x, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add("WORLD_ASSET_ID_MISSING");
                continue;
            }
            var manifest = await registry.ResolveAsync(id, token);
            if (!IsComplete(manifest, id))
            {
                errors.Add("WORLD_ASSET_EVIDENCE_INCOMPLETE:" + id);
                continue;
            }
            result.Add(new(id, manifest!.GameArtsRevision, manifest.StandardVersion,
                manifest.Sheets.OrderBy(s => s.Id, StringComparer.Ordinal)
                    .Select(s => new WorldAssetFileEvidence(s.Id,
                        s.ImageSha256, s.AtlasSha256, s.ComponentId)).ToArray()));
        }
        return errors.Count == 0 ? new(result, []) : new([], errors);
    }

    internal static bool IsComplete(ApprovedArtManifest? manifest, string assetId) =>
        manifest is { SchemaVersion: 1, ApprovalState: ArtApprovalState.Approved } &&
        manifest.AssetId == assetId &&
        !string.IsNullOrWhiteSpace(manifest.GameArtsRevision) &&
        !string.IsNullOrWhiteSpace(manifest.StandardVersion) &&
        manifest.Sheets is { Count: > 0 } &&
        manifest.Sheets.All(s => s is not null && !string.IsNullOrWhiteSpace(s.Id) &&
            ValidHash(s.ImageSha256) && ValidHash(s.AtlasSha256) &&
            !string.IsNullOrWhiteSpace(s.ComponentId)) &&
        manifest.Sheets.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() == manifest.Sheets.Count;

    private static bool ValidHash(string? hash) =>
        hash is { Length: 64 } && hash.All(Uri.IsHexDigit);
}
