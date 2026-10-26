# 4X strategy layer: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [4X strategy layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| factionIds | unique ID array | Stable owners and diplomacy participants. | ["north","south","coast"] |
| turnPolicy | string | Explicit conflict-resolution rules for the chosen mode. | "sequential_factions" |
| researchIds | unique ID array | Validated unlock/prerequisite graph. | ["logistics","engineering"] |
| victoryIds | unique ID array | Authored closing conditions and tie resolution. | ["diplomatic","territorial"] |

Semantic validation: **Research prerequisites form a valid graph; victory conditions cover supported closure and ties. Planned orders require an explicit deterministic conflict policy.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record FourXDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> FactionIds,
    string TurnPolicy,
    ImmutableArray<string> ResearchIds,
    ImmutableArray<string> VictoryIds);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| fourx.submit_order | faction, action and turn revision | Queued/validated plan or faction denial. |
| fourx.offer_agreement | parties and registered typed terms | Visible proposal with no premature transfer. |
| fourx.end_turn | faction and current boundary | Deterministic economy/research/diplomacy resolution. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| FactionState | treasury, research, territory references and turn budget | One authoritative faction owner. |
| KnowledgeState | discovered cells, contacts, agreement visibility | Views filter per faction/controller. |
| AgreementState | agreementId, parties, terms, duration, applied effects | Acceptance/expiry have stable identities. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Another faction's private plans/resources are not exposed by previews.
- Diplomatic proposals apply nothing until valid acceptance.
- Victory ties and mutually exclusive results have explicit policy.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
