using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

/// <summary>Explicitly reserved building or infrastructure polygon in world meters.</summary>
public sealed record VegetationProtectedFootprint(string Id, WorldPolygon Boundary);

/// <summary>Fail-closed acceptance of combined landscape outputs. Never moves or drops plants.</summary>
public sealed record VegetationWorldAcceptance(
    IReadOnlyList<PlannedVegetation> Plants, IReadOnlyList<string> Conflicts)
{
    public bool Accepted => Conflicts.Count == 0;
}

public sealed class VegetationWorldAcceptanceGate
{
    public VegetationWorldAcceptance Accept(
        VegetationComposition zonePlants, PathVegetationResult pathPlants,
        IReadOnlyList<VegetationProtectedFootprint> footprints,
        IReadOnlyList<VegetationClearanceAnchor> anchors,
        IReadOnlyList<VegetationClearanceRoute> routes,
        double plantRadiusMeters)
    {
        ArgumentNullException.ThrowIfNull(zonePlants);
        ArgumentNullException.ThrowIfNull(pathPlants);
        ArgumentNullException.ThrowIfNull(footprints);
        var issues = zonePlants.Diagnostics.Concat(pathPlants.Diagnostics).ToList();
        var plants = zonePlants.Plants.Concat(pathPlants.Plants).ToArray();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var plant in plants)
            if (string.IsNullOrWhiteSpace(plant.Id) || !ids.Add(plant.Id))
                issues.Add("VEGETATION_ID_DUPLICATE:" + plant.Id);
        foreach (var footprint in footprints)
        {
            if (string.IsNullOrWhiteSpace(footprint.Id))
                throw new ArgumentException("Protected footprint requires an ID.");
            footprint.Boundary.Validate();
            foreach (var plant in plants)
            {
                if (!double.IsFinite(plant.PositionMeters.X) || !double.IsFinite(plant.PositionMeters.Y))
                    continue;
                if (Conflicts(plant.PositionMeters, footprint.Boundary, plantRadiusMeters))
                    issues.Add("VEGETATION_FOOTPRINT_CONFLICT:" + plant.Id + ":" + footprint.Id);
            }
        }
        issues.AddRange(new VegetationClearanceValidator()
            .Validate(plants, anchors, routes, plantRadiusMeters).Conflicts);
        return issues.Count == 0 ? new(plants, []) : new([], issues);
    }

    private static bool Conflicts(WorldPoint p, WorldPolygon shape, double radius)
    {
        var vertices = shape.Vertices;
        bool inside = false;
        for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
        {
            var a = vertices[j];
            var b = vertices[i];
            if ((a.Y > p.Y) != (b.Y > p.Y) &&
                p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) /
                (dx * dx + dy * dy), 0, 1);
            var nearest = new WorldPoint(a.X + dx * t, a.Y + dy * t);
            if (p.DistanceTo(nearest) <= radius + 1e-9) return true;
        }
        return inside;
    }
}
