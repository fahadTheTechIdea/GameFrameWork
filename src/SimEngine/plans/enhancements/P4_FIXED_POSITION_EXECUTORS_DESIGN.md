# P4-G3d — Fixed-position activity execution and typed re-approach

## Scope and ownership

Remove the executor contract's mandatory travel-index result. A physical executor may instead
return a typed wait with an explicit blocker. Add a genuinely different authored executor family:
fixed-position work uses one productive phase, never invents a travel/departure phase, and only
admits an actual worker already within physical reach. All jobs, materials, progress, cancellation,
state settlement and persistence remain in the existing WorkSystem/WorkBoard/entity owners.
No renderer, platform, host, business or second clock/scheduler/state store is included.

## Contracts and JSON

- Replace ApproachIndex with ActivityApproachResult: explicit ResumeAt(index) or Wait(blocker).
  Default/malformed values fault. WorkBoard validates resumed indices against the same captured
  phase catalog; wait keeps the actual phase and delegates to the existing pause operation.
- IActivityExecutor.CheckWorker takes the existing WorkRequirementContext and returns a pure Code.
  The existing candidate selector calls it before route/material admission. Native staffed/timed
  definitions explicitly allow their existing semantics, without a compatibility/default method.
- fixed_activity_executors explicitly names id, work phase and access_blocker in master-listed JSON.
  Validate worker roles and a composed productive-purpose phase before Initialize. This first fixed
  executor intentionally refuses dock targets with a typed unsupported-dock blocker: it must not
  reserve a physical bay without a valid exit protocol. Dock-capable fixed machinery remains open.
- One productive Toil at the actual target, no cargo/amount. The same itinerary predicate validates
  admission and restore. Rate delegates to the existing IWorkerDispatch; missing access returns
  the authored wait. No progress is earned and no materials are charged while waiting.

## Integration

Candidate previews and actual acquisition share executor admission. Lost reach during an assigned
job returns the typed executor wait; native productive phases and dock replanning propagate it
through the existing job/node pause facts. Ready updates retry the actual phase, not a second timer.
No job/codec/view bytes change. Strict engine build identity advances; save format stays139.

## One new bounded acceptance case

Use native master/startup/work/navigation JSON only, no proof-specific gameplay module. Author a
stationary worker adjacent to a movable service target. Player commands move that target away and
back over game time. Prove initial out-of-reach candidate blockers, one-phase execution, earned
partial work/materials, retained job/phase/progress while access is lost, physical recovery, private
immutable diagnostics, cancellation, saved twin continuation, isolated sessions and strict invalid
authoring. Every stage has a bounded Update loop and current time/job/node/blocker diagnostics.

## Verification and handoff

Build once after the completed slice; run ONE exact distinct NEW bounded case. Repair/rerun only
observed failures, recording every invocation. Older build assertions are source/compile migration
only. No older/family/full/platform/stale gates. Freeze source/tests/JSON after final green; then
update evidence, master/umbrella tracker, AGENTS and scoped hash/link manifest, docs only.
No commit/push until the FULL goal and plan are complete and verified.

## Remaining gates

This is not all G3: dock-enabled fixed machinery, process/haul/multi-endpoint/team executors,
generated workflows/templates(G4), broader provider/purity/fault/replay/stress/CPU proofs and richer
ETA/diagnostics(G5), plus all remaining P1–P12 requirements, stay open.

- [x] Contracts, authored definition and same-owner runtime integration.
- [x] One new bounded native player acceptance case and focused verification.
- [x] Evidence, trackers and scoped source inventory.

Verified2026-10-07: THREE builds (5 import errors8.92s,1 namespace error1.61s,clean3.52s),
ONE exact NEW bounded case63 checks0 failures/aborts360ms first runtime attempt. Source/tests/JSON
frozen after green.106 older build assertions compile ONLY. [Evidence](../../docs/migration/P4_FIXED_POSITION_EXECUTORS_VERIFICATION.md).
