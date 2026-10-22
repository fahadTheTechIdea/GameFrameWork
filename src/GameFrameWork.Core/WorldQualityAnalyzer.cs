using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core;

/// <summary>Inspectable, deterministic geometry-quality diagnostics; not a renderer or playtest.</summary>
public sealed record WorldQualityReport(
    double NominalTerrainCoverageRatio,
    int ConnectedPathComponents,
    int BuildingCount,
    int PlantedObjectCount,
    IReadOnlyList<string> Issues)
{
    public bool MeetsRules => Issues.Count == 0;
}

public sealed record WorldQualityRules(
    double MinimumTerrainCoverageRatio,
    double MaximumBuildingPathDistanceMeters,
    int MinimumVegetationCount);

/// <summary>
/// Checks a known base-ground rectangle against styled fill geometry, path graph
/// connectivity via exact segment intersections, building access and vegetation counts.
/// Coverage is exact for nonoverlapping convex pieces within the convex base ground;
/// overlapping or out-of-bounds fills are rejected rather than reporting inflated coverage.
/// </summary>
public sealed class WorldQualityAnalyzer
{
    public WorldQualityReport Analyze(DummyVillageWorldDocument document, WorldQualityRules rules)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(rules);
        if (!double.IsFinite(rules.MinimumTerrainCoverageRatio) ||
            rules.MinimumTerrainCoverageRatio is < 0 or > 1 ||
            !double.IsFinite(rules.MaximumBuildingPathDistanceMeters) ||
            rules.MaximumBuildingPathDistanceMeters < 0 || rules.MinimumVegetationCount < 0)
            throw new ArgumentException("Invalid world quality thresholds.");

        var issues = new List<string>();
        var ground = document.SemanticWorld.Terrain.SingleOrDefault(t => t.Id == "village-ground");
        if (ground is null) issues.Add("BASE_GROUND_MISSING");
        double groundArea = ground is null ? 0 : Math.Abs(ground.Boundary.SignedArea());
        double styledArea = document.TerrainMaterials
            .Where(p => p.SourceRegionId == ground?.SourceVisualRegionId)
            .Sum(p => Math.Abs(p.Geometry.SignedArea()));
        var groundPieces = document.TerrainMaterials
            .Where(p => p.SourceRegionId == ground?.SourceVisualRegionId).ToArray();
        var topologyIssues = ground is null
            ? Array.Empty<string>()
            : TerrainCoverageTopology.Validate(ground.Boundary, groundPieces).ToArray();
        issues.AddRange(topologyIssues);
        double ratio = groundArea > 0 && topologyIssues.Length == 0
            ? Math.Min(1, styledArea / groundArea) : 0;
        if (ratio + 1e-9 < rules.MinimumTerrainCoverageRatio)
            issues.Add("GROUND_COVERAGE_INSUFFICIENT");

        var paths = document.SemanticWorld.Paths;
        var parent = Enumerable.Range(0, paths.Count).ToArray();
        int Root(int n) { while (parent[n] != n) n = parent[n]; return n; }
        for (int i = 0; i < paths.Count; i++)
        for (int j = i + 1; j < paths.Count; j++)
            if (paths[i].Geometry.Points.Zip(paths[i].Geometry.Points.Skip(1))
                .Any(a => paths[j].Geometry.Points.Zip(paths[j].Geometry.Points.Skip(1))
                    .Any(b => SegmentsTouch(a.First, a.Second, b.First, b.Second))))
                parent[Root(j)] = Root(i);
        int components = Enumerable.Range(0, paths.Count).Select(Root).Distinct().Count();
        if (components > 1) issues.Add("PATH_NETWORK_DISCONNECTED");

        foreach (var building in document.Buildings)
        {
            var points = building.Boundary.Vertices;
            var center = new WorldPoint(points.Average(p => p.X), points.Average(p => p.Y));
            double distance = paths.SelectMany(p => p.Geometry.Points.Zip(p.Geometry.Points.Skip(1)))
                .Select(segment => PointSegmentDistance(center, segment.First, segment.Second))
                .DefaultIfEmpty(double.PositiveInfinity).Min();
            if (distance > rules.MaximumBuildingPathDistanceMeters)
                issues.Add("BUILDING_PATH_ACCESS_TOO_FAR:" + building.ObjectId);
        }
        if (document.Entrances is { } entrances)
        {
            var access = new BuildingEntranceAccessValidator().Validate(document, entrances);
            issues.AddRange(access.Issues);
            issues.AddRange(new PedestrianAccessNetworkValidator().Validate(document));
            issues.AddRange(new ControlledPedestrianReachability().Validate(document,
                document.Junctions ?? []));
        }
        issues.AddRange(new PedestrianRoadCrossingValidator().Validate(document,
            document.RoadCrossings ?? []));
        issues.AddRange(new PedestrianCrossingSpanValidator().Validate(document,
            document.CrossingSpans ?? []));
        issues.AddRange(new CrossingRoadProfileValidator().Validate(document));
        issues.AddRange(new CrossingApproachValidator().Validate(document,
            document.CrossingApproaches ?? [], 0.02));
        if (document.Vegetation.Count < rules.MinimumVegetationCount)
            issues.Add("PLANTING_DENSITY_INSUFFICIENT");
        return new(ratio, components, document.Buildings.Count,
            document.Vegetation.Count, issues);
    }

    private static double PointSegmentDistance(WorldPoint p, WorldPoint a, WorldPoint b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double squared = dx * dx + dy * dy;
        if (squared <= 1e-15) return p.DistanceTo(a);
        double t = Math.Clamp(((p.X-a.X)*dx + (p.Y-a.Y)*dy)/squared, 0, 1);
        return p.DistanceTo(new(a.X + t*dx, a.Y + t*dy));
    }
    private static double Cross(WorldPoint a, WorldPoint b, WorldPoint c) =>
        (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
    private static bool SegmentsTouch(WorldPoint a, WorldPoint b, WorldPoint c, WorldPoint d)
    {
        const double e = 1e-9;
        var x1 = Cross(a,b,c); var x2 = Cross(a,b,d);
        var x3 = Cross(c,d,a); var x4 = Cross(c,d,b);
        static bool Between(WorldPoint p, WorldPoint q, WorldPoint r) =>
            q.X >= Math.Min(p.X,r.X)-1e-9 && q.X <= Math.Max(p.X,r.X)+1e-9 &&
            q.Y >= Math.Min(p.Y,r.Y)-1e-9 && q.Y <= Math.Max(p.Y,r.Y)+1e-9;
        return (x1*x2 < -e && x3*x4 < -e) ||
            (Math.Abs(x1)<=e && Between(a,c,b)) ||
            (Math.Abs(x2)<=e && Between(a,d,b)) ||
            (Math.Abs(x3)<=e && Between(c,a,d)) ||
            (Math.Abs(x4)<=e && Between(c,b,d));
    }
}
