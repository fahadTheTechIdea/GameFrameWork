# P5-A41: Oil strategy uses the existing engine controller coordinator

## 0. Problem

Oil AutopilotSystem duplicated framework PlayerControllersSystem scheduling. Canonical Oil scenarios did not select OilSettings.Playbook, so that separate system returned without deciding even after a seat selected Autopilot.

## 1. Goals

Delete the separate Oil scheduler, author profiles through the master JSON, use the existing coordinator for cadence/replies/persistence, and prove ordinary founding/selection/saved pending commands. Keep strategy and physical rules inside engine assemblies.

## 2. Design

CompanyControllerDef implements the existing IPlayerControllerDefinition through PlayerControllerDef. It resolves ICompanyPlaybookDefinition from content and creates a distinct CompanyAutopilot per bound company. Strategy has only observation buffers, no retained World or new persistent store. It decides through PlayerDecisionContext.View/Preview/Submit.

PlayerControllersModule accepts an explicit default_profile plus existing companies overrides. It resolves that exact profile and fills only actual session seats not explicitly overridden. Null/absence invents no binding; a missing reference refuses before a game is returned. The existing PlayerControllersSystem remains the sole scheduler/reply/state owner.

Oil master declares sim.player-controllers and oil/controllers.json. Oil requires that module, registers its profile type and removes AutopilotSystem registration. Every authored scenario explicitly binds oil.company.standard. Cadence is two hours and the authored command budget is32; both are JSON policy, not framework caps. decision_hours and the old scenario Playbook selector are deleted. Existing one-hour recovery/port fixtures now edit the profile cadence instead.

PlayerDecisionContext exposes actual RemainingCommands so providers can honor their authored budget without duplicating it or exceeding it. Existing oversubmission refusal remains. PlanningView owns scenario-dependent headquarters recovery placement so the provider need not capture mutable scenario/world services.

## 3. API/state

RemainingCommands is a scoped read-only context fact; default_profile is authored selection, not a fabricated default. Oil planning view becomes2. Core2.557.0/save156/replay1. No compatibility alias, extra clock, changed physical work rules or new controller store.

## 4. Files

Existing PlayerControllersModule/PlayerDecisionContext and current build/protocol identity. Oil Autopilot/Settings/Views/module, master/scenarios/playbooks and new controller JSON. Existing public handover tests and recovery/port fixture API migration. Native controller test authoring/current-codec probes. Stage/evidence/tracker.

## 5. Errors

Unknown profiles refuse. Budget exhaustion stops further strategy submissions; the coordinator's own quota remains authoritative. Provider creation/state/selection and ordinary command admission use existing engine guards. No direct spawn, stock, money, progress or movement writer in the provider.

## 6. Compatibility

Delete AutopilotSystem/DecisionHours/OilSettings.Playbook and their authoring. No parallel scheduler or legacy reader. Captured current codec corruption tests replace obsolete manual schema3 payloads; the actual native plan codec is5.

## 7. Verification

Exact authored96x96 handover with initial pending founding command, private profile facts, two-hour cadence, saved pending twin, ordinary physical command application, return to human and save. Thin demo horizon. Native binding/selection/context/parity/codec probes. One affected named-port physical/rival case. Focused only.

## 8. Risks

Changed scheduling order now uses the established controller coordinator900, whose commands apply next step. Full recovery, expansion, pressure, every optional profile, replay/lockstep and performance acceptance remain open. This stage is not the full engine baseline.

## 9. Out of scope

Financial rules/amounts, Tycoon behavior, adapters/Draw/platforms, full regressions and Git writes.

## 10. Status

Implementation complete; scoped finals601/0 across four exact cases, including physical named-port/saved rivals479/0. Initial native fixture/codec failures are retained. Original221/194 recorded checked/27open unchanged; no gate closure. [Evidence](../../docs/migration/P5_CONTROLLER_COORDINATOR_MIGRATION_VERIFICATION.md).

