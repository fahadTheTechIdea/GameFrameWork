# Game-type extension and capability design

Status: proposed architecture over the existing C# foundation. Updated 25 October 2026, Asia/Kuwait. The detailed designs now live in [one folder per layer or extension](details/README.md). This chapter owns cross-package decisions, sequencing and common acceptance; each package owns its specialized contracts and behavior. No new game/extension runtime is implemented by this reorganization.

[Research and game comparisons](../research/GAME_TYPE_ENGINE_CAPABILITY_RESEARCH.md) own external evidence. Chapters 03, 06 and 07 remain the owners for current engine semantics, native integration and persistence/operations.

## 1 Goals and architectural decision

Preserve GameBase.Init/Update and the current generic engine/layer boundary. Extend a small shared kernel where multiple game types require the same behavior. Put domain rules in replaceable game-layer capabilities and physical behavior in native Godot adapters.

| Alternative | Consequence | Recommendation |
| --- | --- | --- |
| Add hunger, inventory, dialogue, cards, diplomacy and combat to Core | Every game depends on unrelated rules; the kernel becomes a domain owner | Decline |
| Create separate adventure/survival/RTS engine variants | Fixes and saves diverge; mixed genres need a third implementation | Decline |
| Retain one kernel, add explicit shared protocols and compose optional game-layer kits | Shared lifecycle/commit rules survive; kits need disciplined state/schema ownership | Proposed direction |

Genre labels remain catalog metadata. A scenario declares a capability profile such as persistent regions + survival needs + tactical orders. It does not change kernel behavior through a switch on genre. All new contracts require a working consumer and behavioral tests before implementation completion.

## 2 State ownership

Keep GameBase.Init/Update and one registered IGameLayer/ILayerRuntime boundary. Core owns generic commit/lifecycle/receipts/objectives/endings; domain owners own their state; Godot owns native services. [The shared baseline](details/ENGINE_BASELINE.md) maps these seams to actual source. [The package catalog](details/catalog.json) is the dependency inventory. Game labels are metadata, while exact capabilities and compatible definitions select behavior.

## 3 EXT-02: game profiles and optional composition

[Capability profiles](details/capability-profiles/README.md) specifies required host/module identities, exact versions, state writers, validation and rejection before selectable content is registered. Each game-layer package declares the capabilities it consumes; a public JSON definition never installs executable code.

## 4 EXT-03: advancement beyond a hard-coded 50 ms

[Logical time](details/logical-time/README.md) defines fixed/action/calendar advancement, persisted time state, event-boundary interruption and old-format compatibility. Native Timer/signals remain host wakeups; the policy governs saved game semantics.

## 5 EXT-04: acknowledged world requests and transitions

[World requests](details/world-requests/README.md) defines request/attempt/instance identity, scoped revisions, readiness, cancellation, duplicate results and transition recovery. [Native Godot host](details/native-godot-host/README.md) owns the complete native consumer journey.

## 6 EXT-05/12: persistent regions and generation

[Persistent worlds](details/persistent-world/README.md) owns stable world/entity/region identity, retained deltas, tombstones, inactive simulation and region transfer. [Generation and scale](details/generation-scale/README.md) owns pinned manifests/seeds, validation and measured workload tiers. Resource loading and navigation remain native Godot work.

## 7 EXT-06: views and action previews

[Controller views and previews](details/player-queries/README.md) defines filtered immutable output, preview purity, hidden information and revision rules. Trusted raw snapshots remain the save/diagnostic boundary.

## 8 EXT-07: scenario, world, campaign and profile scopes

[Progression scopes](details/progression-scopes/README.md) owns lineage/reset boundaries and durable atomic outcome application. [Play contexts](details/play-contexts/README.md) owns child encounters and returns within a shared campaign. A side activity or battle result does not automatically close its parent world.

## 9 EXT-08/09/10: domain kits and repeatable activities

[Narrative interactions](details/narrative-interactions/README.md), [activity instances](details/activity-instances/README.md) and [resource transactions](details/resource-transactions/README.md) define their individual state/transactions, setup examples, failure behavior and checks. Game-layer packages compose those contracts and specialize their own combat, needs, jobs or economy rules. Introduce shared module machinery only for actual interchangeable consumers; do not add every possible game rule to Core.

## 10 EXT-11: long sessions, saves, receipts and replay

[Checkpoint/history](details/checkpoint-history/README.md) owns manifest promotion, retained sections, segmented replay, command epochs and migration constraints. Raising current constants alone does not establish persistent-world or long-session support.

## 11 EXT-13: optional co-op and specialist action games

[Multiplayer authority](details/multiplayer-authority/README.md) defines native-network integration, trusted peer/controller mapping, filtered replication and resynchronization. Co-op remains optional future scope. Direct-control shooters, racing, platformers and fighting games require their own selected native latency/physics acceptance; the generic rule tick is not that evidence.

## 12 Phasing and proof of reuse

These are implementation recommendations, not completed work or a new product commitment.

| Order | Complete consumer journey | Required enhancements |
| --- | --- | --- |
| 1 | Lost Soldier adventure/survival operation: launch, orders, contact, revisit changed location, recover a failed scene load, save/restore, finish | Native host; required profile/query/request/region contracts; actual contact/resource rules |
| 2 | Unrelated turn-based mini-game: legal action preview, spend action, end turn, save/replay while waiting, reach ending | Explicit advancement policy; kit composition where actually shared |
| 3 | Two linked operations and a repeatable production/survival job | Progress scopes/outcome transaction; activity and resource kit consumers |
| 4 | Long-running persistent world with generated regions and bounded history | Checkpoint manifests, segmented replay/receipt policy, reproducible generation and measured scale |
| 5 | Co-op, 4X/economic depth or specialist action only when selected | Corresponding kit and native/network acceptance |

Generic engine assembly references must remain independent of Godot and all concrete game layers. A new registered layer/content set should run without genre checks in Core. Changes to generic contracts must be driven by both the actual consumer and the compatibility evidence, not by a checklist of potential genres.

The detailed [package index](details/README.md) links the requirements, contracts, authoring, runtime and verification for every proposed slice. The earlier 91-test result covers only the existing kernel. Package runtime gates remain not_run.

## 13 Acceptance and operational evidence

All cases below are proposed and not_run. The existing 91-test result covers the earlier kernel only. Use the same IEngineReporter/ApplicationDiagnostics pipeline for every added capability; UI adapters consume safe scoped notices. Preserve causes in local diagnostics, record durable transition/progression acknowledgements and never turn an unavailable capability into success. Routine simulation events belong in the replay/event stream rather than one toast per update.

| Check | Observable acceptance |
| --- | --- |
| EXT-A01 Profile isolation | Valid profile selects exact capabilities; unknown/missing/cyclic/duplicate-writer modules refuse before session startup |
| EXT-A02 Advancement | Waiting or paused turns consume no time/RNG; failed advance consumes neither time nor resources; fixed-mode old replays retain their meaning |
| EXT-A03 Travel recovery | Duplicate/stale Ready, failed load, cancellation and retry cause at most one transfer and one cost; earlier committed location remains recoverable |
| EXT-A04 World persistence | Loot/build/destroy, leave, save, restart and revisit preserve changes and entity IDs; inactive policy yields its declared result |
| EXT-A05 Projection | Unauthorized view/target cannot expose hidden entities, future random results or other controllers' private state |
| EXT-A06 Preview purity | Preview leaves hash/RNG unchanged; a changed-state command is revalidated rather than trusting the preview |
| EXT-A07 Progression | Apply outcome, interrupt/restart and repeat; resources/unlocks change once and the next operation resumes from the committed revision |
| EXT-A07b Completion scope | Finish a side mission and continue the same persistent world; end a bounded scenario and reject further orders; save/quit never silently starts a new world |
| EXT-A08 Narrative | Save at offered choices and restore exact cursor/choices; interrupted effect commits neither dialogue progress nor resources; missing adapter refuses |
| EXT-A09 Activities | Two instances of one recipe/quest remain distinct; duplicate completion/cancellation cannot duplicate stock or release another job's reservation |
| EXT-A10 Long history | Checkpoint/segment boundary, storage failure, corrupt region and delayed old command yield explicit outcomes with the last acknowledged save recoverable |
| EXT-A11 Generation | Same identity/seed produces identical manifest; old worlds survive a generator upgrade through pinned compatibility or a verified migration |
| EXT-A12 Native scale | Profile frame/rule/path/save behavior at 64 active actors first; explore 256/1,024 only as measured tiers. Publish a supported ceiling from native results |
| EXT-A13 Independent domain | Adventure/survival and unrelated turn-based game run through the same kernel; no concrete kit/genre references enter generic assemblies |
| EXT-A14 Co-op if selected | Duplicate/reordered/disconnected peers, forged ownership and reconnect cannot bypass authority; visible views remain correctly scoped |

Run focused .NET behavioral checks, real filesystem recovery checks and native Godot journeys separately. Scenario JSON/schema validation alone cannot satisfy these gates. No plan or structural test is a claim of finished genre support.

## 14 Multi-mode campaign RPG case

The owner-requested Suikoden I/II applicability case is specified in [the campaign RPG package](details/campaign-rpg-layer/README.md). Its [engine integration decisions](details/campaign-rpg-layer/02_ARCHITECTURE.md#detailed-engine-integration-decisions) preserve the detailed roster, headquarters, party/duel/army context, result and RPG-A acceptance design. Its sibling chapters define the package requirements, DTO/state contracts, authoring, runtime/recovery and delivery. The [play-context extension](details/play-contexts/README.md) is the reusable mode/return boundary, while combat/recruitment/headquarters rules stay in the RPG layer.
