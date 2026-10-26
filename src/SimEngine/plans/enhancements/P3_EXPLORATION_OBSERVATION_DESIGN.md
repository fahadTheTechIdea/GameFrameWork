# P3-S28: exploration observation durability — Design Document

## 0. Problem Statement

`ExplorationContracts.Read`, contact callbacks and `ExplorationSystem` validation use `finally` verification which can replace a callback's original failure. Returned snapshot validation is outside the observed scope. `ExplorationView` can return missing/private answers from a faulted session before consulting the original fault owner.

## 1. Goals

1. Preserve the first actual callback error, including a callback that also mutates the one game clock.
2. Validate returned exploration identity/progress inside the original observation boundary; malformed facts latch the original session fault.
3. Reject cached views before missing/private/source shortcuts and publish no partial collection or snapshot after failure.
4. Preserve physical contact, reset/retain interruption, private knowledge, save/replay and independent sessions through ordinary public player commands.

## 2. Design

Use the existing `EntityObservation.ReadProvider` with `observeTargetState:true` for pure exploration observations, rather than nested `finally` guards. Actual definition admission and loaded source validation use that same owner. Contact presence, region, sensor, target and completed subject reads remain in their existing owners. Validate actual presence space and region result enums before interpreting blockers. Keep the legitimate `_advancing` cleanup and existing progress/ledger commits unchanged. No provider registry, native map requirement, duplicate scheduler, stock, knowledge, fault or clock owner.

## 3. API surface

Existing contracts, JSON and view shapes unchanged. Build2.455.0/save154/replay1; no compatibility path or payload schema change.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | `src/Beep.SimEngine/Standard/Exploration.cs` | 55 | medium |
| Modify | `src/Beep.SimEngine/Standard/ExplorationModule.cs` | 6 | medium |
| Modify | `src/Beep.SimEngine/Views/ExplorationView.cs` | 6 | medium |
| New | engine-side negative exploration source, authored JSON/module and public player driver | 230 | medium |
| Modify | build identity, registration, pure factory and staged evidence/tracker | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Callback throws and changes clock | Preserve callback exception; terminal original session fault |
| Pure callback changes clock without throwing | Original observation rejects and latches fault |
| Malformed snapshot/presence/region status | Reject inside original observation; no published facts |
| Cached faulted missing/private/source query | Reject before any refusal or successful prefix |
| Valid outside/unsupported geometry | Original typed blocker, no invented provider or fallback |

## 6. Backward compatibility

Development framework; no legacy shim. Original contact state payload and JSON remain authoritative.

## 7. Verification

ONE NEW bounded exact case: `Exploration observation durability: physical contact and first faults retain private immutable facts`. One solution compile attempt with necessary targeted repairs only. Physical direct/native contact minute/half, saved interrupted work and replay; later read throw/clock/clock-throw/malformed and composition/load clock-throw probes. Preserve every attempt. No historical/family/full run. P3.008/P4.014 broader requirements remain OPEN.

## 8. Risks

Do not observe legitimate progress writes as pure callbacks. Engine-side faults are explicit negative instrumentation, not host gameplay. Do not turn pure region geometry into a native TileMap dependency or claim universal CLR sandboxing.

## 9. Out of scope

Oil, platform/Draw, protected sole clock source/G19/advanced Tycoon, broad spatial/private/fault/performance/full gates, publication before the whole goal and plan finish.

## 10. Sub-task execution order

1. Inspect original owners and publish this stage.
2. Replace pure callback guards in place and gate staged public facts.
3. Add one JSON/public-player proof, compile and run that exact bounded case; retain attempts.
4. Record scoped evidence, reconcile tracker and freeze actual source/input hashes without changing broader gate claims.

Status: scoped implementation verified on2.455.0/save154/replay1. [Evidence](../../docs/migration/P3_EXPLORATION_OBSERVATION_VERIFICATION.md). Lifecycle skill supplies the staged ownership/evidence workflow; user restrictions override generic full-suite advice. Full goal and plan remain OPEN.

## Verification result

ONE NEW exact FIRST/FINAL449/0/0 in1869ms, PID40840 Exited0/process2031ms, no truncated output/observer error. Four native/direct binding minute/half physical contact flows: finite single-unit supply, actual movement, disabled/outside reset/retain interruption, paused partial/complete saves, private immutable facts, replay and independent session hashes. Minute final54/half96. Four later read failures and composition/load clock+throw preserve original first faults and reject cached private/missing/source shortcuts without fact prefixes. Initial solution5.58s failed on negative proof-only internal ContractIdentity access; necessary targeted6.19s clean after explicit identity refusal. Both attempts retained; one runtime after terminal clean compilation, no executable edits after FIRST/FINAL. Malformed presence/region-status admission is source-reviewed, not separately fault-injected. Broader P3.008/P4.014 and all full/global/provider/stress/publication gates remain OPEN; original221/184checked/37open unchanged.
## Metadata closure

Current closure:1568 source anchors,13 quickstart anchors,839 selected executable inputs; zero hash mismatches/post-pass executable drift.2760 paths/970 Markdown files/8592 local links, zero broken links;18 valid master rows;221 original literal gate/checkbox anchors exact and unchanged (184checked/37open), additional181. Thin driver boundary scan empty. git diff --check exit0 (34 existing line-ending-warning output lines),2748 dirty paths/zero staged. Sole clock/protected G19 hashes unchanged. Seventeen inputs added: thirteen existing dependencies (ExplorationView changed in this slice, twelve unchanged) plus four genuinely new executable proof/driver/JSON files. Fifteen source anchors added: nine existing dependencies plus six genuinely new files. Seven previously frozen executable inputs changed (three production); total four production files changed including first-admitted ExplorationView. No executable edit after FIRST/FINAL. Metadata integrity is not full gameplay/provider/acceptance evidence.
