# P3-S6 optional spatial dispatch — focused verification

Date: 2026-10-04. Working-tree evidence; not a full P3, all-framework or oil acceptance claim.

## Build

`dotnet build Beep.SimEngine.slnx --no-restore -v:q`

Result: zero warnings, zero errors. Runtime remains netstandard2.1 without platform references.

## Only two named suites executed

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Optional space: stationary standard work uses contracts and survives saves"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Optional space: tile reach and travel validate providers without remote instant work"
```

Latest results: 18 checks / one suite, zero failures (87 ms); 13 checks / one suite, zero failures
(152 ms). Combined unique scope: **31 checks / two suites**, zero failures. The second suite initially
found malformed Colony.Defs JSON left by the earlier spatial fixture migration: a nested duplicate
components container in the factory entry. That authored fixture was corrected in place, then the
same named suite passed. No tolerant parser or compatibility loader was added.

No full test command, broad family filter, oil runner, Tycoon, adapter, Unity or Godot project ran.
The preceding three authored-layer suites remain unexecuted. Historical test counts are not current
regression evidence.

## What the checks prove

- Standard nonspatial scenarios initialize without TileMap, NavService or invented snapshot space.
- Stationary workers without IMobility execute a player-submitted normal order on themselves.
- Work progresses over three authored ticks and publishes settled nonspatial entity facts.
- The ordinary worker/job/progress state saves, restores and continues with identical hashes.
- Unrelated nonspatial targets wait with no eligible worker; foreign-session identities refuse access.
- Stationary tile workers operate within physical reach, not at a distant endpoint.
- Tile timed work saves/restores and continues identically.
- Provider approach asks existing mobility to travel without changing the carrier's position instantly.
- One Update does not complete long travel.
- Late navigation registration, missing movement classes and missing tile presence refuse explicitly.

## Source ownership reviewed

WorkSystem and LogisticsService have no TileMap/ITilePresence/TileRect/TileCoord/vehicle-kind references.
ISpatialInteraction delegates to the selected spatial provider. TileMap alone owns tile interaction
geometry; existing IMobility owns travel state. WorkBoard/WorkProgress remain sole job/work owners.
There is no added scheduler, mirrored position, world-size rule, reservation store, platform lifecycle,
compatibility facade or duplicate provider registry. Outlet still inherits inlet.

The development-lifecycle skill informed the staged design, single-owner review and tracker update.
Repository/user instructions override its generic full-regression suggestion: that gate stays open.

## Unproven or incomplete

Authored state-definition ID selection is subsequently verified in the separate
[P3-S7 report](P3_AUTHORED_ENTITY_STATES_VERIFICATION.md); S6's two suites were not rerun for that migration.
Non-tile provider execution, general placement/rollout/upgrade contracts,
resource/unit validation, P9 saved retry/service policies, allocation/soak, all current suites,
oil parity and final framework acceptance remain open. This focused result must not close those gates.
