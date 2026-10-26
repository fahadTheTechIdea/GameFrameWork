# P9-B11.2 — Prospective physical travel durations

Status: B11.2a focused verified, 2026-10-07. B11.2b is now separately
[focused verified](P9_ITINERARY_DURATION_DESIGN.md); the following is B11.2a checkpoint evidence.
Build2.187.0/save101/work view64/motion view8; saved owner payloads unchanged.
Clean build;304 assertions/two exact suites (113 admission;191 physical), zero failures/aborts.
[Evidence](../../docs/migration/P9_PROSPECTIVE_TRAVEL_VERIFICATION.md).
Parent: P9-B11 operating horizons; B11.1 is verified, not rerun in this stage.

## 0. Problem

MotionRouteCheck currently proves structural arrival but contains no duration. IMotionEstimate
only handles a live current route. Fleet admission cannot budget future legs without copying
router/speed arithmetic. Existing WorkBoard dock alternatives must remain the sole selector.

## 1. Goals

1. Extend actual IMotionAreaConstraint.CheckRoute/MotionRouteCheck, never another router/provider.
2. Native and public motion owners calculate prospective duration with their physical interval
   budget and actual controlled multiplier. A typed unknown never becomes zero or a default.
3. Unassigned operating area means no area restriction, not an invented region; structural
   obstacles/access still apply. Native structural probes use the existing single A*.
4. Existing private MotionView exposes pure detached duration/arrival facts; no movement,
   reservations, fuel draw, game-clock change or saved speculative route.
5. Whole actual dock/haul itinerary duration will be aggregated inside existing selectors,
   including chosen service/departure alternatives; never separately choose a second route.

## 2. Design

B11.2a adds required explicit Travel to MotionRouteCheck: admitted structural routes carry
MotionTravelEstimate (known nonnegative duration or typed unknown); refused routes carry null.
CheckRoute receives the controlled speed multiplier observed by MotionAreaContracts.Plan.
Plan guards shape, alias identity, purity, actual geometry and duration. Disabled/rate-gated
operations retain structural arrival but publish typed unknown duration. No fuel gate is used
to decide duration: B11.1 evaluates that separate future requirement against current inventory.

Native NavGrid.ProbeStructuralRoute may return a transient path through the same PathSearch.
StructuralCostFor is its single terrain/static-occupancy rule. Mover's one segment calculator
accepts that rule or actual physical occupancy; both estimates and movement share weighted
segment math and TickBudget. PointMotion uses its own actual coordinate/rate owner. Already
reached prospective geometry has zero travel; a new explicit movement command can still need
an update to settle its state. This is travel exposure, not a promised lifecycle completion.

B11.2b remains required: carry selected travel totals through WorkBoard's existing
PreviewDock/DockJourney/SelectDock and source-to-destination continuation into HaulPreview.
Do not fabricate waiting/handling/service duration or bypass current claims/queues.
B11.3 authored fleet admission then consumes this native itinerary and B11.1 calculator.

## 3. API

IMotionAreaConstraint.CheckRoute(..., Fix speedModifier); MotionRouteCheck.Travel;
MotionView.ReadRoute(id, origin, destination, out route). Existing master JSON time, motion
rate, control and area definitions remain authoritative; no new loader or policy default.

## 4. Files

Standard/MotionAreas.cs, Navigation.cs, Movement.cs; Views/MotionView.cs; public proof
PointSpaceProof.cs; existing two ItineraryAdmissionTests callbacks. Later B11.2b extends
WorkDocks.cs/Logistics.cs using their current selectors. Relevant identity/docs/evidence.

## 5. Errors

Invalid geometry/intents refuse explicitly. Structural Outside/Unsupported/Pending remain
unchanged. Travel unknowns use existing estimate codes; invalid/negative/contradictory or
mutating provider results fault. Overflow is typed, never an incomplete known total.

## 6. Compatibility

Direct contract revision, all current providers/callers migrate. No overload shim, duplicate
planner, legacy reader, clock, route cache or authoritative speculative state.

## 7. Verification

Build and only the same two exact suites:

- Itinerary admission: prospective provider routes and atomic private previews
- Itinerary admission: permitted alternatives, physical queues and saved execution

Add native/public future origins, actual controlled rates, private/immutable/pure views,
typed disabled/zero/unsupported/range results, no-area structural routing, current route
unchanged, provider violations and real conditional physical-duration/save parity.
All other suites compile only. No full/family/third gate.

## 8. Risks

Structural routes ignore movable congestion; future topology/rates/queues may change. Fixed
point partial movement can add rounding delay; prospective ETA is conditional, not reservation.
Path scratch is temporary; broader provider/global purity/performance proofs remain open.

## 9. Out of scope

Oil/Tycoon/hosts/demos/Draw/platforms. B11.3 fleet policy and B11.4 broad acceptance remain open.

## 10. Stages

- [x] B11.2a Provider-owned prospective travel duration and focused native/public physical proof.
- [x] B11.2b Existing-selector complete dock/haul itinerary totals and physical/save proof;
  [separate stage and current evidence](P9_ITINERARY_DURATION_DESIGN.md), not the historical B11.2a gate.
