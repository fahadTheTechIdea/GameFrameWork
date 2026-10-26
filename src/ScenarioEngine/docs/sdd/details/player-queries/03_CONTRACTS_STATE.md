# Controller views and pure action previews: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Controller views and pure action previews SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| viewKinds | unique ID array | Only declared view schemas are queryable. | ["party","actions","known_map"] |
| maxSelection | integer | Bound selected entities and response work. | 32 |
| stalePolicy | string | A preview revision cannot authorize later mutation. | "refuse" |
| unknownValue | string | Hidden or unavailable information cannot become a fabricated zero. | "explicit_unknown" |

Semantic validation: **Each view kind has an output schema and controller visibility policy; stalePolicy applies to previews only and cannot bypass command revalidation.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record GameQueryDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> ViewKinds,
    long MaxSelection,
    string StalePolicy,
    string UnknownValue);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| ReadView | kind and bounded selection | Scoped immutable view or VIEW_DENIED. |
| PreviewAction | action arguments and expected revision | Availability explanation or PREVIEW_STALE. |
| ListActions | actor and context | Declared actions with visible reasons; no hidden-state oracle. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| ViewRequest | trustedController, kind, selection, knownRevision | Identity comes from the trusted host. |
| GameView | session/context revision, permitted entities and values | Read-only projection; secrets are excluded. |
| ActionPreview | legal targets, visible cost/time/risk, reason, revision | No RNG draw, reservation or mutation. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Preview cannot change state, RNG or resource reservations.
- Hidden entities and future random outcomes never appear in a public view.
- A changed revision invalidates or recomputes the preview according to policy.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.

## Detailed engine integration decisions

The following design detail is relocated from the earlier overview and is maintained here as this capability's owner. The configuration DTO above is the setup definition; supplemental runtime/profile records below serve distinct roles. All remain proposed until implemented with the consumer.

Existing Snapshot remains a trusted diagnostics/save boundary. Add optional IGameQueries with immutable controller-scoped views and action previews. It is not a raw JSON-path query over arbitrary state.

View request: trusted controller identity, declared view kind and bounded selection. Result: session/world identity, state revision, visible entities, permitted data and localized explanation keys. The layer decides knowledge/ownership. Hidden enemies, sealed dialogue branches, private orders and future random results are excluded. Native UI receives this projection, not the complete snapshot.

Action preview: actor/target/action, requested view revision and bounded arguments. Result: availability, reason code, visible cost/time/risk estimate, legal targets and revision. Preview reads immutable committed state, consumes no RNG draw, reserves no resource and changes nothing. Enqueue/execution revalidate against current state; an old preview never authorizes an action. Unknown information remains unknown rather than being returned as a fake zero or a hidden exact probability.

Use current operation schemas for arguments. Start with the operations the first native command panel needs, rather than building a universal UI schema language. Presentation remains Godot Control/Container/InputMap/translation work.
