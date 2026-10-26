# Persistent worlds, regions and stable identities: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Persistent worlds, regions and stable identities SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| worldId | string | Stable world lineage; new worlds get separate identities. | "frontier" |
| regionIds | unique ID array | Declared region definitions referenced by exits and entity membership. | ["village","forest","headquarters"] |
| inactivePolicy | string | Authored gameplay behavior while a region is inactive. | "event_based" |
| maxActiveRegions | integer | Native binding budget, independent of the saved region count. | 2 |

Semantic validation: **Every region has a declared inactive policy and one membership owner per entity. Region transfers validate both source and target revisions.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record PersistentWorldDefinition(
    string Id,
    string DefinitionVersion,
    string WorldId,
    ImmutableArray<string> RegionIds,
    string InactivePolicy,
    long MaxActiveRegions);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| ActivateRegion | regionId, expectedRevision | A correlated native request; no regenerated default state. |
| TransferEntity | entity, source/target revisions | Atomic membership move or REGION_CONFLICT. |
| CheckpointWorld | committed manifest references | A consistent set of region revisions or explicit failure. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| WorldState | worldId, seed, allocatorNextId, contentDigest | World identity and entity allocation persist. |
| RegionState | regionId, revision, entity membership, deltas, tombstones, lastSimulatedTime | Removing nodes does not remove logical state. |
| WorldEntity | stableEntityId, owningRegion, localPositionMm | Movement between regions keeps the same identity. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Looted/destroyed objects do not respawn merely because a scene reloads.
- Only one region owns an entity at a time.
- Frozen and abstract simulation are explicit gameplay choices, never silent failure substitutes.
- NodePath and instance IDs are never durable entity identities.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.

## Detailed engine integration decisions

The following design detail is relocated from the earlier overview and is maintained here as this capability's owner. The configuration DTO above is the setup definition; supplemental runtime/profile records below serve distinct roles. All remain proposed until implemented with the consumer.

Open-world support requires durable world state as well as native resource loading.

| Proposed data | Fields/invariants |
| --- | --- |
| WorldIdentity | Stable world ID, content/profile digest, generation seed and generator version |
| WorldEntityKey | World ID + stable EntityId; moving regions preserves identity |
| RegionState | Region ID, revision, entity membership, persistent object changes and last simulated game time |
| RegionPosition | Region ID + local PositionMm; one host conversion and scale; no persistent NodePath/GetInstanceId |
| Entity allocation | One persisted allocator/namespace policy; no ID reuse after unload or death |
| World checkpoint manifest | Referenced region revisions/digests and world-level state revision |
| Generation manifest | Generator ID/version/configuration digest, seed inputs, selected region/encounter results and validation outcome |

Track destroyed/looted/constructed objects explicitly, including tombstones, so revisiting does not regenerate consumed items. Move an entity between region records atomically. Unloading native nodes changes the binding set, not the saved membership, inventory or quest identity.

Each region type declares one inactive policy: Frozen (gameplay intentionally suspended), Event-based (process declared due state changes), or Abstract (domain-owned reduced simulation). This is a declared gameplay rule visible in content, never an outage fallback. Frozen crops do not grow; event-based crops do, if their rules say so. Switching between abstract and active simulation reconciles one state owner; it must not duplicate production or erase an injury.

Godot owns resource loading, scene instantiation, collision, navigation maps/regions and spatial queries. Persistent-world code owns logical identity, state deltas and activation policy. Use native facilities before custom loaders/pathfinders. Coordinate precision, streaming and persistence are separate decisions; qualify a native coordinate strategy for the selected dimensionality and measured world size.
