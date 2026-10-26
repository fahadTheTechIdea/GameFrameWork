# P3-NT1 — Native traffic progress evidence

> Latest physical state:2.647 native harbour complete, two fully manufactured vessels, five pending. Artifact bdc06e51a0fa43d88b0d58410be66437 at18147/hash657409258DFA682B; loaded boat job79 blocked by idle output's home on the only marine service cell. P9.acceptance.051 reopened from actual evidence; original scope221/209 recorded checked/12 open. Initial timing failure retained; no broad regression/whole-goal completion claim. Details and the next owner audit are below.

Date2026-10-18. Status: generic engine correction implemented and focused checks
verified. Current Engine2.647.0/save160/replay1; mover4/movement6 unchanged.
No original gate closure. Earlier2.646 native checkpoints remain preserved and
incompatible by design; never rewrite their build headers.

The generic fixture reuses existing native motion, startup, time, content access
and work contracts. Geometry is explicit JSON through the selected master.
Journey tests submit two ordinary player movement commands; they do not steer
around collisions or update positions. All progress is existing Game.Update.
The choke test separately injects one explicitly authored static obstacle through
the framework's existing spawn/placement owner to prove static disconnection
still terminates Unreachable. This is topology fault injection, not a player
grant, replacement construction implementation or Oil gameplay change.

## Implementation

Native Mover derives in-flight adjacent-segment clearance from the ORIGINAL
route and actual position. No new persistent state, reservation ledger or clock.
NavGrid observes the actual ITileMotionClearance provider/owner/space and full
Entity.Sync through the original guard, rejects malformed geometry, and uses the
clearance only in dynamic costs. Structural route feasibility remains independent
of temporary traffic. A physically started diagonal reserves its native corner
cells too; ended/unstarted motion reserves none.

If dynamic replanning is temporarily impossible, Mover checks the SAME structural
route under the SAME authored area and navigation rule. It retains its original
goal pending rather than reporting permanent Unreachable while another vehicle
crosses a choke. Static topology failure still reports Unreachable. No host
steering, changed speeds/costs/work duration, grants, business or platform edits.

## Builds and retained failures

Targeted framework-test Release builds --no-restore succeeded in22.34s (baseline),
11.95s (first correction),6.53s,6.31s,7.36s and6.69s; each zero warnings/errors.
One intermediate build failed after3.18s because the test-only fault probe lacked
the Hosting namespace for GameClock; the import was corrected before execution.
No full solution/profile/family regression was run.

Baseline exact opposing-vehicles case: child34848,244 passed/1 failed,
719ms/observer880ms, exit1. Both positions/path prefixes repeat every20Updates
through240Updates. This is actual physical oscillation, not a stationary worker.

First correction intermediate: child35092,47/0,310ms/observer465ms. Subsequent
expanded intermediate finals: opposing case566/0 child25380,1534/1719ms;
clearance48/0 child22620,354/534ms; independent guard22/0 child12876,342/524ms.
These are intermediate attempts, NOT extra unique final coverage.

Choke baseline after segment safety alone: child13212,25/1,346/520ms, exit1.
Actual trailing mover#3 stops Unreachable at8.5,8.5 while leading mover#4 arrives
at8.5,3.5. After structural waiting, an intermediate choke44/0 child24148
603/787ms passed; the final case also adds real static-disconnection injection.

## Final exact framework evidence

Seven distinct literal cases,895 assertions passed/0 failed/0 aborted. Each
bounded runner exited0, stdout/stderr untruncated,5s termination grace. New cases
and layered case have30s process bounds; two selected physical/provider cases90s.
Repeated earlier passes above are not added to these totals.

| Literal exact case | Assertions | Child | Case / observer ms |
| --- | ---: | ---: | ---: |
| Native traffic: opposing slow vehicles clear a shared approach without oscillation |566|24328|1652 /1830|
| Native traffic: partial segment clearance is pure, saved and released by player stop |48|26216|341 /504|
| Native traffic: independent clearance contracts reject mutation and malformed segments |22|8536|308 /473|
| Native traffic: temporary choke blockage waits for structural route to reopen |47|16700|504 /651|
| Independent tile world: native layered routing, physical movement and public commands |82|20824|237 /397|
| Fleet navigation: whole selection, physical queued cargo and saved continuation |82|23544|41114 /41276|
| Layered navigation: authored roads, depth gates and physical saved work |48|23952|2942 /3089|

Opposing traffic covers both command orders and30/60second authored quanta,
actual collision-free arrival, every saved-twin update, mid-route resave, four
verified replays and interleaved-session isolation. Clearance covers cardinal
and diagonal cells, structural versus dynamic entry, own-segment exclusion,
query purity, saved state, clock pause and normal player stop/release. Independent
provider faults include original exception, Touch, hidden codec mutation, clock
mutation, invalid geometry and invalid/mutated status. Faults latch and cannot
resume work. Selected existing checks cover independent tile providers, queued
physical cargo, saved work and runtime flooding/replanning.

## Identity and consumer boundary

Core444-source path/hash-list SHA256:
`4dbc6c9e2743a5ac05fd9d7ae9874db21952cfce7713172eeabe410fa76e2446`.
Core DLL SHA256:
`B743B04154DF2AE2DEEF40229F64FD4DE7CD03D7678FC113CE27064BFFF1194C`.
NativeTrafficTests source SHA256:
`28E6AA522AF6BA575F9B2F3D45DBAB030585B3954A2BC1940BFA199352280091`.
Authored native fixture SHA256:
`5EFD537D6D76CDADD0D9E59ED39D7F5961E300A264EB56B494EFA038499444AC`.
Public-contract probe source SHA256:
`F39BDCE1538B123EF0282007852F4B99AA73FCBD4B6C28B5EE611EA7AAAEC871`.

Consumer target OilCo.PublicProof Release --no-restore builds in2.85s, zero
warnings/errors, without consumer source or game-content edits. Canonical marine
PREPARATION ONLY on the new build: child33620,24/0,8519/8674ms, exit0/untruncated.
Gulf96x96/seed1/two human seats/playing company1; ordinary founding and three
physical construction orders at game minute11/hashE7B4CC98D6977D89. The new
preparation checkpoint is actual consumer artifacts/marine/6f17e31580374535b99a6a272635dcc0/
checkpoint.json, harbour_projects_admitted/next harbour_execution. No completed harbour or fleet
is implied by this preparation. Bounded harbour observations follow below.

Preparation native save33058bytes SHA256
`7C1066FA960B7A3BA83EEEFC753A744617D1B29EEE76603017FF472191184FA2`.
Current consumer core DLL matches the framework DLL above. Consumer proof DLL
SHA256 `020C4A40A193462C5A790F93936FC52224B81CE045080051CA8FA2AA0E574788`;
unchanged OffshoreTests source SHA256
`5506D4DB87085640BAAB5D027540D73A34D5150FBC26698F8DEEEF9F143EFEC6`.

First bounded harbour continuation from11: child33736,20/1,
125825ms/observer125938ms, exit1/untruncated. Existing120s inner bound failed at
1391 after a complete60Update observation batch (elapsed124961ms). The original
180s process limit and5s termination grace were NOT increased. No engine fault.
Healthy actual progress was saved as harbour_progress/next harbour_execution,
hashFAB1081EB74896C5, actual consumer artifacts/marine/
ce25468352454ddeb8498eeae02de4a9/checkpoint.json. Its native save131761bytes SHA256
`1023BC8F34C4CBD610B31BFCA468E664F831460798F2DAA24EBDA6AB675D9DC4`.
Bulldozer performs depot clearing310/480; shipyard clearing completed and original
3600-tick erection waits supply. Loaded trucks#5/#6 actually reached the original
construction service approaches and are unloading in jobs8/9, phase3 with cargo
23.333/29.333. This is a FAILED bounded timing observation, not a passing one-day
acceptance or evidence of stalled/oscillating movement. Continuation resumes the
same earned checkpoint, not another preparation or a larger budget.

Second bounded harbour continuation from1391: child5808,22/0,
113873ms/observer113997ms, exit0/untruncated. Actual one-day observation completes
at2831 (inner elapsed112978ms), same120s inner/180s process/5s grace. No engine
fault. Trucks have physically delivered all currently required materials and
returned idle; crane#3 performs shipyard erection1261/3600, other two facilities
wait the same serial crane. No instant work, added stock, free machines or altered
duration. CURRENT native artifact is actual consumer artifacts/marine/
771d00f69ff94709a4c4361310fcd204/checkpoint.json, harbour_progress/next
harbour_execution, minute2831/hashFA8ABF67FE68F656. Native save136009bytes SHA256
`D5C934C3AD5C432CF8B0D78524CB2EFD8616FC46FB24EE0BDA6B01C985F37E2B`.
First timing failure above remains failed; one passing continuation does not
prove a zero-failure performance gate or a completed harbour/fleet. No workers
remain live after both bounded processes exited.

## Subsequent unchanged-build physical continuation

The continuation launched before the intentional interruption was collected
from its original exec session69224; it had exited normally, not timed out or
been restarted. Child8780,22/0,37429ms/observer37560ms, exit0/untruncated,
2831→4271. Crane erection2701/3600; no fault or completion grant. Actual native
artifact artifacts/marine/d9c40e465d1b4b88a97e4d6a51d461b2/checkpoint.json,
hashDA6F2FDF4D089D3D;136009bytes SHA256
`82F05900BA47E3FCACCE5190C622D51D562B7F2350845DF960B344A97395D6FA`.

Next same literal case, child39288,22/0,46752ms/observer46891ms,
exit0/untruncated,4271→5711, inner elapsed45741ms. Shipyard construction
completed as actual entity#13; original crane physically departed and moved
to depot erection469/1800. Grid connection waits the same crane. Native
artifact artifacts/marine/fcec7c831c144512bf9e5004dc7cddc4/checkpoint.json,
hash2DD410FA0D662223;135938bytes SHA256
`18E2EF4E5F3FEB7861C6AC1D69B6F960E7B48EB4E296A23644BFB26263C62562`.

Both use the ORIGINAL120s inner/180s process/5s grace and one-day observation
with the ORIGINAL25-day construction deadline. These are successive pieces of
ONE native workflow, not additional independent scenario coverage. No code,
content, clock, resource, recipe, duration or engine identity changes. No
full/profile/family run; the initial1391 timing failure remains recorded above.

Next continuation, child32840,19/0,50705ms/observer50832ms,
exit0/untruncated,5711→7151, inner elapsed49820ms. Actual depot#14 completes
through the same crane, which physically travels to grid erection80/960;
original grid order/job19 is the only remaining construction work. Native
artifact artifacts/marine/2981f9d5143c497b941b3d014d9302e6/checkpoint.json,
hashDB46A4F879FA7324;135906bytes SHA256
`3938B46DD38235EBEB540D62F822FF65833C3E7DBD847A0943ABFC6F8FDF5545`.

Final harbour continuation, child39456,16/0,52581ms/observer52704ms,
exit0/untruncated,7151→8051, inner elapsed50986ms. HARBOUR COMPLETE: actual
shipyard#13/depot#14/grid#15, no construction orders/jobs, original crane#3
physically returning home at74.585,73.5. Original clearing16/8/4hours and
erection60/30/16hours, material delivery, service/departure and conversions all
earned through the engine. Native artifact artifacts/marine/
f1edfd1de866488b8484c50cbb19dc0b/checkpoint.json, harbour_complete/next fleet,
hash1AD9492D39A6AF33;134888bytes SHA256
`1D00454D5BB99FFBEBE3DC4A634BF6B0EF3B945BD09E768884E7E78513F1C163`.

Original once-only fleet admission, child40796,126/0,6840ms/observer6980ms,
exit0/untruncated. Same MarineRun admits the same9 public standing routes and7
original recipe orders at8051→8067. Native source route chronology again gives
fleet start8067/deadline94467 (original60days). Queue executions1–7 with all
original durations; survey vessel head0/6000 waits physical supply. No vessel
spawned early. Native artifact artifacts/marine/a206637f9ec94cc1b0d8e058a65a79e1/
checkpoint.json, fleet_admitted/next fleet_execution, hash1F1CACD16040CE47;
138059bytes SHA256
`D47DE1FAD9C99500B385123D51A21D969F123500533C7C5D0F68D98EBC667501`.

All subsequent observations above use unchanged source/core444 digest and DLL
identities already recorded. OffshoreTests source SHA remains5506D4DB...F143EFEC6.
No build, consumer source/game-content edit, header conversion or additional test
family was needed. These results complete the current harbour, NOT the fleet,
offshore workflow, initial timing gate or any full original acceptance item.

First new-build fleet day, child37780,204/0,80065ms/observer80217ms,
exit0/untruncated,8067→9507, inner elapsed77971ms. Same seven original recipes,
head survey_vessel Working1292/6000. Actual truck#5 has reached depot service at
74.5,70.5 and is unloading in job34 (phase3, loaded14.666); truck#6 loads the
next shipment in job35. Earlier jobs33/34 and the old repeating approach positions
are NOT retained as a host workaround. This is real physical route/job progress
beyond the reproduced old2.646 traffic cycle. Native artifact artifacts/marine/
011d9761d5e545ef89db90214befa158/checkpoint.json, fleet_progress/next fleet_execution,
hash154E7D9636D9ABD0;138320bytes SHA256
`B726E84F7498EEECDE26CDC609E0A5B96928724348C92F3468DD9395F2CA5151`.
First vessel/full fleet not completed by this observation. All deadlines and
process bounds unchanged; this is another slice of the SAME native workflow.

Second new-build fleet day, child10024,204/0,70642ms/observer70774ms,
exit0/untruncated,9507→10947, inner elapsed68864ms. Head Working2732/6000;
shipments have advanced to jobs47/48, not the former stationary33/34 pair.
Truck#6 unloads at74.5,72.5 and truck#5 physically approaches the depot from the
west at73.5,73.075 with22 units. This confirms continuing delivery cycles,
not only one successful approach. Native artifact artifacts/marine/
6e0b671d527b4cefb57a8c8c1603eca3/checkpoint.json, fleet_progress/next fleet_execution,
hash9F83E8D33CE81450;138907bytes SHA256
`7315C986DC3880B8EDD37D4D42B9CE96C2B1EE468238F8FCFFB2FF227BF2A8BC`.

Third new-build fleet day, child38576,204/0,92999ms/observer93640ms,
exit0/untruncated,10947→12387, inner elapsed88678ms. Survey vessel head
Working4172/6000; actual shipment jobs61/62, truck#6 unloading at74.5,72.5,
truck#5 approaches with30 units at74.631,73.5. First six per-Update observations
also show physical arrival/loading transitions at the depot rather than the
old back-and-forth route prefix. Native artifact artifacts/marine/
e5ed1c6cc9274f87a2f7b2b639505922/checkpoint.json, fleet_progress/next fleet_execution,
hashCBCA14024E012C3D;140515bytes SHA256
`90DFA451FED828D191FE46C9432E5743F6D5605CB4AAB061740DEA4044CC7FEF`.

Fourth new-build fleet day, child40272,204/0,43886ms/observer44032ms,
exit0/untruncated,12387→13827, inner elapsed42059ms. Survey vessel head
Working5612/6000. ALL active haul jobs have settled; both trucks physically
return idle at76.5,79.5 and76.5,80.5. This is stronger than replacing a reported
route: the native supply workload actually drains after repeated deliveries.
Native artifact artifacts/marine/c52af445d0814be18e39c93fc1179f91/checkpoint.json,
fleet_progress/next fleet_execution, hashE6D25CA4E4BC204F;141256bytes SHA256
`3AE3743B75D13ADD556AFB1F75C97E4F3855A85A32FA992E2CF97DC9EB348385`.

Fifth new-build fleet day, child38496,204/0,51982ms/observer52138ms,
exit0/untruncated,13827→15267, inner elapsed50104ms. Survey vessel#16 has
completed its FULL100-hour work and physically rolled onto water at70.5,73.5.
The next original supply-boat recipe is Working866/3600, not the old2.646
head0/materials blockage at the same game minute. No haul jobs remain active;
the trucks have actually delivered and returned idle. Native artifact artifacts/
marine/eaf504349c344c6092e5048b8b2025d8/checkpoint.json, fleet_progress/next
fleet_execution, hash3BE9A8ED1B87EFCB;142718bytes SHA256
`D12963C45E33E031B0BCF93264D01E18FFD96BE537838A4C5234929EB5469A7E`.
Six vessels/full fleet remain uncompleted at this checkpoint. This directly
contrasts the former2.646 minute15267 state, which had the first vessel but
the next recipe waiting stock behind the reproduced truck oscillation.

Sixth fleet day, child39604,204/0,31223ms/observer31355ms,
exit0/untruncated,15267→16707, inner elapsed29528ms. Supply boat Working2306/3600,
no active hauling. Native artifact artifacts/marine/
848e675fe53744a68f29d7e532dcc3ba/checkpoint.json, hashC9D74785CC695DCA;
142718bytes SHA256
`DF82ECFDBB9C0C168FC18FCAD02A8C734A8F47CFCDA7D6C0381FC078D4317C80`.

Seventh fleet day, child34740,204/0,46390ms/observer46522ms,
exit0/untruncated,16707→18147, inner elapsed44586ms. SECOND vessel supply_boat#17
has completed its FULL60-hour work and is on water at70.5,72.5. Original queue
is executions3–7, third platform crane vessel head0/8400 waits materials.
New loaded job79 has60 units on supply boat#17, depot#14→shipyard#13,
phase2, Paused/blocker.unreachable/retry18202. This is NOT the earlier moving
truck cycle; the original workload now reveals a separate occupied-service
berth problem. CURRENT native artifact artifacts/marine/
bdc06e51a0fa43d88b0d58410be66437/checkpoint.json, fleet_progress/next fleet_execution,
minute18147/hash657409258DFA682B;144446bytes SHA256
`8374F549B96191419A375B2FF8BADA0D771C1A0CA9F84914DF9C5CB65F7887E1`.
Two of seven vessels completed, not a full fleet acceptance. Seven successive
fleet204/0 results above are seven observations of ONE case, not seven new suites.

## New actual obstruction: home versus service berth

Stopped blind advancement once job79 exposed the obstruction. Original read-only
inspection: child31424,12/0,1512/1631ms, exit0/untruncated,30s bound. Structural
route read succeeds and selects70,73, exactly occupied by idle survey vessel#16.

Extended ONLY the existing inspection branch in OffshoreTests: enumerate actual
owned entities, query WorkView.ReadHome to identify supported workers, and print
their existing MotionView facts; sample the existing public water layer around
the two facility footprints. No command, Update, placement decision, mover,
recovery rule or game state in the inspector. The retained hash assertion stays
unchanged. Targeted consumer build4.18s, zero warnings/errors. Expanded inspection
child11808,12/0,1760/1871ms, exit0/untruncated,30s bound, same native hash/time.

Actual facts:

- Survey vessel#16 home#13, AtHome/no blocker; physically occupies70,73.
- Supply boat#17 home#13, Working/no execution blocker; carries the same job79 cargo.
- Shipyard fringe70,73,7x6 has exactly ONE water cell:70,73. Every other sample
  is dry. Depot fringe has water access at70,71 and70,72.
- Loaded delivery's prospective structural route still selects70,73. A movable
  occupant is ignored for structural feasibility, but real delivery cannot
  overlap the idle vessel. No cargo was discarded or teleported.
- Transformations.cs Deliver assigns queue.Home or producer.Id to a manufactured
  worker. WorkerHomeContracts.ReadCurrent/UpdateIdle uses ordinary service contact
  for AtHome and therefore leaves this first output in the only marine berth.
- WorkSystem.DeferHaul first asks existing recovery, otherwise retains the loaded
  job with authored backoff; this ordinary job is not a selected recovery-kind
  job. Oil work retry explicitly permits blocker.unreachable with1minute initial,
 2x growth and1hour maximum. Retrying alone does not clear the idle home berth.
- TileMap.FindRollout currently scans successive rings for vacant placement;
  its current CheckRolloutPlacement verifies occupancy/unavailable geometry,
  not a connected producer/service/idle-egress contract. The second output at
 70,72 is outside the producer's only wet contact. This path needs review together
  with home/port admission, not an Oil-specific steering workaround.

Inspector source SHA256 now
`12E449306E2B666D6318AFFB85D784B20F80B55216C45C1B8AB31170436A15B3`;
proof DLL SHA256
`77F69478D82F32027D2707D1871E39220BB3B27EBF0075706877F669E92077D7`.
All advancing observations above used the earlier unchanged proof source/DLL;
only the final expanded READ-ONLY inspection used this new diagnostic build.
Engine/content identities and current native artifact remain unchanged.

[Next engine stage](../../plans/enhancements/P9_PRODUCER_BERTH_HOME_DESIGN.md).

## Limits and next action

This proves the reproduced generic cycle and transient-choke behavior, not
deadlock freedom for every fleet/topology or the full marine workflow. No global
traffic coordinator, passing-place/backtracking policy or indefinite-conflict
resolution is claimed. Wider provider/performance/soak/fault matrices remain open.
Current canonical HARBOUR is complete on2.647. The same native run has TWO fully
manufactured vessels and five pending. Next reproduce/fix the actual producer
home/service/egress conflict through existing engine owners/contracts and authored
content, retaining this artifact for diagnosis. Do not keep advancing long days
through the occupied berth, steer vessels from the driver, cancel loaded cargo,
rewrite headers, restart preparation without a necessary identity/content change,
reset original deadlines or widen budgets. Do not infer full marine/performance
acceptance from metadata and native checkpoint assertions.

Fresh original checkbox audit221/209 recorded checked/12 open after reopening
P9.acceptance.051. The current occupied producer/home/service berth prevents the
previous broad recovery/no-deadlock claim from being accepted. Original scope
and wording are preserved; earlier focused results are not deleted or inflated.
Diff hygiene exit0,35 pre-existing LF-conversion warnings and no other output;
staged0. No full/profile/family test, consumer logic/content change, business/
platform/Draw work or publication. Full fleet and original scope are not proven.
