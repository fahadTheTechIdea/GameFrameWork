# P9-C1 — Open logistics demand ownership

Status: C1 focused-verified, broader P9 open. Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
2026-10-05: build2.55.0/save29/logistics2/logistics-view5, work/work-view13 unchanged.
**108 checks / two exact named suites**, zero warnings/errors/failures/aborts. No broad/full run.
See [verification](../../docs/migration/P9_DEMAND_OWNERS_VERIFICATION.md). Earlier suites were not rerun.

## Problem / goals

LogisticsService.DemandSource.Kinds hard-codes three request origins and cannot represent mod-owned
work. Producer requests update only present lines; replacing recipes can retain obsolete demand.
Order requests withdraw/recreate every line, losing stable request age. Replace these paths in the
existing authority, not another stock table, owner registry or scheduler.

1. LogisticsDemandId is a canonical scope plus positive 64-bit owner identity; no closed kind enum.
   Existing declared service instances implement ILogisticsDemandOwner with session, scope and pure
   active/expired/invalid source checks. Resolve through ServiceContainer.Providers, not a new registry.
2. ILogisticsDemandRequests aliases the existing LogisticsService. It checks and replaces a complete
   native Bill atomically before table mutation, preserving retained line age and withdrawing absent
   lines. Single stock edits use the same owner checks and existing table. Unknown/private/foreign/
   malformed sources, quantities and duplicate owner scopes refuse; no inferred owner or instant supply.
3. WorkBoard owns order liveness/binding; ProductionSystem owns producer liveness/queue intent;
   LogisticsService owns player stock intent. These providers inspect their existing records only.
   No mirrored workflow state or concrete producer lookup inside generic logistics.
4. Plan and reserve/spare calculations ignore expired source intent; planning prunes it normally.
   Existing acquired hauls remain physical commitments, even if their requesting workflow ends.
   Private immutable views expose actual scope/identity/resource/quantity/priority/original age/status.
5. One LogisticsService codec saves open identities and original request age, validates canonical
   records/native quantities/known owners/duplicates and migrates save/view identities without a reader.
   Expired intent can be saved until its normal planning phase; restore must not revive it.
6. A master-listed JSON external provider introduces a new source with an ID above int.MaxValue,
   posts/replaces native demands through the public contract, and receives actual hauling without
   changing the selector. Prove retirement, retained live cargo, saved continuation and isolation.

## Files / API / sequencing

Add Standard/LogisticsDemandContracts.cs and a partial LogisticsService source for owner resolution/
complete-set admission. Revise Logistics.cs demand identity/age/replace/prune/codec, WorkBoard and
ProductionSystem provider aliases, LogisticsModule alias/view schema, stock handler and LogisticsView.
Migrate directly referencing old tests to actual scoped owners; do not retain wrappers or fake orders.
Extend public-only Point proof composition with an explicitly master-listed optional demand module.

Contract/identity → owner aliases → sole request table migration → private view/codec → external JSON
proof → build and exactly two named suites → evidence/tracker/README/AGENTS/parent.
No compatibility, second demand/reservation/cargo store, host/demo gameplay, Draw or domain changes.

- [x] Declared owner composition, whole native request admission and stable source/age migration.
- [x] Private current facts, recipe-line replacement and typed retirement behavior.
- [x] Public-only authored mod demand physically supplied, retired in-flight claims and saved twins.
- [ ] Stock bands/reserves, global demand/route priority/emergency/aging, docks/fleet, broader provider
  purity/fault/replay/stress/allocation and full-stage gates. These remain separate P9 work.

Standing desired quantities are intent, not capacity reservations: they may exceed a store's physical
capacity and wait for actual delivery capacity. Whole-source admission checks exact native quanta and
representable aggregate totals, not an arbitrary level cap. The existing player stock command retains
its public-store permission and physical-capacity envelope. Min/desired/max bands are not implemented.
Withdraw/expiry affects unacquired intent only, not live cargo, jobs or native receipts. Restored owner
liveness is validated after all services load; no load-time fake production or request recreation.

Follow-up [C2 stock/export policies](P9_STOCK_EXPORT_POLICIES_DESIGN.md) is focused-verified at
build2.56.0/save30/logistics3/view6, **112 / two exact suites**. Existing rows now hold native bands,
enabled state and refill latch; common admission enforces reserves/new receiving ceilings. C1's
108-check report above is historical, not rerun in C2. Global demand/route ordering and broader gates
remain open; no additional registry/table was created to implement stock bands.
