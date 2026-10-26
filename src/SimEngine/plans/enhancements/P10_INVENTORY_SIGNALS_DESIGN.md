# P10-B2 — Authored inventory readiness signals

Current follow-on: [B8 receiving-room backpressure](P10_RECEIVING_ROOM_DESIGN.md) replaces the inventory
signal schema with3 and network schema with2 on2.140.0/save76;447 checks/two exact expanded suites.
Existing readiness and stock owners remain; preceding identities/evidence below are historical.

Status: historical focused implementation verified, 2026-10-06. Build2.124.0; not rerun.
Current [B4 connected resource signals](P10_NETWORK_SIGNALS_DESIGN.md) includes the inventory proof
in362 checks/two expanded suites on2.130.0/save70. Signal schema2, stateless inventory component1
and original stock/control codecs remain unchanged; numeric observation is shared with actual pools.
Counts below describe preceding source, not an additional current run.
Clean build;115 checks/two exact suites (69 contracts;46 physical), zero failures/aborts.
[Evidence](../../docs/migration/P10_INVENTORY_SIGNALS_VERIFICATION.md). No compatibility path.

## 0. Problem statement

`Standard/Operations.cs:127` owns typed signal reads and `AutomationSystem` owns deferred
control. `Inventory.cs:78` and ordinary external `IInventory` providers own stock and promises.
There is no native bridge from those actual inventories to JSON-authored control and execution
gates. Clients must not calculate stock readiness or duplicate capacity/reservation rules.

## 1. Goals

1. An optional explicitly selected module supplies stateless named signals over the actual singular
   inventory contract, without modifying Storage or requiring map/work/automation services.
2. An explicit native-resource bill defines whole-set stock, unreserved stock and incoming-capacity
   readiness. Inclusive thresholds and actual inbound/outbound promises are respected.
3. Existing typed signal schemas, private OperationsView, execution gates and next-step controls
   consume these facts without new validation/catalog/control/storage authorities.
4. Native and independent inventories retain their original stock, receipts and save codecs;
   interleaved saved runs remain identical. Invalid content fails before initialization.

## 2. Design

`InventorySignalsModule` (`sim.inventory-signals`,1.0.0) depends on the existing inventory and
operations modules and declares only `inventory_signal` component/schema1. No system, command,
service, view or additional persistent state. The master manifest explicitly lists this module and
its JSON documents; composition selects it through the existing scenario/module resolver.

`InventorySignalDef : ComponentDef<InventorySignal>, IAutomationSignalDefinition` reads `id`,
`inventory` (exact registered sibling component key) and nonempty `quantities` (native BillSpec).
Resolve uses the existing resource catalog. Validate requires the exact singular
`IInventoryDefinition`/`IInventory` owner, accepted resources, exact policy quantum and a bill that
fits that policy when empty. No first-provider choice, resource-unit conversion or empty default.

`InventorySignal : IAutomationSignalSource` derives these boolean metrics in `unit.boolean`:

- `inventory.stock-ready`: all actual stored quantities meet the authored bill, including stock
  committed to other jobs; it does not authorize consumption.
- `inventory.available-ready`: all actual unpromised quantities meet the bill.
- `inventory.incoming-ready`: the actual endpoint admits the whole bill in addition to its current
  stock and inbound promises, using its existing `CheckIncoming` contract.

Read only the selected metric. Validate actual live session, exact named source and inventory
binding/endpoint/company/policy. Existing Pure observation guard plus endpoint-version checks
reject touched mutation, identity/allocation changes and malformed quantities. Unsupported actual
compatibility/capacity returns typed unavailability, never zero/false; known capacity exhaustion
is an available false signal. No global malicious-provider rollback is claimed.

All existing consumers continue using `AutomationSignalDef.ReadFrom`, `SignalGateCondition` and
AutomationSystem order290 with next-tick operations. Native quantity thresholds are authored in
the bill, not reinterpreted as dimensionless numeric stock metrics. B3 now adds exact numeric resource
measurements and explicit quantities:null numeric-only sources through the same owner. Runtime
threshold editing and stock-target controls remain separate future work.

## 3. API surface

No HTTP or host API is added. Existing OperationsView.ReadSignal, gate views and ordinary
ConfigureAutomation/ConfigureOperation/EnqueueTransformation commands remain authoritative.

```json
{"inventory_signal":{"id":"feed-ready","inventory":"storage","quantities":{"steel":8}}}
```

Signals explicitly bind metric/kind/unit/provider component schema through existing `signals` JSON.

## 4. Files

New runtime `Standard/InventorySignals.cs` and `InventorySignalsModule.cs`; new proof JSON and
two-suite InventorySignalTests; register/compile the existing runner and embedded content.
Build identity advances to2.124.0; existing owner schemas/payloads remain unchanged. Earlier build
assertions migrate mechanically and compile only. Update master/parent/evidence/README/help/AGENTS.

## 5. Errors

- Missing/empty/invalid bill, unknown resource, wrong sibling, incompatible policy/quantum:
  ContentException before initialization.
- Foreign/missing signal binding: existing view/reader refusal, no discovery fallback.
- Runtime resource rejection or capacity representation failure: typed signal unavailable.
- Malformed numeric/status/identity or mutating provider: fault, no successful control fact.
- Below stock or exhausted known capacity: available false, not missing/invalid.

## 6. Compatibility

Development only. Exact engine-build checkpoint/peer identity advances; no old loader or shim.

## 7. Verification

Only these two complete exact suites will execute:

- `Inventory signals: authored readiness, contracts and private facts`
- `Inventory signals: physical stock controls and saved continuation`

Prove strict manifest/content, native/multi-resource/independent readiness, promises, inclusive
thresholds, no-map optionality, pure/private retained facts, contract faults, actual physical process
input/output and deferred inventory-driven controls, saved pending/paused/interleaved states.
Build: `dotnet build Beep.SimEngine.slnx --no-restore`. No third/family/full test run.

## 8. Risks

Available stock is not a promise; execution must recheck its existing resource transaction owner.
Incoming readiness predicts this whole bill, not all future simultaneous deliveries. Signal reads
observe their consumer's declared phase; no same-tick recursive control. Guard scope is explicit.

## 9. Out of scope

Utility allocation/pressure/power solvers, remote scoped-network signal binding, new transport,
numeric stock signals or threshold commands, richer control targets, full replay/fault/stress/acceptance,
Oil/Tycoon/hosts/demos/platforms/Draw/world limits. P1-P12 remains partial.

## 10. Execution order

1. Register the optional stateless signal and strict actual-inventory metadata.
2. Derive guarded facts through existing contracts and typed signal reader.
3. Author thin fixtures and lock/run only the two suites above.
4. Record fresh build/checks and update the single tracker and guidance; retain open gates.
