# P4-I10 — Authored tile condition definitions verification

Date2026-10-08. Canonical repository Beep.SimEngine only. Build2.278.0/save140.
[Stage](../../plans/enhancements/P4_TILE_CONDITION_DEFINITIONS_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[source inventory](P4_TILE_CONDITION_DEFINITIONS_SOURCE_MANIFEST.json).

## Outcome

Scoped implementation verified. Native layer conditions now are named frozen Def content;
navigation and placement reference ITileConditionDefinition instead of mutable concrete inline
bands. Existing Def/ContentAuthoring/catalog owns publication, not another condition lifecycle.
One band evaluator, one internal reference linker; no extra live store/service/clock/scheduler.
Optional TileConditionsModule is content-only; unrelated uniform routing does not require it.

Colony wet condition is the SAME actual catalog instance in both consumers; native route examples
reference stable int/fixed predicates. Master/scenario/provider registration is explicit. No old
inline shape reader, class alias, inferred profile or fallback. Rule priority/identity ordering,
AND composition and native footprint/path/travel/construction owners are preserved. Distinct
compatible condition identities may constrain the same layer; duplicate references refuse.

ExtensionProof TileColumnsConditionDef/JSON is an external engine content implementation, not
gameplay in a harness. Both consumers use it unchanged. Player drivers submit ordinary commands,
observe immutable facts and call Update. Explicit definition lifecycle/ownership diagnostics use
internal access for negative probes only; they never implement terrain or scheduler behavior.

## Every build/runtime attempt

1. `dotnet build Beep.SimEngine.slnx --no-restore`: exit1,5.66s reported/6.3564864s tool,
   zero warnings/one CS0117 at new TileConditionDefinitionTests.cs(187,70). New test used
   nonexistent SpatialLayerValueType.Int; actual contract member is Int32. Engine/proof compiled.
2. Same build after that test-only enum correction: exit0,3.42s reported/4.0955169s tool,
   zero warnings/errors. No process restart or full-test run.
3. `dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build --
   "Authored tile conditions: frozen shared contracts and physical saved colony work"`:
   FIRST runtime exit0,64 passed0 failed/aborted1332ms,2.8611131s tool. ONE exact suite selected.

Source/proof/tests/JSON frozen immediately after green; all following edits documentation only.
144 mechanical build identity paths: EngineInfo and143 older fixtures. Old authored/native rule
probes migrated directly to named condition documents/references; compile-only, NOT runtime proof.
No old/family/full/platform/stale runtime. Save140/codec payloads and sole GameClock unchanged.

## What the new bounded case proves

- Frozen original lifecycle; repeated Read/Resolve/Validate and consumer-list mutation refuse.
- Actual shared-instance file/module ownership; unpublished/cross-session/cross-content evaluation refuses.
- Strict missing/unknown/empty/duplicate/inline references, absent selected provider, invalid bands,
  private/foreign layers/spaces refuse before Initialize; valid foreign external condition also refuses.
- Actual byte/int/fixed native predicates, exact fractional fixed bound, stable priority/ID AND rules.
- One JSON band edit changes BOTH actual preview paths and content identity without mutating prior catalog.
- External JSON column predicate affects dry placement and movement without editing consumers/scheduler.
- Pure previews/typed command refusal parity/full-footprint evaluation, ordinary staged project creation.
- Actual moving machines, loaded delivery, positive productive work over sole game time before completion.
- Partial-work saved twins, pause/resume, completed save and interleaved session isolation.
- Native resources:50 opening steel minus30 actual construction inputs leaves20 across all owned stock.

Each physical stage bounds2000 Updates and throws immutable entity/job/clock diagnostics on timeout.
This is a liveness bound, not CPU/allocation/congestion/whole-session conservation proof.

## SOLID and remaining gates

Native predicate owns sampling; original catalog owns lifecycle; shared helper owns references only.
Abstract definition hooks add providers without consumer/scheduler modification. Runtime consumers use
the minimal condition contract. Stable content ownership and no new live owner are maintained.
Definition hooks remain trusted code: this does NOT prove arbitrary provider purity, reflection-safe
freeze or hostile-mod sandboxing. Other predicate families/custom startup/diagnostic file extraction,
Oil/replay/lockstep/stress/allocation and full P2/P4/P6/P1-P12 acceptance remain OPEN. Prior proofs
historical, not rerun on2.278.0. No Tycoon business implementation/Oil/sibling/host/demo/Draw/platform edits.
Full goal ACTIVE; commit/push only after FULL goal AND plan completed and verified. No publication.

## Source checks

Final scoped hashes, local link checks, phase-row shape, source-removal searches and actual
git diff --check are recorded in the self-excluded source inventory. Counts are scoped evidence,
not claims of full plan completion. Inventory excludes itself to avoid a recursive hash.

Scoped826 selected canonical paths/170 Markdown/2352 local links/0 broken/0 duplicate paths.
Actual git diff --check exit0,31 existing LF/CRLF warnings, no whitespace errors.18 five-column
phase rows valid. Old inline class/interface symbol hits0 in actual src/tests.178 prior-covered
engine files: exactly four changed, three native condition/consumer owners plus Build identity.
Sole GameClock SHA339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31 unchanged.
Final readonly SHA/row/source checks are appended only to the excluded manifest.

Read-only discovery failures (Windows rg literal wildcard and guessed EngineInfo/TestRunner/
Spatial paths) made no writes; actual paths were found with rg and read before implementing.
