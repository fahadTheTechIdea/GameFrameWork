# Campaign, run and profile progression: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Campaign, run and profile progression SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| scope | string | The reset and persistence boundary for this aggregate. | "campaign" |
| allowedDeltaKinds | unique ID array | Only registered typed deltas can cross a scenario boundary. | ["supplies","roster","unlock"] |
| nextScenarioIds | unique ID array | Validated destinations; no arbitrary path or script. | ["crossing","rescue"] |
| resetPolicy | string | Restart/new campaign creates a distinct lineage. | "explicit_new_lineage" |

Semantic validation: **A registered delta schema is required for each allowed kind; nextScenarioIds resolve to validated content. Run/profile transfer rules must be explicit per field.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record GameProgressionDefinition(
    string Id,
    string DefinitionVersion,
    string Scope,
    ImmutableArray<string> AllowedDeltaKinds,
    ImmutableArray<string> NextScenarioIds,
    string ResetPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| ApplyOutcome | outcome and expected progress revision | Committed continuation or existing result. |
| CreateRun | explicit parent/profile and reset policy | New run identity with declared carryover. |
| ContinueScenario | validated destination and progress revision | Fresh session using the committed progress state. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| ProgressSnapshot | lineageId, revision, scopedData, appliedOutcomeLedger | Data and ledger share one atomic commit. |
| ScenarioOutcome | outcomeId, session/content identity, typed deltas | Validated against allowed transfer schemas. |
| Continuation | sourceRevision, destinationIdentity, committedProgressRevision | A durable result before the next session begins. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- A repeated outcome cannot award resources or unlocks twice.
- One save lineage cannot mutate another campaign.
- Encounter completion and final campaign closure are different scopes.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
