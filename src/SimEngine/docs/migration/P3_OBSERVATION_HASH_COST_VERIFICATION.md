# P3-S28 observation and routing cost verification

## Current2.682 visibility facts — 2026-10-21

SELF-REVIEW. User-installed sdlc-orch a80d41ad0affd81b549f5f063787e186d162e37f,
handoff/verification then implementation procedures. Windows/PowerShell/.NET
SDK10.0.401/+03:00. Framework master cb61483ab81404c11944931aedb4703ba694edb5;
consumer master8be1eed8b54649ac35cb1a2fe01051dd59d1982e. Dirty trees preserved,
framework3014 status entries/zero staged. Previous goal turn was PROGRESS: original
gate publisher correction, four focused scopes, full actual harbour and once-only
fleet admission. Full goal remains ACTIVE; no new blocker or original gate closure.

### Baseline2.681 fleet pressure

Before production edits, the current2.681 binaries AND exact8127 save hashes were
revalidated against the previous handoff. No source change/build/re-admission/reset.
One ORIGINAL exact native fleet day advanced8127->9567 and saved
consumer artifacts/marine/4588f1bea867472996647a90f0e5d783/checkpoint.json,
hashF8B9F92F184C6E0E,fleet_progress/next fleet_execution.
Actual head build_survey_vessel1268/6000 minutes, all7 sequential orders retained;
trucks5/6 are physically unloading jobs32/33 with2/20.666 cargo. No vessel yet.

Exact native literal Offshore: vessels survey, drill and ship from the sea,
--bounded-exact --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000
--case-input "fleet:artifacts/marine/00ee1c22766c48ac95c3aec5e9f38c53/checkpoint.json".
211/0, worker124385ms/outer124904ms, actual stage119945ms under unchanged120000ms:
ONLY55ms headroom. Raw consumer artifacts/marine-fleet2681-observations/
84d10efd21054cd680e75bcb68f21688/run.log; actual worker32960, terminal exit0.

Instead of another blind long day, existing traffic-trace:<4588.../checkpoint.json>
advanced48 ordinary Updates9567->9615,297/0, worker19524ms/outer19907ms, actual
worker32404, terminal exit0. Head earns another48 minutes and trucks complete
unloads/start jobs34/35; no stuck traffic cycle. Diagnostic9615 has NO save.
Raw consumer artifacts/marine-fleet2681-observations/
e2ab163520a942caa09a5bafebf3f97a/run.log. Update min/mean/max
63.462/260.933917/4226.877ms; mean13997687.5 allocated bytes per Update.
Cold load/JIT and profiling effects are included; not a controlled long-day estimate.

Existing installed dotnet-trace attached to confirmed --worker-exact PID32404 with
that exact input, not the outer observer. Requested3s sampled-thread-time/Speedscope,
collector exit0. Framework artifacts/route-cost2681-fleet-profiles/
81e69d9260fc451cae274cc39bf48cd4/{collect.log,fleet.nettrace,fleet.speedscope.json}.
Actual span3212.883ms,15948 events, balanced stack;1295.443ms within World.Step.
Nearest-real EXCLUSIVE shares: Bytes10.826%, Payload10.022%, reference-list capacity
5.353%, RuleFacts-list capacity3.977%, UpdateWork3.152%, BuildQueue2.990%,
IsEnumDefined2.829%, Round2.373%, byte ValidateValues1.929%, Array.Resize1.819%.
Inclusive provider observation68.073% and TileMap.Sync37.696% OVERLAP and are not
additive. This is a short phase sample, not all CPU/allocation causes or all sizes.

### Existing owner change and preserved contracts

The sole KnowledgeVisibility owner freshly constructs private RuleFacts/PolicyFacts
for every query. Previously each singleton target rule allocated a List, backing
array, readonly wrapper, HashSet and its backing storage; the policy facts list
also regrew while its final engine-owned rule count was already known.

ReadRule now keeps its first validated ACTUAL frozen target reference directly;
it creates remaining-target storage and the SAME reference-identity HashSet only
when a second target is actually enumerated. Every target is still enumerated,
frozen/ownership-checked and duplicate-checked, including repeats of the first or
a later target. It does NOT preallocate from an untrusted provider's Count, infer
another target, skip a callback or retain a previous query's facts. Matches tests
the first reference then the remaining detached references in original order.
All target fields remain private to the ORIGINAL RuleFacts; no new public model.

ReadPolicy now fills one exactly sized RuleFacts array using its already validated
LOCAL actual.Count, then exposes the same read-only interface. Native/independent
policy selection, bindings, alias validation, sorting, dynamic Allows/AllowsEntity/
sample/key callbacks, observation intervals, first faults and privacy remain.
No cache, pool, new service/catalog/knowledge ledger, gameplay default/limit,
native-only special case, compatibility facade or alternate metadata authority.

Strict build2.682.0/save160/replay1; same state layouts, original codecs,
StateHasher and GameClock. Only production KnowledgeVisibility.cs and EngineInfo
identity change, plus the current protocol expectation. Prior2.6819567 remains
preserved but STRICT-INCOMPATIBLE. No header/body/checksum migration or rewrite.
All gameplay, knowledge, physical work and decision rules remain inside SimEngine.

### Focused baseline and final evidence

New VisibilityContentContractTests.Allocation.cs is a partial of the existing
VisibilityContentContractTests owner/registry, using its existing master/JSON
authoring and independent DeclaredVisibilityTargetsDef. Six native/independent
policy x1/2/3-target configurations prove exact matching, hidden/unlisted/observer
behavior, fresh Targets callback on EVERY query, state purity, saved twins,
later original callback failure and healthy interleaved saved session. Late
duplicate-first AND duplicate-remaining targets refuse before play. The probe
counter is diagnostics, never gameplay/visibility state. No new engine content
default, custom scheduler, saved ledger or player simulator.

Exact framework runner:
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll
--bounded-exact "<literal>" --timeout-ms 60000 --termination-timeout-ms 5000
--output-chars 20000. One literal per invocation, never a family/profile/default.
Raw framework artifacts/visibility-facts-observations/<id>/run.log:

|Build/exact case|Passed/failed|Worker/outer ms|Log id|
|---|---|---|---|
|2.681: Visibility facts: single and multiple targets retain live guarded observations BASELINE|80/0|1644/1950|8f402c38fbfa42eb8a55023f385013ed|
|2.682: SAME new exact case FINAL|80/0|1537/1792|9ee3d986e3b84828a4046100b0cc9113|
|2.682: Visibility target metadata: typed actual references avoid runtime match callbacks|26/0|506/769|3da2751cff0d482292e93be7fe5dee55|
|2.682: Visibility content contracts: earned private discovery and guarded rules|168/0|1514/1733|7c154c35c948436693e42fbf31153acb|

Three final focused cases274/0; all attempts terminal exit0, no aborted suite.
Existing physical case verifies original material-consuming eight-minute discovery,
private knowledge, saved partial continuation, replay, native/independent rules,
invalid metadata and hostile callbacks. This is NOT arbitrary-provider/global
privacy, all callers or whole-engine regression. No passing cases rerun after docs.

Targeted test-project builds, both exit0/0 warnings/errors:
artifacts/visibility-facts-observations/ee810e81c8a44b138fbacd5e8e14afea/build.log
21.10s BEFORE production edit; bf41f79dbb464d038edde28abbd4d8bf/build.log34.08s AFTER.
Command: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj
-c Release --no-restore -v quiet.
Targeted consumer build exit0/0 warnings/errors4.67s, command
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v quiet;
consumer artifacts/visibility2682-observations/33d78914e92640b1bdb96eeca3ad6841/build.log.

### Allocation gains are scoped; timings MIXED

Same warmed256 actual queries per configuration; EVERY query still calls Targets.
All state hashes824CA5C094E243A1; differing static authoring is independently
checked by exact matching/refusal assertions and the native content-aware save
loader, not inferred equal merely from that state hash.

|Policy/targets|Before bytes -> after bytes|Before ms -> after ms|
|---|---|---|
|native/1|1067008 ->759808|5.812 ->5.029|
|native/2|1067008 ->833536|7.772 ->8.401|
|native/3|1067008 ->833536|9.535 ->9.209|
|independent/1|1067008 ->759808|7.676 ->9.026|
|independent/2|1067008 ->833536|6.128 ->4.246|
|independent/3|1067008 ->833536|3.091 ->3.146|

Single-target configuration saves307200 total bytes/1200 per query (28.8%);
multi-target saves233472/912 per query (21.9%). These include all selected policy
rules, not a claim of1200 bytes saved per individual target. Wall timings are
mixed; native2, independent1 and independent3 are SLOWER. No universal speedup,
zero-allocation, statistical/hardware/scale or full native budget claim.

### Current native first-day check

Unchanged original native literal/150000ms outer/120000ms per-day and full authored
machine/material/work durations. Because strict build changed, normal preparation
uses2.682; NO old native image is relabeled/loaded as compatible.
Raw consumer artifacts/visibility2682-observations/<id>/run.log:

|Input|Passed/failed|Worker/outer ms; stage ms|Log id|Output/time/hash|
|---|---|---|---|---|
|prepare|37/0|6497/6825;n/a|f023c1b3732e48b486dde3ea93d6b693|0e1aaed6bc6a422f972979496ddafb89/11/2BB2DFA0A8F81502|
|harbour:0e1...|35/0|61143/61341;60211|51638be733704c86a986afd4618005a1|540d6e57d9d6481f84035fa1148ea5d1/1451/C8B349CDD5FF5B06|

Both exit0,72/0 selected checks. Current first-day60211ms is SLOWER than the prior
2.68127906ms observation. Query allocation gains do not override this negative
wall observation; machine load/tier/phase are not controlled and no cause is claimed.
At that source-verification checkpoint, no other long2.682 day or full fleet/sea/
PAD/profile/suite had run. Later physical continuation is recorded in P5 below.

Read-only1451 save comparison against2.681: both134223 bytes; differences ONLY at
build character35 and final8 ORIGINAL file-checksum bytes134215–134222. All other
bytes including gameplay payload match. No file was edited. This is correspondence
at1451, not cross-build compatibility or qualification of current full harbour.

### Current physical continuation is owned by P5

The1451 source-verification image was subsequently continued WITHOUT a rebuild,
source edit or reset. Current harbour completes8111; all seven original vessels
are manufactured, rolled out and publicly inspected AtHome by55407. The original
optional sea survey completes59188, physical24h pad completes61229, and original
180h drilling DAG produces a working well by72810. The pad now passes its original
wall-time bounds; older failed observations remain failures. Current terminal
construction77731, physical crude delivery77852 and all six subsea segments79833
now complete the original staged marine functional workflow on the SAME binary.
The [P5 continuation record](P5_MARINE_FLEET_CONTINUATION_VERIFICATION.md#current2682-physical-continuation--2026-10-21)
owns every new observation, the SOLE current native path/hash and exact next
command. This report owns the engine change, not a second resume pointer.
The obsolete1451 resume command is removed. No old-build header is rewritten.
Current staged marine continuation is verified; monolithic Play wall-time,
P9.051/general performance/broader acceptance remain OPEN.

### Identity, assurance and documentation maintenance

SHA256:

- production/test/consumer core92EA7D61B55C5037C7960CB928DB6E211912736CFB98835D54488A700FCD0E8F
- KnowledgeVisibility.cs50A2BA1A4D929F63FE01094539C41C69FE0DEE8871D5BF537CEF88180E0213FA
- SaveGame.csDED186937D12D7B89F84FC8A1D644D5746A4C78E0480645D4C4FBC4D89E8BFC0
- protocol JSONF1522B8033E2497A3AC28ABEC8435630D386989270CBC4275E865D692D3F3544
- existing test owner9DE7779189696DE7017427ADBFDCF2FE70A376968C5C6AAC9926BB5F13D04DC9
- new partial proof4BEB82120D873226FBAFC133F57BF0AA8D4D3185A2D3F42878F6EEE6E0406F2B
- final framework test DLLA1BC92CE68814CC3D37853FFF1B9CF820D7947E97D332DCFCF1FE05B13769A5A
- consumer proof DLL34316B84392903FAE03A1AA56DF7665982B78CFDE3FBE453777C365B0AD2FAA5
- current checkpoint JSON21AD6C7766C36084AA810ACC6FAFCC8FC49A71A04732D5042DA232A5E1B04570
- current saveA347CC530CECD39F01ABDC2D849C99DE30854CA791918980D9B02D28AF56D83C
- preserved2.6819567 checkpointC48A598684B8249BA4001AC6A713A8D5D8A7C6F05C530491FD7DD3A2F268E320
- preserved2.6819567 save3DBC3AD8F6E25FB27F57BF62A28462A8D507ABB1C73585F8D0DB777001CDEF8F

Operational/assurance mapping remains the
[existing affected-services record](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
Original callback fault cause, refusal, saved state and notification owners retained;
no second logger/audit/outbox. Focused checks do not prove protected audit/channel
outages, arbitrary CLR-provider purity, global security/fault fuzzing or allocation/
soak budgets. No new dependency, host/domain gameplay, Tycoon/business/platform/
Draw change or Git staging/commit/push. No full/default/profile/family runtime.

The SAME P3 plan and report own this changed allocation concern; current navigation
and master are updated in place. The old2.681 operational resume instruction is
removed, while its actual immutable gate/harbour/fleet evidence remains. The P5
fleet record retains the newly observed2.681 first-day/trace facts as history,
not a competing current state. No new fix document, archive copy or requirements
waiver. Original221/209 recorded checked/12 open unchanged; full goal ACTIVE.

Final scoped hygiene08:37:57–08:38:01+03:00:13 current documents/4061 local file
links,0 missing targets (existence only, not comprehensive anchor semantics);
both repositories git diff --check exit0/staged0. Current core/test hashes match
the tested92EA7D... binary; no matching native/framework worker remains. The new
current section/sole resume heading and edited plan/navigation status were
inspected; no executable change or passing runtime rerun after docs maintenance.

<a id="current2681-owned-gate-facts--2026-10-21"></a>

## Historical2.681 owned gate facts — 2026-10-21

SELF-REVIEW. Windows/PowerShell/.NET SDK10.0.401, +03:00. Framework master
cb61483ab81404c11944931aedb4703ba694edb5; consumer master
8be1eed8b54649ac35cb1a2fe01051dd59d1982e; dirty trees preserved (framework3013
status entries; zero staged). Previous goal turn was PROGRESS: measured byte-layer
change, focused validation, failed native bound and an actual post-failure sample.
No completion or blocking condition is inferred from that failure.

Workflow started under user-installed fdc9dceb0bae90a05c1575cb033849daee0e6744.
The user replaced those instructions during this turn: CURRENT user-installed
sdlc-orch snapshot is a80d41ad0affd81b549f5f063787e186d162e37f; entry/orchestrator,
constitution/evidence/delivery/engineering standards and implementation/verification
procedures read. Log/audit and quality/reliability references match the previously
read snapshot byte-for-byte. No project-local pin, framework copy or extra tracker.

### Measured call path and correction

Re-analysis of the retained2.680 native Speedscope file (PID26628,604.486ms actual
World.Step time, balanced stacks) resolves the nearest-real reference-list CopyTo
call stacks. Top paths include physical unloading, destination/origin dock routes,
idle workers and assignment checks; each passes through ExecutionGateContracts.Read
then List<reference>..ctor(IEnumerable)/CopyTo. The largest six observed exclusive
slices are6.830/5.539/4.599/4.313/4.103/3.901ms. These are call-path observations,
not an allocation census or a claim that all CopyTo time has one cause.
Raw profile remains artifacts/route-cost2680-harbour-profiles/
12527fa829b84d24a8d4bc019ec274e1/harbour.speedscope.json; no new profiler run.

SOURCE inspection corrects the first interpretation: Entity.Components already
exposes its fixed read-only array and does NOT copy the full component list.
ExecutionGateEvaluation's internal constructor copied the private, already complete
facts list produced by ExecutionGateContracts.Read. Public condition snapshots
separately copy caller-owned input; those defensive copies remain necessary.

The original gate owner now publishes its EXCLUSIVELY OWNED facts list through
AsReadOnly, without a second list/array copy. It preallocates that list from the
count of the already collected engine-owned gates; no new provider Count getter,
limit, content default, cache, pool, scheduler, state owner or alias shim.
All capability identity collection, canonical sorting, provider/entity/codec/
clock observation, every condition/source validation and first-blocker rules remain
unchanged. Failed callbacks cannot publish the local partial list.

The internal constructor now accepts only List<ExecutionGateSnapshot>; its only
current callers are the original facts publisher and original stateless empty
evaluation. MotionExecution reuses that existing immutable empty evaluation for
non-workers instead of constructing another equivalent empty result. No gameplay
state or session identity is retained in that empty value. Public snapshot
constructors STILL defensively copy arbitrary external inputs, sort conditions
and expose read-only collections.

Production scope: ExecutionGates.cs, the one MotionExecution.cs empty-value caller,
and strict EngineInfo.Build2.681.0. Save160/replay1 layout and all component schemas,
timing/rates/resources/business rules remain unchanged. No compatibility loader or
header rewrite. TileMap.cs/StateSync.cs remain byte-identical to2.680.

### Focused proof, failures and fixture reconciliation

New WorkerExecutionTests.Publication.cs is a partial of the existing test owner:
one exact literal Execution gates: owned immutable publication and bounded allocation costs.
It uses the same authored point master, native control and independent job gate;
tests canonical two-provider ordering, actual intact signal conditions, immutable
old/new views and empty facts, defensive copying of public constructor inputs,
ordinary control commands, current saved twins and repeated pure observations.
It implements no work/dispatch/rate/balance/game clock in a host or test.

The FIRST attempt refused before gameplay:0/1, missing authored work.staffed.
The existing WorkerExecutionTests.Factory had replaced its whole work document
with haul retry settings, losing the executor declarations. It now preserves the
existing work-retry.json declarations and selects existing haul work_retry_profiles
in that SAME master-listed document, as the motion fixture already does.
No engine fallback, invented executor, new file/module or changed authored delays.

Framework exact runner, unchanged limits:
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll
--bounded-exact "<literal>" --timeout-ms 60000 --termination-timeout-ms 5000
--output-chars 20000. Each invocation selects ONE case, not a family/profile.
Raw framework artifacts/gate-publication-observations/<id>/run.log:

|Build / exact case|Passed/failed; exit|Worker/outer ms|Log id|
|---|---|---|---|
|2.680 new publication FIRST, missing authored executor|0/1;1|463/1040|4dbc1643e9d4488eaa8fd17574dde3a1|
|2.680 new publication baseline after authored composition repair|26/0;0|1551/3008|2348d71a96104510af00ee1185200471|
|2.681 new publication FINAL|26/0;0|678/838|116107c95710403da04453d9b91459e3|
|2.681 Worker execution: authored contracts, controls and immutable admission FIRST|74/1;1|458/605|83dfb069490b40aa8ed59bfbfbfe119d|
|2.681 Worker execution: physical work, cargo waits and saved continuation FIRST|55/4;1|607/756|7876fddffba14966ac822a8a35fbfd1d|
|2.681 Motion execution: timed travel, retained cargo and saved continuation FINAL|55/0;0|936/1094|55c8c48b41c64971875a078301a46207|
|2.681 worker admission FINAL, same exact literal|77/0;0|512/689|ff88c0eeaee549efb537132c09bd1dfb|
|2.681 worker physical FINAL, same exact literal|60/0;0|615/774|3fe232a751df49cc8057c81cb83d2cd9|

FINAL selected four case outcomes218/0 on the SAME production core. Passing
publication/motion cases were NOT rerun after test-only worker-oracle repairs.
Their earlier test binary digest was not captured; the final test DLL digest below
identifies the final two worker runs, not retroactive execution of that exact DLL.
All actual attempts retained, no aborted suite or hidden failed assertion.

Worker oracle repairs are evidence-backed, not relaxed success criteria:

- An overflowing pure read throws OverflowException and latches the original
  WorldFault. A later Save correctly throws InvalidOperationException for an
  already faulted world. Assert original overflow text and SAME first-fault object.
- Zero-rate work reopens through Reopen/TryAssign and the authored retry delay.
  Re-enable cannot skip reacquisition/travel. Wait for actual productive resumption
  through original Updates, then STILL assert exact +2 work progress on the next
  Update and identical saved twins; no duration or delay was shortened.
- The idle dispatcher owns home intent and can stop an unassigned test-injected
  motion route. Use the ordinary SetHomeCommand and observe actual WorkerHomeState.AtHome,
  retaining no-motion while disabled and saved-twin identity. No direct route,
  stock/progress injection, second job owner or bypass of idle behavior.

Four targeted test-project builds, all exit0/0 warnings/errors, raw build.log
under artifacts/gate-publication-observations:
86da1fea1c90439aa58d09363bcad4a5=31.70s;
93cda356e053488cab7a98d2c77f1e1e=18.82s;
a6176ef4b049490e9d6c0683e399a8e5=9.45s (production correction);
8dcd77c46fd843b9a0583117bce64bd5=26.85s (worker test-only repair).
Command: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj
-c Release --no-restore -v quiet. No broad solution/test runtime.

### Allocation result, not universal performance

Same warmed2048 actual two-provider gate reads (including intact signal/provider
checks), SAME state hash89488C1858B9F1F8:
2.6805636096 allocated bytes/62.458ms;2.6815455872 bytes/39.371ms.
180224 fewer bytes =88 per read (about3.2% of this measured read workload).
Timing is one local warmed observation across changing machine load/date;
do not infer a universal speed ratio, no-allocation design or large-world guarantee.
Original2.680 failed120s native observation remains a failure, not erased by these
results or relabeled as an engine fault.

### Native physical continuation on current build

Targeted consumer build:
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v quiet;
exit0/0 warnings/errors/2.85s. Consumer artifacts/gate-publication2681-observations/
52fa733cfea74f57834ece9e88dce203/build.log. Production/test/consumer core SHA agrees.

Unchanged exact native literal Offshore: vessels survey, drill and ship from the sea;
--bounded-exact --timeout-ms 150000 --termination-timeout-ms 5000
--output-chars 100000 --case-input "<input>".
Original per-day120000ms and25-day harbour deadline remain unchanged.
Raw consumer artifacts/gate-publication2681-observations/<id>/run.log:

|Input/native start|Passed/failed|Worker/outer ms; stage ms|Log id|Output/time/hash|
|---|---|---|---|---|
|prepare|37/0|3873/3992;n/a|df9682e790414aecbfa80036a3fd463f|c2a23f919ca64776868c73c190362746/11/2BB2DFA0A8F81502|
|harbour:c2a...;11|35/0|28819/28941;27906|4b7b8c3ebf3a4bfea42f313e93ef09e5|8dd867a8d4c54da195ad7c96ea53e9b1/1451/C8B349CDD5FF5B06|
|harbour:8dd...;1451|35/0|40207/40435;39295|4da74c5de15a43789833d9406420c502|ff48f4b1f3e94f5a9610b1cf413b22b3/2891/339A86956FCCD479|
|harbour:ff48...;2891|35/0|16780/16965;15293|3a1f17b198024887ba6eea06574a5c09|6116111b1d6e450a972accfab5b1d970/4331/CF5C2065EAA2979F|
|harbour:611...;4331|32/0|18048/18237;16787|75b23080cb86454ca22df8ca0cff0222|6b0847cb834247f3a3b90f053288299e/5771/094A82BBCCAC7CDC|
|harbour:6b08...;5771|26/0|14852/14971;14201|fe50836025dc4b849c13777c2bb304fa|6a1905cb37b64144978c188cc724684d/7211/4C53C71C2321FA80|
|harbour:6a19...;7211|23/0|9695/9815;9031|02475aa1aac342d4bef1bf4b394526a8|752480e58d414ebcb43ba038490b46e1/8111/FA6595F4C2ECB673|
|fleet-prepare:752...;8111|133/0|6242/6381;n/a|05a752c1665d4aaba38bebccf4432f80|00ee1c22766c48ac95c3aec5e9f38c53/8127/ACACC84174F5F4C6|

Eight actual native invocations356/0, terminal exit0. This is a staged focused
workflow, NOT the full marine workflow or regression suite. Another attempted
shell launch at directory0f3f0d37010443769ac2593d1820e4e6 never ran the child:
PowerShell Utility/Out-File autoload failed before process startup, leaving no log.
The shell misleadingly returned0; that is NOT counted as a passed case.
No matching child was live and that directory was empty; explicit Import-Module
then succeeded before the real retained2891 continuation. No live worker was
restarted on a timeout or observation failure.

Actual completed harbour at8111: shipyard#13, depot#14 and grid#15; no construction
orders/jobs. Ordinary machines delivered materials and performed timed site/build
work; no instant spawn/completion/content duration or stock edits. Fleet admission
at8127 occurred ONCE via original commands. Seven sequential orders are pending;
head build_survey_vessel0/6000 minutes is waiting for real supplied materials;
trucks5/6 hold actual jobs20/21. No vessel or production progress is fabricated.
First/busy day27906/39295ms passed the original120000ms gates, not a statistical
speedup or proof that late fleet/PAD remains under budget.

Read-only native1451 save comparison with2.679: both134223 bytes; ONLY differences
at build characters34/35 and final8 original file-checksum bytes134215–134222.
All other bytes, including gameplay payload, match; no header/checksum was edited.
This proves correspondence at1451, NOT cross-build load compatibility.

### Historical2.681 state at the end of that verification

The then-current consumer artifact was artifacts/marine/
00ee1c22766c48ac95c3aec5e9f38c53/checkpoint.json and sibling game.sav,
8127/hashACACC84174F5F4C6, verified_stage=fleet_admitted/next_stage=fleet_execution.
It was subsequently continued to9567 as recorded in the2.682 baseline above.
Both saves are now preserved historical/strict-incompatible evidence. The former
8127 operational command is removed; the current2.682 instruction above is the
only active resume command. No saved header, checksum or gameplay payload changed.

### Identity, scoped document maintenance and open gates

SHA256:

- production/test/consumer core38106B4F6698205CAFC73F388F53AEB635810AD9EFE7049D0D4744A3FC501F0D
- ExecutionGates.cs63CEDC3A894140CFFE76CBEAAD03B0B1AC249E9E498688165F13DFEBB36FB18A
- MotionExecution.cs6B9D8EE2A0EEB1E3A80707265CEFB1F8E4E5AF56638B88D5521E208F5CF12B38
- SaveGame.cs198CDB8F0142610D55303FB2B030D67B9FB0C251E699E3AB90A2AF1BA2C57C2E
- current protocol JSONCF42B311B8233243AE78A45E50350C74EAB948EDCBE5865781EDE5539338870B
- WorkerExecutionTests.csB845507786EE874835834D5D9438D21D8BBD913C488F319D7D5604183FEBCB15
- new partial proofEF6B12559FFC0711A4F068BE83F2C51F56691E17FCDA4BFE3B7ACC706FE0FF58
- final framework test DLLBB0E94F5DBF2007E801F91127F8C92CB87575E2F1B19354DA0BAD9183A4AC64F
- consumer proof DLL8C7FE2B3E05209E3031203CD992F262D4A0F016CCB7C725C3CC8C092BB12A69C
- current checkpoint JSON2AE978352D308D030C05D9EF2DB3BC13660AD977546E6A1A2041FA677656D9B7
- current game.sav182D6883FEBBBF42313E891B643C8099CA1AEE249DA19876E9A6DF9929600770

MapLayer/StateHasher/observation/clock codecs unchanged in this slice.
Operational/assurance coverage remains the existing
[affected-services record](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
Shared fault/refusal/notification ownership retained; no logger/audit/outbox rewrite
or claim of protected durability, channel outage, arbitrary-provider/global
security/soak coverage. Headless UI/Draw/platform behavior is not implemented.

Documentation is updated IN PLACE: P3 design's old active cost/resume instructions
are replaced; this report retains failed/passing immutable test evidence but not
a competing current native instruction. Twelve navigation documents point here;
master tracker retains the goal/requirement count. No extra fix document or archive
copy; no unique requirement, historical test result or user dirty file is deleted.
Broader historical records below remain evidence, not current operational guides.

Original221/209 recorded checked/12 open unchanged; no original broad gate closes.
Current harbour is verified and fleet admitted, but full native manufacture/sea/
PAD120s/P9.051/performance/broader acceptance and full goal remain OPEN/ACTIVE.
No full/default/profile/family suite, Godot/Unity/Draw/adapter, business/Tycoon
or Oil gameplay source change. No Git staging/commit/push before full goal/plan.

Final scoped hygiene08:14:30–08:14:36+03:00:13 current documents/4060 local file
links checked,0 missing targets (file existence only, not full anchor semantics);
git diff --check exit0 in both repositories,0 staged. Current master/plan/report
instructions manually reconciled against actual2.681 source and8127 JSON; old
2.680/2.679 resume instructions removed while immutable run records retained.
No matching framework/native test worker remained; core/test hashes still match.
No executable edit or passing runtime rerun after documentation maintenance.

<a id="current2680-nonempty-validation--2026-10-20"></a>

## Historical2.680 nonempty validation — 2026-10-20

SELF-REVIEW; Windows/PowerShell/.NET SDK10.0.401, +03:00. User-installed
sdlc-orch snapshot fdc9dceb0bae90a05c1575cb033849daee0e6744, implementation and
verification procedures. Framework master cb61483ab81404c11944931aedb4703ba694edb5;
consumer master 8be1eed8b54649ac35cb1a2fe01051dd59d1982e. Both worktrees remain
dirty; framework3012 status entries, zero staged. Prior implementation turn made
PROGRESS (typed fingerprint and measured native work); the intervening TileMap
clarification was an answer, not an implementation or verified wait.

### Input evidence and single-owner change

Before changing production, the existing native traffic diagnostic loaded the
actual2.679/1451 image and executed48 ordinary Updates, ending1499 WITHOUT a new
save. Consumer artifacts/marine-typed2679-observations/0f6da7e7b4a14db38176578eff29da54/run.log:
115/0, worker9549ms/outer9888ms, actual worker40944, terminal exit0. Actual Update
minimum/mean/maximum35.154/92.559854/804.118ms; mean5411109.333 allocated bytes.
These are diagnostic phase observations, not a completed day or continuation image.

Existing installed dotnet-trace10.0.731102 attached to CONFIRMED --worker-exact
PID40944, not the outer observer. Requested3s dotnet-sampled-thread-time/Speedscope,
collector exit0; framework artifacts/route-cost2679-harbour-profiles/
cddbc113db5f40a781c739229a42ffd7/{collect.log,harbour.nettrace,harbour.speedscope.json}.
Recorded span3083.444ms,19798 events, balanced stack;1047.528ms within actual
Kernel.World.Step stacks. Nearest-real exclusive shares: Bytes13.712%, private
Payload9.279%, MapLayer<byte>.HasValues5.653%, IsZero5.303%, ValidateValues3.260%.
Bytes and Payload are separate exclusive frames. Inclusive owners overlap and
must NOT be added. This short phase sample justified inspecting nonempty scanning;
it does not prove all space observation is dispensable or identify a universal cost.

The existing MapLayer<T>.HasValues owner now tests current byte storage in bounded
eight-byte words and a tail; a word is zero iff all eight constituent bytes are zero.
Int/Fixed generic semantics are unchanged. IsZero has an inlining hint. This is
only the existing nonempty predicate, NOT a hash replacement, occupancy cache,
persisted count, copied ledger or new service. Native Set/Fill pruning and the
original codec remain the sole owners. ValidateValues still checks full shape,
partial-edge padding, every resource-quantity conversion, keys and sparse nonempty
invariants before publication; SyncValues still serializes/fingerprints EVERY cell.
No code/guard/provider/revision/clock/joint-route/claim/fuel/area rule was omitted.
StateSync.cs is unchanged from2.679. Strict build is2.680.0; save160/replay1 layouts
and original save-file checksum remain unchanged. No legacy loader/header rewrite.

The new SpatialLayerTests.NonemptyCodec.cs is a partial of the ORIGINAL test owner,
registered as ONE literal case. This is primitive storage/codec setup, not gameplay:
all256 occupied byte positions; every nonzero byte value at the final position;
normal/observation parity; native saved twins around word/final offsets; zero-chunk
reclamation; malformed zero chunks/padding; later material conversion overflow;
failed candidate publication preserves committed state. It shares original JSON
factory and Encode, not another map, resource model, scheduler or player driver.

### Every focused attempt and build

Exact framework runner:
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll
--bounded-exact "<literal>" --timeout-ms 60000 --termination-timeout-ms 5000
--output-chars 20000. Each invocation selected ONE exact case, never a family/profile.
Raw framework artifacts/nonempty-codec-observations/<id>/run.log:

|Build / literal|Passed/failed|Worker/outer ms|Log id|
|---|---|---|---|
|2.679: Spatial layer nonempty validation: every byte position native codecs and bounded cost (BEFORE production edit)|2066/0|1499/2031|8321790238004767bd5f8d8144ce1562|
|2.680: same new literal|2066/0|1508/2090|db441a6061fa4aea9f9b14a1bc460f40|
|2.680: Spatial layers: canonical sparse storage and generation|196/0|1588/2155|29f7cf55027940b1baba85876aec750e|
|2.680: Shared spatial observation: original codecs and revisions guard physical native and point callbacks|776/0|52441/53237|b2ddfed6bafd49f0b1e7c5c39dc56c95|
|2.680: Guarded byte layers: actual supplied hauling update costs saved twins and replay|515/0|5955/6664|95774e11c32d4620a3bde083b2e88e4f|

All terminal exit0/no aborted cases. FINAL2.680 four focused cases3553/0.
The canonical sparse case includes Byte/Int/Fixed generation, corruption refusal,
partial edges, resource/private behavior and int.MaxValue-square sparse geometry;
it does NOT prove arbitrary provider/global scale or platform rendering.

Targeted framework test build before the production edit: clean0 warnings/errors,
46.48s. The later2.680 framework build output was lost at context truncation;
do NOT fabricate its exit/time. On resume its process was absent, tested binaries
reported2.680, and matching core hashes were inspected. A subsequent retained
targeted consumer build recompiles/resolves the actual core:
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v quiet;
exit0,0 warnings/errors,18.31s; consumer artifacts/nonempty2680-observations/
35d27fce21a740a6b9739f300fdb73cd/build.log. Production/test/consumer core hashes match.
No test runtime reruns after documentation-only edits.

### Performance gate FAILED, not a speedup claim

New primitive4096-codec observations (one timing per position, noisy/JIT-sensitive):

|Occupied byte position|2.679 ms|2.680 ms|Allocated bytes each|
|---|---|---|---|
|0|6.196|7.929|262144|
|127|0.929|8.508|262144|
|255|4.770|1.147|262144|

These inconsistent short timings do NOT prove a reliable gain. Original supplied
hauling29 Updates now1478.902ms/7718280 TOTAL bytes, versus prior2.679334.514ms/
7711160 TOTAL bytes; original guard case52441ms versus10876ms. Both are WORSE
observations and remain visible. No unchanged-condition/hardware/statistical or
causal attribution is claimed; overlapping exact-case execution is not a controlled
performance comparison. Correctness passing does not certify a net optimization.

Native exact unchanged literal Offshore: vessels survey, drill and ship from the sea,
--bounded-exact --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000
--case-input "<input>". Raw consumer artifacts/nonempty2680-observations/<id>/run.log:

|Input|Passed/failed; exit|Worker/outer ms; actual stage ms|Log id|Native output|
|---|---|---|---|---|
|prepare|37/0;0|17108/17644;n/a|5fdbf76c53324b88851426a59199ad75|36202f6db6b740cfa7f2dfb47cf66a17;11;2BB2DFA0A8F81502|
|harbour:36202...|33/1;1|126127/126651;122959|584b3749534d41c5b4020af70eeddb10|7116026fa8a8401394e382fd3071d117;1391;A8C71A3F7D44C732|
|traffic-trace:711602...|115/0;0|15717/18030;48 Updates only|0aa81876ef0548efb2c868abb7868d4f|1391->1439;NO new save|

Day exceeded ORIGINAL120000ms after1380 actual game minutes, not1440; no timeout
increase, instant progress, fake completion or automatic repeated long day.
The native observer preserved a healthy real Update boundary before throwing the
original wall-time failure. All handles terminal (52796 exit1,98342/15083 exit0).
Current state1391: depot319/480; shipyard awaits physical materials; truck5/job8
carries32 and truck6/job9 carries40. This is NOT a passed day/full harbour.
Previous2.679/1451 and earlier states remain historical/strict-incompatible;
none were header-edited, erased or resumed as current.

Read-only comparison of normal prepare11 saves2.679 and2.680: both33154 bytes;
ONLY differences at build characters34/35 and original final8 checksum bytes.
All other bytes, including gameplay payload, match. This is evidence at11 ONLY,
not a claim of loading another build or matching1391 to a different-time1451 save.

### Post-failure diagnostic, current state and next action

The existing traffic branch changes no gameplay decisions. It advances exactly48
ordinary Updates with original rules and emits public facts; does not save.
Actual2.6801391->1439 Updates: min/mean/max41.000/129.217271/1352.866ms;
mean5144070.167 allocated bytes. Load/JIT/phase and profiler effects are included;
do not compare it as a controlled speed ratio to the different2.6791451 phase.
Real travel/loading is visible; this is not the old no-progress traffic cycle.

CONFIRMED actual --worker-exact PID26628 sampled, collector exit0. Requested3s;
actual recorded span1286.777ms (shorter than requested, not a full3s CPU census),
12830 events/balanced stack;604.486ms actual World.Step stacks.
Framework artifacts/route-cost2680-harbour-profiles/
12527fa829b84d24a8d4bc019ec274e1/{collect.log,harbour.nettrace,harbour.speedscope.json}.
Nearest-real exclusive: Payload15.280%, TileMap.Sync13.216%, List<reference>.CopyTo
7.870%, RuleFacts list capacity4.226%, dictionary lookup4.174%, reference list
capacity4.107%, Array.Resize4.063%, HasValues2.393%, ValidateValues0.917%.
Inlining can change attribution; do NOT infer a measured8-point saving by subtracting
different-phase profiles. Inclusive TileMap.Sync40.848% and provider observation
72.020% overlap; those percentages are NOT additive.

Historical2.680 save identity: consumer artifacts/marine/
7116026fa8a8401394e382fd3071d117/checkpoint.json and sibling game.sav:
1391/hashA8C71A3F7D44C732, harbour_progress/next harbour_execution.
Diagnostic1439 has NO save. Neither is a current resume pointer; this failed
observation is retained as historical evidence, not an operational instruction.

The subsequent allocation investigation retained full current codec and nested
observation semantics, without cached world hashes or version-only authority.
Source inspection confirms TileMap.Sync directly traverses its existing layer list;
it does NOT itself make a copied layer list. MapLayer.OrderedKeys creates canonical
key arrays. KnowledgeVisibility.ReadPolicy builds local actual/rule-facts lists,
while ReadRule builds detached targets. ProviderScope.Hash creates a fresh hasher
and invokes the actual selected codec. These are candidate allocation paths, not
a proven root cause or permission to cache mutable provider metadata. The current
2.681 section above records the later CopyTo attribution and owner correction.
These2.680 observations imply no positive performance/PAD/fleet/full-regression result.

### Identity and completion audit

SHA256:
- production/test/consumer core:67F6D01C1F415EB0A36E4DF7A4D33C66795F1EE0E2B376BB383C03AC318848F3
- TileMap.cs:0E827C9147056CC8E612B9869BD402BBEDF8019E5166AD016F8B599EDAA1B4BB
- unchanged StateSync.cs:EA271865353D933D0A268FBEE220CD1864ED6C4ED18571D3E6A505D939D6FCD3
- SaveGame.cs:2181B5FB66081BB660C0BC0796917A14DCFA7A276169CA521EAB42644552161A
- protocol JSON:8D68FFB73FF07B1A419C59FBAB08216A19BA6F6325D46476DCA0FE46D830DEBB
- NonemptyCodec proof:550F5E64966D2E322A619E7ED0EBE323B251F5D198655591372BC426AB6BBB43
- framework test DLL:E445329EFD5331E33BAD448435C42E0CD1E003A20AD7649FD327FA00D055D177
- consumer proof DLL:4332FEB816A797C246EF3C2A5429395331DB8DD65A8EB70A8D514CB79AE7F346
- current checkpoint JSON:AB3FD2C64D5A99547BBE14DDDFE0A87DA1CA748AF6A1AC3DBD0433E7E087E554
- current native save:8347D5BE1900DFD6E6D9D51215DEDB0516DED6F999A12735D28A42E2E950938A

Operational/assurance mapping remains the existing
[affected-services record](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
Original codec/refusal/first-fault owner and diagnostics retained; no second logger,
audit sink or notifications. Narrow guard checks are not protected audit durability,
channel outage, arbitrary CLR provider, security, global scale or service compliance.
SimEngine TileMap is simulation data, NOT a Godot TileMap/TileMapLayer node. No Draw,
Godot/Unity/platform, host/domain gameplay, business or Tycoon change.

Original221 requirements/209 recorded checked/12 open remain unchanged; this turn
closes NO original broad gate. P3-S28/performance, P5 native fleet/sea/PAD120s,
P9.051 and broader acceptance remain OPEN; full goal ACTIVE. No full/default/
profile/family test, package installation, staging, commit or push.

Final scoped hygiene13:52:31–13:52:57+03:00:13 current documents/4059 local file
links checked,0 missing targets (file existence, NOT exhaustive anchor/content
validation); git diff --check exit0 in both repositories,0 staged. Current and
retained historical section anchors inspected. No matching framework/native test
worker remained. No executable edit or passing runtime rerun after these docs.

<a id="current2679-typed-payload-fingerprint--2026-10-20"></a>

## Historical2.679 typed-payload fingerprint — 2026-10-20

SELF-REVIEW; Windows/PowerShell/.NET SDK10.0.401/+03:00. User-installed sdlc-orch
with implementation/verification/architecture procedures, framework snapshot
fdc9dceb0bae90a05c1575cb033849daee0e6744. Framework master
cb61483ab81404c11944931aedb4703ba694edb5; consumer master
8be1eed8b54649ac35cb1a2fe01051dd59d1982e, both dirty/uncommitted.
Previous goal turn was PROGRESS: actual first100h vessel, physical rollout/home and
busy-worker profile. This turn changes the measured engine owner, not another long
fleet replay. Full original221/209 recorded checked/12 open/goal remain ACTIVE.

### Actual input, alternatives and current definition

The SAME original MarineRun inspect-only branch gains PUBLIC layer-shape diagnostics.
It calls original TileMapView.ReadLayer, preserves refusal, aggregates authorized
samples into the engine's advertised16x16 storage shape, and checks unchanged
world hash. No commands/Update, private-state access, source/stock/path/progress
write, new save, game decision or host gameplay. Water/terrain are actual ByteLayer
contracts (WorldGen.cs93–94); no native byte-type assumption is inferred merely
from integer-valued public samples.

Actual2.676 busy13887 inspection:12/0,10466/10575ms, raw consumer
artifacts/marine-codec-shape-observations/3d705b68bfa54cd5ab336c33e268b8f1/run.log.
Targeted proof build3.07s,0 warnings/errors; core/gameplay/content untouched then.
All public layers return Success and9216 cells:

|Layer|Observed palette:value-count|Empty/nonempty blocks|Uniform/mixed0-or1/small-nonbinary/other eight-byte words|
|---|---|---|---|
|water|0:5114,2:4079,3:23|4/32|772/0/252/0|
|terrain|0:6620,1:389,2:96,3:2000,4:111|5/31|549/70/373/0|
|road|0:9216|36/0|0/0/0/0|

Thus625 small-nonbinary mixed words versus70 mixed0/1 words. These are public
input-shape facts, NOT the whole private codec or a universal workload distribution.
The prior binary-only micro fixture was insufficient for most mixed input.

Current design is documented in the EXISTING
[P3 decision](../../plans/enhancements/P3_OBSERVATION_HASH_COST_DESIGN.md#current-implementation-decision-one-typed-state-fingerprint-2679).
One StateHasher/IStateSync owner retains exact signed eight-byte FNV scalar mixing,
field order, every scalar/ID/string, all lengths and observation identity. For EACH
Bytes call it freshly calculates seed-zero XXH64 over ALL raw current payload bytes,
then folds original length and the64-bit payload digest through the scalar mixer.
This is a NEW TYPED FINGERPRINT encoding; it is NOT the old flat FNV raw-array stream,
nor a claim of whole-stream XXH64. No bytes/providers/revisions/clock/route/fuel/
area/claim checks are omitted and no prior world hash/feasibility is cached.

The private payload implementation follows the versioned
[XXH64 specification](https://github.com/Cyan4973/xxHash/blob/v0.8.3/doc/xxhash_spec.md#xxh64-algorithm-description):
local lanes, explicit little-endian reads, full stripes/tails and modular arithmetic.
No unsafe/native/platform/NuGet dependency or content-specific pattern choice.
Both64-bit algorithms are non-cryptographic; neither is a hostile-mod security boundary.
The former2MiB static correction array/creation and all uniform/binary branch
machinery are REMOVED from production, not retained as a compatibility fallback.

Two earlier whole-stream experiments are CORRECT but REJECTED on guarded work:

- 2.677:32-byte array tail/scalar streaming,570.676ms/9545512 allocated bytes;
- 2.678:inline-word tail/scalar streaming,457.988ms/8934408 bytes;
- matched2.676 generic0/3 baseline:307.108ms/7710280 bytes.

Those variants are not production alternatives. Core snapshots remain at framework
artifacts/streaming-hash-candidates/2677 and2678; former golden data is archived in
2678. Their failed performance is not erased by correct oracle/micro results.
No native saved world was claimed on either rejected build.

### Independent correctness and validation scope

The existing StateHasherGoldenTests is revised in place, not supplemented by another
production hasher. Former TABLE-specific exhaustive cases are retired with the removed
tables; scalar boundary literals are preserved verbatim in ScalarVectors.
A test-only byte recorder plus independently written one-shot XXH64 oracle verifies
the new typed fingerprint. The oracle itself is checked against312 vectors generated
by already-installed Microsoft System.IO.Hashing10.0.1, assembly SHA
2679A6164163C1F4B7B5FADA83A55EB48B0A85840E04F00E8921B8D495A66B84.
No new runtime/test package, installed tool or dependency reference.

Authored testdata/state-hash-vectors.json records payload lengths0–65 plus80,
127/128/129,255/256/257,511/512/513,1027/4099; four byte patterns and signed root
prefixes. The final payload vectors differ intentionally from the rejected
whole-stream candidates' encoded-prefix vectors; both are independently generated,
not expectations read from the implementation. The final three algorithm scopes cover
every codec, original signed scalar boundaries, digest-read-then-append, tails/stripes,
every single-bit mutation over four256-byte backgrounds, all256 byte values at
short boundaries, observation parity, concurrent owners and zero warmed append allocation.

Existing supplied-work owner/fixture is migrated ONCE from BinaryLayers to ByteLayers,
with two-valued layer changed0/1->0/3 BEFORE its baseline. Same original factory,
commands, accounting, two real carriers,29 Updates, whole16-unit deliveries,
per-Update saved twins, isolation/private facts and replay; no driver gameplay.
Actual production calls stay inside original engine contracts.

Six final scopes49627/0 on identical production core5B5851F6...:
oracle2230, boundaries45996, shapes18, physical515, original guards776,
native protocol92. The final scalar-literal addition changes ONLY the oracle test;
only that case reruns (2230 versus earlier2200). Unrelated passed cases are retained,
not rerun after the test-only strengthening or later documentation.

Exact framework command for each literal:
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll
--bounded-exact "<literal>" --timeout-ms 60000 --termination-timeout-ms 5000
--output-chars 20000.
Raw framework artifacts/streaming-hash-observations/<id>/run.log.

Labels: oracle=current canonical/payload official vectors/every codec;
boundaries=field/stream boundaries, mutations, independent owners;
shapes=byte codec shapes canonical stream and allocation costs;
physical0/3=Guarded byte layers: actual supplied hauling update costs saved twins and replay;
guards=Shared spatial observation: original codecs and revisions guard physical native and point callbacks;
native protocol=Current native protocol: authored expectations and physical selected-role checkpoints.
Rejected candidates' literal names refer to streaming rather than the final typed algorithm.

|Build/scope|Passed/failed|Worker/outer ms|Log id|
|---|---|---|---|
|2.679/boundaries|45996/0|110/331|0775da4ebaa14e4fb5eafafb713bb794|
|2.677 rejected/boundaries|45996/0|105/349|0967ef1e986f4d2cb141959d00835bee|
|2.679/guards|776/0|10876/11133|1a164b2e971a4abc9c3abd8204c8a90c|
|2.677 rejected/shapes|18/0|3329/3555|3a31791c3ea64fc2bc1e6b9e397e8343|
|2.679/oracle|2200/0|48/240|41b70651302a4fa18ada85d8ec341708|
|2.678 rejected/oracle|2200/0|61/322|471f174aa59b472ea400ee99f573e9c3|
|2.678 rejected/boundaries|45996/0|122/373|4e13005634ef41708b382eb6009d45e6|
|2.679/shapes|18/0|3295/3503|5ffea4b65e964ed1a4dc94629569b28a|
|2.676 baseline/physical0/3|515/0|1294/1462|6bc9286dd7be44cc9ddf2dd91b4a0ee7|
|2.678 rejected/shapes|18/0|3362/3698|90e94dc123bb491ebad5189bf07ed49f|
|2.679/physical0/3|515/0|1473/1647|9fc4c7fe15594219be4bd9eaf5a6f6ca|
|2.677 rejected/oracle|2200/0|60/288|bab6c252d9494fa5bae5846c3f55431a|
|2.677 rejected/guards|776/0|19823/20144|c1c097776cd14a5086f591a582f20b92|
|2.676 baseline/shapes|12/0|3221/3411|c7aaecbb722744fcbce4d1e84b83c149|
|2.677 rejected/physical0/3|515/0|2698/3134|d076478e1cb144c296ddbbe5cdaf52ee|
|2.678 rejected/physical0/3|515/0|2010/2306|d5b96b870c6340819199ac1a05b677fb|
|2.679/native protocol|92/0|3205/3386|d8f87df4a2dc46999c7382cbe431131c|
|2.679/oracle|2230/0|40/190|d5d17782d27243a1812fb37124f58724|

All18 recorded functional invocations150588/0, all terminal exit0/no abort/no output
truncation. This count includes baselines/rejected experiments/intermediate oracle;
it does NOT mean performance passed or18 independent whole-game suites.
No full/default/profile/family suite or default full marine run.

Builds: baseline test7.89s clean; first2.677 compile FAILED16.30s on test CS0675
sign-extension warning-as-error, then existing unsigned cast fixed it and10.16s
clean.2.67822.78s and2.67910.67s clean. Final test-only scalar oracle build5.96s clean.
Final consumer proof1.99s clean. No compile/test overlap; every running handle was
waited to actual completion. No executable change after final validation except
that explicitly scoped oracle-only strengthening; core/consumer unchanged.

### Performance is mixed, not a universal improvement

Same warmed16MiB shape workload; final append allocates0 per call:

|Shape|2.676 baseline ms|2.679 ms|
|---|---|---|
|zero|1.406|5.438|
|uniform3|1.429|3.603|
|water edge|8.764|3.447|
|terrain edge|10.837|4.475|
|small palette|16.487|3.638|
|noise|16.469|3.346|

Mixed edge/palette/noise patterns2.4–4.9x faster locally; uniform inputs SLOWER.
Final generic29-Update work334.514ms versus307.108ms is8.9% SLOWER, with
7711160 versus7710280 allocated bytes (880 more, essentially unchanged).
Original guards10876ms versus previous8831ms also slower. These remain visible;
no cold/hardware/statistical/all-size or allocation-reduction claim is made.

### Actual current native measurement and saved-state equivalence

Only prepare and ONE original first harbour day ran after the strict build change.
Unchanged original literal Offshore: vessels survey, drill and ship from the sea;
--bounded-exact --timeout-ms 150000 --termination-timeout-ms 5000
--output-chars 100000 --case-input "<input>".
Raw consumer artifacts/marine-typed2679-observations/<id>/run.log:

|Input|Passed/failed|Worker/outer ms; stage ms|Log id|Native image/time/hash|
|---|---|---|---|---|
|prepare|37/0|3971/4098;n/a|2753f0ca52484e88b085911728190307|fa1c31b444ca462ebf2386133adfbd80/11/2BB2DFA0A8F81502|
|harbour:fa1c...|35/0|36543/36804;35160|3a6186993e4a44ee8580b14d51fb1004|21b55742c7bd4a6fb31f46a8018c098e/1451/C8B349CDD5FF5B06|

Both terminal exit0/no abort. Original120000ms/full1440 game-minute observation
bounds and all machine/material/physical timing unchanged. First day35160ms versus
2.67649019ms is28.3% lower locally, not a universal engine/large-world result.
Matched visible physical state is identical: depot379/480, shipyard awaits real
material; truck#6/job9 carries15.333 and truck#5 travels on job10.

READ-ONLY binary comparisons prove stronger state correspondence at those two
checkpoints. At11 both native saves33154 bytes; at1451 both134223 bytes.
Every difference is the build-version character at offset35 and the final eight
ORIGINAL FNV file-checksum bytes (33146–33153 or134215–134222).
All other bytes, including gameplay payload and content fingerprint, match.
No saved header/checksum/body was edited, replaced, migrated or loaded as compatible.
The save FILE checksum remains its original FNV routine; state fingerprint changed.
Header-first strict build refusal remains, save160/replay1 payload layouts unchanged.

Historical2.679 save identity:
consumer src/oilCo-engine/src/SimEngine/artifacts/marine/21b55742c7bd4a6fb31f46a8018c098e/checkpoint.json
and sibling game.sav;harbour_progress/next harbour_execution,
1451/hashC8B349CDD5FF5B06. Full harbour, fleet, optional survey and PAD are not
qualified on this build. The earned2.676 first-vessel15327/83b525... image remains
HISTORICAL/STRICT-INCOMPATIBLE, not the current pointer; no header rewrite.

The later2.680 diagnostic sampled this saved phase. Its old physical resume
instruction has been removed: only the current2.681 handoff above is executable
against the current strict build. The native artifact remains unmodified evidence.

### Identity, boundary review and remaining gates

SHA256:

- production core/test copy/consumer copy:
  5B5851F69A02A932D38F204A570CF8F0ED27874A080428B629727838D8079572
- final test DLL after oracle-only strengthening:
  8BADE093645A0551ADC529028CBB2540EF43A4A3C84781E4AED28BCAE33DD70B
- earlier same-core test DLL for unchanged boundary/shape/physical/guard/protocol cases:
  B0C24813D0AB1D342945FCD0CF3BA7D17BE05E2C27197678591DFF1D6642FE96
- StateSync.cs / SaveGame.cs:
  EA271865353D933D0A268FBEE220CD1864ED6C4ED18571D3E6A505D939D6FCD3 /
  1821A8F8DCE74491B4811866AC91CAC27271B7EBC4D0AC15802ED6C5E23A1CED
- final StateHasherGoldenTests.cs / vector JSON:
  C52891854542C4D7E4C7F8086DDED704FD77EE7CB70B983B1457A0B6BD7151F8 /
  45C9B3B031B3312EDC752D4CE5C46ACE44ECB7FB79416E46A9FE242957366DAE
- ByteLayers.cs / original CapacityLogisticsStressTests.cs:
  DE1CFBC216516C4BA1AFF8E00EC698CBB7DB91BCFB96D0B740BF66F89ACB4FF3 /
  C8595D011ECD0A01CDDA1A0CEA229F822502A11F89CA9029BFAC0280086F2E41
- consumer proof DLL / diagnostic-only OffshoreTests.cs:
  242B3890B5B9A445D8F6BB7DF2AAA466956D40F73069A24B98865CE2C01DD445 /
  0A74A4D50469FC2392FAD558E94D7ED13178E053FCFF07097B81DF563E8066C9
- CURRENT1451 checkpoint JSON / game.sav:
  21AD6C7766C36084AA810ACC6FAFCC8FC49A71A04732D5042DA232A5E1B04570 /
  4369D17B5794683E6B4A2483C826A3C97604BBDF707695ED964E37AB2EE2FFE5

Rejected2.677 core C8F0847CBDF71097F50ADF0371792D2DB1A7E9BD4E6BCACB8C308AFB682B254D
and2.678 core7C3C987F42D6AF2228295AF277F0D4BAFA4347092745ED72266326A7BD5C1EDB
are archived but not active production/compatibility implementations.

Source review: no observer/callback/revision/full codec/clock/route/dock/fuel/area/
claim rule was changed. No size ceiling, game clock, gameplay model, platform/Draw,
business/Tycoon/content/host gameplay, new provider service or dependency.
Public inspector aggregation is diagnostic only. Existing
[operational/affected-event mapping](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
applies: first-fault/callback errors/views/replay retained; no parallel logger or
silent fallback. Protected audit/outage/hostile-provider, current full busy/PAD,
small/uniform-workload performance, allocation/soak/cross-platform/replay/lockstep/
full native/whole-plan gates remain OPEN; no independent review/CI claimed.
No primary requirement closure, staging/commit/push or full-goal completion.


Final read-only hygiene2026-10-20T13:20:35+03:00:13 reconciled current documents,
4059 local Markdown links/0 missing targets, current handoff/decision anchors match
their headings, both repositories diff --check0/staged0. Core/final test DLL,
current1451 save and historical2.67615327 save SHA rechecked unchanged after
documentation. All18 hash cases and both current native cases have terminal exit0;
no selected live handle remains. No build/test after documentation updates.

## Current2.676 exact binary-byte folds — 2026-10-20

SELF-REVIEW; Windows/PowerShell/.NET SDK10.0.401/+03:00, evidence through
2026-10-20T11:57:07+03:00. User-installed sdlc-orch implementation/verification,
snapshot fdc9dceb0bae90a05c1575cb033849daee0e6744. Framework master
cb61483ab81404c11944931aedb4703ba694edb5; consumer master
8be1eed8b54649ac35cb1a2fe01051dd59d1982e. Both dirty, staged0;
framework3011 paths/consumer101 before documentation reconciliation.
The preceding visible turn was a read-only TileMap terminology clarification,
not implementation progress. This turn revalidated the actual tracker, implemented
the measured byte owner, and obtained focused baseline/final/native evidence.
Original221 requirements/209 recorded checked/12 open and full goal remain OPEN.

### First current vessel and active-work sample — 2026-10-20 12:26+03:00

SELF-REVIEW; same2.676.0/save160/replay1 and exact source/binary identities below.
Previous goal turn was PROGRESS: full harbour, once-only fleet admission and first
physical production day. This turn resumes SAME9567 world and earns its first
complete100h vessel. No engine/domain/content/proof source edit, build, changed
quantum/duration/materials, grant, steering, header rewrite or prior framework-case
rerun. No full/default/profile/family suite or Git publication.

Same original literal and bounds:
Offshore: vessels survey, drill and ship from the sea;
--bounded-exact --timeout-ms 150000 --termination-timeout-ms 5000
--output-chars 100000 --case-input "<input>".
Native logs: consumer artifacts/marine-binary2676-observations/<id>/run.log.

|Input/time|Passed/failed|Worker/outer ms; stage ms|Log id|Earned native image/hash|
|---|---|---|---|---|
|fleet:932b...;9567->11007|211/0|101034/101150;99592|8dec3a4e9e10439dbf6bf22362d5318d|b32ce175145543648ab71d3db75ea2c5/A83252930225CD43|
|fleet:b32c...;11007->12447|211/0|99347/99467;97910|c68618de8a1d4d4581fdb01ae33bef5f|acdb39fac7b4484a82160b4a4be85326/A3B4D4DE7EBBA6E5|
|fleet:acdb...;12447->13887|211/0|104622/104762;102883|6e105e1851f34691b66c8532266f457e|825c7a56467040fc8ed1d983d6cd47b4/AADFFD8DA7DCD3C5|
|fleet:825c...;13887->15327|211/0|51800/51945;50109|193117a18a26451fb8ef25549d609e48|83b525bdf0f4484c98012e59ec64aac9/6796CA63D294CD10|
|traffic-trace:825c...;13887->13935|297/0|7770/7932;n/a|260ba909b9014fdc808f5e56dd9087b5|NO new save|
|inspect:83b5...;15327 unchanged|12/0|1860/1999;n/a|0b435b8dd2504a259e5db827e7b4e195|NO commands/Update/save|

Six new exact invocations1153/0, terminal exit0/no abort; original120s stage
bounds retained. Raw trace log is complete; tool display was truncated, NOT the
bounded collector's stdout (its authoritative record says False).
Current-build native namespace totals16 terminal invocations1732/0 including
previous milestones; this is not full engine/marine acceptance. Handles78452/
56918/98843/1167 were waited to terminal, not restarted on polling timeout.

Native production1268->2708->4148->5588/6000 earns the WHOLE authored100h
survey_vessel#16, not an early spawn. It physically rolls out onto water and
public inspection reports home#13/AtHome, no home/motion blocker. Actual position
(69.833,72.500), original dock/parking and sole GameClock owners; no host motion.
Original recipe1 retires; recipe2 build_supply_boat now Working848/3600. Six
recipes remain, all actual haul jobs settle at this saved moment. Neither whole
fleet, current optional survey, pad nor drilling is claimed complete.

CURRENT compatible continuation:
consumer src/oilCo-engine/src/SimEngine/artifacts/marine/83b525bdf0f4484c98012e59ec64aac9/checkpoint.json
and sibling game.sav;15327/hash6796CA63D294CD10,
fleet_progress/next fleet_execution. Earlier2.676 images remain compatible earned
history, NOT the latest resumption pointer. Diagnostic13935 is NOT a save.
Historical2.675 native states remain strict-incompatible, untouched.

Runnable physical continuation, NOT RUN after this vessel milestone:

~~~powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000 --case-input "fleet:artifacts/marine/83b525bdf0f4484c98012e59ec64aac9/checkpoint.json"
~~~

#### Current busy-hauling profile, not post-supply home cost

A first intended attachment found actual worker19580 had already exited; no
collector attached to that stale PID, no trace claimed and no day restarted.
Instead the existing48-Update diagnostic loaded earlier SAME-build13887 state,
where actual two loaded/travelling trucks and ongoing production were active.
The collector confirmed actual --worker-exact PID34476 with exact825c... input,
not the outer observer. Existing installed diagnostic-tools/dotnet-trace10.0.731102,
no installation/package/source change. Command:

~~~powershell
artifacts/diagnostic-tools/dotnet-trace.exe collect --process-id 34476 --profile dotnet-sampled-thread-time --duration 00:00:00:03 --format Speedscope --output artifacts/route-cost2676-fleet-profiles/ea560d1802ea4d61a3bb0f59aa351c49/fleet.nettrace
~~~

Collector exit0; collect.log, fleet.nettrace and fleet.speedscope.json retained in
that same framework artifact directory. Balanced event integration uses actual
Kernel.World.Step stacks only, excluding host snapshot/hash reads; nearest-real
exclusive removes synthetic *_TIME leaves. Trace span3028.3018ms, actual Update
interval1546.0120ms,29484 events, balanced final stack0. A broad frame dump was
tool-output truncated and rejected by JSON parsing; filtered analysis succeeded
from the unchanged raw profile, not an invented substitute.

Nearest-real exclusive Update shares: StateHasher.Bytes37.21%,
TileMap.Sync7.34%, MapLayer<byte>.ValidateValues6.65%.
Inclusive: provider hash59.81%, actual map Sync57.68%, DockJourney50.99%,
PathSearch.FindCore48.49%, idle-home Update7.04%/ReadHomeParking6.90%.
Inclusive shares overlap and MUST NOT be added. This is a short phase-specific
diagnostic, NOT a universal CPU distribution or a controlled before/after
comparison with the DIFFERENT historical PAD sample.

Actual48 Updates: min/mean/max44.182/91.3868/601.178ms;
mean7719475.67 allocated bytes, spike40313792 bytes at13932. Sampling overhead
is included; no allocation/latency improvement claim. Clock advances13887->13935
with material handling/travel and5600+ productive minutes, no native save.
The latest real15327 continuation above is unchanged by this diagnostic.

The original byte/observation/route owners still dominate. Next engine cost work
must inspect their ACTUAL busy payload and generic equivalent before another
optimization; a binary0/1 micro-vector alone does not qualify the public terrain
view's3/0 mixtures or the complete current codec. Do not weaken full codec/revision/
provider/clock checks, cache positive feasibility/world hashes, special-case Oil,
pick an easy idle-home allocation from the wrong phase, or blindly replay the
remaining fleet merely to conceal the unresolved PAD gate. A justified production
change still requires strict build identity; never rewrite this earned save.

SHA256 rechecked:

- unchanged production/test/consumer core:
  99286DBFF4AB8C16B5F2600C5533FAB5DCA03388227C1C32ECF3ECFF5C26A4CA
- unchanged framework test DLL:
  3098C14FF25BD6F9B0E0AEED9D861344CDE3A2941579E8166736629C0B4A342E
- unchanged PublicProof DLL / StateSync.cs / OffshoreTests.cs:
  1340CF11FFE62A5A811417D52CDA9B1E7611ACDE4A4CD56EC91720386BFC13AA /
  F68517D3690580D451529C7D5A7B2161944A2EAACDF2B136510151AEFEB42AE6 /
  EB9A7FCACD1BBDA996577BB30244DB1DBCB6A85368FCAD07EEC102FB4330956A
- CURRENT15327 checkpoint.json:
  385DDFF6C2FCE4DF8291F25BF2B16D15871AD71C9B11968FC13FD88B1A45ACBE
- CURRENT15327 game.sav:
  5A765F4E6425CCB1B310902C13BACC87B041E498FFA3FD9F7CD08DAF12F391A0

Read-only final hygiene2026-10-20T12:30:10+03:00:13 reconciled current documents,
4057 local Markdown links/0 missing targets, both repositories diff --check0 and
staged0. Native namespace independently recounted16 invocations/1732 passed/0 failed,
all16 terminal Exited/exit0; collector separately exit0. Current save/core/proof
source SHA unchanged after diagnostics/documentation. No runtime after doc updates
and no selected live handle remains. No original requirement checkbox changed.

Operational/assurance mapping below remains unchanged; no new logger/audit owner,
protected audit/outage/hostile-provider/soak guarantee or independent review/CI.
Original221/209 recorded checked/12 open/full goal ACTIVE, no primary checkbox
closure. Earlier CURRENT labels below are historical milestones. Full fleet/
PAD120s performance/P9.051/broader plan acceptance and publication remain OPEN.


### Native harbour completion and first fleet day — 2026-10-20 12:12+03:00

SELF-REVIEW; same2.676.0/save160/replay1 and actual core/test/consumer identities
as the implementation record below. Previous goal turn was PROGRESS: it changed
the measured engine owner, completed four focused checks and earned first-day1451.
This follow-up resumes that SAME world. No source/content/proof edit, build,
framework-case rerun, new optimization or private state injection. Only the
original public player commands/views, Game.Update and native save/load execute.

Same literal Offshore: vessels survey, drill and ship from the sea, with
--bounded-exact, --timeout-ms 150000, --termination-timeout-ms 5000,
--output-chars 100000 and --case-input "<input>".
All original120000ms observation bounds, productive durations, quantities,
physical machine/route/handling requirements and cumulative deadlines retained.
Raw consumer logs: artifacts/marine-binary2676-observations/<id>/run.log.

|Input/native time|Passed/failed|Worker/outer ms; stage ms|Log id|New native checkpoint/hash|
|---|---|---|---|---|
|harbour:6db4...;1451->2891|35/0|73953/74077;73113|1fb1076defa94f2aa89c7a401223cf8f|bbdc3cd561d84b249bf04b4d6dcc146c/7BB9F98358A81ED2|
|harbour:bbdc...;2891->4331|35/0|24766/24896;24014|f8984a2d5ce34a9d87d83266d2a5ad27|3767dd9f43c94d29b6771278eeabbc69/87E6004E1CF7EEA4|
|harbour:3767...;4331->5771|32/0|26855/26970;26120|7bea50edf85a4489bbacfd72ddde82ca|dc2415d3174f48b6b0e638acb6e5b910/D702F5DA3EA5B127|
|harbour:dc24...;5771->7211|26/0|23392/23504;22702|eb0b8de736254ec996845a8f3d143959|7133dd0ab903477d985e1930afad4723/528271EC5592B92B|
|harbour:7133...;7211->8111|23/0|16490/16608;15824|1eee19cce5ea44609a0b0399bb729301|ce959e31cab54ea0a475c2af00082226/DF87EA156D6CE938|
|inspect:ce95...;8111 unchanged|12/0|1390/1502;n/a|10835dedbeb24a6dbedae761e64f8e4f|NO save/commands/Update|
|fleet-prepare:ce95...;8111->8127|133/0|8032/8143;n/a|1a00944d42a14736bbe43ebf3d17b4e1|ad7e61a4e86742d6a93bf3c52de900c5/4DCED3C9D1ED2C9D|
|fleet:ad7e...;8127->9567|211/0|97848/97958;96353|772b92e226fd407fa854dccabe8b31b9|932bb93eac764a0e90b3adfa0ae78152/34FA986292652D79|

Eight new selected invocations507/0, all terminal Exited/exit0/no abort/no output
truncation. Complete new-build native namespace now10 terminal observations579/0,
including previous prepare/day72/0; NOT a full suite. Live handles83824/58011/
70153/97085/59023/8486 were polled to their actual terminal result, never restarted
after an observation timeout. No source/build changes while any worker ran.

The HARBOUR is now physically COMPLETE at8111: actual shipyard#13, depot#14
and grid connection#15, all original construction orders/jobs gone. Pure public
inspection preserves that exact state/hash; crane#3 is legitimately Returning
with no home/movement blocker and conditional128-minute remaining journey.
It is not teleported, stopped by a test or treated as instantly AtHome.

Original nine fleet transfer routes and seven vessel recipes are admitted ONCE
at8127, using the original producer queue and physical resource owners.
First day delivers real material and earns1268/6000 minutes on the100h survey
vessel. Both actual trucks remain physically unloading to the depot on jobs32/33
with2.000/20.666 cargo. Seven outputs remain in the actual queue; NONE is claimed
manufactured. No survey/pad/drilling/shipping/subsea on this current build yet.

Recorded previous2.675 first fleet day98497ms (raw log
artifacts/marine-wide2675-observations/90694257b79e49e094e06ed8fb585af0/run.log)
versus current96353ms is2.2% lower locally, EXACT same9567/hash34FA986292652D79
and211 checks. This is a modest same-state observation, NOT a universal speedup,
allocation reduction, full fleet proof or closure of the historical pad failure.
No additional CPU sample or native allocation distribution is claimed this turn.

CURRENT compatible continuation is consumer
src/oilCo-engine/src/SimEngine/artifacts/marine/932bb93eac764a0e90b3adfa0ae78152/checkpoint.json
and sibling game.sav; fleet_progress/next fleet_execution, healthy9567/hash
34FA986292652D79. Earlier2.676 harbour/admission images remain compatible earned
history, not the latest pointer. Historical2.675 saves remain strict-incompatible;
no header rewrite or reset. Next exact action, NOT RUN after this first fleet day:

~~~powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000 --case-input "fleet:artifacts/marine/932bb93eac764a0e90b3adfa0ae78152/checkpoint.json"
~~~

SHA256, rechecked against actual files:

- unchanged production core/test copy/consumer copy:
  99286DBFF4AB8C16B5F2600C5533FAB5DCA03388227C1C32ECF3ECFF5C26A4CA
- unchanged framework test DLL:
  3098C14FF25BD6F9B0E0AEED9D861344CDE3A2941579E8166736629C0B4A342E
- unchanged PublicProof DLL:
  1340CF11FFE62A5A811417D52CDA9B1E7611ACDE4A4CD56EC91720386BFC13AA
- unchanged StateSync.cs:
  F68517D3690580D451529C7D5A7B2161944A2EAACDF2B136510151AEFEB42AE6
- unchanged OffshoreTests.cs:
  EB9A7FCACD1BBDA996577BB30244DB1DBCB6A85368FCAD07EEC102FB4330956A
- completed8111 harbour JSON / game.sav:
  5048594763856F10D1A8B5627565FC2C2BE730051A503E40B837F314B00777C2 /
  78FB5BD99B1B1B3D3F1098756359DD1689DC70EC9FF449217DB5EE70165D136C
- fleet-admitted8127 JSON / game.sav:
  E6BC8721048F985044620C35B4AF5125035A42521FC0BA370638C3796B2EEDE6 /
  6B77EBFB9651512F7CB43D3A948F1061447DDD5FF1F91C37355C2A08CE3BF4F5
- CURRENT9567 JSON / game.sav:
  142050E1C820B9C29C948CAB07225CF0B64BB856D120583064F0F6FF8E9FCD10 /
  A50535FFB1B1C65560361371ECAF3FEAEDCF8B414D54BC35BCC69263AD78B62E

Read-only requirement reconciliation matches all221 original frozen audit IDs to
actual primary-document checkboxes:209 recorded checked/12 open. Wrapped source
lines and trailing indented evidence annotations must be normalized; an initial
single-line scan and then exact whole-block scan did not match all entries and
were NOT accepted as an audit. Frozen COMPLETION_AUDIT.json remains its historical
2.569 snapshot, not a current execution claim; its source anchors were not relabeled.
No original requirement text/checkbox changed. Remaining original IDs:
P5.005/.008/.009, P6.007, P7.017/.019, P8.017, P9.051, P10.022,
P11.026/.029/.035 (all acceptance IDs).

Read-only final hygiene2026-10-20T12:15:10+03:00:13 current documents/4057 local
Markdown links/0 missing targets, both repositories diff --check0/staged0.
Dirty inventories remain framework3011/consumer101; current native save and
unchanged OffshoreTests.cs SHA rechecked. No test/build after documentation,
no remaining live selected handle and no current requirement marked complete.

P5 timed scenarios/current full marine, actual PAD120s performance, P9 recovery/
no-deadlock and all broader provider/lockstep/soak/operational assurance gates
remain OPEN. No full/default/profile/family test, business/Tycoon, engine/consumer
source, platform/Draw changes or Git publication. Earlier CURRENT native pointers
below this follow-up are now historical milestones, not resumption instructions.


### Scope, mathematical identity and ownership

The earlier ACTUAL active PAD-supply profile below establishes StateHasher.Bytes
as53.40% nearest-real exclusive Update cost; post-supply idle-home percentages
are a different phase. No new CPU distribution is inferred from this change.
The SAME StateHasher now folds nonuniform eight-byte words only when EVERY byte
is0 or1. For a fixed word w and h=256q+r, the original FNV sequence equals
h*P^8 + (F_w(r)-r*P^8) modulo2^64. The original sole constant array/factory is
renamed ByteCorrections/CreateByteCorrections and extended by256 masks*256 low
states (524288 bytes, total2097152). These are immutable mathematical constants,
not remembered session/world fingerprints or a second owner.

All eight current bytes are decoded explicitly little-endian and tested with
0xFEFEFEFEFEFEFEFE. Multiplication by0x0102040810204080 packs their low bits in
original byte order. An independent JS BigInt check covered all256 masks before
implementation; the executable scalar oracle below covers every mask and every
entering low state. Uniform8/32/256 paths, complete length prefix, arbitrary-byte
scalar fallback, tails and following scalars remain canonical. No callbacks or
guard scopes were removed; no route/dock/fuel/claim/area rule was changed.
No new clock, state schema, gameplay model, module, package, renderer or adapter.

Build is now STRICT beep-simengine-2.676.0/save160/replay1. Old2.675 native saves
are historical and incompatible with this build, even where game hashes match.
Neither saved headers nor old artifacts were rewritten.

### Generic actual supply regression and oracle

Existing CapacityLogisticsStressTests.Factory gains only an optional JSON-authoring
callback. The new exact BinaryLayerSupply case reuses its existing physical
source/endpoints/carriers, accounting and original profile bounds. Authored72x20
native byte layer stripes contain mixed0/1 words; two ordinary haul commands
exercise real guarded routes, reservations, handling, loaded cargo, movement and
whole deliveries. Driver code performs no stock/position/path/completion writes.
Every continuation Update checks saved twin identity and physical conservation;
normal replay and an isolated session are checked. No Tycoon module is required.
This is a bounded generic supplied-work proof, NOT the actual large PAD workload.

The new StateHasherGoldenTests.BinaryBytesCase uses the SAME independent original
scalar oracle already owned by that test class:256 masks*256 low states,
7/9/31/32/33/255/256/257/513 boundaries, every bit mutation (including nonbinary
fallback), following signed/small scalars, observation parity and eight concurrent
mixed-stream owners. Warmed hashing allocates0 per timed byte-call loop.
336923 assertions are ONE selected literal, not336923 workflow/test suites.

### All builds and exact attempts

Build command: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj
-c Release --no-restore -v quiet. First build FAILED in7.00s with three missing
test imports (ReplayLimits/ReplayVerifier and TileSpatialSnapshot). Added existing
Hosting/Maps imports only. Baseline build7.06s and final production build8.02s:
both0 warnings/errors. No compile/test overlap.
Consumer target build: dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj
-c Release --no-restore -v quiet,1.85s,0 warnings/errors; no consumer source edit.

Framework invocation for each literal below:
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll
--bounded-exact "<literal>" --timeout-ms 60000 --termination-timeout-ms 5000
--output-chars 20000.
Raw logs: framework artifacts/binary-byte-fold-observations/<id>/run.log.

|Build/scope|Passed/failed|Worker/outer ms|Log id|
|---|---|---|---|
|2.675 generic supplied hauling baseline|515/0|1204/1357|98afef02008f49fb87671fe82bd60bd8|
|2.675 binary-byte oracle baseline|336923/0|3260/3412|997bea67c3ad462c9dc0b76d353de93c|
|2.676 binary-byte oracle|336923/0|3216/3363|40d96a8cb6594311bfb88bdf171d19ad|
|2.676 generic supplied hauling|515/0|969/1123|5468f54eec6f420593176de0f61ead09|
|2.676 existing shared spatial fault/physical proof|776/0|8831/8998|209748cc5aa54669a5e2cfc08d273abc|
|2.676 existing wide all-values oracle|301331/0|5322/5476|f08815980f364c0d8cbb36caac8db7f0|

Exact literal names:

- State hashing: exact binary words masks boundaries fallback and concurrent owners
- Guarded binary layers: actual supplied hauling update costs saved twins and replay
- Shared spatial observation: original codecs and revisions guard physical native and point callbacks
- State hashing: complete wide uniform folds preserve every byte and owner

Four FINAL focused scopes639545/0. Two baseline scopes337438/0 are separate.
All six workers exit0/no abort/no truncated output. No full/default/profile/family
suite run, no repeated historical cases after documentation/unrelated edits.
Existing shared776 checks retain actual construction/effects, native/point spaces,
half/full quantum, original exceptions, layer/revision mutations, refusal, saved
continuations, private publication and replay. This is not every provider matrix.

Same new warmed16MiB byte loop, engine milliseconds before->after:

|Pattern|2.675|2.676|Timed allocations|
|---|---|---|---|
|alternating|16.465|5.256|0|
|binary-masks|16.013|5.965|0|
|binary-tail/nonbinary last byte|16.019|6.645|0|
|zero|1.351|1.325|0|
|terrain|1.498|1.518|0|
|noise|16.464|16.486|0|

Scalar control16.189–16.452ms before/16.181–16.468ms after. Local binary patterns
are2.4–3.1x faster; unchanged uniform/noise patterns are approximately flat.
Generic actual29 Updates:287.647->248.489ms (13.6% lower in these observations),
7711160->7719320 allocated bytes (8160 MORE, not an allocation improvement).
Both finish clock29/hash5BC771AEC092D5CC, with physically delivered16 units.
No warmed/stable statistical distribution, arbitrary world CPU/soak or universal
speedup claim is made from one generic run per build.

### Native current-build first-day measurement and continuation

The unchanged consumer literal is Offshore: vessels survey, drill and ship from the sea.
Only prepare and ONE harbour day ran on the new strict build:
--bounded-exact "<literal>" --timeout-ms 150000 --termination-timeout-ms 5000
--output-chars 100000 --case-input "<input>".
Raw logs: consumer SimEngine/artifacts/marine-binary2676-observations/<id>/run.log.

|Input|Passed/failed|Worker/outer ms|Log id|Earned native checkpoint|
|---|---|---|---|---|
|prepare|37/0|4958/5069|404d427e90e94a2ea8d7b5dd6383cfb0|bc271de971f74354a4c8b724b6575687,11/A758028A5E3E7B7D|
|harbour:bc271...|35/0|49751/49871|4516a678c5c94f359bff2c14524044fa|6db4b611dd424e6887fe37fb7df7fd13,1451/2EC4336CCADCC849|

Actual first-day stage49019ms versus recorded2.67551036ms:4.0% lower, SAME
clock1451/game hash, original120000ms/full1440 game-minute bound unchanged.
This is only first-harbour-day local evidence, not busy-pad or full marine proof.
Both terminal exit0/no abort; waited SAME live session6720, never restarted.

CURRENT compatible native:
consumer src/oilCo-engine/src/SimEngine/artifacts/marine/6db4b611dd424e6887fe37fb7df7fd13/checkpoint.json
and sibling game.sav; harbour_progress/next harbour_execution. No engine fault.
Physical harbour construction remains partial: depot379/480, shipyard awaiting
delivered material, grid pending; loaded truck#6/job9 carries15.333 while truck#5
travels on job10. No completed shipyard, fleet, survey or pad on this current build.
Old2.675 fully earned fleet/survey and pad wall failure remain historical evidence,
not current-build qualification. Historical pad JSON/save SHA below rechecked
unchanged; DO NOT load it into2.676 or rewrite its headers.

Next in-scope native check, NOT RUN after this measurement:

~~~powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000 --case-input "harbour:artifacts/marine/6db4b611dd424e6887fe37fb7df7fd13/checkpoint.json"
~~~

Resume this earned current state, not prepare again. P5-MS1 pad task6/performance,
full marine/P9.051, original12 gates and full plan remain OPEN. Do not conclude
the original pad failure is solved from byte vectors or an earlier harbour day.

### Identities, self-review and remaining assurance

SHA256:

- production core, framework test copy AND current consumer copy:
  99286DBFF4AB8C16B5F2600C5533FAB5DCA03388227C1C32ECF3ECFF5C26A4CA
- framework test DLL:
  3098C14FF25BD6F9B0E0AEED9D861344CDE3A2941579E8166736629C0B4A342E
- consumer PublicProof DLL:
  1340CF11FFE62A5A811417D52CDA9B1E7611ACDE4A4CD56EC91720386BFC13AA
- consumer OilCo.Game DLL, relinked/no source edits:
  EE7072ADC41B5D6E211B8958E0D0049AC5E40DBC99A9F910B30C2BB610E25E08
- unchanged OffshoreTests.cs:
  EB9A7FCACD1BBDA996577BB30244DB1DBCB6A85368FCAD07EEC102FB4330956A
- StateSync.cs:
  F68517D3690580D451529C7D5A7B2161944A2EAACDF2B136510151AEFEB42AE6
- SaveGame.cs:
  4BE170CD3F54B9BB0CC08276A2C2C7A6D87E859905C0681AEF66A38E81CEBE9E
- StateHasherGoldenTests.cs:
  6459D09B89B6C104FA2E1A51F5D4724AE553FD68DF9B363804865E8F65F69ECC
- new CapacityLogisticsStressTests.BinaryLayers.cs:
  6A3ED7D69C275227592B480A9195F122E69CF590D01AAC4CADCEE36F81750B0B
- existing CapacityLogisticsStressTests.cs (authoring hook/registration):
  2EC1DB38800BF73AB502CDD3D192EC2DE4A662C389F9ABF023DB68A372421390
- CURRENT1451 checkpoint JSON:
  D2F9CCA46E0EF34C019ACAD5179D441FA847E2C8FB05E81F7D433705552A48A2
- CURRENT1451 game.sav:
  B88287E27A470C9BDBC7C4277DF451ED4C141DA8002D6498A15BC8B75114EFF0
- unchanged HISTORICAL2.67559609 pad JSON:
  44D4EA369FA3A22CC2D458B46FE8ABDF9FCA63A08BE662F499D49705E5CE75E9
- unchanged HISTORICAL2.67559609 pad game.sav:
  7F681C533F438CA3F052A7E51BBACCCB189C55EB2D5750A4B6C3D8905F2ED6D0

Existing [operational/affected-event mapping](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
still applies: original first-fault/view/replay/test diagnostics retained, no new
Log/Audit/Notification owner or swallowed exception. Protected audit, outage/retry,
arbitrary provider isolation, allocation budgets, long soak, other platforms and
whole-engine acceptance remain open. Static mathematical table has512KiB extra
process memory; startup generation occurs once, not per session. No native
allocation improvement measured; no independent review or CI claimed.
No business, Tycoon, Oil gameplay/content, platform, host-logic or Draw edits.
No stage/commit/push; no original requirement closure.

Final read-only hygiene2026-10-20T12:00:54+03:00:13 reconciled current documents,
4057 local Markdown links/0 missing targets; current handoff anchor matches its
heading. Both repositories diff --check exit0, staged0; existing line-ending
warnings are not runtime failures. Core/test/source SHA rechecked unchanged after
documentation. All eight selected invocations in the two new artifact namespaces
have authoritative terminal Exited/exit0 records; no live handle remains. No
runtime rerun after documentation reconciliation.


## Current2.675 complete wide-byte fold — 2026-10-20

SELF-REVIEW, Windows/PowerShell/.NET SDK10.0.401/+03:00; initial implementation
evidence through2026-10-20T10:13:25+03:00, later native milestones timestamped
below (latest first). User-installed sdlc-orch snapshot
fdc9dceb0bae90a05c1575cb033849daee0e6744. Framework master
cb61483ab81404c11944931aedb4703ba694edb5 and consumer master
8be1eed8b54649ac35cb1a2fe01051dd59d1982e remain dirty/uncommitted.
At the initial2.675 implementation turn, the preceding turn was a terminology
clarification/no implementation progress. That implementation changed the actual
measured engine owner. Later native-only milestones explicitly record no code change.
No original requirement is closed; the full goal remains active. Older CURRENT
labels and native build pointers below are historical.

### Current survey pass, PAD wall failure and phase-correct diagnosis — 2026-10-20 11:38+03:00

SELF-REVIEW; Windows/PowerShell/.NET10.0.401/Node24.14.0/+03:00. Previous
goal turn was progress: current full fleet completed and was inspected AtHome.
This turn revalidated actual core/proof/native identities, continued the same
saved world and earned the original full60h survey. Pad admission succeeds,
but its first one-day observation FAILS the original120000ms wall gate.
The engine stays healthy and saves actual progress. That failure is NOT erased
by successful admission, survey completion, later diagnostic movement or a fast
sample. No engine/domain/content/proof source edits, build or previous framework
case reruns. Nine exact native invocations2915/1; eight exit0/one exit1/no abort.

Same original P literal and --bounded-exact/150000ms outer/5000ms termination/
100000 output settings; raw consumer artifacts/marine-wide2675-observations/
<directory>/run.log. Every stateful continuation loads the actual prior image:

|Purpose/time|Passed/failed;worker/outer ms;stage ms|run.log directory|Native image/hash|
|---|---|---|---|
|survey-prepare55407->55408|335/0;5688/5814;n/a|6711e0e39fe24dbfaaa2279db72d3b58|40b3f14301744cfe9202bd93c57ef072/5898B8D4BAB3AC08|
|survey55408->56848|347/0;25767/25918;23909|b28946f3e0f54ae4b92358bb8ba658c0|d06451a12d544725ba7457293eb6dea5/4E5158A72C00D468|
|survey56848->58288|349/0;25961/26077;24102|212f88833c0d4dc580ad88b0503ac32d|d890ffb751c944cb923a1d82d9ea68eb/308E3A938EF3888B|
|survey58288->59188 COMPLETE|343/0;16194/16314;14112|44c52d7fe535413bb2ffd858547368ba|7e827021149f4e12a5400911cd1e6ed3/65E5CC3580A76EED|
|pad-prepare59188->59189|470/0;15193/15329;n/a|7f9ed89e43fc47fabe7594a6d5f83237|07603d4c034f4e21971ad47f9eb2c9be/70F4445A19603A68|
|pad59189->59609 FAILED wall gate|382/1;123525/123720;121431|3ffaeacb9d734b779a30debe13c53c74|06e52421aed944e1bb81a4132d28fb99/946393C7A77E63E4|
|post-supply trace59609->59657|230/0;6520/6695;n/a|19f44165e9c64eab8221f9a175d41a6e|NO new save|
|post-supply profiled trace59609->59657|230/0;7435/7762;n/a|331d4430e60f4c61a64f1e595b8a30d4|NO new save|
|EARLIER active-supply profiled trace59189->59237|229/0;31133/31358;n/a|2fbeb3bd906c47529b64d7680fa1430f|diagnostic fork of actual admitted image, NO new save|

Supply boat#17 physically delivers survey material, survey vessel#16 travels,
earns all3600 work minutes and publishes actual private field knowledge/history;
stake is removed and the vessel is normally Returning at59188. The original
scenario voluntarily chooses survey; no seismic/logging drilling prerequisite
was introduced. Native survey completion is not teleported home or instant work.

Pad is admitted ONCE at59189 via existing engine suggestion/command. At the
failed observation's saved59609, materials are supplied, the order still has
0/1440 productive minutes and native crane vessel#18 is travelling on job99.
The engine has NO fault; no completed pad or well is fabricated. Current healthy
saved progress is resumable, but the observation remains FAILED. No second long
pad day was run after failure and no timeout/duration/resource/geometry changed.

#### Paired actual-worker profiles, not a post-failure hotspot guess

Existing installed dotnet-trace attaches ONLY to confirmed --worker-exact PIDs.
Both collectors exit0 with complete Speedscope files. No package install:
--profile dotnet-sampled-thread-time --duration 00:00:00:03 --format Speedscope.
Frame attribution integrates balanced event
stacks and filters to intervals containing Kernel.World.Step, excluding host
snapshot/hash reads; nearest-real exclusive frames exclude synthetic *_TIME.
These are3s phase samples, NOT complete CPU/allocation profiles or speedups.

Framework artifacts/route-cost2675-pad-profiles/<directory>/ contain collect.log,
pad.nettrace and pad.speedscope.json:

|Phase/actual worker|Profile directory|Span / Update intervals ms|Events/frames|
|---|---|---|---|
|post-supply59609,PID40568; attach delay1.2s|761232fb5277413dbd1f299106ea3716|3021.7756 /1492.0770|28432/642|
|ACTIVE supply59189,PID36700; attach delay5s|41d8d6cb40f542779691094f0566b4ba|3032.0536 /2800.5901|16816/424|

Post-supply: StateHasher.Bytes37.29% nearest-real exclusive; geometry validation/
selection5.65/2.89% exclusive; WorkBoard.ReadHomeParking56.38% and worker idle-home
57.35% inclusive. Those home observations occur AFTER supply finished and are
NOT an adequate diagnosis of the earlier timed-out material-delivery workload.

Active supply: StateHasher.Bytes53.40% nearest-real exclusive; PathSearch.FindCore
80.99%, WorkBoard.DockJourney78.42%, TileMap.Sync78.02% inclusive. Idle-home is only
3.14% inclusive in this phase. Inclusive figures overlap: never add them.
Thus choosing a home-only or small geometry-allocation change from the first
sample would target the wrong phase. The next implementation must address the
measured active guarded route/byte-codec cost in its original owners, with no
cached world fingerprints, revision-only checks, skipped bytes/provider callbacks,
weaker joint journey/fuel/claim/area validation or duplicate scheduler/state.

Each trace has48 ordinary Updates and actual physical movement; no diagnostics
save a later image. Timing/allocation observations, min/mean/max ms; mean bytes:

- post-supply unprofiled:50.596/68.4370/211.640;12434085.83;
- post-supply profiled:49.904/76.1934/281.208;12434119.17;
- active supply profiled:392.983/527.6907/1710.819;33676549.17.

Profiler/JIT/phase differences prevent treating these as controlled before/after
measurements. Diagnostic59237 and59657 are NOT native continuation pointers.

Source inspection confirms current owner paths: WorkHomeParking uses original
ReadDock admission, actual arrival/approach and fresh routes; TileWorkDocks
queries actual authored geometry, not a new slot-list factory; Mover remains
the sole path/goal owner; SpatialSpaceContracts validates declared versus actual
geometry. No source change or speculative cache/backoff was implemented.
Next: construct/reuse a generic active-supply guarded-route/codec cost proof,
then correct that measured dominant cost and verify unchanged canonical bytes,
original first-fault guards and actual native workload under the SAME bounds.
Do not optimize a convenient post-supply allocation just because its test is easy.

#### Current resumable state and still-failed gate

CURRENT consumer artifacts/marine/06e52421aed944e1bb81a4132d28fb99/checkpoint.json
and sibling game.sav,59609/hash946393C7A77E63E4,
pad_progress/next pad_execution. Preserve this actual healthy state AND the
earlier admitted07603... input for diagnosing the expensive phase. Full fleet
8595... and earned survey7e82... remain earlier compatible milestones, not current
next actions. No reset/re-admission/rebuild/header rewrite merely to resume.
If a justified production change bumps strict build, preserve these as historical;
never fabricate compatible headers.

SHA256 current checkpoint.json:
44D4EA369FA3A22CC2D458B46FE8ABDF9FCA63A08BE662F499D49705E5CE75E9
Current game.sav:
7F681C533F438CA3F052A7E51BBACCCB189C55EB2D5750A4B6C3D8905F2ED6D0
Earned survey checkpoint.json/game.sav:
46E20EF4C1DFD438D881789CA54593ED15318BC034D3EA5C70F2FC59345898DC /
81912E9240B477D392668DFA1D5C894D7F732690C297918B2594FF32091D638D.
Core/proof/source hashes in prior identity tables are unchanged, rechecked now.
Every selected process and both sampler handles are terminal; no live worker.

P5-MS1 selected survey task5 revalidated on current2.675; pad task6 remains
explicitly OPEN/FAILED for its120s observation. Original221/209 recorded checked/
12 open retained, no original requirement closure or full completion claim.
No full/default/profile/family suite, business/platform/Draw change, source/build
change or staging/commit/push. Existing operational/assurance mapping and broader
protected audit/delivery-outage gaps remain; console profiling is not audit proof.

Current diagnostic command, already run above (not an instruction to auto-repeat):

~~~powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000 --case-input "traffic-trace:artifacts/marine/07603d4c034f4e21971ad47f9eb2c9be/checkpoint.json"
~~~

The next task is the original engine cost correction, NOT blind next-day retries
or clearing the pad checkbox because functional progress was saved.


Diagnosis hygiene11:43+03:00:13 current Markdown files/4056 local links/0 missing
targets and current anchor checked. Both git diff --check exit0 with existing
line-ending diagnostics; framework3010/consumer101 dirty entries and staged0
preserved. Native namespace55 terminal records:54 Exited0/one Exited1; the actual
pad failure stays visible, not hidden in passing totals. Both sampler processes
also exited0, no selected worker remains live. Current core/proof/source and
failed-stage native-save hashes rechecked unchanged. No executable edit/build
or automatic runtime rerun follows documentation; full goal remains ACTIVE.

### Current full-fleet completion — 2026-10-20 11:09+03:00

SELF-REVIEW. Previous goal turn made progress with the first manufactured vessel/
home observation and plan reconciliation. This turn first earns the two-output
milestone below (five selected observations657/0), then finishes ALL five
remaining ORIGINAL recipes through25 forward saved stages plus one read-only
inspection5286/0. Entire turn31 selected invocations5943/0, no aborts/failures.
This verifies the current fleet stage, not a full suite/profile/default marine
workflow or the full engine objective. No engine/domain/content/proof source,
binary, build, money/business/platform/Draw changes or older framework-case reruns.

Same actual2.675 core/copy identity B1AECCD97678BF35FAFC2BD285888AE41567D067D5DAE38D1FB3B9269F38D880.
The original seven orders were admitted ONCE at8127. Each stage resumes the
immediate preceding actual engine save; existing controller observes only ordinary
inputs/immutable facts and drives the one clock. Nothing restarts preparation,
re-admits orders, alters work/materials/power/routes, grants stock, steers vessels,
silently discards cargo or substitutes an instant/admin operation.

ALL7 outputs exist at55407, queue0, processorIdle, no work/haul jobs. Their
original durations100/60/140/180/120/120/48h total768h are preserved. Completion
takes47280 game minutes =32days20h from8127, within original60-game-day deadline.
All25 forward observations pass original120000ms internal bounds; measured stage
range19977..49275ms for this remaining-queue segment, NOT a universal performance
claim. Different earlier busier stages and the original pad failures remain recorded.

A final native reload/read-only inspection uses public WorkView/MotionView/
TileMapView facts without commands or Update. ALL7 workers#16..22 are AtHome to
shipyard#13 with no movement execution blocker; stopped arrival status is the
normal motion.not-moving. All8 starting workers are AtHome to headquarters.
Outputs are the original survey/supply/platform-crane/offshore-rig/shuttle-tanker/
subsea-layer/sea-repair vessels, physically on water, not newly spawned test assets.

Raw consumer logs: artifacts/marine-wide2675-observations/<directory>/run.log.
Original P literal/flags are unchanged. The initial19647 image is cbf5... below;
each subsequent row continues the immediately previous checkpoint. Full raw logs,
not compact console formatting, own final native facts. One early compact display
selected its pre-stage Production line; the wrapper display was corrected only,
with no test-source edit/rerun or effect on native validation/advancement.

|Earned time|Passed/failed;worker/outer ms;stage ms|run.log directory|Native directory/hash|
|---|---|---|---|
|21087|211/0;31980/32120;30580|9b7fc73dbe45435d88eec8b4f357b8cd|0be4584f85a040ac8fdedc019c386c31/0F17B1DD5353AC9A|
|22527|211/0;27922/28059;26300|c91b624ee05042a7b24949a416e14a76|825cc15984b94ccb9e31f1a1466f51fa/1F7BE6678745FBE4|
|23967|211/0;25509/25635;24163|cd9cb4e7535e40c7bfe85770486ae9a0|eb28550c469a4f65826b4ecb4e66bf1a/9B8F67C76083DF29|
|25407|211/0;24570/24695;23243|95fc183826594df49f65a960447ee129|8049c6e404904588b718b20ecb234e6e/A94D429FE56360B2|
|26847|211/0;31271/31403;29825|5dcd8fc47f7344bca15ef9b776bfa7ef|7115c633ec2648debcfb5716ee607e79/4A649C74EE1953F9|
|28287|211/0;31207/31334;29846|df1d28426df046fbb82de5518397e928|8f59beeb95e8448890a9c961e5323cf2/48E6878E590792CA|
|29727|211/0;30149/30316;28556|abc45f05c2f64fbab10b1119724f1475|fb9e40c8c9a94bf09bf81b81a21c1c47/6752685057EE9AA0|
|31167|211/0;25584/25736;24228|1c36c2476a1e4c8cb42e7bffccb397e1|a91cabe20e2a4dea9185a94b99c8118d/718A92CE7C9D3377|
|32607|211/0;26519/26679;25143|4a32222a4eac4353aba554dcafed49b3|15a0f07c94d24df4a4e17ba74d032174/888F3BC4ABAD7D6C|
|34047|211/0;25313/25461;23779|e5670ad05ab64986b04b4245fd82b28b|638cf4c6d1c24989a51050ed1145654a/5E91DDC1E7225AB6|
|35487|211/0;24790/24920;23366|5a6c01b8ad094c0ea7bf881b1bd17990|7b9749fe330b40f9b9bc545548741591/103171110E395039|
|36927|211/0;27398/27533;26032|c9e7110f5d1d4cf1b9e69bb2492d555f|7d61adf71ad54b9080d0d9f690d74fc4/4EB0ECBC62B04C70|
|38367|211/0;50673/50810;49275|f5d1b2cbc25f4d999a40ce905185c3cd|e0383e2de9834e0cbb1ac62c80054b54/03A631DAB0D364B9|
|39807|211/0;27224/27379;25608|465609fb93bc4d0ab1e8945aecc09cda|0d9a1914f97d4973a00f7d4a8429a578/1960980DEB369573|
|41247|211/0;28495/28629;27237|efdf4c69c7ec4f3b9034ff9e6d11bfab|a99655fe8786452fb4ae9171360a94c8/FB001F65DBB0C636|
|42687|211/0;28948/29098;27468|5708576df237419bb47396844df9d668|92fea479648a4489bce99a4dc2440d8a/EA84D4CA35C0D05E|
|44127|211/0;28982/29116;27603|7c8639d76878455c9646aa46ad7a76ce|afa74ec6fe7c407ea14d18870376bd85/2BB1B57916EA2BE2|
|45567|211/0;39692/39832;38375|9d6603d068b2467e9bd9386dae3fb755|38f907ba8a4c47ddbe81ef91ea26dfaa/0F716C2EBEB2FBF1|
|47007|211/0;30816/30992;29397|588f4be147304601a161fb627b6b1d06|ac3448a1281d4126afed2d28f3cfdf55/D6FD0837CEA98A7E|
|48447|211/0;24871/24997;23667|0081a73ccceb48b49ff68534646b3950|3eb5888604d54aa99f4da410d2e7ef89/BDA41BC29A68662B|
|49887|211/0;25056/25180;23820|32319716902d428d97bcfbbca473dd7e|0ec123e877c04ea6a4c9dc3f4410fb2a/251732B64D3EEE90|
|51327|211/0;32126/32257;30870|3aff796046bb45079da51848c5d372c7|3d977ddcdf7349ec9169445a5dcd9b6c/08695484A8EF6590|
|52767|211/0;34062/34199;32609|57ffe27c80f14f9e9644ec29aeca507d|73b058ec916e4c5681c950cf8a58e8ec/F57AD9551981F4B4|
|54207|211/0;25865/26001;24513|e2ef7e27f8464229bcbb78b03b530a4d|002c25e3dc4245088f56da01fd4ba283/7D7710B89346E2E4|
|55407|210/0;21159/21282;19977|a2aa7862fb464693a6690971ebf4b87e|8595ef2ad8d4491888f7a058fcb8c4c3/80D49FC1F2F39AF7|
|inspect55407 unchanged|12/0;1556/1677;n/a|2ee7d12524ad4bd2bb49ccd02a00d054|NO commands/Update/new save|


Earlier completed-fleet checkpoint (not the current pad continuation):
artifacts/marine/8595ef2ad8d4491888f7a058fcb8c4c3/checkpoint.json,
game.sav beside it, time55407/hash80D49FC1F2F39AF7,
fleet_complete/next marine_survey. Preserve THIS fully earned fleet. Earlier
same-build stages remain compatible history, not the current next action.
The game-state hash equals the recorded2.672 full-fleet result; that does NOT
make old native headers compatible with strict2.675 or authorize rewriting them.

SHA256 current checkpoint.json:
9090D8D8FDDDBC338DF686750244B01C5E80CE28BE292445BB291D6930DE6286
Current game.sav:
3A87606659B1A3D1BF25E8AA24A370A367A22D0CFB82A88C5C478862D6CCFF14
Unchanged PublicProof DLL:
2D82876E4C784C0B83CB38C4FE0AA8923CA3E0933A6CC1471373565E05197C58
Unchanged OffshoreTests.cs:
EB9A7FCACD1BBDA996577BB30244DB1DBCB6A85368FCAD07EEC102FB4330956A.
All selected parent/worker handles are terminal, no live continuation remains.

P5-MF1 selected task4 is revalidated on CURRENT2.675, not just the old2.672 result.
This does NOT close full P5.005/.008, P9.051 (road/endpoint loss, fuel/breakdown/
output blocking and broader matrices), offshore pad120s performance, all optional
modules/CI/soak/global provider purity or the full plan. Original221/209 recorded
checked/12 open retained, no primary checkbox change and no staging/commit/push.

The player action recorded for this earlier fleet milestone has NOW RUN above.
It is retained as history, not the current next action:

~~~powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000 --case-input "survey-prepare:artifacts/marine/8595ef2ad8d4491888f7a058fcb8c4c3/checkpoint.json"
~~~

That survey is this original proof's optional player choice, NEVER a new seismic/
logging/drilling prerequisite in the engine. Do not replay fleet admission, reset,
build/header-migrate merely to continue, run a full suite or invent host gameplay.
Operational/assurance service mapping below and its broader audit/outage gaps
remain unchanged; console/native proof is not a protected audit guarantee.


Fleet-completion hygiene11:12+03:00:13 current Markdown files/4055 local file
links/0 missing targets; current heading anchor checked. Both git diff --check
exit0 with existing line-ending diagnostics. Framework3010/consumer101 dirty
entries and staged0 preserved. All46 logs in this2.675 native namespace have
terminal Exited0 records; no selected process remains live. Core/proof/source
and final native-save hashes rechecked unchanged. No executable edit, build or
automatic runtime rerun follows documentation. Historical failures in OTHER
build namespaces remain preserved;46 positive records are not full regression.

### Two-output delivery milestone — 2026-10-20 10:48+03:00

Previous goal turn made progress: first full100h output and observed native home.
This turn revalidated the actual current core/proof/save, then resumed native15327.
No engine/domain/content/proof source edits, build, earlier framework-case rerun,
admission replay or business/platform/Draw change. Three native day stages and
two read-only inspections657/0, no failures/aborts; every original120000ms stage
bound/full authored duration remains. This is not a full fleet or recovery suite.

Same original P literal/flags/consumer artifacts/marine-wide2675-observations/
log namespace as below. Every stateful stage resumes its immediate predecessor:

|Input -> actual time|Passed/failed;worker/outer ms;stage ms|run.log directory|Native directory/hash|
|---|---|---|---|
|fleet:04f6...15327 ->16767|211/0;21166/21291;19740|c4483a3f46e847b493bcd410acd382db|7403547c18ca4b1e89b40ed818139d76/4BCC2BBE7E3E8DF7|
|fleet:7403...16767 ->18207|211/0;27429/27550;26051|001946be6e2f4d428c242301f4799be5|14e2b98703114970a4957a02707ea345/245216015E1C779C|
|inspect:14e2...18207 unchanged|12/0;1515/1626;n/a|31dff56c8df54e0ab4d912d056adb7b6|NO commands/Update/new save|
|fleet:14e2...18207 ->19647|211/0;22018/22129;20675|64d6c224b4b84a99b0f8f8a4df07718b|cbf5e9529653444aaf9bfcdc4bd3a928/C73D9D8CF07B3B4E|
|inspect:cbf5...19647 unchanged|12/0;1387/1493;n/a|373d3ca5266b4a518877e4bd5115030d|NO commands/Update/new save|

Two of seven actual outputs exist: survey_vessel#16 earns100h, supply_boat#17
earns60h. Original remaining executions3..7 remain in order, NextExecution8.
At18207 both vessels are AtHome/no movement blocker; third recipe correctly
waits for material. Actual saved job79 carries40 units on native haul_truck#6
from depot#14 to shipyard#13, travelling with no blocker; pure public route query
reports actual arrival[74,72], remaining10. Truck#5 is Returning with remaining5.
No player carrier selection/steering/cargo mutation or alternate supply rule.

After loading that saved partial haul, actual native Update finishes delivery.
At19647 there are no haul jobs, both trucks have physically returned home, both
vessels remain AtHome/no execution blocker, and third140h platform-crane recipe
works1370/8400 with no blocker. This observes actual current authored producer/
home separation and physical save continuation, not a universal recovery proof.

Do NOT describe this as resuming/recovering the old2.647 boat60-unit commitment.
Current authored multi-access layout lets the normal engine choose compatible
truck#6/40 units; the old counterexample was boat#17/60 at the only wet contact.
The original historical failure remains preserved, not header-rewritten or
relabeled. Different compatible native assignment is allowed by the original
port/transporter contracts, not a driver workaround. Original P9.051 remains
OPEN for removed endpoints/roads/fuel/breakdown/output blocking and full matrices.

Earlier two-output checkpoint (not the current next action):
artifacts/marine/cbf5e9529653444aaf9bfcdc4bd3a928/checkpoint.json,
game.sav beside it, time19647/hashC73D9D8CF07B3B4E,
fleet_progress/next fleet_execution. Five original orders remain; no complete
fleet/sea/pad performance/full goal claim. Earlier same-build checkpoints remain
compatible history, NOT current next actions. Old2.674/2.673 are strict-build
incompatible; never rewrite headers/reset/re-admit merely to continue.

SHA256 current checkpoint.json:
CAFF3FA3EB129ECBF6167AC3FC8BE8D045282B62BA01DD599AD3ACCEE72CCB82
Current game.sav:
904DB3B03E9385CBAE807909D88221A47AC8D8650DCFE671ECAC2CC6AD58500E
Saved partial-haul14e2... game.sav:
CEEC3D240D2148E53D9D41D4443D05DD1F3F984860CBA8A03D771C96DC64F197.
Core/proof/source identities below remain unchanged; all five selected processes
are confirmed terminal. No gameplay/source implementation change was needed
for this positive continuation; no duplicate subsystem was introduced.

Original221/209 recorded checked/12 open retained; no original requirement
closed. The parent P9 implementation step remains reconciled checked, while its
complete verification/canonical steps and the original broad gate remain OPEN.
Native views/diagnostics and first-fault owners remain unchanged; no claim of
protected durable audit or channel-outage qualification from console logs.

The following continuation recorded at the two-output milestone has NOW RUN in
the completed-fleet follow-up above. It is retained as history, not the next action:

~~~powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000 --case-input "fleet:artifacts/marine/cbf5e9529653444aaf9bfcdc4bd3a928/checkpoint.json"
~~~

No full/default/profile/family suite, stage/commit/push or loss of the original
objective. Continue toward remaining original recipes/gates through the engine,
not an easier fixture, observer-side fallback or fabricated completion.


### Sequential fleet milestone — 2026-10-20 10:40+03:00

Previous goal turn was progress: harbour completion and the first fleet day.
Revalidated current source/core/copy/save identities, then resumed the existing
2.675 native9567 image through four consecutive bounded days. No source,
content, proof, binary or build change; no earlier framework tests rerun.
Five selected literal invocations856/0 (four211-assertion stages plus one12-assertion
read-only inspection), no failure/abort. This is not a full fleet/marine/profile
or family test. Full original recipe work, material handling, route policies and
120000ms stage bounds remain; no readmission, reset, shortcut or header rewrite.

Same original P literal/flags/consumer artifacts/marine-wide2675-observations/
log namespace as below:

|Actual input -> time|Passed/failed;worker/outer ms;stage ms|run.log directory|Native directory/hash|
|---|---|---|---|
|fleet:7a37...9567 ->11007|211/0;110911/111044;109447|4056a4562a1f430b86f39a4a71854bb2|b39b977a9a5a41979fa8de147cf5393c/A83252930225CD43|
|fleet:b39b...11007 ->12447|211/0;104147/104272;102589|118afd3630d74987bfca501d95fb893b|00ff68a158884970a0161fae30afb912/A3B4D4DE7EBBA6E5|
|fleet:00ff...12447 ->13887|211/0;93273/93391;91922|e4edfcd0be1b4ae1853d20f28d55a447|7734cd0108994cc68e070261fce71cfd/AADFFD8DA7DCD3C5|
|fleet:7734...13887 ->15327|211/0;43772/43889;42291|73e37c73a116400f9cfc5d050044039e|04f631436dc04ae79d303dd358be717a/6796CA63D294CD10|
|inspect:04f6...15327 ->15327|12/0;1519/1636;n/a|70b047b084104b78b48984655f2813ef|NO commands/Update/new save|

One survey vessel#16 is actually manufactured after its full100h recipe. Native
fleet validation accounts for that output plus the remaining SIX original queue
entries; NextExecution remains8. The second60h supply-boat recipe is Working,
848/3600 productive minutes, no blocker. There are no haul jobs at this checkpoint
because actual current material supply has finished; no cancellation/drain command
was issued to obtain that state.

Read-only inspection observes survey_vessel#16 at(69.833,72.500), actual water
rollout, home#13, AtHome/blocker none. Motion execution blocker is none;
arrival blocker motion.not-moving means stopped/settled, NOT failed travel.
All eight startup workers are also AtHome. The inspection uses existing viewer-
scoped WorkView/MotionView/TileMapView facts, confirms a pure hash and performs
NO Update or commands. This observes actual home/clearance; it is not a new
host parking rule, steering input or claim of universal deadlock freedom.

Earlier sequential-milestone consumer native checkpoint (not the current pointer):
artifacts/marine/04f631436dc04ae79d303dd358be717a/checkpoint.json,
game.sav beside it, time15327/hash6796CA63D294CD10,
fleet_progress/next fleet_execution. One of seven vessels complete; full fleet,
occupied-berth recovery after subsequent outputs, sea/pad performance and original
P5.005/.008/P9.051/full goal remain OPEN. Older2.675 checkpoints remain compatible
earlier stages, NOT current next actions. Old2.674/2.673 remain build-incompatible;
never rewrite headers or reset merely to continue.

SHA256 current checkpoint.json:
385DDFF6C2FCE4DF8291F25BF2B16D15871AD71C9B11968FC13FD88B1A45ACBE
Current game.sav:
069DB7215D9FAAB6057882BC4DC80B918721344F3815A164FCED5AC2F7C3A18B
Core/test/consumer/source identities below remain unchanged; every selected worker
is confirmed terminal. The current home inspection creates no later native image.

Plan reconciliation, not a duplicate implementation: P9_PRODUCER_BERTH_HOME_DESIGN
still called actual layouts/bindings unfinished despite its earlier migration.
Current actual Oil production.json has17 mobile results/17 explicit producer-home
policies. Existing structures.json has exactly four producer dock layouts:

|Actual producer|Slots|Parking|Rollout lanes|Declared outputs|
|---|---:|---:|---:|---:|
|company_base|40|12|1|10|
|vehicle_factory|22|12|1|6|
|maintenance_depot|20|10|1|2|
|shipyard|68|32|1|7|

Their existing layout evidence already records migration; actual current
structures SHA256 E63495E70EE54C3F39D4C00E0DBE75EBF003851BD0CB4C71A606ED766BAE9A28
matches that earlier artifact. Production SHA256:
F2141BAAB9F84DD4EE2BA5ED6DDD5EFFF4C190D4ACA4814C4B085EFDA13BB4B9.
Source rechecked original TransformationHomes, strict OutputHome declaration and
TileRollouts authored-lane path; no second home/rollout/placement implementation.
Parent step2 (implementation) is now reconciled checked; complete steps3/4 and the
original P9.051 requirement stay OPEN. Structural JSON counts are not full
runtime qualification or justification for copying content into the framework.

The next command recorded for that sequential milestone has NOW RUN above.
It is retained as history, not the current next command:

~~~powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000 --case-input "fleet:artifacts/marine/04f631436dc04ae79d303dd358be717a/checkpoint.json"
~~~

Keep original221/209 recorded checked/12 open; no original requirement closure.
No full/default/profile/family test, business/platform/Draw edit, engine build
change or staging/commit/push. Operational/assurance mapping below is unchanged;
console diagnostics and native saves are not a protected audit-outage proof.


Milestone hygiene10:42+03:00:13 current Markdown documents/4054 local file links/
0 missing targets, current heading anchor checked. Both git diff --check exit0
with existing line-ending diagnostics; dirty trees and staged0 preserved. All15
logs in this2.675 native namespace record terminal Exited0; no selected process
remains live. Core/proof/source and current native-save hashes rechecked unchanged.
No executable edit, build or automatic runtime rerun follows documentation.

### Native continuation follow-up — 2026-10-20 10:27+03:00

Previous goal turn was implementation progress (exact wide fold and focused
checks). This turn rechecked core/copy/save identities, resumed the ACTUAL2.675
native1451 image and advanced the same ordinary physical workflow. No engine,
domain, content, proof source or binary edits/builds occurred; no earlier passing
framework cases were rerun. All seven following literal stage invocations exit0
with495/0 selected assertions, no aborts. This is not a full suite/profile/default
marine workflow or full-plan qualification.

Same P literal/flags and consumer log namespace as below. Each row resumes its
immediate preceding native image; preparation is never reset and the seven
manufacturing orders are admitted only ONCE. Full original durations, materials,
routes and120000ms internal bounds remain. Raw run.log IDs:

|Input state -> earned time|Passed/failed;worker/outer ms;stage ms|Log id|New native id/hash|
|---|---|---|---|
|harbour:db60...1451 ->2891|35/0;81610/81742;80712|c192c666786a40358e25c140528ad914|21e438fa0ab8429784d04a9ec52c4bdd/7BB9F98358A81ED2|
|harbour:21e4...2891 ->4331|35/0;27872/28002;27094|76b3541feced499ba865ad23f5e6321a|1f766bd725f149809eda64686740458a/87E6004E1CF7EEA4|
|harbour:1f76...4331 ->5771|32/0;29781/29922;28931|8dc062ba9f624f83bbaf6954bf813a39|5fee21ae154f49e5938a33b5b09067ae/D702F5DA3EA5B127|
|harbour:5fee...5771 ->7211|26/0;24093/24211;23314|e40486d555ad4ab2b6d393fbef3dc32b|39338cfeb6e7494fac2c13413776fb6e/528271EC5592B92B|
|harbour:3933...7211 ->8111|23/0;15632/15766;14841|f70b6dff11094e4e93bf19c8dcb2a925|c7bcc15212f64f298ba01a493da0f42f/DF87EA156D6CE938|
|fleet-prepare:c7bc...8111 ->8127|133/0;8778/8896;n/a|9483c855859347cd98c4707bf04c9641|0d89d670b1a042d5bd86125f65cdaa29/4DCED3C9D1ED2C9D|
|fleet:0d89...8127 ->9567|211/0;100046/100163;98497|90694257b79e49e094e06ed8fb585af0|7a37f34a325542738ee20d317db9ad4a/34FA986292652D79|

Current2.675 HARBOUR IS COMPLETE at8111: actual shipyard/depot/grid, no remaining
construction orders/jobs. At9567 the original survey-vessel recipe has earned
1268/6000 productive minutes and all seven original orders remain queued; there
is NO manufactured vessel or complete fleet. Actual trucks unload finite cargo
through native service phases; no host gameplay, stock grants or instant work.

Fleet day98497ms passes120000ms and is9.54% below historical2.673108888ms at
EXACT same9567/hash34FA986292652D79 (MS1 log3905a077ab254815a02ce03836c390ca).
This is a local combined2.673->2.675 engine comparison, not an isolated wide-fold
benchmark:2.674 had no fleet-day measurement. Do not compare to2.671's DIFFERENT
31D2C58FE3414413 hash or infer universal performance/pad success. All current
harbour stage bounds pass; original offshore-pad wall failures remain OPEN.

Earlier follow-up consumer artifacts/marine/7a37f34a325542738ee20d317db9ad4a/checkpoint.json
and sibling game.sav:9567/hash34FA986292652D79,
fleet_progress/next fleet_execution. Preserve THIS progress; previous2.675 images
remain compatible earlier stages, not the current pointer. Old2.674/2.673 images
remain historical/build-incompatible and must never be header-rewritten.
All seven workers are confirmed terminal; no live selected handle remains.

SHA256 current checkpoint.json:
142050E1C820B9C29C948CAB07225CF0B64BB856D120583064F0F6FF8E9FCD10
Current game.sav:
8110E870D47306D7289A030DC24ACB36B8E97D419FFCF984E3B510A37C9A2F35
Completed harbour checkpoint.json/game.sav:
5048594763856F10D1A8B5627565FC2C2BE730051A503E40B837F314B00777C2 /
83BE2F0C98E5EDFBE89D9989433D4C7461AC9B3AC8D8CE859CA96334C75557DC.
Core/test/consumer/source hashes in the implementation record below are unchanged.

Fresh structural mapping normalizes wrapped primary checklist text and matches
all221 original IDs once:209 recorded checked/12 open/0 unmatched. An initial
single-line-only query missed wrapped records and is NOT a valid mapping. The
corrected capture/normalize query found all221;209=221-12, no checkbox changes.
This does not certify every recorded historical runtime on current build.
Full P5.005/.008, P9.051, current fleet/sea, pad performance, broader provider/
allocation/stress/CI/optional-module gates and the whole goal remain OPEN.

The next command recorded for that earlier follow-up has NOW RUN in the sequential
milestone above. It is retained as history, not the current next command:

~~~powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000 --case-input "fleet:artifacts/marine/7a37f34a325542738ee20d317db9ad4a/checkpoint.json"
~~~

Do not rebuild/reset/re-admit simply to continue this compatible image. No full
test run, lowered work/time, engine-source change, business/platform/Draw edit,
staging/commit/push or primary requirement closure this follow-up.

Follow-up hygiene10:29+03:00: twelve current Markdown files/4033 local file-link
occurrences/0 missing targets; current report anchor checked. Both repositories'
git diff --check exit0 with existing line-ending diagnostics. Framework3010 and
consumer101 dirty entries/staged0 preserved. All ten logs in this2.675 native
namespace (three earlier observations plus seven new stages) record terminal
Exited0; no automatic runtime rerun follows documentation. Core/consumer DLL,
unchanged OffshoreTests source and current native save hashes rechecked above.

### Measured owner and implementation

Actual2.674 traffic worker34708 was sampled for3s with the existing ignored
dotnet-trace tool; no package installed. Profiler/worker terminal exit0.
Consumer artifacts/marine-route2674-cost-observations/bfa764dab57d4cae82216dc80a1a50c3/run.log
records115/0,6206ms worker/6420ms outer,1451->1499, NO new save.
Raw framework artifacts/route-cost2674-profiles/1b3742f390e248f8881f2accd3499f22/
contains collect.log, route.nettrace and route.speedscope.json.

Balanced evented stack:3037.9074ms/17658events/526frames. Filtering to actual
Kernel.World.Step intervals gives1701.7661ms, excluding host snapshot/hash reads.
Nearest-real exclusive StateHasher.Bytes35.22%; inclusive TileMap.Sync53.09%,
ReplanDockBindings39.52%, PathSearch.Find23.21%, Mover.CheckRoute20.58%,
local ReadPath4.08%. Inclusive values overlap: do NOT add them or call this a
complete CPU/allocation distribution. DockSlots returns actual authored metadata,
not a copying list factory. All WorkDocks guards remain unchanged.

Sole StateHasher now freshly checks EVERY byte in an exact256-byte uniform
input before applying its identical canonical FNV transform. Nonuniform inputs
and other lengths retain the original8/32-byte/scalar loop. Separate no-inline
probe checks first/last words for early rejection, but still checks all interior
bytes before success. No world size/content/platform limitation is introduced.

For h=256q+r, F_b^256(h)=h*P^256+C_b(r) modulo2^64;
P^256=13743314361298519041. Wider constants are composed from COMPLETE32-byte
constants in a SECOND initialization pass, never partially initialized low states.
The same mathematical table gains65536ulongs/512KiB (total1.5MiB), not session
data/cached fingerprints. Zero uses the exact multiplication-only transform.
Length prefixes, following state, full provider/map/revision/clock/first-fault
checks and native save layouts remain. No new owner/codec/cache/clock.

EngineInfo2.675.0/save160/replay1; all other native schemas unchanged.
No consumer source/content/gameplay/observer/business changes. Only its binaries
were rebuilt against the framework. Old native headers were never rewritten.

### Every focused attempt

W: State hashing: complete wide uniform folds preserve every byte and owner.
B: State hashing: exact uniform byte blocks tails mixed streams and concurrent owners.
G: Shared spatial observation: original codecs and revisions guard physical native and point callbacks.

Framework command for each literal:
~~~powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "<literal>" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 20000
~~~

Logs: framework artifacts/wide-byte-fold-observations/<id>/run.log.

|Attempt|Passed/failed;worker/outer ms|Log id|Interpretation|
|---|---|---|---|
|W2.674 baseline|301331/0;5378/5578|3adee66c35ba41cf9381206ea303cbd0|Semantic oracle already passes; optimization baseline, not a fabricated functional failure|
|W candidate1|301331/0;5363/5610|d5889f46b2bb4516b38f70a39586e1a2|Eight-byte probe, mixed timing regressions; discarded|
|W candidate2|301331/0;5326/5590|7f41d029c3d8451e8ac4cab0c3aa0486|32-byte probe groups; near-uniform/mixed regressions; discarded|
|W overlapping final build|301331/0;5480/5745|221cb066e6214bcab21efd9f38bd4009|NOT final identity/timing proof; build copy locks|
|W final AFTER terminal build/hash match|301331/0;5364/5546|b20b9d7980944318862ae366c1602dca|Separate exact-width helper|
|B final|299537/0;4233/4403|066ccd05c3414eccbe721ddbc34954fc|Original scalar/mixed/tail/concurrent case unchanged|
|G final|776/0;9968/10154|64efd269ae564c1f96160b2703766f84|Original physical native/point saved work and layer/revision/throw guards unchanged|

Three final literal scopes601644/0, NOT full regression; most assertions are
byte-oracle vectors, not hundreds of thousands of separate game scenarios.
W covers all256 byte values/all256 low accumulator states, every bit mutation on
four backgrounds,255/257/511/512/513/1027 boundaries, normal/observation equality,
subsequent scalar state, unchanged inputs and concurrent independent owners.
No assertion/bound/callback or scenario duration was weakened.

W warmed16MiB observations (ms), baseline->final:
zero1.688->1.312; uniform terrain1.819->1.421; last-byte mismatch2.731->2.860;
eight-byte runs6.604->15.763; noise18.037->18.267.
Do not hide mixed regressions or claim a universal microbenchmark win.
Short-run scalar control also increased16.734->24.070ms, but that does NOT
prove all engine increase is noise. Final B's4096-byte mixed8.808ms still uses
the unchanged generic path. Each W allocation observation40bytes is measured
Stopwatch construction, unchanged, not Update/static-table allocation savings.
Actual native-stage evidence below justifies retaining this candidate.

Targeted test-project Release builds (--no-restore -v quiet):
7.05s baseline,9.59s first candidate,13.97s table second-pass correction,
11.69s unrolled candidate,29.96s final helper. All exit0/zero errors.
First four have0 warnings. Final has TEN MSB3026 copy-retry warnings caused by
the overlapping W observer holding output DLLs; it then completes. This was an
orchestration mistake, not a clean compile or final measurement. No live timed-out
worker was restarted. Final core/copies were matched AFTER terminal build before
final W/B/G. Consumer targeted PublicProof build2.21s/exit0/0warnings/0errors.
No full solution/default/profile/family test.

### Native stage and current continuation

Original consumer literal P: Offshore: vessels survey, drill and ship from the sea.
Each selected input uses --bounded-exact P --timeout-ms 150000
--termination-timeout-ms 5000 --output-chars 100000, as in the runnable command
below. Original120000ms stage bound/full1440 game minutes
unchanged. Logs: consumer artifacts/marine-wide2675-observations/<id>/run.log.

|Input|Passed/failed;worker/outer ms;stage ms|Log id|Native state|
|---|---|---|---|
|prepare|37/0;5668/5800;n/a|8a78b4123c3e45c0a398063d5b166738|9673fac5151a4469b5651076b6831436 at11/A758028A5E3E7B7D|
|harbour:967...|35/0;51881/52018;51036|f4ec9b165cba4df7b89bda834e5785f7|db6057324e2b41598142435207792bb7 at1451/2EC4336CCADCC849|
|traffic-trace:db60...|115/0;5114/5240;n/a|cd82642ad4444fcea0be89f04c9b6bdd|1451->1499;NO new save|

First harbour day51036ms is11.6% below recorded2.67457732ms and3.0% below
historical2.67352590ms at EXACT same1451 game-state hash. This is a local
before/after observation, not statistical/platform-wide or fleet/pad completion.
No domain time/requirements/material/fuel/route/placement/guard was changed.

Matched48-Update trace:2.674 min/mean/max30.095/59.3317/468.344ms versus
2.67530.410/55.1597/382.780ms. Allocation mean5411115.83->5411189.17bytes,
essentially unchanged, NOT an allocation improvement. Attached sampler on the
2.674 trace prevents claiming a clean controlled latency comparison. Current
actual cargo moves/unloads/returns; transient1499 is not the saved pointer.

Earlier first-day consumer artifacts/marine/db6057324e2b41598142435207792bb7/checkpoint.json
and sibling game.sav: time1451/hash2EC4336CCADCC849,
harbour_progress/next harbour_execution. Shipyard clearing finished but physical
construction awaits stock; depot379/480/grid pending. NOT a complete current
harbour/fleet/marine proof. All selected test/native processes are terminal.

SHA256:

- production core and BOTH test/consumer copies:
  B1AECCD97678BF35FAFC2BD285888AE41567D067D5DAE38D1FB3B9269F38D880
- final framework test DLL:
  3D63A84A6F3BF8F58F8CB6656F2DADDC06B3163EBDB608A5CDCF43E72EFDD92F
- consumer PublicProof DLL:
  2D82876E4C784C0B83CB38C4FE0AA8923CA3E0933A6CC1471373565E05197C58
- consumer OilCo.Game DLL, relinked/no source edits:
  8A709630FFFF837266F01B183974D7792489CABAC123507EFCD6779808927066
- unchanged OffshoreTests.cs:
  EB9A7FCACD1BBDA996577BB30244DB1DBCB6A85368FCAD07EEC102FB4330956A
- StateSync.cs:
  41F0D29AC680B903134ECDDFCA4CC3F28F64E478F2B8359DB9A4D50F9434211C
- SaveGame.cs:
  DC55FC57F39098F3DD9F99D1F95B9363AB657A5001D1E18BDCF042C28906126B
- StateHasherGoldenTests.cs:
  12AA8BDA5AF6D122F6AB24E7BC6243E8AE75DD0614E6E8B04DC35FF3945CE61A
- earlier first-day checkpoint.json:
  D2F9CCA46E0EF34C019ACAD5179D441FA847E2C8FB05E81F7D433705552A48A2
- earlier first-day game.sav:
  D993DE9EA862B3210C6E1712CF874616D6A67841ED7DB7DE547394216A449EAD

The following was the next command at the first-day measurement checkpoint;
it has NOW RUN in the follow-up above and is NOT the current next action.
Use the current9567 fleet continuation above instead. Old2.674/2.673 saves
remain strict-build incompatible; never rewrite headers.

~~~powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000 --case-input "harbour:artifacts/marine/db6057324e2b41598142435207792bb7/checkpoint.json"
~~~

No blind preparation reset/full workflow/raised bounds. Current full harbour is
subsequently proven above; fleet/sea, original pad120s performance, arbitrary-provider/private CLR purity,
wider allocation/stress/CI and full plan remain OPEN. Original221/209 recorded
checked/12 open is historical structural bookkeeping, not fresh full-runtime
qualification. Next action must advance the real plan, not just repeat W vectors.

Existing [operational and assurance map](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
owns original first faults/notifications/commands and broader protected-audit/
outage gaps. Console logs are not a protected durable audit. No new logger,
swallowed error, UI/Draw/platform work or parallel owner. No staging/commit/push
until the full requested goal AND plan finish.


## Current2.674 native committed-route revalidation — 2026-10-20

SELF-REVIEW; Windows/PowerShell/.NET SDK10.0.401/+03:00. User-installed sdlc-orch
snapshot fdc9dceb0bae90a05c1575cb033849daee0e6744; no local framework copy.
Framework master cb61483ab81404c11944931aedb4703ba694edb5 and consumer master
8be1eed8b54649ac35cb1a2fe01051dd59d1982e remain uncommitted dirty work.
Previous goal turn was progress: actual2.673 functional marine completion plus
proof correction. This turn implements the next existing P3 engine slice; it does
not redefine the full objective around the new focused test or an easier workload.

### Implementation and guard ownership

Production changes stay in the existing Mover, MotionAreaContracts and
MotionArrivalContracts; EngineInfo advances to2.674.0. Save160/replay1/mover4 remain,
with no extra route, estimate, cache, clock, codec, scheduler or work store.

Mover.CheckRoute reuses its own native Path/PathIndex only when Moving, the path
is planned/nonempty, the actual fractional origin matches exactly and the
requested goal equals its current Goal. It re-reads the current guarded structural
origin, every remaining segment and diagonal corner, and requested-region sweeps.
Distance and duration are freshly calculated at the supplied current rate. A
different hypothetical origin/goal or invalid suffix retains original bounded
prospective search. WorkDocks retains complete joint source/service/departure,
claim, queue, policy, fuel and ranking admission; no system bypass or success cache.

One local ReadPath helper handles the same structural segment/weight calculation
for both already-committed and newly searched paths. A retained arithmetic overflow
falls through to complete search before publishing unknown ETA; it is not a partial
feasibility success. Prospective area search now starts its first sweep at the
actual fractional origin, not a guessed tile centre. Entering the final goal tile
fractionally still has positive remaining travel until physical centre arrival.

The first new cost regression fails58/1 on2.673: ten remaining segments use58
guarded entries, above the declared31 bound. It uses the SAME existing authored
independent tile world/provider and native mover; its positive flow issues normal
MoveVehicleCommand, drives the one clock and observes real state. Initial
implementation passes59/0 with17 guarded reads. No path/position/cargo/work injection.

New late callback proof then fails90/3 on the intermediate implementation: a second
area sweep can mutate a layer before the next grid observation without selected
space in the outer route interval. The existing motion observation boundary now
includes world.Space for route, area and remaining-arrival provider reads. Original
EntityObservation owns full state/revision/clock checks and first faults. Final93/0
rejects late throw/clock/layer mutations, blocks healthy saving/advancement and
keeps another session isolated. No guard was weakened to reduce navigation reads.

### Focused proof and adjacent fixture repairs

New CommittedRouteValidationTests registers two explicit literal cases only:

- N: Native committed route: guarded suffix validation, physical saved twins and replay.
- R: Native committed route: changed geometry, hypothetical origins and late provider faults.

N final62/0 retains the original58-vs31 cost assertion and actual physical saved
twins/replay; three later assertions additionally exercise fractional position
inside the final goal tile. R final93/0 checks same-tile/different-fractional origin,
different goals/rates, zero budget, changed segment/corner/origin/goal, alternative
region paths, fractional departure refusal and six late area/grid callback faults.
Only isolated invalidation/fault contexts mutate terrain; positive movement is
ordinary engine input. Tests own no gameplay path or completion rules.

Four directly affected existing literals were selected separately:

- V: Navigation selection: private controls, retained travel and saved physical work.
- A: Route areas: provider contracts, admission and private diagnostics.
- E: Motion execution: timed travel, retained cargo and saved continuation.
- T: Motion arrival: physical route timing, gates and saved continuation.

V first stops2/1 at composition: its duplicated factory omitted the actual
native-route-conditions document/module required by layered navigation. Its
factory now reuses the existing LayeredNavigationTests factory, preserving its
explicit initial survey.route/two allowed profiles and caller edits. That single
existing factory owns module/master/conditions composition; no engine default.

E first stops0/1 at composition: its retry-only replacement erased authored
work.staffed/work.timed declarations. Its existing factory now explicitly retains
the existing work-retry JSON declarations and replaces only work_retry_profiles
with the unchanged requested haul-retry JSON. Next E fails12/5: direct unowned
PointMotion.GoTo is stopped by current worker ownership. The driver now uses
ordinary SetHomeCommand and MoveVehicleCommand. Original0.5/1.5 rate, waiting,
cargo receipt/conservation and save assertions remain. Point home arrival is
asserted at the engine-selected interaction approach9 to authored destination10,
not a direct unowned coordinate10. No production lifecycle rule is changed or
waived. Final E55/0 and V68/0 preserve all prior failures in the table.

The latest six exact scopes total473/0 assertions: N62 + R93 + V68 + A139 + E55 +
T56. They share the same final production core, NOT necessarily one final test
DLL: unrelated fixture and N final-goal assertions evolved after earlier passing
unchanged case bodies. Do not rerun unaffected passing scopes merely for docs.
These are six focused cases, not473 independent scenarios or a full regression.

### All framework attempts and builds

Framework log root artifacts/native-route-revalidation/<id>/run.log. Each actual
command from framework root:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "<literal above>" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 16000
```

|Case/stage|Assertions|Worker/outer ms; exit|Log directory|
|---|---|---|---|
|N baseline2.673|58/1|299/462;1|ed65d432d55b45f7acedbd9875c8a321|
|N first implementation|59/0|245/396;0|4f1f0fc20a574c5c9637fb9f621c3b3d|
|R exposes late layer mutation|90/3|376/609;1|c61c78a1319c41ed868f7f28983b81e6|
|R after selected-space guard|93/0|256/410;0|45ac393a76114f4fb07d6586c72ecb4f|
|N after guard|59/0|257/415;0|613681c117e54453ba41a17202168b0e|
|V missing condition composition|2/1|87/237;1|66cf28842cb44e19b9af75a7e0c202f0|
|A|139/0|764/920;0|b7ad1b2e0e8f4d9db9df87569fa32ef8|
|E missing executor composition|0/1|86/238;1|af298c9f434e4dc987c6d037279d87f4|
|V repaired shared factory|68/0|2183/2373;0|79524683edf3438c940480297766769e|
|E unowned direct-motion driver|12/5|465/650;1|eea0f51ffbdd4781b84d1a01c2a89413|
|E ordinary owned commands|55/0|1956/2409;0|1ad8ad0bd699439bb235d122262381cc|
|N plus final fractional-goal proof|62/0|313/503;0|f5f5abc668d941a5a7c12186210738dc|
|T actual rates/fuel/native arrival|56/0|1018/1200;0|3d29e0b53f8941bdb28f6b7b77fc3218|

Thirteen explicit invocations753 passing/11 failing assertions,8 exit0/5 exit1,
zero aborts or observation-timeout restarts. Latest affected scopes are green;
this all-attempt total is intentionally not relabelled green.

Actual targeted build commands, from framework and consumer roots respectively:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v quiet
```

Seven targeted framework test-project Release --no-restore builds all exit0,
zero warnings/errors:13.11s baseline,14.83s first implementation,16.73s matrix/
fractional sweep,8.79s selected-space guard,9.24s fixture composition,9.32s owned
commands,10.20s added final-goal assertions. No full solution build or suite.
Consumer PublicProof targeted Release build3.46s exit0/zero warnings/errors,
with NO consumer source/content edit. Recompilation changes its linked binary
identities; do not call the domain DLL unchanged merely because source is unchanged.

### Actual current native workload — measured, not faster

Consumer log root artifacts/marine-route2674-observations/<id>/run.log. Original
literal Offshore: vessels survey, drill and ship from the sea, separately bounded:
--timeout-ms 150000 / --termination-timeout-ms 5000 / --output-chars 22000 with explicit
--case-input prepare, then harbour:<checkpoint>. Original internal120000ms,
one-game-day/game-duration/resource/geometry limits are unchanged.

|Actual input|Assertions; worker/outer ms; stage ms; exit|Log directory|Native output/hash|
|---|---|---|---|
|prepare, actual time11|37/0;6718/6912;n/a;0|34676de66dfe41438d0afc8e02e16e54|87c8318bc40a4fdcb9d6696cb486e4e3/A758028A5E3E7B7D|
|harbour:87c...11->1451|35/0;58648/58799;57732;0|fff42387958b4635b80bbc5d70a4e546|316bc8899b66441ea35d1710762d2826/2EC4336CCADCC849|
|traffic-trace:316...1451->1499|115/0;6855/7032;n/a;0|61e817cc15eb47e3a78929c43dc60eb2|diagnostic only, NO new checkpoint|

TRACE uses output-chars100000 solely to retain all49 vehicle observations; it
still executes exactly48 normal Updates and saves nothing. All three actual
parent/worker handles were observed terminal before their successor. No aborted
poll was mistaken for completion, no stale header was rewritten, no old full
marine workflow/default/profile/family suite was repeated.

The current day PASSES the unchanged120s wall bound, but it is NOT a speedup:
57732ms versus the recorded2.67352590ms stage in MS1 (logc5ea31...,
native5106...); both end at EXACT1451/hash2EC4336CCADCC849. Observed elapsed is
5142ms higher. This is an uncontrolled local observation, not a statistical
regression or universal benchmark. The query read-count reduction does NOT prove
the whole guarded workload faster or close either historical pad wall failure.

Current TRACE reads actual normal cargo handling/travel, no frozen/teleported
worker: truck6 unloads real15.333 stock, truck5 later unloads9.333. Its48 Updates
span36.385..526.128ms/mean71.3516ms, allocate4503352..10066768 bytes/
mean5411090.67 bytes. Different phase from the old59429 pad trace; do NOT compare
those means as controlled improvement. Whole-day elapsed and allocations are
still material; profiling the current busy route/joint future-leg work is next,
not another convenient hasher tweak, larger timeout or weaker purity checks.

### Current identities and native handoff

Strict engine2.674.0/save160/replay1, native mover codec4 unchanged.
Final production core/copied consumer core:
92F26D4D8298C353CB59E57426A9AE4CB565B6BC6A58494BC66883CCDCBB26A6.
Final framework test DLL:
7E48377D555B6790F84A95DD64B1384C1468FF438934FD5F4DA1C1869061721B.
Movement.cs466C129D9A76C2AA91D56AD3B492AE70EB86D9F2C7CD5E421328C84E9C824241;
MotionAreas.csAD61F7874F25B9CEB286B46E2FDDF71AF6C819A25519DB3CC43389DBB676220B;
MotionArrival.cs9129DFBA1490DDFE053C63B3EB25786CB8F9BB263974C0FA5CCFA05CAB9C5FF8;
SaveGame.csC590E4DC511B68752229562C66B3EEE715420BB6C10733400320A5D5CAB151A7;
current-engine-protocol.json395453A6664E56AD57BBF3F40DD1C6B2D5A725A744A4DAB386EF68E57EF0359D.
Consumer proof4C0A3C2189B92489F0AA0674CE74396E74772CB341ED98FE64588DF47D4EA208;
domain6137C648CB29660A41AA65BB6E6618AF223D31DEE85F374884A7C2115C561380;
its OffshoreTests.cs remainsEB9A7FCACD1BBDA996577BB30244DB1DBCB6A85368FCAD07EEC102FB4330956A.
Earlier intermediate binary hashes not captured are not invented. No production
edit occurs after the final8.79s guard build; later builds change test composition/
assertions only. Source/fixture identities identify scope, not full qualification.

CURRENT compatible consumer artifacts/marine/316bc8899b66441ea35d1710762d2826/checkpoint.json,
time1451/hash2EC4336CCADCC849,harbour_progress/next harbour_execution.
JSON SHA256D2F9CCA46E0EF34C019ACAD5179D441FA847E2C8FB05E81F7D433705552A48A2;
game.sav6E6175242E1BA15004DB74E1CDF153ED96F7A53EF013E3634E3CDB0F052B71F6.
The1499 diagnostic state was NOT saved; never use it as the continuation pointer.
Actual shipyard clearing16h is earned, depot379/480 and grid still pending; this
is NOT completed current harbour, fleet, sea workflow or general performance.

Historical2.673 native1fd4de.../79833/full functional marine result is preserved,
but is incompatible with current strict build; never load or rewrite its header.
[Historical full functional scope and every earlier attempt](P5_MARINE_WORKFLOW_STAGING_VERIFICATION.md#current2673-outcome-replacement--2026-10-20)
remain evidence for that source, not current-build acceptance. No business/domain
gameplay, host/demo rules, platform/Draw, dependencies, compatibility shim or new
state owner was added.

Next productive engine task: profile current guarded route evaluation and separate
remaining committed leg from hypothetical source/service/departure legs in the
existing owners. Then change only the demonstrated cost, keep all original live
checks, and measure the same workload. Current first-day timing does NOT justify
blind long-day retries or claiming the pad performance requirement done. Existing
P3 plan items1–4 implemented;5–6 remain PARTIAL/open for broader proof/performance.

Optional reproducible diagnostic command from actual consumer root, already run
once above (not an instruction to auto-repeat):

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 150000 --termination-timeout-ms 5000 --output-chars 100000 --case-input "traffic-trace:artifacts/marine/316bc8899b66441ea35d1710762d2826/checkpoint.json"
```

### Operational assurance and remaining gates

Existing [operational/assurance mapping](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
still owns service inventory. Actual provider faults keep original World first
fault, truthful refusal, no healthy save/advancement and session isolation; normal
events/views/replay/clock remain original owners. Console proof output is not
protected audit. No new logging/audit/channel package or UI service; headless UI/
Draw is not applicable. Broader audit/delivery outages, private CLR purity,
corruption, arbitrary providers, joint logistics, stress, CI and full regression
remain unverified. No claim follows from source inspection or one entry-count test.

Original221 requirements remain209 recorded checked/12 open; no primary closure
or whole-goal claim. Full goal ACTIVE. No staging/commit/push before WHOLE goal AND
plan; no user authorization for full/profile/family/default test runs is inferred.

Final hygiene/self-review2026-10-20T09:52+03:00: twelve edited current Markdown
files,4033 local file-link occurrences/0 missing targets; current report and plan
heading anchors checked separately, not a global anchor audit. First ad-hoc anchor
check used incorrect PowerShell positional binding and is invalid; corrected
LiteralPath/Pattern check is the actual result. Fresh original-ID/text mapping:
221 unique/209 recorded checked/12 open/0 unmatched, not global runtime proof.
Both original master HEADs/staged0 remain; framework3010/consumer101 dirty entries
are preserved. git diff --check exit0 both with35/8 existing line-ending diagnostics.
Eleven edited engine/test/protocol files have0 trailing-whitespace lines; key
production hashes above match. Final new proof source is
DC8D7EF3B59D57F237A97022B0EED186C5F1277E3B276D9F3881808FB9E16DC5.
No selected framework/consumer worker remains live. All13 framework/3 consumer
attempts are terminal; no executable edits or automatic passing-test reruns follow
final focused validation. Whole goal and broader performance/provider gates OPEN.

## Historical2.579 observations — 2026-10-14

Engine2.579.0/save157/replay1. Original scalar FNV bytes and native payload schemas unchanged. Same-object selected-space/provider reads reuse the original SpatialScope codec/revision, retaining target facts and target-codec checks. No cache, ignored callback or alternate hash/state owner.

## Diagnosis and attempts

One additional old-engine layered case timed out30117ms (PID32416). A successful dotnet-stack report on that explicitly matched live worker showed StateHasher.Add → IntLayer.SyncValues → TileMap.Sync → EntityObservation.SpatialScope → PlayerView.CanSeeContent → SpatialLayerAccess.CopyObservationKeys → TileMap.RefreshVisibility → World.Step. This identifies sampled CPU work, not a complete statistical profile.

An initial targeted build and premature test launch overlapped, causing MSB3026/MSB3027 file-lock failure (22.72s). That test ran old binaries and timed out30052ms (PID16996); it is not evidence for2.579.0. Both processes became terminal, exact lock-owner IDs were checked absent, and the clean retry2.41s completed before a new test. No unrelated process was terminated.

After unrolled canonical scalar hashing and removal of same-space duplicate capture, native layered work48/0 finished29624ms (PID26280, observer29970ms). Adding the exact zero-scalar multiplication and golden vectors preceded clean targeted9.04s build. The original aggregate predicate case still exceeded30s (PID30064/observer30050ms); it contained three full physical/replay flows plus authoring/fault matrices. Its bodies were preserved and registered as five separate stages. Final targeted compile6.56s, zero warnings/errors; no executable edits after final cases.

## Final focused results

Each exact case used30s timeout,5s termination budget and9000 output characters; no truncation or observer errors. No profile/family/full run.

| Literal case | Checks | Case ms | PID | Observer ms |
| --- | ---: | ---: | ---: | ---: |
| State hashing: canonical eight-byte FNV golden vectors | 30/0 | 2 | 21200 | 183 |
| Core view observations: guarded callbacks, private facts and atomic fault publication | 179/0 | 12648 | 18888 | 12829 |
| Independent tile world: native layered routing, physical movement and public commands | 82/0 | 314 | 22996 | 496 |
| Layered navigation: authored roads, depth gates and physical saved work | 48/0 | 22056 | 38636 | 22313 |
| Tile condition contracts: native physical construction stage | 18/0 | 13392 | 26088 | 13546 |
| Tile condition contracts: independent band physical construction stage | 21/0 | 15634 | 14072 | 15841 |
| Tile condition contracts: independent columns physical construction stage | 20/0 | 15119 | 36564 | 15299 |
| Tile condition contracts: invalid authored metadata stage | 34/0 | 226 | 31632 | 383 |
| Tile condition contracts: callback fault stage | 48/0 | 499 | 34184 | 659 |

Total480 successful checks across nine selected cases. The first four ran before the final test-only registration/profile split; production source was unchanged. All five replacement stages ran after the final compile. No shortened physical duration, resources, material bill, travel, productive work or replay was used to obtain these results. Extra four protocol checks result from repeating the same two checks across three separately bounded physical stages; no original assertions were removed.

## Verified scope

Golden vectors independently specify signed extrema, mixed bytes, zero, accumulated prefixes and int sign-extension; input refs remain unchanged. Core observation tests retain private/fault/atomic-publication checks. Independent movement uses normal commands and native travel. Layered work reaches actual rerouted arrival at clock59 and subsequent productive completion, including twin saves and earned knowledge. Three construction variants physically haul, consume exact authored30 materials, complete and reproduce replay from public commands. Invalid metadata and eight route/construction callback-fault combinations retain refusal and terminal-session semantics.

## Remaining work

No full regression or all-provider/performance proof. Broad original P3/P11/goal gates remain open; A47 metadata report remains historical. All handles terminal, no staged files or Git commit/push. No Oil/Tycoon/platform/host changes.
