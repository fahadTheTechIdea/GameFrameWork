# Resources, inventory and reservations: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Resources, inventory and reservations SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| resourceIds | unique ID array | Registered quantities with explicit unit/maximum definitions. | ["food","medicine","parts"] |
| maxContainers | integer | Bound container identity space for the profile. | 32 |
| reservationPolicy | string | Reserved stock is distinct from free/consumed stock. | "explicit" |
| overflowPolicy | string | Never clamp away value or silently discard produced items. | "refuse" |

Semantic validation: **Resource definitions declare units, maxima and permitted container/item relations; current examples identify categories, not inventory fixture stock.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record ResourceDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> ResourceIds,
    long MaxContainers,
    string ReservationPolicy,
    string OverflowPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| Reserve | owner/action and required inputs | Reservation or INSUFFICIENT_RESOURCE. |
| Transfer | source/target, item/quantity and revisions | Atomic debit/credit or CAPACITY_REFUSED. |
| CommitOrRelease | reservation and terminal action identity | One consumption/refund outcome. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| ResourceAccount | containerId, resourceId, quantity, capacity | Integer units; nonnegative quantity and bounded capacity. |
| ItemInstance | itemId, definitionId, containerId, durability | Unique item identity and single ownership. |
| ResourceReservation | reservationId, actionId, owner, quantities, state | Free stock equals stored minus active reservations. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- A transfer cannot debit one container while failing to credit the other.
- A preview never creates a reservation.
- No other job may release this action's inputs.
- All arithmetic is checked and uses declared integer units.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
