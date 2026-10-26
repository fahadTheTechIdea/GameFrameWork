using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

/// <summary>Author-defined exclusion zone around a landmark, structure, or reserved point.</summary>
public sealed record VegetationClearanceAnchor(string Id, WorldPoint Center, double RadiusMeters);
/// <summary>Author-defined route reservation, including its physical width.</summary>
public sealed record VegetationClearanceRoute(string Id, WorldPolyline Centerline, double WidthMeters);
public sealed record VegetationClearanceResult(IReadOnlyList<string> Conflicts)
{
    public bool IsValid => Conflicts.Count == 0;
}

/// <summary>Validates proposed planting against explicit protected features.
/// Coordinates are never relocated or silently removed to make a result pass.</summary>
public sealed class VegetationClearanceValidator
{
    public VegetationClearanceResult Validate(IEnumerable<PlannedVegetation> plants,
        IReadOnlyList<VegetationClearanceAnchor> anchors,
        IReadOnlyList<VegetationClearanceRoute> routes,
        double plantRadiusMeters)
    {
        ArgumentNullException.ThrowIfNull(plants);
        ArgumentNullException.ThrowIfNull(anchors);
        ArgumentNullException.ThrowIfNull(routes);
        if (!double.IsFinite(plantRadiusMeters) || plantRadiusMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(plantRadiusMeters));
        var issues = new List<string>();
        foreach (var anchor in anchors)
            if (string.IsNullOrWhiteSpace(anchor.Id) ||
                !double.IsFinite(anchor.Center.X) || !double.IsFinite(anchor.Center.Y) ||
                !double.IsFinite(anchor.RadiusMeters) || anchor.RadiusMeters < 0)
                throw new ArgumentException("Invalid protected landmark clearance.");
        foreach (var route in routes)
        {
            if (string.IsNullOrWhiteSpace(route.Id) || !double.IsFinite(route.WidthMeters) ||
                route.WidthMeters < 0) throw new ArgumentException("Invalid protected route width.");
            route.Centerline.Validate();
        }
        foreach (var plant in plants)
        {
            if (!double.IsFinite(plant.PositionMeters.X) || !double.IsFinite(plant.PositionMeters.Y))
            {
                issues.Add("VEGETATION_POSITION_INVALID:" + plant.Id);
                continue;
            }
            foreach (var anchor in anchors)
                if (plant.PositionMeters.DistanceTo(anchor.Center) + 1e-9 <
                    plantRadiusMeters + anchor.RadiusMeters)
                    issues.Add("VEGETATION_ANCHOR_CONFLICT:" + plant.Id + ":" + anchor.Id);
            foreach (var route in routes)
            {
                var points = route.Centerline.Points;
                for (int i = 1; i < points.Count; i++)
                {
                    var a = points[i - 1]; var b = points[i];
                    var dx = b.X - a.X; var dy = b.Y - a.Y;
                    var t = Math.Clamp(((plant.PositionMeters.X - a.X) * dx +
                        (plant.PositionMeters.Y - a.Y) * dy) / (dx * dx + dy * dy), 0, 1);
                    var closest = new WorldPoint(a.X + dx * t, a.Y + dy * t);
                    if (plant.PositionMeters.DistanceTo(closest) + 1e-9 <
                        plantRadiusMeters + route.WidthMeters / 2)
                    {
                        issues.Add("VEGETATION_ROUTE_CONFLICT:" + plant.Id + ":" + route.Id);
                        break;
                    }
                }
            }
        }
        return new(issues);
    }
}
