using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Core;

/// <summary>
/// Engine-independent end-to-end acceptance gate for planned vegetation.
/// Resolves approved GameArts revision-pinned ground footprints before applying
/// per-asset spatial clearances. This does not verify production PNG/atlas files.
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
}
