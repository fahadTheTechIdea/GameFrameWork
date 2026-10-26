# P9-C15: retained physical retry acceptance

## 0. Problem Statement

P9.acceptance.051 remains open. Historical `HaulRetryTests` use mutable World and direct acquisition, so they do not prove that a player using the public boundary can retain interrupted cargo, claims and earned handling work on current HEAD.

## 1. Goals

Verified one new bounded case over twelve authored combinations: native/independent retry policy, one-minute/half-minute clock quantum, and partial-loading/loaded-travel/partial-unloading interruption. Physical conservation, retained job/carrier/endpoints, bounded backoff, saved inbox/partial continuation, pause, immutable private facts, replay and session isolation pass. This is an additional scoped deliverable, not closure of either original compound recovery/durability gate.

## 2. Design

Reuse the existing pure `HaulRetryTests.Factory` authoring method; expose only that method internally. Its original selected retry replacement had discarded required authored executors: retain the explicit `work-retry.json` declarations and replace only the selected document sections, still through the same master-listed document. Do not invoke any historical runtime method. Reuse `LogisticsPolicyContentTests.Input`, `Step`, `Until`, `Partial`, `Id`, `Stock`, `Supply` and `Jobs` as thin public player drivers. The independent ConstantRetryProfileDef now directly implements IWorkRetryPolicy rather than requiring a native retry base. The new acceptance driver issues only normal commands and reads immutable engine facts. Actual WorkBoard, haul receipts, handling, operation gates, spatial provider, resource stock and sole clock remain authoritative.

## 3. API surface

No production API or protocol change. Public commands: `HaulResourcesCommand`, `SetPointAccessCommand`, `ConfigureOperationCommand`, and `ConfigureWorkPolicyCommand`. Existing work/logistics views, snapshots, saves and replay expose the results.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | `../../tests/Beep.SimEngine.Tests/HaulRetryTests.cs` | Low: pure factory visibility only |
| Update | Same pure factory's selected retry document composition | Low: preserve actual authored executors, no runtime helper migration |
| New | `../../tests/Beep.SimEngine.Tests/RetainedRetryAcceptanceTests.cs` | Low: player-only acceptance driver |
| Modify | `../../tests/Beep.SimEngine.Tests/Program.cs` | Low: one exact registration |
| Modify | `../../tests/ExtensionProof/WorkRetryProof.cs` | Low: independent metadata implements IWorkRetryPolicy directly, not a native retry base |
| Update | master, completion audit and evidence | Low: actual scoped evidence only |

## 5. Error handling matrix

Unavailable operation must retain earned partial handling and original reservations. Closed point-space access must retain loaded stock and produce the actual unreachable blocker. A disabled work policy must suppress retries without fabricating failures. Restoration must respect the saved deadline and resume physical work; no teleportation, grant or replacement ledger is permitted.

## 6. Backward compatibility

No compatibility code, duplicated owner or legacy fallback. Existing protocol remains unchanged unless a real engine defect requires an in-place repair.

## 7. Verification

One solution compile and ONE NEW exact bounded case, maximum60000ms, termination5000ms and captured output30000chars. Same-case repair builds/runs only if needed. Every physical wait is bounded at512Updates with actual job diagnostics. No historical/family/full runtime. The full .051 and .052 requirements stay open: literal endpoint/road removal, fuel exhaustion, actual breakdown, output blocking, queues and paused-plan matrix are not substituted by this proof.

Actual FINAL7008passed/0failed/0aborted in4031ms, owned PID33120 exited0/4235ms. Solution compile26.74s and targeted repair compile12.14s both clean; first same-case attempt refused stale fixture composition at content loading,0passed/1failed/1aborted243ms, PID26356 exited1/475ms. No production change or executable edits after FINAL. [Evidence](../../docs/migration/P9_RETAINED_RETRY_ACCEPTANCE_VERIFICATION.md) records the exact commands, limits and publication boundary.

## 8. Risks

The point provider's access gate is not road removal. An explicit operation disable is not equipment damage. These distinctions remain explicit in the tracker. A finer quantum changes physical throughput and retry step counts, never creates another clock. Claims are observed through actual original diagnostics, not recreated by the driver.

## 9. Out of scope

OilFieldDays, Tycoon, business, platforms, Draw, renderers, adapters, new transport media, protected G19 files and broad validation. No staging, commit or push before the whole original goal and plan finish.

## 10. Sub-task execution order

1. Document scope and preserve original open gates.
2. Reuse pure authoring and implement the public-boundary driver.
3. Compile and execute the single bounded new case; retain every attempt.
4. Record actual evidence and refresh tracker/source anchors without claiming the compound recovery gate complete.
