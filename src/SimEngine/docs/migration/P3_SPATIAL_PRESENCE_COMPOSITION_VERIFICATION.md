# P3-S22 — spatial presence composition verification

## Implemented engine boundary

Engine2.422.0/save154/replay1. The existing ISpatialPresenceDefinition now requires pure ValidateComposition(World, EntityDef). World.FinishComposition checks capability/definition parity and reads every actual presence Space through EntityObservation, then invokes selected-space declarations before system initialization. Unused definitions are included; other authored spaces impose no geometry on a nonspatial session. TilePresenceDef requires the actual selected TileMap service alias. Independent PointPresenceDef requires the actual PointSpace. Existing provider-side shape acceptance remains the provider's responsibility, distinct from each definition's required-provider admission; no replacement map, service, scheduler or state owner.

The lifecycle skill guided stage documentation and reuse of actual owners. Negative proof providers/capabilities never simulate positive gameplay. Healthy fixtures use the original master-listed world/routes declarations, WorldSetup, TileMap/NavService/Mover, independent PointSpace/PointPresence and physical processor/work/resource owners. Drivers submit ordinary commands, call Update and read public detached facts. No runtime world, inventory or progress writes.

## Exact proof and scope

ONE NEW exact case: `Spatial presence composition: unused geometry contracts require actual providers before initialization`.

Native and independently authored tile-space cases reject a missing selected provider, wrong provider identity, a same-ID provider of the wrong geometry, and a provider missing an authored layer. Selected presence-only definitions have no instances and no movement consumer, so wrong-geometry admission cannot rely on startup or navigation checks. All refuse before Initialize. Direct unused presence declarations deliberately mutate the actual clock, throw, or mutate then throw. Original first exceptions survive without finally-based masking; Initialize remains zero. Valid empty tile sessions save/load without invented instances. Other-space content coexists with an explicitly nonspatial local session and no fallback map.

Healthy native and independent tile sessions execute normal input through actual blocked geometry: travel advances over time without teleport, routes do not cross the obstacle, partial and terminal saves restore, saved twins stay hash-identical, pause earns no movement, retained snapshots remain detached, commands replay and interleaved sessions stay isolated. Independent point presence composes without tile-navigation and performs ordinary physically supplied alloy production through the existing engine. It also verifies pure command preview, pending save/twin continuation and replay. Tile travel bounded96 Updates and point production256 Updates.

This is additional scoped deliverable130. Original221 acceptance words/checkboxes remain159 checked/62 open. Broader P3.acceptance.004 higher-level construction/rollout/recovery/custom provider matrices, layer callback purity, full private-query breadth, scale/performance, Oil/full regression and full P1-P12 remain OPEN. Protected G19 authority pending; no Tycoon/platform/Oil/Draw/compatibility changes or staging/commit/push.

## Actual compile and runtime attempts

1. Solution compile: exit0,15.59s,0 warnings/0 errors. The returned live build handle8925 was polled to its terminal exit0; no process was restarted.
2. Same-case targeted repair compile: exit0,6.52s,0 warnings/0 errors, all five dependency projects.

FIRST runtime:16 passed/1 failed/1 aborted suite,122ms; owned PID33944 exit1, observed277ms, outcome Exited. The valid-empty fixture mistakenly added a generic ISpatialPresence against a TileMap, which correctly rejected that incompatible shape. The valid-empty fixture now uses the actual unused native tile definitions only. The direct generic declaration remains negative-only and never spawns or updates. No production admission was weakened.

FINAL runtime:81 passed/0 failed/0 aborted,1351ms; owned PID19968 exit0, observed1521ms, outcome Exited. Both attempts had untruncated stdout/stderr and no observer error. No executable edits after FINAL.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Spatial presence composition: unused geometry contracts require actual providers before initialization' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

Outer deadline30000ms, owned termination5000ms, output30000 chars. No historical/family/full/Oil/Tycoon/platform runtime suite was invoked. Focused evidence cannot prove a full regression.

## Post-FINAL source and metadata audit

Freeze 2026-10-10 03:53:54 UTC:601 selected executable inputs;9 changed/new paths versus the previous599-input checkpoint, including2 new proof/driver files. No executable post-FINAL drift. Current audit has1271 source anchors and13 quickstart anchors; all hashes agree. Refreshed204 original requirement line anchors without changing any original wording or checkbox. Original221/159checked/62open and130 additional scoped deliverables remain distinct; no aggregate completion inferred.

Read-only audit indexed2537 repository paths,874 Markdown documents and7400 local links:0 broken links. All18 master rows retain5 columns;0 malformed rows. git diff --check exit0,34 existing line-ending warning lines. New driver boundary scan has0 world/stock/progress mutation matches. Actual git status --porcelain=v1 --untracked-files=all reports2527 dirty paths and0 staged paths. Dirty source is preserved; these counts do not imply ownership of unrelated changes. Sole GameClock.cs and all three protected Tycoon sources match their previous SHA256 hashes. Goal remains active and full P1-P12 remains OPEN.
