# P10-U1: utility actual-owner observation — Design Document

## 0. Problem Statement

UtilityContracts guards the consumer codec but not sibling persisted state. OperationsView reads interface Entity getters before starting observation. A faulty extension can mutate a sibling without Touch and return apparently healthy utility facts.

## 1. Goals

Observe the actual engine-bound entity before utility identity, port and demand callbacks. Publish no facts from a fault. Preserve native timed consumption, resource balances, private views, save/load, replay and the sole clock.

## 2. Design

The existing EntityObservation guard resolves its existing Component codec owner when whole-target observation is requested without an explicit target. UtilityContracts requests this scope for demand and complete binding observations. OperationsView supplies the entity it already owns, never an early interface getter. No inventory, solver, utility loop, scheduler or provider registry is added.

## 3. API surface

Public contracts and serialized payloads unchanged. Build2.457.0/save154/replay1; no legacy compatibility.

## 4. Files to change

Original EntityObservation, UtilityContracts and OperationsView; engine identity; one engine-side negative component/module, authored JSON/module files, one thin public-player case, registrations and evidence/tracker.

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Getter, demand or port callback changes sibling codec | Original first-fault latch; no healthy snapshot |
| Callback changes clock then throws | Preserve original exception |
| Faulted missing/private query | Original health gate rejects |
| Inactive or physically empty consumer | Existing typed inactive/shortage; no invented supply |

## 6. Backward compatibility

No compatibility shim or second state model. Existing codecs unchanged.

## 7. Verification

ONE NEW exact bounded case: native/direct requirements with minute/half clock, real production consumption, shutdown, shortage and physical refuel, private detached facts, saved twins, replay and isolated sessions. Explicit authored negative getter/demand/port sibling mutations exercise core reads during Update and cached public views. Compile once, then only necessary same-case repairs; retain failures. No old/family/full run.

## 8. Ownership

EntityObservation owns observation. Transformer/OperatingInputs own exposure and consumption. Inventory owns stock. World owns first fault. GameClock owns time. Driver only supplies JSON/player commands, Update and assertions.

## 9. Non-goals

Broad P10.003/P11.026/P3.008/P4.014, network accounting, all utility providers, arbitrary CLR-field purity, deltas, stress/full acceptance, Oil, platform/Draw, protected G19/Tycoon, commit/push before full goal and plan finish.

## 10. Sub-task execution order

Inspect existing owners; refactor scopes in place; add one authored proof; compile and exact bounded run; retain evidence and freeze current metadata without checking broad gates.

Status: scoped verified on2.457.0/save154/replay1; [evidence](../../docs/migration/P10_UTILITY_OWNER_OBSERVATION_VERIFICATION.md). Lifecycle skill supplies staged ownership/evidence; user no-full-suite restriction takes precedence. Full goal and plan remain OPEN.

## Verification result

ONE NEW exact FINAL708/0/0 in5711ms; PID18976 Exited0/process5871ms, no output truncation/observer error. Four native/direct utility requirement minute/half physical research flows exercise travel, material use, shutdown, finite shortage, eight supplied through ordinary hauling, completion, conservation of36 opening stock, saved twins, privacy, detached facts, replay and interleaved isolation. Sixteen active-consumer negative variants: getter/demand/port sibling codec mutation and clock-throw, each through core Update and cached public view, each minute/half. All terminal faults withhold facts and reject private/missing utility/operation/control/schema reads, Save/Hash/Update. First thrown callback retained.

Clean solution12.52s and two necessary clean targeted compiles4.82s/4.97s; all retained. FIRST35passed/1failed1469ms (terminal completed node no longer exists); SECOND212passed/2failed5857ms (idle negative port probe had no resource bindings); fixes use retained node facts and ordinary enqueue before negative reads. No engine repair after initial clean build. No executable edit after FINAL; no historical/family/full runtime. Original221/184checked/37open unchanged; broad P10.003/P11.026/P3.008/P4.014/full/global/provider/stress/publication remain OPEN.

## Metadata closure

Current metadata closure:1580 source anchors,13 quickstart anchors,847 selected executable inputs; zero SHA mismatches/post-pass executable drift.2772 paths/974 Markdown files/8598 local links, zero broken links;18 valid master rows; all221 literal gate/checkbox anchors exact and unchanged (184checked/37open), additional183. Thin driver boundary scan empty. git diff --check exit0 (34 existing line-ending-warning output lines),2760 dirty paths/zero staged. Sole clock/protected G19 unchanged. Six new files (four executable proof/driver/JSON, two stage/evidence) and seven changed frozen executable inputs (four production); consuming dependencies already frozen. No executable edit after FINAL. Metadata integrity is not full gameplay/provider/acceptance evidence.
