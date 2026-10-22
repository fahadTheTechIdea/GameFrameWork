using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Core;

/// <summary>
/// Engine-independent end-to-end acceptance gate for planned vegetation.
/// Resolves approved GameArts revision-pinned ground footprints before applying
/// per-asset spatial clearances. Use AcceptVerifiedAsync for production file hash verification; AcceptAsync is contract-only.
/// </summary>
public sealed class GameArtsSemanticWorldPipeline(IApprovedArtRegistry registry)
{
    public async Task<SemanticWorldAcceptanceResult> AcceptAsync(
        SemanticWorldPackage world,
        VegetationComposition zonePlants,
        PathVegetationResult pathPlants,
        IReadOnlyDictionary<string, BuildingEnvelopeResult> buildings,
        IReadOnlyList<VegetationClearanceRoute> protectedRoutes,
        IReadOnlyList<VegetationClearanceAnchor> protectedAnchors,
        IReadOnlyList<GameArtsVegetationFootprint> footprints,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(zonePlants);
        ArgumentNullException.ThrowIfNull(pathPlants);
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(protectedRoutes);
        ArgumentNullException.ThrowIfNull(protectedAnchors);
        ArgumentNullException.ThrowIfNull(footprints);
        cancellationToken.ThrowIfCancellationRequested();

        // Invalid upstream compositions may not be accepted, even when all asset
        // references independently resolve to approved production manifests.
        var upstream = zonePlants.Diagnostics.Concat(pathPlants.Diagnostics).ToArray();
        if (upstream.Length != 0) return new(null, upstream);

        var required = zonePlants.Plants.Concat(pathPlants.Plants)
            .Select(plant => plant.SpeciesAssetId);
        var resolution = await new GameArtsVegetationProfileResolver(registry)
            .ResolveAsync(required, footprints, cancellationToken);
        if (!resolution.Valid) return new(null, resolution.Issues);

        cancellationToken.ThrowIfCancellationRequested();
        return new SpeciesAwareSemanticWorldGate().Accept(
            world, zonePlants, pathPlants, buildings, protectedRoutes,
            protectedAnchors, resolution.Profiles);
    }
    /// <summary>Strict entry point: acceptance additionally requires matching on-disk
    /// approved PNG/atlas hashes and basic file structure for each vegetation asset.</summary>
    public async Task<SemanticWorldAcceptanceResult> AcceptVerifiedAsync(
        SemanticWorldPackage world,
        VegetationComposition zonePlants,
        PathVegetationResult pathPlants,
        IReadOnlyDictionary<string, BuildingEnvelopeResult> buildings,
        IReadOnlyList<VegetationClearanceRoute> protectedRoutes,
        IReadOnlyList<VegetationClearanceAnchor> protectedAnchors,
        IReadOnlyList<GameArtsVegetationFootprint> footprints,
        string trustedAssetRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trustedAssetRoot);
        var result = await AcceptAsync(world, zonePlants, pathPlants, buildings,
            protectedRoutes, protectedAnchors, footprints, cancellationToken);
        if (!result.Accepted) return result;
        var verifier = new GameArtsFileVerifier(trustedAssetRoot);
        var issues = new List<string>();
        foreach (var id in result.World!.Vegetation.Select(x => x.SpeciesAssetId)
                     .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var manifest = await registry.ResolveAsync(id, cancellationToken);
            if (manifest is null || manifest.ApprovalState != ArtApprovalState.Approved ||
                manifest.Sheets is null || manifest.Sheets.Count == 0)
            {
                issues.Add("GAMEARTS_PRODUCTION_FILES_MISSING:" + id);
                continue;
            }
            var validation = await verifier.VerifyAsync(manifest, cancellationToken);
            issues.AddRange(validation.Issues.Select(x =>
                "GAMEARTS_FILE_" + x.Code + ":" + id + ":" + x.AssetId));
        }
        return issues.Count == 0 ? result : new(null, issues);
    }

}
