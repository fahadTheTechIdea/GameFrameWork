using System.Text.Json.Serialization;
namespace GameFrameWork.Core;
public enum ArtApprovalState { Draft, AwaitingMockupApproval, MockupApproved, AwaitingProductionApproval, Approved, Revoked }
public enum AnimationTechnique { Frames, Transform, Shader, Procedural }
public sealed record AssetRequirement(
    string Id, string SemanticKind, double WidthMeters, double HeightMeters,
    IReadOnlyList<string> Directions, IReadOnlyList<ComponentRequirement> Components);
public sealed record ComponentRequirement(
    string Id, string ParentId, string AttachmentName, bool MovesOrChangesState,
    AnimationTechnique Technique, IReadOnlyList<string> States);
public sealed record SpriteSheetManifest(
    string Id, string ImagePath, string AtlasPath, string ImageSha256, string AtlasSha256,
    string ComponentId, IReadOnlyList<string> Directions);
public sealed record ApprovedArtManifest(
    int SchemaVersion, string AssetId, string GameArtsRevision, string StandardVersion,
    ArtApprovalState ApprovalState, IReadOnlyList<SpriteSheetManifest> Sheets,
    IReadOnlyList<ComponentAttachment> Attachments);
public sealed record ComponentAttachment(string ComponentId, string ParentId, string AnchorName,
    double LocalXMeters, double LocalYMeters, IReadOnlyDictionary<string, PixelAnchor> DirectionAnchors);
public sealed record PixelAnchor(int X, int Y);
public sealed record WorldBlueprint(
    int SchemaVersion, string Id, WorldRequest Request, IReadOnlyList<AssetRequirement> Assets);
public sealed record AssetValidationIssue(string Code, string AssetId, string Message);
public sealed record ArtValidationResult(IReadOnlyList<AssetValidationIssue> Issues)
{
    [JsonIgnore] public bool IsValid => Issues.Count == 0;
}
public interface IApprovedArtRegistry
{
    Task<ApprovedArtManifest?> ResolveAsync(string assetId, CancellationToken cancellationToken = default);
}
