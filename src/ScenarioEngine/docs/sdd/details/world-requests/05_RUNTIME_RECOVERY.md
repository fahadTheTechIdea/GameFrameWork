# Acknowledged world requests and transitions: runtime, failure and recovery

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Acknowledged world requests and transitions SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Execution sequence

1. Commit intent and any resource reservation before dispatch.
2. Host prepares native scenes/resources while retaining the old committed source.
3. Await native bindings and navigation synchronization.
4. Admit only a matching result; validate destination and candidate transfer.
5. Commit transfer/cost once or retain/release reservation by declared failure policy.
6. Free old bindings only after a successful replacement is ready.

## Capability-local state model

- **IntentCommitted:** Commit intent and any resource reservation before dispatch.
- **PreparingNative:** Host prepares native scenes/resources while retaining the old committed source.
- **AwaitingResult:** Await native bindings and navigation synchronization.
- **Resolved:** Admit only a matching result; validate destination and candidate transfer.

~~~mermaid
stateDiagram-v2
    [*] --> IntentCommitted
    IntentCommitted --> PreparingNative: validated transition
    PreparingNative --> AwaitingResult: validated transition
    AwaitingResult --> Resolved: validated transition
~~~

These labels describe this package's local lifecycle; they are not new values silently added to SessionPhase. The final local state may publish a scoped result and return to another active context. Only the selected root completion policy ends the whole session. Re-entry/retry needs an explicit new instance/attempt or a legal continuation of the saved identity.

## Transaction and ordering

Validate identity/authority/revision before costs or random draws. Resolve required prior evidence and due boundaries before effects according to the selected layer's declared ordering. Operate on a candidate of the previous immutable state, ask each owning kit for typed changes, validate final invariants, then publish the committed batch. No native animation, menu click or asynchronous callback writes authoritative state directly.

- Dispatch is never reported as completed work.
- Duplicate Ready cannot spend twice.
- Region binding revisions do not invalidate unrelated regions.
- Current Transitioning saves stay refused until a resumable checkpoint exists.

## Failure contract

| Trigger | Stable proposed code | Preserved state / recovery |
| --- | --- | --- |
| Native load failure | WORLD_PREPARE_FAILED | Preserve source checkpoint; expose retry or cancel. |
| Stale callback | WORLD_RESULT_STALE | Ignore its effects, record refusal and keep the current attempt. |
| Timeout while rules frozen | HOST_OPERATION_TIMEOUT | Host cancels its owned asynchronous operation; do not rely on a stopped logical clock. |


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

WorldRequest already carries RequestId, Generation, Kind and Payload. Add a correlated lifecycle rather than a second generic event bus.

Proposed pending-request fields: requestId, kind/schema version, session/world instance token, affected region, source revision, attempt ID, target, issued commit, logical deadline, reservation reference, status and bounded failure reason. Store pending protocol state with the owning committed snapshot.

Proposed WorldRequestResult fields: matching IDs/revisions, outcome (Ready/Failed/Cancelled), host capability version and validated result data. The host submits it as an observation. Request and result kinds both have registered schemas and bounds; client or scene callbacks cannot invent an authoritative result.

| Transition stage | Required behavior |
| --- | --- |
| Request committed | Reserve travel cost once; keep old region state; freeze only the declared scope |
| Native preparation | Host uses ResourceLoader/PackedScene and waits for binding/navigation readiness |
| Matching Ready admitted | Validate destination and actor bindings; commit logical transfer and cost once |
| Failed/Cancelled admitted | Preserve prior committed world; release or retain reservation according to declared policy; return a retryable result |
| Duplicate result | Return/recognize the already committed outcome; apply no second transfer/cost |
| Stale attempt/session/region result | Refuse with WORLD_RESULT_STALE and safe diagnostic; no mutation |
| Deadline | Logical timeout outcome when time advances; host-owned cancellation for a load operation while rules are frozen |

The current global generation remains a full-world instance boundary. Loading an adjacent region should use its own binding revision, not invalidate every entity/order in the world. Pending commands are invalidated only according to their declared scope. Existing global-transition behavior remains the compatibility path until the new protocol is completely wired.

Current saves/restores refuse Transitioning. Keep that rule until a resumable transition checkpoint is actually implemented. A future mid-transition save must include pending attempt/reservation data and support explicit resume or cancellation after load. Merely permitting the phase in the codec is insufficient.
