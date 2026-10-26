# P4-G2b: Cross-activity completion prerequisite validation — Design Document

Status: scoped implementation verified,2026-10-08. Full P1–P12 remains active.

## 0. Problem Statement

PriorActivityRequirementDef.ValidateBinding checks only IActivityCompletionHistory.IsActivity.
WorkBoard validates individual node/requirement DAGs, but two authored plans can require each other's
earned completion and then wait forever. Missing references and unavoidable cross-plan cycles must
fail before Initialize. An any alternative with a real independent starter must not be rejected.

## 1. Goals

1. Public typed prior-completion requirement contract supports native and declared mod requirements.
2. Same WorkBoard/catalog validates every declared reference; no concrete requirement type switch.
3. Iterative bootstrap analysis respects all/any and every node necessary for full plan completion.
4. Existing DependencyGraph compiler reports deterministic cycles among genuinely unresolved edges.
5. One bounded native physical/saved proof demonstrates viable feedback and rejects deadlocks before
   initialization, including external contracts and nested alternatives. No fabricated starter facts.

## 2. Design

IPriorActivityWorkRequirement extends IWorkRequirement with Plan; this contract means an earlier full
completion is always necessary for this leaf. Conditional alternatives must be expressed by existing
all/any groups, not hidden inside this contract. Native PriorActivityRequirementDef implements it.
Generic ValidateRequirementDefinition verifies nonempty selected Plan and distinct leaf semantics.
The redundant native ValidateBinding implementation was removed; the generic binding interface
remains for other independent requirements with different obligations.

Retain the existing requirement DependencyGraph result locally during composition. WorkBoard's
ValidateActivityPrerequisites uses that topological order to calculate potential requirement readiness
without calling Evaluate or reading/creating gameplay history. Non-prior leaves are potentially ready,
prior leaves require a bootstrappable plan, all requires every entry and any requires one. A plan is
bootstrappable only if every node's requirement is potentially ready. Repeat until no pending plans or
no change. No recursion, fixed depth/plan cap, saved readiness table or new scheduler.

For unresolved plans, traverse only unready nodes/requirements. Ready any branches and already
bootstrappable plans contribute no blocking edges. Deduplicate exact plan dependencies in transient
IDependencyNode records and use the SAME DependencyGraph compiler for stable missing/self/cycle
diagnostics. Discard all analysis after composition. Actual work/conditions/materials remain runtime
requirements; static potential readiness is not a gameplay grant or a feasibility prediction.

## 3. API surface

IPriorActivityWorkRequirement; native class implements existing interface family. No new command,
view, persisted record, clock or host API. WorkBoard owns the composition check over IActivityCatalog.

## 4. Files to change

PriorActivityRequirements.cs, Work.cs, new partial WorkActivityPrerequisites.cs; build identity.
Public extension requirement/probe, strict master-selected JSON; existing native prior factory gets
optional module/document assembly. One new bounded case/registration. Old build assertions compile
only. Stage/parent/master/README/AGENTS/evidence/source manifest.

## 5. Error handling matrix

Missing/empty/unselected reference or prior/group hybrid → ContentException before Initialize.
Unavoidable self/compound/all/any cycles → existing stable DependencyGraph diagnostic. Viable starter
branch remains valid. No history seeding, bypass, default plan/provider or repeated runtime retry.

## 6. Backward compatibility

Development only; direct contract/binding validation. No alternate loader/legacy evaluator.

## 7. Verification

Build completed slice, run ONLY ONE exact NEW bounded case. Repair/rerun observed failures only.
Record every invocation. Freeze source/public proof/tests/JSON after final green; docs only afterward.
No old/family/full/platform/stale suites. Physical stage timeout reports actual clock/orders/blockers.

## 8. Risks

Static structural bootstrap is deliberately optimistic about non-prior runtime guards. It does not
prove physical feasibility, cross-company/target reachability or eventual scheduling. Opaque mod
leaves must declare actual earlier-completion requirements through this contract; no hostile-code
inference. Dynamic bound-plan graphs must not invent undeclared composition obligations.

## 9. Out of scope

Completion counts/freshness/exact-instance requirements, arbitrary runtime dependency rewrites,
physical team atomicity, hostile-provider/global purity/fault/replay/stress/full P4/P1–P12 acceptance.

## 10. Sub-task execution order

1. Add typed prior contract and shared missing-reference validation.
2. Reuse the original requirement/compiler/catalog for iterative cross-plan bootstrap analysis.
3. Add public contract proof/native JSON and one physical saved case with invalid topology matrix.
4. Build/exact proof, freeze, record evidence and update current tracker without broad claims.

## Verified scoped outcome

All four sub-tasks complete for this slice. Build2.266.0/save140/Board43/System41/WorkView80 unchanged.
ONE build --no-restore6.99s0 warnings/errors. ONE exact NEW bounded case86 checks0 failures/aborts893ms,
FIRST attempt.130 older build identity files/native factory assembly compile-only. Nine invalid
compositions reject before Initialize; a 128-plan iterative chain composes without earned history.
Physical alternatives, positive machine travel/work, sequential stages, partial saved twins, equal
explicit pause, cancellation, earned feedback repeat/private compact immutable facts/isolation proved.
Each stage900 Update bound; runtime not CPU budget. Code/proof/tests/JSON frozen after green.
[Evidence](../../docs/migration/P4_ACTIVITY_PREREQUISITE_VALIDATION_VERIFICATION.md).
Section9/full P4/P1–P12 remain OPEN; no full-engine acceptance or per-slice commit/push.
