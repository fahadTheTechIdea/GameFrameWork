# P4-G72: Work-management observation verification

Current scoped engine identity:2.446.0/save154/replay1.
[Design](../../plans/enhancements/P4_WORK_OBSERVATION_BOUNDARY_DESIGN.md).

## Engine changes

Original WorkView resets out values and consults World's first-fault guard before
missing/private/empty work, policy, home, recurring, capacity and load shortcuts.
Home/hold/policy/recurring and missing final-effect previews likewise refuse stopped
sessions. Original WorkBoard checks single-plan owner/group admission and null
dependency/cancellation/group inputs through that same guard.

Both active and prospective candidate projections observe the actual IWorkerDispatch
into local detached lists, then publish only after health verification. IdleWorkers
observes the complete native/independent role query. Existing selector, graph,
jobs, plan records, reservation/resource state and clock remain sole owners.

## Compile attempts (all retained)

1. Solution `dotnet build Beep.SimEngine.slnx --nologo`: failed6.67s,
   zero warnings/one new CS1579 in the negative probe's ComponentList foreach.
2. Targeted test-project compile after canonical indexed-access repair: clean7.55s.
3. Same-case factory repair targeted compile: clean5.36s.
4. Same-case native-pruning chronology repair targeted compile: clean4.81s.

The whole solution was not rerun to claim a clean aggregate result. Every final
runtime input compiled through the targeted project and its dependencies.

## ONE NEW exact case (all attempts retained)

`Work observations: physical execution and terminal candidate plan query boundaries`

Command: `dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Work observations: physical execution and terminal candidate plan query boundaries" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 20000`

1. FIRST1passed/1failed in81ms; PID44648 Exited1, process231ms. Fixture
   called the single-document factory overload with no document. Corrected to
   the existing multiple-module manifest overload with explicit empty documents.
2. SECOND18passed/1failed in441ms; PID36524 Exited1, process584ms.
   Fixture queried a completed, normally pruned order. Corrected to capture
   active productive progress and save before terminal native pruning.
3. FINAL590passed/0failed/0aborted in840ms; PID36016 Exited0,
   process992ms. No output truncation or observer error on any attempt.

## What FINAL proves

- Actual point master and points.json author native and independently implemented
  workers, selected original modules, physical positions/resources and two explicit
  minute/half-minute time variants. No business or platform requirement.
- Ordinary public plan submission/preview/commands pause an intent, save its inbox,
  resume, travel and earn six minutes of inspection. Partial productive saved twins
  continue to the actual terminal plan, respecting original order pruning.
- Actual machine pickup/travel/delivery transfers four resources and preserves
  the original twenty total. Terminal saved twins, sole-clock pause, replay and
  independent untouched session are verified.
- Healthy candidate reads are pure, native/independent, detached and company-private.
- Actual module view failure stops the session. Own/foreign/observer work features
  then refuse live shortcuts without retrying callbacks or replacing the first fault.
- Three later-worker callback failures (preview, active candidates, idle count)
  reach the original fault owner. Failed candidate lists publish no prefix; subsequent
  empty-role queries do not retry the bad worker, Update stops, and saves refuse.
- Positive driver contains no Game.World, TestKit, RunTicks or direct stock/progress
  mutation. Negative callback arming lives in the separately compiled engine proof.

## Limits and remaining requirements

No executable edit after FINAL. No historical/family/full runtime was rerun. These
fixes do not prove arbitrary private CLR provider purity, all alternate selectors,
all work-view/plan-template methods, global durability, lockstep/stress/allocation,
or full P4.014/P1-P12 completion. Original221 texts/checks remain180checked/41open.
Protected GameClock/G19 catalog/Tycoon bytes remain untouched. No Oil, platform,
legacy/compatibility changes, staging, commit or push. Publication remains conditional
on the entire goal AND plan actually finishing and being verified.

## Metadata closure

Current integrity audit:1464 source anchors,740 frozen selected inputs and13 quickstart anchors match current bytes; zero post-FINAL executable drift.950 Markdown documents/8548 local links have zero broken links;18 master rows are structurally valid. All221 literal gate texts/checks/anchors validate,180checked/41open unchanged. Driver boundary matches0; git diff --check exits0 with34 existing line-ending warning lines. Inventory2694dirty/staged0, preserved. Four protected clock/G19 hashes match prior bytes. Initial metadata check identified the existing Program quickstart anchor needing registration-hash refresh; refreshed in place and clean reread verified. An intermediate readonly metadata result was not retained; authoritative process inspection found no remaining matching process, followed by the clean reread. No gameplay/compile rerun or executable edits during metadata closure.
