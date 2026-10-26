# P10-B4 — connected resource-network signals verification

2026-10-06. Framework-only focused evidence, not full P10/P1–P12 completion.
The lifecycle skill guided staged implementation, contract boundaries, focused verification and
tracker alignment. No Oil/Tycoon/host/demo/platform/Draw changes or full regression.

## Source and ownership

- Build beep-simengine-2.130.0; save70, LineService4, infrastructure view4, inventory signal schema2.
  New network_signal stateless component/schema1. All existing authoritative owner payloads unchanged.
- LineService implements INetworkDiagnostics; the composition aliases the same existing owner.
  CopyPools uses actual graph membership, existing capacity/stock/promises and company visibility.
  ReadQuantity requires a live viewer-owned entity and its actual declared INetworkPorts connection.
  It selects the same ComponentOf pool used by flow, never a host-calculated graph or inventory.
- Dirty topology is network.topology-pending; an unconnected declared endpoint is network.disconnected.
  Unowned private-neutral pools withhold values; public neutral pools are available through each
  company's own connection. Unsupported resources return resource.signal-incompatible.
  Unknown graph/resource or undeclared connection refuses explicitly. Pure reads never rebuild graphs.
- NetworkSignalDef requires explicit JSON capability/network and validates the authored connection
  before play. Existing resource_signals select metrics network.stored/available/inbound/outbound,
  resource, exact unit and provider schema. No live pool ID appears in content.
- ResourceQuantitySignals replaces the previous inventory-specific numeric reader. It captures actual
  IResourceOutlet Amount/Available/ReservedIn/ReservedOut, validates nonnegative native units/quanta,
  coherent promises, stable identity/company/policy/version, then performs exact UnitConversion.
  ResourceSignalCodes owns shared incompatibility and conversion-unavailable codes; old inventory
  aliases were removed. Inventory readiness keeps its own no-threshold/capacity blockers.
- ResourceQuantitySnapshot and NetworkPoolView have validated public construction for engine-side
  providers. Pool collections are defensively copied/read-only. No mutable endpoint escapes a view.
- Original AutomationSystem/order290, normal operation commands, network gates and LineSystem/order420
  remain sole control/flow owners. No new update loop, current time, timer, controller, inventory,
  receipt book, graph, history, journal or saved signal codec exists.
- Unavailable inputs retain existing suspended-rule semantics and cancel pending intent. Normal
  reconfiguration after topology settles resumes the rule. Automatic transient-source recovery is an
  explicit remaining policy requirement, not silently introduced fallback behavior.

## Final completed gate

```text
dotnet build Beep.SimEngine.slnx --no-restore
Build succeeded: 0 warnings, 0 errors; MSBuild 3.22 seconds.

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Resource signals: inventory and network contracts and private facts"
211 passed, 0 failed; 557 ms; one complete suite.

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Resource signals: inventory and network controls and saved continuation"
151 passed, 0 failed; 500 ms; one complete suite.
```

**362 checks, two complete suites, zero final failures/aborts.** Includes the original inventory
coverage plus65 connected-network contract checks and21 physical checks.61 other build assertion
files migrated and compiled only. Solver/topology/arbitration/automation families were not run.
Repair runs used these same two names only. They caught pending-event checkpoint misuse, the strict
above-threshold boundary and the existing permanent suspension semantics; final tests exercise normal
completion/reconfiguration rather than changing production rules to accommodate a test.

Verified: actual native/public-only port connections; typed authoring refusals; scaled output; zero
promises versus unavailable quantities; private/public/private-neutral ownership; actual inbound/outbound
receipts, release and saved-session isolation; immutable facts; pure/foreign/mutating callbacks;
pending/changed/disconnected topology; actual fill/stop/withdraw/restart with conserved30 units,
same-step feedback prevention, valve gates, paused clock and saved pending/continuation state.

## Source SHA-256 at final gate

| File | SHA-256 |
| --- | --- |
| Standard/NetworkDiagnostics.cs | 2BEAF804DB78B5229F96073FB04420B1A9CEA264EDA1E958AA7904D5472EE8A9 |
| Standard/NetworkSignals.cs | 34F2ACE44EC02FB7DAAEB8365CE66712E6F541AC63F8E22835B3B6708DB520B2 |
| Standard/ResourceQuantitySignals.cs | 2FDEE7267380915EB2D7691AC59B01B28DDDA0F79C7CBA442674DB0D54B27EA4 |
| Standard/InventorySignals.cs | 7D276A6520FCAC0D5D4F55F04C9F3174113434C5548163D6B6ECA05F269BD028 |
| Views/InfrastructureView.cs | 3EE554B04FB4EBFF99DFBA9CB4E1C1109F7BCE105957F9AAAE113D5585DA89D3 |
| Standard/InfrastructureModule.cs | 1548B0AB0B926D9362125C24157924735E080D402B846B82A1250661A12FDD0F |
| Tests/NetworkSignalTests.cs | E317155B7270E9BFE2386529BCC2FF55A2BD6AC00548409060A62943258C32E4 |
| Tests/InventorySignalTests.cs | 647C2924B93D222CE3D419FA67E861D1D3B7EBDFB80647BC6E0AEAE8475ED892 |
| Proof/content/network-signals.json | 9E35415361BBF18C3C52898E47D89904E4E4568F75E7D8AB1261D898AD3471BA |

## Remaining proof and scope

This proves native connected resource pools, not arbitrary graph-provider construction, pressure/
power/thermal physics, backpressure thresholds, editable rules/reserve targets, automatic source
recovery policy, whole-session provider rollback, replay/lockstep/stress/fuzzing/allocation budgets
or the complete framework roadmap. No claim is made from earlier historical suite counts.
