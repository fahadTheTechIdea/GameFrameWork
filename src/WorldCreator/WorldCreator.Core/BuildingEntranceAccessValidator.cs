using WorldCreator.Core.Geometry;

namespace WorldCreator.Core;

/// <summary>An intentionally authored door position and pedestrian access link.</summary>
public sealed record BuildingEntranceLink(string BuildingId, WorldPoint EntranceMeters,
    WorldPolyline AccessWalkway, string TargetPathId);

public sealed record EntranceAccessReport(IReadOnlyList<string> Issues)
{
    public bool Accessible => Issues.Count == 0;
}

/// <summary>Verifies an authored door lies on the building perimeter and its explicit
/// access walkway joins that door to a declared path. Does not infer a route through obstacles.</summary>
public sealed class BuildingEntranceAccessValidator
{
    private const double Tolerance = 1e-6;
    public EntranceAccessReport Validate(DummyVillageWorldDocument world,
        IReadOnlyList<BuildingEntranceLink> links)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(links);
        var issues = new List<string>();
        var buildings = world.Buildings.ToDictionary(b => b.ObjectId, StringComparer.Ordinal);
        var paths = world.SemanticWorld.Paths.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var assigned = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in links)
        {
            if (!buildings.TryGetValue(link.BuildingId, out var building) ||
                !assigned.Add(link.BuildingId))
            {
                issues.Add("ENTRANCE_BUILDING_UNKNOWN_OR_DUPLICATE:" + link.BuildingId);
                continue;
            }
            var border = building.Boundary.Vertices;
            bool onBoundary = Enumerable.Range(0, border.Count).Any(i =>
                Distance(link.EntranceMeters, border[i], border[(i+1)%border.Count]) <= Tolerance);
            if (!onBoundary) issues.Add("ENTRANCE_NOT_ON_FOOTPRINT:" + link.BuildingId);
            try { link.AccessWalkway.Validate(); }
            catch (ArgumentException)
            {
                issues.Add("ENTRANCE_WALKWAY_INVALID:" + link.BuildingId);
                continue;
            }
            var first = link.AccessWalkway.Points[0];
            var last = link.AccessWalkway.Points[^1];
            if (first.DistanceTo(link.EntranceMeters) > Tolerance)
                issues.Add("ENTRANCE_WALKWAY_NOT_ATTACHED:" + link.BuildingId);
            if (!paths.TryGetValue(link.TargetPathId, out var path) ||
                !Enumerable.Range(0, path.Geometry.Points.Count - 1).Any(i =>
                    Distance(last, path.Geometry.Points[i], path.Geometry.Points[i+1]) <= Tolerance))
                issues.Add("ENTRANCE_PATH_NOT_CONNECTED:" + link.BuildingId);
        }
        foreach (var building in world.Buildings)
            if (!assigned.Contains(building.ObjectId))
                issues.Add("ENTRANCE_MISSING:" + building.ObjectId);
        return new(issues);
    }

    private static double Distance(WorldPoint p, WorldPoint a, WorldPoint b)
    {
        double dx=b.X-a.X, dy=b.Y-a.Y;
        double t=Math.Clamp(((p.X-a.X)*dx+(p.Y-a.Y)*dy)/(dx*dx+dy*dy),0,1);
        return p.DistanceTo(new(a.X+t*dx,a.Y+t*dy));
    }
}
