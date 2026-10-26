# P9-C5b — Loaded recovery destination replanning

Status: historical focused-verified C5b,2026-10-07. C3f preceded this checkpoint.
Current follow-up: [C5c1 partial non-docked delivery](P9_PARTIAL_RECOVERY_REPLANNING_DESIGN.md).
[Evidence](../../docs/migration/P9_LOADED_RECOVERY_REPLANNING_VERIFICATION.md) /
[current manifest](../../docs/migration/P9_LOADED_RECOVERY_REPLANNING_SOURCE_MANIFEST.json).

## 0. Problem

WorkSystem.RunJob/DeferHaul retain a haul's original destination when movement becomes invalid.
Automatic recovery cannot select a different compatible output while retaining its actual worker
and loaded-cargo promise. Fixed player hauls/routes must not silently change their target.

## 1. Goals

- Authored recovery settings explicitly list reroutable blockers; empty means no rerouting.
- Existing Job owns exact recovery provenance; no route/history/reservation mirror.
- Existing IHaulExecution replans a fully loaded delivery GoTo using existing destination preview.
- Reserve all remaining destination capacity before releasing the old promise, preserve cargo claim.
- Preserve original job identity, intent age, physical stock and actual worker; execute new travel.
- Verify normal/public separate cargo, disabled policy, fixed haul immunity and saved continuation.

## 2. Design

Extend LogisticsRecoverySettings with immutable required reroute_blockers JSON, strict unique IDs.
Job.RecoveryKind is nullable provenance assigned by LogisticsService's original acquisition and
saved once by WorkBoard.Sync. Shared Preview/PreviewDestination accept only an internal continuing
job context, using that job's existing cargo-out promise and worker eligibility/dock exclusion.
TryReplanDestination validates actual job, current fully loaded delivery GoTo, selected authored
blocker and actual cargo receipt. It replaces only destination-in promise/target/destination toils
and slot. Cargo promise, amount, worker, original creation and recovery chronology remain unchanged.
WorkSystem invokes this contract on current policy/movement failure, not a second assignment pass.

## 3. Contracts and files

Existing IHaulExecution gains TryReplanDestination(World,Job,Code). WorkJobView projects provenance.
Runtime: Standard/LogisticsRecoveryPolicies.cs, LogisticsRecovery.cs, Logistics.cs, Work.cs,
WorkContracts.cs, WorkDocks.cs, WorkModule.cs, Views/WorkView.cs and Kernel/SaveGame.cs.
Content: existing two explicitly master-listed recovery documents. Tests extend the same two suites.

## 4. Safety

Disabled/unlisted blocker/fixed job/no alternative/partial load or handling: no retarget mutations.
Wrong session/job/cargo promise: refuse explicitly. Failed provider commitment: existing session
fault, no fabricated rollback. Preserve original pickup itinerary and current native quantities.

## 5. Persistence

Build2.200.0/save107/WorkBoard37/WorkSystem37/logistics16/maintenance2/work view70/logistics view27.
One nullable recovery-kind byte is embedded in the existing WorkBoard job codec; logistics chronology
and existing resource payloads unchanged. Direct current migration, no old reader or compatibility.
Exact current build/save/work schema expectations migrated in88 test paths; other suites compile only.
Historical manifests stay historical. One unchanged GameClock.

## 6. Verification

Build, then only the two exact Recovery dispatch suites. Check no alternative atomic refusal,
fixed/manual immunity, native/public loaded reroute, old/new destination claims, original worker/
cargo/job identity/age, real new movement/unload/exit and loaded saved twins. No broad gate.

## 7. Physical boundary

This is the delivery travel phase, before unloading begins. Partial unloading requires separate
earned-prefix and occupied-dock departure semantics; it must not free a still occupied service bay.
Keep that remainder open rather than inventing elapsed handling or skipping physical departure.
WorkBoard.OccupiesDeliveryDock is a pure current occupancy check independent of changed policy;
it prevents retargeting an arrived delivery slot even while its GoTo has not yet settled.
Physical service-bay/partial-unload egress combinations remain a separate unexecuted gate.

## 8. Out of scope

New clocks, schedulers, stores, fallback resources, host gameplay, Oil/Tycoon/platform/Draw.

## 9. Tasks

- [x] Strict authored blocker contract/content; missing array/type/duplicate/blank refuses explicitly.
- [x] Current job provenance/codec/view and shared continuing preview.
- [x] Atomic physical in-transit replanning through existing owners.
- [x] Clean build and two focused suites:111 admission+130 physical=241 assertions,0 failures/aborts.
- [x] Current evidence and tracker reconciliation; no third/family/full/platform/Draw gate.
- [ ] Partial-service egress/mixed fleets/endpoint removal/replay/performance/full acceptance.
