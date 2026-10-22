namespace GameFrameWork.Core;

/// <summary>Verifies GameArts native component bundle references against actual sibling atlas/PNG files.</summary>
public sealed class GameArtsBundleFileValidator
{
    public async Task<IReadOnlyList<string>> ValidateAsync(string bundlePath, CancellationToken token = default)
    {
        var errors = new List<string>();
        var parsed = await new GameArtsBundleReader().ReadAsync(bundlePath, token);
        if (!parsed.IsValid)
            return parsed.Errors;
        var root = Path.GetDirectoryName(Path.GetFullPath(bundlePath))!;
        foreach (var component in parsed.Bundle!.Components)
        foreach (var direction in parsed.Bundle.Directions)
        {
            token.ThrowIfCancellationRequested();
            var filename = component.Atlases[direction];
            if (string.IsNullOrWhiteSpace(filename) || Path.IsPathRooted(filename) ||
                filename.Contains('/') || filename.Contains('\\') ||
                filename is "." or "..")
            {
                errors.Add($"ATLAS_PATH_UNSAFE:{component.Id}:{direction}");
                continue;
            }
            var atlasPath = Path.Combine(root, filename);
            if (!File.Exists(atlasPath))
            {
                errors.Add($"ATLAS_MISSING:{component.Id}:{direction}");
                continue;
            }
            try
            {
                using var json = System.Text.Json.JsonDocument.Parse(
                    await File.ReadAllTextAsync(atlasPath, token));
                var image = json.RootElement.GetProperty("image").GetString();
                if (string.IsNullOrWhiteSpace(image) || Path.IsPathRooted(image) ||
                    image.Contains('/') || image.Contains('\\') || image is "." or "..")
                {
                    errors.Add($"IMAGE_PATH_UNSAFE:{component.Id}:{direction}");
                    continue;
                }
                var pngPath = Path.Combine(root, image);
                if (!File.Exists(pngPath))
                {
                    errors.Add($"IMAGE_MISSING:{component.Id}:{direction}");
                    continue;
                }
                var geometry = await new GameArtsAtlasValidator().ValidateAsync(atlasPath, pngPath, token);
                errors.AddRange(geometry.Select(e => $"{e}:{component.Id}:{direction}"));
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or
                KeyNotFoundException or InvalidOperationException or IOException or ArgumentException)
            {
                errors.Add($"ATLAS_INVALID:{component.Id}:{direction}:{ex.GetType().Name}");
            }
        }
        // Every child's named mounting point must exist on every parent directional frame.
        var parts = parsed.Bundle.Components.ToDictionary(c => c.Id, StringComparer.Ordinal);
        foreach (var child in parsed.Bundle.Components.Where(c => c.Parent is not null))
        {
            if (!parts.TryGetValue(child.Parent!, out var parent)) continue;
            foreach (var direction in parsed.Bundle.Directions)
            {
                var atlasPath = Path.Combine(root, parent.Atlases[direction]);
                if (!File.Exists(atlasPath)) continue;
                try
                {
                    using var json = System.Text.Json.JsonDocument.Parse(
                        await File.ReadAllTextAsync(atlasPath, token));
                    var frames = json.RootElement.GetProperty("frames");
                    int frameIndex = 0;
                    foreach (var frame in frames.EnumerateArray())
                    {
                        if (!frame.TryGetProperty("anchors", out var anchors) ||
                            anchors.ValueKind != System.Text.Json.JsonValueKind.Object ||
                            !anchors.TryGetProperty(child.Mount!, out var _))
                            errors.Add($"PARENT_MOUNT_MISSING:{child.Id}:{direction}:{frameIndex}");
                        frameIndex++;
                    }
                    if (frameIndex == 0) errors.Add($"PARENT_FRAMES_EMPTY:{child.Id}:{direction}");
                }
                catch (Exception ex) when (ex is System.Text.Json.JsonException or
                    KeyNotFoundException or InvalidOperationException or IOException)
                {
                    errors.Add($"PARENT_ATLAS_INVALID:{child.Id}:{direction}");
                }
            }
        }
        return errors;
    }
}
