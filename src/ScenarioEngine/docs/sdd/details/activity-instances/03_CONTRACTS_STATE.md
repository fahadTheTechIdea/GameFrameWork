# Repeatable jobs, quests and incidents: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Repeatable jobs, quests and incidents SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| activityKinds | unique ID array | Registered definition families and result schemas. | ["craft","escort","incident"] |
| maxInstances | integer | Bound live/history instances for the chosen profile. | 128 |
| deadlineUnit | string | Must match the selected advancement profile. | "millisecond" |
| cancellationPolicy | string | Explicit treatment of reserved versus already consumed resources. | "release_unconsumed" |

Semantic validation: **Definition duration/deadline units match the time profile. Repeated jobs have distinct instances and can release only their own reservations.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record ActivityDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> ActivityKinds,
    long MaxInstances,
    string DeadlineUnit,
    string CancellationPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| StartActivity | definition, owner and current revision | Reserved instance or ACTIVITY_REFUSED. |
| CancelActivity | instance and authority | Declared terminal result or existing completion. |
| ResolveDue | logical boundary and due instances | Stable-ID-ordered candidate completions; no background mutation. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| ActivityInstance | instanceId, definitionId, owner/region, state, dueTime | Two instances of one definition remain distinct. |
| ReservationLink | activityId, resource reservationIds | Only this activity releases its own reservation. |
| ActivityResult | instanceId, terminal state, resultId, appliedEffects | One terminal effect sequence per instance. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- A terminal static objective is never reset to repeat a recipe.
- Completion and cancellation cannot both consume/release the same reservation.
- Due-time processing is deterministic and stored in logical units.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
