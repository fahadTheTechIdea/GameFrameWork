using GameFrameWork.Catalogs.Modern;
using System.Diagnostics;
using System.Text.Json;
using GameFrameWork.Core;

internal static class CliIntegrityTests
{
    public static async Task RunAsync(string cliPath, string root, string exportedPath,
        IReadOnlyDictionary<string, ApprovedArtManifest> manifests)
    {
        var registry = Path.Combine(root, "cli-manifests");
        Directory.CreateDirectory(registry);
        foreach (var (id, manifest) in manifests)
            await File.WriteAllTextAsync(Path.Combine(registry, id + ".json"), JsonSerializer.Serialize(manifest));

        var passed = await Run("verify-world", exportedPath, registry, root);
        using (var result = JsonDocument.Parse(passed.Output))
            Check(passed.Code == 0 && result.RootElement.GetProperty("Valid").GetBoolean(), "valid world exit/JSON");
        var failed = await Run("verify-world", Path.Combine(root, "blank-terrain.json"), registry, root);
        using (var result = JsonDocument.Parse(failed.Output))
            Check(failed.Code == 1 && !result.RootElement.GetProperty("Valid").GetBoolean() &&
                result.RootElement.GetProperty("Issues").EnumerateArray().Any(x =>
                    x.GetString() == "WORLD_TERRAIN_ASSET_REFERENCE_INVALID"), "invalid world exit/JSON");
        var missing = await Run("verify-world", Path.Combine(root, "absent.json"), registry, root);
        Check(missing.Code == 1 && missing.Output.Contains("WORLD_DOCUMENT_UNREADABLE"), "missing world diagnostic");
        var usage = await Run("verify-world", exportedPath);
        Check(usage.Code == 1 && usage.Error.Contains("Expected verify-world"), "invalid arguments fail");

        foreach (var sink in new[] { "log.jsonl", "audit.jsonl", "notifications.jsonl" })
        {
            var lines = await File.ReadAllLinesAsync(Path.Combine(root, ".gameframework", "operations", sink));
            var events = lines.Select(x => JsonSerializer.Deserialize<OperationEventView>(x)!).ToArray();
            Check(events.Any(x => x.EventId == "world.verify.succeeded" && x.Outcome == "succeeded") &&
                events.Any(x => x.EventId == "world.verify.failed" && x.Outcome == "failed") &&
                events.All(x => x.Operation == "world.verify" && !string.IsNullOrWhiteSpace(x.CorrelationId)),
                "shared operational events: " + sink);
            if (sink != "notifications.jsonl")
                foreach (var terminal in events.Where(x => x.EventId == "world.verify.succeeded"))
                    Check(events.Any(x => x.EventId == "world.verify.started" &&
                        x.CorrelationId == terminal.CorrelationId), "start/completion correlation");
        }
        Console.WriteLine("PASS: CLI integrity JSON, exit codes and log/audit/notification process checks.");

        async Task<(int Code, string Output, string Error)> Run(params string[] arguments)
        {
            var start = new ProcessStartInfo("dotnet") {
                WorkingDirectory = root, RedirectStandardOutput = true,
                RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
            };
            start.ArgumentList.Add(cliPath);
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("CLI did not start.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw; }
            return (process.ExitCode, await stdout, await stderr);
        }
    }

    private sealed record OperationEventView(string EventId, string Operation, string CorrelationId, string? Outcome);
    private static void Check(bool pass, string scenario)
    {
        if (!pass) throw new InvalidOperationException("CLI integrity regression failed: " + scenario);
    }
}
