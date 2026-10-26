# P4-G3d fixed-position executors — scoped verification

2026-10-07. Canonical Beep.SimEngine only. FULL goal/plan remains ACTIVE. No commit/push.

## Actual invocation ledger

1. `dotnet build Beep.SimEngine.slnx --no-restore`: exit1,5 missing ITilePresence import errors,
   0 warnings,8.92s. Original live session95478 was resumed, not restarted.
2. SAME build after import repair: exit1,1 invalid Map namespace error,0 warnings,1.61s.
3. SAME build after using the actual Maps namespace: exit0,0 warnings/errors,3.52s.
4. `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Fixed activity executor: stationary work, physical lost access and saved recovery"`:
   exit0,63 checks/0 failures/0 aborted,360ms, FIRST runtime attempt.

THREE builds, ONE runtime invocation, ONE distinct exact NEW bounded case. Earlier fixtures:
106 older files/106 build-identity assertion lines migrate source/compile ONLY; no old runtime
acceptance claim. Failed metadata tooling due truncated output was retried with bounded reads,
not a build/test gate. No older/family/full/platform/stale binary test or post-green code/content
edit. Source/public proof/tests/JSON FROZEN after green; only documentation/inventory afterward.

## Sole ownership and contracts

IActivityExecutor.CheckWorker uses the existing WorkRequirementContext. SAME candidate selector
calls the pure actual worker/target observation before route/material offers; actual acquisition
rechecks through that selector. Native staffed/timed definitions explicitly implement their own
unchanged admission. No duplicate candidate owner, requirement graph or claim store is introduced.

ApproachIndex is deleted. ActivityApproachResult explicitly distinguishes ResumeAt(index) from
Wait(blocker), rejecting default/negative/empty-blocker values. SAME board validates a returned
travel-phase index against the actual captured catalog. Wait returns a typed blocker without
changing phase, progress, receipts or worker. Native productive execution delegates to its existing
PausePolicy; dock replanning propagates the same result. Ready updates retry the same real phase.

fixed_activity_executors is a master JSON content section. FixedActivityExecutorDef resolves an
explicit productive-purpose phase and access_blocker. It authors a one-phase actual-target job;
worker admission requires physical reach and refuses dock targets with work.fixed-unsupported-dock.
An open unassigned intent can persist while its target is inaccessible: itinerary validation is
structural, never substitutes current readiness for a historical saved job. No travel/departure
phase, fabricated motion, teleport, progress timer or stock owner. Rate/advancement/spending remain
the actual IWorkerDispatch/WorkBoard algorithms. Dock-enabled fixed machinery is NOT implemented.

Build2.240.0. Save139/Board43/WorkSystem41/WorkView79 and all other payload/view bytes unchanged.
Strict build identity refuses old builds without an old reader/compatibility path. One unchanged
GameClock, constructed only at World.cs:96, SHA256
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

## Actual native player proof

Only existing native engine modules; NO proof-specific gameplay provider. Master-listed JSON
authors the stationary worker, movable storage target, material20, route rules and one-hour work.
Driver only assembles content, previews/commands/reads, advances Update and saves/loads.

- Actual out-of-reach stationary candidate reports authored access blocker, pure/no claims.
  Normal posted intent waits with no progress or material spending.
- Player MoveVehicle physically brings target into reach. Actual worker has NO IMotion and never
  moves from17,10. One productive phase earns real work; native stock20->12 consumes eight once.
  Entity.Update settles actual working state.
- Moving the actual target away pauses the SAME job/node/phase with actual typed blocker and
  settled stopped state. Earned progress survives; continued waits earn nothing/spend nothing.
- Private detached executor/blocker/progress snapshots remain pure and immutable. Foreign company
  gets no node state. Target-motion/access-wait save/load continues identically.
- Identical ordinary player return intents move the target back. SAME job resumes from earned
  progress and reaches sixty actual productive minute updates, with no second material charge.
- Normal unspent access-wait cancellation preserves stock and canonical checkpoint. Interleaved
  session starts with independent20. Missing access blocker, nonproductive/unknown phase and
  roleless fixed node fail authoring/composition.

Every stage is bounded900 Updates with actual time/target/node/job/blocker diagnostics.
Focused proof does not cover arbitrary providers, global purity, dock-fixed machinery or every
migrated workflow. No host/demo/OilFieldDays/Draw/platform/business gameplay changes.

## Remaining and source gates

Scoped hash/link manifest records this checkpoint, excluding its own hash; not whole-repository
or runtime acceptance. Diff-check0 with existing LF/CRLF warnings. Historical evidence preserved.
Scoped inventory492 paths/84 Markdown/1491 local links/0 broken/0 hash mismatches. The generated
[source manifest](P4_FIXED_POSITION_EXECUTORS_SOURCE_MANIFEST.json) records this checkpoint only.

Still open: dock-enabled fixed machinery, arbitrary/team/multi-endpoint/process/haul executors,
generated workflows/templates(G4), provider invocation/purity/fault/replay/stress/CPU and native
haul/dock recovery matrices, richer views/ETA(G5), full regression and remaining P1–P12 plan.
Full G3 and full goal are NOT complete; publication waits for full completion and verification.
