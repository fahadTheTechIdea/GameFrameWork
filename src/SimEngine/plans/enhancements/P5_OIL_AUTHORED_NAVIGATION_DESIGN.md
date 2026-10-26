# P5-A12: Oil native authored layers and navigation — Design Document

## 0. Problem Statement

OilModule creates terrain/water/road layers and registers movement costs imperatively through retired map/navigation APIs. Settings.cs contains a second MovementClassDef cost reader. These duplicate native SimEngine authoring and leave five compiler errors. The sole owners must remain SpatialLayerDeclaration, TileLayerBandDeclaration, LayeredTileNavigationDeclaration, TileMap and NavService.

## 1. Goals

1. Author Oil terrain/water/road in the existing spatial-world.json, with a declared discrete unit and public layer metadata.
2. Author land/sea rules in one master-listed navigation.json using native typed conditions.
3. Delete the old movement class reader/section and imperative map/profile creation; no compatibility fallback.
4. Verify canonical Oil JSON through native composition, read-only views, saved continuation and six scenario geometries using one bounded exact case.
5. Keep complete Oil content/physical acceptance and all original gates open while the extension remains unbuildable.

## 2. Design

Oil declares sim.tile-conditions in its code and master requirements. TileNavigationModule already resolves ITileNavigationRule and creates NavService. Oil composition obtains the original TileMap and existing typed layers, without defining a second profile or cost interpreter.

Three public byte layers use oil.tile-code; these classify terrain/water/roads rather than store resource quantities. No resource association is authored. Existing scenario geometries remain unchanged and are content choices, not engine size limits.

Ten native band conditions implement inclusive minimum/exclusive maximum. Two profiles implement ordered land/sea costs. Paved and dirt roads retain precedence over wet terrain. Unsupported terrain codes explicitly block rather than index beyond a private cost array. Sea accepts water codes2..3. Original minimum-cost computation and routing remain in SimEngine.

The non-friend ExtensionProof project embeds canonical sibling Oil files by link, not copies. Its read-only feature delegates directly to native NavGrid. Fixture initialization uses the original authored WorldSetupModule; the driver never writes world/layer/entity/resource/progress state. This proof dependency belongs to consumer verification, not framework runtime.

## 3. API surface

Oil MovementClassDef and movement_classes section are removed. No new production framework API or callback interpreter. Native navigation/profile/layer contracts remain authoritative. The proof-only view returns native costs and minima.

## 4. Files to change

| Action | File | Risk |
|--------|------|------|
| Modify | OilCo.Game/OilModule.cs and Settings.cs in the Oil extension | medium |
| Modify | Oil content/manifest.json, oil/rules.json, oil/spatial-world.json | medium |
| New | Oil content/oil/navigation.json | medium |
| New | tests/ExtensionProof/OilNavigationProof.cs and content/oil-navigation-master.json, oil-navigation-fixture.json | low |
| Modify | tests/ExtensionProof/ExtensionProof.csproj; tests/Beep.SimEngine.Tests/Program.cs | low |
| New | tests/Beep.SimEngine.Tests/OilAuthoredNavigationTests.cs | low |

## 5. Error handling matrix

| Condition | Outcome |
|-----------|---------|
| Missing/invalid layer, condition or space reference | Native content admission refuses; no invented substitute |
| Unsupported terrain | Explicit authored cost0 blocks |
| Outside selected geometry | Original NavGrid returns blocked0 |
| Invalid optional JSON resource:null | Native strict reader refuses; omit absent association |
| Oil integration compiler errors | Record failing compile; no Oil runtime/completion claim |

## 6. Backward compatibility

Development-only; no legacy reader, duplicate models or compatibility shim retained.

## 7. Verification

Build only the affected focused test project and OilCo.Game. Execute one new bounded-exact navigation case; retain any repair attempts. No old/family/full regression, including final validation, without renewed explicit human request. Hash/link/criterion metadata checks are read-only, not gameplay regressions.

## 8. Risks

Native profile-cost proof does not prove full Oil content, mover payload migration, physical hauling/routes, privacy, rivals or Oil saves. The proof project needs the canonical sibling repository. Full framework and consumer validation remain open.

## 9. Out of scope

Infrastructure callback migration, founding/economy errors, mover/worker/producer content migration, knowledge runtime, Tycoon rule changes, platforms, Draw, hosts and demos.

## 10. Sub-task execution order

1. Extend native spatial/layer JSON and add master-listed navigation conditions/profiles.
2. Remove duplicate cost authoring and manual layer/profile setup.
3. Add independent thin read-only proof and linked canonical data.
4. Build affected projects; run only the exact bounded case.
5. Record evidence and update master/audit without closing broad gates.
