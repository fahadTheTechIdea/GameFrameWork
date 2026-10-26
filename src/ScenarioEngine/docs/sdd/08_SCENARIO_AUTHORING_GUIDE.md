# Scenario authoring and worked example

## 1 Authoring deliverable

A scenario author delivers a playable definition, approved native scene bindings, localization, route/ending cases and validation evidence. Authoring is complete only when the operation runs. Schema validity is an early gate.

The fixture [river_crossing.json](examples/river_crossing.json) is the normalized JSON reference. It includes the entire document set; no missing target zone or undeclared objective is left for the implementer to invent. The fixture's role IDs are proposed bindings; their Godot scenes do not exist yet.

## 2 Ordered authoring workflow

| Stage | Input | Output and gate |
| --- | --- | --- |
| Brief | Approved layer/premise | Situation, pressure, player promise and ending scope |
| State budget | Starting kit and action costs | Resource sources/sinks and two affordable approaches |
| World graph | Native scene/anchors | Zones, exits, risk/confidence and alternate paths |
| Objective graph | Mission verbs | Prerequisites, activation, success/failure and consequences |
| Encounters | Enemy/contact profiles | Information sources, patrols, reports and recovery |
| Endings | Outcome matrix | Exhaustive closure coverage and explicit consequences |
| Variation | Authored profiles/seed | Cases for every selected profile |
| Validation/package | JSON/assets/cases | Structural, semantic, runtime and human evidence |

The Scenario Kit uses native Godot EditorPlugin/Inspector UI and undo/redo for edits. Its preview invokes the same loader/engine; it cannot be the only place a rule is checked.

## 3 Filled example brief

Scenario ID: river_crossing. A separated soldier carries a task to recover a radio component and deliver a report. They begin in an orchard, can visit a farm contact, and must cross a watched river to reach friendly lines. Repairing a boat spends tools; obtaining a ferry passage spends supplies/trust. A radio transmission can finish the mission before escape succeeds.

The intended pressure is a route/resource decision and risk of escalating search. The example demonstrates the existing concept; it is not presented as a new market novelty.

## 4 Zone graph

~~~mermaid
flowchart LR
    O[Orchard and radio wreck] --> F[Farm contact]
    O --> R[River bank]
    F --> R
    R -->|boat repaired| B[Friendly lines]
    R -->|ferry permission| B
~~~

Every target, including friendly_lines, is in scenario.zoneIds. Entry/exit anchors are declared. Both river exits target a real zone but have different conditions and costs. Native Godot tests must verify the physical approaches and sensor volumes.

## 5 Starting state and budget

| State | Start | Source/sink | Strategic effect |
| --- | --- | --- | --- |
| Lead condition | 100 | Damage/treatment | Return ability and action speed |
| Ammunition | 8 | Fire/suppression | Short engagement or preserve |
| Medical | 2 | Treatment/assistance | Lead versus contact help |
| Sustainment | 3 | Ferry trade and long travel | Safe passage costs supplies |
| Tools | 2 | Repair boat consumes 1 | Independent crossing alternative |
| Heat | 0 | Confirmed evidence and decay | Clean/costly return distinction |
| Farm trust | 50 | Help or trade | Permission and contact consequence |
| Radio recovered | false | Wreck interaction | Mission proof/transmission |
| Mission delivered | false | Successful transmission or return with proof | Mission success without return |

Two feasible plans exist from the starting kit: recover radio -> repair boat -> cross; or recover radio -> farm trade -> ferry -> cross. Neither requires finding every container. A no-proof route remains physically possible and produces a failure outcome instead of silently granting proof.

## 6 Objective graph and interventions

recover_radio is active immediately; it completes when the layer confirms the wreck interaction. deliver_report depends on recover_radio Completed and completes on missionDelivered. A lead loss/capture before delivery fails active objectives unless a scenario explicitly defines recovery.

Interactions in the layer data declare zone, action type, duration, resource costs and completion result. The layer registers how recover_radio, repair_boat, trade_ferry and transmit_report affect facts. These strings are recognized handlers, not script paths.

Native arrival at friendly_lines sets atFriendly and, if radioRecovered, missionDelivered. Transmission at river sets missionDelivered without atFriendly. A later capture/death can therefore reach mission_without_return through gameplay.

## 7 Ending truth table

| Closure state | Delivered | Return state | Heat/companions | Ending |
| --- | --- | --- | --- | --- |
| At friendly lines | Yes | Returned | Heat <=30 and no loss | clean_return |
| At friendly lines | Yes | Returned | Heat >30 or companion loss | costly_return |
| Dead or terminal capture | Yes | No return | Any | mission_without_return |
| At friendly lines/dead/terminal capture | No | Any | Any | failure |

The example uses mutually exclusive conditions and unique priorities. The checker tests these rows and an uncovered nonterminal row. This establishes predicate coverage for supplied facts, not that a player can reach all rows.

## 8 Required runtime case records

Each runtime case must include pack/content digest, seed, variant ID, controller commands with ticks, native or synthetic WorldFrames, expected effect transactions and final state/ending. Abstract cases in the fixture contain projected facts only and are labelled accordingly.

| Runtime case | Actions | Evidence required |
| --- | --- | --- |
| Boat clean return | Recover, repair with 1 tool, reach exit | Tools consumed once; native arrival; clean ending |
| Ferry clean return | Recover, trade, use ferry exit | Supplies charged once; route permission |
| Costly escape | Recover, cause recorded alert, return | Heat source ledger; costly ending |
| Mission without return | Recover, transmit, then terminal capture | Delivery before capture; no return flag |
| Failure | Return without recovering radio | No invented proof; failure summary |
| Interrupt repair | Start then cancel/receive incapacitation | Reservation released; boat stays locked |
| Insufficient supplies | Attempt unaffordable trade | No partial deduction; alternate route remains |
| Failed destination load | Enter a ready exit, fail scene load | Old zone recoverable; no double cost |
| Missing radio target | Invalid authored target | Semantic/asset binding diagnostic before play |
| Save and resume | Save after repair before exit | Route/stock/action state restored exactly |

The first two are different approaches to the same objective. Do not create two nominal routes that require the same mandatory key and patrol sequence.

## 9 Encounter and information contract

For every enemy/contact add: spawn anchor; role; active variant; knowledge at start; evidence gained; communication channel; report delay; search anchors; response limits and exit/recovery conditions.

A patrol may react to sight/noise/body/report, never to a global player position. A radio report has sender, recipient scope, source location, confidence and completion time. Interrupting it before completion prevents remote knowledge. Reinforcements require an arrival anchor and visible warning.

## 10 Scenario variation

Use authored variant IDs and seed selection for patrol route, contact availability or weather. Varying only a menu paragraph is not a replay difference. Each variant requires one affordable route, all required item sources, closure coverage and native navigation checks.

Three scenario targets remain product scope: a return crossing, a rescue/escort operation and a mission-delivery operation with escape optional. Only River Crossing is fully represented by the current fixture; the other targets require their own authored data and review. This distinction corrects the prior implication that three ready scenarios already existed.

## 11 Author diagnostic examples

~~~text
PACK_REFERENCE  data/zones/river.json /exits/0/targetZoneId
Unknown zone friendly_lines
Fix: add the zone document and include it in scenario.zoneIds.

CONDITION_TYPE  data/endings/clean_return.json /condition/all/2/value
lost_soldier.heat expects integer 0..100
Fix: replace the string comparison operand with an integer.

LAYER_OPERATION data/scenario.json /layerData/interactions/1/actionType
Unknown action type repair_custom_script
Fix: choose a registered action or implement a trusted game-layer capability.
~~~

Diagnostics explain what to edit; they never advise disabling validation.

## 12 Packaging and done

The kit writes mod_manifest.json and declared documents from normalized data. It copies only approved, licensed assets for the declared trust tier; API 1 public packs refer to built-in asset roles. It validates from a clean staging directory, not the editor cache.

Author definition of done: complete brief/graphs/budget, schema and semantic pass, conditions cover every closure, action-based engine cases pass, Godot routes/sensors work, save round trip passes, localization resolves, no unexplained detection/softlock remains, and a fresh player finishes without external instructions. Record the human test observations separately from automated checks.

Use the same validator in authoring and player load. A pack produced by an official tool is still validated at runtime.

## 13 Future game-type profiles

When the [chapter 09 extensions](09_GAME_TYPE_EXTENSIBILITY.md) are implemented, authoring must select an exact registered capability/advancement profile, declare state scopes and host requirements, then validate the chosen layer's module schemas. Open-world authors must declare inactive-region and completion policies; turn-based authors must declare turn/phase rules; narrative authors must define checkpoint and unavailable-choice behavior. Each profile needs its own scenario checks and native preview. The current schema does not accept those proposed fields, and this chapter's existing fixture remains a bounded Lost Soldier operation.
