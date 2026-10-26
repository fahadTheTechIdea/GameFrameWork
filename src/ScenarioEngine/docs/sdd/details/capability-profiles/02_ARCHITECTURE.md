# Capability profiles and layer composition: architecture

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Capability profiles and layer composition SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

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

GameProfileCatalog owns this capability's policy, registration/configuration and follow-up work. Large implementations may use partial files, retaining one owner/registration. Variant algorithms or providers get separate implementations of the shared capability contract; generic callers see the contract. Thin Godot adapters carry no duplicate domain policy.

Required packages: No other proposed package is required; this defines the composition boundary used by consumers.

Expose the capability through a focused contract only when the complete consumer needs it. Do not expand every IGameLayer implementation with unrelated mandatory methods.

~~~mermaid
flowchart TD
    Host[Trusted host or authoring consumer] --> Game[GameBase and session boundary]
    Game --> Core[Generic commit owner]
    Core --> Owner[GameProfileCatalog]
    Owner --> State[Owned candidate state]
    Owner --> Contracts[Registered schemas and required capabilities]
    Host --> Native[Native Godot adapters when spatial or interactive]
~~~

## State and side-effect ownership

| Owned state | Fields | Responsibility/invariant |
| --- | --- | --- |
| ProfileResolution | profileId, profileVersion, contentDigest, layerIdentity | Immutable accepted profile; no automatic provider substitution. |
| ModuleBinding | moduleId, exactVersion, dependencyIds, ownedStateSections | One binding per ID; cycles and duplicate writers are invalid. |
| HostMatch | hostCapabilityId, providedVersion, requiredVersion | Only documented compatible combinations are eligible. |

The owner may read other kit views and request typed mutations, but it does not keep a second authoritative copy. File/network/UI/native work happens after a committed request or outside rule resolution through the owning adapter. If one collaborator fails, the candidate is discarded; a layer may not partially commit its own result.

## Native integration

Use [the Godot host design](../native-godot-host/README.md) and [native service inventory](../../06_GODOT_RUNTIME.md). Physics/collision, navigation/AStar, Resources/PackedScene/ResourceLoader, Timer/signals, InputMap, animation and Control UI retain their native owners. Pure C# owns this package's game-specific rules and saved logical state. Headless synthetic observations prove rule behavior only.

## Alternative and boundary decision

Putting this capability into genre branches in Core would couple unrelated games; duplicating its state in scenes would break saves/replay. The proposed owner composes through current boundaries and the declared extension contracts. Implement the minimum complete consumer first, then promote genuinely reused variants after substitution checks.

## Detailed engine integration decisions

The following design detail is relocated from the earlier overview and is maintained here as this capability's owner. The configuration DTO above is the setup definition; supplemental runtime/profile records below serve distinct roles. All remain proposed until implemented with the consumer.

Proposed GameCapabilityProfile fields:

| Field | Meaning and validation |
| --- | --- |
| profileId/profileVersion | Exact registered profile identity; part of save/replay compatibility |
| layer identity | Existing exact layer ID/version; remains the single authoritative runtime |
| advancement policy | Registered policy ID/version and bounded configuration |
| required host capabilities | Versioned needs such as native 2D world, grid queries or dialogue presentation |
| module identities | Trusted, explicitly registered kit IDs/versions; bounded configuration schemas |
| state schema versions | Declared versions for world/progress/narrative state actually used |
| completion policy | Bounded session ending or ongoing world with separately scoped activities; no accidental world reset at mission completion |
| content/generator identities | Exact dependency digests and generation algorithm version where applicable |

Content validation checks the entire required profile before a session is selectable. Missing host capability produces CAPABILITY_UNAVAILABLE with the missing identity. An unknown module/configuration field is refused; no substitute module is selected silently. Public JSON cannot name a DLL, arbitrary CLR type, scene script or native resource path.

Keep IGameLayer and ILayerRuntime as the kernel's execution boundary. Initial kits are explicit collaborators of their owning layer. Introduce a shared IGameFeatureModule composition contract only when two real layers need interchangeable modules; do not impose it on Counter, Signal or simple future layers.

For a composite layer, validate module dependency cycles, duplicate operation/effect/fact keys and overlapping state writers at construction. Resolve modules in declared dependency order with a stable ID tie-break. Each kit owns a namespaced state section; cross-kit transfers ask the responsible owner inside the same candidate transaction. One failing module discards the whole tick. Kits may publish domain events/world requests but may not perform file, UI, network or native scene side effects while resolving.

A narrative+survival profile is an engineering composition example, not a selected new game premise. Its future JSON shape may declare profile, capabilities and module identities, but the current content schema does not accept those new fields. Extend that schema, loader, runtime, save/replay metadata and tests together before publishing a runnable example.
