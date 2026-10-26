# P3-S27 verification — 2026-10-14

Engine2.578.0/save157/replay1. Original A*/NavService/Mover/TilePresence/layers/setup/clock owners remain; actual selected interfaces replace concrete routing and sample dependencies. Independent provider supplies occupancy and sample storage, not another router or movement loop. Positive navigation driver submits ordinary commands and reads detached player snapshots; runtime mutation occurs only in explicitly negative callback probes.

## Build evidence

Only the targeted test project was built with --no-restore. Clean7.50s compile followed contract migration. New driver initially failed compilation because it used nonexistent EntitySnapshot (4.66s); repaired to actual EntityView, then clean5.40s. Subsequent clean targeted builds6.49s,4.38s,4.88s and final6.64s. All clean builds had zero warnings/errors. No stale binary was tested after failed compilation.

## Successful scoped cases

| Case | Checks | Case ms | PID | Observer ms | Status |
| --- | ---: | ---: | ---: | ---: | --- |
| New independent physical navigation, initial | 66/0 | 292 | 21080 | 453 | exit0 |
| New independent navigation plus occupancy probes | 82/0 | 284 | 24040 | 436 | exit0 |
| New independent navigation, final combined scope | 82/0 | 281 | 2412 | 421 | exit0 |
| Native sensor, intermediate | 64/0 | 3034 | 3968 | 3180 | exit0 |
| Independent sensor/presence, final | 73/0 | 150 | 2476 | 290 | exit0 |
| Native sensor, final | 64/0 | 2952 | 29000 | 3091 | exit0 |

Literal new case: `Independent tile world: native layered routing, physical movement and public commands`. Existing literals: `Tile sensor contracts: independent sparse space, native control and guarded samples`; `Sensor occlusion: authored screens, physical sight and saved continuation`. Final current total219 successful checks across three selected cases, not a complete regression result. Limits30s/termination5s/output9000; no successful-case truncation or observer error.

Positive independent scenario uses master-selected native layered navigation and public layer-band predicates. Ordinary WorldSetup writes a density strip through the writable sample contract and places a native mover/sensor. Preview is pure; blocked/out-of-bounds destinations refuse. Accepted movement earns partial position through the existing game clock, avoids the strip, survives mid-route save/load and pause, settles entity states and opens foreign visibility only after relocation. Twin hashes and fresh-session positions prove scoped continuation/isolation. Occupancy callbacks changing the clock/layer or throwing latch the original fault, retain original error text, refuse out-of-bounds shortcuts without retry and prohibit update/save.

## Incomplete bounded cases — retained, never counted as passes

| Case/attempt | PID | Observer ms | Outcome |
| --- | ---: | ---: | --- |
| Native layered work, initial | 25964 | 30046 | timeout, exit-1 |
| Independent predicate colony work | 9180 | 30046 | timeout; save154 expectation failed against current157 |
| Native layered work, stage markers | 5056 | 30031 | timeout after physical route selected |
| Native layered work, update diagnostics | 12512 | 30040 | timeout after rerouted-work clock7 |
| Native layered work, final combined scope | 14052 | 30026 | timeout after update25/clock32/tile[8,7]/Moving |

Native literal: `Layered navigation: authored roads, depth gates and physical saved work`. Predicate literal: `Tile condition contracts: independent JSON governs physical colony work`. Watchdog-owned processes terminated; no live handles remain. Diagnostic markers distinguish completed composition/preview, actual route selection and advancing physical travel. The final native case is slow/incomplete under its deadline, not proven deadlocked or completed. The old predicate save assertion now uses StateCodecTests.ExpectedSaveFormat; that case was not retried after the mechanical fix.

The combined-scope change removes overlapping occupancy/cost guards without skipping the original actual rule/source/space verification. This is not proof that the longer work/runtime issue is fixed. Next work must diagnose that path with stage-level evidence rather than extending timeouts or counting historical6.5s/1s results as current.

## Boundaries

No full/profile/family run. No executable edits after final current cases. No original broad checkbox closed; A47 audit/counts/hashes remain historical. Native work/predicate completion, complete provider/privacy/lifecycle/replay/lockstep/stress/allocation gates and full goal remain open. No Oil/Tycoon/platform/host changes or Git staging/commit/push.
