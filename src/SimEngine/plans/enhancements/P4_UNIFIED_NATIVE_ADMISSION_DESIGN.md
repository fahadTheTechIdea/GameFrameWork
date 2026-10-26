# P4-G2f — One native activity admission pass

Follow-on [P9-B4f native prefixes](P9_NATIVE_ADMISSION_PREFIXES_DESIGN.md) now selects the longest
purely feasible first native frontier before original whole-set acquisition. Lower infeasible timers
do not invalidate feasible earlier source alternatives/teams. Whole COMMIT refusal still has no
member fallback. This startup report remains historical, not rerun on2.299.0.

[G2h](P4_MIXED_PRODUCTIVE_SCHEDULING_DESIGN.md) now integrates mixed native productive work
across orders through the same rank and commitment owners. Startup semantics remain unchanged;
earlier runtime evidence is historical, not rerun on2.291.0. Broader external/full gates remain open.

Historical startup proof, not rerun on2.287.0. [G2g](P4_PRODUCTIVE_FRONTIER_COMMIT_DESIGN.md)
extends the same native WorkBoard owner to complete productive commitments and acquired timed
Working/Paused frontiers. G2f startup rank/matching remains; cross-order/mixed productive scheduling
and external distributed obligations are still open.

Status: native slice implemented and focused verified,2026-10-08. Full P4/P7/P9/P12/P6/P1–P12 open.
Build2.286.0/save141/replay1/WorkView81; ONE new bounded case109/0/0 1672ms after two assertion repairs.
All three builds/runs in [evidence](../../docs/migration/P4_UNIFIED_NATIVE_ADMISSION_VERIFICATION.md).

## 0. Problem

WorkSystem.AdvanceOrder starts timed work before WorkSystem.Assign ranks staffed jobs and external
dispatch. Earlier-posted timers can take shared stock before higher-priority native machine work.
G2d/G2e whole-set acquisition is separate by executor kind, not a complete mixed native frontier.

## 1. Goals

1. Timed and staffed native intents enter the same existing dispatch ranking/pass, including emergency,
   player priority, authored starvation, travel and stable identities; no second policy/scheduler.
2. Explicit RequiresWorker facts distinguish a genuine timer from a missing machine, never fake workers.
3. Same WorkBoard admits/acquires feasible mixed same-company native obligations together across orders.
4. Ordinary complete-set refusal has no prefix; scarce sets fall back to ranked physical sequencing.
5. Timed starts remain zero-progress until next Update; real staffed travel/work/consumption unchanged.
6. Existing jobs/nodes/receipts own state, save/hash/replay/private views; no new admission ledger/codec.

## 2. Design

Extend existing WorkerDispatchOffer with required explicit RequiresWorker; migrate all current
constructors in place. Native timed offers use actual order ID and node entry identity, zero travel
cost, no worker/interruption; external IWorkerDispatchSource remains explicitly machine-based.
CopyAssignments adds private dependency-ready timed facts and uses original RankOffer/CompareAssignments.
CopyAssignmentCandidates cannot create machine candidates for timers. Distinct matching gives timed
intents zero machine capacity; joint CheckWorkSet still includes their native resources and costs.

AdvanceOrder only marks ready timed nodes and dirties original dispatch. Delete StartWorkerlessFrontier
and same-loop started-ID guard: starts now happen once at the existing final admission phase.
WorkSystem.Assign groups jointly feasible native staffed/timed entries by company through one existing
WorkBoard acquisition. On nonfeasible sets it processes the actual ranked individual intent, retaining
sequential tool/material use. External offers participate in rank/machine coverage but keep own acquisition.

Refactor WorkBoard timed preflight into a shared pure helper over actual (OrderId,NodeId) references,
not execution copies. Combined native acquisition rechecks before/after original emergency interruption,
then obtains one resource/optional-budget set and binds only original fields. Canonicalize actual node
contexts before acquisition so optional account token order also matches reversed caller arrival.

## 3. API

WorkerDispatchOffer.RequiresWorker is explicit; no old constructor or inferred nature.
WorkView schema81, Build2.286.0/save141/replay1; persistent layouts/sole GameClock unchanged.

## 4. Files

| Action | File | Risk |
| --- | --- | --- |
| Modify | Standard/WorkContracts.cs, WorkScheduling.cs, WorkAcquisition.cs, WorkResources.cs, Work.cs | high |
| Modify | Standard/WorkModule.cs, native/proof offer constructors, Kernel/SaveGame.cs, old identity/view assertions | mechanical |
| New | tests/Beep.SimEngine.Tests/UnifiedNativeAdmissionTests.cs, explicitly listed proof JSON | medium |
| Modify | proof factory/embedding/registration, stage/tracker/parents/evidence/README/AGENTS/help | low |

## 5. Errors

Missing/stale/duplicate/foreign/dependent timer or staffed obligations refuse complete native sets
without mutation. Provider errors or lost admission after interruption fault original Update, no rollback.
Unknown executor nature and missing real machines do not become timers or inferred ready facts.

## 6. Compatibility

None. Remove old early-start scheduling and migrate current public constructors/source in place.

## 7. Verification

Build completed slice; ONE exact NEW bounded case:
`Unified native admission: mixed priorities, complete commitments and physical saved continuation`.
Only observed same-case repairs/re-runs; all older migrations compile-only. No older/family/full/
platform/stale runtime.2000 Update phase bounds with diagnostics, not watchdog/CPU/soak proof.
Freeze source/proof/tests/JSON after final green, then documentation-only handoff.

## 8. Risks

No artificial tool deadlock or extra same-Update timed work. External/native-haul distributed obligations,
paused advancement aggregation, all role/recipe/provider faults/global purity/CPU/stress remain open.

## 9. Out of scope

Tycoon policy/source, Oil/siblings, host/demo gameplay, Draw/platforms, new state/clock/ledger/price defaults.

## 10. Execution

- [x] G2f.1 Explicit mixed dispatch facts and original same-pass rank/matching/admission.
- [x] G2f.2 One fresh mixed WorkBoard acquisition and canonical original resource/budget commits.
- [x] G2f.3 Master-listed JSON/public-player/contract proof, one exact new runtime gate.
- [x] G2f.4 Freeze source, record every attempt and scoped tracker/evidence/inventory.
- [ ] Full external/distributed/global-purity/stress/authorized P6/P1–P12 and final commit/push.

Open decisions: none for unified existing native activity admission.
