using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Core;

/// <summary>Inspectable C# demonstration of complete spatial village planning.
/// Art assignments are test placeholders, not production-approved GameArts assets.</summary>
public sealed record DummyVillageInspection(
    SemanticWorldPackage World,
    LandscapeWorldPlan GardenBeds,
    StyledTerrainComposition GroundMaterials,
    IReadOnlyList<BuildingEnvelopeResult> BuildingFootprints,
    IReadOnlyList<PlannedVegetation> Vegetation);

public static class DummyVillageAssemblyFixture
{
    public static DummyVillageInspection Create()
    {
        var world = DummyVillageWorldFixture.Create();
        var beds = new LandscapeWorldPlanner().Generate(world, [
            new("western-garden", "village-flower-rows", "fixture-flowers", 1, 2, 1),
            new("eastern-garden", "village-orchard-rows", "fixture-orchard-soil", 1, 2, 1)
        ]);
        if (!beds.IsValid) throw new InvalidOperationException(string.Join(";", beds.Diagnostics));

        var visual = new ApprovedVisualWorldBlueprint(1, world.Base.MockupId,
            world.Base.MockupRevision, world.Base.ArtStyleId,
            world.Terrain.Select(t => new VisualRegion(t.SourceVisualRegionId,
                t.Boundary, t.Kind)).ToArray(), []);
        var materials = new StyledTerrainComposer().Compose(visual,
            world.Terrain.ToDictionary(t => t.SourceVisualRegionId,
                t => new TerrainCompositionStyle("village-" + t.Kind,
                    "fixture-" + t.Kind, t.DesignRuleId), StringComparer.Ordinal));
        if (materials.Diagnostics.Count != 0)
            throw new InvalidOperationException(string.Join(";", materials.Diagnostics));

        var homes = new Dictionary<string, BuildingEnvelopeResult>(StringComparer.Ordinal)
        {
            ["house-west"] = new(Rectangle(8, 10, 17, 20), []),
            ["house-east"] = new(Rectangle(43, 10, 53, 20), [])
        };
        var flowers = new VegetationCompositionPlanner().Generate(world, [
            new("western-flowers", "western-garden", "fixture-flower",
                VegetationPattern.OrchardGrid, 4, 2, "ordered flowers inside designed bed"),
            new("eastern-trees", "eastern-garden", "fixture-tree",
                VegetationPattern.OrchardGrid, 7, 3, "small fruit orchard")
        ]);
        if (!flowers.IsValid) throw new InvalidOperationException(string.Join(";", flowers.Diagnostics));
        var accepted = new SpeciesAwareSemanticWorldGate().Accept(world, flowers,
            new PathVegetationResult([], []), homes, [
                new("main-road", world.Paths.Single(p => p.Id == "main-road").Geometry, 4),
                new("crosswalk", world.Paths.Single(p => p.Id == "crosswalk").Geometry, 2)
            ], [
                new("noticeboard", new WorldPoint(28, 28), 1)
            ], [
                new("fixture-flower", 0.3), new("fixture-tree", 1.5)
            ]);
        if (!accepted.Accepted)
            throw new InvalidOperationException("Village spatial validation rejected: " +
                string.Join(";", accepted.Issues));
        return new(world, beds, materials, homes.Values.ToArray(),
            accepted.World!.Vegetation);
    }

    private static WorldPolygon Rectangle(double x0, double y0, double x1, double y1) =>
        new([new(x0, y0), new(x1, y0), new(x1, y1), new(x0, y1)]);
}
