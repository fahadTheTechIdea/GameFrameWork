# Game types and ScenarioEngine capability research

Researched: 25 October 2026, Asia/Kuwait. Scope: game-type requirements, current C# source inspection, and proposed architecture enhancements. This is desk research and self-review; no new genre implementation, benchmark, playtest or Godot execution was performed.

## 1 Main finding

ScenarioEngine is a useful reusable rule and scenario kernel. Its contracts already support different commands, domain state, facts, objectives, endings, seeded randomness, saves and recorded observations. Adventure, survival, RPG and strategy rules can therefore share the kernel while providing different game layers.

The next important enhancements concern time, persistent world state, host acknowledgements, player-facing queries and progression across sessions. Adding an inventory, hunger meter or combat formula to Core would not address those gaps. Those rules belong in optional game-layer capabilities with one owner per capability.

Game types are overlapping dimensions. Open world describes world structure; survival describes pressures and resource rules; RTS describes control and time; roguelite describes run/progression structure. One game may combine all four. The design should compose capabilities rather than select one mutually exclusive Genre enum or create a different engine for each label.

The implementation proposals are owned by [the detailed SDD packages](../sdd/details/README.md), with shared decisions and sequencing in [chapter 09](../sdd/09_GAME_TYPE_EXTENSIBILITY.md). This report owns the research and comparison evidence.

## 2 Method and claim limits

Primary sources below establish publicly described gameplay or documented APIs. They do not reveal the internal architecture of commercial games. The engine requirements are our engineering inferences from those behaviors, identified here as recommendations. No claim is made that these games use our proposed contracts, that a genre combination is commercially novel, or that our engine already supports a finished game of that type.

Developer-written storefront descriptions are used for game features, not user tags or review scores. Historical technical articles are identified as such. Current documentation describes available techniques; the project's Godot editor/export/runtime combination remains unpinned and untested. No dependency or SDK was changed.

## 3 Game-type comparison

Fit means suitability of the current architectural foundation, not implemented gameplay. Every row still needs its domain layer and presentation.

| Game type and researched example | Behavior evidenced by the source | Requirements inferred for our engine | Present fit and principal missing capability |
| --- | --- | --- | --- |
| Narrative adventure — [80 Days](https://www.inklestudios.com/80days/) | Branching choices and route planning across authored destinations | Persistent decisions, available interactions, resumable conversations, travel costs and content/localization identity | Close foundation for bounded adventures; no dialogue session, action preview or retained zone state service |
| Party RPG — [Baldur's Gate 3](https://store.steampowered.com/app/1086940/Baldurs_Gate_3/) | Character builds, party relationships, choices and varied world interactions | Stable character/item identities, abilities, effects, party ownership, dialogue/quest state and controller-specific views | Domain rules fit ILayerRuntime; no reusable RPG kit, view boundary or encounter-time policy |
| Survival/crafting — [Valheim](https://www.valheimgame.com/) | Procedural exploration, gathering, crafting and construction | Resource transactions, recipe validation, exposure/needs, construction persistence and explicit unloaded-region behavior | Rules fit a layer; persistence and native build/navigation integration need substantial work |
| Open-world sandbox — [Valheim](https://www.valheimgame.com/) | Exploration across a generated world with player construction | Stable world/region/entity keys, retained changes, load/unload lifecycle, generator version and saved generation results | Current zone definitions describe topology; they do not implement a streamed or persistent world |
| RTS with tactical pause — [They Are Billions](https://www.numantiangames.com/TheyAreBillions/) | Orders and construction during pause, production/resources and large attacking groups | Group orders, production queues, fog-of-war views, native navigation, batched observations and measured scale | Closest pacing to current fixed-step API; movement, construction and large-unit performance are unimplemented |
| Turn-based tactics — [XCOM 2 manual](https://cdn.cloudflare.steamstatic.com/steam/apps/268500/manuals/XCOM2_Manual_English.pdf?t=1600177724), tactical layer | Action points, contextual actions, cover and mission play | Turn/phase ownership, action budgets, legal target previews, deterministic resolution, animation-independent commits | A layer can encode turns today, but the fixed 50 ms context is an unsuitable shared time contract |
| Tactical campaign — same manual, strategy layer | Mission selection, headquarters, research, equipment and personnel between missions | Campaign/session separation, declared outcome transfers, durable outcome deduplication and recovery after interrupted continuation | OutcomeId exists; an actual campaign owner and atomic outcome application do not |
| Colony/story simulation — [RimWorld](https://rimworldgame.com/) | Needs, wounds, relationships, crafting, travel and a storyteller that chooses incidents | Jobs/reservations, schedules, relationships, incident policy, simulation of inactive areas and explainable event causes | Pure state transitions fit; job/story modules, incident runtime and population-scale evidence are absent |
| Factory/economic simulation — [Factorio](https://factorio.com/) | Mining, research, infrastructure and automated production | Production/storage flows, networks, job completion, bounded integer quantities and efficient long-running state | A production kit can live in a layer; monolithic state hashing/saving and bounded receipt history need scale work |
| 4X strategy — [Civilization VI publisher guide](https://civilization.2k.com/zh-CN/news/new-player-quick-guide/) | Turn-based empire growth, research, diplomacy and multiple victory paths | Turn policy, faction views, research graphs, territory/economy rules and long-session persistence | Conditions/endings help; no 4X kit, turn contract, diplomacy model or hidden-information views |
| Roguelite/action runs — [Hades developer description](https://store.steampowered.com/app/1145360/Hades/) | Different attempts, changing builds, persistent upgrades and evolving relationships | Separate run/profile scopes, seeded encounter selection, death/return policy and idempotent unlock transfers | Random streams and endings help; run reset and durable profile progress need a separate owner |
| Deckbuilding roguelike — [Slay the Spire press kit](https://www.megacrit.com/press-kits/slay-the-spire/) | Cards, encounters, items, procedural levels and unlockables | Explicit player decisions, draw/discard state, effect ordering, bounded reaction chains and run/profile scopes | A useful unrelated substitution proof; card rules belong in a kit, not Core |
| Institution/life management — [Two Point Campus developer page](https://www.twopointstudios.com/en/games/two-point-campus) | Building a campus, student lives and running courses | Calendar boundaries, schedules, jobs, relationships, facilities and summaries that explain change | Requires calendar/progression and management modules; source inspection used the indexed developer description because full-page access was refused |

Action-heavy games also need a different allocation of work: Godot handles continuous movement, collision, input and animation; ScenarioEngine can handle missions, authoritative resource rules and progression. The present 20 Hz rule step is not evidence of a responsive platformer, shooter, racing game or fighting game. Those require their own native movement/latency acceptance before claiming support.

## 4 What the current source actually provides

| Inspected boundary | Observed implementation | Consequence |
| --- | --- | --- |
| [GameBase](../../src/ScenarioEngine.Core/GameBase.cs) and [simulation contracts](../../src/ScenarioEngine.Contracts/Simulation.cs) | Common Init/Update/Submit/Restore lifecycle; one ILayerRuntime per session | Keep the mother game class; compose domain capabilities behind the selected layer |
| [ScenarioEngine.Advance](../../src/ScenarioEngine.Core/ScenarioEngine.cs) | Monotonic Tick + 1; every context uses TimeSpan.FromMilliseconds(50) | Tick order currently implies a fixed real-time duration; turns, rest and calendar jumps need an explicit advancement contract |
| [Definitions](../../src/ScenarioEngine.Contracts/Definitions.cs) | Static objective graph, exact fact declarations, zones/anchors/exits and one layer ID/version | Dynamic quest instances and optional module compatibility need new contracts; static objective completion is not a repeating job system |
| Same runtime | core.change_zone enters Transitioning; the next frame changes global generation; pending commands are rejected on transition | Useful initial seam, but no typed transition record, pending acknowledgement, region-local revisions or load-failure completion protocol |
| Scenario closure | A matching ending changes the entire session to Ended | Completing a side mission in an ongoing world needs a separate activity/outcome scope; mission completion cannot automatically close the world |
| SessionSnapshot | One layer JSON state; no kernel-owned active region, persistent world or campaign record | A layer can retain extra data itself, but this is not a reusable world/progression capability |
| Snapshot access | Trusted callers can obtain the complete authoritative state | A game UI/network adapter needs a scoped projection so fog of war, secrets and future encounters are not exposed |
| [WireJsonRules](../../src/ScenarioEngine.Contracts/WireJsonRules.cs) | 64 KiB payload, depth 32; 256 observations and 1 MiB aggregate frame bounds | These are safety limits, not a promise of a particular actor count or network throughput |
| Command admission | 128 pending, 32 for one tick, 8-step horizon, 100,000 retained receipts | Use bounded group orders; do not enqueue tomorrow's jobs as distant player commands. Long sessions need an intentional receipt-retention protocol |
| [Save codec](../../src/ScenarioEngine.Persistence/ScenarioSaveCodec.cs) | A single checksummed snapshot, maximum 16 MiB; explicit previous generation through store/service contracts | A persistent large world will need partitioned state and consistent checkpoint references |
| [ReplayService](../../src/ScenarioEngine.Replay/ReplayService.cs) | A portable tape capped at 10,000 calls and 16 MiB; state/output hashing | Appropriate bounded diagnostic replay; continuous campaigns need checkpointed segments |
| [ContractJson](../../src/ScenarioEngine.Contracts/ContractJson.cs) | Canonical full snapshot hashing; integer rule numbers | Rich third-party state requires declared encoding/type conversion; growing histories increase serialization/hash work |
| Host boundary | Commands, observations and world requests are generic; no Godot project exists | Reuse is established at the C# boundary, while physical playability remains unproved |

Derived capacity example: at the proposed 20 steps/second, 10,000 replay entries allow at most about 500 seconds (8 minutes 20 seconds) when every entry is a Step. Start/dispose, commands and other calls reduce that interval; 16 MiB can be reached earlier. This is arithmetic from the configured bounds, not a measured recording duration. Raising the limit alone would not solve long-session memory or replay recovery.

The last recorded engine run has 91 passing tests and a successful Counter demo. Its date, commands, hashes and limits are in [execution evidence](../../artifacts/verification/evidence.json). Those are existing foundation checks, not tests of the enhancements proposed here.

## 5 Shared enhancements and where each belongs

The following priorities are recommendations. The SDD specifies their contracts, state ownership, failure behavior and acceptance gates.

| ID | Enhancement | Owner | Unlocks |
| --- | --- | --- | --- |
| EXT-01 | A real Godot host with native readiness, movement/sensors and UI adapter | Godot host through existing engine contracts | Physical play across all spatial game types |
| EXT-02 | Versioned game/capability profile and trusted optional module composition | Content validation + selected game-layer owner | Reuse without genre branches or a growing mandatory base interface |
| EXT-03 | Explicit advancement policy: fixed real time, player action/turn, calendar boundaries | Kernel logical-time owner; native Timer/signals drive host wakeups | Adventure, tactics, management, rest/travel |
| EXT-04 | Correlated, persisted world-request/transition lifecycle | Kernel protocol + native host adapter | Safe travel, async scene loading, cancellation and retry |
| EXT-05 | Stable world/region/entity identity, retained region changes and declared inactive simulation | Persistent-world owner + Godot resource/navigation adapters | Open world, survival bases, revisitable adventure locations |
| EXT-06 | Read-only controller views and action previews with reasons/revisions | Selected layer query owner through an optional query contract | Readable UI, fog of war, fair previews, later co-op |
| EXT-07 | Campaign, run and profile scopes; scoped completion and atomic outcome application | Game progression owner above individual sessions | Linked scenarios, ongoing worlds, tactics campaigns, roguelite progression |
| EXT-08 | Reusable interactions/narrative with complete checkpoint state | Optional narrative/interaction kit | Conversations, contacts, branching adventure |
| EXT-09 | Dynamic quest/job/incident instances and persisted logical deadlines | Optional owning kit; generic instance handles only where shared | Repeating tasks, crafting queues, colony incidents |
| EXT-10 | Resource/inventory/crafting/production/effect kits with explicit transactions | Optional game-layer kits | Survival, RPG, RTS and economic rules |
| EXT-11 | Long-session checkpoint segments, receipt policy and scalable snapshot hashing | Existing save/replay owners | Large worlds, long campaigns and economic simulation |
| EXT-12 | Reproducible world/encounter generation and measured scale profiles | Registered generator variant + content/world owners | Seeded exploration, roguelike encounters and larger populations |
| EXT-13 | Authoritative multiplayer boundary when selected | Godot native network adapter + trusted controller admission | Co-op; not required for the current offline game |

No universal item system, universal combat formula, generic AI platform, custom physics engine or replacement navigation service is justified by this research. Promote a shared game-layer kit only when it has an actual consumer and substitution evidence; otherwise keep the rule in its game layer.

## 6 Lessons from established tools and technical sources

### Narrative state needs more than a list of flags

[Ink's runtime documentation](https://github.com/inkle/ink/blob/master/Documentation/RunningYourInk.md) describes a standalone C# runtime, choices, state save/load and external functions. It is a candidate for a trusted narrative adapter rather than a reason to invent another branching-language runtime. Do not install it merely to complete a module list; first prove deterministic candidate evaluation and the save contract.

[Yarn Spinner's Godot variable-storage documentation](https://yarnspinner.dev/docs/godot/gdscript/05-connecting-to-your-game/04-variables-and-storage/) explicitly distinguishes saving variables from saving the dialogue's current position. Its described built-in numeric storage also returns numbers as floats. Our design therefore requires a cursor/choice checkpoint or an explicit conversation-boundary save policy, plus typed conversion into integer rule state. This page describes its Godot GDScript integration; it is not proof that our standalone C# host can run the same adapter unchanged.

### World loading and navigation already have native owners

[Godot background loading](https://docs.godotengine.org/en/stable/tutorials/io/background_loading.html) supplies threaded resource requests/status/retrieval. Our missing part is the game-specific lifecycle: which region belongs to a save, which request completed, and when a new world is safe to commit. Resource loading and scene readiness must be distinguished; calling the retrieval method before completion can block.

[Navigation optimization guidance](https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_optimizing_performance.html) supports change-driven path requests and careful geometry/baking. Use native navigation facilities and region changes, with gameplay occupancy/construction data in the owning kit. [AStarGrid2D](https://docs.godotengine.org/en/stable/classes/class_astargrid2d.html) is an existing native option for a grid adapter.

[Large-world coordinate guidance](https://docs.godotengine.org/en/stable/tutorials/physics/large_world_coordinates.html) shows that numeric precision is a separate concern from persistence and streaming. Local region coordinates and measured requirements should guide the host; enabling double precision everywhere is not the default recommendation.

### Time and presentation are different responsibilities

[Godot Timer](https://docs.godotengine.org/en/stable/classes/class_timer.html) supplies scheduled timeout signals and physics/idle processing. It is affected by time scale and can fire only once per process/physics frame. Our logical-time policy must not assume that a callback is a precise wall-clock measurement. Native continuous movement belongs in the documented body/physics callbacks, not in a custom replacement scheduler.

### Replay determinism is not network support

[Factorio's 2015 technical explanation](https://www.factorio.com/blog/post/fff-76) describes lockstep's requirement that peers run the same simulation and the desynchronization risk. Its historical peer-to-peer topology is not a recommendation for our game today. Our engine hashes supplied observations; it has not established identical native physics across machines.

[Godot high-level multiplayer](https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html) already supplies transport/RPC facilities. If co-op enters scope, begin with one authoritative host, verified sender-to-controller mapping and filtered views; record the authority's observations for replay. Networking cannot be claimed from the existing local controller IDs or checksums.

## 7 Recommended implementation sequence

1. Deliver a complete native host and small adventure/survival Lost Soldier operation: travel between revisitable locations, persistent object/resource changes, one resumable contact interaction, failed-load recovery and an ending. This tests the selected game while exercising EXT-01, 04, 05, 06 and the necessary narrative/resource rules.
2. Add a deliberately different turn-based mini-game through the same GameBase. Establish EXT-02/03 and a turn budget, with no repeated real-time idle ticks. This is stronger reuse evidence than a second counter-like plugin.
3. Add cross-scenario progression and bounded repeatable jobs, then a survival/production extension using the same transaction contracts. Verify outcome deduplication and save continuation across the different scopes.
4. Measure representative sessions, introduce checkpointed long-session storage/replay where needed, then qualify larger actor counts and procedural generation. Do not advertise large-scale RTS, persistent open world or production economics from source structure alone.
5. Add co-op only with explicit product scope and network acceptance. Defer specialist direct-action prediction, complex diplomacy, planetary simulation and advanced generative story directors until an actual game needs them.

Within step 1, shared request/view/profile contracts should be designed before their consumers. Implementation should ship each complete consumer journey instead of adding unused interface scaffolding. No implementation of these steps was authorized or performed in this research turn.

## 8 Source access and unresolved evidence

All sources were checked on 25 October 2026, Asia/Kuwait. Sources support the bounded facts cited beside them. Full pages were obtained for most game/tool sources, with the following access limits:

- Civilization's landing page/English quick-guide request returned HTTP 403. Returned indexed publisher-page/quick-guide content supports the narrow turn/economy/diplomacy comparison; the linked localized guide returned English source text. Its Steam page was inspected for product context. The large publisher manual exceeded the web reader's limit and was not used for unseen sections.
- Two Point's full game page returned HTTP 403; only the returned indexed developer description supports the narrow management comparison.
- Hades' FAQ link failed in the reader; the developer-written Steam description supports the run/progression comparison.
- Godot's large-world page is under tutorials/physics; an attempted tutorials/3d path failed and is not a source.

Existing C# tests establish the earlier engine slice only. This research provides proposed contracts and acceptance criteria. It provides no player preference measurement, performance result, new genre compatibility certificate, new library compatibility result or commercial novelty claim.

## 9 Follow-up: a Suikoden-style campaign RPG

Requested comparison researched 25 October 2026. The reference is specifically Suikoden I/II, using their HD Remaster's official descriptions. This is an engine applicability case, not a replacement of the selected Lost Soldier project or authorization to implement a new game.

[Konami's system guide](https://www.konami.com/games/suikoden/rem/1_2/us/en-us/system) documents turn-based party formations, duels, recruitment/home-base functions and distinct war rules in I and II. The second game's army mode is turn-based; an army encounter should not automatically inherit RTS rules. [Konami's series description](https://www.konami.com/games/eu/en/topics/18086/) identifies the large recruitment roster, Rune magic, home-base management and army battles as recurring systems.

The engineering inference is a multi-mode campaign RPG. Exploration, dialogue, headquarters visits and different encounters all consume one persistent campaign. It needs a mode/context boundary as well as the previously proposed campaign/time/world capabilities.

| System needed in our game | Domain ownership and data | Shared framework enhancement |
| --- | --- | --- |
| Recruitment | Character identity, recruitment conditions/status, evidence, availability and granted roles | Typed conditions/quest instances; idempotent changes to campaign state |
| Whole roster versus active party | Persistent members, party selection, temporary story restrictions and support roles | Stable identity and scoped views; no requirement to spawn the whole roster as native actors |
| Headquarters | Available services, assigned characters, growth milestones and authored scene changes | Persistent location state and event-driven native scene binding |
| Party battle | Formation, targets, turn/round state, abilities, equipment and shared inventory | Action-driven advancement; encounter checkpoint and preview contracts |
| Duel | Its own legal choices, opponent intent/visible cues and result policy | Registered encounter rules using the same lifecycle, RNG and result envelope |
| Army battle | Army units/assignments, terrain or matchups, objectives and casualties | A separate encounter rule variant; stable links to campaign characters |
| Story chapters and recruitment windows | Chapter state, gates, reversible/irreversible decisions and recovery routes | Complete narrative saves and graph validation; explicit failure reasons for invalid content |
| Abilities and combined actions | Registered abilities, costs, prerequisites, participants and ordered effects | Candidate-state transactions and deterministic RNG; combat formulas remain domain code |
| Encounter return and endings | Rewards, losses, story consequences, resume location and final campaign closure | Scoped completion, atomic outcome application, mode transitions and campaign saves |

The major new requirement is a persisted active play context: exploration, party battle, duel or army encounter. It stores its identity, registered rules/version, participating character references, phase/state, pending result and return context. Complete a battle and return to exploration without closing the campaign. Save/load must preserve the exact encounter phase and avoid awarding the result twice.

Use one registered campaign-RPG layer with explicit collaborators for recruitment, roster, headquarters, narrative and encounter rule variants. Keep the existing GameBase interface. Do not implement a separate mother engine for each combat mode, replace the game layer at random, or copy a character into independent party/army/headquarters records. A bounded encounter participant snapshot is acceptable when its source revision and result reconciliation are explicit.

A first proof should include a small original roster with both combat and headquarters roles, two revisitable locations, one headquarters upgrade, a party fight, a duel, an army encounter and save/resume across the transitions. A large roster is mainly an authoring, validation and presentation workload; its catalog size is different from simultaneous native actor count.

[The campaign RPG package](../sdd/details/campaign-rpg-layer/README.md) owns the detailed recruitment/headquarters/encounter design; [the play-context package](../sdd/details/play-contexts/README.md) owns the shared mode/result boundary. [Chapter 09, section 14](../sdd/09_GAME_TYPE_EXTENSIBILITY.md#14-multi-mode-campaign-rpg-case) links the integration case. No recruitment, RPG battle, headquarters or mode-switching implementation is claimed.
