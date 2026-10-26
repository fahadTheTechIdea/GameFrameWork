# Factory and economic simulation layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Factory and economic simulation layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| resourceIds | unique ID array | Explicit quantity units and capacities. | ["ore","metal","components"] |
| recipeIds | unique ID array | Input/output/tool/time definitions. | ["smelt","assemble"] |
| networkPolicy | string | Registered connectivity and transfer rules. | "directed_ports" |
| maximumMachines | integer | Proposed workload budget to qualify, not an established engine limit. | 64 |

Semantic validation: **Port/recipe resource units agree; network cycles use a declared bounded transfer boundary. Every machine's output fits an explicit capacity policy.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record FactoryEconomyDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> ResourceIds,
    ImmutableArray<string> RecipeIds,
    string NetworkPolicy,
    long MaximumMachines);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| factory.configure | machine, recipe and expected network revision | Validated setup; retain in-flight reservation policy. |
| factory.transfer | source/target ports and quantity | Atomic bounded flow transaction. |
| factory.advance_production | logical boundary and eligible jobs | Stable completion order or explicit blockage. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| MachineState | entityId, recipe, progress, reservations and output capacity | Production does not lose overflow silently. |
| FlowNetwork | ports, directed edges, capacities and revision | Native layout view is derived; domain flow owns stock. |
| EconomicState | research, storage accounts and price/trade rules if selected | One resource owner and explicit market policy. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Total resource changes equal declared inputs/outputs/consumption.
- A blocked output never erases produced value.
- Cycles use explicit bounded transfer rules; no unbounded recursive propagation.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
