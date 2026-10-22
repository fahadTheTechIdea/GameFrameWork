using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core;

/// <summary>An authored crossing intent anchored to a named straight road segment.</summary>
public sealed record SettlementCrossingIntent(string Id, string PedestrianPathId,
    string RoadPathId, WorldPoint CenterMeters, bool Permitted, bool Accessible,
    double LeftCurbRiseMeters, double RightCurbRiseMeters);

public sealed record PlannedSettlementCrossing(PedestrianRoadCrossing Crossing,
    PedestrianCrossingSpan Span, CrossingApproach Approaches);

public sealed record SettlementCrossingPlan(IReadOnlyList<PlannedSettlementCrossing> Crossings,
    IReadOnlyList<string> Issues)
{
    public bool Valid => Issues.Count == 0;
}

/// <summary>Derives actual crossing/curb/sidewalk geometry from named road profiles.
/// Authored crossing centers are required. Curved roads and absent pedestrian
/// intersections fail explicitly rather than guessing a placement.</summary>
public sealed class SettlementCrossingPlanner
{
    private const double Epsilon = 1e-6;
    public SettlementCrossingPlan Plan(SemanticWorldPackage world,
        IReadOnlyList<NamedRoadProfile> profiles,
        IReadOnlyList<SettlementCrossingIntent> intents)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(intents);
        var issues = new List<string>();
        var output = new List<PlannedSettlementCrossing>();
        var paths = world.Paths.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var roadProfiles = new Dictionary<string, RoadCrossSection>(StringComparer.Ordinal);
        foreach (var item in profiles)
        {
            if (!roadProfiles.TryAdd(item.PathId, item.Section))
                issues.Add("DUPLICATE_ROAD_PROFILE:" + item.PathId);
            else
                try { item.Section.Validate(); }
                catch (ArgumentException) { issues.Add("INVALID_ROAD_PROFILE:" + item.PathId); }
        }
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var intent in intents.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(intent.Id) || !used.Add(intent.Id) ||
                !paths.TryGetValue(intent.RoadPathId, out var road) || road.Kind != "road" ||
                !paths.TryGetValue(intent.PedestrianPathId, out var pedestrian) ||
                pedestrian.Kind is not ("footpath" or "pedestrian" or "walkway") ||
                !roadProfiles.TryGetValue(intent.RoadPathId, out var section) ||
                !double.IsFinite(intent.CenterMeters.X) || !double.IsFinite(intent.CenterMeters.Y))
            {
                issues.Add("CROSSING_INTENT_INVALID:" + intent.Id);
                continue;
            }
            var segments = road.Geometry.Points.Zip(road.Geometry.Points.Skip(1))
                .Where(s => OnSegment(intent.CenterMeters, s.First, s.Second)).ToArray();
            if (segments.Length != 1 || !pedestrian.Geometry.Points.Zip(pedestrian.Geometry.Points.Skip(1))
                    .Any(s => OnSegment(intent.CenterMeters, s.First, s.Second)))
            {
                issues.Add("CROSSING_CENTER_NOT_AT_PATH_INTERSECTION:" + intent.Id);
                continue;
            }
            var a = segments[0].First; var b = segments[0].Second;
            double dx = b.X-a.X, dy = b.Y-a.Y;
            double length = Math.Sqrt(dx*dx+dy*dy);
            double half = (section.LaneWidthMeters*section.LaneCount + section.MedianMeters +
                section.CurbLeftMeters + section.CurbRightMeters)/2;
            var first = intent.CenterMeters.Offset(dy/length*half, -dx/length*half);
            var second = intent.CenterMeters.Offset(-dy/length*half, dx/length*half);
            var firstSidewalk = first.Offset(dy/length*section.SidewalkLeftMeters,
                -dx/length*section.SidewalkLeftMeters);
            var secondSidewalk = second.Offset(-dy/length*section.SidewalkRightMeters,
                dx/length*section.SidewalkRightMeters);
            output.Add(new(
                new(intent.Id, intent.PedestrianPathId, intent.RoadPathId, intent.CenterMeters,
                    intent.Permitted, intent.Accessible),
                new(intent.Id, first, second, 2*half, false),
                new(intent.Id, firstSidewalk, secondSidewalk,
                    intent.LeftCurbRiseMeters, intent.RightCurbRiseMeters,
                    intent.Accessible, intent.Accessible)));
        }
        return issues.Count == 0 ? new(output, []) : new([], issues);
    }
    private static bool OnSegment(WorldPoint p, WorldPoint a, WorldPoint b)
    {
        double cross=(b.X-a.X)*(p.Y-a.Y)-(b.Y-a.Y)*(p.X-a.X);
        return Math.Abs(cross)<=Epsilon &&
            p.X>=Math.Min(a.X,b.X)-Epsilon && p.X<=Math.Max(a.X,b.X)+Epsilon &&
            p.Y>=Math.Min(a.Y,b.Y)-Epsilon && p.Y<=Math.Max(a.Y,b.Y)+Epsilon;
    }
}
