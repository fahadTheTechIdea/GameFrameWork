using WorldCreator.Core.Planning;

namespace WorldCreator.Core;

/// <summary>Render binding references approved artwork without altering planned placement.</summary>
public sealed record WorldArtBinding(string ObjectId, string AssetId, string ComponentId,
    string Facing, int RenderLayer, string? ParentObjectId = null);

public sealed record BoundWorldObject(PlannedWorldObject Placement, WorldArtBinding Art,
    ApprovedArtManifest ApprovedManifest);

public sealed record WorldArtAssembly(IReadOnlyList<BoundWorldObject> Objects,
    IReadOnlyList<AssetValidationIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

/// <summary>
/// Binds immutable world design to an approved art registry. Missing artwork is
/// a hard error; no substitute sprite or other asset is selected automatically.
/// Structural manifest validation is not PNG/atlas byte validation.
/// </summary>
public sealed class WorldArtAssembler(IApprovedArtRegistry registry)
{
    public async Task<WorldArtAssembly> AssembleAsync(WorldDesignPlan design,
        IReadOnlyList<WorldArtBinding> bindings, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(design);
        ArgumentNullException.ThrowIfNull(bindings);
        var issues = new List<AssetValidationIssue>();
        var result = new List<BoundWorldObject>();
        var validation = new WorldDesignValidator().Validate(design);
        issues.AddRange(validation.Issues.Select(i =>
            new AssetValidationIssue(i.Code, i.ObjectId, i.Message)));
        if (!validation.IsValid) return new(result, issues);
        var objects = design.Objects.ToDictionary(o => o.Id, StringComparer.Ordinal);
        var bound = new HashSet<string>(StringComparer.Ordinal);
        foreach (var binding in bindings)
        {
            token.ThrowIfCancellationRequested();
            if (!objects.TryGetValue(binding.ObjectId, out var placement))
            {
                issues.Add(new("UNKNOWN_WORLD_OBJECT", binding.ObjectId, "Art binding references no planned object."));
                continue;
            }
            if (!bound.Add(binding.ObjectId))
            {
                issues.Add(new("DUPLICATE_ART_BINDING", binding.ObjectId, "Each object requires one art binding."));
                continue;
            }
            if (string.IsNullOrWhiteSpace(binding.AssetId) ||
                string.IsNullOrWhiteSpace(binding.ComponentId) ||
                string.IsNullOrWhiteSpace(binding.Facing) ||
                binding.RenderLayer < -10000 || binding.RenderLayer > 10000)
            {
                issues.Add(new("INVALID_ART_BINDING", binding.ObjectId, "Invalid visual asset, component, facing or layer."));
                continue;
            }
            if (binding.ParentObjectId != placement.ParentObjectId)
            {
                issues.Add(new("ART_PARENT_MISMATCH", binding.ObjectId, "Art and plan parent relationships differ."));
                continue;
            }
            var manifest = await registry.ResolveAsync(binding.AssetId, token);
            if (manifest is null || manifest.ApprovalState != ArtApprovalState.Approved ||
                manifest.AssetId != binding.AssetId)
            {
                issues.Add(new("ART_NOT_APPROVED", binding.ObjectId, "Matching approved GameArts asset required."));
                continue;
            }
            if (!manifest.Sheets.Any(s => s.ComponentId == binding.ComponentId &&
                s.Directions.Contains(binding.Facing, StringComparer.Ordinal)))
            {
                issues.Add(new("ART_DIRECTION_UNAVAILABLE", binding.ObjectId,
                    "No approved sheet covers the requested component and facing."));
                continue;
            }
            result.Add(new(placement, binding, manifest));
        }
        foreach (var obj in design.Objects)
            if (!bound.Contains(obj.Id))
                issues.Add(new("ART_BINDING_MISSING", obj.Id, "Every rendered object requires an art binding."));
        return new(result, issues);
    }
}
