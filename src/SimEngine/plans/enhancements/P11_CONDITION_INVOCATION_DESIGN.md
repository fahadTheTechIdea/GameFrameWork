# P11-C9a — Single scoped condition invocation

Current M102/native2.738/save161/replay2: existing condition observation/cleanup owns
startup metadata, binding and runtime first failures. Context Enter/Leave/Close and
private-company recursion rules stay intact. Recursive fault checks now have one existing
terminal-case owner56/0; original content case210/0 verifies physical plan/objective replay.
No new evaluator, graph compiler, invocation state, clock or test case.
[Current exact evidence and handoff](../../docs/migration/P11_CONDITION_CONTENT_CONTRACTS_VERIFICATION.md#current-m102-observed-dependency-capture-binding-and-readiness--2026-10-23).

Earlier engine-contract invocation repair implemented and focused verified,2026-10-08.
Build2.296.0/save143/conditions-view2. Two clean builds9.90/5.42s0/0; two SAME ONE new bounded
case attempts:first65/1/0 1130ms,final66/0/0 1100ms. Repair only observed exception assertion.
[All attempts and scoped evidence](../../docs/migration/P11_CONDITION_INVOCATION_VERIFICATION.md).
Executable inputs frozen; full goal/plan remains active and arbitrary CLR recursion/CPU is not sandboxed.

## 0. Problem

Original C9 view-local active sets could be bypassed through fresh child views. C9a moved
that responsibility to the single expiring invocation. M102 preserves that owner while
removing manual finally verification that could mask a callback's first error. No second
evaluator or process-global stack is introduced.

## 1. Goals

1. One session-owned original condition guard creates one short-lived context per root query.
2. Nested contract checks AND same-company view aliases reuse that actual context/private view.
3. Recursive provider paths fail immediately; shared DAG children may be checked repeatedly after return.
4. Context expires on success/failure. A swallowed provider error cannot publish success or checkpoint.
5. Leaf/group contracts migrate cleanly; optional module, normal physical work/saves/replay stay shared.

## 2. Design

Revise IGameCondition.Check to GameConditionContext in place. Context exposes private View and
Check(IGameCondition), keeps only transient active IDs/open flag, and is not constructible by hosts.
Original GameConditionContracts becomes the same module-owned session guard, bound once by
GameConditionsModule. Static root admission delegates to it; nested reads reuse _active and deny
cross-company alias changes. Every child preserves content/visibility/purity/blocker checks.
Provider failures latch existing World fault and root health verification refuses swallowed success.
Finally closes/clears the actual root context. No Sync, condition-value cache, second clock or evaluator.
Remove GameConditionsView active set; facade only resolves IDs/delegates.
Original plan/site/objective binding routes validate selected condition guard/provider scopes.
Native composite evaluates its resolved contract children with context.Check; public clock leaf uses
context.View. Existing independent modules continue to author contracts/content, not scheduler edits.

## 3. API

IGameCondition.Check(GameConditionContext). View getter and Check require actual open invocation.
No old PlayerView overload or reader. Build2.296.0/save143/replay1/conditions-view2; persisted layouts unchanged.

## 4. Files

Standard/GameConditions.cs, GameConditionsModule.cs, PlanPlaybooks.cs, PlanSites.cs,
ObjectiveContracts.cs, Views/GameConditionsView.cs, Kernel/SaveGame.cs.
Migrate public clock proof and older identity assertions compile-only; shared pure factory module selection.
New non-friend call probe, explicit master-selected JSON and thin bounded public-player case.

## 5. Errors

Missing selected guard/foreign condition binding fails explicitly; hidden conditions retain UnknownDef.
Recursive paths/invalid blocker/provider throw latch original fault. Save/Hash then refuse.
Expired contexts throw; no renewed invocation or fallback. Same-company alias is same root private view.
Metadata cycles still reject before Initialize through existing DependencyGraph.

## 6. Compatibility

Development-only breaking contract revision; no shim/legacy adapter/duplicate evaluator.

## 7. Verification

ONE exact NEW bounded case: Conditions: single invocation, recursive providers and durable physical play.
Prove native/public nested leaves, repeated child paths, alias query recursion, direct/mutual recursion,
swallowed error/invalid blocker/provider throw, expired success/failure contexts, private interleaved
sessions, normal supplied construction, saved twins/replay. Physical waits <=2000 updates with diagnostics.
Record every build/run; repair observed same-case failures only. Freeze executable inputs after green.

## 8. Risks

Not a CLR sandbox: direct arbitrary recursion inside trusted provider code and unlimited CPU remain
outside observable engine-contract calls. Broader provider/global-purity/corruption/lockstep/performance/
industrial strategy/Oil/authorized P6/full P1-P12 gates remain open.

## 9. Out of scope

New persistent state/clock/coordinator, Tycoon/Oil/platform/renderer, old runtime/full suite reruns.

## 10. Execution

- [x] Revalidate actual fresh-view flaw and original call/binding owners.
- [x] Revise one original session call scope and migrate contracts/callers compile-only.
- [x] Author one bounded extension/physical proof, build/run and freeze.
- [x] Update tracker/guides/evidence/source provenance and close C9 known gap within proved scope.
Open decisions: none; correct scope is session-local, transient and actual-company.
