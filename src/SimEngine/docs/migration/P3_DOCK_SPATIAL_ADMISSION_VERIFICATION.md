# P3-S26 required dock geometry — scoped verification

Current engine2.453.0/save154/replay1. Full goal AND plan not complete; publication condition not satisfied. Original221/184checked/37open unchanged; original P3.acceptance.004 remains OPEN. This is one concrete original-owner repair, not exhaustive required-provider validation.

## Ownership and scope

`WorkDockRules.ValidateComposition(World, EntityDef)` now owns dock metadata/presence/selected-overlap admission and is invoked by original `WorkSystem.ValidateComposition`. It uses existing actual-definition/selected-space observations; no requirement registry, copied spatial state, schedule, clock, compatibility, default map or host gameplay. Foreign authored space requires valid metadata but not current geometry. Original physical queues/claims/stock/motion/save owners are unchanged. Build identity advances; payload schemas do not. Sole clock and protected G19 files remain unchanged. Advanced Tycoon, Oil consumer, SDKs/rendering/Draw, broad/performance/stress/full profiles are not touched or run.

New negative engine-side instrumentation never executes normal geometry. Actual authored `dock-spatial-admission-master.json` lists its modules and `dock-spatial-admission.json`; proof resource registration explicit. Positive factories reuse existing JSON content and normal engine owners. Drivers only author content before composition, issue ordinary player inputs, advance Game.Update/clock, and read immutable facts/save/replay. No World/TestKit/state/stock/progress mutation path.

## Evidence covered

- Unused matching-presence/no-overlap, missing provider, wrong provider ID, missing dock presence and duplicate slots: actual ContentException, zero Initialize. Missing overlap and presence messages checked explicitly.
- Valid native tile and explicit nonspatial/foreign prototypes: one Initialize, zero fabricated instances, actual selected/native or absent Space, save hashes and deterministic Update.
- Actual physical native tile minute haul32 units; independent point minute/half20 units. Exactly8 hauled. Partial loading and delivery saved; pause preserves stock/handling/time. Every ordinary continuation conserves stock/cargo and matches saved twin hashes. Carrier moves to actual receiving dock over elapsed clock time, then delivery/departure complete, source debit once and cargo zero.
- Company-private logistics returns NotOwned even where geometry is visible. Interleaved idle session unchanged; normal public replay verifies final hash.

## Bounds and terminal outputs

ONE NEW exact identity only, maximum512Updates per physical stage, eight-entry failure history, outer60000ms/termination5000/output30000. Same-case repair only; no family/full or historical runtime. Current final runtime414/0/0 in1985ms, PID38228 Exited0/process2129ms, no stdout/stderr truncation or observer error. Initial runtime411/3/0 in2177ms, PID34700 Exited1/process2336ms; three assertions incorrectly demanded invisible entity geometry. Engine correctly protects owned operational facts; corrected driver tests NotOwned, not invented geometry privacy. No executable modifications after FINAL.

### Build 1 — solution compile failed

Command:

```text
dotnet build Beep.SimEngine.slnx --no-restore --nologo
```

Terminal captured output (same build handle resumed, not restarted):

```text
  Beep.SimEngine -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\src\Beep.SimEngine\bin\Debug\netstandard2.1\Beep.SimEngine.dll
  Beep.SimEngine.Testing -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Testing\bin\Debug\net8.0\Beep.SimEngine.Testing.dll
  ExtensionProof -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\ExtensionProof\bin\Debug\netstandard2.1\Beep.SimEngine.ExtensionProof.dll
  QuickGame.Module -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\help\quickstart\Game.Module\bin\Debug\netstandard2.1\QuickGame.Module.dll
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(96,44): error CS1061: 'Code' does not contain a definition for 'HasValue' and no accessible extension method 'HasValue' accepting a first argument of type 'Code' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(112,30): error CS1061: 'IReplayRecorder' does not contain a definition for 'Record' and no accessible extension method 'Record' accepting a first argument of type 'IReplayRecorder' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(113,71): error CS1061: 'IReplayRecorder' does not contain a definition for 'Record' and no accessible extension method 'Record' accepting a first argument of type 'IReplayRecorder' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(113,50): error CS1061: 'SimGame' does not contain a definition for 'SaveReplay' and no accessible extension method 'SaveReplay' accepting a first argument of type 'SimGame' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(113,37): error CS1061: 'GameFactory' does not contain a definition for 'LoadReplay' and no accessible extension method 'LoadReplay' accepting a first argument of type 'GameFactory' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(113,22): error CS1061: 'GameFactory' does not contain a definition for 'VerifyReplay' and no accessible extension method 'VerifyReplay' accepting a first argument of type 'GameFactory' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]

Build FAILED.

C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(96,44): error CS1061: 'Code' does not contain a definition for 'HasValue' and no accessible extension method 'HasValue' accepting a first argument of type 'Code' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(112,30): error CS1061: 'IReplayRecorder' does not contain a definition for 'Record' and no accessible extension method 'Record' accepting a first argument of type 'IReplayRecorder' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(113,71): error CS1061: 'IReplayRecorder' does not contain a definition for 'Record' and no accessible extension method 'Record' accepting a first argument of type 'IReplayRecorder' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(113,50): error CS1061: 'SimGame' does not contain a definition for 'SaveReplay' and no accessible extension method 'SaveReplay' accepting a first argument of type 'SimGame' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(113,37): error CS1061: 'GameFactory' does not contain a definition for 'LoadReplay' and no accessible extension method 'LoadReplay' accepting a first argument of type 'GameFactory' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\DockSpatialAdmissionTests.cs(113,22): error CS1061: 'GameFactory' does not contain a definition for 'VerifyReplay' and no accessible extension method 'VerifyReplay' accepting a first argument of type 'GameFactory' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\Beep.SimEngine.Tests.csproj]
    0 Warning(s)
    6 Error(s)

Time Elapsed 00:00:13.40

```

### Build 2 — focused repair clean

Command:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore --nologo
```

Terminal captured output:

```text
  Beep.SimEngine -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\src\Beep.SimEngine\bin\Debug\netstandard2.1\Beep.SimEngine.dll
  QuickGame.Module -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\help\quickstart\Game.Module\bin\Debug\netstandard2.1\QuickGame.Module.dll
  Beep.SimEngine.Testing -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Testing\bin\Debug\net8.0\Beep.SimEngine.Testing.dll
  ExtensionProof -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\ExtensionProof\bin\Debug\netstandard2.1\Beep.SimEngine.ExtensionProof.dll
  Beep.SimEngine.Tests -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\bin\Debug\net8.0\Beep.SimEngine.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:08.37

```

### Attempt 1 — incorrect fixture geometry privacy

Command:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Dock spatial admission: required overlap rejects before initialization and preserves physical hauling" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

Terminal captured output:

```text
== Dock spatial admission: required overlap rejects before initialization and preserves physical hauling
dock physical: tile=True half=False
dock stage: partial loading at=7
dock stage: partial delivery at=36
dock stage: delivery and departure complete at=58
  FAIL  private actual endpoint not exposed to another company
dock physical: tile=False half=False
dock stage: partial loading at=5
dock stage: partial delivery at=24
dock stage: delivery and departure complete at=41
  FAIL  private actual endpoint not exposed to another company
dock physical: tile=False half=True
dock stage: partial loading at=9
dock stage: partial delivery at=47
dock stage: delivery and departure complete at=80
  FAIL  private actual endpoint not exposed to another company
   411 passed, 3 failed (2177 ms)
=== 411 passed, 3 failed in 1 suites (2.2 s) ===
bounded case: Exited, pid=34700, exit=1, elapsed-ms=2336, stdout-truncated=False, stderr-truncated=False, observer=

```

### Build 3 — focused privacy assertion repair clean

Command:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore --nologo
```

Terminal captured output:

```text
  Beep.SimEngine -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\src\Beep.SimEngine\bin\Debug\netstandard2.1\Beep.SimEngine.dll
  QuickGame.Module -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\help\quickstart\Game.Module\bin\Debug\netstandard2.1\QuickGame.Module.dll
  Beep.SimEngine.Testing -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Testing\bin\Debug\net8.0\Beep.SimEngine.Testing.dll
  ExtensionProof -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\ExtensionProof\bin\Debug\netstandard2.1\Beep.SimEngine.ExtensionProof.dll
  Beep.SimEngine.Tests -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\bin\Debug\net8.0\Beep.SimEngine.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:04.45

```

### Attempt 2 — FINAL

Command:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Dock spatial admission: required overlap rejects before initialization and preserves physical hauling" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

Terminal captured output:

```text
== Dock spatial admission: required overlap rejects before initialization and preserves physical hauling
dock physical: tile=True half=False
dock stage: partial loading at=7
dock stage: partial delivery at=36
dock stage: delivery and departure complete at=58
dock physical: tile=False half=False
dock stage: partial loading at=5
dock stage: partial delivery at=24
dock stage: delivery and departure complete at=41
dock physical: tile=False half=True
dock stage: partial loading at=9
dock stage: partial delivery at=47
dock stage: delivery and departure complete at=80
   414 passed, 0 failed (1985 ms)
=== 414 passed, 0 failed in 1 suites (2.0 s) ===
bounded case: Exited, pid=38228, exit=0, elapsed-ms=2129, stdout-truncated=False, stderr-truncated=False, observer=

```

## Remaining requirements

Full P3.004 includes every required spatial consumer, not only work docks. Spatial readings, other provider geometries, utility/resource/recovery/site/plan/automation/incident boundaries and broader failure/cancellation/topology cases require their own scoped source/runtime evidence. No new evidence here closes whole provider/global/full gates. All original gate text, line anchors and checkbox states must remain unchanged. No staging/commit/push until full goal AND plan verified.

## Frozen metadata closure

Current closure:1531 frozen source anchors,13 quickstart anchors,802 selected executable inputs; zero hash mismatches/post-pass executable drift.2746 paths/966 Markdown files/8587 local links, zero broken links;18 valid master rows;221 original literal gate/checkbox anchors exact and unchanged (184checked/37open), additional179. Thin driver boundary scan empty. git diff --check exit0 (34 existing line-ending-warning output lines),2734 dirty paths/zero staged. Sole clock and all three protected G19 source hashes unchanged. Twelve executable inputs added to freeze: eight existing dependencies plus four genuinely new proof/driver/JSON files. Source anchors add seven existing dependencies plus six new files. Six existing executable inputs changed (three production); no executable edit after FINAL. Metadata is scoped integrity evidence, not gameplay/full acceptance.

Executable freeze: 2026-10-11 19:16:59 UTC. Stage/evidence-only closure text is rehashed below the original executable freeze; no executable drift or additional runtime. Original broad gate remains open. A metadata patch initially failed because its hunks tried to revisit earlier anchors after the quickstart section; no partial change applied. Separate ordered ownership patches resolved the documentation operation; no executable repair or test restart resulted.
