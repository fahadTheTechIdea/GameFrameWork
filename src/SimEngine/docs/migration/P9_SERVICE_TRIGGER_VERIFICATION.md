# P9-B11.3c2a authored service triggers — verification

Date: 2026-10-07. Dirty working-tree checkpoint, not clean HEAD attribution or full regression.
Build2.192.0/save104/board36/work34/work view67/native recurring8/public recurring5,5,6.
Existing owner payloads and movement/logistics/condition/policy schemas are unchanged.
[Design](../../plans/enhancements/P9_SERVICE_TRIGGER_DESIGN.md).

## Ownership and implementation

Every native/public recurring definition explicitly reads trigger: schedule, maintenance or
schedule_or_maintenance. Non-scheduled modes require actual named operation-policy metadata
and the selected coordinator before gameplay. Shared RecurringPlanContracts reads that policy's
current maintenance band and criticality, never copies thresholds or adds a maintenance scheduler.
Native CheckTrigger now owns only its authored signal filters; public providers retain their own
pure permit/idle filters. Public source intent priority uses the existing EffectivePriority rule.

Schedule-only work cannot become condition-driven. Low condition can request early real service
in either maintenance mode, but disabled state, outstanding work, pending whole-cycle completion,
failed/cancelled-attempt retry spacing and ordinary worker/resource/dock admission remain in force.
Healthy scheduled inspections remain possible. Immutable private views expose explicit mode and
actual MaintenanceDue; DueGameTime remains the scheduled cadence, not a separate clock/deadline.
Actual cohort completion is resolved once per view observation and reused as local read scratch.

Operation policy remains the owner of shutdown hysteresis, latch and restart settings. Condition
wear remains physical game-time exposure. Existing stock-band logistics supplies a real shortage
through other machines; spares and tools use existing claims. Service still earns positive machine
work, restoration, physical exit and all remaining DAG stages. There is no free repair, resource
grant, forced movement through a disabled propulsion gate, success ledger, new codec or clock.

## Focused final gate

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: authored cadence, private admission and retained success"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: timed work, materials, restoration and saved continuation"
```

Final build: zero warnings/errors. Admission138 + physical194 =332 passed, zero final failures
or aborted suites. Only these same two exact suites ran. They retain c1's focused single/grouped
completion/provider/history/save tests and add trigger/private/physical policy coverage, not a
broad family filter. All separate suites compile only; full acceptance/replay was not run.

New evidence covers missing/unknown trigger, required native/public policy linkage, disabled
condition demand, private/pure mode/readiness, healthy scheduled service vs condition-only refusal,
actual wear-driven early maintenance in both maintenance modes, schedule-only waiting despite
the same low condition, ordinary policy threshold commands, cancellation retry spacing, actual
prior-success retention, unsafe operation shutdown, unchanged stock/condition during spares
shortage, refusal of premature player restart, physical stock-band courier delivery, timed repair,
automatic safe restart and saved twin identity. Service stages are bounded at400 normal updates;
retained grouped proofs at480. Source/state rules live in engine/provider contracts, not tests.

## Corrections and limits

The first public linked-policy fixture exposed that its older provider authored raw source
priority while the protocol required policy criticality too. It now calls the same public
EffectivePriority calculator as the native source; no priority rule was copied or weakened.
Existing JSON and inline authored sources migrate explicitly to trigger and policy fields.
Existing linked native policy content selects maintenance; unlinked content selects schedule.
There is no inferred/default/legacy trigger path. Current build identity migrates directly;
save104 and owner payload layouts are unchanged. Historical schema assertions in other suites
are not claimed green. Existing dirty source is preserved.

Native condition wear is used with native and independent public recurring/policy/worker providers.
This does not prove every wear/provider combination or a failed propulsion rescue scenario. The
proof process operation is independent of worker propulsion; shutdown never fabricates mobility.

GameClock.cs remains unchanged, SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
World constructs the sole GameClock once and SimGame reuses it; World.Tick only reads StepIndex.

The lifecycle skill kept stage implementation, scoped evidence and tracker aligned. Literal current
source/document fingerprints appear in [the manifest](P9_SERVICE_TRIGGER_SOURCE_MANIFEST.json).
Earlier reports/manifests remain historical checkpoints, not current fingerprints.

B11.3c2a is focused verified, not full B11.3c2/B11.3/roadmap completion. Wider fleet/trip readiness,
service queue/congestion, failed-order/retry/provider stress, propulsion rescue, arbitrary global
purity, CPU/allocations, replay and full acceptance remain open. No Oil/Tycoon/host/demo/platform/
Draw work and no full/family/third gate.
