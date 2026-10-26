# Godot native runtime host: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Godot native runtime host SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| dimension | string | Select matching native adapter variants without changing Core. | "2d" |
| sceneRoles | unique ID array | Approved PackedScene roles; data never supplies executable paths. | ["world.village","world.forest"] |
| ruleStepMilliseconds | integer | Fixed-mode host configuration must match the time profile. | 50 |
| readinessPolicy | string | Loaded resource alone is not a ready native world. | "bindings_and_navigation" |

Semantic validation: **Dimension-specific roles and adapter variants match the selected native runtime. ruleStepMilliseconds applies only to compatible fixed-time profiles.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record ScenarioGameHostDefinition(
    string Id,
    string DefinitionVersion,
    string Dimension,
    ImmutableArray<string> SceneRoles,
    long RuleStepMilliseconds,
    string ReadinessPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| Launch | validated scenario/profile and native roles | Ready world or explicit repair outcome. |
| CaptureWorld | complete native sensor boundary | Immutable WorldFrame through current contracts. |
| ApplyCommittedRequests | request batch | Native work with correlated results; never apply uncommitted events. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| HostBinding | session/world token, entityId, native body binding, revision | Native instance IDs are transient. |
| ObservationBuffer | ordered sequence, complete native frame, admitted rule boundary | Never publish partially written sensor state. |
| HostOperation | request/attempt, cancellation, resource/native readiness | One owner per load/spawn operation. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Godot owns physics, navigation, animation, InputMap, native timers, resources and Control UI.
- _PhysicsProcess runs for active continuous movement; unchanged menus use signals.
- Headless synthetic movement cannot count as native route evidence.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
