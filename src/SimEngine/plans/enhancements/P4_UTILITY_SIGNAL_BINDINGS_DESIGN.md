# P4-G44: Reusable utility signal bindings — Design Document

Current M101/native2.737/save161/replay2: sole ReadProvider owns complete live target
observation; duplicate finally guard is removed without another utility/process/clock.
Named metadata phase preserves callback causes. Original exact case247/0/0,14fresh/
restored faults and two complete0→90real refuel/manufacture replays with native/direct parity.
[Shared current evidence and remaining gates](../../docs/migration/P10_SIGNAL_CONTENT_CONTRACTS_VERIFICATION.md#current-m101-shared-inventory-network-and-utility-metadata-observation--2026-10-22).

## 0. Problem Statement

The original UtilitySignal-to-UtilitySignalDef dependency prevented independent metadata
from reusing the stateless bridge. G44 replaced it with the declared binding and shared
metric validation. M101 removes duplicate metadata observation, preserving the original
utility consumer's actual state and first callback errors through the shared engine boundary.

## 1. Goals

1. Native/direct authored metadata reuses original utility signal runtime/codec1/schema1.
2. One strict declaration owns id/consumer parsing; actual named utility consumer is mandatory.
3. Original AutomationSignalContracts owns reusable registered metric/owner/runtime admission for utility, inventory and incident bridges.
4. Every loaded reusable utility binding validates before Initialize, including unused definitions; live reads observe actual entity/provider codecs and game-clock facts.
5. One new bounded public-player case covers physically earned utility consumption/shortage/supply, saved twins and replay. Full acceptance stays open.

## 2. Design

`IUtilitySignalDefinition` extends existing IAutomationSignalDefinition with Consumer. Native definition delegates UtilitySignalDeclaration; nongeneric runtime selects the actual focused contract. UtilitySignalBindingContracts validates actual consumer binding and uses existing scoped observation. Existing UtilityContracts and ResourceQuantitySignals calculate facts; no balance, cadence, readiness cache or consumption algorithm is copied. `AutomationSignalContracts.ValidateReusable` admits actual immutable component/owner/runtime/codec/signal schema and exact metric declarations. Existing inventory/incident binding checks delegate that concern while keeping their distinct actual inventory/workflow rules.

## 3. API surface

Engine contracts only, no host/platform API. Strict JSON fields remain id and consumer. Independent registration declares the original four utility metrics/schema1 and stateless codec1. Public OperationsView.ReadSignal remains authoritative.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| New | src/Beep.SimEngine/Standard/UtilitySignalBindings.cs | 75 | medium |
| Modify | src/Beep.SimEngine/Standard/UtilitySignals.cs | 45 | medium |
| Modify | src/Beep.SimEngine/Standard/AutomationSignalContent.cs | 35 | medium |
| Modify | src/Beep.SimEngine/Standard/InventorySignalBindings.cs and IncidentBindings.cs | 55 | medium |
| New | tests/ExtensionProof/UtilitySignalBindingsProof.cs | 85 | low |
| New | tests/Beep.SimEngine.Tests/UtilitySignalBindingTests.cs | 170 | medium |

## 5. Error handling matrix

| Operation | After |
|---|---|
| Absent/wrong consumer | Content refusal before Initialize |
| Unused original runtime without focused metadata | Content refusal before Initialize |
| Runtime metric/schema disagreement | Actual registered metric refusal |
| Pure getter changes private codec/clock | Scoped first fault; no healthy facts/checkpoint |
| Inactive production | Existing explicit unavailable blocker, no invented utility |

## 6. Backward compatibility

Development-only, no shim. Build identity changes; save154/replay1 and stateless schemas stay unchanged.

## 7. Verification

Two clean builds14.69s/8.00s. ONE NEW exact case FINAL161/0/0 in2050ms, PID3420 exit0 observed2202ms. First160/1/0 attempt exposed an actual empty-identity diagnostic bug, fixed in the sole strict declaration; the same case was rerun. No old/family/full/Oil/Tycoon/platform runtime. Consolidated historical inventory/incident paths compile only. [Actual evidence](../../docs/migration/P4_UTILITY_SIGNAL_BINDINGS_VERIFICATION.md).

## 8. Risks

Scoped observations are not global rollback/CLR sandbox. Wider units, consumers, metadata recursion, networks, performance and full acceptance remain distinct open gates.

## 9. Out of scope

G19 protected Tycoon files, resource catalog return types, business defaults, Oil, hosts, platform SDKs, Draw, alternate stock/utility/signal/clock owners.

## 10. Sub-task execution order

1. Consolidate registered-metric admission and migrate utility bridge to actual metadata contract.
2. Author independent JSON and thin physical player proof with bounded diagnostics.
3. Build completed slice and run only new exact bounded case; repair/rerun that same case if needed.
4. Record actual evidence and update tracker/audit without redefining original compound requirements.

## 11. Delivery status

Scoped verified on core2.387.0/save154/replay1, not whole-plan complete or published. Native/direct equal physical timing, four actual utility metrics,32-minute manufacture/eight-unit consumption, shutdown/shortage/real eight-unit physical refuel, private detached views, saved twins/pause/replay/isolation, nine early refusals and four scoped live faults are covered. Selected325 executable inputs hashed AFTER green at2026-10-09 17:34:03 UTC, not pre-run freeze/full transitive closure; no executable edits followed. Prior P4-G43 goal turn was progress. Wider/global/full gates stay OPEN.
