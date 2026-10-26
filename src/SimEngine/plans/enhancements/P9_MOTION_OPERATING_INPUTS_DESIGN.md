# P9-B7 — Resource-backed motion operating inputs

Status: focused verified, 2026-10-06. Build2.105.0/save64/new motion inputs1/motion view2/
movement system4/work system5; existing owner payloads unchanged. **119 checks/two exact suites**
(63 contracts;56 physical), zero failures/aborts, clean build.
[Evidence](../../docs/migration/P9_MOTION_OPERATING_INPUTS_VERIFICATION.md). P9/P1–P12 remain partial.
No compatibility or legacy path.

Follow-on [P10-A8 motion utility consumers](P10_MOTION_UTILITY_CONSUMERS_DESIGN.md) replaces the
motion-only resource snapshot with generic utility facts while retaining this physical draw/exposure
owner. Its current focused evidence supersedes this historical build, not the full P9 acceptance gate.

Follow-on [P9-B10 phase-neutral operating inputs](P9_WORKER_OPERATING_INPUTS_DESIGN.md) directly
replaces this motion-only API/key/concrete owner with one movement/idle/standby protocol. Current
2.170.0/save95 passes279 assertions/two exact suites covering the promoted motion tests and new worker
phases; identities/counts above remain historical, not the current gate. No legacy reader remains.

## 0. Problem statement

MovementSystem.Update currently advances ready IMotion providers without resource expenditure.
ResourceRates.Between and IResourceTransaction already own quantized elapsed consumption and whole
inventory admission. Extend those owners; do not introduce a fuel balance, remainder or scheduler.

## 1. Goals

1. Explicit master-listed JSON declares named motion inputs using existing native resource rates.
2. All providers' next draws are admitted together against actual unreserved IInventory stock.
3. Only ready moving attempts consume inputs and advance saved exposure, once per engine phase.
4. Shortages retain actual position/goal; physical hauling can resupply the same inlet.
5. Immutable company-private motion diagnostics, native staged codec and public extension proof agree.

## 2. Design

IMotionOperatingDefinition exposes Inputs; IMotionOperatingInputs exposes ReadExposure and engine-issued
AdvanceExposure. Native MotionOperatingInputs owns elapsed operating ticks and last accepted tick only.
MotionOperatingContracts computes cumulative ceiling-quantized resource draws using ResourceRates and
the actual store capacity policy, stable named identity and pure-provider guards. Ceiling prepayment
prevents free startup: zero-draw intervals use previously paid native-quantum credit.

MotionExecutionContracts.Read includes the aggregate operating budget in its existing view/blocker.
MovementSystem commits one ResourceInventoryIntent through IResourceTransaction, then verifies exact
exposure changes before invoking the sole actual IMotion.Advance. Stock and reservations may not change
during exposure commits. Existing WorkSystem readiness therefore also sees input shortages when travel
is required. No new work queue, command, supply owner, auto-refuel or enabled-state mirror.

Rates describe an **active motion attempt tick**, not distance or speed. IMotion owns route planning;
a ready attempt can expend resources while searching/replanning without earning distance. Idle,
disabled, zero-rate, work-readiness and shortage waits expend nothing. Distance-specific operating
models require a separately specified provider contract; they are not inferred here.

## 3. API surface

New named definition/provider, exposure/context and detached aggregate/provider snapshots. Existing
MotionView remains the private facade. Native JSON requires id, inputs and states.disabled; existing
ResourceRateSpec remains the sole rate parser. Ceiling is the explicit contract semantics, not a
configurable fallback. Empty input arrays are explicit non-consuming capabilities.

## 4. Files to change

Standard/MotionOperatingInputs.cs; MotionExecution.cs; Movement.cs; MotionModule.cs; build identity;
public-only proof provider and authored fixture; two exact test suites; stage/master/parent/evidence/
README/AGENTS/offline help. No host/demo/OilFieldDays/Tycoon/Unity/Godot/rendering changes.

## 5. Error handling

Missing motion/inventory/transaction or mismatched metadata/catalog/quantum fails before gameplay.
Missing unreserved stock yields motion.operating-inputs; representational next-draw overflow yields
motion.operating-range. Malformed/mutating exposure providers, duplicate tick or non-exact commits
fault explicitly. Native decoding stages both fields before replacement; restored future chronology
is checked against actual session time. Faulting providers do not imply global rollback guarantees.

## 6. Backward compatibility

None. Revise the active contracts and manifests directly; no aliases, parallel readers or shims.

## 7. Verification

Clean solution build and only these two exact suites (same-name repairs allowed):

- Motion operating inputs: authored contracts and whole resource admission
- Motion operating inputs: timed expenditure, physical resupply and saved continuation

Earlier suites compile only, never a full-regression claim.

## 8. Risks

Shared inventory cargo promises must stay protected by transaction Available admission. Fuel-specific
cargo/tank selection and fleet reserve policies remain open; tests use separate ordinary couriers and
a motion-only consumer, not a hidden cargo filter or automatic fuel grant. Scoped provider checks are
not global rollback or exhaustive fuzz/replay/allocation evidence.

## 9. Out of scope

Automatic refuel/service dispatch, reserve/minimum-load/fleet policies, distance/idle/work consumption,
new media, broader replay/lockstep/stress/full P9 and full framework acceptance.

## 10. Execution order

- [x] B7.1 contracts/native owner and whole resource admission
- [x] B7.2 existing movement/readiness/view/checkpoint integration
- [x] B7.3 public extension and two exact verification gates
- [x] B7.4 evidence and aligned tracker/help

## 11. Scoped results

Strict required content/catalog/quantum/filter/motion/inventory bindings; pure private detached facts;
multiple native named owners and independent public provider/five fault modes; staged malformed/truncated
decode/current-clock/range refusal; aggregate stock admission and protected outbound promises pass.
Idle/disabled/zero-rate/shortage waits, prepayment boundaries, retained actual goals/positions, real
courier load/travel/unload/refill, route-attempt semantics, mixed count/liquid consumption, existing
flow accounting and interrupted/interleaved saved twins pass. Physical stages are bounded to200 ticks.
39 preceding identity assertion files and old movement/work schema assertions migrate/compile only;
previous suites are not rerun. No full regression or full framework completion claim.
