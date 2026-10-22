using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core;

/// <summary>Reachability of authored entrances across the explicit pedestrian path network.
/// A road is never treated as walkable unless authored as a pedestrian path.</summary>
public sealed class PedestrianReachabilityValidator
{
    private const double Epsilon = 1e-6;
    public IReadOnlyList<string> Validate(DummyVillageWorldDocument world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var issues = new List<string>();
        var paths = world.SemanticWorld.Paths.Where(p =>
            p.Kind is "footpath" or "pedestrian" or "walkway").ToArray();
        var parent = Enumerable.Range(0, paths.Length).ToArray();
        int Root(int i) => parent[i] == i ? i : parent[i] = Root(parent[i]);
        static double Cross(WorldPoint a, WorldPoint b, WorldPoint c) =>
            (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
        static bool OnSegment(WorldPoint p, WorldPoint a, WorldPoint b) =>
            Math.Abs(Cross(a,b,p)) <= Epsilon &&
            p.X >= Math.Min(a.X,b.X)-Epsilon && p.X <= Math.Max(a.X,b.X)+Epsilon &&
            p.Y >= Math.Min(a.Y,b.Y)-Epsilon && p.Y <= Math.Max(a.Y,b.Y)+Epsilon;
        static bool Touch(WorldPoint a, WorldPoint b, WorldPoint c, WorldPoint d)
        {
            double ab1 = Cross(a,b,c), ab2 = Cross(a,b,d);
            double cd1 = Cross(c,d,a), cd2 = Cross(c,d,b);
            return (ab1*ab2 < -Epsilon && cd1*cd2 < -Epsilon) ||
                OnSegment(c,a,b) || OnSegment(d,a,b) ||
                OnSegment(a,c,d) || OnSegment(b,c,d);
        }
        for(int i=0;i<paths.Length;i++)
        for(int j=i+1;j<paths.Length;j++)
        {
            var a=paths[i].Geometry.Points; var b=paths[j].Geometry.Points;
            if(Enumerable.Range(1,a.Count-1).Any(k =>
                Enumerable.Range(1,b.Count-1).Any(m =>
                    Touch(a[k-1],a[k],b[m-1],b[m]))))
                parent[Root(i)] = Root(j);
        }
        var lookup = paths.Select((p,i)=>(p.Id,i))
            .ToDictionary(x=>x.Id,x=>x.i,StringComparer.Ordinal);
        var entrances = world.Entrances ?? [];
        var reached = new List<(string Building,int Component)>();
        foreach(var entry in entrances)
        {
            if(lookup.TryGetValue(entry.TargetPathId,out int index))
                reached.Add((entry.BuildingId,Root(index)));
        }
        if(reached.Select(e=>e.Component).Distinct().Count()>1)
            issues.Add("PEDESTRIAN_ENTRANCES_DISCONNECTED");
        return issues;
    }
}
