# Gates of Hell Conquest and BATTLETECH salvage research

Date: 24 October 2026, Asia/Kuwait. Scope: desk research and design proposals, not hands-on testing or an approved game specification.

## Owner preferences and current status

Fahad liked combat engineers and salvage strategy, and does not want puzzle-oriented missions with prescribed solutions. The subsequent request is to preserve this research and explore other genre combinations before choosing the game's final identity. Salvage is an explored candidate, not an exclusive commitment.

## What Gates of Hell Conquest teaches us

Conquest links battles through army preparation and progression. The official manual's indexed preparation-menu excerpt shows recruiting, reserves, deployment stages, research and resupply. This supports a force-planning loop beyond isolated battles. The full PDF could not be fetched because it exceeds the web tool's size limit; exact current resource formulas and deployment limits were not verified. [Official manual, indexed excerpt](https://cdn.akamai.steamstatic.com/steam/apps/400750/manuals/CtA_-_GoH_Game_manual_1.0.pdf).

The developer's March 2021 damage-system explanation describes vehicle components, repairable versus destroyed states, limited repair kits and faster repairs by engineers/tank crews. Damage location and surviving equipment therefore matter beyond a single health bar. This is a historical design explanation, not a test of every current unit. [Developer update 97](https://store.steampowered.com/news/posts/?appids=400750&enddate=1620588961&feed=steam_community_announcements).

A June 2021 player discussion describes repairing and crewing captured vehicles to retain them in Conquest. The opening player reports inconsistent outcomes; replies explain expected capture behavior. Treat this as a historical player account, not evidence of a current universal bug or complete present-day capture specification. [Capture discussion](https://steamcommunity.com/app/400750/discussions/0/3073125301396566428/).

An important correction to broad novelty claims: Update 59, version 1.054.0, dated 23 August 2025, already announced fixed map-node locations and persistence of destruction/construction, including fortifications and destroyed vehicles. Its notes explicitly exclude abandoned or damaged-but-repairable vehicles and some other objects. The update also warned about existing-save compatibility. Consequently neither persistent armies nor persistent battlefield destruction is a sufficient distinction for SimpleRTS. [Official update archive, Update 59](https://store.steampowered.com/news/posts/?appids=400750&enddate=1761325255&feed=steam_community_announcements).

The September 2026 Update 70 material is explicitly an open-beta announcement describing a Conquest AI rewrite. It does not establish that the beta is the default stable build or that earlier enemy-scaling descriptions still apply unchanged. Do not design against a supposed current formula without checking the actual selected version. [Official announcements](https://store.steampowered.com/oldnews/?appgroupname=Gates+of+Hell&appids=400750&feed=steam_community_announcements&l=german).

### Enjoyment and friction

In a discussion displayed as 12–14 July, Bandar describes looting and re-equipping as meaningful progression but asks for campaign-menu inventory management and saveable work. Other participants prefer focusing on valuable vehicles, request restoration of intended loadouts, or value detailed equipment persistence. One participant explicitly discloses a supply-regeneration mod after another points out beta differences. This is qualitative evidence of conflicting player preferences, not a representative survey or a verified inventory audit. [Player discussion](https://steamcommunity.com/app/400750/discussions/0/570417684451177456/).

Design inference: retain the decisions about scarce equipment and battlefield exposure; remove repetitive transfers, unclear eligibility and unnecessary post-battle cleanup. Offer detailed inspection without forcing every player into individual-magazine management. A captured vehicle should visibly say whether it is secured for the campaign and what remains necessary.

## What BATTLETECH teaches us

This section concerns Harebrained Schemes' 2018 video game, not tabletop BattleTech, MegaMek/MekHQ, or overhaul mods.

The publisher describes a persistent mercenary company, contracts, repair and maintenance, a customizable base, and a MechLab that replaces damaged weapons using battlefield salvage. The design value is the connection between combat rewards and the player's next deployment. [Publisher overview](https://www.paradoxinteractive.com/games/battletech/about).

Player explanations describe negotiated salvage as selected items plus a random remainder: for example, 1/5 means one player-selected item and five total, not six total. Salvage entitlement and the items actually surviving the battle are separate concepts. A July 2025 question shows that even this notation needs explanation. [Salvage allocation discussion](https://steamcommunity.com/app/637090/discussions/0/601909079151028566/).

Historical base-game player explanations describe one chassis part for center-torso destruction, two for losing both legs, and three for pilot incapacitation with the relevant structure surviving. Excess damage can change the outcome. These are reported baseline mechanics, not an independently executed test, and mods/settings may differ. [Damage and salvage discussion](https://steamcommunity.com/app/637090/discussions/0/1752358461522535673/).

Update 1.1, dated 20 June 2018, documents adjustable assembly-part requirements and whether assembled machines arrive equipped. It also adds running work-order costs, inventory counts, clearer component benefits and warnings, direct store access from the workshop, and scrapping from storage. These are concrete precedents for reducing workshop friction. Avoid assuming three parts or free equipment under every configuration. [Official Update 1.1](https://store.steampowered.com/news/posts/?appids=637090&enddate=1534857293&feed=steam_community_announcements).

A 2019 player discussion contains differing strategies for cash versus salvage, with preferences changing by campaign stage, finances and likely enemy equipment. This suggests testing for dominant choices rather than assuming one setting is always balanced. It does not establish a mathematically optimal strategy in the current game. [Player strategies](https://steamcommunity.com/app/637090/discussions/0/1843566500531355980/?l=spanish).

Design inference: players can enjoy wanting a particular machine, taking risks to preserve it and planning a future roster around it. We should avoid copying opaque fraction notation, arbitrary collection requirements and an economy where every encounter has the same optimal reward choice.

## Comparison and adopt/adapt/avoid decisions

| Decision area | Lesson | Proposed SimpleRTS treatment |
| --- | --- | --- |
| Combat damage | How equipment is defeated can affect future value | Readable mobility, weapon and major-damage states; deliberate tactics improve opportunities without guaranteeing a safe capture |
| Recovery | Battlefield exposure can turn repairs into tactical play | Give recovery teams a protected job, route and withdrawal decision |
| Persistence | Equipment has greater meaning when it affects later operations | Track individual recovered vehicles and their service history |
| Workshop | Information and comparison matter more than manual sorting | Show deployable capability, repair cost, repair duration and supply limitations together |
| Inventory | Players disagree over desired detail | Useful presets and bulk orders, with optional inspection |
| Progression | New machines create anticipation | Make salvage useful through new roles, not only bigger damage numbers |
| Reward allocation | Cash, equipment and future opportunity should compete | Distinct requisition, recovery and refurbishment choices; prevent a universal best reward |
| Consequences | Persistence also preserves bad states and can magnify losses | Explicit recoverable outcomes, interruption-safe saves and a route to rebuild |

## Proposed salvage RTS identity

Working promise: **Command a WWII recovery company. Fight for valuable equipment, bring it home, and build the next operation from what survives.** This is a candidate positioning statement, not a first-of-its-kind claim.

### Three choices around a disabled vehicle

1. **Field repair:** fastest route to immediate use if the damage allows it; consumes supplies and exposes the repair team. Show any temporary limitation before the player commits.
2. **Recover to workshop:** preserves the hull for fuller restoration, but occupies suitable recovery capacity and needs a passable, defended route. The vehicle becomes useful after the strategic repair decision.
3. **Strip useful equipment:** shorter commitment for parts, ammunition or a usable weapon, while sacrificing the complete vehicle opportunity.

Leaving it behind is always a valid fourth outcome. Not every wreck supports all actions. Known impossibilities should be visible, not hidden behind trial and error. Use plausible, researched damage/repair/transport relationships for the eventual faction, theater and date rather than assuming all tanks can tow all wrecks or exchange all parts.

### Make recovery a battle, not an interaction checklist

The player selects an objective and protection plan; assigned units perform the appropriate work. Routes, smoke, suppression, reserve forces and retreat timing supply the decisions. Do not add a repair minigame, a fixed sequence of switches, or a requirement to collect a specific tank to solve a mission.

Some operations can center on recovery; others can be raids, defenses, breakthroughs or withdrawals where salvage is optional. The player must be able to complete a campaign through different force compositions. The game should remain enjoyable when the player chooses to abandon a prize.

Contested recovery can have a clearly communicated threat window, but constant countdowns and infinite reinforcements would undermine thoughtful play. Enemy pressure should come from bounded forces, observable approaches and mission context. Quietly secured territory should allow routine collection without forced waiting or excessive clicks.

### Make the workshop strategic and fast

- Compare deployable roles and expected next-operation readiness, not pages of interchangeable parts.
- Start with a few resource categories, such as supplies, fuel and spare parts. Track exact ammunition/equipment compatibility only where it produces understandable decisions.
- Retain distinct vehicles, crews and chosen loadouts. Automate routine refilling from actual available stock and show shortages honestly.
- Make time strategic: repair work consumes operation turns or opportunity, not mandatory real-world waiting.
- Give a modest reliable force through requisition so unlucky salvage does not make a campaign unwinnable. Scarce recovery remains valuable for specialist roles and flexibility.
- Limit useful recovery capacity and supply support rather than making the enemy secretly mirror every improvement. Include alternate uses for surplus equipment so the optimal plan is not hoarding every wreck.
- Do not turn crew experience into an invisible random-breakdown penalty. Explain known readiness limits and let the player make the tradeoff.

### Narrowest candidate distinction

The most promising experiment is a mission whose central objective is an intact recovery and extraction under changing tactical pressure, connected to a workshop with clear future consequences. The competitor sources examined do not prove that no existing game or mod provides this. Return-to-site recovery of previously abandoned equipment could be a later experiment, but requires campaign ownership, enemy recovery, persistence and save rules; do not add it merely to claim a feature gap.

## A focused first experiment

Use one small map with a road junction, alternative exit route, a disabled medium tank, an ammunition vehicle and a recoverable support gun. Give the player infantry, engineers, basic fire support and one suitable recovery vehicle. Use fictional test conditions until historical scope is selected.

Offer three viable outcomes: repair and use a prize locally; escort a recovery out; take smaller resources and withdraw. Enemy composition, terrain and next-operation needs should change which outcome is attractive. Do not secretly engineer one correct answer.

Proposed checks, none executed:

| ID | Criterion | Evidence needed |
| --- | --- | --- |
| SAL-01 | At least 8/10 intended players distinguish the actions, costs and outcomes without explanation | Observed first-use sessions and recorded misunderstandings |
| SAL-02 | Across the test conditions, every recovery approach is chosen for a reason and none dominates all conditions | Choice logs plus player explanations; inspect mechanical causes rather than requiring an artificial even split |
| SAL-03 | At least 6/10 voluntarily replay with a different recovery plan | Actual offered replay choice, with an equally easy exit |
| SAL-04 | Normal orders complete without silent pathing, attachment or collision failures | Focused mixed-unit traffic, towing, blocked-route and cancellation scenarios in the packaged game |
| SAL-05 | Routine safe-area salvage needs no per-soldier transfers; a normal workshop turnaround has a candidate median below two minutes | Interaction/time observations, separating voluntary detailed browsing |
| SAL-06 | At least 8/10 correctly explain what reached the workshop, what was lost and what is available next | End-of-operation reconciliation and next-deployment observation |
| SAL-07 | One failed recovery leaves an achievable recovery path for the campaign | Execute representative losses and inspect actual force/resource availability |

Ten sessions are formative evidence, not representative market validation. Reconsider targets after the first observations.

## Production implications and limits

If this candidate proceeds, Godot owns native movement/navigation, collision, scenes/resources, input, animation and UI. Game-specific recovery rules, suitable towing combinations, army persistence and tactical decisions need cohesive owners that integrate those services. No custom replacement engine services are justified by this research.

Game Arts would need a readable demonstration of all damage states, attached versus detached towing, engineers working, transported salvage and workshop outcomes at normal gameplay zoom. No images, storyboard, profile or assets have been approved. The SDLC workflow remains research-only; this document creates no code, release, Git or deployment authorization.

Source access date for every linked source: 24 October 2026. No competitor runtime, installed versions, mods, statistical review sample, sales, demand or comparative playtest was inspected. Findings are documented mechanics/announcements and individual reports; proposed improvements remain untested.
