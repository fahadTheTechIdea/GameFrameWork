# P9-B11 — Operating horizons, route funding and fleet trip policies

Status: B11.1 focused verified, 2026-10-07; the full trip-policy capability is not complete.
Build2.186.0/save101/work view64/motion view7; saved owner payloads remain unchanged.
Clean build;323 assertions/two exact suites (156 contracts;167 physical), zero failures/aborts.
[Evidence](../../docs/migration/P9_OPERATING_HORIZONS_VERIFICATION.md).
Depends on B9 actual motion estimates, B10 phase inputs and existing native inventory/rate owners.

Follow-on [B11.2a prospective travel](P9_PROSPECTIVE_TRAVEL_DESIGN.md) verifies the provider-owned
route-duration primitive. [B11.2b selected totals](P9_ITINERARY_DURATION_DESIGN.md) now carries
complete movement through existing dock/haul selectors at build2.188.0/save101/motion view8/
logistics view19.360 assertions/two exact itinerary suites; B11.1 suites compile only at this
checkpoint. B11.2a/b are focused verified; authored fleet admission remains incomplete.

## 0. Problem

OperatingInputsContracts admits only the next accepted interval. A currently payable route can
run out of fuel before arrival; no engine view exposes its remaining input requirement. Hosts
must not implement that calculation. Copying rates or rounding into a route/fleet planner would
duplicate resource authority. Whole-itinerary admission additionally needs actual provider-owned
prospective routes and authored fleet policy, which this stage must not pretend to have.

## 1. Goals

1. One phase-input calculator evaluates explicit nonnegative 64-bit exposure horizons using actual
   provider exposure, selected game time, authored rates, native quantization and unpromised stock.
2. Single-interval movement/idle/standby commits and multi-interval previews use that same owner.
3. Whole vector admission aggregates all selected phase providers before checking stock; overflow
   is typed and returns no incomplete bill, without resource reservation, burn or time advancement.
4. Existing private arrival views attach funding facts for the actual known remaining route.
   Unknown routes have no fabricated budget. Current ETA remains conditional, not a fuel promise.
5. Native/public providers, cumulative credit, funded/unfunded route continuation and saved twins
   demonstrate parity; hosts only read engine-calculated facts.
6. Later stages add actual prospective itinerary contracts and authored fleet reserve/admission
   policy on existing job/logistics owners, with no parallel route/claim/store or default policy.

## 2. Design

B11.1 extends OperatingInputEvaluation with explicit Intervals and RequiredInputs. Remove its
single-interval NextInputs name directly; UtilityDemandSnapshot.NextInputs keeps its distinct
one-next-update meaning. OperatingInputsContracts.Read takes an explicit horizon, defaulting to
one normal interval for existing execution callers. The same selected provider snapshot,
ResourceRates.Between and inventory transaction evaluation serve every horizon. No new rounding
counter, resource ledger or predictor math. Negative author intent refuses; overflow is Range.

Expose the horizon through existing WorkView and MotionView. MotionArrivalView.OperatingBudget
is present only when actual motion produced a known remaining duration. It reads current
unpromised stock, not future resupply or reserved fleet capacity. A known but underfunded current
route has its budget's typed shortage; actual next-update movement readiness remains unchanged.
An idle/unreachable/unplanned/unsupported route has no substitute distance, cost or budget.

B11.2 needs prospective itinerary duration supplied by each actual coordinate/route owner using
the same physical travel-budget formula, with explicit unknowns for unsupported providers.
B11.3 adds explicit JSON fleet reserve/service policy and fresh assignment/replanning admission
on actual native jobs. No silent fallback route, teleport, free fuel or arbitrary size limit.

## 3. API

OperatingInputEvaluation.Intervals/RequiredInputs; OperatingInputsContracts.Read(..., intervals).
Existing WorkView.ReadOperatingInputs gains horizon selection; MotionView exposes the same
movement evaluation. MotionArrivalView.OperatingBudget uses the same immutable evaluation.
IOperatingInputs/Definition, IResourceRate and transaction contracts remain authoritative.
Existing master-listed phase/rate JSON is reused; no new forecast content loader or state model.

## 4. Files

Standard/OperatingInputs.cs, MotionArrival.cs, Views/MotionView.cs and WorkView.cs, relevant
view identities, actual callers/proof fixtures, same two existing operating-input suite callbacks.
Stage/evidence/master and current metadata; no host/demo/platform changes.

## 5. Errors

Invalid horizon/phase/target refuses explicitly. Provider metadata/exposure/utility mismatches
retain existing observation/contract faults. Checked horizon/rate/combined-bill overflow returns
Range and no partial RequiredInputs. Shortage returns Inputs using native available inventory.
No reservation or commit occurs on preview; physical movement still uses one-interval admission.

## 6. Compatibility

Direct current API/identity revision; delete old evaluation member name rather than retain alias.
No old reader, compatibility shim, second calculator, current-time owner or fabricated policy.

## 7. Verification

Build and only:
- Operating inputs: phases, public contracts and atomic resource admission
- Operating inputs: physical work, standby resupply and saved continuation

Add zero/negative/wide/overflow horizons, cumulative quantization credit, native/public overlapping
rates, phase selection, immutable/private views, native availability, actual remaining-route
budget, real physical expenditure matching the prior horizon and saved twins. Existing callbacks
retain physical resupply/standby/provider-fault coverage; all other suites compile only.

## 8. Risks

Remaining-route duration is conditional on current geometry/rate and may differ by fixed-point
rounding. Funding is current stock only, not guaranteed arrival or future service/resupply.
Do not treat horizon exposure as another game clock or save it as a current-time authority.
Performance/global-provider purity and physical itinerary acceptance remain open.

## 9. Out of scope

Draw/platforms/Oil/Tycoon/host/demo logic. Prospective itinerary and fleet policy completion in
B11.2/B11.3; global sandbox/performance/replay/soak/full framework acceptance.

## 10. Stages

- [x] B11.1 Shared operating horizon/remaining-route budget and focused physical/save proof.
- [x] B11.2 [Prospective travel](P9_PROSPECTIVE_TRAVEL_DESIGN.md): provider duration primitive
  and [complete selector-owned dock/haul totals](P9_ITINERARY_DURATION_DESIGN.md) focused verified.
- [ ] B11.3 [Authored trip/service policy](P9_HAUL_TRIP_POLICY_DESIGN.md): B11.3a fresh funding
  and [B11.3b retained funding](P9_RETAINED_HAUL_TRIP_DESIGN.md) focused verified;
  actual timed service intervals remain required.
- [ ] B11.4 Broader congestion/stranding/provider/replay/performance/full framework acceptance.

