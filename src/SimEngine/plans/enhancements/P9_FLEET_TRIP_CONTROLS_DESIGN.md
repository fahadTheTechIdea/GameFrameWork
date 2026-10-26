# P9-C15: Named fleet trip controls — Design Document

Status: scoped implementation verified,2026-10-08. Full P1–P12 goal remains active.

## 0. Problem statement

Standard/HaulTripPolicies.cs owns per-carrier enabled/reserve-protection/profile selection and
reads service windows from original recurring sources. MachineFleetService cannot apply those
settings to a whole current fleet. A host must not reproduce admission or commit a valid prefix.

## 1. Goals

1. Extend original HaulTripPolicyContracts with pure canonical whole-selection admission.
2. Commit exact idempotent settings through actual native/public policy owners.
3. Named command/private immutable FleetView resolve CURRENT membership, never a policy overlay.
4. Keep original fuel stock, outlet reserve, service cadence, physical cargo/jobs/claims and clock.
5. Prove reserve-blocked dispatch and physically retained loaded-trip holds/resumption/save twins.

## 2. Design

Same HaulTripPolicyContracts becomes partial. Read/CheckConfigure guard actual provider codecs with
finally verification and actual entity alias. Configure skips identical settings and verifies exact
settings, increasing version, live identity/owner/clock/allocator and unchanged worker job/home/held
before existing IWorkerDispatch.RefreshPolicy. RefreshPolicy remains original physical hold/resume
owner; no changes to its trip budget/receipts/motion algorithm.

HaulTripSelection.cs contains detached immutable entry settings and canonical whole preview/commit.
Whole ordinary refusal changes no prefix; commit violation fail-stops rather than rollback. Same
MachineFleetService control gate delegates current member IDs; fleet codec1 stays membership only.
ConfigureFleetTripPolicyCommand uses existing HaulTripSettings.Sync in normal sender/inbox.
FleetView5 masks foreign member refusal and never reads foreign settings. Save140 unchanged;
build identity advances because command/view contract changes. No legacy reader/shim.

Strict original haul_trip_profiles JSON authors every field. Native and independent public trip
providers operate on actual native worker/mover/storage/transporter/outlet/operating-input contracts.
Public proof implements only trip settings, not another dispatcher/stock/reserve/service owner.
Selected profiles can reference original service sources; named cadence edits are a separate open
requirement. This slice does not claim physical service-window completion from empty service lists.

## 3. API surface

HaulTripSelectionEntry/Preview; HaulTripPolicyContracts.PreviewGroup/ConfigureGroup;
IMachineFleets.PreviewTripPolicy/ConfigureTripPolicy; ConfigureFleetTripPolicyCommand;
FleetView.PreviewTripPolicy. Existing individual trip protocol stays authoritative.

## 4. Files to change

Original HaulTripPolicies/MachineFleets/module/FleetCommands/FleetView/build identity; new partial
selection; independent public proof, strict JSON/startups, optional factory assembly, one exact new
bounded case/registration. Old identity/view fixtures compile-only. Stage/evidence/manifest,
AGENTS/README/master/P9 updated after final green source freeze.

## 5. Error handling matrix

Null/empty/duplicate/invalid IDs → InvalidArgument; invalid seat → NotSeated; missing → NotFound;
foreign command → NotOwned, private → NotFound; absent provider → no-trip-policy;
unknown/null profile, incompatible reserve/service capabilities → invalid-trip-policy.
Actual read/check mutation or inexact/lying commit faults session; no healthy partial save.

## 6. Backward compatibility

Development only. Revise sole owners directly; no compatibility or duplicate authoritative state.

## 7. Verification

Build completed slice, ONE distinct exact NEW bounded case. Repair/rerun only observed failures in
that case, record EVERY attempt. Prove native/public whole/private/pure/frozen/settings/idempotence/
current-membership/pending saves/reserve-funded admission/real loaded hold/resume/physical delivery/
saved twins/sole pause/material conservation/fuel consumption/isolation/strict JSON/scoped fault.
1500 Updates per physical stage with actual clock/positions/jobs/loaded/blockers diagnostics.
Old/family/full/platform/stale runtime not authorized. Freeze source/proof/tests/JSON after green.

## 8. Risks

Whole preflight is not malicious-provider rollback/global sandbox. Reserve protection is not a
second reserve amount, nor a guaranteed future fuel forecast. Disabled trip rules may HOLD retained
cargo through the original budget (unlike batching's future-intake-only semantics); never cancel it.

## 9. Out of scope

Named reserve amount/service cadence controls, actual multi-source service dispatch, docked homes,
formation/corridors/every provider/fault/global purity/incident/replay/lockstep/CPU/stress/full gates.
No Oil/Tycoon business source/host/demo/Draw/platform changes. No commit/push until FULL finish.

## 10. Sub-task execution order

1. Extend original trip owner and whole admission/exact idempotent commits.
2. Delegate named controls through normal command/private view.
3. Strict master JSON/independent public proof/one bounded physical case.
4. Build/exact runtime; freeze; document observed evidence and remaining full-goal work.

## Scoped verified outcome

All four slice tasks complete. Build2.270.0/save140/FleetView5/fleet service1/native-public trip1;
LogisticsView29/WorkView80/physical payloads unchanged. TWO builds, first12.08s and final4.81s,
both0 warnings/errors. TWO runs SAME ONE exact NEW bounded case: first129 passed2 failed0 aborted
1816ms; new purity assertions assumed initial12 fuel survived pre-held home movement. Test-only
repair compares actual stock before/after query. Final131 checks0 failures/aborts1787ms. Engine,
public proof and JSON unchanged after first runtime; all code/tests/proof/JSON frozen after green.
134 older strict build identities/three old fleet views/shared factory assembly compile-only.
No old/family/full/platform/stale runtime. Native-public actual owners/whole no-prefix/private pure
frozen facts/all fields/current set/pending saves/idempotence/real reserve refusal/loaded trip hold/
original claims/physical fuel burn/metadata removal/physical delivery/saved twins/sole pause/20 steel
=6 delivered+14 source/isolation/omission/five invalid JSON/silent pure-check mutation and ignored
commit fail-stop/refused saves proved.1500 Update bound per physical stage;1787ms not CPU budget.
[Evidence](../../docs/migration/P9_FLEET_TRIP_CONTROLS_VERIFICATION.md). Full goal remains active.
