# RTS and base-defence layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [RTS and base-defence layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| unitDefinitionIds | unique ID array | Registered movement/combat/resource policies. | ["rifle_squad","engineer","transport"] |
| maxGroupSelection | integer | Bounded group intent; one command does not mean one native frame per unit. | 16 |
| productionIds | unique ID array | Activity definitions with reserved inputs and capacity. | ["train_engineer","assemble_transport"] |
| activeActorBudget | integer | A proposed measured tier, not shipping capacity. | 64 |

Semantic validation: **Group membership, input observation budgets and native activeActorBudget are separately validated; production definitions refer to real producers and recipe schemas.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record RtsDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> UnitDefinitionIds,
    long MaxGroupSelection,
    ImmutableArray<string> ProductionIds,
    long ActiveActorBudget);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| rts.group_order | bounded actor set, order and target | Validate members and publish explicit per-member outcomes. |
| rts.place_building | definition, location and reservation | Native legality request before final construction commit. |
| rts.queue_production | producer, recipe and inputs | Queued activity or capacity/resource refusal. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| UnitState | entityId, owner, group/order, health and action state | Stable ownership and deterministic order resolution. |
| BaseState | buildings, supply/capacity, production jobs | Building state and stock share coherent reservations. |
| KnowledgeState | controller contacts, visibility age and source | Fog views never expose global enemy state. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Group commands cannot bypass per-actor ownership or cost checks.
- A navigation target is not evidence that a body reached it.
- Construction updates collision/navigation through native facilities before usable readiness.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
