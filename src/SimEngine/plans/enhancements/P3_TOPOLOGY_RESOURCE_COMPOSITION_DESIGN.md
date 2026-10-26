# P3-S35 — Explicit resource-only topology validation

## 0. Problem

NetworkTopologyTests used the generic Colony factory with a recovery-world document. That loaded an unused supplier requiring inactive Tycoon, then default navigation referencing the absent colony.tiles. Its split/merge/removal assertions never reached the updated engine geometry.

## 1. Goals

1. Replace that mismatched fixture composition, not patch it with optional business activation or legacy map aliases.
2. Preserve original authored resource units, costs, network capacities, rates and protocol assertions.
3. Verify split/merge, capacity, ownership, removal and saved multi-network execution through focused cases.

## 2. Design

An explicit six-module manifest selects access, inventory, motion, tile navigation, work and infrastructure. The world and navigation JSON agree on recovery.tiles. Core declarations contain only the original required steel/water units, capacity conversions, marker storage, machine role, line effects and game clock. Existing network-topology.json remains authoritative for segment workflow, ownership layers, rates and pool capacity policies.

The test factory directly loads that master and its named documents. The old Colony factory and supplier-filter branch are removed from this case. Original protocol setup deliberately stamps topology and seeds actual pool inventories to isolate graph transactions; physical player workflows are separately proven by P3-S34. No gameplay formulas or alternate engine owners are introduced.

The same assertion bodies now run against native and independent tile providers. Setup, ownership queries and the deliberate negative bypass use the existing ILineGeometry/ITileWritableSampleLayer contracts. The independent parser records canonical coordinate intent without preempting the shared engine's typed bounds checks. The large-capacity variant uses typed JSON authoring rather than a whitespace-dependent string replacement, with an explicit admitted-stock assertion.

## 3. API/state

No production API or state change. Engine2.585.0/save157/replay1 remains unchanged.

## 4. Files

NetworkTopologyTests.cs; explicit network-topology-master/world/navigation JSON and embedded registrations. Existing network-topology.json and engine business rules remain unchanged.

## 5. Errors

Malformed module/document/space references refuse rather than load another map or enable business implicitly. Existing graph capacity, ownership, invalid stock and unsettled/faulted-save refusal stay authoritative.

## 6. Compatibility

Old fixture composition is replaced, with no alternative legacy factory or duplicated network implementation.

## 7. Verification

Four exact native/independent topology cases pass124 checks (32 split/merge and30 saved ownership per provider). Affected authored corridor/path admission adds12 checks:136 distinct scoped successes. [All attempts and evidence](../../docs/migration/P3_TOPOLOGY_RESOURCE_COMPOSITION_VERIFICATION.md). No full/profile/family tests.

## 8. Risks

Wider physical stocked split/removal, arbitrary geometry/visibility/utility/failure-recovery matrices and global durability/stress/allocation remain open.

## 9. Exclusions

Tycoon/Oil/platform/host changes, changed authored amounts, full regression and Git publication.

## 10. Status

Explicit resource-only fixture and native/independent protocol verification complete. Physical stocked removal, mixed/provider/utility/fault and global gates remain open; full goal active, no original broad checkbox closes.
