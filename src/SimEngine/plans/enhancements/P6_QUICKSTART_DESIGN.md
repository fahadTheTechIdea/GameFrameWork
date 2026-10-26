# P6 current-contract quickstart

## Scope and cause

The shipped public API example failed to compile because QuickStart called removed World.SpawnAt.
Its world JSON still used removed map/footprint fields, omitted the required game mode, and declared
resources under the obsolete items section. The main solution did not build these sample projects.

## Single responsible owners

QuickModule declares code availability/dependencies and one GameDefinition factory. Existing
ContentSet owns explicit manifest loading/strict resolution; WorldSetupModule owns the startup DAG
and EntitySetupDef executes entity creation. No sample-specific startup system or spawning fallback.
Existing ResourceDef/UnitDef own quantities/capacity; GameClock/SimGame own Initialize and Update;
entities own their state. Host is file loading, lifecycle calls and observations only. No Draw.

## Implemented stages

1. Remove QuickStart and its hardcoded spatial spawn. Select existing access/world-setup contracts.
2. Replace obsolete JSON: explicit versioned master dependencies/documents, startup profile,
   nonspatial placement, resource-only mode, state, units, dimensional resources.
3. Reference the actual public sample module from the test project and copy its actual authored files.
   The test does not reproduce startup rules or use mutable World/internal contracts.
4. Compile the thin console host and public module; run one bounded exact lifecycle acceptance case.
5. Correct the guide and tracker; preserve open broad acceptance and publication gates.

## Verification and limits

28 focused checks pass on unchanged core2.304.0/save144/replay1. The console host compiles but was
not separately executed. No older suite, whole profile, oil migration or platform acceptance run.
This is the P6 quick setup deliverable only; full plan and goal remain active.
[Evidence](../../docs/migration/P6_QUICKSTART_VERIFICATION.md).
