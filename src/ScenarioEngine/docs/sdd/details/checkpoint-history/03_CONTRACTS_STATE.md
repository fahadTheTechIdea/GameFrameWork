# Checkpoint storage, replay segments and receipt retention: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Checkpoint storage, replay segments and receipt retention SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| checkpointEverySteps | integer | Explicit policy boundary, not a claim of measured cost. | 1000 |
| maxSegmentBytes | integer | Hard configured storage bound. | 16777216 |
| retainedCheckpoints | integer | Keep current and previous acknowledged generations. | 2 |
| receiptPolicy | string | Retired epochs are refused; never evict IDs and allow re-execution. | "bounded_epoch" |

Semantic validation: **Every retained manifest references existing verified sections. The receipt policy requires a persisted retired boundary before compaction can remove receipts.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record ScenarioHistoryDefinition(
    string Id,
    string DefinitionVersion,
    long CheckpointEverySteps,
    long MaxSegmentBytes,
    long RetainedCheckpoints,
    string ReceiptPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| SealCheckpoint | committed section references | Durable promoted manifest or CHECKPOINT_FAILED. |
| AppendSegment | ordered admitted/committed operation records | Acknowledged append or explicit limit/full status. |
| RetireEpoch | committed checkpoint and dedup boundary | Persisted refusal boundary before old receipts may be removed. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| CheckpointManifest | checkpointId, revision, content/profile identity, section digests | All referenced sections must validate before promotion. |
| ReplaySegment | start checkpoint, prior segment digest, ordered entries | A replayable prefix is explicitly retained. |
| CommandEpoch | epochId, admitted sequence, retiredBoundary | Old delayed messages cannot become fresh commands. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- No acknowledged manifest references missing/unverified sections.
- A discarded replay prefix is not advertised as replayable.
- Compaction never re-enables an old command.
- Current format-1 interpretation remains explicit and supported until a verified migration exists.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
