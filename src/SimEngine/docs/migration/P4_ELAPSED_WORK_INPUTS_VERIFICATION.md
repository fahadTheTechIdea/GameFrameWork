# P4-G2c4 productive elapsed-input verification — 2026-10-06

Current build `beep-simengine-2.148.0`, save77, WorkBoard24, WorkView40. WorkSystem13 and resource flow
codecs remain unchanged. The existing node codec embeds productive exposure; there is no earlier-save
reader, second scheduler, consumed-stock balance or clock. Existing bounded-only view names are deleted,
not aliased.64 current build,60 save-format and25 board-schema assertion files migrate via apply_patch;
other suites compile only.

## Final verification

`dotnet build Beep.SimEngine.slnx --no-restore`: succeeded;0 warnings,0 errors; MSBuild4.90s.

| Exact suite | Passed | Failed | Runtime |
| --- | ---: | ---: | ---: |
| Activity resources: authored scoped offers, native sets and private selection | 110 | 0 | 484ms |
| Activity resources: physical consumption, held tools, interruption and saved ownership | 312 | 0 | 5596ms |

Total422 checks/two suites, zero final failures/aborts. Each runs with
`dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "<exact suite name>"` after the clean build.
Same-name repair/coverage reruns only; no third/family/full gate. These suites include their older nested
work-start/continuous/resource proofs, not additive historical counts. F2/F1/A9 and broad clock suites
were not rerun. The full framework plan remains partial.

## Evidence covered

- Master-listed embedded elapsed-work.json uses the existing target/worker requirement sections,
  reference native quantities, ordinary all groups and activity nodes. No content scan/default loader.
- Pure candidate admission uses actual effective work and the next productive-time charge, not the
  worker's fractional work increment. Unknown aliases refuse. Cumulative discrete/native amounts are
  rounded once against total actual exposure, not independently per update.
- Half-rate machine:24 productive game quanta/12 water units to finish12 work quanta. Double rate:
  six productive quanta/three water units. Work-normalized continuous still consumes its bounded total.
- Travel, zero rate, pause, stock waits, completion and cancellation earn no productive exposure/input
  consumption. Startup materials/money and held tools remain in the same whole-set advancement path.
- Automatic next-input demand uses the existing LogisticsService. Real trucks deliver the actual native
  stock, then normal work resumes. Counted worker-local material prepayment funds12 productive updates
  before the next unavailable unit pauses half-rate work at six work quanta. Role-free timed activity
  uses the same node/advance/transaction owner and saves without adding a machine/timer subsystem.
- Private immutable views expose Use/ReferenceQuantity/Consumed and actual productive game duration.
  Elapsed RemainingBudget is explicitly absent, never a fabricated fuel remainder. Retained snapshots
  remain detached. Another company cannot read the order's exposure/source/usage.
- Node exposure persists through existing Sync/hash. Saved twins continue through travel, fractional
  work, actual delivery and shortage. Zero-exposure positive work and future exposure refuse restore;
  cancelled records remain durable without refunding consumption. Existing provider fault probes still
  refuse partial checkpoints.

The first run found two stale expected board/save versions and one test reading lifetime consumption
from a64-record retained page. Its ten-water retained total did not represent whole-session consumption.
The new long hauling test explicitly proves page expiry and uses the existing ReadSummary aggregates
to verify12 actual consumed water units. No resource journal/inventory/quantum/default was changed to
make that assertion pass. Final gates above supersede that intermediate3-failure run.

## Fingerprints (SHA256)

| File | SHA256 |
| --- | --- |
| Standard/WorkContinuous.cs | AA93890A4CDEB9F9D1C2DED4855D8FA2DFAC09C0100802CCE0ACE2F7B3587FC8 |
| Standard/WorkStart.cs | AFEA909E314BCBCF0BFEE8304C498A4B408250392A15C6FBD71D7E260FE5A99D |
| Standard/Work.cs | BFFCC55CACB325D2A399ADB32544F34DB5B56BB92A91E7597C8F674FE8227ADC |
| Core/WorkResourceSelection.cs | 474828A6A5D796302EABF5C1777799E5C9C8CE7C8E0EAC65D60AB4903B26881B |
| Tests/ContinuousWorkTests.cs | 5D1E9C6562AEF99FEA68B06A9AF226C2CA36AA8388A53BBB40C5233DDE060F38 |
| Proof content/elapsed-work.json | F73C2E120B7BC1ED8889B8F1257EDF0C3E42C879DA39EB152889CF20310D688F |
| Hosting/GameClock.cs (unchanged) | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |

Exactly one new GameClock remains in World. ProductiveSteps is funded local work exposure derived
from accepted updates of that clock, not independent current time. No module advances another clock.

## Limits

Productive charges use whole game-update quanta, including the final overshooting work update. JSON
reference interval is the selected node duration, not arbitrary independent resource-rate periods.
Standby/idle consumption, broader endpoint-native quantum providers, arbitrary external rate proofs,
mixed special providers, monetary budget reservations, editable operating areas, full DAG executor/
template breadth, replay/stress/allocations and P1–P12 acceptance remain open. No Oil/Tycoon/host/demo/
platform/Draw work. Earlier builds/saves deliberately reject; no compatibility model was retained.
