using GameFrameWork.Core.Geometry;
namespace GameFrameWork.Core.Planning;

/// <summary>A named composition path extracted from an approved GameArts mockup.</summary>
public sealed record VisualDesignPath(string Id, string RegionId, WorldPolyline Centerline, string Role);
public sealed record PathPattern(string Id, string PathId, string Kind, string Purpose,
    string RuleId, double SpacingMeters, double StartInsetMeters = 0, double EndInsetMeters = 0);
public sealed record PathPatternResult(IReadOnlyList<VisualAnchor> Anchors,
    IReadOnlyList<VisualObjectDecision> Decisions, IReadOnlyList<WorldDesignIssue> Issues);

/// <summary>
/// Samples approved paths by arc length with explicit endpoint margins.
/// Locations are derived from the authored path and grammar; no empty-space sampling.
/// </summary>
public sealed class VisualPathPatternGenerator
{
    public PathPatternResult Generate(ApprovedVisualWorldBlueprint blueprint,
        IReadOnlyList<VisualDesignPath> paths, PathPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(pattern);
        PathPatternResult Reject(string code, string message) =>
            new([], [], [new(code, pattern.Id, message)]);
        if (string.IsNullOrWhiteSpace(pattern.Id) || string.IsNullOrWhiteSpace(pattern.PathId) ||
            string.IsNullOrWhiteSpace(pattern.Kind) || string.IsNullOrWhiteSpace(pattern.Purpose) ||
            string.IsNullOrWhiteSpace(pattern.RuleId) || !double.IsFinite(pattern.SpacingMeters) ||
            pattern.SpacingMeters <= 0 || !double.IsFinite(pattern.StartInsetMeters) ||
            pattern.StartInsetMeters < 0 || !double.IsFinite(pattern.EndInsetMeters) ||
            pattern.EndInsetMeters < 0)
            return Reject("INVALID_PATH_PATTERN", "A valid path, design rule and positive spacing are required.");
        var path = paths.SingleOrDefault(p => p.Id == pattern.PathId);
        if (path is null) return Reject("VISUAL_PATH_MISSING", "Approved visual path is absent.");
        if (!blueprint.Regions.Any(r => r.Id == path.RegionId))
            return Reject("PATH_REGION_MISSING", "Path must belong to an approved visual region.");
        try { path.Centerline.Validate(); }
        catch (ArgumentException) { return Reject("INVALID_VISUAL_PATH", "Path geometry is invalid."); }
        var total = path.Centerline.LengthMeters();
        if (pattern.StartInsetMeters + pattern.EndInsetMeters > total)
            return Reject("PATH_TOO_SHORT", "Requested insets exceed the authored path.");
        var available = total - pattern.StartInsetMeters - pattern.EndInsetMeters;
        var count = 1 + (int)Math.Floor(available / pattern.SpacingMeters);
        if (count > 100000) return Reject("PATH_PATTERN_TOO_LARGE", "Requested count exceeds safety limit.");
        var anchors = new List<VisualAnchor>(count);
        var decisions = new List<VisualObjectDecision>(count);
        for (var i = 0; i < count; i++)
        {
            double distance = pattern.StartInsetMeters + i * pattern.SpacingMeters;
            var point = Sample(path.Centerline.Points, distance);
            var id = $"{pattern.Id}-{i}";
            anchors.Add(new(id, path.RegionId, point, path.Role));
            decisions.Add(new(id, pattern.Kind, id, pattern.Purpose, pattern.RuleId));
        }
        return new(anchors, decisions, []);
    }
    private static WorldPoint Sample(IReadOnlyList<WorldPoint> points, double distance)
    {
        for (int i = 1; i < points.Count; i++)
        {
            var a = points[i - 1]; var b = points[i];
            double segment = a.DistanceTo(b);
            if (distance <= segment || i == points.Count - 1)
            {
                var t = Math.Clamp(distance / segment, 0, 1);
                return new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
            }
            distance -= segment;
        }
        throw new InvalidOperationException("Validated path could not be sampled.");
    }
}
