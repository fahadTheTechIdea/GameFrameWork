namespace GameFrameWork.Core.Planning;

/// <summary>One independently validated design discipline, not a generator or rendering layer.</summary>
public sealed record DesignDiscipline(string Id, IReadOnlyList<string> DependsOn);

public sealed record DesignCoordinationResult(
    IReadOnlyList<string> ExecutionOrder,
    IReadOnlyList<WorldDesignIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

/// <summary>
/// Validates dependencies and produces a stable topological execution order.
/// Never silently invents missing plans or breaks cycles to make a world build pass.
/// </summary>
public sealed class WorldPlanCoordinator
{
    public DesignCoordinationResult Coordinate(IReadOnlyList<DesignDiscipline> disciplines)
    {
        ArgumentNullException.ThrowIfNull(disciplines);
        var issues = new List<WorldDesignIssue>();
        var indexed = new Dictionary<string, DesignDiscipline>(StringComparer.Ordinal);
        foreach (var plan in disciplines)
        {
            if (plan is null || string.IsNullOrWhiteSpace(plan.Id))
            {
                issues.Add(new("INVALID_DESIGN_DISCIPLINE", "", "Plan ID is required."));
                continue;
            }
            if (!indexed.TryAdd(plan.Id, plan))
                issues.Add(new("DUPLICATE_DISCIPLINE", plan.Id, "Design IDs must be unique."));
        }
        var degree = indexed.Keys.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        var downstream = indexed.Keys.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var (id, plan) in indexed)
        {
            var dependencies = plan.DependsOn;
            if (dependencies is null)
            {
                issues.Add(new("DEPENDENCIES_MISSING", id, "Dependencies must be specified."));
                continue;
            }
            foreach (var dep in dependencies.Distinct(StringComparer.Ordinal))
            {
                if (!indexed.ContainsKey(dep))
                {
                    issues.Add(new("DESIGN_DEPENDENCY_MISSING", id, $"Required design {dep} is absent."));
                    continue;
                }
                degree[id]++;
                downstream[dep].Add(id);
            }
        }
        if (issues.Count != 0) return new(Array.Empty<string>(), issues);
        var ready = new SortedSet<string>(degree.Where(d => d.Value == 0).Select(d => d.Key), StringComparer.Ordinal);
        var ordered = new List<string>();
        while (ready.Count > 0)
        {
            var current = ready.Min!;
            ready.Remove(current);
            ordered.Add(current);
            foreach (var next in downstream[current])
                if (--degree[next] == 0) ready.Add(next);
        }
        if (ordered.Count != indexed.Count)
            issues.Add(new("DESIGN_DEPENDENCY_CYCLE", "", "Circular design dependencies prevent construction."));
        return issues.Count == 0 ? new(ordered, issues) : new(Array.Empty<string>(), issues);
    }
}
