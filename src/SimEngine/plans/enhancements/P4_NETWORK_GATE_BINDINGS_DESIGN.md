# P4-G46: Reusable network gate bindings — Design Document

## 0. Problem Statement

NetworkSignalGates.cs still coupled SignalNetworkGate to native SignalNetworkGateDef. Independent file-authored definitions could not reuse the engine's gate interpretation without native inheritance. Connection validation was embedded separately in native signal/gate paths. Empty condition identities could escape the content diagnostic because Code construction preceded validation.

## 1. Goals

1. Bind the original stateless gate through ISignalNetworkGateDefinition, preserving codec1.
2. Share one strict network-gate declaration and original SignalGateCondition evaluator.
3. Admit every loaded actual immutable gate/network/condition binding before Initialize, including unused definitions and original runtime/codec mismatches.
4. Guard live metadata through original entity/provider codec and game-clock observations; consume validated lists once.
5. Prove normal source-reserve shutdown, physical resupply and earned-rate reopening through one new bounded player case, without broad completion claims.

## 2. Design

ISignalNetworkGateDefinition extends INamedCapabilityDefinition with Networks/Conditions. Native and independent ComponentDef<SignalNetworkGate> implementations delegate SignalNetworkGateDeclaration. Its strict JSON parsing validates raw identities before Code construction, resolves unique networks, sorts stable identities and delegates conditions to the original shared evaluator.

ExecutionGateContracts.ValidateReusable admits the actual frozen named owner, original runtime and exact registered codec. NetworkGateBindingContracts checks nonempty unique ordered actual catalog networks and resolved compatible signal conditions. InfrastructureModule performs this admission for every loaded original runtime binding before Initialize. Live reads capture validated networks/conditions once inside the existing whole-entity/provider codec/clock guard; the original SignalGateCondition.ReadGate produces detached facts. NetworkConnectionContracts now owns connection admission shared with network signals, rather than two separate implementations.

LineService/INetworkDiagnostics retain topology, pool stock, demands, transfer rates and physical flow. InventorySignal and Storage retain reserve facts and material/promise ownership. Existing work/haul services perform resupply. No readiness cache, new balance, alternate transfer solver, gate state, synthetic time or test-owned reserve decision is introduced.

## 3. API surface

Engine contracts only. JSON still has id, networks and conditions with stable condition/source/signal/blocker identities and explicit boolean expectation or numeric range/unit. Independent definitions use the same declaration and stateless codec1, not native inheritance. Existing InfrastructureView.ReadGates and ordinary HaulResourcesCommand remain the player boundary. This slice introduces no host/platform API.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| New | Standard/NetworkGateBindings.cs | 110 | medium |
| Modify | Standard/NetworkSignalGates.cs | 70 | medium |
| Modify | Standard/ExecutionGates.cs | 12 | medium |
| Modify | Standard/NetworkSignalBindings.cs | 25 | medium |
| Modify | Standard/SignalGateCondition.cs | 10 | medium |
| Modify | Standard/InfrastructureModule.cs | 1 | medium |
| New | tests/ExtensionProof/NetworkGateBindingsProof.cs | 85 | low |
| New | tests/ExtensionProof/content/network-gate-bindings.json | 15 | low |
| New | tests/Beep.SimEngine.Tests/NetworkGateBindingTests.cs | 125 | medium |

Core paths above are relative to src/Beep.SimEngine. The existing network player factory is extended for explicit extra master-listed modules/documents instead of duplicating session construction. Core identity and the protocol fixture move to2.389.0.

## 5. Error handling matrix

| Input/read | Engine response |
|---|---|
| Empty/missing identities or malformed networks/conditions | ContentException before Initialize |
| Unsupported network or incompatible signal source | Actual owner/catalog refusal, no substitute |
| Original runtime lacking focused contract or codec1 | Refusal before Initialize, even unused |
| Metadata supplies null/empty/duplicate networks or conditions | Deterministic binding refusal |
| Connected but not selected network | Non-applicable gate, no conditions/blocker |
| Getter throws or changes actual clock/entity/private codec | First scoped fault; reads/save/hash refuse |

## 6. Backward compatibility

Development only; no legacy accessor or compatibility path. Core2.389.0 uses original save154/replay1 and gate/stock/network state schemas. Existing exact build matching remains authoritative.

## 7. Verification

One clean slice build14.93s; ONE NEW exact case FIRST/FINAL191/0/0 in924ms. PID30296 exited0, observed1084ms. Native/direct gate definitions share actual resupply/resume chronology. No old/family/full/Oil/Tycoon/platform runtime was run. [Actual evidence](../../docs/migration/P4_NETWORK_GATE_BINDINGS_VERIFICATION.md).

## 8. Risks

Scoped codec observations do not establish a private CLR sandbox/global rollback guarantee. Boolean/multiple conjunctive conditions, dynamic topology, populated reservations, public/neutral networks, hostile ports, all signal providers, recursion, allocations, lockstep/fuzz/soak and full acceptance remain separate open gates.

## 9. Out of scope

NetworkDef and concrete network flow/solver/demand policy dependency migrations remain OPEN. Other gate kinds and arbitrary third-party INetworkFlowGate implementations are not migrated or fully proven here. The proof uses one JSON-authored startup pipe, not physical corridor construction or industrial incident recovery. G19 authority, protected Tycoon files, Oil/hosts/Draw/platforms and GameClock are untouched.

## 10. Sub-task execution order

1. Extract strict focused metadata and shared actual connection/registered gate admission; migrate original runtime in place.
2. Validate all loaded aliases before Initialize and guard actual live metadata; repair raw condition identity diagnostics in the original evaluator.
3. Author independent master-selected JSON and thin physical resupply proof; build completed slice and run only its new bounded exact case.
4. Record evidence/hashes and update tracker/audit without altering original compound checkbox requirements or publication condition.

## 11. Delivery status

Scoped verified on core2.389.0/save154/replay1, not whole-plan complete or published. Original reserve gate admits two four-unit injections from source30, then closes at source22/pool8. A normal five-unit truck delivery—not a promise—earns reopening; exactly one more four-unit injection leaves source23/pool12. Source plus pool35 equals initial30 plus physical resupply5. Native/direct chronology, non-applicability, private/detached views, saved twins/pause/replay/isolation,26 malformed unused refusals and4 live getter faults are covered. Selected343 executable inputs were hashed AFTER green at2026-10-09 18:04:36 UTC, not a pre-run freeze/full transitive closure. No executable edits followed. Prior P4-G45 goal turn was progress; wider/full gates remain OPEN.
