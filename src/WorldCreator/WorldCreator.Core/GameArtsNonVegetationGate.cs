namespace WorldCreator.Core;

/// <summary>Discovers all nonvegetation asset identities referenced by an assembled world.
/// Materials are references, not sufficient proof of production approval.</summary>
public static class WorldArtDependencyInventory
{
    /// <summary>Checks the dependency-bearing fields before enumerating untrusted JSON.
    /// A blank asset reference is an error, never an absent dependency.</summary>
    public static IReadOnlyList<string> Validate(DummyVillageWorldDocument world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.SemanticWorld?.Base is not { } package ||
            package.SchemaVersion != 1 || package.ArtBindings is null ||
            package.Objects is null || world.TerrainMaterials is null ||
            world.GardenBeds is null || world.Vegetation is null || world.Buildings is null)
            return ["WORLD_DEPENDENCY_STRUCTURE_INVALID"];

        var issues = new List<string>();
        if (package.ArtBindings.Any(x => x is null || string.IsNullOrWhiteSpace(x.AssetId)))
            issues.Add("WORLD_ART_ASSET_REFERENCE_INVALID");
        if (world.TerrainMaterials.Any(x => x is null || string.IsNullOrWhiteSpace(x.MaterialId)))
            issues.Add("WORLD_TERRAIN_ASSET_REFERENCE_INVALID");
        if (world.GardenBeds.Any(x => x is null || string.IsNullOrWhiteSpace(x.MaterialId)))
            issues.Add("WORLD_GARDEN_ASSET_REFERENCE_INVALID");
        if (world.Vegetation.Any(x => x is null || string.IsNullOrWhiteSpace(x.SpeciesAssetId)))
            issues.Add("WORLD_VEGETATION_ASSET_REFERENCE_INVALID");
        return issues;
    }

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
        token.ThrowIfCancellationRequested();
        var inventoryIssues = WorldArtDependencyInventory.Validate(world);
        if (inventoryIssues.Count != 0) return inventoryIssues;
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
            if (!WorldAssetDependencyManifestBuilder.IsComplete(manifest, id) ||
                manifest!.GameArtsRevision != reference.ApprovedRevision)
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
            NativeBundleResult native;
            try
            {
                native = await new GameArtsBundleReader().ReadAsync(file, token);
            }
            catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException
                or ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                issues.Add("NONVEGETATION_BUNDLE_UNREADABLE:" + id);
                continue;
            }
            if (!native.IsValid || native.Bundle?.AssetId != id)
            {
                issues.Add("NONVEGETATION_BUNDLE_ASSET_MISMATCH:" + id);
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
