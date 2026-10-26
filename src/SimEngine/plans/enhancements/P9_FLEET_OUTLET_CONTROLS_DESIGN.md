# P9-C17: Named fleet outlet-policy controls — Design Document

Status: implemented/focused-verified,2026-10-08; not full-engine acceptance.
Full P1-P12 remains active. Final147 checks0 failures/aborts1935ms, TWO clean builds/TWO same-case
runs including one bounded-stage abort and test-only physical exit-clearance repair.
Build2.272.0/save140/FleetView7/fleet1/outlet2. Source/public proof/tests/JSON frozen after green.
[Evidence](../../docs/migration/P9_FLEET_OUTLET_CONTROLS_VERIFICATION.md) records every attempt and
remaining gates. Existing independent public fault selector optional0 is test-only, not game policy.
Scoped748 canonical selected paths/154 Markdown/2207 local links/0 broken/0 duplicate
selected paths. Actual `git diff --check` exit0,31 existing LF/CRLF warnings. Final readonly SHA
and18 phase-row/five-column checks recorded in source manifest.

## 0. Problem statement

Original LogisticsPolicyRules.CheckChange and ConfigureOutletPolicyCommand own full replacement/
single-resource reserve intent. Named fleets cannot apply this intent to current members, and the
single handler commits directly without exact/idempotent provider verification.

## 1. Goals

1. One original outlet-policy owner validates and commits both single and whole current selections.
2. Native/public actual providers remain settings owners, no fleet fuel/inventory/reserve overlay.
3. Preserve held/job/home and acquired stock/receipts during policy edits; real trip budget observes them.
4. Prove physical loaded reserve hold/resumption/save continuation without instant fuel grants.
5. JSON remains authoritative for policies/prototypes; original command-change codec remains sole intent.

## 2. Design

LogisticsPolicies.cs: OutletPolicySettings.Matches; partial LogisticsPolicyRules extends CheckChange/
Read with actual existing provider-codec observation in finally. Common native capacity/quantum/
content validation precedes provider admission. Configure freshly checks, exact same settings no-op,
actual provider exact settings/version/live identity/owner/clock/allocator/worker facts verified.
No dispatch/exposure/inventory transfer implementation changes.

OutletPolicySelection.cs: immutable entries carry actual Current and proposed Next (null for foreign/
missing), canonical whole selection admission before any original Configure. Reserve changes preserve
each member's other resource lines, priority, filter, access. Refused ordinary selections no prefix;
violated commits fault session, not rollback.

MachineFleets/FleetCommands/FleetView: current original gate delegates to that owner; one normal
sender command uses OutletPolicyChange.Sync, private view7. Fleet codec1 and native/public outlet
payload unchanged. Single normal command also calls original Configure; no alternate purchase path.

## 3. API surface

LogisticsPolicyRules.PreviewGroup/ConfigureGroup/Configure.
IMachineFleets.PreviewOutletPolicy/ConfigureOutletPolicy.
ConfigureFleetOutletPolicyCommand(Fleet,Change); FleetView.PreviewOutletPolicy.

## 4. Files to change

Original policy owner, single handler, partial selection, fleet/service/module/command/view,
existing public ContractOutletPolicy fault injection (settings codec only), declaration-only module,
new strict native/public JSON and factory assembly, new bounded test/registration/build identity.
Older identity/view assertions compile-only; stage/evidence/master/docs finalized after green.

## 5. Error handling matrix

Null/empty/duplicate/invalid targets/change → InvalidArgument; no seat → NotSeated;
missing → NotFound; foreign command → NotOwned/private → NotFound without settings read.
No actual policy → original NoPolicy; missing resource/quantum/capacity/destination → InvalidArgument.
Pure mutation/inexact commit faults; no healthy checkpoint.

## 6. Backward compatibility

Development only, direct sole-owner revision. No old reader, shim or duplicate store.

## 7. Verification

ONE distinct exact NEW bounded case, completed-slice build. Record every attempt; repair/rerun only
observed same-case failure. Whole native/public/private/pure/current membership/pending/idempotence/
full settings/reserve lines/physical loaded cargo/original trip hold/resume/saved twins/pause/
metadata removal/stock conservation/isolated sessions/strict JSON/provider faults.
1500 Update maximum per physical stage with clock/jobs/motion/stock/blocker diagnostics.
No old/family/full/platform/stale runtime. Freeze code/proof/tests/JSON immediately after green.

## 8. Risks

Reserve amount is a stock floor, not fuel capacity, reservation or a second balance. Existing acquired
receipts survive policy edits; emergency access is original declared semantics. Whole preflight is
not global rollback of malicious modules. Existing generic scalar/purity breadth remains separate.

## 9. Out of scope

Docked homes/formations/corridors/service spares/condition effects/all providers/fault/corruption/
global purity/emergency/non-tile/no-work/incident/replay/lockstep/performance/stress/full acceptance.
No Oil/Tycoon business source/host/demo/Draw/platform edits. Publish only after FULL goal/plan verified.

## 10. Sub-task execution order

1. Original policy exact guarded single commit and whole canonical selection.
2. Current fleet delegation/normal change command/private view, no overlay.
3. Authored native/public physical proof and one new bounded case.
4. Build/exact gate/freeze, record every attempt and remaining scope in original tracker.
