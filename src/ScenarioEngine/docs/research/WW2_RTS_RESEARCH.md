# WWII RTS: player appeal, failure modes and proposed direction

Research date: 24 October 2026, Asia/Kuwait. Stage: discovery/research only.

## Decision brief

The strongest direction to investigate is a readable, squad-based WWII RTS in which infantry, tanks and support weapons win through positioning, suppression and cooperation. Give players an engaging solo game, reliable commands and reasons to replay before expanding the roster or adding competitive services.

This is a design recommendation, not a proven market gap or an approved specification. WWII is a setting; the product still needs a distinctive reason to choose it over established games. Proposed promise: **lead a persistent combat company through short operations where preserving veterans and choosing the next objective changes the next battle**.

## Scope and evidence limits

- Inspected Steam listings, visible English Steam Community reviews/discussion, developer explanations and official Steamworks/Godot documentation. Sources and retrieval date are below.
- Reviews are individual reports and preferences, not reproduced defects. Developer statements establish advertised intent or announced changes, not independent runtime verification.
- Community pages are dynamic, selected by Steam's weekly helpfulness/default presentation. This is a qualitative convenience sample, not a representative survey or frequency analysis. Some visible positive reviews contain little useful explanation; they do not establish a mechanism of enjoyment.
- An attempted structured Steam review API sample returned non-JSON responses on all 20 queries. **Zero reviews were retrieved through that method.** The failed collection record is retained in `evidence/steam_review_sample.json`; no statistical conclusions use it.
- No competitor game was played in this session. Sales, revenue, active-player retention, audience proportions and willingness to pay remain unknown. Review scores and reviewer hours do not establish those quantities. Search caches and live pages can differ; the snapshot below uses the explicitly identified source populations.
- The target currently contains empty `docs`, `plans`, `src/Godot` and `src/.NetEngine` folders, with no observed game project, game code, saved art settings or integrated framework. Research does not establish game readiness.

## Competitor signals

Snapshot accessed 24 October 2026. Percentages are rounded as displayed by Steam; all-time English and recent all-language figures must not be treated as identical populations. These comparisons describe reception, not causes or a sales ranking.

| Reference | Displayed review population | What to investigate/adapt |
| --- | --- | --- |
| [Company of Heroes](https://store.steampowered.com/app/228200/Company_of_Heroes/?l=english) | English 94% positive, 5,257; recent 85%, 99 | Squad identity, cinematic campaign and changing battlefield. Its established reputation is a demanding comparison for newcomers. |
| [Gates of Hell: Ostfront](https://store.steampowered.com/app/400750/Call_to_Arms_Gates_of_Hell_Ostfront/) | English 86%, 13,609; recent 87%, 761 | Detailed combined arms, dynamic campaign and multiple ways to control units. |
| [Steel Division 2](https://store.steampowered.com/app/919640/Steel_Division_2/) | English 79%, 4,327; recent 75%, 33 | Large-scale command and an operational layer appeal to a different scope of play. |
| [Company of Heroes 3](https://store.steampowered.com/app/1677280/Company_of_Heroes_3/) | English 62%, 13,450; recent 48%, 219 | A recognizable franchise and extensive feature list can coexist with mixed reception. |
| [Men of War II](https://store.steampowered.com/app/1128860/Men_of_War_II/) | English 64%, 2,361; recent 45%, 33 | Historical content and multiple modes do not prevent dissatisfaction. Small recent samples need caution. |
| [Mud and Blood](https://store.steampowered.com/app/1391530/Mud_and_Blood/) | All reviews 87%, 864; listing is Early Access | A small-squad, hand-drawn, top-down WWII game can receive positive reception. It does not prove this project's visual style or commercial viability. |

## What makes the battles enjoyable

These are proposed design mechanisms, informed by the references rather than experimentally established causes of their scores.

| Mechanism | A concrete moment of fun | Design implication |
| --- | --- | --- |
| Combined arms | An MG pins infantry; smoke lets engineers cross; an anti-tank team attacks the vehicle protecting it | Each role needs a useful ally and an understandable counter. Avoid a universal best unit. |
| Terrain with consequences | A risky flank bypasses a defended road; a ruined wall opens a new approach | Cover, visibility and traversal must agree with what the player sees. |
| Recoverable setbacks | Retreat saves a veteran squad but gives up the objective | Permit meaningful recovery without making casualties irrelevant. |
| Distinct unit identity | Keeping a familiar crew alive changes the next operation | Persistent experience should create attachment and tradeoffs, with recovery after losses. |
| Strategic choice during combat | Spend scarce reinforcements on infantry now or reserve capacity for armor | A clear economy and opportunity cost create strategy beyond selecting targets. |
| Readable consequences | A shot bounces from frontal armor; a flank succeeds for an understandable reason | Explain suppression, cover, range and damage results through concise UI and effects. |
| Escalation and breathing room | Reconnaissance becomes a skirmish, then a counterattack, then regrouping | Constant maximum intensity leaves no room to plan or appreciate a victory. |
| Replay through changed decisions | A different approach, force composition or objective changes the plan | Variation must affect tactics; random enemy placement alone is insufficient. |
| Historical atmosphere | Period equipment, credible architecture and convincing weapon sounds establish place | Research a narrow date/theater thoroughly. Authenticity must survive gameplay simplification. |

Barbedwire's [mission-design explanation](https://www.barbed-wire.eu/our-dedication-to-realism-and-history/) explicitly connects realistic landscapes and alternate approaches with replayability. It also describes hit location, ammunition and internal vehicle components, while acknowledging historical limitations. Adapt the decision value of those systems; copying their full simulation scope would be a major production commitment.

The developer's [current game overview](https://www.barbed-wire.eu/we-are-barbedwire-studios/our-game-development/) describes dynamic campaigns playable in co-op. This supports investigating campaigns as a replay structure, but does not prove co-op is mandatory or that most buyers prefer it.

[CoH3 tactical-pause designer Stuart Warwick](https://smwarwick.com/coh3.html) explains issuing and displaying queued commands while solo play is paused. This is a useful pattern for making tactical depth accessible without requiring constant rapid input.

## What makes players frustrated

The following are reported experiences. Do not repeat the reviewers' numerical, balance or technical allegations as verified facts.

| Source observation | Risk to our game | Proposed response |
| --- | --- | --- |
| GoH reviewer Jagged, 20 October: enjoys the equipment representation and co-op, but dislikes army-management menus, unit-transfer friction, AI behavior and perceived DLC value | A good battlefield can be undermined by the surrounding flow | Test selecting a force, starting a battle, controlling it and returning to campaign as one journey |
| GoH reviewer Fluffy Pocket Dragon, 18 October: difficulty reading the interface; other visible reviews report crashes | Readability and lost progress can overwhelm tactical appeal | Scalable interface, clear contrast, save recovery and packaged-build stability |
| CoH3 reviewer mantegazza.alessio, 19 October: dissatisfaction with solo content, UI and historical portrayal; beeftothetaco, 17 October: complaints about pathing, balance and paid content | Price and franchise expectations amplify perceived incompleteness | Commit to a specific content promise, polish the base game and verify counters |
| CoH reviewer J4MESOX4D, 18 October: dislikes micromanagement, repetitive objectives and perceived AI advantages; Left4pillz, 22 October, still recommends the classic | Even respected games divide players over pacing and challenge | Test the intended audience rather than assuming universal taste |
| SD2 reviewer y17Trees, 17 October: recommends the tactical game while objecting to DLC volume and perceived multiplayer deck advantages | Positive recommendation does not mean acceptance of every business/design choice | Keep a satisfying base purchase and assess competitive fairness if PvP is selected |
| Men of War II launch reviews, May–June 2024: solo disconnections/online dependency caused strong objections | Operational friction can become the defining launch story | Offline solo play and interruption-safe saves from the first public build |

Sources: [GoH reviews](https://steamcommunity.com/app/400750/reviews/), [CoH3 reviews](https://steamcommunity.com/app/1677280/reviews/), [CoH reviews](https://steamcommunity.com/app/228200/reviews/), [SD2 reviews](https://steamcommunity.com/app/919640/reviews/), [MoWII launch reviews](https://steamcommunity.com/app/1128860/reviews/?browsefilter=toprated).

A separate [GoH pathfinding discussion](https://steamcommunity.com/app/400750/discussions/0/755051236320880321/) contains complaints about stalled units and risky routes, alongside a player reporting substantial improvement. That disagreement matters: it identifies movement as a test priority, not proof of universal current failure.

**Historical correction:** the developer's [Steel Rain announcement, 25 October 2024](https://steamcommunity.com/app/1128860/eventcomments/4701287772393051682/) announced offline mode for Men of War II. The launch complaints above are historical. No runtime check of the current implementation was performed.

Avoid additional design traps as hypotheses to test: invisible randomness deciding engagements, surprise spawns without information, repetitive capture missions, manual resupply for every soldier, unreadable realistic colors, permanent campaign collapse after one mistake, and adding large rosters before command reliability. They are not attributed as measured complaint frequencies.

## Audience and product alternatives

| Direction | Player promise | Main production risk | Assessment |
| --- | --- | --- | --- |
| Fast competitive squad RTS | Tight territory contests and repeated mastery | Netcode, matchmaking population, balance, anti-cheat and live operations | A possible later direction; do not assume launch population |
| Detailed battlefield simulation | Granular equipment, armor and individual control | High interaction count, historical asset burden and micromanagement | Valuable inspiration; costly to reproduce comprehensively |
| Large army/operational game | Broad fronts, force planning and major battles | Scale, information density, AI and campaign scope | Different product from a focused company game |
| Company tactics with campaign and reinforcement economy | Accessible combined arms and persistent consequences | Requires excellent solo AI, mission variety and loss recovery | Recommended hypothesis for SimpleRTS |

Proposed first audience: solo strategy players who enjoy WWII tactics and thoughtful maneuvering, including players who prefer pause over high actions per minute. Audience size and demand are not validated. Co-op has an observable reference pattern, but should be chosen deliberately, then planned early if it is a release requirement.

## Proposed game direction, subject to owner selection

- **Core loop:** choose an operation and force → scout → secure terrain/supply objectives → commit reinforcements → suppress and flank → complete or withdraw → recover and choose the next operation.
- **Distinctive feature:** a persistent company with a constrained replacement budget and a choice of next objectives. Taking a depot might improve the next battle's supply; saving veterans may be worth abandoning a secondary objective. Prototype whether this is compelling rather than assuming novelty.
- **Battle scale:** initially test 6–12 controllable squads/vehicles per side and 15–25 minute operations. These are starting hypotheses, not market-derived optimal values or engine capacity claims.
- **Strategic layer:** reinforcement capacity, controlled supply locations and deployable field support. Prototype this against a simpler fixed-force version. If the final game has no economy or strategic resource decisions, describe it as real-time tactics rather than promising classic base-building RTS play.
- **Historical scope:** one theater and a coherent date range. Normandy 1944 offers familiar equipment and mixed terrain; North Africa offers a different battlefield silhouette but requires convincing open-ground tactics; an Eastern Front setting offers other operational stories and a substantial research burden. Normandy is a working example, not an owner-approved setting.
- **Visual candidate:** readable 2D top-down or oblique sprites in Godot, compatible with the inspected Game Arts framework. Test vehicle facing, soldier visibility, walls and line of sight at real gameplay zoom. No camera, art style, generator, language or direction count has been approved.
- **Business-model candidate:** a complete premium base game, with no power sales or mandatory account for solo play. Price, budget, team capacity, launch platform scope and content count remain open. Do not infer pricing from ratings or copy the scope of decades-old franchises.

## Research-to-prototype acceptance map

All criteria below are proposed internal gates; **none have been executed** and none are Steam benchmarks.

| ID | Hypothesis / proposed criterion | Focused evidence to collect |
| --- | --- | --- |
| FUN-01 | Most intended players can learn basic commands without developer intervention | In 10 observed sessions, at least 8 complete select/move/attack/retreat and the first objective within 10 minutes; log confusion and assistance |
| FUN-02 | Positioning creates recognizable value | At least 8/10 players can explain one battle outcome using cover, suppression, facing or a flank; collect the actual encounter and explanation |
| FUN-03 | Replay is wanted voluntarily | At least 6/10 choose another mission or repeat a mission when offered an equally easy exit; record actual choice and reason, not only a satisfaction score |
| CMD-01 | Commands are dependable | Repeat 20 defined mixed-force route scenarios across bridges, narrow roads, opposing traffic and rubble; no silent stuck unit or collision bypass; unreachable orders show a reason |
| AI-01 | AI challenges through understandable rules | On normal difficulty, scripted fog-of-war/resource probes show no unexplained knowledge or undeclared resource grants; scenario exceptions are visible and documented |
| SAVE-01 | Solo respects interruptions | In a packaged build, play offline, pause, save during combat, quit and restore orders/objectives/veterans/resources; simulate an interrupted write without losing the last valid save |
| UX-01 | Combat remains readable | At agreed minimum resolution and enlarged UI, players distinguish friend/enemy, unit role, suppression, selection and available orders without color or sound alone |
| PERF-01 | Target scale is playable | Select actual minimum hardware and maximum battle composition, then measure frame time, navigation spikes and memory in packaged play; a candidate goal is 60 FPS at 1080p, pending hardware selection |
| CAMP-01 | Persistence adds choice rather than grind | Players encounter at least two viable next-operation choices, understand their costs and can recover from one lost battle; compare against fixed-force control scenario |
| VALUE-01 | The promise matches the build | Blind store-pitch test: intended players accurately describe what they will play after seeing real footage; record price expectations separately and avoid leading questions |

Ten sessions are formative usability evidence, not a representative demand estimate. Separate new RTS players from experienced players in results; widen recruiting after revisions. Use local observation/consensual notes initially; do not install telemetry or session-replay services to satisfy this plan.

## Steam: getting interest versus earning repeat play

Discovery needs an immediately understandable hook, readable screenshots and real gameplay demonstrating the decision loop. Purchase needs confidence in quality, content and value. Repeat play needs satisfying decisions, variation, mastery and reliable saves. These are proposed funnel hypotheses; measure them separately.

Valve's [wishlist guidance](https://partner.steamgames.com/doc/marketing/wishlist?l=english) recommends a Coming Soon page when ready, sustained engagement and examining trends. It explicitly gives no minimum wishlist threshold for visibility and no formula accurately predicting sales. Do not promise sales from a wishlist count.

Use a focused public demo once an appealing operation is stable. [Next Fest guidance](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest) requires preparing the demo and review in advance, with edition-specific deadlines. Valve says build review is not comprehensive QA. Select an event only after checking current eligibility and our actual readiness; no event or release date is committed here.

Later evidence should separate page visits, wishlists, demo starts, first-objective completion, second-session return and purchases/refunds where available. There are no universal conversion targets established by this research, and Steam review counts are not sales data.

## Godot and Game Arts implications

Godot is the requested engine; no installed version or project pin was observed. Recheck the selected version's docs before implementation. [Native 2D navigation](https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_introduction_2d.html) offers mesh navigation/agents and grid AStar alternatives. Choose for the world geometry, then test mixed-unit traffic. Avoidance is not collision or a complete dynamic-obstacle path update. A NavigationObstacle2D does not itself change pathfinding.

Use the engine's physics/collision, scenes/resources, signals, native timers, InputMap and Control containers for their documented responsibilities. Use [AnimationPlayer](https://docs.godotengine.org/en/stable/tutorials/animation/introduction.html) and appropriate native animation facilities for authored motion. Actual combat rules, formation decisions, tactical AI and campaign persistence still require game-specific owners. Built-in navigation does not certify RTS movement quality.

Apply the external [Game Arts entry skill](H:/Source/Repos/fahadTheTechIdea/GameArtsSkills/.agents/skills/game-arts/SKILL.md) and [master standard](H:/Source/Repos/fahadTheTechIdea/GameArtsSkills/GAME_ARTS_MASTER_STANDARD.md) when art work starts: confirmed project settings → approved campaign/storyboard sequence → rendered mockup and explicit approval → decomposition → reference-conditioned assets → mechanical validation → human visual acceptance → Godot import/runtime checks. Store game-owned state in SimpleRTS, not GameArtsSkills. No art or settings were generated/approved in this research stage.

RTS art must support selection outlines, team identification, facing, turret/hull separation, readable cover, damage states, ruins, muzzle flashes and suppression effects. Keep independently moving pieces separate. A beautiful full-scene image is not a usable level or production asset set.

## Release and legal applicability at this stage

The shared `C:/Users/f_ald/.codex/APP_RELEASE_REQUIREMENTS.md` remains the policy owner; this section only records current applicability and dependencies.

| Area | Current observation / next dependency |
| --- | --- |
| Signup ages | No signup implementation observed. Confirm intended audience/regions and actual data collection before selecting age controls; a violence rating is a separate question |
| Fonts and all asset rights | No production fonts/art/audio observed. Record licenses/provenance and use local licensed font delivery when chosen; historical subject matter does not license photographs, recordings or game assets |
| Tracking/session replay | No SDK/runtime observed. Add none for checklist completion; assess real data flows if features are later selected |
| Marketing | No email system observed. Sender identity, valid address and unsubscribe enforcement become gates only if marketing is selected |
| Subscriptions | No billing implementation observed. No subscription requirement inferred |
| DMCA / user content | No uploads, chat or Workshop integration observed. Determine actual hosting/linking activity before assessing Section 512 relevance. If relevant, perform the owner-guided official registration walkthrough with real provider/agent details; no filing is performed or claimed here |
| WWII content and markets | Exact uniforms/symbols, violence, historical claims, audience and territories unresolved. Research specific market restrictions and ratings against the selected content before release; do not invent a worldwide legal conclusion |
| Generated content | If AI-assisted artwork/audio/narrative/localization ships, maintain provenance and complete Valve's applicable disclosure. Its [content survey](https://partner.steamgames.com/doc/gettingstarted/contentsurvey) distinguishes pre-generated player-consumed content from live generation and requires honest maturity disclosure |
| Steam delivery | Account, app ID, store presence, OS targets, actual builds, reviews, pricing, content survey and release timing not inspected. These are later external/build gates; this report is not store approval |

## Recommended sequence

### Owner direction and salvage comparison follow-up

The detailed GoH Conquest/BATTLETECH findings, updated persistence correction and proposed salvage experiments are owned by [Salvage systems research](SALVAGE_SYSTEMS_RESEARCH.md). Fahad subsequently asked to explore other genre combinations before selecting the final identity; salvage remains a candidate.

In the research conversation, Fahad selected combat engineers, then favored salvage strategy over puzzle-oriented mission design. This narrows the proposed identity to fighting, recovering useful equipment and rebuilding a company. It does not approve a complete feature list, historical setting, art profile or release scope. Earlier audience/product alternatives remain research history rather than the current recommendation.

Further sources checked 24 October 2026 show substantial existing overlap:

| Reference | Documented or reported overlap | Useful distinction for our design work |
| --- | --- | --- |
| Gates of Hell: Ostfront | Developer [damage-system explanation](https://store.steampowered.com/news/posts/?appids=400750&enddate=1620588961&feed=steam_community_announcements) describes component damage, finite repair kits and faster engineer/tank-crew repairs. [Player discussion](https://steamcommunity.com/app/400750/discussions/0/3073125301396566428/) describes captured vehicles retained in Conquest | Closest direct WWII comparison among those inspected. Capturing, repairing and retaining enemy vehicles is already an existing combination, not our invention. Player-reported retention was not reproduced here |
| Men of War: Assault Squad 2 | An [officer's forum explanation quoting the manual](https://steamcommunity.com/app/244450/discussions/0/620700960791795649/) describes repair kits, repairable components and repair exposure under fire | Relevant tactical reference for vulnerable recovery crews; this source does not establish an equivalent persistent salvage campaign |
| BATTLETECH (2018 video game) | [Publisher documentation](https://www.paradoxinteractive.com/games/battletech/about) describes a persistent mercenary company, repairs, customization and replacing damaged weapons using battlefield salvage | Strong reference for the battle-to-workshop progression loop; science fiction and turn-based combat rather than WWII RTS |
| Battlezone: Combat Commander | [Official Steam description](https://store.steampowered.com/app/624970/Battlezone_Combat_Commander/) describes scavenging battlefield bio-metal to produce units, defenses and facilities, and balancing combat with scrap collection | Reference for salvage as a contested real-time economy; science-fiction FPS/RTS hybrid and resource conversion rather than restoring individual historical vehicles |

Both official manual PDFs attempted in this follow-up exceeded the web tool's fetch-size limit; their full text was not read. The comparison uses the accessible developer explanation and clearly labeled community report rather than claiming full manual verification.

Proposed differentiating focus: recovery itself is a contested operation, with escort/protection, limited recovery capacity, evacuation routes and a choice between taking an intact vehicle, stripping parts or withdrawing. The workshop then makes the recovered equipment relevant to the next operation. These are design proposals, not proven absences in competitors or an established market gap. Do not market the game as the first WWII salvage RTS.

Next comparison should include hands-on observation of GoH Conquest and BATTLETECH's workshop/salvage loop before finalizing detailed mechanics. Capture what the player actually decides, how much work recovery requires, and whether the decision occurs during combat or after victory. No hands-on tests were performed here.

1. Owner selects audience, combat scale, historical theater, base/economy expectations, visual direction and whether co-op is essential. Record available team/budget and desired content scope before estimating schedule.
2. Write a proportionate game brief with the full intended product scope, and isolate one representative battle as the first proof of fun. Do not confuse the first slice with a complete release.
3. Follow the Game Arts settings/storyboard/mockup workflow, then use approved representative art for the playable slice. Evaluate readability in-engine before bulk production.
4. Build and test movement, commands and combined-arms encounters before expanding units/maps. Test the campaign hook separately. Use the acceptance map above and revise it with actual findings.
5. Expand only validated systems into the complete campaign/skirmish product; prepare a demo and truthful Steam page when the build can substantiate them. Recheck release requirements at packaging/submission.

## Source register and status

All accessed 24 October 2026. Steam listings establish the displayed reception/features; Steam Community entries establish that specific players reported experiences; developer pages establish explanations/announcements; official docs establish documented platform behavior. The body links each conclusion to the relevant source. No third-party sales estimates were used.

Research completed as an initial qualitative decision brief. Statistical review coding, competitor hands-on play, market sizing, pricing tests, player recruitment, art approval, prototype tests and release verification remain unperformed.
