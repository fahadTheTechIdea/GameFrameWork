# P9-D5k: Named resource docks and physical lost-access departure

Status: native focused slice verified; full P9/P1-P12 remain OPEN.
FINAL98/0/0 in1386ms; TWO clean builds16.50s/8.69s.
[Evidence](../../docs/migration/P9_DOCK_ORIGINAL_BINDING_VERIFICATION.md).

## 0. Problem statement

TileWorkDockDef.ValidateCore and external PointDockDef.ValidateCore insist on IInletPort even when
an entity has compatible named IResourcePortDefinition access. WorkSystem.RunJob then repeatedly
classifies the lost original binding during an acquired recovery departure, cancelling/reopening
the loaded job before its existing departure executor can move the carrier.

## 1. Goals

1. Validate resource service through compatible named port contracts or an existing unnamed inlet.
2. Preserve the original exact receipt, real partial-unload residual, actual carrier and job.
3. Execute acquired lost-access departure physically, obeying normal policy and geometry gates.
4. Retain the old service-bay claim until actual departure, then permit an ordinary queued carrier.
5. Wait at the real exit if no complete replacement fits, without promising future capacity.
6. Prove policy pause/resume, physical queue, partial unload, departure, saved waits, replacement,
   timed completion, replay and exact local conservation through normal JSON/player commands.

## 2. Design

WorkDockRules.ValidateResourceService is the common content admission owner; native tile and external
point dock definitions delegate. Named definitions must explicitly accept every served resource.
No inventory fallback, copied stock, platform geometry or new service scheduler.

ReadRecoveryStage reports Waiting only after the retained bay has been cleared by actual departure
execution; reaching exit geometry alone before the next native execution phase remains Departing.
Core2.327.0/save154/logistics25/WorkBoard51/WorkSystem44/work-view93/logistics-view39.
Tile dock content schema3/point dock4; saved layouts unchanged.

WorkSystem.RunJob recognizes its same already-acquired DestinationBindingLost recovery cause.
That existing departure phase is governed by current work/dock/motion rules rather than treated as
a new failed service request. Physical site loss remains distinct and retains its explicit recovery
permission. Original LogisticsService/WorkBoard own all selection, claim derivation, motion and release.

Normal JSON authors one named-only tile facility, service/wait/departure slots and two machines.
An ordinary stock/routing edit initiates recovery after real partial unloading. Work policy pauses
the departing carrier while a second ordinary haul physically waits. The bay remains claimed until
actual exit; replacement stock policy creates capacity only through existing demand admission.

## 3. API surface

WorkDockRules.ValidateResourceService(EntityDef, ResolvedContent, IReadOnlyList<ResourceId>).
No new command, job, queue, stock/claim authority, geometry owner or clock.

## 4. Files

DockContracts.cs, TileWorkDocks.cs, Work.cs, semantic protocol/views;
existing external PointDockProof/PointSpaceProof and composition;
normal master-listed dock scenario, one new exact player proof; stage/evidence/tracker/audit.

## 5. Errors

Absent/unsupported resource access fails content validation. Invalid provider identity/mutation still
faults. Existing physical occupancy/area/worker/route rules pause execution. Too-small replacement
does not acquire partial receipts, delete cargo, release original capacity or teleport the carrier.

## 6. Compatibility

Direct development migration only; no old JSON/save reader, shim, duplicate subsystem or default.

## 7. Verification

Compile as necessary; ONE NEW bounded exact case only, rerun only to repair its demonstrated failures.
Each physical stage has a failure bound/diagnostic snapshot. Use commands/views/Update/save/replay,
never world/stock/claims/progress mutation in the driver. No historical/family/full/Oil/platform runtime.

## 8. Risks

Departure is not new service admission; policy still controls real movement. Free physical bay vacancy
alone must not remove its retained job claim before reaching the exit. Replacement remains unpromised
until physical departure. Named access must satisfy each explicitly served resource.

## 9. Out of scope

Actual backing-store/provider destruction, arbitrary faults/corruption, fuel/weather/emergency/stress
breadth, global/full acceptance, platforms/Draw/Oil/Tycoon and publication before the full goal/plan.

## 10. Execution

Revise shared content admission and original departure execution; author normal scenario;
add one thin bounded proof; build/verify; repair only demonstrated failures; reconcile evidence,
master tracker and audit without aggregate phase/goal closure.
