# P7-SB1 authored capacity/logistics stress and allocation budgets

## Current M71: repeated finite dock contention — 2026-10-22

Implemented as two new bounded cases of the EXISTING CapacityLogisticsStressTests,
not a new engine queue or stress runner. Native2.723/save161/replay2/450 sources
and DLL stay unchanged. File-authored four-carrier workload fills one source and
one destination service bay plus three waiting places; three rounds of eight-unit
deliveries use the original profile's speed,2048-update bound and32MiB/1GiB Update
allocation budgets. Native buffers/minute and independent buffers/half-minute
variants pass **22,475/0/two actual replays**,24 physical hauls. Native tile geometry
and native dock provider are shared; independent BUFFER is not independent geometry.
[Current results, all attempts/metrics/images and limits](../../docs/migration/P7_CAPACITY_LOGISTICS_STRESS_VERIFICATION.md#current-m71-repeated-finite-dock-contention--2026-10-22).

The driver authors JSON, submits ordinary commands, observes immutable views, calls
Update and saves/replays. Duplicate same-carrier requests refuse; material/queue
claims, physical service exclusivity, waiting without handling, saved loaded/waiting
continuation and terminal zero promises/dock claims are checked. No direct acquisition,
job/stock/spawn writes, host scheduling, second clock or collision bypass. Fixture
four carriers/72x22 geometry is not an engine ceiling. Earlier complete NG1 budget
evidence and retained SB1 failures below keep their actual historical scope; neither
was rerun. Original221/210source-recorded checked/11open/no original closure/full goal
active. Remaining finite semantic/provider evidence, incident/topology contention and
broader failure/allocation requirements remain open. Focused only, even final.

## Historical SB1 / NG1 chronology

Historical partial checkpoint on2.497.0, not rerun as an old registry case. Subsequent [P7-NG1](P7_NAVIGATION_OBSERVATION_ALLOCATION_DESIGN.md) reuses this same physical helper inside one NEW exact case and completes all tiers with the original budgets on2.498.0. Failures below remain historical evidence, not relabeled as passes.

## 0. Requirement and gap

Original P7.acceptance.004 requires capacity/logistics stress and allocation budgets with older/full runs explicitly authorized. Current engine implements finite stock, native/external endpoints, carrier reservations and physical timed movement, but has no declared focused concurrent-fleet allocation workload. The new gate measures actual Game.Update, not test assertions/authoring/hash/replay, and does not introduce an engine world-size ceiling.

## 1. Outcomes

JSON-declared 8/32-carrier fleets, four destinations and three bursts execute real hauling in minute/half-minute sessions with native storage and independent endpoint providers. Native tiled navigation and blocking loading frontages provide actual geometry; the independent point_stock provider is geometry-neutral. Same-worker competing intents refuse atomically; accepted work uses distinct carriers. Resources/promises/cargo are conserved through pickup/travel/unload, saved twins and replay; private company queries refuse. Actual managed update allocations are measured against declared workload budgets and bounded physical scenarios diagnose stalls.

## 2. Existing owners only

Author original point master/scenario/entity setup JSON with native tile space, presence, mover and navigation content to create fleets/endpoints/finite stock; never spawn or write balances from the driver. Commands/previews/results and WorkView/LogisticsView/snapshots drive observations. Existing WorkSystem, resource transactions, endpoint/cargo buffers and sole clock own everything. Native storage uses its current JSON component; external point_stock remains original proof provider. No engine source, clock, scheduling, capacity arithmetic, resource registry or transfer logic changes unless the completed gate exposes a real defect.

Allocation profile is test data only: per-update and accumulated Game.Update bytes from GC.GetAllocatedBytesForCurrentThread, no engine limit/fallback. Hash checks, queries, asserts, metadata authoring, saves/replay and twin Update run outside measured intervals. Record workload, actual maxima/totals, update count and terminal statuses. Bound each physical round and use existing external exact-case watchdog, no new timer/runner/registry.

The profile authors carrier speed600 tiles/hour, quantity8, three rounds, sample cadence16, round update bound2048, 32MiB peak Update and1GiB total Update allocation budgets per tier. Minute/half-minute clock quantization still makes journeys and loading physical, not instantaneous. The exact case/profile explicitly permits180000ms because eight save/replay stress tiers are longer than a unit test; the earlier60000ms timeouts remain evidence. Native endpoint footprints scale with the declared fleet; the fixture's72-by-(carriers+18) space is content, not an engine ceiling. Distinct new job IDs are collected per burst because original completed-job retirement deliberately removes prior generations from CopyJobs.

### Observed engine correction

The fixed32MiB/1GiB budgets failed on2.496.0. Native and independent8-carrier tiers physically completed, and32 external minute completed, but repeated A* entry-cost checks incurred excessive allocations; two actual dotnet-stack reports showed navigation spatial observation guards on the active stack. This is evidence of a hot path, not an allocation-stack profiler.

Existing PathSearch now owns reusable sparse per-search entry-cost scratch. It calls the original guarded cost callback once per queried tile, including blocked tiles and corner checks, and clears scratch before each valid search. No cost survives across routes or Updates, no provider guard is removed, and no precomputed world-size table/cache/alternate router is introduced. Provider errors still propagate. Engine identity advances to2.497.0, save154/replay1 unchanged because scratch is not authoritative state.

The same new exact case includes a pure standalone search proof: unique corridor path, once-per-tile reads, a changed-cost next search refusing, empty failed path, thrown provider callback, fresh retry and existing typed expansion budget. This standalone algorithm test has no World, gameplay clock or balance writes; the fleet driver continues using only public game surfaces. The original budgets remain unchanged pending measured verification.

## 3. Files/authoring

New tests/Beep.SimEngine.Tests/CapacityLogisticsStressTests.cs and embedded testdata/capacity-logistics-stress-profile.json; existing test project/Program/full explicit profile register one exact case. Existing real point JSON/master content reused and deterministically authored in memory, not a gameplay subsystem. Stage/evidence/master/source anchors update.

## 4. Failure semantics

Competing worker/capacity intents: ordinary typed refusal, no additional job/stock/promise prefix. Unexpected stall: bounded diagnostic with clock/jobs/stocks. Update fault: original engine fault, not fabricated compensation. Allocation overflow: explicit failed budget, not clipping workloads or introducing gameplay limits. All attempts and actual numbers retained; no broad pass inferred from counts.

## 5. Verification order

1. Author explicit workload/budget and public driver.
2. One solution build, necessary focused repairs only.
3. One new exact bounded case, same-case repairs only; minute/half native/external tiers, parallel reservation/refusal/physical accounting, saved continuation/replay/isolation, measured update budgets.
4. Metadata-only complete-profile/source/original gate audit. Close P7.acceptance.004 only if its actual workload/budget proof succeeds; other accounting/provider/topology/full/Oil gates remain open.

## 6. Limits and scope

Budgets describe this declared workload under local .NET8 Debug runner, not arbitrary worlds/hosts, wall-clock FPS/soak/CPU budgets, all dock/tool/incident/conversion/network/transaction/fault cases or P7 whole phase. No Tycoon/Oil/platform/Draw/clock modifications, legacy adapters or alternate model. No old/family/full registry run; final full regression only after implementation finishes. No staging/commit/push.

Dock service/departure queues are deliberately outside this workload. The first point layout had no passing/departure space; its stalled seven loaded carriers are preserved, not reclassified as success. A large tiled dock admission fixture exceeded the watchdog, and its performance/queue breadth remains unproved. The final shared-frontage workload does not relax mobile vacancy, route, conservation or reservation rules to bypass those findings.

## Status

PARTIAL-RUNTIME on2.497.0: per-search cost scratch compiled and measured reductions achieved; four8-carrier tiers meet the original budgets. Three measured32-carrier tiers exceed both budgets; the last32 native half replay exceeded180000ms after physical rounds completed. No terminal suite assertion summary, no complete exact-case pass and no P7.acceptance.004 closure. [Evidence](../../docs/migration/P7_CAPACITY_LOGISTICS_STRESS_VERIFICATION.md).

Next: investigate remaining route/observation allocations with precise allocation profiling, preserve all provider guards and canonical search semantics, then repeat only this bounded exact case when a completed correction is ready. Do not inflate memory budgets to claim completion. The final complete framework regression remains deferred until implementation finishes.

## Metadata closure

Initial post-attempt freeze2026-10-12 15:44:49 UTC:1784 source anchors/1001 selected executable inputs/13 quickstart anchors match actual source with0 drift. Inventory2969 files/1063 Markdown documents/8750 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original188 checked/33 open unchanged, additional228. Six previous executable inputs changed, two new inputs; two existing production files change, no new production owner or state schema. Sole clock/all12 Tycoon sources unchanged. git diff --check exit0 with34 pre-existing warning lines;2957 dirty entries/0 staged. Final documentation annotation hashes are refreshed separately; no executable edits after the final failed/timeout attempt and no complete case/full regression claim.
