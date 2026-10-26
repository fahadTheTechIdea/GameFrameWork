# P12-A2 — Incident signals and physical process isolation

Status: focused verified, 2026-10-06; broader P12 remains partial.
Parent: [P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md).

Current build2.95.0/save61; new stateless incident_signal component codec1/signal-schema1.
Existing incident/work/gate/transformer state layouts remain unchanged. Clean solution build;
**93 checks/two exact suites** (41 authoring/composition;52 physical), zero failures/aborts.
[Scoped evidence](../../docs/migration/P12_INCIDENT_ISOLATION_VERIFICATION.md).

## 0. Problem

P12-A1 phases progress physically, but their names do not stop processing. Existing
`Standard/TransformationSignalGates.cs` and `TransformationExecutionContext.CheckGates` already own
process admission, ongoing-work gating and immutable readiness facts. Use that path, not another
operation scheduler, isolation ledger, enabled-state mirror or host shutdown rule.

## 1. Goals

1. Native incident signals derive exact named episode and compiled phase completion facts.
2. JSON gates can stop startup or in-flight batch/continuous work, preserving native WIP/tools/progress.
3. Cancellation/failure cannot unlock an isolated process; actual whole-response completion is required.
4. Existing explicit enabled/disabled intent remains intact: safety gating must not auto-enable a
   manually disabled operation or let manual enable bypass readiness.
5. Two exact suites prove authoring, physical isolation/restart and deterministic saved continuation.

## 2. Design and authority

Add `IPhasedIncidentDefinition : INamedCapabilityDefinition` exposing the existing workflow. Native
IncidentDef implements it; independent providers can declare the same metadata contract.

Selected `sim.incidents` declares a stateless named `incident_signal` component. JSON explicitly names
`id`, incident `source`, `workflow` and `phase`. All references must resolve on that same entity; the
source definition/runtime must implement phased contracts, and the workflow/phase must match exactly.
Missing/unknown/duplicate ownership is an authoring error, not a guessed incident or generic shutdown.

The component implements existing `IAutomationSignalSource`/`IAutomationSignalDefinition`, schema1,
with boolean metrics `incident.active` and `incident.phase-hold`, unit `unit.boolean`.
`active` means the exact source episode is active. `phase-hold` means active AND all mapped native
nodes of the explicitly selected phase have completed. It remains true through later work, cancellation
and permanent failure, and becomes false only after whole-response resolution. No separate phase,
hold flag, enabled-state snapshot, timer, command, system or progress codec is added.

The one IncidentPhases reader validates session/episode/history before projection. Its shared phase
completion query uses the existing WorkBoard records and workflow mapping. Initial/nonposted active
work is explicitly not yet phase-complete, not a fabricated unavailable/zero signal. A missing actual
source or incompatible workflow reports typed unavailability; it never substitutes another provider.

Existing JSON `signal_transformation_gate` binds either signal with expected=false. An active-signal
gate permits immediate emergency shutdown; a phase-hold gate models physical isolation work taking
time. This uses the unchanged process gate evaluator for startup, advancement and output completion.
Gate processing order remains transformation350 before response390/work400: a phase completed at400
affects the next process update, never retroactively changes prior progress. No same-step feedback loop.

Controls keep their existing provider-owned intent; no safety effect writes/replays a second enabled
value. Manual enable remains an admitted intent but cannot advance a gated process. Manual disable
survives response resolution. The same typed signals can be used by existing next-step automation for
other controllable operations; broader operation/network isolation is not claimed by this slice.

## 3. API

Native incident metadata contract, incident_signal JSON, existing AutomationSignalDef.ReadFrom,
OperationsView.ReadSignal and TransformationManagementView.ReadGates/PreviewEnqueue. No new host API
for commanding phases, no client-side safety predicate and no instantaneous restart command.

## 4. Files

Extend Standard/IncidentWorkflows.cs, IncidentResponseModule.cs and existing signal/phase contracts.
Add Standard/IncidentSignals.cs; public proof JSON plus two focused suites. Update parent/master,
evidence, README, help and AGENTS. Native incident/gate/work/transformer codecs remain unchanged;
the new stateless component has a declared schema but no payload. Build identity changes cleanly.

## 5. Errors and risks

Invalid source/workflow/phase/schema/metric/unit fails content admission or returns existing typed
signal unavailability at runtime. Stale/malformed lifecycle rejects rather than grants safety readiness.
Pure reads must not change state or advance phase work. Holding WIP and native tool receipts while
paused must retain existing save validation and resource conservation. Never open gates from a failed,
cancelled, wrong-owner, forged or stale response.

## 6. Compatibility

Clean current semantics only. No legacy/shim/read fallback. No mandatory operation module or spatial
provider is added to incidents: signal registration uses existing schema contracts; actual signals/gates
require their explicitly selected existing modules.

## 7. Verification and order

1. Add metadata/strict signal authoring over existing phase/lifecycle rules.
2. Add non-oil JSON proof using a real native processor and timed leak-response DAG.
3. Prove active/phase-hold startup and in-flight isolation, blocked repair, cancellation/save/restart,
   preserved WIP/progress/tools, actual consumed/output quantities, manual intent and private views.
4. Build and run exactly two named suites, then record scoped evidence and tracker status.

All four slice steps are implemented and focused verified. The exact gates are
`Incident isolation: authored phase signals and gate admission` and
`Incident isolation: timed process shutdown and saved restart`.
The contract gate also proves mapless/no-operations composition and saved physical response using
ordinary native work. Earlier P12-A1 suites were not rerun. Other build identity assertions compile
only; no third/family/full regression gate is claimed.

## 8. Out of scope

Network disconnection, arbitrary non-transformation operation-gate migration, propagation, condition/
maintenance/preparedness, environmental accounting, failed-response retry policy, independent phased
provider matrix, broad replay/stress/full P12/P1–P12 acceptance. No Oil/Tycoon/platform/Draw work.
