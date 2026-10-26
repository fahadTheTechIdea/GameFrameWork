# P9-PBH1.P — Cost-ranked parking route selection

Subsequent [P9-PBH1.P2](P9_PARKED_ADMISSION_DESIGN.md) refines original arrived-slot
admission on2.653; [all current evidence](../../docs/migration/P9_PARKED_ADMISSION_VERIFICATION.md).
Current native1152 is retained; this stage's2.652 results/checkpoints below are historical.

Status: implemented/scoped-verified on2.652.0/save160/replay1; canonical one-day
performance gate remains NON-GREEN. [Every attempt and identity](../../docs/migration/P9_PARKING_ROUTE_SELECTION_VERIFICATION.md).
Full P9.acceptance.051 and original221/209
recorded checked/12 open remain unchanged. Parent P9_PRODUCER_BERTH_HOME_DESIGN.md.

## 0. Problem and specification

Actual2.650 Gulf native minute11 takes2704ms for ONE healthy update, repeated
2652ms with the same resulting hashE322D6A7983800E5. A live dotnet-stack sample
shows WorkHomeParking -> PlanRoute -> NavGrid.EntryCost -> layered navigation ->
EntityObservation.SpatialScope -> full MapLayer.Sync. Original ReadHomeParking
performs reach/search and route validation for EVERY otherwise eligible slot,
including worse candidates before an already-arrived later slot. More parking
capacity therefore creates unnecessary per-update searches, not a frozen clock.

## 1. Goals

1. Separate pure actual arrival from path feasibility using the original dock
   geometry contract; no native-type checks or geometric assumptions in WorkBoard.
2. Preserve existing physical claims, vacancy, roles, areas, cost and stable-ID
   tie rules, first stable-slot blocker, and existing motion/home owners.
3. Route-check ranked eligible candidates only until the cheapest feasible slot
   is found; already-arrived eligibility performs no route search.
4. Measure route-call counts, actual saved movement, native/independent behavior
   and callback faults with focused exact cases. Do not weaken map/provider guards.

## 2. Design and original owners

Add required IWorkDock.IsAtSlot(World,Entity,Code): pure actual arrival only,
no feasibility search or motion. Native TileWorkDock uses original Goal/presence;
PointDock uses its original position rule. All three actual implementations
(including unused negative admission provider) implement the contract directly.
No optional old-method fallback or new arrival/state registry.

Original WorkBoard.ReadHomeParking first examines stable-ID slots with guarded
role/claim/vacancy/policy/arrival/cost. An eligible arrived slot wins as before.
Other candidates are transient local slot/cost/index values, sorted by original
cost/ID comparison, not persistent reservations or copied paths. Original
DockReach and PlanRoute validate candidates in that order until the first success.
Refusals retain the smallest original stable index, not cost-order accident.
If estimation refuses, actual reach is still checked to retain original typed
reach-vs-cost refusal precedence. No new clock or movement authority.

## 3. API

One required pure bool IsAtSlot on IWorkDock. No new JSON field; existing same
authoritative layouts remain. Native/point registration identities and engine
build advance; saves/replays require exact build, no header or compatibility shim.

## 4. Files

Standard/DockContracts.cs, WorkDocks.cs, WorkHomeParking.cs, TileWorkDocks.cs;
Kernel/SaveGame.cs and current-engine-protocol.json; existing native/point
registration owners; PointDockProof/DockSpatialAdmissionProof and existing
ProducerHomeBerthTests. Oil OffshoreTests only adds explicit one-update timing
and native checkpoint publication to its existing public driver, no gameplay.

## 5. Errors

Actual callback faults still latch first original error and cannot save/resume.
Negative costs reject; unsupported/unreachable/full remain typed. No winner
uses service fallback, teleport, lost cargo, hidden stock, bypassed area or
reduced layout capacity. Already-arrived motion stopping remains original code.

## 6. Compatibility

Development only, no legacy/shim/default method. Old native saves are historical
after the build changes. Fresh canonical preparation must be earned normally.

## 7. Verification

NEW exact route-count repro on2.650 before runtime fix, then same literal on new
build. Focused native physical replenishment, independent blocked/held/arrived
durability and callback guard cases selected separately as needed; no full,
profile or family runs. Native single-update baseline is scoped timing, not a
universal performance gate. Fresh preparation/one-day observation only after
focused proof, unchanged existing bounds, no blind failed-case reruns.

## 8. Risks

Do not trust cost as feasibility or infer arrival from cost0; custom geometry
owns arrival. Preserve first blocker by original index, not new evaluation order.
Pure callbacks not selected for route evaluation are not run merely for side
effects. All callbacks actually invoked remain fully guarded. Test query counters
are non-authoritative proof diagnostics, never persisted gameplay/selection data.

## 9. Out of scope

EntityObservation/map hash changes, guard removal, host logic, content/recipe/
terrain/budget edits, second pathfinder/parking system, platforms or publication.

## 10. Order

1. [x] Actual single-update timing and live-stack evidence, not a guessed hang.
2. [x] Generic failing route-count reproduction on original engine.
3. [x] Required arrival contract and ranked original-owner selection.
4. [ ] Focused physical/durable/fault verification and canonical current evidence.
5. [x] Update tracker/docs with all attempts and remaining broad gates.

Step4 focused parking/guard/native/independent proofs pass190 assertions on
current core, but one-day canonical observation remains20/1 wall-time failure.
Real native progress is retained at minute1092/hash0E0114377A84ADDB in consumer
artifacts/marine/0c700607242e4086866844dd4adccbbe/checkpoint.json. Full shipyard
clearing and subsequent physical material supply are earned, not a completed
harbour. Next measure original steady-state cost on that retained state without
raising limits, reducing capacity or adding a gameplay owner outside the engine.
