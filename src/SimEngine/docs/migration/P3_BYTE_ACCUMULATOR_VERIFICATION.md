# P3-BA1 — Canonical byte accumulation verification

> Engine/consumer now2.671: [P9-RS1 diagnosis/correction/current native evidence and next continuation](P9_RANKED_DOCK_JOURNEYS_VERIFICATION.md). Consumer copied-core identity verified and nine native stages567/0 completed. Previously failed full fleet day now PASSES112330ms/120000ms at EXACT same9567/hash31D2C58FE3414413. CURRENT consumer8be38c0b32dd4267871c50de85cfad65/checkpoint.json,fleet_progress/next fleet_execution; preserve it, no reset/header rewrite. First vessel1268/6000, full fleet/sea/general performance/original plan remain open. No live worker. Records below remain historical2.670/2.669 byte-loop evidence, not current whole-plan acceptance.

## Current follow-through P3-BA2 — engine2.670.0

2026-10-19, Asia/Kuwait(+03:00), Windows/PowerShell/.NET SDK10.0.401.
Same framework/consumer master revisions as below; dirty work preserved, staged0.
This is self-review, not CI, independent review or release approval. Active
user-installed sdlc-orch now resolves shared snapshot
dcecb5fc3475d427be3ec421f3f19ef833c85d38; the earlier2.669 work began under
6dd87eab3de8425f0348b025d1340180b69f0fa1, then8d6d6b5db5f9ee446ff1d3711a857da927cf443f.
New routing/core/implementation/verification/handoff instructions were inspected;
unchanged files were byte-compared to the fully read previous snapshot. No local
framework copy, stack upgrade, platform/publishing work or extra approval gate.

### Actual2.669 continuation and failure, before this correction

All invocations selected the SAME original Offshore literal via --bounded-exact;
no full workflow/profile/family suite. Native bounds remained harbour/fleet150000ms
outer, internal120000ms, termination5000/output18000; admission outer30000ms.
Consumer raw logs are artifacts/marine-observations/<directory>/run.log.

|Input/current native time|Assertions/result,worker/outer ms,stage ms|Log directory|Published native artifact/hash|
|---|---|---|---|
|harbour:c48...,2891->4331|35/0,31542/31682,30589,exit0|ba38a90f79144883b1f2e921d2875e58|897e7cd3eb7847c5b401eefc3a815ee4/87E6004E1CF7EEA4|
|harbour:897...,4331->5771|32/0,36018/36145,35118,exit0|5684575882d7477ea61fc4afabd65244|a672f52baf8f473c90c7f775a705cf34/D702F5DA3EA5B127|
|harbour:a672...,5771->7211|26/0,30699/31043,29829,exit0|ded5bc0b076b4b63978422827243b531|f066414d26ed4c1da2a963be5ae12894/528271EC5592B92B|
|harbour:f066...,7211->8111|23/0,21483/21607,20548,exit0|3ea24b83449c43d3a54f4d9368e892b3|bc505f0532cc4569b6aafbd631588834/DF87EA156D6CE938|
|fleet-prepare:bc505...,8111->8127|133/0,15738/16213,exit0|20622b00a5f54ae18b32d8ff822149b3|7487b107834b4ad9ba4d8a4a8cdc67b3/4DCED3C9D1ED2C9D|
|fleet:7487...,8127->9267|209/1 FAILED,128142/128268,126543,exit1|f8d086e7f1714f81951a80cd73b8bb17|f467f00d0cae4a71b637ffd959512056/C564F4678BEE4F21|

The current-build harbour truly completed, no construction jobs, same exact
8111/8127 game hashes as prior2.668. Seven original full-duration vessel orders,
nine original standing routes, no free output/resources or altered deadlines.
Fleet failure retains healthy actual968/6000 head progress and physical cargo
28.666 unloading with another truck moving. It is NOT fleet acceptance. Every
original handle was awaited to actual terminal; no observation timeout restart.
All these2.669 native artifacts are now historical/incompatible with2.670.

### Bounded actual CPU sampling and tool provenance

The actual worker was inspected through Win32_Process, including exact executable,
--worker-exact, input checkpoint and parent, before each attachment. Isolated
Microsoft dotnet-trace10.0.731102 was installed from api.nuget.org under ignored
artifacts/diagnostic-tools, not as an engine/package/project dependency. Local
nuspec identifies Microsoft/MIT/dotnet/diagnostics revision
f09edf7ea9a17a86b236b91eb4c458d0469546b6. Existing target/runtime pins unchanged.
[Microsoft diagnostic-tool usage](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
was checked2026-10-19+03:00; installed --help and actual collection/report execution
verify the selected command, not every profiler/platform/dependency capability.

Commands, framework root: artifacts/diagnostic-tools/dotnet-trace.exe collect
--process-id <verified worker> --profile dotnet-sampled-thread-time
--duration 00:00:00:10 --output <artifact>/harbour.nettrace or fleet.nettrace;
report <trace> topN --number15 (fleet20) --verbose and --number12 (fleet15)
--inclusive --verbose. This is CPU diagnostic sampling, NOT a test profile run.
Collect/report exit0. Raw trace, collect.log, top-exclusive.log and top-inclusive.log:

|Worker/input|Framework artifact directory|Observed scope|
|---|---|---|
|12848,harbour:a672...|artifacts/native-cpu-sampling/2637ef6edbc74edb963d30b4a1461195|ended with worker after about2.8s, NOT a full10s sample; Bytes56.23% exclusive, TileMap.Sync71.24% inclusive|
|19596,fleet:7487...|artifacts/native-cpu-sampling/b3d01bb92e7f44a5b93b579cb7c14c66|requested10s collection completed; Bytes69.47% exclusive, TileMap.Sync88.18% inclusive, provider hashing89.48% inclusive|

These are phase-specific managed sampled-time proportions, not a complete CPU/
allocation distribution or statistical speedup. Sampling perturbs the observed
harbour/fleet timing; retain the original failed bound rather than remove it or
claim an uninstrumented before/after comparison. The data justifies targeting the
existing byte loop, not dropping original codecs or solving a convenient list allocation.

### Implementation, discarded candidate and focused verification

Same StateHasher, same length/canonical FNV bytes, same local accumulator.
Original32-byte uniform path retained; eight-byte uniform words now fold when
neighbours differ. The one mathematical array/factory holds both fixed correction
regions,1MiB total versus prior512KiB. No session/input fingerprints or second
runtime cache. Nonuniform words use explicitly little-endian decoding and the
original eight scalar rounds, unrolled. No codec, callback, clock, revision,
route/claim/fuel rule, test body/assertion or native observer/content changed.

First eight-byte-only candidate passed299537/0 but was discarded for uniform/noise
regressions. It had engine/scalar16MiB ms: zero0.957/16.942,terrain3.982/15.756,
mixed7.096/15.928,noise18.853/16.001. Raw framework log
artifacts/long-byte-hashing/5c628a697db740078c8842803e831fb4/run.log,
worker4398/outer4903ms,exit0. This passing correctness result is NOT final
performance approval. No native save was produced with that discarded candidate.

Final source2.670 selected cases, same original literals and unchanged test bodies:
--bounded-exact, --timeout-ms 30000 --termination-timeout-ms 5000, output6000
(entry8000); framework logs artifacts/long-byte-hashing/<directory>/run.log.

|Exact registry literal|Assertions,worker/outer ms,exit|Log directory|
|---|---|---|
|State hashing: exact uniform byte blocks tails mixed streams and concurrent owners|299537/0,4199/4372,0|5b601a2f15f44bc993ffbccfb1359566|
|Shared spatial observation: original codecs and revisions guard physical native and point callbacks|776/0,9021/9211,0|2cb1d86c747149898d137d1ef5cd14f6|
|Layered navigation: authored roads, depth gates and physical saved work|48/0,2593/2765,0|3270805d71354673a18fbf01b5655a98|
|Navigation entry contexts: measured native and independent guarded callback allocation|60/0,1307/1485,0|64e40d29018548229683b091f092d6d2|

Four final selected scopes300421/0,299537 in ONE byte-vector case, NOT a full suite.
Final16MiB engine/scalar ms: zero1.082/16.852,terrain1.415/16.644,
mixed8.474/15.757,noise15.843/15.811. Compared to2.669 mixed15.807ms is1.87x
faster; noise is near-flat, while zero0.667/terrain1.076 remain slower. Explicit
static memory/tradeoffs, not a universal win. Actual query allocations unchanged:
native928/3544/3928 bytes, independent328/648/1608. Query timings still vary,
no general CPU or allocation acceptance. Original native/independent callback
faults and physical saved/replay checks remain, no weakened assertions.

Targeted build commands as below: framework candidate10.07s/final11.45s, consumer
2.50s, all exit0/0 warnings/errors. No full solution/default/profile/family run.

### Current source and native handoff

Engine2.670.0/save160/replay1; component/service schemas unchanged. Core448 C#
sorted relative-path/hash LF digest:
5c40cf7cf3a92b59157da7c34ec41d803d1f511f061d655a4406e9964e46a7f2.

|Actual file/copy|SHA256|
|---|---|
|Core and consumer copied core|CBC7243E54D692C94A0B9F7D1D4C526C61C5379071CD4B7684F5E1A8B3A57D20|
|Tests DLL|961B40816B271F99118DED664F551AD7366586132F4FC68BFA58B1734C3166D0|
|ExtensionProof DLL|6E10E0A8A75817CFD22915A389DF6AB109C0164F08547D53855DF2BA8598CBBA|
|Consumer PublicProof DLL|0CFB2663C9D641EA83AC0F1DD4EF2DD3E15A6C3494BBCC6435979C6A1AA80C61|
|Consumer OilCo.Game DLL|ABCE972ACBE147300FC5F0E0E868AC0F24BA22B4DAB8025C959982F0E0F01986|
|Unchanged OffshoreTests.cs|27AA1A1FB3ABF51D52CA43285A2583CA22B9BFA41452A48ECE794E139C348876|

Real production correction justifies current native preparation; old saves/headers
are never rewritten. Same original Offshore literal --case-input prepare, original
Gulf96x96 seed1/two humans/company1/durations/quantum/resources/geometry,37/0,
worker5452/outer5591ms,exit0. Log consumer artifacts/marine-observations/
90e20880ea594c2b9452c34989ba7670/run.log. Preparation artifact
artifacts/marine/08853fd633c742fc8e73efe38d3233de/checkpoint.json at11,
hashA758028A5E3E7B7D again matches prior actual game state; checkpoint SHA256
A21434CA7148A807F1C796F7CFF4F52E2773B87E442F5D6C4CF950048C43B620.
First current bounded day11->1451 passed35/0,worker55240/outer55371ms,
stage54305ms,exit0. Raw consumer log artifacts/marine-observations/
e5740e7a921941e6aae28876e1d35c7e/run.log. Native
artifacts/marine/d1f28bb6b5db4793b7d0f36da40514d7/checkpoint.json at1451,
hash2EC4336CCADCC849 matches both prior builds at the same boundary. Previous
uninstrumented2.669 day72322ms; this single observation is24.9% lower, not a
statistical/global performance result. Second busy day1451->2891 also passed35/0,
worker82810/outer82926ms,stage81960ms,exit0. Consumer log
artifacts/marine-observations/68e7b26a4e7c4f70b44c20e7004cc56d/run.log;
native artifacts/marine/56ab013217854c34a549f0056d26c6e8/checkpoint.json,
hash7BB9F98358A81ED2 again matches actual prior same-boundary hashes. Its JSON
SHA25633B8F79D9449B0E392DC8C06D1CF31A23A861B6B1370365B7F9746A5D045FE8E
also matches, while the native image has the new strict engine build. Previous
uninstrumented2.669 busy day113401ms; this observation is27.7% lower. Different
warm-up/system conditions still limit the comparison; no universal speedup or
allocation claim. Final native continuation and its remaining failed bound are
recorded below; no live worker remains.

Subsequent current native stages, same literal/bounds and NO further executable
edits (consumer logs artifacts/marine-observations/<directory>/run.log):

|Game time|Assertions,worker/outer ms,stage ms,exit|Log directory|Native artifact/hash|
|---|---|---|---|
|2891->4331|35/0,27905/28061,27025,0|583fbdadd8df430097bdfe476f1ed7a6|d265048f9a5b47bbadd8fe0f8590ebca/87E6004E1CF7EEA4|
|4331->5771|32/0,28080/28208,27252,0|fdd7220a659f4946831bce1ad0b7388a|77966c3931514b07bc1b4a2e9f7559d8/D702F5DA3EA5B127|
|5771->7211|26/0,23806/23930,23061,0|7769e84471c94c978e32fecabea57ff1|4f6d83e26c744f80b13f12f8ad3f7b27/528271EC5592B92B|
|7211->8111 harbour COMPLETE|23/0,16228/16357,15539,0|2255e920d3e448b88a36ba91606e3b94|d32c178ce33d4fe4a8e01043a9554e41/DF87EA156D6CE938|
|8111->8127 original fleet admitted once|133/0,10787/10908,0|bb1f76740fe343bea9e8c2d4f312ce8d|9cab09b4f8994365867d31b9067d920a/4DCED3C9D1ED2C9D|
|8127->9567 full game-day fleet observation FAILED wall bound|209/1,122143/122265,120532,1|ad517928f78b4fcd9b0f4604f4e3d34b|e65fce222fc3408da5eec112e79a2ebf/31D2C58FE3414413|

Every listed game-state hash also matches the real2.669 same-time continuation,
not a fabricated expected value or rewritten native image. The current harbour
actually completed at8111 with all three structures and no construction orders or
jobs. Fleet admission/production is not inferred from completed construction.

The final fleet run advances the entire1440 game minutes, but120532ms exceeds
the unchanged120000ms bound. It is explicitly FAILED209/1, not rounded down,
retried or converted into a pass because its saved world is healthy. All native
attempts on2.670 total565 assertions passed/1 failed across nine explicit stages
of ONE literal; this is not full marine or a broad suite. Framework's four final
exact cases remain independently green; do not hide the native failure behind
their299537 vector assertions.

CURRENT compatible native artifact, verified after actual terminal exit1:
consumer artifacts/marine/e65fce222fc3408da5eec112e79a2ebf/checkpoint.json,
minute9567/hash31D2C58FE3414413,verified_stage=fleet_progress,
next_stage=fleet_execution. First vessel head1268/6000, all original seven
orders remain, no vessel manufactured yet. Truck5 unloads21.333 and truck6
physically moves with28 cargo; no fault/no disappeared construction projects.
JSON SHA2561D3B8384D166920DB6A3A1AE3324589397F826036D0180C8E159CC0BD233875F;
native game.sav SHA256CA7E8E44FE9181F0AC95B3D1F944431BDCF4228927217398990B14DC12BC4683.
The original live handle38818 was awaited to terminal; final Win32_Process check
found no selected worker/parent. Never reset preparation, readmit the fleet,
rewrite headers or rerun a passed stage automatically. Older2.669/2.668 images
remain preserved historical/incompatible. Current full fleet/performance is OPEN.

Next ENGINE action: diagnose the still-expensive actual route/observation path
on this retained current state, distinguishing uniform/mixed byte input and nested
codec work. PathSearch already owns query-local EntryCosts/nested scratch, and
all original provider/map/revision/clock/joint route/fuel/claim checks must remain.
Do not add a duplicate cache, raise the bound, skip a callback or make an easy
allocation change without establishing its actual share. Existing bounded clone
diagnostic (NOT RUN on this current artifact), from consumer SimEngine root:

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 18000 --case-input "traffic-trace:C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/e65fce222fc3408da5eec112e79a2ebf/checkpoint.json"
```

After diagnosis, ordinary production continuation is the same command with
`--timeout-ms 150000` and case-input `fleet:<same checkpoint>`;
that next day is NOT RUN. No new build or consumer reset is needed for observation
on the current unchanged binaries. Full fleet/sea/general performance/P5/P9 and
the original221/209 recorded checked/12 open remain OPEN, no requirement closure.

Self-review: the correction-array regions are disjoint fixed mathematical
constants; original32-byte values still execute exactly32 FNV rounds. Uniform
words are endian-insensitive; nonuniform words explicitly decode little-endian.
Every block/tail/index is bounded by actual array length, including short/empty
arrays; length prefix and subsequent scalars remain unchanged. No callbacks run
inside the local byte arithmetic, no input is modified, no new error catch or
fallback hides a failure. Real big-endian hardware was not available; portability
is supported by explicit decoding/source review, not a hardware execution claim.

Operational/assurance matrices linked below still apply. This correction adds no
event handler/channel, hosted service, authorization, UI, secret or dependency.
Diagnostic tool provenance/execution is scoped above; broader vulnerability/secret
scans, protected audit/notification durability, arbitrary providers, allocation/
soak/full fleet and all original221/209 checked/12 open compound gates stay OPEN.
No staging/commit/push until the full goal AND plan are genuinely complete.

Final hygiene observed2026-10-19T11:09:48+03:00: both original master HEADs
unchanged; source digest/copy identities above unchanged since final compilation;
framework3005/consumer101 dirty entries preserved, both staged0. git diff --check
exit0 in each root,35/8 existing LF-to-CRLF warnings. Nine affected Markdown files
3505 local links/0 broken before this final report expansion; final link/inventory
check is recorded by the turn output. Original requirement mapping221 unique,
209 recorded checked/12 open/0 unmatched; historical checks are not globally
revalidated. No executable/test/assertion/native observer edits after final checks.

## Historical2.669 P3-BA1 record

## Revision and scope

2026-10-19, Asia/Kuwait(+03:00), Windows/PowerShell/.NET SDK10.0.401.
Framework master cb61483ab81404c11944931aedb4703ba694edb5; consumer master
8be1eed8b54649ac35cb1a2fe01051dd59d1982e; heavily dirty work preserved, staged0.
This is SELF-REVIEW, not independent/CI/release approval. Whole goal remains ACTIVE.
User-installed sdlc-orch uses shared6dd87eab3de8425f0348b025d1340180b69f0fa1;
existing tracker/contracts and Git workflow retained, no framework copy/new branch.

Only StateSync.cs Bytes accumulator, engine build and protocol fixture changed.
No test assertions, world/content, provider guards, route admissions or client
code changed. Old native2.668 images remain preserved historical/incompatible;
the strict native loader is not weakened and no header conversion is introduced.

## Measured problem and choice

[Current2.668 fleet evidence](P5_MARINE_FLEET_CONTINUATION_VERIFICATION.md) retains
two failed day observations, actual cargo/production progress,48-update timing
and allocation samples, and actual worker18064 stack through StateHasher.Bytes/
TileMap.Sync/SpatialScope/WorkBoard.ReplanDockBindings. It is one sampled stack,
not proof of a complete CPU or allocation distribution.

Baseline vector benchmark showed already-fast uniform buffers but much slower
mixed/scalar fallback than its independent oracle. Chosen correction is a local
accumulator in the SAME method, not bigger uniform tables or a different hasher.
Exploratory arithmetic also disproved a proposed64-byte low-state identity before
any production implementation; no such shortcut/table/branch was added.

Original length encoding and every32-byte fold/scalar/tail iteration remain.
No callback occurs inside Bytes, so its local arithmetic is committed once on
return. Observation/private codec verification, revisions, entity/clock facts,
errors and final publication rules remain in their original owners.

## Commands and all focused results

From framework root, each original exact literal below used
`--bounded-exact`, `--timeout-ms 30000 --termination-timeout-ms 5000`; output6000
for vectors/guards/physical,8000 for guarded query measurement. Raw logs:
artifacts/long-byte-hashing/<directory>/run.log.

|Exact registry literal|Build/result,worker/outer milliseconds|Log directory|
|---|---|---|
|State hashing: exact uniform byte blocks tails mixed streams and concurrent owners|baseline2.668,299537/0,4320/4543,exit0|3d7c55ce165b4f2b95d3cefde5c933d3|
|Navigation entry contexts: measured native and independent guarded callback allocation|baseline2.668,60/0,1367/1540,exit0|9a5b695fcf4c414486f8486946fa6c45|
|State hashing: exact uniform byte blocks tails mixed streams and concurrent owners|final2.669,299537/0,4207/4378,exit0|63cdd7a93eba42c889ccf403589565ae|
|Navigation entry contexts: measured native and independent guarded callback allocation|final2.669,60/0,1184/1336,exit0|f8540c3ec48f4fbeb9eac5a0a10bbce0|
|Shared spatial observation: original codecs and revisions guard physical native and point callbacks|final2.669,776/0,8261/8425,exit0|fb6035c192d44c768864674b4f9004ea|
|Layered navigation: authored roads, depth gates and physical saved work|final2.669,48/0,2443/2601,exit0|b2b16c815aa9411ab1713cf786c55151|

Four final selected scopes total300421 assertions/0 failures.299537 belongs to
ONE exhaustive byte-vector case, not a broad test run. Baseline/final repeats
are the explicit same-workload comparison; no automatic other passing reruns.

The oracle covers every byte value and accumulated low-state/prefix, short tails,
single-bit disruptions, mixed streams, subsequent scalar state and concurrent
independent owners. Shared spatial proof retains all776 existing assertions:
native/independent point geometry, normal/half-minute quanta, physical construction,
hauling/research, saved twins/replay and mutation/revision/throw terminal refusals.
Layered48 independently retains real roads/depth/reroute/work/save semantics.

Targeted builds only:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`
9.33s,exit0/0 warnings/errors; consumer
`dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v quiet`
1.96s,exit0/0 warnings/errors. No full solution/profile/family/default test run.
Build stdout was observed in terminal, not separately archived/machine-validated.

## Before/after measurements and limits

Same existing16MiB benchmark,4096 iterations of4096-byte arrays, one-second
matched warm-up per pattern. Values below are observed samples, not statistical
proof of universal throughput or the whole game's latency.

|Pattern|2.668 engine/scalar ms|2.669 engine/scalar ms|
|---|---|---|
|zero|1.268/16.779|0.667/17.328|
|terrain uniform|1.859/15.890|1.076/16.016|
|mixed short runs|35.617/15.823|15.807/15.778|
|noise|42.755/67.016|15.807/15.784|

The mixed kernel sample is2.25x faster. Noise scalar baseline varies widely;
do not treat its change as an engine benefit. There is no added allocation/cache.
Same guarded query measurements preserve all exact per-call allocations:
native condition928B,structural3544B,live3928B; independent328/648/1608B.
Most query timings are flat/mixed; native structural rounds have strong warm-up
variation (baseline242.199/216.883/118.092 versus193.850/203.760/40.475ms).
This does NOT prove a general guarded-query speedup or solve the allocation gap.

## Current identities

Engine2.669.0/save160/replay1; component/service schemas unchanged.
Core448 C# sorted relative-path/hash LF digest:
4fbd47528ab15538fe313bcc74437a4de505c47d0359f4a3b3e4a305f7728cce.

|Actual file/copy|SHA256|
|---|---|
|Core and consumer copied core|6CEC993EC5FD9054AAA2E1FF3921A8D8F93F599CF3C0B8B6194FDEE8B3BBB072|
|Tests DLL|A7A3EF2327AEEF32AC5DEF7534EEC3BCE725DB6431C1350145538EEC01C922A8|
|ExtensionProof DLL|1D3E5686B764CC32CC02FA84BCB04CB8DC453E2E11283255350667E95491B2C9|
|Consumer PublicProof DLL|B8BE026C0A2F64A439DA3B49BF9AC632F2A8876BE5ACDFFDF51A876FD653A0D3|
|Consumer OilCo.Game DLL|582B187B94BDCDEFA743F3414EAA23E97AE6F6A39DEDC7F240A39440CA788174|
|Unchanged observer OffshoreTests.cs|27AA1A1FB3ABF51D52CA43285A2583CA22B9BFA41452A48ECE794E139C348876|

## Native observation and continuation

Fresh preparation is justified only by the real engine build change. Original
Gulf96x96 seed1/company1, geometry/materials/work/quantum/settings untouched.
Same original literal selected with explicit prepare, not the default full case.
37/0,worker6488/outer6610ms,exit0. Consumer log
artifacts/marine-observations/77c82fd9f65c4f16a045e0e4135967a8/run.log.
Native current checkpoint c64fa1faf0dd4d488b3c970e23ced039/checkpoint.json,
minute11/hashA758028A5E3E7B7D,harbour_projects_admitted/next harbour_execution.
This matches the prior2.668 game-state hash/positions at the same boundary;
native header build identity is intentionally different.

First bounded current day11->1451 passes35/0,worker73320/outer73465ms,stage72322ms,
exit0. Consumer log artifacts/marine-observations/5a6fcfdf15ae4a109b0df8c14e70a02d/run.log.
Native e5fe2c010bd9412caa5643b3ebaa8803/checkpoint.json/hash2EC4336CCADCC849
matches the actual prior2.668 same-boundary state hash. Its recorded stage time
was78438ms; this one observed sample is7.8% lower, not a statistical/global result.
Second current day1451->2891 passes35/0,worker114548/outer114695ms,stage113401ms,
exit0. Consumer log artifacts/marine-observations/04f4f188464e473ab29939944da63c42/run.log.
CURRENT native artifacts/marine/c48a1f08e00e4e1c8865852be317ab3a/checkpoint.json,
minute2891/hash7BB9F98358A81ED2,harbour_progress/next harbour_execution. This
also exactly matches the actual prior2.668 same-boundary game hash; its previous
stage114952ms is nearly unchanged. Both trucks park, grid awaits crane rather
than stock, shipyard erection1345/3600. No healthy process was silently replaced;
both original live handles were awaited to terminal. No native worker remains.

Preparation/two explicit days total107/0, not full harbour/fleet or performance
acceptance. The kernel improvement is real in its measured scope, but nearly flat
busy-day timing shows it does NOT solve the broader guarded-workload cost. Keep
P3-BA1 step4/P3-S28 and all full native fleet/plan gates OPEN; do not treat this
narrower correction as completion of the original performance objective.

Current next continuation, from actual consumer SimEngine root, NOT RUN yet:

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 18000 --case-input "harbour:C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/c48a1f08e00e4e1c8865852be317ab3a/checkpoint.json"
```

Native prepare/day invocations used that same original literal: prepare outer
30000ms and days150000ms, termination5000/output18000, unchanged internal120000ms.
No unselected/default full workflow or altered game-time work. Preserve this
current state; old2.668 fleet10227 remains historical/incompatible, never rewritten.
Further engine cost analysis must retain actual scope validation and joint route
admission. Source review confirms PathSearch ALREADY has query-local EntryCosts
and nested scratch frames; do not add another cost cache or claim a new one is
needed. No additional optimization is implemented or proven by that review.

## Operational services, assurance and outstanding gates

Reuse the existing [P9-HI1 capability/event and assurance matrices](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage):
World.Fault is the first-failure owner, views/results/replay stay authoritative,
and the transient notification ring is not a protected audit log. This arithmetic
method adds no event, exception handler, UI, external channel, package, dependency,
clock or service. The original throwing/mutating callbacks still fail explicitly
and cannot resume a healthy session/save in the selected776 proof.

There is no skipped guard/test, changed authored configuration, lowered work,
raised timeout, new authorization boundary, secret or host gameplay. UI/agent
evaluations are N/A (no UI or skill/tool/model implementation change); broader
structured log/audit/channel durability, provider/authorization matrices and
vulnerability/secret scans remain unverified foundations, not silently complete.
No claim of CI, independent review, universal CPU/allocations, full native fleet/
marine or original P5/P6/P9 durability/regression acceptance. Whole goal remains
ACTIVE; original221/209 checked/12 open remain the unamended tracker scope.
No staging/commit/push before full goal AND plan completion.
