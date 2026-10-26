# P4-G19b resource snapshot boundary — scoped evidence

2026-10-09; core2.352.0/save154/replay1. Resource catalog return migration remains pending.
Full P1–P12/goal incomplete; no staging/commit/push. Lifecycle skill guided staged design/code/evidence,
with user/repository focused-test limits overriding its full-regression recommendation.

## Existing engine boundary changed

Hosting/PlayerView.cs ResourceInfo now captures source trait/measure collections once, copies their
values, and exposes readonly detached collections. Quantity and capacity unit metadata remain copied
scalar UnitInfo facts. No executable provider, mutable source list or second catalog escapes.

CopyResources uses original EntityObservation.ReadProvider around each actual definition projection,
retaining original content ownership/visibility and staging all results before publication. Exceptions
or detected clock/identity mutation latch original observation.provider fault. An earlier successful
resource or stale caller result cannot survive a later read failure. No new rollback/observation owner.
Faulted partial session state cannot update, save or hash normally; no compensation or callback retry.

No catalog return type, resource ID, inventory/flow/reservation/job, UnitConversion, GameClock,
persistent state/view schema, optional Tycoon/default money/business, Oil or platform implementation change.

## Actual build and runtime

`dotnet build Beep.SimEngine.slnx --no-restore`: succeeded7.44s, zero warnings/errors.
One NEW exact bounded case, first/final run, no repair/rerun or old/family/full/Oil/Tycoon/platform runtime:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- --bounded-exact "Resource snapshots: detached metadata and guarded atomic publication" --timeout-ms 30000 --termination-timeout-ms 2000 --output-chars 24000
```

90passed/0failed/0aborted in455ms; childPID7880 exited0/observed601ms. Output untruncated, no observer
error. Child bounds30000ms/termination2000ms; healthy continuation exactly12 Updates, six finite fault
variants. 40 named executable inputs frozen2026-10-09 06:56:57UTC; no post-pass executable edits.

## What the case proves

- Framework-only projection instrumentation uses an IResourceDefinition with mutable metadata lists
  and units, NOT a registered independent resource, stock owner or host gameplay implementation.
  The ACTUAL ResourceInfo constructor preserves original traits, measures, quantity/measure unit values
  after source changes. Returned lists reject caller mutation.
- Explicit master-listed resource-snapshots.json supplies actual unit contracts and native resources.
  ResourceSnapshotModule registers unit content and a normal IVisibility; transient fault callbacks
  only instrument exceptions/clock mutation/entity identity allocation, not gameplay.
- Healthy player reads show the authored quantity/measure units as detached facts; one viewer cannot
  see/find snapshot.hidden while another may, through the same existing engine predicate.
- Observations preserve hash/time. One-clock pause, save/load12-Update twins, retained metadata, replay
  and independent interleaved session remain identical. This case runs no physical hauling/manufacturing.
- Six late nested-provider fault variants: quantity unit and measure unit each throw, pause the sole
  clock, or allocate an actual entity identity. Every read throws, clears the full caller list and
  retains original fault; update/save/hash/retry refuse and callback is not called again.

## Limits / remaining work

This guard checks its existing scoped clock/identity/provider facts; it is NOT a sandbox for arbitrary
CLR execution, undisclosed metadata mutation or all remote state. It does not make mutated faulted
state resumable. Framework direct-copy fixture does not prove independent content catalog registration.

IResourceContentDefinition/catalog return types/consumer migrations/actual metadata ownership validation
remain OPEN pending the three excluded Tycoon type-only bindings. Full direct-resource hauling/WIP/
manufacturing/reservations/lockstep/fault/stress/performance/full acceptance remains OPEN.
G19a/G18 and older runtime evidence is historical, compile-only on this build, not silently rerun.
All221 original primary acceptance requirement texts/checkboxes retained unchanged.

[Stage](../../plans/enhancements/P4_RESOURCE_CONTENT_CONTRACTS_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[audit](../../plans/enhancements/COMPLETION_AUDIT.json).
