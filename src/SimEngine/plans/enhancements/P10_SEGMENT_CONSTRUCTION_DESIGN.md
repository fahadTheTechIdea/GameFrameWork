# P10-A3 — physical corridor activity DAG

Current authored segment binding2026-10-07: build2.242.0/save139/all payload bytes unchanged.
[P4-G4b stage](P4_AUTHORED_SEGMENT_WORKFLOWS_DESIGN.md),
[current evidence](../../docs/migration/P4_AUTHORED_SEGMENT_WORKFLOWS_VERIFICATION.md).
LineDef now references a selected reusable activity DAG/segment_node; no flat gameplay stage
generation remains. TWO builds:1 new comparison error5.65s, clean3.79s. ONE exact NEW bounded
native case80 checks0 failures/aborts486ms FIRST run. Six JSON/nine bindings/profiles/nodes and
two older fixtures source/compile ONLY, NOT a rerun of the historical suites below.
Full P10/G4/P1–P12 remain open; source frozen after green; publication ONLY after FULL completion.

Historical original native slice implemented and focused-verified on2026-10-06; build2.125.0/save68/line_work2,
386 checks/two exact suites, zero failures/aborts and clean build.
[Evidence](../../docs/migration/P10_SEGMENT_CONSTRUCTION_VERIFICATION.md). Earlier suites compiled
only, not rerun; provider-neutral corridors, utilities and the full P10/P1-P12 roadmap stay open.

## Scope and authority

Replace the one-node corridor and progress-derived remote stamping. No compatibility reader or old
completion path remains. LineService owns geometry; WorkBoard owns stages, scheduling, progress,
requirements, reservations and completion. Existing logistics and recovery own actual cargo and
co-located leftover disposition. Hosts supply commands and observe, never advance construction.

## Authored inputs

Each line explicitly names `activity`, `segment_node` and `finish_effect`. The selected ActivityDef
owns all authored nodes/roles/executors/rational durations/requirements/supply/effects. The existing
`line_effects` declares `operation: segment` or `operation: finish`. Designated final node must
stamp and depend on every other profile node, with positive physical work in its ancestry. Other
nodes may not stamp or finish the cursor. Profile has no standalone orderability/target/final-effect
policy. Missing/conflicting metadata refuses; deleted flat fields have no old reader.

## Execution

Bind all authored nodes per tile to stable `segment.<index>.<authored-node>` IDs through existing
ActivityGraph. Preserve exact execution data/intra-segment edges/parallel branches; each root waits
on previous tile's terminal. Session time converts each actual stage; checked native node-count/
total tick arithmetic refuses overflow, not a platform/map-size cap. Shared SupplyCapacity projects
one authored segment's conservative target envelope, not whole corridor stock. Future supply waits
on actual dependencies.

The machine travels to the current cursor through normal work jobs. Only its completed node effect
may stamp that segment. The effect rechecks current geometry, ownership, tile rules, exact order/node
binding and earned progress. Before moving the empty intent cursor, promises must be absent and
leftovers must be evacuated through the selected recovery owner at the old location. No cargo,
worker, progress or claim is moved to the next tile. The next job must physically approach again.

No duplicate saved laid counter; completed terminal prefix, not NodesDone, is tile progress. Completion
only removes a fully completed marker, never stamps remaining tiles. Cancellation preserves real
laid geometry and existing material disposition. Refusals remain pure and use authored retry policy.

## Durability and verification

LineWork schema2 still owns only path/order identity. Current save139/build2.242.0 admits no old
content/build; there is no legacy graph translator. Loaded markers
validate path, line/marker identity, order binding and current cursor against the existing DAG.
Network pools/topology and work/inventory payloads remain unchanged.

Historical original invocation scope (NOT rerun or newly authorized; current gate is above):

- `Line segments: authored DAG, admission and pure completion`
- `Line segments: physical supply, recovery and saved continuation`

Cover small-buffer long corridors, explicit references/duration, unearned effects, high work rate,
per-segment consumption/cost, actual travel, future ownership conflicts, leftover recovery,
cancellation and saved interleaved continuation. Earlier tests are revised/compiled only.
No utilities, generic non-tile provider, fleet/deadlock/global rollback/replay/full suite or roadmap
completion is claimed.
