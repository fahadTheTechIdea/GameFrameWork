# P9-C14: Named fleet batching controls — Design Document

Status: scoped implementation verified,2026-10-08. Full goal and P1–P12 remain open.

## 0. Problem statement

HaulLoadPolicyContracts already owns player-editable per-carrier minimum load, maximum wait, enabled
and emergency bypass settings. MachineFleetService exposes navigation/work/home but not batching.
A host loop could commit a valid prefix before another member refuses. Whole admission/current
membership belong in engine, never another fleet settings store or dispatcher.

## 1. Goals

1. Original HaulLoadPolicyContracts owns canonical whole admission and exact idempotent provider commit.
2. Named normal command resolves current members; private immutable facts read actual policy owners.
3. Whole ordinary refusal changes no prefix; provider violation fail-stops rather than fake rollback.
4. Actual minimum/wait rules use existing request age/last dispatch; edits do not reset either.
5. Prove physical native/public hauling and saved continuation without cancelling accepted cargo.

## 2. Design

HaulLoadPolicyContracts becomes partial with Standard/HaulLoadSelection.cs. PreviewGroup copies,
sorts and validates explicit unique actual IDs/seat/ownership, then original CheckConfigure and Read.
Entries contain detached immutable HaulLoadSettings only for owned providers. Read and CheckConfigure
observe the actual existing provider codec/scoped entity facts; entity alias must match. Configure
freshly checks, skips identical settings, verifies exact settings/version/live owner/clock/allocator
and existing worker job/home/held state. ConfigureGroup preflights all before stable original commits;
a violated commit faults the original session, with no healthy partial checkpoint.

SAME MachineFleetService uses ControlledFleet and delegates current IDs. Fleet records still contain
only company/name/profile/members. ConfigureFleetLoadPolicyCommand uses normal sender/inbox and
existing HaulLoadSettings.Sync. FleetView4 exposes private per-entry settings/blockers; service1,
save140/LogisticsView29/current load provider2 and physical payloads remain unchanged.

Original LogisticsService.CheckLoad is still the single dispatcher, with age/last dispatch on
existing route/demand rows. New settings change future intake only; acquired workers/receipts/cargo
remain on WorkBoard and real handling/motion. No override loop, minimum/wait copy, age reset or timer.
Native HaulLoadPolicy projects disabled state only when idle and not moving. A future-intake
restriction must not override the actual moving/loading/working state of retained work or home return.
Existing strict haul_load_profiles/master authors rules and initial settings. Independent declaration
reuses ContractHaulLoad and OddHaulLoadProfile, not new provider logic. New native/public machine
prototypes/startups are actual JSON and reuse original worker/mover/inventory/transporter capabilities.

## 3. API surface

HaulLoadSelectionEntry/Preview; original HaulLoadPolicyContracts.PreviewGroup/ConfigureGroup;
IMachineFleets.PreviewLoadPolicy/ConfigureLoadPolicy; ConfigureFleetLoadPolicyCommand;
FleetView.PreviewLoadPolicy. Existing individual settings/control remain the sole protocol.

## 4. Files to change

Modify HaulLoadPolicies.cs, MachineFleets.cs/module, FleetCommands.cs, FleetView.cs, current build ID.
Add partial selection file, public declaration/three JSON docs, one new bounded test/registration.
Shared native factory adds optional module/master-document assembly only. Prior build/view identities
compile-only. Stage/evidence/source manifest/master/P9/README/AGENTS updated after verified freeze.

## 5. Error handling matrix

Malformed/null/empty/duplicate target set → InvalidArgument; invalid seat → NotSeated;
missing member/fleet → NotFound; foreign command member → NotOwned, private view → NotFound;
absent actual load provider → no-load-policy; null/unknown rule/non-native or over-capacity minima/
unrepresentable wait → original invalid-load-profile; empty fleet → fleet.empty.
Provider read/check mutation or ignored/inexact commit faults original session; no default policy.

## 6. Backward compatibility

Development only: revise original owners; no shim, profile-only fallback, duplicate state or old reader.

## 7. Verification

Build completed canonical slice, run ONE distinct exact NEW bounded runtime case. Repair/rerun only
observed failures in that same case, record EVERY attempt. Native/public whole admission/private pure
frozen facts/all fields/idempotence/current membership/pending saves/short-load wait and real expiry/
original request history/partial loading/retained accepted cargo/metadata removal/saved twins/pause/
alternate mod rule/conservation/isolation/strict JSON and one public fault.1500 Updates per physical
stage with engine diagnostic snapshot. Prior cases compile-only; no old/family/full/platform/stale run.
After final green freeze gameplay/public proof/tests/JSON; docs only. Full acceptance remains open.

## 8. Risks

Whole admission is not malicious-provider rollback/global sandbox. Minima are per-resource alternatives,
not simultaneous cargo reservations. Maximum wait derives from original request/last dispatch, not
edit time. Disabled batching prevents new intake, not motion of accepted cargo or source refund.

## 9. Out of scope

Fleet fuel/service controls/docked homes/formation/corridors, every provider/fault/corrupt/global purity,
all emergency/preemption/policy combinations/non-tile/no-work/replay/lockstep/CPU/stress/full P9/P1–P12.
No Oil/Tycoon business source/host/demo/Draw/platform edits. Commit/push after FULL verified finish only.

## 10. Sub-task execution order

1. Extend sole load policy protocol with pure whole selection/exact idempotent original commit.
2. Delegate current named membership through command/private view, no settings overlay.
3. Assemble strict JSON native/public physical proof and ONE new bounded case.
4. Build/exact gate, freeze, record every attempt and update authoritative tracker.

## Scoped verified outcome

All four slice stages complete. Build2.269.0/save140/FleetView4/membership1/load provider2/
LogisticsView29/WorkView80/physical payloads unchanged. THREE builds: first failed10.06s0 warnings/
2 errors in NEW ReservationToken equality assertions; original Equals used, clean4.95s and5.77s0/0.
TWO runs SAME ONE exact NEW bounded case: first94 passed2 failed2022ms, including one stage timeout/
suite abort; NEW test counted retired finished jobs as live board history. Test-only live-job repair;
final122 checks0 failures/aborts763ms. Engine/public proof/JSON unchanged after first runtime.
133 older build identities/two prior view identities/two old disabled-state assertions/shared factory
assembly compile-only; no old/family/full/platform/stale runtime. Source/public proof/tests/JSON
frozen after final green. Actual native/public whole no-prefix/private pure frozen facts/all settings/
current membership/pending saves/idempotence/original age/real short-load expiry/retained cargo/
metadata removal/saved twins/sole pause/alternate mod rule/truthful moving state/stock20=6 delivered+
14 source/isolation/omission/five invalid JSON/one scoped public pure-check fail-stop proved.
[Evidence](../../docs/migration/P9_FLEET_LOAD_CONTROLS_VERIFICATION.md).
Each physical stage1500 Update bound;763ms not CPU/allocation budget. Full acceptance remains open.
