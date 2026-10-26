# P7-D7 topology redistribution/accounting revalidation

## Current M136: native flow observation and physical contention — 2026-10-23

Native **2.767.0/save161/replay2**, master base
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work. The complete
M1352233-input freeze matched before work. M135 fixed scoped endpoint lookup purity;
this continuation checks its remaining native network/spatial callers, not unrelated
tests or another resource owner. Original P7.acceptance.017 remains open.

### Reproduced native defect and actual game-update boundary

The existing native physical contention literal fails **11/1** on2.766 at update15:
`resource.endpoint.resolve`, invalid network elapsed flow cursor. The new lookup
observation calls the original network codec while Flow has already earned the
end of the current interval but the sole GameClock has not yet committed the step.
The prior settled-checkpoint timestamp check incorrectly rejected this legitimate
intermediate state. No resource, speed, capacity, geometry or test bound caused it.

The ORIGINAL NetworkService now owns a transient SimulationOperation scope around
Flow. It is not persisted, scheduled separately or used as an elapsed-time counter.
FlowTimeLimit derives one observation bound from the SAME clock: at most the end
of the actual active flow/game Update. The bound also covers the subsequent event
and entity phases before GameClock.CompleteStep. Only observation fingerprints may
see that interval; normal writes/hashes/loads retain the settled timestamp checks.
An interrupted flow cannot checkpoint. The actual insertion-order cursor list may
be fingerprinted during Flow before its existing final canonical sort; settled
ordering, quantum, resource, endpoint, elapsed-time and service-history checks stay.
The flow-active marker itself is included in the existing observation codec.
Recursive Flow refuses instead of earning another interval. No second clock, saved
phase, flow balance, endpoint registry, ledger, scheduler or compatibility path.

First correction restores native contention555/0. A new **read-only event** case
then reproduces the distinct post-flow observation defect1/1: successful-service
history is already at the earned interval end during event dispatch, after Flow's
scope closes but before the same game Update ends. The final single clock-derived
limit handles both phases. Its initial6/2 attempt had not selected the declared
probe module in the scenario; that fixture failure is preserved, not native evidence.

### Existing proof owners and strict negative checks

NetworkRateTests extends its existing factory with an optional observer and reuses
the existing generic ProbeModule. Its empty-document JSON master declaration AND
scenario module list explicitly select the listener. The listener only reads actual
committed ResourceFlowFact events, original CopyFlow progress and the clock. Its
Apply-like behavior performs no stock, work, clock or gameplay writes. The existing
rate fixture still owns authoring/negative setup; this is explicitly engine-event/
protocol instrumentation, not a new ordinary-player physical acceptance driver.
Minute and half-minute sessions observe real transfers and save canonically.

The affected older Contracts case also had stale expectations: schema4 versus the
already-existing schema6, and reuse of a session after a terminal failed checkpoint.
Its13/4 result is retained. Each of the ORIGINAL elapsed/quantum/duplicate corruptions
now starts from a fresh saved branch. Additional future-cursor/future-service-history
branches prove the normal settled boundary remains strict. Every failure must be
CorruptStateException at the original checkpoint.encode owner, stop time, reject
healthy hashing and retain its first fault, leaving the healthy primary untouched.
Its later67/1 result exposes an old sim.lines phase expectation; the final exact
expectation requires the actual observation.provider phase AND scoped-mutation cause,
not merely any failure. No invalid assertion or malformed-content branch is removed.

### Actual focused qualification and image distinction

Six clean test-project Release builds:7.55/5.52/5.54/7.32/5.25/5.07seconds, no warnings
or errors. Twelve actual literal runtime attempts are retained. No full/profile/
family/soak executor, consumer runtime or raised limit. Each exact name below uses
the Release/net8.0 tests DLL with `--bounded-exact`,60000ms timeout/5000ms termination/
50000output characters. Build command:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.

| Exact literal | Checks | Suite/observer ms | PID/exit | Test build |
| --- | ---: | ---: | --- | --- |
| Network flow observation: committed transfer events expose earned end-of-update progress without advancing the clock |2904/0|2710/2839|3184/0|4|
| Network rates: authored native periods, contracts and private progress |68/0|1031/1160|22340/0|6|
| Network rates: slow physical legs, blocked intervals and saved continuation |45/0|3889/4019|11024/0|4|
| Topology contention: repeated competing cuts and physical reconnect preserve native claims stock and allocation budgets |555/0|3916/4058|10280/0|4|
| Topology contention: half-minute independent ports preserve competing cuts physical reconnect and allocation budgets |849/0|5156/5295|19372/0|4|
| Spill disposal recovery: native interrupted cargo completes physical timed disposal |482/0|5470/5610|25048/0|4|

**4903/0**, six selected final scopes on one unchanged native2.767 image. This is
**not one unchanged test assembly**: build5/6 edit only NetworkRateTests.Contracts;
native450 sources, DLL/PDB and proof inputs match build4 exactly. Only that repaired
literal reruns after those test-only edits; no unrelated green reruns for counts.

Actual physical topology continuations retain original work/material/tool owners:
four successful cuts/eight typed losing cuts, physical reconstructions, saved twins,
and six real setup/complete-round replay verifications. Native rounds87→137/137→203
reach **414C63766F3C4F2B /2DA5E05CEED3ADC8**,7/8commands and50/66checkpoints.
Independent half-minute rounds160→251/251→373 reach
**0A801D4F72C84213 /FF8D9E051AE476FD**,7/8commands and91/122checkpoints.
Per-Update managed allocation remains below ORIGINAL33554432peak/1073741824total:
native116Updates peak2290696/total64129952; independent213Updates peak2285976/
total101862352. Costs increased versus earlier records; this is budget qualification,
not a performance-improvement, all-thread/native-memory or whole-engine guarantee.

The spill literal actually propagates finite oil, supplies response work, recovers,
loads/interrupts/saves/returns cargo, unloads over time and earns timed disposal.
Its additional complete replay0→213/17commands/213checkpoints verifies
**B247BEA74DE3E140**, matching the actual live state. These seven verifier calls are
distinct from ordinary Save/Load assertions and the non-recorded event probes.

### Census, maintained records and remaining scope

Fresh audit matches450native source/PDB checksums/138typed calls/14primitive and
8layer writes/zero unknown, unmapped or indirect. No resource-call family or semantic
owner changes from M135. DLL
`04D2B0D16DC1E9883F9A9DDF1BB2245A05F5451A84DCDE3C1B8533405738F575`, PDB
`2BB4234CF79F1B2010CB7DF21D25A95AE462061E5B54850CBFFDDB53B8908B14`.
Metadata-only profile1099registered/1079included/20excluded executes zero bodies;
removing only the new60000ms row restores exact prior bytes.

[Before](../../artifacts/optional-module-durability/b181b6a29d134a3c8f06824f8e678032/before.json),
[all attempts](../../artifacts/optional-module-durability/b181b6a29d134a3c8f06824f8e678032/attempts.json),
[build4 qualification](../../artifacts/optional-module-durability/b181b6a29d134a3c8f06824f8e678032/tested-build-4-inputs.json),
[final build6 inputs](../../artifacts/optional-module-durability/b181b6a29d134a3c8f06824f8e678032/tested-build-6-inputs.json),
[native image preservation](../../artifacts/optional-module-durability/b181b6a29d134a3c8f06824f8e678032/qualified-image-preservation.json),
[fresh census](../../artifacts/optional-module-durability/b181b6a29d134a3c8f06824f8e678032/compiled-resource-audit.json),
[profile preservation](../../artifacts/optional-module-durability/b181b6a29d134a3c8f06824f8e678032/profile-preservation.json),
[original gates](../../artifacts/optional-module-durability/b181b6a29d134a3c8f06824f8e678032/original-gates-current.json),
[structural checks](../../artifacts/optional-module-durability/b181b6a29d134a3c8f06824f8e678032/structural-checks.json),
[frozen inputs](../../artifacts/optional-module-durability/b181b6a29d134a3c8f06824f8e678032/final-inputs.json).

Self-review only; original first-fault, command results, immutable private facts,
ResourceFlowJournal, notifications and replay remain the operational owners, with
[wider service/assurance limitations](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
Original221requirement words/checks remain210source-recorded checked/11open, full goal
ACTIVE. These finite native/independent-port and native-spill results do not establish
all incident/cargo/reference providers, independent spatial cells, future/private CLR
mutation, every pressure strategy, lockstep/fuzz/soak or full regression/solution/CI.
Next continue the actual original caller/fault/stage-stress matrix, especially native
observation during other declared owner operations; no new resource owner or arbitrary
new workflow. No Oil/marine/consumer/business/platform/Draw/adapter work or publication.

<a id="current-m74-complete-topology-replay-identity--2026-10-22"></a>

## Earlier M74: complete topology replay identity — 2026-10-22

Follow-up: the formerly next physical continuous-wear branch is now qualified in
[M75](P8_CONTINUOUS_CATALYSTS_VERIFICATION.md#current-m75-physically-hauled-continuous-catalyst-accounting--2026-10-22).
The M74 topology record below retains its actual source image; not rerun for M75.

SELF-REVIEW via installed sdlc-orch implementation/verification on Beep.SimEngine/
master HEAD `cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work.
Previous M73 added actual physical contention proof: progress, but its full-round
replay claim was unsupported and is withdrawn. No original acceptance gate closes.

### Defect, focused reproduction and original ownership

ReplayRecorder owns one readonly SimGame reference. M73's Save helper replaced the
recorded primary with a loaded session while retaining that original recorder.
Later inputs/Updates went to the replacement. Finish verified the abandoned old
session's valid PREFIX, not the completed reconstruction. Its two setup recordings
and four prefix verifier calls are factual; four COMPLETE-round claims are not.

First add ONLY the missing `replay.ActualHash == game.Hash()` assertion. The native
literal reproduces **539/2**, one failure per round, exit1; physical accounting/work
checks remain green. Build1 clean12.01s. This exposes a proof defect, not a native
replay/gameplay bug. The original failing log/image are retained, not overwritten.

Correct only the existing ConnectedResourcePortTests.Contention owner: preserve
the recorded primary; load the twin at each save, immediately require hash identity,
then advance both through every later Update. The twin genuinely exercises restored
pending/partial/split/reconstruction states. No recorder migration, copy-back,
compatibility wrapper, new save format, resource/graph owner or game clock.
Count actual primary inputs INCLUDING rejected duplicate reconnect. Each round
requires this count delta == recorder.CommandCount and recorder.CheckpointCount ==
final Clock.StepIndex minus recording.InitialStep. Finally require verifier success
AND exact final live hash. Existing plan/work/network/resource/logistics owners
and unchanged file-authored content still perform all gameplay.

| Context / round | Actual steps | Commands / checkpoints | Live AND verified final hash |
| --- | --- | ---: | --- |
| Native port /0 |87→137|7 /50|74D0DF9D4B71B7AD|
| Native port /1 |137→203|8 /66|F38401265C28AFEE|
| Independent metadata port /0 |160→251|7 /91|EDA2E0C057B1ABD5|
| Independent metadata port /1 |251→373|8 /122|F03EC2AD5B814E53|

### Actual affected finals

Build2 clean8.49s, zero warnings/errors. Only both AFFECTED topology literals rerun;
no incident/dock/manufacturing/unrelated green scope. Final **1404/0**, four successful/
eight failed cuts/four physical reconstructions. Six actual replay verifications =
two reused physical setups +four COMPLETE new competition rounds, including actual
claim release, typed losing cuts, duplicate reconnect and physical supply through
final reconstruction. Not every interruption point, geometry/provider or lockstep.
Both contexts use independent point geometry/native NetworkResourcePort runtime.
Native **2.723.0/save161/replay2**, all450 native sources, native and proof DLLs are
unchanged. Only this test partial changes; no native defect/feature is claimed.

| Exact literal | Checks | Case / observer ms | PID / exit | Primary Updates | Peak / total Update bytes |
| --- | ---: | ---: | --- | ---: | ---: |
| Topology contention: repeated competing cuts and physical reconnect preserve native claims stock and allocation budgets |555/0|3801 /3939|26900 /0|116|1556664 /56928112|
| Topology contention: half-minute independent ports preserve competing cuts physical reconnect and allocation budgets |849/0|4883 /5017|8068 /0|213|1554744 /92823824|

Original33554432 peak/1073741824 cumulative per-tier managed Update budgets pass.
The primary now carries recording cost for the WHOLE continuation. Metrics exclude
original setup, twin/replay execution and hash/view/assertion work outside primary
Update; not native/all-thread/global-FPS or whole-suite allocations. Bounds stay
512Updates/stage,60000ms exact-case/5000ms termination; setup unchanged.
Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
The same two exact literals run Release/net8.0 test DLL via `--bounded-exact`,
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.
[All builds/runs and replay ranges](../../artifacts/optional-module-durability/c933df202b6a4a3f8d0cf1366f8bb129/attempts.json),
[raw logs/images](../../artifacts/optional-module-durability/c933df202b6a4a3f8d0cf1366f8bb129/),
[frozen inputs](../../artifacts/optional-module-durability/c933df202b6a4a3f8d0cf1366f8bb129/final-inputs.json),
[original221 map](../../artifacts/optional-module-durability/c933df202b6a4a3f8d0cf1366f8bb129/original-gates-current.json),
[structural checks](../../artifacts/optional-module-durability/c933df202b6a4a3f8d0cf1366f8bb129/structural-checks.json).

Shared results/plan blockers/notifications remain authoritative; [wider operational/
assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain. Self-review, not independent review.221 original/210 source-recorded checked/
11 open/full goal ACTIVE. No full/default/profile/family/soak, consumer/Oil/marine,
business/platform/Draw or staging/commit/push. Failed multi-file documentation patch
was atomic/no changes and corrected against actual text; harmless guessed recorder
source read corrected to discovered Replay.cs. No engine/build/runtime failure hidden.

Next candidate: actual hauled continuous catalyst wear. ContinuousCatalystTests.Fill
delegates to TransformationInputPortTests.Transfer/TransferProbeCommand; existing
networked physical continuous supply covers feed/fuel/reusable tools but not that
wear branch. Inspect selected JSON/evidence before adding missing physical proof
through original TransformerFlow/TransformationsTools, not a new wear ledger,
loader/scheduler or wholesale rerun. Broader original obligations remain open.

<a id="current-m73-repeated-competing-cuts-and-physical-reconnect--2026-10-22"></a>

## Earlier M73: competing cuts — full replay claim corrected by M74

SELF-REVIEW using installed sdlc-orch implementation/verification, canonical snapshot
`a80d41ad0affd81b549f5f063787e186d162e37f`. Beep.SimEngine/master HEAD
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty/untracked work.
Previous M72 added actual concurrent incident proof: progress. Its final evidence
was completed without rerunning green tests;739 frozen inputs/450 unchanged native
sources/72 selected documents/5580 local file links, zero structural issues, staged
or live owned processes. One premature final-inputs link check was retained as a
structural ordering failure, then corrected; no hidden engine/build/runtime failure.

### Existing ownership, not a second topology implementation

Native **2.723.0/save161/replay2**, all450 native sources and native DLL unchanged
from M72. LineConstructionService/LineWork own actual corridor execution;
NetworkService owns graph/pool identity, split/merge and connected access;
ResourceTransactions owns material movement and flow facts; WorkBoard/WorkSystem
own pending plans, single-machine scheduling, positive-time work, completion refusal
and history. The original physical helper still builds three real members, processes
finite material and physically hauls three units before the new continuation.

Existing ConnectedResourcePortTests gets optional content-authoring delegates on
its original Physical helper, two registrations and one Contention partial. Old
callers remain unchanged. New embedded `topology-contention.json` authors three
co-located removal structures/startup entries, two rounds and a reconnect path;
these are merged into the original selected module/world documents before master
composition. No second runtime loader, stock store, graph, command scheduler,
material rule, time source or engine world-size limit. No engine defect was found;
this closes a missing finite contention proof, not a missing engine feature.

The new driver uses public seats/commands, detached views, Update, Save/Load and
replay only. It never writes World, provider state, stock, tokens, geometry, job
assignment or progress and never issues a TransferProbe. Synthetic co-location
is valid authored point-space content, not hidden map mutation.

### Actual repeated competition and accounting

Each round posts normal connected inspection, waits for earned work and a real
two-unit original pool tool receipt, then submits three distinct persistent cut
intents in the same Update. All remain Pending/Order0/ActivePromises; saving and
three more Updates preserve geometry, original endpoint and exact receipt token.
Cancel inspection through the inbox. All three plans activate, but only one actual
machine is assigned at any observed Update. Saved partial cutting has not erased
the segment. One earned four-minute cut commits; both losing physical plans finish
Failed/NotFound instead of reporting a second success or faulting the world.

The actual split has two fresh one-member pools, no inbound/outbound promises;
minimum-three-member port is truthfully unavailable with null stock, not substitute
inventory. Old endpoint/receipt authority is retired. Submit reconnect twice in one
Update: first succeeds, duplicate returns PlanCodes.Duplicate and creates no second
marker/order/bill. Physically haul two finite previously delivered units to its real
marker; one machine performs original positive-time construction. The new merged
three-member endpoint is fresh and the existing capability rebinds. Failed cut
histories remain terminal instead of cutting the rebuilt graph. Before round two,
two more actual pooled units are physically staged at the destination through an
ordinary named-port haul; no free refill or new default supply.

Every observed continuation Update checks original remaining nine units = actual
entity stock/cargo/WIP/connected pools + the cumulative classified consumption delta.
Two reconstructions spend exactly four units, leaving five per context. Each context
retains six distinct cut order IDs, two successful/four refused cuts and seven unique
original/split/merged endpoint identities. Saved twins match after every Update,
foreign plan views remain private and an idle saved session stays unchanged.

Native connected-port at minute quantum and independent metadata connected-port at
half-minute quantum BOTH use the existing independent point geometry and native
NetworkResourcePort runtime. Independent metadata is not an independent port
runtime, tile-space topology or every geometry provider. Three targets/two rounds
are workload data, never engine caps.

### Focused execution and budgets

One target build, clean6.34s/zero warnings/errors. Two new literals, first-pass;
no existing M60/other green scope was rerun. No full/default/profile/family/soak,
consumer/Oil/marine, business-rule, platform/Draw, staging/commit/push activity.
Harmless guessed PointGame source read and positional Windows glob discovery errors
were corrected to actual discovered files/directory filters; not engine failures.

| Exact literal | Checks | Case / observer ms | PID / exit | Measured Updates | Successful / refused cuts | Peak / total Update bytes |
| --- | ---: | ---: | --- | ---: | ---: | ---: |
| Topology contention: repeated competing cuts and physical reconnect preserve native claims stock and allocation budgets |541/0|4158 /4295|15096 /0|116|2 /4|1557448 /42512480|
| Topology contention: half-minute independent ports preserve competing cuts physical reconnect and allocation budgets |835/0|4794 /4929|19616 /0|213|2 /4|1554248 /65953888|

Historical **1376/0**, four successful/eight failed cuts/four reconstructions on
build1. M74 CORRECTION: two verifier calls cover reused setup streams; four cover
only abandoned original-session round PREFIXES, not complete rounds. The missing
final-live-hash assertion reproduces two failures. Correct full-round proof is above.

Original stress Profile reader/budgets reused, not copied or relaxed. Allocation
measurement covers primary Update calls in NEW continuation only, excluding setup,
saved twins/replays/hash/view/assertion costs. Each context is below original
33554432 peak/1073741824 cumulative managed bytes; not native/all-thread/FPS or
whole-test allocation proof. New stage waits512Updates plus outer exact-case
60000ms/5000ms termination; original physical setup800Update bound stays unchanged.

Build command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
Each exact literal runs Release/net8.0 test DLL via `--bounded-exact`,
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.
[All attempts](../../artifacts/optional-module-durability/b73819afad074f93aee6bbd9ea000a53/attempts.json),
[raw logs/images](../../artifacts/optional-module-durability/b73819afad074f93aee6bbd9ea000a53/),
[frozen inputs](../../artifacts/optional-module-durability/b73819afad074f93aee6bbd9ea000a53/final-inputs.json),
[original221 requirement map](../../artifacts/optional-module-durability/b73819afad074f93aee6bbd9ea000a53/original-gates-current.json),
[structural results](../../artifacts/optional-module-durability/b73819afad074f93aee6bbd9ea000a53/structural-checks.json).

### Assurance, limits and next original obligation

Existing command results, plan/order failure blockers and notifications report
ordinary loser/duplicate outcomes; no swallowed exception or parallel diagnostic
service. [Shared operational boundary and wider assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain. Self-review is not independent review. Replay/privacy/durability/allocation
evidence is finite and selected, not global resource/provider qualification.

Original221/210 source-recorded checked/11 open is unchanged; no original gate
closure/full completion. P7.017 still includes remaining finite semantic/provider
accounting and broader fault obligations. P7.019 conditional lot/quality and other
P5/P6/P8/P10/P11 gates remain distinct. Review the existing M70 semantic-owner matrix
against actual caller branches next; do not invent another scheduler, lot ledger,
topology/conveyor/rail feature or rerun unrelated green families. Full goal ACTIVE;
publication waits for the whole goal/plan, not this finite proof.

<a id="current-d7b-m60-connected-claim-continuation"></a>

## Earlier D7b/M60: connected claim continuation

The selected next capacity-outcome work is now [qualified at CP2/M61](P10_TOPOLOGY_CAPACITY_ADMISSION_VERIFICATION.md#current-cp2-m61-legal-capacity-refusal) through its existing owner. This D7b record retains its actual image/results; broader original provider/fault/stress requirements remain OPEN and need requirement-level reconciliation rather than another overlapping driver.

UNCHANGED core2.717.0/save161/replay2; master HEAD `cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work. All450 native sources/native DLL and the proof DLL remain identical to M59. Existing network/port/work/claim owners already support the workflow; this adds missing current public-player evidence, not a fabricated engine feature or duplicate graph/stock/claim/clock model.

Inspected and ran two exact current baselines before source edits: durable topology81/0 (actual native stock/claims/redistribution/codec refusals) and retained endpoint integrity342/0 (physical setup plus explicitly labelled low-level original receipt/fault probes). They run on captured M59 tests image, not the final tests image, with zero actual replay verifications. [Baseline identities](../../artifacts/optional-module-durability/cfd3b07829694e9988e3cef5c1ec68c6/baseline-binaries.json) are captured separately. These protocol probes do not become player acceptance because their setup includes physical work.

The EXISTING ConnectedResourcePortTests.Physical helper now has an optional continuation; default behavior/registration remains. New partial source belongs to that SAME owner. Existing master JSON still authors all corridor/connected ports/processing/tool requirements/removal/rebuild rules. No new content subsystem, host gameplay, compatibility wrapper or native API. The new continuation contains no World/stock/token/geometry/worker/progress writes: public authoring already loaded, player commands, detached views, actual Update, Save/Load and replay only.

Four native/direct connected-port × minute/half-minute contexts first physically build the graph, run timed processing/work and haul through the original helper. From its actual saved six-unit pool/three-unit delivered state, new inspection earns machine work while holding a two-unit original pool receipt. Removal preview returns exact ActivePromises without mutation. A real persistent cut plan is accepted as intent but stays Pending/Order0 with that blocker. Save/wait preserves the original token/owner/amount and geometry. Cancelling the inspection releases its actual claim, letting the SAME plan activate and earn positive-time physical cutting; a saved partial cut still has not erased the segment or retired the endpoint.

Earned cut splits into two new one-member endpoint owners with six pooled units total, zero original work holds and exact complete stock conservation through the split. The retired original cannot resolve as current/old receipt authority. The SAME authored minimum-three-members capability returns InsufficientMembers with null stock/version-1, not an empty usable inventory. A real queued processor sees that same blocker, no WIP and no child-pool claim. This unavailable topology saves canonically and freezes under the sole paused clock.

Cancel that queue and submit a real line reconstruction plan. Original logistics hauls two units from the finite previously delivered stock; actual machine construction reconnects the missing segment. The merged three-member pool is a genuinely new identity distinct from retired/split endpoints. Final remaining stock is exactly original minus the authored two-unit rebuild bill; during reconstruction it stays within that bill's exact bounds. Subsequent ordinary inspection acquires a NEW actual receipt on the merged owner, never remapping an old token. Cancellation releases current holds; private plan views and an independent saved session remain isolated. Saved twins agree after each Update. Eight actual replays comprise four existing physical-setup replays plus four new command-continuation replays; failed/unfinished clips are not counted.

| Selected literal | Checks | Suite/observer ms | PID/exit | Actual replays | Image |
| --- | ---: | --- | --- | ---: | --- |
| Network transfers: durable topology identities, live claims and exact redistribution | 81/0 | 829/1011 | 25240/0 | 0 | baseline M59 |
| Topology endpoint integrity: retained claims survive erase and callback mutations fault | 342/0 | 2530/2722 | 2864/0 | 0 | baseline M59 |
| Connected topology continuation: real tool claims protect saved cuts and physical reconnect rebinds original owners | 918/0 | 9881/10056 | 2264/0 | 8 | final build1 |

Final-image claim is the new918/0/eight replays; baseline423/0 remains separate. One targeted build clean0warnings/0errors/6.27s; all three runtimes first-pass/exit0. Final live observer63718 polled terminal0, not restarted. Build command `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`; each exact literal uses Release/net8.0 tests DLL with `--bounded-exact --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`. New continuation stages512Updates/20000ms are verification failure bounds, not engine limits; original helper800Updates remains unchanged. Framework profile enrollment is metadata only. NO old green rerun after test-only edits, full/default/profile/family/soak, consumer build/runtime or increased limit.

[Every attempt](../../artifacts/optional-module-durability/cfd3b07829694e9988e3cef5c1ec68c6/attempts.json), [logs](../../artifacts/optional-module-durability/cfd3b07829694e9988e3cef5c1ec68c6/), [frozen inputs](../../artifacts/optional-module-durability/cfd3b07829694e9988e3cef5c1ec68c6/final-inputs.json), [original221 source map](../../artifacts/optional-module-durability/cfd3b07829694e9988e3cef5c1ec68c6/original-gates-current.json), [structural checks](../../artifacts/optional-module-durability/cfd3b07829694e9988e3cef5c1ec68c6/structural-checks.json). Native SHA75BFEE4612E242906F880107BF7AB7F5A77DF0EBAAE59C652DD9B2BC891FDD24 / proofB635CE3A26642A5F05394A7F9830A6C33ED7A0EA4BACE007298118CD4762647C unchanged; final tests3A569AF62586B367BF25BAA988702A0D8FE19AB9E6CF5175EEB4C6F2FAFA4207. Baseline tests5CDBBC5B... separately captured. No native/schema/header rewrite, business/platform/Draw/Oil/adapters, staging/commit/push.

Self-review, not independent review: real original receipt blocks destructive work; one persistent intent activates only after release; saved unavailable ports/queues expose truthful blockers, not fallbacks; original topology redistributes actual material and fresh identities; reconnect spends authored finite material; token ownership/privacy/session isolation and replay remain through existing owners. Original reporting/results/notifications stay authoritative; [broader operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage) remain. Existing D7 design/evidence/master/current P6 navigation updated in place, historical initial D7 record retained below with its actual identity. Original221/210 source-recorded checked/11 open unchanged, no original gate closure/full completion. Next audit coupled topology capacity/erase/provider refusal coverage against existing contracts, retaining public workflow versus explicit protocol-probe boundaries and exact focused selection. Broader provider/reentry/lockstep/stress/every-module requirements remain OPEN. No live owned worker or staged files at handoff.

<a id="outcome-and-scope"></a>

## Initial D7 outcome and scope

Engine remains **2.619.0 / save159 / replay1**. No production source, protocol, business rule, host application, renderer or platform change. 442 core C# path/content SHA256 remains `800b2e6f72551035f403a1fd9d5c2b376f7bb72c4febd5e017f10327846cb9bc`.

Five selected literal finals: **81 topology +149 timed flow +93 incidents +111 cargo +347 mixed-party topology =781 passed, zero failures**. This is not full/profile/family regression or original P7.017 completion. Bookkeeping remains221 requirements /200 recorded checked /21 tracked open, not a fresh global audit.

## One physical-only fixture owner

Original durable topology could not compose because it loaded the old recovery supplier without Tycoon. The actual engine behavior had not been reached.

Physical-only authoring was moved, not copied, from `ResourceDispositionTests.Factory` to sole `PhysicalRecoveryTestContent.Factory`. The old method was deleted; no forwarding compatibility alias. Every selected caller explicitly supplies its extra definitions. The shared owner reuses existing Colony composition and existing master-listed navigation/startup/physical JSON. Its only extension is supplying the actual infrastructure document through Colony's existing parameter.

The fixture still explicitly omits the unused market prototype and never activates business, invents money or relaxes provider validation. Cargo explicitly selects its existing native recovery module/profile and private depot property. Other old raw recovery callers are not silently migrated.

Compiler evidence exposed unqualified static-import callers missed by the initial qualified-reference search. Eight files now import the new actual fixture owner; two cargo calls explicitly pass their original disposition definitions. The worker-specification caller migrates directly. These are mechanical authoring-call changes, not gameplay or new schedulers. Nonselected callers were compiled, **not** broadly runtime validated.

## Current native codec corruption proof

The old partial decoder assumed no initialized marker and TileCoord members. Native NetworkService.Sync now writes its actual initialization marker and stable geometry-member strings. The proof now requires actual canonical network.main/initialized real component/native discrete steel as fixture preconditions and locates current typed fields. It does not guess/fallback to old codecs.

Seven negative mutations remain: invalid next endpoint, endpoint outside its allocator range, negative version, incorrect ownership, fractional discrete stock, over-capacity stock, and invalid membership despite the dirty-geometry flag. The final mutation uses original StateWriter to encode an existing member into the first member field, producing actual duplicated/out-of-order geometry. Obsolete SkipElapsedFlow/tile-member decoding was deleted. This is a small native field corruption probe, not a second engine serializer or topology algorithm.

## Actual topology and flow ownership

Source inspection of `Networks.cs` confirms that live redistribution creates actual endpoint owners, groups whole resource Bills and delegates to original `IResourceTransaction.CommitMovements`. Old endpoints retire only after real stock and reservations are empty. Ordinary network transfer delegates to original CommitMovement. Candidate allocation lists do not become stock balances. No alternate stock owner/redistribution path was introduced.

Durable topology81/0 verifies claimed pools block destructive cuts, unrelated component identities/receipt tokens survive pending geometry and save/load, exact split/merge and private facts, aggregate later-network capacity refusal without an earlier prefix, invalid raw topology with live claims, and current codec/refusal cases.

The topology-only protocol fixture explicitly removes the source prototype's automatic flow port. Its manual split/merge measurement must not receive additional fluid from unrelated timed source flow while comparing a closed redistributed vector. No engine timing/routing/resource rule changed. The separate timed-flow literal retains the original automatic ports and source stock.

New topology assertions close actual source+receiver+all-main-pool steel/water balances before/after claimed transfer, split and merge. Each split fact verifies exact original-source debit, actual new-destination credit and live destination identity. Raw journal totals equal the whole original split vector once, with no creation/consumption. Actual fractional water remains5.5 and divides into2.75 shares; no numeric rounding was substituted.

The old malformed-rate probes used whitespace-sensitive replacements that did not modify the current formatted JSON. They now edit parsed quantity/resource fields to prove actual zero-rate and duplicate-resource refusal.

Timed flow149/0 retains native rate accrual, fractional remainder, current stock/version identity, saved continuation, public-neutral ownership, private-neutral refusal, receipt release and private journal partitions. Its negative case now explicitly authors both retain_take and partial_take. The original resource transaction first-fault phase remains `resource.transaction`, not the outer network system. Retain_take returns a claimed amount without advancing its mutation version and is rejected at that earlier actual invariant; partial_take fails exact receipt fulfillment. Both prevent healthy history/save/hash, leave no queued success events and cannot resume time.

## Independent envelope evidence

The unchanged mixed-party topology literal347/0 passed on the same unchanged core before the fixture edits. It exercises complete shared-stock movement envelopes, aggregate competing claims, original-party receipt release, private/world actor attribution, actual physical owned-world graphs, saved continuation/replay and provider failure boundaries. Its live runner handle was polled to completion, not restarted.

All direct stamps, actual receipt setup and corrupt payloads are **protocol/provider instrumentation**, not proof of complete public-player acceptance. Physical player segments in the envelope case do not make every low-level setup a player command.

## Every observed attempt

Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

Each runtime is one `--bounded-exact` child, timeout60000 /termination5000. Runtime outputs below are untruncated; output4500 except topology/flow intermediate output6000.

| Attempt | Result | Evidence |
|---|---|---|
| Original topology baseline |0/1,79ms|pid25032,exit1,outer218ms; inactive supplier before play.|
| Mixed-party envelope final |**347/0**,12661ms|live handle78422 polled terminal;pid19304,exit0,outer12793ms. No production edits afterward.|
| Initial shared-owner build |Failed3.77s,47 errors|Static-import Factory callers were unresolved; returned compiler output truncated. No runtime on failed build.|
| Direct import/cargo-argument migration build |Clean4.52s|zero warnings/errors,exit0.|
| Topology intermediate |56/4,680ms|pid436,exit1,outer821ms; automatic fluid inflow confounded closed split; two malformed JSON replacements changed nothing.|
| Flow intermediate |135/2,879ms|pid12380,exit1,outer1023ms; actual resource first-fault ownership/earlier invariant differed from old assertions.|
| Incident final |**93/0**,1191ms|pid22940,exit0,outer1334ms; moved shared composition preserves named responses.|
| Cargo final |**111/0**,586ms|pid16944,exit0,outer729ms; moved shared composition preserves interrupted partial unloading/receipts.|
| Closed-topology/parsed-negative/fault-diagnostic build |Clean4.79s|zero warnings/errors,exit0.|
| Topology final |**81/0**,725ms|pid1544,exit0,outer866ms.|
| Flow diagnostic intermediate |136/1,880ms|pid2188,exit1,outer1012ms; actual retain_take error is missing mutation-version advance, not a short return.|
| Two explicit receipt-mode proof build |Clean6.00s|zero warnings/errors,exit0.|
| Flow final |**149/0**,854ms|pid30876,exit0,outer998ms.|

No executable edits after the last final. Incident/cargo fixture and bodies, independent envelope source/core, and topology body were unchanged after their respective finals; later edits were confined to the separate timed Fault method. No full/profile/family runtime or Git publication. All process handles terminal.

## Remaining gates

Original P7.017 remains open for broader topology capacity/erase/fault/stress, other disposition/recovery consumers, coupled callback/auxiliary observation and healthy creation/initialization/replacement boundaries. Other spill/topology scenarios and mechanically migrated static-import/worker callers are not claimed runtime green. No arbitrary-map deadlock, all-mod purity, global allocation/performance or full-regression claim. No staging/commit/push before full goal and plan completion.
