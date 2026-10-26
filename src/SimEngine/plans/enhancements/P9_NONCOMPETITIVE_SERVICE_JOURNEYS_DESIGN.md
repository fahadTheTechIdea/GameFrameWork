# P9-SJ1 — Noncompetitive dock candidate journeys

## 0. Problem and specification

WorkBoard.SelectDock.Check performs a complete DockJourney before the outer loop
uses Cheaper to discard a noncompetitive candidate. P9-DJ1 corrected this pattern
inside departure journeys; the main slot selector still repeats unnecessary
route and continuation callbacks while canonical construction exceeds its bound.

## 1. Goals

1. Preserve the exact feasible selected slot, cost, arrival, duration and stable tie.
2. Preserve all guarded policy/claim/queue/vacancy/cost/reach admission. Skip only
   a losing complete journey after a genuinely feasible incumbent exists.
3. Keep retained-slot preference/full validation, necessary cheaper alternatives,
   unavailable-cheap fallback, and no-feasible-result typed Pending/refusal ordering.
4. No new scheduler, cache, reservation owner, shorter work or changed bounds.

## 2. Design

The existing SelectDock.Check helper becomes eligibility-only. The retained-slot
branch invokes the same DockJourney after Check succeeds, as before. The candidate
loop invokes Check, then the original Cheaper comparator against an already
feasible result, and only then DockJourney for a potentially winning candidate.
No eligible route is treated as feasible based only on its estimated cost.
Successful choices still use actual engine route/area/continuation checks.

## 3. API surface

No public API, state/codec/save layout. Build2.646/save160/replay1; no old-header
migration. Existing test-only PointMotion/PointDock probes witness losing routes
and required guarded cost checks; original master-authored fixture/loader reused.

## 4. Files

Standard/WorkDocks.cs, Kernel/SaveGame.cs, current protocol fixture; existing
DockTravelSelectionTests, PointSpaceProof and PointDockProof. Stage evidence,
master/P3/P5/AGENTS/README updated without business/platform/Draw edits.

## 5. Errors

Mutation in even a losing candidate's required cost callback still faults. A
blocked cheap candidate cannot prune later feasible fallback. No incumbent means
no pruning; Pending precedence and empty result stay original. Retained claims
are not displaced merely because a new cheaper slot exists.

## 6. Compatibility

Development only. No legacy selector, fallback, second model or save conversion.

## 7. Verification

One new exact case before/after: losing far/equal-cost service journeys, blocked
cheap fallback, all-blocked/Pending, and losing-cost mutation. Reverse authored
slot order, actual native save/load, purity and isolated sessions. Existing exact
physical dock choice/replay and retained-claim replanning checks verify risk paths.
Then unchanged bounded canonical preparation/continuation if needed. No full/
profile/family run, increased limits, reduced geometry or altered authored work.

## 8. Risks

The candidate's ranking cost is the existing authoritative cost, not a new route
heuristic. Retained slots must still validate a full journey before any candidate
selection. Local pruning does not prove all route performance or traffic liveness.

## 9. Out of scope

New traffic systems, positive-result caching, callback exemptions, world/time/
stock changes, business/platform/Draw, full regression or Git publication.

## 10. Execution order

1. [x] Reproduce unnecessary losing service-route evaluation in the existing proof.
2. [x] Separate eligibility and potentially winning journey inside the sole selector.
3. [x] Verify exact query/physical/retained/canonical scopes and record all limits.
   Three focused finals460/0; actual three-facility harbour completes at8051 through
   native staged continuation. Initial20/1 wall-limit failure remains, broader
   marine/full goal and original gates stay open.

[Current evidence and canonical boundary](../../docs/migration/P9_NONCOMPETITIVE_SERVICE_JOURNEYS_VERIFICATION.md).
