using GameFrameWork.Core;
using GameFrameWork.Core.Operations;
using GameFrameWork.Worlds;
using GameFrameWork.Godot;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    Console.WriteLine("Usage: dotnet run --project src/GameFrameWork.Cli -- desert [output-dir] [seed]");
    return;
}
var operation = "world.generate";
var id = Guid.NewGuid().ToString("N");
var eventRoot = Path.Combine(Environment.CurrentDirectory, ".gameframework", "operations");
var reporter = new WorldOperationReporter(
    new JsonLineOperationStore(Path.Combine(eventRoot, "log.jsonl")),
    new JsonLineOperationStore(Path.Combine(eventRoot, "audit.jsonl")),
    new ConsoleAndFileNotificationService(new JsonLineOperationStore(Path.Combine(eventRoot, "notifications.jsonl"))));
OperationEvent Entry(string eventId, OperationSeverity severity, string message, string outcome) =>
    new(eventId, DateTimeOffset.UtcNow, severity, "GameFrameWork.Cli", operation, id, message, args.Length > 1 ? Path.GetFileName(args[1]) : null, "local-cli", outcome);
try
{
    if (args[0] == "dummy-village")
    {
        var destination = args.Length > 1 ? args[1] : "generated/dummy-village";
        await new DummyVillageAssembler().ExportAsync(destination);
        Console.WriteLine($"Generated test Godot village at {Path.GetFullPath(destination)}");
        return;
    }
    if (args[0] != "desert") throw new ArgumentException("Supported: desert or dummy-village.");
    var output = args.Length > 1 ? args[1] : "generated/desert";
    if (args.Length > 3) throw new ArgumentException("Expected at most three arguments.");
    var seed = args.Length > 2 ? int.Parse(args[2]) : 42;
    await reporter.ReportAsync(Entry("world.generate.started", OperationSeverity.Information, "World generation started.", "started"));
    var world = await new DesertWorldGenerator().GenerateAsync(new WorldRequest(64, 64, seed));
    await new GodotWorldExporter().ExportAsync(world, output);
    await reporter.ReportAsync(Entry("world.generate.succeeded", OperationSeverity.Information, "World data and skeleton scene exported.", "succeeded"), notify: true);
    Console.WriteLine($"Generated {world.Width}x{world.Height} desert world at {Path.GetFullPath(output)}");
}
catch (Exception ex) when (ex is not OutOfMemoryException)
{
    var failure = Entry("world.generate.failed", OperationSeverity.Error,
        $"World generation failed ({ex.GetType().Name}).", "failed");
    try { await reporter.ReportAsync(failure, notify: true); }
    catch (Exception reportingException)
    {
        Console.Error.WriteLine($"Operational reporting also failed: {reportingException.GetType().Name}");
    }
    Console.Error.WriteLine($"Failed to generate world: {ex.Message}");
    Environment.ExitCode = 1;
}
