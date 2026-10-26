# RTS and base-defence layer: runtime, failure and recovery

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [RTS and base-defence layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Execution sequence

1. Validate group ownership and canonicalize actor ordering.
2. Preview known routes/costs through scoped queries.
3. Commit orders/reservations; host applies native movement and placement requests.
4. Admit complete batched observations with declared safety bounds.
5. Resolve production/combat/objectives under one candidate commit.
6. Measure path, frame, rule and save costs before qualifying a larger tier.

## Capability-local state model

- **Planning:** Validate group ownership and canonicalize actor ordering.
- **OrdersCommitted:** Preview known routes/costs through scoped queries.
- **NativeActionsActive:** Commit orders/reservations; host applies native movement and placement requests.
- **ResultsObserved:** Admit complete batched observations with declared safety bounds.

~~~mermaid
stateDiagram-v2
    [*] --> Planning
    Planning --> OrdersCommitted: validated transition
    OrdersCommitted --> NativeActionsActive: validated transition
    NativeActionsActive --> ResultsObserved: validated transition
~~~

These labels describe this package's local lifecycle; they are not new values silently added to SessionPhase. The final local state may publish a scoped result and return to another active context. Only the selected root completion policy ends the whole session. Re-entry/retry needs an explicit new instance/attempt or a legal continuation of the saved identity.

## Transaction and ordering

Validate identity/authority/revision before costs or random draws. Resolve required prior evidence and due boundaries before effects according to the selected layer's declared ordering. Operate on a candidate of the previous immutable state, ask each owning kit for typed changes, validate final invariants, then publish the committed batch. No native animation, menu click or asynchronous callback writes authoritative state directly.

- Group commands cannot bypass per-actor ownership or cost checks.
- A navigation target is not evidence that a body reached it.
- Construction updates collision/navigation through native facilities before usable readiness.

## Failure contract

| Trigger | Stable proposed code | Preserved state / recovery |
| --- | --- | --- |
| Mixed/invalid selection | GROUP_ORDER_REFUSED | Return visible member reasons without leaking enemies. |
| Placement blocked | BUILD_BLOCKED | No material consumption or ghost building. |
| Observation/profile overload | RTS_PROFILE_LIMIT | Refuse/report the unsupported workload instead of dropping evidence. |


Expected rejection returns a typed reason and no partial effects. An unexpected rule exception discards the candidate and follows the existing engine fault policy; preserve the cause through IEngineReporter. Native/storage operations have their own owner/cancellation and correlated outcomes. A request cancellation received after successful commit cannot turn the completed operation into a false cancelled result.

## Cancellation, retries and concurrency

One host-owned writer advances a session; concurrent/reentrant mutation is refused using the existing engine boundary. Cancel only work owned by the matching action/instance/attempt. A retry keeps its identity where it is the same operation, or uses a recorded new attempt when policy requires it. Deduplication is based on stored command/result identities, not a UI button's disabled state. Do not retry a failed side effect by inventing stock, state, content or readiness.

## Save, restore and replay

Checkpoint the state records in [contracts/state](03_CONTRACTS_STATE.md), pending effects/work/reservations, logical time and profile identities. Restore definitions first, validate references/invariants, reconstruct native bindings, and enable input only after readiness. A save taken while waiting/paused resumes that boundary. Use existing explicit previous-generation recovery; never silently choose another lineage or generation.

Replay records actual admitted inputs/world results plus compatible time/context metadata. Same content/profile/initial state and observations must give matching state/output hashes. Native physics determinism and physical route acceptance remain separate. If storage/history limits are reached, return explicit limit status rather than claiming a recoverable checkpoint that was never written.

## Operational services and UI

Use existing ApplicationDiagnostics through IEngineReporter and the native UI notification adapter when implemented. Log reference/correlation and original causes locally; UI shows safe reason/reference, not raw payloads or exception stacks. Audit important acknowledged outcomes consistently with their commit, and label denied/failed/cancelled results truthfully. No account, payment, tracking or remote notification feature is added merely because this package exists.
