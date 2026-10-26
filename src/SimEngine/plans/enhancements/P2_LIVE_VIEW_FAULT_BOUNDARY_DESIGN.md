# P2-F8 — Terminal live-view query boundary

## Scope and original owner

Repair PlayerView and TileMapView in place. After the first World fault, a live
query must refuse before empty, missing, private, observer, mapless or invalid
region shortcuts. RequireHealthyUpdate remains the sole terminal guard; no new
fault service, cached world, publication epoch, clock or compatibility path.

The last safe GameSnapshot remains readable and fault publication continues to
retain each viewer's own facts, not another viewer's projection. Captured feature,
command, event and codec declarations remain diagnostic metadata. Feature access
alone does not read gameplay; a failed factory cannot be retried by the existing
ReadProvider guard. Cached read models must guard their own actual live queries.

## Implementation stages

1. Guard core space/entity/inspection/content queries at entry. Clear collection
   destinations and initialize out values before refusal. Internal visibility and
   entity projection helpers use the same original World guard.
2. Guard tile version, layer discovery/reads/pages, region change cursors and
   occupancy before shortcut returns. Tile entity lists clear before the guard.
3. Verify one new exact bounded case using existing master JSON compositions and
   real declared module factory failure. Drivers use public views, snapshots,
   game Update, sole clock, saves and replay; no direct gameplay state writes.
4. Record all compile/runtime attempts and freeze inputs. Update actual tracker
   and evidence without treating a focused slice as whole-plan verification.

## Identity and acceptance

Engine build 2.439.0; save154/replay1 and module payload schemas unchanged. Runtime
behavior changes require matching build identity, not a compatibility reader.

- Mapless empty composition and populated private tile composition remain healthy.
- Missing/private/empty/observer queries refuse consistently after actual fault.
- Cleared list/out destinations never retain stale successful facts on refusal.
- Faulted queries invoke no visibility, inspector, space or factory callback.
- Last safe private snapshots stay readable; repeated Update retains safe facts.
- Healthy saved twins, replay and another session remain deterministic/isolated.

No claim of arbitrary provider purity, custom model immutability, all spatial
providers, full P2/P3/P11/P6 regression or full P1–P12 completion. Original open
checkboxes stay open unless their complete requirement is demonstrated. G19
protected sources remain untouched. No Oil, Tycoon, rendering or host gameplay.
Commit/push remain conditional on the full goal and plan finishing.

[Evidence](../../docs/migration/P2_LIVE_VIEW_FAULT_BOUNDARY_VERIFICATION.md)

## Verified scoped outcome

Implemented in the original owners. One clean solution compile6.81s and ONE NEW
exact FIRST/FINAL252/0/0 in167ms, PID34076 Exited0/process321ms, untruncated and
without observer error; no repairs/reruns or executable edits after FINAL.
Original221/178checked/43open unchanged, additional163. Inputs frozen at2026-10-11
14:09:33 UTC:1417 source/13 quickstart anchors,706 selected executable inputs.
Read-only closure found zero hash/requirement/link/driver boundary drift;2669
paths/934 Markdown/8432 local links/18 valid master rows. git diff --check exit0,
34 existing line-ending warning lines, dirty2657/staged0. All four protected
hashes unchanged. Full goal/plan remains open; no staging/commit/push.
