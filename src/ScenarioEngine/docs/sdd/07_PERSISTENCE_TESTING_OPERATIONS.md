# Persistence operations and verification design

## 1 Save model

A save stores a committed session snapshot. The current format is defined by [SaveDocument](../../src/ScenarioEngine.Contracts/Infrastructure.cs) and [ScenarioSaveCodec](../../src/ScenarioEngine.Persistence/ScenarioSaveCodec.cs):

| Group | Fields |
| --- | --- |
| Envelope | envelopeVersion 1, randomAlgorithm, payload, payloadDigest |
| Payload metadata | saveFormat 1, engineBuildId, createdAtUtc, snapshot |
| Content | snapshot identity: packId/version, contentDigest, scenarioId, layerId/version |
| Clock/identity | sessionId, seed, controllers, tick, phase, worldGeneration, last observation/event/ingress sequence |
| Engine | objective statuses, ending/OutcomeId, effect keys, queued commands and receipts |
| Layer | Selected layer's validated JSON state; Counter/Signal currently implemented |
| Random | algorithm ID, each stream's state and draw count |
| Integrity | Canonical payload SHA-256 digest; shared snapshot invariants |

Slot IDs and file generations belong to the store; they are not fields in the current save payload. Audit is stored separately. Campaign data, entity allocation, crash-safe journal boundaries and embedded audit receipts are future domain/durability work. Persist integer ticks; UTC is metadata only. Native paths, node instance IDs, transient audio and animation will be recreated by the Godot host. Unknown layer payload schemas are refused during session restore.

## 2 Safe-save sequence

~~~csharp
public interface IScenarioSaveCodec
{
    SaveDecodeResult Decode(ReadOnlyMemory<byte> bytes);
    byte[] Encode(SaveDocument document);
}

public interface IScenarioSaveService
{
    Task<SaveResult> SaveAsync(
        string slotId, SessionSnapshot snapshot,
        CancellationToken cancellationToken);
    Task<SaveDecodeResult> LoadAsync(
        string slotId, CancellationToken cancellationToken);
}

public interface IScenarioSaveRecoveryService : IScenarioSaveService
{
    Task<SaveDecodeResult> LoadPreviousAsync(
        string slotId, CancellationToken cancellationToken);
}
~~~

SaveResult contains Committed and a diagnostic Code. SaveDecodeResult contains a nullable SaveDocument and ValidationReport. The codec performs no disk I/O. ScenarioSaveService coordinates the store, audit and codec. There is no migration method or PrepareRestoreAsync in the implemented contract.

The implemented standalone sequence is:

1. Capture an immutable committed snapshot; refuse Faulted, Disposed and Transitioning phases.
2. Encode its envelope and decode/validate the in-memory bytes before any write.
3. Record SavePrepared; an audit exception prevents the write.
4. LocalFileSaveStore serializes writes per slot within the same store instance, writes a unique temporary file and flushes it to disk.
5. Promote using File.Replace when a current file exists, retaining it as .previous; use File.Move for the first generation.
6. Return a committed result after the store completes. Cancellation is checked before promotion; cancellation after successful promotion does not undo it.

Stores implement ISaveStore. The optional IRecoverableSaveStore adds ReadPreviousAsync; base-only stores remain supported. Paths are bounded slot identifiers under a configured root, and encoded saves are limited to 16 MiB. Store reads treat FileNotFoundException as absence while permission and other I/O failures propagate to the service. Cross-process serialization, post-write reread, power-loss behavior and Godot FileAccess remain unverified future work. A checksum detects corruption; it does not provide authentication.

### Explicit previous-generation recovery

LoadAsync reads only the current slot. LoadPreviousAsync reads only the earlier generation selected by the host, then runs the same codec and shared snapshot validation. It audits a missing, validated or rejected generation before returning; an audit failure prevents release of the recovered snapshot. Neither call promotes, repairs or overwrites files. Session restore still verifies the installed scenario/layer and its schemas.

| Recovery condition | Result |
| --- | --- |
| Store has no history capability | SAVE_RECOVERY_UNAVAILABLE |
| No earlier file | SAVE_PREVIOUS_MISSING; no save |
| Corrupt earlier file | Codec validation diagnostics; SAVE_PREVIOUS_CORRUPT notice; bytes preserved |
| Permission/I/O/audit failure | SAVE_RECOVERY_FAILED; no save |
| Cancellation | OperationCanceledException reaches the caller |
| Valid earlier generation | Verified SaveDocument; current and earlier files unchanged |

Focused tests corrupt the current slot, explicitly recover the earlier snapshot, preserve both generations, distinguish missing/unsupported/unreadable data and refuse an audit outage. The Counter demo writes tick 1 and tick 2, verifies current tick 2, selects previous tick 1, and matches continuation hashes. Crash/fault injection at every write stage remains a release gate.

## 3 Save eligibility and restore

The generic service enforces phase and snapshot validity. The following Lost Soldier checkpoint policy remains a domain/host target: automatic checkpoints at rally points, completed important contact decisions, before irreversible transitions and after an ending; manual saves outside active combat at committed tick boundaries. A paused game retains the safe eligibility of its snapshot; pause is not a combat-save bypass.

Current restore validates envelope, size, payload digest, content identity and installed layer. Shared SessionSnapshotRules reject invalid phases, queue/receipt mismatches, duplicate order sequences and untrusted controllers; pending payloads also satisfy the same UTF-8, depth and duplicate-property policy as live commands. The runtime revalidates operations and schemas against the selected layer. Native target: rebuild scene, actors, anchors and sensors while rule timers remain stopped, publish an initial frame and resume only after readiness. A blocked restore spawn will return RESTORE_SPAWN_BLOCKED rather than teleporting to an invented safe point.

The session factory Restore method receives the selected ValidatedScenario, decoded SessionSnapshot and rebuilt WorldFrame, preserving the saved tick, phase, generation, command receipts and random streams. A paused snapshot remains paused until Resume. The future Godot host uses a new instance token to reject callbacks from the disposed world and reissues pending native movement from the saved action/target, rather than persisting an engine-specific path cache.

Missing pack -> offer locate/install exact content or return to menu. Corrupt save -> list previous committed generation as an explicit recovery choice. Incompatible save -> preserve bytes and explain required format/layer. Do not overwrite an incompatible slot with a newly initialized session.

## 4 Migration and outcome idempotency

Initially API 1 has no previous format. Future migrators are registered source->target functions over immutable documents. Migration writes a new generation after dry-run validation; it never edits the source in place.

Campaign outcome application uses OutcomeId. The committed next-campaign state and applied OutcomeId are saved together. Replaying an ending after a crash returns the existing transition. A scenario cannot award supplies/unlocks twice.

## 5 Replay journal

Journal records content identity, initial snapshot, commands with admission receipts, WorldFrames, PRNG version, committed event sequence and periodic rule-state hashes. Write committed batches with length and checksum; an incomplete tail is ignored with a visible truncated-journal diagnostic.

Exact rule replay requires recorded observations. Abstract route simulations and Godot integration are separate evidence classes. Archive size is bounded by rotating sealed segments at checkpoints, keeping required replay prefixes or a checkpoint base snapshot. A discarded prefix is not advertised as replayable.

### Portable replay evidence

The demo exports replay.json, decodes it and verifies against the same validated content. Focused checks cover first-divergence reporting, altered observations, changed events with identical state hashes, checksum/content mismatch, rejected commands, lifecycle actions, caller JSON disposal and recording-budget cleanup. These are headless rule replay checks; no Godot observation capture has been run.

## 6 Shared operational services

Use Microsoft.Extensions.Logging abstractions where suitable in .NET, coordinated through ApplicationDiagnostics. Godot adapters route failures to that owner rather than creating another logger.

| Capability | Contract/owner | Store or consumer | Failure policy |
| --- | --- | --- | --- |
| Log | ILogger via ApplicationDiagnostics | Bounded local diagnostic files | Expose degraded health; emergency stderr/Godot error once |
| Audit | IApplicationAudit via same owner | Local append records; save/outcome receipt in committed envelope | Refuse protected install/save/outcome acknowledgement if durable receipt cannot be recorded |
| Notification | ApplicationNotifications | Safe local routing and delivery ledger | Bounded retry for local delivery; no false delivered status |
| UI notification | GodotUiNotificationAdapter | Native Control notification panel | Session-scoped, accessible; stale session messages dropped |

No remote email or push channel is enabled. Audit is a local engineering record, not claimed tamper-proof or legally certified. File permissions, rotation limits and user-selected diagnostic export apply. Actual operator retention periods remain undecided; configure bounded storage before first runnable slice.

Envelope fields: eventId, occurredAtUtc, applicationBuild, component, operation, session/correlation ID, target, result, severity, diagnosticCode. Actor attribution is local controller/system/unknown. User-authored prose, absolute personal paths and raw exception stacks are excluded from UI; detailed causes stay in local logs.

### Coverage and consistency

| Event family | Log | Audit | UI |
| --- | --- | --- | --- |
| Startup/layer/content validation | Summary and diagnostics | Accepted/rejected pack identity | Actionable status |
| Command rejection | Debug reason; rate-limited | Aggregate/session receipt as appropriate | Per-order explanation |
| Action/objective/ending | Committed event journal | Checkpoint/outcome receipt | HUD/result |
| Save/restore/migration | Phase and exception | Prepared/committed/failed | Honest progress/result |
| Install/remove pack | Paths redacted | Identity and outcome | Repair/install status |
| Unexpected exception | One primary cause/stack | Failed operation | Reference and recovery choice |
| Notification delivery failure | Independent diagnostic | Failed/suppressed status | Health indicator if usable |

Per-tick movement records belong in the replay journal rather than a toast/audit file for every pose. Crash-durable guarantees apply to acknowledged saves, pack installs and outcomes. Ordinary live unsaved ticks can be lost; the UI never calls them saved.

Exception boundary owner: content load for pack errors, engine Step for rule faults, save owner for persistence and host for uncaught native integration. Inner calls enrich and rethrow; do not duplicate the primary exception at each layer. Emergency recording never recurses into the failing service.

## 7 Security and privacy applicability

Baseline is local/offline. Content validation and local file access are the primary trust boundaries. No account signup, tracking/replay upload, marketing, subscription or hosted user content is implemented or specified. Therefore account-age, consent banner, unsubscribe, renewal and DMCA-host workflow are not currently active product journeys; revisit if those features enter scope. Local diagnostic replay is not a third-party website session-replay SDK.

Licensing/provenance for fonts, assets, code and dependencies remains required before distribution. Do not invent operator identity, rights or retention promises. Apply the shared release requirements at C:/Users/f_ald/.codex/APP_RELEASE_REQUIREMENTS.md when release features become concrete, without copying that checklist here.

Adversarial pack tests: traversal, absolute paths, case collisions, duplicate entries/properties, oversized numbers/depth, cyclic dependencies, script injection, malicious asset role, expanded archive allocation, cancellation and same-ID/different-digest conflicts.

## 8 Focused test and acceptance matrix

The standalone foundation now has executed checks. [Execution evidence](../../artifacts/verification/evidence.json), [test results](../../artifacts/verification/scenario-engine.trx) and [demo output](../../artifacts/counter-demo/result.json) cover generic engine behavior on Windows. Godot-dependent and human checks remain not_run.

Implemented: transactional ticks, typed content, two plugin domains, command deduplication/cancellation, bounded UTF-8/duplicate/depth wire admission, nonmutating invalid-world refusal, objective/ending failure paths, PCG reference vector, deterministic synthetic-input hashes, save integrity, actual file replacement, explicit validated previous-generation recovery, notification failure and save continuation.

Portable JSON command/world replay exports and verification are now implemented in ScenarioEngine.Replay. Not yet implemented/proven: continuous crash-safe append-journal segments, whole-campaign outcome application, native world restoration, public raw archive installation, old-version migrations, power-loss fault injection and release performance budgets. Save storage is currently a .NET adapter behind ISaveStore; Godot FileAccess is the future native adapter. Diagnostics use Microsoft logging plus durable local audit; production file-log rotation/UI delivery remain host work.

The matrix below remains the full target and must be read with those scope limits.

| Check | Requirement | Procedure and required result |
| --- | --- | --- |
| CHK-01 | SYS-01 | Compile generic assemblies without Godot/WWII references; run counter layer to ending |
| CHK-02 | SYS-03/06 | Validate fixture and add another valid scenario without source edits |
| CHK-03 | SYS-06 | Invalid field/fact/reference/effect/unsafe path returns specific refusal |
| CHK-04 | SYS-10 | Capture Godot WorldFrames, replay in .NET, compare every committed hash |
| CHK-05 | SYS-10 | Alter one observation/command and require divergence at known tick |
| CHK-06 | SYS-02/08 | Native navigation test through narrow passage and blocked exit; no teleport |
| CHK-07 | SYS-09 | Enemy hears/sights/reports with known source; no knowledge through disconnected radio |
| CHK-08 | SYS-05 | Run every declared ending through actions in simulator and actual Godot world |
| CHK-09 | SYS-11 | Interrupt save steps and restore last acknowledged generation |
| CHK-10 | SYS-11 | Wrong content/layer, corrupt save and duplicate outcome preserve previous progress |
| CHK-11 | SYS-13 | Disk-full/log/audit/UI failure produces truthful result and cause |
| CHK-12 | SYS-14 | Keyboard selection/order/menu/save/ending and enlarged EN/AR text when supported |
| CHK-13 | SYS-07 | Proposed five-player clarity/agency study with cause reports and route choices |
| CHK-14 | SYS-04 | Three distinct operations each pass content/runtime checks |
| CHK-15 | SYS-12 | Design checker validates links/schema/references and detects negative mutations |
| CHK-16 | SYS-09 | Concurrent treatment/repair orders cannot spend a reserved unit twice |
| CHK-17 | SYS-02/10 | Scene transition load failure/duplicate ready signal charges cost once |
| CHK-18 | Quality budgets | Representative release-build timing, save and memory measurement |

The design checker covers CHK-15 and a subset of CHK-02/03 at the documentation boundary. A passing predicate fixture is not CHK-08 runtime evidence.

## 9 Design checker and evidence

Run from the project root using PowerShell 7:

~~~powershell
./tools/Validate-Sdd.ps1
~~~

The checker uses native Test-Json with local schemas, follows local Markdown file links, resolves the normalized example, and exercises explicit invalid mutations. It does not download packages or run the game.

Current design evidence is recorded by that tool in plans/SDD_VALIDATION.json. Record checked-at UTC, source hashes, check names and actual results. No Git command is required or authorized. Future runtime evidence identifies build/content digests, environment, commands, exit status, traces and covered requirements.

## 10 Delivery sequence and exit criteria

| Slice | Deliverable | Exit criterion |
| --- | --- | --- |
| A | Pinned Godot/.NET/native spike | Timer/order, C# build, navigation and packaged startup evidence |
| B | Contracts, engine, fake layer | CHK-01 plus ordering, rejection and state tests |
| C | Loader and normalized JSON | Valid/invalid fixture refusal; atomic registration |
| D | Save/replay/diagnostics | CHK-04/05/09/10/11 |
| E | Lost Soldier rules and abstract case runner | Transaction/ending cases using explicit world inputs |
| F | Godot River Crossing | Native movement/sensors and all declared endings |
| G | Scenario Kit and three scenarios | No source edits per scenario; author feedback and variants |
| H | Playtest/balance/art/release | Human evidence, approved artwork and distribution gates |

Each slice updates the existing tracker. The user has now authorized implementation: the standalone generic foundation and sample plugins are built and checked. Broader native/gameplay stages still require their own execution evidence.
