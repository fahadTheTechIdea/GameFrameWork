using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core;

/// <summary>Explicit pedestrian sidewalk approaches, curb accessibility and connection
/// to the authored crossing, measured in world meters.</summary>
public sealed record CrossingApproach(string CrossingId,
    WorldPoint LeftSidewalkPoint, WorldPoint RightSidewalkPoint,
    double LeftCurbRiseMeters, double RightCurbRiseMeters,
    bool LeftTransitionAccessible, bool RightTransitionAccessible);

public sealed class CrossingApproachValidator
{
    private const double Epsilon = 1e-5;
    public IReadOnlyList<string> Validate(DummyVillageWorldDocument world,
        IReadOnlyList<CrossingApproach> approaches, double maximumCurbRiseMeters)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(approaches);
        if (!double.IsFinite(maximumCurbRiseMeters) || maximumCurbRiseMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumCurbRiseMeters));
        var issues = new List<string>();
        var spans = (world.CrossingSpans ?? []).ToDictionary(x => x.CrossingId, StringComparer.Ordinal);
        var crossings = (world.RoadCrossings ?? []).ToDictionary(x => x.Id, StringComparer.Ordinal);
        var profiles = (world.RoadProfiles ?? []).ToDictionary(x => x.PathId, StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var approach in approaches)
        {
            if (approach is null || string.IsNullOrWhiteSpace(approach.CrossingId) ||
                !used.Add(approach.CrossingId) ||
                !spans.TryGetValue(approach.CrossingId, out var span) ||
                !crossings.TryGetValue(approach.CrossingId, out var crossing) ||
                !profiles.TryGetValue(crossing.RoadPathId, out var profile))
            {
                issues.Add("CROSSING_APPROACH_INVALID:" + approach?.CrossingId);
                continue;
            }
            if (!double.IsFinite(approach.LeftCurbRiseMeters) ||
                !double.IsFinite(approach.RightCurbRiseMeters) ||
                approach.LeftCurbRiseMeters < 0 || approach.RightCurbRiseMeters < 0 ||
                approach.LeftCurbRiseMeters > maximumCurbRiseMeters ||
                approach.RightCurbRiseMeters > maximumCurbRiseMeters ||
                !approach.LeftTransitionAccessible || !approach.RightTransitionAccessible)
                issues.Add("CROSSING_CURB_TRANSITION_INACCESSIBLE:" + approach.CrossingId);
            double dx = span.SecondCurb.X - span.FirstCurb.X;
            double dy = span.SecondCurb.Y - span.FirstCurb.Y;
            double length = Math.Sqrt(dx*dx + dy*dy);
            if (!double.IsFinite(length) || length <= Epsilon ||
                !double.IsFinite(profile.Section.SidewalkLeftMeters) ||
                !double.IsFinite(profile.Section.SidewalkRightMeters))
            {
                issues.Add("CROSSING_APPROACH_GEOMETRY_INVALID:" + approach.CrossingId);
                continue;
            }
            var expectedLeft = span.FirstCurb.Offset(
                -dx/length*profile.Section.SidewalkLeftMeters, -dy/length*profile.Section.SidewalkLeftMeters);
            var expectedRight = span.SecondCurb.Offset(
                dx/length*profile.Section.SidewalkRightMeters, dy/length*profile.Section.SidewalkRightMeters);
            if (!double.IsFinite(approach.LeftSidewalkPoint.X) ||
                !double.IsFinite(approach.LeftSidewalkPoint.Y) ||
                !double.IsFinite(approach.RightSidewalkPoint.X) ||
                !double.IsFinite(approach.RightSidewalkPoint.Y) ||
                approach.LeftSidewalkPoint.DistanceTo(expectedLeft) > Epsilon ||
                approach.RightSidewalkPoint.DistanceTo(expectedRight) > Epsilon)
                issues.Add("CROSSING_SIDEWALK_ENDPOINT_MISMATCH:" + approach.CrossingId);
        }
        foreach (var span in spans.Values)
            if (!used.Contains(span.CrossingId))
                issues.Add("CROSSING_APPROACH_MISSING:" + span.CrossingId);
        return issues;
    }
}
