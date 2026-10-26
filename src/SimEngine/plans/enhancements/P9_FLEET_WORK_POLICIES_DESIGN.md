# P9-C12: Named fleet work-policy controls — Design Document

Status: scoped implementation verified,2026-10-08. Full P1–P12 goal remains active.

## 0. Problem Statement

Standard/WorkPolicies.cs owns individual admission/exact commit; MachineFleets.cs currently supports
only named navigation. Hosts would otherwise loop separate policy commands and could change a prefix
before another member refuses. Whole member admission and current named resolution belong in engine.

## 1. Goals

1. Existing WorkPolicyRules owns deterministic whole-set preview and commit through actual IWorkPolicy.
2. Named command resolves CURRENT membership and does not persist policy settings in fleet records.
3. Refused admission changes no valid prefix; provider commit violations fail-stop, never fake rollback.
4. Immutable company-private per-member facts expose actual settings/blockers through FleetView.
5. One bounded native/public-provider JSON proof demonstrates settings, real queued cargo, pause,
   saved continuation, current membership, no materials loss or instant transfer.

## 2. Design

WorkPolicyRules becomes partial, with selection methods in Standard/WorkPolicySelection.cs.
PreviewGroup copies/canonically sorts unique actual IDs, validates seat/ownership and uses original
CheckConfigure. Actual provider codec observation guards Read/CheckPolicy, not another state store.
Selection entries contain detached immutable WorkPolicySettings and typed blockers. Foreign entries
contain no settings. ConfigureGroup preflights every member before invoking original Configure in
stable order; changed count derives from actual settings. Original Configure treats exact same settings
as a no-op and retains its exact owner/job/session commit checks. Provider failure latches session fault.

MachineFleetService shares named control admission with navigation, then delegates to WorkPolicyRules.
IMachineFleets adds PreviewWorkPolicy/ConfigureWorkPolicy. ConfigureFleetWorkPolicyCommand carries
fleet ID and existing WorkPolicySettings, uses original sender stamping/inbox copying/shared Sync.
No fleet profile overlay, automatic override, work queue, job, receipt, route or policy copy.

FleetView2 exposes private immutable management preview; commands retain normal ownership refusal,
views conceal foreign existence. Fleet codec1, WorkView80, native/public policy codecs, save140 and
physical payloads remain unchanged. WorkPolicyDef/strict JSON remain the single authored settings path.
Public proof module reuses existing independent ProofWorkPolicy, not another gameplay implementation.

## 3. API surface

WorkPolicySelectionEntry/WorkPolicySelectionPreview; WorkPolicyRules.PreviewGroup/ConfigureGroup;
IMachineFleets.PreviewWorkPolicy/ConfigureWorkPolicy; normal ConfigureFleetWorkPolicyCommand;
FleetView.PreviewWorkPolicy. No new loop/clock/draw/platform or composite fleet settings.

## 4. Files to change

WorkPolicies.cs; new WorkPolicySelection.cs; MachineFleets.cs/module; FleetCommands.cs; FleetView.cs;
build identity. Independent module declaration, actual master-selected policy/entity/startup JSON;
shared native factory module/document assembly only. ONE new bounded case/registration. Old build/
fleet-view identity assertions compile-only. Stage/parent/master/README/AGENTS/evidence/manifest.

## 5. Error handling matrix

Malformed/duplicate/empty explicit selection → InvalidArgument; invalid seat → NotSeated;
missing member/fleet → NotFound; foreign command member → NotOwned, private view → NotFound;
absent IWorkPolicy → work.not-policy; null/unknown role/area → original invalid-settings code;
empty named fleet → fleet.empty; native/public refusal preserves all original settings.
Provider read/check mutation or inexact commit → original fail-stop, no healthy partial save/hash.

## 6. Backward compatibility

Development only; revise existing owners directly. No legacy command/alternate policy model.

## 7. Verification

Build completed slice and run ONLY ONE exact NEW bounded case. Repair/rerun observed failures only.
Record EVERY build/runtime attempt. Native/public physical loading/travel/delivery/saves with bounded
stage diagnostic snapshots. After final green freeze gameplay/public proof/tests/JSON; docs only.
No old/family/full/platform/stale tests; broader gates remain open until authorized/proven.

## 8. Risks

Whole admission is not malicious-mod rollback or whole-world sandboxing. Interdependent provider
commit violations fault the session. Changed policy acts through original dispatch/motion gates;
it does not cancel jobs, refund materials, change held/home state or manufacture progress.

## 9. Out of scope

Fleet-wide home/fuel/service/load targets, formations/corridors, every provider/fault/corruption/global
purity matrix, emergency incident end-to-end, replay/lockstep/CPU/stress/full P9/P1–P12 acceptance.

## 10. Sub-task execution order

1. Extend sole WorkPolicyRules with pure whole admission/exact idempotent commit and immutable facts.
2. Extend existing membership owner/normal command/private view without storing policy settings.
3. Compose JSON native/public physical proof and ONE new bounded case.
4. Build/exact proof, freeze, record every attempt and update authoritative tracker.

## Scoped verified outcome

All four slice tasks complete. Build2.267.0/save140/FleetView2/membership1/WorkView80 and physical
payloads unchanged. THREE builds: first6.18s failed0 warnings/1 NEW numeric JSON fixture error;
clean3.35s/3.32s0/0. TWO runs SAME ONE exact NEW bounded case:first110 passed1 failed0 aborted897ms
NEW assertion used definition ID instead of authored state code; test-only repair, final111 checks
0 failures/aborts767ms.131 older build identities/factory assembly compile-only; no old/family/full/
platform/stale suite. Code/proof/tests/JSON frozen after final green.
Physical/native-public saved scope and open gates recorded in
[evidence](../../docs/migration/P9_FLEET_WORK_POLICIES_VERIFICATION.md).
Each physical stage1500 Update bound;767ms not a CPU budget. Full P9/P1–P12 remains open.
