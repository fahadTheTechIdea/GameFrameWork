# P9-B4b1 — Authored emergency activity preemption

Status: focused-verified B4b1, 2026-10-05. Parent [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Current source: build2.67.0/save39/work18/work-view22; 174 checks in two exact suites.
[Evidence](../../docs/migration/P9_EMERGENCY_PREEMPTION_VERIFICATION.md). Broader B4b remains open.
Prerequisite: [B4a shared starvation dispatch](P9_STARVATION_DISPATCH_DESIGN.md).

## 0. Problem

WorkSystem.CopyCandidates rejects every worker committed to another job. Shared emergency ordering
therefore cannot reclaim an interruptible machine. Existing Interrupt/Reopen retain ordinary activity
age/progress/spent inputs, but only explicit player control uses them. Extend that owner, not a
preemption scheduler, synthetic worker, cloned job, virtual inventory or priority loop.

## 1. Goals

1. The selected immutable dispatch profile may also expose IWorkerDispatchPreemptionPolicy.
   Authored profiles require explicit preempt_emergencies; no default or additional policy store.
2. One explicit WorkCandidateMode selects idle-only or emergency-interruption previews through
   the existing candidate implementation. Public contracts/callers migrate, no old overload.
3. Engine safety independently requires a valid current bound emergency activity, same-company
   live non-emergency ordinary incumbent, actual worker relation, interruptibility, compatible role/
   area, availability, positive requirements and actual route. A mod profile cannot bypass this.
4. Prefer a genuinely eligible idle worker over interruption. Otherwise use existing machine
   preference/travel/stable ties. Existing common emergency/priority/age comparison remains sole
   intent ranking. Profiles inspect detached incumbent facts, never mutable job/world handles.
5. Combined immutable offers identify the potential InterruptedJob without acquiring anything.
   Actual TryAssign rechecks current admission, interrupts only that actual incumbent, stops its
   old motion, then freshly selects/acquires the emergency through normal native admission.
6. Original incumbent node/job/age/progress/spent inputs remain authoritative. Release only its
   unspent owned claims; no refund or reset. The emergency travels/works over time, then ordinary
   work can resume through the same queue. Saved/private projections and continuation remain exact.
7. Never automatically interrupt actual hauls, loaded cargo, dock ownership or another emergency.
   Generic standing-haul preemption and physical cargo handoff require subsequent current-owner
   admission work; this gate must not claim they are implemented.

## 2. Design

Extend DispatchPolicies/DispatchPolicyModule with an optional preemption capability on the same
actual catalog alias. WorkExecutionFacts are immutable scalar facts derived from the actual job/node.
WorkContracts adds explicit candidate mode and potential interrupted-job identity on detached offers.
WorkSystem.CopyCandidates remains the sole selector, with the same policy/route/requirement evaluator.
WorkScheduling shares one BuildQueue and rank helper; native diagnostics stay idle-only and combined
assignment may preview safe emergency interruption. Source providers cannot submit interruption rank.
WorkSystem.TryAssign owns actual revalidation/Interrupt/native reacquisition; no additional system/
phase/queue/persistent state. Failed re-admission after actual interruption faults rather than
fabricating rollback, compensation, a free worker success or resumable partial checkpoint.

## 3. API

IWorkerDispatchPreemptionPolicy.CanPreempt(WorkerDispatchOffer, WorkExecutionFacts).
IWorkerDispatch.CopyCandidates gains required WorkCandidateMode before destination. Existing
WorkWorkerCandidate/WorkDispatchOffer/WorkerDispatchOffer gain detached InterruptedJob identity.
Authored dispatch JSON requires preempt_emergencies. Optional independent content can implement
the same capability. Work/job/native/logistics/alert codecs stay unchanged; view/build/save identity
advance with breaking semantics. Module/content fingerprints describe the selected policy.

## 4. Files

Existing DispatchPolicies/module/WorkContracts/WorkScheduling/Work/WorkView/WorkModule/SaveGame;
explicit proof JSON/derived profile and registered two-suite tests; master/parent/P4/P7/README/
AGENTS/current evidence. No copied candidate selector, compatibility loader or host logic.

## 5. Errors

Missing/nonboolean authoring refuses. Foreign preemption capability alias refuses composition.
Locked/ineligible/foreign/emergency/haul incumbents remain protected with typed existing blockers.
Invalid current response never becomes an interruption offer. Source-supplied interruption identity
refuses. Native/provider failure faults normal Update; no compensating resource/state fabrication.

## 6. Compatibility

Development only, no legacy API/JSON/save reader or compatibility overload.

## 7. Focused verification

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency preemption: strict policy, pure candidates and protected execution"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency preemption: physical interruption, retained inputs and saved continuation"
```

Only these two distinct names may run/repeat this stage. Bounded physical helpers must diagnose
actual job/worker/response/progress/stock/blocker on failure. Other suites compile only.

## 8. Risks

Counterfactual eligibility may ignore only the actual incumbent worker relation, never inventory,
money, route or policy blockers. Existing held resource claims can block a response; do not invent
post-release supply previews. Actual native re-admission is required after interruption.

## 9. Out of scope

Standing-haul supply interruption, actual haul/loaded cargo handoff, normal-priority preemption,
editable plans/fleet/areas, fuel/repair/congestion/replanning, complete provider/replay/lockstep/
stress/acceptance, Oil/Tycoon and host/demo/platform/rendering/Draw. Full plan remains partial.

## 10. Execution

- [x] B4b1.1 strict optional same-profile capability and detached execution/candidate contracts.
- [x] B4b1.2 same-selector pure safe preview and actual interruption/re-admission.
- [x] B4b1.3 private/strict/safety/material/progress/physical/saved proof in two suites.
- [x] B4b1.4 linked current evidence/tracker and remaining full-preemption gates.
