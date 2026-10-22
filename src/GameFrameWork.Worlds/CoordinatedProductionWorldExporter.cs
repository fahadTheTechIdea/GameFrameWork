using System.Text.Json;
using GameFrameWork.Core;
using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Worlds;

/// <summary>Production export from an already coordinated world. Every referenced
/// production object and surface must be approved by the caller's external authority;
/// this first integration strictly validates vegetation bundles only and therefore
/// refuses worlds containing unverified nonvegetation artwork.</summary>
public sealed record CoordinatedProductionRequest(
    CoordinatedSettlementResult Composition,
    ProductionWorldRequest GameArtsRequest);

public sealed class CoordinatedProductionWorldExporter(
    IApprovedArtRegistry registry, IGameArtsApprovalAuthority approvals)
{
    public async Task<ProductionWorldBuildResult> ExportAsync(
        CoordinatedProductionRequest request, string outputPath,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (!request.Composition.Valid)
            return new(null, request.Composition.Issues.Count > 0
                ? request.Composition.Issues : ["COORDINATED_COMPOSITION_INVALID"]);
        var document = request.Composition.World!;
        var art = request.GameArtsRequest;
        if (art.SemanticWorld != document.SemanticWorld &&
            JsonSerializer.Serialize(art.SemanticWorld) != JsonSerializer.Serialize(document.SemanticWorld))
            return new(null, ["PRODUCTION_SEMANTIC_WORLD_MISMATCH"]);
        if (!art.ZoneVegetation.Plants.SequenceEqual(document.Vegetation) ||
            art.PathVegetation.Plants.Count != 0)
            return new(null, ["PRODUCTION_VEGETATION_MISMATCH"]);

        // ProductionWorldBuilder currently validates only vegetation assets. We cannot
        // truthfully mark building sprites or surface materials production-approved.
        if (document.SemanticWorld.Base.ArtBindings.Count != 0 ||
            document.TerrainMaterials.Count != 0 ||
            document.GardenBeds.Count != 0)
            return new(null, ["PRODUCTION_NONVEGETATION_ART_VERIFICATION_REQUIRED"]);

        var accepted = await new GameArtsProductionWorldGate(registry, approvals)
            .AcceptAsync(art.SemanticWorld, art.ZoneVegetation, art.PathVegetation,
                art.BuildingEnvelopes, art.ProtectedRoutes, art.ProtectedAnchors,
                art.VegetationFootprints, art.TrustedAssetRoot, art.ProductionBundles, token);
        if (!accepted.Accepted) return new(null, accepted.Issues);

        var full = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var json = JsonSerializer.Serialize(document with { ProductionApproved = true },
            new JsonSerializerOptions { WriteIndented = true });
        var temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, json, token);
            File.Move(temporary, full, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        return new(full, []);
    }
}
