# System requirements and operating context

## 1 Purpose and scope

The software has two products at different levels. ScenarioEngine is a reusable local scenario orchestration framework. Lost Soldier is its first game layer: a WWII separated soldier or small group chooses routes, manages supplies and information, and attempts a mission and return to friendly lines. Godot provides the interactive world.

A scenario is a bounded playable operation. A campaign is an ordered/branching sequence of scenarios whose declared outcome deltas carry forward. A game layer defines domain rules; a scenario supplies data using those rules. A data pack cannot introduce a new game layer through arbitrary code.

This describes the current bounded-operation model. [Chapter 09](09_GAME_TYPE_EXTENSIBILITY.md) proposes capability-based extensions for other game types, including ongoing worlds whose individual activities can complete without closing the world. These proposals do not change the selected Lost Soldier scope or the currently implemented contracts.

## 2 User requirements and acceptance

| ID | Source and requirement | Observable acceptance |
| --- | --- | --- |
| SYS-01 | User: reusable ScenarioEngine separated from Lost Soldier | Generic assemblies compile without Godot or Lost Soldier references; fake unrelated layer completes |
| SYS-02 | User: run C# engine with Godot | Exported desktop build loads a scenario, accepts orders and reaches an engine-resolved ending |
| SYS-03 | User: scenario setup through contracts and JSON | A new example scenario loads with no source changes |
| SYS-04 | User: multiple scenarios | Catalog isolates scenario state and loads at least three distinct operation definitions before full-game acceptance |
| SYS-05 | User: multiple endings | Every declared ending has a witnessed runtime route and cause summary |
| SYS-06 | User: extend and mod | Valid data-only pack adds content; incompatible/unsafe pack is refused with diagnostics |
| SYS-07 | User: strategic play rather than prescribed puzzles | Playtests demonstrate at least two viable approaches to major operations, including recovery after detection |
| SYS-08 | Standing rule: Godot native services | Movement, collision, navigation, animation, signals, timers, UI and resources use native facilities |
| SYS-09 | Research: reliable interactions and AI fairness | Rejected actions state why; enemy knowledge has a source and age |
| SYS-10 | Engineering: repeatable simulation | Same initial state, ordered commands and recorded world observations yield identical rule-state hash |
| SYS-11 | Engineering: safe persistence | Interrupted save leaves the last committed slot recoverable; failed save never displays success |
| SYS-12 | Standing rule: current documents | One design package; links and example contracts pass focused checks |
| SYS-13 | Standing rule: operational services | Logs, audit and UI notifications cover the affected journeys with explicit failure outcomes |
| SYS-14 | Product: usable settings and feedback | Menu, selection, order, explanation, save and ending flows work with keyboard and text enlargement |
| SYS-15 | User: research applicable game types and needed engine enhancements | Dated primary-source comparison, current source gap analysis, ownership boundaries, priorities and proposed acceptance gates; implementation remains separately scoped |

SYS-01–08 are user intent or standing constraints. The numeric acceptance targets below are proposed defaults for a prototype. SYS-10 is deliberately scoped to rule replay with the same world observations, not an unsupported promise of identical Godot physics across machines.

## 3 System context

| Boundary | Inputs | Outputs |
| --- | --- | --- |
| Player to Godot host | InputMap actions, menu/slot choice | UI, camera, sound and safe diagnostics |
| Pack source to content owner | Local built-in files or installed ZIP bytes | Validated immutable catalog entries |
| Godot world to simulation | Ordered pose, perception, arrival and collision observations | Movement/sensor requests and committed domain events |
| Layer to engine | Fact projection, action results, layer snapshot | Tick context, generic graph notifications |
| Save owner to local disk | Immutable committed snapshot | Slot generation and audit outcome |
| Scenario Kit to runtime | Declared JSON pack and approved asset roles | Validated content identity |
| Simulator to engine | Same contracts plus synthetic or recorded observations | Trace, ending, hashes and diagnostics |

Baseline is offline single-player desktop. No identity server, remote inference, analytics, advertising, checkout, chat or content hosting is included. Reassess those boundaries if added.

## 4 Principal journeys

### Start or resume

Menu loads built-in catalog first. User packs validate in a cancellable operation; pending packs show status and are not launchable. Selection captures the content digest and layer version. Start creates state and loads the zone; resume verifies save identity before constructing state. Controls enable only after native navigation/sensors are ready.

### Act and recover

The player selects actor(s), previews a route/action cost, and submits an order. The simulation checks current conditions at execution. The UI distinguishes queued, started, completed, cancelled and rejected. Detection opens investigation/search/combat options; it does not automatically end the run. Retreat, contact help and authored capture recovery can preserve progress.

### End and continue

A terminal condition closes the scenario once. The result shows mission, return, companion and contact consequences with contributing facts. Continuing applies a declared campaign delta once, then selects a validated next scenario. A scenario never chooses arbitrary file names or executes a script as a consequence.

### Author and load

The author follows chapter 08, validates schema and references, runs simulator cases and previews the Godot scene. Installation uses a staging copy and atomic catalog registration. A corrupt user pack does not disable built-in content.

## 5 Operating limits and units

| Item | Prototype default |
| --- | --- |
| Party | 1 lead plus at most 2 companions |
| Active actors | 64 total including 24 decision-making enemies |
| Native physics | 60 Hz proposal |
| Rule tick | 20 Hz, integer tick index, 50 ms per step |
| Position | Signed integer millimetres in layer state; adapter conversion uses one scene scale |
| Ratios and probability | Integer 0–1000 per mille |
| Time | Tick durations in state; UTC ISO 8601 for diagnostics only |
| Fixed-point quantities | Integer stock units and milliseconds; no floating point in rule decisions |
| Text | Localization keys and bounded parameters, UTF-8 |
| Pack | 16 MiB compressed; runtime expanded budget must be enforced before arbitrary allocation |

The 64-actor target is a design envelope for the first slice, not a shipping performance claim.

## 6 Quality budgets

| Property | Proposed acceptance target | Measurement |
| --- | --- | --- |
| Rule tick | p95 under 5 ms and p99 under 10 ms at prototype actor count | Release build, 10-minute representative trace |
| Input feedback | Queued/rejected feedback within next rendered frame; accepted rule command within next tick | Host timestamps and trace |
| Save | Freeze snapshot without more than one tick of blocking; disk commit target under 1 s | Slow-disk integration scenario |
| Pack validation | Complete or explicit timeout within 10 s per allowed-size pack | Clean/invalid/adversarial fixtures |
| Recovery | Return to last committed slot; no lost acknowledged commit | Kill/write interruption tests |
| Clarity | 4 of 5 first-time testers correctly explain an observed detection cause | Proposed formative playtest, not run |
| Agency | 4 of 5 testers identify two plausible major approaches without external guide | Proposed formative playtest, not run |

Baseline hardware is unresolved; performance numbers cannot be certified until a measurement machine and target hardware are recorded.

## 7 Product decisions still requiring evidence

| ID | Decision | Working default and consequence | Resolution gate |
| --- | --- | --- | --- |
| OPEN-01 | Exact theater and historical boundary | Fictional operation; no named historical claims | Narrative/content lock |
| OPEN-02 | Final 2D/3D presentation | 2D top-down prototype; coordinate contract remains dimension-neutral | Readability spike before art production |
| OPEN-03 | Campaign size and duration | Three independent scenario targets; only River Crossing fully specified as fixture | Production scope decision |
| OPEN-04 | Capture experience | Authored recoverable capture in slice | Playtest before content expansion |
| OPEN-05 | Godot/.NET pin and renderer | No version pin inferred from online stable docs | Compatibility spike |
| OPEN-06 | Audience, distribution regions, hardware and price | Windows desktop target only; values unresolved | Release planning |
| OPEN-07 | Final balance | Layer chapter values are tuning defaults | Recorded playtest and simulations |
| OPEN-08 | Visual profile and asset family | No generated or approved artwork claimed | Game Arts production workflow |

Proceed with reversible design defaults. Do not label these decisions owner-approved or replace missing evidence with a claim of readiness.
