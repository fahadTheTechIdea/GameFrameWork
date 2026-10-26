# P12-D4 — Operating thresholds and physical maintenance policy

Status: historically focused verified, 2026-10-06; not rerun in P9-B5. Build2.102.0/save64/policy1/condition view3/system3/recurring2/
work view31; condition2/WorkBoard22/work system2 unchanged.129 checks/two exact suites (108 contracts;
21 physical), zero failures/aborts, clean build. [Evidence](../../docs/migration/P12_OPERATION_POLICIES_VERIFICATION.md).
Full P12/P1–P12 remain partial; no third/family/full regression authorized or run.

## 0. Problem statement

`Standard/Operations.cs` owns normal control admission and exact enabled/target commits.
`EquipmentConditionSystem.Update` owns condition observation at order 370. Recurring sources
already submit ordinary plans through WorkBoard. These owners need policy composition, not
another enabled state, repair queue, scheduler or compatibility path.

## 1. Goals

1. Author explicit named policy/operation/condition/unit, criticality and three thresholds in master JSON.
2. Stop unsafe operations through their actual control owner; restart only policy-owned stops.
3. Enforce restart admission for player and automation through the existing registered command.
4. Link recurring plans to maintenance readiness and effective priority without rewriting admitted work.
5. Save/hash policy settings and latch once; expose private detached previews and validate providers.

## 2. Design

`IOperationPolicyDefinition` binds actual named `IControllableOperation` and `IConditionProvider`.
`IOperationPolicy` owns immutable current settings and shutdown latch/change tick only. Enabled/target
remain exclusively with the operation. Bands satisfy 0 <= shutdown <= maintenance < restart <=
actual maximum, in the condition's exact unit/quantum. Criticality is nonnegative; linked base priority
plus criticality must fit its actual integer representation, otherwise admission explicitly refuses.

The existing condition system captures all policy observations after its captured wear commits.
It disables actual enabled operations at/below shutdown. Automatic restart requires its own latch
and condition at/above restart. Manual restart uses ordinary controls. An explicit disable clears
policy ownership so the engine cannot undo player intent. Rate-only changes on enabled operations
use shutdown safety, not the restart band. Every policy-induced control is exact-checked by the same
control commit path. A provider refusal leaves a typed blocker; a lying/mutating provider faults.

Native recurring sources require `operation_policy: null` or an explicit named policy. A link ANDs
maintenance readiness with the existing trigger and derives base priority plus current criticality.
Existing WorkBoard remains sole plan/job/resource/timing owner. Admitted priorities never change
because a source or policy is edited. Metadata contracts permit other source/policy providers.

## 3. API surface

Named policy contracts; immutable settings/state/view; `ConfigureOperationPolicyCommand` and matching
EquipmentConditionView read/preview. No host gameplay, renderer, platform adapter or HTTP API.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| New | Standard/OperationPolicies.cs; Commands/OperationPolicyCommands.cs | high |
| Modify | Standard/Operations.cs; EquipmentCondition.cs/Module.cs | high |
| Modify | Standard/RecurringPlans.cs; WorkModule.cs; Views/EquipmentConditionView.cs | medium |
| New | tests/Beep.SimEngine.Tests/OperationPolicyTests.cs | medium |
| Modify | native fixture JSON, Program.cs, build identity assertions, docs/tracker/help | low |

## 5. Error handling

Missing/duplicate/foreign bindings fail composition or named admission. Invalid settings/overflow
refuse without allocating work. Unsafe enabling returns `operation.policy-unsafe`. Automatic control
refusal is visible; invalid provider commits fault Update and prohibit checkpoints. Native decode
stages settings/latch before replacing live values; future timestamps validate after full restore.

## 6. Compatibility

Development-only direct replacement. No old JSON defaults, alias, shim or save reader.

## 7. Verification

Build the solution; run exactly these two full names, no broader/full regression:

- `Operation policies: authored thresholds, controls and checkpoint ownership`
- `Operation policies: shutdown, physical maintenance and saved restart`

Cover strict JSON, privacy, command/preview parity, native codec, alternative public owners, manual
override, current target preservation, physical spares/tools/travel, saved continuation and twins.

## 8. Risks

Manual disable must release latch ownership, not be automatically reversed. Threshold edits affect
future observation, not an already-admitted plan. Captured observations precede any policy commit.
Order 370 affects ensuing process updates (process order 350), never retroactive production.

## 9. Out of scope

Fleet/preparedness, inspection science, arbitrary incident isolation, contamination/formulas,
global rollback/purity, broad replay/lockstep/fuzz/stress/allocation gates and full P1–P12 acceptance.

## 10. Execution / status

- [x] D4.1 contracts, native state, shared controls and condition-system integration
- [x] D4.2 linked recurring readiness/priority and authoring migration
- [x] D4.3 commands, immutable view and focused tests
- [x] D4.4 build, two exact suites and synchronized evidence/tracker/help

## 11. Implemented result

Policy settings have one JSON parser and one wire serializer reused by native codec, normal command and
public proof. The existing condition system owns wear and captured policy observation at370, no new loop.
Settings/latch commits verify bound controls/condition were not changed by the state owner; normal control
and policy actions share exact control admission/commit. Five public provider faults include a policy
that silently changes actual enabled state while configuring itself. Unit aliases, representational
priority bounds, manual ownership, exact hysteresis, staged malformed/future/contradictory checkpoints,
pending command saves and an unrelated operation/alternative condition unit are focused-proven.
Physical service consumes native spares, holds tools, respects ordinary pause and restarts only after
actual repair; wide saved worlds remain identical.36 preceding identity files compile only; no earlier
suite was rerun. This does not prove global provider purity, arbitrary operation execution behavior or
end-to-end AutomationSystem policies/replay/stress/full roadmap acceptance.
