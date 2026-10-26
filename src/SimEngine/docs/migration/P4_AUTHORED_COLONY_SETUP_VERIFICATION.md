# P4-I6 authored colony startup and rectangular layer setup — verification

Date:2026-10-08. Canonical repository C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
Scoped implementation only; full P1-P12 and active goal remain unfinished. No commit/push.

## Original owners and direct migration

Original WorldSetupModule/ProfileDef/WorldSetupStage/DependencyGraph and EntitySetupDef remain
the sole initialization pipeline. Entity placement/creation/home relations still use actual spatial
placement/World.Spawn/IWorker owners; transient setup links are not saved as another entity registry.
Original TileLayerSetupDef now stores one explicit TileRect. Required positive dimensions and
nonnegative origins validate with wide arithmetic against the selected authored TileSpaceDef.
No framework map/player/area cap or dimension default added. Each region is completely checked
through native MapLayer.CheckSample before its row-major MapLayer.SetSample writes. Original
layer ownership/value interpretation/resource callbacks/versioning/save codecs remain authoritative.
This is per-region preflight, NOT global startup rollback or arbitrary-provider atomicity.

Deleted compiled ColonyStart and SpawnWorker. New actual colony-master.json owns18 module
declarations and default document selection; world-setup selects colony-startup.json. Its12 stable
entries author lake11x11, dependent island3x3 and10 entities with explicit two-company placements
and dependency-bound worker homes. The two-company48x32 layout is game content, not a framework
size constraint. Actual scenario explicitly selects profile colony.start. Factory assembles authored
content and caller-supplied optional documents only; no implicit compiled startup/default fragments.
Four native setup assets (survey, sensor, layered navigation, occlusion) directly migrate22 existing
tile entries to explicit1x1 dimensions. No reader for the old shape or compatibility fallback.

Original declared independent ColonyModule navigation/placement providers remain compiled engine
extension rules; fully authored policies are still a separate open migration. Older custom colony
scenarios must explicitly select/author appropriate startup, not receive an implicit default routine.
TestLedger remains only explicit older fault instrumentation; no default monetary provider is restored.
No Tycoon business source/Oil/sibling/host/demo/Draw/platform changes.
Build2.275.0; save140/persistent codecs/sole original GameClock unchanged.

## Every build and runtime attempt

ONE build: `dotnet build Beep.SimEngine.slnx --no-restore`.
FIRST attempt exit0; reported8.66seconds;0 warnings/0 errors; tool wall9.3467019seconds.
No live process/completion poll, no second build or intermediate compile failure.

ONE runtime:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Authored colony setup: rectangular layers, stable homes and physical saved work"`.
FIRST attempt exit0;81 passed/0 failed/0 thrown or aborted suites; reported1512milliseconds,
tool wall3.1627066seconds. No test/runtime repair or rerun. Max2000 Updates per physical stage,
with public work order/job/blocker/loaded-cargo/clock diagnostics on stage exhaustion. Runtime
wall duration is not a CPU/allocation/stress budget or complete release gate.

140 mechanical metadata paths (EngineInfo +139 older test identities) compile-only. One older
NativeSurveyTests.Factory made internal for pure authored content assembly; its Run/helpers and
old suite never invoked. Four old JSON fixture migrations compile/content-assembly only except
the native survey asset sampled by this NEW case. No old/family/full/platform/stale runtime.
Code/public proof/tests/JSON frozen immediately after final green; docs/evidence only afterward.
One documentation patch verification refused duplicate file targets before writing anything;
split/rebuilt unique-target documentation patch applied. No hidden build/runtime attempt.

## Observed new public-command proof

- Default factory initializes exact10 entities with clock0, explicit selected WorldSetup, no Tycoon.
  All six workers expose actual same-company depot homes through detached WorkView facts.
- Complete121-cell lake validates112 ones and9 island zeros; neighboring column stays zero.
  Initial save/load hash equal without executing startup twice.
- Reversing setup/profile declarations preserves entity identities/prototypes/owners/positions,
  homes and dependent layer samples through the existing graph order. Different authored packs
  compare actual initial projections, NOT equal content/hash identity across changed documents.
- JSON rectangle extent edit changes actual samples to1/1/0; JSON truck position edit moves it
  to10,6 in the new session, never the unchanged original. No compiled substitute or live repair.
- Actual native2x2 byte42, signed integer-7 and private fixed quantity31 populate all four cells.
  Owner read succeeds; other seat gets Unavailable and cleared destination; native initial save
  retains hash and clock0 through the original map codecs.
- Strict missing/zero/negative/overflow/out-of-bounds rectangle, missing layer, cyclic dependency,
  absent home dependency, foreign home, unseated company, placement collision, missing selected
  profile, fractional byte and negative resource value all reject with ContentException.
- Normal player construction preview is pure. Normal commands create an actual tower project and
  queue a10steel machine recipe, not instantaneous structure/machine output. Loaded hauling,
  movement and productive elapsed activity nodes are observed; a partial physical save restores
  the same state without doubling startup. Saved twins hash equal after every subsequent Update.
- Sole clock pause stops all physical progress. Native construction/manufacturing complete with
  real material use:50 opening steel minus30 tower minus10 truck leaves10. Other company keeps
  its50; isolated inactive initialized session hash unchanged. Final save/load equal and original
  lake/island unchanged by normal updates (startup is not a gameplay update routine).
- Driver uses normal commands/previews and public detached snapshots/views/clock/save/hash;
  readonly public observer inventory inspection is used for conservation assertions. No World,
  live capability writes, stock grants, private reflection, state repair or copied scheduler logic.

## Remaining scope and continuation

Navigation/placement policy JSON migration and explicit setup authoring for older custom colony
scenarios remain open. Mandatory modes/native default game configuration, general startup providers,
global preflight/atomic rollback/purity, hostile-provider fault/corruption coverage, huge-map startup
cost, arbitrary-space region content, company templates, all baseline regressions and complete
mod/industrial/Oil/replay/lockstep/soak/allocation acceptance are not verified by this slice.
Old gates remain historical/not rerun2.275.0. No full P4/P6/P1-P12 completion claim.

Lifecycle skill kept this as a designed bounded slice with original-owner migration, one focused
gate, immediate source freeze and evidence/tracker handoff. Publication stays deferred until FULL
goal AND plan are finished and verified; preserve all unrelated dirty work.
[Stage](../../plans/enhancements/P4_AUTHORED_COLONY_SETUP_DESIGN.md),
[master tracker](../../plans/enhancements/MASTER_TRACKER.md),
[source manifest](P4_AUTHORED_COLONY_SETUP_SOURCE_MANIFEST.json).

## Scoped final source checks

Inventory769 selected canonical paths/162 Markdown/2290 local links/0 broken/0 duplicate paths.
Actual git diff --check exit0 with31 existing LF/CRLF warnings. Sole GameClock SHA256 unchanged:
339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31.
Final read-only rehash/18 phase-row/five-column shape checks follow the last documentation edit;
their actual results are appended to the excluded self-manifest, not inferred from prior slices.

Selected canonical inventory, local Markdown references, source SHA256, phase-row shape and actual
git diff --check results are recorded in the source manifest. Its own hash is excluded to avoid
self-reference. These read-only checks are not runtime regression or release acceptance.
