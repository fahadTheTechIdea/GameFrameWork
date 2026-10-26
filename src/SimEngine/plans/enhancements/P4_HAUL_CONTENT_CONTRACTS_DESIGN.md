# P4-G12 — Open hauling load and trip profile content

Status: scoped verified75 checks; full P1–P12 remains open. [Evidence](../../docs/migration/P4_HAUL_CONTENT_CONTRACTS_VERIFICATION.md).

## Problem and goals

Before this slice, IHaulLoadProfile and IHaulTripProfile existed, but native carrier definitions, command/settings admission, restoration, LogisticsService composition and LogisticsView still required native profile bases. Independently authored loading and journey policies must use actual owned content contracts.

1. Extend both existing profile contracts with IContentDefinition; migrate every actual catalog consumer.
2. Extract the original load JSON reader/resource resolver/decision and trip JSON reader/service metadata/decision into sole reusable declarations. No duplicated policy interpreter.
3. Validate real owned aliases, native cargo capacity/resources/time and compatible scheduled service references. Guard profile decisions and service-source observations.
4. Preserve current load/trip component schemas, demand age, native fuel exposure, stocks, receipts, trip holds, work/job progress, movement and sole GameClock.
5. Prove master-listed direct profiles through ordinary player route/configuration commands, minimum-load wait, fuel-reserve refusal, physical cargo/hold/resume/delivery, immutable private budgets, saved twins/replay and invalid content before Initialize.

## Design and API

- Standard/HaulLoadPolicies.cs: IHaulLoadProfile content identity; HaulLoadProfileDeclaration.Read/Resolve owns original native rule; component/catalog/decision lookup uses actual contract.
- Standard/HaulTripPolicies.cs: IHaulTripProfile content identity; HaulTripProfileDeclaration.Read owns original native rule and service list; all binding/settings/saved/native budget/window paths resolve actual contract.
- Standard/Logistics.cs and Views/LogisticsView.cs: real owned contract composition/private metadata, no added state owner.
- Existing external carrier fixture definitions use contract references, not concrete native types.
- Public-only proof module registers direct Def implementations and master-listed JSON; thin player case reuses PointGame authoring inputs, not historical runtime suites.

## Errors and compatibility

Missing booleans, native minimum object, duration or service list; unknown resources; wrong-kind/missing profiles; duplicate/invalid/incompatible service references; capacity/time violations fail explicitly. Runtime disabled/minimum/funding holds remain typed and never consume stock instantly. No default, compatibility shim or second policy/catalog path.

## Verification and execution order

1. Shared declarations and actual contract consumers; source review.
2. Independent profiles/explicit master authoring and one thin player case.
3. Build Beep.SimEngine.slnx; only ONE NEW bounded exact runtime case, same-case repairs if necessary.
4. Freeze executable inputs and reconcile stage/evidence/tracker/audit without changing original acceptance text/checkboxes.

## Risks and limits

Trip service references belong to the actual carrier's recurring definitions; a valid content ID alone is insufficient. Loading/operating quantities retain native units and original transaction/elapsed exposure. Profile willingness cannot override physical blockers. Scoped observation is not arbitrary CLR sandboxing.

## Out of scope

Oil, business/Tycoon, mandatory budget/money, Draw/adapters/platforms, new transport media, second clocks/schedulers/ledgers, historical/family/full runtime gates, and publication before full completion.
