namespace GameFrameWork.Core;

/// <summary>Discovers all nonvegetation asset identities referenced by an assembled world.
/// Materials are references, not sufficient proof of production approval.</summary>
public static class WorldArtDependencyInventory
{
    public static IReadOnlyList<string> Collect(DummyVillageWorldDocument world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return world.SemanticWorld.Base.ArtBindings.Select(x => x.AssetId)
            .Concat(world.TerrainMaterials.Select(x => x.MaterialId))
            .Concat(world.GardenBeds.Select(x => x.MaterialId))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }
}

public sealed class GameArtsNonVegetationGate(IApprovedArtRegistry registry,
    IGameArtsApprovalAuthority approvals)
{
    public async Task<IReadOnlyList<string>> VerifyAsync(DummyVillageWorldDocument world,
        string trustedAssetRoot, IReadOnlyList<GameArtsWorldBundleReference> references,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(references);
        ArgumentException.ThrowIfNullOrWhiteSpace(trustedAssetRoot);
        var issues = new List<string>();
        var map = new Dictionary<string, GameArtsWorldBundleReference>(StringComparer.Ordinal);
        foreach (var reference in references)
            if (reference is null || string.IsNullOrWhiteSpace(reference.AssetId) ||
                !map.TryAdd(reference.AssetId, reference))
                issues.Add("NONVEGETATION_REFERENCE_INVALID:" + reference?.AssetId);
        string root = Path.GetFullPath(trustedAssetRoot).TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        foreach (var id in WorldArtDependencyInventory.Collect(world))
        {
            token.ThrowIfCancellationRequested();
            if (!map.TryGetValue(id, out var reference) ||
                string.IsNullOrWhiteSpace(reference.BundlePath) ||
                string.IsNullOrWhiteSpace(reference.ApprovedRevision))
            {
                issues.Add("NONVEGETATION_BUNDLE_MISSING:" + id);
                continue;
            }
            var manifest = await registry.ResolveAsync(id, token);
            if (manifest is null || manifest.AssetId != id || manifest.SchemaVersion != 1 ||
                manifest.ApprovalState != ArtApprovalState.Approved ||
                manifest.GameArtsRevision != reference.ApprovedRevision ||
                manifest.Sheets is null || manifest.Sheets.Count == 0)
            {
                issues.Add("NONVEGETATION_MANIFEST_NOT_APPROVED:" + id);
                continue;
            }
            var files = await new GameArtsFileVerifier(trustedAssetRoot).VerifyAsync(manifest, token);
            issues.AddRange(files.Issues.Select(i => "NONVEGETATION_FILE_" + id + ":" + i.Code));
            if (Path.IsPathRooted(reference.BundlePath))
            {
                issues.Add("NONVEGETATION_BUNDLE_PATH_INVALID:" + id);
                continue;
            }
            var file = Path.GetFullPath(Path.Combine(root, reference.BundlePath));
            if (!file.StartsWith(root, StringComparison.Ordinal) || !File.Exists(file))
            {
                issues.Add("NONVEGETATION_BUNDLE_PATH_INVALID:" + id);
                continue;
            }
            var readiness = await new GameArtsProductionGate(approvals)
                .CheckAsync(file, reference.ApprovedRevision, token);
            issues.AddRange(readiness.Issues.Select(i => "NONVEGETATION_BUNDLE_" + id + ":" + i));
            if (!readiness.Ready && readiness.Issues.Count == 0)
                issues.Add("NONVEGETATION_NOT_READY:" + id);
        }
        return issues;
    }
}
