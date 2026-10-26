# P11-A6: utility research through earned manufacturing — Design Document

## 0. Problem Statement

Original P11.acceptance.030 still lacks current combined proof for utility research, independent provider/effect breadth and actual transformation-unlock execution. Existing owners and reusable declarations exist; separate old suite results do not prove the combined physical workflow on current source.

## 1. Goals

1. Run native and independently authored technologies/utility requirements/completion effects through the same original WorkBoard and company knowledge ledger.
2. Prove shutdown, finite fuel shortage, physical refuel, prerequisite order, once-only private earned completion and save/pause/replay behavior.
3. Prove a refused locked recipe becomes executable only after earned research, then consumes inputs, retains reusable tools and manufactures one result over game time.
4. Keep normal test/host behavior to authored JSON and public input/time/facts only; invent no readiness, research, money or utility loop.

## 2. Design

Reuse actual UtilityResearchContractTests.Factory and its master-listed point/utility/technology/capacity/retry documents. Add only an optional pure world JSON edit. Existing WorkEffectContractsModule registers the already-existing IndependentTechnologyDef and exposes detached alias facts alongside its already-existing IndependentResearchCompletionDef. No new gameplay implementation or duplicated type is introduced.

New driver authors an unlocked finite-fuel lab.power process, utility-dependent foundation research, a dependent confirmation technology and a separately locked six-minute manufacturing recipe. Native/direct metadata and minute/half-minute time variants exercise real workers, service docks, stock, utility burn, interruption and earned unlocks. Existing module contracts remain authoritative and all completion delegates TechnologyCompletionContracts. Negative utility consumers use existing instrumentation only.

## 3. API surface

No production engine API/schema/behavior changes. Core2.434.0/save154/replay1 retained; new proof registration and immutable alias fact only. No second clock, research ledger, queue, scheduler or compatibility path.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | ../../tests/ExtensionProof/WorkEffectContractsProof.cs | 3 | low |
| Modify | ../../tests/Beep.SimEngine.Tests/UtilityResearchContractTests.cs | 2 | low |
| Modify | ../../tests/Beep.SimEngine.Tests/Program.cs | 1 | low |
| New | ../../tests/Beep.SimEngine.Tests/UtilityResearchAcceptanceTests.cs | 180 | medium |

## 5. Error handling matrix

| Condition | Expected original result |
|---|---|
| Unlearned transformation | TechnologyCodes.Locked in preview/actual command, no queue/product |
| Unlearned prerequisite | TechnologyCodes.MissingPrerequisite, no advanced order |
| Shutdown/finite fuel exhaustion | Paused original progress and utility consumption; no earned knowledge |
| Missing named consumer | Typed utility.missing-consumer, no substitution |
| Provider state/clock/identity/error fault | No successful facts or resumable save |
| Completed technology ordered again | TechnologyCodes.Known, no repeat grant/cost |

## 6. Backward compatibility

None; reuse original contracts/content/declarations/owners. No legacy path or duplicate proof definitions.

## 7. Verification

ONE NEW exact case: Utility research acceptance: independent earned completion unlocks physical manufacturing. FINAL240/0/0 in6976ms, PID40120 Exited0/7127ms observed, untruncated/no observer error. Clean solution6.48s and two same-case targeted compiles4.16s/3.85s. All3 attempts retained:2/1/0 missing authored executors,40/1/0 incorrect finished-node query,FINAL240/0/0. Only fixture repairs, production engine unchanged. Outer60000ms/termination5000ms/output30000 characters; waits700 Updates. [Evidence](../../docs/migration/P11_UTILITY_RESEARCH_ACCEPTANCE_VERIFICATION.md). No old/family/full/Oil/Tycoon/platform runtime or executable edits after FINAL.

## 8. Risks

Discrete fuel draw rounds at the existing native quantum; tests must account actual recorded consumption, never predict or replace its algorithm. Consumption facts belong to the utility execution and must be retained before cancelling that execution. Selected process-only/non-tile facts are not broad provider or performance proof.

## 9. Out of scope

Any original gate not covered by complete current evidence; arbitrary providers, research networks, remote sensors, Tycoon, Oil/platform/Draw and full P1-P12/publication before goal AND plan complete.

## 10. Sub-task execution order

1. [x] Inspect original utility/technology/unlock/effect/work owners and literal gate.
2. [x] Reuse existing independent definitions and pure JSON composition.
3. [x] Add one bounded combined public gameplay/negative case.
4. [x] Observe compile/runtime and retain all attempts.
5. [x] Only original P11.030 checked, literal text unchanged; all221 requirements/checkboxes validated,204 anchors shift seven lines.1378 source/13 quickstart anchors/683 executable inputs frozen at2026-10-11 11:50:11 UTC match without drift; links/master columns/thin driver/protected files verified.174checked47open/additional154; full goal/plan/publication remain OPEN.
