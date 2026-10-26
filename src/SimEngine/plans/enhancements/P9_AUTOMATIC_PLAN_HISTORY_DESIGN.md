# P9-A13.2 — JSON-authored automatic plan history lifecycle

M104 qualification on unchanged2.739: [existing physical/history145/0 and public-policy71/0](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m104-networked-plan-history-continuation-after-retention-correction--2026-10-23)
prove selected networked age/private retention and one-cohort fair saved cleanup, eight
actual replays. These are independent existing durability cases, not the old broad template
helpers. No engine/test/profile edit, new case or rebuild; broader original gates remain open.

Current reference boundary: [M103, native2.739/save161/replay2](../../docs/migration/P9_PLAN_REFERENCE_OBSERVATION_VERIFICATION.md#current-m103-first-failures-and-full-component-owner-observation--2026-10-23).
Automatic cleanup uses the same full-owner/first-error observation and terminal reentry guard.
Its extracted focused case passes54/0 across six actual provider/sibling contexts; the old
broad automatic physical helper is not rerun. No policy/cleanup clock or saved layout changes.

Earlier status: focused verified on2026-10-07. Extends A13.1 explicit reference admission.
Build2.182.0/save100/board34/work31/view62/native-recurring7/public5/construction-view4.
Clean build;1,007 assertions in two exact suites (559 contracts;448 physical), zero final
failures/aborts. [Evidence](../../docs/migration/P9_AUTOMATIC_PLAN_HISTORY_VERIFICATION.md).
The full framework roadmap, global purity and allocation/performance acceptance remain open.

## 0. Problem

Explicit safe retirement does not bound unreferenced histories during long-running recurring
games. A game must select its own retention content, never inherit a platform size cap or
another cleanup clock. Created time is not terminal history age.

## 1. Goals

1. Optional sim.plan-history module requires an explicit master-listed policy selection.
2. Policy contracts expose minimum terminal age, minimum newest terminal cohorts per company,
   and cohorts examined per normal Update. Missing/invalid selection fails before Update.
3. Actual terminal timestamps and the saved round-robin native plan-ID cursor use WorkBoard Sync.
4. Automatic retirement reuses A13.1 whole references, including cross-cohort dependencies.
5. Private immutable diagnostics explain live/young/newest/reference-blocked cohorts.
6. Physical work, structures, resources, save/hash and pause remain on the single GameClock.

## 2. Design

PlanHistoryPolicyDef implements IPlanHistoryPolicy; AuthoredPlanHistoryPolicyDef reads required
minimum_age rational duration, minimum_retained_cohorts nonnegative count, cohorts_per_update
positive work budget. PlanHistoryModule declares plan_history_policies and selects required
policy from its scenario settings, after sim.work composition. It configures the same board;
there is no new service, system, stateful owner or module-owned clock.

PlanEntry stores EndedTick only for actual terminal history, assigned on cancellation/settlement
from GameClock. Live entries have -1. Same native Sync validates chronology and provider identity.
The cleanup cursor is an actual last-examined plan ID, not session age or a second sequence.

Transient cohorts derive from actual records: recurring CycleHead; manual company/group; ungrouped
manual singleton. Stable cohort head is the smallest actual member ID; membership is sorted.
Newest completed cohorts are retained per company by latest member terminal time and stable ID.
Age uses latest terminal time, never creation time. Reference-blocked histories can exceed the
retention target indefinitely: references always win, so this is not a maximum world size.

After existing settlement/pruning, examine at most the authored number of cohorts in native head-ID
round-robin order. Group eligible candidates per company; invoke existing whole retirement preview.
Remove blocked whole cohorts and recheck until a stable safe set remains, then use normal board
retirement. This keeps dependencies into excluded/young/live/provider-held cohorts intact.
No partial recurring cycle, auto-completion token, archive store, implicit policy or wall clock.

The public-only proof also supplies its own PlanHistoryPolicyDef JSON section through the same
module/board contracts. Providers do not add a second scheduler or retirement algorithm.
Reference callbacks cannot recursively enter reference admission: the existing board rejects
re-entry explicitly and clears the transient guard in finally. This is not another saved state.

Policy facts and cohort diagnostics are detached views derived on demand, not another history ledger.
Ordinary explicit retirement remains independently available; automated minimum age/count policy
does not forbid a player's explicit safe retirement.

## 3. API surface

IPlanHistoryPolicy, PlanHistoryPolicyDef, PlanHistoryPolicyView, PlanHistoryCohortView;
IPlanBoard.ReadPlanHistoryPolicy/CopyPlanHistory and same WorkView delegations; PlanView.EndedTick.
No extra execution command, host loop, platform adapter or rendering.

## 4. Files

PlanHistoryPolicies.cs, PlanHistoryModule.cs, WorkPlanHistoryPolicy.cs; existing WorkPlans/
Work/PlanContracts/WorkView; separate explicit policy/manifest proof JSON and public module proof;
same two exact template suites; current identities and staged evidence/tracker.

## 5. Errors

Unselected module: no automatic policy. Selected missing/null/unknown policy: content failure.
Negative age/count or nonpositive budget: content failure. Unrepresentable time: composition failure.
Live/young/minimum-retained cohort: typed diagnostics. Actual reference blockers reuse A13.1.
Malformed saved policy/cursor/terminal chronology: CorruptState. Provider impurity faults explicitly.
Recursive reference diagnostics: explicit InvalidOperationException, not stack overflow or pruning.

## 6. Compatibility

Direct breaking current save/board/view revision; no old terminal timestamp fallback or reader.

## 7. Verification

Build and only two existing exact recurring-template suites. Strict JSON/master/module selection,
terminal age not creation age, per-company newest cohorts, bounded/fair scans, cross-cohort reference
fixed point, manual/native/public cycles, diagnostics purity/privacy, physical stock/structures,
controller references inherited from A13.1 callbacks, corrupt saved chronology/cursor, saved partial twins, pause and interleaved
sessions. No separate third/controller/replay/full suite; allocation/global soak remain open.

Final exact gates: authored groups/private admission/cycle ownership559 passed in1108ms;
physical projects/cancellation/saved continuation448 passed in4683ms. Existing A13.1/A12/A11/
A10/A8/A7 callbacks remain included. A9 and all separate suites compile only, not rerun.
Fixture corrections supplied actual point presence and authored enabled work permission/state
for self-inspection, and asserted at-most budget behavior rather than instant cleanup. No engine
fallback or instant work was introduced. Physical construction consumed8 from20 units and retained
two completed structures after whole graph retirement. Native/public sources completed20 actual
inspection cycles; saved continuation, pause and frozen two-entry cycle retirement also passed.

## 8. Risks

Never prune ancestry retained by excluded cohorts. Persist fairness cursor through the actual board.
Only complete native cycles are units of automatic retirement. Whole-board grouping/validation
still has allocation costs; authored cohort work budget is not proof of bounded CPU time.

## 9. Out of scope

Oil/Tycoon/host/demo/platform/Draw. Active graph changes, global provider purity, replay/lockstep/
allocation/soak/full framework acceptance remain open; focused retention is not full goal closure.

## 10. Stages

- [x] Authored optional module/policy and authoritative terminal chronology/codec.
- [x] Same-board bounded cohort selection, safe fixed point and immutable diagnostics.
- [x] Focused physical/durability proof, evidence and tracker reconciliation.
- [ ] Wider reference/provider purity, allocation/performance/replay/soak and full acceptance.
