# Narrative and interaction runtime: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Narrative and interaction runtime SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| storyId | string | Registered compiled story/content identity. | "river_contact" |
| checkpointPolicy | string | The selected adapter must prove exact restoration at supported boundaries. | "choice_boundary" |
| allowedEffectKinds | unique ID array | Game-changing external calls become typed candidate intents. | ["grant_item","set_relationship"] |
| maxChoices | integer | Bound offered options and UI work. | 8 |

Semantic validation: **The adapter must support the declared checkpoint boundary. Effect arguments use registered schemas and choice IDs must belong to the currently offered revision.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record NarrativeDefinition(
    string Id,
    string DefinitionVersion,
    string StoryId,
    string CheckpointPolicy,
    ImmutableArray<string> AllowedEffectKinds,
    long MaxChoices);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| BeginInteraction | actor, target and definition | Offered choices or explicit unavailable reason. |
| Choose | instanceId, offeredChoiceId and revision | Candidate text/effects or CHOICE_STALE. |
| CancelInteraction | instance and interruption cause | Declared checkpoint/cancellation result. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| NarrativeCheckpoint | runtime identity/version, contentDigest, cursor, variables, offeredChoiceIds | Complete resumable state, not variables alone. |
| InteractionInstance | interactionId, actor/target, status, checkpointRef | One authoritative interaction owner. |
| NarrativeEffectIntent | instance/choice identity, effect kind, arguments | Applied in the same candidate transaction as dialogue progress. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Uncommitted interpreter callbacks perform no file/network/native side effects.
- A choice may be selected only if offered at the cited revision.
- Float/opaque interpreter data has an explicit compatible encoding; no silent conversion into integer state.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.

## Detailed engine integration decisions

The following design detail is relocated from the earlier overview and is maintained here as this capability's owner. The configuration DTO above is the setup definition; supplemental runtime/profile records below serve distinct roles. All remain proposed until implemented with the consumer.

Candidate narrative adapter: Ink, whose documented C# runtime supports choices and full runtime state serialization. Evaluate it behind INarrativeRuntime only when implementing the first conversation. Yarn is an alternative requiring its own compatibility/checkpoint proof. Install neither in this research pass.

Narrative runtimes must evaluate a candidate clone restored from their checkpoint. Game-changing callbacks produce typed effect intents; they cannot directly spend supplies, mutate a scene, save files or notify UI. Commit narrative state and effects together. On failure discard both. For third-party serialized states, declare an exact checkpoint encoding and digest; floats or opaque interpreter data must not be silently inserted into the current integer-only rule hash format. Verify byte-stable normalization/restore with the selected runtime. Variables alone are insufficient when the active line/choice position matters.
