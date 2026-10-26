# P4-C3a selected-content verification

Date: 2026-10-04. Working-tree build `beep-simengine-2.23.0`; save format 11 unchanged.

## Implemented responsibilities

- GameFactory removes AllModuleIds/all-module pre-resolution. NewGame and ScenarioTime select their
  authored scenario before resolving only its requested dependency closure. Selected extension requests
  use captured module identities; the declaring scenario owner must be present.
- ContentSet remains the only manifest/raw loader. It freezes raw JSON before projection and uses the
  same ResolvedContent.ReadDefinition as actual resolution. Its transient engine-only projections do
  not create a menu resolver, retained typed catalog or second scenario parser.
- Scenarios exposes detached immutable ScenarioInfo intent and nested module/menu lists, without
  resolving time/space/gameplay. InputMap reads the core declaration only; existing clock/quit UI default
  is unchanged. Fully selected scenarios still go through normal strict reference validation.
- ResolvedContent still owns the canonical selected definitions/indexes/cache/fingerprints. Actual
  worlds and saves consume those definitions, not the transient discovery projections.
- DiscoveryProbe is compiled as an independent public-contract extension. Its failing Read/Resolve
  providers, failing generator and callback counters prove which code paths execute. Each composition
  gets a distinct initialization system; its aggregate counters are test instrumentation, not game state.

## Exact commands and results

Run from `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Selected content: inactive providers and engine-only discovery"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Selected content: selected dependencies, refusal and saved session identity"
```

Build: zero warnings, zero errors. Discovery: **90 passed / 0 failed**, 91 ms.
Selected sessions: **38 passed / 0 failed**, 86 ms. Combined **128 checks / two exact suites**.
No broad/full run or additional older suite was executed. Scoped git diff --check reports no whitespace
errors; only pre-existing line-ending conversion warnings appear.

## Evidence established

- Menus, default/authored core input and a base scenario clock/start continue despite an unused failing
  reader/resolver/generator, unknown component or removed overlay section. No unrelated provider
  Read/Resolve/generator/Compose/Initialize callback is invoked. Menu lists/fields/nested lists cannot
  be mutated. Listing is authored intent, not fabricated validated gameplay.
- A malformed unrelated scenario body does not prevent single-ID selection of the valid base scenario.
  Selecting that malformed ID or listing the malformed metadata refuses explicitly.
- Selecting an extension includes its transitive prerequisite and runs its generator once per selected
  cached catalog. Clock queries create no live world. Each actual session composes/initializes normally;
  the sessions share only canonical immutable content.
- Ordinary player command streams, interleaved time advances, save/load/continue and reversed module
  enumeration retain exact hashes. Explicit duplicate mod requests select/compose one provider.
- Missing time/space/owner/module and duplicate/unknown scenario identities refuse through typed
  content/composition errors before Compose/Initialize. Discovery does not silently substitute a scenario.

## Remaining scope

ContentSet.Load still parses and validates manifest syntax/version/ownership and JSON syntax for all
listed files. Invalid JSON is not silently ignored. Discovery must validate all scanned identity/array
shapes; it cannot safely skip malformed identity headers. Menu listing reads its metadata, which can
fail independently of a valid single-ID start.

Only Beep.SimEngine changed: no oil, Tycoon, host/demo gameplay, rendering, adapters or platform edits.
Older suites compile but were not rerun; no full regression, current lockstep or oil-integration proof.
C1 optional module decomposition, remaining C2 imports/root work, arbitrary DAGs/saved node IDs,
resource transactions and authored construction/salvage remain open. This independent C3a slice
advanced before C1; it does not close the whole P4 stage or larger framework goal.

The lifecycle skill guided staged implementation, single ownership and evidence tracking. The user's
exact-suite instruction overrides its generic full-regression recommendation.
