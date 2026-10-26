# P10-B9 workload and physical queue signals — focused verification

## Current M160 contract-authored workload and fleet signals — 2026-10-25

Observed engine2.777.0/save161/replay2, dirty master based on
cb61483ab81404c11944931aedb4703ba694edb5; only Beep.SimEngine was edited.
Artifact owner:`artifacts/optional-module-durability/m160-workload-signal-contracts/`.

Native WorkLoadSignalDef now delegates to the shared WorkLoadSignalDeclaration;
ProofWorkLoadSignalDef implements IWorkLoadSignalDefinition without native inheritance
or its own validation override. WorkLoadSignal consumes that contract. Its original
14 metrics, semantic schema2 and empty codec1 remain unchanged. The module admits all
loaded bindings; actual frozen metadata aliases/role/worker shape and live provider/
entity/clock observation use the existing authorities. There is no second counting,
worker assignment, dock, automation, scheduling, resource or time implementation.

Each exact case used:
`dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact '<case>' --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 25000`.

| Exact selected case | Final checks | Body/process ms | PID | Log/test build |
|---|---|---|---|---|
| Workload signals: independent JSON metadata drives physical queues saved controls and replay | 65/0 | 1817/1997 | 21076 | independent-workload-8.log/build9 |
| Worker capacity: independent JSON metadata drives physical fleet dispatch saved controls and replay | 72/0 | 2548/2728 | 18648 | independent-capacity-4.log/build5 |
| Workload signals: authored scopes, private facts and query ownership | 49/0 | 558/727 | 29120 | native-workload-contracts-2.log/build10 |
| Worker capacity: role filters, current eligibility and private fleet signals | 117/0 | 687/984 | 6324 | native-capacity-contracts.log/build9 |

303/0 aggregate, zero final aborts/truncation/observer errors. Native/proof source did
not change after build1; subsequent builds repaired only tests/fixtures. This is not
a final-build10 whole-test-image regression. No green fleet case was rerun after the
later workload-only edits. One complete backlog replay and two fleet policy-context
replays require Success and ActualHash equal the actual final live hash. Queue/current-
leg subcases retain explicit low-level haul/fault probes; they are not represented as
complete public command-stream replays. Hash values/checkpoint counts are not printed.

Retained attempts: workload1–3 each12/1 stalled on physical departure blocked by parked
disabled machines. Attempt3 records an ineffective prototype-position edit because
actual startup placements still selected the old positions. Workload4 26/1 reached
later queue stage after the first scenario was repaired, but its naturally brief
loading window ended before simultaneous waiting arrival. Workload5 27/1 used ordinary
carrier pause but revealed nearest-slot ordering crossed the two waiting workers.
Workload6 57/2 exposed a non-hauling idle contract worker in the second carrier's path
and the changed nearest receiving slot. Workload7 62/1 reached real receiving delivery
but a parked first vehicle blocked the second departure. Attempt8 is the passing
literal above. All original assertions/resources and bounds remain; no teleport,
disabled collision or time/rate/budget increase was introduced.

Fleet attempts1–2 each17/1 stalled behind waiting/idle machines; attempt3 17/1 confirms
moving prototype positions alone did not change explicit startup. Attempt4 passes
with a source exit behind the bay and off-route idle startup parking. Workload native
contracts first35/1 attempted a normal read in a session deliberately faulted by the
foreign-world negative probe. Final repair uses a separate doomed session, asserts
its first fault and save refusal, and keeps the healthy isolation witness separate.
No failure was deleted, excluded or reported as a pass.

Ten targeted compile attempts, not test-suite runs: build2 failed on an incorrectly
named diagnostic field (CS1061); fixed using actual SourceDockSlot/DestinationDockSlot.
The other nine builds are clean: build1 9.52s,3 6.36s,4 6.52s,5 6.77s,6 7.24s,7 7.01s,
8 6.58s,9 7.00s,10 6.67s. build-1.log through build-10.log retain every output.
Only four distinct affected test cases were selected, with failed-case retries;
no full/profile/family/soak run. Metadata-only profile inspection1112 registered/
1091 included/21 excluded/zero runtime bodies, profile-metadata.log.

Current native DLL SHA256:
7DBF759B0908E5A451B522DD205698E8A7EFBFE51122A233808AC491E4ACF172.
before-inputs.json records eight selected pre-edit source/config hashes; final-inputs.json
records those plus five current DLL/PDB hashes. Not a whole-repository freeze or fresh
resource-mutation census; historical resource accounting evidence keeps its own images.

Self-review: existing physical tests were parameterized, not copied. Explicit fixture
JSON changes select the independent component and exact signal provider schema; the
existing proof module declares it in the master and only delegates authoring. Startup
positions are authored before Initialize. Waiting behind bays and two-sided exits
make the one-dimensional convoy feasible without replacing native motion. Player
operation commands hold/resume a real loading carrier to form a queue; engine rules
still determine arrival, priority, dwell, action timing, loading, delivery and departure.
Failure diagnostics only read existing jobs/positions and retain all original limits.
No normal gameplay/state mutation, fake counts, fallback or duplicate owner was added
to host/demo/tests. World first-fault/private views/events/journal/replay remain the
affected diagnostic owners; wider operational sink/audit/notification gaps remain in
the master. No universal malicious-CLR-provider, all-module or allocation claim.

P10.022 and unrelated full feature gates remain open; this closes the workload-source
concrete metadata dependency. Quality/lot deferred. No Oil/host/demo/platform/business
change or staging/commit/push occurred.

## Historical B9 evidence

Date: 2026-10-06. Runtime: Beep.SimEngine only.
Build **beep-simengine-2.153.0**, save **80**. WorkView44; optional work_load_signal codec1/schema1.
Existing WorkBoard24, WorkSystem15, automation/job/cargo/clock layouts unchanged.
[Stage](../../plans/enhancements/P10_WORKLOAD_SIGNALS_DESIGN.md).

## Authoritative ownership

IWorkLoad aliases the actual existing WorkBoard, not a second query service or state store. Its pure
ReadWorkLoad returns detached immutable company/endpoint/assigned-worker counts from the board's real
live jobs: activity, hauling, open, assigned, paused, waiting claims and physically arrived waiting.
Terminal jobs do not count. A haul touching both endpoints counts once per query; queue counts refer
only to its current loading/delivery endpoint. Company scope requires no subject; other scopes require
the actual live owned endpoint/worker. Invalid scopes/company/ownership/subjects refuse explicitly.
WorkView hides foreign company aggregates; OperationsView retains its existing NotOwned signal refusal.

WorkDocks owns ReadDockWaiting through its existing slot classification/reach methods. Approaching or
paused claims remain honest waiting counts; physically arrived means real reach, not FIFO eligibility,
service availability or an invented ETA. Missing waiting participants/unsupported geometry return a
typed query refusal with no partial aggregate. Arbitrary job instances cannot impersonate board jobs.
Neither query nor signal is another queue, assignment policy, cached count or scheduler.

Optional sim.work-signals requires sim.work and sim.operations. It registers one stateless metadata
component, no system, persistent service or additional view. Explicit JSON id/scope and signal provider
metric/kind/unit/schema are validated by existing content ownership/operations catalogs. Empty actual
workload is exact zero. Eight count metrics use unit.count; actual world/entity/content identities and
existing pure geometry/source guards remain required. WorkLoadSignal reads only IWorkLoad; there are
no concrete worker/dock cases or host-owned counting rules.

Existing rule thresholds, dwell, cooldown, private discovery/previews and deferred normal commands
consume these sources unchanged. Backlog control enables a real native worker execution capability;
physical work uses ordinary jobs, actual travel/service/exit and entity updates. Queue control consumes
actual arrived waiting, earns dwell/cooldown and changes actual worker execution only next update.
No same-update feedback or new gameplay path is introduced.

Exactly one GameClock remains constructed at Kernel/World.cs:84. World.Tick at line99 is only
Clock.StepIndex. GameClock source remains unchanged:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
Counts contain no current time or local timer. Paused Update(0) advances neither actions nor that clock.

## Final commands and results

```
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Workload signals: authored scopes, private facts and query ownership"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Workload signals: physical queues, deferred controls and saved continuation"
```

- Final clean build: **0 warnings, 0 errors**, MSBuild **2.34 s**.
- Authoring/scopes/private facts/query ownership: **47 passed, 0 failed**, **269 ms**.
- Physical queues/deferred controls/saved continuation: **60 passed, 0 failed**, **437 ms**.
- **107 passed, 0 failed, 0 aborted suites**. Only these same two complete names were run this stage.
- 63 previous build assertions/files migrate to2.153.0; those suites compile only. Save80 and existing
  service payload schemas do not change. ServiceDockTests' authoring helper forwards optional explicit
  module/document declarations; its older suites were not rerun.
- Scoped source/document whitespace and local workload links pass; Git reports normal line-ending
  notices only. Source did not change after the final clean build and the two gates.

Contract evidence covers actual board alias/codec, native and public-only ContractWorker, eight
immutable signal bindings, exact empty counts, private/company/endpoint/worker ownership, actual
job state partition, missing subjects, arbitrary-job refusal, invalid required JSON/scope/kind/unit/
metric/provider schema, wrong-world signals, frozen snapshots, normal cancellation, saves and twins.

Physical evidence includes two parallel real jobs behind an occupied hauling bay; waiting claims
before arrival; zero remote work; pure repeated arrival reads; queue-driven deferred execution;
physical promotion; normal policy pause without progress; cancellation preserving unrelated cargo;
real eight-unit delivery. Two competing real hauls reserve future receiving waiting places without
inventing a destination queue while still loading. Only actual receiving arrival changes that queue;
saved twins deliver exactly sixteen units through the existing stock/receipt/handling owners.
Explicit missing-worker fault instrumentation returns NotFound/no partially counted prefix, then
restoration returns the exact prior hash. Backlog-driven native work moves to the bay, completes both
nodes and departs before zero backlog earns deferred shutdown. Isolated sessions remain unchanged.
Each wait is bounded at400 game updates with workload/queue diagnostics; no full acceptance run.

## Same-gate repairs

The first new fixture was not included in ExtensionProof's explicit embedded-resource list. The
project now explicitly embeds it; no scanner/fallback was added. Later assertions expected NotFound
where existing OperationsView correctly returns NotOwned; only expectations changed. A new step
helper originally passed zero elapsed time, so the existing sole clock correctly did not progress;
the helper now drives ordinary Update(), with zero elapsed retained only for pause checks. Finally,
an execution-disable test expected policy-pause semantics; ordinary work execution disable correctly
reopens an ordinary assignment. The test now uses the real servicing actor and normal work-policy
pause, preserving existing engine semantics. Final clean build and both same-name gates pass.

## Source fingerprints (SHA-256, framework-relative)

```
7D73A0FC626819F033B6A85C09DDC2DF2C8037E59BC0CE47B82A84F45481C379 src/Beep.SimEngine/Standard/WorkLoad.cs
6C36DC6E6F88571A217F900030135E9561C95964FAF1D0B0C7042D815B1FC2D7 src/Beep.SimEngine/Standard/WorkSignals.cs
0DE2FC25BE0ABB05BAA9E6EB887401B231820150A75188DEF2FD1B2BFDEF103D src/Beep.SimEngine/Standard/WorkSignalsModule.cs
14589CA7F3AB0B862E26729F8DF926E8C167FFA94E8EF7FE4743D05AFC1D8767 src/Beep.SimEngine/Standard/WorkDocks.cs
FD2C58033851AF38DEEBE64AC332CE81097B7919A893C7A2C89427A08E35D605 tests/Beep.SimEngine.Tests/WorkLoadSignalTests.cs
F1C61D6F333D54A966A24232AC2C2ECB8DBA51F28B9EAEF20618C4C288048BCA tests/ExtensionProof/content/workload-signals.json
```

## Remaining scope

These counts describe created live jobs, not unpromoted plans, arbitrary pending-node shortages or
idle fleet capacity. Waiting claims/arrival do not prove unconditional queue fairness or service ETA.
Unsupported geometry returns typed refusal, but its broader external-provider fault matrix remains
open. Existing scoped pure guards are not proof of global callback rollback/purity.
No third/family/full test run, Oil/Tycoon/host/demo/platform/Draw work, separate clock/queue/store or
legacy loader. Earlier recurring/area/haul/utility/automation suites were not rerun. Wider providers,
service subjects/fleet policies, traffic/congestion/fuel/route forecasts, global utility physics and
arbitration, broad replay/stress/fuzz/allocation and full P9/P10/P1–P12 acceptance remain open. The full
framework goal is active/partial, not completed by this focused gate.
