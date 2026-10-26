# P9-RS1 — Guarded dock-bay ranking verification and handoff

> Current engine2.682: [Engine implementation/focused proof](../../docs/migration/P3_OBSERVATION_HASH_COST_VERIFICATION.md#current2682-visibility-facts--2026-10-21); [sole current native state and next action](../../docs/migration/P5_MARINE_FLEET_CONTINUATION_VERIFICATION.md#current2682-physical-continuation--2026-10-21). No full test suite or Git publication. Earlier build-specific records below are historical evidence, not current execution instructions.

## Native2.671 follow-through — current stage

2026-10-19+03:00, same source/core digest and original master HEADs below.
No production/test/content/observer edits or framework-suite repeats this turn.
The previous goal turn made real source and focused-proof progress. Current
work executes its pending native stage5, not a reset caused by a failed test.

Targeted consumer build from actual consumer SimEngine root:
`dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v quiet`.
Exit0/0 warnings/errors,2.72s. Actual copied core SHA256
FAB1C7824A1DF32F37F4576F93A9EEA4A4D2F4267DDF4558F6FF0C99A250E68E
matches the tested framework2.671 binary. Consumer PublicProof SHA256
72056A13CCCA6B273A2E18E27E265376698DE8D693155878D58E6A3CF2B35CC7;
OilCo.Game3661E2A8D9D922760B3FBE11E566FB4D34F1DD2B8D80D5F3905631CC3F9C00A6.
OffshoreTests.cs stays27AA1A1FB3ABF51D52CA43285A2583CA22B9BFA41452A48ECE794E139C348876.
Build stdout observed, not separately archived. Older2.670 images are now
historical/incompatible with the rebuilt consumer; preserve them without any
header rewrite or compatibility fallback.

Same literal `Offshore: vessels survey, drill and ship from the sea`, explicitly
--bounded-exact with --case-input prepare, then harbour:<published checkpoint>.
Preparation outer30000ms, harbour150000ms/internal120000ms, termination5000,
output18000. All original Gulf96x96/seed1/two humans/company1, physical duration,
quantum, materials, machines, placement and total25-day harbour deadline unchanged.
Raw consumer logs artifacts/marine-observations/<directory>/run.log.

|Input/game time|Assertions,worker/outer ms,stage ms,exit|Log directory|Native artifact/hash|
|---|---|---|---|
|prepare ->11|37/0,5103/5231,0|ac2a913be6e048328b4aedbfbea78c07|abe7bd49205d4d149f36964ba9462493/A758028A5E3E7B7D|
|harbour:abe...11->1451|35/0,49487/49612,48735,0|12299c0e850f4afe8d22d8d72a0651f7|b7776d1a1f174560a41d9725e07d0f86/2EC4336CCADCC849|
|harbour:b777...1451->2891|35/0,76115/76238,75212,0|8d1a5610675d4d74bb3d5dadf1dc2ce3|07097c5c45594ff2a89044c12505d7fd/7BB9F98358A81ED2|
|harbour:070...2891->4331|35/0,23144/23255,22383,0|bb1a6a03fbdd4126a1b379d78db22bd4|f638dd244b334b0a9121b3b3c51df869/87E6004E1CF7EEA4|
|harbour:f638...4331->5771|32/0,26730/26840,25969,0|66d4e6ad415b44e99c39f8881a9a6c91|9a80e0dad6d749db9a049327e78abba9/D702F5DA3EA5B127|
|harbour:9a80...5771->7211|26/0,22309/22421,21622,0|86c7dba87c954ebe82844f898b881819|c573d5d428f44f8ca9ae696e377ae817/528271EC5592B92B|
|harbour:c573...7211->8111 COMPLETE|23/0,16001/16131,15303,0|ba1e6190148a41cab23acd0fddccbf35|f7844cfbf239446da13ed283f49edbfd/DF87EA156D6CE938|
|fleet-prepare:f784...8111->8127|133/0,9036/9148,0|cb390db49a0448f5b61358d6a674f198|62c2f4d4707e40f790d59b1c2647fa9e/4DCED3C9D1ED2C9D|
|fleet:62c...8127->9567 full day PASSED|211/0,113800/113915,112330,0|9c8451240b8342d288268162a0158bd6|8be38c0b32dd4267871c50de85cfad65/31D2C58FE3414413|

First day reaches the exact prior2.670/2.669 same-time game hashes; actual
materials are hauled and clearing work advances, not instant completion.
Its48735ms versus prior54305ms is a10.3% lower single observed elapsed time,
not statistical or universal performance proof. No sampling or other gameplay
tests ran alongside this current workload. Busy day75212ms versus prior81960ms
is8.2% lower in this observation, with the exact same2891 game hash. The following
day advances full crane erection time to2785/3600 and retains the same4331 hash.
Preparation/six bounded harbour stages223/0 now prove all three actual facilities
completed at8111 with no construction orders/jobs, same exact prior game hash.
Original nine standing routes and seven full-duration vessel orders were then
admitted once at8127 with the same prior game hash; no manufactured vessel yet.
The first complete fleet day now PASSES112330ms under the unchanged120000ms
internal limit,211/0/actual exit0. It reaches the EXACT same9567 game hash as the
old2.670 failed120532ms day, with the same1268/6000 head progress/cargo/positions.
Observed elapsed is6.8% lower at the same workload boundary; original failure is
preserved, not rounded down or hidden. This single observation is not statistical
or universal throughput, allocation/latency/soak or full fleet/sea acceptance.

All nine explicit current native stages567/0, no aborted/timed-out/killed worker.
Only the original literal was selected, not the default full workflow or a suite
family/profile. Original handle16582 was awaited to actual terminal, and final
Win32_Process check found no selected worker/parent. Core/source/content/observer
remain unchanged since the prior five exact framework checks; those were not rerun.
Selected P9-RS1 native measurement stage5 is now verified; no original compound
requirement closes and the full goal remains ACTIVE.

### Current compatible state and next action

Consumer artifacts/marine/8be38c0b32dd4267871c50de85cfad65/checkpoint.json,
minute9567/hash31D2C58FE3414413,verified_stage=fleet_progress,
next_stage=fleet_execution. First vessel1268/6000, original seven orders remain,
no manufactured vessel yet. Truck5 unloads21.333, truck6 moves with28 actual
cargo. All three harbour facilities truly completed. JSON SHA256
1D3B8384D166920DB6A3A1AE3324589397F826036D0180C8E159CC0BD233875F;
native game.sav FD9D46965C9B632C42FCFC98C9A5642D46AC684FE3BDDD691637E5DD3C63D5CA.
JSON/game-state hash matches old2.670 at the same boundary; native build header is
new and was NEVER rewritten. Preserve this actual current progress, not old e65...
or another preparation. No live worker. Next bounded fleet continuation from
actual consumer SimEngine root, NOT RUN:

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 18000 --case-input "fleet:C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/8be38c0b32dd4267871c50de85cfad65/checkpoint.json"
```

Do NOT rebuild/reset/prepare merely to continue this unchanged image, readmit
routes/recipes, raise120s or the original60-game-day fleet deadline, shorten
work, add resources, steer workers manually or add driver gameplay. Full fleet/
sea, arbitrary mod behavior, original P5/P6/P9/durability/stress/whole performance
and original221/209 recorded checked/12 open remain OPEN. No commit/push before
the full goal AND plan are genuinely complete.

Current follow-through hygiene2026-10-19T12:17:36+03:00:10 affected Markdown
files/3518 local links/0 broken; original221 requirements each matched once,
209 recorded checked/12 open/0 unmatched (not global runtime revalidation).
Both original master HEADs unchanged; staged0, framework3007/consumer101 dirty
entries preserved. git diff --check exit0 in both roots,35/8 existing line-ending
warnings. No selected process remains live. Core448 digest and copied-core
identity above unchanged. No executable/test/observer/content edits or full/
profile/family/framework runtime reruns during this native validation turn.

## Historical pre-native implementation record

## Scope, revision and authority

2026-10-19+03:00, Windows/PowerShell/.NET SDK10.0.401, self-review only.
Framework master cb61483ab81404c11944931aedb4703ba694edb5; consumer master
8be1eed8b54649ac35cb1a2fe01051dd59d1982e. Existing dirty work preserved,
both staged0. No branch/PR, staging/commit/push, Oil/gameplay/content/business,
renderer/platform/Draw or public API change. Whole goal remains ACTIVE.

Active user-installed sdlc-orch snapshot is fdc9dceb0bae90a05c1575cb033849daee0e6744;
this turn started under dcecb5fc3475d427be3ec421f3f19ef833c85d38, then507f3f6bcf13006883101e0eea1f3f746566de5c.
Changed entry/orchestrator/handoff instructions were fully read; relevant unchanged
core/specialist files were byte-compared to the fully read prior versions. Wizard,
project setup, technology migration, publishing and multi-agent work are N/A.
Existing tracker, contracts, authorized Git scope and original gates are retained.

## Actual current-consumer diagnosis, before implementation

Framework artifacts/native-cpu-sampling/aa1f4d149cd7451aba418426bb40853f contains
run.log/run.err, worker.json, traffic.nettrace, collect.log and top reports.
Existing consumer2.670 binaries and original9567 checkpoint were verified before
launch. No healthy live worker existed, so there was no process to resume or replace.

Exact child selection, from consumer SimEngine root:

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 24000 --case-input "traffic-trace:C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/e65fce222fc3408da5eec112e79a2ebf/checkpoint.json"
```

Parent19008 started hidden with owned process handle; Win32_Process verified actual
child33160, parent, exact DLL/--worker-exact/input before attaching the already
installed isolated Microsoft dotnet-trace10.0.731102. No new tool/package install.
`collect --process-id 33160 --profile dotnet-sampled-thread-time
--duration 00:00:00:10 --output <artifact>/traffic.nettrace` exit0; child exited
before requested10s collection, so this is NOT a complete10s sample. Reports:
`report <trace> topN --number 25 --verbose`, and the same with --inclusive, exit0.
CPU sampling is a diagnostic, NOT a test profile or broad filtered suite.

Original short trace297/0, worker8289/outer8607ms, actual terminal exit0. Clone
advances9567->9615 through48 ordinary engine Updates; no new native checkpoint,
commands, job creation policy, manual steering, extra clock, or altered game rules.
Original native e65... remains unchanged. Actual jobs/cargo unload and new haul
assignment progresses, first recipe1268->1316/6000; not a traffic cycle.

|Observed instrumented metric|Actual result|
|---|---|
|48 Update elapsed|mean119.452875ms,median24.3455,min19.459,max3110.349,sum5733.738|
|48 current-thread allocations|sum471476176B,mean9822420.333,min4054312,max190393096|
|Update9600|3110.349ms/190393096B; finished haul32 replaced by actual new job34|
|CPU sampled SelectDock|52.74% inclusive|
|CPU sampled PathSearch.Find|51.18% inclusive|
|CPU sampled StateHasher.Bytes|49.73% exclusive|
|CPU sampled TileMap.Sync/provider hashing|68.79%/72.39% inclusive|

The mean includes the outlier; it is not discarded to claim performance acceptance.
Sample also includes initialization and explicit diagnostic view/hash reads;
profiler overhead and a short phase limit causal/general conclusions. Do NOT
compare these proportions directly to the earlier2.669 full-day sampling as a
controlled speedup. No claim that every outlier byte belongs to SelectDock.

Source inspection identifies a concrete independent gap: SelectDock only prunes
expensive journeys after an ID-ordered feasible incumbent. An expensive first-ID
bay still runs before a cheaper feasible late-ID bay. Existing exit ranking is
already cost-first; retained-slot and PathSearch caches already exist.

## Implementation and behavior

The sole original WorkBoard.SelectDock now groups its existing ordered slot list
by kind, performs original request/policy/claim/queue/vacancy/cost checks in stable
ID order, then ranks eligible local tuples through original Cheaper. First fully
feasible DockJourney/continuation wins. A feasible service never probes unused
waiting kinds. Existing retained commitment path is unchanged.

Original-ID indices preserve Pending precedence, otherwise the FIRST failure,
with any retained failure first. This deliberately differs from DockJourney.Finish's
LAST-ID convention. Cost, selected ID, geometry and travel publish only on success.
No new route/hash/stock/claim service/cache, codec, coordinator, clock or fallback.
OrderedSlots simply returns its already-created List for indexed grouping.
Candidate storage is transient query arithmetic, not authority between queries.

Original DockTravelSelectionTests Factory/content composition reused; one new
literal and point-provider negative modes distinguish early-ID expensive route,
cheaper refusal/fallback, Pending, opposite typed total refusals and cost mutation.
Existing test bodies/assertions were not weakened; point hooks only activate for
the explicit test authoring. No second scenario/factory or production backdoor.

## Every build and selected check

Framework `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`:
baseline8.35s/final8.36s, both exit0/0 warnings/errors. No solution/full/profile/
family/default test run, no automatic passing-case repeats, no consumer rebuild.
Raw framework logs: artifacts/ranked-dock-journeys/<directory>/run.log.
Each literal used --bounded-exact, `--timeout-ms 30000 --termination-timeout-ms 5000`;
output8000 for new ranking,6000 for existing checks (separate CLI option/value).

|Exact literal|Result,worker/outer ms,exit|Log directory|
|---|---|---|
|Dock candidate ranking: cheaper feasible bays precede expensive identities without changing refusal precedence|baseline2.670,0/1,279/440,1; losing-service-was-evaluated|081dafe8557e48a59355c83a970ada70|
|Dock candidate ranking: cheaper feasible bays precede expensive identities without changing refusal precedence|final2.671,194/0,320/474,0|06abc0a5a8b646a5879edd86856b3401|
|Dock candidate selection: full eligibility precedes pruning losing service journeys|142/0,307/486,0|270edd5be6734742b0c32fa755d04e83|
|Dock travel selection: actual prospective cost precedes stable identity without displacing physical claims|241/0,1896/2047,0|71b2a2bbd934450fac57c762016539d9|
|Dock queue promotion: one cost-aware selector preserves physical waiting and shared eligible precedence|403/0,2679/2827,0|cf49676ed56e482f973fd6a6ac2a38a5|
|Dock provider observation: pure previews and live motion intent preserve original authority and first faults|725/0,1972/2139,0|1462c4a612174e65baafd3866b5f465f|

Five final exact scopes1705/0, not the entire dock family or regression suite.
New regression originally threw through actual PlanRoute/guarded CheckRoute, not
a fabricated timing expectation. Final proof covers shuffled/restored/isolated
sessions and first-fault persistence; existing physical case retains material
conservation, elapsed work/motion, real departures and replay; queue/provider
cases retain physical FIFO/occupancy/area/admission and hostile callback coverage.
Unselected cases/full provider matrices remain unverified, not silently green.

## Artifact identity and current native status

Core build2.671.0/save160/replay1; schemas/public API unchanged. Core448 C#
sorted relative-path/hash LF digest:
7f843822b8b31013edad616af246b2166cd8269716a7b3cf62c830fa5b842fb3.

|Actual file|SHA256|
|---|---|
|Core Release DLL|FAB1C7824A1DF32F37F4576F93A9EEA4A4D2F4267DDF4558F6FF0C99A250E68E|
|Tests Release DLL|DD18B6EECA2C553B233617673FEF173856A2AA526D80D36B970C2638F6CFDF80|
|ExtensionProof Release DLL|2C798CD76CCB00FD2D6398CF2AEEDA415E363672A9AACFD624C778B95ED55C5F|
|Unrebuilt consumer core2.670 DLL|CBC7243E54D692C94A0B9F7D1D4C526C61C5379071CD4B7684F5E1A8B3A57D20|
|Unchanged consumer PublicProof DLL|0CFB2663C9D641EA83AC0F1DD4EF2DD3E15A6C3494BBCC6435979C6A1AA80C61|
|Unchanged OffshoreTests.cs|27AA1A1FB3ABF51D52CA43285A2583CA22B9BFA41452A48ECE794E139C348876|

There is NO2.671 native consumer save or native performance result. Existing
consumer binaries remain2.670 and can inspect their original compatible
artifacts/marine/e65fce222fc3408da5eec112e79a2ebf/checkpoint.json at9567,
hash31D2C58FE3414413,fleet_progress/next fleet_execution. JSON SHA256
1D3B8384D166920DB6A3A1AE3324589397F826036D0180C8E159CC0BD233875F.
That old consumer still fails its day gate; the new selector cannot be credited
with native gains from old binaries. Current2.671 framework must NEVER load
that image through a header rewrite, compatibility path or stale copied DLL.

Next stage5, NOT RUN: from actual consumer SimEngine root, build only
`dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v quiet`.
Verify copied core matches2.671, preserve the old images historical, then invoke
the existing exact Offshore literal with --case-input prepare and original30000ms
bound. Continue only from its actually published current checkpoint using the
original independently bounded stages. A fresh current preparation is justified
by the real production change, never just by a failed test. No unbounded/full
workflow, shortened durations, raised deadlines or test/gameplay in the harness.

## Review, operational/assurance coverage and remaining requirements

Self-review: same comparator, strict kind groups, retained path unchanged,
eligibility reads stay original-ID and original observation owners, all needed
fallbacks still route. Refusal rank is explicit despite reordered evaluation.
No external I/O, authorization, config, event handler/catch, package, channel,
secret, application UI or simulation clock added. Existing World.Fault and
EntityObservation own callback failures; no swallowed errors or false success.
Reuse the [P9 operational/assurance matrices](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
Wider structured diagnostic/audit/notification durability and dependency/security
scans remain tracked gaps, not implementations proven by these checks.

Poison-route tests prove omitted irrelevant calls, NOT a native speedup. Extra
transient ranking lists may cost allocations on small cases; actual CPU/GC,
full native fleet/marine, arbitrary mods, durability/stress and all original
compound gates remain open. Original221/209 recorded checked/12 open, no closure.
Goal ACTIVE; no publication before the full goal AND plan are genuinely complete.

Final hygiene2026-10-19T11:25:37+03:00: eight affected Markdown documents,
3500 local links/0 broken; original221 requirements each matched exactly once,
209 recorded checked/12 open/0 unmatched. This inventory is not global runtime
revalidation. Both original master HEADs unchanged, staged0; framework3007 and
consumer101 dirty entries preserved. git diff --check exit0 in both roots with
35/8 existing LF-to-CRLF warnings. No selected worker/parent remains live.
No executable, protocol or test/assertion edits after the final five exact checks.
