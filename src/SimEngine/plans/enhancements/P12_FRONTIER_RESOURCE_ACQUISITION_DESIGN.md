# P12-D13 / P4-G2 — selected resource-source acquisition

## 0. Problem

Joint native admission can backtrack an earlier flexible resource occurrence to an alternate
endpoint, preserving the sole eligible endpoint for a later demand. Dispatch discarded the selected
results and reacquired the earlier canonical source. A feasible current frontier could therefore
starve despite sufficient real stock.

## 1. Goals

Preserve selected source identity and actual role/recipe in the original acquisition path.
Freshly recheck allowed current offers and complete native obligations before mutation.
Keep one requirement evaluator, transaction owner, node codec, worker dispatch and game clock.

## 2. Design

FitsAssignmentSet projects the SAME CheckWorkSet selected results and service IDs in a transient
per-demand candidate/slot tuple, only after complete native per-company admission. No partially
admitted map survives a refusal. No external provider receives invented native obligations.

TryAssign freshly recopies eligible workers with the selected recipe through the existing choices
argument, preserving actual worker and role. Spent execution choices remain authoritative. Existing
preemption recopies again after actual interruption. Assign rechecks the exact service slot first.

Existing WorkBoard.BindResourceSelection binds selected identities to fresh native resource
occurrences/offers: exact path, requirement, bill, use and rates; complete one-to-one occurrence
coverage. Old quantities/endpoint handles never become authority. Current bound/spent choices stay
with their actual node owner. Missing/foreign/stale sources refuse without substituting another.
The SAME CheckResourceSet rechecks the complete set with only those current selected offers.
The original AcquireResources still checks optional costs and acquires actual native receipts.
Each successful acquisition invalidates all remaining offers and rebuilds the existing pass.

## 3. API and persistence

Internal acquisition arguments change in place, with no compatibility overload. Public views,
commands and saved payload shapes unchanged. Build2.255.0/save139. No selection cache/codec.

## 4. Content and files

WorkScheduling, Work Assign and WorkResources remain the existing responsible owners. New bounded
proof reuses the master-selected resource requirements and engine-side resource connection/pool
contracts. Authored restricted endpoint and fluid-only activity exercise genuine source backtracking.
Explicit physical-only proof composition excludes supplier and monetary cost content; no Tycoon
source changes or business enablement. Older fixture business composition remains a separate gate.

## 5. Errors

Malformed/incomplete/duplicate metadata intentions return InvalidArgument. Unoffered endpoints return
MissingSource; actual native claim checks retain their existing stock/lost/compatibility blockers.
Refused rebinding is pure. Provider faults propagate; no invented source, stock, rollback or fallback.

## 6. Verification

One exact NEW bounded case, repaired/rerun only for observed failures:
`Work source selection: joint backtracking, fresh refusal and saved acquisition`.
Record every build/run. No older/family/full/platform/stale suite. Freeze source/tests/JSON after
final green; documentation only afterward. Older strict identities/internal call migrations compile-only.

## 7. Risks

Selections are not atomic multi-job reservations or future stock/service guarantees. External joint
obligations, cross-role/global recipe search, preemption/provider fault matrices, remote physical
delivery, congestion, generated frontiers, combinatorial CPU, replay/stress/full acceptance remain open.

## 8. Non-goals

No Oil, Tycoon source/business policy, host/demo gameplay, Draw, platform adapter or extra clock.

## 9. Execution

1. Carry selected results and slots through the original native dispatch handoff.
2. Rebind exact source intentions using current offers and existing native preflight.
3. Build; run the one new case; repair only observed proof failures.
4. Freeze source, update tracker and retain scoped evidence. Full P1-P12 remains active.

## 10. Outcome

Implemented; final focused gate35 checks/0 failures/0 aborts519ms. Three clean builds and three
runs of that same case; first content refusal and second proof filter error are recorded, not hidden.
[Evidence](../../docs/migration/P12_FRONTIER_RESOURCE_ACQUISITION_VERIFICATION.md); [master tracker](MASTER_TRACKER.md).
