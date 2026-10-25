using System.Text.Json.Serialization;
using GameFrameWork.Core.Planning;
namespace GameFrameWork.Core;
public enum Biome { Desert, Forest, Grassland, Coastal, Water, Rock }
public enum CellKind { Sand, Rock, Road, Structure, Water, Grass, Tree }
public sealed record WorldRequest(int Width = 64, int Height = 64, int Seed = 42, Biome Biome = Biome.Desert, int SettlementCount = 1,
    MockupWorldDesign? Design = null)
{
    public void Validate()
    {
        if (Width is < 16 or > 4096 || Height is < 16 or > 4096) throw new ArgumentOutOfRangeException(nameof(Width), "World dimensions must be 16..4096.");
        if (SettlementCount < 0 || SettlementCount > Width * Height / 128) throw new ArgumentOutOfRangeException(nameof(SettlementCount));
    }
}
public sealed record WorldCell(int X, int Y, CellKind Kind, float HeightMeters, bool BlocksMovement, int TraversalCost, int RenderLayer,
    string? SourceVisualRegionId = null, string? DesignRuleId = null, string? SurfaceMaterialId = null);
public sealed record WorldObject(string Id, string Kind, int X, int Y, string State, string? AnimationId = null,
    VisualPlacementSource? VisualSource = null);
public sealed record WorldDocument(int SchemaVersion, WorldRequest Request, IReadOnlyList<WorldCell> Cells, IReadOnlyList<WorldObject> Objects,
    WorldDesignPlan? Design = null, IReadOnlyList<BuildingSiteFeature>? SiteFeatures = null,
    IReadOnlyList<PlannedVegetation>? Vegetation = null, IReadOnlyList<LandscapeZone>? Landscape = null,
    IReadOnlyList<PlannedInfrastructure>? Infrastructure = null,
    IReadOnlyList<InfrastructureRasterCell>? InfrastructureLayers = null, WorldSitingAssessment? Siting = null, ResolvedSettlementCatalog? Catalog = null)
{
    [JsonIgnore] public int Width => Request.Width;
    [JsonIgnore] public int Height => Request.Height;
    public WorldCell At(int x, int y) => Cells[y * Width + x];
}
/// <summary>Retains water/rock underneath a traversable crossing in the 2D adapter.</summary>
public sealed record InfrastructureRasterCell(string InfrastructureId, WorldCell UnderlyingTerrain,
    bool CarriesRoute, string? PassageMode = null);
public interface IWorldGenerator
{
    Task<WorldDocument> GenerateAsync(WorldRequest request, CancellationToken cancellationToken = default);
}
public interface IWorldExporter
{
    Task ExportAsync(WorldDocument world, string outputDirectory, CancellationToken cancellationToken = default);
}
