# P9-A1b — Editable operating areas: focused verification

Date:2026-10-06. Build `beep-simengine-2.151.0`, save79, operating-area service1,
native work policy2, WorkSystem14, WorkView42. WorkBoard24, MovementSystem5, native mover3,
LogisticsService8, logistics view16 and existing cargo/receipt/clock layouts remain unchanged.

Stage: [P9-A1b](../../plans/enhancements/P9_EDITABLE_OPERATING_AREAS_DESIGN.md).

## Gate actually run

`dotnet build Beep.SimEngine.slnx --no-restore`: exit0, zero warnings/errors, final MSBuild2.16s.
After that clean build, exactly these two complete named suites:

| Suite | Passed | Failed | Elapsed |
| --- | ---: | ---: | ---: |
| Route areas: provider contracts, admission and private diagnostics |139|0|397ms|
| Route areas: physical constrained travel, cargo and saved continuation |290|0|780ms|

Total **429 checks/two suites**, zero final failures/aborts. Same-name repair reruns only; no third,
family/full regression. Existing route checks are included, not additive historical counts.
An initial physical run had287 passed/1 failed: the new assertion expected a route blocker after
home endpoint exclusion, but existing home policy correctly stops motion and exposes OutsideArea
through ReadHome. The assertion was corrected to inspect the actual home-policy authority; no route,
stock, clock or schedule was changed to make it pass.

62 preceding test source files' current build identity and59 preceding save-format assertions migrate
mechanically; unrelated suites compile only. C6b/G2c5/F2/F1/A9 gates were not rerun here.

## What the evidence covers

- Company-owned initial rectangle/paint/public interval/station geometry is seeded from explicitly
  master-listed JSON, never inferred map defaults. Each version uses the same region reader,
  resolved spatial validation, Check and CheckTravel implementation.
- New runtime names are not inserted into shared content. Current policy validation, destination,
  docks, route constraints and native/public motion resolve one IOperatingAreas authority.
- Commands and immutable/private WorkView previews share validation. Stable IDs, exact expected
  revisions, invalid JSON, bounds, unknown prototypes, extra fields and duplicate paint cells refuse
  before commit. Object members and paint order canonicalize using the existing JSON serializer.
- Same authored name can be edited independently by two companies. Private names/listings, edit
  sequences, retained snapshots and interleaved sessions remain isolated.
- Removal refuses active policy references and authored future initialization references. Delete/
  recreate increments the existing company's edit sequence, preventing stale version acceptance.
  Revision is an edit identity, not another game clock or timestamp.
- Area state has one Sync. Restore reconstructs the same provider geometry and rejects malformed,
  noncanonical, unknown, incompatible or out-of-range shape/identity/revision data. Failed direct
  decode retains existing state. Missing required initial-policy area rejects before commit.
- Runtime policy references validate after all services decode; pending area creation followed by
  policy selection preserves command ordering through a checkpoint. Sole-clock pause commits neither.
- Native loaded hauling is physically interrupted by editing its existing named geometry to a
  disconnected painted area. The same job, cargo, destination promise and handling progress persist.
  A painted reconnection reopens the existing route/dock owners, delivers exactly once and departs
  within the current area. Saves before/during/after interruption continue identically.
- Public-only continuous point geometry can be narrowed and expanded through the same engine
  commands, stopping actual home travel/propulsion consumption and resuming physical arrival.
  No native coordinate branching is added to the scheduler or area authority.

## One game clock

`new GameClock` still occurs only at Kernel/World.cs:84. Hosting/GameClock.cs is unchanged:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
World.Tick remains only Clock.StepIndex's read-only projection. Areas contain no game-time counter,
timer, update system or second current-time codec. Existing Game.Update remains the loop owner.

## Source anchors at the final gate

| File | SHA256 |
| --- | --- |
| Content/SpatialRegionDef.cs |9363C6588A22B2FD69A58CD543B0ECD622A332C4831530320ACA26BF037FE071|
| Standard/OperatingAreas.cs |8631C6B6440BA826C4352594938D79CE616E6B7EC84471E91BC638BEE6F0417B|
| Standard/WorkPolicies.cs |B6E2AF3505689EFD180E51BFA425F0777D28D43387FB7117995B69B78E966107|
| RouteOperatingAreaTests.cs |F647CE91F055B4E98B471F1883E0FFFB0F7229606A6F53E8FDEEC0441912C4C2|
| ExtensionProof/PointRegionProof.cs |3751A2DC2C1DD1B48C6A51497F68FE612A7DB662AA7AC044A7219B906905FBA3|

## Not proven / still open

This proves focused editable areas for existing worker-based work/hauling/home/dock and native/public
region providers. Fixed authored target-region prerequisites deliberately remain content conditions,
not editable player policies. Additional provider matrices, layer-aware/3D region tools, moving/hull
geometry, preferred routes, congestion, future road/traffic/fuel reservations, snapshot-area deltas,
arbitrary provider global rollback, replay/lockstep/fuzz/soak/allocation and full P9/P1–P12 remain open.
No Oil/Tycoon/host/demo/platform/Draw changes, legacy reader, parallel containment algorithm or extra
clock/scheduler/route/stock authority. The full implementation goal remains active and incomplete.
