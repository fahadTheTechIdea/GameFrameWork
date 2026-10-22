using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Core;

/// <summary>Portable, accepted world composition with no renderer dependency.</summary>
public sealed record AcceptedSemanticWorld(
    SemanticWorldPackage World,
    IReadOnlyList<PlannedVegetation> Vegetation);

public sealed record SemanticWorldAcceptanceResult(
    AcceptedSemanticWorld? World, IReadOnlyList<string> Issues)
{
    public bool Accepted => World is not null && Issues.Count == 0;
}

/// <summary>
/// Terminal validation gate for semantically planned worlds. Building footprints must
/// originate from evaluated envelope geometry; no raster-cell approximations are used.
/// </summary>
public sealed class SemanticWorldAcceptanceGate
{
    public SemanticWorldAcceptanceResult Accept(
        SemanticWorldPackage world,
        VegetationComposition zonePlants,
        PathVegetationResult pathPlants,
        IReadOnlyDictionary<string, BuildingEnvelopeResult> buildings,
        IReadOnlyList<VegetationClearanceRoute> protectedRoutes,
        IReadOnlyList<VegetationClearanceAnchor> protectedAnchors,
        double vegetationRadiusMeters)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(buildings);
        var issues = new List<string>();
        var footprints = new List<VegetationProtectedFootprint>();
        foreach (var entry in buildings.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(entry.Key) ||
                entry.Value is null || !entry.Value.IsFeasible ||
                entry.Value.Footprint is null)
            {
                issues.Add("BUILDING_ENVELOPE_NOT_READY:" + entry.Key);
                continue;
            }
            footprints.Add(new(entry.Key, entry.Value.Footprint));
        }
        var vegetation = new VegetationWorldAcceptanceGate().Accept(
            zonePlants, pathPlants, footprints, protectedAnchors,
            protectedRoutes, vegetationRadiusMeters);
        issues.AddRange(vegetation.Conflicts);

        // Check the physical extents of *all* proposed plants, including plant-to-plant
        // overlaps across independently produced zone and path grammars.
        var plants = zonePlants.Plants.Concat(pathPlants.Plants).ToArray();
        for (int i = 0; i < plants.Length; i++)
        for (int j = i + 1; j < plants.Length; j++)
            if (plants[i].PositionMeters.DistanceTo(plants[j].PositionMeters) + 1e-9 <
                vegetationRadiusMeters * 2)
                issues.Add("VEGETATION_PAIR_CONFLICT:" + plants[i].Id + ":" + plants[j].Id);

        return issues.Count == 0
            ? new(new(world, plants), [])
            : new(null, issues);
    }
}
