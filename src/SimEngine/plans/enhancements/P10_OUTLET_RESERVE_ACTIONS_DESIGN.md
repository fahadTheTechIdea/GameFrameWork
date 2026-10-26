# P10-B7b — native outlet reserve actions

Current M147/native2.772: native/independent JSON reserve actions share
OutletReserveControlActionDeclaration and the common resource linker/admission, not a
required native Def base. Original outlet policy/current-field preservation and physical
receipts remain authoritative. [Coupled held/released supply, committed delivery, saves,
replays and actual scope](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m147-reusable-resource-controls-and-detached-quantities--2026-10-23).
Earlier single-action/family results below retain their historical image.

Current follow-on [B7c stock-target actions](P10_STOCK_TARGET_ACTIONS_DESIGN.md),2.137.0/save74,
consolidates native resource action linking into ResourceControlActions.cs; no reserve-only file path
survives. Same two suites now832 checks (493 contracts;339 execution), includes this preceding682,
not additive. Native reserve behavior retains the same owners. [Current evidence](../../docs/migration/P10_STOCK_TARGET_ACTIONS_VERIFICATION.md).

Status: native focused slice verified on2.136.0/save73, 2026-10-06. Full P1-P12 remains open.

## 0. Problem and specification

The generic action dispatcher exists, but no native rule can control outlet reserves. The normal
ConfigureOutletPolicyCommand replaces complete settings: constructing that payload from authored
defaults would erase later player edits. Automation must express intent, not own another policy store.

## 1. Goals

1. Explicit master-listed JSON selects an outlet-reserve action, actual resource, native unit and
   existing logistics command/owner. Missing content, wrong units or registrations fail composition.
2. The existing outlet command owns a detached immutable OutletPolicyChange: complete replacement
   or one-resource reserve change. Reserve changes preserve other reserves, priority and filters.
   Zero removes that resource's reserve; negative/fractional/incompatible/overcapacity changes refuse.
3. Shared normal admission resolves the change against current IOutletPolicy facts. Preview, command
   and deferred automation use the same validation and actual provider mutation, including mods.
4. Generic action-policy target discovery is engine-owned and returns detached stable binding IDs.
   The singular IOutletPolicy uses one public outlet-policy slot identity, not a concrete component key.
5. Existing rule decision/dwell/cooldown/cancellation/save/hash owners and one GameClock remain.
   Reserve edits do not rewrite or cancel physical reservations, stock, jobs or cargo.
6. Prove physical hauling, in-flight reservations, later unrelated player edits, pending saves,
   ownership/refusal parity and native/public outlet providers through the existing two suites.

## 2. Design and API

Standard/LogisticsPolicies.cs gains immutable OutletPolicyChange with explicit Replace and Reserve
factories and its embedded command-intent codec. LogisticsPolicyRules.CheckChange resolves current
settings and invokes the actual IOutletPolicy.CheckOutletPolicy. The ordinary handler remains the
only command owner and the existing provider remains the only persistent settings owner.
ConfigureOutletPolicyCommand replaces Settings with Change and implements IEntityControlCommand.
No legacy property, alias, secondary command, fallback settings or parallel queue is retained.

OutletReserveControlActionPolicyDef resolves required resource/unit/command/owner. It constructs
the normal change intent without World, and validates the singular public outlet-policy binding.
AutomationModule declares outlet_reserve_control_actions but does not require LogisticsModule unless
selected content actually needs its command. IControlActionPolicy.CopyTargetIds and the existing
OperationsView project actual compatible binding IDs with ownership, profile visibility and purity
guards. No host duplicates target/provider selection rules.

## 3. Files and risks

Existing logistics policy/command/view/module, automation action contract/module/view,
public proof policy, two-suite helpers, master-listed JSON proof, strict build identity and command
schema evidence. No provider persistent layout changes. Changed command payload breaks old builds;
no compatibility reader. Risk: stale settings at commit; resolve intent again through normal admission
against the actual provider, never capture a replacement policy at rule registration.

## 4. Verification

Clean build, then exactly these two complete directly affected suites (same names for repair reruns):

- Automation: threshold and recovery contracts, bindings and private controls
- Automation: threshold edits, recovery, deferred timing and saved processes

Other suites compile only. No full/family/third suite, Oil/Tycoon/host/demo/platform/Draw changes.

## 5. Execution

- [x] Revise the existing normal command intent and shared admission; native action and target discovery.
- [x] Explicit JSON, current call-site migration and physical native/public proof in the two suites.
- [x] Clean build, two exact suites, source review and current evidence/master/README/help updates.

## Recorded gate

Clean build, zero warnings/errors;682 checks/two exact complete suites (394 contracts;288 execution),
zero final failures/aborts. [Evidence](../../docs/migration/P10_OUTLET_RESERVE_ACTIONS_VERIFICATION.md).
Build2.136.0/save73/automation3/operations view7/logistics view14. Normal outlet command payload is
revised, not its persistent provider settings layout. ContentDependencies validates actual catalog
ownership and complete resource/unit visibility; CopyControlTargetIds performs guarded private target
discovery through the selected provider. Native reserve actions preserve current unrelated settings,
respect combined native capacity at actual commit and never rewrite acquired cargo or receiving claims.
Both native/public outlet owners continue timed physical travel/unload identically after saves.
Pending normal command save/load also retains detached intent through the existing inbox codec.
Includes previous B5/B6/B6b/B7a coverage, not additional545 current checks.62 build/59 save assertions
migrate; unrelated suites compile only. The clock source is unchanged; earlier85 clock checks not rerun.
The lifecycle skill kept the implementation, owner boundaries and focused evidence in one tracked stage.

Stock-band automation, runtime action editing, wider provider matrices, global rollback, replay,
soak/allocation gates and full framework acceptance remain open.
