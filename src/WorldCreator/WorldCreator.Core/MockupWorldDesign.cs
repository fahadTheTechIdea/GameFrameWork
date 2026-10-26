using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

/// <summary>Structured visual input, not evidence of GameArts lifecycle approval.
/// The engine computes parcels, envelopes, terrain and props from these rules.</summary>
public sealed record MockupWorldDesign(int SchemaVersion,
    ApprovedVisualWorldBlueprint Blueprint, double MetersPerCell,
    IReadOnlyList<VisualDesignPath> Paths,
    IReadOnlyList<MockupRoadRule> RoadRules,
    IReadOnlyList<SettlementRegionRule> SettlementRules,
    IReadOnlyList<MockupTerrainRule> TerrainRules,
    IReadOnlyList<PathPattern> PublicRealmPatterns,
    IReadOnlyList<BuildingDefinition> BuildingCatalog,
    IReadOnlyList<LandscapeZone>? LandscapeZones = null,
    IReadOnlyList<VegetationGrammar>? VegetationGrammars = null,
    IReadOnlyList<VegetationAssetClearance>? VegetationClearances = null,
    IReadOnlyList<InfrastructureDesignRule>? InfrastructureRules = null,
    WorldSitingPlan? Siting = null, SettlementCatalogSelection? Catalog = null,
    Capture.MockupCaptureDocument? Capture = null,
    IReadOnlyList<SurfaceClearanceRule>? SurfaceClearances = null,
    IReadOnlyList<FacilityPlacement>? FacilityPlacements = null,
    IReadOnlyList<PlannedFacility>? PlannedFacilities = null,
    IReadOnlyList<string>? BuildingCatalogIds = null);

public sealed record SurfaceClearanceRule(string Id,string RegionId,double MinimumGapMeters,
    IReadOnlyList<string> OtherRegionIds,IReadOnlyList<string> RoadIds,bool ProtectBuildings,
    IReadOnlyList<string> VegetationAnchorIds,double VegetationRadiusMeters,string? GapMaterialId = null);

public sealed record MockupRoadRule(string PathId, RoadClass Class, RoadCrossSection Profile);

/// <summary>A rectangular, north/south street-fronted region. Building count follows
/// frontage subdivision and envelope feasibility, never a hardcoded warehouse quota.</summary>
public sealed record SettlementRegionRule(string Id, string RegionId, string SettlementId,
    string FrontagePathId, string Purpose, double TargetFrontageMeters,
    double MinimumFrontageMeters, BuildingEnvelopeRules Envelope,
    IReadOnlyList<BuildingComponentRule> Components,
    string? SizeVariantId = null, int RequiredCapacity = 0, SettlementDemand? Settlement = null,
    StructureSitingContext? Siting = null, FacilityMembership? Facility = null);

public sealed record BuildingComponentRule(string Id, string Kind, string Purpose,
    string State = "idle", string? AnimationId = null);

/// <summary>Higher priority overlays lower priority. Equal-priority overlap and gaps
/// are errors. Slopes are authored height gradients, not arbitrary noise fields.</summary>
public sealed record MockupTerrainRule(string RegionId, CellKind Kind, int Priority,
    double ElevationMeters, string RuleId, double SlopeX = 0, double SlopeY = 0,
    string? SurfaceMaterialId = null);

/// <summary>Exact visual-source provenance retained beside raster coordinates.</summary>
public sealed record VisualPlacementSource(string MockupId, string MockupRevision,
    string ArtStyleId, string RegionId, string FeatureId, WorldPoint PositionMeters,
    WorldPolygon? Footprint = null, string? BuildingDefinitionId = null,
    string? SizeVariantId = null, double? HeightMeters = null, int? ServiceCapacity = null, FacilityMembership? Facility = null);
