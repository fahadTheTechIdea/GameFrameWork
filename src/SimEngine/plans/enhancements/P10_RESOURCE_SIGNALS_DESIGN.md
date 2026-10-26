# P10-B3 — unit-aware numeric resource signals

Current follow-on: [B8 receiving-room backpressure](P10_RECEIVING_ROOM_DESIGN.md),2.140.0/save76/
inventory signal3/network signal2;447 checks/two exact expanded suites. Shared quantity owner now
requires explicit CapacityOffer facts; no old constructor/schema loader. Evidence below is historical.

Status: preceding native focused slice verified,2026-10-06. Its suites are extended/included in
current [B4 connected network signals](P10_NETWORK_SIGNALS_DESIGN.md), build2.130.0/362 checks.
Counts below are the original B3 evidence. Build2.129.0; clean build;276 checks/two exact
suites (146 contracts;130 physical controls), zero failures/aborts.
[Evidence](../../docs/migration/P10_RESOURCE_SIGNALS_VERIFICATION.md). Full P10/P1-P12 stays open.

## 0. Problem

The preceding InventorySignal published only three whole-bill booleans. AutomationSignalDef and the
preceding provider schema fixed every metric's unit at component registration, preventing native
quantities in game-authored resource units. A generic engine must not force all resources into
unit.count, add per-resource stock mirrors, or make hosts calculate thresholds.

## 1. Acceptance

1. JSON resource_signals declare stable identity, numeric metric, resource, exact output unit and
   provider/schema through the existing signal catalogue. Unknown resources/units, incompatible
   dimensions, old schemas and missing quantity parameters refuse before initialization.
2. Extend AutomationMetricSchema with explicitly authored quantity-unit bindings and a public
   IAutomationQuantitySignalDefinition contract. Fixed metrics retain exact fixed units; quantity
   metrics require actual catalog-owned unit metadata. No unit wildcard/fabricated conversion.
3. Existing InventorySignal exposes stored, available, inbound-promised and outbound-promised native
   quantities through existing IInventory/port contracts and exact UnitConversion. Explicit
   quantities:null supports numeric-only sources without fabricated readiness thresholds; missing or
   empty bills refuse and disabled readiness reports typed unavailable. Original boolean
   readiness remains the same responsibility. No new component, balance, promise book or reader.
4. Native and unrelated public-only inventories work without concrete provider lookups. Reads are
   pure, private and detached. Unsupported compatibility/conversion returns typed unavailable,
   malformed or mutating provider facts fault. Zero stock is an available exact zero, not missing.
5. Existing numeric control rules/hysteresis/dwell/cooldown and deferred normal operation commands
   act on real physical process stock/WIP; pause/save/load/interleaved continuation use one clock
   and existing codecs. No same-step feedback or new control scheduler.

## 2. Design and API

- Core/OperationSchemas.cs: IAutomationQuantitySignalDefinition.QuantityUnit and explicit
  AutomationMetricSchema.Quantity(Code). Kind=Number, UsesAuthoredUnit marks a mandatory resolved
  signal unit; fixed constructors still reject empty units. Provider schemas capture/revalidate the
  two explicit binding forms and preserve stable metadata/Sync ownership.
- Standard/Operations.cs: open AutomationSignalDef, shared declared binding and returned numeric
  quantity validation. ResourceAutomationSignalDef implements IResourceAutomationSignalDefinition,
  resolves Resource/QuantityUnit using existing ResourceDef/UnitDef catalog and exact dimensions.
  It calls the same signal reader/schema checks, never another catalogue or fallback path.
- OperationsModule registers resource_signals alongside signals. InventorySignal's existing schema
  becomes2 and declares quantity metrics inventory.stored/available/inbound/outbound. Its same pure
  read validates actual stock, unpromised and promise quantities against native units/quanta and
  identities, then converts only the requested resource value. Known conversion limits withhold
  values and preserve typed blockers. Existing whole-bill booleans remain schema2 bindings.
- Build2.129.0; save70/InventorySignal stateless component1 and all owner payloads remain unchanged.
  Signal schema2 changes authored bindings and content fingerprints; old build/content refuses.

```json
{"resource_signals":[{"id":"feed.available","kind":"number","metric":"inventory.available",
  "resource":"steel","unit":"unit.steel","providers":[{"component":"inventory_signal","schema":2}]}]}
```

The source still explicitly binds inventory_signal id/inventory/quantities (a nonempty bill or explicit
null to disable readiness); no inferred initial
stock, numeric thresholds, output units or default controller. Resources and quantities stay with
the original inventory. IResourceAutomationSignalDefinition is metadata only, never an endpoint.

## 3. Files and sequence

1. Create stage/master entry; inspect schema, stock, unit, control and time owners.
2. Extend existing metric/signal contracts and author resource definitions through OperationsModule.
3. Add numeric observations to the sole InventorySignal; migrate its existing JSON schema references.
4. Extend the existing directly affected InventorySignalTests and ordinary public proof fault probes.
5. Migrate earlier build assertions, compile, then run only the two exact complete suites below.
6. Review source/whitespace; update master/parent/AGENTS/README/help and fresh source-scoped evidence.

## 4. Errors and durability

Missing parameters/schema/catalog unit, wrong dimension/kind/provider: ContentException before
Update. Unsupported resource: existing typed incompatibility. Exact conversion loss/fractional
output: typed unavailable; output range overflow: signal.numeric-range. Negative/non-native/incoherent
stock/promise facts or mutation: InvalidOperationException, fault on actual automation Update.
No zero/default/rounded substitute, compensating grant, old reader or signal-owned persistent state.

## 5. Verification

Only these two renamed, complete directly affected suites execute; repairs rerun these names only:

- Inventory signals: authored quantities, contracts and private facts
- Inventory signals: numeric controls and saved continuation

Prove fixed boolean and quantity bindings, count/fluid/scaled/discrete/unrepresentable/range cases,
native and unrelated inventories, actual receipt reservations/releases, no-map optionality, strict
content/contract faults, private immutable facts, interleaved sessions, physical process spending,
numeric hysteresis/deferred control and stopped/pending/paused/returned-WIP saved continuation.
Completed clean build precedes tests. Earlier suites compile only; no third/family/full test run.

## 6. Remaining scope

This completes native numeric resource observations, not remote network/utility signals, rate/pressure/
power physics, runtime threshold/profile editing, richer reserve/dispatch actions, global provider
rollback, broad replay/lockstep/stress/allocations or full P10/P1-P12 acceptance. No Oil/Tycoon/host/
demo/platform/Draw or game-size limit changes. Lifecycle guidance preserves existing single owners.
