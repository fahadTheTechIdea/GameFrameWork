using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;

namespace WorldCreator.Core;

/// <summary>Explicit game-world clearance dimensions in meters, keyed by production asset ID.
/// Values must ultimately come from verified GameArts metadata; no pixel-to-meter guessing.</summary>
public sealed record VegetationAssetClearance(string AssetId, double RadiusMeters, bool BlocksMovement = true);

public sealed class SpeciesAwareSemanticWorldGate
{
    public SemanticWorldAcceptanceResult Accept(
        SemanticWorldPackage world,
        VegetationComposition zonePlants,
        PathVegetationResult pathPlants,
        IReadOnlyDictionary<string, BuildingEnvelopeResult> buildings,
        IReadOnlyList<VegetationClearanceRoute> routes,
        IReadOnlyList<VegetationClearanceAnchor> anchors,
        IReadOnlyList<VegetationAssetClearance> assetClearances, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(zonePlants);
        ArgumentNullException.ThrowIfNull(pathPlants);
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(routes);
        ArgumentNullException.ThrowIfNull(anchors);
        ArgumentNullException.ThrowIfNull(assetClearances);
        token.ThrowIfCancellationRequested();
        var issues = new List<string>();
        var byAsset = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var profile in assetClearances)
        {
            if (profile is null || string.IsNullOrWhiteSpace(profile.AssetId) ||
                !double.IsFinite(profile.RadiusMeters) || profile.RadiusMeters <= 0 ||
                !byAsset.TryAdd(profile.AssetId, profile.RadiusMeters))
                issues.Add("VEGETATION_CLEARANCE_PROFILE_INVALID:" + profile?.AssetId);
        }
        var plants = zonePlants.Plants.Concat(pathPlants.Plants).ToArray();
        foreach (var plant in plants)
            if (!byAsset.ContainsKey(plant.SpeciesAssetId))
                issues.Add("VEGETATION_ASSET_DIMENSIONS_MISSING:" + plant.Id);
        if (issues.Count > 0) return new(null, issues);

        // Individual plants receive their own footprint radius at the existing
        // polygon/route/landmark gate. Never approximate all species with one size.
        var accepted = new SemanticWorldAcceptanceGate().Accept(world,
            new VegetationComposition([], zonePlants.Diagnostics),
            new PathVegetationResult([], pathPlants.Diagnostics),
            buildings, routes, anchors, 0);
        issues.AddRange(accepted.Issues);
        foreach (var plant in plants)
        {
            token.ThrowIfCancellationRequested();
            var one = new VegetationWorldAcceptanceGate().Accept(
                new VegetationComposition([plant], []),
                new PathVegetationResult([], []),
                buildings.Where(b => b.Value is { IsFeasible: true, Footprint: not null })
                    .Select(b => new VegetationProtectedFootprint(b.Key,b.Value.Footprint!))
                    .ToArray(),
                anchors, routes, byAsset[plant.SpeciesAssetId]);
            issues.AddRange(one.Conflicts);
        }
        // Pairwise sum of true physical clearance radii.
        for (int i = 0; i < plants.Length; i++)
        for (int j = i + 1; j < plants.Length; j++)
        {
            token.ThrowIfCancellationRequested();
            var p = plants[i];
            var q = plants[j];
            if (p.PositionMeters.DistanceTo(q.PositionMeters) + 1e-9 <
                byAsset[p.SpeciesAssetId] + byAsset[q.SpeciesAssetId])
                issues.Add("VEGETATION_PAIR_CONFLICT:" + p.Id + ":" + q.Id);
        }
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var plant in plants)
            if (string.IsNullOrWhiteSpace(plant.Id) || !identities.Add(plant.Id))
                issues.Add("VEGETATION_ID_DUPLICATE:" + plant.Id);
        return issues.Count == 0
            ? new(new AcceptedSemanticWorld(world, plants), [])
            : new(null, issues);
    }
}
