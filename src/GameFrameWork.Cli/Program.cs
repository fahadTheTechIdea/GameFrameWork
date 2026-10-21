using GameFrameWork.Core;
using GameFrameWork.Worlds;
using GameFrameWork.Godot;
if (args.Length == 0 || args[0] is "--help" or "-h")
{
    Console.WriteLine("Usage: dotnet run --project src/GameFrameWork.Cli -- desert [output-dir] [seed]");
    return;
}
if (args[0] != "desert") throw new ArgumentException("Only desert is supported in the first slice.");
var output = args.Length > 1 ? args[1] : "generated/desert";
var seed = args.Length > 2 ? int.Parse(args[2]) : 42;
var world = await new DesertWorldGenerator().GenerateAsync(new WorldRequest(64, 64, seed));
await new GodotWorldExporter().ExportAsync(world, output);
Console.WriteLine($"Generated {world.Width}x{world.Height} desert world at {Path.GetFullPath(output)}");