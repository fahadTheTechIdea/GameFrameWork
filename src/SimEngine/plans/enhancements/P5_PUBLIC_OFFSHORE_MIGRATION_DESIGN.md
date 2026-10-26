# P5-A46: Public offshore workflow and field classification

## 0. Problem

The final old Oil suite reads Game.World/Geology/Reservoir directly and relies on removed tile, resource, production, map, path and static clock APIs. The public field projection has no typed offshore classification, forcing callers to reach into private content or guess type names.

## 1. Goals

Move the original full sea workflow once to the public runner without deleting its stages. Expose authored classification through the existing scoped field view, never unknown geology. Verify a short canonical field-observation/private-save case and remove old compile failures.

## 2. Design

Add nullable Offshore to existing FieldView. Existing ExplorationView.CopyFields sets it only inside the same Identified-confidence branch as Type/Primary, from the actual reservoir definition; below identification it stays null. Unknown fields remain absent. No second geology/knowledge store or classification algorithm.

Bump existing oil.exploration feature version2 to3. Framework core2.557/save156/replay1 unchanged; no physical state payload or business amount changes.

Move OffshoreTests to OilCo.PublicProof and reuse ClientTests.Factory. Replace raw geology fixture selection with the explicit Observer's detached FieldView records. Omniscient fixture selection is confined to test initialization; the actual player uses seat-scoped facts/commands and must physically survey as before.

Use actual TileSpatialSnapshot, TileMapView, ResourceId/named oil.stock ports, LogisticsRouteSettings, TilePlacement, EnqueueTransformationCommand and InfrastructureView path results. Native timed wait helper supplies failure snapshots instead of a silent false timeout; existing25/60/30/40-day stage limits remain.

Add a separate short case using canonical96x96 Gulf/two human seats: observer classification exists, both unsurveyed companies get no fields, reads do not change hash, saved sessions retain scoped facts/continue identically, captured facts remain detached.

## 3. API

FieldView.Offshore is bool?; null means undisclosed, not onshore. oil.exploration feature3 describes the changed projection. No legacy fields/API or privileged test helpers restored.

## 4. Files

OilCo.Game/Views.cs, OilModule.cs; move OffshoreTests; old/public registrations; OilCo.Tests/PurityTests.cs exact public input/fact exceptions and negative owner checks; per-stage evidence/master/audit.

## 5. Refusals

No valid fixture returns explicit failure. Missing metadata/site/resource, refused map reads and blocked waits are diagnostic failures. Path refusal is reported explicitly, never silently skips subsea-stage evidence.

## 6. Compatibility

No World escape, friend access, concrete geology lookup, type-name inference, static time, old production adapter or alternate work/transfer owner.

## 7. Verification

Public and remaining old project compile. Exact canonical short field/privacy/save and compiled client-boundary cases only. Boundary repair names nine actual public input/fact types rather than opening Kernel/Maps/Standard/Content namespaces; negative raw owner checks retained. No original multi-month offshore workflow or full regression.

## 8. Risks

Runtime identification-threshold crossing, physical sea production/hauling/drilling/subsea execution, original seed-search and full privacy/durability/performance acceptance remain open.

## 9. Out of scope

Financial/game rules, platforms/Draw, full tests and Git writes.

## 10. Status

Implemented; focused53/0, both public/original test projects compile clean. Initial boundary23/1 retained; full original sea and runtime identification threshold crossing remain unverified. Original221/194 recorded checked/27open unchanged, goal open. [Evidence](../../docs/migration/P5_PUBLIC_OFFSHORE_MIGRATION_VERIFICATION.md).

