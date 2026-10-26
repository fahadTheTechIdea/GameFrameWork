# P12-A9: external incident, response and environmental contracts

## 0. Problem statement

Original P12.acceptance.007 requires an external incident/response/environment provider with no scheduler or client gameplay changes. Standard/IncidentWorkflows.cs currently seals the reusable Incident owner; IncidentBindings.cs requires its exact runtime type. Independent metadata exists, but an external component cannot reuse the engine-owned episode algorithm under its own definition.

## 1. Goals

1. External component/metadata reuse the sole engine Incident episode algorithm and codec through IIncidentEpisodeDefinition, with no second state machine or new gameplay in the client.
2. Actual immutable owner/runtime-family/instance/slot/state/workflow aliases still validate before Initialize and during native guarded reads.
3. Master-selected external workflow, completion and environmental metadata use existing declarations and interface catalogs, never native metadata base classes or scheduler edits.
4. One new exact case proves two clock resolutions, physical incident response, paused waste WIP, recovery/disposal, cancellation, private saved observations, exact rates, eviction, claims, replay and isolation.

## 2. Design

Open Incident for derivation without moving or duplicating its algorithm. IncidentBindingContracts.Actual admits only the declared reusable runtime family and requires an actual entity component's exact runtime to match its immutable definition. ValidateComposition requires IIncidentEpisodeDefinition for every Incident-derived runtime; original codec2 and full state/workflow admission remain.

An empty ExternalIncident subclass in the non-friend ExtensionProof assembly reuses all engine behavior. ExternalIncidentDef implements IIncidentEpisodeDefinition through the existing IncidentEpisodeDeclaration and public binding validation. A single module registers this component plus existing independent workflow/completion/environment definitions. Its Compose checks actual frozen aliases/type ownership, not gameplay.

PhysicalRecoveryScenario assembles two external-owned JSON documents from existing physical content and a master-listed file declaration. Metadata/native sections are selected once under the external owner; no duplicated definitions or compatibility paths. Shared environmental acceptance Scenario exposes an optional external authoring flag. The new case drives the same public engine commands/views, not the old registered suite.

## 3. API surface

Incident becomes a reusable public base component; episode schema2 remains unchanged. Engine build2.426.0, save154/replay1. No scheduler, command, codec-shape, renderer or adapter API added.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | src/Beep.SimEngine/Standard/IncidentWorkflows.cs | 2 | Medium |
| Modify | src/Beep.SimEngine/Standard/IncidentBindings.cs | 6 | Medium |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs and tests/Beep.SimEngine.Tests/testdata/current-engine-protocol.json | 2 | Low |
| New | tests/ExtensionProof/ExternalResponseProof.cs | 55 | Medium |
| New | tests/ExtensionProof/content/external-response-module.json | 1 | Low |
| New | tests/Beep.SimEngine.Tests/ExternalResponseAcceptanceTests.cs | 18 | Low |
| Modify | shared physical authoring, environmental driver, registration and resource project | 32 | Medium |

## 5. Error handling

Missing/foreign/wrong runtime or immutable alias still refuses composition. Physical stages remain512 Updates with native diagnostics; outer90000ms/termination5000ms/output30000 characters. Observe handles to terminal; no restarting on observer timeout.

## 6. Compatibility

No shim or duplicate state. Native default Incident remains the ordinary provider; external definitions share its one implementation. Development build identity changes, no old-save compatibility introduced.

## 7. Verification

One solution compile; ONE NEW exact case `External response acceptance: engine-owned episode workflow and environmental providers`. Same-case repairs only, no old/family/full runtime. [Evidence](../../docs/migration/P12_EXTERNAL_RESPONSE_ACCEPTANCE_VERIFICATION.md). Literal .007 is checked only after external physical proof succeeds.

## 8. Risks

Opening runtime reuse must not accept incorrect definition/runtime pairs or relax original schema/owner checks. Shared driver authoring must select external module/documents explicitly, not silently substitute native metadata. No mutable cross-session proof state.

## 9. Out of scope

OilFieldDays, business/Tycoon, rendering/platforms, arbitrary-provider fault/purity coverage and broad regression/publication before full goal/plan completion.

## 10. Execution order

- [x] Review native incident binding/state, independent metadata and physical proof.
- [x] Open the single episode owner with strict actual-runtime binding.
- [x] Register external metadata/module through master JSON and one thin case.
- [x] Compile and execute the bounded case; retain all attempts.
- [x] Refresh actual tracker/audit/evidence and remaining scope.
- [ ] Full regression remains unrequested and unproven.
