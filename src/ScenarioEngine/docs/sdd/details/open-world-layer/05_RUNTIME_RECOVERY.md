# Open-world sandbox layer: runtime, failure and recovery

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Open-world sandbox layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Execution sequence

1. Resolve world identity and pinned generation manifest.
2. Load saved region revisions; generate only explicitly new regions.
3. Prepare native residency and reconcile bindings.
4. Admit region-local observations without closing unrelated work.
5. Complete quests/jobs through separate instance outcomes.
6. Checkpoint world manifest and resume the same lineage after quit.

## Capability-local state model

- **WorldActive:** Resolve world identity and pinned generation manifest.
- **PreparingRegion:** Load saved region revisions; generate only explicitly new regions.
- **RegionActive:** Prepare native residency and reconcile bindings.
- **ActivityResolved:** Admit region-local observations without closing unrelated work.

~~~mermaid
stateDiagram-v2
    [*] --> WorldActive
    WorldActive --> PreparingRegion: validated transition
    PreparingRegion --> RegionActive: validated transition
    RegionActive --> ActivityResolved: validated transition
~~~

These labels describe this package's local lifecycle; they are not new values silently added to SessionPhase. The final local state may publish a scoped result and return to another active context. Only the selected root completion policy ends the whole session. Re-entry/retry needs an explicit new instance/attempt or a legal continuation of the saved identity.

## Transaction and ordering

Validate identity/authority/revision before costs or random draws. Resolve required prior evidence and due boundaries before effects according to the selected layer's declared ordering. Operate on a candidate of the previous immutable state, ask each owning kit for typed changes, validate final invariants, then publish the committed batch. No native animation, menu click or asynchronous callback writes authoritative state directly.

- Unloading is not deletion of logical entities or state.
- A finished side mission does not set the ongoing world to Ended.
- Region-local changes do not invalidate every world order.

## Failure contract

| Trigger | Stable proposed code | Preserved state / recovery |
| --- | --- | --- |
| Missing region | REGION_UNAVAILABLE | Retain current checkpoint and expose repair/retry. |
| Invalid side-activity result | ACTIVITY_RESULT_DENIED | Preserve ongoing world state. |
| Residency budget exceeded | WORLD_RESIDENCY_LIMIT | Refuse transition or reduce declared residency explicitly; never omit required state silently. |


Expected rejection returns a typed reason and no partial effects. An unexpected rule exception discards the candidate and follows the existing engine fault policy; preserve the cause through IEngineReporter. Native/storage operations have their own owner/cancellation and correlated outcomes. A request cancellation received after successful commit cannot turn the completed operation into a false cancelled result.

## Cancellation, retries and concurrency

One host-owned writer advances a session; concurrent/reentrant mutation is refused using the existing engine boundary. Cancel only work owned by the matching action/instance/attempt. A retry keeps its identity where it is the same operation, or uses a recorded new attempt when policy requires it. Deduplication is based on stored command/result identities, not a UI button's disabled state. Do not retry a failed side effect by inventing stock, state, content or readiness.

## Save, restore and replay

Checkpoint the state records in [contracts/state](03_CONTRACTS_STATE.md), pending effects/work/reservations, logical time and profile identities. Restore definitions first, validate references/invariants, reconstruct native bindings, and enable input only after readiness. A save taken while waiting/paused resumes that boundary. Use existing explicit previous-generation recovery; never silently choose another lineage or generation.

Replay records actual admitted inputs/world results plus compatible time/context metadata. Same content/profile/initial state and observations must give matching state/output hashes. Native physics determinism and physical route acceptance remain separate. If storage/history limits are reached, return explicit limit status rather than claiming a recoverable checkpoint that was never written.

## Operational services and UI

Use existing ApplicationDiagnostics through IEngineReporter and the native UI notification adapter when implemented. Log reference/correlation and original causes locally; UI shows safe reason/reference, not raw payloads or exception stacks. Audit important acknowledged outcomes consistently with their commit, and label denied/failed/cancelled results truthfully. No account, payment, tracking or remote notification feature is added merely because this package exists.
