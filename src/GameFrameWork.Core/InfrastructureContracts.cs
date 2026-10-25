using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Planning;

/// <summary>Authored alignment and connections, not a vacant-site search. Variant depth
/// is the maximum supported alignment length; width/height are retained unscaled.</summary>
public sealed record InfrastructureDesignRule(string Id, string RegionId, string SettlementId,
    string DefinitionId, string AlignmentPathId, string ObstacleRegionId, string Purpose,
    SettlementDemand Settlement, string StartConnectionPathId, string? EndConnectionPathId = null,
    string? ServicePathId = null, string? SizeVariantId = null, int RequiredCapacity = 0,
    double ClearanceMeters = 2, StructureSitingContext? Siting = null);

public sealed record InfrastructureReservation(string Id, string Kind, WorldPolygon Boundary, string Purpose);
public sealed record InfrastructureConnection(string PathId, string Mode, WorldPoint PositionMeters);
public sealed record InfrastructureAccessLink(WorldPolyline Centerline, double WidthMeters, string Mode, string Purpose);
public sealed record PlannedInfrastructure(string Id, string DefinitionId, string AlignmentPathId,
    string ObstacleRegionId, BuildingSizeVariant Variant, int RequiredCapacity, double LengthMeters,
    WorldPolygon Footprint, IReadOnlyList<InfrastructureConnection> Connections,
    IReadOnlyList<InfrastructureReservation> Reservations, VisualPlacementSource VisualSource,
    string Purpose, string RuleId, bool CarriesRoute, string? PassageMode, double PassageWidthMeters,
    IReadOnlyList<InfrastructureAccessLink> AccessLinks, bool IsSubsurface = false);
public sealed record InfrastructureDesignResult(IReadOnlyList<PlannedInfrastructure> Structures,
    IReadOnlyList<string> Issues);

public interface IInfrastructureDesignStrategy
{
    InfrastructureDesignResult Plan(MockupWorldDesign design, CancellationToken token=default);
}

public static class InfrastructureGeometry
{
    public static bool Overlap(WorldPolygon a,WorldPolygon b) => TerrainCoverageTopology.IntersectionArea(a,b)>1e-8;
}
