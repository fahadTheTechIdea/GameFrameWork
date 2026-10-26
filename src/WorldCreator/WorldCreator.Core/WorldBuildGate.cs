namespace WorldCreator.Core;
public sealed record WorldBuildCheck(bool Ready, IReadOnlyList<AssetValidationIssue> Issues);
/// <summary>Never treat missing or unapproved GameArts production bundles as render-ready.</summary>
public sealed class WorldBuildGate(IApprovedArtRegistry registry)
{
    public async Task<WorldBuildCheck> CheckAsync(WorldBlueprint blueprint, CancellationToken token = default)
    {
        var issues = new List<AssetValidationIssue>();
        if (blueprint.SchemaVersion != 1)
            issues.Add(new("BLUEPRINT_VERSION", blueprint.Id, "Unsupported world blueprint schema."));
        if (blueprint.Assets.Select(a => a.Id).Distinct(StringComparer.Ordinal).Count() != blueprint.Assets.Count)
            issues.Add(new("DUPLICATE_ASSET_ID", blueprint.Id, "Asset requirement IDs must be unique."));
        var validator = new ApprovedArtValidator();
        foreach (var asset in blueprint.Assets)
        {
            token.ThrowIfCancellationRequested();
            var manifest = await registry.ResolveAsync(asset.Id, token);
            issues.AddRange(validator.Validate(asset, manifest).Issues);
        }
        return new(issues.Count == 0, issues);
    }
}
