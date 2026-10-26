# P5-CB1 — Protect actual and pending facility clearance

> Continuation completed in [P9-HI1 current2.668 evidence/handoff](P9_HOME_APPROACH_INTENT_VERIFICATION.md). Subsequent2.667 continuation failed two original day bounds and revealed an18-update loaded-traffic cycle at3553. Old saves/data below are historical/incompatible, not header-rewritten. The2.668 correction now physically completes the shipyard at5153 and full harbour at8111 through original rules/timing/bounds. CURRENT compatible b63abed3fb504a71ba4f7a1ea7b5c13b/checkpoint.json/hashDF87EA156D6CE938 is harbour_complete/next fleet. Earlier generic234/0 evidence remains historical, not rerun; current417/0 targeted parking proofs and253/0 native stage assertions have their own exact scopes. Full marine, arbitrary clearance providers/fault/stress and broader acceptance remain open.

2026-10-19. Current engine2.667.0/save160/replay1.
[Stage](../../plans/enhancements/P5_CONSTRUCTION_COMPLETION_BOUNDARY_DESIGN.md).
Placement correction implemented and scoped-verified; new native finalization,
full harbour/fleet and original goal remain OPEN.

## Actual failure and cause

Continuing the real2.666 harbour exposed a correctness failure beyond its known
active-hauling performance failure. Shipyard erection progressed3386/3600 at4932,
but by6372 neither the pending site nor completed structure existed. The next
strict checkpoint load correctly refused12/3 before any Update. An ended order
was initially described as completed construction; that interpretation is
explicitly withdrawn. No actual completed shipyard was proven.

The SAME MarineRun observer gained explicit harbour-boundary:<checkpoint> input:
same strict native load, same RunTicks loop, original limits, stop on site
disappearance and read public company notifications. From valid4932 it reports
at5153 note.order-failed/build:shipyard followed by core.blocked. At5172 the
completed-structure assertion fails17/2 and no new verified checkpoint publishes.

Source confirms why. TileWorkDockDef.CheckTilePlacement validates the new
facility's slots against current static bodies. TilePlacementRules did not
check the reverse direction, nor the final target's auxiliary geometry behind a
pending IConstructionSite. The shipyard's future access.18/access.17 tiles70,76
and70,77 are inside the later grid site's69,76,2x2 footprint. The site-only dock
layout did not protect these future completed-facility slots. At earned
finalization the existing dock constraint correctly refuses core.blocked; the
authored retry policy does not retry it, so ordinary failure cleanup retires the
site. Do NOT disable that constraint or fabricate a built entity in the driver.

ConstructionDeclaration already correctly binds sim.construction.finish.
Its standalone authored work profile intentionally has null OnComplete; no
completion binding/JSON was changed based on that false lead.

## Engine correction and responsibility

Map/TilePresence.cs adds ITilePlacementClearance to the existing placement
constraint family. It declares inverse obstruction checks for authored auxiliary
geometry. Native TileWorkDockDef implements it from the SAME immutable Slots
and TryTile geometry, not a second clearance list or JSON schema.

Sole TilePlacementRules now refuses a permanent blocking footprint that occupies
existing or pending facility clearance. Existing geometry is observed through
actual ITilePresence; pending target is the existing IConstructionSite.Target
contract, guarded and checked against actual frozen compatible metadata. Other
facilities use their actual definition. Clearance callbacks use original whole
provider/entity/space/clock observation, including target state. Replacing skips
only itself. No native construction/site class lookup, copied reservation map,
planned-definition alias, second placement owner, guard exemption or route cache.

Only permanent blocking candidates use this inverse check. Movable vehicles and
nonblocking cargo are not banned from service tiles. Declared clearances may
still share space with one another; a blocking body cannot destroy them.
Cancellation releases protection by retiring the actual site; no ghost ledger.
No map size, placement search radius, resource amount or work duration changed.

Build advances to2.667, save160/replay1/component schemas unchanged; previous
2.666 saves remain historical/incompatible, never header-rewritten or repaired.
Production changes are TilePresence.cs, TilePlacementRules.cs, TileWorkDocks.cs
and SaveGame.cs. No Oil gameplay/business JSON/code, platform or Draw changes.

## Thin observer correction

Only consumer OilCo.PublicProof/OffshoreTests.cs changed outside the framework:
boundary input plus public notification diagnostics. No steering, spawning,
material grants or finalization logic was added. Existing project-presence and
full authored-duration assertions are extracted ONCE into ValidateHarbourProjects,
shared by LoadCheckpoint and SaveCheckpoint. A healthy native hash is not enough
to publish a missing scenario goal as verified progress. Strict loader validation
remains unchanged in meaning. Bad historical6372 artifact is retained as failure
evidence, not an accepted next checkpoint. Initial boundary case aborts before
publication on missing built result; ordinary publication now preflights too.

## Every2.666 native observation before correction

All inputs select ONE original literal:
`Offshore: vessels survey, drill and ship from the sea`.
All --bounded-exact with explicit --case-input and termination5000ms. Day process
150000ms/internal120000ms/global25-day bounds unchanged/output18000. Inspect
30000ms/output18000; boundary30000ms/output16000. No full/default workflow.

| Input | Passed/failed | Worker / outer ms | Actual result |
|---|---:|---:|---|
|harbour:17ec0.../checkpoint.json|20/1|126441 /126581|FAILED125359ms,1452->2052,healthy partial|
|inspect:4a54c.../checkpoint.json|18/0|1620 /1742|pure actual truck/route inspection,5/33 movement remaining|
|harbour:4a54c.../checkpoint.json|22/0|43140 /43260|2052->3492,42173ms day; haul backlog drains|
|harbour:4058a.../checkpoint.json|22/0|33469 /33599|3492->4932,32598ms day; erection3386/3600|
|harbour:d7129.../checkpoint.json|22/0|38392 /38516|4932->6372,37552ms day; missing result NOT noticed by old writer|
|harbour:1824a.../checkpoint.json|12/3|774 /893|strict load rejects missing shipyard/location, no Update|
|harbour-boundary:d7129.../checkpoint.json|17/2|11019 /11142|4932->5172,exact failure notifications5153,no artifact|

These include failures, not an all-green acceptance. No timed-out observation
was restarted; actual live sessions55498/56849/26499/68604/19374 were each
awaited on their SAME handles to terminal. After the first timeout, inspection
confirmed real reachability/progress before the next stage; no blind reset/day.
The later healthy hash did not prove goal completion. No fleet work began.

Artifacts under consumer artifacts/marine/, all preserved historical:

| Directory | Minute | Hash | Disposition |
|---|---:|---|---|
|4a54c0eb0c94460ba0228e0fad9fdd97|2052|550F0B976C840DD9|healthy retained partial after FAILED observation|
|4058acda209c440bb14389850505e496|3492|017534CAEF6FA734|healthy physical progress|
|d712966a8c864107acde1c09bf58bd33|4932|EE8083B4E6517C49|LAST VALID2.666 state before failure reproduction|
|1824aa966f5c4b3a87dbe52c7e24b772|6372|C578098B84E82624|SCENARIO INVALID,missing shipyard,DO NOT RESUME|

## Focused generic regression and existing physical checks

The SAME AuthoredConstructionWorkflowTests owner gains a partial clearance case.
Its existing JSON factory selects two normal dock offsets on the target (service
and departure), not on its pending site. Existing startup JSON can author an
already built control; pending variant uses normal ConstructCommand. Both use
the native modules, original stock/workflow and public previews/commands.

Each exact case uses30000ms timeout/5000ms termination, output9000 for clearance/
independent or10000 for original authored workflow. No family/profile/full run.

| Literal | Attempt | Passed/failed | Worker / outer ms |
|---|---|---:|---:|
|Construction clearance: pending and built facility access survives neighbouring placement|baseline2.666|32/8|404 /545|
|same clearance literal|initial2.667,preview scope|40/0|378 /528|
|Authored construction workflows: shared DAG, physical tools and staged removal|2.667 existing case|61/0|454 /608|
|same clearance literal|final strengthened command scope|52/0|393 /543|
|Independent tile construction: physical material supply and native project completion|final2.667|121/0|976 /1168|

Final distinct selected scopes:52+61+121=234 passing assertions/0 failures.
Initial40/0 is an intermediate version of the SAME case, not a fourth final case.
Baseline eight actual wrong admissions are retained, no assertions relaxed.
Original61/0 was NOT automatically repeated after test-only command assertions
and their compile correction; production core stayed identical.

Final clearance proof covers pending AND built facilities, both service/exit
positions, saved twins, allowed adjacent ground, pure hash/time, actual inbox
refusal with no allocated site identity/result, resumable refused state,
cancellation and real subsequent placement, and isolated idle session. Existing
authored case retains held tools, timed staged material consumption, saved partial
finalization/twins, full removal and cancellation. Independent tile case proves
physical supply/project completion through non-native space contracts.
Arbitrary clearance-provider fault/private-state/stress matrices remain OPEN.

## Every build attempt

Tests target Release --no-restore: baseline7.69s success; production9.05s success;
strengthened-test compile4.94s FAILED (NextIdentity is EntityId, not int, causing
CS0029/CS0411); corrected test6.23s success. All0 warnings; successful builds0
errors. No test ran against the failed compile/stale binary. Correction used the
actual EntityId type, not a cast/compatibility workaround.

Consumer target Release --no-restore: boundary diagnostic2.76s success on2.666;
current production4.23s success; shared publication-assertion helper1.71s success.
All0 warnings/errors. No full solution/test build or suite.

## New native2.667 observation — not full finalization proof

Unchanged canonical Gulf96x96/seed1/two humans/company seat1/yard71,73/field21,
no mandatory Tycoon/cash. The engine's existing SuggestSites/Preview now chooses
grid origin75,69 instead of69,76, with no scenario/content edits or driver
placement rule. This is an actual current engine decision, not a manual relocation.

Same original literal and explicit inputs, prepare30000ms/output14000,
day150000ms/output18000, single step30000ms/output13000, termination5000ms:

| Input | Passed/failed | Worker / outer ms | Actual result |
|---|---:|---:|---|
|prepare|24/0|7570 /7684|new compatible native11,valid different grid location|
|harbour:560be.../checkpoint.json|22/0|74063 /74179|11->1451,day73196ms PASS|
|harbour-step:7cea7.../checkpoint.json after shared writer validation|37/0|2620 /2738|1451->1452,Update486ms,load AND publication assertions|

83 assertions across THREE invocations of ONE literal, not a full suite or
completed harbour. Increased final step count comes from stronger writer
preflight, not new independent cases. Day live session96141 awaited to terminal.
No timeout increase or concurrent build. Timings are not a controlled speedup
comparison; current layout changed legitimately through corrected admission.

At1452: full960-minute shipyard clearing earned, depot380/480, real loaded truck
unloading14.666 and another travelling to pickup. Shipyard still needs stock and
its full3600-minute erection. Grid waiting its worker. New-build native shipyard
finalization at the former failure boundary and complete harbour/fleet are
UNVERIFIED, so the stage's final acceptance box stays open.

## Current compatible native state and next action

Consumer artifacts/marine/:

| Directory | Minute | Hash | Stage |
|---|---:|---|---|
|560bef3b77d2424787f23e3ebac936c0|11|F77979F78C72758E|harbour_projects_admitted|
|7cea712fa3ea43bda38fa659b745528d|1451|7142F2E3C9CCDF91|harbour_progress|
|d82556b34a8b4b09bd6f3885e7736856|1452|EBB39CE65AFBF335|CURRENT,harbour_progress/next harbour_execution|

Resume THIS actual state under original bounds, not preparation or old2.666
headers. Native writer verifies whole save hash/in-memory/disk reload; final
publication now also verifies actual scenario project presence/durations.

## Exact current identities and original scope

Core448 C# sorted relative-path/hash LF digest:
6a41cff7164f4520b4472899f1057fab47f3e34a3e8a7e6b4224ae9fcab88f58.

| Actual binary/source | SHA256 |
|---|---|
|Core,Tests copied core,consumer copied core|FEB4BF5FA55F73076DCB17DCC13D67CDC2E19F0E2BD337475DDE9215AAB9AD94|
|Tests DLL|3F695362059AB42FF11C15798BFABA322815BC99B3976E0127CF379A09AD261E|
|ExtensionProof DLL|28E479E6826B5256BB9F46F451A38E8731D37198D1EC55DC2234092C13FAA8A1|
|Consumer PublicProof DLL|D7C2A3B6702FD5B356CC5A289C06FDA99CDEA6BF83E7E19220F66F60D0732F8F|
|Consumer OilCo.Game DLL|61A9693FEDD1CFB610AA2493F7A006F6314EEACDD4270E56521CB10CFFBD5828|
|Observer OffshoreTests.cs|89720A5C30F1812B0B679444BCA978144C181317D8C71D65AE1015E78BE32CCF|
|Current checkpoint.json|1EACA19E95C6F450FAD6BE714F0E04C0B96A16CA67F5C0A0A080FD2AC4715199|

Original221 requirements match once each in current documents:209 checked/12
open, no original requirement text/checkbox changed. Counts are not fresh proof
of all previously checked work. P5.005/.008/.009,P6.007,P7.017/.019,P8.017,P9.051,
P10.022,P11.026/.029/.035 remain open. Full goal ACTIVE. No full/profile/family/
all-tier runtime, full harbour/performance claim, business/platform/Draw changes
or staging/commit/push. All dirty unrelated work and historical artifacts kept.

## SDLC self-review and precise handoff

Workflow entry: user-installed sdlc-orchestrator, shared snapshot
C:/Users/f_ald/.agents/frameworks/agentic-sdlc-framework/snapshots/e567f84738e3cd3342ae45891823ad8e1287a6ef.
No project-local pin was found in either target root. No framework copy or
second application tracker was created. Relevant implementation, verification
and handoff procedures were loaded; review below is SELF-REVIEW, not an
independent review or release approval.

Reviewed2026-10-19T09:25:24+03:00, Windows/PowerShell, .NET SDK10.0.401;
framework netstandard2.1 and focused test host net8.0. Framework branch master,
HEAD cb61483ab81404c11944931aedb4703ba694edb5; consumer branch master,
HEAD8be1eed8b54649ac35cb1a2fe01051dd59d1982e. Both remain substantially dirty;
the tested core/binary digests above identify the working state, not clean HEAD.
User authorization is the existing implement-plan request; commit/push is still
conditional on WHOLE goal/plan completion. No branch/PR/publication authorized
or performed merely by adopting the workflow.

| Criterion | Change and observed check | Status | Remaining limitation |
|---|---|---|---|
|P5-CB1.1 actual failure|Strict old6372 load12/3 and boundary notification5153/core.blocked,17/2|Observed failure reproduced|Historical2.666,not a current native pass|
|P5-CB1.2 prevent conflict|ITilePlacementClearance/TilePlacementRules,baseline32/8 -> final52/0|Verified focused admission scope|Arbitrary provider/fault/stress matrices not run|
|P5-CB1.3 preserve timed work|Existing authored61/0 plus independent physical121/0|Verified selected physical scopes|Not all projects/providers/current canonical finalization|
|P5.005/P9.051 full acceptance|New native preparation/day/step83/0 and retained current save|Partial|Full finalization,harbour/fleet,recovery/deadlock matrices open|
|P6.007 regression|Not run|Not run|User explicitly prohibits full/profile/family runs|

Self-review findings: the inverse check lives in the existing placement owner;
JSON slots and actual IConstructionSite.Target remain authoritative. A proposed
second planned-definition alias/clearance ledger was rejected as redundant.
Allowing shared auxiliary tiles while excluding permanent blocking bodies is
intentional; mobile traffic retains existing claims/collision rules. The actual
root conflict is fixed at admission, not hidden by weakening finalization or
changing an authored retry policy. O(number of live entities) placement scans
and untested arbitrary clearance callbacks remain explicit performance/provider
risks; no general scalability or independent-mod certification is claimed.

Runtime output tables above derive from actual terminal/tool results. Not every
earlier stdout was separately archived as a raw local log, so no machine-validated
EVIDENCE.json record or authenticated CI evidence is claimed. Native checkpoint
files and source/binary digests are retained local evidence; full completion
cannot be inferred from their existence or structure alone.

Exact next action, after rechecking branch/tree/core/copied binary identities,
from consumer C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine:

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 18000 --case-input "harbour:C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/d82556b34a8b4b09bd6f3885e7736856/checkpoint.json"
```

Expected observation: real hauling/construction progress under original bounds,
or a retained typed failure to inspect. Do not restart preparation, reuse old
headers, repeat already-passing framework cases automatically, or declare the
new native shipyard finalized until its actual completed entity is observed.
