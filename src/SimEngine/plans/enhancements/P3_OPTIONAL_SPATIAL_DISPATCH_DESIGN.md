# P3-S6 — Optional spatial dispatch and stationary machines

Status: implemented and focused-verified, 2026-10-04. P3 as a whole remains incomplete.

## Problem and scope

WorkerDef required IMobility even for a stationary machine. WorkSystem and LogisticsService read
ITilePresence, TileRect and vehicle kinds directly, making a nonspatial or alternate-topology worker
unsafe to schedule. StandardModule also registered the tile router when no tile space was selected.

Replace those dependencies through the selected spatial authority, not another scheduler or position
model. Initialize → Update and entity-owned state settlement remain unchanged. Rendering, adapters,
oil, Tycoon, new transport media and the later activity DAG are not part of this slice.

## Goals and ownership

1. A nonspatial stationary machine executes an authored timed order on itself through WorkBoard.
2. Unrelated nonspatial entities never share an invented origin or receive remote instantaneous work.
3. A selected provider owns interaction reach, cost, route eligibility and physical approach.
4. Work and logistics coordinators depend on that contract, not tile coordinates or entity kinds.
5. Existing mobility state owns travel; its existing codec owns save/load/hash. No extra route ledger,
   reservation store, update loop or compatibility facade is introduced.
6. Tile mobility content validates its presence/navigation requirements before Initialize.

## Public contracts

`Kernel/SpatialInteractions.cs` declares `ISpatialInteraction`:

```csharp
bool TryEstimateCost(World world, Entity actor, Entity target, out long cost);
SpatialInteractionStatus Check(World world, Entity actor, Entity target);
SpatialInteractionStatus Approach(World world, Entity actor, Entity target);
```

Results distinguish WithinReach, Reachable, Pending, Unreachable and Unsupported. Cost is a
nonnegative deterministic provider metric, not pixels or renderer distance. Unsupported estimation
returns false; its output is not a valid cost. Pending navigation budgets are not converted into
unreachable routes. Approach requests movement and must never place the actor at the destination.

`SpatialInteraction` is the sole dispatch boundary: it verifies live session identity, recognizes
actual nonspatial self-interaction, and delegates to `world.Space`. It is stateless and creates no
parallel provider registry. Missing space/provider or incompatible endpoints refuse interaction.

TileMap implements the contract using ITilePresence and IMobility. Its one interaction-area calculation
uses the target footprint expanded/clamped to authored bounds, with wide intermediate arithmetic.
There is no vehicle-kind shortcut. Existing live movement intent is reused rather than running a
second search every work tick. Mover/Mover.Sync remain the path/progress authority.

## Coordinator migration

- WorkerDef no longer requires IMobility. Roles and authored work rate still validate.
- WorkSystem evaluates every job stop through the provider, chooses deterministic cost/identity
  ordering, advances ordinary toils, and checks reach before work/loading/unloading. Stationary work
  consumes the same WorkProgress and game time as mobile work.
- LogisticsService evaluates endpoint distance and carrier serviceability through the same provider.
  It does not fabricate transfers between nonspatial endpoints.
- TransporterDef requires IInventory and ISpatialPresence. Physical reach is decided by its selected
  provider, not a compulsory tile-movement implementation. The outlet/inlet invariant is preserved.
- StandardModule registers NavService only for a selected TileMap. Empty nonspatial jobs/production
  do not need a fake tile map/router. Existing economy requirements are not changed by this slice.
- MoverDef validates movable tile-presence contracts. NavService validates the selected space's
  movement classes and freezes registrations before Initialize; other-space definitions remain inert.

## JSON and state boundaries

No new JSON loader is added. Existing master manifest, scenario space selection, tile presence,
worker roles, orders, duration and state definitions remain authoritative. The focused proof's
nonspatial scenario omits space; its station declares a worker without a mover. The tile proof removes
only the crane's mover while keeping explicit tile presence and normal commands.

WorkBoard, Worker and mobility codecs are unchanged in layout. This slice adds no persisted mirrors.
The preceding authored-layer slice remains map schema 4. P3-S7 subsequently removes built-in
state-definition ID conventions through explicit bindings; see its separate
[design/evidence](P3_AUTHORED_ENTITY_STATES_DESIGN.md). These S6 test counts describe the earlier source.

## Verification and remaining work

- [x] Solution build: zero warnings/errors.
- [x] Exact nonspatial stationary-work suite: 18 passing checks.
- [x] Exact tile reach/travel suite: 13 passing checks.
- [x] Scope tracked in the [verification report](../../docs/migration/P3_OPTIONAL_SPACE_VERIFICATION.md).
- [x] Non-tile provider and alternate movement contract prove the same coordinators unchanged
  ([P3-S8](P3_PROVIDER_MOTION_DESIGN.md), scoped work/hauling/home/recovery, not full spatial acceptance).
- [ ] General route/placement/rollout/upgrade consumers complete their provider contracts.
- [x] Built-in capability state sources explicitly select arbitrary definition IDs through JSON (P3-S7).
- [ ] Retry/service/policy budgets complete P9's saved dispatch behavior.
- [ ] P3-wide and final framework gates run only with explicit authorization.

No broad filtered sweep or full regression ran. The earlier three spatial-layer suites remain
unexecuted; these two suites do not claim their coverage.
