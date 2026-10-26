# P3-S10 — provider-owned production rollout

## Problem and approved scope

`ProductionSystem.Deliver/RolloutTile` owns tile rings, TileMap/NavService lookup and a vehicle-only
result. That contradicts optional space and capability-based entities. P3 removes this dependency;
P8 still owns the later single transformer/resource/WIP migration, not a parallel processor now.

## Sole owners and API

- Add optional `ISpatialRollout : ISpatialPlacement` to the selected spatial authority. Pure
  `CheckRolloutDefinition(World, EntityDef producer, EntityDef output)` validates provider requirements;
  `FindRollout(World, Entity producer, EntityDef output, out SpatialPlacement)` selects current legal
  output placement. The provider owns geometry and rollout policy; it stores no extra entity position.
- `EntityRollout` is the reusable, stateless current-session boundary. A non-spatial result needs no
  provider, coordinate or invented origin. Spatial output requires compatible authored source/target
  presence and the selected rollout provider. Returned intents pass the existing placement check.
- Move the single deterministic tile ring search into TileMap. Mobile tile outputs require their
  actual authored navigation contract/class; stationary outputs use their target footprint. No kind
  check, six-tile cap, concrete Mover requirement or default movement class survives.
- The independent point provider authors its positive rollout offset in JSON and can block output on
  its actual saved access state. It implements the same contracts without tile types/services.
- Replace RecipeDef.Vehicle and JSON `vehicle` with `EntityOutput` / `entity_output`, rename the event,
  notification and view query in place. No old field, wrapper, event alias or second content loader.
- Existing ProductionSystem/Producer remain the only queue/progress loop until P8 replaces them.
  Validate rollout capabilities before system initialization using existing ICompositionValidator;
  extend World.FinishComposition to validate declared systems after service validators.
- Enqueue and execution check existing access rules for both recipe and output. Queue admission checks
  static compatibility, not temporary congestion. Completion waits intact on typed rollout blockers.
  Spawn/initialize the entity before adding item outputs, so initialization failure cannot publish items
  without its entity. P7/P8 still own comprehensive resource/WIP transaction and cancellation accounting.
- WorkView publishes detached output identity and engine-calculated rollout previews. Hosts neither
  reproduce geometry nor manufacture outputs. Entity.Update still settles every newborn once.

## Files and execution

Kernel/SpatialPlacement + World, Map/TileMap, Standard/Production + StandardModule,
Views/WorkView, authored proof JSON and affected fixture transforms. Design → implementation →
two exact tests → current evidence/master/README. Preserve all unrelated dirty work.

## Verification

Only two exact bounded suites: non-tile timed queued production, blocked output, saved twin and
non-spatial output; tile mobile/stationary rollout, pure previews, invalid provider/content, sequential
production and initialization failure without partial item output. Build the standalone solution.
No broad/full regression, oil, Tycoon, rendering or platform work is authorized here.

## Remaining gates

The transformer manager, atomic resource tokens/WIP, generic authored construction/activity sites,
infrastructure/salvage, bounded saved rollout searches on huge blocked worlds, comprehensive lifecycle
and full stage regression remain open. No compatibility layer or whole-P3/P8 completion claim.
