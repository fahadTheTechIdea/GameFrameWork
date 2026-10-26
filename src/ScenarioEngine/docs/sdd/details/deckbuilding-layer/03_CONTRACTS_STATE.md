# Deckbuilding and card encounter layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Deckbuilding and card encounter layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| cardIds | unique ID array | Registered card definitions, targets, costs and effects. | ["strike","guard","supply"] |
| startingDeck | unique ID array | Unique instance definition references; duplicates by card definition use separate instance IDs. | ["starter_strike","starter_guard"] |
| handLimit | integer | Bound visible hand and draw overflow policy. | 10 |
| maxEffectDepth | integer | Reject content/effects that require unbounded recursive resolution. | 16 |

Semantic validation: **Starting deck entries resolve to unique instances; repeated card definitions are legal through separate instance definitions. Trigger chains are bounded and every card owns one zone.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record DeckbuildingDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> CardIds,
    ImmutableArray<string> StartingDeck,
    long HandLimit,
    long MaxEffectDepth);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| card.play | instance, targets and encounter revision | Validated atomic cost/effect resolution. |
| card.choose | pending choice and offered option ID | One branch; stale selections are refused. |
| card.end_turn | owner and turn revision | Deterministic discard/status/turn transition. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| CardInstance | instanceId, definitionId, owner, currentZone | One zone owns each instance. |
| DeckState | ordered draw pile, hand, discard, exhaust and RNG stream | Shuffle/draw state is fully saved. |
| CardEncounter | turn, energy, pending choices, ordered effect queue | Player-choice boundary is resumable. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- A card instance exists in exactly one zone.
- Rejected play spends no energy or card.
- Triggered effects cannot recurse indefinitely or use presentation callbacks as rule execution.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
