# P10-A7 — Infrastructure service contracts: scoped verification

Verified 2026-10-10. Core `beep-simengine-2.402.0`, save154/replay1/infrastructure-view6; persistent and detached fact layouts unchanged.

The existing `LineService` binds `ILineConstruction`, `ILineWorkEffects`, `ILineSimulation`, `INetworkTopology` and `INetworkDiagnostics`. No concrete service registration or static segment/final effect dispatch remains. Native cursor binding and saved execution identity belong to that original owner; the declaration keeps the sole path-intent identity formatter. `Begin` returns its original Order. Existing LineSystem order420 still invokes Advance -> RebuildDirty -> Flow; no foreign World argument, second clock, state store or scheduler.

Segment/final effects require their actual `ILineWorkEffects` capability. Removal requires only topology. Existing earned completion validates actual session entity/order identity and rechecks original earned admission before commit. Flow queries return the original detached private cursor facts through diagnostics with typed unknown-content refusal and cleared destination.

## Exact runtime

Only this ONE NEW case was run (five attempts of that same case):

```text
Infrastructure contracts: one owner earns physical corridor work and private flow facts
```

| Attempt | Checks passed | Failed | Aborted | Suite ms | Child PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 81 | 4 | 0 | 1290 | 21416 | 1 | 1454 |
| 2 | 81 | 4 | 0 | 1105 | 25900 | 1 | 1262 |
| 3 | 5 | 1 | 1 | 549 | 20868 | 1 | 691 |
| 4 | 25 | 1 | 1 | 929 | 2892 | 1 | 1085 |
| FINAL 5 | 95 | 0 | 0 | 1011 | 39524 | 0 | 1165 |

All children authoritatively Exited; stdout/stderr untruncated and observer errors empty. Outer30000ms, owned termination5000ms, output30000 characters, each physical wait bounded160 Updates with actual step/tile/order/blocker diagnostics. No process restarted merely because an observation expired.

Attempts1/2 incorrectly assumed live flow under the inherited gate. Attempt3's bounded live-flow wait proved that the three-tile fixture capacity30 was outside its inherited one-tile maximum10. Attempt4 admitted flow with maximum30 but filled beyond the remaining two-tile split capacity20; the original removal correctly refused. FINAL authors an explicit minimum26/maximum30 receiving-room band in fixture JSON: two earned four-unit injections retain eight units, allowing physical split. These are test content corrections, not production policy defaults or altered refusal rules.

## Build evidence

`dotnet build Beep.SimEngine.slnx --no-restore`:

| Build | Exit | Elapsed seconds | Warnings | Errors | Note |
|---|---:|---:|---:|---:|---|
| 1 | 1 | 7.73 | 0 | 4 | new driver closing-parenthesis correction |
| 2 | 0 | 7.46 | 0 | 0 | clean |
| 3 | 0 | 6.15 | 0 | 0 | clean |
| 4 | 0 | 3.90 | 0 | 0 | clean |
| 5 | 0 | 3.88 | 0 | 0 | clean |
| FINAL 6 | 0 | 3.89 | 0 | 0 | clean |

Sixteen historical native-owner inspection files mechanically select the implementation from existing provider inventory; the optional-work absence assertion selects ILineConstruction. Original assertions remain; none of these old cases were run.

## What the final proof establishes

Native/direct removal effects use the same single contracts and physical chronology. Composition/load/replay sees one persistence codec and one endpoint provider across five aliases, with no concrete binding. Ordinary player commands earn crane travel/cut, split real eight-unit pool stock, haul one actual steel unit, earn segment work and final cleanup, and rejoin original topology. Source plus pool stock30 and depot49 are conserved. Partial flow/cut/segment and completed saves restore actual execution identity; pause uses one clock. Flow privacy, unknown-content output clearing, engine-calculated line/link paths, retained detached elapsed facts, session isolation and replay are proven. Unused direct segment/final definitions without the capability refuse before Initialize.

Selected executable inventory:446 inputs, observed2026-10-09 21:45:24 UTC after final green;30 changed/new vs previous438. Exact hashes are attached to the completion audit. No executable edits after this pass.

## Remaining scope and authority

This is not full P4/P10/P1-P12 completion. Independent infrastructure service implementations, arbitrary custom provider purity/recursion, all owned/public networks, multi-segment stocked-cursor recovery, non-tile corridors, congestion/global/performance/fuzz/soak/lockstep and full regression remain open. Historical line/incident cases compile only. Tycoon resource-catalog authority remains pending. No protected clock/Tycoon file, Oil behavior, rendering, platform, staging, commit or push changes. Publication remains deferred until the FULL goal AND plan genuinely finish and are verified.
