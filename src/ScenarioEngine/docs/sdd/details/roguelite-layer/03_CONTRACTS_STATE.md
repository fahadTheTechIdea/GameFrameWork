# Roguelite run and meta-progression layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Roguelite run and meta-progression layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| encounterIds | unique ID array | Registered encounter variants selected by a saved manifest. | ["ruins","crossroads","sanctum"] |
| runResourceIds | unique ID array | These quantities reset with the run. | ["run_gold","charges"] |
| profileUnlockIds | unique ID array | Only declared permanent unlocks cross the result boundary. | ["scout_path","healer_start"] |
| deathPolicy | string | Explicit result/reset rule. | "end_run" |

Semantic validation: **Run resources cannot appear in permanent transfer schemas unless explicitly allowed. Encounter selection and already offered rewards are saved rather than rerolled.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record RogueliteDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> EncounterIds,
    ImmutableArray<string> RunResourceIds,
    ImmutableArray<string> ProfileUnlockIds,
    string DeathPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| rogue.start_run | profile revision and chosen supported loadout | New run lineage with declared carryover. |
| rogue.choose_reward | offered reward ID and context revision | One selection/effect set. |
| rogue.finish_run | result and source revision | Atomic profile update or existing applied outcome. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| RunState | runId, seed/manifest, temporary inventory/build, current encounter | Fresh identity for every attempt. |
| ProfileState | permanent unlock ledger and applied run outcomes | No accidental temporary-stock carryover. |
| EncounterProgress | context references, selected rewards and terminal result | Selected reward is persistent, not rerolled by reopening UI. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Reloading cannot reroll an already selected encounter/reward.
- Run failure does not erase valid profile progress.
- A duplicate run result cannot unlock/grant twice.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
