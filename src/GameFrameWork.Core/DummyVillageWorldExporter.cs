using System.Text.Json;
using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Core;

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
    IReadOnlyList<PlannedVegetation> Vegetation);

public static class DummyVillageWorldExporter
{
    public static DummyVillageWorldDocument Assemble()
    {
        var inspection = DummyVillageAssemblyFixture.Create();
        var houses = inspection.World.Base.Objects.Where(o => o.Kind == "home")
            .OrderBy(o => o.Id, StringComparer.Ordinal).ToArray();
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
            inspection.Vegetation.ToArray());
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
