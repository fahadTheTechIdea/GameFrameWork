# P9-B11.3c2b3 — Earned service windows, not retry credit

Status: B11.3c2b3 focused verified, 2026-10-07. Wider fleet/queue/failure acceptance remains open.
Build2.195.0/save104/board36/work35/logistics13/work view69/logistics view24/native recurring8.
[Verification](../../docs/migration/P9_EARNED_SERVICE_WINDOW_VERIFICATION.md).

## 0. Problem

RecurringPlanContracts.ResolveCadence delays failed/cancelled retries using LastPostedTick.
CheckMovementWindow currently treats that admission deadline as earned service readiness. An
unsuccessful attempt can therefore make an overdue or never-serviced machine appear fresh.

## 1. Goals

1. Successful-completion service windows derive only from actual whole-cycle success and interval.
2. Retry spacing remains unchanged, but never grants maintenance credit.
3. Fresh hauling service references require scheduled successful-completion worker-bound sources;
   generic submission-based recurring plans remain valid outside these service references.
4. Private immutable views distinguish next attempt timing from actual earned service timing.
5. Cancelled/unfinished work retains prior success, purity, saves and deterministic physical retry.
6. All timing remains derived from one GameClock and the existing source/board history owners.

## 2. Design

Extend the existing canonical ResolveCadence calculation with a second derived output: earned
service due time. This is read scratch, not saved state, another clock, a second cadence calculator
or another history ledger. Existing DueTick/next attempt semantics retain cancellation retry spacing.
ServiceDueTick/ServiceDueGameTime on RecurringPlanView expose actual successful-completion timing.
Generic submission cadence has no completed-service deadline. Initial unserviced completion cadence
is due at game time zero, not at the most recent cancelled posting.

CheckMovementWindow uses earned service due time while preserving outstanding/pending/overflow,
disable and emergency semantics. Existing HaulTripPolicyContracts validates referenced cadence as
successful_completion; submission time cannot impersonate performed maintenance. Whole real success
earns a new window; attempted, cancelled or partially completed service never does.

## 3. API

RecurringPlanView.ServiceDueTick and ServiceDueGameTime are detached derived facts. No new command,
profile field, saved payload, compatibility reader or host decision rule.

## 4. Files

Standard/RecurringPlans.cs and HaulTripPolicies.cs; current semantic metadata; focused
CompletionServiceTests, existing WorkerServiceTests content composition and public CompletionEffect
provider; new master-listed completion-failed-service.json; stage verification and tracker. Framework only.

## 5. Errors

Submission-based service references refuse composition/configuration, not silently convert cadence.
Overdue earned service refuses movement even while retry admission is in the future. Existing
unknown completion, overflow and physical safety refusals remain authoritative.

## 6. Compatibility

Direct current contract/semantic identity change. Generic recurring submission is still a valid
explicit mode; it is not a service-credit mechanism. No legacy/default/compatibility path.

## 7. Verification

Build plus only two exact completion-service suites. First reproduce false window after initial
cancellation. Cover native/public sources, cancellation after prior success, immutable timing,
private purity, saved retry wait, real timed retry and restored readiness after whole completion.
Separate suites compile only; no full/family/third gate.

## 8. Risks

Movement-only windows still exclude unknown queues/handling delays and do not guarantee arrival.
Broader failed-order/provider/congestion/replay/performance acceptance remains open.

## 9. Out of scope

Oil/Tycoon/hosts/demos/platforms/Draw; new service schedulers, clocks, resource grants or success ledgers.

## 10. Stages

- [x] Reproduce unearned cancellation credit with exact focused regression: two initial failures,
  native/public, both returning none instead of the typed movement-window refusal.
- [x] Canonical earned-service timing, strict references and immutable view projection.
- [x] Native/public cancelled and actual failed workflow, physical retry/save proof, current clean
  build and tracker evidence:550 assertions/two exact suites (274 admission;276 physical), zero
  final failures/aborts. Other suites compile only; one unchanged GameClock and owner payloads.
- [ ] Wider B11.3c2b/B11.4 fleet/queue/failure/provider/replay/performance/full acceptance.
