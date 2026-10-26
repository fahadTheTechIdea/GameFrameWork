using System.Reflection;
using System.Text.Json;
using WorldCreator.Core.Metadata;

namespace WorldCreator.Core;

/// <summary>Packages the JavaScript schematic viewer with its metadata; no remote resources or artwork.</summary>
public sealed class WorldMetadataSchematicExporter
{
    public async Task WriteAsync(WorldMetadataDocument world, string directory, CancellationToken token = default)
    {
        var files=await CreateAsync(world,token);
        await File.WriteAllTextAsync(Path.Combine(directory,"schematic.html"),files.Html,token);
        await File.WriteAllTextAsync(Path.Combine(directory,"schematic.js"),files.JavaScript,token);
    }

    public async Task<(string Html,string JavaScript)> CreateAsync(WorldMetadataDocument world,CancellationToken token=default)
    {
        string template = await ResourceAsync("schematic.html", token);
        // The default JSON encoder escapes HTML delimiters, including script closing tags.
        string html = template.Replace("__WORLD_METADATA_JSON__", JsonSerializer.Serialize(world, WorldJson.CreateOptions()), StringComparison.Ordinal);
        return (html,await ResourceAsync("world-schematic.js",token));
    }

    private static async Task<string> ResourceAsync(string file, CancellationToken token)
    {
        var assembly = typeof(WorldMetadataSchematicExporter).Assembly;
        string name = assembly.GetManifestResourceNames().Single(n => n.EndsWith(".Schematic." + file, StringComparison.Ordinal));
        await using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(token);
    }
}
