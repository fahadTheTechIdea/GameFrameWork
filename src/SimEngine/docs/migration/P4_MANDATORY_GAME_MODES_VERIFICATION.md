# P4-I8 mandatory authored game modes — verification

Date:2026-10-08. Canonical repository C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
Scoped implementation verified; full P1-P12 active. No commit or push.

## Original responsibility and direct migration

Original ScenarioDef requires stable game_mode and explicit object mode_overrides, including {}.
Required native GameModeDef resolution and unconditional immutable configuration precede composition.
Original GameModeConfiguration rejects null identity/capability inputs; no optional-mode protocol,
inferred genre, fallback loader, second configuration store, service, clock or scheduler.
World documentation reflects required configuration. Build2.277.0 rejects earlier semantic identity;
save140 wire fields and persistent codecs unchanged, no compatibility reader.

All25 actual embedded JSON scenario documents now explicitly select profiles/overrides. Missing
profiles are authored per-game resource-only presets in the same owned document. Existing profiles
are preserved. Obsolete unconfigured scenario deleted, older negative probes directly updated.
Shared TestGame.Defs explicitly authors test.resource-only. Other older inline C# scenarios remain
an explicit unfinished authoring migration; strict reads reject them, never infer a usable profile.

Canonical game-mode-properties.json retains exactly eight original business schemas/defaults.
Three unrelated proof properties moved to game-mode-proof-properties.json, not copied. The original
mode/business proof manifests and three pure factories explicitly load the new proof document.
Direct old numeric/text property-index probes address their new document; compile-only, not old runs.
Actual colony master owns canonical schemas and colony-game-modes.json; colony.survival authors
eight false settings, colony.resource-defaults authors {} and uses explicit schema defaults.
TycoonModule code is available and master-declared (20 modules), but not selected/composed by default
scenario. Engine property-capability registration stays strict even when false. No Tycoon business
implementation, pricing, account, market, supplier or sales behavior changed.
Hosts/demo/Oil/sibling/Draw/platform code untouched. No framework world-size restriction.

## Every build and runtime attempt

ONE build: `dotnet build Beep.SimEngine.slnx --no-restore`.
FIRST attempt exit0;0 warnings/0 errors; reported12.05seconds. Initial tool wall10.0146606seconds,
live session25615, one completion poll tool wall0.0000095seconds. One process, not two builds.

ONE runtime:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Mandatory modes: explicit survival configuration and resource-only physical play"`.
FIRST attempt exit0;91 passed/0 failed/0 thrown or aborted suites; reported1772milliseconds,
tool wall3.4265349seconds. No runtime repairs or reruns. Every physical stage max2000 Updates,
with public configuration/clock/production/queue/order/job/loaded-cargo/blocker snapshot on exhaustion.
Timing is not a CPU/allocation acceptance budget.

142 strict identity paths (EngineInfo+141 older fixture files) mechanically migrated; compile-only.
Older metadata/schema/index/factory migrations compile-only. Two existing pure assembly factories
are reused for authored metadata/native empty-schema setup, not their prior test Run logic.
No old/native survey/game-mode/colony/family/full/platform/stale runtime. All code/tests/public proof/
JSON frozen immediately after final green; only plans/docs/evidence/source manifest afterward.
Readonly inspection had a PowerShell foreach-to-pipe parse error and two guessed missing file paths;
corrected read-only searches followed. Identity JSON capture was truncated once after successful new
test creation; a compact per-file capture was used before applying identity migration. None caused a
build/runtime attempt or source fallback.

## Observed new public-player proof

- Menu, scenario profile and empty-request preview alias the sole frozen game configuration; preview
  does not mutate hash or spend time. Initialize clock0. Eight monetary flags are explicit profile
  false, no unrelated proof properties. Optional Tycoon is unselected; public BusinessView absent.
- Authored {} profile uses actual schema defaults/provenance. Equal scenario/request false retains
  distinct provenance; equal values with different explicit request intent change authoritative hash.
  Request list mutation cannot change retained game/preview; immutable property list refuses mutation.
  Captured request save/restore preserves identity and provenance.
- Missing/null/empty mode and missing/null/array overrides refuse both engine preview and creation;
  malformed metadata refuses menu enumeration. Unknown mode refuses resolved preview/creation.
  Enabled money without selected owner refuses before the counted composition callback. Unknown
  property and incoherent income-on never acquire fabricated settings or auto-enable prerequisites.
  Nullable capability-validation/identity bypasses now throw, not silently skip.
- Actual native survey assembly selects an authored empty physical preset. Zero property schemas
  produce zero configured properties; money is typed Unavailable, not null configuration or invented
  false. Wrong-kind access refuses; explicit empty config participates in actual save/hash protocol.
- Ordinary commands enqueue two real ten-steel truck batches and a thirty-steel tower. No outputs
  or finished building appear instantly. Foreign cancellation refuses. Actual positive production,
  physical movement, loaded delivery and machine work precede completion. Saved partial twins remain
  identical after every Update; one pause freezes all work. First manufactured machine completes
  while second remains queued; final two batches/tower consume exactly50 opening steel.
- A third batch waits with typed material blocker/zero progress and retained intent. No stock/output
  is invented even with all monetary flags off. Saved shortage roundtrips. An untouched concurrent
  session retains its hash, and a fresh session retains original startup/configuration hash.

## Open scope and next action

This closes mandatory native scenario selection, actual JSON fixture authoring and default colony
configuration only. Remaining older inline scenario migration/custom startup/rule authoring, global
immutable-authoring lifecycle/provider purity/hostile mods, advanced monetary activation, module
acceptance, oil consumers, replay/lockstep/fault breadth/fuzz/soak/CPU/allocation and full P4/P6/P1-P12
remain OPEN. No broad regression claim. Prior checkpoint counts are historical NOT rerun on2.277.0.
Next directly migrate remaining inline scenarios into explicit owned authored profiles, preferably
master-listed JSON documents, preserving all existing engine owners and using a distinct bounded gate.

## Source-only checks

Scoped799 selected canonical paths/166 Markdown/2320 local links/0 broken/0 duplicate paths.
Actual git diff --check exit0,31 existing LF/CRLF warnings; sole GameClock SHA unchanged.
Final readonly SHA and18 five-column phase-row checks recorded in self-excluded source manifest.

Final selected-path hashes, local Markdown links, duplicate paths,18 five-column phase-row shape,
actual git diff --check and unchanged original GameClock hash are recorded in
[source manifest](P4_MANDATORY_GAME_MODES_SOURCE_MANIFEST.json). They are not runtime acceptance.
[Design](../../plans/enhancements/P4_MANDATORY_GAME_MODES_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md).
