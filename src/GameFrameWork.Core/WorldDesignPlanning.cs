using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Planning;

/// <summary>
/// Every constructed world object must originate from a validated design plan.
/// Grid coordinates are canonical for this first raster compiler; continuous polygon
/// footprints will be a separate adapter without weakening the provenance gate.
/// </summary>
public sealed record PlannedWorldObject(
    string Id, string Kind, GridPoint Site, string Purpose,
    string DesignRuleId, string SourcePlanId, string? ParentObjectId = null,
    string State = "idle", string? AnimationId = null);

public sealed record WorldDesignPlan(
    string Id, GridBounds Bounds, IReadOnlyList<PlannedWorldObject> Objects);

public sealed record WorldDesignIssue(string Code, string ObjectId, string Message);

public sealed record WorldDesignValidation(IReadOnlyList<WorldDesignIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

/// <summary>
/// Fails closed: missing rationale, invalid parent references, cycles, overlapping
/// independent objects and out-of-bounds geometry all prevent construction.
/// Components may share their parent's site; unrelated roots cannot.
/// </summary>
public sealed class WorldDesignValidator
{
    public WorldDesignValidation Validate(WorldDesignPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var issues = new List<WorldDesignIssue>();
        void Fail(string code, string id, string message) => issues.Add(new(code, id, message));
        if (string.IsNullOrWhiteSpace(plan.Id)) Fail("PLAN_ID_MISSING", "", "A design plan must have an ID.");
        if (plan.Bounds.Width <= 0 || plan.Bounds.Height <= 0)
            Fail("INVALID_PLAN_BOUNDS", plan.Id, "Plan bounds must be positive.");
        if (plan.Objects is null)
        {
            Fail("OBJECTS_MISSING", plan.Id, "Objects collection is required.");
            return new(issues);
        }
        var byId = new Dictionary<string, PlannedWorldObject>(StringComparer.Ordinal);
        foreach (var item in plan.Objects)
        {
            if (item is null)
            {
                Fail("NULL_OBJECT", plan.Id, "Null plan entry.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(item.Id) || !byId.TryAdd(item.Id, item))
                Fail("DUPLICATE_OR_EMPTY_ID", item.Id, "Object IDs must be unique and nonempty.");
            if (string.IsNullOrWhiteSpace(item.Kind) ||
                string.IsNullOrWhiteSpace(item.Purpose) ||
                string.IsNullOrWhiteSpace(item.DesignRuleId) ||
                item.SourcePlanId != plan.Id)
                Fail("DESIGN_PROVENANCE_MISSING", item.Id, "Kind, rationale, rule and matching source plan are required.");
            if (!plan.Bounds.Contains(item.Site))
                Fail("OUTSIDE_DESIGN_BOUNDS", item.Id, "Site must lie within plan bounds.");
        }
        var roots = new HashSet<GridPoint>();
        foreach (var item in byId.Values)
        {
            if (item.ParentObjectId is null)
            {
                if (!roots.Add(item.Site))
                    Fail("ROOT_FOOTPRINT_CONFLICT", item.Id, "Independent objects cannot share a cell.");
                continue;
            }
            if (!byId.TryGetValue(item.ParentObjectId, out var parent))
            {
                Fail("PARENT_NOT_FOUND", item.Id, "Component parent does not exist.");
                continue;
            }
            if (parent.Site != item.Site)
                Fail("ATTACHMENT_SITE_MISMATCH", item.Id, "Raster component must share its parent's anchor cell.");
            var visited = new HashSet<string>(StringComparer.Ordinal) { item.Id };
            var cursor = item;
            while (cursor.ParentObjectId is { } id && byId.TryGetValue(id, out var next))
            {
                if (!visited.Add(id))
                {
                    Fail("PARENT_CYCLE", item.Id, "Attachment hierarchy must be acyclic.");
                    break;
                }
                cursor = next;
            }
        }
        return new(issues);
    }
}

public sealed class WorldPlanCompiler(WorldDesignValidator? validator = null)
{
    private readonly WorldDesignValidator _validator = validator ?? new();

    public IReadOnlyList<WorldObject> Compile(WorldDesignPlan plan)
    {
        var result = _validator.Validate(plan);
        if (!result.IsValid)
            throw new WorldPlanException(result.Issues);
        return plan.Objects.Select(item =>
            new WorldObject(item.Id, item.Kind, item.Site.X, item.Site.Y, item.State, item.AnimationId))
            .ToArray();
    }
}

public sealed class WorldPlanException(IReadOnlyList<WorldDesignIssue> issues)
    : InvalidOperationException("World construction blocked by invalid design plan: " +
        string.Join("; ", issues.Select(x => x.Code + ":" + x.ObjectId)))
{
    public IReadOnlyList<WorldDesignIssue> Issues { get; } = issues;
}
