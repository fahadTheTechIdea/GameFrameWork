# P4-G2b2b / P9-A1 — Authored target operating regions

Status: focused-verified, 2026-10-05: **73 checks / two exact named suites**, zero failures/aborts.
Build2.52.0/save26/work11/view11; [evidence](../../docs/migration/P4_TARGET_REGIONS_VERIFICATION.md).
Parents: [P4 requirements](P4_REQUIREMENT_EXPRESSIONS_DESIGN.md), [P9 policy](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
This is the immutable target-region prerequisite, not complete operating-area policy or P9.

Follow-up [P9-B1 worker policies](P9_WORK_POLICIES_DESIGN.md) is focused-verified on build2.53.0/
save27/work12/work-view12/logistics-view4 (**147 / two exact suites**). It assigns these same region
definitions to work/haul destinations and automatic home return, including physical policy pause.
This document's 73-check gate is historical, not rerun there. Editable region geometry, route containment
and broader service/source/fleet/all-dispatch enforcement remain open.

## Acceptance and sole ownership

1. SpatialRegionDef / ISpatialRegion is the single authored region family in the existing content
   catalog. Stable IDs and explicit space references resolve before Initialize. JSON files remain
   explicitly master-listed. No region registry, mutable region service or additional spatial world.
2. Providers own geometry. The kernel accepts a detached SpatialSnapshot and typed Inside/Outside/
   Unsupported status. Space mismatch and absent geometry refuse; no default origin or tile cast in
   the work evaluator. Invalid provider statuses fault, not silently become a satisfied alternative.
3. Tile implementations provide rectangular and painted regions. Every occupied footprint tile must
   fit. Painted tiles are unique, nonnegative, sparse and canonically sorted; their public list is
   immutable. Validate bounds with wide intermediates rather than overflow or an arbitrary size cap.
4. TargetRegionRequirementDef is an ordinary named IWorkRequirement leaf, referencing ISpatialRegion.
   Existing all/any composition, invalid dominance, committed branches, candidate checks, ongoing work,
   immutable private views and native spending/progress own their current responsibilities unchanged.
5. Outside is Blocked with `work.target-outside-region`; missing/incompatible geometry is Invalid
   with `work.target-region-unsupported`. A plan can queue while blocked; the ordinary post command
   admits intent, not instantaneous execution. Requirement previews distinguish those semantics.
6. Work inside an area still physically approaches its target. A worker can start outside this target
   region; it is not a current-worker-position or route restriction. Leaving pauses progress/spending
   while preserving the actual job and consumed startup inputs. Re-entry resumes through existing
   reach/work checks. Cancellation releases the existing relation; no refund or teleport.
7. Regions are immutable content, not saved live settings. Save/hash uses the current content
   fingerprint and existing entity/node/job codecs. Build identity changes; save/work/view layouts do
   not change merely to add a stateless leaf. No old-build compatibility reader is introduced.
8. An ordinary public-only mod implements PointIntervalRegionDef over the existing point provider
   without editing WorkBoard/WorkSystem or installing a TileMap. Its physical paused saved twins pass.

## Authoring

```json
{
  "rectangular_tile_regions": [
    {"id":"field.allowed","space":"world.tiles","x":18,"y":18,"width":8,"height":8}
  ],
  "painted_tile_regions": [
    {"id":"pad.allowed","space":"world.tiles","tiles":[{"x":20,"y":20},{"x":21,"y":20}]}
  ],
  "target_region_requirements": [{"id":"work.in-field","region":"field.allowed"}]
}
```

Reference `work.in-field` from a node or named all/any group, just like materials/costs/tools. No
second requirement parser, activity graph, selector, progress owner or reservation book is needed.
Another spatial module declares a concrete SpatialRegionDef section through ContentTypes; it validates
its own space/shape and implements CheckShape. The shared base owns space identity/refusal.

## Files and validation sequence

Kernel/SpatialRegions.cs; Content/SpatialRegionDef.cs and ContentTypes; Map/TileRegions.cs;
Standard/SpatialWorkRequirements.cs and WorkModule. Proof JSON and PointRegionProof are ordinary
engine-side extension content, not host gameplay. Tests orchestrate checkpoints/commands and read
engine results; they do not implement containment, matching, progress, reservation or spending.

Read/resolve geometry → normal requirement leaf → private previews/physical guards → non-tile
extension → bounded saved/cancel proof → build and exactly two named gates → tracker/evidence.
All physical waits are bounded at 200 updates and report tick/node/state/progress/blocker on failure.

- [x] Full-footprint rectangle/painted membership, immutable canonical intent and strict invalid JSON.
- [x] Pure/private requirement views, all/any composition and explicit unsupported geometry.
- [x] No worker claim/progress/material consumption while target is outside.
- [x] Actual travel, outside pause, preserved job/inputs, re-entry, cancellation and saved twins.
- [x] Timed nodes and public-only non-tile authored regions reuse the same execution owners.
- [ ] Player-editable/versioned areas and saved policy assignment/commands.
- [ ] Worker/fleet permitted destinations, routes, recovery/service/extraction and source policies.
- [ ] Layer/vertical provider region implementations and complete policy/emergency interactions.
- [ ] Broad regression/replay/lockstep/stress/performance/full-stage acceptance, only when authorized.

Remaining scope must extend these same contracts and existing job/logistics/spatial owners. Do not
infer route-constrained pathfinding, all-dispatch area enforcement or complete P4/P9 from this gate.
No oil, Tycoon, host/demo gameplay, Draw, platform adapter or renderer work is included.
