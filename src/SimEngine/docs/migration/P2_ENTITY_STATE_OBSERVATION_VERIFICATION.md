# P2-F3b actual source-codec entity state observation — scoped evidence

2026-10-09, core2.363.0/save154/replay1; entity/provider gameplay state schemas unchanged.
Full P1-P12 and active goal OPEN. No staging/commit/push.

## Original loop and state authority

Entity.Update still owns engine-issued once-per-update context, source collection, authored priority,
ordinal catalog-ID tie-breaking, default fallback, state transition/version and state-since. World.Step
still invokes entities after systems/events, before completion and the sole clock commit. No new loop,
clock, entity state store, resolver, host gameplay decision, renderer or Draw implementation.

Entity.Update now captures/verifies the actual source Component.Sync through the original
EntityObservation.ProviderScope, not only scalar entity/clock observation. ProviderScope accepts the
actual owner's internal fault phase; ordinary readers retain observation.provider, entity sources
retain entity.state:id:key[slot]. Callback exceptions, hidden source payload changes, clock/identity/
version mutation and foreign state references fault before selected state or time commits. Nested
recursive entity/game faults retain their first specific diagnostic even when swallowed by a source.

The observation hashes only the selected source's existing codec and checks original scoped entity/
clock facts. It is not a world hash, arbitrary CLR/content sandbox, or proof for undeclared cross-provider
hidden mutations. No additional serialized chronology or saved permission is introduced.

## In-place proof migration and thin player

Existing EntityBoundaryProbe is reused. JSON now explicitly authors active state; its one Boolean
is actual provider state with codec2. Reads/LastContext remain transient diagnostic instrumentation,
excluded from Sync. Existing entity-boundaries.json authors all four active values; original declaration
uses codec2. Existing lifecycle SignalA/SignalB keep authoritative Booleans but no longer serialize
Reads, with explicit schema2 declarations. Those historical suites were compiled only, not rerun.

IndependentConditionDef gains only a transient states-getter probe. Existing policy/condition/recipe
master authoring helpers are extended in place to select the new proof module. Per-session diagnostic
views reference actual existing providers and inject deliberate faults only; they implement no healthy
work, state transition, resource, travel, timing, repair or policy rule.

ONE NEW thin-player case uses normal commands/one clock/Update/retained engine snapshots/immutable
condition views/save/replay. It proves machine travel, positive-time stress, damage state in the same
completed Update with actual change timestamp, unchanged state-since, snapshot deltas, real spare/tool
hauling and thirty-minute repair, next-update restart/default fallback, three spares consumed/tools
conserved, retained snapshots, saved in-travel and repair twins, pause, interleaved isolation, replay,
settled save, source visit exactly once per completed Update and shared clock identity.
Saved source counters/context reset because they are instrumentation, not gameplay authority.

Two reversed independent condition declaration arrays select the same ordinal catalog-ID priority
winner; zero initial condition selects authored failed state through normal Update. Three invalid
default/retained/foreign contexts execute no callbacks and create no invented active-update fault.

Ten direct source faults (exception, hidden Boolean mutation, clock pause/speed, Touch, identity
allocation, recursive/swallowed entity and game Update), one foreign state reference and four
native authored state-getter faults exercise source-specific diagnostics and first-fault cutoff.
No selected state/time commits, later source/entity callback, healthy retry/hash/save or retained
snapshot mutation. Native condition metadata sources precede the probe sources in the actual
registered order; exact zero diagnostic reads confirms cutoff there.

## Actual compilation and bounded runtime

Three clean compilation builds:
dotnet build Beep.SimEngine.slnx --no-restore.
Actual elapsed:11.22s,4.76s,3.99s; zero warnings/errors.
Initial build returned live exec session37237; same handle was polled to confirmed successful exit.

Only ONE NEW exact bounded case was executed, three times:

~~~text
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- --bounded-exact "Entity state observation: authored physical transitions and codec-pure update sources" --timeout-ms 30000 --termination-timeout-ms 2000 --output-chars 24000
~~~

1.13 passed/1 failed (suite exception) in1099ms; pid41524 exit1, observed1283ms.
  New proof used nonexistent point.moving instead of actual authored point.travel; bounded helper
  stopped after480 updates with step482/Idle/operation.disabled/source20/cargo0 diagnostics.
2.223 passed/12 failed in1772ms; pid20248 exit1, observed1940ms.
  Gameplay and faults passed; native condition state sources actually precede probes, so four fault
  cases correctly had zero probe calls, not the assumed one. Fixed source-order assertions only.
3.Final235 passed/0 failed/0 aborted in1742ms; pid41840 exit0, observed1896ms.

All process output untruncated, no observer errors. Final91 executable/fixture inputs frozen
2026-10-09 10:09:53 UTC and retained unchanged after final. No executable edits or runtime after final.
No old/full/family/Oil/Tycoon/platform runtime. Counts are scoped evidence, not full completion.

## Remaining acceptance

G27 and all older executable evidence historical, not rerun on2.363.0. Original proof A/B and old
boundary suite behavior, every native/extension codec and state source, whole CLR/world/provider
sandbox, undeclared cross-provider hidden mutation, full mutating transactions, spatial intent deep
immutability, multi-lane/network/continuous/technology/removal/concurrency/lockstep/fuzz/soak/
allocation/performance and all P1-P12 requirements remain unproved. Existing pending excluded Tycoon
catalog type-binding authority is unchanged. No business/default/domain/platform/Draw/clock change;
no publication.
