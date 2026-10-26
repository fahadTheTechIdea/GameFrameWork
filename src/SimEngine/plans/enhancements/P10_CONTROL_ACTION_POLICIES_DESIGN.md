# P10-B7a — contract-based normal-command control actions

Current M146/native2.771/save161/replay2: operation and company actions use shared reusable
declarations, native/independent definitions implement IControlActionPolicy directly and
the redundant pure abstract base is deleted. Resource action sharing remains substantive
inside its existing owner, now implementing the contract directly. No new command or queue.
[Focused368/0, original redirect refusal and remaining scope](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m146-shared-json-actions-and-input-recovery--2026-10-23).
Earlier results below remain historical, not new-runtime reruns or all-action qualification.

Status: native focused slice verified on2.135.0/save72, 2026-10-06. Continues P10;
full P1-P12 remains open.

Current follow-on [B7f typed company targets](P10_COMPANY_ALERT_AUTOMATION_DESIGN.md),2.227.0/save131,
revises SAME binding/action/dispatcher. Explicit TargetKind/typed entity or company normal command
checks replace single-entity restriction; no compatibility/default/company proxy. Automation5/
operations view10/alert view4, existing alert owner3/remaining payloads unchanged. Real incident/
private company alert/physical response/deferred restoration, pending/partial saves/public redirect
refusal pass61 checks/ONE exact NEW case. Older entity controls compile only, NOT rerun.
[Evidence](../../docs/migration/P10_COMPANY_ALERT_AUTOMATION_VERIFICATION.md). Earlier B7a–e below
are historical implementation/evidence, not current runtime coverage.

Current follow-on [B7e complete stock-band controls](P10_STOCK_BAND_CONTROLS_DESIGN.md),2.139.0/save76,
replaces desired-only actions with one typed minimum/desired/maximum contract and explicit JSON
selector. Same two suites now1069 checks (622 contracts;447 execution), clean build, includes
B5/B6/B6b/B7a/B7b/B7c/B7d, not additive. Existing normal demand/action/rule owners preserve bands,
physical claims and sole GameClock. Utilities/wider providers/full P10 remain open.
[Evidence](../../docs/migration/P10_STOCK_BAND_CONTROLS_VERIFICATION.md).

Preceding follow-on [B7d editable runtime branches](P10_RUNTIME_ACTION_SETTINGS_DESIGN.md),2.138.0/save75,
extends these two suites to909 checks (522 contracts;387 execution), clean build. Existing action/
normal command contracts handle explicit player branches; same-latch edits re-earn dwell with actual
cooldown/history and unselected edits avoid redundant commits. Physical native/public stock/reserve
commitments and saved twins survive edits. Includes B5/B6/B6b/B7a/B7b/B7c, not additive. One GameClock
unchanged; arbitrary stock-limit actions/utilities/full P10 remain open.
[Evidence](../../docs/migration/P10_RUNTIME_ACTION_SETTINGS_VERIFICATION.md).

Preceding follow-on [B7c native stock-target actions](P10_STOCK_TARGET_ACTIONS_DESIGN.md),2.137.0/save74,
extends these same two suites to832 checks (493 contracts;339 execution), clean build. Immutable
normal stock intent preserves current bands/priority; shared native action linker and resource-specific
private bindings use existing contracts. Native/public physical refill and saved twins keep acquired
cargo/claims; no new controller/clock. Includes B5/B6/B6b/B7a/B7b, not additive. Arbitrary stock-limit
automation/runtime edits/utilities/full P10 remain open.
[Evidence](../../docs/migration/P10_STOCK_TARGET_ACTIONS_VERIFICATION.md).

Preceding follow-on [B7b native outlet reserve actions](P10_OUTLET_RESERVE_ACTIONS_DESIGN.md),2.136.0/save73,
extends these contracts with actual content dependencies and guarded private target discovery. Normal
outlet changes preserve current unrelated settings and physical cargo/claims.682 checks/two exact
suites, clean build, includes this preceding545 coverage, not additive. Stock-band/runtime actions,
utilities and full P10 remain open. The implementation and gate below are historical B7a evidence.

## 0. Problem

AutomationSystem hardcodes ConfigureOperationCommand, IControllableOperation and its command owner.
JSON/mod rules can change evaluation but cannot select another ordinary engine control command.
Reserve-policy and other domain actions therefore require scheduler changes or parallel controllers.

## 1. Goals

1. Every rule requires a catalog-owned IControlActionPolicy selected explicitly by JSON action_policy.
2. An open ControlActionPolicyDef declares a normal command ID/owner, validates target/action semantics,
   and constructs an unstamped command from detached request values. No mutable World in construction.
3. One shared dispatcher resolves the actual registry entry, checks ownership, guards pure callbacks,
   snapshots command payloads with the existing codec and uses the ordinary handler Check/Apply.
4. Native operation control preserves all existing refusals, units, safety, timing and physical effects.
   An independent public-only policy uses a different registered command without coordinator edits.
5. Existing phase290/pending decisions/state/Sync and the sole GameClock own timing. No new scheduler,
   command queue, policy state, codec or current-time owner. Actions still defer to the next update.
6. Complete profile views include detached action-policy/command identities and withhold hidden policy
   dependencies. Preview/command/refusal, pending saves, cancellation, pause and saved twins agree.

## 2. Design and API

Add Standard/ControlActionPolicies.cs: IControlActionPolicy, ControlActionPolicyDef,
ControlActionRequest, native OperationControlActionPolicyDef and stateless internal dispatch.
Required IControlRule.ActionPolicy plus native/public rule JSON linking. Existing ControlAction remains
the immutable enabled/target/unit payload; provider contracts interpret it without new coordinator cases.
Rename OperationBinding to ControlBinding everywhere: target binding is not restricted to an operation.
No alias/overload/legacy path. Physical target must still exist and belong to the acting company.

Native JSON operation_control_actions records require id/command/owner; native provider verifies the
actual operation-control registration. Other action types register their own content contract. Command
construction is guarded, must return the declared actual command type without sender/timing stamps,
and is detached with CommandRegistry.Copy before handler use. IEntityControlCommand exposes the actual
target entity; redirected targets refuse. Header/target/payload fingerprint checks prevent a normal
handler's Check from silently changing intent. Existing handlers own actual mutation.

Composition validates actual policy/content/command ownership before Update. CheckReferences retains
actual signal binding admission but delegates action-target semantics, not IControllableOperation.
Configure preflights both actions through normal handlers. Commit rechecks current binding/action
and applies only the existing pending decision. No fabricated successful action or alternate target.

## 3. Files

Standard/AutomationContracts.cs, ControlRules.cs, AutomationSystem.cs, AutomationModule.cs,
AutomationProfileSnapshot.cs, OperationsModule.cs, Kernel/SaveGame.cs; public extension policy, four
existing master-listed rule JSON files, affected tests/helpers/build assertions; this stage/master/
evidence/README/help. Build2.135.0/save72/automation3/operations view6; persistent layouts unchanged.

## 4. Refusals and verification

Missing/wrong/foreign policy or command ownership fails composition. Unknown/hidden policy produces
no profile and UnknownDef on configure. Target/owner/unit/bounds refusals come from the provider and
ordinary handler. Mutation, null/wrong command or forged stamps fault, never fall back to operation.

Clean build before exactly two complete suites:

- Automation: threshold and recovery contracts, bindings and private controls
- Automation: threshold edits, recovery, deferred timing and saved processes

Extend these two existing suites only. Previous recovery/threshold/native physical proof remains;
add policy content/metadata/normal-handler parity/provider-fault and independent deferred/saved proof.
Other suites compile only; no third/family/full run.

## 5. Scope and risks

This establishes the generic action boundary, not native reserve-policy actions or runtime action editing.
Those remain next P10 work. The scalar enabled/target/unit action does not claim arbitrary multi-command
transactions. Participating target and clock/identity guards are scoped purity proof, not global rollback.
Native operation safety stays in the normal owner. No Oil/Tycoon/host/demo/platform/Draw work.

## 6. Execution

- [x] Required contracts, native action policy, shared dispatch and existing owner migration.
- [x] Explicit JSON/public proof and neutral binding migration; focused tests/build assertions.
- [x] Clean build and two exact suites; scoped evidence and staged documentation.

## Recorded gate

Clean build, zero warnings/errors;545 checks/two exact complete suites (312 contracts;233 execution),
zero final failures/aborts. [Evidence](../../docs/migration/P10_CONTROL_ACTION_POLICIES_VERIFICATION.md).
Includes previous B5/B6/B6b coverage, not additional479 current checks. Independent action interpretation
selects a different actual command/unit, preserves ordinary ownership/refusals and uses the same
deferred dwell/cooldown/clock. Eight targeted provider/command faults reject. Hidden action-policy
metadata is scoped by existing company knowledge. Four existing master-listed JSON files explicitly
declare native policies and required references; no fallback or old binding alias.62 build assertions
migrate; other suites compile only. Persistent owner layouts and clock source are unchanged.
The lifecycle skill kept the stage, sole-owner implementation and scoped gate aligned.
