# P9-B8 — Operating reserves and powered transporters

Status: focused-verified, 2026-10-06. Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Build2.106.0/save64/logistics service6/work system6/logistics view11; existing payloads unchanged.
113 checks/two exact suites, clean build. [Evidence](../../docs/migration/P9_TRANSPORTER_RESERVES_VERIFICATION.md).
Full P9/framework roadmap remains partial; preceding suites compile only, not rerun.

## 0. Problem statement

LogisticsService.Preview excludes cargo.Stock.Count !=0 before ordinary haul assignment; WorkSystem's
emergency interruption check uses the same blanket exclusion. Existing stock bands and outlet reserves
already protect intended onboard operating material, but an otherwise idle fuelled carrier therefore
cannot haul. Do not create a fuel store, cargo copy, tank default or new fleet scheduler to bypass it.

## 1. Goals

1. One immutable cargo disposition distinguishes retained unpromised stock, committed outbound stock
   and unassigned surplus using actual inventory and current authored stock/outlet policy.
2. Ordinary haul admission accepts retained stock and accounts for its real occupied capacity.
3. Committed cargo and unassigned surplus remain ineligible for fresh hauling/emergency preemption.
4. Existing JSON stock bands physically replenish a fuelled loaded worker through ordinary couriers;
   fuel shortages retain its job/cargo/promises/age without changing quantities or resetting exposure.
5. Company-private diagnostics and save/load use the same authority, with exactly two named suites.

## 2. Design

LogisticsCargo.cs is another partial file of the same LogisticsService. ReadCargoDisposition returns
worker/endpoint/retained/committed/unassigned detached facts through existing IHaulExecution.
Retention is min(real Available, max(active Desired, explicit outlet Reserve)), using the same protection
calculation as Spare. Export-disabled is not a substitute for an explicit reserve: it stops exports,
but does not classify arbitrary onboard surplus as operating stock. ReservedOut is committed physical
cargo and never reclassified by changing policies. Inbound promises remain actual capacity obligations.

LogisticsService.Preview replaces the blanket stock-count exclusion with disposition.HasResidualCargo.
The same read replaces WorkSystem's blanket emergency-preemption exclusion. Existing capacity offers,
whole atomic reservations, handling, fuel consumption, stock hysteresis, automatic matching, retry and
mobile reach checks remain their sole owners. Motion inputs consume unreserved stock independently of
the export floor; no automatic fuel-resource inference is introduced.

Native master-listed logistics_policy content already authors minimum/desired/maximum/priority and
explicit outlet reserve. It initializes the existing demand rows; new gameplay needs no fuel profile,
requester scope, supply registry, timer, command or fleet state. Normal policy commands change those
same owners. A refuel courier follows actual mobile endpoint positions; no teleport or host decision.

## 3. API surface

CargoDispositionSnapshot and IHaulExecution.ReadCargoDisposition(EntityId). Private LogisticsView
ReadCargoDisposition provides refusal semantics and immutable facts. ITransporter still supplies its
actual cargo inventory; public providers use the same path. No parallel payload inventory or codec.
LogisticsView.PreviewStockPolicy uses ILogisticsStockPolicies.CheckStockPolicy, the same admission as
ConfigureStockPolicyCommand. Remove that command's contradictory public-port/non-transporter restriction:
actual owned private/mobile inlets support ordinary stock bands; ownership, capacity and native quantum
checks stay with the existing service. No new policy command, schema or private-stock mirror.

## 4. Files to change

New Standard/LogisticsCargo.cs; existing Logistics/Work/WorkContracts/LogisticsModule/LogisticsView/
LogisticsPolicyCommands/build
identity; authored powered-hauling fixture and two suites; stage/master/parent/evidence/README/AGENTS/help.

## 5. Error handling matrix

| Case | Result |
|---|---|
| Foreign/private view request | Refuse without facts |
| No transporter | Explicit refusal, no invented cargo |
| Foreign or mutating cargo/policy provider | Fault scoped observation |
| Negative or inconsistent stock/Available/ReservedOut | Fault, never clamp balances |
| Retained stock occupies capacity | Existing exact capacity offer limits shipment |
| Loaded cargo or surplus | Existing no-carrier admission; never discard or borrow |
| Fuel shortage | Existing retained physical retry; stock policy refills normally |

## 6. Backward compatibility

None. Replace active admission checks directly; no empty-cargo fallback or legacy reader.

## 7. Verification

Clean build and only these exact suites (same-name repair reruns allowed):

- Transporter reserves: cargo disposition and canonical admission
- Transporter reserves: powered hauling, physical refill and saved cargo

Prior suites compile/migrate only; no full run.
Final focused gates:43 admission checks and70 physical checks, zero failures/aborts. Native and
independent public transporters follow the same full physical scenario. Ordinary master-listed fuel
stock demand dispatches real courier cargo to the moving/stranded destination; same-resource own fuel
versus loaded refuel stock remains separated by actual receipts. Interrupted saves and accounting pass.
The interruption call-site migration is source-reviewed/compiled; broader emergency-preemption matrices
were not run in this slice. No claim of a full regression or complete fleet subsystem.

## 8. Risks

Retention is an explicit player/content policy, not an inferred fuel exemption. Insufficient reserve or
no compatible standby courier may strand a vehicle normally. Stock maximum/cargo contend for actual
capacity. Same-resource cargo remains protected by existing outbound receipts, not a copied tag.
Scoped observation purity is not global rollback or complete allocation/provider proof.

## 9. Out of scope

Separate named tanks/holds, home-depot/preferred-route/load/wait/service fleet policies, distance/idle/work
consumption, proactive travel-budget planning, wider preemption/provider/replay/stress/full acceptance.

## 10. Execution order

- [x] B8.1 sole disposition/protection calculation and native admission
- [x] B8.2 private view, canonical private/mobile stock policy and powered master-JSON proof
- [x] B8.3 two exact gates, save/load and receipt/conservation proof
- [x] B8.4 scoped evidence and tracker/help; full roadmap remains partial
