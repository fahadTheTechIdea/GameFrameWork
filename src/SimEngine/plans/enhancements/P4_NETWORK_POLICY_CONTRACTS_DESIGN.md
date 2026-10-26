# P4-G47: Authored network policy contracts — Design Document

## 0. Problem Statement

NetworkDef.ResolveCore required NetworkFlowPolicyDef, NetworkSolverDef and NetworkDemandPolicyDef even though execution already used focused interfaces. Independent JSON definitions therefore needed native inheritance. LineService admitted only used solvers' time callbacks. Existing arbitration guards observed endpoint versions, but not participating entities' private codecs or the actual pool inventory codec.

## 1. Goals

1. Resolve all three network policy roles through immutable content contracts, deleting required native abstract bases.
2. Reuse one standard algorithm per ranking, capacity demand and resource transfer responsibility.
3. Admit every loaded policy alias and every solver's actual session time before Initialize, including unused content.
4. Detect callback changes to the sole clock and participating entity/pool owners without rejecting authorized context transfers.
5. Verify authored independent/native physical parity through ONE new bounded player case.

## 2. Design

INetworkFlowPolicy, INetworkSolver and INetworkDemandPolicy extend IContentDefinition. Native definitions implement these directly through the universal Def authoring lifecycle; original NetworkDef resolves the interfaces in the same content catalog. The three policy-specific abstract bases are deleted with no substitutes or compatibility reader.

RoundRobinNetworkRanking, CapacityNetworkDemand and ResourceTransferNetworkSolver own their respective standard algorithms. Native and independent definitions delegate to these functions; the proof does not copy gameplay formulas. UtilityNetworkDemandPolicyDef retains its distinct consumer-driven demand algorithm as a direct contract implementation.

NetworkPolicyContracts admits actual frozen catalog/identity/module aliases through the original EntityObservation provider guard. LineService.ValidateComposition validates every loaded policy, including unused solver ValidateTime callbacks, before any system Initialize. No additional catalog, service or mutable registration table is introduced.

FlowReadGuard observes each participating entity's original complete Sync and its actual NetComponent.Inventory.Sync. Before/after pure demand/ranking and bounded solver callbacks, verify those observations plus existing endpoint identities/versions. Only an accepted engine transfer can refresh their baseline. Exact selected source/destination/entity changes are allowed for that receipt; unrelated participating private codecs remain guarded. NetworkPolicyContracts.Read guards the actual provider and sole clock around solver execution while allowing verified stock transfers through the original context.

Do not hash LineService.Sync during Flow: unfinished step cursors legitimately name the completing update before the game clock commits it. Guard the original embedded pool inventory codec instead, without inventing a checkpoint format, flow-state replica or rollback. Whole network-owner private metadata, nonparticipating entities and arbitrary private CLR state are NOT fully covered by this slice.

LineService still owns topology, pool inventories, elapsed allowances and successful-service turns. NetworkExecutionContext retains exact request membership, positive earned allowance, native quantum, once-per-step attempts and expiry. ResourceTransactions, WorkBoard, hauling, gates and the one clock retain all gameplay state and execution.

## 3. API surface

The three policy interfaces now include stable content Id/Module through IContentDefinition. Independent Def implementations are registered through normal module sections and master-listed JSON documents. Native JSON section names and network policy selectors stay explicit. Public player commands, immutable views, clock control, save and replay remain unchanged. Shared standard algorithm methods are engine-side extension helpers, not host gameplay code.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| Modify | Standard/NetworkFlowPolicies.cs | 20 | medium |
| Modify | Standard/NetworkSolvers.cs | 25 | medium |
| Modify | Standard/NetworkDemands.cs | 20 | medium |
| Modify | Standard/Networks.cs | 6 | medium |
| Modify | Standard/NetworkFlow.cs | 30 | medium |
| New | Standard/NetworkPolicyContracts.cs | 45 | medium |
| Modify | tests/ExtensionProof/NetworkFlowPolicyProof.cs, NetworkSolverProof.cs, NetworkDemandProof.cs | 20 | low |
| New | tests/ExtensionProof/NetworkPolicyContractsProof.cs | 115 | low |
| New | tests/ExtensionProof/content/network-policy-contracts.json | 5 | low |
| New | tests/Beep.SimEngine.Tests/NetworkPolicyContractTests.cs | 135 | medium |

Core paths are relative to src/Beep.SimEngine. Program registration, embedded-resource registration, EngineInfo and current-engine-protocol.json also change. Original save154/replay1 and persistent network/resource layouts remain.

## 5. Error handling matrix

| Input/callback | Result |
|---|---|
| Unknown contract selector or non-owned/non-frozen policy alias | Typed content refusal; no substitute |
| Unused solver rejects actual session time | Refusal before Initialize |
| Time validation changes the actual clock | Scoped observation fault before Initialize |
| Live callback throws or changes clock/entity/participating private codec/pool | Faulted session; views/save/hash refuse |
| Context retained beyond the completing step | Expired capability refuses |
| Authorized engine-owned transfer | Verify receipt, then refresh only accepted observation baseline |

## 6. Backward compatibility

Development only; no compatibility classes or old policy readers. Core2.390.0 uses save154/replay1 and the original exact build/content identity checks. Existing proof subclasses migrate directly to their contract; old runtime cases compile only.

## 7. Verification

ONE clean completed-slice build15.11s, zero warnings/errors. ONE NEW exact case FIRST/FINAL143/0/0 in901ms; child PID39156 exited0, observed1067ms. Native/direct four-unit earned flow, backpressure, physical hauling, saved twins, paused clock, replay and isolated session are verified. Two unused solver admission failures and18 live callback/participating-owner faults are covered. [Evidence](../../docs/migration/P4_NETWORK_POLICY_CONTRACTS_VERIFICATION.md).

No old/family/full/Oil/Tycoon/platform runtime. Full regression remains unauthorized and OPEN.

## 8. Risks

Codec observation adds hashing cost; this case is not an allocation/throughput budget. Guards are scoped to actual participating owners, not a CLR sandbox or global rollback. Shared content callback installation is negative proof instrumentation only; it is not a supported mutable policy configuration API. Wider hostile metadata, provider recursion, utility demand, full topology ownership and pool receipt fault matrices remain OPEN.

## 9. Out of scope

NetworkDef and LineDef themselves still require native content classes. Other gates, full utility/custom solvers, neutral/private/public networks, dynamic corridors, populated promises, congestion, starvation, lockstep, fuzz/soak and full colony acceptance remain OPEN. No physical corridor-construction claim: the selected proof starts with engine-authored startup geometry. G19 authority, protected Tycoon files, Oil, hosts, rendering/platforms and GameClock remain untouched.

## 10. Sub-task execution order

1. Remove native policy bases; migrate catalog selectors and native/existing independent definitions in place.
2. Add all-loaded alias/time admission and participating owner/clock observations without duplicating state.
3. Register explicit master-listed independent JSON; build completed slice and run only the new exact bounded player case.
4. Record source hashes, process evidence and tracker status; preserve original compound gate text/checkboxes and delayed publication condition.

## 11. Delivery status

Scoped verified on core2.390.0, not full plan complete. Selected353 executable inputs were hashed AFTER green at2026-10-09 18:25:16 UTC; this is not a pre-run freeze/full transitive binary/environment closure. No executable edits followed. Previous goal turn was no progress (publication condition restatement); this turn changes authoritative engine source and verifies the next safe action. No staging, commit or push.
