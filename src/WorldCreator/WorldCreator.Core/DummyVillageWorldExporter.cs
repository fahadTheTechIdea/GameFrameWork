using System.Text.Json;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;

namespace WorldCreator.Core;

/// <summary>Names the building footprint without relying on dictionary iteration order.</summary>
public sealed record NamedBuildingFootprint(string ObjectId, WorldPolygon Boundary);

/// <summary>Single renderer-neutral document for inspecting a generated demo village.
/// Fixture assets and mockup metadata are explicitly simulated, not production approved.</summary>
public sealed record DummyVillageWorldDocument(
    int SchemaVersion,
    string ArtifactKind,
    bool ProductionApproved,
    SemanticWorldPackage SemanticWorld,
    IReadOnlyList<StyledTerrainPiece> TerrainMaterials,
    IReadOnlyList<LandscapeWorldFeature> GardenBeds,
    IReadOnlyList<NamedBuildingFootprint> Buildings,
    IReadOnlyList<PlannedVegetation> Vegetation,
    IReadOnlyList<BuildingEntranceLink>? Entrances = null,
    IReadOnlyList<PedestrianJunction>? Junctions = null,
    IReadOnlyList<PedestrianRoadCrossing>? RoadCrossings = null,
    IReadOnlyList<PedestrianCrossingSpan>? CrossingSpans = null,
    IReadOnlyList<NamedRoadProfile>? RoadProfiles = null,
    IReadOnlyList<CrossingApproach>? CrossingApproaches = null,
    IReadOnlyList<WorldAssetDependency>? AssetDependencies = null);

public static class DummyVillageWorldExporter
{
    public static DummyVillageWorldDocument Assemble()
    {
        var inspection = DummyVillageAssemblyFixture.Create();
        var houses = inspection.World.Base.Objects.Where(o => o.Kind == "home").ToArray();
        if (houses.Length != inspection.BuildingFootprints.Count)
            throw new InvalidOperationException("FIXTURE_BUILDING_FOOTPRINT_COUNT_MISMATCH");
        var footprints = houses.Zip(inspection.BuildingFootprints,
            (house, envelope) => envelope.IsFeasible && envelope.Footprint is not null
                ? new NamedBuildingFootprint(house.Id, envelope.Footprint)
                : throw new InvalidOperationException("FIXTURE_BUILDING_FOOTPRINT_INVALID")).ToArray();
        if (!inspection.GardenBeds.IsValid || inspection.GroundMaterials.Diagnostics.Count != 0)
            throw new InvalidOperationException("FIXTURE_GEOMETRY_INVALID");
        return new(1, "simulation-fixture", false, inspection.World,
            inspection.GroundMaterials.Pieces.ToArray(),
            inspection.GardenBeds.Features.ToArray(), footprints,
            inspection.Vegetation.ToArray(), [
                new("house-west", new WorldPoint(17, 20),
                    new WorldPolyline([new(17, 20), new(17, 28), new(24, 28)]), "crosswalk"),
                new("house-east", new WorldPoint(43, 20),
                    new WorldPolyline([new(43, 20), new(43, 28), new(40, 28)]), "crosswalk")
            ], [], [new("village-zebra", "crosswalk", "main-road",
                new WorldPoint(32, 28), true, true)],
                [new("village-zebra", new(30,28), new(34,28), 4, false)],
                [new("main-road", new RoadCrossSection(1.8, 2, 0.8, 0.8, 0.2, 0.2))],
                [new("village-zebra", new(29.2,28), new(34.8,28), 0, 0, true, true)]);
    }

    public static async Task<string> ExportAsync(string outputPath,
        CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var document = Assemble();
        var target = Path.GetFullPath(outputPath);
        var parent = Path.GetDirectoryName(target)!;
        Directory.CreateDirectory(parent);
        var json = JsonSerializer.Serialize(document,
            new JsonSerializerOptions { WriteIndented = true });
        token.ThrowIfCancellationRequested();
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, json, token);
            File.Move(temporary, target, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        return target;
    }
}
