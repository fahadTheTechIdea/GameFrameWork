# P2-F8 live-view terminal boundary verification

Scoped verified on engine2.439.0/save154/replay1. Original221/178checked/43open
unchanged; additional163. [Stage](../../plans/enhancements/P2_LIVE_VIEW_FAULT_BOUNDARY_DESIGN.md).
Full P1–P12 goal/plan and commit/push condition remain incomplete.

## Actual build and runtime

One solution compile: dotnet build Beep.SimEngine.slnx --no-restore, terminal
exit0, zero warnings/errors,6.81s. No runtime overlapped the compile.

ONE NEW exact case: Live views: terminal faults refuse empty missing private
observer and spatial shortcuts. FIRST/FINAL252passed/0failed/0aborted in167ms;
PID34076 Exited0, process321ms, no stdout/stderr truncation or observer error.
Outer limit30000ms, termination5000ms, output30000 characters. No repair/rerun,
historical/family/full runtime or executable edits after FINAL.

## In-place engine repair

Original PlayerView uses original World.RequireHealthyUpdate before core live
entity/space/inspection/content queries and internal visibility/projection helper
shortcuts. Missing entities/resources initialize out values before refusal; list
queries clear stale destinations before refusal and retain local publication.

Original TileMapView guards actual version, layer discovery/read/page resolution,
region cursor queries and occupancy. Entity lists now clear before the existing
terminal guard. Captured declaration metadata/cached read-model handles remain
available; their live query methods refuse. No duplicate state/fault/clock/cache,
protocol payload change, module schema change or compatibility implementation.

## Proven boundaries

Existing explicit master/JSON compositions create mapless empty and populated
two-seat tile games. Actual declared module factory exceptions latch the original
observation.provider fault. Empty/missing/owner-filter/private/observer/resource
and tile empty/invalid/occupancy/layer/page/change-cursor shortcuts cannot turn
partial state into successful facts. Seeded collection/out destinations clear;
no factory or visibility provider is called again after fault.

Healthy public save/load twins, three interleaved updates and replay match. Public
repeated faulted Update earns no additional time and republishes each viewer's own
last safe detached snapshot, with original fact epoch and empty deltas. Previously
retained snapshots stay unchanged. Independent sessions retain their hashes.
Drivers contain no game.World, TestKit, RunTicks or direct gameplay state writes;
the existing module owns its failure instrumentation.

## Limits

This is scoped core/native-tile terminal read coverage, not every custom provider,
arbitrary returned object immutability, arbitrary provider mutation sandbox,
complete fog/query matrix, full P2/P3/P11/P6 or full regression. Original broader
checkboxes remain unchanged. Protected G19/clock/Tycoon sources are unchanged.
No Oil, renderer, host gameplay, Unity/Godot or advanced Tycoon work. Commit and
push only after full goal AND plan completion; neither happened here.

## Metadata closure

Inputs frozen at2026-10-11 14:09:33 UTC.1417 source/13 quickstart anchors and706
selected executable inputs match, zero drift. Five existing selected inputs
changed (three production files, test registration and protocol fixture) and one
new executable input. All221 original literal requirements/checkboxes unchanged,
178checked/43open;204 anchors shifted six lines after14 current-header updates.
2669 paths/934 Markdown/8432 local links, broken0/thin-driver boundary0;18 valid
master rows. git diff --check exit0 with34 existing line-ending warning lines;
dirty2657/staged0. Four protected clock/Tycoon hashes unchanged. Documentation-only
closure, no executable edits/runtime/publication after FINAL; full goal/plan open.
