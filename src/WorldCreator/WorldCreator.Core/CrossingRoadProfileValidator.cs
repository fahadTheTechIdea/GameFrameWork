using WorldCreator.Core.Geometry;

namespace WorldCreator.Core;

/// <summary>Associates a planned road with its authored physical cross-section.</summary>
public sealed record NamedRoadProfile(string PathId, RoadCrossSection Section);

/// <summary>Checks curb-to-curb crossings against actual lane, median, curb and
/// sidewalk dimensions instead of accepting an independent invented span width.</summary>
public sealed class CrossingRoadProfileValidator
{
    private const double Tolerance = 1e-5;

    public IReadOnlyList<string> Validate(DummyVillageWorldDocument world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var issues = new List<string>();
        var profiles = new Dictionary<string, RoadCrossSection>(StringComparer.Ordinal);
        foreach (var item in world.RoadProfiles ?? [])
        {
            if (item is null || string.IsNullOrWhiteSpace(item.PathId) ||
                !profiles.TryAdd(item.PathId, item.Section))
            {
                issues.Add("ROAD_PROFILE_DUPLICATE_OR_INVALID:" + item?.PathId);
                continue;
            }
            try { item.Section.Validate(); }
            catch (ArgumentException) { issues.Add("ROAD_PROFILE_INVALID:" + item.PathId); }
        }
        var crossings = (world.RoadCrossings ?? [])
            .ToDictionary(c => c.Id, StringComparer.Ordinal);
        foreach (var span in world.CrossingSpans ?? [])
        {
            if (!crossings.TryGetValue(span.CrossingId, out var crossing) ||
                !profiles.TryGetValue(crossing.RoadPathId, out var section))
            {
                issues.Add("CROSSING_ROAD_PROFILE_MISSING:" + span.CrossingId);
                continue;
            }
            // Sidewalks sit beyond the curbs. Curb-to-curb distance includes both
            // curb widths, traveled lanes and median, but excludes sidewalks.
            double curbSpan = section.LaneWidthMeters * section.LaneCount +
                section.MedianMeters + section.CurbLeftMeters + section.CurbRightMeters;
            if (!double.IsFinite(curbSpan) || Math.Abs(curbSpan - span.RoadWidthMeters) > Tolerance)
                issues.Add("CROSSING_ROAD_PROFILE_WIDTH_MISMATCH:" + span.CrossingId);
            if (section.SidewalkLeftMeters <= 0 || section.SidewalkRightMeters <= 0)
                issues.Add("CROSSING_SIDEWALK_CONNECTION_MISSING:" + span.CrossingId);
        }
        return issues;
    }
}
