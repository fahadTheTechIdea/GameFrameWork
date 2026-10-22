using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core;

/// <summary>Additional pedestrian access rules for renderer-neutral village inspection.
/// Does not substitute for a navigation mesh or fully modeled right-of-way network.</summary>
public sealed class PedestrianAccessNetworkValidator
{
    private const double Epsilon = 1e-7;
    public IReadOnlyList<string> Validate(DummyVillageWorldDocument world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var issues = new List<string>();
        var entrances = world.Entrances ?? [];
        var paths = world.SemanticWorld.Paths;
        var permitted = paths.Where(p => p.Kind is "footpath" or "pedestrian" or "walkway")
            .ToArray();
        var known = permitted.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var link in entrances)
        {
            if (!known.Contains(link.TargetPathId))
                issues.Add("ENTRANCE_TARGET_NOT_PEDESTRIAN:" + link.BuildingId);
            var vertices = link.AccessWalkway.Points;
            for (int i = 1; i < vertices.Count; i++)
            foreach (var building in world.Buildings)
            {
                // The issuing building is allowed to touch its door at the first vertex.
                // The link may never pass through any building's interior.
                var polygon = building.Boundary.Vertices;
                var midpoint = new WorldPoint((vertices[i-1].X+vertices[i].X)/2,
                    (vertices[i-1].Y+vertices[i].Y)/2);
                if (Inside(midpoint, polygon))
                    issues.Add("ENTRANCE_WALKWAY_CROSSES_BUILDING:" +
                        link.BuildingId + ":" + building.ObjectId);
                for (int k = 0; k < polygon.Count; k++)
                {
                    var a = polygon[k]; var b = polygon[(k+1)%polygon.Count];
                    if (ProperIntersect(vertices[i-1],vertices[i],a,b))
                    {
                        issues.Add("ENTRANCE_WALKWAY_CROSSES_BUILDING:" +
                            link.BuildingId + ":" + building.ObjectId);
                        break;
                    }
                }
            }
        }
        return issues.Distinct(StringComparer.Ordinal).ToArray();
    }
    private static double Cross(WorldPoint a, WorldPoint b, WorldPoint c) =>
        (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
    private static bool ProperIntersect(WorldPoint a, WorldPoint b, WorldPoint c, WorldPoint d) =>
        Cross(a,b,c)*Cross(a,b,d)<-Epsilon &&
        Cross(c,d,a)*Cross(c,d,b)<-Epsilon;
    private static bool Inside(WorldPoint p, IReadOnlyList<WorldPoint> boundary)
    {
        bool inside=false;
        for(int i=0,j=boundary.Count-1;i<boundary.Count;j=i++)
        {
            var a=boundary[i]; var b=boundary[j];
            if((a.Y>p.Y)!=(b.Y>p.Y) &&
                p.X<(b.X-a.X)*(p.Y-a.Y)/(b.Y-a.Y)+a.X) inside=!inside;
        }
        return inside;
    }
}
