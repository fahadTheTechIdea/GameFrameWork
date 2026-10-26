# P8-D1 — Explicit native output-port routing

## Problem and goals

Native Deliver and continuous TransformerFlow always produced into Entity.Get<IInventory>().
ResourceTransactions already supports multiple output endpoints, but native processes could not
author separate product and waste vessels. This stage removes that implicit destination.

Batch and continuous native processes now declare exactly one output port for every produced
resource. Selected named bindings provide actual stock ownership/capacity. Completion remains one
resource conversion; a full destination retains all WIP/progress/tools and yields no partial result.
No input/tool/catalyst routing is claimed by this output-only stage. Full P8 remains partial.

## Contracts and sole owners

Standard/ResourcePorts.cs defines IResourcePortDefinition : INamedCapabilityDefinition and
IResourcePort : INamedEntityCapability. ResolveStore(World) is a pure binding to an IResourceOutlet,
which also implements IResourceInlet. The component is a binding, not a second inventory.
InventoryPortDef/InventoryPort explicitly alias an entity's existing inventory. External engine
modules can bind their own actual endpoint books with these same contracts; schedulers need no edits.

TransformationDef.OutputPorts is an immutable resource-to-capability list resolved from required
output_ports JSON. Unknown, extra, missing, blank or incompatible bindings refuse content.
TransformerDef validates selected port definition metadata and native quantity packing. No default
local output and no compatibility parser. Native input, reusable tools and operating feed remain
under their existing local contracts pending the remaining P8 stages.

TransformationContracts resolves named stores, observes provider identity/purity, captures versions,
and groups resource yields by actual endpoint. Two aliases of one store produce one transaction
intent, not two overlapping receipt sets. It owns no balance, registry, queue, clock or codec.
ResourceTransactions remains the only reservation/consumption/production owner.

Deliver uses these grouped intents with the existing protected WIP/tool receipts and entity-result
creation protocol. Continuous flow derives cumulative/next yields with each selected destination's
capacity quantum, preflights the complete grouped output envelope and commits through that same
conversion. Output waits earn no progress and consume no feed. No copied remainder ledger.

## Current authored schema

```json
{
  "transformations": [{
    "id": "parts.assembly", "mode": "batch", "cancel_policy": "return_inputs",
    "inputs": {"parts.raw": 3}, "tools": {}, "operating_inputs": [],
    "outputs": {"parts.finished": 1},
    "output_ports": {"parts.finished": "product"},
    "duration": {"numerator": 6, "denominator": 1, "unit": "minute"}
  }],
  "entities": [{
    "id": "parts.assembler", "kind": "entity", "components": {
      "inventory_port": {"id": "product"}
    }
  }]
}
```

This routing fragment is not a complete game: actual storage, transformer settings, capacity,
resources, units, state, mode and master dependencies must also be authored. Entity-only recipes
explicitly author output_ports:{}; continuous output rate rows use this same mapping. The new proof
uses two pool_port bindings to existing internal solid/liquid vessels, not remote teleportation.
No new rail/conveyor/network medium or platform behavior is implied.

## API and durability

TransformationManagementView.ReadOutputPorts returns immutable private per-resource/port/endpoint/
version/actual-stock facts. Foreign-company reads return NotFound and an empty collection.
TransformationFlowSnapshot replaces the obsolete singular Output/OutputVersion with copied
OutputPorts facts. ITransformerFlow no longer declares the singleton OutputStore escape.

Engine2.305.0/save144/replay1; transformation-view schema10. Existing execution/books/queue codecs
are unchanged; explicit inventory binding component schema1 has no mutable payload. Old builds are
refused, not migrated. Existing fixture entity component order changes are ordinary new authored
content identity changes, never a save shim. One unchanged GameClock.

## Migration, proof and risks

The one-time output authoring script (now revised/renamed [AuthorTransformationPorts.ps1](../../docs/migration/AuthorTransformationPorts.ps1)
for input and then reusable-tool migrations) mechanically preserved existing JSON values and
adds explicit aliases:19 files,20 recipe route maps,18 native inventory bindings. It is not called
by any runtime loader. Native examples are migrated, not patched with missing-route fallback.
Older dynamic authoring/test cases are compiled, not re-proven by this new case.

Public-only engine proof PoolPort aliases the existing EndpointPools/ResourceInventoryState owner.
Its JSON owns yields, dimensions, limits, modes, roles and startup. Tests only issue ordinary player
queue/cancel/native-movement requests and read private engine views; no stock writes or timers.

Focused evidence: ONE distinct NEW bounded exact case, two runtime attempts; first strict fixture
capacity failure, corrected explicit dimensions, then80/0/0 in362ms. Four builds: clean13.33s,
new-test compile failure7.83s, repaired clean3.61s, fixture-repair clean4.15s. No old/full suite run.
[Verification](../../docs/migration/P8_OUTPUT_PORT_ROUTING_VERIFICATION.md).

Remaining: mixed input/utility/tool ports, catalyst wear/disposition, remote/entity tools, physical
automatic export through scoped ports, dynamic endpoint disappearance/recovery, malicious-provider
purity breadth, privacy beyond the owned-route proof, complete accounting and full P8 durability.
The output proof is not a substitute for any of these or full P1–P12 acceptance.
