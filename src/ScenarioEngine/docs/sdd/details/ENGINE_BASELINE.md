# Engine baseline and common design rules

Updated 25 October 2026, Asia/Kuwait. This is the common baseline for all [detailed SDD packages](README.md). Existing C# source is the authority for implemented API shape; each package explicitly labels proposed additions.

## Implemented seams reused by every package

| Boundary | Existing source | Reuse obligation |
| --- | --- | --- |
| Common game lifecycle | [Simulation interfaces](../../../src/ScenarioEngine.Contracts/Simulation.cs) and [GameBase](../../../src/ScenarioEngine.Core/GameBase.cs) | Keep Init/Update/Submit/Restore and one candidate commit; new game rules implement the layer boundary. |
| Definitions and registry | [Definitions](../../../src/ScenarioEngine.Contracts/Definitions.cs) | Use exact layer identity, registered schemas, typed conditions and immutable definitions. |
| Core transaction | [ScenarioEngine](../../../src/ScenarioEngine.Core/ScenarioEngine.cs) | The kernel owns command order, commit, objective/ending state and rule hashes. |
| Content | [Content owner](../../../src/ScenarioEngine.Content/ScenarioContentService.cs) | Extend existing validation/catalog policy rather than creating another loader. |
| Persistence and diagnostics | [Infrastructure contracts](../../../src/ScenarioEngine.Contracts/Infrastructure.cs) | Reuse save codec/store/recovery and IEngineReporter/notification boundaries. |
| Replay | [Replay contracts](../../../src/ScenarioEngine.Contracts/Replay.cs) | Preserve recorded-input/state/output integrity and explicit compatibility. |
| Native integration target | [Godot design](../06_GODOT_RUNTIME.md) | Native movement, collision, navigation, timers, resources, animation, input and UI remain Godot responsibilities. |

## Current constraints and evidence

The current context always carries 50 ms; a matching scenario ending closes its whole session. Zone topology exists, but persistent regions, generic world-result acknowledgements, campaign application, turn policies, game views, narrative runtime and play-context switching are proposals. Wire admission is bounded to 64 KiB payloads, depth 32, 256 observations and a 1 MiB frame. Current saves/tapes are bounded documents; replay permits 10,000 calls/16 MiB and command history retains at most 100,000 accepted IDs. These are observed safety bounds, not genre capacities.

[Existing execution evidence](../../../artifacts/verification/evidence.json) records the earlier 91-test C# run; it establishes neither these proposed packages nor native Godot behavior. [SDD validation evidence](../../../plans/SDD_VALIDATION.json) records only design checks. No new engine implementation, dependency, Godot editor/export combination or performance result is created by these documents.

## Units, state and trust

Persist checked integer quantities and logical durations with explicit units. Commit index is ordering, not automatically a turn/day/millisecond. IDs/revisions are stable domain identities; NodePath/native instance IDs are transient bindings. Immutable snapshots have one authoritative owner per state section. A candidate cannot perform durable/native side effects before commit.

Trusted application code registers implementations and native roles. Public JSON selects validated data only; it cannot load executable code, arbitrary CLR types or scripts. Host-issued controllers supply authority, and views must filter knowledge/ownership. Seeds and iteration order use stable documented encodings; wall time is diagnostic metadata.

## Failure, compatibility and operations

Expected denial has a typed reason and no partial effects. Unexpected rule exceptions follow the kernel's fault/rollback boundary; native and I/O owners report their own failed operations. Use existing IEngineReporter/ApplicationDiagnostics, preserving original cause and a safe UI reference. No failed operation is labelled committed. Cancellation and retries keep command/request/result identity, prevent duplicate consumption and never invent fallback data.

A new schema/profile/state format must be implemented together with its consumer and explicit save/replay interpretation. Unknown versions preserve bytes and refuse; migration writes a verified new generation rather than editing the source. Use explicit previous-save recovery, not silent substitution.

The [persistence/operations chapter](../07_PERSISTENCE_TESTING_OPERATIONS.md) owns shared operational coverage and actual applicability. Headless UI may be not applicable; logging/audit still apply. Online accounts, analytics, payments, remote messaging and hosted content are outside these offline packages unless deliberately selected. Apply the shared owner release checklist to actual features, with truthful legal/asset/dependency provenance.

## Boundary between examples and runnable content

Each definition.schema.json and example is a complete structural design artifact for that package's configuration. It does not extend the current pack document-kind enum or create its proposed layer. Cross-reference/reachability/cost/time rules are recorded as semantic checks, not falsely asserted by the JSON Schema. Install no new package merely to satisfy a folder; ship a complete consumer journey with all necessary dependencies when implementation is authorized.

## Cross-package delivery order

Build the native selected-game consumer and required query/request/profile/world seams first, then prove an unrelated turn-based consumer. Add progression and actual repeatable resource jobs, then measured long-session/generation support. Co-op and specialist direct-action profiles require their own explicitly selected native/network evidence. Each package's delivery chapter narrows this order to its own acceptance cases.
