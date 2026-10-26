# Colony and emergent-story layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Colony and emergent-story layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| inhabitantIds | unique ID array | Stable definitions/roster sources. | ["settler_a","settler_b","settler_c"] |
| jobIds | unique ID array | Definitions declare prerequisites, reservations and interruption. | ["haul","treat","repair"] |
| incidentProfile | string | Registered policy with visible causes and bounded event budgets. | "measured_pressure" |
| activePopulation | integer | Proposed qualification tier; inactive state uses declared world policy. | 32 |

Semantic validation: **Each job declares eligibility and exclusive reservation rules. The incident profile has bounded weights/cooldowns and cannot read hidden player knowledge as evidence.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record ColonyDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> InhabitantIds,
    ImmutableArray<string> JobIds,
    string IncidentProfile,
    long ActivePopulation);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| colony.assign_job | inhabitant/activity/target | Legal reservation or eligibility reason. |
| colony.set_priority | work category and bounded priority | Future assignment policy change, not cancellation of committed costs. |
| colony.resolve_incident | definition and due boundary | Validated event instance; no omniscient hidden-state shortcut. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| InhabitantState | needs, skills, injuries, relationships and current activity | One owner for personal state. |
| WorkState | job instances, assignee, target and reservations | No two jobs claim the same exclusive resource. |
| IncidentState | pressure inputs, cooldowns, selected event and seed stream | Saved decision evidence supports explanations/replay. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Incident policies cannot mutate state outside the candidate transaction.
- Assignment never duplicates an exclusive reservation.
- Offscreen abstract simulation reconciles with the same saved inhabitants.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
