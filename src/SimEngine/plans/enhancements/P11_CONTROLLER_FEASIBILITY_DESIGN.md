# P11-C10: controller feasibility and shared observations — Design Document

## 0. Problem Statement

P11.034 remains unproven on the current engine: recovery must wait with actual
typed affordability/route blockers and bounded decisions, never free resources
or instant work. PlanRecoveryContracts.Read and PlanSiteContracts.Read still use
finally verification, as do PlanPlaybookDef.ValidateBinding's provider calls;
these can replace an original callback failure.

## 1. Goals

1. Existing site/recovery/binding callbacks use the sole shared observation owner
   and preserve first failures. No new state, clock or strategy service.
2. Current public-input proof exercises ordinary rival cancellation/alternate
   planning, real closed-route waits and optional-budget waits/funding over two
   clock configurations with native/independent policies, saves and replay.
3. Authored one-command cadence is proven against multiple simultaneously ready
   steps; a missing resource/money route never fabricates an extra action.

## 2. Design

Use EntityObservation.ReadProvider for the four original callback paths in
PlanRecoveryPolicies.cs, PlanSites.cs and PlanPlaybooks.cs. Keep original
identity/private checks, policies and cadence. Observe actual provider scopes,
not unfinished whole-service Sync or a parallel ledger.

The new fixture uses existing ConstructionPlanTests.Points master/JSON authoring
with PlayerControllerProofModule and ordinary point-space commands. Physical
content and optional economics are explicit separate master-listed JSON files.
Business is absent in resource-only scenarios. Enabled-budget scenarios use the
existing sole Tycoon accounts/valuation/primary-generation mechanism, no buyer,
purchase, trading, protected catalog changes or invented income fallback.

Existing finite stocks, staffed construction, hauling, route access and timed
generation execute through their current engine owners. Drivers are player
inputs/public assertions only. If current evidence does not qualify all P11.034
terms, keep its original checkbox open and document the missing scope.

## 3. API surface

No new public API or serialized state. Build2.452.0/save154/replay1; controller
codec5/service1/view7 and native economic contracts unchanged.

## 4. Files to change

|Action|File|Risk|
|---|---|---|
|Modify|src/Beep.SimEngine/Standard/PlanRecoveryPolicies.cs|medium|
|Modify|src/Beep.SimEngine/Standard/PlanSites.cs|medium|
|Modify|src/Beep.SimEngine/Standard/PlanPlaybooks.cs|medium|
|Modify|Existing build/protocol fixture/runner|low|
|New|tests/ExtensionProof/Content/controller-feasibility.json|low|
|New|tests/ExtensionProof/Content/controller-feasibility-economics.json|low|
|Modify|Explicit proof embedded-resource inventory|low|
|New|tests/Beep.SimEngine.Tests/ControllerFeasibilityTests.cs|medium|
|New|docs/migration/P11_CONTROLLER_FEASIBILITY_VERIFICATION.md|low|
|Modify|Master/checkpoints/audit and only fully qualified original gate|low|

## 5. Error handling matrix

|Actual condition|Expected result|
|---|---|
|Original pure callback failure|First exception latched, no finally masking|
|Closed route|Typed engine wait, no progress/material/payment or duplicate plan|
|Money disabled|No account/income dependency; same timed physical work|
|Optional insufficient budget|Typed native wait/real holds, no debt/grant fallback|
|Actual timed resource generation|Native authored value settles once, then physical work|
|Multiple ready steps|Original cadence and command budget limit native decisions|

## 6. Backward compatibility

Development only; no legacy or compatibility branch.

## 7. Verification

ONE NEW bounded exact case, same-case repairs only. One solution compile attempt,
then only necessary targeted repairs; confirmed terminal compile before runtime.
60000ms/termination5000ms/output30000chars; bounded stages512Updates with last12
actual private facts. Current executable byte freeze after FINAL, no old/family/
full runtime. Save/load/replay/interleaved sessions through public engine API.

## 8. Risks

Point geometry does not permit machine crossing; depots are authored at0/70,
actual sites30/42. Tests wait for declared cadence, not assumed one-step answers.
Do not infer hostile CLR purity, every recovery strategy or universal deadlock
freedom from a focused case. Protected G19 files/clock remain unchanged.

## 9. Out of scope

Oil/platform/Draw/adapter work, advanced Tycoon/purchases/trading, whole-provider/
knowledge/pressure/stress/full-profile gates and publication before full goal/plan.

## 10. Sub-task execution order

1. Inspect original callbacks, typed work/plan blockers and authored economic gates.
2. Migrate only original observation paths and build identity.
3. Author master-listed physical/optional files and one combined public-player case.
4. Compile/run exact, preserve failures, then update tracker and integrity evidence.

Status: scoped implementation and focused verification complete. FIRST/FINAL
4997/0/0 in33452ms, one clean21.86s solution compile and one exact runtime.
Only original P11.034 checked; unchanged literal/line1598. Original221 gates:
184checked/37open; additional178. Full goal/plan active, no staging/commit/push.
[Verification](../../docs/migration/P11_CONTROLLER_FEASIBILITY_VERIFICATION.md).

### Verified scope

Eight native/independent policy × minute/half × money-off/on recovery flows:
actual closed-route blocker.unreachable with zero earned work/spend and same
active intent; actual blocker.cash appears on build and controller, saved
insufficient-money wait, then timed20-output generation values100 once, physical
8-material/80-cost construction yields stock32/cash20/no holds. Resource-only
variants compose no business and consume20→12. Saved twins, pause, retained
private facts, interleaved session isolation and public command replay agree.
Two quota flows use three ready authored steps and one-command contexts; native
decision delta0/1/actual inbox limits monitored every ordinary Update. One real
recovery Observe throw proves first-fault cutoff; remaining binding/site/hostile
CLR purity breadth is source-only. No executable edit after FINAL or old/full
runtime; protected clock/G19 untouched. Metadata closure follows evidence.

### Metadata closure

1518 source anchors/790 selected executable inputs/13 quickstart anchors match
current bytes; zero post-FINAL drift.964 Markdown documents/8584 local links
valid,18 master rows and all221 literal gates validate. Only P11.034 checkbox
changed, no original text/document/line changes. Five existing selected inputs
changed,20 added (17 existing/three new).14 existing source files admitted plus
five new driver/JSON/stage/evidence files. Economic sources unedited this slice;
shared callback owners/build identity only. Freeze2026-10-11 18:59:04 UTC;
diff --check0/34 existing line-ending warnings, dirty2728/staged0. Four protected
clock/G19 hashes unchanged. Full P1-P12/37 original gates/publication remain OPEN.
