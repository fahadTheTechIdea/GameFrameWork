# P9-C5c2 — Physical recovery departure from an occupied dock

Status: historical focused-verified,2026-10-07. Continues [C5c1](P9_PARTIAL_RECOVERY_REPLANNING_DESIGN.md).
Current follow-up: [C5d retained destination-loss recovery](P9_DESTINATION_LOSS_RECOVERY_DESIGN.md).
[Evidence](../../docs/migration/P9_DOCK_RECOVERY_EGRESS_VERIFICATION.md) /
[current manifest](../../docs/migration/P9_DOCK_RECOVERY_EGRESS_SOURCE_MANIFEST.json).

## 0. Specification

An authored recovery must leave an occupied output dock physically before retargeting partially
unloaded cargo. Delivered stock, unfinished handling, original job and actual promises must not be
lost, duplicated or silently completed. All selection, movement, waiting and decisions remain engine-side.

## 1. Acceptance requirements

- One existing Job retains its original amount/worker/cargo-out token/creation and delivered prefix.
- Existing WorkBoard derives both old bay and departure claims from that job while departure runs.
- Select a feasible exit plus prospective output through existing pure dock/haul selectors.
- Departure remains actual timed movement and obeys worker/area/route/fuel/safety/occupancy gates.
- Recheck output after real departure; do not store a speculative future room or destination mirror.
- Lost output capacity causes a typed physical wait at the exit with real cargo/old inlet held.
- Clear the old bay only after reaching departure geometry; reserve new room before releasing old room.
- The next output starts its own unloading work using the existing committed baseline calculator.
- Immutable private diagnostics and the existing job codec cover departure/wait/retarget stages.
- Normal player policy changes initiate recovery; native/public cargo and saved twins continue identically.

## 2. Design

Existing Job embeds RecoveryCause and RecoveryDockSlot. A nonempty cause denotes its loaded
LeaveDock recovery phase, not a second job or scheduler. RecoveryDockSlot retains the old physical
bay until departure is actually reached. Existing claim/removal/validation rules include that slot.
Existing LogisticsService.TryReplanDestination begins departure or commits retarget after departure.
Existing WorkSystem executes LeaveDock; it must not finish a loaded recovery job at the exit.

Prospective candidate selection uses the old dock's actual departure geometry as the continuation
origin and aggregates departure plus delivery duration using existing travel contracts. The future
output is not committed during departure; capacity and trip eligibility are freshly rechecked when
arriving. If no current complete output exists, keep cargo protected and wait at the actual exit.
There is no cached route, ETA, stock amount, future-room ledger or independent clock.

During egress, worker eligibility still applies. The old service endpoint is not a remaining service
stop: its selected departure geometry and continuous route must satisfy the existing area policy.
Departure is movement, not a new request for loading/unloading at a disabled old service endpoint.
Keep request admission for future service; retain all actual departure vacancy/area/route gates.

## 3. Contracts and authoritative files

Extend existing IHaulExecution semantics, Job state and immutable WorkJobView only.
Standard/WorkDocks.cs owns departure selection, claim derivation and pure physical stage reads.
Standard/Logistics{,Recovery}.cs owns current output selection/commit/cleanup.
Standard/Work.cs owns execution, validation and embedded Sync.
Views/WorkView.cs projects exact state; WorkModule/Kernel.SaveGame identify the direct current codec.
Same master-listed recovery JSON governs eligible blockers; no fabricated/default policy.

## 4. Errors and blockers

| Situation | Engine behavior |
|---|---|
| Fixed haul/unlisted blocker/disabled recovery | No recovery departure |
| No feasible prospective complete output/exit | Existing refusal; no speculative mutations |
| Exit becomes blocked or worker disabled | Existing physical/policy wait; retained claims/cargo |
| Output room vanishes during departure | Typed destination wait after actual exit |
| Output becomes feasible later | Same job reserves new room and travels/unloads normally |
| Wrong codec/provenance/phase/retained slot | Explicit corrupt/protocol refusal |
| Cancellation | Existing owner cleanup; no cargo deletion or fabricated completion |

## 5. Persistence

Two Code identities embed in current WorkBoard job Sync: build2.202.0/save109/board39/work39/work view72.
Logistics16/maintenance2/logistics view27 and recurring schemas unchanged. Direct current migration;
no old loader, facade or second authority. Existing GameClock remains unchanged.

## 6. Verification policy

Do not rerun suites after each edit. Build once when implementation needs compilation verification.
Run only the new named dock-recovery behavior suite at the completed slice; rerun that case only to
repair a demonstrated failure. Earlier suites compile
only. No full/family gate. Explicit endpoint protocol instrumentation may vary capacity while real
Game.Update executes all movement/handling. Normal commands change player policy.

## 7. Risks and remaining scope

Output capacity is deliberately unpromised until departure; a competing job can take it, so expose
and test the physical wait instead of granting room or cargo. Departing cargo remains protected.
Broader mixed fleets, arbitrary provider failures, endpoint destruction, cancellation-origin routing,
global purity/replay/performance and full acceptance remain distinct open gates. No Oil/Tycoon/Draw/
platform/adapter/host gameplay. No engine/map size cap or second game clock.

## 8. Tasks

- [x] Existing job/claim/selector/validation/codec changes.
- [x] Timed departure, capacity wait, retarget and immutable diagnostic integration.
- [x] Clean build;81 assertions/one new named suite,0 failures/aborts. Earlier suites compile only.
- [x] Tracker reconciliation; full completion remains unproven.
