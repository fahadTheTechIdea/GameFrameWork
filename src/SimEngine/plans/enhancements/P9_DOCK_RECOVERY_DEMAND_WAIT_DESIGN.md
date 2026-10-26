# P9 — Physical dock departure before replacement demand

## 0. Problem

LogisticsRecovery.TryReplanDestination requires a ready full replacement before its leaveDock
branch. A loaded worker whose old service endpoint is outside its new operating area remains in
the bay even though a valid physical exit exists. Destination readiness should gate retargeting,
not safe physical departure. SimGame.World is already internal; no boundary mutation is needed.

## 1. Goals

1. Existing job leaves the actual bay through admitted exit geometry without future inlet promises.
2. Cargo/old inlet receipt, partial delivered prefix, worker/job identity and age survive departure.
3. Demand-only destinations below full remaining commitment cause physical exit wait, not splitting,
   teleporting, instant fallback or a new job. Changed compatible demand resumes normal retarget.
4. Normal player policy/stock commands and saved native/public cargo sessions prove the workflow.

## 2. Design

Move existing leaveDock execution before replacement selection. Validate actual cargo and old inlet
receipts first. PreviewRecoveryDeparture with null continuation admits only real exit motion/dock
rules; it invents no output. Existing recovery bay/exit/cause/toils own the state. Once physically
departed, existing WorkSystem clears the bay and waits or selects/reserves full actual replacement.
No new field, scheduler, policy, reservation store or clock. Same IWorkDock/IHaulExecution contracts.

## 3. API

No new API or payload. Direct engine/save identity because behavior changes; no compatibility path.

## 4. Files

Standard/LogisticsRecovery; Kernel/SaveGame; existing recovery fixture/new exact scenario; one
explicit JSON native demand-only policy; embedded resource; direct metadata; tracker/readme/evidence.

## 5. Errors

Invalid actual receipt faults. Blocked exit retains actual bay/cargo. No compatible full output
waits at actual exit through existing DestinationUnavailable, retaining original commitments.

## 6. Compatibility

Direct current build/save identity. Existing payloads unchanged; no old reader or fallback.

## 7. Verification

One completed-slice build/ONE exact new bounded scenario only; repair reruns only for observed
failure. Normal player commands, partial unload, physical departure, inadequate demand, saved wait,
compatible demand, real completion and conservation. No older/family/full/platform suite.

## 8. Risks

Departure no longer promises a feasible future output: cargo may wait indefinitely at the exit.
That is explicit physical state, not a fabricated solution. Existing bay vacancy/route/area gates apply.

## 9. Non-goals

Endpoint loss, arbitrary docks/fuel/congestion/provider/replay/stress/full framework acceptance.
No platforms/Draw/hosts/demos/OilFieldDays. Commit/push only after FULL goal/plan completion.

## 10. Execution

- [x] Reorder the existing receipt-validated departure and destination phases.
- [x] Author policy and normal player native/public saved workflow; run only exact gate.
- [x] Record evidence and reconcile tracker; leave full goal/publication open.

Build2.219.0/save123; existing payloads/logistics view28/work view76 unchanged. Existing
LogisticsService validates actual cargo/inlet receipts, admits physical dock exit independently of
future full destination readiness, and lets normal WorkSystem release the bay at actual arrival.
Demand-only shortage waits at real exit; changed full demand reuses same shipment/age/cargo receipt.
Native/public separate-inventory cargo, partial unloading, player area/stock commands and saved
continuations conserve6 without direct failure signals or invented capacity. Final clean build
0 warnings/errors2.58s; ONE exact new scenario30 checks0 failures/aborts391ms. Two clean builds/
two SAME scenario runs: initial28 passed/2 premature bay-release assertions430ms; predicate waits
for engine release after motion arrival, final30.93 older metadata files/151 identity lines compile
only. No older/family/full/platform or stale-binary run; no post-gate gameplay edit. No new state owner,
clock, API or payload. Full goal/plan remain open; commit/push only after FULL completion/verification.
[Evidence](../../docs/migration/P9_DOCK_RECOVERY_DEMAND_WAIT_VERIFICATION.md). Only this scope is
complete; unused prospective-egress selector cleanup and broader framework acceptance remain open.
