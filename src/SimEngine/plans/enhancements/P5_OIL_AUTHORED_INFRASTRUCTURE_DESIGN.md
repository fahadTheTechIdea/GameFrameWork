# P5-A14 — Authored Oil infrastructure and optional cost bindings

## Scope and authority

OilFieldDays remains a normal domain extension of the canonical framework. All placement predicates,
work admission, travel, deliveries, construction, costs and topology run inside the engine.
No host gameplay, platform adapters or Draw implementation is included. One GameClock remains unchanged.

The human explicitly approved optional JSON cost-binding contracts, beyond the earlier mechanical
Tycoon API migration. This authorizes optional binding authoring only: existing amounts, cost categories,
accounting, reservation, spending and business rules remain unchanged. The broader plan remains open.
Focused tests only, including final validation; no full regression is authorized.

## Stage 1 — Native resource and spatial declarations

Replace the old Oil items reader with the original unit/resource contracts. Preserve eighteen resource
identities, categories and quantities. Seven explicit units include a game-space storage-slot measure.
Its unit coefficient preserves old normalized inventory capacity; it is not invented physical density
or a barrel-to-gas-volume conversion. Fractional construction quantities remain legal.

Declare road, pipe and power topology and owner layers through the existing spatial layer contracts.
Use typed all/any tile-condition groups for wet/dry and mountain conditions. The original dependency
compiler refuses missing references and cycles; no callback-based placement interpreter remains.
Road allows wet mountain tiles; land pipes require dry non-mountain terrain; subsea pipes require water.
The land-pipe refusal is now the authored aggregate code oil.line-land-blocked, not two old callback codes.

## Stage 2 — Physical line workflows

Author eight ordinary activities, the original staffed executor, target material requirements, one
line marker definition and original segment/finish effects. Original line/WorkBoard/logistics owners
still reserve, haul, travel, consume and build over the one game clock. No instant construction fallback.
Marker capacity explicitly accommodates the largest authored per-segment material bill.

Use native line and network definitions. Oil/gas/water resource rates preserve the original hourly
quantities with explicit hour periods. Power explicitly declares empty rates/resources for connectivity
only; no fictitious resource or inventory is introduced. This is not proof of utility generation,
power satisfaction or complete industrial network behavior.

## Stage 3 — Optional Tycoon requirements

Physical networks.json contains only material requirements. The master manifest assigns business-costs.json
to optional sim.tycoon. It contains the eight unchanged work-cost definitions and activity/node bindings;
Oil does not require or automatically select Tycoon.

IActivityRequirementBinding is the general content contract. ActivityDeclaration compiles selected
bindings into its sole graph, without work-scheduler changes. ActivityCostBindingDef is Tycoon's immutable
all-group owner: it captures the actual physical root and actual monetary leaf once before publication.
Missing activity/node/cost references and duplicate node bindings refuse before initialization.
The compiled graph is used by original disclosure, previews, supply, acquisition, receipts, progress,
save validation and hashing. No second cost ledger, scheduler, graph runtime or reservation model.

When Tycoon is absent there is no monetary leaf or account. When selected with money disabled, authored
costs are interpreted by the original disabled-cost rules without a ledger. When money and construction
costs are selected, original atomic work acquisition reserves and charges the existing amount once.

## Stage 4 — Focused verification and remaining work

One bounded NEW exact public-input case links canonical Oil JSON rather than copying it. It verifies
native placement, connectivity-only power, physical road supply/construction, isolated sessions and
saved twin continuation with Tycoon absent, selected-disabled and enabled. Enabled costs are saved
during both held-reservation and spent lifetimes. Invalid predicate and binding references refuse.

This is a stripped native engine scenario, not the complete Oil module. OilCo.Game still fails to compile
at its old FreeEconomy composition reference. Full Oil settings/components, optional business configuration,
founding, unsurveyed drilling, rival parity, privacy, networks and durability still need migration/proof.
No original broad acceptance checkbox closes. Commit/push waits for the full goal AND plan.

[Verification](../../docs/migration/P5_OIL_AUTHORED_INFRASTRUCTURE_VERIFICATION.md)
