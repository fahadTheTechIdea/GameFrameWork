using GameFrameWork.Core;
using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Worlds;

/// <summary>Combines an existing semantic world with its approved settlement crossing design.
/// The resulting portable document is assembled only after transport quality rules pass.</summary>
public sealed class SettlementTransportWorldAssembler
{
    public (DummyVillageWorldDocument? World, IReadOnlyList<string> Issues) Assemble(
        DummyVillageWorldDocument world,
        SettlementDesignResult settlement,
        SettlementTransportSpecification specification,
        WorldQualityRules qualityRules)
    {
        ArgumentNullException.ThrowIfNull(world);
        var transport = new SettlementTransportDesignBuilder().Build(
            world.SemanticWorld, settlement, specification);
        if (!transport.Valid) return (null, transport.Issues);
        var crossings = transport.Crossings!.Crossings;
        var integrated = world with
        {
            RoadCrossings = crossings.Select(x => x.Crossing).ToArray(),
            CrossingSpans = crossings.Select(x => x.Span).ToArray(),
            CrossingApproaches = crossings.Select(x => x.Approaches).ToArray(),
            RoadProfiles = settlement.Streets.OrderBy(s => s.Id, StringComparer.Ordinal)
                .Select(s => new NamedRoadProfile(s.Id, s.Profile)).ToArray()
        };
        var quality = new WorldQualityAnalyzer().Analyze(integrated, qualityRules);
        return quality.MeetsRules
            ? (integrated, Array.Empty<string>())
            : (null, quality.Issues);
    }
}
