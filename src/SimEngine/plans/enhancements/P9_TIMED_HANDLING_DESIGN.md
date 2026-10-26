# P9-C4a — Authored timed cargo handling

Status: historical focused implementation verified, 2026-10-05. Parent [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Follow-on [C4c progressive handling](P9_PROGRESSIVE_HANDLING_DESIGN.md) now uses build2.64.0/save36/
work17/work-view19. These timed tests were migrated in place to the two progressive suite names;
current evidence is [here](../../docs/migration/P9_PROGRESSIVE_HANDLING_VERIFICATION.md).
Historical C4b dock implementation uses build2.62.0/save34/work16/work-view18 and a final LeaveDock toil.
This stage's two suites were compiled but not rerun in C4b; counts below are its original gate.
Build2.61.0/save33/work15/work-view17/transporter2, logistics4/view8 unchanged.
**100 checks / two exact suites**, zero warnings/errors/failures/aborts; no broader gate ran.
[Evidence](../../docs/migration/P9_TIMED_HANDLING_VERIFICATION.md).

## Problem and goals

Standard/Work.cs RunJob Load/Unload currently commits the whole native transfer on the first reached
update, independent of amount. Standard/Capabilities.cs TransporterDef reads an empty object. Revise
that single path, not a host timer or another cargo ledger. This implements quantity-scaled physical
handling as a prerequisite for C4b dock queues and later incremental-native-transfer protocols.

1. Every transporter explicitly selects a master-listed ResourceHandlingProfileDef through its existing
   definition. ITransporter exposes the immutable IResourceHandlingProfile contract alongside its existing
   cargo. No absent-profile/default-duration/instant compatibility path.
2. JSON declares nonempty resource rows, positive native basis quantity and separate positive rational
   loading/unloading durations. Unknown/duplicate resources, unsupported amounts and out-of-range time
   refuse explicitly. Open profile subclasses use existing content base indexes, not another registry.
3. The existing GameTime converter resolves a duration multiplied by an exact quantity ratio before
   rounding to session ticks. No round-per-unit accumulation, float timing, arbitrary size limit or second
   duration calculator. Checked wide/fractional WorkProgress remains the sole progress value type.
4. WorkSystem alone advances Job.HandlingProgress while physically reached and eligible. Movement,
   policy pauses, interruption, cancellation and entity states reuse existing owners. Source/cargo/inlet
   promises remain native and protected. LogisticsService Load/Unload refuses premature direct commits.
5. Whole receipt-backed batch stock transfers still commit once after handling completes. No partial
   stock is fabricated while time advances. Progress resets at the real leg transition/end; stock/flow
   history changes only at verified existing transaction commit.
6. WorkBoard.Sync owns elapsed handling, validating phase/profile/bounds on save/load. Private WorkJobView
   exposes detached progress and required ticks so hosts do not calculate handling. Save/work/view/build
   metadata migrate in place without legacy readers.

## Files and API

Standard/ResourceHandling.cs: IResourceHandlingProfile, open ResourceHandlingProfileDef, authored native
rows, typed ResourceHandlingCheck and shared stateless HaulHandling rules. Existing TransporterDef reads
handling profile and Transporter aliases it. Existing LogisticsModule declares the section/component.
Core/GameTime.cs extends ISimulationTime.Ticks(duration,numerator,denominator), using the same converter.
Standard/Work.cs persists/advances one job progress field; Logistics.cs previews compatible real carriers
and guards current reached/full-duration commit. Views/WorkView.cs projects current progress/requirement.
No new module/system/component-owned progress, saved ETA, queue, inventory or receipt owner.

Master-listed point-handling.json supplies explicit proof content. Existing standalone colony/recovery/
quantum fixtures migrate their JSON bindings; they are compiled/parsed only, not counted as rerun.
Public-only proof authors an independent handling profile subtype through a normal content module.

## Errors and acceptance

Admission is pure and precedes job/claim acquisition. Missing/invalid definition fails content resolution;
unsupported resource/quantity/time has typed blocker, never zero-duration service. Premature direct
Load/Unload refuses without stock mutation. Provider violations fault, never compensate/fabricate stock.
Policy pause preserves progress/claims; cancellation releases only this job's unspent claims. Loaded
cancellation preserves actual cargo for existing return handling. Save corruption rejects progress on
non-haul/ended jobs or above its actual current leg duration. Original intent age is never reset.

Two exact suites only: strict authored admission/exact ratio/private facts/public extension; then
physical loading/travel/unloading, amount scaling, paused/cancelled/removed endpoints, saved twins and
isolated sessions. All waits bounded with job/toil/progress/blocker snapshots. No third/broad/full gate.

- [x] Strict profiles and exact quantity/session time conversion.
- [x] Physical progress and premature commit guards through existing owners.
- [x] Phase-bound saved progress, private views and public-only extension.
- [x] Bounded physical pause/cancel/removal/save/isolation proof.
- [x] Build/two suites/scoped checks and master/README/AGENTS/P9/P4/evidence handoff.

Dock/service occupancy and physical waiting queues, per-subquantity receipt semantics/incremental stock,
congestion/fuel/fleet/areas, starvation/preemption, plans/templates, wider external-provider/fault/replay/
stress/performance and full roadmap gates remain open. No Oil/Tycoon/host/platform/Draw changes.
