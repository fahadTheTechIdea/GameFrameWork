# P4-G24 named signal bindings — scoped evidence

2026-10-09, core2.359.0/save154/replay1; persisted state and view schemas unchanged.
Full P1–P12 and active goal OPEN; no staging, commit or push.

## Original owners and boundary

Existing EntityDef owns one authored Code-to-component-slot index. Definition lookup,
capability checks and Entity.Get reuse those original slots without live identity getters.
Final ComponentDef validation rejects resolved identity drift and signal-schema aliases,
including unused authored entities. No runtime registry, new entity IDs or saved lookup state.

Existing IAutomationSignalDefinition exposes its actual registered SignalSchema. Existing
AutomationSignalContracts selects actual named metadata and guards source/descriptor reads.
AutomationSystem, SignalGateCondition, ExecutionGateContracts and OperationsView signal-schema
projection use that owner; no runtime Component/Def schema cast remains in these signal paths.
Original missing/incompatible unavailability, provider/schema identity, callback validation and
first-fault semantics remain. Actual signal callback failures retain operation.signal; metadata
read failures use the existing observation.provider fault.

Components still own per-entity state and Sync. This does not authorize arbitrary non-Component
parts, provide a CLR sandbox or close complete operation/capability decomposition.
TryGetOperationSchema's concrete selector is still open. No separate clock, scheduler, schema
store, signal math, recipe execution or compatibility/native fallback was added.

## Public extension proof / thin player

One explicitly JSON-authored metadata_source has original OperationSettings, named metadata,
actual registered signal schema and ordinary saved control state. Its transient getter counters
prove selection and schema contract use; deliberate getter faults test existing observation.
Existing shared recipe factory appends explicit modules/master documents in place; no second
content loader. The player driver only authors inputs and uses public commands, views, Update,
clock, saves and replay; no mutable World, stock or progress writes.

## Actual verification

Compilation: dotnet build Beep.SimEngine.slnx --no-restore.
First build failed in8.05s: one proof referenced internal ResolvedContent.ComponentDeclaration.
Replaced it with the other actual entity's public named metadata schema; no new API/access shim.
Repaired build succeeded in9.33s, zero warnings/errors.

ONLY ONE NEW exact bounded runtime case was executed, ONCE:

```text
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- --bounded-exact "Signal bindings: authored named metadata drives gates automation and physical production" --timeout-ms 30000 --termination-timeout-ms 2000 --output-chars 24000
152 passed, 0 failed, 0 aborted;1448ms.
Child pid42188, exit0; observed1614ms; no truncated output or observer error.
```

64 executable/source-fixture inputs frozen2026-10-09 08:52:09 UTC, verified unchanged afterward.
Actual scope: stable named selections without identity-getter execution; actual schema getter;
pure/private/retained signal/schema/gate facts; missing/incompatible typed refusal; original
Boolean production gate and deferred native hysteresis/dwell/cooldown automation; physical
four-unit hauling, thirty-minute manufacturing, conserved reusable tool, one result; disabled
and unsafe work paused; saved twins, one-clock pause, replay and settled save identity.

12 metadata-fault combinations (signal/schema/gates/automation preview × exception/pause/identity
allocation) reject partial facts and retain first fault; no healthy Update/save/hash/read retry.
Eight authored malformed variants refuse before Initialize: null/foreign schema, identity drift,
unused invalid provider, duplicate identity, missing/incompatible source, provider schema mismatch.

No historical/family/full, OilFieldDays, Tycoon or platform runtime. Earlier G23 and all older
runtime evidence is historical, not rerun on2.359.0. Complete operation decomposition, arbitrary
CLR/provider metadata sandbox, spatial intent deep immutability, all external codecs, multi-lane
staffing/network/continuous/technology/removal/concurrency/lockstep/fuzz/soak/performance and
global acceptance remain unproved. Pending excluded Tycoon type-binding authority unchanged.
