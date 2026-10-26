# Optional multiplayer authority and scoped replication: contracts and state

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Optional multiplayer authority and scoped replication SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Definition contract

The [JSON Schema](contracts/definition.schema.json) is the structural owner for this proposed definition. The root requires id, definitionVersion and every setting below, with additional properties refused. Identifiers are bounded strings; exact schema/profile versions determine compatibility. Any intentionally empty collection needs an explicit schema/policy change, not a missing-configuration default.

| Field | Wire type | Meaning and constraint | Example |
| --- | --- | --- | --- |
| authorityMode | string | One authority validates orders and world results. | "host" |
| maxPeers | integer | Profile admission bound, not current network support. | 4 |
| reconnectPolicy | string | Reconnect from an authoritative revision. | "checkpoint_resync" |
| maxQueuedPerPeer | integer | Bound per-peer work before core admission. | 32 |

Semantic validation: **Peers must match content/profile/epoch before admission. Authority maps actual transport senders to controllers and rejects client-supplied authority claims.** JSON Schema alone does not establish cross-reference, reachability, unit compatibility or duplicate-effect safety.

## Proposed C# definition DTO

~~~csharp
using System.Collections.Immutable;

// Design target only; not a compiled API in the current Contracts assembly.
public sealed record MultiplayerAuthorityDefinition(
    string Id,
    string DefinitionVersion,
    string AuthorityMode,
    long MaxPeers,
    string ReconnectPolicy,
    long MaxQueuedPerPeer);
~~~

Enumerated string settings are validated against closed schema values before construction/use. Runtime APIs should use their owning validated types; this illustrative wire DTO does not authorize arbitrary strings or untrusted runtime type discovery.

## Operations and observable results

| Operation / query | Required arguments | Result contract |
| --- | --- | --- |
| AdmitRemoteIntent | transport sender plus bounded command | Mapped local admission or PEER_DENIED. |
| PublishView | permitted peer/controller | Filtered revision data. |
| Reconnect | handshake and authoritative checkpoint | Resync or explicit mismatch; no client snapshot overwrite. |


Commands use the existing CommandEnvelope boundary where they mutate scenario state: trusted controller, command ID, target commit index, world generation, registered operation and bounded payload. Queries are pure and use the [query contract](../player-queries/README.md). An extension's I/O method is called by its trusted owner/adapter, not exposed as an arbitrary player command. Exact command namespaces and payload schemas must be registered with the working consumer before use.

## Runtime checkpoint data

| Record / state section | Required fields | Validation/ownership |
| --- | --- | --- |
| PeerBinding | actualPeerId, trustedControllerId, permissions, sessionToken | Identity assigned by the authority, never a payload claim. |
| ReplicationView | controller-scoped revision and permitted delta | No raw trusted SessionSnapshot transfer. |
| ResyncBoundary | content/profile identity, checkpoint/revision, admitted epoch | Reconnect preserves dedup and ownership. |


IDs are stable bounded identifiers, distinct from native instance IDs. Revisions/sequences and elapsed logical units are nonnegative checked 64-bit integers. Quantity/duration fields carry the explicit unit declared by their owning definition/time profile. Collections/maps are immutable at the simulation boundary; reference identity and JSON ownership cannot leak mutable state into a committed snapshot.

Keep one authoritative state section per owner. Distinguish absent/unknown information from a real zero or empty result. Large opaque/third-party checkpoints require a declared encoding/version/digest and bounded size; they cannot silently relax the existing integer-only rule-state hashing contract.

## Compatibility and invariants

- Clients never choose authoritative controller identity or RNG/world-result state.
- Checksum/replay determinism does not imply cross-machine native physics determinism.
- Disconnected peers cannot retain unauthorized ownership.

Save/replay must carry the definition/profile/content identities and all pending state needed for continuation. Unknown versions return an incompatibility result and preserve bytes. An existing format-1 snapshot is never reinterpreted as this new package. Migrations are explicit source-to-target transforms, validated before a new generation is written.
