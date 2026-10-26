# P10-A10 — Authored line worksite verification

Current scoped result: Beep.SimEngine2.442.0/save154/replay1. Full goal and plan
remain incomplete. No staging, commit or push; publication remains conditional on
full goal AND plan completion and verification.

## Implementation and ownership

Each line now requires an explicit JSON work_marker EntityDef. The original
InfrastructureModule marker generator, work_capacity declaration and generated
identity convention were deleted. Eight existing content files were migrated in
place, with eleven explicit worksite prototypes across seven files; independent
line definitions reference the already-authored prototypes rather than duplicating
them. Strict parsing rejects the removed work_capacity field.

ILineWorkDefinition, ILineWork and ILineExecutionPlan expose the original cursor
binding and derived graph through contracts. LineService uses these capabilities,
not concrete LineWork lookups. Native LineWork retains the sole private line/path/
order payload and schema2. Original activity compiler, WorkBoard, graph progress,
inventory, physical workers, effects, resource recovery, topology and game clock
remain authoritative. No second state store, scheduler, parser or compatibility path.

Worksite admission validates actual marker kind, component/capability bindings,
nonblocking movable unit placement in the actual line space, empty authored initial
stock and original inventory capacity against the segment material envelope.
Inventory/public export rules remain explicitly authored original contracts.
Native path views are read-only; player views remain detached/private.

The independent proof metadata implements ILineWorkDefinition and reuses the
original LineWork runtime/codec. It does not prove an arbitrary custom cursor
runtime. Negative existing tests retain their original corrupt-path/order assertions
through test-only reflection into the private original payload; the new positive
driver never invokes that instrumentation.

## Compilation — actual attempts, not historical full-suite claims

1. Solution: dotnet build Beep.SimEngine.slnx --no-restore -v minimal.
   Exit1, zero warnings/one error,6.42s: new test referenced nonexistent command
   Priority. Removed that test field; original command priority remains engine-owned.
2. Targeted test project: exit0, zero warnings/errors,4.50s.
3. Same targeted project repair: exit0, zero warnings/errors,10.33s, terminal before
   runtime. Required after the genuine transient cursor observation failure below.

## Exactly one new case — both attempts retained

Case: Authored line cursors: contract worksites replace generation with physical durable DAG execution

Command: tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.exe
--bounded-exact <exact case above> --timeout-ms60000
--termination-timeout-ms5000 --output-chars30000.

Attempt1:30passed/1failed/0aborted,336ms; PID31276 Exited1,process486ms.
Original WorkBoard.Post legitimately observes BindPath before BindOrder assigns
its positive order. Cursor Sync now permits that actual transient zero, not negative
orders. Actual durable order identity remains checked by the original execution
binding; loaded marker validation checks graph/path/placement against WorkBoard.
No instant fallback or synthetic order was introduced.

FINAL:249passed/0failed/0aborted,4702ms; PID34320 Exited0,process4998ms.
Both processes exited without stdout/stderr truncation or observer error. No old,
family, broad filtered or full runtime was run. No executable edit after FINAL.

## What the new proof exercises

Four native/independent-metadata × minute/half-clock public-player flows select
actual JSON cursor.road/cursor.pipe identities rather than generated names. Ordinary
commands create original nine-node physical corridor DAGs. Machines move to work,
consume their actual carried materials over game time, retain earned progress
through worker hold/release, and stamp only completed cells. Saved twins and the
single paused clock preserve progress. Finalization removes the original worksite.
Cancellation preserves the earned prefix, clears actual worksite/claims and does
not stamp future cells. Actual total steel is40initial/38first cell/34completed
road/32after the first pipe cell. Detached old views, foreign graph withholding,
replay and idle interleaved-session isolation are checked.

Twenty-six early admission variants (thirteen native and thirteen independent)
reject wrong kind, blocking/immobile/wide footprint, missing inventory/presence/
cursor capability, granted initial stock, missing capacity policy/space/work_marker,
unknown work_marker and obsolete work_capacity before Initialize.

The new driver composes master-listed JSON and uses public commands/results/views/
Update/clock/save/replay, not game.World, TestKit, RunTicks or direct stock/progress
writes. There is no separate hauler-supply or explicit different-worker reselection
claim, arbitrary custom cursor runtime proof, zero-order corrupt-save case or full
all-network/global provider-fault/stress/allocation proof.

## Read-only architecture review and remaining gates

Reviewed395 production C# files and1251 capability/component lookup lines.
Remaining concrete hits in that scan are native JSON registrations and one native
TileWorkDockDef self-identity guard, not high-level LineService/System cursor lookup.
Old WorkCapacity/LineMarkerDef/MarkerDefinition/Get<LineWork>/Components<LineWork>
paths are absent. Oil-name hits are a documentation example and test friend
assembly, not gameplay bindings. This scan does not close the broader P2.003 gate
or prove every custom capability/provider. Original221gates:178checked/43open,
unchanged. Additional scoped checkpoint166. Full P1–P12, non-tile corridors,
topology/accounting/fault/stress gates and protected G19 remain open.

[Stage](../../plans/enhancements/P10_AUTHORED_LINE_CURSOR_DESIGN.md).

## Metadata closure

Source/input freeze:2026-10-11 15:17:45 UTC. Read-only audit validates1431source
anchors,718selected inputs,13quickstart anchors,221literal requirement/checkbox
anchors and18well-formed master rows. It inventories2682paths/940Markdown files/
8526local links with zero missing targets. Zero source/quickstart/input drift or
positive-driver boundary matches. git diff --check exit0 with34line-ending warnings.
Dirty inventory2670, staged0; all four protected clock/Tycoon hashes unchanged.
Nineteen previously selected inputs changed; six were added (three new executables
and three existing migrated fixtures). Five existing production sources changed.
204original gate anchors shift six lines for new headers; zero requirement or
checkbox changes. Metadata refresh includes14current headers, not quickstart runtime.
No executable edit after FINAL. All broader gates/publication remain open.
