using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Planning;

/// <summary>Deliberate placement pattern within an approved visual region, never a free-space search.</summary>
public sealed record RegionPattern(string Id, string RegionId, string Kind, string Purpose,
    double SpacingMeters, double MarginMeters, string RuleId);

public sealed record RegionPatternResult(IReadOnlyList<VisualObjectDecision> Decisions,
    IReadOnlyList<VisualAnchor> GeneratedAnchors, IReadOnlyList<WorldDesignIssue> Issues);

/// <summary>
/// First supported grammar: orderly row of repeating props along an axis-aligned
/// rectangular region's southern edge, inset by a specified design margin.
/// Suitable for fences/hedges/lamps of a styled game scene; not arbitrary scattering.
/// </summary>
public sealed class VisualRegionPatternGenerator
{
    public RegionPatternResult Generate(ApprovedVisualWorldBlueprint blueprint, RegionPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(pattern);
        var issues = new List<WorldDesignIssue>();
        if (!double.IsFinite(pattern.SpacingMeters) || pattern.SpacingMeters <= 0 ||
            !double.IsFinite(pattern.MarginMeters) || pattern.MarginMeters < 0 ||
            string.IsNullOrWhiteSpace(pattern.Id) || string.IsNullOrWhiteSpace(pattern.Kind) ||
            string.IsNullOrWhiteSpace(pattern.Purpose) || string.IsNullOrWhiteSpace(pattern.RuleId))
            return new([], [], [new("PATTERN_INVALID", pattern.Id, "Pattern needs a purpose, rule, and valid spacing/margin.")]);
        var region = blueprint.Regions.FirstOrDefault(r => r.Id == pattern.RegionId);
        if (region is null)
            return new([], [], [new("REGION_MISSING", pattern.Id, "Approved visual region not found.")]);
        try { region.Outline.Validate(); }
        catch (ArgumentException)
        {
            return new([], [], [new("REGION_INVALID", pattern.Id, "Region polygon invalid.")]);
        }
        var points = region.Outline.Vertices;
        var xs = points.Select(p => p.X).Distinct().Order().ToArray();
        var ys = points.Select(p => p.Y).Distinct().Order().ToArray();
        if (points.Count != 4 || xs.Length != 2 || ys.Length != 2)
            return new([], [], [new("UNSUPPORTED_REGION_SHAPE", pattern.Id, "This pattern supports rectangles only.")]);
        double left = xs[0] + pattern.MarginMeters;
        double right = xs[1] - pattern.MarginMeters;
        double y = ys[1] - pattern.MarginMeters;
        if (left > right || y < ys[0])
            return new([], [], [new("REGION_TOO_SMALL", pattern.Id, "Pattern margins exceed available area.")]);
        int count = 1 + (int)Math.Floor((right - left) / pattern.SpacingMeters);
        if (count > 100_000)
            return new([], [], [new("PATTERN_TOO_LARGE", pattern.Id, "Pattern exceeds configured safety limit.")]);
        var anchors = new List<VisualAnchor>();
        var decisions = new List<VisualObjectDecision>();
        for (int i = 0; i < count; i++)
        {
            string id = $"{pattern.Id}-{i}";
            anchors.Add(new(id, region.Id, new(left + i * pattern.SpacingMeters, y), pattern.Kind));
            decisions.Add(new(id, pattern.Kind, id, pattern.Purpose, pattern.RuleId));
        }
        return new(decisions, anchors, issues);
    }
}
