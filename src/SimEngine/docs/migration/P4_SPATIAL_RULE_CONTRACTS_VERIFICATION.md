# P4-G34 scoped verification — 2026-10-09

Core beep-simengine-2.371.0; save154/replay1/map6/mover4 unchanged.
Full P1-P12 and active goal remain OPEN. Nothing staged, committed or pushed.

## Actual engine ownership

Removed TileNavigationRuleDef and ConstructionPlacementRuleDef. Native definitions implement
ITileNavigationRule/IConstructionPlacementDefinition directly. Module enumeration/selection uses
these existing catalog aliases. UniformTileNavigationDeclaration, LayeredTileNavigationDeclaration
and TileConstructionPlacementDeclaration are sole native JSON/interpreter owners, reused by
independent definitions. ResolvedContent validates every loaded actual profile identity/space/
minimum, including unused content. Construction definitions may describe other spatial providers;
the selected tile module still requires its actual tile space.

NavService.Define(ITileNavigationRule) binds the original NavGrid/PathSearch/Mover. It retains the
declared immutable minimum and guards actual provider costs. NavGrid no longer clamps negatives:
zero explicitly blocks, negative/positive-below-minimum results fault, preserving admissible search.
Manual Define remains one existing extension hook, not a replacement catalog/router. Original
ConstructionService guards every actual selected policy callback. EntityObservation.ReadProvider
now optionally observes actual TileMap.Version; predicates, profiles and placement share that
existing guard, not whole-map hashing per cell, another observation class or persisted condition state.
No added route/placement authority, scheduler, clock, compatibility base, platform-size cap or business rule.

## Actual commands and results

Compilation: dotnet build Beep.SimEngine.slnx --no-restore.
Initial failed build2.31s: missing Maps import in original ConstructionService.
Second failed build5.57s: test-only unsupported JsonValue.Items iteration.
Repaired to the actual namespace and indexed JSON array API.
Clean builds4.03s and4.39s; zero warnings/errors.

ONE NEW exact case only:

    dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spatial rule contracts: independent JSON profiles drive timed colony work" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 50000

| Attempt | Passed | Failed | Aborted | Case ms | PID | Exit | Observed ms |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Initial | 2 | 1 | 0 | 221 | 36956 | 1 | 376 |
| Final same case | 193 | 0 | 0 | 8415 | 24716 | 0 | 8576 |

Initial suite exception correctly exposed the proof module inspecting selected services before
their owners composed. The proof now explicitly declares its TileNavigation/TileConstructionRules
requirements in the module AND master JSON. No lazy lookup, fallback service, weakened assertion
or engine ordering workaround. All processes exited with no output truncation/observer error.
Runtime60s/termination5s/output50000 bounds unchanged.
183 selected inputs frozen2026-10-09 12:20:06UTC before final pass. No executable edits afterward.
Original historical player helper methods gained internal visibility only; their old Run was not called.

## Narrowly proved

Native profiles, independent shared-declaration profiles and distinct authored gradient navigation
use actual original router/placement service aliases. Normal uniform/land selection changes real
route feasibility; immutable/private motion facts are preserved. Placement preview/normal command
refusals agree and create no site. Accepted construction creates a staged project; real loaded
hauling, movement, building and exact50-minus30 material use take game time. Interrupted save/load
twins, sole-clock pause, completed saves, replay and interleaved sessions pass. Independent JSON
cost/placement edits change actual player admission without mutating earlier frozen metadata.
19 malformed selected/unused content cases refuse before Initialize.10 scoped faults prove actual
callback exceptions/native map writes/clock changes/identity allocation and negative/below-minimum
costs fail before returning facts; no continued healthy Update/hash/save.

## Still OPEN

Full provider/geometry/movement/placement matrices, hidden unrelated owner/metadata mutations,
whole-world or CLR sandboxing, lockstep/concurrency/fuzz/soak/allocation/performance, full accounting,
and all original P1-P12 broad gates. This is not all-profile/full-regression verification.
Pending G19 excluded-file scope decision remains unanswered. No Oil/Tycoon/platform/Draw runtime.
The development-lifecycle skill governed stage/owner/tracker order; repository/user focused-test
and delayed-publication rules override generic broad-test/early-commit guidance.
