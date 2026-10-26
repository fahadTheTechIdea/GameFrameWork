# Current native harbour workload — engine2.664

> Historical2.664 evidence, superseded by [P9-RD1 current2.665 native state and all attempts](P9_RANKED_DEPARTURE_JOURNEYS_VERIFICATION.md). Later2.664 profiling reaches1455; preserved, incompatible with2.665. New-build day passes at89445ms with identical1452 hash; current compatible native checkpoint is f193e6daeec044b586b258d0ed947f57 at1453. No completed harbour/general speedup claim.

2026-10-19; observations continued across the local date change.
Unchanged engine2.664.0/save160/replay1.
[Existing stage](../../plans/enhancements/P5_MARINE_CHECKPOINT_CONTINUATION_DESIGN.md).
PARTIAL physical progress; full harbour/fleet/marine workflow and full goal OPEN.

## Outcome

ONE original game-day observation passes the unchanged120000ms stage bound:
minute12 ->1452,103500ms stage/104451ms bounded outer process. The original Gulf
fixture remains96x96/seed1/two human seats/player company1, yard71,73/field21.
No smaller map, faster clock/work, extra stock, instant placement or different
business mode was authored. Ordinary founding, public placement/work commands,
physical workers/logistics and native saves remain engine-owned.

The engine earns the full960-minute shipyard clearing stage. At1452 the next
shipyard stage still needs stock and its full3600-minute authored work; depot
preparation380/480, grid project waiting for its physical worker. Truck#6 is
actually unloading with14.666 material remaining; truck#5 physically approaches
its source for the next haul. No completed shipyard/depot/grid or fleet is claimed.

The active-hauling single Update is STILL expensive:581ms, then656ms in an
independent saved replay of the same next step. These are separate fresh-process
observations, not a timing guarantee or controlled old/new A/B comparison.
Do not claim that allocation improvements solved CPU or whole-harbour performance.
No second long day was started after this high-cost active stage was measured.

## Source and build boundary

No executable/source/gameplay JSON changes in either repository during this
revalidation. Only framework plans/evidence/status documents were edited.
The existing OilCo.PublicProof target was rebuilt against current framework;
hosts/demos remain thin. The proof used its existing explicit prepare, harbour
and harbour-step inputs, not its absent-input/full offshore workflow.

Working directory:
C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine.

```powershell
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v minimal
```

ONE target build, MSBuild2090ms/command2647ms, terminal exit0/0 warnings/0 errors
before any runtime. No framework test suite or unrelated consumer proof executed.
Copied consumer core now matches the actual2.664 framework DLL, not prior2.659.

## Every targeted runtime observation

All five invocations select the SAME original literal:

```text
Offshore: vessels survey, drill and ship from the sea
```

Every command uses --bounded-exact, --termination-timeout-ms5000 and explicit
--case-input. Output cap14000 for preparation,16000 for single updates and18000
for the one day. Preparation/single-update process bounds30000ms; day150000ms.
Original internal day120000ms/one-day observation/global25-day deadline unchanged.

| Input | Passed/failed | Worker ms | Outer ms | Actual result |
|---|---:|---:|---:|---|
|prepare|24/0|6512|6686|new compatible native preparation at11|
|harbour-step:66c50.../checkpoint.json|24/0|1864|1981|11->12; actual Update298ms|
|harbour:fef480.../checkpoint.json|22/0|104330|104451|12->1452; original one-day stage103500ms PASS, harbour still partial|
|harbour-step:10fdec.../checkpoint.json|24/0|2084|2223|1452->1453; actual Update581ms|
|same harbour-step:10fdec.../checkpoint.json|24/0|2249|2392|same1453/hash; actual Update656ms|

All exit0, stdout/stderr untruncated. Day invocation yielded live session88788;
all subsequent waits used THAT SAME handle until terminal success. No restart
on an expired observation, no timeout increase, no interrupted physical workflow
passed off as successful. No heavy source audit/build ran concurrently with the
timed day. Fresh step re-execution was deliberate saved-next-state verification,
not an automatic rerun of the long day.

118 assertions across FIVE targeted invocations of ONE case, not118 independent
tests, five different cases or a full regression. The one-day pass does NOT close
full harbour, production, fleet, offshore survey/drilling/piping or shipping.

At11, ordinary returning workers have home-read samples1.485–382.007ms and the
following Update298ms. At1452, idle home reads10.214–15.491ms but the active
Update581ms. The independent repetition reports idle reads10.297–15.966ms,
Update656ms. These observations localize remaining cost to actual active work/
route/coordination rather than proving idle parking or every native update cheap.
They do not replace a fresh current-runtime stack/cost diagnosis.

## Native artifacts and exact continuation

Artifacts are under the consumer's existing artifacts/marine directory. Every
manifest is an intent reference to an opaque native game.sav, not copied engine
state or a driver-owned progression model. Existing SaveCheckpoint verifies
in-memory/disk reload and whole-state hash before publishing the manifest.

| Directory | Game minute | Expected hash | State |
|---|---:|---|---|
|66c50b6870bd4bc290549bd0e74fb633|11|40370764A1DBECED|harbour_projects_admitted|
|fef480a808754341945c046b2c31b41f|12|9DE9BDEA2B959F10|harbour_progress|
|10fdec6195a7499e9bea6d7381994e1a|1452|212C83747276E486|harbour_progress|
|5996fe9ca42044378ca507c2aae4bb83|1453|91994FE145519DA0|harbour_progress|
|00d5e0a0130a4f4eaaac783fef549b12|1453|91994FE145519DA0|CURRENT, same verified saved next state|

CURRENT compatible manifest:
C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/00d5e0a0130a4f4eaaac783fef549b12/checkpoint.json.
Next stage is STILL harbour_execution. Depot preparation381/480, truck#6 unloading
remaining14.000, no fault. Both1453 game.sav files are exactly134223 bytes and have
identical SHA256 below; only their artifact-directory references differ.

Continue this CURRENT native state under2.664 rather than regenerating another
seed or repeating preparation/day1. If runtime identity changes, preserve these
as historical/incompatible; NEVER rewrite save headers or bypass native checks.

## Final identities — SHA256

Unchanged448 core C# sorted relative path/hash LF digest:
7ab60feff685fb948b0e216855a4beb53d1b9c5560e654e183e930d36fbff453.

| Artifact | SHA256 |
|---|---|
|Actual framework and copied consumer core|0B0F09D651442E4A9FAE695E84CC6FA80342F07DA975525F50B30EF09DCCB177|
|OilCo.PublicProof DLL|A5838BF97A61C3813D4C8BF64A9292561A7EACAC19D132DF74B371C152E966A5|
|OilCo.Game DLL|EEDBE18479562BF261180828EC3FF566CC5E6ABD2B5802D6508E70106A7F8700|
|Unedited OffshoreTests.cs|487EABB46683B8E6EF7FDF7C1D944ACF2EB6E9EB64A27D56DC8AD419F6DF55B8|
|Both independently produced1453 game.sav files|D98E76BBC4033808AB1E685619F70746D40CAFFAB28DC80DEDCCE09E7BC93434|

## Remaining work and audit

P5-MC2 step4 remains unchecked: physically complete the original harbour under
original bounds. Active581/656ms updates remain a performance concern. Next work
is a bounded current-runtime route/work cost diagnosis on this saved state before
another long observation; keep route/fuel/claim/policy/departure checks and full
provider/space guards. No blind repeated day, cached world-hash authority, smaller
fleet, extra materials or deadline inflation.

Fresh original221 text/checkbox audit209 recorded checked/12 open, unchanged.
No broad original requirement closes from this one workload segment. Full goal,
full/profile regression (not authorized), broader provider/accounting/control/
isolation matrices and complete marine workflow remain OPEN.

No staging/commit/push until entire goal AND plan finish. Framework diff --check
exit0/35 existing LF warnings/no other findings; staged0 in both repositories;
no live bounded/worker processes. Previous2.659 and older saves/results remain
preserved historical/incompatible, not current acceptance.
