using GameFrameWork.Core;
using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Worlds;

/// <summary>
/// Executes existing planning disciplines in dependency order and emits a validated
/// world design. This first integration uses a grid settlement plan as its source;
/// road surfaces and public realm remain separate semantic deliverables.
/// </summary>
public sealed record SettlementDesignResult(
    WorldDesignPlan Design,
    SettlementPlan Settlement,
    IReadOnlyList<StreetCorridor> Streets,
    PublicRealmPlan PublicRealm,
    IReadOnlyList<string> Diagnostics);

public sealed class SettlementDesignPipeline
{
    public SettlementDesignResult Build(WorldRequest request, CancellationToken token = default)
    {
        request.Validate();
        var order = new WorldPlanCoordinator().Coordinate(
        [
            new DesignDiscipline("terrain", []),
            new DesignDiscipline("transport", ["terrain"]),
            new DesignDiscipline("parcels", ["transport"]),
            new DesignDiscipline("public-realm", ["transport"]),
            new DesignDiscipline("objects", ["parcels"])
        ]);
        if (!order.IsValid) throw new InvalidOperationException("Invalid design-stage dependencies.");
        SettlementPlan? settlement = null;
        List<StreetCorridor> streets = [];
        PublicRealmPlan? publicRealm = null;
        List<PlannedWorldObject> objects = [];
        var diagnostics = new List<string>();
        const string planId = "settlement-integrated-v1";
        foreach (string stage in order.ExecutionOrder)
        {
            token.ThrowIfCancellationRequested();
            switch (stage)
            {
                case "terrain":
                    // Terrain suitability and hydrology are not integrated yet.
                    break;
                case "transport":
                    settlement = new SettlementPlanner().Plan(request, token);
                    diagnostics.AddRange(settlement.Diagnostics.Select(d => d.Code + ":" + d.Message));
                    foreach (var road in settlement.Roads)
                    {
                        var path = new WorldPolyline([
                            new WorldPoint(road.Start.X, road.Start.Y),
                            new WorldPoint(road.End.X, road.End.Y)
                        ]);
                        streets.Add(new StreetCorridor(road.Id, path,
                            new RoadCrossSection(1, 1, 0, 0, 0, 0)));
                    }
                    break;
                case "parcels":
                    // Existing raster frontage decisions; polygon parcels are a later integration.
                    break;
                case "public-realm":
                    publicRealm = new PublicRealmPlanner().Plan(streets,
                        new PublicRealmRules(25, 1, 0.5));
                    diagnostics.AddRange(publicRealm.Diagnostics);
                    break;
                case "objects":
                    foreach (var building in settlement!.Buildings)
                    {
                        objects.Add(new PlannedWorldObject(building.Id, building.Kind,
                            building.Site, building.Purpose, building.ConstraintId, planId));
                        objects.Add(new PlannedWorldObject(building.Id + "-gate", "gate",
                            building.Site, "control building entry", "gate-attachment-v1",
                            planId, building.Id, "closed", "gate-open-close"));
                    }
                    break;
            }
        }
        var design = new WorldDesignPlan(planId,
            new GridBounds(request.Width, request.Height), objects);
        var validation = new WorldDesignValidator().Validate(design);
        diagnostics.AddRange(validation.Issues.Select(i => i.Code + ":" + i.ObjectId));
        return new(design, settlement!, streets, publicRealm!, diagnostics);
    }
}
