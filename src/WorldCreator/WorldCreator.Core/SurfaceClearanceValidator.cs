using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

/// <summary>Opt-in authored visual spacing in continuous meters; never shifts source objects.</summary>
public static class SurfaceClearanceValidator
{
    public static IReadOnlyList<WorldDesignIssue> Validate(MockupWorldDesign design,
        IReadOnlyList<WorldPolygon>? buildings=null,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(design);token.ThrowIfCancellationRequested();
        var issues=new List<WorldDesignIssue>();var rules=design.SurfaceClearances??[];
        void Fail(string code,string id,string message)=>issues.Add(new(code,id,message));
        if(rules.Count>256)return [new("SURFACE_CLEARANCE_LIMIT","","At most 256 spacing rules per design.")];
        var regions=design.Blueprint.Regions.ToDictionary(r=>r.Id,StringComparer.Ordinal);
        var paths=design.Paths.ToDictionary(p=>p.Id,StringComparer.Ordinal);
        var roads=design.RoadRules.ToDictionary(r=>r.PathId,StringComparer.Ordinal);
        var anchors=design.Blueprint.Anchors.ToDictionary(a=>a.Id,StringComparer.Ordinal);
        var ids=new HashSet<string>(StringComparer.Ordinal);
        var surfaces=new Dictionary<string,IReadOnlyList<StreetSurface>>(StringComparer.Ordinal);
        foreach(var rule in rules)
        {
            token.ThrowIfCancellationRequested();
            bool ValidRefs(IReadOnlyList<string>? values,Func<string,bool> exists)=>values is not null&&values.Count<=256&&
                values.Distinct(StringComparer.Ordinal).Count()==values.Count&&values.All(id=>!string.IsNullOrWhiteSpace(id)&&exists(id));
            if(rule is null||string.IsNullOrWhiteSpace(rule.Id)||!ids.Add(rule.Id)||string.IsNullOrWhiteSpace(rule.RegionId)||!regions.ContainsKey(rule.RegionId)||
                !double.IsFinite(rule.MinimumGapMeters)||rule.MinimumGapMeters<0||!double.IsFinite(rule.VegetationRadiusMeters)||rule.VegetationRadiusMeters<0||
                !ValidRefs(rule.OtherRegionIds,id=>id!=rule.RegionId&&regions.ContainsKey(id))||!ValidRefs(rule.RoadIds,id=>roads.ContainsKey(id)&&paths.ContainsKey(id))||
                !ValidRefs(rule.VegetationAnchorIds,anchors.ContainsKey)||
                (rule.GapMaterialId is { } material&&(string.IsNullOrWhiteSpace(material)||material.Length>128||material.Any(char.IsControl)||!design.TerrainRules.Any(r=>r.RegionId==rule.RegionId))))
            {Fail("SURFACE_CLEARANCE_RULE_INVALID",rule?.Id??"","Complete spacing rules require finite nonnegative distances and unique existing references.");continue;}
            var shape=regions[rule.RegionId].Outline;
            foreach(var id in rule.OtherRegionIds)CheckGap(shape,regions[id].Outline,id);
            foreach(var id in rule.RoadIds)
            {
                if(!surfaces.TryGetValue(id,out var bands))surfaces[id]=bands=StreetSurfaceBuilder.Build(new(id,paths[id].Centerline,roads[id].Profile));
                foreach(var band in bands)CheckGap(shape,new(band.Polygon),id+":"+band.Kind);
            }
            if(rule.ProtectBuildings)foreach(var building in buildings??[])CheckGap(shape,building,"building");
            foreach(var id in rule.VegetationAnchorIds)
            {
                var p=anchors[id].PositionMeters;double radius=rule.VegetationRadiusMeters;
                if(!shape.ContainsPoint(p)||BoundaryDistance(shape,p)+1e-8<radius)
                    Fail("SURFACE_CLEARANCE_TREE_OUTSIDE",rule.Id,"Adjusted planted ground does not contain fixed tree base "+id+". Revise the design; never relocate it.");
            }
            void CheckGap(WorldPolygon a,WorldPolygon b,string target)
            {
                token.ThrowIfCancellationRequested();double gap=Distance(a,b,token);
                if(gap+1e-8<rule.MinimumGapMeters || (rule.MinimumGapMeters==0 && gap==0 && TerrainCoverageTopology.IntersectionArea(a,b)>1e-8))
                    Fail("SURFACE_CLEARANCE_TOO_SMALL",rule.Id,FormattableString.Invariant($"{target}: measured gap {gap:0.###}m; required {rule.MinimumGapMeters:0.###}m. Revise the authored surface boundary."));
            }
        }
        return issues;
    }

    public static double Distance(WorldPolygon a,WorldPolygon b,CancellationToken token=default)
    {
        if(a.ContainsPoint(b.Vertices[0])||b.ContainsPoint(a.Vertices[0]))return 0;
        for(int i=0;i<a.Vertices.Count;i++)for(int j=0;j<b.Vertices.Count;j++)
        {
            token.ThrowIfCancellationRequested();
            if(PlanarIntersections.Intersect(a.Vertices[i],a.Vertices[(i+1)%a.Vertices.Count],b.Vertices[j],b.Vertices[(j+1)%b.Vertices.Count]).Kind!=IntersectionKind.None)return 0;
        }
        return Math.Min(a.Vertices.Min(b.DistanceToPoint),b.Vertices.Min(a.DistanceToPoint));
    }
    private static double BoundaryDistance(WorldPolygon shape,WorldPoint p)
    {
        double distance=double.PositiveInfinity;
        for(int i=0;i<shape.Vertices.Count;i++)
        {
            var a=shape.Vertices[i];var b=shape.Vertices[(i+1)%shape.Vertices.Count];double dx=b.X-a.X,dy=b.Y-a.Y;
            double t=Math.Clamp(((p.X-a.X)*dx+(p.Y-a.Y)*dy)/(dx*dx+dy*dy),0,1);
            distance=Math.Min(distance,p.DistanceTo(new(a.X+t*dx,a.Y+t*dy)));
        }
        return distance;
    }
}
