# P10-A13 shared physical corridor execution — scoped verification

Date: 2026-10-12 (Asia/Kuwait). Core2.460.0/save154/replay1. Existing line_work codec3, native topology4 and LineSystem1 unchanged. Full goal and plan remain active.

## Implementation

LineConstructionService is now the sole corridor admission, actual cursor/order binding, earned segment/finish, canceled cursor recovery and restored-prefix validation owner. Generic implementations were removed from native LineService. Selected ILineGeometryComposition composes geometry; ILineGeometry validates paths, current segment layer/ownership, cursor location, earned stamp and cursor movement only. Original frozen JSON LineWork and WorkBoard remain the sole path/order/progress state, and original logistics/stock/recovery/motion/clock services remain authoritative.

Infrastructure requires work/inventory, not tile navigation. Native TileMap composition alone binds original LineService to native geometry/routing/topology/flow, requires native navigation, and exposes native lay-line command/view. Independent PointSpace retains its actual sparse fixed layers and PointPresence with no tile proxy, native route/topology/flow or second spatial store. Its geometry alias is bound by original points module alongside its actual spatial service, avoiding cross-module service ownership. Shared LineSystem cleans/validates cursors and invokes optional declared native topology/flow.

Line values are explicitly authored positive Fix quantities, validated against actual layer representation. Native byte geometry retains exact integrality/range checks. Existing actual infrastructure master dependencies and omitted line values migrate in place, as do contract signatures and consuming tests; no old default, compatibility facade or parallel implementation.

## Current evidence

One NEW exact bounded case:

```powershell
dotnet tests\Beep.SimEngine.Tests\bin\Debug\net8.0\Beep.SimEngine.Tests.dll --bounded-exact "Spatial corridor execution: one physical owner across tile and point geometry" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 16000
```

FINAL198 passed,0 failed,0 aborted,5201ms. Process Exited0,PID25028,5466ms; neither stream truncated, no observer error. Runtime invokes only this same new case, never historical/family/full suites.

Four native/point × minute/half flows load actual JSON/master documents and use ordinary public seat previews, commands, results, plans/work/inspection/layer views and Game.Update. They verify pure admission, paused intent allocating/consuming nothing, actual travel and time-earned work, foreign work/plan withholding, detached facts, original bound graph, partial cursor/prefix save twins, sole-clock pause, explicit work pause/resume, earned three-segment completion, actual provider layer values, retirement, cancellation, replay and interleaved session isolation.

Native geometry consumes six of40 steel; point geometry consumes six of20 supplies through original automatic physical hauling to target stock, leaving34/14 respectively. Workers do not acquire free cargo. Point authoring uses target_resource_requirements and automatic_supply:true, with explicitly selected original construction recovery profile/container for canceled stock evacuation. Five unused malformed point-line declarations (missing/zero value, unknown layer, wrong cursor, owned without owner layer) refuse before Initialize. Probe verifies shared construction/effects alias and separate native topology/endpoint owner; independent point session has no native navigation/topology/flow owner.

## All build and runtime attempts

1. Initial solution compile: exit1,2.57s,0warnings/1error. New shared validator used foreach on non-enumerable ComponentList. Necessary production correction uses original indexed API.
2. Necessary focused compile: exit1,3.63s,0warnings/1error. New external proof referenced wrong NavService namespace; corrected existing Standard namespace.
3. Necessary focused compile: exit0,6.09s,0warnings/errors.
4. Runtime FIRST:94 passed/1failed suite exception,2369ms;PID44416,Exited1,2529ms. Point alias bound under infrastructure rather than original points owner. Native flows had passed. Alias correction stays with actual original PointSpaceDef.Compose.
5. Necessary focused compile: exit0,5.13s,0warnings/errors.
6. Runtime SECOND:106 passed/1failed suite exception,3526ms;PID24264,Exited1,3687ms. Bounded900-update diagnostic clock903/order1 Active/work.resource.stock/prefix0. Authored worker stock requirement with automatic supply disabled was unreachable from empty worker inventory. Corrected to original target supply contract and selected recovery authoring, not driver cargo mutation.
7. Necessary focused compile: exit0,12.50s,0warnings/errors.
8. Runtime FINAL:198/0/0 in5201ms,PID25028,Exited0,5466ms. All three runtimes bounded60000ms,termination5000ms,output16000chars; no truncation or observer error. Every failure preserved. No executable edits after FINAL.

## Qualification and remaining work

This is scoped physical corridor construction portability, not full P10.acceptance.003/P3.acceptance.004 closure. Arbitrary providers/owned non-tile geometry, all utility/network topology/pressure/removal, private/fault/performance/stress/global/full matrices remain required. Original221 gates/184checked/37open are unchanged; this is additional deliverable186. No Oil consumer, Draw/platform/adapters, Tycoon, protected G19 or sole clock changes. No staging/commit/push; publication still requires the whole goal AND plan finished and verified.


## Metadata closure

Post-pass freeze 2026-10-11 21:30:51 UTC: 1602 source anchors,13 quickstart anchors,863 selected executable entries. Metadata scan2791 paths/980 Markdown files/8608 local links,zero broken links,18 master rows with correct shape. No source/quickstart/executable mismatch or driver boundary matches; git diff --check exit0 (34 existing line-ending warning lines). All221 original gate texts/checkboxes/line anchors unchanged,184 checked/37 open; additional186. Thirty-four frozen executable entries changed (seven existing production sources),six new executable files (two production),three existing consuming inputs admitted to the freeze; eight new source files and three existing source anchors. Protected clock/Tycoon hashes unchanged. Dirty2779/staged0; no executable edits after FINAL or commit/push. Integrity is not full gameplay verification.
