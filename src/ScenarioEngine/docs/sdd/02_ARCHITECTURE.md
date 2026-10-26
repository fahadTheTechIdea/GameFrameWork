# Architecture and capability ownership

## 1 Design decisions

The user selected a C# engine and Godot host with a replaceable Lost Soldier layer. Compiled interfaces now live in ScenarioEngine.Contracts; their behavioral design is specified here. Later Godot/gameplay features remain proposed. The following decisions live here; separate ADR copies are unnecessary.

| Decision | Chosen design and reason | Alternative and consequence | Verification |
| --- | --- | --- | --- |
| DEC-01 | Pure C# orchestration and layer rules allow headless execution | Godot-node-only rules simplify scene scripting but cannot satisfy standalone C# reuse | Fake-layer and build-reference test |
| DEC-02 | JSON DTOs plus registered native assets give authors portable definitions | Resources-only authoring is convenient in Godot but requires that toolchain for all content | JSON scenario without source edits |
| DEC-03 | Godot owns physical movement and sensing, journaled as world observations | Custom physics/pathfinding duplicates native services; pretending native physics deterministic invalidates replay claims | Godot capture then .NET rule replay |
| DEC-04 | Public packs are data-only with built-in asset roles | Arbitrary scenes/scripts provide power but execute user code | Unsafe pack refusal |
| DEC-05 | Single simulation writer and committed event batches simplify state consistency | Independent node mutation creates partial outcomes and order races | Atomic action/exception tests |
| DEC-06 | Small FSM and utility decision owner is prototype AI | Add-on behavior trees add toolchain dependencies before complexity is measured | Explainability and compatibility spike if needed |

## 2 Dependency structure

~~~mermaid
flowchart TD
    Host[Godot Host] --> Core[ScenarioEngine Core]
    Host --> Content[ScenarioContent]
    Host --> Save[ScenarioPersistence]
    Host --> LS[LostSoldier GameLayer]
    Sim[Dotnet Simulator] --> Core
    Sim --> LS
    LS --> Contracts[ScenarioEngine Contracts]
    Core --> Contracts
    Content --> Contracts
    Save --> Contracts
    Host --> Native[Godot native services]
~~~

The composition root registers layer variants. Core invokes IGameLayer through contracts; it never references a concrete game layer. There is one running session and one writer per session. No microservices, database or service locator is required.

## 3 Assemblies and consumers

| Proposed assembly | Capability owner class | Consumers and reason for separation |
| --- | --- | --- |
| ScenarioEngine.Contracts | DTO and interface declarations | Core, layers, host and tooling share one contract |
| ScenarioEngine.Core | ScenarioEngine | Host and simulator share authoritative orchestration |
| ScenarioEngine.Content | ScenarioContentService | Host menu and Scenario Kit share validation/catalog |
| ScenarioEngine.Replay | ReplayService | Portable input recording, codec and replay verification |
| ScenarioEngine.Persistence | ScenarioSaveService | Godot and simulator share envelopes/migration logic |
| LostSoldier.GameLayer | LostSoldierLayer | First registered domain runtime |
| GameHost.Godot | ScenarioGameHost | Native scene integration and composition |
| ScenarioSimulator | SimulatorProgram | Batch runs/replays using the same engine |
| ScenarioKit.Godot | ScenarioKitController | Native editor UI, preview and export |
| ScenarioEngine.Diagnostics | ApplicationDiagnostics and INotificationSink | Shared reporting/audit and user notifications |

The implemented foundation is [ScenarioEngine.slnx](../../ScenarioEngine.slnx): ScenarioEngine.Contracts, ScenarioEngine.Core, ScenarioEngine.Content, ScenarioEngine.Persistence, ScenarioEngine.Diagnostics and ScenarioEngine.Replay. ScenarioSimulator is a runnable tool; Counter.GameLayer is a separate sample plugin. LostSoldier.GameLayer, GameHost.Godot and ScenarioKit.Godot remain planned. Public source contracts take precedence over illustrative signatures below.

## 4 Common game lifecycle

[IGame](../../src/ScenarioEngine.Contracts/Simulation.cs) is the application boundary. [GameBase](../../src/ScenarioEngine.Core/GameBase.cs) supplies the common Init, Update, Submit, CancelQueued, Pause, Resume, Restore and Dispose methods. Derived games identify their registered layer; domain code runs through ILayerRuntime. Init and Update are owned by the base class so a subclass cannot bypass transactional orchestration.

IGameFactory/GameFactory can create that shared lifecycle for any registered layer. Failed initialization disposes its candidate before allowing retry; lifecycle construction/disposal is synchronized. CounterGame demonstrates the base class. PluginTests adds a boolean SignalGame and its layer entirely outside generic engine source. The factory and registry are injected through interfaces; no custom service locator or runtime DLL scanning is used.

## 5 One owner per capability

ScenarioEngine owns lifecycle, command admission/order, tick commits, graph transitions and ending resolution. Its partial files may split these concerns while preserving one owner and registration.

ScenarioContentService owns discovery, pack parsing, schema/semantic validation, content digest, catalog registration and unregistration. A parser helper may be a pure utility; it must not become a second catalog or validation policy owner.

ScenarioSaveService owns save eligibility, codec coordination and explicit previous-generation recovery. The codec owns envelope encoding/integrity; the store owns physical promotion and previous-generation retention. IScenarioSaveRecoveryService and IRecoverableSaveStore extend the base interfaces so stores without history remain valid. Migration and the Godot-native store remain planned; the current .NET file adapter is for the standalone host.

LostSoldierLayer owns domain state and rules. Partial files cover Actor, Inventory, Combat, Ai, Contacts and Campaign without distributing policy among Godot nodes. Immutable definitions and DTOs are data, not service owners.

ApplicationDiagnostics exposes platform log and audit interfaces with one coordinated implementation. ApplicationNotifications owns routing and deduplication; native UI notification controls are thin consumers. Required operational behavior is in chapter 07.

## 6 Native and domain boundary

Godot owns navigation meshes, path queries, avoidance, collision response, raycasts, animation, UI layout, input, resource loading and scheduled native events. Custom C# rules own scenario-specific conditions, action costs, combat outcome policy, resource transactions and ending interpretation.

A pure .NET process has no native world. It consumes a recorded or synthetic WorldFrame. A synthetic adapter is an explicit test environment, not evidence of Godot pathfinding. Runtime movement uses native agents and collision-aware bodies; poses are observations imported on the next rule tick.

## 7 Runtime flow

~~~mermaid
sequenceDiagram
    participant UI
    participant Host as Godot host
    participant Engine
    participant Layer
    participant World as Native world
    UI->>Host: Intent
    Host->>Engine: Enqueue command for next tick
    World->>Host: Pose and sensor observations
    Host->>Engine: Step complete TickInput
    Engine->>Layer: Resolve admitted commands and world frame
    Layer-->>Engine: New layer state and facts
    Engine->>Engine: Objectives then endings then commit
    Engine-->>Host: Committed event batch and world requests
    Host->>World: Apply movement or sensor request
    Host-->>UI: State projection and explanations
~~~

Committed world requests take effect after the tick. Their consequences enter through subsequent WorldFrames; callbacks cannot reenter the current tick.

## 8 Trust and permissions

Trusted application code registers engine, layer and native adapter implementations. Scenario data names supported handlers and asset roles only. The process does not reflect a type name, load an assembly, execute a script or mount a PCK based on untrusted JSON.

A local player's identity is a session controller ID assigned by the host, not a string supplied by a pack. Layer commands are checked for controlled actor, legal target and available action. This is gameplay permission, not online authentication or anti-cheat.

## 9 Concurrency and transactions

Pack I/O and save serialization may run asynchronously on immutable byte buffers. Native scene/resource operations use documented main-thread patterns. The engine and layer run on one host-owned thread. Concurrent Step calls fail with ENGINE_BUSY.

Each command uses a candidate layer state. Preconditions and costs are evaluated together. Success commits all effects; rejection commits none. The whole tick is published only when world observations, admitted commands, timed actions, AI, objective graph and ending evaluation finish. An unexpected exception faults the session at its previous committed snapshot; no event from the failed candidate state is published.

Native effects already observed cannot be undone. The host pauses/frees that scene on fault and reconstructs it from the last committed checkpoint after an explicit restore.

## 10 Lifecycle composition

Startup: configure diagnostics, content service, trusted layer registry, native asset catalog and save service; validate built-in content; show menu. A missing Godot/layer capability disables affected scenarios with a readable reason.

Scenario launch: freeze selected content identity, construct layer runtime, load world scene, synchronize native navigation, capture initial WorldFrame, create initial committed state, enable input.

Shutdown: stop admitting commands, pause native world, finish or cancel owned I/O, save only a fully committed snapshot, dispose session and disconnect signals. Do not start an asynchronous autosave that is abandoned during process exit.

## 11 Extension proof

A fake game layer named test_counter accepts one increment command, exposes an integer fact and reaches an ending at value 3. It uses no soldier/resource/navigation types. Adding it may change registry configuration and test files only. A test fails if generic source references a LostSoldier namespace or domain operation. A separate scenario extensibility check adds a second content file without changing code.

## 12 Packaging tradeoff

The C# host and pure core need compatible target frameworks; select them together in the Godot spike. Native add-ons are absent from the baseline. Trusted PCK/DLC mounting, if later introduced, must be explicit with verified provenance and namespace collision controls; public data packs never use that path.

## 13 Proposed capability-based game-type expansion

[Chapter 09](09_GAME_TYPE_EXTENSIBILITY.md) owns the proposed expansion decision and contracts. Keep one kernel and the existing GameBase lifecycle; compose optional domain kits behind the registered game layer, with native Godot owners for presentation and physical behavior. Shared extensions concern advancement, acknowledged host requests, persistent identity/state, scoped queries and progress across sessions. Genre labels do not dispatch kernel behavior. No additional runtime assembly or dependency has been created for this proposal.
