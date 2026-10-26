using System.Text.Json;

namespace WorldCreator.Core;

/// <summary>Reads actual GameArts v7.7 component bundles. Structural gate, not artwork approval.</summary>
public sealed record NativeGameArtsBundle(string AssetId, IReadOnlyList<string> Directions,
    IReadOnlyList<NativeGameArtsComponent> Components);
public sealed record NativeGameArtsComponent(string Id, string? Parent, string? Mount,
    IReadOnlyDictionary<string, string> Atlases);
public sealed record NativeBundleResult(NativeGameArtsBundle? Bundle, IReadOnlyList<string> Errors)
{
    public bool IsValid => Bundle is not null && Errors.Count == 0;
}
public sealed class GameArtsBundleReader
{
    public async Task<NativeBundleResult> ReadAsync(string bundleFile, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleFile);
        await using var stream = File.OpenRead(bundleFile);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: token);
        var root = document.RootElement;
        var errors = new List<string>();
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("schema_version", out var version) || version.GetString() != "1.0")
            return new(null, ["UNSUPPORTED_BUNDLE_SCHEMA"]);
        string assetId = root.TryGetProperty("asset_id", out var asset) && asset.ValueKind == JsonValueKind.String
            ? asset.GetString() ?? "" : "";
        var directions = root.GetProperty("directions").EnumerateArray().Select(d => d.GetString() ?? "").ToArray();
        if (string.IsNullOrWhiteSpace(assetId) || directions.Length == 0 ||
            directions.Any(string.IsNullOrWhiteSpace) || directions.Distinct().Count() != directions.Length)
            errors.Add("INVALID_ASSET_OR_DIRECTIONS");
        var components = new List<NativeGameArtsComponent>();
        foreach (var part in root.GetProperty("components").EnumerateArray())
        {
            var id = part.GetProperty("id").GetString() ?? "";
            string? parent = part.TryGetProperty("parent", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
            string? mount = part.TryGetProperty("mount", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
            var atlases = part.GetProperty("atlases").EnumerateObject().ToDictionary(
                x => x.Name, x => x.Value.GetString() ?? "", StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(id) || directions.Any(d => !atlases.ContainsKey(d) ||
                string.IsNullOrWhiteSpace(atlases[d]))) errors.Add("INCOMPLETE_COMPONENT_DIRECTIONS:" + id);
            components.Add(new(id, parent, mount, atlases));
        }
        if (components.Count(c => c.Parent is null) != 1) errors.Add("INVALID_ROOT_COUNT");
        var index = components.ToDictionary(c => c.Id, StringComparer.Ordinal);
        foreach (var item in components.Where(c => c.Parent is not null))
        {
            if (!index.ContainsKey(item.Parent!) || string.IsNullOrWhiteSpace(item.Mount))
                errors.Add("INVALID_COMPONENT_MOUNT:" + item.Id);
            var visited = new HashSet<string> { item.Id };
            var cursor = item;
            while (cursor.Parent is { } key && index.TryGetValue(key, out var next))
            {
                if (!visited.Add(key)) { errors.Add("COMPONENT_CYCLE:" + item.Id); break; }
                cursor = next;
            }
        }
        return errors.Count == 0 ? new(new(assetId, directions, components), errors) : new(null, errors);
    }
}
