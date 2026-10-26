# P10-A8: external flow facts and observed metadata — Design Document

## 0. Problem Statement

The public INetworkDiagnostics contract returns NetworkFlowSnapshot, but its constructor is internal, preventing non-friend runtime providers from publishing that contract. Native CopyFlow also reads INetworkDefinition.Rate directly: a live metadata callback can alter the connected entity's private codec during an apparently pure read. NetworkDefinitionContracts.Read separately verifies entity state in finally, which can replace a callback's original failure.

## 1. Goals

1. Permit external providers to construct valid detached flow facts without native inheritance or stock ownership.
2. Validate identity, distinct endpoints, nonnegative allowance and earned-time coordinates in the existing DTO.
3. Observe CopyFlow metadata through the existing provider and whole-target guard before any publication.
4. Remove the nested finally verifier, retaining the first original callback error even after mutation.
5. Prove physical source/pool conservation, private detached facts, saved continuation, pause, replay and callback refusals in ONE NEW exact case.

## 2. Design

Make NetworkFlowSnapshot's existing constructor public and validate its value-only shape. No state, serializer, ledger, clock or transfer algorithm changes. Extend the existing internal NetworkPolicyContracts.Read call with the original EntityObservation.ReadProvider whole-target option. NetworkDefinitionContracts.Read delegates to that owner rather than adding a finally scope. CopyFlow resolves its actual rate through this same path for each actual cursor/entity; output stays locally staged until all callbacks pass.

The non-friend IndependentNetworkDef copies an actual immutable flow fact to prove public construction. Existing master-listed network-definition JSON selects native/direct metadata. Negative-only extension instrumentation adds mutation-then-throw callbacks; normal gameplay remains in original infrastructure, resource, work and clock owners.

## 3. API surface

The existing NetworkFlowSnapshot constructor becomes public. Invalid identities, equal endpoints, negative/excess earned time, nonpositive last timestamp, negative allowance or a positive blocked allowance throw ArgumentException. No extra context, mutable handles or replacement diagnostics model.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | ../../src/Beep.SimEngine/Standard/NetworkFlow.cs | 10 | medium |
| Modify | ../../src/Beep.SimEngine/Standard/NetworkDefinitions.cs | 7 | medium |
| Modify | ../../src/Beep.SimEngine/Standard/NetworkPolicyContracts.cs | 3 | medium |
| Modify | ../../tests/ExtensionProof/NetworkDefinitionContractsProof.cs | 13 | low |
| Modify | Protocol and test registration | 3 | low |
| New | ../../tests/Beep.SimEngine.Tests/ExternalFlowFactsTests.cs | 150 | medium |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Invalid detached flow shape | ArgumentException, no live mutation |
| Private/unknown network | Original typed refusal with empty output |
| Silent target codec mutation | Guard latches fault before publishing |
| Throw after target/clock mutation | Original callback error is first fault, not a finally replacement |
| Any cached read after fault | Existing healthy-update cutoff; no prefix or healthy save |

## 6. Backward compatibility

None. The original DTO/guard/owner are updated in place; no shim, second inventory or alternate coordinator.

## 7. Verification

ONE NEW exact case: Network flow contracts: external detached facts and first-fault metadata guards. FINAL164/0/0 in1427ms; child PID24544 Exited0/1568ms observed, no truncation/observer error. One clean solution compile8.54s, then one same-case targeted compile5.48s. First attempt164/1/0 in1492ms incorrectly included plain live-throw in the mutation-then-throw expectation; only that test predicate was corrected. Outer60000ms/termination5000ms/output30000 characters; physical waits256 Updates. No executable edit after FINAL; historical/family/full/Oil/Tycoon/platform runtime not run. [Evidence](../../docs/migration/P10_EXTERNAL_FLOW_FACTS_VERIFICATION.md).

## 8. Risks

Constructor validation covers detached shape, not truthful quantities, provider scope ownership or arbitrary CLR/global mutation. Full independent network execution, non-tile topology and utility/corridor breadth still need actual runtime acceptance.

## 9. Out of scope

Original P10.acceptance.005 closure, complete graph provider, broad regression, protected clock/Tycoon edits, Oil/platform/Draw and publication before entire goal AND plan completion.

## 10. Sub-task execution order

1. [x] Inspect actual DTO, metadata guard and native diagnostics publication.
2. [x] Update existing public fact construction and single observation path.
3. [x] Author bounded public physical and callback proof.
4. [x] Observe build and exact runtime, retaining all attempts.
5. [x] Reconcile master/audit/evidence:1365 source anchors/13 quickstart headers,678 selected executable inputs frozen at2026-10-11 11:02:54 UTC, zero post-FINAL drift. All221 literal original requirements/checkboxes unchanged;204 anchors shift seven lines,172checked/49open, additional150. Local links/master columns/thin driver/protected owners validated; full publication condition remains unsatisfied.
