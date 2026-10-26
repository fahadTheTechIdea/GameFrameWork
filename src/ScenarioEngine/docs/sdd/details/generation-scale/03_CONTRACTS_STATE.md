# Reproducible generation and measured scale profiles: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Reproducible generation and measured scale profiles SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| generatorId | string | Trusted registered algorithm identity. | "authored_variants" |
| generatorVersion | string | Exact algorithm version; changing it changes compatibility. | "v1" |
| maxAttempts | integer | A bounded authored policy; no hidden reroll-until-pass loop. | 1 |
| activeActorTier | integer | Proposed test tier, not an established supported ceiling. | 64 |

Semantic validation: **Generator/configuration identity and seed encoding are pinned. activeActorTier is a proposed measurement workload and is never a compatibility certificate.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record GenerationDefinition(
    string Id,
    string DefinitionVersion,
    string GeneratorId,
    string GeneratorVersion,
    long MaxAttempts,
    long ActiveActorTier);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| Generate | definition identity, stable seed inputs | Validated manifest or GENERATION_INVALID. |
| LoadGenerated | saved manifest and exact generator/content | Pinned results or GENERATOR_MISMATCH. |
| MeasureProfile | registered representative workload | Bounded diagnostic trace; no simulation policy changes. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| GenerationManifest | algorithm/config digest, seed encoding, selected results | Existing worlds pin their selected generation result. |
| ValidationResult | required routes/resources/objectives, rejection reasons | Separate structural validation from physical reachability. |
| ScaleTrace | profile identity, workload, environment, p95/p99 costs | Observed measurements, never invented capacities. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- String.GetHashCode and dictionary order do not define seeds.
- An upgrade cannot silently regenerate an existing world differently.
- Actor catalog size and simultaneously active native actors are separate budgets.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.

## Detailed engine integration decisions

The following design detail is relocated from the earlier overview and is maintained here as this capability's owner. The configuration DTO above is the setup definition; supplemental runtime/profile records below serve distinct roles. All remain proposed until implemented with the consumer.

Generation is a registered trusted variant. Derive sub-seeds using a documented stable encoding of world seed, generator version and region/encounter key. Save selected results or their immutable manifest; a future algorithm revision cannot silently regenerate an existing world differently. Check required resources/routes/objectives. A failed generator returns GENERATION_INVALID with bounded diagnostics, not arbitrary repeated rerolls until something appears valid.
