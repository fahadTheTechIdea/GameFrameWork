# Recruitment and headquarters campaign RPG: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Recruitment and headquarters campaign RPG SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| characterIds | unique ID array | Original character definitions with combat/service/army role capabilities. | ["leader","quartermaster","healer","captain"] |
| headquartersId | string | Persistent headquarters definition and approved native scene role. | "river_keep" |
| contextKinds | unique ID array | Each kind selects its own registered encounter rules and time profile. | ["exploration","party_battle","duel","army_battle"] |
| partyCapacity | integer | Configurable active party size; roster size is independent. | 4 |

Semantic validation: **Recruitment, chapter, service and encounter-return references resolve. Support roles do not require combat abilities; partyCapacity limits selection, not the whole roster.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record CampaignRpgDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> CharacterIds,
    string HeadquartersId,
    ImmutableArray<string> ContextKinds,
    long PartyCapacity);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| crpg.recruit | character/interaction evidence and revision | One roster member and declared service/role unlocks. |
| crpg.assign_party | roster references and formation | Eligible selection without copying character state. |
| crpg.enter_encounter | party/duel/army definition and participants | Acknowledged context transition with saved parent. |
| crpg.apply_encounter_result | result/source revision | Atomic rewards/losses/story changes, then resume parent. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| CampaignRoster | stable character identity, recruitment/availability, equipment, progression | Party/army/headquarters views share this owner. |
| HeadquartersState | services, assignments, growth milestones and region revision | Recruitment unlocks services once. |
| CampaignPlayState | chapter, active context/checkpoint, return context, applied result ledger | Battle completion does not close the entire campaign. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- A support character need not implement an attack action.
- Recruitment status is distinct from combat health and temporary availability.
- Duplicate battle results never repeat rewards, deaths or service unlocks.
- Entering a battle does not call GameBase.Init again or reset the campaign.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
