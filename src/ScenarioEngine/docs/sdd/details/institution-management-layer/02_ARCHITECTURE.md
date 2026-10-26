# Institution and life-management layer: architecture

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Institution and life-management layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

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

InstitutionLayer owns this capability's policy, registration/configuration and follow-up work. Large implementations may use partial files, retaining one owner/registration. Variant algorithms or providers get separate implementations of the shared capability contract; generic callers see the contract. Thin Godot adapters carry no duplicate domain policy.

Required packages: [capability-profiles](../capability-profiles/README.md), [logical-time](../logical-time/README.md), [activity-instances](../activity-instances/README.md), [persistent-world](../persistent-world/README.md), [resource-transactions](../resource-transactions/README.md), [progression-scopes](../progression-scopes/README.md), [player-queries](../player-queries/README.md)

The proposed layer implements IGameLayer and returns one ILayerRuntime. Its selected collaborators operate on the same candidate snapshot; Core does not branch on its genre.

~~~mermaid
flowchart TD
    Host[Trusted host or authoring consumer] --> Game[GameBase and session boundary]
    Game --> Core[Generic commit owner]
    Core --> Owner[InstitutionLayer]
    Owner --> State[Owned candidate state]
    Owner --> Contracts[Registered schemas and required capabilities]
    Host --> Native[Native Godot adapters when spatial or interactive]
~~~

## State and side-effect ownership

| Owned state | Fields | Responsibility/invariant |
| --- | --- | --- |
| PersonState | needs, relationships, enrolled/assigned activities and availability | One identity across facility/location views. |
| FacilityState | capacity, service queue, staffing and condition | Reservations prevent overbooking. |
| CalendarState | day/period, scheduled instances, completed outcomes | No silent wall-clock/offline catch-up. |

The owner may read other kit views and request typed mutations, but it does not keep a second authoritative copy. File/network/UI/native work happens after a committed request or outside rule resolution through the owning adapter. If one collaborator fails, the candidate is discarded; a layer may not partially commit its own result.

## Native integration

Use [the Godot host design](../native-godot-host/README.md) and [native service inventory](../../06_GODOT_RUNTIME.md). Physics/collision, navigation/AStar, Resources/PackedScene/ResourceLoader, Timer/signals, InputMap, animation and Control UI retain their native owners. Pure C# owns this package's game-specific rules and saved logical state. Headless synthetic observations prove rule behavior only.

## Alternative and boundary decision

Putting this capability into genre branches in Core would couple unrelated games; duplicating its state in scenes would break saves/replay. The proposed owner composes through current boundaries and the declared extension contracts. Implement the minimum complete consumer first, then promote genuinely reused variants after substitution checks.
