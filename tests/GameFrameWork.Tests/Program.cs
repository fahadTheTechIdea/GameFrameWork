using System.Text.Json;
using GameFrameWork.Core;
using GameFrameWork.Core.Operations;
using GameFrameWork.Worlds;
using GameFrameWork.Godot;
var generator = new DesertWorldGenerator();
var req = new WorldRequest(32, 32, 123, Biome.Desert);
var first = await generator.GenerateAsync(req);
var second = await generator.GenerateAsync(req);
Assert(JsonSerializer.Serialize(first) == JsonSerializer.Serialize(second), "Determinism");
Assert(first.Cells.Count == 1024, "Grid dimensions");
Assert(first.Cells.Any(c => c.Kind == CellKind.Road), "Roads");
Assert(first.Objects.Any(o => o.Kind == "gate"), "Gate metadata");
var dir = Path.Combine(Path.GetTempPath(), "gameframework-test-" + Guid.NewGuid().ToString("N"));
try
{
    await new GodotWorldExporter().ExportAsync(first, dir);
    Assert(File.Exists(Path.Combine(dir, "GeneratedWorld.tscn")) && File.Exists(Path.Combine(dir, "world.json")), "Export outputs");
}
finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }

var blueprint = new WorldBlueprint(1, "test-city", req, new[]
{
    new AssetRequirement("warehouse", "building", 8, 6, new[] { "S", "N" }, new[]
    {
        new ComponentRequirement("door", "warehouse", "door-hinge", true, AnimationTechnique.Frames,
            new[] { "closed", "opening", "open", "closing" })
    })
});
var empty = new InMemoryRegistry(null);
var missing = await new WorldBuildGate(empty).CheckAsync(blueprint);
Assert(!missing.Ready && missing.Issues.Any(i => i.Code == "ART_MISSING"), "Missing GameArts asset rejection");
const string hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
var manifest = new ApprovedArtManifest(1, "warehouse", "approved-r1", "7.5",
    ArtApprovalState.Approved, new[]
    {
        new SpriteSheetManifest("door-s", "door_s.png", "door_s.json", hash, hash, "door", new[] { "S" })
    },
    new[]
    {
        new ComponentAttachment("door", "warehouse", "door-hinge", 0.4, 0.2, new Dictionary<string, PixelAnchor>
        {
            ["S"] = new(8, 13)
        })
    });
var incomplete = await new WorldBuildGate(new InMemoryRegistry(manifest)).CheckAsync(blueprint);
Assert(!incomplete.Ready && incomplete.Issues.Any(i => i.Code == "ANIMATION_SHEET_MISSING"), "Incomplete direction sheet rejected");
Assert(incomplete.Issues.Any(i => i.Code == "DIRECTION_ANCHOR_MISSING"), "Missing direction attachment rejected");
var full = manifest with
{
    Sheets = new[]
    {
        manifest.Sheets[0],
        new SpriteSheetManifest("door-n", "door_n.png", "door_n.json", hash, hash, "door", new[] { "N" })
    },
    Attachments = new[]
    {
        manifest.Attachments[0] with
        {
            DirectionAnchors = new Dictionary<string, PixelAnchor> { ["S"] = new(8, 13), ["N"] = new(8, 12) }
        }
    }
};
var approved = await new WorldBuildGate(new InMemoryRegistry(full)).CheckAsync(blueprint);
Assert(approved.Ready, "Structurally complete art manifest accepted; byte/atlas verification remains separate");
var revoked = await new WorldBuildGate(new InMemoryRegistry(full with { ApprovalState = ArtApprovalState.Revoked })).CheckAsync(blueprint);
Assert(!revoked.Ready && revoked.Issues.Any(i => i.Code == "NOT_APPROVED"), "Revoked art rejected");

// Durable operational service contract test: log, audit and user-facing notification are separate sinks.
var operationsDir = Path.Combine(Path.GetTempPath(), "gameframework-ops-" + Guid.NewGuid().ToString("N"));
try
{
    var logPath = Path.Combine(operationsDir, "log.jsonl");
    var auditPath = Path.Combine(operationsDir, "audit.jsonl");
    var notificationPath = Path.Combine(operationsDir, "notifications.jsonl");
    var reporter = new WorldOperationReporter(new JsonLineOperationStore(logPath),
        new JsonLineOperationStore(auditPath),
        new ConsoleAndFileNotificationService(new JsonLineOperationStore(notificationPath)));
    var evt = new OperationEvent("world.validation.failed", DateTimeOffset.UtcNow,
        OperationSeverity.Warning, "tests", "world.validate", "correlation-test",
        "Invalid placement rejected.", "test-world", "test-runner", "failed");
    await reporter.ReportAsync(evt, notify: true);
    foreach (var output in new[] { logPath, auditPath, notificationPath })
    {
        Assert(File.Exists(output), "Operation sink file created");
        var stored = File.ReadAllText(output);
        Assert(stored.Contains("world.validation.failed") && stored.Contains("correlation-test"), "Correlated operation persisted");
    }
}
finally { if (Directory.Exists(operationsDir)) Directory.Delete(operationsDir, true); }
Console.WriteLine("PASS: world model and art contract structural tests (not PNG/atlas/Godot runtime verification).");
static void Assert(bool pass, string message)
{
    if (!pass) throw new Exception("Failed: " + message);
}
sealed class InMemoryRegistry(ApprovedArtManifest? manifest) : IApprovedArtRegistry
{
    public Task<ApprovedArtManifest?> ResolveAsync(string assetId, CancellationToken cancellationToken = default)
        => Task.FromResult(manifest);
}
