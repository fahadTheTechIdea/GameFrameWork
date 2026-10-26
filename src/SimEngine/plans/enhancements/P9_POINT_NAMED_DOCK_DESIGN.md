# P9-D5m: External point-space named dock continuation — Design Document

Status: external point focused slice verified; full P1-P12 remains OPEN.
ONE exact case221/0/0 in1331ms; FIVE clean builds, FINAL4.05s.
[Evidence](../../docs/migration/P9_POINT_NAMED_DOCK_VERIFICATION.md).
Parents: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md), [P4](P4_REUSABLE_MODULES_DESIGN.md),
[P3](P3_TIME_AND_WORLD_DESIGN.md), [P6](P6_VERIFICATION_DESIGN.md).
Previous: [native acquired dock restoration](P9_DOCK_RESTORATION_DESIGN.md).

## 0. Problem statement

PointDockDef delegates named resource-service admission to WorkDockRules but the current D5k/D5l
proofs execute only native tile geometry. Source/compilation cannot prove that external spatial
contracts support named-only facilities, actual recovery egress, productive re-entry, saved progress
and resource promises through the same engine. This leaves P4 file/provider extension and P6 external
spatial proof requirements incompletely evidenced.

## 1. Goals

1. A normal master-listed point-space document selects existing public PointModule and native owners.
2. Named-only point dock uses real service, waiting and departure geometry without unnamed stock.
3. The original job/cargo/receipt/earned handling survives restoration before exit, at exit and under
   busy service, through the existing native selectors/executors.
4. Checkpoint twins, replay and exact local conservation pass through public commands/views/Update.
5. Shared player workflow, no copied scheduler/formulas or driver stock/progress mutation.

## 2. Design

Extend ScopedHaulGame.Factory with an explicit pointDockRecovery composition choice. Register existing
PointModule and select a new explicit master; keep tile and point scenario documents separate and do
not activate sim.tile-navigation for the point scenario. Five canonical shared JSON files own the
reused resource/stock/recovery/work-policy definitions;13 existing masters explicitly reference them. The selected PointSpace owns geometry/motion;
WorkBoard, LogisticsService and IResourceTransaction retain all work, dispatch, handling and receipts.

Author scoped-point-dock-restoration.json: point space, point presence/motion and point dock slots,
native entity_setups/world_setup_profiles, same existing steel/capacity/work-policy/recovery definitions.
Keep PointModule's point_starts empty: native WorldSetup alone creates the declared entities/homes.
EndpointProofModule no longer declares endpoint_starts or adds PointStart world generation. Its existing
endpoint fixture now uses native WorldSetup through explicit endpoint-startup.json; no duplicate type
registration or startup owner. This endpoint fixture migration is source/compile only, not rerun.
Two departure positions are explicit content, not a fallback. Extent is authored fixture data,
not a framework size limit.

Reuse NamedDockRecoveryTests' existing three-episode RunRestoration player workflow with explicit
scenario/geometry selection. Snapshot readers compare actual PointSnapshot or TileSpatialSnapshot
values; no synthesized tile geometry, selection/route/ETA rule, domain formula or second execution model.
Only a NEW exact point case runs; earlier tile cases compile only.

## 3. API surface

Composition only: ScopedHaulGame.Factory(..., bool pointDockRecovery=false).
No core contract/command/field additions. Executable evidence exposed point motion crossing an occupied
bay: PointSpace.CheckMobileVacancy now owns one actual occupancy scan for motion segments and dock
vacancy. PointMotion waits without crossing another physical worker/carrier; PointDock delegates.
Point space schema5/motion2/dock5, existing layouts unchanged. Core2.328.0/save154 remains unchanged.
Use existing IWorkDock, IMotion, ISpatialPresence/ISpatialPlacementIntent and named resource contracts.

## 4. Files to change

- ScopedHaulGame.cs: composition branch and explicit preloaded document.
- Two normal JSON files and explicit ExtensionProof.csproj resources.
- NamedDockRecoveryTests.cs: shared workflow, immutable geometry observation, one new exact case.
- Stage/evidence, current tracker/parents/audit, AGENTS/README/help.
- EndpointProof.cs and endpoint JSON/master: delete duplicate startup path; use existing WorldSetup.
- PointSpaceProof.cs/PointDockProof.cs: one occupancy scan serves movement and service vacancy.
- Five shared scoped JSON files/five original documents/13 masters: canonical definitions without copies.

## 5. Errors

Missing/mismatched providers or content fail normal composition; blocked actual service waits through
native typed diagnostics. Each stage has1000 Update bound and geometry/job diagnostic snapshot.
No substituted tile fallback or driver-created arrival/receipt/stock. The initial copied one-dimensional
standby/exit layout genuinely blocked its lane after occupancy enforcement; explicitly authored standby0,
primary exit25 and alternative exit14 make real pickup/service/egress feasible without disabling physics.

## 6. Compatibility

Development only; no compatibility layer, old-reader shim or duplicated gameplay model.

## 7. Verification

Compile when ready. Run ONE NEW exact case:
External point dock: named original restoration, physical service and saved continuation.
Use existing OS/process-bounded runner15s/termination3s; repair/rerun only this case if needed.
No historical/family/full/Oil/platform runtime; full regression remains open until authorized.

## 8. Risks

Non-tile content must activate its correct space/module without trying to compose tile navigation.
Productive re-entry must use real external dock geometry rather than retained departure semantics.
Native entity initialization must not also trigger point_starts or duplicate entities. Endpoint startup
now delegates to its sole native owner instead of registering PointStartDef again. The one-dimensional
reference provider proves contract routing/dock vacancy, not every volumetric collision model.

## 9. Out of scope

Unity/Godot/Draw, Oil/Tycoon, new transport media, arbitrary providers/fault/collision/area/fuel/stress
matrices, global accounting/full acceptance, commit/push before full goal AND plan completion.

## 10. Execution order

Read existing contract/provider/composition owners; author normal master/content; share player workflow;
compile and run one bounded case; fix demonstrated defects in original owners only; record exact evidence,
hash anchors and unchanged full-plan requirements. Keep goal active.
