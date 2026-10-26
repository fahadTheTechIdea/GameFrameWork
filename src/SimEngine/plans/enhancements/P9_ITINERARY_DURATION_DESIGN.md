# P9-B11.2b — Selected dock and haul travel totals

Status: B11.2b focused verified, 2026-10-07. Depends on B11.2a prospective travel and B11.1 phase budgets.
Build2.188.0/save101/work view64/motion view8/logistics view19; saved owner payloads unchanged.
[Verification](../../docs/migration/P9_ITINERARY_DURATION_VERIFICATION.md).

## 0. Problem

WorkBoard.DockJourney proves finite approach/service/departure alternatives but drops their
duration. LogisticsService.Preview reruns destination selection after its successful source
continuation. A host cannot budget the selected complete trip without duplicating selectors.

## 1. Goals

1. Existing PreviewDock/DockJourney/SelectDock return their selected movement total and arrival.
2. LogisticsService retains the actual successful destination continuation, never reselects it.
3. HaulPreview exposes complete conditional movement duration for its actual chosen carrier.
4. Unknown/range totals never publish a partial known prefix; zero only means proven no travel.
5. Native/public physical movement exposure and saved continuation match quiet selected trips.

## 2. Design

Extend the existing internal out-results with MotionTravelEstimate. One checked stateless
MotionArrivalContracts.Combine adds legs in route order. Existing actual motion contracts
provide each leg, including no-area native/public routes. Optional providers that cannot
estimate retain explicit unknown, not zero/distance/rate substitution; existing spatial
admission still owns their readiness. No speculative route or selected alternative is saved.

DockJourney accumulates only its successful approach, optional waiting-to-service leg and
selected departure. Failed alternatives do not contribute. PreviewDock handles real local
stationary interaction with zero travel; no fictitious motion capability is required.
Logistics Preview captures selected destination slot/duration in its source continuation;
remove its subsequent repeated destination preview. Returning cargo selects only destination.
Carrier ranking, amounts, claims and fresh acquisition remain under their existing owners.

Travel excludes queue delay, handling, work/service duration and future topology/control
changes. B11.3 fleet rules must treat these separately and refuse unsupported required facts.
No default policy, resource promise, stock grant, timer or fleet model is introduced.

## 3. API

HaulPreview.Travel (null on refusal); MotionArrivalContracts.Combine. Existing IHaulExecution
and LogisticsView project the same result. Definitions remain explicit master JSON; no loader.

## 4. Files

Standard/WorkDocks.cs, Work.cs current callers, Logistics.cs and MotionArrival.cs; relevant
view identity; existing two itinerary test callbacks and authored native/public fixture content.
Stage/evidence/master/current metadata only; no Oil/Tycoon/host/demo/platform work.

## 5. Errors

Existing structural/slot/ownership/resource refusals remain typed. First ordered unknown
propagates; checked total overflow is estimate-overflow with no partial known time. Preview
refusal publishes no trip total or native allocation. Mutating provider guards remain active.

## 6. Compatibility

Direct owner/caller revision, no old overload/wrapper, duplicate selector/codec or legacy reader.

## 7. Verification

Build and only the same two exact Itinerary admission suites. Test selected alternate slots,
waiting/service/departure inclusion, no-area/native/public/unsupported duration, zero/overflow
combination, private/pure/fresh reads, real movement exposure and interrupted saved twins.
Other suites compile only; no full/family/third gate.

## 8. Risks

Structural routes are conditional and do not reserve future traffic/slots. Queues add unknown
delay; fixed-point partial motion may add rounding delay. Broader congestion/performance and
all provider/topology combinations remain open; do not call this full fleet admission.

## 9. Out of scope

Authored fleet reserve/service admission (B11.3), broad B11.4 acceptance and platform drawing.

## 10. Stages

- [x] B11.2b Same-selector complete movement totals and focused native/public/save proof.
  Clean build;360 assertions/two exact itinerary suites (142 admission;218 physical), zero
  final failures/aborts. Other suites compile only. No full/family/third runtime gate.
- [ ] B11.3 Authored fleet policy consumes native selected totals and phase budgets.
  [Fresh funding substage](P9_HAUL_TRIP_POLICY_DESIGN.md) and
  [retained-job funding](P9_RETAINED_HAUL_TRIP_DESIGN.md) are focused verified;
  real service intervals remain required.
