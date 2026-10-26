# P10-PV1 — Infrastructure preview boundary verification

Date: 2026-10-13. Core 2.531.0, save 154/replay 1. Existing state/view layouts unchanged.

## Concrete changes and sole owners

InfrastructureView removal previews now require a healthy session before missing/private early returns, select the actual visible line and observe the original topology/line/space. Routing previews stage local output and publish only after the actual provider, selected space and line pass existing observation guards. Link search observes both actual target entity codecs and requires placed tile presences in the selected native map. Missing/foreign/hidden/non-tile targets refuse without fabricated geometry. Invalid route-result enum or empty Found/nonempty refusal faults instead of publishing partial facts.

Existing link candidate calculation stays inside the engine view; native TileLineGeometry/NavService own route search and NetworkService owns topology/resource disposition. The remaining NetworkService demand Carries selector uses the existing definition observation guard. No new router, catalog, ledger, command, scheduler or game clock.

## Every compilation and exact-case attempt

1. First Release compile: exit 1, zero warnings/one error, 15.23 seconds. New driver omitted the actual TilePlacement position argument; no runtime started.
2. Corrected constructor compile: exit 0, zero warnings/errors, 7.11 seconds.
3. First exact runtime: 102 passed/1 failed/0 aborted in 6189 ms; PID 41288, Exited/1, process 6415 ms. New driver used a nonexistent point receiver prototype. Actual authored machine substituted as the second valid point target.
4. Fixture reference compile: exit 0, zero warnings/errors, 6.62 seconds.
5. FINAL exact runtime: 543 passed/0 failed/0 aborted in 7542 ms; PID 31116, Exited/0, process 7747 ms.

All runtime streams untruncated, no observer error. No executable edits after FINAL.

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Infrastructure previews: complete route publication removal authority and first-fault cutoff" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

Profile-info: 731 registered/included, zero excluded/long cases, zero runtime suites, exit 0. Profile metadata is not profile runtime evidence. No old exact case, family filter or full regression ran.

## Scoped executable evidence

Four tile/external-point × minute/half flows reuse the existing thin physical public-command player, without executing its old suite. Timed construction, hauling, earned removal, split/reconnect, conservation, saved twins, pause and replay remain engine-owned. After real reconnect, native route/link results are complete; point worlds return explicit unsupported tile-routing status. Missing/foreign links clear old output. Removal previews use actual original topology, reject a foreign company and leave state/hash unchanged; save/load remains identical.

Eight actual authored faults cover throw, clock mutation, mutation then original throw, and map mutation at minute/half time. Route failure leaves the supplied caller list empty. Cached own/foreign views then reject existing/missing line/link/removal, pool/flow/connection/demand/gate queries before an early refusal can masquerade as healthy. First fault and last healthy snapshot time remain stable; Update stops and save/hash refuse. A separate actual session remains unchanged.

This does not prove every arbitrary routing implementation, fog/visibility provider, direct diagnostic caller or original full acceptance matrix. Content/entity visibility checks are source-implemented; hidden-content provider breadth remains in the original privacy gates.

## Scope remaining

Original 221 criteria: 193 recorded checked, 28 open; no criterion, checkbox or line changed, no gate closed. Prior P5.009 mechanical-only Tycoon authority remains. Full engine/Oil/provider/privacy/topology/lockstep/stress/performance acceptance and goal remain open.

No Tycoon, Oil consumer, platform/adapter/Draw or clock edit. No staging, commit or push. The single authorized final full regression is deferred until implementation finishes; commit/push remains conditional on the full goal and plan finishing.

## Metadata-only checkpoint validation

1945 source anchors, 1113 selected executable inputs and 13 quickstart anchors: zero hash mismatch/post-pass executable drift. 3093 paths, 1133 Markdown documents and 8849 local links: zero broken links. Eighteen master rows are well formed. All 221 current criterion/checkbox/line checks pass unchanged: 193 recorded checked, 28 open, additional checkpoint 263.

Three existing production sources and seven existing selected inputs changed; three new files include one new executable driver. The driver boundary search has no mutable World/TestKit/RunTicks/resource/progress writer. All twelve Tycoon sources and the sole clock remain unchanged. Diff-check exit 0 with 34 existing LF/CRLF warning lines; dirty 3081, staged 0. Metadata validation does not imply full runtime acceptance.
