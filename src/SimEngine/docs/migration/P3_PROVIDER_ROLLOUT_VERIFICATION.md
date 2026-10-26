# P3-S10 provider-owned production rollout — scoped verification

Date: 2026-10-04. Working-tree source, engine **2.21.0 / save format 11**.

## Commands and actual scope

```powershell
dotnet build Beep.SimEngine.slnx --no-restore -v:q
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Provider rollout: timed non-tile outputs, congestion and non-spatial saves"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Provider rollout: tile result contracts, sequential output and fault isolation"
```

Clean standalone solution build, zero warnings/errors. **41 + 25 = 66 checks / two exact suites**,
zero failures/aborted suites (109 ms and 202 ms respectively on final 2.21.0 source). Only those same
suites were rerun during this slice. An initial fixture
compile typo was corrected before executing either suite; early stale-binary attempts matched zero
suites and are not test evidence. No broad/full filter, previous family, oil, Tycoon or platform ran.

## Source ownership changes

- RecipeDef now has EntityOutput from strict JSON `entity_output`; no Vehicle property, kind gate,
  old vehicle field, RolloutTile wrapper or default navigation class remains. Existing queue/state
  owners stay in place until P8 replaces them with the sole transformer path.
- Optional ISpatialRollout extends ISpatialPlacement on the selected space. EntityRollout checks
  current entity/definition identity, optional space requirements and returned legal intents. It owns
  no mutable resource, reservation, output, position or queue state.
- TileMap owns the single stable bottom/right/top/left ring search. Mobile outputs use actual authored
  navigation; stationary outputs use authored occupancy/footprints. There is no six-tile/world-size cap.
- Point-space proof authors a required positive rollout offset through the sole master manifest.
  Its own access state blocks current release without changing static definition compatibility.
- Existing composition validation visits service then system validators before Initialize, deduplicating
  provider instances across aliases. Production's validator checks selected output requirements.
- Enqueue and execution read the existing content access policy for both recipe and result. This
  source path is implemented; permission changes during manufacturing were not independently tested.
- WorkView exposes RecipeEntityOutput, immutable production result identity and PreviewEntityOutput.
  Private producer requests refuse before geometry disclosure; no renderer or host placement rule.
  Feature schema 2 records the changed view contract.
- EntityProduced / sim.entity-produced and note.entity-produced replace vehicle-only names, without aliases.
  Engine build identity changes; persisted layouts and save format 11 remain unchanged. Exact build
  matching continues to reject old saves/peers; no compatibility reader was added.

## Checks establish

- Non-tile generic entities manufacture over all six authored work ticks, not by instant spawn.
  Inputs are consumed once per batch. A closed provider retains progress and its queue head, emits no
  entity/byproduct, and exposes Codes.Blocked with no invented placement. Queue preview may admit a
  persistent request despite current congestion; neither preview changes hash.
- Blocked saves restore actual access/progress/queue/inventory. Opening access releases one result and
  one byproduct; the second entry completes its own work. Saved twins and accounting match throughout.
- New presence uses the legal rollout position, not a prototype coordinate. Current/previous position
  and entity-owned settled state are consistent. No TileMap/NavService is composed for that scenario.
- A genuinely non-spatial producer/result runs the same queue and saved continuation with Space null.
  Its preview contains no fabricated coordinate. The removed vehicle JSON field refuses strict loading.
- A spatial result with no selected compatible rollout provider fails composition before a deliberately
  failing startup callback can run. A foreign live producer refuses before provider execution.
- Ordinary tile manufacturing queues produce sequential machines, preserve worker-home policy and
  saved twins, and publish detached actual duration/result identity. Stationary structures are also
  produced through the same timed queue and claim their full authored footprint/center.
- Missing output references and missing authored mobile navigation refuse before play. An output
  initialization exception registers no entity, releases attempted occupancy and publishes no partial
  item output; it faults that session explicitly, leaving other sessions independent.

## Boundaries and open gates

All changes are inside Beep.SimEngine. The independent proof uses public engine contracts, manifest
JSON and engine-side startup/providers. Test scenarios issue commands/Update and observe facts;
framework instrumentation seeds supply and initialization faults, not production/placement decisions.
Older Capability/DurationProgress/Operation fixture transforms were migrated and compiled, not executed.

Spawn-before-items prevents the tested startup partial-output failure; it is not a general transaction
for arbitrary extension callback side effects or misbehaving inventory providers. P7/P8 retain atomic
resource tokens/WIP, cancel recovery, multi-port processing and external transformer requirements.
Full-world blocked tile rollout can still be expensive; bounded resumable search is open. Generic
construction/project authoring, infrastructure/salvage, full lifecycle/privacy/layer/replay proofs,
oil migration and the authorized full regression gate remain open. No whole-P3/P8 completion claim.

The lifecycle skill guided staged design, sole-owner/interface review and current tracker/evidence.
User/repository instructions override its generic full-regression recommendation.
