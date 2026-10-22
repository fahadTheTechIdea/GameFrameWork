using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core;

/// <summary>Explicit, design-authored pedestrian crossing of a vehicular route.</summary>
public sealed record PedestrianRoadCrossing(string Id, string PedestrianPathId,
    string RoadPathId, WorldPoint PositionMeters, bool Permitted, bool Accessible);

/// <summary>Requires every pedestrian crossing of a road to be represented by an
/// authorized, accessible design decision; geometric intersection alone is insufficient.</summary>
public sealed class PedestrianRoadCrossingValidator
{
    private const double Epsilon = 1e-6;
    public IReadOnlyList<string> Validate(DummyVillageWorldDocument world,
        IReadOnlyList<PedestrianRoadCrossing> crossings)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(crossings);
        var issues = new List<string>();
        var paths = world.SemanticWorld.Paths.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var crossing in crossings)
        {
            if (crossing is null || string.IsNullOrWhiteSpace(crossing.Id) ||
                !ids.Add(crossing.Id) ||
                !paths.TryGetValue(crossing.PedestrianPathId, out var walkway) ||
                !paths.TryGetValue(crossing.RoadPathId, out var road) ||
                walkway.Kind is not ("footpath" or "pedestrian" or "walkway") ||
                road.Kind != "road" || !double.IsFinite(crossing.PositionMeters.X) ||
                !double.IsFinite(crossing.PositionMeters.Y) ||
                !OnLine(crossing.PositionMeters, walkway.Geometry) ||
                !OnLine(crossing.PositionMeters, road.Geometry))
            {
                issues.Add("ROAD_CROSSING_INVALID:" + crossing?.Id);
                continue;
            }
            if (!crossing.Permitted) issues.Add("ROAD_CROSSING_PROHIBITED:" + crossing.Id);
            if (!crossing.Accessible) issues.Add("ROAD_CROSSING_INACCESSIBLE:" + crossing.Id);
        }
        foreach (var pedestrian in paths.Values.Where(p =>
                     p.Kind is "footpath" or "pedestrian" or "walkway"))
        foreach (var road in paths.Values.Where(p => p.Kind == "road"))
        {
            // This initial conservative test detects centerline intersections, including
            // touching endpoints. Actual road widths and grade separation come later.
            if (pedestrian.Geometry.Points.Zip(pedestrian.Geometry.Points.Skip(1))
                .Any(a => road.Geometry.Points.Zip(road.Geometry.Points.Skip(1))
                    .Any(b => Intersects(a.First, a.Second, b.First, b.Second))) &&
                !crossings.Any(c => c.PedestrianPathId == pedestrian.Id &&
                    c.RoadPathId == road.Id && c.Permitted && c.Accessible &&
                    double.IsFinite(c.PositionMeters.X) && double.IsFinite(c.PositionMeters.Y) &&
                    OnLine(c.PositionMeters, pedestrian.Geometry) &&
                    OnLine(c.PositionMeters, road.Geometry)))
                issues.Add("ROAD_CROSSING_AUTHORIZATION_MISSING:" + pedestrian.Id + ":" + road.Id);
        }
        return issues.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static bool OnLine(WorldPoint p, WorldPolyline path) =>
        path.Points.Zip(path.Points.Skip(1)).Any(s => OnSegment(p, s.First, s.Second));
    private static double Cross(WorldPoint a, WorldPoint b, WorldPoint p) =>
        (b.X-a.X)*(p.Y-a.Y)-(b.Y-a.Y)*(p.X-a.X);
    private static bool OnSegment(WorldPoint p, WorldPoint a, WorldPoint b) =>
        Math.Abs(Cross(a,b,p)) < Epsilon &&
        p.X >= Math.Min(a.X,b.X)-Epsilon && p.X <= Math.Max(a.X,b.X)+Epsilon &&
        p.Y >= Math.Min(a.Y,b.Y)-Epsilon && p.Y <= Math.Max(a.Y,b.Y)+Epsilon;
    private static bool Intersects(WorldPoint a, WorldPoint b, WorldPoint c, WorldPoint d)
    {
        double u=Cross(a,b,c), v=Cross(a,b,d), w=Cross(c,d,a), z=Cross(c,d,b);
        return (u*v < -Epsilon && w*z < -Epsilon) ||
            OnSegment(c,a,b) || OnSegment(d,a,b) ||
            OnSegment(a,c,d) || OnSegment(b,c,d);
    }
}
