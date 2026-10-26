using System.Collections.Immutable;
using Counter.GameLayer;
using Microsoft.Extensions.Logging;
using ScenarioEngine.Content;
using ScenarioEngine.Contracts;
using ScenarioEngine.Core;
using ScenarioEngine.Diagnostics;
using ScenarioEngine.Persistence;
using ScenarioEngine.Replay;

var packPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "counter.json");
var output = Path.GetFullPath(args.Length > 1 ? args[1] : Path.Combine("artifacts", "counter-demo"));
Directory.CreateDirectory(output);
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
using var diagnostics = new ApplicationDiagnostics(loggerFactory.CreateLogger("ScenarioEngine"),
    Path.Combine(output, "audit.jsonl"), new ConsoleNotificationSink());
try
{
    var registry = new GameLayerRegistry([new CounterLayer()]);
    var validator = new JsonSchemaContractValidator();
    var catalog = new ScenarioContentService(registry, validator, diagnostics);
    var load = await catalog.LoadAsync(new JsonFilePackSource(packPath), CancellationToken.None);
    if (!load.IsReady)
    {
        foreach (var diagnostic in load.Report.Diagnostics) Console.Error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
        return 2;
    }
    var factory = new ScenarioSessionFactory(registry, validator, diagnostics);
    var selected = load.Scenarios[0];
    IGameFactory games = new GameFactory(factory, registry);
    using var game = games.Create(selected);
    var options = new ScenarioStartOptions("counter-demo", 42, ImmutableHashSet.Create("player"));
    game.Init(new(selected, options, new(0, 0, [])));
    var hashes = new List<string>();
    var saveService = new ScenarioSaveService(new ScenarioSaveCodec(), new LocalFileSaveStore(Path.Combine(output, "saves")), diagnostics);
    for (var tick = 1; tick <= 3; tick++)
    {
        var receipt = game.Submit(new("increment-" + tick, "player", tick, 0,
            "test_counter.increment", ContractJson.Element(new { amount = 1 })));
        if (!receipt.Accepted) throw new InvalidOperationException(receipt.Code);
        var step = game.Update(new(tick, new(0, tick, [])));
        if (!step.Committed) throw new InvalidOperationException(step.Code);
        hashes.Add(step.RuleStateHash);
        Console.WriteLine($"tick={step.Tick} value={step.Snapshot.Layer.Data.GetProperty("value")} phase={step.Snapshot.Phase} hash={step.RuleStateHash}");
        if (tick is 1 or 2)
        {
            var saved = await saveService.SaveAsync("demo", game.Snapshot, CancellationToken.None);
            if (!saved.Committed) throw new InvalidOperationException(saved.Code);
        }
    }
    var current = await saveService.LoadAsync("demo", CancellationToken.None);
    if (current.Save?.Snapshot.Tick != 2) throw new InvalidOperationException("Current generation differs from the acknowledged save.");
    // Explicit recovery selection; loading a corrupt current slot never silently selects another generation.
    IScenarioSaveRecoveryService recovery = saveService;
    var loaded = await recovery.LoadPreviousAsync("demo", CancellationToken.None);
    if (loaded.Save is null) throw new InvalidOperationException("Saved snapshot could not be loaded.");
    if (loaded.Save.Snapshot.Tick != 1) throw new InvalidOperationException("Previous generation differs from the earlier save.");
    using var restored = factory.Restore(selected, loaded.Save.Snapshot, new(0, 1, []));
    for (var tick = 2; tick <= 3; tick++)
    {
        restored.Enqueue(new("increment-" + tick, "player", tick, 0,
            "test_counter.increment", ContractJson.Element(new { amount = 1 })));
        var step = restored.Step(new(tick, new(0, tick, [])));
        if (!step.Committed || step.RuleStateHash != hashes[tick - 1])
            throw new InvalidOperationException("Save continuation diverged.");
    }
    var replay = new ReplayService(factory, diagnostics);
    using var recording = replay.Record(factory.Create(selected, options, new(0, 0, [])), new(0, 0, []));
    recording.Start();
    for (var tick = 1; tick <= 3; tick++)
    {
        recording.Enqueue(new("increment-" + tick, "player", tick, 0,
            "test_counter.increment", ContractJson.Element(new { amount = 1 })));
        recording.Step(new(tick, new(0, tick, [])));
    }
    var replayBytes = replay.Encode(recording.CaptureReplay());
    await File.WriteAllBytesAsync(Path.Combine(output, "replay.json"), replayBytes);
    var replayResult = replay.Verify(selected, replay.Decode(replayBytes).Tape!);
    if (!replayResult.Matches || replayResult.ActualHash != hashes[^1])
        throw new InvalidOperationException("Recorded replay differs from the GameBase run: " + replayResult.Code);
    var result = new { layer = "test_counter", ending = game.Snapshot.EndingId, hashes,
        saveRestoreMatches = true, previousGenerationRecoveryMatches = true, replayMatches = true, nativeWorld = "not_run" };
    await File.WriteAllBytesAsync(Path.Combine(output, "result.json"), ContractJson.Canonical(ContractJson.Element(result)));
    Console.WriteLine("PASS: GameBase.Init/Update, game factory, ending, explicit previous-save recovery, continuation and portable replay.");
    return 0;
}
catch (Exception exception)
{
    diagnostics.Report(new("SIMULATOR_FAILED", "Simulator failed.", Severity.Error, Exception: exception));
    return 1;
}
