# Recruitment and headquarters campaign RPG: architecture

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Recruitment and headquarters campaign RPG SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Reuse before additions

| Existing seam | Authoritative source | How this package builds on it |
| --- | --- | --- |
| Game lifecycle | [IGame / IGameLayer / ILayerRuntime](../../../../src/ScenarioEngine.Contracts/Simulation.cs) | Register one exact game layer; keep Init/Update/Submit/Restore and candidate-state ownership. |
| Composition | [GameBase](../../../../src/ScenarioEngine.Core/GameBase.cs) | The base owns lifecycle. Domain/extension behavior does not override commit ownership. |
| Definitions | [Definitions and capabilities](../../../../src/ScenarioEngine.Contracts/Definitions.cs) | Reuse conditions, facts, schemas, objectives and exact layer identities. |
| Content | [IScenarioContentService](../../../../src/ScenarioEngine.Contracts/Infrastructure.cs) | Reuse pack/schema/catalog ownership. New definition documents need explicit loader/schema support. |
| Persistence/reporting | [Save/reporting interfaces](../../../../src/ScenarioEngine.Contracts/Infrastructure.cs) | Keep codec/store/recovery and IEngineReporter boundaries; no private parallel save/log service. |
| Rule replay | [Replay contracts](../../../../src/ScenarioEngine.Contracts/Replay.cs) | Record accepted inputs, explicit observations and compatible state/output identities. |

## Responsible owner and dependencies

CampaignRpgLayer owns this capability's policy, registration/configuration and follow-up work. Large implementations may use partial files, retaining one owner/registration. Variant algorithms or providers get separate implementations of the shared capability contract; generic callers see the contract. Thin Godot adapters carry no duplicate domain policy.

Required packages: [capability-profiles](../capability-profiles/README.md), [progression-scopes](../progression-scopes/README.md), [play-contexts](../play-contexts/README.md), [logical-time](../logical-time/README.md), [narrative-interactions](../narrative-interactions/README.md), [resource-transactions](../resource-transactions/README.md), [player-queries](../player-queries/README.md), [persistent-world](../persistent-world/README.md)

The proposed layer implements IGameLayer and returns one ILayerRuntime. Its selected collaborators operate on the same candidate snapshot; Core does not branch on its genre.

~~~mermaid
flowchart TD
    Host[Trusted host or authoring consumer] --> Game[GameBase and session boundary]
    Game --> Core[Generic commit owner]
    Core --> Owner[CampaignRpgLayer]
    Owner --> State[Owned candidate state]
    Owner --> Contracts[Registered schemas and required capabilities]
    Host --> Native[Native Godot adapters when spatial or interactive]
~~~

## State and side-effect ownership

| Owned state | Fields | Responsibility/invariant |
| --- | --- | --- |
| CampaignRoster | stable character identity, recruitment/availability, equipment, progression | Party/army/headquarters views share this owner. |
| HeadquartersState | services, assignments, growth milestones and region revision | Recruitment unlocks services once. |
| CampaignPlayState | chapter, active context/checkpoint, return context, applied result ledger | Battle completion does not close the entire campaign. |

The owner may read other kit views and request typed mutations, but it does not keep a second authoritative copy. File/network/UI/native work happens after a committed request or outside rule resolution through the owning adapter. If one collaborator fails, the candidate is discarded; a layer may not partially commit its own result.

## Native integration

Use [the Godot host design](../native-godot-host/README.md) and [native service inventory](../../06_GODOT_RUNTIME.md). Physics/collision, navigation/AStar, Resources/PackedScene/ResourceLoader, Timer/signals, InputMap, animation and Control UI retain their native owners. Pure C# owns this package's game-specific rules and saved logical state. Headless synthetic observations prove rule behavior only.

## Alternative and boundary decision

Putting this capability into genre branches in Core would couple unrelated games; duplicating its state in scenes would break saves/replay. The proposed owner composes through current boundaries and the declared extension contracts. Implement the minimum complete consumer first, then promote genuinely reused variants after substitution checks.

## Detailed engine integration decisions

The following design detail is relocated from the earlier overview and is maintained here as this capability's owner. The configuration DTO above is the setup definition; supplemental runtime/profile records below serve distinct roles. All remain proposed until implemented with the consumer.

Status: proposed follow-up from the owner's Suikoden question, researched 25 October 2026. [Research section 9](../../../research/GAME_TYPE_ENGINE_CAPABILITY_RESEARCH.md#9-follow-up-a-suikoden-style-campaign-rpg) records the primary sources and distinguishes the I/II battle systems. This case exercises the same kernel; it is not a commitment to replace Lost Soldier or implement an entire RPG now.

### 14.1 One campaign, several play contexts

Keep GameBase.Init/Update/Submit/Restore and one registered CampaignRpgLayer identity. A proposed IEncounterRuleSet is an optional domain contract implemented by separate party, duel and army rule owners. It is not a new interface that every game layer must implement. Generic Core knows the commit/advance/result protocol; it knows no Suikoden character, Rune, castle or formation rule.

The game-layer owner selects its active context through validated registered definitions. A context's identity, supported commands, advancement profile and checkpoint state determine legal behavior. Godot switches presentation through native scenes and signals. Do not reset the whole IGame or construct a fresh campaign when entering a battle.

| Proposed state | Required contents and owner |
| --- | --- |
| Campaign roster | Stable CharacterId, recruitment/availability status, progression, equipment and role eligibility; roster owner |
| Party selection | References to roster identities, formation slots and authored restrictions; party owner |
| Headquarters | Service unlocks, assignments, growth revision and persistent location changes; headquarters owner |
| Story | Chapter, completed decisions, recruitment opportunities and narrative checkpoint; narrative/recruitment owners |
| PlayContextFrame | ContextId, kind, rules/profile identity, source campaign revision, participants, state/checkpoint and parent/return reference; context owner |
| Encounter participant | Stable source character reference and explicitly scoped tactical state; encounter rule owner |
| EncounterResult | EncounterId, result ID, source revision, bounded typed deltas, return context and status; owning layer validates/apply once |

Only one context is active initially. Bound saved return contexts and refuse illegal nesting rather than inventing an unrestricted scene stack. Exploration can enter a party battle, duel or army encounter; an accepted terminal encounter result returns to its recorded parent. Story-driven chains require explicit validated transitions. Quitting to the menu preserves the campaign and active checkpoint.

### 14.2 Safe transitions and outcomes

1. Validate entry conditions, participants, required content and profile; create a candidate context and freeze the source revision.
2. Commit a context-entry request. Native preparation follows the acknowledged request protocol; failed loading preserves the source context and costs.
3. Admit matching readiness and activate the encounter. Its recorded advancement profile decides whether time advances on actions/rounds or on native fixed steps. Define whether the parent world's clock is frozen; no hidden switch from milliseconds to turns.
4. Resolve commands against the encounter candidate. Narrative, resource and combat outcomes still obey the whole-commit transaction.
5. At encounter completion, validate/apply its result once using EncounterId/result ID and expected source revision. Commit character/equipment/resource/story changes and the applied-result record together.
6. Restore the saved parent location/context after native readiness. Duplicate completion or retry cannot repeat rewards, casualties, recruitment or headquarters unlocks.

An encounter's victory is a scoped result, not the root scenario EndingId. The campaign closes only through its declared final closing policy. If the initial bounded-scenario implementation uses a separate session for a battle, the progression owner must preserve the parent checkpoint and apply the result atomically; session replacement cannot become an untracked reset. Prefer an active-context contract when several rulesets share one game's campaign state.

### 14.3 Large cast and authoring

Recruitment is a state transition over a character definition, not a counter increment. Track eligibility, joined/temporarily unavailable/departed status and declared role capabilities separately from combat health. The game decides any death, missed-opportunity or return policy explicitly. A noncombat service character need not implement an attack action.

The campaign roster is persistent logical data. Native actors are instantiated for the active party, visible location residents and active encounter. Headquarters placement/portraits/menus derive from stable roster references; membership in multiple views does not duplicate the character's authoritative equipment or progression. Never interpret a roster of 108 definitions as a requirement for 108 continuously simulated bodies.

Authoring validates character, ability, service and story references; recruitment prerequisites; chapter gates; formation/role eligibility; combined-action participants; context entry/return routes; rewards and final closure. Required story progress must remain reachable under every supported recruitment-loss policy. Deliberate missable content is declared; broken or cyclic prerequisites are content errors. Optional conversation/quest instances do not require a fixed global fact for every possible roster interaction.

### 14.4 Acceptance before claiming support

All checks are proposed/not_run. A small original-content proof is sufficient to test the architecture; full-roster art/dialogue/content completion is a separate production gate.

| Check | Observable result |
| --- | --- |
| RPG-A01 Recruitment | Repeating a recruit command or restoring/replaying its outcome creates one member and one service unlock |
| RPG-A02 Roster ownership | Party, army and headquarters views reference the same character; equipment/progression cannot diverge across copies |
| RPG-A03 Mode continuity | Exploration -> party battle -> exploration, duel and army encounter retain campaign state and the intended parent clock policy |
| RPG-A04 Encounter checkpoint | Save/restart during command selection and after result commit resumes the same phase; awards/losses apply once |
| RPG-A05 Failure | Missing battle content, native load failure and stale/duplicate results preserve the last valid context and produce explicit diagnostics |
| RPG-A06 Story authoring | Invalid recruitment dependency/service/return target is rejected; supported chapter branches retain required progression |
| RPG-A07 Scale distinction | Load the full declared roster catalog while spawning only the bounded active native population; measure both separately |
| RPG-A08 Kernel independence | New encounter rule variants register in the RPG layer without adding party/duel/army branches to generic Core or GameBase |
