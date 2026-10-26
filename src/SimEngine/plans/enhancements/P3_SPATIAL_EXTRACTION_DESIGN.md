# P3-S5h — Receipt-protected spatial extraction through timed activities

Focused-verified, 2026-10-06: build2.117.0/save66/native map6/public point space4.
195 checks in the two exact suites below; clean build.54 preceding assertion files migrated and
compiled only, not executed. [Evidence](../../docs/migration/P3_SPATIAL_EXTRACTION_VERIFICATION.md).
This closes this bounded slice, not the full P3 or framework roadmap.

## 0. Problem statement

P3-S5g provides exact reads/depletion, but not protected material promises or inventory
transfer. Actual cells must participate in the existing resource transaction protocol;
positive-time activities must move their material rather than fabricate production.

## 1. Goals

1. Actual layer cells expose scoped IResourceOutlet and IResourceInlet, without stock copies.
2. Existing ResourceReservationBook protects cell stock, generation and ownership changes.
3. One per-layer binding owner saves/hashes the existing endpoint books alongside actual values.
4. Explicit master-listed JSON binds stable cell IDs, geometry, capacity and extraction effects.
5. Existing PhysicalWorkEffectDef/WorkBoard/WorkSystem perform timed extraction and existing
   IResourceTransaction commits deposit-to-inventory transfers with real receipt provenance.
6. Native and independent provider hooks use the same binding/promise rules. No new scheduler.

## 2. Design

ISpatialResourceQuantity<TLocation> gains composition-time cell binding. A public
ISpatialResourceCellGuard extends endpoint metadata with actual quantity and mutation/checkpoint
hooks. SpatialResourceCellBindings is the single embedded callback/codec owner shared by native
and independent layers; it holds endpoint aliases, never quantities or a second reservation book.

SpatialResourceEndpoint<TLocation> owns one ResourceReservationBook, persistent endpoint version
and transient bounded commit state. Amount derives from the existing layer.ReadQuantity. Normal
withdrawal/Set/Fill cannot reduce below promised stock; ownership changes refuse live promises.
The selected receipt's exact bounded debit is admitted only during its actual TakeReserved call.
Books are staged/validated with candidate layer values before restore publication. An interrupted
commit cannot checkpoint. Every outlet is also an inlet; these finite deposits explicitly reject
incoming material, with no replenishment/default capacity path.

SpatialResourceCellDef is an abstract content/geometry contract. Native TileResourceCellDef
declares identity, layer, x/y, capacity_policy and public access. Duplicate cell/endpoint bindings,
nonquantity layers, invalid bounds and private-public access contradictions fail before tick one.
ISpatialResourceEndpoints aliases these actual endpoints for the existing transaction provider
lookup; it adds no persistent store. Other spaces author their own definition/binding contracts.

IResolvedContentValidator supplies one post-reference validation phase before content publication.
Cells/effects bind references in Resolve and validate linked metadata after all definitions have
resolved, independent of section/document order. No special spatial ordering or repeated Resolve.

SpatialExtractionEffectDef references one cell definition and one explicit native Bill. It requires
positive machine work in dependency ancestry, validates the real stationary target/site and uses
EvaluateMovement/CommitMovement to the target's actual inventory. No Produced/Initialized grant.
Existing graph completion/blockers/retry, priorities, jobs, movement and immutable private work
views remain authoritative. Claims at the final transaction boundary are not promises of future
deposit stock throughout earlier work; earlier competition may block completion legitimately.

## 3. API surface

Engine-side bindings/endpoints/effects only. Player uses ordinary SubmitPlan/PostOrder commands,
previews, existing views and Update. No instant extraction, rendering or host gameplay command.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| New | Kernel/SpatialResourceCells.cs | high |
| Modify | Kernel/SpatialResourceValues.cs; Map/TileMap.cs | high |
| New | Standard/SpatialResources.cs; SpatialResourcesModule.cs | high |
| Modify | Content/Defs.cs; Content/ResolvedContent.cs | medium |
| Modify | tests/ExtensionProof/PointLayers.cs | medium |
| New | tests/ExtensionProof/SpatialResourcesProof.cs; content/point-spatial-extraction.json | medium |
| New | tests/Beep.SimEngine.Tests/SpatialExtractionTests.cs | medium |
| New | tests/ExtensionProof/content/spatial-extraction.json | low |
| Modify | engine identities/assertions/registration/master/parents/README/help | low |
| New | docs/migration/P3_SPATIAL_EXTRACTION_VERIFICATION.md | low |

Runtime paths are under src/Beep.SimEngine. Public proof paths under tests/ExtensionProof.

## 5. Error handling matrix

| Case | Outcome |
|---|---|
| Missing/duplicate/foreign/nonquantity binding | ContentException before tick one |
| Normal debit/generation would violate a live promise | Refuse before actual values/cursors change |
| Ownership change with live promises | Refuse before owner/cursor change |
| Invalid receipt/owner/quantity | Existing typed transaction refusal; no stock movement |
| Invalid saved identity/book/native quantum/stock | CorruptStateException; staged books never publish |
| Interrupted provider commit | No resumable checkpoint or fake compensation |
| Wrong site/moving target/foreign access | Same effect admission/preview refusal |
| No stock/capacity at completion | Existing authored work retry/blocker, no material grant |

## 6. Backward compatibility

None. Save/build/native/public layer schemas change as needed. No legacy readers or shims.

## 7. Verification

Only two exact complete suites:

- Spatial extraction: authored ownership, receipts and actual transfer provenance
- Spatial extraction: timed work, blockers and saved physical continuation

Build: dotnet build Beep.SimEngine.slnx --no-restore. No prior/family/third/full gate.

## 8. Risks

- Layer callbacks are engine-side contracts, not universal rollback. Validate scoped purity and
  fail unsafe provider commits; never fabricate compensating material or resumable partial state.
- Native/public geometry owns cell identity; shared binding logic must not infer tile coordinates.
- Site changes and rival consumption after plan admission require fresh completion checks.

## 9. Out of scope

Hydrology/replenishment, reservoir science, 3D, mining content/rail/conveyors, Oil/Tycoon/hosts/
platforms/Draw, global rollback, broad replay/lockstep/stress/allocations/full-roadmap gates.

## 10. Execution order

- [x] Shared binding/guard contracts and single embedded checkpoint implementation.
- [x] Generic real-stock endpoint, native JSON cells and existing transaction provider alias.
- [x] Native/public value/ownership/generation guards and checkpoint hooks.
- [x] JSON timed extraction effect using existing movement/graph owners.
- [x] Two named gates, clean build, evidence and tracker/parent/help alignment.
