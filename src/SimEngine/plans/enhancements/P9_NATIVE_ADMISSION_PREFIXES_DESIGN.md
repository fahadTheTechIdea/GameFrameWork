# P9-B4f — Feasible ranked native admission prefixes

## 0. Problem statement

Follow-on B4g replaces full outside-demand coverage with prefix-local worker matching. Excluded
lower work has no hypothetical actor veto; full attained coverage remains required INSIDE the
selected prefix. B4f's result below is historical, not rerun on2.300.0.
[Priority-local worker design](P9_PRIORITY_LOCAL_WORKERS_DESIGN.md).

Standard/WorkScheduling.FitsAssignmentSet currently includes every ready timer and matched native
job in the entire queue. One infeasible lower-ranked timer invalidates joint admission for ALL native
work. WorkSystem.Assign then takes individual preferred input sources, even when an alternative source
would admit two higher-ranked actual machines together. This defeats authored alternatives/parallelism.

## 1. Goals

1. Choose the longest jointly feasible prefix of the FIRST runnable native company frontier.
2. Preserve original full distinct-worker coverage/ordered candidate search and B4e provider barriers.
3. Jointly select actual requirement input sources/resource endpoints/docks through original WorkBoard.CheckWorkSet.
4. Never partially commit a refused whole set or let an excluded lower timer take claims/progress.
5. Prove material alternatives, scarce staffed/timed prefixes, real physical execution, cancellation,
   pause, saved pending/held/working twins, session isolation and canonical replay through public commands.

## 2. Design

Original WorkSystem owns a private MatchNativeFrontier helper, not a new scheduler/service.
First use the same DistinctWorkerMatching to find actual runnable machine coverage. Collect only
the first uninterrupted native company activity frontier; skip unavailable/exhausted (already filtered)
offers, stop at runnable source/company/standalone-native boundaries.

Search prefix lengths longest-first. For each length use the SAME distinct matching with the original
ordered edges and full attained coverage, but constrain native feasibility ONLY to that explicit prefix.
FitsAssignmentSet takes the exact native demand-index set, includes only matched native machines and
timers in that set, and delegates fresh resources/docks/optional-cost choice to original CheckWorkSet.
Failed pure whole-prefix search drops only its final ranked member; no claims were acquired.
The first feasible prefix returns matching and selected sources/docks. Assign passes ONLY those actual
members to the existing WorkBoard whole-set acquisition, which still freshly rechecks before mutation.
If no prefix fits, original actual ranked individual refusal/execution remains authoritative.
No parallel reservation ledger, recipe evaluator, matching state, age table, clock or copied context.

## 3. API surface

No public runtime API/codec-layout change. Build2.299.0; save143/replay1/work-view84 unchanged.
One explicit master-listed JSON extension document authors worker inputs/alternatives and activities.

## 4. Files

Modify Standard/WorkScheduling.cs and Work.cs; Kernel/SaveGame.cs identity only; pure proof factory
seam already exists. Add native-prefix.json, embedded resource, NativeDispatchPrefixTests registration.
Older identity assertions migrate compile-only. No older runtime suites.

## 5. Error handling

Actual provider/material/dock/optional-budget failures still use original refusal/fault semantics.
Membership cannot authorize a stale acquisition; retained matching is transient same-call intent.
A normal whole-prefix commit refusal must not fall through to a member mutation in that pass.

## 6. Compatibility

Development only, direct owner refactor. No old all-native feasibility path, shim or legacy codec.

## 7. Verification

ONE distinct NEW bounded case:
Native admission prefixes: alternatives, timed scarcity and physical saved work.
Build only when needed; at most1200 normal updates per stage with clock/job/blocker diagnostics.
Only same-new-case observed repairs/reruns. Record every build/run, freeze all executable/proof/tests/JSON
after final green, then docs/read-only. No older/family/full/Oil/Tycoon/platform/stale runtime.

## 8. Risks

Full distinct coverage is preserved, not an unbounded global resource-optimal schedule. Alternative
matching/provider enumeration and longest-prefix scans need broader performance/stress evidence.
Distributed provider atomicity/global purity and complete mixed-company/native-haul matrices remain open.

## 9. Exclusions

No platforms/Draw/Oil/Tycoon business policy, engine/world-size cap, second clock, instant inputs or
host-owned scheduling. Full P1-P12/P6 acceptance and commit/push are not inferred from this focused slice.

## 10. Execution

Inspect original matching/admission → replace feasibility membership/native batch loop →
author JSON and thin public physical driver → build/ONE case → freeze → tracker/evidence/manifest.

## 11. Focused result

Implemented through original WorkSystem/matching/WorkBoard, plus one independent public requirement
offering actual target/worker ports with explicitly authored native bill/use/source costs. New JSON is
master-listed; ordinary commands/views drive physical proof, not test-owned assignment/material logic.
Build2.299.0/save143/view84. ONE clean build13.55s; ONE attempt ONE new case159/0/0 891ms.
Real parallel source alternatives, cancellation, timer-first scarcity and full mixed frontier,
pause/private facts/saved twins/session isolation/canonical replay scoped. No repairs/reruns.
166 older identity paths compile-only; frozen2026-10-08 12:59:15 UTC. Broader/full gates OPEN.
[Detailed evidence](../../docs/migration/P9_NATIVE_ADMISSION_PREFIXES_VERIFICATION.md).
