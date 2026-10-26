# Repeatable jobs, quests and incidents: runtime, failure and recovery

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Repeatable jobs, quests and incidents SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Execution sequence

1. Validate prerequisites and queue capacity.
2. Reserve inputs through the resource owner and commit the new instance.
3. Wake from declared time/evidence changes rather than per-render polling.
4. Resolve interruption before completion according to authored ordering.
5. Apply completion/cancellation effects once and commit status/reservation changes.
6. Expose bounded summaries to scenario objectives; do not create unbounded fact declarations.

## Capability-local state model

- **Created:** Validate prerequisites and queue capacity.
- **Reserved:** Reserve inputs through the resource owner and commit the new instance.
- **Running:** Wake from declared time/evidence changes rather than per-render polling.
- **Terminal:** Resolve interruption before completion according to authored ordering.

~~~mermaid
stateDiagram-v2
    [*] --> Created
    Created --> Reserved: validated transition
    Reserved --> Running: validated transition
    Running --> Terminal: validated transition
~~~

These labels describe this package's local lifecycle; they are not new values silently added to SessionPhase. The final local state may publish a scoped result and return to another active context. Only the selected root completion policy ends the whole session. Re-entry/retry needs an explicit new instance/attempt or a legal continuation of the saved identity.

## Transaction and ordering

Validate identity/authority/revision before costs or random draws. Resolve required prior evidence and due boundaries before effects according to the selected layer's declared ordering. Operate on a candidate of the previous immutable state, ask each owning kit for typed changes, validate final invariants, then publish the committed batch. No native animation, menu click or asynchronous callback writes authoritative state directly.

- A terminal static objective is never reset to repeat a recipe.
- Completion and cancellation cannot both consume/release the same reservation.
- Due-time processing is deterministic and stored in logical units.

## Failure contract

| Trigger | Stable proposed code | Preserved state / recovery |
| --- | --- | --- |
| Queue full | ACTIVITY_LIMIT | Refuse new work without reserving stock. |
| Duplicate completion | ACTIVITY_ALREADY_TERMINAL | Return existing result; no second output. |
| Owner/target lost | ACTIVITY_INTERRUPTED | Apply declared interruption/refund policy in one commit. |


Expected rejection returns a typed reason and no partial effects. An unexpected rule exception discards the candidate and follows the existing engine fault policy; preserve the cause through IEngineReporter. Native/storage operations have their own owner/cancellation and correlated outcomes. A request cancellation received after successful commit cannot turn the completed operation into a false cancelled result.

## Cancellation, retries and concurrency

One host-owned writer advances a session; concurrent/reentrant mutation is refused using the existing engine boundary. Cancel only work owned by the matching action/instance/attempt. A retry keeps its identity where it is the same operation, or uses a recorded new attempt when policy requires it. Deduplication is based on stored command/result identities, not a UI button's disabled state. Do not retry a failed side effect by inventing stock, state, content or readiness.

## Save, restore and replay

Checkpoint the state records in [contracts/state](03_CONTRACTS_STATE.md), pending effects/work/reservations, logical time and profile identities. Restore definitions first, validate references/invariants, reconstruct native bindings, and enable input only after readiness. A save taken while waiting/paused resumes that boundary. Use existing explicit previous-generation recovery; never silently choose another lineage or generation.

Replay records actual admitted inputs/world results plus compatible time/context metadata. Same content/profile/initial state and observations must give matching state/output hashes. Native physics determinism and physical route acceptance remain separate. If storage/history limits are reached, return explicit limit status rather than claiming a recoverable checkpoint that was never written.

## Operational services and UI

Use existing ApplicationDiagnostics through IEngineReporter and the native UI notification adapter when implemented. Log reference/correlation and original causes locally; UI shows safe reason/reference, not raw payloads or exception stacks. Audit important acknowledged outcomes consistently with their commit, and label denied/failed/cancelled results truthfully. No account, payment, tracking or remote notification feature is added merely because this package exists.

## Detailed engine integration decisions

The following design detail is relocated from the earlier overview and is maintained here as this capability's owner. The configuration DTO above is the setup definition; supplemental runtime/profile records below serve distinct roles. All remain proposed until implemented with the consumer.

A terminal static objective must not be reset to represent a second crafting job or a repeatable quest. Kits create separate activity instances from registered definitions. Expose bounded summary facts for the scenario graph rather than adding an unbounded per-entity fact key set. Limit queued activities, effect recursion, authored condition cost and instance counts; overflow refuses an action with an explicit reason.

Persist due game-time records and reservations in the kit that owns the work. Godot native Timers/signals wake scheduled native work; headless simulation explicitly advances through the same logical boundaries. Do not add a parallel real-time scheduler or scan unchanged menus/entities every render frame.
