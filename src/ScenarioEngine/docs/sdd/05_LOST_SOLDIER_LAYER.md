# Lost Soldier detailed game layer design

## 1 Ownership and model

LostSoldierLayer implements gameLayerId lost_soldier. Partial files may group inventory, actions, AI, contacts, combat and campaign code, but one layer owner commits domain state. The layer uses generic position/fact/event DTOs and receives native observations; it never queries a Godot singleton.

All numbers below are prototype tuning, stored in a layer tuning definition rather than spread through code. Values may change after testing. They are game balance, not claims of historical/medical realism.

## 2 Actor state

| Field | Type/range | Behavior |
| --- | --- | --- |
| actorId / entityId | Stable string / Int64 | Authored ID maps to allocated engine ID |
| role | lead, companion, sentry, patrol, officer, civilian | Selects registered behavior profile |
| condition | Integer 0..100 | 0 incapacitates; does not by itself imply death |
| pain, fatigue, stress | Integer 0..100 | Explicit performance modifiers |
| wounds | Named list with severity 1..3 and stabilized flag | Persist until treatment/aid-station policy resolves |
| lifeStatus | active, incapacitated, dead, captured, missing, returned | Determines available actions and ending facts |
| skills | Six integer ratings 0..3 | movement, marksmanship, fieldcraft, firstAid, engineering, communication |
| aiProfileId / weaponProfile | Registered profile ID or none | Authoritative decision/combat configuration |
| positionMm / zoneId | Accepted native observation | No direct UI mutation |
| currentAction / orders | Action record plus bounded queue | At most one active blocking action |
| personalMemory | Major event IDs | Readable consequence history |

Lead plus two companions is the default party limit. Companion recruit actions reject a full party with an explicit choice; no actor is silently dismissed. Dead/captured/missing actors stop accepting movement/combat commands.

## 3 Resources, inventory and encumbrance

| Resource | Unit/capacity proposal | Main sink | Alternative |
| --- | --- | --- | --- |
| ammunition | Shared supply unit, capacity 24 | Rifle/pistol attack 1; suppress burst 3 | Avoid, deceive, retreat |
| medical | Kit, capacity 4 | Stabilize/treat action 1 | Aid station or leave wound |
| sustainment | Travel ration, capacity 8 | Long exit 1; safe rest 1 | Shorter riskier route |
| tools | Kit unit, capacity 4 | Repair route/radio 1–2 | Contact trade or different exit |

Fuel and batteries are optional later definitions, not additional mandatory slice meters. Intelligence is a set of facts with confidence; trust is per contact; heat and time are domain pressures. They are not interchangeable stock items.

Stock transactions check free quantity, action reservation and carrying capacity together. Shared stock does not mean actors can fire without their equipped weapon. The slice treats ammunition as readable supply units; a later caliber model needs explicit compatibleItem types.

Starting reserved quantities are zero. A scenario must provide exactly one lead, and every startingParty ID must refer to a lead/companion actor. Party members cannot begin dead, captured or outside the start zone unless an explicit scenario start mode supports it.

Load score = ammunition + 3*medical + 2*sustainment + 3*tools + carried mission-item weights. Light <= 30; burdened 31..45; overloaded >45. Burdened speed factor 850/1000; overloaded disallows sprint and advertises extra travel cost. These bands avoid a packing-grid minigame. Dropping an item creates a declared cache at the actor's accepted position and emits an event.

## 4 General action pipeline

Requested -> Validated -> Reserved -> Active -> Completed, Cancelled or Interrupted.

Preconditions: controlled actor active, same-zone target, native proximity/line-of-sight where needed, legal action profile and sufficient free stock. Range is checked again on start/completion when applicable. An actor leaving the zone, falling, losing the target or receiving a cancel can interrupt. The UI shows cost, duration and interrupt rules before confirmation.

| Action | Duration proposal | Completion and cost | Interruption |
| --- | --- | --- | --- |
| Move | Until native arrival | No stock cost; fatigue for sprint | Stop/cancel safely |
| Recover/search | 40 ticks | Obtain declared item/fact, no random loot | Progress resets; no cost |
| Treat | 80 ticks | Consume reserved medical 1, condition +25 capped 100, stabilize one wound | Release reservation; no heal |
| Repair | 60 ticks with engineering 0 | Consume tools 1, unlock declared route | Release tools unless explicit start cost |
| Report | 20 ticks | Transmit proof fact if radio recovered | No delivery until completion |
| Trade | 20 ticks | Atomic offered stock and contact outcome | All-or-nothing |
| Rest | 120 ticks in safe area | Sustainment 1; fatigue -20, stress -10 | Cost only on completion |
| Escort | Native follow until safe anchor | Companion/target rescue fact | May resume from current location |

Engineering/firstAid skills reduce relevant action duration by 10% per rating, with integer ceiling and 50% minimum base duration. No skill can reduce a duration below one tick. Treating does not remove a severe wound; an aid station has a separate declared action.

## 5 Wounds, fatigue and stress

A hit uses game-defined damage and may apply a named wound. Light wound reduces maximum condition by 10, medium by 20, severe by 35 until resolved. A leg wound also multiplies movement speed by 800/1000; stacking speed factors clamps at 500/1000. Pain reduces aim by 1 per 5 points. These are intended feedback rules, not medical simulation.

Incapacitated actors can be stabilized by a companion; otherwise an authored bleed-out duration may apply. That duration is shown and preserved in the snapshot. A scenario without a rescuer must specify capture/death/checkpoint behavior, not leave permanent unplayable incapacitation.

Sprint adds 1 fatigue per 20 ticks of active sprint. Fatigue >=70 disables sprint; fatigue >=90 adds a visible 200/1000 noise modifier. Stress >=70 reduces aim by 10 points, >=90 by 20; it cannot secretly cancel player commands. Rest and assistance give explicit reductions. Only timed active actions advance these values.

## 6 Movement, routes and perception

Godot handles paths, collision and sensor observations. The layer issues desired movement profile/target and accepts reported pose/arrival/blocked results. Core contains no replacement navigation mesh.

Prototype walk speed is 1600 mm/s and sprint speed 2800 mm/s before fatigue/wound/load factors. Interaction proximity is 1500 mm unless overridden by a registered action profile. The 2D adapter uses 100 pixels per metre (PositionMm / 10), a 300 mm body radius and configured matching navigation clearance; these are prototype scale settings to validate against art and doorway geometry. A 3D adapter would use metres (PositionMm / 1000).

Route UI shows estimated travel time, resource cost, risk and confidence. Unknown estimates are labelled. Cover and concealment are distinct: cover modifies hit/block results; concealment modifies awareness from vision.

Perception evidence record: evidenceId, source actor/object, observer, location, tick, confidence 0..1000, kind and communication channel. Native ray/area queries establish physical line-of-sight/range. Domain awareness accumulation interprets those observations.

Prototype awareness gain per decision pulse = visibleFraction * profileGain / 1000, clamped to 0..100. ProfileGain is 25 for clear view and 10 for partial view. No confirmed sight subtracts 5. Suspicious >=25, searching >=60; confirmed identification >=100. Noise/evidence can cause investigation without inventing a current player position. Debug mode shows the full ledger; normal UI shows only clues the player can perceive.

## 7 Combat resolution

The layer resolves a shot only if actor active, weapon ready, target valid, ammo available and native line-of-fire sample is fresh (within 2 rule ticks). Missing/stale sample requests a new sensor check and keeps the action pending; it does not assume a clear shot.

Prototype hit chance in percent:
clamp(base 65 + 5*marksmanship - rangePenalty - coverPenalty - stressPenalty - painPenalty, 5, 95).

Range bands and cover penalties belong to the weapon/cover profile. Draw one integer in [0,99] from that actor's combat stream; hit if draw < chance. A fired shot consumes ammunition even if it misses, emits noise and applies weapon cooldown. Rifle damage 35, pistol 20, automatic burst 25 are prototype defaults. A severe hit reduces condition and can apply one named wound according to the weapon profile's explicit roll; no renderer decides damage.

| Weapon profile | Near / far / maximum range | Range penalty near/far/max | Cooldown | Ammo |
| --- | --- | --- | --- | --- |
| Rifle | 20 / 40 / 60 metres | 0 / 15 / 30 | 30 ticks | 1 |
| Pistol | 6 / 12 / 20 metres | 0 / 20 / 35 | 15 ticks | 1 |
| Automatic burst | 10 / 20 / 30 metres | 0 / 15 / 30 | 20 ticks | 3 |

Range beyond maximum rejects a shot. Cover penalty is 0 for none, 20 for partial and 40 for substantial cover, using a native line-of-fire observation; fully blocked line-of-fire cannot be shot through. A hit with damage >=35 draws a second combat-stream value: 0..19 applies leg wound severity 1, 20..39 arm wound severity 1, otherwise no new wound. Pain rises by floor(damage / 2), capped 100. These simple defaults are balance candidates, not a final injury simulation.

Suppress action consumes 3 ammunition and applies a bounded suppression duration to targets with confirmed line-of-fire samples. Smoke modifies native sensing through an authored volume and is not a blind +stealth flag. Animation/sound are consequences of fired-shot events.

Death/capture is explicit state. Surrender does not universally guarantee safety; the scenario states its capture policy. Recovery branches consume authored opportunity and time, not unlimited resurrection.

## 8 Heat and local awareness

Heat is integer 0..100. Default changes: confirmed loud shot +10, discovered body +15, successful reported identity +20, interrupted report +0 network heat. Each source event applies once by evidenceId. Heat drops 1 per 200 safe ticks without current hostile evidence. It never falls during active pursuit.

Heat >=30 enables wider local search, >=60 can close a declared road, >=85 requests authored reinforcements if a communication path exists. Thresholds cannot spawn reinforcements from nowhere; an arrival anchor, warning and delay are mandatory.

Local enemy awareness is per observer. Shared heat does not grant every enemy exact player coordinates.

## 9 AI state machine and decisions

| State | Entry | Choice | Exit |
| --- | --- | --- | --- |
| Patrol/guard | Assigned route/post | Follow native path and observe | New evidence or threat |
| Investigate | Audible/partial clue | Visit last-known point | Confirm, search or expire |
| Search | Recent evidence | Choose reachable authored search anchor | New clue or memory expiry |
| Alert/report | Confirmed identity | Radio/runner if available, cover otherwise | Report completion/interruption |
| Engage | Valid target and line of fire | Fire, suppress, reposition or withdraw | Lost sight, ammo or risk |
| Regroup/retreat | High risk/no ammo/order | Reach safe anchor | Stable defensive position |
| Surrender | Authored morale/encirclement decision | Stop hostile action | Capture policy resolves |

A physics-process native Timer at 0.25 s creates decision pulses (5 rule ticks). Relevant evidence/arrival events can wake an actor earlier. The layer scores valid choices once per pulse/event rather than every rendered frame.

Scenario layerData.aiProfiles declares zone, initial state, patrol anchor IDs, sight range, report delay, evidence memory, communication group and enemy ammunition. Each enemy actor references one profile in its own zone; civilians/party actors can omit AI by using null. Enemy ammunition is stored per actor and never drains the player's shared stock. The fixture includes an orchard patrol, river sentry and farm officer.

Communication group denotes an eligible receiver network, not instant knowledge. A report takes reportDelayTicks; cancellation, incapacitation or interrupted radio before completion emits ReportInterrupted. A completed report creates new evidence for eligible receivers with original location/confidence/source tick. Receivers in inactive zones update persistent knowledge; native actors appear only when those zones load.

Utility score = urgency + evidenceConfidence + allySupport - travelCost - exposureRisk - fatiguePenalty; terms use 0..100 integers. Invalid choices receive no score. Ties use stable action ID. Keep the selected reason, top three choices and source evidence. Enemy memory expires after a profile duration, default 600 ticks; no global scan can reveal the hidden player.

Blocked path: stop, emit reason, inspect reachable alternatives on the next pulse; after three failed requests enter hold/report rather than freeze or teleport. AI retries are evidence/path-change driven, not repeated every frame.

## 10 Contacts and trust

Each contact has availability, safety, trust 0..100 and offered interactions. Default trust 50 unless authored. Keeping a promise +15; assistance +10; threat/violence -25. At trust >=60 a declared safe route may unlock; <=20 may refuse trade. Every threshold consequence is shown through dialogue/map changes.

Trades reserve both sides and commit once. A contact does not accept the same mission item twice. Outdated information carries source age/confidence; new evidence supersedes it rather than silently rewriting history.

## 11 Objectives and scenario endings

The layer exposes registered facts; the engine handles dependency/condition evaluation. Common facts: leadDead, leadCaptured, atFriendly, radioRecovered, missionDelivered, companionLost, heat and route availability. Fact names and types are defined in layer capabilities and the layer schema.

The projection is explicit: leadDead comes from lead lifeStatus dead; leadCaptured is true only for terminal capture or exhausted recovery; atFriendly comes from an accepted friendly-zone arrival; returnWindowClosed is currentTick >= deadlineTicks; companionLost comes from a recruited companion becoming dead/missing/terminally captured. radioRecovered and missionDelivered are persisted achievement flags; boatReady/ferryReady come from route unlocks. Runtime does not accept arbitrary set-fact player commands.

Interaction result compatibility is fixed: recover -> radio_recovered, repair -> boat_ready, trade -> ferry_ready, report -> mission_delivered, treat -> treated, escort -> contact_rescued, rest -> rested. Once-only mission/route results mark that interaction completed and cannot charge again. These handlers are a prototype registry; more results require a trusted layer capability update and schema/tests.

The River Crossing fixture demonstrates clean return, costly return, successful mission without return and failure. Clean/costly are separated by heat and companion outcome. Delivery by radio can precede capture/death, so mission-without-return has a real rule path. Return without proof can fail; reaching the right map alone does not fake mission completion.

The full game may add sacrifice or compromised-return families only when an authored scenario defines their conditions and witnessed routes.

## 12 Scenario variation and campaign progression

Variation chooses from authored, validated profiles: start kit, patrol route, weather, contact availability or exit closure. Seed selection happens before initial state and is included in content/session identity. A variant cannot remove every affordable route; its own scenario cases must pass.

Campaign state contains campaignId, completed OutcomeIds, unlocked scenario IDs, party condition, contacts and allow-listed tags. Only declared carry fields cross scenarios. Heat resets or carries by scenario policy; no accidental full snapshot copy. Applying a previously committed OutcomeId returns the existing transition.

Skill progression is capped 0..3 and awarded at scenario completion for declared achievements. No per-shot farming or random loot rarity is required.

## 13 UI feedback obligations

HUD shows condition/wounds, fatigue, free/reserved resources, heat, objective and rally location. Route preview shows assumptions and costs. Event log explains why knowledge/damage changed without exposing omniscient hidden data. Outcome screen shows both successful and harmful consequences.

A companion action either follows its order or reports why it cannot; routine movement does not choose a dangerous detour without a visible route policy. Keyboard select/order/pause and enlarged text must be tested in the native UI.
