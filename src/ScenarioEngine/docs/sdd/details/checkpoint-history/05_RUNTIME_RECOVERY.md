# Checkpoint storage, replay segments and receipt retention: runtime, failure and recovery

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Checkpoint storage, replay segments and receipt retention SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Execution sequence

1. Write immutable sections using standard SHA-256 integrity.
2. Validate all references and create the next manifest.
3. Atomically promote the manifest and retain its previous generation.
4. Seal replay segments with their starting checkpoint/linkage.
5. Retire command history only through a versioned persisted epoch policy.
6. Delete unreferenced sections only after checking every retained manifest.

## Capability-local state model

- **Capturing:** Write immutable sections using standard SHA-256 integrity.
- **SectionsWritten:** Validate all references and create the next manifest.
- **ManifestValidated:** Atomically promote the manifest and retain its previous generation.
- **Promoted:** Seal replay segments with their starting checkpoint/linkage.

~~~mermaid
stateDiagram-v2
    [*] --> Capturing
    Capturing --> SectionsWritten: validated transition
    SectionsWritten --> ManifestValidated: validated transition
    ManifestValidated --> Promoted: validated transition
~~~

These labels describe this package's local lifecycle; they are not new values silently added to SessionPhase. The final local state may publish a scoped result and return to another active context. Only the selected root completion policy ends the whole session. Re-entry/retry needs an explicit new instance/attempt or a legal continuation of the saved identity.

## Transaction and ordering

Validate identity/authority/revision before costs or random draws. Resolve required prior evidence and due boundaries before effects according to the selected layer's declared ordering. Operate on a candidate of the previous immutable state, ask each owning kit for typed changes, validate final invariants, then publish the committed batch. No native animation, menu click or asynchronous callback writes authoritative state directly.

- No acknowledged manifest references missing/unverified sections.
- A discarded replay prefix is not advertised as replayable.
- Compaction never re-enables an old command.
- Current format-1 interpretation remains explicit and supported until a verified migration exists.

## Failure contract

| Trigger | Stable proposed code | Preserved state / recovery |
| --- | --- | --- |
| Storage full | HISTORY_LIMIT | Refuse acknowledgement; keep earlier committed data. |
| Corrupt section/tail | HISTORY_CORRUPT | Preserve bytes; offer explicitly validated earlier checkpoint. |
| Unrecognized epoch/format | HISTORY_INCOMPATIBLE | Refuse reinterpretation and retain source data. |


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

Keep current bounded format-1 saves and tapes valid. Larger limits require measured need and a new supported storage design, rather than editing constants and claiming open-world support.

Proposed world checkpoint protocol: write immutable region/state blobs with standard SHA-256 digests; validate them; write a manifest referencing every required revision; atomically promote the manifest and retain its previous generation. No referenced blob is removed while either retained manifest needs it. An incomplete new manifest leaves the earlier acknowledged world recoverable. Save/replay services remain their single owners; storage variants implement their contracts.

Proposed replay segments carry a validated starting checkpoint, content/profile/generator identities, ordered advance/command/world-result data and previous-segment linkage. A partial tail is diagnosed. Do not claim replay of a discarded prefix. Stop with explicit storage-full/limit status when the configured budget cannot retain required history.

Receipt retention must preserve at-most-once effects. Current sessions retain up to 100,000 IDs and then refuse admission. Alternatives are bounded scenario epochs, or a versioned command epoch/sequence protocol where retired epochs are always refused. Merely deleting old receipts would allow old commands to execute again. Persist the selected epoch policy in saves/replay and test delayed duplicates after restart before enabling compaction.

Full-state JSON cloning/serialization/hashing remains the correctness reference. Profile representative long sessions before introducing section hashes or dirty-section optimizations; use standard cryptographic primitives and compare against full canonical hashes. Do not create a custom ECS, database or hash algorithm solely for a hoped-for performance gain.
