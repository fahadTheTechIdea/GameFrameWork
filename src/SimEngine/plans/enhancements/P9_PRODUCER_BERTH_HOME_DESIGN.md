# P9-PBH1 — Producer output, service berth and worker-home separation

Completed historical consumer execution is recorded in the
[P5 handoff](../../docs/migration/P5_MARINE_FLEET_CONTINUATION_VERIFICATION.md#current2682-physical-continuation--2026-10-21).
Current harbour and all seven original vessels are complete at55407. Read-only
inspection confirms all seven AtHome without blockers. This2.682 image is preserved,
not current2.687 consumer qualification or an instruction to reset/rerun it.

Current2.687 [C16I criterion audit and recovery evidence](../../docs/migration/P9_PHYSICAL_RECOVERY_ACCEPTANCE_VERIFICATION.md#current-c16i-independent-wide-regions-and-exact-movement--2026-10-21) verifies original P9.051 again. Native and independent2x2/60-unit flows retain original loaded commitments through actual service/parking obstruction, then physical clearance, saved partial unloading, departure and production. Full region sweeps and exact movement budgets are engine-owned; current six-interruption matrix and affected faults/traffic also pass. The unchanged original requirement is checked, not a universal arbitrary-mod liveness claim or resumption of an incompatible old save. Other original/full-plan/native qualification gates remain OPEN; no consumer reset, completed marine rerun, header rewrite or publication. Earlier Current labels below are historical.

Observed2.675 two-output follow-up reached19647: both100h/60h vessels AtHome,
saved native truck delivery completes and the140h third recipe works1370/8400.
That build's multi-access layout selected truck40-unit delivery, not the original
2.647 boat60-unit commitment; do not claim that old saved cargo was resumed or
that this selected positive flow closes the full recovery/canonical matrices.

Subsequent current2.675 continuation completes ALL7 original vessels55407,
queue0/processorIdle/no jobs and all7 AtHome/no movement blockers. P5-MF1 selected
fleet task4 is revalidated; complete marine/pad/per-provider recovery matrices
and original P9.051 remain OPEN. The current handoff above owns that saved fleet.

Latest2.660 [P9-DB1](P9_DOCK_BASELINE_RECONCILIATION_DESIGN.md) resolves the two
original dock failures with valid authored1D clearance/current partial handling
and fixes the discovered World checkpoint failure cutoff.543 focused assertions;
[all attempts/limits](../../docs/migration/P9_DOCK_BASELINE_RECONCILIATION_VERIFICATION.md).
Consumer not rebuilt; older native saves below remain historical/incompatible.
General performance/berth/fleet/P9.051 acceptance remains OPEN.

Latest2.659 [P3-NC1](P3_DIAGONAL_ROUTE_COST_DESIGN.md) corrects actual shortest
path diagonal rounding, not dock/fleet performance. CURRENT consumer native
c75792a88ada4d00ad72b9bea4b73cc5 at11/hash40370764A1DBECED, preparation only.
[All attempts/identities/remaining work](../../docs/migration/P3_DIAGONAL_ROUTE_COST_VERIFICATION.md).
Older2.658 native checkpoints below remain preserved historical/incompatible.

Latest unchanged2.658 native checkpoint206d9a85888943b58838dcfb03f3aeff is at1992,
hash9C330B2441C954AF. Harbour/fleet acceptance OPEN; Update983ms needs actual
work/route/provider cost diagnosis before a long day. Shared-spatial776/0 after
negative-helper-only correction, not engine guard changes. [Current evidence](../../docs/migration/P3_SPATIAL_REVISION_PROBE_VERIFICATION.md).

Current2.658 continuation: [P3-S28.B exact hasher optimization](P3_UNIFORM_BYTE_HASH_DESIGN.md),
[all evidence/current native1451 checkpoint](../../docs/migration/P3_UNIFORM_BYTE_HASH_VERIFICATION.md).
Joint retained-journey validation stays intact. The previously failed canonical
day now passes unchanged bounds, but full harbour/fleet/berth acceptance remains
OPEN. A separate shared guard case also stays non-green with scalar A/B proof.

Current continuation: [P9-PBH1.P2 arrived-slot admission](P9_PARKED_ADMISSION_DESIGN.md),
[current2.653 evidence and retained1152 checkpoint](../../docs/migration/P9_PARKED_ADMISSION_VERIFICATION.md).
Focused parking admission is verified; canonical day still exceeds original wall
bound. Next sampled path is active-haul joint dock-journey revalidation, requiring
full original-owner audit before another generic correction. Older paths below historical.

Current continuation: [P9-PBH1.P ranked parking](P9_PARKING_ROUTE_SELECTION_DESIGN.md),
[all current2.652 evidence](../../docs/migration/P9_PARKING_ROUTE_SELECTION_VERIFICATION.md).
Original required arrival/selection owners avoid redundant search, but canonical
one-day performance gate remains failed. Current native state is minute1092 in
0c700607242e4086866844dd4adccbbe, not the older minute11/12 checkpoints below.
Full berth/fleet/offshore and original P9.051 remain OPEN.

Status: PARTIAL on2.650.0/save160/replay1. Explicit output-home policy is retained;
mobile launch/egress now uses authored dock slots and original navigation rather
than world-wide ring fallback. [Current stage](P9_CONNECTED_ROLLOUT_DESIGN.md),
[scoped checks and mandatory remaining migration](../../docs/migration/P9_CONNECTED_ROLLOUT_VERIFICATION.md).
Ten literal latest results2001/0 on the same core are not full/canonical acceptance.
Two older dock cases and one combined transformation case remain unresolved.
Engine connected rollout is implemented and scoped-verified. Actual Oil producer
layouts are now authored through original contracts: [layout stage](P9_OIL_PRODUCER_LAYOUTS_DESIGN.md),
[69 focused passes and one timed-out harbour continuation](../../docs/migration/P9_OIL_PRODUCER_LAYOUTS_VERIFICATION.md).
Complete canonical berth/fleet acceptance and performance remain OPEN. Current
native save is minute11/a937fc29fae9499985e14b03b63c7c64; measure original home-parking
selection/update cost before more blind native days or any generic repair.
[Historical2.648 parking evidence](../../docs/migration/P9_PRODUCER_BERTH_HOME_VERIFICATION.md).

## 0. Problem and specification

The current2.647 canonical harbour completes physically and the original truck
cycle is fixed. Two vessels finish their full100/60-hour recipes. A different
real obstruction appears at18147: idle manufactured survey vessel#16 is AtHome
on shipyard#13's ONLY wet contact70,73. Supply boat#17 carries60 units in job79
but cannot deliver to that occupied contact; the next140-hour recipe waits stock.

This stage must resolve the underlying producer/home/service ownership through
generic engine contracts and authored data, not a driver move, discarded cargo,
new traffic scheduler or an easier coastal fixture. The full original scope
remains unchanged. P9.acceptance.051 is reopened by this counterexample: current
original audit221/209 recorded checked/12 open, not a new completion claim.

## 1. Goals

1. Reproduce the occupied-home/service failure in a small generic authored world
   with ordinary production and transport, independent of Oil types.
2. Separate safe idle/home arrival from working service/rollout clearance while
   retaining original worker, motion, dock, job, cargo and resource owners.
3. Prove whole carrier/endpoint admission and recovery with occupied berths;
   no loaded commitment may silently become unassigned stock or disappear.
4. Require explicit contract/JSON policy where behavior is needed. No implicit
   legacy producer-home or world-wide placement fallback may substitute for an
   authored functional producer/port/egress requirement.
5. Preserve physical travel, operating costs/gates, work durations, company
   privacy, areas, cancellation, saves/hashes/replays and one GameClock.

## 2. Existing owners and design route

Read the full affected paths BEFORE selecting an API:

- Existing P9-L2.4/P6-I2 parking and request-specific berth work in
  P9_PLANS_POLICIES_LOGISTICS_DESIGN.md and
  P6_INDEPENDENT_CONTROL_INTEGRATION_DESIGN.md. Reuse those actual capabilities;
  do not assume parking requires another registry or home-policy subsystem.

- Standard/Transformations.cs: Transformer.Deliver and queue Home binding,
  existing resource-conversion/creation preflight and completion.
- Map/TileMap.cs: existing ISpatialRollout implementation FindRollout and
  CheckRolloutPlacement. Current ring placement is not a service/egress proof.
- Kernel/SpatialPlacement.cs: EntityRollout and its guarded provider boundary.
- Standard/WorkerHomes.cs: WorkerHomeContracts.ReadCurrent and UpdateIdle.
  Current AtHome reuses SpatialInteraction service contact.
- Standard/WorkDocks.cs and WorkAssignments.cs: current dock eligibility,
  occupancy/claims/departure and acquired carrier bindings.
- Standard/Work.cs: actual job ownership, MoveTo/DeferHaul/backoff and release.
- Standard/LogisticsRecovery.cs: existing TryReplanDestination and commitment
  disposition. Ordinary jobs must not be assumed to have a recovery-kind policy.

First prove whether existing dock/placement/home capabilities can express the
needed distinction. Extend those actual owners/contracts in place; do not add
a second parking registry, production queue, home binding, cargo ledger or path.
If another capability is genuinely needed, its geometry remains space-provider
owned and its policy is explicit master-listed JSON. No native TileMap-only
assumption may leak into generic production or home orchestration.

The authored design must describe producer working access, physical egress and
safe idle/home intent separately. Carrier selection must honor actual endpoint
availability/compatibility before loading, and retain/recover existing loaded
jobs through the original engine protocol if conditions change. Arrived/home
views and previews must use the same evaluator as actual movement.

Do not predetermine a global idle-yield service or new API when an existing
contract suffices. Do not solve producer access by spawning outputs across the
map or declaring overlap nonblocking. Queued worker Home overrides and any
implicit producer binding need one coherent authored policy, not a compatibility
branch. Apply movement through existing motion and operating-input contracts.

## 3. API surface and current implementation

Implemented home substage: [explicit authored output-home policy](P9_OUTPUT_HOME_POLICY_DESIGN.md).
Current partial rollout substage: [authored connected rollout](P9_CONNECTED_ROLLOUT_DESIGN.md).

The generic reproduction confirms an idle manufactured worker can occupy the
producer's sole service contact while the next transformation's carrier retains
loaded cargo and cannot unload. The correction reuses the ORIGINAL `IWorkDock`,
`IWorkDockDefinition`, `IWorker.Home`, WorkBoard slot observation/selection,
physical presence, motion, policy and job owners; no new parking service exists.

`WorkDockSlotKind.Parking = 4` is authored with `"kind":"parking"` in the
existing slot declaration. Native tile geometry still authors relative `x/y`;
independent point geometry still authors `offset`. Optional `work_role` selects
compatible workers; a handled `resource` is invalid for home parking. Parking
never matches a service, waiting or departure request. Home readiness and idle
Update use the same guarded original WorkBoard selection and physical arrival;
an authored but unavailable parking set returns a typed blocker, not fallback to
the service contact. Manual hold and acquired jobs retain precedence.

Parking-only facilities explicitly author empty `work_roles` and `resources`
arrays and nonempty parking slots. They need no fabricated loading bay or exit.
Any authored service/waiting/departure still requires both service and departure,
and service policy must be explicit. Shared `WorkDockRules` owns that admission
for native and independent definitions; an all-parking endpoint does not gain
resource or staffed service capability. Existing spatial homes WITHOUT parking
remain actual spatial interaction targets, not another docking implementation.

Explicit authored output-home selection is now implemented through the shared
recipe/queue/completion protocol; see the linked substage for exact coverage.
Connected producer rollout/egress is now implemented through optional launch
metadata on the original layout, with native and independent scoped proofs.
Actual Oil launch/berth/parking bindings are implemented through existing
module JSON, not an outstanding engine API: production.json authors explicit
output_home for all17 mobile results; structures.json supplies company base,
vehicle factory, maintenance depot and shipyard dock/rollout/parking layouts.
[Existing layout stage/evidence](../../docs/migration/P9_OIL_PRODUCER_LAYOUTS_VERIFICATION.md)
owns their migration; source rechecked on2.675 without consumer edits. Current
harbour and production continuation do not by themselves prove the whole loaded
recovery/provider/cancellation/fuel/area and canonical acceptance matrices.

## 4. Files

Existing files above, their single declarations/authoring paths, relevant
public views, exact proof fixture/master and focused tests. Native Oil content
may only supply actual module policy bindings after engine behavior is settled;
no business rules, recipe amounts/durations or canonical geography changes.
This document, master, evidence, AGENTS and README track the same stage.

## 5. Errors

No safe idle/egress location, occupied berth, disconnected route, invalid policy,
removed endpoint and incompatible carrier are typed blockers. Refused creation
must leave conversion/WIP commitments intact. Loaded job79/cargo remains actual
state; never manufacture capacity, erase receipts or auto-complete delivery.

## 6. Compatibility

Development only; no legacy loader, shim or header rewrite. Preserve the original
2.647 counterexample as historical/build-incompatible evidence. The current
handoff above owns the compatible image; continue it through normal commands
without rebuilding/resetting merely to resume or rewriting an older header.

## 7. Verification

Failing small single-service/idle-output case first. Focused checks for physical
egress/home travel, repeated production, competing deliveries, areas, fuel,
occupied/removed endpoints, saved loaded cargo, replay/twins, cancellation and
independent non-tile contract behavior. Then the canonical unchanged-durations
marine sequence through public commands/views. No full/profile/family run.

## 8. Risks

Changing home semantics can move deliberately held/manual workers or steal an
assigned carrier. Preserve manual hold and actual job ownership. Parking can
occupy another service point; authored destinations and current admission must
cover that. Recovery must neither silently retarget player intent nor duplicate
cargo. Global ring limits are not a substitute for data-owned geometry.

## 9. Out of scope

Host steering/retry/game rules, AI-only shortcuts, instant movement, extra
resources, cheaper work, Tycoon/business changes, rendering/adapters, unrelated
transport media, broad tests or Git publication.

## 10. Execution order

1. [x] Audit existing owners and reproduce the real failure generically.
2. [x] Specify and implement the smallest complete authored contract correction
   across original production/home/port owners, removing obsolete paths.
   Reconciled2026-10-20: existing explicit output-home, authored dock/parking and
   connected-rollout owners plus all four actual producer layouts are present.
   Current2.675 first native vessel earns its full recipe, rolls out on water,
   settles AtHome without a movement blocker and permits the next recipe to work.
   This closes implementation only, not the complete matrices in3/4 or P9.051.
3. [ ] Focused durability/physical/fault/provider checks and explicit identity.
4. [ ] Earn affected canonical continuation and update evidence without bypasses.

Current first slice: native and independent parking share the original dock and
home protocols. Parking-only admission, occupied/area/route retry, provider
faults, manual hold, saved motion/cargo, replay and session isolation have scoped
proof. A real retarget-at-arrival drift was reproduced and fixed through the
original guarded motion Stop before Movement. Explicit output-home policy is
subsequently implemented and scoped-verified in P9-PBH1.H. P9-PBH1.R implements
connected rollout/egress in original owners. Actual consumer layouts and berth
bindings subsequently landed in P9-PBH1.O; do not implement a duplicate migration
because an older paragraph called them unfinished. Current full occupied-berth
recovery/canonical acceptance is not inferred from source presence or historical
focused runs; the remaining complete-stage boxes stay open.

[Actual source artifact and read-only diagnosis](../../docs/migration/P3_NATIVE_TRAFFIC_PROGRESS_VERIFICATION.md).
