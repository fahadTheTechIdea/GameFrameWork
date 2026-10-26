# P4-I9 authored reusable proof worlds — verification

Date:2026-10-08. Canonical repository C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
Scoped verified migration only; full P1-P12 goal remains active. No commit or push.

## Original responsibility and direct migration

Source inventory found16 C# files with scenario text: three reusable canonical worlds,12 valid
diagnostic fixture declarations, and one replacement-only input test. SelectedContentTests also
contains intentionally malformed inactive metadata; that negative input remains invalid.
All12 valid diagnostic declarations directly author required mode/overrides; the selected extension
references its actual required TestGame profile. Other diagnostic inputs author their own empty
profiles in their owned JSON text. No runtime/factory synthesizes a missing mode or compatibility data.

Canonical TestGame.Defs/ExtraDefs/Manifest, PointGame.Definitions/static master, CompositionProbe
Definitions are now explicitly read actual embedded JSON files. Removed original canonical C# literals.
Six assets: test/defs.json, extra/defs.json, test-master.json, points.json, point-master.json, probe.json.
Logical master paths retain original document owners. Exact native resources/roles/states/prototypes/
time/profile/settings content preserved apart from point/probe required authored mode selection.
Existing framework clocks/typed inventory/resource/physical job/port/provider owners execute rules.
Pure assembly default arguments select the explicit base file, not an inferred profile or genre.
Const-dependent TestGame.Content/PointGame.Factory/SelectedContentTests/StrictContentTests metadata
selectors migrated directly; no compatibility overload/alias/factory fixup.

No engine simulation source change: Build2.277.0/save140/actual persistent codecs/sole original
GameClock unchanged. Content fingerprint identifies authored data changes. No identity-fixture bump.
No Tycoon business implementation/Oil/sibling/host/demo/Draw/platform edits or framework size cap.
Further extraction of every dynamic diagnostic string is separate from these canonical game assets.

## Every build/runtime attempt

TWO builds of `dotnet build Beep.SimEngine.slnx --no-restore`:

1. Exit1,reported3.10seconds,tool wall3.8201368seconds,0 warnings/1 CS1736 at StrictContentTests.cs:18.
   Its default manifest argument depended on the extracted C# constant; direct pure base-file selector
   repaired. One initial repair patch used an inaccurate signature and failed verification before
   writes; actual inspected signature then applied. No engine/provider/gameplay repair.
2. Exit0,reported3.89seconds,tool wall4.5462194seconds,0 warnings/0 errors.

ONE runtime:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Authored proof scenarios: native metadata and portable timed player workflows"`.
FIRST attempt exit0;70 passed/0 failed/0 thrown or aborted suites,reported648milliseconds,
tool wall2.2105825seconds. No runtime fixes/reruns. At most1000 Updates per physical stage with
public mode/clock/entity/order/job/state/loaded-cargo/blocker diagnostic snapshot on exhaustion.
Timing is not CPU/allocation proof. All code/tests/public proof/JSON frozen after green; docs-only.
One pre-build new test correction replaced invented JsonValue.Values with actual indexed APIs;
no compile/run failure from it. Older fixture/helper metadata migrations compile-only. Existing pure
assembly helpers are reused, never their prior test Run logic. No prior/family/full/platform/stale run.

## Observed distinct normal-player proof

- Canonical reusable properties read exact authored file content; point master explicitly owns
  points.json. Tile, probe, point and mapless metadata/Initialize name actual authored profile IDs.
  Empty request aliases original immutable configuration/preview. Physical proof schemas expose
  Unavailable monetary values, never fabricated false. No selected Tycoon or public BusinessView.
- Tile Initialize clock0; ordinary Update advances tile session clock and saved twin matches. The
  tile hash/time assertion is not an isolated entity-update proof; point physical facts below are real.
  Detached JSON edits profile ID and explicit scenario selection; native preview and actual game
  change identity without mutating retained session. Removing required mode still refuses.
- Probe metadata preview invokes no composition; actual module sees the same required immutable
  configuration before composition. Callback is read-only instrumentation, not a gameplay rule.
- Point JSON authors actual initial position0. Normal inspect order does not complete immediately:
  real travel, positive incomplete machine work and elapsed time precede activity completion.
  Saves during movement and work continue identically after every Update. Sole pause freezes both.
  Retained point publication remains at original position after live movement.
- Normal route command resolves actual public resource identity, creates physical finite shipments.
  Loaded cargo exists before complete delivery. Saved cargo twins continue identically; full native
 20 supply reaches destination once, source depleted and machine empty. Idle shared-content session
  remains unchanged; completed point world roundtrips.
- Actual file-authored mapless process has no geometry requirement. Normal enqueue consumes/produces
  real native materials, cannot create instant token. Partial saved process produces one entity over
  elapsed time; remaining supply18 is exactly20 initial minus3 consumed plus1 produced. Final hash
  roundtrip preserves explicit required mode.

## Remaining scope / next action

Direct valid embedded metadata migration and canonical reusable file extraction are closed scoped.
Diagnostic input-file extraction and older custom colony startup/rules remain separate authoring
work. No broad old-fixture behavior/regression claim. Global immutable-authoring lifecycle/provider
purity/hostile mods, every provider, advanced economics/Oil/replay/lockstep/fault breadth/fuzz/soak/
CPU/allocation and full P4/P6/P1-P12 remain OPEN. Prior checkpoints historical, NOT rerun here.
Next continue the actual engine authoring/lifecycle and custom startup migration through existing
owners; do not add compatibility mode/startup helpers or mirror state into harnesses.

## Source-only checks

Scoped818 selected canonical paths/168 Markdown/2331 local links/0 broken/0 duplicate paths.
Actual git diff --check exit0,31 existing LF/CRLF warnings; sole GameClock SHA unchanged.
Final readonly SHA/unchanged covered engine-source/18 five-column phase-row checks in self-excluded manifest.

Selected file hashes/local Markdown links/duplicate paths/18 five-column tracker rows/actual
git diff --check/unchanged engine source and GameClock recorded in
[source manifest](P4_AUTHORED_PROOF_SCENARIOS_SOURCE_MANIFEST.json); not runtime acceptance.
[Design](../../plans/enhancements/P4_AUTHORED_PROOF_SCENARIOS_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md).
