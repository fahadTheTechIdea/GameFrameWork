# P12-D11 — Reserved worker self-service

Status: focused verified, 2026-10-06. Scope: engine only; extends P9/P12, not a new fleet scheduler.

Build2.163.0/save90/work24/work view51/native policy4/public policy2. One unchanged GameClock.
Clean build;509 checks/two exact suites (258 contracts;251 physical), zero final failures/aborts.
[Evidence](../../docs/migration/P12_RESERVED_SELF_SERVICE_VERIFICATION.md). Wider P9/P12 remain partial.

## 0. Problem

Emergency-only standby and company ready-worker floors prevent a reserved machine from maintaining
itself through an ordinary service DAG. Enabling all ordinary work exposes it to unrelated tasks.
The engine already owns recurring source bindings, immutable plan origins and exact selected workers.

## 1. Goals

1. Explicit work-policy `emergency_and_self_service` allows genuine recurring self-service only.
2. Recurring binding JSON requires `reserve_use`: `none` or `self_service`; self-service requires
   positive machine nodes explicitly bound to the actual recurring source worker.
3. Both prospective facility readiness and actual dispatch use the same engine-derived provenance.
4. Manual plans, arbitrary high priority, ordinary supply hauling and another worker cannot borrow it.
5. Existing company readiness remains honest: busy maintenance is not counted ready. Explicit opted-in
   self-service may use the worker despite its floor; unrelated work remains refused.
6. Existing service resources, tools, docks, travel, repair, cancellation and saved continuation apply.
7. One WorkBoard, one policy/settings codec and one unchanged GameClock remain authoritative.

## 2. Design

Extend WorkAssignmentMode, strict reader, shared codec and entity standby rule. No extra settings
table or maintenance balance. Add ReserveUse to RecurringPlanBinding and validate it at composition.
All existing bindings explicitly migrate to none, without a compatibility default.

WorkBoard derives actual exemption from its active plan origin, actual recurring capability,
definition/binding/graph identity and exact node worker selection. Prospectively, the same checker
uses an actual recurring source and its authored node during facility selection. That preview grants
no mutation authority: actual assignment checks the admitted plan origin again.

Extend existing CopyCandidates with an explicit nullable recurring source context, not a caller-owned
privilege flag. WorkSystem's sole assignment calculation derives the exemption and shares it with
individual standby and company admission. Existing roles, enabled state, areas, execution, dock and
resource rules continue to apply. Maintenance stays ordinary, never emergency-classified.

## 3. API

WorkAssignmentMode.EmergencyAndSelfService; required RecurringPlanBinding ReserveUse constructor
argument and strict reserve_use JSON; explicit source context on IWorkerDispatch.CopyCandidates.
Existing work-policy commands/views persist the selected mode through their shared codec.

## 4. Files

Modify WorkPolicies, RecurringPlans, WorkRecurringPlans, WorkContracts, Work, WorkFacilities,
WorkModule, WorkView, SaveGame, public registration/fixtures and exact callers. Add two focused suites
and verification evidence; update authoritative tracker/parents/README/help.

## 5. Errors

Missing/unknown/mistyped reserve_use fails composition. Empty/timer node bindings reject self-service.
Missing or unrelated origins/sources/workers receive ordinary standby/floor refusal. Invalid enum
rejects construction/decode. Missing stock/tool/route/bay/role remains its real blocker, not exemption.

## 6. Compatibility

Development-only breaking replacement; no aliases, old overload, missing-field default or new loader.

## 7. Verification

Clean build and exactly:

- `Reserved self-service: authored provenance, private controls and safe admission`
- `Reserved self-service: physical maintenance, reserve floors and saved continuation`

Native/public policy and worker cases, strict JSON, settings/private controls, manual refusal,
prospective facilities, actual timed service/spares/tools, readiness while busy, cancellation/repost,
policy edits, partial save/twins/pause/session isolation. Bounded waits with engine diagnostics.
Older gates compile only, not runtime proof; no third/family/full run.

## 8. Risks

Authored self-service is intentional game content, not scientific certification that every node repairs
condition. Providers may author other readiness workflows; the engine enforces actual self ownership,
positive worker binding and safety. Privileged previews must never authorize unrelated actual jobs.

## 9. Non-goals

Exempting arbitrary repair crews or supply hauling; target/distinct-role preparedness, fuel/traffic,
science/contamination, wider provider purity/replay/stress/full framework. No host or rendering logic.

## 10. Tasks

- [x] D11.1 — Replace authored binding contract and extend the existing work mode/codec.
- [x] D11.2 — Share truthful self-service provenance across candidate preview and actual assignment.
- [x] D11.3 — Build and prove two exact suites with physical service and interruption.
- [x] D11.4 — Record scoped evidence/tracker and remaining full requirements.

Eight native/public policy × worker × recurring-source combinations are covered. Same-update standby
and cancellation prevent fresh posting; a genuine incumbent may finish after removing the opt-in.
This is self-service, not general repair-crew/supply/material exemption. Older suites compile only;
D10 and earlier runtime evidence was not rerun. Full framework acceptance remains open.
