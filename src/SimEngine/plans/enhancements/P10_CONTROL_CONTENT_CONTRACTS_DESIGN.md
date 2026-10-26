# P10-C7 — Open automation control content

Current M148/unchanged native2.772: selected quantity-provider/original binding and first-
fault semantics are qualified491/0 with existing AtomicInventory instrumentation, no new
buffer/guard or engine change. [Actual selected matrix and limits](../../docs/migration/P11_INVENTORY_VISIBILITY_VERIFICATION.md#current-m148-selected-scalar-provider-fault-and-original-binding--2026-10-23).
M147's physical controls remain at their actual proof image; broader gates stay open.

## Current M147 — resource controls and detached stock quantities, 2026-10-23

Scoped verified350/0 coupled controls plus399/0 existing visibility:749/0, with native
code identical across the two actual tested proof images. [All attempts, complete coupled
replays and remaining limits](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m147-reusable-resource-controls-and-detached-quantities--2026-10-23).
Remove the native ResourceControlActionPolicyDef authoring inheritance;
one non-content ResourceControlActionDeclaration retains its substantive common resource/
unit/command admission, with stock/outlet declarations retaining their distinct target and
normal command semantics. Native and independent Def/IControlActionPolicy implementations
delegate those same owners. No new stock/demand/reserve state, handler, scheduler or clock.

The existing private InventoryView has guarded local/named/original endpoint resolution but
only exposes named stock, capacity and reservations. Add a detached TryGetAmount query
through that SAME resolver/whole-provider observation, explicit unavailable/private refusal
and actual unit validation; inventory view10→11, build2.772/save161/replay2 unchanged.
This lets the new driver observe native and independent inventories without live World access.

One explicitly master-listed add-on composes source reserves with existing minimum/desired/
maximum controls and actual hauling. One new exact coupled physical case, not the broad
historical automation family: native/direct policy definitions and native/independent
inventory contexts, all three stock levels, held supply, pending saved actions, normal
player edits, acquired cargo, cancellation, second physical refill, privacy/conservation/
whole replays and malformed declarations. Keep original physical rates and220-step stage
bounds;60000ms child timeout. No business/Oil/platform/Draw edits or full/profile/family run.

## Current M146 — shared action and recovery declarations, 2026-10-23

Scoped verified368/0 across three focused exact scopes; three complete physical replays.
[Actual attempts, ownership, refusal coverage and limits](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m146-shared-json-actions-and-input-recovery--2026-10-23).
Runtime already selects IControlActionPolicy/IControlInputRecoveryPolicy;
reusable operation/company-alert action and retry-classification JSON remains private to
native definitions. Extract each original algorithm into one shared declaration, make
native/independent definitions delegate it, delete the two redundant pure abstract bases
without aliases and migrate their current implementers/callers. Keep substantive resource
action sharing inside its original owner, now implementing the action contract directly.
Move the company control capability to the shared declaration, not a native metadata type.

Existing master/factory/physical-response proof owner selects the three independent JSON
policy kinds, no hidden native operation/company/recovery definitions. Same direct condition
DAG, deferred alert, real supplied response, saved pending/paused state, hysteresis, sensor
failure recovery and complete replay; add malformed command/owner/profile/role/retry checks.
Reuse preserved M145 baseline90/0 rather than rerun it before edits. One new literal and
the affected original native literal, each60000ms. The existing exact typed-company-alert
case also checks the migrated negative provider still cannot redirect another company's
command; do not run its separate repeated-emergency case or a family. Native2.771/save161/replay2; no
scheduler/state codec/command/stock/business/Oil/clock timing changes. Broader gates remain.

## Current M145 — shared direct JSON condition declarations, 2026-10-23

Scoped verified199/0 across the new direct declaration and affected original exact cases.
[Actual attempts, complete replays, ownership and limits](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m145-shared-direct-json-condition-declarations--2026-10-23).
Runtime selection already uses IControlRule; the remaining authoring gap
is the parser/evaluator being private to AuthoredControlRuleDef. Extract its existing
logic into ONE public ControlRuleDeclaration, make native and independent definitions
delegate it, and delete the redundant abstract ControlRuleDef without a shim. Migrate
the two custom rule implementations and historical generic callers directly to IControlRule.
No scheduler, threshold comparison, dependency compiler, runtime record or clock is copied.

Use the existing master-listed control files and existing physical-response proof owner.
One new exact case selects two direct definitions (no hidden native rule definitions),
authors a shared-leaf all/any condition DAG and retains the original deferred alert,
physical response, material consumption, saved pending/paused state and numeric hysteresis.
Strengthen whole recording/live-final identity. Negative declarations must refuse before
Initialize. Run only this new literal and the affected original native literal, each
60000ms; baseline original87/0 on2.769 recorded separately. Native2.770/save161/replay2;
no state schema, business, Oil, platform, Draw or physical timing changes.

## Historical C7 contract-selection evidence

Status: scoped verified, ONE NEW exact bounded case87/0/0; full P1–P12 remains open. [Verification](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md).

## Problem and goals

AutomationSystem, ControlActionDispatch, AutomationProfileSnapshot and OperationsView still require ControlRuleDef/ControlActionPolicyDef/ControlInputRecoveryPolicyDef despite their existing contracts. Native AuthoredControlRuleDef resolves those concrete policies. Independent JSON declarations cannot consistently drive the same automation owner.

Extend IControlRule, IControlActionPolicy and IControlInputRecoveryPolicy with IContentDefinition. ContentDependencies becomes IReadOnlyList<IContentDefinition>, preserving the existing disclosure/admission list. Resolve actual owned contract aliases everywhere; no duplicate registry/state or native-inheritance fallback. Native definitions remain ordinary implementations.

## Stages and ownership

1. Migrate rule/policy resolution and pure private profile views, ordinary command dispatch and saved record validation in existing files. Shared declaration validation checks actual aliases, typed owned dependencies and original state/threshold/timing contracts before Initialize. Optional IDependencyNode references use existing DependencyGraph across these control declarations; no second topology compiler.
2. Preserve AutomationSystem order290/state5, next-update intent, original hysteresis/dwell/cooldown, binding/command guards and normal mutation owners. Wrap content composition/evaluation/recovery callbacks in existing observation guards. No instant response, stock writes, new rule state or second clock.
3. Master-listed independent rule/action/recovery JSON delegates existing native rule evaluation/policy behavior, not new game rules. Direct providers implement contracts without required native definition bases. Forward references declare optional dependencies and missing/wrong/cyclic references refuse before Initialize.
4. ONE NEW exact bounded player-only proof: immutable/private profiles, preview/command parity, deferred alerts, real incident dispatch/travel/material consumption, pause/saved pending and physical twins/replay, cancellation, session isolation and early invalid declarations. Build and run only that case; repairs rerun only it.
5. Freeze executable inputs and reconcile tracker/audit with explicit source/compile-only historical scope. Keep original broad acceptance checkbox text and status unchanged.

## Protocol and limits

Core2.336.0/save154/replay1; existing automation5/operations-view11 and physical owner schemas unchanged. At this historical checkpoint signal descriptors still used AutomationSignalDef. [P10-C8](P10_SIGNAL_CONTENT_CONTRACTS_DESIGN.md) now migrates their consumers to IAutomationSignalContent with one shared parser/guarded reader; this C7 runtime was not rerun on2.337.0. Resource/alert profile definition internals, broad provider/fault/purity/stress/soak/full P1–P12 remain open.

No full/family/historical/Oil/Tycoon/platform runtime, mandatory budget, business expansion, Draw/adapter/host gameplay, compatibility subsystem or second clock. Full goal AND plan must actually finish and be verified before staging/commit/push.
