# P5-A22 verification — Native vehicle and handling authoring

## Current evidence

Core2.544.0/save154/replay1 unchanged. FINAL new exact277/0/0 in18411ms, bounded observer18569ms,
PID14388, exit0, stdout/stderr untruncated. Only this new affected case ran; no old/family/full suites.
No executable edits after FINAL. [Design](../../plans/enhancements/P5_NATIVE_VEHICLE_AUTHORING_DESIGN.md).

All17 vehicles now author original speeds/roles with explicit native mover classes/allowed navigation,
neutral work_rate1 and working/loading/moving/stopped bindings. Ten land and seven sea classes;
all other vehicle data, including cargo inventories, research execution, wear/incident/sensor components,
remain semantically unchanged. Those domain components are NOT exercised by the projection.

Four canonical handling profiles contain36 compatible resource rows. Original holds40/120/400/120
supply each native quantity basis; one hour loading and one hour unloading per full nominal hold are
NEW explicitly authored balancing values. No previous rate was authored: historical handling timing
is not claimed preserved. Partial quantities use original linear handling/game-time contracts.
File registered once by the actual master; no second cargo owner/timer/default/instant transfer.

The single Showcase horizon_days1500 moves to oil.time.showcase.horizon as exact1500days.
Other five time definitions unchanged; no compatibility reader/clock change. Static authoring parity,
not a1500-day terminal scenario run.

## All focused build and runtime attempts

Three affected test-project compiles: initial failed one fixture WorkerRoleId.Id member12.37s;
clean repairs10.53s and10.44s, zero warnings/errors. Use the actual typed role value, not a guessed member.
Fixture metadata is cached/frozen, not live state, avoiding repeated parsing during snapshot observation.

1. 4passed/1failed,115ms/outer275ms,PID24964,exit1: strict loader requires every registered code module
   in the manifest, including inactive ones. The preceding construction stage made ConstructionModule
   available but declared it only in its own fixture callback. Shared inventory/vehicle fixtures could
   not load that registry. Correct the existing shared master with one inactive empty construction
   declaration, and remove the construction case's now-duplicate callback declaration. No implicit
   activation or relaxed engine validation. Historical construction/other cases compiled, NOT rerun.
2. FINAL277passed/0failed,18411ms/outer18569ms,PID14388,exit0.

No bounded timeout or stdout/stderr truncation. No failed apply_patch validations.
One diagnostic inspection used an incorrect Map/Maps source path and changed nothing; actual source
paths were discovered afterward. Some diagnostic source output was truncated; no runtime outcome
inferred from it.

## Focused command and scope

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Oil vehicles: native land sea state roles and authored physical handling" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 100000
```

All17 base native vehicle components are projected with their exact identities/roles/speeds/storage/
handling/state/geometry. Oil research/wear/incident/sensor components are excluded. An explicitly
authored water lane supports sea navigation; hosts never modify the live map.

Actual engine worker/navigation views disclose original roles and singleton land/sea choices.
Public movement commands take all17 machines across six tiles; native moving states and partial-motion
saved twins are observed before arrival, with no teleport or direct worker/state/position write.
Each of the four actual transporter bindings then carries a finite full nominal load through native
loading/travel/unloading. Player held-state release is a command, not a field edit.
Source+cargo+destination quantities conserve on every observed update; partial loaded cargo is saved,
twins remain identical, loading state is engine-owned, whole finite stock arrives once and empty cargo
remains. Full loading+unloading earns at least120 minute updates. All runs omit Tycoon.

Not actual whole Oil initialization, Oil research speed/failure behavior, every cargo resource,
weather/fuel/dock/congestion, terminal horizon, full privacy/rivals/fault/stress/replay/lockstep proof.

## Actual Oil admission and parity

Three public preflights:
1. After vehicle migration: Showcase scenario refused old horizon_days key before initialization.
2. After horizon migration: stabilizer refused duplicate network port connection.
3. Final current content: same duplicate stabilizer connection refusal.

No actual Oil world initialized or updated. Native vehicle read blocker is gone; next merge authored
same-network input/output connections through the existing ports owner, then recovery and remaining
sales/research ownership/paid-supply fixture/full Oil physical acceptance.

Semantic comparisons confirm all17 speeds/original roles/land-sea intent and every field outside
mover/worker/transporter unchanged. Current content counts17 explicit movers/workers/four selected
handling profiles/36 resource rows; handling master reference1; legacy horizon count0.
New base-rate/state/handling authoring is explicit, not silently inserted by the engine.

## Remaining gates and publication

Original221 criterion texts/checkboxes/line anchors retain193recorded checked/28open. Additional286
records scoped progress only. Complete goal/plan unfinished, no staging/commit/push.
No platform/Draw/adapter/host gameplay, engine algorithm/schema/clock/business-rule changes.
Focused tests ONLY including final, no full regression.

## Final metadata

2068 source anchors,13 quickstart anchors,1204 selected executable inputs: zero hash drift.
8682 local links: zero broken;18 master rows valid; thin-driver boundary search empty.
All221 original criterion texts/checkboxes/line anchors match; no acceptance change.
Git diff --check exit0 with34 existing diagnostic/warning lines;3167 dirty paths, zero staged.
Protected GameClock/Tycoon account/value/supplier hashes match the preceding stage.
All17 canonical speeds/roles/other fields preserve semantic parity; one master handling reference.
Only documentation/audit edited after FINAL; no executable drift.
