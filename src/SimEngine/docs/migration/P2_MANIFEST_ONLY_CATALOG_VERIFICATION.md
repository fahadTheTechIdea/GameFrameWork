# P2-F9 — Manifest-only catalog verification

Current scoped checkpoint: engine2.443.0/save154/replay1. Full goal and plan remain
incomplete. No staging/commit/push; publication is conditional on full goal AND plan.

## Production migration

ContentTypes no longer exposes Generator or stores generator callbacks/owners.
ResolvedContent.Build no longer invokes generators or admits their produced Def
objects. Actual master-selected JSON entries pass through the sole strict reader,
catalog aliases, reference resolution, metadata validation and frozen publication.
EntityDef.Generated, StorageDeclaration.Make, StorageDef.Make, TilePresenceDef.Make
and its unused Create helper were deleted. No compatibility shim or fallback.

Native content registration remains contract-based. Runtime entity spawning,
startup generation, runtime spatial intents and compiled execution graph nodes are
not catalog definitions; their original owners remain. No graph/stock/reservation/
catalog/parser/time owner was added and no payload schema was changed.

## Existing assertion migration — compiled, not old-suite runtime claims

- ContentLifecycleTests: authored value23 worksite replaces generated prototype.
  Read/resolve/validate/freeze, active codec, duplicate attachment and original-owner
  protections remain. Cross-catalog negative rejection calls original internal Add
  with a published definition. Noncatalog runtime metadata lifecycle remains tested.
- SchemaBindingTests: authored schema_probe replaces generator callback. Original
  codec, signal owner, control and signal assertions remain. Deliberate mismatched
  control injection now occurs inside the authored reader instrumentation, cleared
  after the test, and still must refuse original metadata validation.
- StateCodecTests: explicit JSON entity replaces generator. Original schema7/key/
  owner/diagnostics, undeclared/inactive codec refusal, captured state and saved
  continuation assertions remain. MutableCodecSystem uses actual probe.authored
  identity and ordinary nonspatial World.Spawn, not an absent generated name/tile map.
- CompositionTests: inactive/active component selection remains authoritative;
  obsolete generator invocation counts become removed-API/active authored-contract
  checks. No generator callback can be run by either selection.
- SelectedContentTests: removed generator count/failure probes become removed-API,
  actual cached content instance and once-only Initialize checks. Strict selected
  malformed content, unselected provider noncomposition and session assertions remain.
- ProjectionTests: original tags/component array capture assertions use test-only
  internal attachment instrumentation calling original EntityDef.Read/SetComponents.
  It never enters a catalog or spawns positive gameplay. Schema part also uses its
  original reader; no production constructor-generated API remains.

All affected registrations remain; three obsolete generated-case labels are renamed
to authored-case labels. No suite is deleted. ContentAttachmentProbe is negative
test instrumentation only, not a parser, positive content loader or compatibility
API. CompositionProbe's optional definitions parameter supplies JSON to the same
existing master/document loader. The new positive driver never calls attachment
instrumentation, game.World, TestKit, RunTicks or direct stock/progress writes.

## Actual compile attempts

1. dotnet build Beep.SimEngine.slnx --no-restore -v minimal: exit0,zero warnings/errors,
   10.78s; terminal before runtime.
2. Targeted test project, same options: exit0,zero warnings/errors,4.46s after authored
   assertion labels/runtime metadata wording were corrected.
3. Targeted same project: exit0,zero warnings/errors,4.31s after native fixture
   conversion was restricted to actual proof_line_work components.
4. Targeted same project: exit0,zero warnings/errors,6.79s after retained codec
   fixture identity/nonspatial spawn migration and public continuation coverage.
5. Targeted same project: exit0,zero warnings/errors,4.26s after ensuring the new
   codec proof creates separate original system/service instances per session.

Targeted command: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj
--no-restore -v minimal. No compile/runtime overlap and no full/family runtime.

## ONE NEW exact case — all attempts retained

Manifest-only content: authored prototypes own physical durable corridor execution

Runner: tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.exe
--bounded-exact <exact case above> --timeout-ms60000
--termination-timeout-ms5000 --output-chars30000.

1.6passed/1failed/0aborted,175ms; PID38384 Exited1/process315ms.
   The native fixture conversion incorrectly added line_work to an unrelated empty
   prototype. Actual engine capability validation correctly refused. Conversion now
   changes only actual proof_line_work authoring; no production guard was weakened.
2.142passed/0failed/0aborted,2813ms; PID20300 Exited0/process2951ms. Historical
   intermediate pass: read-only migration review then found the retained old codec
   prototype-name lookup and its unnecessary tile assumption; repaired before FINAL.
3.146passed/0failed/0aborted,3301ms; PID11240 Exited0/process3461ms. Historical
   intermediate pass: new codec proof still captured mutable system/service owners
   across factory sessions. Composition now creates them per session and explicitly
   checks idle-session isolation, not just twin equality.
4.FINAL147passed/0failed/0aborted,2687ms; PID45488 Exited0/process2825ms.

All processes are terminal with no stdout/stderr truncation or observer error.
No executable edits after FINAL; earlier passes are not current completion evidence.

## Scope actually proved

Removed public generator/prototype/native constructor APIs; actual authored
nonspatial schema7 codec entity/system/service state saves and continues, with
separate original session owners. Four native/independent cursor metadata × minute/
half-clock flows use actual manifest-only-catalog.json prototypes. Three authored
catalog entries do not create unused live entities. Detached tags and private
activity views stay distinct from prototype intent. Nine original physical DAG nodes
earn cells/material costs over game time; steel40initial/34completed. Paused clock,
saved twins, once-only original cursor removal, replay and idle isolation agree.
Changing an unused actual prototype's authored tags changes selected content identity
and refuses old save restore. Twelve early invalid document cases reject generators,
patches, duplicate IDs, unknown component/field and missing capacity reference before
Initialize through original readers.

This does not prove all migrated old runtimes, arbitrary custom providers, every
ownership fault/transaction, non-tile corridor breadth, full P1–P12 or broad stress/
allocation acceptance. Protected G19 and all four clock/Tycoon byte identities remain
unchanged. Original221gates/178checked/43open unchanged; additional167. Broader
P4.015 catalog/legacy assertions remain open until their entire scope is evidenced.

[Stage](../../plans/enhancements/P2_MANIFEST_ONLY_CATALOG_DESIGN.md).

## Metadata closure

Executable/source freeze:2026-10-11 15:35:34 UTC. Read-only audit validates1445source
anchors/731selected inputs/13quickstart anchors with zero mismatch;221literal gate
requirements and checkboxes remain exactly unchanged. Eighteen master rows are
well-formed. Inventory2687paths/942Markdown files/8528local links, no missing targets
or positive-driver boundary matches. git diff --check exit0 with34line-ending
warnings. Dirty2675, staged0; all four protected clock/Tycoon hashes unchanged.
Eleven previously selected inputs changed; thirteen added to the freeze (three newly
created executable inputs and ten existing migrated fixtures). Six existing
production files changed. Fourteen current headers replaced without appending
another historical header block; zero original anchor line shifts, requirement or
checkbox changes. No quickstart runtime. No executable edit after FINAL. Full goal,
plan and broader verification/publication gates remain open.
