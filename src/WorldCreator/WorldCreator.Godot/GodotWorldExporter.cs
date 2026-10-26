using System.Text.Json;
using WorldCreator.Core;
namespace WorldCreator.Godot;
/// <summary>Exports a Godot 4 scene skeleton and lossless world JSON for subsequent tile/asset compilation.</summary>
public sealed class GodotWorldExporter(IWorldPlanRenderer? planRenderer=null) : IWorldExporter
{
    public async Task ExportAsync(WorldDocument world, string outputDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var json = JsonSerializer.Serialize(world, new JsonSerializerOptions { WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "world.json"), json, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "plan.svg"), (planRenderer ?? new WorldPlanSvgRenderer()).Render(world,cancellationToken),cancellationToken);
        var scene = "[gd_scene format=3]\n\n[node name=\"GeneratedWorld\" type=\"Node2D\"]\n\n" +
          "[node name=\"Ground\" type=\"Node2D\" parent=\".\"]\n\n" +
          "[node name=\"BaseTerrain\" type=\"TileMapLayer\" parent=\"Ground\"]\n\n" +
          "[node name=\"Roads\" type=\"TileMapLayer\" parent=\"Ground\"]\n\n" +
          "[node name=\"Objects\" type=\"Node2D\" parent=\".\"]\n\n" +
          "[node name=\"Navigation\" type=\"Node2D\" parent=\".\"]\n";
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "GeneratedWorld.tscn"), scene, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "EXPORT_STATUS.md"),
            "Foundation export only. TileSet atlas, tiles, collisions, navigation polygons, animation scenes and art assets are NOT compiled yet. world.json is the source data for a future asset-aware compiler.\n", cancellationToken);
    }
}
