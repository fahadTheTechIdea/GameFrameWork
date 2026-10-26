# P3-GT1 — Goal-directed deterministic route ties

> Historical2.666. [P5-CB1 current2.667, every subsequent failure and current checkpoint](P5_CONSTRUCTION_COMPLETION_BOUNDARY_VERIFICATION.md) supersedes continuation: the later2.6666372 artifact is SCENARIO INVALID/missing shipyard and must not resume. Actual cause was future dock clearance blocked by a later site; generic placement correction is scoped-verified, current full native finalization still open. Never rewrite old headers or infer completion from healthy hash alone.

2026-10-19. Current engine2.666.0/save160/replay1.
[Design](../../plans/enhancements/P3_GOAL_DIRECTED_ROUTE_TIES_DESIGN.md).
Focused implementation verified; original full goal and harbour acceptance OPEN.

## Outcome and implementation boundary

The sole PathHeap.Compare now orders estimated total cost ascending, travelled
cost descending, row ascending, column ascending. For equal f=g+h, larger g
means smaller remaining h. This reduces broad equal-cost plateaus without
altering cost/heuristic mathematics, sparse scratch, closed-set rules, query-local
memoization, expansion limits or any provider/space/clock/occupancy/area guard.
No second pathfinder, route cache, world grid, clock or controller was added.

The same authored96x96 plateau queries previously read1926/326/326/1884 unique
tiles; now326/326/326/326. All retain optimal cost960 and80 actual adjacent steps.
All four now find the goal within the same80-expansion TEST budget; two directions
previously exceeded it. This is an authored fixture assertion, not an engine
size/route cap or a guarantee for arbitrary blocked/weighted maps. New standalone
algorithm callback counters do NOT prove native callback purity; separate actual
NavGrid fault and physical saved-work tests cover those scopes.

Equal-cost selected paths may intentionally change. Build identity advances to
2.666; no compatibility loader, header rewrite or promise to retain old paths.
Save160/replay1 and component codecs unchanged. Production edits are only the
existing Navigation.cs comparator/documentation and SaveGame.cs identity.
Existing CapacityLogisticsStressTests gains a partial proof and explicit embedded
navigation-goal-ties.json; no parallel test scheduler or game logic in a host.
No Oil source/gameplay JSON, business, platform or Draw edits.

## Pre-change native continuation — failure retained

Started from the actual2.665 checkpoint f193e6daeec044b586b258d0ed947f57,1453.
Existing copied binaries/core/source digest matched before the run; no rebuild
or repeated passing framework cases before this observation.

All three are explicit inputs to the SAME original literal:
`Offshore: vessels survey, drill and ship from the sea`.

| Input, engine2.665 | Passed/failed | Worker / outer ms | Actual result |
|---|---:|---:|---|
|harbour:f193e.../checkpoint.json|20/1|123784 /123926|one-day observation FAILED at122774ms,600 minutes earned|
|inspect:00393.../checkpoint.json|18/0|1689 /1822|pure actual state/route inspection, no commands or Update|
|harbour-step:00393.../checkpoint.json,profiled|24/0|2505 /2647|actual Update824ms,2053->2054|

Day process bound150000ms/internal stage120000ms unchanged. Live session44222 was
polled to terminal exit1, never restarted on a wait timeout. Healthy progress
was retained by the existing native writer BEFORE the original failure was
reported; this is NOT a passed day. At2053: full depot/grid clearing finished,
shipyard erection507/3600, loaded truck#6 carries16 toward grid, truck#5 moves to
next pickup. Inspection shows actual structural route to grid available, no
fault, truck remaining movement estimates4/32. No evidence of a current deadlock.

Historical native artifacts under consumer artifacts/marine/:
00393ca3b0034aac8c8a279ea9137344 at2053/hashCCDB43B1F9789AD5;
748334a63b1f41aeafad46128b6c1922 at2054/hash0748168D86EE6F2C.
Both preserved but INCOMPATIBLE with2.666, not erased or header-rewritten.

One installed dotnet-stack diagnostic attached to the verified actual child8388
of parent40936 at requested child-age offsets1200/1600ms. Both reports exit0;
parent and child terminal exit0. Logs under consumer
artifacts/profiling/43107afc1a3c4d33bf5f1c014cd66370/:
stack.1200.txt, stack.1600.txt, observer.stdout.txt, observer.stderr.txt.
First stack: StateHasher.Bytes -> ByteLayer.SyncValues -> full TileMap observation
verification -> layered cost -> NavGrid.CanReach -> TileWorkDock.Check ->
WorkBoard.SelectDock -> ReplanDockBindings -> WorkSystem.Update -> World.Step.
Second stack instead samples actual saved reload/snapshot visibility publication.
These are two location samples, not dominant-CPU statistical attribution, and
profiled824ms is not an unperturbed benchmark. No profiler install/full suite.

## Every focused framework attempt

Exactly two Release Tests target builds: baseline8.17s/final8.94s, both0 warnings/
errors and terminal exit0 before runtime. No executable edits after final build.

Each runtime selects ONE literal with --bounded-exact, timeout30000ms,
termination5000ms, output9000 for new case/6000 oracle/6500 other cases.

| Literal | Build | Passed/failed | Worker / outer ms |
|---|---|---:|---:|
|Navigation goal ties: equal cost frontier remains optimal deterministic and bounded|baseline2.665|38/8|43 /190|
|same goal-tie literal|final2.666|46/0|31 /185|
|Navigation diagonal costs: rounded authored edges retain optimal deterministic routes|final2.666|360/0|45 /210|
|Navigation exact goals: blocked admission preserves guarded costs budgets and region alternatives|final2.666|25/0|167 /315|
|Layered navigation: authored roads, depth gates and physical saved work|final2.666|48/0|2355 /2497|

All final processes terminal exit0/no truncation/abort. Four separate exact cases,
479 passing assertions, NOT a full/family/profile run. Baseline38/8 is retained:
two read-budget failures, two bounded-search failures, two missing path-repeat
results and two original heap tie-order assertions. No assertion was relaxed.

New case covers four symmetric authored directions, once-per-query cost reads,
legal adjacent route/goal/rounded cost, bounded/unbounded identical paths,
zero-budget refusal with no old prefix, primary estimated-cost precedence and
stable row/column ties after progress. Existing weighted Dijkstra oracle checks
nonuniform/blocked/rounded/maximum-cost/directed/area goals and repeated queries.
Existing exact-goal proof keeps actual callback faults and region/budget refusals;
native layered proof keeps changed terrain/depth/routes, physical time, saved
twins and replay. Those existing cases were unchanged, not weakened for new ties.

Query timings in milliseconds, first query includes first-use/JIT work:
baseline14.527/0.330/0.257/3.845; final7.519/0.247/0.240/0.246. No unstable timing
assertion or universal speedup claim; deterministic read counts are direct proof.

## New-build native observation

One consumer target Release build, MSBuild1.84s/command2.395s,0 warnings/errors:
`dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v minimal`.
No consumer source/content changed. Earlier incompatible native state was NOT
silently loaded; current progress was earned anew through existing commands.

All three again select the SAME original offshore literal, not its absent-input/
full workflow. --bounded-exact, termination5000ms, explicit --case-input.
Preparation/single-update process bound30000ms/output12000; day150000ms/output18000.
Original internal day120000ms/25-day game deadline unchanged.

| Input,engine2.666 | Passed/failed | Worker / outer ms | Actual result |
|---|---:|---:|---|
|prepare|24/0|6129 /6240|native11/hash40370764A1DBECED|
|harbour:54cab.../checkpoint.json|22/0|83308 /83428|11->1451,one day82469ms PASS|
|harbour-step:c9479.../checkpoint.json|24/0|2318 /2446|1451->1452,actual Update468ms|

70 assertions across three explicit invocations of ONE literal, not70 cases or
full marine completion. Day session31948 was awaited on that SAME live handle
until terminal success. No timeout restart, concurrent build, raised bound,
smaller map, extra stock, cheaper work, faster clock or driver movement.

Unchanged canonical Gulf96x96/seed1/two humans/player company1/yard71,73/field21.
Full960-minute shipyard clearing earned. At1452 depot380/480, truck#6 unloading
14.666 material and truck#5 moving to next pickup. Shipyard waits actual stock
and its full3600-minute erection stage; grid waiting worker. No harbour/fleet
completion. First day82469ms and single Update468ms are current observations,
not a controlled same-start multi-sample speedup comparison. The new second
active day has not been run; earlier2.665 active-day failure is not erased.

## Current checkpoint — resume this exact state

Consumer artifacts/marine/ directories, using original native codec and whole
hash/in-memory/disk reload verification before publishing each intent manifest:

| Directory | Minute | Hash | Stage |
|---|---:|---|---|
|54cab68e803a4dabbea07ab524128da0|11|40370764A1DBECED|harbour_projects_admitted|
|c94796d662634a3f9b2dee0602810399|1451|66D8457C44AC7FF6|harbour_progress|
|17ec0ca7986e4326b1ce60684febf3ad|1452|212C83747276E486|CURRENT,harbour_progress/next harbour_execution|

Next continue THIS native state under original bounds. Do not regenerate
preparation, rewrite old headers, or infer full active-work/traffic acceptance.

## Source/binary identities and remaining original scope

Core448 C# sorted relative-path/hash LF digest:
fe794ac57c03bb3ce2aacdf2aa2d686859c4077b46b87e026b397a55353353bc.

| Actual binary/source | SHA256 |
|---|---|
|Core,Tests copied core,consumer copied core|8DE129F8099BC8B9DF77CEF06765FC284E9D4C4CD0B28F16DBA6DF6AA873D7BE|
|Tests DLL|33162787CA8EB250A5CCAB040E16A2B3344CDE4E72F5EB70C2E172945497ECFF|
|ExtensionProof DLL|A9D999FAAE1F80D7FA8F66A072F9799623691788C18DB25C6104471D74B529BD|
|Consumer PublicProof DLL|DB6E5971A1B0EA762427A416A149C5888BEE4E6834B03E6385B0ADD30C79D614|
|Consumer OilCo.Game DLL|7A34535EDA43BD71FA49764DB05D0754F67862BE91319AFEFE9B0D3E5FFF7869|
|Unedited OffshoreTests.cs|487EABB46683B8E6EF7FDF7C1D944ACF2EB6E9EB64A27D56DC8AD419F6DF55B8|
|Current checkpoint.json|0B46F8438516C50B94C1BC01ECAEB444249957ADF116843F97A8F81D73FC84C2|

All221 original requirements still match once in current plan documents,209
checked/12 open, no original text/checkbox changed. Counts are NOT fresh runtime
proof of all previously checked work. P5.005/.008/.009,P6.007,P7.017/.019,P8.017,
P9.051,P10.022,P11.026/.029/.035 remain open. P5 harbour step4 remains OPEN.
Full goal ACTIVE; no full/profile/family/all-tier run or full acceptance claim.
Dirty worktrees preserved; no staging/commit/push before whole goal/plan finish.
Final git diff --check exits0 in both repositories, only existing LF/CRLF
warnings (framework35/consumer8), no other output. New stage/report/proof/JSON
files have no trailing whitespace matches. Both staged-file counts0; no live
bounded-exact/worker-exact dotnet process remains.
