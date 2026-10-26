# P3-EG1 — Proven impassable exact-goal admission

## 0. Problem and specification

Native PathSearch.Find can exhaust a frontier while seeking one exact tile whose
guarded entry cost is zero. Added authored dock alternatives expose this unnecessary
work for water/blocked service and departure cells. This is source-identified; the
complete docking run's cost is not statistically attributed to this one cause.

## 1. Goals

1. Prove a valid exact goal with zero entry cost unreachable before frontier expansion.
2. Preserve source validation, zero-budget precedence, ordinary A*, path clearing,
   multi-cell goals, all guarded callback/fault behavior and sparse world dimensions.
3. No altered topology, work duration, timeouts, positive-result cache or second router.

## 2. Design

After original source/zero-budget checks, PathSearch.Find uses its EXISTING per-search
EntryCost for an in-bounds1x1 goal. Zero rejects immediately; a positive read remains
in the same query-local cost cache. Larger goal regions are unchanged, never scanned.
Actual native NavGrid callbacks keep original provider/space/clock observations.

## 3. API surface

No new API/state/codec. Engine2.643/save160/replay1; current protocol fixture updated.
Definite refusal may replace BudgetExceeded when proof needs no expansion. Zero-budget
requests still retain their original precedence. No old-build checkpoint migration.

## 4. Files

Standard/Navigation.cs, Kernel/SaveGame.cs, current-engine-protocol.json; dedicated
focused tests reuse original SpatialLayerTests.Factory and native NavGrid/PathSearch.
Register with existing spatial-layer test owner. Staged evidence/master/P3/P5/AGENTS/README.

## 5. Errors

Blocked source still refuses first; zero budget remains pending before goal callback.
Invalid goal cost/mutation/throw still faults through the same guarded entry boundary.
Empty/blocked paths never publish an arrival or a fabricated successful prefix.

## 6. Compatibility

Development only. No compatibility reader or alternate router/cache.

## 7. Verification

Run one new exact case before/after change, then one affected authored layered physical
case and bounded canonical docked preparation/continuation. No full/profile/family run.

## 8. Risks

Other dock matching/route observation work may remain expensive. Do not treat this
local proof as complete canonical harbour, arbitrary traffic yielding or broad regression.

## 9. Out of scope

Changed budgets/maps/durations/materials, callback exemptions, broad suites or publication.

## 10. Execution order

1. [x] Reproduce native guarded exact-goal expansion in the focused case.
2. [x] Add the proven refusal to the sole original search owner.
3. [x] Verify exact/physical/canonical scopes and record all limitations. Canonical
   harbour remains partial; no original broad acceptance gate closes.

[Evidence and limits](../../docs/migration/P3_EXACT_GOAL_ADMISSION_VERIFICATION.md).
