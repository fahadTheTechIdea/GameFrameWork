using System.Text.Json;

namespace GameFrameWork.Core;

/// <summary>Rechecks a previously exported portable world against current approved
/// GameArts manifests and real on-disk file hashes, without rerunning world generation.
/// This is an integrity audit, not independent proof of lifecycle approval.</summary>
public sealed record PortableWorldVerification(IReadOnlyList<string> Issues)
{
    public bool Valid => Issues.Count == 0;
}

public sealed class PortableWorldPackageVerifier(IApprovedArtRegistry registry)
{
    public async Task<PortableWorldVerification> VerifyAsync(
        string worldJsonPath, string trustedAssetRoot, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(worldJsonPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(trustedAssetRoot);
        DummyVillageWorldDocument? world;
        try
        {
            await using var stream = File.OpenRead(worldJsonPath);
            world = await JsonSerializer.DeserializeAsync<DummyVillageWorldDocument>(
                stream, cancellationToken: token);
        }
        catch (Exception ex) when (ex is IOException or JsonException or NotSupportedException)
        {
            return new(["WORLD_DOCUMENT_UNREADABLE"]);
        }
        if (world is null || world.SchemaVersion != 1 || !world.ProductionApproved ||
            world.AssetDependencies is null || world.SemanticWorld is null ||
            world.Vegetation is null || world.TerrainMaterials is null ||
            world.GardenBeds is null)
            return new(["WORLD_PRODUCTION_DOCUMENT_INVALID"]);

        var issues = new List<string>();
        var expected = WorldArtDependencyInventory.Collect(world)
            .Concat(world.Vegetation.Select(x => x.SpeciesAssetId))
            .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var recorded = new Dictionary<string, WorldAssetDependency>(StringComparer.Ordinal);
        foreach (var dependency in world.AssetDependencies)
        {
            if (dependency is null || string.IsNullOrWhiteSpace(dependency.AssetId) ||
                !recorded.TryAdd(dependency.AssetId, dependency))
                issues.Add("WORLD_DEPENDENCY_DUPLICATE_OR_INVALID:" + dependency?.AssetId);
        }
        foreach (var unexpected in recorded.Keys.Except(expected, StringComparer.Ordinal))
            issues.Add("WORLD_DEPENDENCY_UNEXPECTED:" + unexpected);

        var verifier = new GameArtsFileVerifier(trustedAssetRoot);
        foreach (var id in expected)
        {
            token.ThrowIfCancellationRequested();
            if (!recorded.TryGetValue(id, out var evidence))
            {
                issues.Add("WORLD_DEPENDENCY_MISSING:" + id);
                continue;
            }
            var manifest = await registry.ResolveAsync(id, token);
            if (manifest is null || manifest.SchemaVersion != 1 ||
                manifest.AssetId != id || manifest.ApprovalState != ArtApprovalState.Approved ||
                manifest.GameArtsRevision != evidence.GameArtsRevision ||
                manifest.StandardVersion != evidence.StandardVersion ||
                manifest.Sheets is null || evidence.Files is null)
            {
                issues.Add("WORLD_DEPENDENCY_APPROVAL_OR_REVISION_MISMATCH:" + id);
                continue;
            }
            var actualSheets = manifest.Sheets.OrderBy(s => s.Id, StringComparer.Ordinal)
                .Select(s => new WorldAssetFileEvidence(s.Id, s.ImageSha256,
                    s.AtlasSha256, s.ComponentId)).ToArray();
            var evidenceSheets = evidence.Files.OrderBy(s => s.SheetId, StringComparer.Ordinal).ToArray();
            if (actualSheets.Length == 0 || actualSheets.Length != evidenceSheets.Length ||
                !actualSheets.SequenceEqual(evidenceSheets))
            {
                issues.Add("WORLD_DEPENDENCY_HASH_EVIDENCE_MISMATCH:" + id);
                continue;
            }
            var files = await verifier.VerifyAsync(manifest, token);
            issues.AddRange(files.Issues.Select(e =>
                "WORLD_DEPENDENCY_FILE_" + e.Code + ":" + id + ":" + e.AssetId));
        }
        return new(issues);
    }
}
