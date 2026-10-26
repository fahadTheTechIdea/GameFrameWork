# P3-S29 shared spatial observation verification

## Implementation

ISpatialSpace now requires pure nonnegative ObservationVersion from the existing provider counter. TileMap uses its original Version; PointSpace uses its original observer counter. Immutable negative composition fixtures explicitly expose their unchanged revision; no default interface implementation, saved counter, clock or duplicate spatial data.

Original EntityObservation.SpatialScope observes actual selected alias, original ProviderScope/Sync and existing revision. It verifies getter purity and codec state as well as revision changes, including restoring original layer values. Original ReadProvider accepts ISpatialSpace, not TileMap. Fourteen common caller files no longer cast their observation argument to a tile map. Original utility read now uses that shared argument instead of a utility-specific nested spatial observation.

Original work-effect/physical-effect guards share the scope on refused commits; successful earned commits may legitimately change geometry. Original template owner uses it on pure callbacks and typed authoring refusals. First-fault handling and every gameplay state owner remain unchanged.

Existing negative access/effect probes share stateless spatial mutation instrumentation over actual point/native writers and Sync. They independently measure payload restoration and original revision changes. Original negative fault enums remain unchanged; new fault categories are explicit instrumentation only. The new driver uses actual JSON/master, public commands/views/Update; it does not mutate world/layers/stock/progress or implement callback rules.

## Build evidence

| Attempt | Scope | Seconds | Exit | Errors |
|---|---|---:|---:|---:|
| 1 | solution --no-restore | 8.87 | 1 | 1 new probe local shadow CS0136; core compiled, runtime not launched |
| 2 | focused compile repair | 11.39 | 0 | 0 |
| 3 | focused same-case/source-audit repair | 18.26 | 0 | 0 |
| 4 | focused same-case owner lookup repair | 9.58 | 0 | 0 |
| 5 | focused same-case stage diagnostics | 9.21 | 0 | 0 |

All builds zero warnings. Only the first solution command ran; no solution rerun. Four clean focused commands were dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore. Final compile preceded final runtime; no executable changes afterward.

## Runtime evidence

One NEW exact bounded case, all four attempts retained:

| Attempt | Passed | Failed | Aborted | Runtime ms | Result |
|---|---:|---:|---:|---:|---|
| 1 | 40 | 1 | 0 | 51610 | bounded physical wait diagnosed work.policy-disabled; normal player enable was missing |
| 2 | 3 | 1 | 0 | 575 | prototype-only worker lookup ambiguous across native seats; owner-scoped lookup required |
| 3 | unknown | unknown | 1 | unknown | terminal TimedOut, PID7912, process120077ms; no suite totals returned |
| 4 FINAL | 776 | 0 | 0 | 118982 | terminal Exited0, PID15976, process119175ms |

Every stream untruncated, no observer errors. Attempt3 timeout is not hidden or counted as successful checks. Diagnostic repair prints bounded stages and reads actual facts; it does not alter gameplay or extend timeout.

```powershell
dotnet tests\Beep.SimEngine.Tests\bin\Debug\net8.0\Beep.SimEngine.Tests.dll --bounded-exact "Shared spatial observation: original codecs and revisions guard physical native and point callbacks" --timeout-ms 120000 --termination-timeout-ms 5000 --output-chars 16000
```

Case120000ms/termination5000ms/output16000chars. Physical stages900 updates; refused-effect trigger180 updates. Final process runs close to its120000ms ceiling; this is NOT allocation/performance/soak certification. Earlier timeout remains evidence of a runtime-margin limitation.

## Proven behavior

Four native/point minute/half flows: authored access policies/rules, normal player worker enable and owner-scoped selection, paused build intent, selected-space template capture/preview/store, saved pending state, real loaded hauling, partial save/pause/continue, earned structure and exact30 native/eight point material consumption. Native loaded/built indices160/431 and310/854; point28/68 and52/131. Retained plan facts detach; final saves, normal command/time replay and independently interleaved sessions agree.

Two point minute/half flows physically travel, consume authored research materials and execute original private research completion (learned at44/85). Success is not mistaken for a pure callback; original work/knowledge owners retain earned results, saves/replay/isolation.

Forty-eight access callback flows: native/point, minute/half, basis/rule-check/rule-metadata, persisted layer mutation/mutation-plus-throw/revision-only restoration/restoration-plus-throw. Actual original writers changed the original revision. Restored-payload cases independently measure same Sync hash and still refuse because the original revision advanced. Original callback errors remain first; no healthy admission, cached work prefix, save/hash/update/clock continuation; independent session remains healthy.

Eight physically earned refused completion flows: minute/half and four spatial fault modes. Real partial work saves before arming engine-side fault instrumentation. Exactly one actual refused completion mutates space and cannot become healthy pending/applied state. Restoration is measured over original codec, original callback error retained, first-fault/clock/save/query cutoff and session isolation proven.

## Source audit and qualification

No remaining .Space as TileMap/Maps.TileMap observation arguments or ReadProvider tile-map dependency in current production source. All five existing spatial provider implementations migrated to the required revision contract; old/default/legacy overload absent.

Selected current native/point callbacks are proven, not every missing-provider/composition/private CLR/fog/visibility/invalidation exhaustion/claims/pressure/control/arbitrary-provider/performance/stress/global/full matrix. Original221 gate literals/checkboxes unchanged:184 checked,37 open; additional192. The original P3.acceptance.004 missing-required-provider composition gate is not closed by this callback proof. No Oil/platform/Draw/adapters/Tycoon/G19/sole clock edits. Nothing staged/committed/pushed; publication waits whole goal AND plan finished and verified.

Metadata closure: 1,630 source anchors, 878 selected executable inputs and 13 quickstart anchors match frozen SHA-256 values. All 221 original gate objects/literals/checkboxes remain unchanged (184 checked, 37 open); additional192 is scoped evidence, not aggregate completion. All 8,620 local links resolve; 18 master rows have the expected shape, the new driver boundary scan is clean, and git diff --check exits0. Source audit has zero observation tile casts; all five current spatial providers implement the required original revision. Thirty-one existing inputs changed, twenty production, with no new production owner. Sole clock/protected Tycoon hashes unchanged; no executable edit after FINAL and nothing staged.
