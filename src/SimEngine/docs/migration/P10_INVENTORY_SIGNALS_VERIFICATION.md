# P10-B2 — Inventory readiness signal verification

Date:2026-10-06. Framework-only scope;
[stage](../../plans/enhancements/P10_INVENTORY_SIGNALS_DESIGN.md).

## Identity and exact gate

Build **beep-simengine-2.124.0**. Save67 and every existing owner schema/payload remain unchanged.
New optional module sim.inventory-signals1.0.0 declares inventory_signal component/schema1 with
no mutable fields to serialize. Exact build save/peer admission excludes earlier builds; no old
reader, compatibility overload or parallel signal/control path.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Inventory signals: authored readiness, contracts and private facts"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Inventory signals: physical stock controls and saved continuation"
```

Final build: **0 warnings / 0 errors**, MSBuild elapsed **6.00 seconds**.
Final contract suite: **69 passed / 0 failed**,404ms.
Final physical suite: **46 passed / 0 failed**,411ms.
Total: **115 checks / exactly two complete suites**, zero failures/aborts.

Only those names ran, including same-name repair.57 preceding test files/57 exact build assertions
migrated mechanically and compile only; no earlier suite rerun. Existing ProcessProof start worldgen,
SingleResourcePort, flow journal and typed control/gate fixtures are reused, not their test gates.
The first fixture failed before initialization because its resource metadata did not measure every
capacity dimension. The fixture now explicitly authors one shared mass measure for both resources
and the actual mass policy. No engine capacity rule was weakened or default measure fabricated.
Scoped tracked-source whitespace check passed with line-ending conversion warnings only.
Dirty/untracked source was preserved; no Oil/Tycoon/host/platform work or full gate.

## Actual owners and proof

InventorySignalDef captures immutable explicit id/inventory key/native bill, uses existing BillSpec,
resource catalog and actual singular IInventoryDefinition, checks accepted resources, exact native
quantum and empty-policy whole-set capacity. Existing component signal schema declares the three
boolean metrics. Optional module declares metadata only, with inventory/operations dependencies;
there is no new coordinator, lifecycle loop, inventory/reservation/control registry or codec state.

InventorySignal derives stock from actual Amount, unpromised stock from actual Available, and whole
incoming readiness from actual CheckIncoming. It observes only the selected metric, retaining the
same resource/quantity/capacity/reservation owners. Incoming stock includes real inbound commitments.
Typed false is a valid insufficient/exhausted reading, not unavailable. Unsupported actual capacity
representation remains explicitly unavailable. Identity/policy/version/entity/tick/allocator pure
guards reject touched mutation and malformed native quantities/unknown statuses. These guards do
not claim global malicious-provider rollback or universal cross-owner mutation detection.

- No-map/no-WorkBoard scenario, native multi-resource storage and independent SingleResourcePort
  use the same signal source without runtime inventory-type switches.
- Existing schema alias/private OperationsView and observer, missing entity/content/named source,
  incompatible schema and foreign same-ID session behavior are verified.
- Native count thresholds are inclusive; actual shared mass capacity includes the entire bill;
  continuous fluid thresholds preserve native fractional units. One deficient fluid blocks the set.
- Real acquired outbound promises leave stored stock ready but make available stock insufficient;
  releasing them reopens readiness. Real inbound promise consumes future capacity. No signal state,
  new receipt, fabricated balance or observation event is created. Retained facts stay detached.
- Eighteen strict authored errors fail before play: missing/invalid identity/binding/bill, unknown
  resources, nonpositive/fractional-discrete thresholds, overcapacity/filter mismatch, extra legacy
  field and invalid schema/metric/unit/kind. Immutable authored bills cannot mutate shared sessions.
- Independent provider touched amount/capacity callbacks, negative native amounts and unknown
  capacity statuses refuse; out-of-range capacity produces typed unavailable, not false. Hostile
  source faults real AutomationSystem.Update before a successful control decision.
- Existing native process gate waits at startup and mid-batch for real promised capacity, preserving
  WIP/work and appending no fake flow. Release resumes actual timed input/output; stored supply
  ends20→18 (three consumed, one byproduct), threshold fluid stays3. Saved twins match hashes.
- Actual material use lowers available stock, existing next-step automation disables a native or
  independent-inventory transformer only on its deferred step, and actual earned work/WIP persists.
  Disabled frames alter neither materials nor journal. Normal cancellation returns real WIP; its
  derived readiness reopens deferred ordinary control and a fresh timed process. Pending/disabled/
  return/restart checkpoints and interleaved sessions retain the original owners and equal hashes.

Tests author JSON, submit ordinary player commands, call the actual engine receipt execution boundary,
advance Update and observe. They implement no threshold/capacity/control/production scheduler. New
SingleResourcePort observation fault fields are hostile probes only, not saved gameplay state.

## Unproven / open

Native numeric stock/unit projections, remote scoped-network signal binding, threshold editing and
stock-target controls, full utility supply/allocation/pressure/power solvers, broader custom provider
fault matrices, global rollback, explicit work/motion/network-gate integration scenarios, shuffled
replay/lockstep/soak/stress/allocation budgets and full P1-P12 acceptance remain open. Existing gate
contracts can consume these declared signals; only process integration was directly executed here.
Earlier suites compile only. No world-size cap, game-specific rules, platform adapter or Draw.

## SHA-256 of final compiled/gated sources

| File | SHA-256 |
| --- | --- |
| src/Beep.SimEngine/Standard/InventorySignals.cs | 81B4888E8564E807132FD4A492DD21A6F561EA7002D31595D1EFF9839608FFA5 |
| src/Beep.SimEngine/Standard/InventorySignalsModule.cs | 57D2126AD782428416F718FEF90307A1F7DE5B1A883441C175A8CBE003C1EBBE |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 2918D23150B6EE128A812F4ACA5D6AAC976D10845DF016B39DABEAE901977662 |
| tests/Beep.SimEngine.Tests/InventorySignalTests.cs | BF3812DD2C0DF4519310B06695E41F0298198E873F7E58D988A89B8A8A371335 |
| tests/ExtensionProof/content/inventory-signals.json | 3CB77070A945BCA5C2347E36CFCBFA78B479675E2C17822C57AB01083D7713A9 |
| tests/ExtensionProof/ContractCapabilities.cs | 0B5A5386630BBE50CA31766C04F21F14590E524AEDEA848B9C3190C487E13E6E |
| tests/Beep.SimEngine.Tests/Program.cs | 3B11764B90A644DBF088CC9E0BF6EB7D920E044C0DF2BDDE5BCC50879D5593F3 |
