# P5-A5: Native incidents and fleet manufacturing — Design Document

## 0. Problem Statement

Oil Wear duplicates the framework incident episode, stores a nullable response plan, and posts repair work itself. Autopilot uses removed production queues. PowerSource accepts a concrete signal definition rather than the signal contract.

## 1. Goals

1. Incident state, episode serialization, response priority and repair posting have one framework owner.
2. Oil failure probabilities reference explicit named incident sources; no default repair response.
3. Fleet manufacturing discovers actual named operations and uses private immutable transformation facts and ordinary commands.
4. Signal providers accept IAutomationSignalContent without concrete definition assumptions.
5. Preserve open runtime gates until composition, physical execution and durability are proven.

## 2. Design

Existing WearDef now contains immutable failure probability metadata: kind, source and game duration. ValidateCore requires the actual named IIncidentEpisodeDefinition with the same workflow kind and positive-time physical response.

Wear is stateless and projects active native incident facts for inspection. WearSystem rolls hourly using the original game time and RNG, excludes construction sites and already-troubled entities, then raises the named IIncident. It never owns an episode or posts/resumes repair work. The existing IncidentResponseSystem and WorkBoard own those functions.

Each authored entity receives an incident array: one native episode owner per failure source. Four incident_workflows map the existing repair activities and actual node IDs. The common completion effect uses bound_response, not an ambiguous hardcoded wear source. Storyteller references the correct named breakdown/blowout source. Different sources can have independent native episodes; no old single-field behavior is promised.

Autopilot enumerates OperationsView identities in stable ordinal order, reads TransformationManagementView queues/outputs, and submits EnqueueTransformationCommand with the actual capability and transformation. It counts actual queued entries, not a guessed producer or parallel production model.

PowerSource.ReadSignal takes IAutomationSignalContent and retains typed supported/unavailable results.

## 3. API surface

Wear metadata: failures entries require kind, source and mtbf_days. Native incident metadata requires id, workflow and active-state binding. Existing transformation and signal contracts are used directly. Removed Wear.ResponsePlan, Failure and episode codec are not replaced with aliases.

## 4. Files to change

Existing Oil Operations.cs, Autopilot.cs and OilModule.cs; structures, vehicles, effects, manifest and storyteller JSON. New content/oil/incident-workflows.json. Canonical design, evidence, tracker and audit.

## 5. Error handling matrix

- Missing/duplicate probability source or zero duration: content refusal.
- Missing/wrong-kind native incident source: actual component validation refusal.
- Missing runtime incident: explicit error, no fallback.
- Unsupported/private transformation: existing view/command refusal.
- Unknown signal metric: typed signal unavailability.
- Response cancellation/failure: native episode/work-board rules; no replacement repair queue.

## 6. Backward compatibility

Development only. The old Wear episode codec and repair posting implementation are deleted. No compatibility classes, default response or legacy purchase/production path.

## 7. Verification

One scoped OilCo.Game Release --no-restore compile after the completed source slice. Source JSON inspection checks all probability/source/workflow links. No gameplay executor or full regression while integration remains incomplete. Physical repair, fault isolation, private controller behavior and save/load remain required.

## 8. Risks

The remaining Fog declaration error masks body/API errors. Old module and work JSON composition is still pending; authored source inspection is not engine loading. Native phased response requires current activities and the incident module. No successful compilation or runtime equivalence is claimed.

## 9. Out of scope

Framework production changes, platform/Draw/adapters, Tycoon business behavior, hosts/tests and final full regression. Fog and full module/content migration remain next steps.

## 10. Sub-task execution order

1. Replace duplicate Wear state with native incident bindings.
2. Author explicit workflows/sources and bound completion.
3. Migrate named fleet queues and signal contract.
4. Compile once, inspect source references, record incomplete integration.

## Outcome

Source implemented: 26 entities, 30 probability bindings and four actual response workflows. Build exit 1, zero warnings, one reported Fog declaration error; later errors can surface. No runtime or original acceptance gate closed. See [evidence](../../docs/migration/P5_OIL_INCIDENT_FLEET_MIGRATION_VERIFICATION.md).
