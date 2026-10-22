using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Core;

/// <summary>Production bundle location and lifecycle revision must be explicitly declared
/// for every asset used by the accepted world.</summary>
public sealed record GameArtsWorldBundleReference(
    string AssetId, string BundlePath, string ApprovedRevision);

/// <summary>Strict world acceptance combining hash verification, bundle structural/motion
/// validation, and an independent lifecycle approval authority.</summary>
public sealed class GameArtsProductionWorldGate(
    IApprovedArtRegistry registry, IGameArtsApprovalAuthority approvals)
{
    public async Task<SemanticWorldAcceptanceResult> AcceptAsync(
        SemanticWorldPackage world,
        VegetationComposition zonePlants,
        PathVegetationResult pathPlants,
        IReadOnlyDictionary<string, BuildingEnvelopeResult> buildings,
        IReadOnlyList<VegetationClearanceRoute> routes,
        IReadOnlyList<VegetationClearanceAnchor> anchors,
        IReadOnlyList<GameArtsVegetationFootprint> footprints,
        string trustedAssetRoot,
        IReadOnlyList<GameArtsWorldBundleReference> bundles,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(bundles);
        ArgumentException.ThrowIfNullOrWhiteSpace(trustedAssetRoot);
        var result = await new GameArtsSemanticWorldPipeline(registry)
            .AcceptVerifiedAsync(world, zonePlants, pathPlants, buildings, routes,
                anchors, footprints, trustedAssetRoot, token);
        if (!result.Accepted) return result;
        var issues = new List<string>();
        var lookup = new Dictionary<string, GameArtsWorldBundleReference>(StringComparer.Ordinal);
        foreach (var item in bundles)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.AssetId) ||
                !lookup.TryAdd(item.AssetId, item))
                issues.Add("GAMEARTS_BUNDLE_REFERENCE_DUPLICATE_OR_INVALID:" + item?.AssetId);
        }
        var root = Path.GetFullPath(trustedAssetRoot).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        foreach (var id in result.World!.Vegetation.Select(x => x.SpeciesAssetId)
                     .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            if (!lookup.TryGetValue(id, out var bundle) ||
                string.IsNullOrWhiteSpace(bundle.BundlePath) ||
                string.IsNullOrWhiteSpace(bundle.ApprovedRevision))
            {
                issues.Add("GAMEARTS_BUNDLE_REFERENCE_MISSING:" + id);
                continue;
            }
            var manifest = await registry.ResolveAsync(id, token);
            if (manifest is null || manifest.GameArtsRevision != bundle.ApprovedRevision)
            {
                issues.Add("GAMEARTS_BUNDLE_REVISION_MISMATCH:" + id);
                continue;
            }
            if (Path.IsPathRooted(bundle.BundlePath))
            {
                issues.Add("GAMEARTS_BUNDLE_PATH_INVALID:" + id);
                continue;
            }
            var full = Path.GetFullPath(Path.Combine(root, bundle.BundlePath));
            if (!full.StartsWith(root, StringComparison.Ordinal) || !File.Exists(full))
            {
                issues.Add("GAMEARTS_BUNDLE_PATH_INVALID:" + id);
                continue;
            }
            NativeBundleResult native;
            try
            {
                native = await new GameArtsBundleReader().ReadAsync(full, token);
            }
            catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException
                or ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                issues.Add("GAMEARTS_BUNDLE_UNREADABLE:" + id);
                continue;
            }
            if (!native.IsValid || native.Bundle?.AssetId != id)
            {
                issues.Add("GAMEARTS_BUNDLE_ASSET_MISMATCH:" + id);
                continue;
            }
            var readiness = await new GameArtsProductionGate(approvals)
                .CheckAsync(full, bundle.ApprovedRevision, token);
            issues.AddRange(readiness.Issues.Select(issue =>
                "GAMEARTS_BUNDLE_" + id + ":" + issue));
            if (!readiness.Ready && readiness.Issues.Count == 0)
                issues.Add("GAMEARTS_BUNDLE_NOT_READY:" + id);
        }
        return issues.Count == 0 ? result : new(null, issues);
    }
}
