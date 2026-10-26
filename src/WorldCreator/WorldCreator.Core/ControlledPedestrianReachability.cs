using WorldCreator.Core.Geometry;

namespace WorldCreator.Core;

/// <summary>Author-approved connection between two pedestrian routes.
/// Visually intersecting paths are not automatically traversable.</summary>
public sealed record PedestrianJunction(string Id, string FirstPathId,
    string SecondPathId, WorldPoint PositionMeters, bool Traversable);

public sealed class ControlledPedestrianReachability
{
    private const double Tolerance = 1e-6;
    public IReadOnlyList<string> Validate(DummyVillageWorldDocument world,
        IReadOnlyList<PedestrianJunction> junctions)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(junctions);
        var problems = new List<string>();
        var paths = world.SemanticWorld.Paths.Where(p =>
            p.Kind is "footpath" or "pedestrian" or "walkway")
            .ToDictionary(p => p.Id, StringComparer.Ordinal);
        var parent = paths.Keys.OrderBy(x => x, StringComparer.Ordinal)
            .ToDictionary(x => x, x => x, StringComparer.Ordinal);
        string Find(string id)
        {
            while (parent[id] != id) id = parent[id];
            return id;
        }
        static double Distance(WorldPoint p, WorldPoint a, WorldPoint b)
        {
            double dx=b.X-a.X, dy=b.Y-a.Y;
            var length=dx*dx+dy*dy;
            var t=length>0 ? Math.Clamp(((p.X-a.X)*dx+(p.Y-a.Y)*dy)/length,0,1) : 0;
            return p.DistanceTo(new(a.X+t*dx,a.Y+t*dy));
        }
        static bool Located(WorldPoint point, WorldPolyline line) =>
            Enumerable.Range(1,line.Points.Count-1)
                .Any(i=>Distance(point,line.Points[i-1],line.Points[i])<=Tolerance);

        var junctionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var junction in junctions)
        {
            if (string.IsNullOrWhiteSpace(junction.Id) || !junctionIds.Add(junction.Id) ||
                !paths.TryGetValue(junction.FirstPathId, out var first) ||
                !paths.TryGetValue(junction.SecondPathId, out var second) ||
                junction.FirstPathId == junction.SecondPathId ||
                !double.IsFinite(junction.PositionMeters.X) ||
                !double.IsFinite(junction.PositionMeters.Y) ||
                !Located(junction.PositionMeters, first.Geometry) ||
                !Located(junction.PositionMeters, second.Geometry))
            {
                problems.Add("PEDESTRIAN_JUNCTION_INVALID:" + junction.Id);
                continue;
            }
            if (junction.Traversable)
                parent[Find(junction.FirstPathId)] = Find(junction.SecondPathId);
        }
        var entrances = world.Entrances ?? [];
        var connected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entrance in entrances)
        {
            if (!paths.ContainsKey(entrance.TargetPathId))
            {
                problems.Add("PEDESTRIAN_ENTRANCE_TARGET_INVALID:" + entrance.BuildingId);
                continue;
            }
            connected.Add(Find(entrance.TargetPathId));
        }
        if (connected.Count > 1) problems.Add("PEDESTRIAN_ENTRANCES_DISCONNECTED");
        return problems;
    }
}
