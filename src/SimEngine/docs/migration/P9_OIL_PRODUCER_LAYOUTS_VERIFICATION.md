# P9-PBH1.O — Actual Oil producer layout verification

Subsequent [P9-PBH1.P evidence](P9_PARKING_ROUTE_SELECTION_VERIFICATION.md) now owns
current2.652 engine/results/native minute1092 progress. The original layouts and
preservation proof below remain unchanged;2.650 saves/results are historical.

2026-10-18. Existing master-listed Oil content; engine2.650.0/save160/replay1
unchanged. Original221 requirements/209 recorded checked/12 open. This does NOT
close P9.acceptance.051, completed offshore acceptance or the full goal.

## Changes and preservation

Only company_base, vehicle_factory, maintenance_depot and shipyard component
objects gain tile_work_dock. No existing field, recipe amount/duration, kit,
terrain/scenario/master, money or business rule changes. Original dock, rollout,
navigation, presence, worker, reservation, resource and clock contracts remain
authoritative. No host gameplay, duplicate registry or mobile ring fallback.

Pre-edit normalized structures text SHA256:
`1C9E8C380B61F4A8D84606B99FDE0B0C38F4092F1667FF5C984742B8E3B45D94`.
Removing exactly FOUR inserted component lines plus their newline from the
edited file reproduces that SHA byte-for-byte after CRLF normalization. This
compares the actual dirty pre-edit source, not Git HEAD. Edited file SHA256:
`E63495E70EE54C3F39D4C00E0DBE75EBF003851BD0CB4C71A606ED766BAE9A28`.

| Producer | Service | Rollout | Departure | Waiting | Parking | Starts | Output prototypes |
|---|---:|---:|---:|---:|---:|---:|---:|
| company_base4x4 | 2 | 18 | 4 | 4 | 12 | 20 | 10 |
| vehicle_factory4x4 | 2 | 0 | 4 | 4 | 12 | 2 | 6 |
| maintenance_depot3x3 | 2 | 0 | 4 | 4 | 10 | 2 | 2 |
| shipyard5x4 | 6 | 16 | 7 | 7 | 32 | 22 | 7 |

Slot IDs and positions are unique within each layout. Starts resolve to
Service/Rollout and exits to distinct Departure slots. All lanes explicitly
author max_expanded4096. Fourteen known roles and ten actual solid resources
preserve compatible service eligibility, not work permissions, stock or prices.
The actual content loader validates references before initialization.

Non-vacuous static comparison covered all6/6/2/7 actual transformation entries,
plus every startup vehicle occurrence in all six canonical scenarios. The first
static recipe check used nonexistent recipes; it was discarded and repeated
against real transformations arrays with mandatory nonempty counts. No runtime
or test was changed to accommodate that diagnostic mistake.

Static slot geometry avoids the former yard71,74 neighbour footprints
(depot71,69 and grid75,71). It does not prove all parking slots are traversable
or require new engine suggestions to select old coordinates. Native terrain,
route and placement owners still decide. Current actual proposals are below.

## Builds and identities

Two targeted OilCo.PublicProof Release builds: zero warnings/errors, elapsed
3.46s and1.72s. The second only compiles the new public Sandbox case in
ClientTests.cs. No engine/Oil rules changed and no full/profile/family run.

Core448 C# source aggregate SHA:
`b5bbb9a12ef5da072b8d294acc0d76f7c4d034058c1b2eb0edf801ce1c627a8f`.
Copied core DLL SHA:
`FECAA1476DC589A9BB5A7D15D5C302A268F064D2293736850475A30F7622F190`.
Both equal the preceding connected-rollout build; no engine version bump.
Current OilCo.Game DLL SHA:
`07E24563C5348AD5FFCD4B6F5695E8ED47434DF8E90EA9233521BF4B49739832`.
First public proof DLL (Frontier and Gulf preparation):
`CA0AA4624CF69617CB77C8F120549B400AD4413A39D23810E0B7B565689F5C37`.
Second public proof DLL (Sandbox and harbour observation):
`474F8752723655228FEF0E9EA078BAEAD447FBA08D928A0202679C8D888A2169`.

## Every exact attempt

Existing bounded-exact runner, one literal per process, termination-timeout5000ms.
No suite/profile/family filter. All terminal results, including failure, retained.

| Literal / input | Bound ms | Passed / failed | Worker ms | Outer ms |
|---|---:|---:|---:|---:|
| Clients: canonical Frontier initializes and admits ordinary founding | 90000 | 9/0 | 17008 | 17155 |
| Offshore: vessels survey, drill and ship from the sea / prepare | 90000 | 24/0 | 50822 | 50955 |
| Clients: canonical Sandbox founds its eleven-machine kit and saves physical return | 90000 | 36/0 | 56448 | 56579 |
| Offshore: vessels survey, drill and ship from the sea / harbour:a937fc29... | 150000 | TIMED OUT; no terminal assertion total | not reported | 150054 |

Three passed attempts total69 assertions. The fourth is NON-GREEN, not part of
a passing aggregate. No timeout was raised or failed case silently rerun.

Frontier: actual96x96seed9, ordinary preview/inbox, whole nine-machine prefix,
no preview grants, identical save reload. Sandbox: actual eleven-machine kit
including duplicate truck/tanker, distinct starts, original HQ home identities,
eight real physical updates, saved twins identical and no duplication/removal.
This does not prove all starting machines have finished parking or all seeds.

Gulf: actual96x96seed1/two humans/seat1, no Tycoon or invented cash. Original
engine suggestions now choose yard71,73 and field21 at58,78; depot/grid follow
current rules too. No source selects an easier substitute coordinate or edits
geography. Three real sites retain16/60h shipyard,8/30h depot,4/16h grid
clearing/erection durations. Minute11/hash6290BC3E27964628 earned normally.

Native checkpoint relative to The-Tech-Idea:
`Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/a937fc29fae9499985e14b03b63c7c64/checkpoint.json`.
Verified harbour_projects_admitted, next harbour_execution. Native bytes reload
identically from memory/disk; observer artifact publication changes no hash.
This is preparation, not completed construction/production. Older2.647 artifacts
remain historical/incompatible; no save headers are rewritten.

The harbour continuation loaded that exact native save using the second runner.
Original one-day/25-day game-time and per-stage120000ms bounds were unchanged;
outer150000ms includes actual loading/diagnostics. It printed initial state at11
and then timed out before reporting a completed first60-update batch. The
process is terminal; no later checkpoint/progress is claimed. Current resumable
artifact remains minute11. Do not restart blind days or enlarge the bound.

## Next diagnosis and remaining gates

Inspect the original WorkHomeParking.ReadHomeParking query cost before more
canonical days. It evaluates every candidate using separate guarded role,
claim/vacancy, policy, reach, route and cost queries. WorkerHomes.Read and
UpdateIdle both use it. This is a source-level performance candidate, NOT a
measured root cause. Next obtain bounded individual-update timing/call evidence
on the actual native save or a generic reproduction; preserve provider purity,
all physical compatibility/occupancy constraints and deterministic cost ties.
Do not reduce authored capacity, bypass guards or add a second parking owner
merely to make the acceptance faster. Fix an actual generic defect in its
original owner only after reproduction, with focused saved/replay checks.

Factory/depot manufacture, marine berth clearance, seven full-duration vessel
recipes, physical offshore work, general deadlock freedom, arbitrary provider
purity and global CPU/allocation budgets remain unproved. Two older dock cases
and combined transformation75/16 were not rerun or erased. P9.acceptance.051 and
the whole plan remain OPEN. No staging/commit/push, renderer, platform adapter
or business implementation. The lifecycle skill kept staged changes and scoped
evidence together; the user's focused-only test constraint takes precedence.

Final hygiene: framework git diff --check exit0,35 existing line-ending warnings
and no other findings; two edited consumer source/content paths exit0 without
warnings. Both repositories have zero staged paths. No live bounded-exact or
worker-exact dotnet processes remain. Goal stays ACTIVE, not complete/blocked.
