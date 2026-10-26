# P4-G43: Reusable inventory signal bindings — Design Document

Current M101/native2.737/save161/replay2: sole ReadProvider owns the full live target,
metadata and clock; duplicate finally verification is removed. Named inventory metadata
phase preserves original callback causes, including combined mutation/throw. Original
exact case262/0/0,14fresh/restored faults and two complete0→32physical recovery replays.
[Shared current evidence and remaining gates](../../docs/migration/P10_SIGNAL_CONTENT_CONTRACTS_VERIFICATION.md#current-m101-shared-inventory-network-and-utility-metadata-observation--2026-10-22).

## 0. Problem Statement

`InventorySignal` required `InventorySignalDef` even though stock and typed signal contracts already exist. Independent authored components could not reuse the stateless engine reader. This stage removes that concrete dependency without creating a second inventory or signal calculator.

## 1. Goals

1. Native and independent JSON metadata reuse the original `InventorySignal`, codec1 and registered signal schema3.
2. One declaration reads/resolves explicit inventory identity and null or nonempty readiness quantities.
3. All loaded bindings, including unused definitions, validate actual owner, inventory, capacity and metric aliases before Initialize.
4. Live reads guard actual entity/inventory codecs and sole game-clock facts.
5. One new bounded exact public-player case proves physical material consumption changes immutable observations, saved twins and replay; wider gates stay open.

## 2. Design

`Standard/InventorySignalBindings.cs` owns `IInventorySignalDefinition`, sole `InventorySignalDeclaration` and binding admission/observation. The interface extends existing `IAutomationSignalDefinition` with `InventoryKey`, `Inventory` and `Quantities`. Explicit null means numeric-only, never fabricated readiness. Native `InventorySignalDef` delegates the declaration; nongeneric `InventorySignal` selects the actual contract. Existing `ResourceQuantitySignals`, stock, capacity, work and transaction owners remain unchanged. `InventorySignalsModule.Compose` checks every loaded reusable binding before any Initialize.

## 3. API surface

Engine contracts only; no host endpoints or renderer. JSON fields remain `id`, `inventory`, required `quantities` (explicit null or nonempty native bill). ContentTypes registers independent component identities/schema1 with original eight metric bindings/schema3.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| New | ../../src/Beep.SimEngine/Standard/InventorySignalBindings.cs | 150 | medium |
| Modify | ../../src/Beep.SimEngine/Standard/InventorySignals.cs | 90 | medium |
| Modify | ../../src/Beep.SimEngine/Standard/InventorySignalsModule.cs | 1 | low |
| New | ../../tests/ExtensionProof/InventorySignalBindingsProof.cs | 90 | low |
| New | ../../tests/Beep.SimEngine.Tests/InventorySignalBindingTests.cs | 160 | medium |

## 5. Error handling matrix

| Operation | Before | After |
|---|---|---|
| Independent definition | Concrete cast failure | Actual focused contract admission |
| Invalid unused binding | Native-only threshold checks | Explicit early content refusal |
| Mutating metadata read | Scalar guard only | Actual entity/inventory codec scope and fault |
| Numeric-only readiness | Unavailable | Same explicit no-threshold blocker |

## 6. Backward compatibility

Development-only; no compatibility model/shim. Build identity changes; save154/replay1 and stateless component codec1/signal schema3 remain.

## 7. Verification

Two clean completed-slice builds (14.40s and6.11s). ONE NEW exact case FIRST/FINAL176/0/0 in978ms, PID14452 exit0, observed1129ms. No historical, family, full, Oil, Tycoon or platform runtime. Original full P1–P12 gates remain open. [Actual evidence](../../docs/migration/P4_INVENTORY_SIGNAL_BINDINGS_VERIFICATION.md).

## 8. Risks

Scoped entity codecs are not global rollback or an arbitrary CLR sandbox. Wider inventory callbacks, metadata recursion, all quantity units/quantums and throughput require separate proof; no broad completion claim.

## 9. Out of scope

Tycoon/G19 protected files, resource catalog return-type migration, adapters, Draw, platform SDKs, business defaults, alternate stock stores and clocks.

## 10. Sub-task execution order

1. Replace concrete dependency and consolidate declaration/admission.
2. Author independent JSON proof and thin public-player driver.
3. Build completed slice; run only new exact bounded case, repairing/rerunning that case if needed.
4. Record actual evidence and reconcile original tracker/audit without changing compound gates.

## 11. Delivery status

Scoped verified on core2.386.0/save154/replay1, not full-plan complete or published. Native/direct original runtime parity, eight facts, physical travel4→20 and stock20→12, explicit numeric-only blockers, private detached views, saved twins/pause/replay/session isolation, eleven early refusals and four scoped live faults are covered. Selected320 executable inputs hashed AFTER green at2026-10-09 17:22:51 UTC, not a pre-run freeze/full transitive closure; no executable edits followed. Prior status-only publication acknowledgement was no progress; this stage changes the actual engine contract. Wider gates remain OPEN.
