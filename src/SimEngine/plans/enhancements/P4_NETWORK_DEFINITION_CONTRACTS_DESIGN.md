# P4-G48: Authored network definitions — Design Document

## 0. Problem Statement

Network policies were already focused contracts, but ports, signals, gates, diagnostics and LineService still required concrete NetworkDef. A mod could not register a network definition independently without changing those consumers. Native parsing also owned resource selectors; duplicating it for extensions would create a second authoring path.

## 1. Goals

1. Expose INetworkDefinition and migrate every engine network consumer/query/signature to the same contract.
2. Let native and independently registered JSON definitions share one strict NetworkDeclaration.
3. Validate actual content/reference/resource/rate aliases for all loaded networks, unused included, before Initialize.
4. Observe live selected-network callbacks with the original scoped entity/provider/clock guards.
5. Prove native/direct physical parity without creating another topology, pool, inventory or time owner.

## 2. Design

INetworkDefinition extends IContentDefinition with original line/capacity/policy/rate/resource/public metadata and resource selectors. NetworkDef remains only a normal native JSON registration, not a required extension base. Both it and independent Def implementations delegate NetworkDeclaration.Read/Resolve/Carries/Rate. ResourceRateSpec and original rational native rates remain their sole authoring/math owners.

PortSpec, NetworkPortsDef, signal/gate definition contracts, gate callbacks, diagnostics/views and LineService now use INetworkDefinition. Original content interfaces index actual objects, not copied definitions. Existing network/pool/flow state and Sync retain their original IDs and layout. No alternate registration table, pool, topology scheduler or native compatibility accessor survives in consumers.

NetworkDefinitionContracts.Validate checks actual catalog Id/Module/reference identity, original line and capacity aliases, actual flow/demand/solver aliases, nonempty matching unique ordered native resource/rate metadata, positive periods and capacity compatibility. Selectors must return the actual declared rate objects and may not claim absent/invalid resources. Public access metadata is observed too. ResolvedContent invokes this same validator after non-entity references resolve and before entity ports resolve. InfrastructureModule and original LineService composition apply it under existing provider/clock observations before Initialize, including unused definitions.

NetworkDefinitionContracts.Read uses original NetworkPolicyContracts actual frozen-alias admission and captures selected entity Sync where present. Gates, connected signals, flow rate/ready/ranking/demand paths and pool resource compatibility use these scoped reads. Existing FlowReadGuard still owns participating entity/pool observation around policy work and authorized transfers. Full LineService checkpoint hashing during unfinished Flow is not introduced.

Native LineDef remains the required corridor prototype for this contract. Its future contract migration is separate and OPEN; do not claim arbitrary non-tile transport/topology support from this slice.

## 3. API surface

Engine-only INetworkDefinition replaces native types in PortSpec.Network, INetworkSignalDefinition.Network, ISignalNetworkGateDefinition.Networks, INetworkFlowGate.ReadGate and LineService.Network. Native networks JSON keeps line, capacity_policy, flow_policy, solver, demand_policy, rates and explicit public selection. A module may register its own section and master-listed file containing independent Def/INetworkDefinition implementations that reuse NetworkDeclaration.

Player commands and immutable InfrastructureView/OperationsView remain the boundary. No host, platform, renderer, adapter or Draw API.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| New | Standard/NetworkDefinitions.cs | 130 | medium |
| Modify | Standard/Networks.cs | 95 | medium |
| Modify | Content/ResolvedContent.cs | 3 | medium |
| Modify | Standard/InfrastructureModule.cs | 1 | medium |
| Modify | Standard/NetworkFlow.cs, NetworkPolicyContracts.cs | 25 | medium |
| Modify | Standard/Capabilities.cs, NetworkDiagnostics.cs | 5 | medium |
| Modify | Standard/NetworkSignals.cs, NetworkSignalBindings.cs | 20 | medium |
| Modify | Standard/NetworkSignalGates.cs, NetworkGateBindings.cs | 20 | medium |
| Modify | Views/InfrastructureView.cs | 4 | medium |
| New | tests/ExtensionProof/NetworkDefinitionContractsProof.cs | 75 | low |
| New | tests/ExtensionProof/content/network-definition-contracts.json | 10 | low |
| New | tests/Beep.SimEngine.Tests/NetworkDefinitionContractTests.cs | 135 | medium |

Core paths are relative to src/Beep.SimEngine. Existing independent signal/gate/policy proofs and historical network/content tests migrate type references only; they compile, not run. Program, embedded-resource registration, EngineInfo and current protocol fixture move to2.391.0.

## 5. Error handling matrix

| Definition/read | Result |
|---|---|
| Missing/empty/unknown explicit reference or invalid raw rate | ContentException, no fallback |
| Foreign line with unset Id/Module | ContentException before dictionary lookup; never ArgumentNullException |
| Null/empty/duplicate/inconsistent metadata or selectors | Actual content refusal before Initialize, even unused |
| Unsupported public metadata callback | Typed content refusal before Initialize |
| Live Rate callback throws or changes clock/entity/private codec | Scoped session fault; views/save/hash refuse |
| Ordinary authorized transfer/haul | Original receipts, earned time and state owners unchanged |

## 6. Backward compatibility

Development only; no native-only consumer overload, compatibility reader or duplicate network model. Native JSON registration is a normal helper using the same declaration. Core2.391.0 retains save154/replay1 and original network/resource state layouts. Exact build/content identity checks remain authoritative.

## 7. Verification

THREE clean builds14.58s/9.03s/6.31s, zero warnings/errors. ONE NEW exact bounded case, THREE attempts:147 passed1 failed822ms;148/0/0 in917ms after engine alias diagnostic repair; FINAL150/0/0 in831ms after source-review addition of unused Public metadata observation and two assertions. Final child PID13948 exited0, observed969ms. [Actual evidence](../../docs/migration/P4_NETWORK_DEFINITION_CONTRACTS_VERIFICATION.md).

No old/family/full/Oil/Tycoon/platform runtime. Full regression remains unauthorized and OPEN.

## 8. Risks

Scoped observations are not a private CLR sandbox/global rollback guarantee. Guards do not fully prove every topology/public-access/property/private-provider callback, nonparticipating state or performance budget. Extending the contract does not make native LineDef/general tile infrastructure provider-neutral. Historical runtime suites need separate authorized verification.

## 9. Out of scope

Native LineDef/ports authoring-base migration, non-tile networks, all utility/custom/public-neutral/congestion/topology/promise/receipt matrices, recursive providers, private CLR/global rollback, allocations/lockstep/fuzz/soak and full colony acceptance remain OPEN. Startup pipe geometry is engine initialization, not physical corridor construction. G19 authority, protected Tycoon files, Oil, hosts/platforms/Draw and GameClock remain untouched.

## 10. Sub-task execution order

1. Extract focused contract/shared declaration and migrate all network consumers in place.
2. Admit all actual metadata before entity resolution/Initialize and add scoped selected-network reads.
3. Register master-listed independent network JSON and one thin native/direct physical player case.
4. Build the slice/run only that exact case; repair actual diagnostic failure and cover the source-reviewed Public gap.
5. Record every attempt/build/source hash; update tracker/audit without changing original compound requirements or publication condition.

## 11. Delivery status

Scoped verified, not whole-plan complete. Native/direct source30/pool0 becomes source26/pool4 then source22/pool8 over two game minutes. Existing gates block catch-up flow. Ordinary truck haul moves five actual depot units to watcher over travel/loading/unloading without changing pool stock. Save/load twins, pause, replay, isolated session, original pool/flow views,37 unused malformed refusals and4 live Rate faults are proven.

Selected363 executable inputs were hashed AFTER final green at2026-10-09 18:41:29 UTC; not a pre-run freeze or full transitive source/binary/environment closure. No executable edits followed. Previous goal turn was progress; original P1-P12 requirements remain OPEN. No staging, commit or push.
