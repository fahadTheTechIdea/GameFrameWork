using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

/// <summary>Queries the same authored terrain overlays used by the raster compiler.
/// Boundary partitioning detects interruptions smaller than an arbitrary sample interval.</summary>
public sealed class MockupTerrainQuery(MockupWorldDesign design)
{
    private readonly IReadOnlyDictionary<string,VisualRegion> _regions=design.Blueprint.Regions.ToDictionary(r=>r.Id,StringComparer.Ordinal);
    public CellKind? KindAt(WorldPoint point) => design.TerrainRules.Where(t=>_regions[t.RegionId].Outline.ContainsPoint(point))
        .OrderByDescending(t=>t.Priority).ThenBy(t=>t.RegionId,StringComparer.Ordinal).Select(t=>(CellKind?)t.Kind).FirstOrDefault();

    public bool Matches(WorldPolyline line,Func<WorldPoint,bool> predicate,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        if (line.Points.Any(p=>!predicate(p))) return false;
        foreach (var pair in line.Points.Zip(line.Points.Skip(1)))
        {
            token.ThrowIfCancellationRequested();
            var a=pair.First; var b=pair.Second;
            double vx=b.X-a.X,vy=b.Y-a.Y,squared=vx*vx+vy*vy;
            var cuts=new SortedSet<double> { 0,1 };
            foreach (var rule in design.TerrainRules)
            {
                token.ThrowIfCancellationRequested();
                var vertices=_regions[rule.RegionId].Outline.Vertices;
                for (int i=0;i<vertices.Count;i++)
                {
                    var hit=PlanarIntersections.Intersect(a,b,vertices[i],vertices[(i+1)%vertices.Count]);
                    if (hit.Point is { } p) cuts.Add(Math.Clamp(((p.X-a.X)*vx+(p.Y-a.Y)*vy)/squared,0,1));
                    else if (hit.Kind==IntersectionKind.CollinearOverlap)
                        foreach (var v in new[] { vertices[i],vertices[(i+1)%vertices.Count] })
                            cuts.Add(Math.Clamp(((v.X-a.X)*vx+(v.Y-a.Y)*vy)/squared,0,1));
                }
            }
            if (cuts.Zip(cuts.Skip(1)).Any(c=>!predicate(new(a.X+vx*(c.First+c.Second)/2,a.Y+vy*(c.First+c.Second)/2)))) return false;
        }
        return true;
    }
}
