# P9-RD1 — Rank departure candidates before physical route evaluation

## 0. Problem and specification

The current native2.664 active-hauling stack passes through
WorkBoard.ReplanDockBindings -> SelectRemainingJourney -> DockJourney.Finish ->
PlanRoute -> guarded structural tile search. Finish visits exits by stable ID,
evaluating an expensive early-ID exit before a cheaper late-ID exit. Existing
domination pruning helps only after a feasible incumbent has been evaluated.
The other captured stack is save/reload visibility, not Update routing; neither
sample is a statistical CPU profile or a speedup result.

## 1. Goals

1. Rank eligible departure exits by the existing Cheaper comparator, using their
   original guarded policy/cost reads in original ID order.
2. Route only until the first feasible ranked exit. Preserve full selected route,
   continuation, operating-area, fuel, claims and elapsed travel checks.
3. If every exit refuses, retain Pending precedence and the last refusal in
   ORIGINAL ID order, not the new evaluation order.
4. Prove an expensive early-ID route is not invoked, blocked cheaper exits still
   fall back, saved sessions agree, and cost callback mutation still fail-stops.

## 2. Design

Change only WorkBoard.DockJourney.Finish's exit loop. Collect local tuples of
slot, guarded estimate and original index after the existing policy/cost checks.
Sort using the sole existing Cheaper comparator. Each needed candidate still
calls original PlanRoute and continuation. First feasible rank is definitive.
On total refusal track greatest original index and sticky Pending. No persistent
route/estimate cache, new coordinator, navigation algorithm or copied geometry.

## 3. API surface

No new public contract or codec. Engine build becomes2.665.0; save160/replay1 and
component schemas unchanged. Earlier native saves remain preserved historical
artifacts and are NOT header-rewritten or loaded through compatibility paths.

## 4. Files

Modify Standard/WorkDocks.cs and Kernel/SaveGame.cs. Extend the existing
DockTravelSelectionTests owner, independent PointSpaceProof refusal witnesses,
PointDockProof cost-mutation witness, and current-engine-protocol.json. Update
this stage, verification evidence, master tracker and AGENTS/README.

## 5. Errors

Guarded cost callbacks still run for every eligible exit before success is
published. Selected or necessary fallback route errors retain original guards.
No result/arrival/travel publishes when all candidates refuse. Reordered failure
evaluation must not change original typed refusal precedence.

## 6. Compatibility

Development only; no legacy or compatibility overload. No saved state change.

## 7. Verification

One new literal exact case before/after; separate original physical dock-travel
case and existing departure-pruning case after implementation. Bounded child
runner, no family/profile/full suite. Record every attempt, build and identity.
No timing claim from poison-route witnesses; native performance remains open.

## 8. Risks

Callbacks are contractually pure; call-local ranking is not permission to skip
eligibility checks. Transient list allocation may offset saved searches on tiny
maps. Independent point proof does not certify canonical harbour performance.

## 9. Out of scope

Main service-slot ranking, guard/hash changes, Oil/gameplay JSON or business,
platforms/Draw, work-duration changes, full acceptance and Git publication.

## 10. Execution order

1. [x] Reproduce unnecessary early-ID expensive route invocation (actual baseline0/1).
2. [x] Rank exits in the existing journey owner, retaining original refusals.
3. [x] Run focused proofs and record current evidence and remaining gates.

[Every attempt, current native artifact and limits](../../docs/migration/P9_RANKED_DEPARTURE_JOURNEYS_VERIFICATION.md).
Three exact framework finals461/0; canonical one day94 checks across four
explicit same-case native invocations, all passing. Harbour remains partial;
active Update657ms is not a performance victory. Original broad gates OPEN.
