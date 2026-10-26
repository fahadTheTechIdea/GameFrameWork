# P9-D5e actual-endpoint logistics diagnostics — 2026-10-08

## Outcome and ownership

Native scoped diagnostics now use the existing LogisticsService/PhysicalPort/ResourcePortContracts
path rather than requiring local inventory. ReadDiagnostics and ReadIncomingThroughput accept an
explicit named port and optional original endpoint. Unknown originals never fall back to the current
selection. LogisticsView checks company ownership before resolving or publishing private facts.

Operational snapshots expose requested Port and resolved Endpoint. Stock, inbound promises, desired
deficit and unpromised delivery gap all refer to that actual store. Queue offers and live jobs filter
by actual destination endpoint, not physical site ID. An alias at another site observes the same
actual demand/receipt/job facts without creating another queue. Shipment facts carry original
source/destination port names and admitted endpoint identities, even after current selections change.

One actual-inlet DeliveryGap calculation serves original Demand/local/recovery entry points as well
as diagnostics. Pure reads guard actual endpoint identity/company/capacity/version and existing
entity/session facts. The original ResourceFlowJournal projects committed native incoming quantities
and exact sole-clock rates, filtered by actual endpoint. TransferLegs counts committed incremental
unload transfer legs; it is deliberately not completed truck trips or current inventory.

No diagnostic state is saved. Existing endpoint books, WorkBoard jobs, demand records and resource
journal remain authoritative; no registry/cache/ledger/matcher/scheduler/clock is added. Core2.320.0
and logistics-view34 identify the changed public facts. Save153/replay1/logistics19/policy3/WorkBoard47/
work-view90/transformation-view17/operations-view11/inventory-view5 remain unchanged.

The new thin player case reuses normal existing master-listed scoped-policies/scoped-haul JSON.
JSON owns starting stock, pools/ports, policy profiles, capacities, handling, movement and game mode.
The test only submits public commands, reads detached views, calls Update and saves/replays.
Materials physically move from the warehouse. No direct World/stock/state writes, host gameplay,
money/Tycoon/Oil/platforms/Draw changes or duplicated authored scenario.

## Verification actually run

TWO clean compilation builds: dotnet build Beep.SimEngine.slnx --no-restore.
First17.06s, final6.46s; both zero warnings/errors. Earlier drivers were compile-only, never executed.

Only ONE NEW distinct bounded exact case, TWO attempts:

1.194 passed/6 failed/0 aborted in985ms. Child PID29944 exited1, observed1145ms, untruncated, no
  observer error. All six failures were test expectations counting trips rather than native
  incremental unload transfer legs. Corrected only those expectations; engine rules unchanged.
2.FINAL200 passed/0 failed/0 aborted in1131ms. Child PID33388 exited0, observed1334ms,
  stdout/stderr untruncated, no observer error. No timeout or forced termination.

Exact command:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Scoped logistics diagnostics: actual stores, committed trips and incoming history" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

Every physical wait has a1000-Update bound with offer/job snapshot diagnostics. The case verifies
typed missing/invalid/private reads, local and named quantities, independent stores, aliases across
sites, competing demands, exact promises, original live shipment facts, changed current selection,
physical delivery, detached snapshots, saved twin hashes, journal history/rates, conservation and
ordinary command replay. Executable inputs frozen2026-10-08 22:13:10 UTC; only documents/audit
metadata changed after the final pass. All previous runtime evidence is historical/not rerun.

## Remaining scope

This is native actual-endpoint operational/throughput observation, not complete logistics,
P9/P8/P1-P12 acceptance or full goal completion. Generic scoped activity-DAG supply, scoped recovery
dispatch and same-site automatic matching are the next source gaps. Existing recovery calculation
delegates to the unified gap helper but recovery runtime was NOT revalidated in this slice.
Removed-store/provider/purity-fault/corruption matrices, optional absent/expired journal cases,
docks/areas/emergency/congestion/fuel/routes/stress/allocation/global accounting/lockstep/shuffled
arrival/full durability remain open. No historical/family/full/Oil/platform tests were run.
Publication remains gated on FULL goal AND plan genuinely finished and verified; nothing staged,
committed or pushed.
