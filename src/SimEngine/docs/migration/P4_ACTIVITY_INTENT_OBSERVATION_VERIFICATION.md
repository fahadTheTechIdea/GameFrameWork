# P4-R2 activity-intent observation evidence

2026-10-15. Engine2.607.0/save159/replay1/work-view106. Three literal finals1096/0.

## Demonstrated defect and repair

The actual proposed-requirement path and WorkBoard's shared player-activity
admission called arbitrary `IPlayerActivityDefinition.CheckIntent` directly. The
service path used EntityObservation.Read, which did not latch callback exceptions
or observe the whole target/provider/selected space. World.Check itself delegates
to command handlers and does not provide those callback-specific scopes.

Negative independent content reproduced target Touch, sole clock resume and
selected point-space mutation returning normally from requirements, command and
plan previews. Original exceptions propagated but did not latch a terminal fault.
Baseline624/21 is direct evidence across all12mode/surface combinations.

One internal `ActivityDefinitionContracts.ObserveIntent` now invokes the actual
callback inside the EXISTING EntityObservation.ReadProvider, including whole
target state and selected space. WorkView, WorkBoard.CheckPlayerActivity and
PostServiceActivity migrate in place. No raw invocation remains at these three
owned admission sites. Authored CheckIntent implementations, requirement evaluator,
start rules and actual WorkBoard/resource/work/clock owners are unchanged. The
IPlayerActivityDefinition contract now explicitly documents purity.

The negative subtype extends the EXISTING independent declaration implementation
and parser. It reads explicit immutable JSON fault selection, with no mutable
shared content callback or another runtime owner/codec. Each fresh world still
owns its original state. Test-only injection happens only while explicitly paused;
positive player paths remain commands/time/immutable observations. No host/demo
gameplay is introduced.

Engine2.606.0→2.607.0 and work-view105→106 explicitly identify the changed
observation/refusal semantics. Independently authored protocol expectation updates
build identity only; save159/replay1 and existing state codec schemas unchanged.
No compatibility alias, duplicate evaluator or old-build reader.

## Build and runtime attempts

Build command:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

- First test-instrumentation build exit1/1.43s: CS0246, missing Hosting import for
  GameClock in the negative external proof. Added import; no runtime from failed build.
- Corrected build exit0/4.94s, zero warnings/errors.
- Pre-fix work-observation reproduction624/21,907ms, PID25120 exit1/1048ms.
  Nine mutation combinations returned without an exception/fault; three exception
  combinations threw but lacked the original fault latch. All21failures retained.
- Production repair build exit0/7.13s, zero warnings/errors.
- FINAL work-observation748/0,849ms, PID24332 exit0/986ms.
- FINAL graph-disclosure246/0,949ms, PID29260 exit0/1087ms.
- FINAL process-staffing102/0,6320ms, PID10652 exit0/6460ms.

Only three literal selectors ran, each using `--bounded-exact`,60second timeout,
5second termination limit and bounded output:

1. `Work observations: physical execution and terminal candidate plan query boundaries`
2. `Activity graph disclosure: nested requirement and node final effects stay private while earned work continues`
3. `Process staffing: automatic exact queue teams, retained duty, sibling contention and saved departure`

All runtime started after observed successful terminal builds. All children
terminated; no abort/timeout. Final sum1096/0, excluding earlier failed attempt.

## What assertions prove

Work case preserves original physical travel/earned inspection/hauling, saved
twins, public plan/player control, candidate fault boundaries and replay. New
12negative combinations verify guarded callback failure, first-fault identity,
empty diagnostic output, no earned time after attempted resume, own retained
healthy snapshot/space with empty deltas, no callback retry, refused healthy
save/hash and another session's unchanged actual hash. The previous hidden-target
diagnostic/observer/save checks also remain passing on this build.

Graph case retains physical discovery/earned work, revocation without cancelling
accepted execution, nested/node/final metadata privacy, saved twins, rediscovery
of original completion and canonical replay in30/60second compositions.

Process staffing case exercises the migrated service admission in normal physical
work: actual queue-bound automatic teams, retained duty, sibling contention,
saved continuation and departure. It demonstrates healthy service behavior,
not arbitrary-service fault or global performance coverage.

Core441 C# combined SHA256 now:
`ee6b7718a6e1047a526d2a771224fd9594b86d8c888fe8a36d8963bf4b782cc6`.

## Remaining scope

P4.acceptance.014 is still OPEN for its full evaluator/private projection matrix;
other plan callback protocols and arbitrary CLR purity are not proven here.
No original acceptance checkbox changes. Bookkeeping221/198recorded checked/
23tracked open unchanged; full objective active. Previous build evidence remains
historical, not silently rerun. No full/profile/family runtime, Oil/Tycoon/platform/
host/rendering edits or Git staging/commit/push.
