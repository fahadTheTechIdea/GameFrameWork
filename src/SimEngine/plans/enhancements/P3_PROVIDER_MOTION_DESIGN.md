# P3-S8 — provider-neutral motion and non-tile physical dispatch

Current [P9-C8h shared queues](P9_SHARED_DOCK_QUEUE_DESIGN.md):2.123.0; schemas/payloads unchanged.
Actual provider overlap serves the existing WorkBoard's physical arrived FIFO. Pure new admission
and promotion share the same candidate-specific eligibility/geometry check without another route,
clock or queue. Native/public physical continuation passes87/two exact suites, clean build. Moving
docks/hulls/traffic/fleet/deadlocks/provider/global rollback/full gates remain open.

Historical [P9-C8g physical claims](P9_DOCK_GEOMETRY_CLAIMS_DESIGN.md):2.122.0, owner schemas/codecs
unchanged. Actual ISpatialOverlap interprets detached current geometry; WorkBoard alone compares
live claim fields across facilities/companies. Public point/native physical mixed queues and saved
continuation pass71 checks/two exact suites, clean build. No new motion/claim/presence owner.
Hull/trajectory reservations, traffic/fairness/deadlocks/broader providers/full gates remain open.

Historical [P9-C8f committed itinerary recovery](P9_DOCK_REPLANNING_DESIGN.md):2.121.0/save67/work13/
work view39; motion owners/codecs unchanged.197 checks/two exact suites, clean build. The existing
WorkBoard reselects actual bay/wait/exit intentions before obsolete routes gate execution, preserving
physical origins, cargo, receipts and work. Public point/native saved continuations pass. Future
traffic/fuel/road reservations, editable geometry, arbitrary providers/full gates remain open.

Historical [P9-C8e prospective itineraries](P9_ITINERARY_ADMISSION_DESIGN.md):2.120.0/save67/mover3/
movement5/work12/logistics8,259 checks/two exact suites, clean build. Actual motion providers prove
future Exact/Interaction arrival; one A* reports geometry without another path, and TileMap shares
ordinary interaction bounds. Future traffic/fuel/road guarantees and broader providers remain open.

Historical [P9-C8d whole-route areas](P9_ROUTE_OPERATING_AREAS_DESIGN.md):2.119.0/save67/mover3,
movement5/work11/motion view4,280 checks/two exact suites and clean build. Actual optional motion/
sweep contracts gate physical automatic travel before input consumption, with no duplicate route.
Native A* detours, retained cargo/claims, ordinary local work and public point saved twins pass.
Full itinerary admission/vehicle envelopes/editable areas/provider/replay/stress/full gates stay open.

Historical native [P3-S8a route checkpoints](P3_MOTION_CHECKPOINT_DESIGN.md):2.118.0/save67/mover3.
435 checks/two exact suites, clean build. Writable route state belongs to the existing Mover;
read-only facts/one staged Sync/actual origin/post-decode geometry validation remove partial/corrupt
native route publication. Actual pending/fractional/changed-route/wide/stop/arrival continuation passes.
[Evidence](../../docs/migration/P3_MOTION_CHECKPOINT_VERIFICATION.md).55 earlier identity assertion
files and WideTime instrumentation compile only; original provider suites below are historical.
Broader independent-provider validation, full itinerary admission/vehicle envelopes/fleet/replay/stress/full P3 remain open.

## Problem and goals

`Standard/Movement.cs` enumerates IMobility (tile goals), and `WorkSystem.IdleBehaviour` reads that same
contract just to observe movement status. A selected non-tile ISpatialInteraction provider can request
motion but cannot use the existing movement phase or idle/held-worker rules without pretending to use
tile rectangles. Finish this contract boundary in place and prove ordinary timed work and hauling on
a genuinely non-tile spatial provider.

## Design and sole ownership

- `Kernel/EntityMotion.cs`: IMotion : IEntityCapability owns Status, Stop and Advance(World, TickInfo,
  Fix speedModifier). MoveStatus moves here unchanged in numeric representation. There is no separate
  motion component, coordinate system, state ledger, registry, scheduler or second game loop.
- IMobility inherits IMotion and retains only tile-specific navigation/class/goal members.
- Mover and independent tile mobility expose their existing status through IMotion. MovementSystem
  enumerates IMotion and advances each active live owner once in its declared phase. IdleBehaviour
  reads IMotion. Tile navigation, tile commands and tile rollout remain explicitly provider-specific.
- SpatialInteraction continues delegating approach/reach/cost to the sole selected ISpatialSpace.
  A non-tile module owns its presence, motion intent, route access and immutable coordinate facts.
- Entity.Update alone arbitrates and commits authored state candidates after physical movement/work.
  Existing standard work/logistics/job/reservation codecs remain authoritative.

## Non-friend proof module

Add a small fixed-point point-space provider in ExtensionProof, not in a host or harness. Its definitions,
startup entities, positions, rate and states come from a document listed in the master JSON manifest.
Presence and motion each have one Sync owner. The selected space owns a saved route-access gate;
its initial access is explicitly authored, and its public command changes that gate. No tile services,
TileCoord, TileRect, IMobility or platform SDK
are used by this provider. It executes movement through the existing MovementSystem, work through
WorkSystem, and cargo through ordinary port/transporter/logistics contracts.

## Execution and verification

1. Factor motion lifecycle out of the tile contract; compile existing providers against it.
2. Author the external non-tile module and JSON startup scenario.
3. Two exact bounded suites: travel/work/home/saved twins and physical logistics/interrupted route/cargo
   recovery/idle motion status. Query immutable facts; harnesses do not implement gameplay decisions.
4. Solution build and those two suites only. Record actual results and update the master tracker.

No full tests or broad filters are authorized. The general placement/rollout/upgrade/route-query APIs,
comprehensive layer tests, P9 dock/policy/reservation completion, P4 activity DAG, oil integration and
P3-wide acceptance remain open. No compatibility alias for the old enum namespace is retained.

## Current result

- [x] Single motion lifecycle factored in place; existing tile providers compile through inheritance.
- [x] Non-friend JSON-authored point provider executes physical work and logistics with no tile contracts.
- [x] Two exact suites: 52 checks, zero failures; solution build has zero warnings/errors.
- [x] Source identity 2.19.0 / format 11, [scoped evidence](../../docs/migration/P3_PROVIDER_MOTION_VERIFICATION.md)
  and master tracker updated. Earlier full/source-family tests were not rerun.
- [ ] General spatial placement/rollout/upgrade/query mechanisms and P3 acceptance remain open.
