# P3-S12: JSON-authored layered tile navigation — Design Document

Status: scoped implementation verified,2026-10-08. Build2.258.0/save139/persistent payloads unchanged.
ONE clean build12.35s/0 warnings/errors; ONE exact NEW bounded case48 checks0 failures/aborts1049ms,
FIRST attempt.122 older strict build identities compile-only; no older/family/full/platform/stale
gate. Source/tests/content frozen after green. [Evidence](../../docs/migration/P3_LAYERED_NAVIGATION_VERIFICATION.md).
Full framework goal remains active; no publication until full verified completion.

## 0. Problem Statement

TileNavigationModule composes existing ITileNavigationRule providers, but native JSON supplies only
UniformTileNavigationDef. Games cannot author road preference, terrain blocking or depth-dependent
land/vessel travel without implementing a cost callback. Existing multilayer state must remain owner.

## 1. Goals

1. Master-selected JSON defines movement classes over actual public native layers.
2. Rules have stable IDs, explicit priorities/costs and conjunctive native value bands.
3. Same NavService/PathSearch/Mover owns admission, travel weighting, reroute, time and saved intent.
4. No copied terrain, second route/clock, fallback profile, platform code or Tycoon dependency.
5. One exact NEW bounded case proves authored routing and physical work/save continuation.

## 2. Design

Add LayeredTileNavigationDef : TileNavigationRuleDef in Standard/LayeredTileNavigation.cs.
Register layered_tile_navigation in the existing TileNavigationModule. Compose needs no new branch:
its existing TileNavigationRuleDef enumeration binds the actual selected definition to NavService.

JSON declares space, base_cost and rules. Each rule explicitly declares id, priority, cost and
nonempty conditions. Each condition names one actual spatial layer and explicit minimum/maximum
(number or null); lower inclusive, upper exclusive. Null means explicitly unbounded. At least one
bound is required; duplicate condition layers/rule IDs, invalid intervals, negative costs and
no positive possible authored cost refuse. Byte/int bounds are integral; fixed bounds use native Fix.
Cross-space/private references refuse after linked metadata resolves, before initialization.

Highest priority matching rule wins; equal priority breaks by stable ordinal rule ID, independent of
JSON order. All conditions in a rule must match. Zero blocks entry. No matching rule uses the explicit
base_cost, not fabricated ground. MinCost derives from every positive authored outcome for an
admissible A* heuristic. These are preference weights, not a reserved route or guaranteed ETA.

Cost reads actual MapLayer values in the same selected TileMap. No full-map scan/cache/codec/value
copy. Current native routing already checks occupancy/diagonal constraints and reopens its path when
an upcoming cell becomes impassable. This slice does not invent hydrology or promise rerouting merely
because a still-passable alternative road became cheaper. Runtime player movement-class switches,
fleet preferred corridors and service congestion remain separate open work.

## 3. API surface

Existing ITileNavigationRule and native mover/player work/move views/commands. New immutable authored
rule/condition metadata. No new scheduler, service, command, codec or renderer.

## 4. Files to change

New Standard/LayeredTileNavigation.cs; TileNavigationModule section registration; build identity;
one new case, two master-selected JSON documents and embedded-resource/test registration. Older build
identity assertions compile-only. Stage/master/AGENTS/current evidence and scoped inventory.

## 5. Error handling

Explicit malformed/missing/duplicate bands/IDs, all-zero or negative outcomes and foreign/private
layers fail game initialization. Runtime foreign session/map/content refuses; off-map cost is zero.
Existing unreachable/budget/physical blocker/refuel and save-fault semantics remain authoritative.

## 6. Backward compatibility

Development only. New authored provider alongside distinct existing uniform behavior; no legacy
reader, profile inference, shim or duplicate cost/search implementation.

## 7. Verification

One build after complete source/proof; ONLY one exact NEW bounded native case. Repair/rerun ONLY
observed failures. Older/family/full/platform/stale suites excluded. Source/tests/JSON freeze after
final green; docs-only afterward. Record every invocation; focused proof is not full P1–P12 acceptance.

## 8. Risks

Cost evaluation scales with authored rules/conditions and existing search budget. No CPU/allocation
claim. Public-only native layers prevent hidden geology driving observable route choices. Dynamic
costs are current admission/weighting, never a future road/traffic guarantee or new stock authority.

## 9. Out of scope

Hydrology/weather, non-tile geometry, vehicle hulls, global provider purity, runtime fleet route
policies, structural construction/deletion of roads, replay/stress/full P3/P9/P1–P12 gates. No Oil/
Tycoon/host/demo/Draw/platform changes. Commit/push only after full goal and plan finish.

## 10. Sub-task execution order

1. [x] Native authored provider through existing contracts and actual layer owners.
2. [x] Master-selected road/depth content, bounded normal physical work plus native layer protocol proof.
3. [x] Build and one exact new case; first attempt green; freeze source after green.
4. [x] Record scoped evidence, update authoritative tracker and preserve broader open requirements.
