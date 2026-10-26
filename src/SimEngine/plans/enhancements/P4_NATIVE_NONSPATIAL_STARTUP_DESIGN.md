# P4 — Native nonspatial startup ownership

Historical checkpoint; not rerun after P4-N3. [Sole native point startup](P4_NATIVE_POINT_STARTUP_DESIGN.md) now removes the remaining duplicate point executor. Dated limits below are historical; full plan remains open.

Status: scoped implementation and focused verification complete (72 checks); full P1–P12 remain open. See [verification](../../docs/migration/P4_NATIVE_NONSPATIAL_STARTUP_VERIFICATION.md).

## Intent and single owner

Remove `PointStartDef` registrations and `PointStart` world-generation stages from the independent local-work, process, and resource providers. Providers declare their capabilities, not a second startup executor. `WorldSetupModule`, `WorldSetupProfileDef`, `EntitySetupDef`, and `WorldSetupStage` remain the only startup owners for these fixtures. No compatibility section, fallback spawn path, generated default profile, or new clock is introduced.

## Authored migration

1. Replace `work_starts`, `process_starts`, and `resource_starts` with native `entity_setups`. Every entry explicitly declares stable `id`, `after`, `entity`, `company`, `placement:null`, and `home:null`.
2. Author one explicit selected `world_setup_profiles` definition per scenario. Combined automation, inventory-signal, transformer, gate, and purchase fixtures select explicit profiles containing their actual base and additional entries; unused profiles do not spawn entities.
3. Register/select `sim.world-setup` in masters, game definitions, and scenarios. Preserve other settings when editing knowledge visibility or disabling optional economics.
4. Update authoring-only fault fixtures that add startup entities to link the same native graph. They must not write live world state to simulate startup.
5. Keep all initial stock creation, timed work, transformation queues, cancellation, and saved state in their existing engine contracts. Optional Tycoon fixtures receive startup/composition edits only, no business behavior changes and no runtime validation in this slice.

## Focused acceptance

One new bounded exact case exercises three separate native nonspatial sessions. It observes public immutable entity/inspection/work/process facts and sends ordinary player commands. It proves exact authored opening entities and stock, no geometry/money dependency, pure previews, positive-duration completion, interleaved saved twins, no startup re-execution on load, and replay identity. Native missing references, cycles, invalid presence/placement and obsolete startup sections must refuse before provider initialization. Nonseated company admission occurs in the existing generation stage after system initialization and refuses before that entry spawns; this slice preserves, and does not overclaim, that lifecycle boundary. Timed work spends authored inputs on its first productive update, not on mere accepted intent.

Compile affected source together. Run only the new exact case with a 15-second process bound and 3-second termination bound; repair/rerun that same case if needed. Existing affected cases are compiled, not rerun, and must not be reported as newly verified.

## Remaining boundaries

Spatial `point_starts` migration is separate: point presence requires explicit native placement support. This stage neither fabricates null placement for spatial entities nor claims all old proof startup is removed. No Oil consumer, platform adapter, Draw implementation, second scheduler, or engine size limit is introduced. Full regression, broader plan completion, and publication remain pending.

Commit and push only after the full goal AND plan are genuinely finished and verified, as requested by the user.
