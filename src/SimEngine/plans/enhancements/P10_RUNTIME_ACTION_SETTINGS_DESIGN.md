# P10-B7d — editable runtime action branches

Current follow-on [B7e complete stock-band controls](P10_STOCK_BAND_CONTROLS_DESIGN.md),2.139.0/save76,
replaces desired-only actions with explicit minimum/desired/maximum levels. Same two suites now1069
checks (622 contracts;447 execution), includes preceding909, not additive. Existing editable action
settings/history/timing owners are unchanged. [Current evidence](../../docs/migration/P10_STOCK_BAND_CONTROLS_VERIFICATION.md).

Status: native focused slice implemented and verified, 2026-10-06. Build2.138.0/save75,
automation4/operations view8/logistics view15. Continues P10/P1-P12, not completion.
[Evidence](../../docs/migration/P10_RUNTIME_ACTION_SETTINGS_VERIFICATION.md).

## 0. Problem

Before B7d, AutomationSystem read rule.WhenTrue/WhenFalse directly for admission and deferred commit.
AutomationSettings owned editable thresholds but no action values. Players could not change an operation
target, reserve or desired stock without changing catalog JSON. Merely adding settings values would
also miss reapplying an edited action when the boolean condition remains latched.

## 1. Goals

1. Both branches become explicit immutable ControlAction values in existing AutomationSettings.
   JSON rule declarations remain initial intent surfaced by existing profile views, never a fallback.
2. Existing configure command/preview/owner validates both actions through actual action contracts,
   native units and ordinary command handlers. No action editor subsystem or privileged mutation.
3. Same-binding edits clear pending/candidate intent, preserve actual last action/latch/cooldown and
   re-earn dwell for a changed selected action even if the condition remains unchanged. Editing the
   unselected branch must not cause redundant commits. No same-update application or new clock.
4. The existing record stores nullable last-applied ControlAction instead of a separate HasApplied
   boolean. HasApplied derives from that fact. Snapshot exposes detached last-action diagnostics.
5. Settings, normal inbox commands, candidate/pending/applied phases and saved twins hash/save/load
   through their existing owners. Strict breaking build/save/state/view identities; no old readers.
6. Native operations, reserves, stock and public-only actions support runtime edits without modifying
   their execution coordinators. Physical cargo/claims and normal current-capacity admission survive.

## 2. Design and API

AutomationSettings constructor requires existing profile/enabled/target/inputs/thresholds plus
whenTrue/whenFalse. Remove the old constructor outright and migrate every current caller using
explicit profile initial actions or preserved current actions, not invented defaults.
ControlAction owns one embedded Sync and value equality. AutomationSettings.Sync embeds both actions;
AutomationSystem.SyncRecord embeds nullable AppliedAction, replacing the old HasApplied field.
CheckShape enforces both configured units match the declared action unit; normal handlers own bounds.
CheckReferences/CheckConfigure/Update consume configured actions only. Evaluate skips a stable latch
only when its selected configured payload equals the actual applied payload. Configure preserves
history for unchanged bindings and clears stale intent; changed bindings reset their own record.

Files: Standard/AutomationContracts.cs, AutomationSystem.cs, OperationsModule.cs; Kernel/SaveGame.cs;
current tests and explicit JSON/public-only fixtures. No new runtime store/command/provider switch.
No compatibility: development-only strict schema transition. Existing profile declarations stay JSON.

## 3. Risks and refusals

Wrong branch units, invalid normal bounds, hidden definitions and foreign targets refuse without
partial rule changes. A valid edit can later become invalid before due commit; normal refusal suspends
while keeping player settings, last applied action and physical commitments. Last-action history may
be different from currently configured branches, so load validates identity/unit/phase, not current
target capacity. Cooldown remains a deadline on the one GameClock, not a new current-time owner.

## 4. Verification

Clean build followed by exactly the same two complete suites, for all repair reruns:

- Automation: threshold and recovery contracts, bindings and private controls
- Automation: threshold edits, recovery, deferred timing and saved processes

Extend these suites only. Prove both branches, selected/unselected edits, stale pending cancellation,
fresh dwell and preserved cooldown, pause, native/public values, current bounds refusal, submitted
command detachment, immutable views, corrupted action/unit state and saved/interleaved identity.
Other suites compile only; no third/family/full gate. Preserve the dirty worktree.

## 5. Execution

- [x] Revise explicit settings/action codec/history and existing admission/dispatch.
- [x] Migrate all current callers; native/public runtime edit and physical/saved proof.
- [x] Clean build, same two suites, source review, evidence/master/README/help updates.

Clean build, zero warnings/errors;909 checks/two exact complete suites (522 contracts;387 execution),
zero final failures/aborts. Includes B5/B6/B6b/B7a/B7b/B7c, not additive. Other suites compile only.
The initial gate exposed one stale schema3 assertion; it now checks the actual sole rule codec4.
No engine admission or timing rule was relaxed to obtain a green gate.

Arbitrary automated stock minimum/maximum editing, utilities/backpressure, wider provider/global
rollback/replay/stress/allocations and full framework acceptance remain open. No Oil/Tycoon/hosts/
demo/platform/Draw. The lifecycle skill keeps this a staged engine change with scoped evidence.
