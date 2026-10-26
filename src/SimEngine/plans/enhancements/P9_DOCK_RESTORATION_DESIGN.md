# P9-D5l: restore the original acquired dock delivery

Status: native focused slice verified; full P1-P12 remains OPEN.
ONE exact case209/0/0 in2109ms; THREE build attempts, final clean5.57s.
[Evidence](../../docs/migration/P9_DOCK_RESTORATION_VERIFICATION.md).
Parent: [P9 policies and logistics](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Previous: [D5k named resource docks and physical access-loss departure](P9_DOCK_ORIGINAL_BINDING_DESIGN.md).

## Engine-owned behavior

When an acquired original named port becomes available again, the same loaded haul must
finish any admitted physical egress before returning to productive service. It keeps its
actual original inbound receipt, cargo, already-earned partial handling, and original unload
baseline. Reusing owned capacity is not a fresh stock request and does not subtract or claim
that capacity twice.

The existing LogisticsService private preview owns both fresh and retained delivery admission.
It verifies the actual receipt, current acceptance, native quantum, company/worker policy,
productive dock availability, route, and trip/service constraints. A retained original delivery
does not use fresh ReceivingCeiling, Offer, or CanReserveIn. WorkBoard's one dock selector must
evaluate productive service rather than inherit departure selection from the current toil.
An occupied bay cannot be replaced by a waiting slot for a partially handled delivery.

The existing TryReplanDestination owns continuation. After actual exit settlement it first
previews the exact original physical binding when restored, otherwise uses the existing
replacement selector. Original continuation neither acquires a second inbound claim nor
releases the first; different-destination replacement retains the existing complete claim
transaction. No new scheduler, inventory, recovery state, host rules, or second clock.

## Authored proof composition

A new master-listed JSON scenario reuses the native machine, work policy, resource capacity,
handling, and recovery definitions. Its named-only facility has a current-only original alias,
a separate stable queue port, one service bay, a waiting tile, and two real departure tiles.
The second exit is explicit facility capacity, not an engine fallback or map size limit.
The old D5k content remains unchanged.

## Bounded verification

One new exact case:
`Named dock restoration: original receipt, partial handling, physical egress and busy service`.

Three player-driven episodes: restore while egress is policy-paused at the original bay;
restore at the settled exit; restore while another physical vehicle pauses in the productive
bay and later leaves through its authored alternative exit. Each physical stage is bounded
to 1000 Updates. Checkpoint twins and command replay must agree, existing partial handling
must survive, all stock/cargo/promise totals must conserve thirty actual units.

Only build and this exact case are authorized for this slice. All historical runtime cases
remain not rerun. Source/compile changes are not full regression evidence.

## Protocol and completion boundary

Direct semantic identity: core2.328.0/save154/replay1/logistics26/WorkBoard52/WorkSystem45,
work-view94/logistics-view40. Existing saved fields stay unchanged. Tile/point dock content
schemas remain3/4. No compatibility reader, platform, Draw, Oil, or Tycoon work.

Evidence document records actual build/process/check counts and residual limits.
Master/audit links update after the exact gate, without claiming full P1-P12 completion.
No staging/commit/push until the full goal AND plan are finished and verified.
