# Optional multiplayer authority and scoped replication: runtime, failure and recovery

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Optional multiplayer authority and scoped replication SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Execution sequence

1. Match content/profile versions before admitting a peer.
2. Map transport sender to controller and enforce permissions.
3. Validate/rate-bound commands through existing admission.
4. Run authoritative simulation and record its world observations.
5. Publish filtered views using native Godot transport/RPC facilities.
6. Resync disconnected peers from a verified boundary.

## Capability-local state model

- **Disconnected:** Match content/profile versions before admitting a peer.
- **Handshaking:** Map transport sender to controller and enforce permissions.
- **Admitted:** Validate/rate-bound commands through existing admission.
- **Resynchronizing:** Run authoritative simulation and record its world observations.

~~~mermaid
stateDiagram-v2
    [*] --> Disconnected
    Disconnected --> Handshaking: validated transition
    Handshaking --> Admitted: validated transition
    Admitted --> Resynchronizing: validated transition
~~~

These labels describe this package's local lifecycle; they are not new values silently added to SessionPhase. The final local state may publish a scoped result and return to another active context. Only the selected root completion policy ends the whole session. Re-entry/retry needs an explicit new instance/attempt or a legal continuation of the saved identity.

## Transaction and ordering

Validate identity/authority/revision before costs or random draws. Resolve required prior evidence and due boundaries before effects according to the selected layer's declared ordering. Operate on a candidate of the previous immutable state, ask each owning kit for typed changes, validate final invariants, then publish the committed batch. No native animation, menu click or asynchronous callback writes authoritative state directly.

- Clients never choose authoritative controller identity or RNG/world-result state.
- Checksum/replay determinism does not imply cross-machine native physics determinism.
- Disconnected peers cannot retain unauthorized ownership.

## Failure contract

| Trigger | Stable proposed code | Preserved state / recovery |
| --- | --- | --- |
| Forged ownership | PEER_DENIED | Refuse and audit actual sender identity. |
| Content mismatch | NET_PROFILE_MISMATCH | Refuse entry without changing the session. |
| Lost/reordered/duplicate packets | NET_RESYNC_REQUIRED | Recover through bounded sequence/checkpoint rules. |


Expected rejection returns a typed reason and no partial effects. An unexpected rule exception discards the candidate and follows the existing engine fault policy; preserve the cause through IEngineReporter. Native/storage operations have their own owner/cancellation and correlated outcomes. A request cancellation received after successful commit cannot turn the completed operation into a false cancelled result.

## Cancellation, retries and concurrency

One host-owned writer advances a session; concurrent/reentrant mutation is refused using the existing engine boundary. Cancel only work owned by the matching action/instance/attempt. A retry keeps its identity where it is the same operation, or uses a recorded new attempt when policy requires it. Deduplication is based on stored command/result identities, not a UI button's disabled state. Do not retry a failed side effect by inventing stock, state, content or readiness.

## Save, restore and replay

Checkpoint the state records in [contracts/state](03_CONTRACTS_STATE.md), pending effects/work/reservations, logical time and profile identities. Restore definitions first, validate references/invariants, reconstruct native bindings, and enable input only after readiness. A save taken while waiting/paused resumes that boundary. Use existing explicit previous-generation recovery; never silently choose another lineage or generation.

Replay records actual admitted inputs/world results plus compatible time/context metadata. Same content/profile/initial state and observations must give matching state/output hashes. Native physics determinism and physical route acceptance remain separate. If storage/history limits are reached, return explicit limit status rather than claiming a recoverable checkpoint that was never written.

## Operational services and UI

Use existing ApplicationDiagnostics through IEngineReporter and the native UI notification adapter when implemented. Log reference/correlation and original causes locally; UI shows safe reason/reference, not raw payloads or exception stacks. Audit important acknowledged outcomes consistently with their commit, and label denied/failed/cancelled results truthfully. No account, payment, tracking or remote notification feature is added merely because this package exists.
