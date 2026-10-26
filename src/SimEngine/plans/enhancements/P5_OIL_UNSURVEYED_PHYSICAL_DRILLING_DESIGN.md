# P5-A28 — Actual unsurveyed preparation and drilling acceptance

Current M149/native2.772/save161/replay2 requalifies the original complete well617/0 and
dry-hole629/0 scenarios through actual Oil public commands, original8/12/72/24-hour work,
real physical supplies, saved twins and outcome/history/privacy. No source or gameplay
change. [Actual build, two exact executions, preservation and original-scope limits](../../docs/migration/P5_OIL_UNSURVEYED_PHYSICAL_DRILLING_VERIFICATION.md#current-m149-actual-supplied-well-and-dry-hole-completion--2026-10-23).
This is current selected timed gameplay evidence, not completion of broader P5.005/.008/.009.
Older implementation paragraphs below retain their historical context; no duplicate workflow.

## 0. Problem statement

Native authored graphs/effects exist, but actual supplied/machine-executed preparation and blind well
completion have not been proven together through public commands. Seismic/logging must stay optional.

## 1. Goals

Found a resource-only company, construct a supplied pad, post drilling with no survey knowledge or
survey/logging workers, and complete through original8/12/72/24-hour stages. Verify real materials,
worker travel/work, intermediate saves and well-derived knowledge only after actual completion.

## 2. Design

Use canonical modules, actors, inventory, construction/activity graphs and effects, not projection.
A24x24 Sandbox fixture authors one guaranteed conventional field near the start, no extra fields,
normal volumes and a small crew including grader/hauler/drill rig/repair truck. Existing starting
materials plus spare parts are explicit scenario inputs. All authored work/travel/handling/rate
definitions remain unchanged. Hosts/test drive public placement/work/worker commands and Update only.
Bound each stalled workflow and the whole case with public diagnostic state; preserve live handles.

## 3. API surface

Existing PlanningView/ConstructionView/WorkView/ExplorationView, ConstructCommand/PostOrderCommand/
worker control, saves and snapshots. No new host gameplay or instant effect endpoint.

## 4. Files

New public drilling case/registration, affected engine/Oil sources only if an actual gap appears,
stage/evidence/master/audit. No Tycoon/platform/Draw work.

## 5. Errors

Stock, route, work and completion blockers must remain typed/public. Missing progress aborts with
tick/order/node/worker diagnostics, not a hang or resource grant. Old goal scope remains open.

## 6. Compatibility

No legacy/shim/fake completion or parallel graph/clock/stock/provider.

## 7. Verification

One bounded exact physical case at canonical durations and rates. Save/continue during actual stages.
FINAL610/0/0 in544882ms/observer545142ms, actual site6/pad7/well8 and minute7270 completion.
P5.acceptance.004 closes only; broader requirements remain open. See
[evidence](../../docs/migration/P5_OIL_UNSURVEYED_PHYSICAL_DRILLING_VERIFICATION.md).
Not dry-hole/offshore/full-size/rival/replay/lockstep/all-industrial acceptance from this single flow.

## 8. Risks

The canonical workflow runs more than four game days; each stage has a progress timeout.
Failure/repair and automatic hauling use native policies; do not bypass a stall to get green.

## 9. Out of scope

Modified work duration/rates, privileged state writes, full suites, platform/Draw and publication
before the whole goal/plan finish.

## 10. Execution order

1. Implement the public controlled-world player scenario and full physical staging assertions.
2. Compile and run only that case; inspect/fix authoritative gaps and retain all attempts.
3. Record exact proof boundaries and close only the directly proven unsurveyed-drilling criterion.
