# P9 — Dock departure before replacement demand

2026-10-07. Build2.219.0/save123; existing payloads/logistics view28/work view76 unchanged. Existing
LogisticsService validates actual cargo/inlet receipts, admits physical dock exit independently of
future full destination readiness, and lets normal WorkSystem release the bay at actual arrival.
Demand-only shortage waits at real exit; changed full demand reuses same shipment/age/cargo receipt.
Native/public separate-inventory cargo, partial unloading, player area/stock commands and saved
continuations conserve6 without direct failure signals or invented capacity. Final clean build
0 warnings/errors2.58s; ONE exact new scenario30 checks0 failures/aborts391ms. Two clean builds/
two SAME scenario runs: initial28 passed/2 premature bay-release assertions430ms; predicate waits
for engine release after motion arrival, final30.93 older metadata files/151 identity lines compile
only. No older/family/full/platform or stale-binary run; no post-gate gameplay edit. No new state owner,
clock, API or payload. Full goal/plan remain open; commit/push only after FULL completion/verification.

Exact gate: `Dock demand recovery: physical departure, full-cargo wait and saved retarget`.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Dock demand recovery: physical departure, full-cargo wait and saved retarget"
```

Build1 clean4.31s/run1:28 passed,2 failed430ms. Build2 clean2.58s/run2:30 passed,0 failed391ms.
No suite aborts. The test initially observed WaitingForDestination immediately after physical
arrival, before the next gameplay update cleared its retained bay. Final predicate observes both
actual arrival and native bay release, without changing engine lifecycle/state semantics.

Native and public separate-inventory cargo begin with authored6 and perform actual partial dock
unloading. Player demand1 is smaller than actual residual commitment. Player operating-area
change invalidates old service endpoint but permits real exit movement. Same job/cargo/inlet
receipts/age survive departure; no future room is promised. Existing Update clears bay only at exit,
waits with old inlet promise/cargo unchanged, then replaces only inlet room when player demand6
is authored. Saved departure/wait/retarget/physical completion twins are identical; total stock6
and actual claims conserve/clear. No direct TryReplan signal, stock mutation or fake player logic.
Every stage is bounded360 updates with position/job/toil/cargo/blocker diagnostics. One additional
explicit embedded/master-listed JSON authors demand-only policy; existing fixture/geometry reused.

SimGame.World is already internal; inspection changed no host boundary. Development-lifecycle/SOLID
retained existing execution/state owners. No platform/Draw/host/demo/OilFieldDays changes. Current
proof does not cover endpoint loss, blocked exits, fuel, congestion, arbitrary providers/global
purity, replay/stress/advanced/full acceptance. Now-unused prospective-egress private selector
parameters require subsequent cleanup; this is not a claim of full legacy-path removal.

[Scoped source inventory](P9_DOCK_RECOVERY_DEMAND_WAIT_SOURCE_MANIFEST.json) excludes itself.
Inventory:233 unique files,33 Markdown documents,1,200 local links; final hash/link check requires
zero mismatches/broken links. `git diff --check` exited0. Sole GameClock source remains unchanged:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.
Read-only hash/link/clock/diff checks are not runtime gates; historical inventories remain unchanged.
Full goal/plan active; commit/push only after FULL completion and verification.
