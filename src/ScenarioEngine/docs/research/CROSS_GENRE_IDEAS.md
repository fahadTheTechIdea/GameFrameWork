# Cross-genre directions for a WWII RTS

Date: 24 October 2026, Asia/Kuwait. Status: researched concept alternatives; no concept selection, demand validation, artwork approval or implementation.

Owner response: Fahad found these alternatives insufficiently distinct. Preserve the recommendation below as research history, not an accepted direction. The subsequent hunting proposal is evaluated in [Hunting competitor audit](HUNTING_CONCEPT_COMPETITOR_AUDIT.md); no final identity is selected.

## Intent

Fahad requested saving the salvage research before exploring another approach: bring an experience from another genre into WWII RTS, analogous to making his soccer-management game about the life of a coach and team. That analogy concerns inhabiting a role and its connected decisions; it does not require farming, a cozy wartime tone, or copying Stardew Valley's visual identity.

Saved prior work: [WWII RTS research](WW2_RTS_RESEARCH.md) and [GoH/BATTLETECH salvage systems](SALVAGE_SYSTEMS_RESEARCH.md). Salvage remains an option; do not assume it must be included in every alternative below.

## Selection principle

Choose a second gameplay loop that changes what the player decides before and after battles. The two loops should affect each other. A decorated menu, unrelated minigame or a large list of disconnected systems is insufficient. All candidates retain real-time command of units; the borrowed references do not dictate turn-based combat. Avoid prescribed puzzle solutions.

## A. Living company: RTS plus character/colony simulation

**Promise:** Lead a company in battle and live with the people who return.

Borrow the sense of individuals producing stories from [RimWorld](https://rimworldgame.com/), whose developer describes moods, needs, injuries, relationships, backgrounds and events. Adapt the connection between personal circumstances and decisions, rather than its entire simulation or fictional setting.

The player's persistent field camp contains a command post, training space, workshop, medical area and living quarters. A small cast of officers and squad leaders has recognizable responsibilities. Larger forces can be represented as squads and crews rather than fully simulating every soldier's private life.

Example: a veteran squad leader can train replacements or lead tomorrow's difficult operation. Training improves the reserve; deployment improves immediate readiness but delays that development. A damaged vehicle, wounded crew and inexperienced replacement make that choice visible in camp and consequential in the next battle. Avoid treating trauma or relationships as arbitrary combat buffs.

Loop: return → inspect people/equipment → choose rest, training and preparations → plan/deploy → fight → bring consequences home. An embodied commander walking through camp is a possible presentation, not a selected requirement. Equivalent quick commands should keep repeated visits from becoming travel chores.

The camp needs its own satisfying decisions and visible change. It must not become a compulsory sequence of food meters, errands or repeated dialogue before every battle. Ordinary routines should run through sensible assignments; players intervene when priorities conflict.

Existing overlap matters: [UBOAT](https://store.steampowered.com/app/494840/UBOAT/) explicitly centers WWII crew life and physical/mental readiness aboard a submarine. [Partisans 1941](https://store.steampowered.com/app/1227530/Partisans_1941/) already combines real-time tactics with camp development, resource management and squad motivation. Merely adding a camp is not new. Our proposed identity would emphasize a regular combined-arms company, its evolving home and leadership decisions, subject to historical scope research.

Risk: animation, characters, writing, state persistence and combat could become two large games. First test one linked leadership choice across two battles, not a full life simulator.

## B. Supply commander: RTS plus production/logistics management

**Promise:** Build the supply network that makes an offensive possible, then command the forces defending it.

[Factorio](https://www.factorio.com/) describes building infrastructure, automating production and protecting the system. Borrow the satisfaction of resolving a visible bottleneck and watching a network work.

Example: an ammunition depot can feed the front through a short exposed road or a longer safer route. The player assigns trucks and escorts, establishes a forward depot, then uses RTS forces to keep the route open. Taking a railway junction changes the network and the next operational choices. Requisition and workshops can establish a meaningful economy without requiring an entire country's industrial simulation.

Loop: assess demand → establish supply routes/capacity → defend or raid infrastructure → exploit the resulting battlefield opportunity → adapt the network. Automate deliveries after setting priorities. Do not make players drive every truck or count every bullet.

Existing overlap: [Foxhole](https://store.steampowered.com/app/505460/Foxhole/) already emphasizes player-produced equipment, logistics, infrastructure and persistent war. It is an MMO, not proof that a single-player commander version is an unoccupied market. Our candidate changes the player responsibility to commanding the network and combat units in one manageable campaign.

Risk: traffic/pathfinding and production complexity could dominate the RTS. First test one visible supply constraint and two defensible route choices, with no huge crafting tree.

## C. Expedition company: RTS plus a branching survival journey

**Promise:** Lead a stranded force across a hostile region, choosing the route and deciding what can be brought along.

[FTL](https://store.steampowered.com/app/212680/FTL_Faster_Than_Light/) describes a crew/ship adventure through a generated galaxy, real-time decisions and pause. Borrow short linked journeys, adaptation and multiple responses to setbacks; a strict reset-on-death structure is optional rather than mandatory.

Example: one route offers a fuel depot but stronger patrols; another is longer and reaches friendly territory with fewer opportunities to repair. The force encountered and supplies consumed change the next route decision. Avoid unknown random events that simply punish an uninformed choice, or a route with a single correct sequence.

Existing overlap: [Last Train Home](https://store.steampowered.com/app/1469610/Last_Train_Home/) already combines a post-WWI journey, real-time combat and crew/resource management. WWII plus a journey is not a sufficient novelty claim. A road-bound mixed force and freedom of operational approach would require testing as specific differences.

Risk: repeated survival pressure can become exhausting; procedural variety needs tactical consequences. First test a short branching operation with two viable ways through and a recoverable setback.

## D. Company sandbox: RTS plus a tactical RPG's campaign freedom

**Promise:** Develop a unit's identity by choosing assignments, people and equipment across a changing regional campaign.

[Battle Brothers](https://store.steampowered.com/app/365360/Battle_Brothers/) describes choosing travel, recruits, contracts, training and equipment in an open-world campaign. Borrow player-directed company development rather than its medieval mercenary fiction or turn-based rules.

Example: headquarters offers reconnaissance, holding a sector or supporting an attack. Available crews, supplies, time and relationships determine the attractive assignment; results change the region and the next offers. Historical regular units need plausible command constraints, rather than unexplained freedom to trade between opposing armies. A fictional campaign can declare different boundaries explicitly.

Risk: ordinary mission selection plus persistent troops already has close overlaps with the games previously researched. The operational freedom and company identity must be substantive. First test whether players choose different assignments because their company has developed differently, rather than choosing the highest reward every time.

## Recommendation and choice criteria

| Candidate | Primary pleasure | Fit with Fahad's coach-life analogy | First uncertainty |
| --- | --- | --- | --- |
| Living company | Attachment and leadership | Strongest | Do camp choices improve battle decisions and make returning enjoyable? |
| Supply commander | Building and protecting a working system | Different, equally coherent role | Is logistics satisfying with automated routine work? |
| Expedition company | Adaptation and a memorable journey | Moderate | Does uncertainty create decisions rather than frustration? |
| Company sandbox | Freedom and a self-developed fighting force | Strong management connection | Is there meaningful freedom beyond a mission menu? |

Recommend investigating Living Company and Supply Commander as the two clearest contrasting directions. The former follows the lived-role quality Fahad described; the latter gives a different source of strategic satisfaction. This is a design judgment, not evidence of market demand. Do not combine all four into the initial product.

Salvage may support a selected concept later: recovered equipment could matter to a living company's workshop or a supply network. It should not displace the concept's main promise by default.

Before art or code, prepare a short scenario and decision flow for the preferred candidate, then test whether target players understand and want that experience. A paper/concept test can assess clarity and interest, but cannot establish combat fun, runtime behavior or purchase intent. After owner direction, follow the existing SDLC specification process and Game Arts confirmed-profile/storyboard/mockup gates.

## Source/evidence limits

All linked sources accessed 24 October 2026; primary developer or developer-authored Steam descriptions. They establish advertised mechanics, not causal explanations for commercial success. No game was run, no user study or sales analysis performed, no final identity approved, and no marketing novelty claim substantiated. Existing similarities are recorded deliberately so recommendations do not rest on imagined gaps.
