# P9-B11.3b — Retained haul funding and physical replenishment

Status: focused verified, 2026-10-07. Parent B11.3 and the full framework goal remain incomplete.
Build2.190.0/save103/board36/work32/movement6/logistics11/work view65/motion view9/
logistics view21/trip component1. [Evidence](../../docs/migration/P9_RETAINED_HAUL_TRIP_VERIFICATION.md).

## 0. Problem

Fresh trip admission is not sufficient when actual routes, outlet floors or operating controls
change during travel. Movement runs before work. Work-only checking would allow another input
charge before discovering the hold. Loaded machines must remain physically refuelable without
releasing cargo or restarting their jobs. Existing paused-haul validation needs an explicit
saved policy-hold cause rather than interpreting every possible extension refusal as a retry.

## 1. Goals

1. Recompute remaining selected movement from actual geometry and native exposure credit.
2. Gate physical movement before its actual operating-input commit.
3. Keep the same job, carrier, cargo, destination capacity and earned handling during holds.
4. Permit existing automatic stock-band refueling through actual courier motion and transfers.
5. Save/hash the hold on the existing job; never store a route, budget, deadline or extra clock.
6. Publish private immutable funding and hold diagnostics from the same authoritative owners.

## 2. Design

WorkBoard.SelectRemainingJourney is extracted from ReplanDockBindings. Pure remaining-trip
reads and actual dock rebinding use this one selection path, including pickup continuation,
wait-to-service and departure. Failed selections expose unknown travel with a typed blocker,
not a known prefix. No-dock endpoints have no fabricated departure. Replanning alone changes
native slots and obsolete motion; reading never does. Partial handling cannot move to waiting
slots. Existing providers own route geometry, rate arithmetic and native input quantization.

IHaulExecution.ReadHaulTripBudget delegates through the existing LogisticsService, actual
IWorker.Job and WorkBoard. Existing HaulTripPolicyContracts and inventory transaction evaluate
current requirements and actual outlet floors. Acquired return cargo is already excluded by
native outbound receipts; the fresh admission commitment is not added a second time.

MotionExecutionContracts has one capture path. Actual movement includes the incumbent trip
gate before existing operating-input consumption. Assignment and dock FIFO read physical
readiness without incumbent trip selection, preventing queue -> job -> trip -> queue recursion.
This separation does not bypass actual movement funding or introduce another scheduler.

WorkSystem.HoldTrip serves normal execution and policy refresh. A blocked retained haul is
paused, RetryAt is zero, and actual motion is stopped through its verified contract. No failure
count, handling credit, cargo or native promise is recreated. The stationary machine remains
an actual logistics recipient. Existing stock-band commands and couriers supply physical fuel;
the next normal update rechecks actual inventory and resumes the same job from its position.
Player/work/plan/ownership refusals retain their existing precedence and cancellation rules.

Job.TripHeld distinguishes policy holds from retry waits on that same authoritative record.
Its existing Sync writes it once. Validation requires a paused haul, typed blocker, no retry
deadline and actual trip capability. Ordinary pauses, retries, interruption and ending clear
the flag. No current-time field is added. All activity continues against the same GameClock.

## 3. API

- IHaulExecution.ReadHaulTripBudget(EntityId) returns a detached evaluation or null when no
  authored active haul applies.
- LogisticsView.ReadHaulTripBudget returns private typed absence/ownership results.
- MotionExecutionView.TripBudget projects the same engine-calculated gate.
- WorkJobView.TripHeld projects the actual job's hold cause.
- Existing ConfigureHaulTripPolicy/Operation/StockPolicy/OutletPolicy commands remain owners.
- Existing master-listed profiles and content remain unchanged; no implicit new JSON rule.

## 4. Files

Standard/WorkDocks.cs, Work.cs, WorkContracts.cs, LogisticsTripBudget.cs, HaulTripPolicies.cs,
MotionExecution.cs; existing Work/Motion/Logistics module view identities; WorkView/LogisticsView;
HaulTripPolicyTests and RetainedHaulTripTests. Direct global build/save metadata, tracker and docs.

## 5. Errors

Disabled and zero-rate operations keep unknown typed travel. Input/reserve shortages hold
without borrowing cargo or future supplies. Structural detours reevaluate actual cost.
Invalid provider mutation remains a fault, not a fabricated route or recovery. Existing live
resource claims prevent endpoint removal; ordinary interruption releases claims before removal.
Lost-participant paths remain native cancellation/recovery, not indefinite trip holds.

## 6. Compatibility

One direct current board codec and view/build/save migration. No old reader, shim or parallel
hold table. Existing work/movement/logistics-service and trip-component payloads are unchanged.

## 7. Verification

Build and only these two exact suites, extending their existing callbacks:

- Haul trip policy: authored contracts, budgets and atomic private admission
- Haul trip policy: physical funding, return commitments and saved continuation

205 assertions (58 admission;147 physical), zero final failures/aborts. Native/public loaded
holds, partial loading/unloading, pause, saved twins, returned-fuel claims, courier refueling,
native detour/repair, protected removal/interruption and pending operation commands pass.
Each physical stage is bounded to 300 normal updates with game-time diagnostics on timeout.
Other suites compile only. No full/family/third runtime gate.

## 8. Risks

Funding is a current movement estimate, not reserved future fuel, traffic or guaranteed arrival.
Queue/handling/work/idle/service input costs are not included in movement duration. Per-read
routing is not a CPU/allocation-budget proof. Native dock refueling under congestion, alternative
cargo inventories, broader provider faults, dynamic area/topology combinations, replay and full
acceptance remain unproven. The refueling execution proof uses the public point-space provider;
the obstacle proof uses engine-owned authored spawn/removal, not player construction acceptance.

## 9. Out of scope

B11.3c timed service intervals; B11.4 broader fleet acceptance; Oil/Tycoon/hosts/demos/platforms/
Draw. No new game clock, route cache, inventory/claim owner, instant fuel or repair rule.

## 10. Stages

- [x] Share native remaining itinerary selection and pure private funding diagnostics.
- [x] Gate actual movement before inputs; retain durable job holds and handling/claims.
- [x] Prove native/public continuation, controlled rates/routes and actual physical refueling.
- [x] Direct metadata migration, scoped build/two-suite evidence, source inventory and tracker.
- [ ] Parent B11.3c actual service intervals and B11.4 broad acceptance remain required.
