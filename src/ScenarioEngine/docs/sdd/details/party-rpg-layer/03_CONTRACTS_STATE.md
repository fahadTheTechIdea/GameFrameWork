# Party RPG layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Party RPG layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| characterIds | unique ID array | Definitions referenced by the persistent roster. | ["leader","medic","scout"] |
| partyCapacity | integer | Configurable selection bound, independent of total roster size. | 3 |
| abilityIds | unique ID array | Registered effects and target/cost rules. | ["strike","treat","guard"] |
| formationPolicy | string | Authored formation legality and range behavior. | "front_back" |

Semantic validation: **partyCapacity bounds selected members, not characterIds; formation and equipment eligibility are checked against individual definitions.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record PartyRpgDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> CharacterIds,
    long PartyCapacity,
    ImmutableArray<string> AbilityIds,
    string FormationPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| party.select | roster IDs and formation | Valid party or PARTY_SELECTION_DENIED. |
| party.equip | character, item and slot | Atomic inventory/equipment transfer. |
| party.use_ability | actor, targets, ability and revision | Validated cost/effect outcome; action rejection spends nothing. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| RosterState | characterId, level/progression, equipment, availability | One authoritative identity per character. |
| PartyState | selected character references, formation slots | No duplicate slot/character assignment. |
| EncounterState | turn/round, participant state, statuses, applied results | Context-scoped tactical state reconciles through explicit results. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- A unique item occupies one container or equipment slot.
- Unavailable/departed characters cannot be selected.
- Ability/status order is declared, bounded and independent of animation timing.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
