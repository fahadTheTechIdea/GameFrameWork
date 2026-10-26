# P2-F14: format-bounded state codec verification

Current identity: **beep-simengine-2.450.0 / save154 / replay1**.
Stage: [design](../../plans/enhancements/P2_STATE_CODEC_BOUNDS_DESIGN.md).
Full goal/plan remain active; no staging, commit or push.

## Actual implementation

The original `Kernel/StateSync.cs` no longer rejects a complete UTF-8 string
above1MiB or a nonnegative count above16,777,216. The existing signed32-bit
wire representation remains; `StateReader.Need` enforces actual slice bounds
before string decode/allocation. Null (-1) and invalid negative lengths remain
distinct. One `StateSyncExtensions.NonnegativeCount` validation is used by
writer, reader and hasher; invalid count writes/hashes no prefix. Count itself
does not allocate a collection or assume a mod element's serialized byte width.

No alternate codec/store, policy default, clock, state format or compatibility
path. `EngineInfo.Build` and the current protocol fixture agree on2.450.0;
save154/replay1 unchanged. The existing pure work-lifecycle master/JSON factory
became internal for reuse; its gameplay and historical test body were unchanged.
One new driver was registered in the original runner.

## Compile and runtime evidence

ONE solution compile attempt, terminal clean:

`dotnet build Beep.SimEngine.slnx --no-restore --nologo`

Exit0, zero warnings/errors, elapsed10.56s. Live session74243 was polled to its
confirmed terminal result. No targeted repair compile or executable edits after
the successful runtime.

ONE NEW exact case, FIRST/FINAL:

`dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "State codec: format-width counts and strings preserve physical planned workflows" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`

**225 passed /0 failed /0 aborted**, runtime29679ms, PID30440 Exited0,
process29874ms, stdout/stderr untruncated, observer empty. Live session9084 was
polled to terminal; no restart on observation yield. No older/family/full suites.

## Requirement-to-evidence mapping

| Scope | Actual proof |
|---|---|
| Native count format | 0,1,former ceiling+1,int.MaxValue write/read/hash exact; four-byte header only, no huge run allocation |
| Invalid count | -1/int.MinValue refuse all passes; writer length/hash unchanged |
| UTF8 strings | Null/empty and600000 Greek characters plus water/rocket restore exact supplied offset slice; actual payload exceeds1MiB; original hash agrees |
| Corruption/boundaries | Negative lengths, int.MaxValue length with absent data, truncated prefixes/final payload byte, attempted read of outer backing byte all refuse |
| Ordinary large player input | Valid1,048,577-character group previews without changing hash, queues through original command codec, saves pending inbox, executes to real paused intent |
| Actual gameplay | Four native/independent × minute/half-minute flows; ordinary hauling/work/home/calibration commands supply and consume8 finite resources, physically move builder to30, earn actual graph prefix |
| Active save/pause | Ordinary configure uses original large group on real partial construction, saved twin retains same work/identity; paused sole clock and paused plan earn no progress |
| Completion | Ordinary rename/resume earns remaining original graph work and exactly one facility/completion fact; original site removed, total12 retained |
| Boundaries/durability | Foreign plan/node facts withheld; retained large views detached; terminal save/hash, public command replay and untouched interleaved session agree |

Physical waits bound512Updates with last12 original private stock/job diagnostic
snapshots. Replay envelope32MiB/6000 records is an explicit driver verification
budget, not a new engine game-volume limit. Serialization primitive probes
never execute or reproduce gameplay; positive drivers use only authored factory,
public seat commands/previews/results/views, saves and `Game.Update`/sole clock.

## Original gates and limitations

Original221 acceptance requirements/text/documents/lines/checkboxes are retained:
**181 checked /40 open**, no original gate newly closed. Additional delivery175.
This removes two actual hidden decoder ceilings, not every possible hidden
limit/catalog/balance/legacy path named by P7.020. Native int.MaxValue wire
width and actual .NET memory are representation constraints, not guaranteed
unlimited state. No huge collection/world allocation stress, hostile-provider
containment, UTF8-malformation redesign, wide wire-format migration or global
regression claim. Protected clock/G19 remain untouched; Oil, Tycoon, adapters,
rendering/Draw and publication were not changed. Historical proofs are not
reclassified as current2.450 runtime evidence.

## Metadata closure

Current read-only integrity:1492 source anchors/760 selected executable inputs/
13 quickstart anchors match current bytes, zero post-FINAL drift.958 Markdown
documents/8572 local links have zero broken links;18 master rows structurally
valid. All221 original literal gates validate and are exactly unchanged against
the preceding baseline (text/documents/line anchors/checkboxes). Counts181checked/
40open, additional175. Driver boundary matches0; `git diff --check` exit0 with
34 existing line-ending warning lines. Inventory2717dirty/staged0 preserved;
four protected clock/G19 SHA256 values unchanged.

The same authoritative selected input freeze gains one new driver and the
previously existing actually changed `Kernel/StateSync.cs`, which earlier
selected freezes did not include. Four previously selected inputs changed;
two existing production sources changed (codec and build identity). This is
an explicit selected-input boundary extension, not a historical proof refresh.
Program's quickstart registration hash refreshed in its existing13-anchor set.
No executable edit/compile/gameplay rerun during metadata closure.
