namespace WorldCreator.Core;
/// <summary>Validates the manifest contract only; PNG/atlas measurement and GameArts provenance require separate validators.</summary>
public sealed class ApprovedArtValidator
{
    public ArtValidationResult Validate(AssetRequirement requirement, ApprovedArtManifest? manifest)
    {
        var issues = new List<AssetValidationIssue>();
        void Fail(string code, string message) => issues.Add(new(code, requirement.Id, message));
        if (manifest is null)
        {
            Fail("ART_MISSING", "Approved production art is not available.");
            return new(issues);
        }
        if (manifest.SchemaVersion != 1) Fail("SCHEMA_VERSION", "Unsupported art manifest schema.");
        if (manifest.AssetId != requirement.Id) Fail("ASSET_ID", "Manifest asset ID mismatch.");
        if (manifest.ApprovalState != ArtApprovalState.Approved) Fail("NOT_APPROVED", "Production bundle must be approved.");
        if (string.IsNullOrWhiteSpace(manifest.GameArtsRevision) || string.IsNullOrWhiteSpace(manifest.StandardVersion))
            Fail("PROVENANCE", "Missing GameArts revision or standard version.");
        var sheetIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sheet in manifest.Sheets)
        {
            if (!sheetIds.Add(sheet.Id)) Fail("DUPLICATE_SHEET", $"Duplicate sheet {sheet.Id}.");
            if (string.IsNullOrWhiteSpace(sheet.ImagePath) || string.IsNullOrWhiteSpace(sheet.AtlasPath)
                || !IsHash(sheet.ImageSha256) || !IsHash(sheet.AtlasSha256))
                Fail("UNVERIFIED_SHEET", $"Sheet {sheet.Id} lacks paths or SHA-256 hashes.");
        }
        foreach (var component in requirement.Components.Where(c => c.MovesOrChangesState))
        {
            if (!manifest.Attachments.Any(a => a.ComponentId == component.Id
                && a.ParentId == component.ParentId && a.AnchorName == component.AttachmentName))
                Fail("ATTACHMENT_MISSING", $"Component {component.Id} has no matching attachment.");
            foreach (var direction in requirement.Directions)
            {
                if (!manifest.Attachments.Any(a => a.ComponentId == component.Id
                    && a.DirectionAnchors.ContainsKey(direction)))
                    Fail("DIRECTION_ANCHOR_MISSING", $"Component {component.Id} lacks anchor for {direction}.");
                if (component.Technique == AnimationTechnique.Frames &&
                    !manifest.Sheets.Any(s => s.ComponentId == component.Id && s.Directions.Contains(direction)))
                    Fail("ANIMATION_SHEET_MISSING", $"Component {component.Id} lacks frame sheet for {direction}.");
            }
        }
        return new(issues);
    }
    private static bool IsHash(string value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);
}
