using WorldCreator.Core;

namespace WorldCreator.Worlds;

public enum SettlementRoadClass { Local, Collector, Arterial }
public sealed record SettlementRoadPolicy(string RoadPathId, SettlementRoadClass RoadClass,
    bool PedestrianCrossingsAllowed, bool RequiresControlledCrossing);
public sealed record SettlementCrossingPolicyResult(SettlementCrossingPlan? Plan,
    IReadOnlyList<string> Issues)
{
    public bool Valid => Plan is { Valid: true } && Issues.Count == 0;
}

/// <summary>
/// Applies explicit transport policy to authored crossing decisions before invoking
/// the existing settlement geometry integration. Never adds or repositions crossings.
/// </summary>
public sealed class SettlementTransportCrossingCoordinator
{
    public SettlementCrossingPolicyResult Build(
        SemanticWorldPackage world,
        SettlementDesignResult settlement,
        IReadOnlyList<SettlementCrossingIntent> intents,
        IReadOnlyList<SettlementRoadPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(intents);
        ArgumentNullException.ThrowIfNull(policies);
        var issues = new List<string>();
        var byRoad = new Dictionary<string, SettlementRoadPolicy>(StringComparer.Ordinal);
        foreach (var policy in policies)
        {
            if (policy is null || string.IsNullOrWhiteSpace(policy.RoadPathId) ||
                !Enum.IsDefined(policy.RoadClass) || !byRoad.TryAdd(policy.RoadPathId, policy))
                issues.Add("ROAD_CROSSING_POLICY_INVALID:" + policy?.RoadPathId);
        }
        var knownStreets = settlement.Streets.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var policy in byRoad.Values)
            if (!knownStreets.Contains(policy.RoadPathId))
                issues.Add("ROAD_CROSSING_POLICY_UNKNOWN_STREET:" + policy.RoadPathId);
        foreach (var intent in intents)
        {
            if (!byRoad.TryGetValue(intent.RoadPathId, out var policy))
            {
                issues.Add("ROAD_CROSSING_POLICY_MISSING:" + intent.Id);
                continue;
            }
            if (!policy.PedestrianCrossingsAllowed)
                issues.Add("ROAD_CROSSING_PROHIBITED_BY_POLICY:" + intent.Id);
            if (policy.RequiresControlledCrossing && (!intent.Permitted || !intent.Accessible))
                issues.Add("ROAD_CONTROLLED_CROSSING_NOT_READY:" + intent.Id);
        }
        if (issues.Count > 0) return new(null, issues);
        var plan = new SettlementCrossingIntegration().Build(world, settlement, intents);
        return plan.Valid ? new(plan, []) : new(null, plan.Issues);
    }
}
