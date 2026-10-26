# P9-RD1 — Ranked departure routing verification

> Historical2.665 evidence. [P3-GT1 current2.666 and all subsequent observations](P3_GOAL_DIRECTED_ROUTE_TIES_VERIFICATION.md) retains the subsequent2.665 active-day FAILED20/1 at2053, inspection18/0/profiled step24/0 to2054, and the actual new-build current native state. Old artifacts preserved/incompatible, never header-rewritten. This report's earlier one-day pass is not full active-stage acceptance.

2026-10-19. Engine2.665.0/save160/replay1.
[Design](../../plans/enhancements/P9_RANKED_DEPARTURE_JOURNEYS_DESIGN.md).
Implemented focused correction, NOT whole-plan or full-harbour completion.

## Outcome and ownership

The original WorkBoard.DockJourney.Finish now evaluates departure routes in the
existing Cheaper comparator's cost/identity order, rather than routing by ID and
discarding improving incumbents afterward. Every eligible exit still receives
its original guarded policy/cost reads in original order before ranking. The
first feasible ranked exit has the same winning rank. Necessary blocked-cheap
fallbacks still route normally. Total refusal retains sticky Pending, otherwise
the last ORIGINAL-ID refusal; the new evaluation order is not refusal order.

All selected routes and continuations use original motion/area/provider/space/
clock guards. Existing claims, fuel, docks, handling, work and travel timing are
not replaced. Call-local tuples are not persistent feasibility/route caches.
No second scheduler, route algorithm, inventory, map, clock or state machine.

Production changes: Standard/WorkDocks.cs and engine identity in Kernel/SaveGame.cs.
The same DockTravelSelectionTests owner gains a partial proof, with JSON fixture
variation through its existing strict reader. Independent PointMotion and
PointDock gain negative witnesses only, not gameplay shortcuts. Protocol fixture
build updated; no codec, save-format or component-schema change.
No Oil source/gameplay JSON, business, host, platform or Draw edits.

## Actual profiling evidence before the change

These completed observations used unchanged2.664, not2.665. Existing installed
dotnet-stack attached only to the identified actual --worker-exact child, with
PID/creation identity checked. Both bounded parents and children reached terminal
exit0; all three stack reports exited0. No tool was installed or full suite run.
Paths below are relative to the Oil consumer SimEngine root.

| Original source | Native result | Update ms | Case checks | Worker / outer ms |
|---|---|---:|---:|---:|
|00d5e0a0130a4f4eaaac783fef549b12,1453|534a730cb94343c4bd8b5c2c57f35d0f,1454,6B8A2FE637A3ED1B|619|24/0|2779 /2919|
|534a730cb94343c4bd8b5c2c57f35d0f,1454|8a3a9e2139184c5db1b65097c16382d0,1455,8251404071EF487D|696|24/0|2375 /2500|

Logs: artifacts/profiling/3d5529664dbd4c1faaadf396eb5e8a19/stack.txt and
observer.stdout.txt; artifacts/profiling/83cb518106a44d919a9e2566bda7768f/
stack.1000.txt, stack.1400.txt and observer.stdout.txt. The first sample landed
inside actual WorkSystem delivery -> LogisticsService.Unload ->
ResourceTransactions.Commit -> Storage.ReceiveReserved. The1000ms sample landed
inside actual Update -> ReplanDockBindings -> SelectRemainingJourney ->
DockJourney.Finish -> PlanRoute -> structural PathSearch -> guarded layered tile
condition/RequireSampleLayer. The1400ms sample instead landed in
SaveCheckpoint -> GameFactory.Load -> TileMap.Sync -> restored layer visibility.

These are three location samples, not a statistical attribution of dominant CPU.
Their timings are profiler-perturbed and are NOT benchmark/speedup evidence.
An initially oversized combined log read truncated; the SAME existing files were
then read in bounded portions. Profiling was not restarted on observation loss.
These2.664 artifacts are preserved historical/incompatible with2.665; no header
rewriting, legacy decoder or migrated-state claim.

## Every focused framework attempt

Only literal --bounded-exact selections,30000ms timeout/5000ms termination bound.
Framework working directory: C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "<literal below>" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 10000
```

| Literal | Build | Passed/failed | Worker / outer ms |
|---|---|---:|---:|
|Dock departure ranking: guarded cost order precedes route evaluation and preserves original refusals|baseline2.664|0/1|336 /555|
|same new ranking literal|final2.665|142/0|347 /512|
|Dock departure selection: dominated routes lose only after a feasible guarded incumbent|final2.665|78/0|278 /439|
|Dock travel selection: actual prospective cost precedes stable identity without displacing physical claims|final2.665|241/0|2303 /2461|

Baseline throws proof.dominated-departure-was-evaluated on the actually invoked
expensive early-ID exit. It is retained as NON-GREEN evidence, not omitted from
the result. Final three separate exact cases:461 assertions/0 failures. Output
caps for existing departure/physical cases6000/8000; none truncated or aborted.

New proof varies original/reversed authoring, cheap success with expensive route
poison, cheap route refusal with physically checked expensive fallback, distinct
all-refused typed results in reversed cost/ID order, sticky Pending, and actual
losing-exit cost mutation. It queries saved independent sessions, compares native
hash/time, withholds refused arrival/travel, and verifies a separate idle session.
Mutation fail-stops with no healthy save. Existing physical case retains ordinary
public hauling, partial saves/twins, slow handling, claims, physical departure,
resource conservation, privacy, replay and actual cost/reach callback failures.
No poison witness is substituted for the selected route or physical scenario.

Exactly TWO Tests target Release builds: baseline11.26s/final10.63s, both0 warnings/
errors. Each initially yielded a live handle (93926/86410); that SAME handle was
awaited to terminal success before runtime. No stale-binary test was accepted.

## Native new-build workload — actual limited acceptance

ONE consumer target Release build, MSBuild2.28s/command2.972s,0 warnings/errors:

```powershell
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v minimal
```

All four below are explicit invocations of the SAME original literal, NOT four
different cases or the full absent-input offshore workflow:

```text
Offshore: vessels survey, drill and ship from the sea
```

Use existing OilCo.PublicProof.dll --bounded-exact, termination5000ms, explicit
--case-input; prepare/single-update timeout30000ms/output12000, day timeout150000ms/
output18000. Original internal one-day stage120000ms/global25-day limit unchanged.

| Input | Passed/failed | Worker / outer ms | Actual result |
|---|---:|---:|---|
|prepare|24/0|6735 /6880|new native2.665 at11|
|harbour-step:764af.../checkpoint.json|24/0|2469 /2600|11->12,Update415ms|
|harbour:cc14e.../checkpoint.json|22/0|90599 /90755|12->1452,one day89445ms PASS|
|harbour-step:9290d.../checkpoint.json|24/0|3206 /3333|1452->1453,Update657ms|

All terminal exit0/no truncation,94 assertions across four invocations of ONE
literal. The day yielded live session74763; only that handle was polled to terminal.
No timeout restart, raised bound, concurrent build or second blind day.

Unchanged canonical Gulf96x96/seed1/two humans/player company1/native yard71,73/
field21. Full960-minute shipyard clearing earned, depot380/480 at1452, actual
hauling with truck#6 unloading14.666 material and truck#5 moving to next pickup.
At1453 depot381/480/truck#6 retains14.000. The shipyard still waits stock plus its
full3600-minute next stage; grid waiting worker. No completed harbour/fleet claim.

Native state hashes match the historical2.664 same-time results at11,12,1452,1453.
The observed day89445ms versus historical103500ms is only one sample per build,
not a controlled or general performance guarantee. Fresh-process active Update657ms
does NOT improve the earlier581/656ms observations. Significant active routing/
work/checkpoint cost remains open; do not claim solved harbour performance.

## Current native artifacts and exact next step

Under consumer artifacts/marine/; every entry has native save, in-memory and disk
reload/hash verification through the existing engine codec before publication.

| Directory | Minute | Hash | State |
|---|---:|---|---|
|764af49ab0df45b3b479bf32e38d1ac8|11|40370764A1DBECED|harbour_projects_admitted|
|cc14e92c66964e9f8abe0fbf0ec082ec|12|9DE9BDEA2B959F10|harbour_progress|
|9290d46127664f888913e745366aa5be|1452|212C83747276E486|harbour_progress|
|f193e6daeec044b586b258d0ed947f57|1453|91994FE145519DA0|CURRENT,harbour_progress/next harbour_execution|

Continue THIS compatible native state; no preparation reset or old-header rewrite.
Next address/observe actual active route/work/physical progress under original
bounds. Do not infer general deadlock freedom or full marine acceptance.

## Source and binary identities

Core448 C# sorted relative-path/hash LF digest:
91b14423a014287d97972bf01fe9cf71240826155c9b55656cc3ea6d14520099.

| Actual binary/source | SHA256 |
|---|---|
|Core DLL, Tests copied core, consumer copied core|A07083DB6CC8E8D9C421F098CD6B3419E2DB2871A9B3CD77D6A4F2BFB72B562D|
|Tests DLL|73B7FC7FEC85544C6897993CEDEE3DEE361DC231ED1FB699C987549A10C45B7B|
|ExtensionProof DLL|FBA5CA3E30C99D15B18789F26C82ADB84FC05339F21DC60DF2A3014DCE86EF12|
|Consumer PublicProof DLL|D7D0ED25A2F3EB7A2F2DE6B98142085735907147EDC2DEE3546D8FF72581AB74|
|Consumer OilCo.Game DLL|5AB7ACD3CA18DFE561E0507133580769B6FFAECE78CA1639F58B5483E5B912FB|
|Unedited OffshoreTests.cs|487EABB46683B8E6EF7FDF7C1D944ACF2EB6E9EB64A27D56DC8AD419F6DF55B8|
|Current checkpoint.json|4297BFBE4D414A8D3F45C0046348EA44BD5A6429DFADA9CF2D19874B36F64445|

## Remaining scope and audit

Original221 requirements matched once each against current plan text after
normalizing wrapped whitespace:209 currently checked/12 open. No original gate
text or checkbox changed by this slice. COMPLETION_AUDIT.json's historical195/26
snapshot is not current completion proof. An initial single-line-only regex
incorrectly missed wrapped requirements; the corrected wrapped-text scan finds
zero missing/duplicate originals. Checkbox counts themselves are not verification
of all earlier implementations or a full current regression.

P5.005/.008/.009,P6.007,P7.017/.019,P8.017,P9.051,P10.022 and
P11.026/.029/.035 remain open. P5 harbour-stage step4 remains OPEN.
No full/profile/family/all-tier tests; no broad gate closure or full goal claim.
Both worktrees remain dirty/preserved, nothing staged/committed/pushed.
Final git diff --check exits0 in both repositories; only existing LF/CRLF
warnings (framework35/consumer8), no other output. Both staged-file counts0;
no live bounded-exact/worker-exact dotnet process remains.
