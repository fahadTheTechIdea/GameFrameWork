# P4-C1b2 — Explicit work, supply, construction, infrastructure and response owners

## Problem and success

Status: registration/contract migration focused-verified on 2.26.0 / save 11: 61 checks in two exact
suites. See [verification](../../docs/migration/P4_OPTIONAL_WORK_VERIFICATION.md). Full C1/P4 remains open.

At this slice's start, WorkSystem resolved LogisticsService/IEconomy unconditionally and abandoned
concrete Site parts. WorkBoard validated concrete construction/line/recipe plans, while PhysicalWorkModule
and WorkView registered/projected unrelated mechanisms together. Transfer owners and replace those hidden
dependencies in place: local unpriced work must run without space, logistics, construction or money;
automatic supply/haul/positive cash must require actual selected contracts before Initialize.

## Contracts and authoritative state

Extend IWorkPlan with required AutomaticSupply, authored as automatic_supply on orders/construction/
lines. False waits on actual local inventory; true requests through IWorkSupplyRequests, implemented
by the same LogisticsService/demand identities. IHaulExecution delegates existing load/unload/cargo
recovery methods to that same instance; WorkSystem keeps its existing jobs/toils, not another scheduler.
IWorkPlanSource exposes additional component-authored plans for the board's one validator.
IWorkCancellationTarget lets the actual component own cleanup; Site uses its existing abandon/salvage
algorithm. No concrete construction/line/production validation remains in the generic WorkBoard.
Positive step/supplier prices require IEconomy; zero-priced work does not resolve it.

## Module and view ownership

Delete PhysicalWorkModule, without a registration facade. WorkModule (sim.work) owns WorkerDef,
OrderDef, WorkBoard/WorkSystem/order/home/control commands and WorkView. It requires motion/access/
inventory, not automatic hauling, construction, networks or money. LogisticsModule (sim.logistics)
owns transporter/supplier declarations, LogisticsService/LogisticsSystem, stock/route commands and
LogisticsView. ConstructionModule (sim.construction) owns constructible/site/effects/generated sites/
salvage, its existing service/resolver/commands/events and ConstructionView. InfrastructureModule
(sim.infrastructure) owns line/network/port declarations, existing LineService/system/resolver/
markers, layer/time/plan validation and InfrastructureView; it explicitly selects tile navigation.
IncidentResponseModule (sim.incidents) owns response effects and IncidentsView, without a second
incident ledger. InventoryView owns the transferred StoreAccepts definition query. WorkView retains
only work facts; no forwarding compatibility methods survive. Existing queue/store/job codecs remain
the authoritative owners, with module manifests reflecting transferred registrations.

## Migration and error handling

Update all affected standalone declarations/manifests/required JSON fields/views in place. Colony
explicitly selects physical capabilities; point-space work selects work/logistics/production but no
tile construction/infrastructure. Optional providers are read only when authored/runtime capability
requirements justify them. Missing requirements fail explicitly; do not install free economy or
fabricated supply. Construction and line plans validate using the same WorkBoard.ValidatePlan path
before initialization/posting. Generated capacity/templates remain later authored-construction work.

## Verification and boundaries

Build all standalone projects. Run exactly two suites: local timed work/refusals/saved continuation,
and bounded physical colony composition/hauling/construction/production/infrastructure/cancellation.
No broad/full run. Keep remaining DAG/transactions/generic construction/process/policy/incident
stages open; this is mechanism composition, not completion of those algorithms. No legacy aliases,
platform Draw/adapters, oil/Tycoon or host/demo gameplay. Use staged ownership and evidence tracking.
