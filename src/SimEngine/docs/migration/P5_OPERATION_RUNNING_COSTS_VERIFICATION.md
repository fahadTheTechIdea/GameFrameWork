# P5-A17 verification — Running costs and canonical clock correction

## Current evidence

Core2.542.0/save154/replay1; BusinessView8, periodic system1.
FINAL NEW exact101/0/0,2248ms case,2404ms bounded observer, PID43548, exit0, untruncated.
No old/family/full suites. No runtime logic edits after FINAL; retired-field XML comment corrected afterwards.

[Design](../../plans/enhancements/P5_OPERATION_RUNNING_COSTS_DESIGN.md).
The grid connection's original60/hour/5-minute-offset monetary data is now optional Tycoon-owned.
Power's cash reader/field/constant/charge path is removed; physical fuel and authoritative state unchanged.
Thirty-eight upkeep policies keep their original amounts/periods/reasons, explicitly unconditional.
All six canonical modes now use one-minute simulation quantum and ten updates/frame on the same clock.
This corrects A15's coarse-quantum mistake; its earlier native finance fixture did not prove full Oil timing.

## All attempts

OilCo.Game targeted builds clean3.32s and1.15s, zero warnings/errors.
Focused compile clean10.93s; exact attempt1:71/0/0,2520ms/outer2699ms, PID45416, exit0, untruncated.
Business view version updated and all six canonical offset checks added; compile clean7.48s.
FINAL exact101/0/0 as above. Two later documentation/audit patch validations failed atomically; corrected with exact ordered anchors.
One intermediate concatenated-JSON diagnostic parse failed before editing; separate reads recovered it.

## Focused command

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Tycoon running costs: actual operation state and fine game-clock offsets" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 100000
```

Native processing is controlled only through EnqueueTransformationCommand and ConfigureOperationCommand.
The engine's real operation state gates fees: enabled-but-idle not eligible, running4940 balance after
first original60 charge, disabled no later charge, resumed4880 after the next original anchored charge.
Saved twins match; money-off selected mode has no accounts/budgets. Missing named operation refuses
before initialization. Six canonical profiles exactly represent5/15/45/50-minute schedules with frame10.

## Actual Oil admission diagnostic

Public OilCoGame.LoadContent/Factory/NewGame against the current actual master refuses before initialization:
`oil@1.0.0 oil/production.json: unknown section 'recipes'`.
This is one targeted initialization preflight, not a regression suite or successful Oil gameplay test.
It identifies the next genuine strict-content migration boundary. No world updates ran.

## Evidence limits and remaining work

Native transformer projection is not actual Oil PowerSystem/grid/fuel testing. Complete Oil content/
initialization, sale settlement/statistics, research monetary ownership, arbitrary conditional provider
faults, indirect privacy, removal, replay/lockstep/global stress remain open.
Original221/193recorded checked/28open retained; additional281. Goal incomplete; no Git writes/full regression.

## Final metadata

2046 source anchors,13 quickstart anchors,1184 selected executable inputs: zero hash drift.
8673 local links: zero broken;18 master rows valid; thin-driver boundary search empty.
All221 original criteria/checkboxes/line anchors match; no changes this stage.
Parsed-data parity checks confirm only the intended power-money/explicit-condition/time changes.
Git diff --check exit0 with34 existing diagnostic/warning lines;3151 dirty paths, zero staged.
Of14 recorded Tycoon sources, only EntityOperatingCosts and TycoonModule changed; one new condition source added.
Original account/value/pricing/purchase/budget algorithms and GameClock implementation unchanged. Full goal and actual Oil runtime open.
