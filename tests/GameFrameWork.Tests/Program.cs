using System.Text.Json;
using GameFrameWork.Core;
using GameFrameWork.Worlds;
using GameFrameWork.Godot;
var generator = new DesertWorldGenerator();
var req = new WorldRequest(32, 32, 123, Biome.Desert);
var first = await generator.GenerateAsync(req);
var second = await generator.GenerateAsync(req);
if (JsonSerializer.Serialize(first) != JsonSerializer.Serialize(second)) throw new Exception("Determinism test failed.");
if (first.Cells.Count != 1024) throw new Exception("Grid length wrong.");
if (!first.Cells.Any(c => c.Kind == CellKind.Road)) throw new Exception("No roads.");
if (!first.Objects.Any(o => o.Kind == "gate")) throw new Exception("No gate.");
var dir = Path.Combine(Path.GetTempPath(), "gameframework-test-" + Guid.NewGuid().ToString("N"));
try
{
    await new GodotWorldExporter().ExportAsync(first, dir);
    if (!File.Exists(Path.Combine(dir, "GeneratedWorld.tscn")) || !File.Exists(Path.Combine(dir, "world.json"))) throw new Exception("Export files missing.");
}
finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
Console.WriteLine("PASS: deterministic generation, grid, roads, gate metadata, export.");