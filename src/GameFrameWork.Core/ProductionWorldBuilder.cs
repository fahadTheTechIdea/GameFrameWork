using System.Text.Json;
using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Core;

/// <summary>Production inputs for a single engine-independent, approved world export.</summary>
public sealed record ProductionWorldRequest(
    SemanticWorldPackage SemanticWorld,
    VegetationComposition ZoneVegetation,
    PathVegetationResult PathVegetation,
    IReadOnlyDictionary<string, BuildingEnvelopeResult> BuildingEnvelopes,
    IReadOnlyList<VegetationClearanceRoute> ProtectedRoutes,
    IReadOnlyList<VegetationClearanceAnchor> ProtectedAnchors,
    IReadOnlyList<GameArtsVegetationFootprint> VegetationFootprints,
    string TrustedAssetRoot,
    IReadOnlyList<GameArtsWorldBundleReference> ProductionBundles);

public sealed record ProductionWorldBuildResult(
    string? OutputPath, IReadOnlyList<string> Issues)
{
    public bool Produced => OutputPath is not null && Issues.Count == 0;
}

/// <summary>
/// Standard production entry point. An output file is created only after all GameArts
/// native bundle, approval, file integrity, and spatial gates succeed.
/// Existing contract-only APIs remain for editor previews and diagnostics.
/// </summary>
public sealed class ProductionWorldBuilder(
    IApprovedArtRegistry registry, IGameArtsApprovalAuthority approvals)
{
    public async Task<ProductionWorldBuildResult> BuildAsync(
        ProductionWorldRequest request, string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var accepted = await new GameArtsProductionWorldGate(registry, approvals)
            .AcceptAsync(request.SemanticWorld, request.ZoneVegetation,
                request.PathVegetation, request.BuildingEnvelopes,
                request.ProtectedRoutes, request.ProtectedAnchors,
                request.VegetationFootprints, request.TrustedAssetRoot,
                request.ProductionBundles, cancellationToken);
        if (!accepted.Accepted)
            return new(null, accepted.Issues);

        var target = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        // Serialize before writing; do not replace an existing accepted package if
        // serialization or cancellation fails.
        var json = JsonSerializer.Serialize(accepted.World,
            new JsonSerializerOptions { WriteIndented = true });
        cancellationToken.ThrowIfCancellationRequested();
        var staging = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(staging, json, cancellationToken);
            File.Move(staging, target, overwrite: true);
        }
        finally
        {
            if (File.Exists(staging)) File.Delete(staging);
        }
        return new(target, []);
    }
}
