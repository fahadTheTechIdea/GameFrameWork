# P10/P4 shared signal observation boundary — scoped verification

2026-10-08. Canonical engine only. FULL goal/plan ACTIVE; no commit/push.

## Actual invocation ledger

1. `dotnet build Beep.SimEngine.slnx`:exit1,0 warnings/1 error,8.20s. New probe passed Code where
   AutomationSignalSnapshot expects string; repair only that argument.
2. Same build:exit0,0 warnings/errors,6.80s.
3. `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Signal observation boundary: independent physical work, hidden mutation and shared fault cutoff"`:
   exit1,2 checks passed/0 failed assertions/1 aborted suite109ms. New extension declared in master
   but absent from scenario selection. Repair only fixture's explicit module selection.
4. Same build:exit0,0 warnings/errors,4.95s.
5. SAME exact new case:exit0,94 passed checks/0 failures/0 aborted492ms.

THREE builds and TWO runs of ONE distinct exact NEW bounded case.115 older identity assertion files/
lines source/compile ONLY. No older/family/full/platform/stale gate. Source/public proof/tests/JSON
FROZEN after final green; docs-only afterward. No new test family or silent partial gate.

## One authoritative reader

AutomationSignalDef.ReadFrom preserves selected definition and actual session source identity
preflight. Missing/incompatible sources return native typed SignalUnavailable; no substituted
provider/value. Healthy-world check prevents an already faulted session presenting healthy reads.
For a compatible actual source, SAME EntityObservation.CaptureProvider observes the provider's
existing Sync and scoped entity/liveness/ownership/revision/state/identity/sole clock facts. Exact
snapshot validation remains in the reader. finally verifies observation; exceptions latch the
existing first World fault, including direct calls outside normal Update. No rollback or compensation.

SignalWorkRequirementDef removes its local observer and uses this reader directly. Existing
OperationsView, AutomationSystem and SignalGateCondition gate readers already call it. No new
provider/parser/scheduler/stock/state registry or persistence layout. Shared guard is scoped, NOT a
security sandbox; sibling/cross-entity/service/transient state is not a global purity guarantee.

## Bounded proof

- Actual native disabled/backlog and independent Boolean/numeric signals are pure exact facts.
  Missing capability and incompatible schema report typed unavailable without another source.
  Unavailable independent source preserves its authored offline reason.
- Foreign private requests refuse before hidden-mutation probe can run. Foreign-session actual
  source throws before callback, leaves both worlds healthy and numeric state unchanged. This tests
  source identity, not every foreign-content or knowledge policy branch.
- Explicit master-listed JSON declares the existing independent AutomationProbe and actual named
  source through a small extension module. SAME normal provider command and existing WorkBoard/
  WorkSystem own a real staffed sequential6-minute preparation then2-minute verification graph.
  Unsafe level blocks all work; actual safe signal enables real worker travel/arrival/positive work;
  subsequent unsafe facts pause without losing earned progress. Saved twins resume/interleave to
  actual completion with identical hashes. No optional business or native transformer on the target.
- Read-only view catches malformed unit, entity revision mutation, hidden persisted mutation without
  Touch, throwing callback and malformed signal identity. Direct reader and ordinary work requirement
  preview catch hidden mutation through the SAME owner, not a local fallback. Normal work Update
  also faults. Every observed fault preserves first fault and stops time/save/hash/further healthy
  signal reads. New and saved sessions remain isolated. No partial state is fabricated or rolled back.
- Healthy signal snapshots remain detached immutable values; completed work saves with existing codecs.

Automation and other execution-gate callers were source-inspected; their complete runtime fault
matrix was NOT run in this slice. Sibling/cross-entity/service mutation, hostile codecs, exact quantity
provider breadth, hidden-content/area/haul/team/template/ETA/replay/stress/allocations/full P4/P10/
P1-P12 acceptance remain open. This closes one shared signal purity gap, not the full goal.

Engine2.249.0/save139/Board43/System41/WorkView80/TransformationView9/persistent layout unchanged.
One unchanged GameClock; no Oil/host/demo/Draw/platform/business changes. Dirty work preserved.
Lifecycle skill guided ownership and evidence; focused-only testing and delayed publication override
generic full-regression/early-commit guidance.
[Stage](../../plans/enhancements/P10_SIGNAL_OBSERVATION_BOUNDARY_DESIGN.md).
[Source manifest](P10_SIGNAL_OBSERVATION_BOUNDARY_SOURCE_MANIFEST.json).
Scoped566 paths/103 Markdown/1555 local links/0 broken/0 hash mismatches after final documentation
refresh. Git diff-check exit0 with existing LF/CRLF warnings. GameClock unchanged SHA256
339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31.
Own manifest hash excluded; not whole-repository/runtime acceptance. No publication yet.
