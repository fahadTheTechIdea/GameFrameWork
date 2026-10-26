# P5-A43: Optional storyteller and current visibility tests use public contracts

## 0. Problem

OptionalModuleTests assumed a removed fog module and no default visibility service, used internal World access/static clocks, and read obsolete EntityView.Tile. Canonical Oil now explicitly selects sim.knowledge-visibility and oil.sight.

## 1. Goals

Migrate the existing suite once, use actual selected modules/public views/session time/typed spatial snapshots, retain long scenario intent, and prove composition/save/read purity in focused cases.

## 2. Design

Move OptionalModuleTests into the existing non-friend public runner. Reuse canonical ClientTests.Factory. Remove the old registration, add one public registration.

Replace the obsolete default-neither-module assertion with actual composition: storyteller is optional, no legacy fog module exists, engine visibility is explicitly selected, and saved module/sight state restores. This is an explicit correction of stale architecture, not a restored alias or all-visible fallback.

Other original case names/scenarios remain:300-day storyteller and150-day resumed halves;40-day presenter; physical sight discovery by a traveling vehicle. They use session ExactTicks/GameDuration, long notification/coordinate arithmetic and TileSpatialSnapshot. Presenter reads do not invoke a time-advancing Update just to draw.

A new short viewer-read case retains actual two-company founding, pure scoped reads, saved twin identity and ordinary time advancement. It is separate evidence, not a shortened replacement for original long scenarios.

## 3. API/state

No production API, game logic, visibility rules, rates, money or protocol changes. Core2.557.0/save156/replay1 unchanged.

## 4. Files

Move original suite; old/public registrations. Add bounded short reads to same suite. Stage/evidence/current tracker and audit.

## 5. Errors

Missing founding sites are handled explicitly, not indexed. Long travel already has a simulation deadline; selected runtime cases have independent process bounds. Unsupported legacy module names are removed rather than silently mapped.

## 6. Compatibility

No fog shim, internal friend, static clock or raw entity/provider/world access. One responsible engine visibility implementation.

## 7. Verification

Public build, remaining old compile diagnostic, exact composition/save and short read/save-continuation cases. No300-day/40-day/travel/full runtime.

## 8. Risks

Long storyteller pacing, physical distant-visibility travel, all provider/private/delta/optional combinations and full old test migration remain open. Source/API compilation does not prove long behavior.

## 9. Out of scope

Production changes, adapters/platforms/Draw, Tycoon behavior, full tests and Git writes.

## 10. Status

Migration implemented; composition6/0 plus short purity19/0, focused total25/0. Old project111 compiler diagnostics across three files. Original long scenarios remain compile-only; original221/194 recorded checked/27open unchanged. [Evidence](../../docs/migration/P5_PUBLIC_OPTIONAL_VISIBILITY_MIGRATION_VERIFICATION.md).

