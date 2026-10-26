# P3-S7 authored entity states — focused verification

Date: 2026-10-04. Working-tree source, build identity 2.17.0 / save 10; not a full framework gate.

## Commands actually executed

```powershell
dotnet build Beep.SimEngine.slnx --no-restore -v:q
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Authored states: arbitrary bindings settle physical capabilities and saved twins"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Authored states: strict references, immutable bindings and isolated content"
```

Final build: zero warnings/errors. Runtime remains dependency-free netstandard2.1. Physical suite:
**35 passed, 0 failed / one suite, 144 ms**. Contract suite: **57 passed, 0 failed / one suite, 150 ms**.
Combined executed scope: **92 checks / two exact named suites**, zero failures or aborted suites.
No broad family filter or full tests ran. Other affected fixtures compile but their suites were not run.
Earlier optional-space and layer evidence is source-specific, not a current-source regression claim.

## Changes and demonstrated behavior

- EntityStateBindings strictly reads required semantic modes and resolves immutable references through
  ResolvedContent's existing EntityStateDef catalog. No state names, IDs or runtime state are fabricated.
- WorkerDef, MoverDef and ProducerDef select their definitions from JSON; generated SiteDef inherits
  the target ConstructibleDef's resolved site binding. Engine source has no conventional state-ID lookup.
- Independent non-friend worker and mobility providers use the same public reader and state-source
  contract. Public movement invokes the external provider without a scheduler change.
- All conventional IDs are removed from the physical proof content. Initialize, self-work, production,
  disabling/restarting, travel and construction settle arbitrary published state codes through Update.
- Simultaneous worker and producer contributions use authored priorities. Disabled processing wins
  over active work; idle capability contributions relinquish their state to the authored default.
- State does not bypass timed work, production or travel, nor complete structures instantly. Stages
  are bounded with tick/fault diagnostics. An in-progress site remains physical work.
- Saved active work, disabled/restarted production, motion and sites continue identically in twins.
  Existing state/progress/mobility codecs remain sole persistence/hash owners; no schema/payload is added.
- Six malformed binding cases for each of four built-in definitions refuse before module composition
  or initialization. Resolved dictionaries reject replacement and clearing. Multiple modes may
  intentionally share one definition; immutable definition sharing does not share live entity state.
- Changing a binding changes content identity: save menus report incompatibility and load refuses.

## Fixture migration and limits

Colony's primary JSON and stationary-worker fixture now explicitly author required bindings. Related
missing-mobility, salvage and external-worker transformations were migrated. Three older fixture
injections searched for construction as the first component, which became stale when tile presence
was authored; they now match the actual construction key. No compatibility parser or alternate fixture
gameplay path was introduced. Those existing suites were compiled, not executed in this slice.

The development-lifecycle skill informed the staged design, ownership review and tracker/evidence
updates. User/repository instructions limit execution to directly affected exact tests; the full gate
remains open. No OilFieldDays, Tycoon, Unity, Godot, adapters, Draw or rendering implementation changed.

P3 is still partial: non-tile execution, generalized placement/rollout/upgrades, complete optional
spatial requirement validation, lifecycle/robustness/allocation proofs and authorized broader regression
remain open. P4's JSON activity DAG and the later gameplay mechanism stages are not completed here.
