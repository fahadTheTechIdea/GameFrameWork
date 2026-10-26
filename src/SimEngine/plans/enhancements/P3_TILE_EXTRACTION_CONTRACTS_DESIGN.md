# P3-S31 — Tile resource extraction and reading contracts

## 0. Problem

TileResourceCellDeclaration still resolves concrete TileMap/MapLayer, and TileReadingDef still requires MapLayer. Authored workflows therefore cannot use a genuinely independent tile provider despite its existing geometry, sample and resource capabilities.

## 1. Goals

1. Resolve actual registered layers once through capability-based tile binding, without requiring a concrete provider or an unrelated sampling capability for resource quantities.
2. Keep the original endpoint receipts, resource transaction, work graph, knowledge ledger and clock authoritative.
3. Prove physical extraction and separately ordered optional survey through normal commands, immutable views, partial saves and deterministic continuation.

## 2. Design

TileSpatialGeometry.RequireLayer<TLayer> owns actual registered layer selection and metadata admission. RequireSampleLayer adds only sample revision admission. TileReadingDef requires ITileSpatialSpace/ITileSampleLayer; TileResourceCellDeclaration requires ITileSpatialSpace/ISpatialResourceQuantity<TileCoord>.

The independent proof provider exposes quantities over its original sparse sample dictionary. SpatialResourceValues performs exact conversion/depletion and SpatialResourceCellBindings embeds the original endpoint guards/books. No second balance, reservation ledger or extraction implementation exists. Its local codec stages candidate samples and endpoint books before publication; selected-space observation revision covers every layer.

Explicit master-listed extraction and knowledge JSON add a private half-unit deposit, native resource cell/effect, six-minute extraction and independently orderable three-minute survey. The same native worker travels and earns work. Tests only issue commands, update the game and inspect public facts; extraction does not require prior surveying.

## 3. API/state

Generic tile-layer binding replaces concrete lookups. Engine build2.582.0, save157/replay1; native engine payloads unchanged. Independent proof space schema2 explicitly records sample owners and original resource-cell books, with no compatibility reader.

## 4. Files

Map/TileSpatialContracts.cs; Standard/SpatialReadings.cs and SpatialResources.cs; current engine/protocol identity; independent provider/work factory; explicit extraction/readings JSON and embedded registrations; IndependentTileExtractionTests.cs.

## 5. Errors

Missing/duplicate/incompatible registered layers refuse admission; stock remains protected by actual receipt guards. Invalid coordinates, illegal or unrepresentable material, corrupt owner/sample/books and revision exhaustion refuse rather than invent stock or default providers.

## 6. Compatibility

Development-only replacement; no concrete fallback, parallel owner or legacy codec.

## 7. Verification

Targeted compile succeeded with zero warnings/errors. Final exact extraction/survey88/0 and affected independent sensor73/0. The existing combined native/point extraction admission case returned83/2: point schema expectation4 is stale (actual5), and point withdrawal enters a public visibility observation while its resource commit is unsettled. That gate remains open; no serialization guard is relaxed. [All attempts and evidence](../../docs/migration/P3_TILE_EXTRACTION_CONTRACTS_VERIFICATION.md). No full/profile/family execution.

## 8. Risks

Broader geometry, concurrent extraction, recovery, utility, privacy, lifecycle, replay/lockstep/stress and original compound acceptance gates remain open.

## 9. Exclusions

Oil/Tycoon/platform rendering, instant resource credits, host gameplay, full test runs and Git publication.

## 10. Execution/status

Tile contract migration and independent physical proof verified within the stated scope. Existing point-provider commit/admission regression is the next corrective work; broader extraction and full goal remain open. No original broad checkbox closes.
