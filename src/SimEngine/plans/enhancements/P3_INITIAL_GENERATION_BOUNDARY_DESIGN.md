# P3-IG1: Engine-owned generation and committed-state observation

## 0. Problem statement

The migrated controller test timed out without phase diagnostics. Instrumentation showed two distinct issues: a reduced24x24 fixture did not found, and restoring the actual96x96 Frontier could not finish initialization within120seconds.

TileMap.PrepareValueWrite checks every changed sample for every viewer. SpatialLayerAccess guards each check through the selected space codec. During initial Fill this repeatedly serializes the whole growing map although no viewer has a published baseline.

A focused existing bulk-layer test also exposed a separate regression: observation hashing of a second layer traversed an in-progress first layer and was rejected as a checkpoint. The first layer's candidate is private; its original committed chunks remain authoritative until publication.

## 1. Goals

- World owns generation phase entry/exit and final startup visibility settlement.
- Generation does not advance game time or create presentation baselines prematurely.
- Runtime writes retain exact per-sample authorization and invalidation.
- Read-only observations may inspect committed chunks during Fill; normal hashing, saving, restoring and same-layer mutation still refuse.
- The original authored Frontier geometry, ordinary founding and save identity are tested through public APIs.

## 2. Design

World.GenerateNewGame stages InitializeSystems, generation in a try/finally window and StartNewGame. GameDefinition delegates to this existing World owner rather than keeping a second generation loop.

TileMap.PrepareValueWrite continues computing actual native changes and global/public counters. It defers viewer-specific sample checks only during World.Generating. StartNewGame already refreshes actual visibility through ISpatialVisibilityInvalidation after all generated values settle. The phase is transient, not serialized, and does not become another clock or gameplay state machine.

EntityObservation uses the original StateHasher with an internal IStateObservationSync purpose capability. SyncMode remains Hash and the existing bytes/FNV codec are unchanged. MapLayer.Sync permits this purpose to read its unchanged committed chunks while Fill holds its private candidate. Ordinary StateHasher, StateWriter and StateReader retain the unsettled-layer refusal. No duplicated codec, candidate publication or fabricated rollback.

ClientTests.Factory restores the original disk-authored content instead of inheriting the reduced proof fixture. The bounded demo explicitly retains its own existing small fixture. Controller diagnostics report actual public clock/controller/company/start/entity/fault facts and reject a missing founding site before a long wait.

## 3. API surface

Internal World.GenerateNewGame and Generating lifecycle facts; internal IStateObservationSync purpose. Public game/clock/command APIs unchanged. Core2.556.0/save156/replay1. No new serialized fields or compatibility reader.

## 4. Files to change

World.cs, Modules.cs, TileMap.cs, EntityObservation.cs and StateSync.cs. Build identity/current protocol expectation. Existing spatial fixture helper and bulk test class; one stale shared-observation save-format assertion. Public ClientTests/DemoTests. Stage/evidence/master/audit.

## 5. Error handling

Generation's phase clears in finally, and a throwing stage never reaches StartNewGame. Fill keeps its candidate private. Native runtime preflight still validates current counters after generator callbacks. Independently committed other-layer writes remain committed when the main Fill refuses; there is no invented rollback.

Observation retains original provider/space codecs, scalar facts, revision guards and first-fault behavior. Normal checkpoints cannot serialize an in-progress Fill.

## 6. Compatibility

No legacy clock, duplicate generation loop, hash algorithm, save reader or host-side gameplay path. Only the current protocol identity is supported.

## 7. Verification

Exact focused cases: initial generated scopes/privacy/saved continuation; original bulk generation/counter/refusal/privacy suite; native/point observation mutation/fault suite; canonical96x96 Frontier initialization/ordinary founding/save. Bounded diagnostic controller attempts remain aborted, not passes. No full/family regression.

## 8. Risks

This fixes pre-publication invalidation cost, not all runtime spatial hashing or allocation budgets. It does not verify arbitrary custom generation providers or the entire initialization failure matrix. Broad initialization/privacy/mod/performance acceptance remains open.

The Oil AutopilotSystem still schedules independently despite the existing framework PlayerControllersSystem. Its delayed first decision is not repaired by this stage. Future migration must use the existing coordinator, not add another enrollment flag or scheduler.

## 9. Out of scope

Controller enrollment fix, strategy/financial rule changes, Tycoon behavior, adapters, rendering/Draw, platform engines, full profile closure and Git writes.

## 10. Status and next execution

Generation/observation fixes implemented; final focused931/0 across four exact cases. Initial failures and three diagnostic timeouts are retained. Original221 requirements and194 recorded checks/27open remain unchanged. [Evidence](../../docs/migration/P3_INITIAL_GENERATION_BOUNDARY_VERIFICATION.md).

Controller follow-up is implemented in [P5-A41](P5_CONTROLLER_COORDINATOR_MIGRATION_DESIGN.md): Oil strategy now uses IPlayerController/IPlayerControllerDefinition and PlayerDecisionContext with master-listed profile/cadence/budget/binding JSON. AutopilotSystem's separate loop and decision_hours are removed; PlanningView retains scenario recovery rules and the original coordinator owns replies/persistence. Focused hand-over/pending saves pass; full physical strategy/baseline acceptance remains open.

