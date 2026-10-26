using System.Text.Json;
using WorldCreator.Core.Planning;

namespace WorldCreator.Core;

/// <summary>Engine-independent world package; no Godot or rendering dependency.</summary>
public sealed record WorldAssemblyPackage(
    int SchemaVersion,
    string PlanId,
    string MockupId,
    string MockupRevision,
    string ArtStyleId,
    IReadOnlyList<PlannedWorldObject> Objects,
    IReadOnlyList<WorldArtBinding> ArtBindings);

public sealed record WorldAssemblyReport(
    IReadOnlyList<string> Errors,
    int ObjectCount)
{
    public bool Valid => Errors.Count == 0;
}

/// <summary>Constructs a portable package only from an approved visual blueprint and validated design.</summary>
public sealed class WorldAssemblyPackageBuilder
{
    public (WorldAssemblyPackage? Package, WorldAssemblyReport Report) Build(
        ApprovedVisualWorldBlueprint visual,
        WorldDesignPlan design,
        IReadOnlyList<WorldArtBinding> artBindings)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(design);
        ArgumentNullException.ThrowIfNull(artBindings);
        var errors = new List<string>();
        if (visual.SchemaVersion != 1 || string.IsNullOrWhiteSpace(visual.MockupId) ||
            string.IsNullOrWhiteSpace(visual.ApprovedRevision) ||
            string.IsNullOrWhiteSpace(visual.ArtStyleId))
            errors.Add("APPROVED_VISUAL_SOURCE_MISSING");
        errors.AddRange(new WorldDesignValidator().Validate(design).Issues
            .Select(issue => $"{issue.Code}:{issue.ObjectId}"));
        var ids = design.Objects.Select(o => o.Id).ToHashSet(StringComparer.Ordinal);
        var bound = new HashSet<string>(StringComparer.Ordinal);
        foreach (var binding in artBindings)
        {
            if (!ids.Contains(binding.ObjectId)) errors.Add($"ART_OBJECT_UNKNOWN:{binding.ObjectId}");
            if (!bound.Add(binding.ObjectId)) errors.Add($"ART_OBJECT_DUPLICATE:{binding.ObjectId}");
            if (string.IsNullOrWhiteSpace(binding.AssetId) ||
                string.IsNullOrWhiteSpace(binding.ComponentId) ||
                string.IsNullOrWhiteSpace(binding.Facing))
                errors.Add($"ART_REFERENCE_INCOMPLETE:{binding.ObjectId}");
        }
        foreach (var obj in design.Objects)
            if (!bound.Contains(obj.Id)) errors.Add($"ART_REFERENCE_MISSING:{obj.Id}");
        if (errors.Count != 0) return (null, new(errors, design.Objects.Count));
        var package = new WorldAssemblyPackage(1, design.Id, visual.MockupId,
            visual.ApprovedRevision, visual.ArtStyleId,
            design.Objects.ToArray(), artBindings.ToArray());
        return (package, new([], package.Objects.Count));
    }

    public async Task WriteAsync(WorldAssemblyPackage package, string destination,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        var full = Path.GetFullPath(destination);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var json = JsonSerializer.Serialize(package,
            new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(full, json, token);
    }
}
