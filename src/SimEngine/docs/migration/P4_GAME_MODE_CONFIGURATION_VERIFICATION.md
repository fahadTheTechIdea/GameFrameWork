# P4-I2 configuration foundation — focused evidence

Date: 2026-10-07. Runtime build: `beep-simengine-2.206.0`; save format110.
Stage: [P4-I2](../../plans/enhancements/P4_GAME_MODE_CONFIGURATION_DESIGN.md).
Status: partial stage, configuration foundation verified; not complete game-mode enforcement.

## Implemented owner path

- Existing ContentTypes/master pipeline registers `game_mode_properties` and `game_modes`.
- Native immutable definitions implement IGameModePropertyDefinition/IGameModeDefinition.
- GameModeConfiguration/IGameModeConfiguration resolves explicit property defaults → profile →
  scenario overrides in stable ordinal property order; no genre switch, second catalog or clock.
- Boolean, bounded Fix and constrained-text values refuse unavailable/wrong-kind access. Native
  declarations validate IDs, registered code capability references, prerequisites, limits and defaults.
- Existing ScenarioDef validates the complete linked configuration. World captures the same immutable
  projection before module composition; SimGame and GameFactory expose read-only configuration/metadata.
- One `SyncIdentity` checks derived mode/property/provenance/value identity without restoring a second
  mutable configuration. World.Sync checks it before InitializeSystems; World.Hash and HashParts include
  the same protocol. Save110 is a direct layout change, with no old reader.
- Master-listed proof JSON defaults all eight monetary features off for survival. A new master file
  adds a profile and property without scheduler changes. This is authoring proof, not new rule enforcement.
- 88 existing test files have142 mechanical build/save identity-line updates. Their scenarios were not run.

GameClock.cs SHA256 remains
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
One `new GameClock` exists, in World; no loop/time authority was added.

## Executed gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Game modes: authored profiles, typed overrides and deterministic resolution"
```

Initial build failed only in two new test calls to StateReader's byte-array constructor. The fixture
was corrected to use its actual `(byte[], offset, count)` signature. Rebuild succeeded:0 warnings,
0 errors,2.65s reported. There were two build invocations and exactly one new scenario invocation.
Result: **99 passed,0 failed, one suite,114ms**. No suite abort, full run, broad filter, older scenario,
business/recovery family rerun, platform or Draw gate.

The scenario exercises eight explicit survival defaults; native provider absence; immutable sharing
before composition; numeric/text/default/profile/scenario precedence and provenance; private session
isolation; save/continue; game-mode hash distinction; reverse document/key order; extra master enrollment;
direct identity mismatch fault instrumentation; unavailable/wrong-kind access; duplicate keys;
unknown IDs/capabilities/prerequisites; missing/null defaults; invalid type/range/text domains;
locked overrides; contradictory income/trading dependencies; fixed overflow; and profile-less override refusal.
Runs are bounded to four ordinary game updates, not unbounded physical acceptance loops.

## Additional user-directed work/manufacturing proof

User clarification: ordinary work/manufacturing are simulation mechanics, never Tycoon features;
monetary flags gate only optional authored costs/rewards. Settlers-style/space-colony games need no market.

```powershell
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Game modes: physical manufacturing and work without business or Tycoon"
```

The new exact scenario reuses existing public engine modules and explicitly master-listed profile/
property/process/work JSON; the harness submits player commands and observes actual owner state only.
It proves authored survival mode, no selected business module or account/value/income services,
pure work preview, six-update local resource work, actual material consumption at productive start,
timed machine manufacturing with consumed inputs/resource and entity output, and saved continuation.
Business code registration for property capability identity does not compose/select business.

The first run had25 passes/1 failure because the new fixture expected material consumption at order
queueing. Source inspection confirms existing AdvanceWork consumes only at productive advancement;
the proof was corrected to assert both queued stock and productive consumption. No runtime rule was
changed. After rebuilding, **27 passed,0 failed, one suite,310ms**; final build0 warnings/errors3.37s.
Only that failed new scenario was rerun; the earlier99-check configuration scenario was not rerun.
Across this slice: two distinct new scenarios,126 final assertions,4 builds (one compiler repair and
one test-timing repair),3 scenario invocations (one failed new-scenario run). No old/full/family gate.

This proves unpriced timed local work and machine manufacturing independence. It does not prove
disabled positive monetary-cost enforcement, physical mobile construction/hauling in this new mode
fixture, advanced business execution, or the complete industrial-colony acceptance scenario.

## Explicitly open work

- NewGameRequest overrides/capture and canonical saved request intent are not implemented.
- Existing scenarios without explicit profiles still expose null, not a fabricated survival/default
  mode. Their mandatory direct profile migration is open; this is not complete I2.
- Registered capability identity is validated. Actual enabled-provider availability and semantic
  enforcement are I3; configuration values alone do not enable/disable current work/purchase code.
- Existing BusinessModule still has its own settings/mode interpretation. I3 must remove overlapping
  enablement and decouple account currency/provenance from compulsory valuation.
- Unpriced physical work/manufacturing independence is proven by the additional27-check scenario.
  Disabled positive monetary-cost construction/manufacture, priced-supplier refusal and classified
  monetary work/reward/running gates are not proven here.
- Earlier H2 account2/view2/primary-income scaffolding compiles in this build, but resource commits
  still do not invoke settlement. No actual gold income/budget or advanced Tycoon proof is inferred.
- Lockstep/request/replay tests, independent executable property/rule proof, provider purity/fault
  matrix, broad durability/stress/allocations and final framework acceptance remain open.
- H1's72-check/account1/view1 report is historical, not a runtime test of current H2 source.

Engine/ExtensionProof/Testing/Tests compile. OilFieldDays and all platform/host/demo sources are unchanged
by this slice. The full goal remains active. The adjacent source manifest is a scoped hash inventory,
not a full-repository cleanliness or full-regression claim.
