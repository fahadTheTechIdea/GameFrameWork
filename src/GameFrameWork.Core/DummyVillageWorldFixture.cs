using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Core;

/// <summary>
/// Deterministic, engine-independent authored village fixture. Mockup approval is
/// simulated for tests only; this fixture is NOT an approved production GameArts asset.
/// The design and semantic world are constructed through the normal validation stages.
/// </summary>
public static class DummyVillageWorldFixture
{
    public static SemanticWorldPackage Create()
    {
        const string planId = "fixture-village";
        const string mockupId = "fixture-village-mockup";
        var grass = Rectangle(0, 0, 64, 48);
        var westPlot = Rectangle(4, 6, 24, 26);
        var eastPlot = Rectangle(36, 6, 60, 26);
        var westGarden = Rectangle(5, 30, 25, 45);
        var eastGarden = Rectangle(39, 30, 59, 45);
        var visual = new ApprovedVisualWorldBlueprint(1, mockupId, "fixture-r1",
            "cozy-pixel-village", [
                new("ground", grass, "continuous playable ground"),
                new("west-plot", westPlot, "west residential cluster"),
                new("east-plot", eastPlot, "east residential cluster"),
                new("west-garden", westGarden, "planned flowerbeds"),
                new("east-garden", eastGarden, "planned orchard")
            ], [
                new("west-home", "west-plot", new(12, 14), "home beside road"),
                new("east-home", "east-plot", new(48, 14), "home beside road"),
                new("notice-board", "ground", new(28, 28), "village crossing notice")
            ]);
        var decisions = new VisualObjectDecision[] {
            new("house-west", "home", "west-home", "west village residence", "fixture-housing"),
            new("house-east", "home", "east-home", "east village residence", "fixture-housing"),
            new("notice", "sign", "notice-board", "crossroads information", "fixture-civic")
        };
        var designed = new VisualWorldPlanner().Plan(visual, decisions, planId,
            new GridBounds(16, 12), 4);
        if (!designed.IsValid) throw new InvalidOperationException(
            string.Join(";", designed.Issues.Select(i => i.Code)));
        var bindings = designed.Design!.Objects.Select(o =>
            new WorldArtBinding(o.Id, "fixture-" + o.Kind, "base", "S", 0)).ToArray();
        var (basePackage, baseReport) = new WorldAssemblyPackageBuilder()
            .Build(visual, designed.Design, bindings);
        if (!baseReport.Valid) throw new InvalidOperationException(
            string.Join(";", baseReport.Errors));
        var assembled = new SemanticWorldBuilder().Build(basePackage!, visual, [
            new TerrainRegion("village-ground", "grass", grass, "ground", "fixture-base-ground"),
            new TerrainRegion("western-plot", "settlement", westPlot, "west-plot", "fixture-plots"),
            new TerrainRegion("eastern-plot", "settlement", eastPlot, "east-plot", "fixture-plots")
        ], [
            new PlannedWorldPath("main-road", "road", new WorldPolyline([
                new WorldPoint(32, 0), new WorldPoint(32, 48)
            ]), "approved-road-axis", "fixture-road-layout"),
            new PlannedWorldPath("crosswalk", "footpath", new WorldPolyline([
                new WorldPoint(24, 28), new WorldPoint(40, 28)
            ]), "approved-pedestrian-crossing", "fixture-pedestrian")
        ], [
            new LandscapeZone("western-garden", "flowerbed", westGarden,
                "west-garden", "fixture-garden-layout"),
            new LandscapeZone("eastern-garden", "orchard", eastGarden,
                "east-garden", "fixture-orchard-layout")
        ]);
        if (!assembled.Valid) throw new InvalidOperationException(
            string.Join(";", assembled.Errors));
        return assembled.Package!;
    }

    private static WorldPolygon Rectangle(double x0, double y0, double x1, double y1) =>
        new([new(x0, y0), new(x1, y0), new(x1, y1), new(x0, y1)]);
}
