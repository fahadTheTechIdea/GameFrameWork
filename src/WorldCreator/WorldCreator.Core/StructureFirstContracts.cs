using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;

namespace WorldCreator.Core;

public enum VisualScopeKind { World, Settlement, District, Site }
public sealed record StructureVisualScope(string Id, string? ParentId, VisualScopeKind Kind,
    string? SettlementId, SettlementClass? SettlementClass, IReadOnlyList<string> RegionIds,
    bool IsSample = false);
public sealed record StructureImageReference(string File, string Sha256);
public sealed record StructureProductionReference(string AssetId, string Revision, string BundlePath,
    string ComponentId, string Facing, string SheetId, string FrameName);
public sealed record StructureArtEntry(string DefinitionId, string VariantId,
    StructureImageReference? Master = null, StructureProductionReference? Production = null);
public sealed record StructureScopeConcept(string ScopeId, string MockupId, string Revision, StructureImageReference Image);
public sealed record StructureFirstSpecification(int SchemaVersion, string Id, string WorldSha256,
    string ArtStyleId, string CameraProfile, int MaximumStructuresPerMockup,
    IReadOnlyList<StructureVisualScope> Scopes, IReadOnlyList<StructureArtEntry> Art,
    IReadOnlyList<StructureScopeConcept>? Concepts = null);
public sealed record StructureFirstJob(string Id, string DefinitionId, string VariantId,
    int InstanceCount, double WidthMeters, double DepthMeters, string Purpose,
    StructureCompositionAssessment Composition, string Prompt, string Status);
public sealed record StructureMockupJob(string ScopeId, string Role, IReadOnlyList<string> ObjectIds,
    IReadOnlyList<string> StructureJobIds, string Prompt, string Status);
public sealed record StructureScopeCoverage(string ScopeId, int StructureCount, bool Complete,
    IReadOnlyList<string> Issues);
public sealed record StructureFirstInstance(PlannedWorldObject Placement, string ScopeId,
    string StructureJobId, StructureArtEntry Art);
public sealed record StructureFirstAssembly(int SchemaVersion, string WorldSha256, string PlanId,
    IReadOnlyList<StructureFirstInstance> Instances);
public sealed record StructureFirstResult(StructureFirstAssembly Assembly,
    IReadOnlyList<StructureFirstJob> StructureJobs, IReadOnlyList<StructureMockupJob> MockupJobs,
    IReadOnlyList<StructureScopeCoverage> Coverage, IReadOnlyList<string> ProductionIssues)
{
    public bool CoverageComplete => Coverage.All(c => c.Complete);
}
public sealed record StructureFramePlacement(string ObjectId, string AssetId, string ImageFile,
    string ImageSha256, int ImageWidth, int ImageHeight, int FrameX, int FrameY,
    int FrameWidth, int FrameHeight, double PixelsPerMeter, PixelAnchor Pivot,
    WorldPoint PositionMeters);
public sealed record StructureFirstProductionResult(StructureFirstResult Draft,
    WorldArtAssembly? ApprovedAssembly, IReadOnlyList<StructureFramePlacement> Frames,
    IReadOnlyList<string> Issues)
{
    public bool Ready => Issues.Count == 0 && ApprovedAssembly?.IsValid == true;
}
