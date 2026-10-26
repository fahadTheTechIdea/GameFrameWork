# P10-B3 — unit-aware numeric resource signals verification

2026-10-06, framework only. Preceding B3 focused evidence, not full P10/P1-P12 completion.
Its inventory coverage is included in the expanded current
[B4 gate](P10_NETWORK_SIGNALS_VERIFICATION.md),2.130.0/362 checks. Counts/hashes below identify B3.
No Oil/Tycoon/host/demo/platform/Draw work. The lifecycle skill guided staged design, sole-owner
extension and directly affected verification, not a broad historical-green claim.

## Source and single ownership

- Build **beep-simengine-2.129.0**, save **70**, native inventory signal schema **2**.
  InventorySignal remains stateless component1; all existing owner payloads remain unchanged.
  Earlier builds/content are refused, never migrated through a compatibility reader.
- AutomationMetricSchema.Quantity explicitly declares mandatory authored units through
  IAutomationQuantitySignalDefinition. Fixed metric constructors still require exact fixed units.
  Existing provider metadata captures/revalidates both forms and its same Sync stores identities.
  An authored quantity binding is not a unit wildcard: its signal must resolve an actual catalog
  unit, report that exact unit and return a valid exact quantity through the shared reader.
- OperationsModule declares resource_signals alongside signals. ResourceAutomationSignalDef extends
  the same open AutomationSignalDef and exposes IResourceAutomationSignalDefinition. Resource and
  output unit are existing catalog aliases with matching dimensions; no new registry/converter.
- Existing InventorySignal reads actual IInventory Amount/Available/ReservedIn/ReservedOut. Its
  common native quantity validator also serves boolean stock readiness. Actual stock/promise owners
  retain all balances/receipts/version/Sync; signal results are detached and never consumption rights.
  Numeric metrics are inventory.stored/available/inbound/outbound. Explicit quantities:null supports
  numeric-only sources; disabled readiness reports inventory.signal-no-threshold, not empty-bill true.
- Exact UnitConversion alone owns scale/rational/discrete conversion. Fractional-discrete or
  unrepresentable output is inventory.signal-quantity-unavailable; output range overflow is
  signal.numeric-range. Missing resource compatibility returns inventory.signal-incompatible.
  Malformed/incoherent native quantities, mutated providers or invalid metadata fault pure reads.
- Existing numeric rules, hysteresis, dwell/cooldown, AutomationSystem order290, normal operation
  commands and original process/WIP/fuel/stock codecs own gameplay. No second controller, timer,
  inventory, promise book, event feed, saved signal value or loop. Pause uses the sole game clock.
- The existing master-listed inventory-signals.json now declares schema2 and numeric resource/unit
  definitions; there is no implicit threshold, unit or default rule. Public-only proof inventory
  remains an unrelated IInventory implementation; fault probes add no authoritative gameplay state.
  61 other earlier build assertion files migrated and compiled only, not executed this stage.

## Completed verification

Final completed `dotnet build Beep.SimEngine.slnx --no-restore`: **0 warnings,0 errors**, MSBuild4.79s.
The completed build preceded both final gates; no concurrent build/test or stale binaries.

| Exact complete directly affected suite | Passed | Failed | Reported time |
| --- | ---: | ---: | ---: |
| Inventory signals: authored quantities, contracts and private facts | 146 | 0 | 359ms |
| Inventory signals: numeric controls and saved continuation | 130 | 0 | 587ms |

**276 checks, two suites, zero failures/aborts.** Only these two revised suite names ran; repair
reruns used those same names. No third, prior, broad-family or full regression suite ran.

Contract proof includes preserved boolean bindings, exact quantity metadata, immutable/private/observer
facts, actual outbound/inbound reservations and release, stored-versus-available distinction, count/
fluid/scaled/discrete/rational/range boundaries, accepted empty stock, numeric-only source/readiness
unavailability, actual catalog aliases, missing/unknown/wrong-dimension/schema/kind/untyped definitions,
cross-session refusal, guarded mutation/malformed promise callbacks and faulted-save rejection.

Physical proof includes original affected readiness gates and real WIP/cancellation; numeric stock
hysteresis at the same value with opposite retained latch states; actual reservation-driven threshold
crossings; dwell and next-step control; pause; saved pending/stopped/returned/restarted work; native
and independent stock owners. Scaled-fluid rules control actual elapsed operating draws and preserve
unfinished physical work. Actual native fuel consumed equals the sole resource journal; stopped
control burns nothing. Unsupported fluid signals suspend rules with the exact typed blocker.
All workflow waits are bounded with state/phase/blocker diagnostics.

Repairs corrected proof names to actual Codes.NotOwned and PreviewAutomation APIs. The scaled-fluid
fixture originally assigned an empty recipe list, then left a count-only processor supporting a fluid
recipe. Engine authoring validation correctly refused both. The fixture now leaves count-only endpoints
inventory-only; only the actual fluid-compatible processor selects the powered recipe. No engine
validation was weakened, no dummy recipe/default unit/stock grant or compatibility path was added.

## SHA256 source fingerprints

Relative paths below identify the verified source, not a clean checkout or Git commit.

```text
src/Beep.SimEngine/Core/OperationSchemas.cs 4AB18816EC632B8500A424F327CAC11A6438FB78C60E7DADAF7F2812138475AD
src/Beep.SimEngine/Standard/Operations.cs 2EE80D1B7A5C738EE38955D167C58C2C17B4CB3A1DF376053B3BDE88F7F41448
src/Beep.SimEngine/Standard/OperationsModule.cs 78346FB4F0ECEE54CAF887CB6E66F54C116593BC6D4012F000E9AA786CA68E1C
src/Beep.SimEngine/Standard/InventorySignals.cs 99B8EDD391B4F85D4AF57B1A4D2F218CEF5539B40C45D847A44DA3579E9351E2
src/Beep.SimEngine/Kernel/SaveGame.cs BB2E8D766812BC0133C5729A03D710DC3D52D8702E3650320516B7FA2A6539CF
tests/Beep.SimEngine.Tests/InventorySignalTests.cs 758C54FA79298F661127A9281CC6406878C3B2E86789F267CA95CCDCBEB5FCF7
tests/ExtensionProof/content/inventory-signals.json 97012AC1FB5AE53E08514B967C9D34F64A8644DF0AAA945F2FA7B1715E68D9C6
tests/ExtensionProof/ContractCapabilities.cs 5C4853502F8CE6F4F9C8F8D67294F542228EB753B37E1CCDD3C6C1D06FD9B83D
```

## Remaining scope and review

Remote network/utility signals, delivered satisfaction/rates/pressure/power physics, editable thresholds,
reserve/dispatch target actions, generic graph providers, full provider purity/rollback/book-fault
matrices, replay/lockstep/stress/allocations and full P10/P1-P12 acceptance remain open. Guards reject
captured provider violations; they do not undo arbitrary unrelated module mutations. Available stock
is not reserved stock; ordinary execution must still acquire its existing real receipts.

Single-responsibility review: schema describes bindings; signal definitions resolve metadata; the
existing source observes its actual stock; UnitConversion converts; automation decides; original
transactions/processes execute. No parallel quantity or control owner was introduced.
