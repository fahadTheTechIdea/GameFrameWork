# Survival and crafting layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Survival and crafting layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| needIds | unique ID array | Each need defines integer units, bounds, rates and threshold consequences. | ["hunger","fatigue","exposure"] |
| recipeIds | unique ID array | Registered inputs, tools, durations, outputs and interruption policy. | ["field_dressing","camp_shelter"] |
| startingFood | integer | Explicit authored stock; not an outage fallback. | 4 |
| restPolicy | string | Rest stops at danger, completion and threshold events. | "interrupt_at_event" |

Semantic validation: **Every need declares integer threshold/rate units, and recipes declare inputs, tools, duration, outputs and interruption effects. Resource sources and required routes must be attainable.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record SurvivalDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> NeedIds,
    ImmutableArray<string> RecipeIds,
    long StartingFood,
    string RestPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| survival.gather | actor and resource source | Single committed extraction or exhausted/blocked reason. |
| survival.craft | recipe, container and owner | Reserved job or missing-input refusal. |
| survival.rest | actor, shelter and requested interval | Bounded advancement interrupted by declared events. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| NeedState | actorId, need values, rates/status modifiers | Threshold changes commit at logical boundaries. |
| SurvivalInventory | containers, stock, tools and reservations | Resource kit owns quantities. |
| BuildCraftState | job instances, placement/reservation status, constructed object IDs | Objects survive world unload/revisit. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Sources do not regenerate on scene reload unless an explicit replenishment rule says so.
- Rest cannot skip an attack or lethal threshold.
- Failed placement cannot consume materials or create an invisible shelter.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
