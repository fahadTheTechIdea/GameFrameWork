# P10-A10 — File-authored physical line worksite and cursor contracts

## Problem and goals

InfrastructureModule.LineMarkerDef generates every corridor cursor with native
TilePresenceDef/StorageDef/LineWorkDef. LineService reads concrete LineWork. That
makes this remaining game object different from the file-authored, contract-based
entity model and forces mods through native marker generation.

Replace that path in place: each ILineDefinition references an actual JSON-authored
EntityDef work_marker. Its existing inventory is the only capacity/stock/receipt
owner. Its ILineWork capability holds the sole line/path/order binding and derives
progress from the existing graph. Never create a second cursor, project, catalog,
scheduler, parser, reservation store or clock.

## Stages and actual owners

1. Add ILineWorkDefinition, ILineWork and ILineExecutionPlan to InfrastructureWork.cs.
   Shared cursor binding validation covers actual tile presence, empty initial
   inventory, nonblocking movable unit footprint and actual line space/capacity.
2. Extend ILineDefinition/LineDeclaration with WorkMarker/work_marker; remove
   WorkCapacity/work_capacity. Validate actual catalog entity and capability
   aliases, inventory capacity against the existing segment material envelope,
   exact space/geometry before Initialize. Public/export policy stays explicitly
   authored through original inventory/logistics contracts, never a native type gate.
3. Delete InfrastructureModule.LineMarkerDef generator and its registration.
   Make LineWorkDef strictly authored; native LineWork uses contract metadata and
   private original payload fields. BindPath/BindOrder initialize one actual cursor;
   engine-side path properties are read-only; player projections are detached.
   Neither introduces mutable player-owned path or progress storage.
4. Migrate every LineService cursor lookup/iteration to ILineWork and every derived
   plan use to ILineExecutionPlan. Actual WorkMarker identity replaces generated
   name conventions. Original compiler/board/segment/effect/recovery/topology owners
   remain. Validate runtime provider binding through original entity observation.
5. Migrate ALL canonical line fixture documents and metadata providers in place.
   Explicit entity prototypes remain in their master-listed files; no helper
   generator, absent-field default, legacy fallback or compatibility reader.
6. ONE NEW exact bounded public-player acceptance case uses native and independent
   JSON cursor metadata, minute/half clocks, physical machine-carried materials and
   multi-segment work, worker hold/release, pause/cancel, saved twins/private
   observations/replay/interleaving and
   early invalid worksite admission. Repair only this same case when needed.
7. Freeze actual source/input hashes and update master/evidence; no full/family
   runtime or broad checkbox closure without matching current evidence.

## API, errors and persistence

work_marker is a required entity reference. work_capacity is removed, so old
declarations fail strict parsing. Missing/foreign/invalid worksite, incompatible
space, native typed fallback, nonempty initial stock, immobile
or blocking footprint and insufficient segment envelope refuse before Initialize.
The definition's complete disclosure includes this actual worksite prototype.

Original cursor sync order and schema2 remain line/path/order; original line
service schema4, save154/replay1 and game clock are unchanged. Build2.442.0 marks
new contracts/content shape. Manifest/content identity, not a compatibility shim,
handles saves across changed authored files. Stable line/plan/node IDs stay.

## Scope and limitations

SimEngine only; no Oil extension, Draw/platform/adapter, advanced Tycoon or protected
G19 migration. Generic other-space corridor authoring, arbitrary independent cursor
runtime/fault purity, network topology/accounting/stress/global/full P10/P1–P12
remain open. This is a real file-owned object migration, not proof of every remaining
plan gate. Commit/push remain conditional on full goal AND plan finish.

[Evidence](../../docs/migration/P10_AUTHORED_LINE_CURSOR_VERIFICATION.md)

## Current scoped checkpoint

Implemented and verified by one new exact case: FINAL249passed/0failed/0aborted
in4702ms, process Exited0. Read-only metadata audit:1431source anchors,
718selected inputs,13quickstart anchors,221literal gates and18master rows;
zero hash drift/broken links/driver-boundary matches, git diff --check exit0
(34line-ending warnings), staged0. Original178checked/43open unchanged;
additional166. No executable edits after FINAL and no full/family runtime.
This is scoped verification, not completion of P10 or the full goal/plan.
