# P9-B11.3c2b2 — Authored service windows for fresh hauling

Status: B11.3c2b2 focused verified, 2026-10-07. Wider fleet/trip acceptance and roadmap remain open.
Build2.194.0/save104/board36/work35/logistics12/work view68/logistics view23/native recurring8.
[Verification](../../docs/migration/P9_SERVICE_MOVEMENT_WINDOW_VERIFICATION.md).

## 0. Problem

Service-first prevents fresh work once a plan exists, but an idle machine can start a selected
movement longer than its remaining scheduled service interval. Trip admission must read the same
real service chronology, not copy deadlines or predict unavailable queue/handling time.

## 1. Goals

1. Haul trip profiles explicitly declare service_sources as named references or an empty array.
2. Each reference resolves to this machine's actual worker-bound scheduled recurring provider.
3. Recurring views and prospective movement use one canonical cadence calculation.
4. Fresh hauling/return admission compares selected movement against the sole GameClock and
   actual due time; it never invents service success, condition forecasts or resource promises.
5. Existing loaded jobs continue and retain all fuel/safety/stock/area gates; eligible emergency
   admission bypasses only the prospective service-window restriction.
6. Private previews, actual alternative-carrier selection, commands and saves use existing owners.

## 2. Design

Extend IHaulTripProfile with immutable ServiceSources. Existing HaulTripSettings stores its selected
profile only; no new saved field or command family. Explicit JSON arrays are validated for missing,
duplicate and incompatible references in existing composition/configuration admission. Empty means
no service-window requirement, not a default. A reference cannot bind a template, unbound plan,
foreign source or condition-only source lacking a scheduled service promise.

Factor RecurringPlanContracts.Read's actual board/source chronology into one private calculator
shared with public CheckMovementWindow. It reads real successful native cycle history and keeps
outstanding/pending/cancel retry/overflow semantics. It never calls worker/plan admission. There is
no copied deadline, cadence counter, service ledger or second game clock. Disabled future schedules
impose no future window; existing service-first admitted-plan rules remain independent.

HaulTripPolicyContracts.CheckDispatch checks the selected known movement against the selected
profile's actual sources. Existing LogisticsService fresh haul/return selector calls it before
claims/interruption. Retained fuel-budget reads do not call this fresh-admission rule; they continue
the same cargo/job instead of deadlocking behind service that needs their selected worker.

## 3. API

IHaulTripProfile.ServiceSources; RecurringPlanContracts.CheckMovementWindow; existing trip contract
CheckDispatch and a typed recurring movement-window refusal. No host/game platform API or new command.
LogisticsView.ReadHaulTripServiceSources returns detached immutable private references from the actual
selected profile, sharing existing policy ownership admission.

## 4. Files

Standard/HaulTripPolicies.cs, RecurringPlans.cs, Logistics.cs and LogisticsModule.cs; Views/LogisticsView.cs; explicit
master-listed profile JSON; focused CompletionServiceTests; current build metadata and evidence.

## 5. Errors

Missing array/duplicate/missing/incompatible source fails before gameplay or rejects configuration.
Unknown selected movement retains its typed refusal. Pending/unacknowledged service cannot pretend
to have a known completion deadline. Overflow never narrows or substitutes zero time. Normal
physical/safety/emergency eligibility remains authoritative.

## 6. Compatibility

Direct content/contract/build migration. Existing owner payloads unchanged; no legacy/default reader.

## 7. Verification

Build plus only the two exact completion-service suites. Cover native/public source bindings,
invalid/missing arrays, private purity, equal/short/long windows, disable/reconfigure/actual completion,
ordinary alternative selection, real loaded continuation across due time and saved twins.
Separate suites compile only; no full/family/third gate.

## 8. Risks

Movement duration excludes uncertain handling/congestion/queue delay. This is not guaranteed arrival,
future fuel reservation or forecast condition wear. Broader fleet/queue/failure/provider/performance/
replay/full acceptance remains open.

## 9. Out of scope

Oil/Tycoon/hosts/demos/platforms/Draw; independent clocks, duplicate routing or service schedulers.

## 10. Stages

- [x] Explicit profile references and canonical chronology/window admission.
- [x] Focused private/native/public/physical/saved continuation proof:484 assertions in two exact
  service suites (248 admission;236 physical), zero failures/aborts. Other suites compile only.
- [x] Current clean build, scoped evidence and master tracker. Existing owner payloads unchanged;
  one unchanged GameClock, no deadline copy/service ledger/new scheduler/compatibility reader.
- [ ] Wider B11.3c2b/B11.4 fleet/queue/provider/performance/replay/full acceptance.
