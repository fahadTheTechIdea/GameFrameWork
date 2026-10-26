# P4-G45: Reusable network signal bindings — Design Document

Current M101/native2.737/save161/replay2: sole ReadProvider owns complete live target
observation; no duplicate finally guard or network/stock authority. Named metadata phase
preserves callback causes. Original exact case255/0/0,14fresh/restored faults and two
complete0→28replays of real flow/backpressure/physical hauling with native/direct parity.
[Shared current evidence and remaining gates](../../docs/migration/P10_SIGNAL_CONTENT_CONTRACTS_VERIFICATION.md#current-m101-shared-inventory-network-and-utility-metadata-observation--2026-10-22).

## 0. Problem Statement

The existing stateless NetworkSignal depended on NetworkSignalDef rather than an authored binding contract. Independent JSON definitions could not reuse the engine's connected-pool facts without native inheritance. The existing network diagnostics, stock, solver, gates and clock already own their responsibilities and must not be duplicated.

## 1. Goals

1. Bind the original runtime through INetworkSignalDefinition, using the original codec1 and signal schema2.
2. Share one strict id/network declaration and the existing registered metric admission.
3. Validate every loaded binding, including unused entities, before Initialize.
4. Observe actual immutable network aliases and entity/provider codec/clock facts during reads.
5. Prove the seam through one bounded JSON/public-player case; keep compound acceptance open.

## 2. Design

NetworkSignalDeclaration owns JSON identities, resolution and owner connection checks. Native and independent definitions delegate it. NetworkSignalBindingContracts uses AutomationSignalContracts.ValidateReusable for actual frozen owner/runtime/codec/schema/metrics, checks the actual catalog NetworkDef and declared INetworkPortsDefinition connection, and passes the validated network to the read callback exactly once. InfrastructureModule admits every loaded original runtime binding before Initialize. Live observation uses the existing whole-entity/provider codec and clock guards.

INetworkDiagnostics.ReadQuantity and ResourceQuantitySignals remain the sole fact readers/calculators. LineService retains topology, pool inventories, transfer timing, demand and solver ownership. SignalGateCondition and the original native gate still interpret backpressure. No new store, cached balance, topology solver, stock subtraction, gate state, cadence or clock exists.

## 3. API surface

INetworkSignalDefinition extends existing IAutomationSignalDefinition with its resolved Network. Strict JSON fields remain id/network. Independent registrations declare the original five quantity metrics and signal schema2, separately from stateless codec1. Existing OperationsView.ReadSignal and InfrastructureView remain the public read boundary.

## 4. Files to change

NetworkSignalBindings.cs is the new focused metadata contract/declaration/admission owner. NetworkSignals.cs delegates it and removes the concrete runtime dependency. InfrastructureModule.cs performs composition admission. Kernel/SaveGame.cs and the protocol fixture use core2.388.0. The independent proof, two master-listed JSON documents and one new test driver demonstrate reuse rather than implementing gameplay.

## 5. Error handling matrix

| Input/read | Engine response |
|---|---|
| Missing/empty/unknown/unsupported network or extra field | Content refusal before Initialize |
| Original runtime without focused contract | Content refusal before Initialize, even unused |
| Null network or schema/metric disagreement | Actual immutable registered binding refusal |
| Physically disconnected connection | Existing typed unavailable blocker |
| Foreign-company signal read | Original NotOwned refusal, no private value |
| Getter throws or mutates clock/entity/private codec | Scoped first fault; views/save/hash refuse |

## 6. Backward compatibility

No legacy path or compatibility shim. Core identity changes to2.388.0; save154/replay1 and the original signal/network/stock state schemas remain unchanged. Exact build matching remains authoritative.

## 7. Verification

Five clean slice builds; ONE NEW exact case, five attempts, FINAL169/0/0 in777ms. PID30800 exited0, observed935ms. The first four attempts exposed authored fixture/expectation problems, not engine stock or timing failures; details and repairs remain in [actual evidence](../../docs/migration/P4_NETWORK_SIGNAL_BINDINGS_VERIFICATION.md). No old/family/full/Oil/Tycoon/platform runtime was run.

## 8. Risks

Scope is actual selected metadata and its connected-fact bridge, not a CLR sandbox or global rollback guarantee. Promise-populated metrics, hostile port implementations, topology edits during gameplay, neutral/public networks, recursion, performance, lockstep, fuzz/soak and full acceptance remain separate open obligations.

## 9. Out of scope

NetworkDef and its required concrete policy bases are NOT migrated here. SignalNetworkGateDef/runtime still require their own contract migration. The proof starts with one engine-initialized JSON-authored pipe; it does not prove physical corridor construction or incident response. G19 protected Tycoon files, optional business rules, Oil, hosts, Draw and platform SDKs are untouched.

## 10. Sub-task execution order

1. Introduce sole metadata declaration/binding contract and migrate original stateless runtime.
2. Admit all loaded actual bindings before Initialize, guard live metadata and preserve existing facts/owners.
3. Author an independent JSON/master selection and thin player proof; build completed slice and run only that exact bounded case.
4. Record all attempts, selected post-run hashes and unchanged original compound gates in tracker/audit.

## 11. Delivery status

Scoped verified on core2.388.0/save154/replay1, not whole-plan complete or published. Native/direct equal chronology, actual shared/scaled pool metrics, four units per game minute, eight-unit backpressure, source/pool conservation, physical five-unit hauling to a local inventory without altering pool facts, detached/private views, saved twins/pause/replay/session isolation,11 early malformed unused refusals and4 live getter faults are covered. Selected339 executable inputs were hashed AFTER green at2026-10-09 17:52:20 UTC, not a pre-run freeze/full transitive closure. No executable edits followed. Full P1–P12 and broader gates remain OPEN; prior P4-G44 goal turn was progress.
