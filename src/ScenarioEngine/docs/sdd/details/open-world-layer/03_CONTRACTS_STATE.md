# Open-world sandbox layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Open-world sandbox layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| worldDefinition | string | Registered world/content/generator identity. | "borderlands" |
| regionIds | unique ID array | Persisted regions with stable graph/coordinate keys. | ["village","forest","pass"] |
| streamRadiusRegions | integer | Proposed residency policy; qualify actual native budgets. | 1 |
| completionPolicy | string | Side activities do not automatically end the world. | "ongoing_world" |

Semantic validation: **A bounded expedition declares final closure; ongoing_world activity completion leaves the world active. streamRadiusRegions is residency policy, not proof of physical reachability.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record OpenWorldDefinition(
    string Id,
    string DefinitionVersion,
    string WorldDefinition,
    ImmutableArray<string> RegionIds,
    long StreamRadiusRegions,
    string CompletionPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| open.travel | declared region/exit and expected boundary | Correlated activation/transfer. |
| open.interact | visible persistent entity and action | Domain transaction against the current region revision. |
| open.complete_activity | instance/result identity | Scoped result; ongoing world remains playable. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| WorldProgress | discovered regions, global decisions, activity summaries | Separate local quest completion from root closure. |
| RegionLedger | persistent-world references/revisions and inactive policy | World owner holds authoritative region data. |
| ResidencyState | desired/ready bindings and activation attempts | Transient native bindings are reconstructed from saved intent. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Unloading is not deletion of logical entities or state.
- A finished side mission does not set the ongoing world to Ended.
- Region-local changes do not invalidate every world order.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
