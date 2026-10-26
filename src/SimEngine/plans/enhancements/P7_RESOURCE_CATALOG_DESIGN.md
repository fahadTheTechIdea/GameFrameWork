# P7-A — Extensible resource catalog and authored units

Status: focused-verified, 2.27.0 / save 12, 113 checks in two exact suites. See
[evidence](../../docs/migration/P7_RESOURCE_CATALOG_VERIFICATION.md). Overall P7 remains partial.

## Problem and goals

The sealed ItemDef and unvalidated display-unit string prevent real resource providers and unit-safe
requirements. ResolvedContent already owns compact identities and base-definition aliases; extend it
in place. Delete ItemDef/ItemId/ItemStack and old items/item public and JSON paths in this breaking
migration. No second registry, converter ledger, content loader or compatibility API.

## Design and public contracts

ResourceId replaces the compact session index; ResourceAmount replaces its quantity pair. Stable
external identity remains Def.Id. Abstract ResourceDef implements IResourceDefinition; its sealed
Read/Resolve validate shared required fields then call protected provider hooks. BasicResourceDef
is the built-in resources JSON section. Extensions register derived kinds through ContentTypes.Section
and reuse ResolvedContent's ResourceDef base aliases and the same compact ID index.

IResourceCatalog is implemented by ResolvedContent itself. Its resource/definition references alias
existing definitions; read-only projections never expose mutable registries. Required resource data:
quantity unit, phase, category, trait IDs and an explicit capacity object of unit ID -> per-quantity
measure. UnitDef implements IUnitDefinition with stable UnitId, dimension, positive rational scale
and required discrete flag. UnitConversion uses exact BigInteger rational arithmetic into Q32.32,
with typed incompatible-dimension, fractional-count, unrepresentable, invalid and overflow refusals.
No kg/litre density inference, rounding, authored-unit substitution or implicit units.

Units and resources resolve before consumers, exactly once even for extension kinds. BillSpec validates
authored quantity legality. PlayerView copies immutable resource/unit facts; port/haul/process/state-
sync consumers migrate to ResourceId/ResourceAmount in place. All standalone fixtures explicitly author
units and resource metadata; their known previous choices become JSON, not runtime fallbacks.

## Files and sequence

Revise Core/Ids.cs, Content/Defs.cs/ContentTypes.cs/ResolvedContent.cs/Bill.cs, Hosting/PlayerView.cs,
Kernel/StateSync.cs and all existing inventory/work/logistics/network/process/command consumers.
Add Content/Resources.cs and Core unit contracts/conversion under the existing framework namespace.
Migrate all standalone providers and JSON. Add a public-contract resource provider with a master-listed
Planet X ore document and focused catalog/unit tests. Build/save identity breaks explicitly.

1. Rename all consumers together and delete old symbols/sections.
2. Add authored unit/resource contracts, exact conversion and catalog aliases.
3. Resolve base/derived resources before bills and entity components; migrate authored fixtures.
4. Prove extension ownership/immutability/selected files, unit refusals and saved execution.
5. Record exact verification in the master. Capacity policy and owned transactions are the next P7 slices.

## Errors, verification and remaining scope

Missing shared fields/unit/measure, invalid rational/discrete quantity, duplicate traits/measures/base IDs
and unknown references fail explicitly with module/file/definition context. Conversion never mutates.
Build the standalone solution and run exactly two named directly affected suites, not a broad/full run.
One tests catalog/units/refusals; one tests a resource-consuming saved physical workflow/provider.
No old-save compatibility, oil/Tycoon, hosts/demos, platform Draw or renderer work.

This is P7's catalog/unit slice, not all inventory atomicity. Existing scalar storage/network capacity,
Bill.Total and anonymous endpoint promises must migrate in P7-B/C before the resource gate closes.
No claims of mixed-unit live capacity or atomic carrier/tool acquisition until those migrations land.
