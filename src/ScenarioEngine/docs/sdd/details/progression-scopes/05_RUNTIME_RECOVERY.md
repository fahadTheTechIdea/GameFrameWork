# Campaign, run and profile progression: runtime, failure and recovery

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Campaign, run and profile progression SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Execution sequence

1. Validate outcome identity, content and transfer schemas.
2. Look up the outcome ledger and return an existing transition for duplicates.
3. Apply all deltas to one candidate progress revision.
4. Persist the candidate data and ledger together through the save owner.
5. Acknowledge continuation only after durable success.
6. Launch the next session; resume that same revision after restart.

## Capability-local state model

- **OutcomePending:** Validate outcome identity, content and transfer schemas.
- **Validated:** Look up the outcome ledger and return an existing transition for duplicates.
- **CandidateBuilt:** Apply all deltas to one candidate progress revision.
- **DurablyCommitted:** Persist the candidate data and ledger together through the save owner.

~~~mermaid
stateDiagram-v2
    [*] --> OutcomePending
    OutcomePending --> Validated: validated transition
    Validated --> CandidateBuilt: validated transition
    CandidateBuilt --> DurablyCommitted: validated transition
~~~

These labels describe this package's local lifecycle; they are not new values silently added to SessionPhase. The final local state may publish a scoped result and return to another active context. Only the selected root completion policy ends the whole session. Re-entry/retry needs an explicit new instance/attempt or a legal continuation of the saved identity.

## Transaction and ordering

Validate identity/authority/revision before costs or random draws. Resolve required prior evidence and due boundaries before effects according to the selected layer's declared ordering. Operate on a candidate of the previous immutable state, ask each owning kit for typed changes, validate final invariants, then publish the committed batch. No native animation, menu click or asynchronous callback writes authoritative state directly.

- A repeated outcome cannot award resources or unlocks twice.
- One save lineage cannot mutate another campaign.
- Encounter completion and final campaign closure are different scopes.

## Failure contract

| Trigger | Stable proposed code | Preserved state / recovery |
| --- | --- | --- |
| Storage/audit failure | PROGRESS_COMMIT_FAILED | Keep previous revision and refuse continuation. |
| Missing next content | CONTINUATION_UNAVAILABLE | Preserve progress and expose repair; do not start another scenario silently. |
| Conflicting revision | PROGRESS_CONFLICT | Return the committed revision for explicit reconciliation. |


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

| Scope | Examples | Reset boundary |
| --- | --- | --- |
| Scenario/encounter | Current orders, active objective graph and tactical timers | Scenario end/restart policy |
| Run/world | Discovered regions, built shelter, collected items, run inventory | New run/world policy |
| Campaign | Roster, injuries, strategic supplies, completed operations | New campaign |
| Profile | Declared permanent unlocks/settings; no undeclared carryover | Explicit profile reset |

These are configurable scopes, not four mandatory saves for every game. A bounded adventure may need only one session; a roguelite needs a run/profile boundary; a tactical campaign needs campaign/session state. Keep their IDs and schemas explicit.

The existing scenario ClosingCondition/ending closes the whole session. Preserve that bounded-operation behavior. In an ongoing world, a side mission instead belongs to a quest/activity instance with its own outcome; completing it does not set the world session to Ended. Quit/save closes the host's session lifetime while retaining the world checkpoint. Only the declared world/run completion policy closes that scope. If reusable concurrent objective graphs are later required, introduce scoped graph instance IDs/state and outcome identities rather than repeatedly resetting the static scenario graph.

Proposed IGameProgression owns a GameProgressSnapshot and outcome application. A completed scenario returns an OutcomeId, source content/session identity and declared typed delta. Progression validates the delta against its registered schema, checks the applied-outcome ledger, creates a candidate next revision, and atomically saves that revision with the ledger entry before acknowledging continuation. Only then may the host create the next GameBase session.

Repeat the same outcome after a crash and return the existing committed transition. A write/audit failure leaves the previous revision and refuses continuation. A player choosing another save branch gets an explicitly identified progress lineage; one slot cannot accidentally mutate another campaign. Content upgrades and missing destination packs are compatibility failures with preserved bytes.

Do not perform separate uncoordinated writes to supplies, unlocks and the outcome ledger. First implementation can store the small progress aggregate in one atomic document using the existing save-store pattern. Partitioning requires the manifest-commit protocol in section 10. No database or service deployment is required by this offline design.
