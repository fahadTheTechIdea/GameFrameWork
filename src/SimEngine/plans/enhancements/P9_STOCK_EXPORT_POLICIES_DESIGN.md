# P9-C2 — Native stock bands and export policies

Status: C2 focused-verified, broader P9 open. Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
2026-10-05: build2.56.0/save30/logistics3/logistics-view6/logistics_policy1.
**112 checks / two exact named suites** (63 admission; 49 physical), zero warnings/errors/failures/aborts.
No broad/full run. [Evidence](../../docs/migration/P9_STOCK_EXPORT_POLICIES_VERIFICATION.md).
Work/work-view13 and resource-flow4 remain unchanged; preceding suites were not rerun.

Follow-up [C3a demand/route dispatch](P9_LOGISTICS_DISPATCH_DESIGN.md) uses build2.57.0/save31/
logistics4/view7: **120 / two exact suites**, zero warnings/errors/failures/aborts. C2's report above
is historical, not rerun. Existing rows/policies feed one current ranked queue; explicit route settings
retain physical loaded commitments. Broader alerts/emergency/fleet/docks/plans gates remain open.

## Problem / goals

LogisticsService.Demand currently stores one Level and Priority. SetStockTargetCommand exposes no
minimum/desired/maximum or enabled state. Only automatic demand source matching calls Spare; routes
and public haul acquisition use raw Available and bypass protected stock. FindSource ranks distance
only. Add policy to those existing owners, not another demand/inventory/reservation/scheduler store.

1. Immutable LogisticsDemandSettings author enabled, minimum <= desired <= maximum, and priority.
   Existing Demand implements ILogisticsDemand and owns those settings plus original age and refill
   latch. Complete workflow Bills remain exact targets; stock settings replace the same sim.stock
   row. Disable stops new requests/protection, never erases already acquired cargo/receipts.
2. Stock replenishment starts below minimum, remains active until actual unpromised stock reaches
   desired, and caps new inbound commitments by maximum minus actual stock and inbound promises.
   Native work/production demand remains additive. A stock maximum is an explicit receiving ceiling,
   not an inventory mutation or cancellation of already acquired commitments when lowered.
3. LogisticsPolicyDef contains explicit native stock entries and outlet enabled/reserve/export_priority/
   allowed_destinations (prototype IDs or explicit null). A normal component seeds the existing stock
   table only on new entity initialization and owns outlet settings only. It delegates stock state
   to the coordinator and never persists a second stock-policy list. Load/replacement never reseed.
4. IOutletPolicy supports public-contract providers. The shared haul preview/acquisition checks current
   export permission/reserve and receiving ceiling for automatic supply, routes, direct acquisition,
   evacuation and cargo return. Reserve protects max(active desired, explicit reserve), not their sum.
   Source matching compares export priority then current provider cost/stable ID. Existing inventory
   books and physical jobs remain the only claim, stock and execution authorities.
5. Replace the old single-level stock command/handler/registration, migrate referencing tests in place;
   no old command wrapper. Commands, private immutable views, codecs and entity-state contribution
   share the same contracts. Exact whole native settings admission precedes mutation.
6. Master-listed JSON point proof and public-only outlet implementation prove real movement, bounded
   refill hysteresis, ceiling contention, source choice, policy changes with loaded cargo, native
   quanta/refusals, save/load/twins and independent sessions. Build and exactly two affected suites.

## Files / sequence / contracts

Standard/LogisticsPolicies.cs owns immutable settings, profile definitions, outlet component and native
validation. A partial LogisticsService file owns stock settings/admission/readiness in the existing
table; Logistics.cs integrates all selection and codec paths. Commands/LogisticsPolicyCommands.cs
replaces single-level command and adds outlet configuration; existing LogisticsModule declares content,
bindings, commands and view schema. LogisticsView reports actual settings/refill and export facts.
Existing lifecycle, capacity/resource catalogs, transactions and entity state resolver stay authoritative.

Settings/contract → sole demand migration → JSON/lifecycle → commands/views → common admission/source
choice → strict codec → public JSON/provider proof → focused gate → tracker/evidence/README/AGENTS.
Current layouts break together; no save reader, compatibility shim or second gameplay loop.

## Errors / verification / risks

Reject null/unordered/negative bands, nonnative quanta, unknown/incompatible resources, bad owner,
unknown destinations, duplicate profiles/resources, foreign session and unrepresentable totals.
Use typed policy blockers for disabled export, destination filter, reserve and maximum waits.
No retained preview authorizes mutation. Lowered limits apply to future promises only; acquired hauls
finish physically. Policy minimum zero deliberately never triggers automatic refill below zero.
Prototype filtering is not an editable area/network/fleet policy. Total representational range is not
a game-size cap. Whole profile maxima/reserves must fit native authored capacity at content validation.

- [x] Single settings/stock authority, deterministic refill latch and maximum admission.
- [x] Explicit JSON/export contracts, commands/private facts and common haul admission.
- [x] Timed physical contention, retained loaded claims, saved twins and external provider proof.
- [ ] Global demand/route ordering, alerts/starvation, fleet/service/areas/plans/docks, provider-fault
  matrices, replay/stress/allocation and full P9/framework acceptance. These remain separate work.

Priority-only stock edits retain an active refill cycle when actual stock remains below desired.
Disabling, reaching the new desired quantity, or explicitly setting minimum zero stops that cycle.
Minimum zero is a manual-only receiving ceiling policy; the engine never invents a negative trigger.
Profile destination filters use stable prototype identities, not live entities or platform coordinates.
The old stock command/handler/registration and fixed PlayerStockPriority are deleted, not wrapped.
