# P9-A13.4 — Single-admission history dependency closure

Current callback boundary: [M103/native2.739, first errors and full component-owner observation](../../docs/migration/P9_PLAN_REFERENCE_OBSERVATION_VERIFICATION.md#current-m103-first-failures-and-full-component-owner-observation--2026-10-23).
Original dependency-closure algorithm is unchanged and its broad chain/group helpers are not
rerun. Current focused results cannot certify every closure topology.

Earlier status: focused verified2026-10-07. Extends A13.2/A13.3 on the sole WorkBoard.
Build2.184.0/save100/board34/work31/view62/native7/public5/public-retention2/construction-view4.
Clean build;1,558 assertions/two exact suites (1,062 contracts;496 physical), zero failures/aborts.
[Evidence](../../docs/migration/P9_PLAN_HISTORY_CLOSURE_VERIFICATION.md). Full roadmap remains partial.

## 0. Problem

SafeHistoryCohorts repeatedly reconstructs the remaining selection and invokes whole reference
admission after every rejected wave. A long chain whose tail is provider-held can require one
full provider/board scan per ancestor. A game-owned visit budget is not an engine size limit,
but repeated diagnostic rescans make large authored workflows needlessly expensive.

## 1. Goals

1. One initial native admission per candidate company, not one per dependency wave.
2. Preserve existing direct blockers and propagate HistoryDependency to newly retained ancestry.
3. Use actual PlanEntry.Dependencies and whole existing cohorts, never another graph authority.
4. Group-contracted cycles terminate; unrelated descendants remain safely eligible.
5. Normal commands/updates, explicit JSON policy, private views and saved twins prove behavior
   with counted provider reads rather than a machine-dependent runtime threshold.

## 2. Design

Keep candidate grouping and PreviewRetirePlanHistory as the sole direct-reference admission.
Build the existing transient actual plan-ID-to-cohort lookup once. Seed rejected whole cohorts
from the initial native diagnostics, preserving each cohort's first direct blocker. Queue these
cohorts in stable native entry order. For every dequeued cohort, follow its actual members'
prerequisite IDs; a candidate prerequisite cohort not already rejected receives HistoryDependency
and enters the queue once. A rejected dependent retains its prerequisites, not its descendants.

This computes the same removal fixed point without rescanning all providers per wave. Contracted
cohort cycles are safe because the rejection set admits each cohort once, even though the actual
plan DAG remains acyclic. Surviving candidates retain original company/head order. Final physical
retirement still uses ordinary board admission and existing guards at mutation time.

No saved topology/index/cache, completion token, second plan/history model, loop or clock. The
closure phase follows actual rejected members/edges once. Initial admission, provider callbacks,
cohort grouping, current newest-policy sorting and final retirement retain their existing costs.

## 3. API

Unchanged public contracts, JSON schemas, commands, views and state codecs. Direct build identity
revision only. Existing authored policy and reference-provider definitions configure the proof.

## 4. Files

Standard/WorkPlanHistoryPolicy.cs; existing public-reference test fixture plumbing; new bounded
closure proof callbacks in the same two exact template suites; current identity/docs/tracker.

## 5. Errors

Existing ownership/terminal/cycle/source/native/provider diagnostics remain authoritative.
Missing selected admission diagnostics remain an explicit contract error. Pure providers must
not change state; current shared ReadProvider/full-owner/first-error guards remain in force.
Only transient reentry clearing belongs in finally. No swallowed mutation or resumable callback fault.

## 6. Compatibility

Replace the repeated-wave algorithm, not retain a second path. Native and public saved schemas
are unchanged; no fallback/legacy reader, new clock, platform/world-size cap or default policy.

## 7. Verification

Build and only two exact existing recurring-template suites. A real command-authored long chain
ages through the single clock and keeps all referenced native history. Count one provider read
per initially observed plan, private immutable diagnostics, hash/time purity and saved repeatability.
Cover whole grouped ancestry, an acyclic plan DAG that contracts into a cohort cycle, an unrelated
eligible descendant, ordinary subsequent retirement, and all prior physical/provider callbacks.

## 8. Risks and honest limits

Do not reverse the dependency direction or override a cohort's initial direct blocker. Never
remove a prerequisite retained by any excluded/live/young/newest/provider-held cohort. This is
closure work-count evidence, not constant CPU/allocation, arbitrary-provider purity, large-world
memory certification or full regression. Native direct/final admission may still be expensive.

## 9. Out of scope

Oil/Tycoon/host/demo/platform/Draw. Active DAG revision, fleet/fuel/team features, new graph/index/
ledger/cache, global sandboxing, replay/lockstep/allocation/soak/full framework acceptance.

## 10. Stages

- [x] Replace repeated-wave scans with one native admission and actual-edge rejection closure.
- [x] Command-authored counted chain/grouped-cycle/eligible-descendant and saved proofs.
- [x] Two exact focused gates, source evidence and tracker reconciliation.
- [ ] Broader allocation/performance/global purity/replay/full framework acceptance.
