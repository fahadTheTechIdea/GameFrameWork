# P9-B4i/P12-D14 — Role-aware facility intent

## 0. Problem statement
WorkFacilities.ReadFacilities stops at the first candidate for the source worker before
IActivityFacilityPolicy.Rank. Excluding that role incorrectly hides another ready role.
The original ActivityWorkerSelection persists worker identity only, so a preferred role
is not binding on actual acquisition.

## 1. Goals
Evaluate every real ready worker-role offer, select by rank, route cost and ordinal role.
Capture the selected probe-node role in the existing worker selection, not another model.
Ordinary native acquisition and continuation must obey it. Save/load, hashing, pending
commands and replay must retain exactly the same intent. Policy edits affect future
postings, not active intent. No extra scheduler, claim book, clock or host gameplay.

## 2. Design
Extend ActivityWorkerBinding with Role; None deliberately means any compatible declared
role. Extend IActivityWorkerSelection.RoleFor and its existing canonical parser, identity
and Sync. Explicit selected-worker JSON requires role:null or a valid role identity.
Validate selected roles against authored positive-time machine nodes.
Original WorkSystem.CopyCandidatesCore filters actual selected roles before admission.
Original WorkBoard validates actual job roles and fresh requested candidates.
WorkFacilities ranks every admitted source-worker role, stores one deterministic best
offer per actual facility, then builds the chosen intent with its exact probe-node role.
All other worker-bound nodes retain explicit unrestricted role intent.

## 3. API surface
ActivityWorkerBinding(node,worker,role=WorkerRoleId.None); IActivityWorkerSelection.RoleFor.
Selected JSON entry: node,worker,role. Role null is deliberate unrestricted intent.
No rendering/platform API or new command. Existing PlanIntent transports the same intent.

## 4. Files to change
Existing ActivityWorkerSelection.cs, WorkFacilities.cs, Work.cs, WorkAcquisition.cs,
WorkModule.cs, Kernel/SaveGame.cs. Independent public policy content/type and one bounded
player driver; normal master declaration/embedding. Identity assertions migrate compile-only.

## 5. Error handling
No ready policy-admitted role: typed no-facility; ready excluded roles: typed exclusion.
Negative rank: contract fault. Missing/unknown/undeclared role: strict invalid argument
or composition error. Wrong actual assignment: fault, never role substitution.
Malformed saved selection: corrupt state. Original physical admission owns resource/dock waits.

## 6. Compatibility
Development-only direct breaking save format and board codec. No old reader or shim.

## 7. Verification
One solution build when implementation is ready. One distinct new exact bounded runtime
case with public commands/views, role exclusion/preference, real travel/spares/work/repair,
pause, save twins, pending command, cancellation, future-policy change and replay.
Repair/rerun only observed failures of this same new case; record every attempt.
No older/family/full/Oil/Tycoon/platform runtime. Freeze executable files after final green.

## 8. Risks
A view-only role choice is insufficient: exact role must reach original job acquisition.
Stock/resources must remain native. Optional role API denotes intentional unbound selection,
not legacy compatibility. A selected role is immutable after posting, like selected worker.

## 9. Out of scope
Global role optimization, all provider/dock/interruption matrices, broad regression,
Oil/Tycoon/platform edits, Draw, publication before full plan completion.

## 10. Execution order
Read original owners; extend sole selection contract and codec; repair facility ranking;
wire original admission/restore; author public proof and driver; focused verification;
update master and source evidence. Full P1-P12 completion remains unproven.

## 11. Approval
Covered by the active approved implement-plan goal. User publication is conditional on
FULL goal and plan completion, not this slice.

## 12. Focused result
Implemented and frozen on2.302.0/save144/board44/work-view86 at 2026-10-08 13:46:23 UTC.
Four builds (three recorded compile failures, final clean3.74s); ONE new exact case135/0/0
1537ms first runtime attempt. No older/full runtime. Scope and remaining gates in
[evidence](../../docs/migration/P9_ROLE_FACILITY_INTENT_VERIFICATION.md).

