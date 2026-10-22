using GameFrameWork.Core;
using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Worlds;

/// <summary>Settlement transport integration for authored pedestrian crossing decisions.
/// Requires real street corridors and matching semantic road paths; never synthesizes
/// missing paths or placement locations.</summary>
public sealed class SettlementCrossingIntegration
{
    public SettlementCrossingPlan Build(SemanticWorldPackage world,
        SettlementDesignResult settlement,
        IReadOnlyList<SettlementCrossingIntent> intents)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(intents);
        if (settlement.Diagnostics.Count != 0)
            return new([], settlement.Diagnostics.Select(x => "SETTLEMENT_NOT_READY:" + x).ToArray());

        var errors = new List<string>();
        var roadPaths = world.Paths.Where(p => p.Kind == "road")
            .ToDictionary(p => p.Id, StringComparer.Ordinal);
        var profiles = new List<NamedRoadProfile>();
        foreach (var street in settlement.Streets.OrderBy(s => s.Id, StringComparer.Ordinal))
        {
            if (!roadPaths.TryGetValue(street.Id, out var path) ||
                path.Geometry.Points.Count != street.Centerline.Points.Count ||
                !path.Geometry.Points.SequenceEqual(street.Centerline.Points))
            {
                errors.Add("STREET_SEMANTIC_GEOMETRY_MISMATCH:" + street.Id);
                continue;
            }
            profiles.Add(new(street.Id, street.Profile));
        }
        if (errors.Count != 0) return new([], errors);
        return new SettlementCrossingPlanner().Plan(world, profiles, intents);
    }
}
