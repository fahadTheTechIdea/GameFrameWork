# P9-RS1 — Rank eligible dock bays before complete journey evaluation

## Evidence and scope

The2.670 fleet day still fails120532ms/120000ms despite full physical day
progress. Its retained9567 checkpoint is healthy, not completion. A bounded
48-update clone trace passes297/0; update9600 hands off an actual haul job and
takes3110.349ms/190393096 allocated bytes. Median24.3455ms is not a replacement
for the full119.452875ms mean. Concurrent managed CPU sampling attributes
52.74% inclusive to WorkBoard.SelectDock,49.73% exclusive to Bytes. Diagnostics
also include view/hash reads and profiler overhead, not a whole-engine benchmark.
Raw framework artifacts/native-cpu-sampling/aa1f4d149cd7451aba418426bb40853f.

Existing departure ranking already checks guarded costs in original ID order
then routes by the sole Cheaper comparator. SelectDock still routes an expensive
early-ID bay before a cheap later-ID feasible bay. Pruning only after a feasible
incumbent cannot remove that first wasted journey. Retained-claim fast paths and
PathSearch query-local EntryCosts already exist; do not duplicate them.

## Requirements and sole-owner design

1. Keep the existing retained-slot path and all original request/kind/area,
   claim, vacancy, queue and guarded cost checks. Never move a valid incumbent.
2. For each actual slot kind in priority order, perform original eligible reads
   in stable ID order, then rank transient candidates by existing Cheaper.
   Service stays preferable to waiting irrespective of distance. Do not query
   unused waiting kinds after a feasible service, or skip a cheaper blocked bay.
3. Invoke original complete DockJourney/continuation until the first feasible
   ranked candidate. Keep actual source/destination/departure/fuel/area/claims
   and route guards; no cached route/hash/clock or parallel coordinator.
4. On total refusal retain sticky Pending, otherwise the FIRST original-ID
   failure, including retained failure precedence. This differs from departure
   Finish's LAST original-ID refusal; do not accidentally copy that convention.
5. Failed selection publishes no selected ID, cost, arrival or travel prefix.
   Cost mutation and needed route faults remain terminal original failures.
6. Prove both authoring orders and restored/isolated sessions through existing
   independent point contracts, then selected physical/queue/provider behavior.

Implementation owner: original WorkBoard.SelectDock. OrderedSlots may return
its already-created list for indexed kind grouping. Candidate lists/estimates
exist only within one synchronous pure query, not saved/persistent authority.
No new public API, schema or compatibility path. Engine2.671.0/save160/replay1;
actual production correction advances strict build identity, not a test failure.
Never rewrite old native save headers.

## Verification stages

1. [x] Measure current actual cost and identify the existing selector gap.
2. [x] Reproduce expensive-first-ID route invocation through one new exact regression (actual0/1, losing-service-was-evaluated).
3. [x] Implement within the sole selector, preserving all refusal/guard/retention rules.
4. [x] Run only selected exact bay, physical, queue and provider checks; retain all attempts (five final scopes1705/0; baseline0/1 preserved).
5. [x] Measure actual current native impact; no claim from poison-route witnesses alone (actual2.671 fleet day211/0,112330ms/120000ms,same9567 hash as old failed day).

Use existing DockTravelSelectionTests Factory, master-listed dock-cost-points
content and PointSpace/PointDock negative hooks. No second fixture/composer,
modified original assertions, shortened work, added resources, raised deadlines,
Oil/gameplay/business/platform/Draw edits or broad/profile/family/full tests.
Transient ranking allocations may outweigh saved routes on small/tiny cases;
native performance remains open until measured. Original221/209 recorded
checked/12 open, full goal ACTIVE; no staging/commit/push.

Operational/assurance coverage reuses World.Fault/EntityObservation/native codecs,
immutable views and existing P3/P9 matrices. No new sink/channel/UI/runtime tool,
package, notification or simulation clock. Wider foundations remain open.

[All attempts, actual source/binary identity and exact next consumer stage](../../docs/migration/P9_RANKED_DOCK_JOURNEYS_VERIFICATION.md).
Consumer target rebuilt/copy identity verified with NO gameplay/content/observer
edits; nine explicit native stages567/0 now earn full harbour8111, original fleet
admission8127 and full first fleet day9567. Current day112330ms passes the SAME
120000ms bound that2.670 failed120532ms, reaching the EXACT same game-state hash.
This6.8% observed elapsed reduction is local, not statistical/universal throughput,
allocation, full fleet/sea or original compound acceptance. Five original focused
framework scopes were NOT automatically rerun; no executable/test edits afterward.

CURRENT consumer artifacts/marine/8be38c0b32dd4267871c50de85cfad65/checkpoint.json
at9567/hash31D2C58FE3414413,fleet_progress/next fleet_execution. No live worker;
next exact continuation in the handoff is NOT RUN. Preserve THIS current image,
do not reset/prepare/readmit or rewrite old2.670 headers. First vessel1268/6000,
all seven orders retained/no output yet. Selected P9-RS1 stage is implemented,
focused-verified and measured. Full fleet/marine/performance and original221/209
recorded checked/12 open/full goal remain OPEN, no original requirement closure.
