# P9-B11.3c2b4 — Engine-owned multi-source service readiness

Status: focused verified, 2026-10-07. Broader fleet/queue/acceptance roadmap remains open.
Build2.196.0/save104/board36/work35/logistics14/work view69/logistics view25/native recurring8.
[Verification](../../docs/migration/P9_MULTI_SOURCE_SERVICE_WINDOW_VERIFICATION.md).

## 0. Problem

Standard/HaulTripPolicies.cs.CheckDispatch checks each configured service source, but
Views/LogisticsView.cs exposes only source references. A host would have to combine deadlines
and blockers to describe the machine's readiness. That calculation belongs in the engine.

## 1. Goals

1. One pure recurring service-window projection derives earned time from ResolveCadence.
2. One trip-policy aggregate serves both private diagnostics and fresh movement admission.
3. Multiple native/public sources select the earliest deadline, with stable source-ID ties;
   typed blockers take precedence and ties between blockers also use stable source identity.
4. Disabled future sources and empty profiles are explicitly unrestricted, not zero-duration
   service. Pending/invalid/expired service never becomes an invented usable window.
5. Real two-source service completes through the existing physical board and survives save/load.
6. All facts derive from the session's one unchanged GameClock. No separate time/update owner.

## 2. Design

ServiceMovementWindow is an immutable read projection with source, restriction, earned due game
time, remaining game time/update steps and typed blocker. It is not persisted and never grants
authority to mutate or reserve. RecurringPlanContracts.ReadServiceWindow shares ResolveCadence;
CheckMovementWindow delegates to that projection's movement check.

HaulTripPolicyContracts.ReadServiceWindow reads actual selected profile/source contracts under
EntityObservation, sorts source IDs, and selects typed blockers before earliest ready deadline.
CheckDispatch uses the same aggregate. Unknown movement, disabled trip policy and eligible
emergency precedence remain unchanged. Retained cargo still uses CheckTrip, not fresh windows.

LogisticsView.ReadHaulTripServiceWindow uses existing company ownership and returns detached
facts; hosts do not combine deadlines or decide eligibility. No new JSON field, codec or clock.

## 3. API

Public immutable ServiceMovementWindow; ReadServiceWindow on recurring and trip contracts;
private LogisticsView.ReadHaulTripServiceWindow. Existing public checks retain their signatures.

## 4. Files

Standard/ServiceMovementWindow.cs, RecurringPlans.cs, HaulTripPolicies.cs, Logistics.cs,
LogisticsModule.cs; Views/LogisticsView.cs; CompletionServiceTests.cs; current semantic identity
literals; this stage, verification, master tracker, AGENTS, README and help.

## 5. Errors

Outstanding service reports ServicePending with unknown usable duration. Other protocol blockers
stay typed. Overdue service refuses even zero movement; at exact due time zero movement is valid.
Empty/disabled source sets explicitly report unrestricted without a fabricated zero deadline.
Foreign callers receive existing ownership refusal and no diagnostic value.

## 6. Compatibility

Direct current semantic identities; no legacy reader, shim, duplicate ledger or default content.

## 7. Verification

Clean solution build, then only the same two exact completion-service suites. Add multi-source
native/public composition, tie/order invariance, purity/privacy/retained snapshots, real serial
service, remaining-time admission, ordinary policy reconfiguration and saved twin assertions.
Separate suites compile only. No full/family/third gate.

## 8. Risks

The result is a movement-only allowance, not a queue/handling ETA or guaranteed arrival. Multiple
service fairness is proven for the authored two-source case, not arbitrary fleet contention.

## 9. Out of scope

Oil/Tycoon/hosts/demos/platforms/Draw; new scheduler, clock, saved service state or automatic repair.

## 10. Stages

- [x] Shared recurring projection and canonical trip aggregation/private view.
- [x] Two-source physical, deterministic and saved-continuation proof in two exact suites.
- [x] Clean build, scoped evidence and tracker update:702 assertions (338 admission;364 physical),
  zero final failures/aborts. Other suites compile only; one unchanged GameClock.
- [ ] Broader B11.3c2b/B11.4 fleet/queue/provider/performance/replay/full acceptance.
