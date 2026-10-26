# P10-B7c — native stock-target actions

Current M147/native2.772/save161/replay2: stock actions now delegate the sole reusable
StockControlActionDeclaration and common resource declaration through IControlActionPolicy;
native authoring inheritance is deleted, not retained as a shim. The coupled physical proof
executes minimum/maximum/desired with current player settings, reserve-held supply, cargo
continuation/cancellation and second refill. [Actual350/0 plus399/0 visibility, image
boundaries and limits](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m147-reusable-resource-controls-and-detached-quantities--2026-10-23).
Old stock suites below compile only, not a current broad automation pass.

Current replacement [B7e complete stock-band controls](P10_STOCK_BAND_CONTROLS_DESIGN.md),2.139.0/save76,
removes the desired-only class/API/JSON section. One explicitly selected minimum/desired/maximum
level uses the same demand owner, normal command and editable actions. Same two suites now1069
checks (622 contracts;447 execution), includes preceding832, not additive.
[Current evidence](../../docs/migration/P10_STOCK_BAND_CONTROLS_VERIFICATION.md).

Current follow-on [B7d editable runtime branches](P10_RUNTIME_ACTION_SETTINGS_DESIGN.md),2.138.0/save75,
uses explicit values in the existing AutomationSettings instead of content-action fallback. Same
two suites now909 checks (522 contracts;387 execution), includes this preceding832, not additive.
Native/public stock edits retain physical commitments and current bands through the same demand owner.
[Current evidence](../../docs/migration/P10_RUNTIME_ACTION_SETTINGS_VERIFICATION.md).

Status: native focused slice implemented and verified, 2026-10-06. Build2.137.0/save74,
automation3/operations view7/logistics view15. Continues full P10/P1-P12, not completion.
[Evidence](../../docs/migration/P10_STOCK_TARGET_ACTIONS_VERIFICATION.md).

## 0. Problem and specification

JSON actions can control operations and outlet reserves, but cannot change desired refill stock.
The existing stock command replaces complete bands. An authored action must not reset later player
minimum/maximum/priority edits or create a parallel demand table or instant supply path.

## 1. Goals

1. Explicit JSON stock_target_control_actions selects actual resource/native unit/normal stock command.
2. Revise the existing command/ILogisticsStockPolicies entry points to carry immutable LogisticsDemandChange
   replacement or desired-target intent. LogisticsService alone resolves current rows and validates
   ordered bands, native quantum, full receiving capacity and demand ownership.
3. Desired changes preserve current minimum/maximum/priority, never fabricate an absent stock row,
   and keep existing refill latch/age/claims semantics. Enabled is explicit action intent.
4. Bindings are stable per entity/resource, discovered through the existing action/view contracts.
   Different resources are distinct targets; competing profiles for the same resource conflict.
5. Share native resource-action content linking/unit/command validation between reserve and stock
   policies. Replace the reserve-only implementation file; no duplicate linker or legacy class path.
6. Prove normal preview/command parity, later band refusal, private views, physical timed refill,
   in-flight disabled/cancelled requests, pending commands/rules and saved twins on actual inventories.

## 2. Design and API

LogisticsDemandChange owns its embedded normal-command intent codec. ILogisticsStockPolicies and
ConfigureStockPolicyCommand replace Settings input with Change; no old overload/property survives.
The existing LogisticsService stock-policy owner resolves desired intent against its current row,
then uses its one canonical admission and refill mutation. The row/settings persistent codec is unchanged.
LogisticsView reads detached current stock settings and previews the same intent contract.
Generic SetRequest/ReplaceRequests/WithdrawRequests cannot mutate the native stock scope: they refuse
explicitly. Current native callers use stock intent; disabling keeps the authoritative policy row.
CheckRequests remains the shared read-only aggregate admission beneath the stock owner.

ResourceControlActions.cs replaces OutletReserveControlActions.cs: one ResourceControlActionPolicyDef
owns required JSON read/resolve/catalog dependencies/normal registration/native-unit validation.
OutletReserveControlActionPolicyDef and StockTargetControlActionPolicyDef own distinct contract target
selection and command construction. AutomationSystem and its timing/store/codec require no changes.
Stock target identity is the actual authored resource identity in a public stock-policy slot; no part
key, numeric catalog index or new registry is used. Only actual existing stock rows are targetable.

## 3. Files and risks

Existing logistics policies/service/commands/view, automation module/resource actions, current call sites,
master-listed JSON/public inventory proofs and strict build/save identity. Preserve current dirty work.
Main risk: later band edits invalidate a due desired target. Re-resolve/recheck at normal commit; typed
refusal suspends the rule without resetting those bands or rewriting physical receipts/cargo.

## 4. Verification

Clean build followed by exactly these two complete suites, same names for all repair reruns:

- Automation: threshold and recovery contracts, bindings and private controls
- Automation: threshold edits, recovery, deferred timing and saved processes

Extend those suites only. Other suites compile only; no third/family/full gate. All timing uses the
single GameClock. No Oil/Tycoon/host/demo/platform/Draw work, fallback or compatibility path.

## 5. Execution

- [x] Revise normal stock intent/admission/view and consolidate native resource action linking.
- [x] Migrate current callers; explicit JSON/private/physical native-public inventory proof.
- [x] Clean build, same two exact suites, source review and evidence/master/README/help updates.

Clean build, zero warnings/errors;832 checks/two exact suites (493 contracts;339 execution), zero
final failures/aborts. Includes B5/B6/B6b/B7a/B7b, not additive. Other suites compile only. Initial
physical assertions stopped before the next logistics phase settled the refill latch; the bounded
wait now requires actual delivery and settled demand, without changing engine phase ownership.

Arbitrary minimum/maximum action editing, runtime rule actions, utility consumer allocation/backpressure,
wider provider/global rollback/replay/stress/allocations and complete framework acceptance remain open.
