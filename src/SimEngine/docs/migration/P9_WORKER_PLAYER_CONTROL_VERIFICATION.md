# P9 / P4-H2 — Spatial-neutral player worker control

## Current M108 — 2026-10-23

Native2.742/save161/replay2: [actual combined proof, failures, source and limits](P10_COMPANY_ALERT_AUTOMATION_VERIFICATION.md#current-m108-held-emergency-cargo-serves-its-original-private-demand--2026-10-23).
Existing worker control correctly interrupts a loaded haul, preserves four real cargo units,
denies foreign hold with NotFound, waits eight game steps and saves Held/job0/location.
Ordinary release then enables the repaired ORIGINAL logistics matcher to create a new
CargoReturn job with the original emergency requester and real private destination. Saved
recovery physically supplies the incident; no copied job, worker state, stock or host logic.
Emergency literal144/0/thirteen actual replays; separate ordinary recovery137/0/seven replays
preserves native policy constraints. This is not paid-control or arbitrary-worker qualification.
Older149checks below remain historical, not rerun. All original broad gates/goal stay open.

## Earlier H2 — 2026-10-07

Build `beep-simengine-2.216.0`, save120, work view75.
Account4, board41, work system41, business view5, worker component1, supplier4 and transformation
view9 payloads unchanged. Direct current command/view identity; no old release API or reader.

## Exact completed-slice gate

ONE distinct new bounded scenario:
`Workers: spatial-neutral held control, paid work and cargo preservation`.
149 passed,0 failed,0 aborted suites,438ms; first attempt. One clean solution build0 warnings/errors,
4.09s. One build, one scenario invocation, no repair/rerun, no stale binary or post-gate gameplay edit.

93 older metadata files migrate151 exact current build/save assertion lines. Three old release
callers migrate directly to SetWorkerHeldCommand { Worker,Held=false }. They compile only, not run.
No older/family/full/platform/OilFieldDays suite. Narrow proof does not imply full roadmap completion.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Workers: spatial-neutral held control, paid work and cargo preservation"
```

## One control owner, no legacy path

IWorkerControl aliases the SAME WorkSystem/IWorkerDispatch instance. CheckHeld and SetHeld own pure
player admission and actual execution. They read actual live worker/company/job facts, consult the
existing interruptibility policy, and recheck before committed mutation. Existing Interrupt/Reopen/
haul EndHaul and resource/budget owners handle release. Actual IMotion.Stop stops navigation; a
stationary worker with no spatial presence or movement is valid. Existing worker Held/HeldArrived,
job, home and entity state remain the saved authorities, not another control/selection/job store.

SetWorkerHeldCommand replaces ReleaseVehicleCommand and sim.release-vehicle directly. MoveVehicle's
tile destination admission/GoTo remain optional navigation, but all worker held/interruption mutation
delegates to IWorkerControl, not a copied financial/job path. WorkView.PreviewWorkerHeld calls the
same admission. Existing ReadHome is the immutable held-state projection. Stationary held workers
publish their authored stopped state during normal entity Update; moving held workers retain actual
motion-state evaluation. No Draw, platform adapter, host/demo gameplay, clock or second scheduler.

## Proven player workflows

- Authored stationary IWorker has no IMotion/ISpatialPresence, yet can hold/release. Same WorkSystem
  instance supplies both control and dispatch. Entity Update reports purchase.blocked when held;
  existing immutable home view reports Held. Repeated hold is safe; saved continuation/release equal.
- Missing/nonworker/nonheld-release admission returns typed refusal. A foreign private view cannot
  control another company's worker. Preview/command admission are pure and identical. Current command
  payload round-trip retains actual worker identity and explicit bool held intent.
- Holding both compatible machines leaves physical build intent pending, no stock consumption or
  cost binding. Releasing one allows actual point-space dispatch, not instant work; its motion is
  IMotion, not concrete tile IMobility. Ordinary construction reserves80 in the existing account.
- Player policy disables interruption during live travel; command/view return the same NotInterruptible
  with unchanged hash. Explicitly restoring interruptibility allows the actual hold. Motion stops at
  the real position before another movement update. Unpaid promises release: cash100, reserved0,
  materials8, progress0; saved held updates remain stationary and identical.
- Releasing the actual worker lets normal dispatch/travel acquire and pay at productive progress,
  consuming2 materials once and paying80 once. Holding paid work preserves progress and stock6,
  refunds nothing and does not earn work during held updates. Saved paid release finishes without
  repeated cash/material payment. Post-completion interleaved twins remain equal.
- The same unpaid/paid physical hold/resume scenario runs with authored money-off mode: neither
  IEconomy nor IMonetaryBudgets exists, yet machines travel, consume materials and complete. Selecting
  dormant Tycoon to own native disabled costs does not activate accounts or make them required.
- Public stock policy lets the native engine dispatch an actual purchasing carrier. The first earned
  partial pickup has physical paid cargo. Holding the machine ends its actual haul commitment but
  retains cargo and money spent; no refund/destruction/repurchase. Saved held updates keep that
  inventory and spend nothing. Release/three subsequent travel updates do not repurchase cargo and
  saved twins remain equal. This is retention/early recovery proof, not full destination delivery/loss.

All normal gameplay uses player hold/policy/build/stock commands, authoritative observations,
save/load and bounded engine time. No test-owned assignments, receipts, charges/credits, work,
movement or cargo recovery. Policy/stationary content is one explicit embedded/master-listed JSON
document; the existing physical fixture is extended, not duplicated. Each Reach stage is bounded120
updates with actual clock/order diagnostics on failure. No direct state corruption/fault probe here.

## Source checks and remaining gates

[Scoped inventory](P9_WORKER_PLAYER_CONTROL_SOURCE_MANIFEST.json) records this checkpoint and excludes
itself from hashes.205 files,0 duplicate paths,0 mismatched hashes;24 inventoried documents contain
1169 local links,0 broken targets. Read-only git diff --check exits0. Historical inventories and
reports remain unchanged; source checks are not extra runtime gates. One GameClock constructor
remains in Kernel/World.cs; unchanged clock SHA256:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

Open: tile-specific MoveVehicle runtime revalidation, full residual-cargo destination/loss/fuel/dock
recovery, adversarial IWorker/control providers, normal Update fault/recovery, removal/endpoint loss,
policy/emergency interruption matrices, monetary range/foreign/unrelated-provider/haul-budget gates,
wider income/trading/replay/lockstep/session/stress/allocation, advanced Tycoon and full P9/H2/H3/I4/
framework acceptance. Foreign preview proof is not a two-player live command/replay stress proof.
Older tests only compile; no inferred regression pass. Development-lifecycle/SOLID review retained
one state/control implementation, contract-driven callers and the existing staged tracker.

FULL goal/plan remain active. Per user instruction, commit/push only after full completion and
verification. No per-slice publication; preserve unrelated dirty work and verify Git targets then.
