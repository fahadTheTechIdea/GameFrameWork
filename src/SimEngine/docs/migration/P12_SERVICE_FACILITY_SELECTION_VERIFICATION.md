# P12-D6c — Recurring service facility selection evidence

Date: 2026-10-06. Scope: Beep.SimEngine only.
[Design](../../plans/enhancements/P12_SERVICE_FACILITY_SELECTION_DESIGN.md).

## Source protocol and ownership

Build beep-simengine-2.157.0; save84; WorkBoard27; WorkSystem19; private WorkView48;
native recurring source codec5; public proof recurring codec3 unchanged.
The unchanged board payload still owns plan intent/origin, orders and reservations. Worker/home,
condition, stock, dock and clock layouts are unchanged. No facility state cache or selection codec.

IActivityFacilitySelection aliases the existing WorkBoard. It enumerates only live owned actual
IWorkDock capabilities, delegates ordinary plan admission and IWorkerDispatch.CopyCandidates, and
filters the offers to the source's selected worker. An explicitly authored positive machine probe
node determines present resource/tool/role/area/route/bay readiness. This is not a guarantee that
future DAG nodes are already supplied. Existing admission and acquisition recheck before spending.

IActivityFacilityPolicy ranks detached safe facts. Native authored nearest/home-first and an unrelated
public policy use the same engine ranking: preference, actual travel cost, stable target identity.
Null explicitly excludes; negative ranks fault. Unsafe candidates are never sent to ranking. Private
immutable views publish actual blockers, not invented zero costs or privileged foreign stock facts.
No eligible facility returns work.recurring-no-facility without posting work or advancing cadence.

IEntityTargetPlanDefinition owns target argument authoring. ActivityWorkerSelection owns one shared
canonical writer; RecurringPlanBinding's duplicate writer is deleted. Strict master-listed JSON adds
activity_facility_policies and service_facility; every native source explicitly supplies facility_policy
(null for self/home). Binding validates the actual catalog profile and positive selected probe node.
Normal existing source requests, plan admission/origin, physical jobs, ports and conditions execute
service. Observation never retargets existing work, acquires stock or repairs a machine.

## Final focused gate

Working directory: C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine.

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Service facilities: authored policies, private candidates and deterministic selection"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Service facilities: physical repair, replanning and saved continuation"
```

Final build: zero warnings/errors; MSBuild3.59s (4.11s command). Contracts101 passed/0 failed/458ms.
Physical65 passed/0 failed/549ms. Total166 checks/two exact suites/zero aborted suites.
Only these exact suites ran; same-name repair reruns were used. No third/family/full runtime gate.
Runtime source/test/content unchanged after this final gate; subsequent edits are documentation only.

Contracts prove strict missing/null/unknown/wrong-kind/wrong-target profile and node authoring,
actual board service alias, deterministic nearest/home-first/tie/public rankings, immutable captured
facts/intent, private foreign refusal, wrong-session source rejection and saved query identity.
Actual stock, roles, operating areas, closed routes, occupied bays/waits and disabled selected workers
refuse candidates through existing owners. The exercised impure public dock and negative public rank
fault; explicit public exclusion retains its typed blocker. Queries leave hash, jobs and stock unchanged.

Native/public workers and native/public condition owners earn actual travel, three consumed spares,
held tools, timed progress, bay departure and worker repair. Active home edits keep the selected
facility; healthy machines do not repost. Cancellation does not repair or refund spent spares.
A facility with remaining stock correctly refuses deletion (sim.resource.undisposed-stock). The
scenario physically hauls its two remaining tools out through existing IHaulExecution before deletion;
the next source posting chooses the remaining supplied facility. No deletion guard was weakened,
stock drained, cargo fabricated or worker teleported. Interrupted work/departure/cancel/replan/ended
saves, saved twins, session isolation and sole-clock pause agree. Physical waits are bounded300
updates with stage/node/progress/blocker diagnostics.

An earlier physical run aborted on stocked deletion; the fixture was corrected to exercise real
stock disposition. This is not a claim that the first iteration passed. 71 older test files mechanically
migrate67 build/64 save/3 work-schema assertion lines; those suites compile only. D6b and all older
runtime evidence remain historical and are not added to166.

## Remaining scope

Exactly one GameClock is unchanged. Its read-only update sequence is not another time owner. No
new scheduler, queue, clock, service history, compatibility loader or target argument fallback.
No OilFieldDays/Tycoon/host/demo/platform/Draw source changes. Public proof compiles without internals.

This is a focused slice, not completion of P9/P12 or the full goal. Mutable facility preference controls,
standby/preparedness/fuel/traffic, global optimization/congestion/deadlines, inspection science,
incident breadth, whole-world callback purity/rollback, wider provider/target-factory/protocol matrices,
replay/fuzz/soak/stress/performance and full framework gates remain open. Current query is scoped to
one ready probe node and is not a claim/arrival prediction. No allocation budget has been demonstrated.

## SHA256 of verified source

| File | SHA256 |
|---|---|
| Standard/ActivityFacilities.cs | C096D6DA3C53707CC988C3E56CDEC9E9993C5B4BB08E5320DECB41A4FD23C1F2 |
| Standard/WorkFacilities.cs | 80DD6980FB34A6116546C5026B6D4000E69B2D528D66ECF63F9927FF42A26CDB |
| Standard/RecurringPlans.cs | 28308945ECF345127633B58129C419FE2AA9481EF315C5C1AE9026E8D2BF0BE4 |
| tests/FacilitySelectionTests.cs | 925DF2A9409CD22FC7F198A0D811CC6DF1A517D115180F5CB271FBC2B8300E3F |
| proof/FacilityPolicyProof.cs | 0C689C3E7268D84212625AB8C444B1E15448129225E1B1013D620982354F0B97 |
| content/service-facilities.json | CEE94F45A275EE018D95716C88626C2858E423180F2326012BD33569506BD64E |
| Hosting/GameClock.cs (unchanged) | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |
