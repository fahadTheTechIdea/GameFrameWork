# P11-C2 — Core controller commands and stable playbook execution

Status: scoped implementation verified,2026-10-05.142 checks/two exact suites, clean build;
[evidence](../../docs/migration/P11_CONTROLLER_EXECUTION_VERIFICATION.md). Continues C1;
full P11/P1–P12 remains partial.

## 0. Problem statement

Before C2, WorkModule.Compose registered SetControllerCommand although PlayerTable is core session state.
A game without work could not hand control over through ordinary commands. PlanPlaybook observed a
mutable group label rather than the stable plan identities returned by its real accepted command;
regrouping can lose its actual execution or substitute unrelated completed plans.

## 1. Goals

1. Register the sole controller-selection command in the core world, independent of optional modules.
2. Keep normal preview/inbox/refusal/save/replay serialization and the actual PlayerTable unchanged.
3. Bind playbook progress to real accepted stable plan identities, not copied progress or a group index.
4. Prove no-work/no-space controllers, saved handover, sequential physical expansion, player pause,
   regroup/cancel and absence of duplicate/fabricated completion using only the existing two suites.

## 2. Design

Move existing SetControllerCommand and its handler out of mixed StandardCommands into
Commands/PlayerCommands.cs. World registers core.set-controller once under engine ownership before
module composition. Remove WorkModule registration and old handler; no additional module, alias or
fallback. Existing CheckController/SetController remain single admission/commit owners.

PlanPlaybook provider saves its current stable first-plan identity instead of a submitted boolean.
The actual PlacePlanTemplate answer already returns the contiguous first identity and entry count;
validate that real result against the authored template before retaining this execution link. The
template command handler validates the whole returned contiguous identity contract before publishing success.
This is the native board's existing allocation contract, now explicit on IPlanBoard, not inferred from
the group name or an unvalidated third-party response. Each
decision reads current private native plan views by those identities. Group/priority/pause remain the
actual plan settings and can change without losing the link. Missing, failed or cancelled linked plans
block; another group cannot stand in for them. Sequential next steps wait for actual preceding Done
and a later decision. No plan state/resources/jobs/group membership are duplicated.

## 3. API surface

SetControllerCommand public type remains in Commands, sole ID becomes core.set-controller/owner engine.
IPlayerController/decision/state contracts are unchanged. Native playbook embedded state becomes2;
controller scheduling/view1 remains unchanged. Build2.91.0/save60; other native layouts unchanged.

## 4. Files

World.cs; Commands/PlayerCommands.cs; remove old command/handler/WorkModule registration;
Standard/PlanPlaybooks.cs; existing PlayerControllerTests/public JSON proof; stage/master/evidence/
parents/README/help/AGENTS. Other current build/save assertions migrate and compile only.

## 5. Errors

Unknown/missing selected profile and invalid controller enum keep current ordinary refusal semantics.
Duplicate core command registration refuses normal composition. Malformed accepted template results
fault Update; invalid playbook cursor/range refuses checkpoint. Lost/cancelled/failed execution remains
a typed blocker without automatic restart, stock grants or silently borrowing another plan.

## 6. Compatibility

Breaking core command identity/order and native provider codec revision. No sim.set-controller alias,
legacy submitted-boolean reader, compatibility module or duplicate registration.

## 7. Verification

Clean standalone build; exactly the existing two names:

- Player controllers: authored bindings, scoped decisions and command parity
- Player controllers: physical playbooks and saved cadence

Add explicit no-work/no-space fixture and saved sequential/regroup/pause/cancel physical cases. Other
suites compile only; no third/family/full gate. Bounded physical waits include current native diagnostics.

## 8. Risks

Execution-link validation does not make arbitrary provider callbacks pure or add transactional rollback.
Returned contiguous identity contract is the existing native board allocation; do not infer identities
from group names. Core selection without a controller module changes selection only, never invents AI.

## 9. Out of scope

General knowledge/pressure/recovery strategy, automatic repeats, arbitrary CPU isolation, Oil/Tycoon,
platforms/Draw, broad replay/stress/full acceptance and old-save support.

## 10. Execution

1. [x] Move sole selection registration to core, prove optional-module independence.
2. [x] Revise native playbook execution link and saved schema in place.
3. [x] Prove physical sequential/player-control continuity through the same private views.
4. [x] Run two exact suites and update staged evidence/master/parents/help/instructions.
