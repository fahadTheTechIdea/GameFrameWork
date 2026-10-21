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
        return errors;
    }
}
