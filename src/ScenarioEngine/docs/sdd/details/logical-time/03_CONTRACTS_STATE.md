# Logical time and advancement policies: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Logical time and advancement policies SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| policy | string | Registered policy determines permitted advancement boundaries. | "action" |
| unit | string | Explicit integer unit; never infer the unit from Tick. | "millisecond" |
| fixedDelta | integer | Zero for action/calendar modes; fixed mode requires a positive value. | 0 |
| maximumAdvance | integer | Cap one requested interval; stop at required event boundaries. | 1 |

Semantic validation: **fixed requires fixedDelta > 0; action/calendar require fixedDelta = 0. Unit/policy combinations must be explicitly supported. These cross-field rules require semantic validation.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record SimulationTimeDefinition(
    string Id,
    string DefinitionVersion,
    string Policy,
    string Unit,
    long FixedDelta,
    long MaximumAdvance);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| Advance | request and current boundary | Validated delta or ADVANCE_REFUSED; failure changes neither time nor RNG. |
| EndTurn | controller and active turn revision | Layer validates ownership/budget before advancing its turn. |
| PauseResume | session lifecycle | Waiting/paused sessions consume no logical time. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| SimulationTime | commitIndex, elapsedUnits, unit, policyId/version | Commit index and elapsed units are monotonic independently. |
| AdvanceRequest | kind, requestedUnits, controller, boundaryRevision | Trusted policy validates before time can change. |
| AdvanceContext | previousUnits, deltaUnits, nextUnits | Candidate context supplied to the owning layer. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- No DateTime.Now or elapsed render frame count controls rule time.
- A failed commit cannot consume game time.
- Fast-forward never skips an unresolved observable deadline.
- Native body movement remains native continuous physics work.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.

## Detailed engine integration decisions

The following design detail is relocated from the earlier overview and is maintained here as this capability's owner. The configuration DTO above is the setup definition; supplemental runtime/profile records below serve distinct roles. All remain proposed until implemented with the consumer.

Today every LayerTickContext.FixedStep is 50 ms. Preserve this behavior for current content and replay. Future advancement separates the monotonic commit index from elapsed game time.

Proposed contracts/data:

| Item | Responsibility |
| --- | --- |
| AdvanceProfile | Policy ID/version, integer time unit, limits and mode-specific configuration |
| SimulationTimeSnapshot | Elapsed game-time units and advancement-policy state; persisted with the commit |
| AdvanceRequest | Requested boundary: fixed step, player decision/end turn or calendar interval |
| AdvanceContext | Validated commit index, previous time, elapsed units and next time supplied to the layer |
| IAdvancePolicy | Validate an advance against the current immutable boundary; no wall-clock or native scheduling |

Fixed real-time policy supplies the configured delta for each committed step. A player-action policy advances only on an explicit accepted decision/phase boundary. A calendar policy advances through explicitly processed event boundaries. Initiative, action points, hunger costs, school terms and seasons remain domain state, not fields in the generic policy.

Godot Timer/signals wake real-time or scheduled host work. Continuous native movement uses documented physics callbacks. The policy is saved game-time semantics for C# simulation and replay; it is not a second native timer or per-frame scheduler. A turn-based host stops idle simulation wakeups while waiting for the player. Animation completion controls presentation readiness, never whether an already committed attack spent its cost.

A failed advance changes neither commit index nor game time. Pausing changes neither. Fast-forward/rest must stop at the first required interrupt or event boundary; it cannot skip an attack, starvation, recipe completion or expiring choice. Inactive-world advancement must use its declared domain rules. Do not translate eight hours of rest into 576,000 empty 50 ms steps or treat native movement as safely skippable.

Compatibility work is mandatory: define a new supported context/profile format, retain explicit old-format interpretation, update content/save/replay codecs and hash inputs together, and refuse unknown time policies. Never reinterpret an old tick number as a turn or a day.
