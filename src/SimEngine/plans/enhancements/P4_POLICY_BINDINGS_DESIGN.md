# P4-G26 — Actual contract-based safety policies

## 0. Problem

OperationPolicy runtime is bound to OperationPolicyDef, the native policy parser is duplicated in
proof definitions, and selection/admission uses concrete part metadata. Policy/condition reads
check revisions but not hidden saved-state changes or clock speed/pause mutation.

## 1. Goals

1. One OperationPolicyDeclaration reads/resolves native and independent JSON metadata.
2. Original OperationPolicy runtime consumes IOperationPolicyDefinition, same codec/state.
3. Original policy owner guards complete metadata/state/condition/control reads and policy admission.
4. Named contract lookup governs policy links, operation/condition validation, shutdown and acknowledgment.
5. Existing ConditionContracts.Read observes its actual original provider codec, retaining math.
6. One new thin-player case proves physical shutdown/repair/restart, policy parity/save/replay and faults.

## 2. Design

Keep OperationPolicyContracts as sole safety evaluator and mutation owner. Its guarded Read validates
actual named metadata, original chronology/bands, actual condition/control and original refusal rules.
Pure checks and discovery use existing EntityObservation.ReadProvider. Public policy ID projection
stages IDs before publication. Acknowledgment observes policy state before clearing a latch; it must
not require the temporarily inconsistent control/latch pair to pass a full read before the clear.

Native OperationPolicy becomes Component plus its existing policy contract; metadata is the actual
IOperationPolicyDefinition. No new policy runtime/cache/condition state/scheduler or legacy path.
ConditionContracts.Read replaces duplicated manual revision checks with original provider observation.

## 3. API

Public policy/condition commands/views remain unchanged. New public OperationPolicyDeclaration is
the sole authoring helper. Native runtime/metadata use existing contracts and payload schema1.

## 4. Files

OperationPolicies, EquipmentCondition, EquipmentConditionView, EntityObservation; original public policy proof/parser
and new independent metadata/public probe module; thin PolicyBindingTests; current build/protocol;
stage/evidence/master/audit.

## 5. Errors

Malformed references/bands/aliases/duplicate owners refuse before Initialize. Pure callback/getter
exception or saved state/clock/identity mutation faults via original observation.provider; no partial
outs/IDs, healthy continuation or callback retry. Existing typed refusal semantics remain.

## 6. Compatibility

Development only; delete native dependency/duplicated parser/manual pure-read checks, no shim.

## 7. Verification

Build for compilation; ONE NEW bounded exact case:
Policy bindings: authored contracts govern physical shutdown repair and restart.
Only that case and its repairs; no historical/family/full runtime.

## 8. Risks

Framework state components remain required. Whole CLR/provider sandbox and complete mutating-policy/
condition transaction breadth remain open. No claim of full regression/capability decomposition.

## 9. Out of scope

Tycoon/catalog authority, business/defaults, renderer/platform/Draw, publication.

## 10. Sequence

Shared authoring/native metadata contract; guarded original owners; named consumer migration;
independent metadata/in-place public probes; thin physical proof; actual evidence/tracker.

Status: narrowly verified, core2.361.0/save154/replay1; final NEW exact bounded case646/0/0 in3657ms.
[Actual evidence](../../docs/migration/P4_POLICY_BINDINGS_VERIFICATION.md).
Three failed same-case attempts and the guard repair are recorded; no historical/family/full run.
Full P1–P12 and goal remain OPEN. Full condition/capability decomposition, arbitrary provider/CLR
sandbox, mutating transaction breadth, spatial deep immutability and global acceptance unproved.
