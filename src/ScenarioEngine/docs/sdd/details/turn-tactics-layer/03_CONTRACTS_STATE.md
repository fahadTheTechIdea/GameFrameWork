# Turn-based tactics layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Turn-based tactics layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| turnPolicy | string | Registered ordering/tie rules. | "alternating_factions" |
| actionPoints | integer | Authored per-turn budget; ability costs can differ. | 2 |
| gridRole | string | Approved native grid/geometry adapter. | "tactical_grid" |
| missionIds | unique ID array | Validated bounded objective/ending definitions. | ["rescue","withdrawal"] |

Semantic validation: **The selected turn policy determines authorized owner/tie ordering. gridRole resolves through the host capability profile, not an arbitrary scene path.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record TurnTacticsDefinition(
    string Id,
    string DefinitionVersion,
    string TurnPolicy,
    long ActionPoints,
    string GridRole,
    ImmutableArray<string> MissionIds);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| tactics.act | actor/action/target and turn revision | Legal effect/cost or TURN_ACTION_DENIED. |
| tactics.end_turn | controller and turn revision | One phase transition; duplicate requests cannot skip a turn. |
| tactics.preview | selection and known revision | Pure movement/target/cost explanation. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| TurnState | round, phase, active faction/actor, remaining budgets | Only the authorized turn owner may spend actions. |
| TacticalState | positions, cover/evidence, ability/status state | Native observations and domain policy remain distinct. |
| MissionState | encounter objective instances, extraction and result | Encounter closure returns a scoped result. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Waiting for player input consumes no game time or RNG.
- An invalid action spends no action points.
- Animation completion cannot grant a second action or decide damage.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
