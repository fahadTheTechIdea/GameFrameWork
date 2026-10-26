# P5-A16 verification — Tycoon upkeep ownership

## Current evidence

Core2.541.0/save154/replay1; BusinessView7 and stateless periodic-cost system1.
FINAL NEW exact2303/0/0 in4989ms; bounded5288ms, PID42232, exit0, untruncated.
Only this exact case ran; no old/family/full suites. No executable edits after FINAL.

[Design](../../plans/enhancements/P5_TYCOON_UPKEEP_OWNERSHIP_DESIGN.md).
Thirty-eight original upkeep rates now belong to optional Tycoon JSON; all25 structures/17vehicles
retain non-upkeep data. No Oil upkeep component/registration/charge loop/ledger constant remains.
Original accounts/budgets and GameClock are retained. This is not all-business consolidation.

## Attempts retained

OilCo.Game targeted builds clean5.21s,1.34s and final1.22s, zero warnings/errors.
Initial focused compile failed five test-only errors11.90s (long FromInt and read-only Paused).
Repair uses existing Pause/Resume API and checked expected-amount conversion; clean6.77s.
Runtime attempt1:2295passed/4failed,6297ms/outer6492ms, PID33972, exit1, untruncated.
The four oracle failures used completed-step time rather than the original TickInfo interval start.
Production schedule unchanged; corrected oracle/save boundaries. Compile clean7.69s.
Runtime attempt2:2299/0/0,6216ms/outer6527ms, PID44064, exit0.
Review applied original owned observation gate, deleted unused Oil constant and added cleared-target checks.
Compile clean11.17s; runtime attempt3:2303/0/0 in4667ms/outer4839ms, PID37792, exit0.
Final metadata review rejects null target contracts explicitly; compile clean10.10s and FINAL runtime as above.
No failed patch validation.

## Exact command

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Tycoon upkeep: authored periodic costs debt pause saves and disabled money" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 100000
```

Three native modes each run450updates under one ten-minute clock. Three policies are an explicit
test-authoring projection of canonical Oil company_base1500, haul_truck60 and bulldozer90 daily fees.
The engine alone interprets them. Enabled balance5000 becomes-1600 after four daily charges, retaining
debt behavior. Pause creates no charges. Saves span before/after due boundaries and debt; twins and idle
sessions remain deterministic. Selected-disabled mode has no account/budget; absent mode has no Tycoon.
Pure immutable fee views, unknown-target output clearing and early invalid reference/offset refusal pass.

## Evidence limits

Projection is not complete Oil prototype/content/runtime validation. It does not execute actual
Oil commerce/disposal, remaining sales/research/power callers, full domain campaigns/rivals/private views,
arbitrary mod getters, concurrent debt/held budgets, removal or replay/lockstep/global stress.
Original221/193recorded checked/28open retained; additional280. Existing authorizations preserved.
Goal and remaining financial consolidation open; no staging/commit/push/full regression.

## Final metadata

2042 source anchors,13 quickstart anchors,1182 selected executable inputs: zero hash drift.
8669 local links: zero broken;18 master rows valid; thin-driver boundary search empty.
All221 original criterion texts/checkboxes/line anchors match the audit; no changes this stage.
Exact parsed-data equality confirms all42 prototypes preserve every non-upkeep field.
Git diff --check exit0 with34 existing diagnostic/warning lines;3147 dirty paths, zero staged.
Of13 recorded Tycoon sources, only TycoonModule registration/composition changed; one new cost definition/interpreter source added.
Original account/pricing/value/purchase/budget algorithms and GameClock unchanged. Goal/remaining consolidation/Oil runtime open.
