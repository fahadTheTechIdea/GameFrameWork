# Capability profiles and layer composition: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Capability profiles and layer composition SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| layerId | string | Exact IGameLayer identity; layer version is pinned by the containing profile. | "adventure" |
| requiredCapabilities | unique ID array | All requirements must resolve before the scenario becomes selectable. | ["world.native2d","queries.actions"] |
| moduleIds | unique ID array | Explicit trusted registrations; public data cannot introduce executable types. | ["inventory","interactions"] |
| writerPolicy | string | Each state section and operation key has one owner. | "single_owner" |

Semantic validation: **Resolve exact versions in the containing profile and reject duplicate state/operation writers and dependency cycles. Never accept runtime code paths in content.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record GameProfileDefinition(
    string Id,
    string DefinitionVersion,
    string LayerId,
    ImmutableArray<string> RequiredCapabilities,
    ImmutableArray<string> ModuleIds,
    string WriterPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| ResolveProfile | profile identity, host inventory | Ready or CAPABILITY_UNAVAILABLE; no catalog mutation on failure. |
| ValidateComposition | bindings and namespaces | One stable dependency order or MODULE_CONFLICT. |
| InspectProfile | trusted selection identity | Read-only readiness and missing capability list. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| ProfileResolution | profileId, profileVersion, contentDigest, layerIdentity | Immutable accepted profile; no automatic provider substitution. |
| ModuleBinding | moduleId, exactVersion, dependencyIds, ownedStateSections | One binding per ID; cycles and duplicate writers are invalid. |
| HostMatch | hostCapabilityId, providedVersion, requiredVersion | Only documented compatible combinations are eligible. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Profile resolution never loads a DLL named by JSON.
- A module failure rolls back the entire candidate transaction.
- Counter/Signal layers remain usable without implementing every optional kit.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
