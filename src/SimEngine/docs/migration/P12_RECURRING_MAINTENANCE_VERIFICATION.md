# P12-D3 recurring inspection / maintenance — scoped verification

Date: 2026-10-06. Runtime: Beep.SimEngine only. Build **beep-simengine-2.101.0**, save **64**.
[Stage](../../plans/enhancements/P12_RECURRING_MAINTENANCE_DESIGN.md).

Historical evidence, not rerun at current source. [P12-D4](P12_OPERATION_POLICIES_VERIFICATION.md)
replaces source codec with2/work view31 and requires explicit operation_policy:null/ID for native
recurring authoring. These original fingerprints/counts describe D3, not D4 regression coverage.

## Implemented ownership

Native recurring_activity is a named optional entity capability in existing WorkModule, not another
module, service or scheduler. Explicit master-listed JSON authors actual activity, positive aligned
interval, enabled/priority/group and shared typed conditions. IRecurringPlanDefinition/Source expose
metadata, immutable schedule/intent, pure readiness and exact control/acknowledgement contracts. Each
source's codec1 owns only current enabled/priority and last accepted plan/tick. WorkBoard22's payload is
unchanged and alone owns plans/orders/jobs/progress/pause/cancel/history. No health/resource/work mirror.

The existing WorkSystem400 asks its board to observe sources before PromotePlans. Every trigger is
captured before submissions. Competing offers sort by descending explicit priority then stable entity/
source identity; ordinary board admission is rechecked immediately before submission. Manual/pending/
paused/active identical work prevents duplicate execution and refused sources never earn a posting
clock. First eligible observation posts immediately; cadence is minimum time between accepted plans,
not time since completion. Next-due overflow returns a typed blocker, not a wrapped/int-sized deadline.

Sources reference actual native plan history by company/key/intent identity/created tick, including
after ended native orders are pruned. Current group/priority/pause remain solely on plans/orders.
Disabling a source stops new postings without rewriting/cancelling already-admitted work. Existing
plan commands pause/cancel; re-enabled eligible sources submit fresh work after cadence. Shared
structural schedule validation stages native decode. Actual session time and plan references validate
through existing board checkpoint/post-restore paths. Provider purity/acknowledgement guards verify the
actual entity/tick/allocator and exact admitted reference, not arbitrary global transactional rollback.

ConfigureRecurringPlanCommand and matching WorkView preview/read use the same source control protocol.
Private views return detached frozen intent/schedule/due/blocker facts, never a mutable provider. Shared
SignalGateCondition remains the only native condition parser/binder/evaluator. Independent public source
providers can implement other triggers/intent schemas with no coordinator edits or friend access.
Current work view30/system2 replace their prior schemas directly. Existing owner payloads stay unchanged;
old saves/peers still fail exact build identity. No compatibility shim, old loader or inferred policy.

## Final commands and evidence

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Recurring plans: authored schedules, controls and checkpoint ownership"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Recurring plans: physical inspection, maintenance and saved continuation"
```

- Build: zero warnings/errors, **4.04 s**.
- Contracts/schedules/ownership: **70 passed, 0 failed**, 668 ms, one exact suite.
- Physical inspection/service: **26 passed, 0 failed**, 637 ms, one exact suite.
- **96 total passed, 0 failed, 0 aborted suites**. Subsequent additions reran only these same two names.
- First runs also passed; final additions explicitly prove equal-priority array-order independence and
  capture of two backlog-based trigger decisions before either admission changes board state.
- 35 earlier engine identity assertion files migrate/compile only. No earlier, third, family, long
  acceptance or full-regression suite was executed. Prior D1/D2 counts are historical, not current gates.
- Scoped tracked-file whitespace check passed, with only Git line-ending notices.

Contract evidence covers the sole board alias/codec, required authoring and source identities, missing
references/orderability/target/unit/quantum errors, private immutable reads, preview/command refusals,
healthy-condition suppression, first posting/exact cadence, saved references, staged malformed decode,
future checkpoint/post-restore rejection, forged plan links, competing priorities/stable ties, pending
manual duplicate prevention, public permit/backlog triggers, same-pass capture, five impure/malformed/
dishonest acknowledgement fault modes and explicit full-width due overflow.

Physical evidence submits normal engine player commands: a real machine performs timed damage; the
player enrolls threshold maintenance; automatic intent starts ordinary physical work. Crane travel and
positive-time repair spend three actual spares and hold two tools. Source disable/reprioritization does
not rewrite admitted work. A native plan command pauses execution; cancellation releases only held
tools with no refund or repair. Re-enabling posts fresh work, pays another three spares, repairs once
and suppresses further healthy-service posts. Terminal order cleanup leaves the actual plan history
reference valid. Periodic inspection is another physical DAG, spends one actual spare and reposts at
exact authored cadence. Saves during work and interleaved wide-time worlds retain identical hashes;
foreign company stock/sources remain untouched. All waits are bounded at700 updates and report tick,
condition, source blockers and each plan's state/blocker on failure.

## Source fingerprints (SHA-256, relative to framework root)

```
3d3e531606e9e66d4867695ca9c83c828d9b0674a471b7370d3b499dbd1f4e97 src/Beep.SimEngine/Standard/RecurringPlans.cs
021b6a3410f29b2b0da8f42507e463bf70d57de011bb2424a80f02023ed3de8b src/Beep.SimEngine/Standard/WorkRecurringPlans.cs
7b8127b816a96741a85ddbdd2a898de6aa416074664e16372519980792c34af4 src/Beep.SimEngine/Commands/RecurringPlanCommands.cs
50cf1e02dfe9ed43d62e7c0c2174a11480558da89ef7c0fea367ce7354ce2a16 src/Beep.SimEngine/Standard/Work.cs
cc684e10bde765010b7c8cf673b9765e9cd3db1b441f1f286a07bd5c53c02fcd src/Beep.SimEngine/Standard/WorkModule.cs
ca23c10730f9f32dde9646bee37625107865b51299b1bf6b302ceb9587957e7f src/Beep.SimEngine/Views/WorkView.cs
7e097d31f8b10b43f637b97dc064759d4b4a012514775511a70f7042a9a6c2c9 src/Beep.SimEngine/Kernel/SaveGame.cs
3f0bfc45ac1823357ac7fbe1be323e9527e308fa072d8de4ed32881c331deb8c tests/Beep.SimEngine.Tests/RecurringPlanTests.cs
aaae5fbb9b8efa86efa86bed411bf8303614a3ea9e6dd7ddbf5871a2a5346ed5 tests/ExtensionProof/RecurringPlanProof.cs
51fdcab4b717760e87aa18431e09ddea99f866dcce74a4a8de1eb1cae9ab4026 tests/ExtensionProof/content/recurring-maintenance.json
```

## Remaining scope / limitations

Operating/fleet/preparedness reserves and policies, inspection discovery/scientific formulas, incident/
condition effects, arbitrary operation gates, contamination, provider replacement/reference migration,
broader failed-execution/recovery scenarios, replay/lockstep/shuffled arrival/fuzz/stress/soak/allocations
and full P12/P1–P12 acceptance remain open. This does not certify every possible cross-service mutation
or global transactional restore rollback. Cadence is admitted-intent spacing, not automatic cancellation,
completion spacing or free inspection information. No OilFieldDays, Tycoon, host/demo/harness, Unity/Godot/
platform or Draw changes. Development-lifecycle guidance kept code, stage and tracker aligned.
The full framework goal remains active and partial, not complete.
