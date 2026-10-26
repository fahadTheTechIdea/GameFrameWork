# P4-G47 actual verification — 2026-10-09

Canonical repository Beep.SimEngine, core beep-simengine-2.390.0/save154/replay1.
[Design](../../plans/enhancements/P4_NETWORK_POLICY_CONTRACTS_DESIGN.md). Scoped proof only; full P1-P12 remains OPEN.

## Actual commands and terminal evidence

```text
dotnet build Beep.SimEngine.slnx --no-restore
```

ONE build succeeded in15.11s, exit0, zero warnings/errors. Initially yielded session11487; the same handle was polled to terminal completion. No build handle remains live.

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Network policy contracts: independent JSON retains timed flow, physical hauling and bounded callbacks' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

ONE NEW exact case, ONE attempt: FIRST/FINAL143 passed,0 failed,0 aborted in901ms.
Child PID39156 was observed Exited/exit0 in1067ms. No execution/termination timeout, output truncation or observer error. No runtime repair/rerun. Historical cases were compiled, not rerun.

## Actual engine changes

Three required native abstract policy bases are deleted. NetworkDef resolves INetworkFlowPolicy/INetworkSolver/INetworkDemandPolicy, now immutable content contracts. Native definitions and existing independent proofs implement them directly. Shared engine algorithms own standard round-robin ranking, capacity demand and bounded transfers; new proof definitions delegate rather than calculate gameplay in the harness.

Original LineService composition validates every loaded policy alias and all actual solver time callbacks, including unused content, before Initialize. Original entity observation guards protect callback clock/identity facts. FlowReadGuard observes actual whole participating entity codecs and original embedded pool inventory codecs. Exact accepted engine receipts alone refresh baselines; unrelated participating private changes fault. No blanket full network checkpoint hash is attempted during an unfinished update. No new catalog/store/queue/clock/checkpoint or compatibility implementation.

## Exact runtime scope

Native and independent JSON policies select the same original engine modules and have identical physical chronology. Startup pool is empty; each game minute earns four steel units. Actual source30 becomes26/pool4, then22/pool8. Authored backpressure freezes flow during four saved updates, with no blocked-time catch-up credit. Pausing the sole clock freezes physical work/flow.

An ordinary HaulResourcesCommand delivers five real depot units to the watcher through truck travel/loading/unloading. Submission is not instantaneous. Depot50 becomes45; local watcher stock5 never becomes pool stock. Original source plus pool remains30. Interleaved saved twins, completed restoration, command-stream replay and an untouched shared-factory session verify their original paths. Retained demand/solver capabilities cannot read/transfer after expiry.

Unused independent solver invalid-time and actual clock-pause callbacks refuse before Initialize. Eighteen live injections cover ranking/demand/solver callbacks, each throwing, pausing, touching source, changing source private codec, changing another participating peer's private codec, or taking actual pool stock outside the authorized solver context. First healthy earned update succeeds; next callback faults. Pool views/save/hash then refuse. Peer fault setup explicitly declares a receiving connection so the peer is actually a participating owner. These are negative instrumentation, not proof-owned gameplay.

## Source and completion boundaries

Selected353 executable inputs were hashed AFTER green at2026-10-09 18:25:16 UTC. No executable edits followed. This is a post-run selected inventory, not a pre-run freeze or full source/binary/environment closure. Actual machine-readable audit records source anchors, original gate offsets, process evidence and this limited scope.

Original221 compound requirements retain their text/checkboxes; counts are not a completion percentage. Native NetworkDef/LineDef migration, all utility/custom solver/native/public-neutral/topology/promise/receipt matrices, provider recursion/nonparticipating private state/global rollback, performance, lockstep/fuzz/soak and full acceptance remain unproven. Startup geometry is not physical corridor construction. G19 unanswered; protected Tycoon and sole GameClock unchanged. Nothing staged, committed or pushed; publication requires actual whole-goal AND plan completion.
