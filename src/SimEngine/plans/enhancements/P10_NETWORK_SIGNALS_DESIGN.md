# P10-B4 — connected resource-network signals

Current follow-on: [B8 receiving-room backpressure](P10_RECEIVING_ROOM_DESIGN.md),2.140.0/save76/
inventory signal3/network signal2;447 checks/two exact expanded resource-signal suites. Evidence below
is preceding source. B8 extends this same diagnostic/quantity owner, not a graph/controller/clock.

Status: native focused slice verified,2026-10-06. Build2.130.0/save70; clean build;
362 checks/two exact suites (211 contracts;151 physical), zero failures/aborts.
[Evidence](../../docs/migration/P10_NETWORK_SIGNALS_VERIFICATION.md). Extends P10-B3,
not another graph, stock store or clock. Full P10/P1–P12 remains open.

## 0. Problem

Inventory signals observe local stock only. InfrastructureView.CopyPools depends directly on
LineService.NetComponent, and automation cannot observe the actual connected pool. Hosts must not
calculate pool availability or implement network control rules.

## 1. Acceptance

1. An engine read contract aliases the existing LineService. It reads actual declared INetworkPorts
   connections and the same ComponentOf selection used by flow. No graph rebuild occurs in a read.
2. Missing/foreign/private/unsupported connections and pending topology have typed blockers; they
   never provide fabricated zero quantities or disclose another company's pools.
3. JSON network_signal explicitly names its capability and network. Existing resource_signals declare
   resource, metric, output unit and provider schema. All links validate before the first Update.
4. Numeric stored/available/inbound/outbound facts use actual endpoint owners and the same exact
   quantity validation/conversion as inventory signals. Infrastructure views delegate to the read
   contract; detached snapshots never expose mutable pools.
5. Existing automation hysteresis, deferred commands and network gates control real timed flow.
   Save/load, pause and interleaved sessions retain the single game clock and existing codecs.

## 2. Design and API

Standard/NetworkDiagnostics.cs: INetworkDiagnostics.CopyPools and ReadQuantity return detached
facts with explicit viewer and connection identity. LineService implements the contract; no new
service owner. Standard/ResourceQuantitySignals.cs owns shared endpoint quantity observation and
exact unit conversion, replacing the numeric implementation inside InventorySignal.
Standard/NetworkSignals.cs declares stateless NetworkSignalDef/NetworkSignal. InfrastructureModule
registers network_signal/schema1 and aliases the existing service. Operations remains optional
unless the authored game selects signals/rules; no new update system is added.

ResourceQuantitySnapshot and NetworkPoolView have public validated, defensive constructors for
engine-side contract providers. ResourceSignalCodes now owns shared resource incompatibility and
conversion blockers; old InventorySignal aliases are removed, not retained as compatibility paths.
Inventory readiness keeps its own capacity/no-threshold blockers. Original owner payloads, inventory
signal schema2 and infrastructure view4 remain unchanged; new stateless network component/schema1.

Existing automation suspension semantics are unchanged: unavailable inputs cancel pending intent
and suspend with their typed blocker. Normal player reconfiguration after the source becomes valid
clears suspension. Automatic recovery policy is not invented in this bridge and remains open.

## 3. Files and execution order

1. Add this stage and master entry.
2. Extract shared resource quantity observation; implement pure connected diagnostics and view delegation.
3. Register explicit JSON signal source and extend the two resource signal suites with network proof.
4. Build, then run only the two complete suites listed below; review and record scoped evidence.

## 4. Errors and compatibility

Malformed definition/provider facts fault. Unsupported resource, disconnected connection, private
pool and pending topology withhold values using typed blockers. Old build/content is refused;
no compatibility reader, implicit connection, rounded quantity or substitute clock.

## 5. Verification

Exactly two complete directly affected suites:

- Resource signals: inventory and network contracts and private facts
- Resource signals: inventory and network controls and saved continuation

Includes preceding inventory coverage because the numeric reader is shared. Add native/public-only
port selection, real pool receipts, scaled units, dirty/disconnected topology, privacy, deferred
valve control, conserved physical transfers and paused/saved twins. No broad/full regression.
Final completed results:211 contracts in557ms and151 physical in500ms; build0 warnings/errors.
61 other earlier build assertions compile only. Repair runs used these same names only.

## 6. Remaining scope

Utility pressure/power solvers, remote generic graph providers, backpressure thresholds, editable
rules/reserve actions, explicit transient-input recovery policy, global rollback, replay/stress/
allocations and full P10/P1–P12 remain open.
No Oil/Tycoon/host/demo/platform/Draw work.
