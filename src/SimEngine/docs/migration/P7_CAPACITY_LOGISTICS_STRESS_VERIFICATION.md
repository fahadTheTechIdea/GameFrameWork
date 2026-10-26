# P7-SB1 capacity/logistics stress evidence

## Current M71: repeated finite dock contention — 2026-10-22

SELF-REVIEW via installed sdlc-orch implementation/verification/handoff procedures,
snapshot `a80d41ad0affd81b549f5f063787e186d162e37f`. Canonical Beep.SimEngine/master
HEAD `cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work. Previous
M70 added actual physical WIP/material accounting proof: progress. Its current
matrix explicitly leaves dock/incident/topology contention outside old capacity
stress; this continuation addresses that concrete dock gap, not another re-audit.

### Existing owners and new file-authored proof

Unchanged native **2.723.0/save161/replay2**, all450 native sources and native DLL
identical to M70. No engine defect/new native feature, queue/index, ledger, stock/book,
router, provider fallback, state payload, clock, business rule or platform change.
Original WorkBoard derives dock claims/waiting order from actual jobs; original
native tile dock/geometry, movement/navigation, logistics, resource transactions,
checkpoint and clock owners perform every game decision and physical operation.

The EXISTING CapacityLogisticsStressTests partial gains two individually selectable
literals and one Docks partial. It reuses the ORIGINAL factory, Profile and Accounting
helper, not a second stress driver/model. New embedded `dock-contention.json` authors
four carriers, one destination and service/waiting/departure geometry. Before engine
composition, this dock declaration is folded into the original master-selected
scenario content. Existing source/worker content, quantities, navigation and actual
typed source/destination buffers stay with their original owners.

Each variant has one source bay, one destination bay, three waiting places at each
and actual departure locations. All four carriers contend for those same endpoints
for three rounds of eight-unit batches:12 physical hauls, finite source96→0 and
destination0→96. The original profile retains600 tiles/hour,2048 Update bound per
round,33554432 peak Update bytes and1073741824 total Update bytes PER TIER. No limit
is increased or quantity/time shortened. Four carriers/72x22 geometry is scenario
content, not a framework world-size or fleet ceiling. Original8/32-carrier full case,
all-tier NG1 chronology, old profiles and all other registry cases were NOT run.

### Actual player-facing coverage

The new driver performs only JSON setup, command preview/submission/results,
immutable views, Update, Save/Load, replay and allocation observation. It never
directly acquires hauls, edits/spawns entities, modifies books/jobs/progress, picks
queue winners, steers vehicles, ignores collisions or grants stock. Existing
helper Entity reads the actual public snapshot; no duplicate locator was added.

- Submit four ordinary haul commands and four duplicate same-carrier requests per
  round. Queuing advances no game time. First requests acquire actual resources/
  carriers; duplicates refuse without extra jobs or claims. New rounds retain fresh
  stable job IDs even when old completed rows retire.
- Observe original stock/cargo conservation and representable funded promises every
  Update, private-company isolation and distinct assigned workers. One service claim
  per actual bay and at most one physical carrier occupying each service tile.
  Waiting pickup earns no loading/handling progress.
- Save the reserved queue and an actual checkpoint containing both loaded movement
  and waiting jobs. Loaded twins continue with identical per-Update hashes. No test
  scheduler, instant handling, invented parking, queue state or collision exception.
- Each complete round delivers32 once; final carrier cargo, all endpoint promises
  and both original source/destination dock claims are empty. The sole resource
  history contains Initialized/StockTransfer only, no hidden creation/purchase/
  consumption/loss/destruction balancing duplication. An idle session is unchanged.
- Verify the real full command/refusal/queue/hauling replay for each variant. Measure
  managed current-thread allocation around primary game.Update only; observer/
  twin/hash/test assertions are outside that interval. No wall-FPS, native-memory,
  all-thread allocation, hosted-provider or whole-engine performance claim.

### Every attempt, final image and metrics

Initial new partial compile FAILED13.33s: three uses of the original static Entity
helper lacked its file-scoped using-static import. Added that original helper import,
not another lookup. Build2 clean12.26s. Both initial literal runtimes passed22,455/0,
two replays; all outputs retained. Final adds explicit zero terminal material promises
and cleared dock claims. Build3 clean6.43s, only those two affected new literals rerun.
No unrelated prior green case reran after test-only edits.

| Final exact scope | Checks | Suite / observer ms | PID/exit | Updates / hauls | Peak / total Update bytes |
| --- | ---: | ---: | --- | ---: | ---: |
| Dock contention: native buffers repeated physical queue bursts preserve saved resources and allocation budgets |7708/0|6218 /6371|27012/0|272 /12|5508304 /208621328|
| Dock contention: independent buffers half-minute physical queue bursts preserve saved resources and allocation budgets |14767/0|9171 /9315|8184/0|525 /12|5377704 /373872592|

Final **22,475/0**,24 physical hauls and **two actual replay verifications**, one
captured build3 image. Both exit0, untruncated, all original budgets met per tier.
Initial build2 native7698/0 (5480/5630ms,PID11516) and independent14757/0
(9638/9809ms,PID29500) remain interim, not added to final count. The independent
buffer tier changes the inventory implementation and quantum, not native dock or
tile geometry. Selected native/minute and independent/half-minute are not an unrun
full cross-product proof or every future provider. A harmless guessed Check.cs
read failed during discovery; no engine/test failure is hidden.

[All builds/runs/metrics](../../artifacts/optional-module-durability/b829d81571bd4c10a4e9240ee0153fe2/attempts.json),
[final build identities](../../artifacts/optional-module-durability/b829d81571bd4c10a4e9240ee0153fe2/build-3-inputs.json),
[frozen inputs](../../artifacts/optional-module-durability/b829d81571bd4c10a4e9240ee0153fe2/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/b829d81571bd4c10a4e9240ee0153fe2/structural-checks.json),
[original gates](../../artifacts/optional-module-durability/b829d81571bd4c10a4e9240ee0153fe2/original-gates-current.json).
Targeted build only: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
Each table literal uses Release/net8.0 tests DLL with `--bounded-exact`,
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.

### Scope and continuation

This adds missing finite repeated dock-contention/allocation proof, not global
fairness under endless work, every named/scoped/shared-geometry provider, all
incidents/topology/faults, continuous manufacturing or broad fuzz/soak/lockstep.
Existing source-described shared/cross-company queue and negative provider tests
retain their actual earlier images and scope; they were not relabeled reruns.
No original requirement closes:221/210source-recorded checked/11open, full goal ACTIVE.
Original P7.004 keeps its actual later NG1 closure, not an invented new M71 closure.
Next finish remaining finite semantic/provider accounting and genuine simultaneous
incident/topology contention/failure/allocation obligations through existing owners;
do not rerun unchanged reference/budget families or create a parallel scheduler.

Affected typed refusals/results, first-fault handling, private detached views and
flow records reuse existing owners. Wider [operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain; self-review is not independent review or external sink/outage/retention
qualification. Headless UI/renderer/store/distribution is N/A. No dependency,
full/default/profile/family/soak even final, consumer/Oil/marine, business-rule,
platform/Draw work, staging/commit/push or extra branch/PR. Publication waits for
the entire goal AND plan.

## Historical SB1 / NG1 chronology

Historical partial checkpoint on2.497.0, not rerun as an old registry case. Subsequent [P7-NG1 evidence](P7_NAVIGATION_OBSERVATION_ALLOCATION_VERIFICATION.md) records the completed unchanged workload/budgets on2.498.0 inside one NEW exact case. Preserve all failures/timeouts below.

## Outcome: partial, original gate open

Existing PathSearch now reuses pure entry costs only inside each synchronous search. Scratch is cleared for the next search; original guarded callback, structural/dynamic occupancy, corner checks, expansion bound and canonical tie-breaking remain. No alternate router/provider guard/state/clock owner or world-size ceiling. Engine2.497.0/save154/replay1; scratch is not saved. The original32MiB peak Update and1GiB total Update budgets have NOT been increased.

Seven fleet tiers emitted metrics. Four8-carrier tiers meet both budgets. Three measured32-carrier tiers exceed them. Last32 native half-minute tier completed all three physical rounds at clock236, but replay was still running when the180000ms watchdog terminated it. No terminal suite assertion summary and no complete case pass. P7.acceptance.004 and all other original gates remain open/unchanged:221 total,188 checked,33 open; additional228.

## Authoritative surfaces and workload

Actual master/point JSON is deterministically authored into native tile geometry and navigation:8/32 carriers, four shared-stock destinations, three rounds,8-unit cargo, explicit600 tiles/hour,60s/30s quantum. Blocking loading frontages and movable carrier occupancy stay authoritative. The independent point_stock endpoint provider is geometry-neutral; native storage uses its existing component. Empty native stock is an empty bill, not a fabricated supply:0 entry.

The driver submits real haul previews/commands, competes twice for each actual carrier, reads results and immutable public work/logistics/spatial facts, and advances Game.Update. Stock/cargo/promises conservation, distinct reserved workers, new stable job IDs across retired generations, private-company filtering, saved twins, replay and idle-session identity are checked. No World, TestKit, RunTicks or gameplay balance/progress mutations in this fleet driver.

Pure standalone PathSearch tests have no World/session/gameplay clock: a unique corridor, at-most-once entry reads, changed callback on a later search, blocked empty path, thrown callback, fresh retry and typed expansion-bound refusal. No printed failure in this portion, but the enclosing case did not finish.

## Measured Update allocations

GC.GetAllocatedBytesForCurrentThread is sampled immediately around Game.Update only. Queries/assertions/hashes/JSON authoring/save/replay/twin Update are excluded. This is local .NET8 Debug managed current-thread allocation evidence, not all-thread/process/CPU/FPS/GC/soak certification.

| Carriers | Endpoint | Quantum | Updates | Hauls | Peak Update bytes | Total Update bytes | Result |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 8 | independent | 60s | 109 | 24 | 13730984 | 207998056 | within budgets |
| 8 | independent | 30s | 206 | 24 | 13572280 | 343311872 | within budgets |
| 8 | native | 60s | 109 | 24 | 13936296 | 209920008 | within budgets |
| 8 | native | 30s | 206 | 24 | 13777512 | 345264736 | within budgets |
| 32 | independent | 60s | 124 | 96 | 74839832 | 1271478752 | FAIL budgets |
| 32 | independent | 30s | 236 | 96 | 70428048 | 2072106776 | FAIL budgets |
| 32 | native | 60s | 124 | 96 | 75679000 | 1279396216 | FAIL budgets |

Before optimization, corresponding8 external minute/half peaks were56764736/56116328 and totals704433560/1153809040. Native8 peaks56970448/56321160.32 external minute peak346320464/total5204864704. Comparable peaks decreased about75-78%; physical completion clocks stayed34/72/109 and63/135/206 for8,40/81/124 for32 minute. Two dotnet-stack samples showed NavGrid.Cost / spatial observation guards during A* work. Stack sampling identifies a hot path, not exact allocation attribution; further allocation profiling remains necessary.

## Bounded attempts, all retained

1. Original point fixture:3768 passed/2 failed,18619ms; queued hash expectation was wrong and a1D route lacked passing/departure space. Seven loaded carriers stalled within2048 updates.
2. Corrected command/stock assertions:3770 passed/1 failed,11996ms; first truck parked at point12 and blocked positions11..5. No physical vacancy rules were relaxed.
3. Tiled32-service/departure-slot fixture: TimedOut, PID44732,60740ms, no emitted metrics. Dock breadth/performance remains unproved.
4. Blocking tiled frontage: TimedOut, PID40136,60044ms; first8 external minute physically completed three rounds, but two assertions assumed retired jobs remained visible.
5. Corrected job ID accounting/explicit speed:1251 passed/4 failed,50595ms, PID1288/process50984ms. First8 tiers exceeded budgets; native authoring incorrectly supplied a zero quantity in an empty bill.
6. Empty-bill repair, preoptimization2.496.0: TimedOut, PID4724,180062ms; five tiers emitted budget failures/metrics before32 half. Two stack samples captured the navigation hot path.
7. FINAL ATTEMPT after search scratch on2.497.0: TimedOut, PID43536,180041ms; seven metrics above, six printed budget failures, last tier physical completion but replay incomplete. No stdout/stderr truncation or observer error. Passed/failed/aborted suite totals unavailable; do not infer a green/zero-abort result.

The first tiled compile output was not retained after tool truncation; its status is not invented. Missing TileEntitySnapshot references were corrected to actual TileSpatialSnapshot, with subsequent clean compile. Seven retained successful builds: solution21.92s; focused7.86s/13.36s/9.46s/7.96s/9.96s/9.85s, all0 warnings/errors. No executable edits after FINAL ATTEMPT.

## Exact commands and metadata-only selection

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Capacity logistics stress: authored concurrent fleet budgets and physical conservation" --timeout-ms 180000 --termination-timeout-ms 5000 --output-chars 24000
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --profile-info tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json
```

Earlier attempts used60000ms; latest profile entry explicitly180000ms. Profile-info:696 registered/included,0 excluded,0 game suites executed, exit0. This only validates complete selection metadata; no complete profile/full executor run. No old/family/full game suite executed.

## Remaining work and authority

Original allocation stress gate is OPEN. Investigate remaining32-carrier route/observation allocations without weakening provider purity/physical rules or inflating budgets. Dock queues, congestion stress, incident/topology/cargo/quality/global provider fault matrices, arbitrary worlds, complete profiles and final acceptance are not closed by this workload. Scope SimEngine only; all12 Tycoon sources and sole clock unchanged, no Oil/platform/Draw implementation edits. No staging/commit/push; full goal and plan remain incomplete. The authorized one final full regression is deferred until implementation finishes.

## Source freeze

Initial post-attempt freeze2026-10-12 15:44:49 UTC:1784 source anchors/1001 selected executable inputs/13 quickstart anchors match actual source with0 drift. Inventory2969 files/1063 Markdown documents/8750 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original188 checked/33 open unchanged, additional228. Six previous executable inputs changed, two new inputs; two existing production files change, no new production owner or state schema. Sole clock/all12 Tycoon sources unchanged. git diff --check exit0 with34 pre-existing warning lines;2957 dirty entries/0 staged. Final documentation annotation hashes are refreshed separately; no executable edits after the final failed/timeout attempt and no complete case/full regression claim.
