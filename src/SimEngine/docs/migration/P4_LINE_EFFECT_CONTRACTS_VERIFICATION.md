# P4-G51 corridor completion contract evidence — 2026-10-09

## Scope and current protocol

Core `beep-simengine-2.394.0`, save154/replay1. One focused immutable effect contract and JSON declaration replace native-required corridor effect references. Original LineService, physical effect dispatch, WorkBoard, work graph, inventory/claims, map/cursor, recovery and sole GameClock remain authoritative. No second effect registry, stock, completion or scheduler.

## Build — actual terminal evidence

Command: `dotnet build Beep.SimEngine.slnx --no-restore`.

ONE attempt: initial session5864 observed running; the same handle completed with exit0, **15.47 seconds, zero warnings and errors**. No repair or rebuild. Historical segment-test lookups migrated to the effect interface and compiled; their old cases were not executed.

## New exact bounded case — actual terminal evidence

Command:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Line effects: direct master JSON earns physical segments and whole-order completion" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

ONE attempt, FIRST/FINAL: **136 passed, zero failed, zero aborted suites,1715ms**. Owned process **PID28340**, terminal `Exited`, exit0; observer elapsed1870ms. stdout/stderr not truncated, no observer error or timeout. Runtime timeout30,000ms, owned termination budget5,000ms, output30,000 characters.

Only this new exact case ran; no historical, broad family, full, Oil, Tycoon or platform runtime. No count is asserted as whole-plan/full-regression evidence.

## Proven behavior

- Native and direct `Def` implementations bind actual ILineEffectDefinition/IWorkEffect catalog aliases and complete the same physical chronology through shared engine algorithms.
- Public commands create intent, not stamped geometry. Future-segment and whole-order previews refuse unearned completion without mutation; foreign company readiness refuses.
- Actual machines travel and run parallel preparation/inspection, retain held tools and wait before terminal work. Original stock40 becomes38 for one earned segment,34 for three, then32 after another earned prefix and ordinary cancellation.
- Future geometry stays absent, earned geometry survives cancellation, and final completion retires the original cursor. No extra completion/stock state is created.
- Partial/completed/cancelled saves, continued saved twins, detached progress, clock pause, interleaved session isolation and original command/clock replay succeed.
- Eight unused malformed effect cases refuse before Initialize: missing/empty/unknown operation, extra instant field, invalid enum, mismatched Id/Module aliases and throwing metadata getter.
- Six effect-position/reference cases refuse before Initialize: early segment, terminal finish, final segment, unknown final, unused finish-node and unused segment-final activity.
- Four intentionally armed negative selected callbacks throw, pause, Touch the actual cursor or mutate original LineWork.Order without Touch. Existing selected entity/clock/codec observations fault; save/hash then refuse. These probes are not normal gameplay providers.

## Frozen selected executable evidence

**386 selected source/proof/test/project/JSON inputs** hashed AFTER final green at **2026-10-09 19:32:02 UTC**. Thirteen are new/changed versus the previous selected inventory; all changes occurred before the green build/case. This is selected-input evidence, not complete transitive-source closure or a pre-run capture. No executable edits afterward.

Protected GameClock and three excluded Tycoon files remain unchanged. No hosts/demos/renderers/platform code changed. No staging, commit or push.

## Remaining gates

Stocked cursor evacuation/recovery, owned/foreign geometry, retries/dynamic topology, arbitrary callback/private-state/global rollback, all providers, areas/congestion, non-tile protocols, performance/allocation, lockstep/fuzz/soak and full original acceptance remain OPEN. Older proofs are historical, not rerun on2.394.0. G19 authority remains unanswered. Original221 compound gate text and checkbox states are preserved; scoped proof does not close them. Full goal active.

[Stage design](../../plans/enhancements/P4_LINE_EFFECT_CONTRACTS_DESIGN.md), [master tracker](../../plans/enhancements/MASTER_TRACKER.md).
