# P4-G50 actual verification — 2026-10-09

Canonical Beep.SimEngine; core beep-simengine-2.393.0/save154/replay1.
[Design](../../plans/enhancements/P4_NETWORK_PORT_BINDINGS_DESIGN.md). Scoped authored/runtime connection seam, not full original goal/plan completion.

## Actual commands and builds

```text
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Network ports: independent JSON binds original timed push-pull, gates and physical hauling' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

FIVE build attempts: initial exit1/one CS0246/zero warnings in4.21s, missing existing Maps namespace import; repaired. FOUR later builds exit0/zero warnings/errors:12.69s,5.86s,4.47s,4.35s. Second yielded session46411, same handle polled to terminal; others completed directly. No live handles remain.

ONE NEW exact case, FOUR attempts; no other runtime case:

- 4 passed1 failed0 aborted in54ms; PID37648 Exited1, observed201ms. Case stopped on duplicate proof component registration.
- 192 passed13 failed0 aborted in1339ms; PID26288 Exited1, observed1487ms.
- 190 passed15 failed0 aborted in1177ms; PID22436 Exited1, observed1334ms.
- FINAL205 passed0 failed0 aborted in1169ms; PID39096 Exited0, observed1314ms.

All used30,000ms execution/5,000ms owned termination/30,000-character output limits. No timeout/truncation/observer error. Only FINAL proves current executable sources.

First proof failure registered an existing ExposurePrivateProbeDef a second time. Removed unused duplicate registration and reused its actual source component. Second attempt passed physical parity, positive push/pull and early refusals but custom fault injection was installed during Initialize before startup entities existed; copy fault was also active before the intended healthy first step. Third attempted OnNewGame installation still ran before pending generated spawns were flushed. Final instrumentation attaches faults at first Update, with clock-based activation after the healthy first step. Installer changes only negative callbacks; no normal transfer/game rules. Original GameClock/lifecycle was not changed to accommodate proof.

## Implementation and exact scope

Existing INetworkPortsDefinition now drives reusable Ports; native generic NetworkPortsDef base and Specs accessor deleted. Native/direct metadata share sole NetworkPortsDeclaration. Original custom proof also delegates it. Every loaded owner/connection definition, unused included, validates actual metadata/runtime/catalog aliases, resource/space/inlet/export requirements before Initialize. Frozen composition checks actual registered codecs; original reusable ports keep empty codec1.

Runtime flow, connected-quantity diagnostics and gate views use actual singular provider and immutable authored list aliases under existing provider/whole selected entity Sync/clock observations. Reject copied connection lists; no substituted metadata. Original LineService/pools/flow/demand/solver/resource/work/clock owners retain gameplay and persistence.

Native/reusable/custom normal chronology is identical: real source30/pool0 ->26/4 ->22/8 over two minutes, existing backpressure blocks catch-up. Ordinary real truck haul delivers five actual depot units to watcher local stock over movement/loading/unloading, depot45/pool8. Same private gate/quantity refusals, original pool/flow views, saved twins/pause/replay and shared-factory isolation succeed. Positive native/reusable pull earns nothing from same-step newly pushed stock, then watcher4 and8 with source18/pool4/watcher8 conserved30 after three updates; native cursors save/replay identically.

27 unused invalid definitions/authorings refuse before Initialize: null/default/duplicate/foreign connection metadata, invalid/unsupported/duplicate resource metadata, throwing metadata getter, missing list/network, empty/unknown network/resources, duplicate raw connections/push/pull, unsupported raw directions, extra instant field, original runtime without contract, contract without matching runtime, missing inlet/presence/wrong space. Nine live getter faults: metadata and custom throw/pause/Touch/private sibling plus custom copied list. All have healthy first step, rejected observation, refused save/hash.

## Source and remaining boundaries

Selected382 executable inputs hashed AFTER final green at2026-10-09 19:17:02 UTC; not pre-run/full source/binary/environment closure. No subsequent executable edits. Final audit verifies selected hashes, source anchors/local links, original221 gate text/states/offsets and protected files.

Bad original codec registration branch, dynamic connection policies, non-tile topology, line-effect contracts, all ownership/public/areas/congestion/promises/receipt/utility/recursive/nonparticipating private/global/performance/lockstep/fuzz/soak/full acceptance remain OPEN. Older cases compiled only; prior G49/G48 evidence historical, not rerun on2.393.0. G19 unanswered; protected Tycoon/GameClock/Oil/hosts/platforms untouched. Nothing staged/committed/pushed.
