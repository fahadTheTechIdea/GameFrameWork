# P4-G2c5 exact activity input rate verification — 2026-10-06

Build `beep-simengine-2.149.0`, save77, WorkBoard24, WorkView41. Node/receipt/resource/clock layouts
are unchanged. Elapsed content breaks directly to explicit rates; no quantities-only elapsed reader.
64 current build identity source/assertion files migrate through apply_patch; unrelated suites compile only.

## Final verification

`dotnet build Beep.SimEngine.slnx --no-restore`: succeeded;0 warnings,0 errors; MSBuild1.98s.

| Exact suite | Passed | Failed | Runtime |
| --- | ---: | ---: | ---: |
| Activity resources: authored scoped offers, native sets and private selection | 140 | 0 | 452ms |
| Activity resources: physical consumption, held tools, interruption and saved ownership | 345 | 0 | 4837ms |

Total485 checks/two exact suites, zero final failures/aborts. Commands use
`dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "<exact suite name>"` after the clean build.
Same-name repair/coverage reruns only, no third/family/full gate. Older G2c4/G2c3/work-start/resource methods
are directly nested and included, not additive historical counts. F2/F1/A9/broad clock gates are not rerun.

## Covered evidence

- Existing master-listed embedded elapsed-work.json authors strict rates through ResourceRateSpec.
  Each native input has its own resource, quantity and positive rational game-time period. Empty/null/
  old/conflicting/duplicate/unknown resource or unit/illegal native quantity/zero period content refuses.
- IResourceWorkRequirement, immutable WorkResourceChoice and WorkResourceUsage expose exact rates.
  Elapsed Quantities is the derived reference bill, not a second authored rate/bill or stock ledger.
  Requirement multipliers scale actual native quantity, not the period or game clock. Public choices
  refuse bill/rate disagreement. Native rates and candidate/usage collections are detached/read-only.
- WorkBoard delegates elapsed cumulative quantities to existing QuantizedGameRates. A ninety-second
  water period is not rounded to two game-minute updates; twelve productive minutes consume eight
  units with no per-update drift. A thirty-second session quantum gives the same eight native units
  in twenty-four productive updates; no additional clock/remainder store is introduced.
- Two inputs use six- and five-minute periods in one requirement. Actual physical consumption is six
  water and three prepaid discrete steel in twelve productive minutes. Duration doubling leaves next
  half-unit fuel demand unchanged and correctly pauses the longer activity after twenty minutes at
  empty physical stock. Scaling doubles the native rate while preserving its period.
- Preview remains pure and cannot advance GameClock. Private immutable views, retained detached rates,
  slow/fast worker execution, zero-rate/travel/pause/shortage/cancel waits, automatic real hauling and
  saved twins all use existing owners. Public-only connected source provider consumes actual selected
  scoped stock at an independent six-minute period; it cannot switch silently to another stocked pool.
- Existing save owner restores scaled rates from the manifest-bound definition, preserving actual
  productive exposure, source identity, progress and receipts. No rates/current-time duplication is
  serialized. Checked native accumulation rejects unrepresentable quantities instead of wrapping.

The first physical run had170 passes/two failures (one assertion plus consequential indexing exception):
the new test requested a completed order after ordinary terminal pruning. The corrected probe captures
rate views while the order is retained, then proves its immutable snapshot survives completion/pruning.
No engine pruning, stock, duration or resource rules were changed to make the test pass.

## SHA256

| File | SHA256 |
| --- | --- |
| Standard/WorkContinuous.cs | 447197F88BA740366CAA23A30201282904D393A762D046B7685675EEDFEE24A5 |
| Standard/ResourceWorkRequirements.cs | 507F0128EE14DC962375A42A66608C91975B5FF020BA2877435D9B9A1242ABCB |
| Core/WorkResourceSelection.cs | 02D5A380F113B406A47CAC51ACCF2AE309D114745F76F667898799D8278DD40F |
| Standard/WorkResources.cs | ADD6D3C60F87131FBDBE4617089C853A1C3F16AD8C783F3417208B788BAE1F71 |
| Tests/ContinuousWorkTests.cs | 47C1C48D041D8F5F9D5EA50B3C274DF9B137057F25F98E4B906CF41C8430F4B2 |
| Proof content/elapsed-work.json | 6CC9E2A174D7F468B5D4F7E8F7BEB04687F3B8E5B1BB4727DD4D698A41EF1F25 |
| Hosting/GameClock.cs (unchanged) | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |

Exactly one new GameClock remains in World. ProductiveSteps is accepted local work exposure derived
from that clock's updates, not a timer/current-time authority. Resource rates are immutable conversion
intent, not clocks. No Oil/Tycoon/host/demo/platform/Draw implementation changed.

## Remaining

Whole final update charging remains deliberate. Standby/idle/travel rates, endpoint-specific coarse
quantum providers, mutable content freeze/full external rate proofs, monetary budget reservations,
editable/all-dispatch operating areas, full DAG/template breadth, replay/stress/allocations and P1-P12
acceptance remain open. Focused gates do not establish full regression or plan completion.
