# Narrative adventure layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Narrative adventure layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| locationIds | unique ID array | Declared locations with validated exit and interaction references. | ["village","river","friendly_lines"] |
| startLocation | string | Must be in locationIds. | "village" |
| interactionIds | unique ID array | Referenced narrative/activity definitions. | ["contact_ferryman","recover_radio"] |
| completionScope | string | Close only the selected adventure scope. | "operation" |

Semantic validation: **startLocation must occur in locationIds; every interaction/exit reference resolves, and every closing state has one highest-priority authored ending.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record AdventureDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> LocationIds,
    string StartLocation,
    ImmutableArray<string> InteractionIds,
    string CompletionScope);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| adventure.interact | actor, target, expected revision | Available interaction or visible unavailable reason. |
| adventure.choose | interaction and offered choice ID | One committed decision/effect set. |
| adventure.travel | exit ID and party | Validated route request, reserved cost and correlated destination readiness. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| AdventureState | currentLocation, visitedLocations, decisionLedger, discoveredClues | Location changes preserve prior discoveries. |
| InteractionState | instanceId, targetId, choiceCheckpoint, completedEffects | Owned by narrative/activity kit, referenced here. |
| JourneyState | elapsedTravel, routeUnlocks, activeObjectiveIds | Travel consequences use logical time and explicit costs. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Required story progress has an affordable supported route.
- A clue or item cannot be collected twice by reloading a scene.
- Dialogue choice availability uses current state, not a stale menu.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
