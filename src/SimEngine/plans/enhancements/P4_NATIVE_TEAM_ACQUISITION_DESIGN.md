# P4-G2d / P12-D14 — Native team acquisition

Historical scoped proof, not rerun on2.286.0. [G2f](P4_UNIFIED_NATIVE_ADMISSION_DESIGN.md)
extends the same WorkBoard native assignment owner to mixed timed/staffed commitments and
shared priority admission. Paused/external distributed obligations remain open.

Status: native slice implemented and focused verified,2026-10-08. Full P4/P7/P9/P12/P6/P1–P12 remain open.
Build2.284.0/save141/replay1; ONE new bounded case final97 passed0 failed/aborted1623ms.
Five builds and all three same-case attempts recorded in the
[verification evidence](../../docs/migration/P4_NATIVE_TEAM_ACQUISITION_VERIFICATION.md).

## 0. Problem Statement

WorkScheduling.FitsAssignmentSet selects feasible distinct native machines/resources/productive bays,
but WorkSystem.Assign in Standard/Work.cs commits one job and rebuilds. WorkBoard.Assign acquires
one node's resources/costs before assigning its machine/bay. This cannot acquire the selected native
parallel team as a complete commitment; a later changed obligation can leave an acquired prefix.

## 1. Goals

1. One WorkBoard acquisition owner preflights fresh native job/machine/role/source/dock intent for
   every selected same-company activity job before any mutation; retained preview is not authority.
2. Acquire complete new native resource claims once through original IResourceTransaction, and any
   explicitly enabled optional budget holds once through original IMonetaryBudgets. No new ledger.
3. Existing job/machine/node/dock fields alone own committed team state; no new codec or clock.
4. Single-job and workerless acquisition reuse the same complete resource/budget implementation.
5. Native feasible matched teams commit as one acquisition; unavailable simultaneous sets retain
   deterministic physical sequencing. Emergency interruption uses the existing contract and rechecks
   the complete team after interruption, with the same fault-on-lost-admission semantics as before.
6. One new bounded case proves ordinary refusal has no prefix commitments, real same-Update native
   teams and saved/replayed physical follow-through. External dispatch still owns its acquisition;
   generalized cross-provider distributed acquisition remains a separately open requirement.

## 2. Design

Add Standard/WorkAcquisition.cs as another file of the existing WorkBoard partial owner. A transient
prepared list captures actual jobs/workers/current choices/service slots only for one synchronous
acquisition. Original Assign(job,candidate,slot,resources) delegates to the set path; basic native
Assign(job,IWorker) remains the one actual worker/job field mutation. Fresh IWorkerDispatch.CopyCandidates,
BindResourceSelection and existing dock proposal geometry enforce role/area/selected-machine/slot
identity. Invalid or stale choices cannot silently substitute another selected endpoint or bay.

Generalize existing AcquireResources to one multi-node implementation. Aggregate existing resource
intents and budget requests; all current pure checks precede acquisition. AcquireClaims executes once;
distribute verified actual records to original node.Resources and node.Costs. BindCosts becomes binding
only, not a second reserve path. Workerless/single-node work calls this same owner with a one-entry set.

WorkSystem.Assign groups the current feasible native selections by company in priority order, counts
each existing attempt budget once, commits the native group and rebuilds existing mixed dispatch.
No new scheduler or provider discovery. Nonfeasible joint sets still use existing individual physical
sequencing; haul/external-source offers retain their existing contracts and actual reservation owners.
Native interrupted workers recheck the same selected obligations after original interruption. A failed
provider after valid admission faults Update, never compensates or checkpoints partial success.

## 3. API surface

No new public gameplay/state authority. Internal WorkBoard.Assign set overload uses existing candidates
and stable job IDs. Existing external dispatch/resource/budget/dock/worker contracts remain authoritative.
Build2.284.0/save141/replay1; persistent job/node/service layouts and sole GameClock unchanged.

## 4. Files

| Action | File | Risk |
| --- | --- | --- |
| New | Standard/WorkAcquisition.cs, tests/Beep.SimEngine.Tests/NativeTeamAcquisitionTests.cs | high/medium |
| Modify | Standard/Work.cs, WorkResources.cs, WorkStart.cs | high |
| Modify | Kernel/SaveGame.cs, older identity assertions, Program.cs | mechanical |
| Modify | tracker/P4/P7/P9/P12/P6/README/AGENTS/help/evidence | low |

## 5. Error handling

Missing/stale/noneligible job, machine, chosen source or productive bay: ordinary false/refusal before
mutation. Duplicate set identities/foreign ownership/malformed authored choices: reject complete set.
Pure provider mutation and invalid actual acquire/worker transition: terminal original Update fault;
no simulated rollback or resumable prefix. Costs are absent unless authored configuration enables them.

## 6. Compatibility

None; original paths replaced in place. No adapter, legacy alias or alternate acquisition ledger.

## 7. Verification

Build once after the implementation slice; ONE distinct NEW bounded case:
`Native team acquisition: complete commitments, refusal purity and physical saved continuation`.
Only observed same-case failures repaired/re-run; every build/run recorded. Earlier API/identity
migrations compile-only. No old/family/full/platform runtime. Freeze source/tests/proof/JSON after
final green; documentation-only handoff checks afterward.

## 8. Risks

Current G2e [timed frontier acquisition](P4_WORKERLESS_FRONTIER_ACQUISITION_DESIGN.md) uses this same
owner for complete eligible timed-node sets; focused83 checks. Combined staffed/timed/paused frontiers
and all external/distributed/global-purity/stress gates remain open; this earlier case was not rerun.

Transient admission is not compensation for dishonest extensions. Matching is combinatorial; no cap
or hidden default introduced. External source/native haul joint obligations, global provider purity,
aggregate paused-workerless frontier starts, cross-company distributed transactions and broad stress
remain explicit open gates; do not claim this native team stage finishes them or the full plan.

## 9. Out of scope

Tycoon business policy/source, Oil/siblings, host/demo gameplay, rendering/platforms, extra clocks,
new persistent reservation stores, pricing or an unrelated transport medium.

## 10. Execution

- [x] G2d.1 Generalize the original resource/budget acquisition to a complete node set.
- [x] G2d.2 Replace single native assignment with the shared set admission and dispatch grouping.
- [x] G2d.3 Author/register/build/run one bounded contract and ordinary-player physical proof.
- [x] G2d.4 Freeze green source; update tracker/parent stages/evidence/inventory.
- [ ] Full joint-provider/frontier/global-purity/stress/P1–P12 acceptance and final commit/push.

Open decisions: none for this existing native acquisition requirement.
