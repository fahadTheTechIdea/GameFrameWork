# P9-B4g — Priority-local physical worker matching

## 0. Problem statement

Follow-on B4h extends actual worker candidates to role pairs and the same WorkBoard joint admission
to role combinations. Distinct matching still counts real actors once; excluded lower work owns none.
B4g below is historical, not rerun on2.301.0. [Role combination design](P9_ROLE_COMBINATIONS_DESIGN.md).

Standard/WorkScheduling.MatchNativeFrontier searches native prefix resources, but still passes
capacities for EVERY ranked demand to DistinctWorkerMatching.Solve. A discarded lower native
activity therefore keeps a hypothetical worker allocation. If an earlier feasible team needs that
machine's actual inventory, the discarded activity vetoes it despite acquiring nothing.
This is not a real claim and must not overrule player priority.

## 1. Goals

1. Feasibility search matches exactly the selected first native prefix, not hypothetical lower work.
2. Preserve all distinct actual workers within that prefix and original ordered candidate edges.
3. Keep provider/company/standalone boundaries and fresh WorkBoard whole-set admission.
4. Rebuild all remaining offers after actual acquisition; never persist speculative allocations.
5. Prove scarce/abundant source, promoted lower priority, physical cancellation/recovery, saved
   pending/claimed/working continuation, detached/private views, isolation and canonical replay.

## 2. Design

Replace the full-capacity constrained call inside the existing private WorkSystem helper with an
aligned prefix-capacity vector: original capacity for members, zero for nonmembers. The original
full matching still identifies the first runnable native company frontier and real provider barriers.
For each longest-first candidate prefix, the SAME matching owner searches its full attained worker
coverage, ordered edges and actual WorkBoard resource/dock/cost feasibility. Original individual
fallback remains for no feasible prefix. No second selector, resource evaluator, claim store or queue.

An actual successful native acquisition restarts the original pass from current claims. Lower work
gets no speculative ownership. Refused whole actual acquisition still cannot fall through to members.

## 3. API surface

No new public engine API or persistent layout. Build2.300.0/save143/replay1/work-view84.
Existing role/source/worker/policy/activity contracts, normal JSON content master and native commands.

## 4. Files

Modify Standard/WorkScheduling.cs; Kernel/SaveGame.cs identity only. Existing
NativeDispatchPrefixTests registers ONE distinct new bounded case and reuses its pure driver helpers.
Add master-listed/embedded native-priority-workers.json. Older identity assertions compile-only.

## 5. Errors

Pure infeasible proposals claim nothing. Current provider faults and whole actual resource/dock/
optional-cost refusals remain with original owners. No resource grant, rollback or instant fallback.

## 6. Compatibility

Development-only direct replacement. No old matching branch, legacy reader or compatibility shim.

## 7. Verification

ONE exact NEW bounded case:
Priority-local workers: scarce source, lower-role contention and saved physical execution.
No older/family/full/Oil/Tycoon/platform/stale runtime. Record every build/run; only observed
same-new-case repairs. Bounded1200 normal updates per physical wait with engine diagnostics.
Immediately freeze executable source/proof/tests/JSON after final green; docs/read-only afterward.

## 8. Risks

Matching is priority-local, not a globally optimal all-provider scheduler. Exhaustive feasible
assignment search has no arbitrary game-size/combination cap, but CPU/fairness/stress gates remain.
Global purity/distributed providers/mixed-haul/lockstep/accounting/full acceptance remain OPEN.

## 9. Exclusions

No Oil/Tycoon/business/platform/Draw/host gameplay, new clock or engine-size limit.
Full goal/plan and publication are not inferred from this focused result.

## 10. Execution

Read actual owners → replace speculative coverage constraint → author physical JSON proof →
build/ONE new case → freeze → update tracker/evidence/source manifest.

## 11. Focused result

Implemented in original WorkSystem helper. Explicit role/source/worker JSON and the existing thin
public-command driver prove actual scarce/abundant teams, promoted priority and cancellation/release.
Build2.300.0/save143/view84; ONE clean build5.79s; ONE new case160/0/0 951ms first attempt.
167 older identity paths compile-only. Frozen 2026-10-08 13:13:04 UTC. No repairs/reruns/full runtime.
Private saved pending/claimed/working twins/session isolation/replay scoped; full goal/plan OPEN.
[Evidence](../../docs/migration/P9_PRIORITY_LOCAL_WORKERS_VERIFICATION.md).

