# P4-G4d authored manufacturing dispatch — scoped verification

2026-10-08. Canonical Beep.SimEngine only. Full goal/plan ACTIVE; no commit/push.

## Actual invocation ledger

1. `dotnet build Beep.SimEngine.slnx`:exit0,0 warnings/errors,8.61s.
2. `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Manufacturing dispatch: authored physical preparation, sole queue and timed saved production"`:
   exit1,2 passed checks/0 failed assertions/1 suite aborted286ms. New fixture authored auto_repeat:null,
   but existing optional-string schema represents no automatic repeat by absence. Original live26751
   resumed, never restarted due observation timeout.
3. Repair ONLY observed fixture field (omit auto_repeat). SAME build:exit0,0 warnings/errors,4.93s.
4. SAME exact case:exit1,22 checks/0 failed assertions/1 suite aborted545ms. New queue append check
   incorrectly required unchanged Tools.Version despite native Inventory.Version aliasing Entity.Version.
5. Repair ONLY observed append check: legitimate queue Touch changes enclosing revision; verify same
   tool endpoint/count/exact commitments instead. SAME build:exit0,0 warnings/errors,5.13s.
6. SAME exact case:exit0,68 passed checks/0 failures/0 aborted536ms.

THREE builds, THREE runs of ONE distinct NEW bounded case. Not first runtime attempt.111 older
build identity assertion files/lines source/compile ONLY. No older/family/full/platform/stale runtime
gate. Source/public proof/tests/JSON FROZEN after final green; docs-only afterward.

## Sole authority and actual proof

ITransformationQueue aliases SAME TransformerManager. Existing enqueue admission migrates from
static native calls to that contract; ordinary command, management preview and work effect use it.
Enqueue rechecks actual current ownership/content/named provider/support/provider admission, reads
detached actual queue facts, commits through ITransformer.Enqueue once and verifies exact prefix/
requested append/count/unchanged processing/WIP/tool commitments. Provider violation faults, not
compensation or invented success. Actual queues remain in named providers, not a coordinator store.

TransformationModule declares TransformationDispatchEffectDef JSON. Required transformation,
capability/count/repeat/home, selected actual definition alias, positive physical ancestry, explicit
none/target home and exact entity-activity named target support. Ordinary WorkBoard completion owns
exactly-once status. Effect adds production intent only; transformation owner handles actual resource
WIP, tools, time, operation gates, output and cancellation through existing state/Sync.

New master explicitly lists manufacturing/world/startup/work documents alongside existing native
worker/geometry/roles/requirements/executors. Five new JSON files, no test-driver-made gameplay,
fake progress, raw stock setter or new provider scheduler. Actual player SubmitPlan/CancelPlan,
ConfigureOperation/EnqueueTransformation/preview/private views/Update/save/load drive everything.
Premature direct Apply is a negative protocol probe only and performs no gameplay mutation.

- Coordinator alias and exact selected transformation/capability/count/repeat/home verified. Real
  startup factory20/two moving workers20 each,total60; no IEconomy/business module.
- Same pure queue admission refuses foreign company and unknown named provider; plan previews pure.
  Actual three-node DAG has parallel inspect/prepare then final physical work. Both earn time; held
  tool consumes nothing, and unearned effect refuses without hash change or queued production.
- Partial jobs/tools save/load. Same saved execution completes and dispatches exact two batches
  once, consumes exact two worker materials60->58. Disabled factory retains idle queue/progress0;
  eight Updates cannot duplicate dispatch or consume manufacturing input.
- Private immutable detached queue/nodes; foreign company cannot read actual production. Saved queued
  work restores through existing owners. Enabling factory starts real10-minute batch, moves3 steel
  into actual WIP and holds1 real tool, preserving total58 until earned completion.
- Mid-production WIP/tools/time save and continue identically. First batch3 consumed/1 output =>56;
  second sequential batch=>54. Actual promises/WIP/tools clear; final hashes and checkpoint agree.
- Cancelling preparation before final effect leaves queue0/material60 with no fake refund. Same
  ordinary enqueue appends1 then2 through coordinator, retaining exact real prefix,queue3.
- Required count/repeat/home, positive count, repeat-count agreement, unknown home/reference/named
  target and timer-only physical ancestry refuse before work. Every Reach bounded900 Updates with
  time/actual queue phase-progress-blocker/node diagnostics.

Engine2.245.0/save139/Board43/System41/WorkView80/TransformationView9/all persistent bytes unchanged.
No compatibility reader, alternative queue/graph/clock/stock ledger, Oil/host/demo/platform/Draw/
business gameplay change. Lifecycle skill guided staged ownership/design/evidence; focused validation
and delayed publication instructions supersede generic full-suite/early-commit advice.

## Scope limits and inventory

Not a graph-owned manufacturing completion wait, queue takeover or implicit production cancellation.
Existing physical queue remains beneath authored dispatch. External provider malformed append/global
purity/fault, continuous/repeat/home/entity-output breadth, team/haul/generated/non-tile/templates,
ETA/replay/stress/CPU and full G3-G5/P1-P12 acceptance remain open. Earlier reports historical.

[Source manifest](P4_MANUFACTURING_DISPATCH_SOURCE_MANIFEST.json).
Scoped543 paths/95 Markdown/1524 local links/0 broken/0 hash mismatches after final documentation
refresh. Git diff-check exit0 with existing LF/CRLF warnings. No old static CheckEnqueue calls remain
(rg0 matches/exit1 means absence, not build/test failure); independent final diff-check observed0.
GameClock unchanged SHA256339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31,
sole construction World.cs:96. Own manifest hash excluded; not whole-repository/runtime acceptance.
Dirty work preserved; publication only after FULL goal done.
