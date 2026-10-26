# Scenario framework competitor audit

Date: 24 October 2026  
Purpose: revise the scenario-authoring SDD using comparable WWII tactics games and adjacent scenario-authoring evidence.  
Status: desk research; no hands-on playtest was performed.

## Scope

This audit revisits the games previously identified as closest to the lost-soldier concept and asks a narrower question: what must a scenario framework capture so that new scenarios are varied, readable, replayable and technically reliable?

## Comparative findings

### Broken Lines

The official Steam description presents a squad crash-landed behind enemy lines, story-changing choices and multiple campaign endings. A detailed player review describes a 12/21-mission campaign, branched mission selections, multiple endings, morale, permadeath and replay-oriented choices. The same review reports that the actual mission maps and spawns are largely static, so replay changes route selection more than battlefield state.

**Framework lesson:** represent narrative branch state and mission selection separately from runtime encounter variation. A scenario contract must declare which choices change content and which are only text. Each branch needs at least one changed route, encounter, resource, companion or ending condition.

### Partisans 1941

The official page combines real-time tactics, stealth, resource management, base-building, recruitment and diverse missions. Reviews praise flexible approaches, open maps and the mid-campaign increase in mission variety. They also report flat or constrained early missions, unclear objectives, movement/interaction problems, repetitive looting and enemy behaviour that can feel hyper-alert.

**Framework lesson:** a scenario must have an early-play quality gate, not only an endgame content gate. Objective definitions need explicit discoverability and proof-of-location. The authoring contract must model interaction affordances and AI information sources so a player can explain a failure.

### Commandos: Behind Enemy Lines

The official page documents six specialists, 20 missions, sabotage and the requirement to plan, synchronize and bring the squad back alive. Contemporary review evidence praises clear specialist roles, distinct environments, patrol/vision information and open-ended tactical plans. Player criticism focuses on precision difficulty, timing, pathing and puzzle-like repetition.

Community modding evidence shows that even an older game supported mission files, custom maps and tool-assisted authoring, but the tools were community-made and fragmented rather than a stable official content contract.

**Framework lesson:** scenario contracts should expose stable role tags, patrol/vision parameters, objective verbs and map landmarks. Authoring should not require reverse engineering binary files or editing multiple opaque formats. A mission brief must state the intended tactical affordances.

### War Mongrels

The developer and publisher material emphasizes an Eastern Front journey, distinct characters, interactive environments, mission replay and co-op. Review evidence praises atmosphere, story, visuals and level ideas but reports control problems, inconsistent AI, bugs, unclear command execution and insufficient mission variety.

**Framework lesson:** scenario validation must include command execution, save/checkpoint recovery and AI behaviour—not only JSON references and graph reachability. A scenario is not complete if the author can technically finish it but input failures or unreliable AI make the route feel arbitrary.

### OUTGUNNED and Extraktion 1943

The available developer/Steam pages describe a lost soldier behind enemy lines, survival, escape/extraction, sabotage and hideout or resistance-network systems. They are not usable evidence of shipped quality because the available material is announced or in-development content.

**Framework lesson:** treat announced features as precedent evidence only. Do not copy their promised systems into acceptance criteria until a playable implementation exists.

### Last Train Home

This is a World War I game rather than WWII, but its official page documents a complete journey structure: crew roles, real-time missions, resource stockpiling, upgrades, morale and a clear return-home objective.

**Framework lesson:** a scenario can have a strong macro objective without becoming a nation-scale RTS. The contract should separate journey state, crew/companion state, mission state and route state so authors can reuse the pattern without hardcoding a train or a specific theater.

### Adjacent authoring evidence

The Commandos community built map/mission tools and scenario packs, demonstrating demand for extensible mission content but also the cost of fragmented formats. The Second Front manual describes a first-class scenario editor with unique names, dates, maps, forces and battle types, plus Workshop dependencies. These are not direct gameplay competitors, but they validate the need for stable IDs, explicit dependencies, authoring tools and compatibility-aware publishing.

## Revised framework requirements

The SDD must now require the following in addition to syntax, references and graph validation:

1. **Scenario intent contract:** a one-sentence promise, primary pressure, secondary pressure, intended player verbs and declared non-goals.
2. **Replay contract:** which choices alter routes, encounters, resources, companions or endings; text-only variation must be labelled as flavour.
3. **Mission variation contract:** each scenario must include multiple objective verbs, at least two approaches for major operations and at least one operation that changes its available approach after a prior choice.
4. **Information contract:** every enemy alert, objective clue and route estimate has an authored source, confidence and expiry rule.
5. **Interaction contract:** every required interactable declares its affordance, skill/tool tags, feedback and fallback if the interaction fails.
6. **AI fairness contract:** patrol, search, communication and memory rules are testable; no enemy may gain exact knowledge without an event source.
7. **Checkpoint contract:** every convergence point and irreversible decision has a recoverable save state and a player-visible label.
8. **Early-game gate:** the first two operations must demonstrate the scenario’s pressure and at least two viable approaches; authors cannot defer variety until the final third.
9. **Technical route gate:** a clean exported build must load the pack, execute a representative route, save, reload and reach an ending without editor-only dependencies.
10. **Post-release compatibility contract:** pack version, content API, dependencies and save compatibility must be visible to the author and player.

## Quality conclusion

The current contract-and-JSON direction is sound, but it is incomplete without these gameplay contracts. The strongest lesson across the games is not “add more branches.” It is “make the scenario’s tactical promise, information flow, interaction rules, AI knowledge and consequences explicit enough to test.”

## Sources

- [Broken Lines official Steam page](https://store.steampowered.com/app/926580/Broken_Lines/)
- [Broken Lines player framework review](https://steamcommunity.com/app/926580/reviews/?browsefilter=toprated)
- [Partisans 1941 official site](https://partisans-game.com/)
- [Partisans 1941 official Steam page](https://store.steampowered.com/app/1227530/Partisans_1941/)
- [Partisans 1941 PC Gamer review](https://www.pcgamer.com/partisans-1941-review/)
- [Partisans 1941 review with mission and interaction criticism](https://as.com/meristation/2020/11/18/analisis/1605691381_485522.html)
- [Commandos: Behind Enemy Lines official Steam page](https://store.steampowered.com/app/6800/Commandos_Behind_Enemy_Lines/)
- [Commandos: Behind Enemy Lines GameSpot review](https://www.gamespot.com/reviews/commandos-behind-enemy-lines-review/1900-2538454/)
- [Commandos community modding tools](https://sites.google.com/site/commandosmod/downloads)
- [War Mongrels official site](https://www.warmongrels.com/)
- [War Mongrels review evidence](https://www.gamepressure.com/editorials/reviews/war-mongrels-review-battle-weary-tactics/z94bc)
- [OUTGUNNED developer page](https://www.dicesterstudio.com/outgunned/)
- [Extraktion 1943 Steam page](https://store.steampowered.com/app/2631080/Extraktion_1943/)
- [Last Train Home official site](https://lasttrainhome.thqnordic.com/lasttrainhome/INT/win)
- [Second Front scenario editor manual](https://cdn.cloudflare.steamstatic.com/steam/apps/1148490/manuals/SecondFrontManualv.1.05.pdf?t=1675255109)
