# P9-C8f — Committed dock itinerary replanning

Status: focused implementation verified, 2026-10-06. Parent: P9. Prerequisite: C8e itinerary admission.
Clean build;197 checks/two exact suites (77 admission;120 physical), zero failures/aborts.
[Evidence](../../docs/migration/P9_DOCK_REPLANNING_VERIFICATION.md).

Historical gate, not rerun by [C8g physical geometry claims](P9_DOCK_GEOMETRY_CLAIMS_DESIGN.md).
Current build2.122.0 replaces identity-only claim checks through actual ISpatialOverlap and the same
job fields; no owner payload change. This document's counts identify its preceding source only.

## 0. Problem

WorkBoard.CheckAssignedDockPolicy and DepartDock retain an already-selected slot even when a later
policy or structural change invalidates it. Available authored alternatives cannot recover that job.
RunJob checks the obsolete motion intent before giving the same dock owner a chance to reselect.

## 1. Success

1. Existing WorkBoard revalidates committed source/destination/service/wait/departure itineraries,
   retaining valid slots and selecting stable feasible alternatives only when necessary.
2. Source and future destination proposals are admitted together before either live slot changes.
   No alternative means existing job, receipts, cargo, progress and claims remain intact with a blocker.
3. Retargeting stops obsolete motion without changing presence, resets only the current approach
   toil when needed, and preserves earned handling/work and physical queue exclusivity.
4. Pure previews never retarget; Update and ordinary policy commands use the same execution owner.
5. Native tile and independent public point providers demonstrate timed recovery and saved twins.

## 2. Owners and API

Standard/WorkDocks.cs factors one pure slot selector out of PreviewDock. It accepts existing job
claims, retained slot and allowed slot kinds. WorkBoard.ReplanDockBindings proposes the complete
remaining chain through C8e's DockJourney/actual motion CheckRoute, then commits existing job fields.
No second queue, route, reservation, policy store, timer or speculative persisted plan is introduced.
Valid existing claims survive transient occupancy; actual ApproachDock/DepartDock enforce vacancy.
An alternative must have no conflicting job or occupant. Partial handling never moves into a waiting
slot with nonzero handling progress; service alternatives retain its earned progress. Ordinary work
can return to its approach toil with the same activity progress. Finished pickup claims remain until
actual departure, and completed delivery never replays stock transfer when its exit changes.

WorkSystem invokes replanning in mutating RefreshPolicy and RunJob, after ordinary assignment and
plan permission admission and before checking the obsolete current motion. CheckJob/candidate/view
queries remain pure. Actual motion Stop clears only obsolete intent and subsequent Approach requests
ordinary movement; no provider coordinates are set by the scheduler.
Finish completes a fully-earned paused node when physical departure finishes, through the existing
CompleteNode/effect owner. It rejects premature completion instead of releasing a stranded DAG job.

## 3. Errors and durability

Use existing policy/route/dock refusal codes. Missing/unsupported geometry does not authorize travel.
All route/provider checks complete before slot commit; existing purity guards reject faulty callbacks.
Existing WorkBoard job Sync owns commitments and progress. Bump engine/system/view semantic identity,
not payload layout. No old reader or compatibility overload. Pure guards are not global rollback.
Build2.121.0/save67/work system13/work view39; WorkBoard23/native mover3/movement5/logistics8/
logistics view13/motion view4 and persistent owner payloads unchanged.54 prior assertion files/
63 identity lines migrate and compile only; their suites and reused preceding fixture suites were not run.

## 4. Verification

Build, then only these exact suites; same-name repairs are allowed:

- Dock replanning: pure alternatives, retained claims and typed holds
- Dock replanning: physical retargeting, earned progress and saved continuation

Authored master-listed JSON defines alternative slot geometry and operating regions. Cover current
and future commitment edits, no alternatives, finite competitors, stable retained slots, partial
handling/work, loaded cargo, real exit, private frozen views, native/public providers and save twins.
Physical loops are bounded with diagnostic snapshots. Other suites compile only, never family/full.

## 5. Boundaries

This is current structural/policy replanning, not reserved roads, traffic forecasts, fleet policies,
deadlock recovery, mutable area editing, vehicle envelopes, arbitrary global provider rollback,
replay/lockstep/stress/allocations or full P1–P12 acceptance. No Oil/Tycoon/host/demo/Draw/platform work.

## 6. Execution

- [x] Factor sole pure dock selection and implement atomic existing-binding replanning.
- [x] Integrate mutating policy/update paths; preserve motion, progress and resource ownership.
- [x] Add and verify only the two directly affected complete suite names.
- [x] Align tracker/parent/guidance and record source-specific evidence.
