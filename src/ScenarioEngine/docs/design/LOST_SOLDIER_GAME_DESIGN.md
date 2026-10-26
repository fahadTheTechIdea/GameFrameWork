# Lost Line Game Design Document

Status: supporting player-facing design input; the authoritative architecture is the [SDD package](../sdd/README.md). This document is not an alternate SDD or implementation contract.  
Date: 24 October 2026  
Target platform: Windows PC through Steam  
Engine target: Godot 4, exact minor version to be locked before implementation

## Purpose

This document defines a focused WWII real-time tactical survival game about a separated soldier trying to reconnect with friendly forces and complete a changing chain of missions. It turns the research findings into testable systems. It is deliberately not a macro-economy RTS, an open-world construction game, a puzzle game, or a conventional squad-management campaign.

The design goal is not to claim that the premise has no precedent. The goal is to avoid the known weaknesses of comparable games: repetitive kill-everyone missions, opaque detection, unreliable AI, mandatory loot chores, excessive inventory friction, shallow character consequences and failure caused by interface problems.

## Product definition

### Player promise

The player is separated from their unit during a fictionalized but historically grounded 1944 European operation. They have incomplete information, limited supplies and a damaged route home. Every journey changes the local situation. The player decides whether to move quietly, risk contact, spend scarce equipment, help people encountered on the way, or take a dangerous shortcut. The campaign ends when the soldier reaches a friendly rally point, is captured, or is killed.

### Genre and camera

- Single-player real-time tactical survival strategy.
- Isometric or high-angle 2D presentation with readable line of sight and cover.
- The player directly controls one lead soldier and may direct up to two temporary companions.
- Real-time movement and combat with pause and a short tactical slow-motion option.
- The campaign is a connected regional map, not an infinite procedural open world.

### Scope target

- First release target: three independent scenarios, each 2 to 4 hours on a first playthrough.
- Scenarios share the core systems but vary the theater slice, starting condition, route problem, enemy pressure and ending logic.
- Each scenario uses 6 to 9 connected zones, with authored landmarks and route alternatives.
- Each scenario contains 8 to 12 authored operations assembled through state-aware objectives.
- 4 to 6 enemy archetypes and 4 to 6 companion archetypes.
- No multiplayer, base construction, research tree or nation-scale economy in the first release.

## Design pillars

1. **Every action advances the return journey.** Scavenging, treating a wound, helping a contact and taking a shortcut must affect route, time, risk or trust.
2. **Danger is readable.** The player can understand what an enemy knows, why a route is risky and why an action failed.
3. **Scarcity creates decisions, not chores.** Supplies are meaningful when the player must choose between competing uses, not when they must search every container.
4. **Soldiers are people under pressure.** A small cast has distinct motives, fears and useful skills; consequences appear in play, not only in dialogue.
5. **The battlefield changes.** Patrols, weather, damaged bridges, civilian movement, friendly contacts and enemy alert alter available routes.
6. **Tactics are flexible.** Stealth, deception, direct combat, evasion and social contact are all valid in the right situation.

## Explicit anti-goals

- No mission whose only meaningful instruction is “kill every enemy and walk to the marker.”
- No enemy that instantly knows the exact location of the player without a visible information path.
- No mandatory container-by-container looting or grid inventory.
- No resource meters that drain constantly without creating a meaningful choice.
- No large cast of interchangeable soldiers.
- No black-box machine-learning behaviour in the player-facing build.
- No procedural generation that produces objectives without authored context.

## Campaign loop

The repeatable loop is:

1. **Read the situation.** Review the paper map, known patrols, weather, time window, wounds, supplies and contact reports.
2. **Choose a route and intention.** Select a safe route, a fast route or a mission detour. Decide whether the goal is to pass unseen, acquire something, help someone or strike a target.
3. **Travel and observe.** Move through the zone, inspect landmarks, listen for patrols and update the map.
4. **Resolve an encounter.** Use stealth, dialogue, a tool, a companion skill, combat or retreat.
5. **Pay a cost.** Spend time, ammunition, medical supplies, trust, stamina or heat to change the situation.
6. **Update the campaign.** The operation result changes the next zone, enemy alert, contact network, injuries and available objectives.
7. **Reach a rally point or continue.** A friendly rally point provides a checkpoint and limited resupply; the player chooses whether to press forward or recover.

The loop is intentionally smaller than a conventional RTS economy. The strategic question is not “How do I grow my base?” It is “Which route and cost give this small force the best chance of reaching the next friendly position?”

## Campaign structure

### Acts

**Act I: Orientation.** The soldier establishes location, finds a damaged map or radio clue and learns which nearby route is watched.

**Act II: The broken network.** The soldier chooses between several contacts: a civilian courier, a scattered friendly patrol, a resistance safehouse and a captured supply depot. Helping one contact changes the availability or safety of another.

**Act III: The crossing.** The front shifts, routes close and the soldier must use accumulated information, trust and equipment to reach the rally point.

### World map

Each zone contains:

- A safe or relatively safe entry edge.
- Two or three meaningful exits.
- One authored landmark that changes the tactical geometry.
- A route table showing estimated time, exposure and known obstacles.
- A small set of encounter slots whose content is selected from authored templates based on campaign state.
- A friendly or neutral contact opportunity when appropriate.

The map displays confidence rather than false certainty. “Patrol seen at 14:00” is different from “road probably clear.” Information decays when the campaign state changes.

### End states

- **Rally:** reach a friendly position with the lead soldier alive.
- **Mission success:** complete the required objective and reach the extraction point; the final score records cost and consequences.
- **Capture:** the scenario declares a recoverable branch or an explicit terminal capture outcome. A recoverable capture opens an escape or interrogation sequence; the result and cause remain visible.
- **Death:** a clear, attributable failure. The latest safe rally checkpoint remains available.

## Scenario and ending framework

The game is a collection of authored scenarios rather than one very long campaign. A scenario is a complete return operation with its own cast, map slice, pressure model, objective chain and ending set. The shared systems make the game learnable; the scenario-specific pressures keep the experience from becoming the same mission repeated in new scenery.

### Scenario data contract

Each scenario is a versioned JSON definition compiled to an immutable C# scenario DTO. The [SDD content contract](../sdd/04_SCENARIO_CONTENT.md) defines its technical shape; Godot Resources hold approved native assets and profiles. The player-facing scenario brief includes:

- Title, date window and historical-fiction boundary.
- Starting separation event and initial equipment.
- Lead soldier profile and available companion pool.
- Zone graph, landmarks and route rules.
- Enemy doctrine and communication rules.
- Primary objective and two or more possible approaches.
- Contact network and trust thresholds.
- Critical decisions and state variables used by the ending resolver.
- Ending scenes, outcome text and post-ending summary.

Scenarios are selectable from the beginning. Completion may unlock optional challenge modifiers or archive material, but the player is not forced to finish one scenario to access the others.

### Scenario identity requirements

Every scenario must differ in at least four of these dimensions:

1. Starting condition: alone, wounded, carrying a document, separated from a vehicle, or responsible for a companion.
2. Primary pressure: patrol density, weather, time window, damaged route, communications failure or civilian evacuation.
3. Route problem: crossing a checkpoint, finding a safehouse chain, repairing a radio path, moving through a flooded area or reaching a moving friendly line.
4. Contact structure: one trusted contact, competing contacts, a false lead or no reliable network.
5. Enemy response: local search, road blockade, radio triangulation, counter-sabotage or a slowly advancing front.
6. Final objective: return with personnel, return with proof, complete a rescue, deliver intelligence or open a route for others.

The scenario cannot be considered distinct merely because it changes the map art or nationality of the soldier.

### Branching model

Branching uses a directed graph with deliberate convergence points. The player sees the immediate consequence of a choice, while later consequences may appear in a route, a contact, a patrol schedule or a companion state.

Each scenario contains:

- 4 to 6 critical decisions.
- 2 to 3 route alternatives after each major decision.
- At least one branch that can be resolved through stealth, one through contact or deception, and one through controlled combat or sacrifice.
- Two convergence points so content remains authored and testable rather than multiplying into an unmaintainable tree.
- At least one late-game state that is only available if an earlier resource, contact or evidence choice was preserved.

Choices must change play. A choice that changes only a line of dialogue is flavour, not a branch.

### Ending resolver

Endings are calculated from a small set of visible campaign variables:

- `objective_status`: failed, partial, complete or complete_with_proof.
- `lead_status`: alive, wounded, captured or dead.
- `companion_status`: who returned, who was lost and who chose to leave.
- `contact_trust`: which network remains willing to help.
- `enemy_heat`: whether the route home is still viable or has become a hunted escape.
- `time_state`: early, late or missed window.
- `evidence_state`: whether the player can prove what happened.

The resolver maps those variables to authored ending scenes. It does not generate prose at runtime. The result should explain which actions produced the ending and which unresolved costs remain.

### Ending families

Each scenario should contain four to six ending families, with variations inside each family:

1. **Clean return:** the lead reaches friendly lines with the primary objective complete and manageable cost.
2. **Costly return:** the lead returns, but a companion, evidence, equipment or contact network is lost.
3. **Mission success without return:** the objective is completed or delivered, but the lead remains missing, captured or unable to cross the line.
4. **Compromised return:** the lead returns with incomplete information, a false report or a damaged network; the immediate goal is met but later consequences remain.
5. **Sacrifice or rescue:** the lead chooses another person’s survival or a larger operational benefit over personal escape.
6. **Failure:** death, capture without escape, or a missed deadline closes the operation. This is still an authored ending with a clear cause, not a generic “you lost” screen.

Not every ending must be equally happy. Every ending must be legible, earned by play and materially different in the final situation.

### Replay and ending discovery

- The ending archive records the scenario, ending family, key decisions and final resource/relationship state.
- After completion, the archive identifies the major variables that differed without revealing every undiscovered branch.
- The player may restart from the scenario opening or from a safe convergence checkpoint for testing a different route.
- Restarting never removes the first result from the archive.
- A “compare outcomes” screen shows changed allies, casualties, objective status and route control—not just a different paragraph.
- Challenge modifiers alter pressure, not story truth: limited saves, harsher weather, reduced intelligence or stronger patrol communication.

### Guardrails against branch bloat

- No more than six ending families per scenario in the first release.
- Critical decisions are represented as state changes that later systems can read; they are not duplicated mission trees.
- Every branch must have one automated state test and one human playtest route.
- A scenario is not complete until every ending can be reached without exploiting a save bug or performing an exhaustive loot sweep.
- A branch is removed if it changes only text, adds no meaningful decision and increases testing cost.

### Multiple-scenario quality bar

The first release succeeds only if players can explain why they want to replay a scenario and why they want to start another one. The reasons should include different pressure, route logic, companion obligations or objective structure—not merely a new ending paragraph.

## Soldier and companion systems

### Lead soldier identity

The lead soldier has:

- Name, unit history and one personal reason for returning.
- Two strengths, one fear and one obligation selected at campaign start.
- Six skills rated from 0 to 3: movement, marksmanship, fieldcraft, first aid, engineering and communication.
- A current loadout with visible equipment and condition.
- A short memory log of major choices and witnessed events.

Traits must modify options, not randomly remove control. A fearful soldier may lose composure near a specific threat but can regain control through rest, a companion action or a successful familiar task.

### Temporary companions

The lead can have up to two companions. Companions are recruited through authored contacts and may leave, be wounded or be lost. They are not a second full management game.

Each companion has:

- One primary field role.
- One secondary utility skill.
- A relationship stance toward the lead.
- A personal red line that affects a small set of decisions.
- A simple command set: follow, hold, scout, cover, interact, treat and retreat.

The player may issue direct orders, but companions handle routine movement. They do not wander into danger without a visible reason.

### Health, wounds and recovery

Health is not a single hit-point bar. The model has:

- **Condition:** short-term ability to move and fight.
- **Wound:** a named injury that can impose a specific limitation.
- **Pain:** temporary accuracy and stamina pressure.
- **Recovery time:** how long the wound remains relevant.

Field bandages stabilize condition. A proper friendly aid station removes or reduces a wound. Wounds can persist across zones, but the player always sees the expected consequence before committing to a route.

### Fatigue and stress

Fatigue reduces sprint time and increases noise. Stress changes perception and decision quality; it does not secretly change the rules. Rest, food, safety, companion reassurance and successful progress reduce stress. Shock events, civilian harm, prolonged pursuit and watching a companion fall increase it.

## Resource system

Resources are grouped by decision purpose rather than by a long inventory list.

| Resource | Sources | Sinks | Player decision |
| --- | --- | --- | --- |
| Ammunition | Starting kit, selected caches, friendly contacts | Firearms and suppression | Fight now, evade now, or preserve firepower for a checkpoint |
| Medical supplies | First-aid caches, aid stations, limited trades | Stabilizing wounds and companions | Treat the lead, save a companion, or risk continuing |
| Sustenance | Rations and water points represented as supply units | Travel days and recovery | Take a longer safer route or spend supplies to move quickly |
| Tools | Engineer kits, workshops, battlefield salvage | Opening routes, repairing radio/vehicle, disabling obstacles | Convert a tool into time, information or access |
| Batteries and fuel | Caches, vehicles, contacts | Radio, torch, vehicle movement | Use reliable communication or remain silent and conserve power |
| Intelligence | Maps, observation, radio reports, contacts | Route confidence and mission planning | Spend time learning the route or move with uncertainty |
| Trust | Help, trade, honesty and fulfilled promises | Access to shelter, supplies and information | Build a safer network or take resources by force |
| Heat | Enemy observations, noise, bodies, sabotage | Decays through time, concealment and false trails | Accept attention for a fast objective or lower the profile |
| Time | Campaign clock and local windows | Patrol cycles, weather, rendezvous deadlines | Wait for darkness or move before a contact disappears |

### Resource rules

- No resource is allowed to matter only because the game refuses to explain it.
- Every resource display includes current amount, next likely use and a warning threshold.
- Caches offer a small number of high-value decisions instead of a hundred low-value containers.
- Salvage is contextual: a wreck may provide a tool, ammunition or vehicle part, but recovery takes time and creates exposure.
- Encumbrance uses three clear bands: light, burdened and overloaded. It does not use a grid puzzle.
- The game never requires a full-map loot sweep to stay viable on the intended difficulty.

## Mission system

### Objective verbs

Operations are assembled from at least two different verbs:

- Reach a location.
- Recover a person, document or radio component.
- Escort or guide a vulnerable contact.
- Repair or activate a communication point.
- Cross a checkpoint without raising a full alert.
- Create a diversion or false trail.
- Disable a vehicle, bridge or supply route.
- Confirm information and return with proof.
- Rescue a wounded friendly soldier.
- Choose whether to help, trade with or avoid civilians.

### Mission composition

Every operation has:

- A visible primary objective.
- A hidden but discoverable complication.
- At least two viable approaches.
- A cost for haste and a cost for caution.
- A consequence that affects the next route or contact.

The objective generator selects only from authored combinations that have been tested. It may vary patrols, weather, entrances and contact state, but it cannot invent an untestable mission.

### Failure and recovery

Failure is classified in the event log as one of:

- Detected too early.
- Path blocked or route misread.
- Equipment insufficient.
- Companion incapacitated.
- Deadline missed.
- Player chose an escalation that exceeded available strength.

The player can retry from a local tactical checkpoint during an operation, or from the last safe rally point after a campaign failure. A checkpoint never hides the cause of failure.

## Exploration and navigation

### Routes

Each route exposes four estimates:

- Travel time.
- Visibility or concealment.
- Patrol exposure.
- Resource cost.

Estimates may be uncertain, but the uncertainty is labelled. Reconnaissance can improve confidence.

### Landmarks

Landmarks are tactical anchors: a ruined barn, bridge, railway cutting, church, hedgerow, field hospital, factory yard or river crossing. Each landmark has a small authored interaction set and a recognizable silhouette.

### Cover and concealment

- Cover stops or reduces incoming fire.
- Concealment reduces detection but does not stop bullets.
- Noise travels through a simple local propagation model.
- Bodies, open doors, lights and vehicle movement create evidence that can raise heat.

## Combat system

Combat is real-time, pauseable and intentionally short. The player should often be able to withdraw.

### Core actions

- Move and take cover.
- Aim and fire.
- Suppress an area.
- Throw a limited grenade or smoke item.
- Treat an ally.
- Interact with a route object.
- Order a companion to hold, cover, scout or retreat.

### Weapon model

Weapons use a small number of readable categories: rifle, submachine gun, pistol, machine gun, grenade and improvised tool. Each category communicates range, noise, ammunition use and suppression value. Weapon statistics are not hidden decimal spreadsheets.

### Escalation model

Enemy awareness moves through:

1. Unaware.
2. Suspicious: heard or saw evidence.
3. Searching: investigating a last-known location.
4. Alerted: local units coordinate.
5. Lockdown: roads close, reinforcements or a hunt begin.

The player sees the current state, the evidence that caused it and the actions that can lower it. Enemies may share information through radios, runners or line of sight; the player can interrupt those channels.

## Enemy AI design

### Archetypes

- Sentry: observes a post and reports suspicious evidence.
- Patrol: follows a route and reacts to local evidence.
- Officer: coordinates nearby units and changes search priorities.
- Tracker: follows footprints, sound or recently disturbed objects.
- Vehicle crew: controls a road or open area but is vulnerable when isolated.
- Reinforcement: arrives only through a visible communication or schedule.

### AI rules

- No enemy teleports knowledge to another enemy.
- Every alert has a source, timestamp and confidence.
- Searchers investigate evidence, not the player’s exact position.
- AI uses a bounded memory and forgets stale information.
- A failed path request produces a fallback state and a debug reason instead of freezing.
- AI decisions are throttled; no crowd agent evaluates expensive logic every frame.

## AI library and Godot integration decision

### Godot-native foundation

Godot’s `NavigationAgent2D` and `NavigationAgent3D` provide pathfinding, path following and agent avoidance through the engine’s NavigationServer. The official documentation describes them as optional helper nodes for these navigation tasks. We will use the native navigation system before adding a third-party pathfinding library.

Decision-making remains a project-owned, deterministic layer:

- A finite-state machine handles major states such as patrol, investigate, search, flee, treat, regroup and surrender.
- A small utility scorer chooses among valid actions within a state.
- The pure C# layer state stores threat, last-known position, cover, stamina, ammunition, mission goal and communication status. Godot Resources may hold authored profiles but do not duplicate authoritative blackboard state.
- Native Timer and evidence signals provide decision pulses; the C# layer evaluates the game rules when those pulses arrive.
- The selected action records a reason string for debugging and for the player-facing “why did they do that?” panel.

### Candidate addons researched

| Option | Evidence | Strength | Risk | Decision |
| --- | --- | --- | --- | --- |
| Native Godot FSM plus utility scoring | Built from Godot nodes, signals, Resources and NavigationAgent | No dependency; fully explainable; easiest to test and version | More project code if behaviour grows large | **Required baseline** |
| Beehave | [Official repository](https://github.com/bitbrain/beehave) documents a Godot 4 behavior-tree addon, GDScript workflow, tests and MIT license. Its documentation explicitly notes that behavior trees can be overkill for simple AI. | Pure GDScript, visible tree structure, editor workflow and debugger | Still requires authoring actions; tree complexity can hide simple rules; Godot version must be pinned | **Evaluate after the vertical slice if three or more archetypes become hard to maintain** |
| LimboAI | [Official repository](https://github.com/limbonaut/limboai) documents Godot 4 behavior trees plus state machines, editor/debugger, GDScript tasks and MIT licensing. The [Godot Asset Library](https://godotengine.org/asset-library/asset?user=limbonaut) lists versioned releases for specific Godot versions. | Strong editor tooling and suitable for complex hierarchical behaviour | C++ GDExtension compatibility and version-lock risk; more machinery than the first slice needs | **Candidate for later, only after engine-version lock and a compatibility spike** |

### Recommendation

Start with native Godot FSM plus utility scoring and NavigationAgent. Do not add an AI addon to solve a problem we have not measured. If the vertical slice has patrol, investigation, tracking and companion behaviours that are difficult to inspect, run a small Beehave spike first because its GDScript and MIT workflow are simpler to audit. Consider LimboAI only if the project genuinely needs hierarchical behavior trees and its exact Godot version is supported.

No machine-learning model, online inference service or LLM is proposed. The game needs repeatable, debuggable decisions, not unpredictable text or policy generation.

### Example utility decision

When a patrol hears a shot, valid actions might be investigate, call radio, take cover, continue route or retreat. Each action receives a score from authored inputs:

`score = urgency + evidence_confidence + ally_support - distance_cost - risk - fatigue_penalty`

The AI records the top three scores and the chosen action. Designers can inspect why an officer called a search instead of returning to route. The formula is a design aid, not a claim of realistic human cognition.

## Friendly contacts and trust

Contacts are not a political simulation. They are local people and units who can provide information, shelter, supplies or route access.

- A contact has trust, safety and availability.
- Trust rises when the player keeps promises, shares scarce supplies or protects the contact.
- Trust falls through betrayal, unnecessary violence or arriving with high heat.
- A contact can give partial or outdated information; the map records confidence.
- Contact outcomes affect routes and resources, not just a final morality score.

## Progression

Progression improves capability without turning the soldier into a superhero.

- Skill growth comes from completing relevant actions and surviving meaningful operations.
- New options are field techniques, contact knowledge and equipment familiarity.
- A skill increase must unlock a route or approach, not simply add invisible damage.
- There is no infinite level grind and no random loot rarity ladder.

## Save and campaign persistence

Godot native file APIs provide the storage adapter. The SDD persistence chapter owns the committed snapshot, generation, migration and recovery design; scene/resource serialization alone is not the save contract.

Save data includes:

- Campaign seed and current act.
- Zone state, patrol schedules and heat.
- Soldier condition, wounds, fatigue and equipment.
- Companion status and trust.
- Resource quantities.
- Contact trust and availability.
- Completed, failed and abandoned objectives.
- A deterministic random-state value for reproducible debugging.

Autosave occurs at rally points, after major contact decisions and before entering a new zone. Manual save is available outside active combat. The player can always identify the current save location and the last irreversible decision.

## UI and information design

The interface must answer three questions quickly: what do I know, what can I do, and what will it cost?

### Persistent HUD

- Lead condition, wound and fatigue.
- Current ammunition, medical supplies and sustainment.
- Heat and enemy awareness summary.
- Current objective and next known rally point.

### Tactical overlays

- Sight cones only where the player has current information.
- Sound or evidence radius after a noisy action.
- Cover quality and route preview.
- A compact event log with source and timestamp.
- AI explanation on hover or pause: “searching last-known position because radio report was received 4 seconds ago.”

### Map screen

- Route alternatives with time, exposure, known obstacles and confidence.
- Contact markers separated from confirmed military information.
- A clear distinction between “unknown,” “suspected” and “confirmed.”

## Audio and art requirements

These are production requirements for a future Game Arts profile, not generated assets.

- Silhouette-first landmarks and soldier poses so cover and roles are readable at the gameplay zoom.
- Muted wartime palette with high-contrast interactable elements; avoid dark scenes where enemies and cover merge.
- Distinct sound layers for footsteps, vehicle engines, radio traffic, distant fire and search states.
- Radio distortion and environmental occlusion communicate information uncertainty without hiding required feedback.
- Character portraits and small authored details should support identity without becoming a life-simulation layer.

## Technical design authority

The [SDD index](../sdd/README.md) owns the implementation design. This game-design document describes player-facing intent and references that design:

- [Architecture and ownership](../sdd/02_ARCHITECTURE.md): generic ScenarioEngine, replaceable layers and shared capabilities.
- [Engine behavior](../sdd/03_SCENARIO_ENGINE.md): authoritative state, ticks, ordering, conditions and observation-based replay.
- [Content contracts](../sdd/04_SCENARIO_CONTENT.md): JSON fields, loading, trust tiers, validation and compatibility.
- [Lost Soldier layer](../sdd/05_LOST_SOLDIER_LAYER.md): domain state, resource/action transactions, combat, AI and consequences.
- [Godot integration](../sdd/06_GODOT_RUNTIME.md): native movement, collision, timers, scenes, UI and sensing.
- [Persistence and verification](../sdd/07_PERSISTENCE_TESTING_OPERATIONS.md): save recovery, audit, traceability and evidence.
- [Scenario authoring](../sdd/08_SCENARIO_AUTHORING_GUIDE.md): workflow, complete River Crossing fixture and scenario acceptance.

The former technical/authoring sections are consolidated in those chapters. JSON is the portable scenario contract; Godot Resources and scenes supply trusted native presentation. This document does not maintain a competing Resource-only scenario format.

## Vertical slice

The first playable slice must contain:

- One zone with three exits.
- One lead soldier and two temporary companions.
- Three enemy archetypes: sentry, patrol and officer.
- Three objectives: recover a radio part, cross a checkpoint and escort a wounded contact.
- Five resource categories: ammunition, medical supplies, sustainment, tools and heat.
- One dynamic event: a patrol reroute after a radio report.
- One rally checkpoint and one capture recovery branch.
- Native NavigationAgent pathfinding and the explainable FSM/utility controller.

### Slice acceptance checks

- A new player can identify why they were detected without reading external documentation.
- At least two objectives can be completed through different approaches.
- No mandatory full-map loot sweep is needed to complete the slice.
- Enemy search behaviour never jumps directly to the player without a recorded information source.
- A blocked path recovers or reports a clear fallback state.
- The player can finish the slice without firing every weapon they carry.
- A wound, contact decision and heat change are visible in the next route choice.
- Playtesters describe the strongest moment as a decision under pressure, not as waiting for a patrol puzzle.

## Risks and mitigations

| Risk | Early signal | Mitigation |
| --- | --- | --- |
| The game becomes a slow stealth puzzle | Testers wait for patrol cycles and reload instead of improvising | Add reversible retreats, deception, short combat windows and route costs |
| Resources become chores | Players loot every object before moving | Use curated caches, grouped supplies and strong route decisions |
| AI feels unfair | Testers cannot explain detection or search | Information-source ledger, visible awareness states and deterministic debug replay |
| Story is stronger than play | Testers remember dialogue but not tactical choices | Tie every narrative decision to route, resource, contact or injury state |
| Scope expands into an RTS economy | New systems require base construction or nation-level production | Keep the return journey and local contact network as the only strategic layer |
| Godot addon mismatch | Plugin cannot load under locked engine version | Native baseline first; run a compatibility spike before adopting Beehave or LimboAI |

## Open decisions before implementation

1. Exact WWII theater and historical-fiction boundary.
2. 2D or 3D presentation after a readability test.
3. Whether the player begins alone and recruits companions, or begins with a two-person separation event.
4. Final campaign length and number of zones.
5. Whether capture is a playable branch or a checkpoint setback.
6. Locked Godot minor version and target hardware range.
7. Game Arts visual profile, storyboard and approval gate.
8. Public data-only contract fields and the first Scenario Kit workflow.

## Research sources

- [Godot NavigationAgent documentation](https://github.com/godotengine/godot-docs/blob/master/tutorials/navigation/navigation_using_navigationagents.rst)
- [Godot exporting packs patches and mods](https://docs.godotengine.org/en/stable/tutorials/export/exporting_pcks.html)
- [Godot runtime file loading and saving](https://docs.godotengine.org/en/stable/tutorials/io/runtime_file_loading_and_saving.html)
- [Godot JSON documentation](https://docs.godotengine.org/en/latest/classes/class_json.html)
- [Beehave official repository](https://github.com/bitbrain/beehave)
- [LimboAI official repository](https://github.com/limbonaut/limboai)
- [Godot Asset Library LimboAI releases](https://godotengine.org/asset-library/asset?user=limbonaut)
- [Commandos quality research](../research/LOST_SOLDIER_GAME_QUALITY_RESEARCH.md)
- [Lost soldier precedent screen](../research/LOST_SOLDIER_RETURN_MISSION_SCREEN.md)
