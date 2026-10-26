# P9-DC1 deterministic dock travel selection verification

## Engine change

Core2.475.0/save154/replay1. The existing WorkBoard selector, journey admission and IWorkDock provider contract now use actual/prospective origins. Fresh choices prefer service over waiting, then minimum nonnegative provider cost, then stable identity. Valid existing claims stay retained. Future departure admission and actual departure share the same ranking. Original route, area, vacancy, queue, claims, stock, motion and sole-clock owners remain authoritative; no compatibility overload or new subsystem.

Native TileWorkDock and independent PointDock implement the changed contract directly. The original native test call is migrated but its historical runtime is not rerun. No new save payload/schema; the current engine/protocol identity records changed behavior.

## Builds and bounded attempts

One solution build only, failed9.55s with0warnings/1error: the old TileServiceDockTests call omitted the new explicit origin. Three necessary focused builds of tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore succeeded8.00s,6.15s,FINAL6.25s, each0warnings/errors.

| Attempt | Passed | Failed | Suite ms | PID | Process ms | Outcome |
|---|---:|---:|---:|---:|---:|---|
| FIRST | 239 | 2 | 1896 | 14880 | 2083 | Historical vacancy-touch check throws but does not latch a session fault; broader callback audit remains open. Replaced with actual cost-touch probe for changed callback |
| 2 | 238 | 1 | 1616 | 30340 | 1824 | New cost-touch probe missing from explicit JSON fault allowlist; declaration corrected |
| FINAL | 241 | 0 | 1596 | 21184 | 1765 | All scoped physical and cost-provider checks pass |

All processes Exited, failures exit1 and FINALexit0; zero aborted suites, observer errors or truncated streams. No executable edits after FINAL. No historical/family/full runtime.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Dock travel selection: actual prospective cost precedes stable identity without displacing physical claims" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Outer60000ms, termination5000ms, output24000chars. Each physical haul bounded160Updates.

## Public-player proof

One NEW case uses an actual embedded master-listed dock-cost-points.json and existing point-space/game composition. Its driver only authors startup JSON and submits ordinary haul inputs, previews, results, immutable views, Game.Update, saves and replay; it does not mutate world, claims, stock, geometry or progress.

Twelve physical flows cover minute/half-minute clocks and ordered/reversed authored slots for closest service, prospective pickup-origin and equal-cost cases. A farther alphabetically first bay loses to the cheaper service; a closer waiting place does not displace service. Prospective pickup-origin ties use stable identity even when the carrier starts elsewhere. Chosen claims remain through saved movement. Loading, travel, unloading and nearest actual departure happen over game time; four finite supplies leave source20 and reach destination, with source16 and cargo0. Ordered/reversed content yields identical physical chronology. Private commitments, pure previews, saved twins, replay and isolated sessions pass.

Negative-cost and cost-touch providers fault through the existing observed callback before healthy publication; a faulted session cannot save as healthy.

## Qualification and remaining work

No original gate is closed or narrowed:221/186checked/35open, additional202. This point-space case is not the all-native-tile/arbitrary-provider, dock fairness, congestion, invalid-reference, retargeting, stress/performance or complete P9/P1-P12 runtime matrix. Historical vacancy callback fault-latching remains explicitly unproved and open. Native costs remain provider estimates, not a claimed route ETA.

Canonical SimEngine only; protected G19/Tycoon/sole-clock source hashes unchanged, no Oil/platform/Draw/adapters or staging/commit/push. Full goal AND plan remain incomplete.

## Metadata closure

Fresh read-only metadata closure:1684 source anchors,935 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2872 paths/1012 Markdown files/8645 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2860/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional202. Ten existing selected executable inputs changed, two new executable inputs/four new source files, four existing production sources changed/zero new. No runtime rerun for documentation checks.

