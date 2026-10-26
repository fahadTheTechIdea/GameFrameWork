# ScenarioEngine

A standalone C# scenario engine with a shared GameBase lifecycle, replaceable game layers and portable recorded replay. It contains no Godot or WWII domain dependency. Godot hosting and Lost Soldier rules remain separate next stages.

[Game-type research](docs/research/GAME_TYPE_ENGINE_CAPABILITY_RESEARCH.md) compares adventure, survival, open worlds, tactics, management and other genres against the current source. The [extension design](docs/sdd/09_GAME_TYPE_EXTENSIBILITY.md) proposes the missing shared capabilities and acceptance gates; these are research/design work, with no new gameplay implementation claimed.

The [detailed SDD index](docs/sdd/details/README.md) organizes 14 game-layer and 14 shared-extension folders. Each contains separate requirements, architecture, contracts/state, authoring, runtime/recovery and verification chapters, plus a proposed definition schema/example. Existing design decisions are maintained in their owning packages; the overview links them.

## Build and run

Use the SDK pinned in global.json. Library assemblies target net8.0 so newer .NET hosts can consume them; the executable/test hosts target net10.0. This is not proof of a particular Godot/runtime combination.

~~~powershell
dotnet build ScenarioEngine.slnx -c Release
dotnet run --project tools/ScenarioSimulator/ScenarioSimulator.csproj -c Release
dotnet run --project tests/ScenarioEngine.Tests/ScenarioEngine.Tests.csproj -c Release -- -noColor
~~~

[Verify-ScenarioEngine.ps1](tools/Verify-ScenarioEngine.ps1) runs the scoped checks and records exact commands/results under artifacts/verification.

The verifier accepts -DotnetPath for an isolated SDK and automatically detects artifacts/toolchain/dotnet/dotnet.exe. A system SDK/runtime change occurred during development; the final verifier can use the official, SHA-512-checked SDK archive in that isolated location without changing the system installation.

The Counter demo loads JSON, calls Init, submits one order per tick, calls Update, reaches an ending, writes two save generations, explicitly restores the previous one, and compares continuation and replay hashes.

## Layers

| Project | Public boundary |
| --- | --- |
| ScenarioEngine.Contracts | IGame, IGameLayer, ILayerRuntime, immutable messages and adapter interfaces |
| ScenarioEngine.Core | GameBase, ScenarioEngine, GameLayerRegistry and session factory |
| ScenarioEngine.Content | JSON Schema validation, normalized pack loading and validated catalog |
| ScenarioEngine.Persistence | Save codec/service and replaceable ISaveStore |
| ScenarioEngine.Diagnostics | IEngineReporter implementation, platform logger, audit and notifications |
| ScenarioEngine.Replay | IReplayService/IReplayCodec, bounded command/world recording and verification |
| Counter.GameLayer | Example plugin and CounterGame subclass, outside the engine |
| ScenarioSimulator | Headless composition root and runnable demonstration |

## The shared game class

~~~csharp
public sealed class MyGame(IScenarioSessionFactory sessions) : GameBase(sessions)
{
    protected override LayerIdentity GameLayer => new("my_game", "1.0.0");
}
~~~

Every game uses the same lifecycle:

~~~csharp
IGame game = new MyGame(sessionFactory);
game.Init(new GameStart(validatedScenario, startOptions, initialWorld));
game.Submit(command);
StepResult result = game.Update(tickInput);
game.Pause();
game.Resume();
game.Dispose();
~~~

Init creates/starts a scenario session and disposes a candidate if startup fails; initialization/restore/disposal are synchronized. Update advances one integer simulation tick using explicit world observations. Restore creates a session from a validated saved snapshot. The base methods enforce lifecycle; game rules implement ILayerRuntime instead of being scattered into overrides or scene callbacks.

## Create a game from registered content

~~~csharp
IGameFactory games = new GameFactory(sessionFactory, layerRegistry);
using IGame game = games.Create(validatedScenario);
game.Init(new GameStart(validatedScenario, startOptions, initialWorld));
~~~

The factory selects the common GameBase lifecycle by the scenario's registered layer ID/version. A domain may still provide its own GameBase subclass.

## Record and verify replay

Replay is a separate C# layer depending only on Contracts. IReplayService records simulation calls; IReplayCodec exports/imports the JSON tape. The demo writes artifacts/counter-demo/replay.json and verifies that its result matches the GameBase run.

~~~csharp
var service = new ReplayService(sessionFactory, reporter);
IReplayService replay = service;
IReplayCodec codec = service;
using var recorded = replay.Record(session, initialWorld);
recorded.Enqueue(command);
recorded.Step(tickInput);
byte[] bytes = codec.Encode(recorded.CaptureReplay());
ReplayResult result = replay.Verify(scenario, codec.Decode(bytes).Tape!);
~~~

The tape includes a starting snapshot/world, commands, cancellations, start/pause/resume/step/dispose calls and expected hashes for state AND returned events/requests/receipts. Verification identifies the first divergent entry. Initial content identity and SHA-256 integrity are checked. Checksums detect modification; they are not authentication.

Recording owns its session. Use the recorder exclusively while recording. It accepts bounded, serializable JSON inputs, limits tapes to 10,000 entries/16 MiB, and reserves capacity for cleanup. It does not implement Godot physics or a crash-safe append journal.

## Content source and catalog lifecycle

IScenarioContentService.LoadAsync accepts any IScenarioPackSource and propagates cancellation before registration. RemoveFromCatalog removes an exact pack/digest from new selection while existing sessions retain their immutable definitions. Audit failure leaves the catalog unchanged.

Restore validates queued operations/payloads/controllers/fingerprints, duplicate sequences, receipt relationships and phase values before accepting a snapshot. Controller matching is ordinal regardless of a caller's collection comparer.

## Input budgets and save recovery

[WireJsonRules](src/ScenarioEngine.Contracts/WireJsonRules.cs) supplies the shared payload policy for command admission and restored queues. JSON payloads are limited to 64 KiB of UTF-8, depth 32, with duplicate property names rejected at every level. World frames allow 256 observations within a 1 MiB aggregate budget, including metadata allowance. Invalid observations produce an uncommitted result while retaining the running session; invalid initial frame shapes produce a contract diagnostic before plugin construction.

Previous-save recovery is an optional capability, so existing storage implementations keep using ISaveStore. Implement IRecoverableSaveStore to expose an earlier generation. IScenarioSaveRecoveryService validates and audits it before returning data:

~~~csharp
IScenarioSaveRecoveryService saves = new ScenarioSaveService(codec, store, reporter);
SaveDecodeResult previous = await saves.LoadPreviousAsync(slotId, cancellationToken);
// Check previous.Report and restore through IGame.Restore or IScenarioSessionFactory.Restore.
~~~

LoadAsync reads the current generation only. LoadPreviousAsync reads the backup only after the host explicitly selects recovery; neither call promotes, repairs or rewrites files. Missing, unsupported, corrupt and unreadable recovery data have explicit diagnostics. The selected scenario/layer is revalidated by the session factory when restoring.

## Add a game layer

1. Implement IGameLayer with an exact ID/version, fact declarations and operation/effect/observation schemas.
2. Return a pure ILayerRuntime that takes an immutable previous snapshot and returns a candidate state/events.
3. Register the implementation in GameLayerRegistry at the host composition root.
4. Derive the game's class from GameBase and author JSON with that layer ID/version.
5. Run the same engine/adapter behavioral checks against the new layer.

[CounterLayer](samples/Counter.GameLayer/CounterLayer.cs) is a working example. PluginTests registers an unrelated boolean-signal layer alongside Counter and runs it without engine changes. Registration accepts interface implementations; it does not scan or execute DLLs named by JSON.

Content validation uses IContractValidator; data comes through IScenarioPackSource; save persistence uses ISaveStore; reporting uses IEngineReporter and INotificationSink. The provided .NET filesystem adapters belong to the standalone host. A Godot host supplies native adapters rather than replacing Godot services.

## Guarantees exercised by the checks

Command bounds/ownership, UTF-8/depth/duplicate payload guards, observation-batch limits, deduplication, cancellation, transactional failed-tick rollback, typed conditions, objective effects, ending ambiguity/refusal, PCG reference vectors, identical-input rule hashes, checksummed saves, real file replacement, explicit previous-generation recovery and save continuation.

Native Godot movement/navigation, arbitrary archive installation, complete campaign behavior, migrations from nonexistent older formats, Lost Soldier gameplay and crash/power-loss durability remain future work. Portable replay records explicit world inputs; synthetic input checks do not prove native navigation.

## Dependencies and licensing

Versions are pinned in Directory.Packages.props and per-project packages.lock.json.

- JsonSchema.Net 7.3.4: MIT release, JSON Schema provider behind IContractValidator. It is intentionally pinned after checking the package metadata; newer binary distribution terms were not adopted.
- Microsoft.Extensions.Logging.Console 10.0.13: platform logging, MIT.
- xunit.v3 4.0.2: test-only, Apache-2.0.

The dependency audit result is scoped to the current NuGet advisories. Source/license references and execution evidence live in the [SDD](docs/sdd/README.md) and [tracker](plans/MASTER_TRACKER.md). No release or broad compliance claim follows from a clean package scan.
