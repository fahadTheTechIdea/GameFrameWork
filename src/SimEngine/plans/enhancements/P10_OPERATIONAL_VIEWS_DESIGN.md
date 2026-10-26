# P10-B10: operational diagnostics durability — Design Document

## 0. Problem Statement

OperationsView directly reads some utility/signal identities, automation metadata and service results. Missing/private/none branches can hide an existing world fault; collections can publish provider prefixes. InfrastructureView and ResourceFlowView need the same publication boundary around their actual contract services. ControlActionDispatch.CopyTargets verifies in finally and can mask the original callback exception. The existing EntityObservation owns purity admission; views publish authoritative detached facts only.

## 1. Goals

1. Observe complete operation/utility/signal/target entity codecs and selected providers before publishing facts.
2. Enforce first-fault cutoff for every operational view and native automation query/preview, including missing/private/empty branches.
3. Stage whole lists locally; preserve original callback failures, including combined throw and clock mutation.
4. Reuse one EntityObservation implementation for whole-entity observation; remove redundant orchestration from condition/incident views without changing owners.
5. Verify physical utility satisfaction, actual controls, deferred automation and connected-network throughput/backpressure through public views, saved continuation, privacy and one clock.

## 2. Design

Extend EntityObservation.ReadProvider with optional observeTargetState using the actual original Entity.Sync; no copy, catalog, runtime provider or new mutable state. Existing view helpers delegate this common boundary. OperationsView observes actual capabilities, original action/rule definitions and IAutomationRules. AutomationProfileSnapshot.VisibleTo guards its complete actual metadata/dependency read, shared with original configure admission. ControlActionDispatch.CopyTargets returns locally collected IDs through ReadProvider rather than finally verification. Native AutomationSystem queries/previews check the original World first fault and stage detached lists. InfrastructureView guards selected INetworkDiagnostics, uses local list/out values and observes whole target entities. LineService pool/connection/flow diagnostics stage native facts and cut off missing/private queries. ResourceFlowView observes the existing journal; native journal reads check the same World first fault.

Proof composition reuses existing pure UtilitySignalBindingTests, ControlContentContractTests and NetworkSignalBindingTests JSON factories. New OperationalViewProofModule only captures existing metadata and installs negative hooks; normal gameplay remains in the engine and original authored modules. No duplicated workflow or physics. Metadata hooks in existing non-friend probe definitions execute only when explicitly armed by the negative proof.

## 3. API surface

No new gameplay API or state codec. Existing view methods reject faulted worlds and publish complete immutable facts. Build2.429.0/save154/replay1; operation view registration unchanged because its shape is unchanged. EntityObservation optional argument is internal only.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| Modify | ../../src/Beep.SimEngine/Kernel/EntityObservation.cs | 5 | medium |
| Modify | ../../src/Beep.SimEngine/Views/OperationsView.cs | 120 | medium |
| Modify | ../../src/Beep.SimEngine/Views/EquipmentConditionView.cs and IncidentsView.cs | 8 | low |
| Modify | ../../src/Beep.SimEngine/Standard/AutomationProfileSnapshot.cs, AutomationSystem.cs, ControlActionPolicies.cs | 40 | medium |
| Modify | ../../src/Beep.SimEngine/Views/InfrastructureView.cs and ResourceFlowView.cs | 60 | medium |
| Modify | ../../src/Beep.SimEngine/Standard/NetworkDiagnostics.cs, NetworkFlow.cs, ResourceFlows.cs | 40 | medium |
| Modify | Existing probe metadata, utility composition visibility, registration and protocol identity | 30 | low |
| New | ../../tests/ExtensionProof/OperationalViewProof.cs | 70 | medium |
| New | ../../tests/Beep.SimEngine.Tests/OperationalViewTests.cs | 220 | medium |

## 5. Error handling matrix

| Failure | Result |
|---|---|
| Healthy missing/private/unsupported binding | Existing typed refusal; no invented quantities/state |
| Already faulted World | Reject before missing/private/empty branch |
| Late callback throws or alters clock/neighbor codec | First fault latches, complete output withheld; save refuses |
| Throw plus clock mutation | Original callback error preserved, no finally masking |

## 6. Backward compatibility

None. Exact current build is required; no legacy owner, shim or migration.

## 7. Verification

ONE NEW exact case: `Operational views: private utility flow and deferred controls publish only healthy complete facts`. FINAL531/0/0 in2474ms; PID24980 exit0/observed2616ms, untruncated output/no observer error. One solution compile13.34s and two same-case targeted compiles5.78s/4.39s, all clean and observed terminal before respective runtimes. All three attempts retained in [evidence](../../docs/migration/P10_OPERATIONAL_VIEWS_VERIFICATION.md). Outer60000ms/termination5000ms/output30000 characters, physical waits256 Updates. Source review plus focused positive/negative proof, not broad stress/global/full regression.

Original P10.acceptance.025 remains OPEN: native flow facts report elapsed service and next allowance, not effective delivered throughput. The original journal proves actual transfer amount; this stage does not add a host-derived rate or close that broader acceptance item. All221 literal original requirements/checkboxes unchanged;171checked/50open, additional148.

## 8. Risks

Whole entity observation has an allocation/CPU cost; budgets remain OPEN. Arbitrary private CLR or cross-entity state without selected target codecs is not a sandbox. No transient guard becomes a saved state owner. Shared native rules/stock/work/clock protocols stay unchanged.

## 9. Out of scope

Oil/Tycoon/platform/renderers, generic electrical physics, broad regressions, hidden size limits, compatibility, commit/push before full goal AND plan. Protected G19 permission remains pending.

## 10. Sub-task execution order

1. [x] Review existing view/contract/dispatch boundaries.
2. [x] Implement common observation and in-place view/service guards.
3. [x] Add one authored public proof with negative-only metadata hooks.
4. [x] Observe one solution build and bounded exact runtime; retain same-case repairs.
5. [x] Reconcile master/audit/evidence and current-source metadata:1357 source anchors/13 quickstart headers,674 selected inputs;221 original literal requirements/checkboxes unchanged,171checked/50open, additional148. Read-only paths/links/master/thin-driver/protected owners and zero post-FINAL executable drift verified; no staging/publication.
