namespace GameFrameWork.Core;

/// <summary>Footprint size is independently authored in world meters. Sprite image dimensions
/// and AssetRequirement.HeightMeters cannot determine ground clearance.</summary>
public sealed record GameArtsVegetationFootprint(
    string AssetId, string GameArtsRevision, double GroundRadiusMeters);

public sealed record VegetationProfileResolution(
    IReadOnlyList<VegetationAssetClearance> Profiles,
    IReadOnlyList<string> Issues)
{
    public bool Valid => Issues.Count == 0;
}

/// <summary>
/// Rejects missing, stale or unapproved footprints before a world may be assembled.
/// Note that manifest approval and matching revision are contract checks, not proof of
/// on-disk PNG/atlas integrity; production file verification is a separate gate.
/// </summary>
public sealed class GameArtsVegetationProfileResolver(IApprovedArtRegistry registry)
{
    public async Task<VegetationProfileResolution> ResolveAsync(
        IEnumerable<string> requiredAssetIds,
        IReadOnlyList<GameArtsVegetationFootprint> footprints,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requiredAssetIds);
        ArgumentNullException.ThrowIfNull(footprints);
        var problems = new List<string>();
        var profiles = new List<VegetationAssetClearance>();
        var byId = new Dictionary<string, GameArtsVegetationFootprint>(StringComparer.Ordinal);
        foreach (var footprint in footprints)
        {
            if (footprint is null || string.IsNullOrWhiteSpace(footprint.AssetId) ||
                string.IsNullOrWhiteSpace(footprint.GameArtsRevision) ||
                !double.IsFinite(footprint.GroundRadiusMeters) ||
                footprint.GroundRadiusMeters <= 0 ||
                !byId.TryAdd(footprint.AssetId, footprint))
                problems.Add("GAMEARTS_FOOTPRINT_INVALID:" + footprint?.AssetId);
        }
        foreach (var assetId in requiredAssetIds.Distinct(StringComparer.Ordinal)
                     .OrderBy(x => x, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(assetId) || !byId.TryGetValue(assetId, out var footprint))
            {
                problems.Add("GAMEARTS_FOOTPRINT_MISSING:" + assetId);
                continue;
            }
            var manifest = await registry.ResolveAsync(assetId, cancellationToken);
            if (manifest is null || manifest.SchemaVersion != 1 ||
                manifest.AssetId != assetId ||
                manifest.ApprovalState != ArtApprovalState.Approved ||
                string.IsNullOrWhiteSpace(manifest.GameArtsRevision))
            {
                problems.Add("GAMEARTS_ASSET_NOT_APPROVED:" + assetId);
                continue;
            }
            if (!string.Equals(manifest.GameArtsRevision, footprint.GameArtsRevision,
                    StringComparison.Ordinal))
            {
                problems.Add("GAMEARTS_FOOTPRINT_STALE:" + assetId);
                continue;
            }
            profiles.Add(new(assetId, footprint.GroundRadiusMeters));
        }
        return problems.Count == 0 ? new(profiles, []) : new([], problems);
    }
}
