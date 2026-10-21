using GameFrameWork.Core;
namespace GameFrameWork.Worlds;
/// <summary>First deterministic settlement topology with purpose, frontage and conflict checks.
/// Uses an explicit corridor and settlement spacing policy rather than unconstrained coordinates.
/// </summary>
public sealed class SettlementPlanner
{
    public SettlementPlan Plan(WorldRequest request, CancellationToken token = default)
    {
        request.Validate();
        var width = request.Width;
        var height = request.Height;
        var roads = new List<RoadSegment>();
        var buildings = new List<PlacementDecision>();
        var diagnostics = new List<PlacementDiagnostic>();
        if (request.SettlementCount == 0) return new(roads, buildings, diagnostics);
        int centerX = width / 2;
        var access = new HashSet<GridPoint>();
        var occupied = new HashSet<GridPoint>();
        void AddRoad(RoadSegment segment)
        {
            roads.Add(segment);
            foreach (var cell in segment.Cells()) access.Add(cell);
        }
        // A shared collector spine ensures every settlement connects to a common transport network.
        AddRoad(new("regional-collector", new(centerX, 1), new(centerX, height - 2), RoadClass.Collector));
        for (int i = 0; i < request.SettlementCount; i++)
        {
            token.ThrowIfCancellationRequested();
            // Centers are derived from service-area partitioning, with no arbitrary tile sampling.
            int cy = (int)((long)(i + 1) * (height - 1) / (request.SettlementCount + 1));
            AddRoad(new($"settlement-{i}-main", new(1, cy), new(width - 2, cy), RoadClass.Local));
            // Warehouse lots require one frontage on the main road and a free building footprint.
            // Stable candidate priority moves outward from the collector on both sides.
            int placed = 0;
            for (int offset = 2; offset < width - 3 && placed < 4; offset++)
            {
                foreach (int side in new[] { -1, 1 })
                {
                    var entrance = new GridPoint(centerX + side * offset, cy);
                    var site = new GridPoint(entrance.X, cy - 1);
                    if (!new GridBounds(width, height).Contains(site) ||
                        !new GridBounds(width, height).Contains(entrance) ||
                        access.Contains(site) || occupied.Contains(site) || !access.Contains(entrance))
                        continue;
                    // A larger simulation would evaluate soil/foundation slope and zoning here.
                    occupied.Add(site);
                    buildings.Add(new($"settlement-{i}-warehouse-{placed}", "warehouse", site,
                        "industrial storage adjacent to service road", $"settlement-{i}",
                        "road-frontage-and-free-footprint-v1", entrance));
                    placed++;
                    if (placed == 4) break;
                }
            }
            if (placed < 4)
                diagnostics.Add(new("UNMET_BUILDING_CAPACITY",
                    $"Settlement {i} placed {placed}/4 required warehouses; no valid additional frontage."));
        }
        return new(roads, buildings, diagnostics);
    }
}
