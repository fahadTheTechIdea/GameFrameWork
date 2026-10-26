# SimpleRTS Software Design Description

Status: detailed design with a verified standalone C# foundation; Godot/domain stages remain planned  
Design owner and product decisions: Fahad  
Maintainer: project technical lead  
Reviewed: 25 October 2026, Asia/Kuwait  
Implementation evidence: the separate C# contracts, core, content, persistence, diagnostics and replay projects exist under src. CounterGame runs through GameBase.Init/Update; an unrelated Signal game is covered by plugin checks. IGameFactory creates games for registered layers; asynchronous sources, catalog removal, strict restore invariants, bounded wire admission, explicit previous-save recovery and exported command/world replay are implemented. SDK 10.0.401 is pinned; libraries target net8.0, executable hosts net10.0. No Godot project or Lost Soldier runtime is implemented.

## How to use this design

This SDD explains how the system will work: who owns each state, what each interface accepts and returns, how commands change the world, how content is validated, how failures are recovered and which checks establish correct behavior. The files are chapters of one design, maintained in place. File count and format do not determine design completeness.

The user has selected a reusable C# ScenarioEngine with separate game layers, Godot execution, JSON scenarios, mod extension and multiple scenarios/endings. Numeric tuning, prototype presentation and budgets below are proposed engineering defaults. They are not approved historical facts, measured performance, completed implementation or evidence of a unique commercial game.

## Reading map

| Chapter | Design questions answered |
| --- | --- |
| [System overview](01_SYSTEM_OVERVIEW.md) | Requirements, scope, journeys, quality targets and open decisions |
| [Architecture](02_ARCHITECTURE.md) | Assemblies, capability owners, dependencies, trust and decisions |
| [Scenario engine](03_SCENARIO_ENGINE.md) | Public C# boundary, state, tick transaction, ordering, conditions and replay |
| [Content contracts](04_SCENARIO_CONTENT.md) | Exact fields, schemas, IDs, effects, loading, compatibility and diagnostics |
| [Lost Soldier layer](05_LOST_SOLDIER_LAYER.md) | Actor/resource models, actions, wounds, combat, AI, contacts and progression |
| [Godot runtime](06_GODOT_RUNTIME.md) | Native services, scene tree, movement, sensors, UI, clock and scene changes |
| [Persistence and verification](07_PERSISTENCE_TESTING_OPERATIONS.md) | Saves, journals, recovery, security, operations, traceability and delivery gates |
| [Scenario authoring](08_SCENARIO_AUTHORING_GUIDE.md) | Author workflow, complete example, endings, variants and release criteria |
| [Game-type extension](09_GAME_TYPE_EXTENSIBILITY.md) | Proposed capabilities for adventure, survival, open worlds, turns, campaigns and other game types |
| [Detailed layer and extension packages](details/README.md) | A separate folder and multi-document SDD for each of 14 game layers and 14 shared extensions |

Machine-readable design assets:

- [Content schema](contracts/content.schema.json): structural contract for the example bundle and its definitions.
- [River Crossing example](examples/river_crossing.json): self-contained content fixture with zones, actors, objectives, interactions, endings, localization and expected cases.
- [Design checker](../../tools/Validate-Sdd.ps1): checks links, schema, selected semantic invariants and invalid fixtures. This is documentation tooling, not the game engine.

## Authority and maintenance

Each fact has one owner. The engine chapter owns current tick semantics; the content chapter owns current wire contracts; the Lost Soldier chapter owns its game rules; the Godot chapter owns native integration; the persistence chapter owns current save/durability behavior. Chapter 09 owns cross-package extension decisions and sequencing. Each folder under details owns its specialized proposed requirements, contracts, authoring, runtime and acceptance design. Other chapters link to those owners rather than maintaining competing specifications.

Update these files when design changes. Remove superseded material after checking references. Keep current contract identifiers such as API major and pack version because compatibility depends on them; do not create v2/v3 copies of design files.

[Lost Soldier game design](../design/LOST_SOLDIER_GAME_DESIGN.md) owns player-facing intent and atmosphere. Its duplicated technical sections are replaced by links here. Research remains evidence, including rejected concepts; a research record is not an alternate design.

## Design readiness

The generic foundation and Counter/Signal extension proof are implemented. See the [engine README](../../README.md), [GameBase](../../src/ScenarioEngine.Core/GameBase.cs), [public contracts](../../src/ScenarioEngine.Contracts/Simulation.cs) and [execution evidence](../../artifacts/verification/evidence.json). Production visuals, exact WWII theater, campaign length, hardware minimum, final balancing and Steam release require the decisions listed in chapter 01. They do not prevent designing or testing generic orchestration.

The 25 October game-type research adds proposed extension requirements, not additional engine implementation. The detailed designs are organized as 28 separate packages under details, each with seven Markdown chapters, a strict definition schema and a filled example. Chapter 09 links those owners and preserves common integration decisions. Current engine content schemas and C# contracts remain unchanged. The existing 91-test evidence does not establish turn-based, persistent-world, campaign or new narrative support.

Completion of this design revision means concrete rules and checked examples exist. The implementation is a first working engine slice; broader planned capabilities are not implied by its checks. Completion of the game requires the runtime gates in chapter 07. Schema checks cannot establish playability, AI quality, navigation correctness, ending reachability in the real world or save recovery.

## Evidence basis

- [Competitor framework audit](../research/SCENARIO_FRAMEWORK_COMPETITOR_AUDIT.md): source of lessons on route variation, readable detection, interaction reliability and meaningful consequences.
- [Game-type capability research](../research/GAME_TYPE_ENGINE_CAPABILITY_RESEARCH.md): primary-source genre comparison, current code gaps and priorities behind chapter 09.
- [Lost Soldier quality research](../research/LOST_SOLDIER_GAME_QUALITY_RESEARCH.md): research limitations and good/bad reference behavior.
- [Salvage research](../research/SALVAGE_SYSTEMS_RESEARCH.md): contextual recovery cost, selective looting and preservation of strategic choice.
- [WWII RTS research](../research/WW2_RTS_RESEARCH.md): audience and initial qualitative scope.

Official technology sources and executed compatibility status are recorded in chapter 06. No generic standards conformance or market novelty is claimed.
