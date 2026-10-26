# P11-A8 — complete progression binding validation before initialization

## 0. Problem

`ObjectiveSystem.ValidateComposition` validates metadata aliases for all definitions but calls `ValidateBinding` only for predicates and terminal effects reachable from enrolled objectives. An unused effect can therefore contain an invalid declared bound or missing provider without failing startup. The same callbacks are not enclosed in the original provider observation boundary.

## 1. Goals

Reject missing references, dependency cycles, invalid declared effect bounds and missing predicate/effect providers before any system initializes. Validate every loaded predicate and effect, including unused content and an explicitly empty enrollment. Keep bound and capability rules in their existing contracts, not another progression catalog or validator service.

## 2. Design

The original ObjectiveSystem validates all predicate aliases/observation references and invokes each actual predicate's binding validator inside `EntityObservation.ReadProvider`. It validates every loaded effect through the same boundary, regardless of enrollment. Definition metadata uses that boundary too. Actual aliases must be frozen owned `Def` instances, not persistent services or fabricated identity wrappers. Enrollment prerequisite checks remain separate and unchanged. Runtime observation continues to evaluate only the original reachable DAG; startup validation never executes predicates or effects.

## 3. API and errors

No new engine service, state, command, catalog, clock or schema. Existing `IObjectivePredicate.ValidateBinding` and `IObjectiveEffect.ValidateBinding` now explicitly apply to all loaded definitions before Initialize. Content errors keep original ContentException semantics; throwing or clock-mutating validators retain original first-fault behavior and cannot proceed to initialization. No compatibility or legacy behavior.

## 4. Files

Modify ObjectiveSystem and contract comments; update current build identity. Add one negative-only extension proof module and explicit master/add-on JSON, extending the existing progression fixture composition rather than duplicating it. Register one thin new bounded test driver. Update the master tracker and evidence after verification.

## 5. Verification

Compile the solution at the completed slice, then run ONE NEW exact bounded case. Its matrix covers native/independent missing/cyclic technologies, native/independent objective references, missing/cyclic predicate DAGs, invalid native physical effect amounts, independent effect bounds and missing providers, unused/selected/empty enrollment, and impure or throwing startup validators. Positive physical research, original objectives, save/load, replay and session isolation use normal commands, views and the one clock. No old/family/full/Oil/Tycoon/platform runtime.

## 6. Risks and limits

Authored but unused content must now declare valid bindings: optional modules must omit unsupported content rather than silently defer validation. Extension authors still own their declared bounds and must implement their required validator; the engine cannot infer an arbitrary provider's undeclared rule. Full P1–P12, broader lifecycle/utility/privacy/provider/stress gates and protected G19 remain open. No commit/push until the full goal and plan finish.

## 7. Execution order

1. Move complete binding admission into the existing ObjectiveSystem and guard actual callbacks.
2. Add the file-based negative fixture and one public-input physical proof case.
3. Build once; repair only this new case if needed; run its bounded exact gate.
4. Record all attempts, refresh source evidence and update original gates only where their complete stated scope is proven.
