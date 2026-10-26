# P4-G45 actual verification — 2026-10-09

Current M101/native2.737/save161/replay2: original network case255/0/0 after actual
249/6 baseline. Fourteen fresh/restored faults preserve first causes; native/direct
complete0→28/1input/28checkpoint replay7FC1298055748CDC. No new topology/stock/clock.
[Every shared attempt, exact scope and handoff](P10_SIGNAL_CONTENT_CONTRACTS_VERIFICATION.md#current-m101-shared-inventory-network-and-utility-metadata-observation--2026-10-22).

## Earlier G45 evidence — 2026-10-09

Canonical repository: Beep.SimEngine. Core beep-simengine-2.388.0, save154, replay1. [Design](../../plans/enhancements/P4_NETWORK_SIGNAL_BINDINGS_DESIGN.md). Scope is one reusable network signal-binding seam, not full framework completion.

## Actual commands and terminal evidence

Slice build: `dotnet build Beep.SimEngine.slnx --no-restore`.

| Build | Exit | Elapsed | Warnings/errors |
|---|---|---|---|
| 1 | 0 | 14.98s | 0/0 |
| 2 | 0 | 6.16s | 0/0 |
| 3 | 0 | 4.73s | 0/0 |
| 4 | 0 | 4.69s | 0/0 |
| 5, final | 0 | 3.90s | 0/0 |

Build1 initially yielded session4399; that same handle was polled to terminal success. No running build remains.

Exact bounded invocation, and no other runtime case:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Network signal bindings: independent JSON observes timed flow, backpressure and physical hauling' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

| Attempt | Passed/failed/aborted | Suite ms | PID | Exit | Observed ms |
|---|---|---|---|---|---|
| 1 | 2/1/0 | 94 | 30328 | 1 | 242 |
| 2 | 2/1/0 | 123 | 43128 | 1 | 268 |
| 3 | 2/1/0 | 283 | 24852 | 1 | 460 |
| 4 | 165/2/0 | 791 | 25928 | 1 | 930 |
| 5, final | 169/0/0 | 777 | 30800 | 0 | 935 |

Every child was observed Exited, not an outstanding handle. No execution/termination timeout, aborted suite, output truncation or observer error. Only the SAME new case was rerun after repairs; old/family/full/Oil/Tycoon/platform runtime remained unexecuted.

## Repairs, not erased failures

1. Reused resource-recovery authoring included an unused supplier with an inactive Tycoon provider. The proof's authored resource-only selection excludes that entity; no Tycoon implementation was edited or activated.
2. The old Colony fixture referenced colony.tiles through its navigation documents while resource-recovery selected recovery.tiles. Replaced that fixture dependency with an explicit JSON master and only relevant native modules/documents; used original native uniform navigation. No compatibility alias or second gameplay owner.
3. Existing owned network layer encodes company0 as1, not0. Corrected authored startup ownership to1. Existing LineSystem.OnNewGame settles that authored geometry; the opening assertion now verifies available zero stock before Update, not fictitious pending topology. No gameplay stamping or stock mutation in the driver.
4. Existing public view returns NotOwned for the visible foreign-company entity, not NotFound. The proof asserts that exact refusal AND no available signal value. Privacy semantics were not changed.

## Narrow positive and negative coverage

Direct ComponentDef<NetworkSignal> implements INetworkSignalDefinition and delegates the same strict declaration, never NetworkSignalDef inheritance. Native/direct runs share actual chronology. JSON startup goes through original world generation/Initialize/OnNewGame. Engine Update earns four-unit/minute flow; original gate stops at eight because actual receiving room falls below four. Source22 plus pool8 conserves initial30.

Normal HaulResourcesCommand physically moves five units from the depot to the watcher after travel/loading/delivery time, not submission. Its local inventory becomes5 while its connected-pool signal remains8. Existing public diagnostics and exact scaled pair units agree. Private detached facts, original paused clock, saved twins during backpressure/hauling, completed restoration, replay and isolated session state are exercised.

Eleven unused malformed bindings refuse before Initialize: missing/empty/unknown/unsupported network, empty id, extra field, null network, schema disagreement, null/empty bindings and an unbound original runtime. Four injected live Network getters throw, pause the actual clock, Touch the actual entity or mutate its actual private sibling codec. Existing scoped observations latch the first fault; signal views/save/hash then refuse.

## Source evidence and remaining gates

Selected339 executable inputs were hashed AFTER final green at2026-10-09 17:52:20 UTC. This is a post-run inventory, not a pre-run freeze or full transitive source/binary/environment closure. Final tracker/audit validation verifies no later selected executable changes. No staging, commit or push.

Original221 compound checkbox requirements retain their exact text/states; line offsets only may be refreshed. Full P1–P12 is still open. Network content/policy and native gate dependencies, populated reservations, hostile port providers, topology changes, neutral/public breadth, recursion/private CLR behavior, global rollback, performance, lockstep/fuzz/soak and full acceptance were NOT proven. Earlier signal cases were compiled only, not rerun on2.388.0. G19 is unanswered; protected Tycoon files and GameClock are unchanged.
