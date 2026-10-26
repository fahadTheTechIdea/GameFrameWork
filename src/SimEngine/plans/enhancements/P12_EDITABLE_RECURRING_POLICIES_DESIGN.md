# P12-D3b — Editable recurring inspection/service policies

Status: focused verified, 2026-10-06; build2.152.0/save80/native recurring3/public proof2/work15/work view43.
**324 checks/two exact suites** (191 contracts;133 physical), zero failures/aborts, clean build.
[Evidence](../../docs/migration/P12_EDITABLE_RECURRING_POLICIES_VERIFICATION.md).
Full P12/P9/P1–P12 remain partial. Parent: [P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md),
shared policy/physical execution: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).

## 0. Problem statement

Before this stage, `RecurringPlans.cs` stored enabled/priority in the source but read cadence from immutable
definition metadata. A player cannot edit inspection/service intervals without changing content.
The source already references its last real WorkBoard submission; no new scheduler or timer is needed.

## 1. Goals

1. One immutable `RecurringPlanSettings` owns enabled state, priority and exact game-time interval.
2. JSON provides explicit initial settings through `IRecurringPlanDefinition.Initial`; runtime source,
   command, preview, save/hash and public provider use the same settings contract.
3. Changing cadence uses the original last successful submission timestamp from the sole GameClock.
   Failed triggers/admission, disable/enable and edits cannot earn a new posting timestamp.
4. Active plans, jobs, spent materials, held tools and physical progress are not rewritten by edits.
5. Invalid/unaligned/out-of-range settings and dishonest provider commits refuse or fault explicitly.

## 2. Design

Replace separate native enabled/priority fields with one settings value. `RecurringPlanSchedule`
contains those settings and its existing last-posted/plan references. Definition interval/initial
enabled/priority getters and old control overloads are deleted, not retained as compatibility APIs.

`RecurringPlanContracts.Read` calculates due time from current settings and existing history using
the selected session quantum. Cadences remain positive and exactly aligned to that authored quantum;
no rounding or fabricated time default is introduced. First eligible submission remains immediate.
Due arithmetic overflow remains a typed blocker; a representable shorter interval can recover it.

CheckConfigure and Configure validate the actual source, linked operation criticality, interval and
current history. The provider must acknowledge exact settings without changing last-posted/last-plan,
owner, allocator or game time, and must touch its actual entity. RecordSubmission must retain exact
current settings while acknowledging only the new real plan and current GameClock coordinate.

Native/public codecs embed the shared exact GameDuration value codec and decode before commit.
Runtime session alignment/history/plan links validate at the existing WorkBoard checkpoint/load
boundaries. WorkSystem remains the only dispatch owner; there is no additional update system.

## 3. API surface

- `RecurringPlanSettings(enabled, priority, interval)`; strict Read/Sync.
- `IRecurringPlanDefinition.Initial`.
- `RecurringPlanSchedule(Settings, LastPostedTick, LastPlan)`; immutable projections.
- `IRecurringPlanSource.Configure(World, settings)`.
- `ConfigureRecurringPlanCommand(Entity, Source, Settings)` and matching WorkView preview.
- RecurringPlanView retains actual history/due coordinate and adds derived due game duration.

## 4. Files

Standard/RecurringPlans.cs, WorkRecurringPlans.cs, WorkModule.cs, Work.cs;
Commands/RecurringPlanCommands.cs; Views/WorkView.cs; Kernel/SaveGame.cs;
public RecurringPlanProof/ColonyGame; existing recurring and affected operation-policy source tests.
JSON stays explicitly master-listed; its initial enabled/priority/interval fields remain authoritative.

## 5. Errors

Null settings, missing source and foreign commands refuse. Nonpositive intervals/negative priorities
reject construction/JSON/decoding. Unaligned intervals and conversion/deadline range return typed
refusals; source chronology/link failures reject checkpoints. Dishonest settings/history commits fault
normal game Update before successful completion; no partial save or retry fallback.

## 6. Compatibility

No old overload, definition getter, source field, codec reader or compatibility facade.

## 7. Verification

Clean build, then exactly these two complete named suites; same-name repairs allowed:

- Recurring plans: authored schedules, controls and checkpoint ownership
- Recurring plans: physical inspection, maintenance and saved continuation

Cover native/public edits, immutable/private/pure previews, alignment/overflow, pending commands,
disabled/live/paused/cancelled work, original history/deadlines, real materials/tools/progress,
saved continuation, public callback failures and sole-clock pause/session isolation. No full run.

## 8. Risks

An expired shorter cadence must still obey existing duplicate/active-plan admission; never create
overlapping work. Changing source priority cannot change an already admitted plan's priority.
Provider settings equality must include the exact duration representation, not just enabled/priority.

## 9. Out of scope

Full mobile fleet subject/dock service orchestration, preparedness/inspection-discovery science,
preferred routes/congestion/fuel forecasts, broader provider rollback/replay/stress/full P12/P1–P12.
No Oil/Tycoon/host/demo/platform/Draw changes or independent clock.

## 10. Execution order

- [x] Replace contracts/settings in the existing source and board paths.
- [x] Migrate command/view/native/public codecs without aliases.
- [x] Extend and run only the two focused gates after clean build.
- [x] Record evidence and update current tracker/guidance while keeping broad gates open.
