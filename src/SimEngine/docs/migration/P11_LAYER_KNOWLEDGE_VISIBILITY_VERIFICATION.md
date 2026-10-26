# P11-D4 — Whole-layer knowledge visibility

2026-10-07. Build2.221.0/save125. Existing native map6, knowledge1 and point payloads/views unchanged.
LayerKnowledgeVisibilityDef uses the existing JSON target/policy contracts. Actual ICompanyKnowledge
and IVisibility retain authority. Shared spatial authorization filters metadata, samples and linked
content; ownership is not learned permission. Existing native/public-provider cursor records now
track transient readable membership and only authorized writes. No second fog/knowledge/layer/clock
owner or saved cursor table. World refreshes after settled startup, normal CompleteUpdate and complete
restore, never from reads/hash/save. One unchanged GameClock; no hosts/demos/OilFieldDays/Draw/
platform/economics edits. Full goal/plan remain active; commit/push only after FULL completion.

## Every invocation for this slice

Same build command, three invocations:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
```

1. Failed4.52s,0 warnings/10 errors: new test omitted existing static Tick helper import.
   No stale test binary run. Added import, no duplicate helper/gameplay implementation.
2. Clean2.74s,0 warnings/errors. First scenario then failed below.
3. Clean2.89s,0 warnings/errors after only the observed fixture correction.

ONE distinct exact NEW scenario, TWO invocations:

```powershell
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Layer knowledge: authored discovery, private cursors and saved continuation"
```

1.3 checks passed/1 aborted suite88ms: master rejected duplicate point-layers.json document ownership.
   Existing ConstructionPlanTests.Points supplies its work-policy fixture through PointGame's second
   document argument, already using that logical filename. New layer definitions therefore use an
   explicit UNIQUE discovery-layers.json master entry. Strict engine validation was correct and unchanged.
2.70 checks passed,0 failures/aborted suites363ms. Final exit0. No additional scenario/older/family/
   full/platform suite, no stale binary, no gameplay/test edit after this final successful gate.

93 older assertion files/151 current identity lines migrated/compiled only, NOT executed. Older
historical gates are not current regression evidence.

## Authorship and physical proof

Three new embedded JSON documents author layer rules and stable facts. Existing physical discovery
work/material/policy JSON is reused; TechnologyTests.DiscoveryFactory forwards the existing additional-
document contract, not another loader. layer-knowledge.json and layer-discovery-facts.json preload
as logical visibility.json/knowledge.json. Existing point-layers.json content loads as the uniquely
master-listed discovery-layers.json. tile-layer-knowledge.json keeps its actual filename; existing
spatial-observer.json content loads as logical layers.json. These aliases are fixture document wiring,
not compatibility schemas or inferred game content.

The point workflow uses normal player policy/order/sample commands, real worker/time, eight consumed
materials, partial-work save, company-private reveal, actual sampled values and a saved queued command.
No tests calculate discovery rules or grant the physical workflow's learned fact. Native layer/knowledge
fixtures directly use engine-only authority and are explicitly PROTOCOL instrumentation, not a native
survey player feature or physical acceptance. Both providers' samples are initially real setup values.

Scoped checks cover all/any native targets, missing targets/facts, hidden/missing no-detail admission,
metadata links, pure views/previews, output clearing, ownership without discovery, hidden public/private
writes without cursor/chunk leakage, once-only readable membership invalidation, visible pages/chunks,
positive nonseat provider denial, retained snapshots, saved partial work and post-load learned visibility,
independent sessions and stable identical saved continuation. Physical waits bound900 steps with actual
order/state/blocker diagnostics. Native map remains sparse at70000×70000; no platform size cap.

## Remaining scope

Not per-cell/partial-layer exploration, historical stale observations, arbitrary custom-feature/event/
relationship/ID privacy, global provider purity/fault/overflow/performance matrices, pressure/strategy,
replay/lockstep/stress or full game/framework acceptance. Development-lifecycle/SOLID review extended
existing authorities, added only an optional spatial invalidation capability and retained thin players.

[Design](../../plans/enhancements/P11_LAYER_KNOWLEDGE_VISIBILITY_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[scoped source inventory](P11_LAYER_KNOWLEDGE_VISIBILITY_SOURCE_MANIFEST.json), excludes itself.
Read-only hash/link/clock/diff checks below are NOT extra runtime gates.

Scoped inventory251 unique files/40 Markdown documents/1240 local links;0 broken links. Final
inventory validation requires0 hash mismatches. `git diff --check` exited0; warnings only describe
existing LF/CRLF Git configuration. Sole GameClock constructor remains World.cs:96; unchanged source
SHA256 `339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.
