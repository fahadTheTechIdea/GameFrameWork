# P4-I7 authored colony routing and construction rules — verification

Date:2026-10-08. Canonical repository C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
Scoped verified migration only; full P1-P12 goal remains active. No commit/push.

## Original responsibility and direct migration

Deleted ColonyModule land/lane lambdas and entire ColonyPlacementRule class. Actual
colony-navigation.json belongs to original TileNavigationModule and authors native layered routing:
land base10, water>=1 blocked; lane base0, public colony.lanes[1,2) AND water[0,1) cost10.
Original NavService/PathSearch/Mover own search, occupancy and physical timing, not a new router/cache.
Authored startup adds stable lane entry48x1 at row9 through the same WorldSetup pipeline before
dependent depots/entities. Actual colony.json adds public byte lane metadata; the48x32 scenario is
authored game size, not a framework limit. Actual colony-master.json now lists19 module declarations
and colony-placement.json under its new optional native owner. No compiled default route fallback.

Original LayeredTileNavigationCondition directly renamed/extracted to TileLayerBandCondition,
implementing ITileLayerCondition. Navigation and native placement use this SAME evaluator/parser/
resolver/public same-space native layer validation and inclusive-minimum/exclusive-maximum bounds.
No legacy alias, value interpretation copy, layer cache or persistent condition state.

New optional TileConstructionRulesModule requires existing ConstructionModule, requires explicit
selected settings.profile and actual matching TileMap, and binds one selected definition through
the original IConstructionPlacementRule. IConstructionPlacementDefinition/ConstructionPlacementRuleDef
allow declared extension definitions; native TileConstructionPlacementDef contains readonly authored
rule entries, not another placement manager. Sort rules descending priority then stable ID; test all
actual target footprint cells row-major using native samples and return first matching authored Code.
Original ConstructionService/ConstructionView/normal commands remain the placement/admission owner.
No system/scheduler/clock/codecs or command added. Native rule profile [] means explicit absence of
extra exclusions, not a missing-profile fallback. Unselected module has no implicit colony rule.

No ConstructionService source behavior change or parallel construction pipeline. Other external
declared IConstructionPlacementRule providers remain existing extension contracts, not compatibility
wrappers for the deleted colony class. Factory only assembles actual files/caller document edits.
Other custom colony scenarios require appropriate explicit startup/rule authoring; no old helper restored.
TestLedger still absent from default composition. Tycoon business/Oil/sibling/host/demo/Draw/platform
source untouched. Build2.276.0/save140/persistent codecs/sole original GameClock unchanged.

## Every build/runtime attempt

ONE build: `dotnet build Beep.SimEngine.slnx --no-restore`.
FIRST attempt exit0;0 warnings/0 errors; reported11.82seconds. Initial tool wall10.0044302seconds,
live session89305, completion poll tool wall0.0000118seconds. One process, not two build attempts.

ONE runtime:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Authored colony rules: terrain admission, physical routing and saved construction"`.
FIRST attempt exit0;53 passed/0 failed/0 thrown or aborted suites; reported1626milliseconds,
tool wall3.4692283seconds. No runtime fixes/reruns. Max2000 Updates per physical stage and public
work-order/job/blocker/loaded-cargo/clock diagnostics on exhaustion. Timing is not CPU/allocation proof.

141 metadata paths (EngineInfo +140 older strict identities) mechanically migrated and compile-only.
No old/native survey/colony setup/family/full/platform/stale runtime. Existing public pure content
assembly is reused, not previous test Run logic. One pre-build proof correction changed invented
CommandResult.Reason to its inspected actual Refusal field; no failed build/run. All code/public
proof/tests/JSON frozen immediately after final green; only docs/evidence/manifest changed afterward.
One documentation patch verification failed before writes because historical header hunks were
ordered behind their following text; reordered non-overlapping documentation hunks applied.
No engine/test/content change or extra build/runtime resulted.

## Observed new normal-player proof

- Default master selects optional native tile construction policy; Initialize clock0. Tower origin33,4
  is dry but its2x2 footprint intersects actual water; authored Code rejects. Pure preview and normal
  command return same refusal, no fault or physical project. Native truck water destination refuses
  core.unreachable with same pure preview/command parity through original routing.
- JSON refusal changes to terrain.wet in actual preview. Raising authored threshold to2 permits
  water-value1 construction, but command creates only a real pending site, never instant finished
  inaccessible tower. Separate JSON navigation threshold edit permits normal water-route preview.
  Admission is not a promise of later worker/resource/traffic feasibility.
- Equal-priority authored exclusions choose stable ID a rather than earlier declaration z; higher
  priority z then determines actual refusal. Explicit profile [] permits; unselected optional module
  also permits through unchanged generic construction, without compiled fallback.
- Native lane-only mover accepts authored row9 route and refuses off-lane row11. Moving lane startup
  samples and initial vehicle intent to row11 removes row9 route and admits row11. Normal command
  starts real motion, not teleportation; saved moving twin hashes equal at each subsequent Update,
  and actual vehicle reaches15,11 only after positive game time. No raw provider/spawn/move writes.
- Strict missing rules/empty conditions/missing layer/empty band/fractional byte/missing bound/
  duplicate rule ID/duplicate layer/empty refusal/wrong space/missing module settings/missing selected
  profile/private public-rule terrain all throw ContentException. Private terrain fixture is rejected
  by native composition; this is not an assertion about which of its shared guards executes first.
- Dry build preview is pure; normal command creates actual site, not instant tower. Foreign
  cancellation refuses ownership. Actual loaded hauling and productive activity work precede saved
  partial state; identical saved twins continue after each Update. Sole clock pause stops physical
  progression. Native staged tower completes with50 opening steel minus30 real materials =20 left.
  Final roundtrip equal; separate idle session remains unchanged despite shared readonly rule content.
- New driver uses normal player commands/previews and public detached seat views/snapshots/clock/
  save/hash/material facts only. No World, live capability writes, privileged reflection, grants,
  state repair or copied pathfinder/scheduler/gameplay execution.

## Remaining scope and lifecycle handoff

This closes default colony compiled navigation/placement policy authoring, not every sample gameplay
provider or all custom scenarios. Other old custom startup/monetary/provider migrations, mandatory
game modes, arbitrary-space/private-knowledge rule geometry, condition-authoring lifecycle sealing,
hostile provider/global purity/fault/corruption/atomic rollback, dynamic congestion/layer-edit behavior,
huge-map startup cost, all baseline regressions, full mod/Oil/industrial/replay/lockstep/soak/allocation
acceptance remain OPEN. Shared public condition Resolve/Validate API is trusted authoring, not a
hostile-mod sandbox or a proof of global immutability. Previous gates historical/not rerun2.276.0.

Lifecycle skill kept design, original-owner implementation, one bounded focused gate, source freeze
and tracker/evidence handoff aligned. No broad regression performed; full goal/plan completion is
not claimed. Commit/push only after FULL verified completion, preserving unrelated dirty work.
[Stage](../../plans/enhancements/P4_AUTHORED_COLONY_RULES_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[source manifest](P4_AUTHORED_COLONY_RULES_SOURCE_MANIFEST.json).

## Scoped final source checks

Scoped777 canonical selected paths/164 Markdown/2308 local links/0 broken/0 duplicate paths.
Actual git diff --check exit0 with31 existing LF/CRLF warnings. Sole GameClock SHA unchanged:
339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31.
Final read-only rehash/18 five-column phase-row checks occur after the final documentation edit;
actual results append to excluded self-manifest. No older proof/runtime is repeated or inferred.

Final selected source SHA/local Markdown links/phase rows and actual git diff --check recorded in
the self-hash-excluded manifest. These source-only checks are not runtime/regression acceptance.
