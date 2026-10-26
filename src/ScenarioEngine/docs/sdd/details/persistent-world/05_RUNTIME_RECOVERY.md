# Persistent worlds, regions and stable identities: runtime, failure and recovery

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Persistent worlds, regions and stable identities SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Execution sequence

1. Resolve saved state or explicitly generate a new declared region.
2. Advance inactive state using its authored policy and elapsed logical interval.
3. Prepare native resources and bind stable identities.
4. Activate after matching readiness; expose scoped views.
5. On unload retain deltas/tombstones and stop native callbacks.
6. Commit a manifest referencing exact region revisions before acknowledging a save.

## Capability-local state model

- **Dormant:** Resolve saved state or explicitly generate a new declared region.
- **PreparingRegion:** Advance inactive state using its authored policy and elapsed logical interval.
- **Active:** Prepare native resources and bind stable identities.
- **Checkpointed:** Activate after matching readiness; expose scoped views.

~~~mermaid
stateDiagram-v2
    [*] --> Dormant
    Dormant --> PreparingRegion: validated transition
    PreparingRegion --> Active: validated transition
    Active --> Checkpointed: validated transition
~~~

These labels describe this package's local lifecycle; they are not new values silently added to SessionPhase. The final local state may publish a scoped result and return to another active context. Only the selected root completion policy ends the whole session. Re-entry/retry needs an explicit new instance/attempt or a legal continuation of the saved identity.

## Transaction and ordering

Validate identity/authority/revision before costs or random draws. Resolve required prior evidence and due boundaries before effects according to the selected layer's declared ordering. Operate on a candidate of the previous immutable state, ask each owning kit for typed changes, validate final invariants, then publish the committed batch. No native animation, menu click or asynchronous callback writes authoritative state directly.

- Looted/destroyed objects do not respawn merely because a scene reloads.
- Only one region owns an entity at a time.
- Frozen and abstract simulation are explicit gameplay choices, never silent failure substitutes.
- NodePath and instance IDs are never durable entity identities.

## Failure contract

| Trigger | Stable proposed code | Preserved state / recovery |
| --- | --- | --- |
| Missing/corrupt region | REGION_UNAVAILABLE | Refuse activation and preserve earlier world manifest. |
| Conflicting region revision | REGION_CONFLICT | Refuse stale transfer and refresh the view. |
| Inactive simulation failure | REGION_ADVANCE_FAILED | Do not activate invented state or double-run production. |


Expected rejection returns a typed reason and no partial effects. An unexpected rule exception discards the candidate and follows the existing engine fault policy; preserve the cause through IEngineReporter. Native/storage operations have their own owner/cancellation and correlated outcomes. A request cancellation received after successful commit cannot turn the completed operation into a false cancelled result.

## Cancellation, retries and concurrency

One host-owned writer advances a session; concurrent/reentrant mutation is refused using the existing engine boundary. Cancel only work owned by the matching action/instance/attempt. A retry keeps its identity where it is the same operation, or uses a recorded new attempt when policy requires it. Deduplication is based on stored command/result identities, not a UI button's disabled state. Do not retry a failed side effect by inventing stock, state, content or readiness.

## Save, restore and replay

Checkpoint the state records in [contracts/state](03_CONTRACTS_STATE.md), pending effects/work/reservations, logical time and profile identities. Restore definitions first, validate references/invariants, reconstruct native bindings, and enable input only after readiness. A save taken while waiting/paused resumes that boundary. Use existing explicit previous-generation recovery; never silently choose another lineage or generation.

Replay records actual admitted inputs/world results plus compatible time/context metadata. Same content/profile/initial state and observations must give matching state/output hashes. Native physics determinism and physical route acceptance remain separate. If storage/history limits are reached, return explicit limit status rather than claiming a recoverable checkpoint that was never written.

## Operational services and UI

Use existing ApplicationDiagnostics through IEngineReporter and the native UI notification adapter when implemented. Log reference/correlation and original causes locally; UI shows safe reason/reference, not raw payloads or exception stacks. Audit important acknowledged outcomes consistently with their commit, and label denied/failed/cancelled results truthfully. No account, payment, tracking or remote notification feature is added merely because this package exists.
