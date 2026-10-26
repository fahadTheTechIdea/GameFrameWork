# P11-C9 — Shared authored condition composition

Current ownership revision: [C9b condition content contracts](P11_CONDITION_CONTENT_CONTRACTS_DESIGN.md) migrates the same group/objective/site/playbook/private-view paths to actual independent IGameCondition aliases. Same expiring invocation guard/dependency compiler, no readiness state. NEW exact case84/0/0 on2.334.0/save154; [evidence](../../docs/migration/P11_CONDITION_CONTENT_CONTRACTS_VERIFICATION.md). Earlier C9/C9a runtimes historical/not rerun on2.334.0; full breadth remains open.

Historical C9 source/evidence below. [C9a invocation repair](P11_CONDITION_INVOCATION_DESIGN.md)
revises the original guard to one expiring session invocation and closes its cross-view engine-contract
recursion gap with66 focused checks. C9 runtime was not rerun on2.296.0; full broader gates remain open.

Status: native declared-graph slice implemented and focused verified,2026-10-08.
ONE clean build10.09s0/0; ONE exact NEW bounded case FIRST43/0/0 981ms. Executable inputs frozen.
[All attempts and scoped evidence](../../docs/migration/P11_COMPOSITE_GAME_CONDITIONS_VERIFICATION.md).
Full goal/plan active. Cross-view runtime recursion safety remains incomplete, noted below.

## 0. Problem

GameConditions.cs exposes private pure leaf readiness but no authored all/any composition across
different contract providers. Controllers/sites/objectives otherwise need bespoke providers for
resource reserve AND (safe operation OR elapsed readiness).

## 1. Goals

1. Master-listed JSON defines nonempty all/any groups of existing condition IDs.
2. Generic declared dependency metadata and original DependencyGraph reject missing/cyclic references
   before Initialize, including groups not bound to a controller.
3. Shared private guarded evaluator handles every child, stable identity order and actual blocker;
   unreadable or unavailable data never becomes a fabricated successful leaf.
4. Public private-view condition query lets hosts read engine results, not duplicate rules.
5. Existing leaves/contracts, saves/hash/replay, physical work and one clock remain authoritative.

## 2. Design

Extend existing GameConditions.cs with IGameConditionDependencies (stable child IDs) and
CompositeGameConditionDef. It owns immutable group metadata, resolves existing IGameCondition
definitions and evaluates all/any through new GameConditionsView.CheckCondition(string).
Original GameConditionContracts.Read retains content/visibility/purity checks; view owns only a
transient active-call cycle guard and exposes no World or provider. No stored condition values.
GameConditionsModule declares composite_conditions and validates one generic DependencyGraph
over actual registered condition definitions/dependency contracts before installing view schema1.
Binding validates each child under original provider purity guards.
Stable IDs canonicalize short-circuit order; all returns first actual refusal, any accepts an
actual ready branch, otherwise returns first real blocker. No negation of unavailable data.

## 3. API

IGameConditionDependencies.ConditionIds, immutable composite JSON {id,mode,conditions}.
GameConditionsView.CheckCondition(id) -> Code. Unknown/hidden identity -> UnknownDef; no-seat -> NotSeated.
Build2.295.0/save143/replay1; existing persistent/controller/view layouts unchanged, new conditions view1.

## 4. Files

Standard/GameConditions.cs, GameConditionsModule.cs, Kernel/SaveGame.cs;
new Views/GameConditionsView.cs. Existing identity fixtures compile-only.
One explicit composite document, new thin bounded player proof, fixture visibility only, embedding.
No platform/Oil/Tycoon edits.

## 5. Errors

Missing/null/empty/duplicate/malformed/wrong-type/missing reference/cycles fail before Initialize.
Provider faults propagate through existing guarded reads. Post-green source review found that child
reads create fresh PlayerViews, so the view-local active set cannot establish runtime undeclared
recursion safety. Declared native cycles reject at composition; external call-scope repair is next.
Read-only queries grant no material or completion; no fictitious blocker or default ready result.

## 6. Compatibility

Development-only; no old loader/alias/parallel predicate evaluator.

## 7. Verification

ONE exact NEW bounded case: Conditions: authored all-any graphs, private strategy and physical replay.
Prove native signals plus independent clock provider, nested groups and stable blockers, each own
company state, no-money mode, invalid graph cases, one normal intent and physical supplied completion,
saved queued/physical twins and replay. Waits <=2000 updates with clock/condition/plan diagnostics.
Record every build/run; repair observed same-case failures only. Freeze executable inputs after green.

## 8. Risks

Global external dependency/purity/CPU/fault breadth, deeply nested graph performance, full industrial
strategy, lockstep/corruption/accounting/Oil/authorized P6/full P1-P12 remain open.

## 9. Out of scope

New scheduling/state/budget/clock, active plan policy revocation, renderer/adapters/platforms/Tycoon.

## 10. Execution

- [x] Inspect shared condition/source/objective/composition owners.
- [x] Implement generic group metadata/validation/private query, migrate identity callers compile-only.
- [x] Author bounded public-player physical proof and freeze after green.
- [x] Update tracker/guides/evidence/provenance.
- [x] Repair runtime recursion scope across fresh child views for undeclared engine-contract dependencies (C9a scoped proof).
Open decisions: none; all/any compose existing registered providers.
