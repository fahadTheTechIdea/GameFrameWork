namespace WorldCreator.Core.Planning;

/// <summary>
/// Composes GameArts-authored regions/paths with explicit procedural grammars.
/// No scene object is produced without a mockup source or a validated pattern.
/// </summary>
public sealed record VisualCompositionRequest(
    ApprovedVisualWorldBlueprint Blueprint,
    IReadOnlyList<RegionPattern> RegionPatterns,
    IReadOnlyList<VisualDesignPath> Paths,
    IReadOnlyList<PathPattern> PathPatterns,
    IReadOnlyList<VisualObjectDecision> ExplicitObjects);

public sealed record VisualCompositionResult(
    WorldDesignPlan? Design,
    IReadOnlyList<WorldDesignIssue> Issues)
{
    public bool IsValid => Design is not null && Issues.Count == 0;
}

public sealed class VisualCompositionGenerator
{
    public VisualCompositionResult Generate(
        VisualCompositionRequest request, string planId,
        GridBounds bounds, double metersPerCell)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Blueprint);
        var issues = new List<WorldDesignIssue>();
        var anchors = new List<VisualAnchor>(request.Blueprint.Anchors);
        var decisions = new List<VisualObjectDecision>(request.ExplicitObjects);
        var regionGenerator = new VisualRegionPatternGenerator();
        foreach (var pattern in request.RegionPatterns.OrderBy(p => p.Id, StringComparer.Ordinal))
        {
            var result = regionGenerator.Generate(request.Blueprint, pattern);
            issues.AddRange(result.Issues);
            anchors.AddRange(result.GeneratedAnchors);
            decisions.AddRange(result.Decisions);
        }
        var pathGenerator = new VisualPathPatternGenerator();
        foreach (var pattern in request.PathPatterns.OrderBy(p => p.Id, StringComparer.Ordinal))
        {
            var result = pathGenerator.Generate(request.Blueprint, request.Paths, pattern);
            issues.AddRange(result.Issues);
            anchors.AddRange(result.Anchors);
            decisions.AddRange(result.Decisions);
        }
        if (issues.Count > 0) return new(null, issues);
        var duplicateAnchor = anchors.GroupBy(a => a.Id, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateAnchor is not null)
            return new(null, [new("COMPOSITION_ANCHOR_COLLISION", duplicateAnchor.Key,
                "Authored and generated anchor IDs must not collide.")]);
        var duplicateObject = decisions.GroupBy(d => d.Id, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateObject is not null)
            return new(null, [new("COMPOSITION_OBJECT_COLLISION", duplicateObject.Key,
                "Each design object must have a distinct identity.")]);
        var withAnchors = request.Blueprint with { Anchors = anchors };
        var planned = new VisualWorldPlanner().Plan(withAnchors, decisions, planId, bounds, metersPerCell);
        return new(planned.Design, planned.Issues);
    }
}
