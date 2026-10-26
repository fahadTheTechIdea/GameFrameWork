# P11-C3 — Conditional dependency-aware playbooks

Status: focused slice verified,2026-10-07.71 checks/ONE exact NEW bounded case, final clean build
2.224.0/save128; controller view2/system1/native codec3. TWO builds/TWO same-case runs include
actual authored stock-budget repair; full P11/goal remain open. Previous gates are historical.

## 0. Specification

Before this slice, PlanPlaybookDef/Playbook stored one positional _step cursor, pending ticket and first-plan link.
A blocked step prevents unrelated strategic work; JSON cannot declare dependencies, priorities or readiness.
Revise this SAME native provider, not another AI/scheduler/plan executor. Success: authored stable steps
depend on actual completed native plans, independent steps can be queued together within command budget,
and typed private conditions control eligibility. All physical work remains in WorkBoard/WorkSystem.

## 1. Goals

1. Required after/priority/conditions per step; stable IDs, missing/cyclic dependencies fail composition.
2. Reuse sole DependencyGraph compiler; ready selection sorts explicit priority then stable ID.
3. Public strategy condition contracts use private PlayerView only; native local signals reuse shared gates.
4. Save one pending/first-plan execution link per stable step, never copied plan progress/completion.
5. Read-only step diagnostics and decisions share evaluation, own enrollment/visibility and ordinary previews.
6. Normal commands, real material/hauling/construction, handover and pending/partial saves stay authoritative.

## 2. Design

Revise Standard/PlanPlaybooks.cs in place: immutable step topology and priority order; provider retains
one record per declared stable ID. Resolve all actual answers before reading the private native plan board.
Dependencies read real Done, not group labels or a controller-completion Boolean. Pending/active/failed/
cancelled/missing plans keep their own status/blocker. Independent ready nodes proceed even when another
branch is blocked. Submit fresh normal PlacePlanTemplateCommand up to existing CommandsPerDecision.
Retain all linked native plans through IPlanHistoryRetention; no alternate queue/job/stock/pacing owner.

New public IPlayerStrategyCondition/PlayerStrategyConditionDef and native SignalStrategyConditionDef
in Standard/PlayerStrategyConditions.cs. Explicit entity/mode(any/all)/conditions use actual private
prototype instances and SignalGateCondition; no conditions inferred. Shared ReadFromView eliminates
duplicate pressure/strategy signal-read interpretation. PlayerDecisionContext guards pure observation;
native diagnostics use the same guard. Providers cannot receive World/submission through the condition
contract. Existing controller system remains cadence/mail/codec owner; native provider codec3.

Optional IPlanPlaybookDiagnostics/IPlayerControllerStateValidation contracts expose only detached intent
facts and validate actual pending/mail/native plan links. System/private view delegates, never concrete
provider type checks. Exact observer only; malformed negatives/nonseats receive no controller data.
New controller view2; system1 unchanged. Engine identity changes; no old cursor/JSON reader.

## 3. Authoring/API

plan_playbooks.steps rows require id,template,group,anchor,after:[],priority,conditions:[aliases].
signal_strategy_conditions rows require id,entity,mode:any/all,conditions:[typed shared signal gates].
Public condition sections may be registered by content modules, without playbook/scheduler edits.
PlaybookStepSnapshot reports ID/dependencies/priority/status/blocker/pending/first-plan/count, not commands.
PlayerControllersView.CopyPlaybook(company,destination) requires actual own enrollment/provider.

## 4. Files

PlanPlaybooks, PlayerControllers/System/Module/View, PlayerDecisionContext, shared SignalGateCondition
and pressure reader delegation; new condition contracts/provider. Explicit new JSON playbook/conditions/
public-provider world fixtures; migrate old playbook JSON and old codec fixture to current layout.
EngineInfo/current identity assertions compile only. ONE new exact physical test; tracker/docs/evidence.

## 5. Errors/compatibility

Missing/duplicate node/condition/edge, cycles, invalid priority/mode/unit/source: ContentException.
Condition wait/unavailable and normal command refusal retain typed blocker. Pending/linked native plans
must correlate to actual mail/inbox/own board; malformed saved IDs/count/range/overlap refuse.
No old positional cursor, implicit sequential edges, default conditions or compatibility alias.

## 6. Verification

After implementation: one clean build and ONE exact NEW bounded scenario:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Player controllers: conditional dependency playbooks and physical independent branches"
```

Only actual failed new gate may be repaired/rerun; record every invocation. Older tests compile only.
No family/full/platform/stale-binary run or gameplay/test change after final green.

## 7. Risks/exclusions

This is authored strategic planning, not a complete industrial rival planner. Rich risk/maintenance/
survey preferences, generated expansion sites, finite-field competition, arbitrary CPU sandbox and
broader corruption/replay/lockstep/stress/full acceptance remain open. Scoped observation guards are
not global information-flow isolation. No business/host/demo/OilFieldDays/Draw/platform changes.

## 8. Execution

- [x] Revise native playbook topology/links/diagnostics and pure condition contracts.
- [x] Author public/native condition proof and actual physical branches through ordinary commands.
- [x] Build/run ONE distinct new case; record both attempts and freeze gameplay/tests after green.
- [x] Update tracker/docs/current scoped source evidence; full goal remains active.

[Verification](../../docs/migration/P11_PLAYBOOK_DEPENDENCIES_VERIFICATION.md) records final71 checks,
all invocations, real local reserve fixture repair, physical/native ownership and remaining gates.

[Parent](P11_PROGRESSION_KNOWLEDGE_CONTROLLERS_DESIGN.md), [tracker](MASTER_TRACKER.md).
