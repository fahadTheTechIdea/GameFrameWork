# P4-G48 actual verification — 2026-10-09

Canonical repository Beep.SimEngine; core beep-simengine-2.391.0/save154/replay1.
[Design](../../plans/enhancements/P4_NETWORK_DEFINITION_CONTRACTS_DESIGN.md). Scoped network content seam, not full P1-P12 completion.

## Actual commands and terminal evidence

```text
dotnet build Beep.SimEngine.slnx --no-restore
```

THREE clean builds, zero warnings/errors, exit0:14.58s,9.03s,6.31s.
First yielded session19648; same handle polled to terminal. Later builds completed directly. No live build remains.

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Network definitions: independent master JSON drives original pools, gates and physical hauling' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

ONE NEW exact case, THREE attempts; no other runtime cases:

- 147 passed,1 failed,0 aborted in822ms; PID34888 Exited/exit1, observed959ms.
- 148 passed,0 failed,0 aborted in917ms; PID4028 Exited/exit0, observed1071ms.
- FINAL150 passed,0 failed,0 aborted in831ms; PID13948 Exited/exit0, observed969ms.

All used30,000ms execution,5,000ms owned termination,30,000-character output limits. No timeout, output truncation or observer error. Only FINAL is current executable evidence.

First failure was an actual engine boundary bug: foreign LineDef with unset ID reached ResolvedContent.Owns dictionary lookup and raised ArgumentNullException, not typed ContentException. Shared RequireAlias now checks raw Id/Module before dictionary access. Second run proved that repair. Source review then found Public access getter was not explicitly observed by the shared validator; it now is, with one unused public-throw authoring adding two assertions. Third build/run verifies those final sources. No executable changes after final green.

## Implementation and exact scope

INetworkDefinition replaces concrete network types across ports, signals, gates, diagnostics, immutable views and original LineService. NetworkDef is only native JSON registration. Independent Def/INetworkDefinition delegates sole NetworkDeclaration; normal JSON, resource selector and rate semantics are not copied into tests. Actual master-listed document registers independent network content with the same existing line/capacity/policy aliases.

One shared metadata validator admits actual network/reference Id/Module/catalog aliases, matching unique ordered native resource/rate lists, valid periods, original capacity compatibility and exact selector identities. Every loaded definition, unused included, validates after references and before entity ports/Initialize. Existing observations guard actual frozen aliases and selected entity/provider/clock facts on gates/signals/flow rate/readiness/ranking/demand and pool resource compatibility. Original topology, inventories, receipts, elapsed time, success turns, work and clock own all gameplay.

Native/direct physical chronology matches: startup source30/pool0; first minute source26/pool4; second source22/pool8. Same original signal gate blocks further flow without accumulating catch-up credit. Ordinary HaulResourcesCommand sends a real truck to collect/load/travel/deliver five depot units; submission gives no immediate stock. Depot50 becomes45, watcher5 is local inventory, pool remains8 and source/pool total30. Original pool/flow diagnostics show one actual component. Saved twins, completed restoration, sole-clock pause, command replay and isolated shared-factory session use existing engine paths.

Thirty-seven unused malformed definition authorings/implementations refuse before Initialize: null/foreign line; null capacity; missing/empty/duplicate/inconsistent resources/rates; null policies; fabricated/extra/copied selector results; throwing Public metadata; missing/empty/unknown five references; missing/empty/duplicate raw rates; fractional discrete native quantity; zero period; extra instant_flow field. Four live Rate callbacks throw, pause clock, Touch source or corrupt its private sibling codec; first earned step succeeds, next faults and subsequent pool views/save/hash refuse. Negative instrumentation never computes normal gameplay.

## Source and remaining boundaries

Selected363 executable inputs hashed AFTER final green at2026-10-09 18:41:29 UTC; not a pre-run freeze/full source/binary/environment closure. Final audit checks actual selected hashes, source anchors/links, original gate text/states/offsets and protected files.

Native LineDef/corridor/ports authoring and non-tile network support remain unfinished. Wider utility/custom/public-neutral/topology/congestion/promise/receipt/provider recursion/nonparticipating private/global rollback/performance/lockstep/fuzz/soak/full acceptance stay OPEN. Startup geometry is not physically constructed corridor evidence. Original221 compound gates retain text/checkboxes. G19 authority unanswered; protected Tycoon, sole GameClock, Oil/hosts/platforms untouched. Nothing staged/committed/pushed.
