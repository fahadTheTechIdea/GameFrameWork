using GameFrameWork.Core;
using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Worlds;

/// <summary>Approved, repeatable transport design input for a settlement.
/// Crossings are authored decisions, never sampled from empty map locations.</summary>
public sealed record SettlementTransportSpecification(
    string DesignId, string VisualMockupId, string VisualRevision,
    IReadOnlyList<SettlementRoadPolicy> RoadPolicies,
    IReadOnlyList<SettlementCrossingIntent> CrossingIntents);

public sealed record SettlementTransportDesignResult(
    SettlementCrossingPlan? Crossings, IReadOnlyList<string> Issues)
{
    public bool Valid => Crossings is { Valid: true } && Issues.Count == 0;
}

/// <summary>Single transport design entry point with provenance and policy validation.</summary>
public sealed class SettlementTransportDesignBuilder
{
    public SettlementTransportDesignResult Build(SemanticWorldPackage world,
        SettlementDesignResult settlement, SettlementTransportSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(specification);
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(specification.DesignId) ||
            string.IsNullOrWhiteSpace(specification.VisualMockupId) ||
            string.IsNullOrWhiteSpace(specification.VisualRevision) ||
            specification.VisualMockupId != world.Base.MockupId ||
            specification.VisualRevision != world.Base.MockupRevision)
            issues.Add("SETTLEMENT_TRANSPORT_PROVENANCE_MISMATCH");
        if (specification.RoadPolicies is null || specification.CrossingIntents is null)
            issues.Add("SETTLEMENT_TRANSPORT_RULES_MISSING");
        if (issues.Count != 0) return new(null, issues);
        var result = new SettlementTransportCrossingCoordinator().Build(
            world, settlement, specification.CrossingIntents!, specification.RoadPolicies!);
        return result.Valid
            ? new(result.Plan, [])
            : new(null, result.Issues);
    }
}
