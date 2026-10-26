# Detailed layer and extension SDD packages

Updated 25 October 2026, Asia/Kuwait. Status: proposed multi-document designs over the existing C# engine. Each folder is a separate SDD package with its own requirements, architecture, contracts/state, authoring, runtime/recovery and verification documents. None of the proposed game layers/extensions is claimed implemented.

## Start here

Read [engine baseline and common rules](ENGINE_BASELINE.md), then the package index for the layer or extension. [Chapter 09](../09_GAME_TYPE_EXTENSIBILITY.md) is the cross-package overview and sequencing owner. [The research report](../../research/GAME_TYPE_ENGINE_CAPABILITY_RESEARCH.md) owns the dated game comparisons and source-access limitations.

The [catalog](catalog.json) is the package/dependency inventory consumed by the documentation checker. Every package has seven Markdown chapters, one strict definition schema and one filled definition example. Its concrete semantic rules, runtime transitions and acceptance cases live in its own chapters. All runtime acceptance statuses are not_run.

## Game-layer packages

| Package | Builds on these shared capabilities |
| --- | --- |
| [Narrative adventure layer](adventure-layer/README.md) | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [world-requests](world-requests/README.md), [persistent-world](persistent-world/README.md), [player-queries](player-queries/README.md), [narrative-interactions](narrative-interactions/README.md), [activity-instances](activity-instances/README.md) |
| [Party RPG layer](party-rpg-layer/README.md) | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [player-queries](player-queries/README.md), [resource-transactions](resource-transactions/README.md), [progression-scopes](progression-scopes/README.md), [narrative-interactions](narrative-interactions/README.md), [play-contexts](play-contexts/README.md) |
| [Survival and crafting layer](survival-layer/README.md) | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [world-requests](world-requests/README.md), [persistent-world](persistent-world/README.md), [resource-transactions](resource-transactions/README.md), [activity-instances](activity-instances/README.md), [player-queries](player-queries/README.md) |
| [Open-world sandbox layer](open-world-layer/README.md) | [capability-profiles](capability-profiles/README.md), [persistent-world](persistent-world/README.md), [world-requests](world-requests/README.md), [generation-scale](generation-scale/README.md), [checkpoint-history](checkpoint-history/README.md), [player-queries](player-queries/README.md), [activity-instances](activity-instances/README.md) |
| [RTS and base-defence layer](rts-layer/README.md) | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [resource-transactions](resource-transactions/README.md), [activity-instances](activity-instances/README.md), [world-requests](world-requests/README.md), [player-queries](player-queries/README.md), [native-godot-host](native-godot-host/README.md), [generation-scale](generation-scale/README.md) |
| [Turn-based tactics layer](turn-tactics-layer/README.md) | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [player-queries](player-queries/README.md), [play-contexts](play-contexts/README.md), [resource-transactions](resource-transactions/README.md) |
| [Tactical campaign layer](tactical-campaign-layer/README.md) | [capability-profiles](capability-profiles/README.md), [progression-scopes](progression-scopes/README.md), [play-contexts](play-contexts/README.md), [activity-instances](activity-instances/README.md), [persistent-world](persistent-world/README.md), [narrative-interactions](narrative-interactions/README.md) |
| [Colony and emergent-story layer](colony-layer/README.md) | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [activity-instances](activity-instances/README.md), [persistent-world](persistent-world/README.md), [resource-transactions](resource-transactions/README.md), [generation-scale](generation-scale/README.md), [player-queries](player-queries/README.md) |
| [Factory and economic simulation layer](factory-economy-layer/README.md) | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [resource-transactions](resource-transactions/README.md), [activity-instances](activity-instances/README.md), [persistent-world](persistent-world/README.md), [checkpoint-history](checkpoint-history/README.md), [generation-scale](generation-scale/README.md) |
| [4X strategy layer](four-x-layer/README.md) | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [player-queries](player-queries/README.md), [progression-scopes](progression-scopes/README.md), [activity-instances](activity-instances/README.md), [checkpoint-history](checkpoint-history/README.md) |
| [Roguelite run and meta-progression layer](roguelite-layer/README.md) | [capability-profiles](capability-profiles/README.md), [progression-scopes](progression-scopes/README.md), [play-contexts](play-contexts/README.md), [generation-scale](generation-scale/README.md), [resource-transactions](resource-transactions/README.md) |
| [Deckbuilding and card encounter layer](deckbuilding-layer/README.md) | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [play-contexts](play-contexts/README.md), [resource-transactions](resource-transactions/README.md), [progression-scopes](progression-scopes/README.md), [generation-scale](generation-scale/README.md) |
| [Institution and life-management layer](institution-management-layer/README.md) | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [activity-instances](activity-instances/README.md), [persistent-world](persistent-world/README.md), [resource-transactions](resource-transactions/README.md), [progression-scopes](progression-scopes/README.md), [player-queries](player-queries/README.md) |
| [Recruitment and headquarters campaign RPG](campaign-rpg-layer/README.md) | [capability-profiles](capability-profiles/README.md), [progression-scopes](progression-scopes/README.md), [play-contexts](play-contexts/README.md), [logical-time](logical-time/README.md), [narrative-interactions](narrative-interactions/README.md), [resource-transactions](resource-transactions/README.md), [player-queries](player-queries/README.md), [persistent-world](persistent-world/README.md) |

## Shared-extension packages

| Package | Owning capability | Required proposed dependencies |
| --- | --- | --- |
| [Capability profiles and layer composition](capability-profiles/README.md) | GameProfileCatalog | Composition foundation |
| [Logical time and advancement policies](logical-time/README.md) | SimulationTimeOwner | [capability-profiles](capability-profiles/README.md) |
| [Acknowledged world requests and transitions](world-requests/README.md) | WorldRequestOwner | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md) |
| [Persistent worlds, regions and stable identities](persistent-world/README.md) | PersistentWorldOwner | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [world-requests](world-requests/README.md) |
| [Controller views and pure action previews](player-queries/README.md) | GameQueryOwner | [capability-profiles](capability-profiles/README.md) |
| [Campaign, run and profile progression](progression-scopes/README.md) | GameProgressionOwner | [capability-profiles](capability-profiles/README.md), [checkpoint-history](checkpoint-history/README.md) |
| [Narrative and interaction runtime](narrative-interactions/README.md) | NarrativeOwner | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [player-queries](player-queries/README.md), [resource-transactions](resource-transactions/README.md) |
| [Repeatable jobs, quests and incidents](activity-instances/README.md) | ActivityOwner | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [resource-transactions](resource-transactions/README.md) |
| [Resources, inventory and reservations](resource-transactions/README.md) | ResourceOwner | [capability-profiles](capability-profiles/README.md) |
| [Checkpoint storage, replay segments and receipt retention](checkpoint-history/README.md) | ScenarioHistoryOwner | [capability-profiles](capability-profiles/README.md) |
| [Reproducible generation and measured scale profiles](generation-scale/README.md) | GenerationOwner | [capability-profiles](capability-profiles/README.md) |
| [Optional multiplayer authority and scoped replication](multiplayer-authority/README.md) | MultiplayerAuthorityOwner | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [player-queries](player-queries/README.md), [world-requests](world-requests/README.md) |
| [Play contexts and encounter rulesets](play-contexts/README.md) | PlayContextOwner | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [world-requests](world-requests/README.md) |
| [Godot native runtime host](native-godot-host/README.md) | ScenarioGameHost | [capability-profiles](capability-profiles/README.md), [logical-time](logical-time/README.md), [world-requests](world-requests/README.md), [player-queries](player-queries/README.md) |

## Composing a game

A game selects one registered IGameLayer and an exact capability profile. It can combine adventure, survival, progression and encounter capabilities without a genre enum in Core. An extension serves its named owner/consumer rather than creating a separate mother engine. Keep GameBase.Init/Update and the single immutable candidate commit.

The [campaign RPG package](campaign-rpg-layer/README.md) applies this to recruitment, headquarters, party battles, duels and army contexts. The existing selected Lost Soldier design remains in [chapter 05](../05_LOST_SOLDIER_LAYER.md) and its [authoring guide](../08_SCENARIO_AUTHORING_GUIDE.md); it consumes suitable capabilities without duplicating their shared policy. Earlier rejected identity proposals stay in their research records.

## Package maintenance and validation

Maintain these packages in place. Compatibility IDs/schema versions are data contracts; do not create v2/v3 document copies. Chapter 09 now links the detailed owners and retains cross-package decisions instead of being a competing full design.

Run [Validate-Sdd.ps1](../../../tools/Validate-Sdd.ps1) for recursive local links/anchors, package inventory/dependencies, structural definition examples and deliberate invalid mutations. Structural acceptance does not execute the future semantic validators or C#/Godot consumers. Existing engine verification remains separately recorded in [execution evidence](../../../artifacts/verification/evidence.json).
