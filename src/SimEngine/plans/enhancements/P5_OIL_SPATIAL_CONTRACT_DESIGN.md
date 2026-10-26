# P5-A8: Oil spatial consumers and public module query boundary

## 0. Problem statement

Oil still used retired World.Map, Entity.Tile/Footprint, EntityDef dimensions, concrete construction access and RecipeDef APIs. Pure module projections could not call the framework's existing internal fault/disclosure methods. Checked rectangle inflation could overflow at a valid maximum-coordinate world edge.

## 1. Goals

Migrate domain consumers to the selected native map and tile-presence contracts; use existing construction/content access owners; share original read-only query guards with independent modules; add bounded rectangle expansion without a platform-size clamp. Preserve incomplete integration gates.

## 2. Design

TileRect.InflateClipped(int by, TileRect bounds) extends the existing geometry owner. Wide intermediates expand and intersect positive source/bounds rectangles; negative padding or empty inputs refuse. Bounds are explicitly supplied by actual scenario geometry, never inferred from a platform. Ordinary fully contained inflation is identical; maximum-coordinate expansion cannot overflow. No second geometry/state owner.

World.RequireHealthyUpdate and PlayerView.CanSeeContent(IContentDefinition) become public module APIs in place, sharing the original fault and owned-content dependency/visibility checks. No duplicated guard, policy or content model. SimGame/host World exposure remains unchanged; this does not expose a host mutable World escape. Core2.535.0; save154/replay1 unchanged.

Oil Ground/Territory use actual ITilePresenceDefinition/ITilePresence dimensions and selected TileMap; authored-map clipping handles coastal and territorial expansion. Survey admission uses IContentAccess and selected-map placement preflight, then native marker placement; survey work still belongs to WorkBoard. World generation/town placement uses existing map placement/occupancy, not EntityStore.IsFree or World.SpawnAt. Reservoir layer access uses the actual selected map. Storyteller candidates require matching placed tile presence and use that contract for distance. Research unlocks reference ITransformationDefinition.

Planning views call original fault/disclosure checks, IConstructionProjects with TilePlacement arguments, and actual footprint metadata. Long intermediate ring coordinates are clipped before int conversion. Hidden survey/prototype metadata is withheld rather than guessed. No host placement/path/hazard/production logic added.

## 3. API surface

TileRect.InflateClipped(int, TileRect); original World.RequireHealthyUpdate and PlayerView.CanSeeContent made public read-only queries. Existing construction/access/spatial contracts unchanged. No new gameplay command, adapter or renderer.

## 4. Files

Framework Math/Vectors.cs, Kernel/World.cs, Hosting/PlayerView.cs, Kernel/SaveGame.cs. Oil Commands.cs, WorldGen.cs, Reservoirs.cs, Views.cs, Research.cs, Storyteller.cs. New independent SpatialQueryBoundaryProof.cs and ClippedTileGeometryTests.cs; existing Program/protocol/tracker/evidence.

## 5. Errors

Missing/wrong spatial capability refuses instead of fabricating dimensions. Native placement/content admission owns refusals. Out-of-bounds coordinates never index layers. Missing content query returns false through the original disclosure owner. Module queries share latched-fault refusal, not local substitute state.

## 6. Compatibility

Direct development revision; no legacy World.Map/Entity dimensions restored or compatibility shim.

## 7. Verification

Two focused framework compiles clean12.88s/9.44s. ONE NEW exact64/0/0 in494ms/outer707ms: clipped boundary arithmetic and an actual independent assembly's public metadata queries with saved continuation. Two scoped Oil compiles fail40 then34 errors; first exposed six inaccessible query APIs, fixed in the original framework. No old/family/full test runtime.

## 8. Risks

Oil cannot yet compile/load. Remaining founding8, operations10, composition16 errors; all physical/domain/runtime acceptance remains open. The focused proof does not establish global target disclosure, malicious provider/fault behavior, Oil wide-world generation, circle/distance overflow, allocations, replay/lockstep or full scenarios. Existing full-map domain terrain generation/noise and reservoir numeric paths need further review; no general Oil scalability claim.

## 9. Out of scope

Tycoon business rules, platforms/Draw/Unity/Godot, hosts, old sibling engine, generic rollout/bootstrap redesign and complete industrial regression. No commit/push before whole goal and plan complete.

## 10. Outcome

Actual current framework/module contracts implemented; independent module proof passed64 checks. Oil compile errors70 to34; five fully migrated files have zero diagnostics. All original221 criterion texts/checkboxes/line anchors unchanged (193 recorded checked,28 open); no gate closure.

[Evidence](../../docs/migration/P5_OIL_SPATIAL_CONTRACT_VERIFICATION.md).
