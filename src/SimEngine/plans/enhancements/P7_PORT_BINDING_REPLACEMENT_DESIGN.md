# P7-ER10 — Named-port backing ownership during replacement

Current framework2.713/save161/replay2. BindsEntityInventory remains the original required ownership guarantee; later selection/topology prediction extends logistics and stock-control targets rather than weakening it. [Current owner and scoped evidence](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m52-independent-automation-stock-targets--2026-10-22). Original ER10 execution/results below are historical.

## 0. Problem

Capacity and resource acceptance do not prove backing-store identity. Before ER10, a named stock-policy endpoint could pass admission while selecting a separate pool, then fault after physical work under the then-native-only reference rule. The ownership guarantee fixed that ambiguity; current logistics additionally forecasts explicitly declared independent/topology bindings through its original owner.

## 1. Goals

Make entity-inventory binding explicit in the existing named-port contract. Refuse incompatible durable endpoint intent before work without forbidding legitimate standing-route reselection.

## 2. Design

IResourcePortDefinition requires BindsEntityInventory. True guarantees every ready resolution aliases the owner's actual IInventory and native entity endpoint. False gives no such guarantee: connected and selectable stores remain valid ports.

ResourcePortContracts verifies true claims against the actual returned store inside the existing pure observation boundary. No additional endpoint registry, inventory, reservation book, scheduler or replacement authority is introduced.

Existing LogisticsPolicyRules replacement admission still accepts an explicit entity-inventory requirement for consumers that require it. Current LogisticsService shares one prospective binding path for stock policies, evacuation and stock-control targets: declared initial/retained selectors or topology-owned future contact. Stock policies/control targets can retain a pinned original; evacuation requires current access to the unchanged public evacuating provider. Routes retain current-selection dispatch semantics. StockControlActionPolicyDef now uses required ILogisticsStockPolicies prediction/actual binding instead of assuming native ownership. Actual prepared binding checks remain strict and independent of demand-row callback order.

## 3. API and state

Required port metadata; native and independent implementations migrate directly. Original ER10 was core2.567/save157/replay1; current framework2.713/save161/replay2 adds no port payload or codec. Declaration semantics come from each contract implementation and existing JSON-authored backing-store content, not a second state table.

## 4. Files

Original resource-port contracts, native inventory/network/operating port definitions, logistics admission/reference owner and stock control action; independent proof implementations; explicit master-listed port-replacement.json; two existing focused player/provider proofs.

## 5. Errors

Missing port retains NotFound. Unsupported durable bindings still refuse before work. Supported policy bindings use explicit prediction and actual ownership/capacity/claim checks; independent evacuation rejects changed backing and acquired work. A provider falsely claiming local inventory but returning another owner is a latched provider fault, never a silently redirected store.

## 6. Migration

Mandatory contract migration, no default interface fallback, compatibility wrapper or legacy path. Independent authors declare the actual binding semantics of their port. False does not mean the port is invalid or must own a new inventory.

## 7. Verification scope

Current M52 four selected scopes6679/0/twenty-one actual replays qualify independent stock-control and policy slices, including original automation550/0. Native/independent evacuation qualification remains M51/core2.712; no current rerun of every consumer or original ER10 inventory-compatibility case. Historical measurements retain their identities.

Original ER10 logistics proof added then-unsupported wrong-backing refusal and a standing-route physical reselection. M46–M51 replaced the blanket logistics limitation with declared policy/recovery support and expanded the same proof owners; that old refusal is not current behavior for all independent stores. Current supported paths preserve identities, finite resources, saved loaded continuation, privacy and isolation.

Existing inventory compatibility proof adds a false ownership claim and confirms the original first fault and checkpoint refusal. Existing automation replacement proof verifies retained typed stock targets and decisions under the required metadata.

## 8. Risks

Guaranteed metadata is enforced when an actual ready port resolves. Unavailable ports do not fabricate a stock owner. Arbitrary acquired/shared endpoint replacement is still not admitted by this stage. A metadata guarantee is not permission to migrate live jobs or rewrite historical observations.

## 9. Out of scope

Oil, business rules, the sole game clock, hosts, rendering/platform adapters, acquired job migration, other reference owners, full regression and Git publication.

## 10. Execution

Implemented and scoped verified:261 logistics,45 inventory-compatibility and550 automation checks;856/0 across three exact bounded cases. Final solution build7.24s, zero warnings/errors. The initial new fixture referenced a nonexistent capacity policy; it was corrected to the existing authored40-unit policy before final verification. Full original221 requirements/194 recorded checked/27 open unchanged; additional322. [Evidence](../../docs/migration/P7_PORT_BINDING_REPLACEMENT_VERIFICATION.md).
