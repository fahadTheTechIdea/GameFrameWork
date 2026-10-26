# P5-A3: Oil resource-contract migration — Design Document

## 0. Problem Statement

The current oil extension has28 compile errors, many caused by removed ItemId/ItemStack and catalog APIs. Reservoir composition is incorrectly represented by a physical Bill although its values are dimensionless fractions. Native bills now validate real resource quantities/units, so renaming that metadata alone would preserve an invalid model.

## 1. Goals

1. Remove obsolete resource identity/amount/catalog/codec APIs from OilCo.Game; use existing ResourceId/ResourceAmount/catalog contracts directly.
2. Reservoir composition stores immutable actual resource aliases and dimensionless fractions, not a second inventory or native quantity bill.
3. Validate nonempty positive unique shares summing to one within the existing fixed-point parser's representation quantum; preserve canonical resource ordering and explicit primary selection.
4. Do not restore removed framework aliases or claim runtime acceptance while other migrations prevent compilation.

## 2. Design

Existing resource callers in Autopilot, Campaign, Commands, Company, Operations, Reservoirs and Views directly use current resource identifiers, amount fields, catalog methods and Resource codec. The original Bill remains physical only.

ReservoirTypeDef retains its existing composition JSON object intent until ResolveCore, then resolves actual IResourceDefinition aliases, reads positive fractions, verifies sum and sorts by ResourceId. Positive decimal parsing truncates each share by less than one raw Q32 unit, so only residuals in[0,share-count) raw units are admitted, not the old one-percent tolerance. No share is normalized or rewritten. Its immutable IReadOnlyList<KeyValuePair<IResourceDefinition,Fix>> is metadata, not physical balances. Existing extraction consumes those aliases/fractions rather than Bill lines. Geology truth/stock/time owners remain unchanged.

Old work effects/plans, client queue facts, composition/spatial/resource transaction execution, mixed-unit statistics and optional business ownership remain explicit subsequent migrations. No renamed copy of those removed framework services is introduced.

## 3. API surface

Oil internal resource names/types and reservoir composition metadata change directly. Public resource queries retain their string intent but resolve through the actual resource catalog. No compatibility schema/type/loader.

## 4. Files to change

Seven existing OilCo.Game sources listed above; canonical stage/evidence/master/audit. Framework/Tycoon/hosts/platforms unchanged.

## 5. Error handling matrix

Missing/empty/malformed composition or share outside(0,1] refuses authoring. Unknown resource refuses actual catalog resolution. A sum outside the explicit native parser resolution refuses, without normalization or a percentage fallback. Remaining compiler errors are integration gaps, not runtime evidence.

## 6. Backward compatibility

Development only; no ItemId/ItemStack alias, Bill.Total shim or fallback resource identity.

## 7. Verification

Completed slice scoped OilCo.Game Release build; exact old-resource API search and content share source inspection. No test executor while assembly compilation remains unavailable; original physical, privacy and durability acceptance remains open.

## 8. Risks

Correct identity types do not fix downstream old storage/spatial/work/composition semantics. Optional business and mixed-unit statistics still require authoritative-owner migration, not a native sum fallback. New compiler errors can emerge after old types resolve.

## 9. Out of scope

Full oil runtime/content/work/business migration, any Tycoon rule change, framework aliases, client/host/platform/Draw changes and Git writes.

## 10. Sub-task execution order

1. Directly migrate resource API callers.
2. Replace incorrect physical composition bill with typed fraction metadata.
3. Compile and retain unresolved errors; run no unavailable runtime.
4. Update tracker/evidence/source freeze and continue remaining integration.

## Partial implementation outcome

Seven existing sources directly migrated;43 identity/catalog/codec caller lines plus composition/resource-fact changes. Compile28 ->13 ->12 errors, all still exit1; observed builds3.04s/1.05s, zero warnings. Old resource type/catalog/codec searches are empty. Five actual authored reservoir compositions sum to one as decimal source data; this is not engine-load/runtime evidence. Full work/physical/business/unit/statistics/content/visibility/durability acceptance remains open. See [verification evidence](../../docs/migration/P5_OIL_RESOURCE_CONTRACT_MIGRATION_VERIFICATION.md).
