# P9-CT1 — Crossing traffic correction: focused and native verification

## Scope and actual native defect

2026-10-19+03:00, Windows/PowerShell/.NET SDK10.0.401, SELF-REVIEW only.
Framework master cb61483ab81404c11944931aedb4703ba694edb5; consumer master
8be1eed8b54649ac35cb1a2fe01051dd59d1982e. Dirty work preserved, staged0.
User-installed sdlc-orch snapshotfdc9dceb0bae90a05c1575cb033849daee0e6744;
same fully read core/specialist guidance. No framework copy/branch/PR/publication.
Original221/209 recorded checked/12 open/full goal ACTIVE, no original closure.

Unchanged consumer2.671 resumed its original Offshore literal from saved9567,
only fleet:<checkpoint>, outer150000ms/internal120000ms/termination5000/output18000.
Original60-day fleet deadline, Gulf96x96 seed1/company1, recipes/materials/time
unchanged; no rebuild/reset/admission/steering/grants/content changes.

|Interval|Result,worker/outer ms,stage ms|Consumer artifacts/marine-observations log|
|---|---|---|
|9567->11007|211/0,106206/106348,104698,exit0|f67382f6f6f2417e9f3f74c762e63a33/run.log|
|11007->12447|211/0,80253/80384,78814,exit0|f5d2bdc0a07b492caa066eb46776e5be/run.log|

Head1268->2708->4148/6000, no vessel. But jobs39/40/cargo40/0 retain EXACT
positions(78.277,77.500)/(78.277,78.500) across the latter full day. A green day/
hash/time check is NOT transport liveness. Stop blind days.

Existing traffic-trace:610... on the same literal, original30000ms/5000/output24000:
297/0,worker5614/outer5733ms,exit0; consumer log
artifacts/marine-observations/649e0633349d40dd92b4a57b780e0372/run.log.
Clone12447->12495, no new checkpoint, proves18-update recurrence at12447/12465/
12483 with unchanged jobs/cargo. Head4148->4196 is already-funded work, not delivery.
All original handles awaited to actual terminal; no restart on observation timeout.

## Generic reproduction and implementation

Reuse NativeTrafficTests.Factory/Journey/master-listed native-movement-traffic.json:
two real movers, two parked obstacles and ordinary commands. Half-minute/default
order reproduces fixed-goal parallel cycling; other order/quantum controls arrive.
Baseline945/5 includes actual arrival/clearance failure after240Updates.

Sole Mover.Advance now retains its own route when blocked ONLY by lower-priority,
execution-ready moving traffic (stable entity-ID tie-breaker). Other actors use
existing replanning. NavGrid's original observed entry pipeline supplies an
internal WAIT classifier, NEVER permission to step. Actual live CostFor/TrySegment,
occupant/sweep/corner/terrain/area/provider checks remain. No goal/position swaps,
teleport, new route/reservation/hash/clock owner, cached readiness or pathfinder.

First candidate630/0 cleared crossings, but expanded ordinary disabled-control
proof exposed1354/2: Moving intent is not execution readiness. Final classifier
uses SAME MotionExecutionContracts.Read.Blocker (rates/control/worker/area/fuel/
trip/dock). Idle/static/blocked peers and topology changes do not grant waiting
priority. Original route codec persists held intent; no new state/schema field.

## Every build and focused attempt

Only framework test-target Release --no-restore -v quiet builds: baseline18.91s,
candidate15.49s, expanded disabled fixture6.72s, readiness fix7.89s, choke-test
update5.52s; all exit0/0 warnings/errors. These were framework-only builds;
the later consumer target build is recorded below. No solution/full/
profile/family/default run. Six literals selected individually --bounded-exact,
--timeout-ms30000/termination5000 (separate CLI option/value), output12000 crossing,
8000 others. Framework logs artifacts/crossing-traffic/<directory>/run.log.

|Exact literal|Attempt/result,worker/outer ms,exit|Log directory|
|---|---|---|
|Native traffic: opposite crossings clear parallel approaches between parked obstacles|baseline2.671,945/5,1574/1724,1|010397184b17456b99ded4a48dc9ba69|
|Native traffic: opposite crossings clear parallel approaches between parked obstacles|first candidate2.672,630/0,1447/1692,0|b1f0320147de4d3abe94c0242887fe3d|
|Native traffic: opposite crossings clear parallel approaches between parked obstacles|expanded disabled peer,1354/2,1824/1993,1|f0f91bfcb40248a387444da5ed81a304|
|Native traffic: opposite crossings clear parallel approaches between parked obstacles|final765/0,1242/1391,0|f34e231255b64ea3b8d0be191f80805d|
|Native traffic: opposing slow vehicles clear a shared approach without oscillation|538/0,1453/1600,0|54f659c34b004c93a897a6d029ccb605|
|Native traffic: partial segment clearance is pure, saved and released by player stop|48/0,290/450,0|0d55fbfa4d514db3a2cefbf9ba75e4c0|
|Native traffic: independent clearance contracts reject mutation and malformed segments|22/0,283/436,0|108bf7f3189d400bb8bc1c2a47c894c4|
|Native traffic: temporary choke blockage waits for structural route to reopen|old waiting witness33/1,370/521,1|164a81241bfe43c08f72a21dab7bef96|
|Native traffic: temporary choke blockage waits for structural route to reopen|corrected physical-wait witness34/0,354/501,0|b07d4ff058384ac0b39c4a0faf7801fd|
|Native traffic: returning home and outbound movement clear adjacent slow approaches|227/0,825/977,0|98aee3e8ed4f4777bb4959ebd8be8f7c|

Choke actually arrived with unchanged positions/static-disconnection checks, but
its old witness only recognized discarded routes(PathIndex=-1). It now ALSO
requires an actual stationary Moving vehicle whose next LIVE entry is blocked.
Original pending branch,160Update arrival/saved equality/static closure remain.
This verifies intended retained-route waiting, not arbitrary success or weaker
collisions. No production edit after final readiness fix. Four passes were NOT
rerun after this test-only choke edit; those bodies/core are unchanged. Six final
selected scopes1634/0, not full provider/fairness/regression qualification.

## Identity and exact next native gate

Framework2.672.0/save160/replay1, no new state fields/component schemas/public API.
Core448 C# relative-path/hash LF digest:
792840cf63ab066893d371e97d3006288960708067be07d887d2a7008d5db1f8.

|File|SHA256|
|---|---|
|Core DLL|ADC5329E0BADE0667E7CCFC21C8239E6B15F47C1079B33A314FE09A0C5254546|
|Final Tests DLL|17F79A05628E4BBFC0E986A462A53A86BC6440D6E2A4B2FE53466EBFA3564CEE|
|ExtensionProof DLL|155D8858D9840CF832768B8DF423718FE9EA0DC9C93C512D2CFCF05DF049E76C|
|Historical consumer core2.671|FAB1C7824A1DF32F37F4576F93A9EEA4A4D2F4267DDF4558F6FF0C99A250E68E|
|Historical consumer PublicProof2.671|72056A13CCCA6B273A2E18E27E265376698DE8D693155878D58E6A3CF2B35CC7|
|Rebuilt consumer core2.672|ADC5329E0BADE0667E7CCFC21C8239E6B15F47C1079B33A314FE09A0C5254546|
|Rebuilt consumer PublicProof2.672|D931E017B614DDC9A225C7057D277AB195559E6043132F6D56018775D51067EE|
|Rebuilt OilCo.Game2.672|822C1939341480228BF4FE543AF521BBF06B8733EAA3CE582AAC23953A0F933E|
|Unchanged OffshoreTests.cs|27AA1A1FB3ABF51D52CA43285A2583CA22B9BFA41452A48ECE794E139C348876|

Historical2.671 artifacts/marine/610415a02f0b4af9a9154cebef8f9e64/checkpoint.json is
at12447/hashD1F7F5DB3998B10C,fleet_progress/next fleet_execution. JSON SHA256
F15FBE83EF57CA9F971740681B5274CC419DEF04860C418CF4D112AF376BA5D1;
game.sav1061DCD1ADD258F8013FF115A50BE3BF58617B744D2575B5F2D7258D99810F7D.
Preserve this cycle evidence; it is now incompatible with current2.672 binaries.
Never rewrite its header, credit old binaries with the fix or blindly advance it.

## Current2.672 native verification — 2026-10-19+03:00

Actual production correction justified ONE fresh native preparation; no reset
because of a failed test. Targeted consumer build ONLY:
`dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v quiet`
completed exit0/1.84s/0 warnings/errors. Copied core identity matches the tested
framework DLL above. No engine, consumer gameplay, content, observer or test-body
edits accompanied this build. Old2.671 and earlier images remain historical.

Original literal `Offshore: vessels survey, drill and ship from the sea` ONLY,
`--bounded-exact`, `--termination-timeout-ms 5000 --output-chars 18000`.
Prepare outer30000ms; explicit harbour continuations outer150000ms and unchanged
internal120000ms, one game day each, no full/default workflow. Original Gulf96x96,
seed1/company1/yard71,73/field21, all machines/materials/work and25/60-day deadlines
remain. Each next invocation consumes the actual preceding saved checkpoint;
no simultaneous gameplay workers, steering, grants or queue readmission.

Consumer logs: `artifacts/marine-observations/<directory>/run.log`.
Native images: `artifacts/marine/<directory>/checkpoint.json` plus original save.

|Input/time|Assertions,worker/outer ms,stage ms,exit|Log directory|Native directory/hash|
|---|---|---|---|
|prepare ->11|37/0,5075/5193,n/a,0|bae72337c2f347b980e361c451b08898|bace1f4a4700418fa60f9fd905359d97/A758028A5E3E7B7D|
|harbour:bace...11->1451|35/0,50748/50861,50028,0|2976bd9ee4f7498696d9d37cb1601d5f|64db3fb82be044e7b6dda0528e307fe4/2EC4336CCADCC849|
|harbour:64db...1451->2891|35/0,78255/78369,77450,0|69c40453b982436c89265d4076df83b1|4fa0815a22b24d3d819dd360508254b6/7BB9F98358A81ED2|
|harbour:4fa...2891->4331|35/0,24913/25039,24153,0|813fe6d9172f499ea3ad80f06761aad7|bdfc4122354b4ffbad84d3747a6f52f4/87E6004E1CF7EEA4|
|harbour:bdf...4331->5771|32/0,28074/28190,27305,0|c527ab3d7f67469e9b9551692d811007|3d837093a6d946b5884a401f3a5a4626/D702F5DA3EA5B127|
|harbour:3d8...5771->7211|26/0,23366/23483,22667,0|4b00c99b512c4fd8b4f34ad7fe770e46|39fa3a8a68fb4cc2b6b721d77f7ce62d/528271EC5592B92B|
|harbour:39f...7211->8111|23/0,16893/17008,16238,0|d9ac061444e64fa598dcd05be65ade9e|1aee1eee37d44d4d8d0b78ba3bd1f8b2/DF87EA156D6CE938|
|fleet-prepare:1aee...8111->8127|133/0,9308/9426,n/a,0|99a1867303844d05be216ab6ce0c7b39|ee08b5ded2034c9eae3ea41c2fb57f62/4DCED3C9D1ED2C9D|
|fleet:ee08...8127->9567|211/0,110498/110613,108975,0|41ee2c2c32364cc981e420cba645aeea|d3735decb5a34f2dace775e029627b47/34FA986292652D79|
|fleet:d373...9567->11007|211/0,116890/117002,115463,0|1dfe6b36a5eb4ce5b10a3407315f3d26|8576996fec014496aab0303a859df952/A83252930225CD43|
|traffic-trace:857...11007->11055|297/0,6088/6202,n/a,0|0a2287618ab94d4b98627f0bc1ae1a05|clone only; no new checkpoint|

First completed run was recovered from its captured terminal log (actual exit0),
with no matching live process, not restarted after losing the tool result.
The next same-session handles9024/79626/76145/72666/63472 were awaited to actual
exit0. All three actual harbour structures complete at8111; no construction
orders/jobs remain. Once-only fleet admission completes at8127 with seven original
queued recipes and actual first two hauling jobs20/21. Fleet handles48303/90154
were also awaited to actual exit0 before their next stages. No selected parent
or worker remains live. No new passing framework scopes were automatically rerun.

Eleven explicit invocations of the SAME original literal total1075/0; these are
assertions across bounded stages, NOT1075 independent tests or a full suite.
The first fleet day has the same1268/6000 work but a DIFFERENT actual traffic hash
from2.671; do not claim identical whole-game state or a controlled CPU speedup.
The second day PASSES115463ms under unchanged120000ms; it is slower than old2.671's
104698ms at the same game time. Current near-bound CPU/allocation cost remains
a risk, not a solved performance gate.

At11007 current trucks have progressed to jobs45/46, versus old2.671 jobs39/40
cycling in parallel rows. Actual truck6 is at depot73.500,70.500 unloading36.666,
and truck5 is moving74.442,72.442 on its next source trip. The unchanged48-update
trace proves cargo36.666->4.666 at the depot while the other truck physically
travels to77.500,79.500, arrives11050 and starts loading11051, reaching3.333 at11055.
These observed cargo/phase/position changes, not a passing hash or advancing
manufacturing counter, establish selected native logistics liveness beyond the
old blockage. The trace's297 assertions check load/queue/privacy/purity/time;
its actual printed physical facts supply the liveness evidence, not a generic
success count. No extra player command, free resource or manual steering ran.

Stage5 is VERIFIED for this selected crossing defect. Arbitrary congestion,
starvation, later workload/performance/P9.051/full fleet/sea/plan remain OPEN.
Head2708/6000 at saved11007, all seven original orders still queued, NO vessel yet.
The trace is a disposable loaded clone; it does NOT replace the saved continuation
with11055 or certify an extra day. Full100h vessel work,120s/60-day bounds, stock
and physical movement remain unchanged.

## CT1 handoff at11007 — subsequently continued

The next command below WAS RUN in the following goal turn. Current native state,
all later attempts and exact next action now have one owner:
[P5-MF1 continuation verification](P5_MARINE_FLEET_CONTINUATION_VERIFICATION.md#current2672-continuation--2026-10-19).
Do not resume this historical11007 pointer merely because it says compatible.

Preserve consumer `artifacts/marine/8576996fec014496aab0303a859df952/checkpoint.json`,
minute11007/hashA83252930225CD43, fleet_progress/next fleet_execution.
Checkpoint SHA2562004FC21ADC48B9828CC114D5F601866202D2FE20456FBC4AED47CBF89388861;
game.sav27DC7018590D1D8986AC5BE71E94F2BBEB811F8D94FAB5D1B186A1E684722BF9.
No rebuild/reset/prepare/fleet readmission/header rewrite was needed to continue
that compatible build. Historical command, now executed (not the next action):

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact 'Offshore: vessels survey, drill and ship from the sea' --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 18000 --case-input 'fleet:C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/8576996fec014496aab0303a859df952/checkpoint.json'
```

Its original handle was awaited to actual terminal. Inspect actual progress/cargo/jobs
before another day. A wall-time failure remains a failure even if a healthy save
is retained; diagnose rather than increase bounds or reset. Future manufacturing,
connected rollout/parking, marine work/recovery and broad original gates remain
necessary. No staging/commit/push before the full goal AND plan genuinely finish.

Self-review: physical permission stays unchanged; qualifier only retains a real
pending segment. Peer readiness is fresh/guarded through original owners. Selected
native delivery is now observed; starvation/many-agent stress, independent waiting-mode provider,
future budget/route complexity and performance/GC remain unverified. Existing
[operational/assurance matrices](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
apply: World.Fault/codecs/views/replay remain authoritative. No new I/O/catch,
channel, dependency, secret, UI, platform, simulation clock or Oil/business/observer
source changes. Broader audit/log/notification/provider/security gaps stay open.
No staging/commit/push before the full goal AND plan genuinely finish.

Historical pre-native hygiene2026-10-19T12:45:53+03:00:10 affected Markdown files/3529 local
links/0 broken; original221 requirements each matched once,209 recorded checked/
12 open/0 unmatched (not global runtime revalidation). Both original master HEADs
unchanged, staged0, framework3009/consumer101 dirty entries preserved. git diff
--check exit0 both roots,35/8 existing line-ending warnings. No selected worker
or parent remains live. No executable/test edit after final choke/home checks.
Source/consumer identities were deliberately distinct then; current targeted
build/prepare evidence supersedes that status above. No original gate closure.

Current final hygiene2026-10-19T13:03:48+03:00:11 affected Markdown files/4022
local links/0 broken. Actual primary checkbox mapping221 unique/209 recorded
checked/12 open/0 unmatched; historical audit JSON counts are NOT current runtime
qualification. The first read-only comparison used single lines and missed wrapped
requirements; joining their indented continuations and normalizing whitespace
matches every original requirement once. No primary requirement/checkbox changed.
Only selected subordinate P9-CT1 stage5 is newly checked from the native evidence.
Both original master HEADs unchanged/staged0, framework3009/consumer101 dirty
entries preserved. git diff --check exit0 both roots,35/8 existing line-ending
diagnostics. Copied coreADC5329E.../PublicProofD931E017.../observer27AA1A1F...
identities rechecked; no selected live dotnet parent/worker. Current source448
digest792840cf63ab066893d371e97d3006288960708067be07d887d2a7008d5db1f8 unchanged.
No executable/content/observer/test-body change after native build or during these
observations; documentation-only handoff edits require no automatic gameplay rerun.
No full/default/profile/family suite, wider completion claim or Git publication.
