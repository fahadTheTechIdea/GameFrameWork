# P12-D13 / P4-G2 feasible distinct worker teams — verification

Date: 2026-10-08. Canonical `Beep.SimEngine` only. Full P1-P12 remains ACTIVE. Publication is
deferred until the full goal and plan are completed and verified.

## Current source

Build `beep-simengine-2.253.0`; save139, incident view7, WorkBoard43, WorkSystem41, WorkView80.
No persistent layout or public snapshot shape changes. Exact build identity refuses old saves/peers;
no compatibility reader. No new assignment, reservation, incident, account, graph or clock owner.

- [DistinctWorkerMatching.Solve](../../src/Beep.SimEngine/Standard/DistinctWorkerMatching.cs)
  accepts a pure complete-set feasibility predicate. The existing augmenting-path maximum result
  stays authoritative for each attained priority demand count. Iterative alternatives preserve all
  those counts and actual compatible edges. Same-demand permutations suppressed; slots expanded
  from attained counts only, never huge requested capacities. Original coverage is returned if no
  jointly feasible set exists, for honest shortages and ordinary physical sequencing.
- [WorkBoard.CheckWorkSet](../../src/Beep.SimEngine/Standard/WorkStart.cs) composes existing
  joint dock, material and optional monetary admission once, with actual selected results/contexts.
- [CapturePreparedness](../../src/Beep.SimEngine/Standard/WorkIncidentPreparedness.cs)
  and [FitsAssignmentSet](../../src/Beep.SimEngine/Standard/WorkScheduling.cs) reuse that evaluator
  and actual candidate roles. Preparedness keeps acquired incumbents fixed; dispatch groups by
  actual company and uses [Assign](../../src/Beep.SimEngine/Standard/Work.cs)'s existing rebuild
  after each real acquisition. External providers still own their unrepresented obligations.
- [Focused proof](../../tests/Beep.SimEngine.Tests/FrontierTeamFeasibilityTests.cs) reuses existing
  master-selected point-space, worker policy, response DAG and dock documents. Fixture inputs are
  JSON content; tests neither select/acquire jobs nor implement movement/work. Actual player policy,
  incident and home commands are submitted normally.

## Every build/run attempt

Commands, canonical repository working directory:

```
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Response frontier teams: feasible alternatives, dispatch parity and saved physical work"
```

| Attempt | Build | Exact new case |
|---|---|---|
| 1 | exit0; 6.20s; 0 warnings/errors | exit1; 49 passed, 1 failed; 622ms; bounded queued completion threw |
| 2 | exit0; 3.92s; 0 warnings/errors | exit1; 49 passed, 1 failed; 719ms; expanded timeout diagnostic only |
| 3 | exit0; 3.72s; 0 warnings/errors | exit0; 53 passed, 0 failures/aborts; 517ms |

Only ONE distinct NEW case ran, repaired/rerun for the observed failure. No older, family, full,
platform or stale suite ran.119 older C# build assertion files/lines were mechanically migrated
and compiled only, not runtime-verified. Counts are assertions, not independent acceptance games.

The timeout was not false engine admission: both machines earned their work, but the first stayed
at position12, the sole area-permitted departure point. It had no home. Second job remained paused
at position11 with earned progress12 and a real occupied-exit blocker. The observed proof repair
explicitly waits for that blocker, submits ordinary SetHomeCommand to send the first machine to
the actual source, and verifies physical continuation with a saved twin. No occupancy rule,
area constraint or engine source was weakened; no teleport, default home or free material added.

## Proven scope

- Complete-set alternative matching with multi-capacity demands, priority preservation, immutable
  detached lists, huge requested capacity bounded by actual workers, no-feasible result and
  uncaught predicate exception (not a full provider fault matrix).
- Three actual machines in an independent point-space provider; two area-restricted machines can
  individually reach the same service bay, third inspector can reach a disjoint bay. Preparedness
  selects the feasible third machine; real dispatch does too, including after first acquisition.
- Private company facts, prospective purity/hash/save round trip, frozen snapshots, policy-disabled
  alternate refusal and joint material shortage without bypassing physical stock. Money absent;
  no account fabricated. Native joint-money search integration is compiled, not separately proven.
- Actual jobs/material claims, physical parallel progress, fixed acquired incumbents, sole-clock
  pause, travelling and partially productive saved twins, departure/restart and eight units consumed
  exactly once by the existing owner. Completed state saves without another matching codec.
- No feasible simultaneous team still uses real waiting and physical sequential work. Actual
  occupied departure remains blocked until the player assigns a home and the engine moves normally.

## Freeze, inventory and limitations

Source/public proof/tests/JSON frozen immediately after final green. Later edits are docs only.
The [scoped source manifest](P12_FRONTIER_TEAM_FEASIBILITY_SOURCE_MANIFEST.json) hashes accumulated
engine-plan paths plus current changes, not the entire repository. Local Markdown links and actual
Git whitespace checks are diagnostics, not runtime acceptance. Current counts recorded below.

Scoped587 hashed paths,113 Markdown documents,1687 local links,0 broken links; actual
`git diff --check` exit0 with existing LF/CRLF warnings. Own manifest excluded from its hashes.
Historical manifests/results are preserved unchanged; no broad repository/runtime claim.

The sole GameClock SHA256 is unchanged:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

Atomic whole-team acquisition, global requirement-branch optimization, mixed external/native joint
resource/dock obligations, complete provider fault/global purity proofs, future departure/funding
guarantees and combinatorial CPU/stress/replay/full acceptance remain open. No Oil, host, demo,
Draw, platform, Tycoon source or business-policy changes. No Git commit/push yet.

See [stage](../../plans/enhancements/P12_FRONTIER_TEAM_FEASIBILITY_DESIGN.md) and
[master tracker](../../plans/enhancements/MASTER_TRACKER.md).
