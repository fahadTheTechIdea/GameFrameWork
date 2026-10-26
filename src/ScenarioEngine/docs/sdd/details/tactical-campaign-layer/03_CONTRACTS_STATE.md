# Tactical campaign layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Tactical campaign layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| operationIds | unique ID array | Validated next-operation identities. | ["crossing","rescue","supply_raid"] |
| strategicActivityIds | unique ID array | Campaign time jobs owned by activities. | ["research_radio","recover_wounded"] |
| rosterCapacity | integer | Logical personnel capacity, not active native body count. | 24 |
| failurePolicy | string | Declare which losses permit continuation. | "authored_fail_forward" |

Semantic validation: **All operation destinations and strategic activity references resolve before selection; failurePolicy determines the declared terminal/continuation paths.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record TacticalCampaignDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> OperationIds,
    ImmutableArray<string> StrategicActivityIds,
    long RosterCapacity,
    string FailurePolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| campaign.select_operation | operation and current roster/loadout | Validated deployment context. |
| campaign.assign_work | personnel, activity and resources | Reserved strategic activity. |
| campaign.apply_operation_result | outcome and source revision | Atomic continuation or existing result. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| CampaignState | lineage/revision, strategic stock, unlocked operations | Progress owner commits outcome deltas. |
| PersonnelState | stable roster IDs, injuries, equipment and availability | Mission participants reference one campaign source. |
| StrategicWork | research/healing assignments, due time and reservations | No independent untracked resource writes. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Replaying a mission result cannot heal or reward twice.
- Equipment deployed in a mission is not independently duplicated in headquarters stock.
- Fail-forward branches are authored, not invented when a mission fails.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
