# Acknowledged world requests and transitions: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Acknowledged world requests and transitions SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| requestKinds | unique ID array | Each kind needs both request and result schemas. | ["region.enter","actor.spawn"] |
| maxPending | integer | Admission bound per session. | 64 |
| deadlineUnits | integer | Logical deadline for requests whose rule clock is advancing. | 200 |
| freezeScope | string | Only declared scope is frozen while native preparation runs. | "context" |

Semantic validation: **Result schemas must match every requestKinds entry. Session tokens, attempt IDs and scope revisions are checked at result admission, not inferred from the payload name.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record WorldRequestDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> RequestKinds,
    long MaxPending,
    long DeadlineUnits,
    string FreezeScope);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| IssueRequest | validated target and scope | Committed pending request or REQUEST_LIMIT. |
| AdmitWorldResult | correlated result observation | Apply once or WORLD_RESULT_STALE. |
| CancelRequest | request/attempt and authority | Explicit cancelled/already-completed result; no cancellation after commit is disguised as failure. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| PendingWorldRequest | requestId, attemptId, instanceToken, scopeRevision, reservationRef | Persist exact attempt and source boundary. |
| WorldResult | matching identifiers, Ready/Failed/Cancelled, bounded resultData | Admitted observation; never trust a stale callback. |
| TransitionRecord | source, target, status, committedOutcome | One terminal outcome per attempt. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Dispatch is never reported as completed work.
- Duplicate Ready cannot spend twice.
- Region binding revisions do not invalidate unrelated regions.
- Current Transitioning saves stay refused until a resumable checkpoint exists.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
