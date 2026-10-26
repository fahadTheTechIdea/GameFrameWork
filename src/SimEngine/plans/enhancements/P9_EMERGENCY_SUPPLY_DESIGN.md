# P9-B4b2 — Emergency supply dispatch

Status: focused-verified B4b2, 2026-10-05. Parent [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Current source: build2.68.0/save40/work18/work-view23/logistics4/logistics-view9.
173 checks / two exact suites. [Evidence](../../docs/migration/P9_EMERGENCY_SUPPLY_VERIFICATION.md).
Broader B4b/P9/full framework gates remain open.
Prerequisite [B4b1](P9_EMERGENCY_PREEMPTION_DESIGN.md).

## 0. Problem

LogisticsService.Preview uses idle-only CheckAssignment. Urgent response demands may rank first
but cannot reclaim an interruptible ordinary activity worker to deliver their actual inputs.
WorkSystem already owns safe activity interruption; reuse it, not a hauling scheduler or fake stock.

## 1. Goals

1. Required WorkCandidateMode on existing dispatch-source CopyOffers/TryAcquire migrates every
   implementation/caller; no overload/default/parallel source. Native raw logistics diagnostics
   stay idle-only, combined offers include potential emergency supply takeover.
2. One public IWorkerDispatch preview/actual interruption protocol generalizes existing engine
   safety/profile willingness to declared source offers. Original activity candidate path reuses it.
   Only current real emergency demand can reclaim same-company Assigned/Paused ordinary non-haul
   work, with interruptibility, empty cargo, allowed role/area, availability and selected scope.
3. HaulPreview exposes detached InterruptedJob. Existing native selector still checks all current
   stock/reserves, receiving/cargo capacity, handling, dock slots and both route legs. Idle carriers
   precede interruption. No post-release imaginary supply, native claim mutation or copied jobs.
4. Actual source acquisition re-resolves its intent, previews again, asks the existing WorkSystem
   owner to interrupt, then performs fresh idle-only native haul admission. A failed committed
   takeover faults, cannot checkpoint, and never compensates/refunds/reset progress.
5. Real pickup/loading/travel/delivery precede response work. Original job/progress/age/spent
   inputs remain, unspent holds release. Actual acquired hauls/loaded cargo/emergencies remain
   protected. Ordinary stock/routes or high priority are not emergency authority.
6. Private pure projections, policy scope/veto/eligibility, native supply blockers, physical timed
   emergency supply/repair/resumption, protected cargo and isolated saved twins pass in two suites.

## 2. Design

Generalize WorkSystem's existing CanConsiderInterruption guard to company/scope/emergency/worker;
actual activity binding remains checked by its current owner. IWorkerDispatch.PreviewInterruption
accepts detached current offer facts, checks that sole guard and selected immutable profile.
InterruptForDispatch rechecks before existing Interrupt. Providers retain native/intent eligibility.
WorkSystem validates source interruption identity against current relation and same preview protocol.
Sources may request interruption facts, never override safety or submit derived alert/aging scores.

LogisticsScheduling.BuildQueue uses required mode plus detached standing intent metadata through
the existing PreviewSource/Preview selector. Preview ignores only prospective incumbent busy
relation in emergency mode; after physical/native checks, selected profile must accept that actual
candidate. Idle first, then existing carrier preference/cost/ID. Across sources idle-backed supply
precedes interrupt-backed supply before source preference/cost. Existing Assignment carries facts.
TryAcquire re-evaluates; WorkSystem performs actual interruption; ordinary Acquire owns real receipts,
carrier/dock/job relation. No new system/pass/store/codec/registry or saved preemption state.

## 3. API

Required mode on IWorkerDispatchSource.CopyOffers/TryAcquire. WorkerDispatchOffer's public constructor
requires explicit interrupted-job identity; zero means none. IWorkerDispatch adds pure
PreviewInterruption(World, WorkerDispatchOffer) and actual InterruptForDispatch(World, WorkerDispatchOffer).
HaulPreview adds InterruptedJob. Work/logistics view/build/save identities advance together; layouts
stay with existing owners. Existing strict master-listed preempt_emergencies/scopes are the policy.

## 4. Files

WorkContracts/Work/WorkScheduling; Logistics/LogisticsScheduling; WorkModule/LogisticsModule/SaveGame;
public-only dispatch proof/profile and explicit JSON; two registered tests; master/P9/P4/P7/README/
AGENTS/evidence. No OilFieldDays/Tycoon/host/demo/platform/rendering/Draw work.

## 5. Errors

Malformed/foreign/private source interruption facts refuse. Scope exclusion/veto or current guard
keeps a truthful wait. Native/dock/route/resource blockers never imply readiness. Actual lost native
admission after interruption faults Update and blocks save/hash. Never release another owner's cargo.

## 6. Compatibility

Development only. Migrate contracts/callers in place, no old reader/overload/compatibility shim.

## 7. Verification

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency supply: pure dispatch, native blockers and protected carriers"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency supply: timed delivery, retained work and saved continuation"
```

Only these two distinct complete names may run/repeat. Physical waits bounded with actual job,
blocker, cargo and demand diagnostics. Other suites compile only; full regression remains open.

## 8. Risks

Busy prospective workers must pass every native/route/policy check without mutations. Actual old
resource holds can block a prospective supply; do not fabricate released quantities. Retained views
never authorize actual takeover. Whole acquisition/refusal revalidation stays with native owners.

## 9. Out of scope

Automatic active-haul interruption/cargo handoff, ordinary-priority preemption, evacuation/return
arbitration, all provider matrices/replay/stress, editable plans/areas/fleets/fuel/repair/congestion/
replanning/full acceptance. This completes standing emergency-supply takeover only, not full P9.

## 10. Execution

- [x] B4b2.1 same-owner generalized safety and required source contracts.
- [x] B4b2.2 pure physical/native logistics preview and actual revalidated takeover/admission.
- [x] B4b2.3 two-suite private/strict/physical/durability proof.
- [x] B4b2.4 linked current evidence/tracker and honest broader remaining gates.
