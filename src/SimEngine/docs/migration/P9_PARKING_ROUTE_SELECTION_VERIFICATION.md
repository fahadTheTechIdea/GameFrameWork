# P9-PBH1.P — Parking route selection evidence

Subsequent [P9-PBH1.P2 evidence](P9_PARKED_ADMISSION_VERIFICATION.md) owns current
2.653 source/results/native1152 continuation. Records below are preserved
historical2.652, not the latest compatible checkpoint or broad acceptance claim.

2026-10-18. Current engine2.652.0/save160/replay1, tile_work_dock5/point_dock7.
Original221 requirements/209 recorded checked/12 open; P9.051/full goal OPEN.
[Design](../../plans/enhancements/P9_PARKING_ROUTE_SELECTION_DESIGN.md).

## Diagnosis, not a frozen clock

The preceding2.650 native harbour day timed out150054ms before its first60-update
batch reported. New explicit harbour-step input in the existing public proof
performs exactly ONE normal update and publishes a real healthy native checkpoint.
It adds no gameplay, stock, clock, placement or scheduler logic.

On actual2.650 a937fc29fae9499985e14b03b63c7c64 minute11, first load882ms/update2704ms;
repeated load824ms/update2652ms. Both23/0 and resulting hashE322D6A7983800E5 at12.
Artifacts fb1b17c39e1b42068bc8b971003fc604 and6d5e8ec5e23242ab87f1cca162db4bba
are historical2.650, not current compatible saves.

Live dotnet-stack report sampled actual worker31028 during the second update:

```
MapLayer<byte>.Sync -> TileMap.Sync -> EntityObservation.ProviderScope.Verify
 -> SpatialScope -> LayeredTileNavigationDeclaration.Cost -> NavGrid.EntryCost
 -> PathSearch.Find -> NavGrid.ProbeStructuralRoute -> Mover.CheckRoute
 -> MotionAreaContracts.Plan -> WorkBoard.PlanRoute -> ReadHomeParking
 -> WorkerHomeContracts.UpdateIdle -> WorkSystem -> World.Step -> SimGame.RunTicks
```

This is one sampled hot path, not a full CPU profile. Source confirms original
selection searched every eligible parking slot and earlier worse slots even
when a later slot was already occupied by the worker itself. A generic route-
count reproduction proves three reach probes where one suffices, and two where
arrival requires zero:24 passed/2 failed on original2.650, not a hidden green baseline.

## Original-owner change

Required IWorkDock.IsAtSlot supplies pure actual geometry arrival without route
search or motion. Native tile, independent point and unused negative admission
providers implement it directly; no optional legacy/default path. Original
TileWorkDock.Check shares that actual arrival rule. All arrival callbacks use
existing ReadDock/EntityObservation full participant/space/clock guards.

WorkBoard alone retains selection. Stable-ID preflight checks role, actual
claim/vacancy, policy and arrival. Remaining local slot/cost/index candidates
are cost/ID sorted; existing DockReach and PlanRoute run until the cheapest
feasible candidate succeeds. No persistent home-slot binding, route copy,
reservation table or second queue. Zero cost does NOT mean arrival. Failed
candidates retain smallest original stable index for public typed blockers.

Additional refusing-estimator/blocked-route test exposed an intermediate2.651
regression:30/1, expected motion.area-outside but got blocker.unreachable. Fixed
in the same original selection owner by retaining route-before-cost refusal
precedence in this rare branch. Build2.652 makes this state change explicit;
save payload remains160, no shim/header rewrite. Final rules case31/0.

EntityObservation, map/hash algorithms, scheduler, authored layouts/capacity,
world generation, recipes/times, stock, economy and Tycoon rules are untouched.
PointDock.ReachChecks is non-authoritative test diagnostics, never persisted or
read by gameplay. Added zero/refusing cost and arrival fault probes are explicit
proof content behavior, not production fallback.

## Target builds

All builds are targeted Release --no-restore, zero warnings/errors. No full
solution/profile/family run. Build elapsed seconds in chronological order:

| Target/change | Seconds |
|---|---:|
| Consumer one-update observer | 1.90 |
| Framework generic pre-fix repro | 6.57 |
| Framework arrival/ranking2.651 | 8.19 |
| Framework tie/fault proof additions | 10.00 |
| Consumer2.651 | 1.96 |
| Framework refusing-estimator edge proof | 6.27 |
| Framework final2.652 refusal correction | 8.67 |
| Consumer final2.652 | 2.24 |

Current448-file core sorted path/hash LF SHA:
`1afc5ec739f411b7a677c936b34d282145b9e914b43699a3397b94c39f34671a`.
Core DLL and copied consumer core:
`89DE9EE2EC58526E002252366314E09BB94B4FF1B510928460AC801EAFF7F9F6`.
Current Tests DLL:
`EF35383C0FA0631F8874DA904009CC02F49C102A746984CB005E688C41623740`.
Current ExtensionProof:
`01E205373AC609A81379CF324A71A6884015149E00948F6763EA173002101D53`.
Current public proof:
`982D7FD594087613A122E8DEB2B840A6320CD163C3018E8AFCF8D6ADEC73F4C2`.
Actual structures source remains SHA
`E63495E70EE54C3F39D4C00E0DBE75EBF003851BD0CB4C71A606ED766BAE9A28`.

Intermediate2.651 core448 SHA70b28ae48d774cbff14356e1ee602fc87201b91b1cfc782b11c721212d3884e7;
DLL346DD217676D7DDF6BAEA28606D131A90A3CF6ABA880D1D545B04EB8A5A66C8B.
Earlier five-mode rules/guard/physical group Tests EE83C05CBE6DE1C9D409E452CF8D0995BF03BD4BF19F6583074FEA7982E37F18,
ExtensionProof88C0A5942E84DCB35DE0C3410CABF2A1585F8DD9A627214FAADD2A43A12B5076.
Subsequent sixth-mode test changed only Tests, then produced the30/1 failure.
Initial repro/first26-check Tests binary hashes were not captured, not invented.

## Every bounded attempt

Exact literal aliases below denote ONE selected case per process, not families:

- Q: Producer home: parking ranks costs before route probes and arrival never searches
- R: Producer home: ranked parking preserves ties route refusals and stable blockers
- I: Producer home: independent point docks share parking movement and provider fault guards
- N: Producer home: idle output clears service access for physical replenishment
- B: Producer home: occupied parking and operating areas retry through physical saved movement
- T: Producer home: arrived parking stops superseded home motion without drift
- M: Offshore: vessels survey, drill and ship from the sea (ONLY explicit input below)

All framework bounds30000ms; M step/prepare90000ms, M harbour150000ms;
termination5000ms throughout. No broader/default offshore workflow ran.

| Build | Case/input | Passed/failed | Worker ms | Outer ms |
|---|---|---:|---:|---:|
| 2.650 | M/harbour-step:a937... | 23/0 | 3916 | 4041 |
| 2.650 | M/harbour-step:a937... + live-stack sample | 23/0 | 3785 | 3908 |
| 2.650 | Q generic baseline | 24/2 | 310 | 471 |
| 2.651 | Q | 26/0 | 314 | 461 |
| 2.651 | R five modes | 27/0 | 292 | 453 |
| 2.651 | I incl.seven arrival faults | 29/0 | 394 | 556 |
| 2.651 | T | 15/0 | 320 | 477 |
| 2.651 | B | 79/0 | 373 | 542 |
| 2.651 | N | 57/0 | 1410 | 1568 |
| 2.651 | M/prepare | 24/0 | 9906 | 10030 |
| 2.651 | R added sixth mode | 30/1 | 1059 | 1500 |
| 2.652 | R six modes | 31/0 | 294 | 451 |
| 2.652 | N | 57/0 | 1296 | 1445 |
| 2.652 | M/prepare | 24/0 | 10604 | 10729 |
| 2.652 | M/harbour-step:c428... | 23/0 | 1913 | 2036 |
| 2.652 | Q | 26/0 | 310 | 474 |
| 2.652 | I incl.seven arrival faults | 29/0 | 410 | 572 |
| 2.652 | M/harbour:f9d04d57... | 20/1 wall-time bound | 130808 | 130933 |

Current six passing selected attempts190/0, PLUS one NON-GREEN harbour20/1;
this is not a clean regression. Prior2.651 B/T results
are historical, not rerun on final core. Original older non-green dock and
combined transformation cases remain open; do not erase or call them green.

N physically manufactures one output, clears service, hauls real replenishment,
finishes later production with conservation, saved twins, replay, privacy and
isolation. Q proves 3->1/2->0 route counts, pure queries, real saved physical
arrival. R covers cost/ID ties, zero-cost-not-arrival, route fallback, stable
blockers and refusing estimator precedence. I covers physical independent saved
motion/private facts and actual arrival endpoint/carrier payload/version, space,
clock and throwing failures, retaining original first fault without resumption.

## Current canonical evidence

2.651 prepare earned eb95435a350e400b9c41bb5ea860ff82 at11/hash40370764A1DBECED.
It is now HISTORICAL/INCOMPATIBLE, preserved with exact headers.
2.652 separately earned c4289e8167a1482ea1820f9c6ca56c61 at11/same world hash;
strict engine build is still different. Original96x96seed1, two humans/seat1,
engine-selected yard71,73, field21, full authored work and no Tycoon/cash.

Final single native update load821ms/update828ms; original baseline load882ms/
update2704ms (repeat824/2652). Current checkpoint f9d04d57fc2e4d6cb9dfd33e55b1591f
at12/hash9DE9BDEA2B959F10, harbour_progress/next harbour_execution. Visible
positions and work progress match the baseline observation; this is not a
cross-build whole-hash equality claim or global performance guarantee.

All checkpoint IDs above resolve under actual consumer
`src/oilCo-engine/src/SimEngine/artifacts/marine/<id>/checkpoint.json`.
Native save/load/disk hash verification uses the original writer/strict loader.

The final2.652 harbour day from f9d04d57... reached minute1092, then correctly
FAILED the original120000ms per-stage bound:129852ms at a completed60-update
boundary, worker130808ms/outer130933ms,20 passed/1 failed, terminal process.
Outer150000ms/one-day/25-day limits were unchanged. This is NOT a passed day.

Healthy native progress was preserved at
`0c700607242e4086866844dd4adccbbe/checkpoint.json`, minute1092/hash0E0114377A84ADDB,
harbour_progress/next harbour_execution. Actual full16h shipyard clearing is
complete; its erection waits physical stock. The dozer now earns20/480 ticks
clearing the depot. Two actual trucks have jobs4/5 with32.666/20 units of loaded
cargo; one unloads at the site, the other moves from HQ. All three projects,
original amounts/durations and healthy native state remain. The partial cargo
is not recreated, refunded or discarded. This is the CURRENT compatible save,
superseding minute12 for future diagnosis, not a fabricated/rewritten artifact.

Selection optimization is proven and initial timing improved, but remaining
steady-state work/hauling/home cost still misses this original day gate. Next
measure bounded queries/individual updates on this actual1092 state before
changing an original owner. Do not enlarge limits or blindly replay further
days. No general CPU/allocation/soak result is inferred from the single sample.

Full canonical berth/fleet/offshore, arbitrary providers, stress/soak/allocation
and original twelve gates remain OPEN. No full tests, business changes, platform
work, staged paths or publication. The active goal is not complete or blocked.

Final source hygiene: framework diff --check exit0 with35 existing line-ending
warnings/no other findings; changed consumer observer path exit0/no warnings.
Both repositories staged0; no live bounded/worker-exact processes. Fresh original
requirement-text/checkbox audit221/209 recorded checked/12 open, unchanged.
No executable or authored content edits followed the final runtime observations.
