# P3-G1 — Scenario-owned tile geometry evidence

Date2026-10-13. Engine2.533.0/save154/replay1.

## Source implementation

The existing ITileSpaceDefinition and TileSpaceDeclaration now publish/select immutable geometries for a fixed space or explicit scenarios. SpatialSpaceContracts owns canonical metadata/alias/selection validation. No old Width/Height interface shim, separate map model or mutable shared selection.

Existing EntityStore/World/Entity bind the actual immutable session identity for spawn/replacement/restore. Native motion/checkpoints and docks use selected geometry. Origin constraints receive the existing World; native dock helpers take detached geometry. Region/resource/reading/setup declarations and independent proof providers were directly migrated.

One shared Oil space declares frontier96x96, adventure128x128, continent224x224, sandbox64x64, showcase112x112 and gulf96x96. Six scenarios point to oil.tiles; old root map blocks removed and the existing master lists the new file. Source inspection found no absent/mismatched scenario binding. Oil engine composition has NOT run.

## Build observations

Framework Release test-project --no-restore:
1. Exit1, zero warnings,10 missing fixed-size dock/mover API errors,5.41s.
2. Exit1, zero warnings,5 remaining restore/dock call errors,2.94s.
3. Exit1, zero warnings,2 independent proof bound API errors,6.32s.
4. FINAL exit0, zero warnings/errors,14.75s.

All observations retained; no suite was run during these compile repairs. One refused multi-file edit with an out-of-order redundant hunk was corrected without a source mutation.

## Exact current runtime evidence

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Scenario geometry: shared prototypes isolated sparse bounds and saved continuation" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 100000
```

One new attempt:207 passed,0 failed,0 aborted,349ms case; outer537ms/PID10316/exit0, no truncation. Native and independent declarations use the same engine composer. Three different bounds share prototypes/layer identities while retaining independent sparse maps, public views, entity state, clock updates, saves and continued hashes. Metadata-negative cases cover nonpositive bounds, duplicate/unknown/missing/empty scenario identities, mixed modes, empty lists and independent dimension substitution.

No direct World/TestKit/gameplay-state mutation in the new driver. Setup comes from files and the engine world-setup owner; the driver uses GameFactory, public updates/views/saves/hash.

This does NOT prove physical movement/dock travel at variant boundaries, all variant region/resource consumers, replay/lockstep, fault-purity/provider corruption, allocation budgets or complete industrial acceptance.

## Oil integration

One completed OilCo.Game Release --no-restore compile: exit1, zero warnings,85 errors,3.64s; full output retained. Current content/mode/time/access/spatial/resource/activity/operation and optional business migrations remain incomplete. No successful Oil build/content load/gameplay test inferred.

Changed source metadata and current driver boundary checks only; no old/family/full runtime. Tycoon rules, the game clock, platform/Draw/hosts untouched. Original221/193recorded checked/28open criterion text/status/line anchors unchanged. No gate closed; no staging, commit or push. Latest user instruction: focused tests only. No full regression is authorized without a renewed explicit request.

## Metadata verification

Current audit:1,991 source anchors,13 quickstart anchors and1,145 selected executable inputs; zero hash mismatches or post-pass executable drift. All221 original criterion text/status/line anchors unchanged (193 recorded checked,28 open). Additional scoped records:270; no original gate closed. All18 master rows valid;8,658 local Markdown links checked, zero broken. Current focused driver has no forbidden direct-state boundary matches. Whole-worktree diff check exit0 (34 warning/output lines);3,102 dirty paths preserved, zero staged. Game clock and all12 recorded Tycoon sources unchanged. Goal remains open; commit/push condition is not satisfied.

