# Play contexts and encounter rulesets: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Play contexts and encounter rulesets SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| contextKinds | unique ID array | Trusted registered rule variants owned by the selected game layer. | ["exploration","party_battle","duel","army_battle"] |
| initialContext | string | Must resolve to a declared kind/entry definition. | "exploration" |
| maxDepth | integer | Bound parent/return contexts; no unrestricted scene stack. | 2 |
| parentClockPolicy | string | Explicitly define time behavior outside the active context. | "freeze" |

Semantic validation: **initialContext resolves in contextKinds; allowed entry/return edges and mode time profiles are validated. maxDepth bounds stored parent frames.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record PlayContextDefinition(
    string Id,
    string DefinitionVersion,
    ImmutableArray<string> ContextKinds,
    string InitialContext,
    long MaxDepth,
    string ParentClockPolicy);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| EnterContext | kind/definition and participant references | Prepared transition or CONTEXT_DENIED. |
| SubmitContextAction | active context/revision and operation | Delegated candidate resolution under the owning layer. |
| CompleteContext | result and source revision | Apply once and resume parent, without ending the whole game. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| PlayContextFrame | contextId, rules/version, time profile, source revision, participants, checkpoint | Only one active frame initially. |
| ReturnContext | parent identity, location/anchor, resume policy | Persist before entering the child. |
| EncounterResult | context/resultId, typed deltas, source revision, applied status | Root campaign ending is a separate operation. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Do not call GameBase.Init again for every battle.
- Character/inventory identities survive context changes.
- Duplicate completion cannot duplicate rewards or casualties.
- No context animation callback mutates authoritative rules.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
