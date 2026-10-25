using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Planning;

/// <summary>Approved GameArts visual layout interpreted into explicit authored anchors.</summary>
public sealed record VisualRegion(string Id, WorldPolygon Outline, string VisualRole);
public sealed record VisualAnchor(string Id, string RegionId, WorldPoint PositionMeters, string VisualRole);
public sealed record ApprovedVisualWorldBlueprint(
    int SchemaVersion, string MockupId, string ApprovedRevision, string ArtStyleId,
    IReadOnlyList<VisualRegion> Regions, IReadOnlyList<VisualAnchor> Anchors);

/// <summary>An intentional, traceable composition decision, not a placement search.</summary>
public sealed record VisualObjectDecision(
    string Id, string Kind, string AnchorId, string Purpose, string RuleId,
    string? ParentObjectId = null, string State = "idle", string? AnimationId = null);

public sealed record VisualWorldBuildResult(WorldDesignPlan? Design, IReadOnlyList<WorldDesignIssue> Issues)
{
    public bool IsValid => Design is not null && Issues.Count == 0;
}

/// <summary>
/// Compiles authored or engine-derived mockup anchors. Never invents missing anchor positions.
/// Uses explicit rounding to raster cells until continuous-object output is supported.
/// </summary>
public sealed class VisualWorldPlanner
{
    public VisualWorldBuildResult Plan(ApprovedVisualWorldBlueprint blueprint,
        IReadOnlyList<VisualObjectDecision> decisions, string planId,
        GridBounds bounds, double metersPerCell, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(decisions);
        token.ThrowIfCancellationRequested();
        var issues = new List<WorldDesignIssue>();
        void Fail(string code, string id, string message) => issues.Add(new(code,id,message));
        if (blueprint.SchemaVersion != 1 || string.IsNullOrWhiteSpace(blueprint.MockupId) ||
            string.IsNullOrWhiteSpace(blueprint.ApprovedRevision) ||
            string.IsNullOrWhiteSpace(blueprint.ArtStyleId))
            Fail("MOCKUP_APPROVAL_OR_STYLE_MISSING", planId, "Approved mockup identity and style are required.");
        if (string.IsNullOrWhiteSpace(planId) || bounds.Width <= 0 || bounds.Height <= 0 ||
            !double.IsFinite(metersPerCell) || metersPerCell <= 0)
            Fail("INVALID_VISUAL_WORLD_SETTINGS", planId, "Positive raster scale, bounds, and plan ID required.");
        var regions = new Dictionary<string, VisualRegion>(StringComparer.Ordinal);
        foreach (var region in blueprint.Regions ?? [])
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(region.Id) || !regions.TryAdd(region.Id, region))
                Fail("DUPLICATE_REGION", region.Id, "Visual region ID must be unique.");
            try { region.Outline.Validate(); }
            catch (ArgumentException) { Fail("INVALID_VISUAL_REGION", region.Id, "Invalid region polygon."); }
        }
        var anchors = new Dictionary<string, VisualAnchor>(StringComparer.Ordinal);
        foreach (var anchor in blueprint.Anchors ?? [])
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(anchor.Id) || !anchors.TryAdd(anchor.Id, anchor))
                Fail("DUPLICATE_ANCHOR", anchor.Id, "Visual anchor ID must be unique.");
            if (!regions.TryGetValue(anchor.RegionId, out var anchorRegion))
                Fail("ANCHOR_REGION_MISSING", anchor.Id, "Anchor must belong to a declared region.");
            else if (!issues.Any(i => i.Code == "INVALID_VISUAL_REGION") &&
                !anchorRegion.Outline.ContainsPoint(anchor.PositionMeters))
                Fail("ANCHOR_OUTSIDE_VISUAL_REGION", anchor.Id, "Anchor must stay within its authored parent region.");
            if (!double.IsFinite(anchor.PositionMeters.X) || !double.IsFinite(anchor.PositionMeters.Y))
                Fail("ANCHOR_POSITION_INVALID", anchor.Id, "Anchor coordinates must be finite.");
        }
        var planned = new List<PlannedWorldObject>();
        if (issues.Count != 0) return new(null, issues);
        foreach (var decision in decisions)
        {
            token.ThrowIfCancellationRequested();
            if (!anchors.TryGetValue(decision.AnchorId, out var anchor))
            {
                Fail("VISUAL_ANCHOR_MISSING", decision.Id, "An explicit approved-mockup anchor is required.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(decision.Purpose) || string.IsNullOrWhiteSpace(decision.RuleId))
            {
                Fail("VISUAL_PURPOSE_MISSING", decision.Id, "A visual planning rule and purpose are required.");
                continue;
            }
            var x = anchor.PositionMeters.X / metersPerCell;
            var y = anchor.PositionMeters.Y / metersPerCell;
            if (!double.IsFinite(x) || !double.IsFinite(y) ||
                x < 0 || y < 0 || x >= bounds.Width || y >= bounds.Height)
            {
                Fail("VISUAL_ANCHOR_OUTSIDE_WORLD", decision.Id, "Anchor is outside the world raster.");
                continue;
            }
            planned.Add(new(decision.Id, decision.Kind,
                new GridPoint((int)Math.Floor(x), (int)Math.Floor(y)),
                decision.Purpose, decision.RuleId, planId, decision.ParentObjectId,
                decision.State, decision.AnimationId,
                new VisualPlacementSource(blueprint.MockupId, blueprint.ApprovedRevision,
                    blueprint.ArtStyleId, anchor.RegionId, decision.RuleId, anchor.PositionMeters)));
        }
        if (issues.Count != 0) return new(null, issues);
        var design = new WorldDesignPlan(planId, bounds, planned);
        issues.AddRange(new WorldDesignValidator().Validate(design,token).Issues);
        return issues.Count == 0 ? new(design, issues) : new(null, issues);
    }
}
