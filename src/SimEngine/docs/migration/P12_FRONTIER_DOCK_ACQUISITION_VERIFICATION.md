# P12-D13 / P4-G2 selected service-slot acquisition — verification

Date: 2026-10-08. Canonical engine only. Full P1-P12 goal ACTIVE; no commit/push until full
verified completion. This is scoped evidence, not whole-engine acceptance.

## Current engine ownership

Build `beep-simengine-2.254.0`; save139/incident view7/WorkBoard43/WorkSystem41/WorkView80.
All persistent payloads unchanged. Exact build identity changes; no compatibility reader.

- [FitsAssignmentSet](../../src/Beep.SimEngine/Standard/WorkScheduling.cs) optionally projects
  the SAME CheckWorkSet's selected actual service IDs to native matched demands. No partially
  feasible output map survives refusal. Worker/target correspondence is validated. External
  sources still own their obligations; no fictitious activity context is supplied for them.
- [Assign and TryAssign](../../src/Beep.SimEngine/Standard/Work.cs) recompute that same admission
  after matching and pass the transient selected slot into original native acquisition. Each
  successful original worker/material acquisition invalidates the set and rebuilds all offers.
- [PreviewDock and SelectDock](../../src/Beep.SimEngine/Standard/WorkDocks.cs) apply an exact
  required-slot filter in the existing selector, alongside productive kind, role, geometry, route,
  policy, live claims, occupancy and departure admission. Actual Assign freshly checks before
  any worker/material mutation. Missing/area-invalid/occupied/waiting slots never silently
  substitute another bay. A dockless target cannot satisfy a required real service ID.

No new selector, reservation, job, provider model, persistent state, clock or public command.
Existing physical waiting remains available when no simultaneous set is feasible.

## Exact focused gate

Canonical repository working directory:

```
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Response service selection: exact joint bays, fresh refusal and saved acquisition"
```

ONE build: exit0,4.83s,0 warnings/errors. ONE distinct NEW bounded case: exit0,48 checks,
0 failures/aborted suites,486ms, FIRST runtime attempt. No repair/rerun needed.

120 older strict build assertion files/lines migrated and compiled only, not rerun.
Two internal test call/helper edits compile-only: explicit nullable selection argument in an older
acquisition assertion, and visibility of the existing dock fixture for reuse. The new case calls
that fixture; its older cases are not executed. No older/family/full/platform/stale gate.

## Proven behavior

The [new case](../../tests/Beep.SimEngine.Tests/FrontierDockAcquisitionTests.cs) reuses the existing
[dock fixture](../../tests/Beep.SimEngine.Tests/FrontierDockFeasibilityTests.cs), master-selected
response graph, point-space and dock JSON. No duplicate content/workflow or test-owned scheduling.

- Actual two-worker frontier: unrestricted first machine takes the later service bay, allowing
  the area-constrained inspector to use its only permitted bay. Real native jobs acquire precisely
  the preparedness-selected bays and remain productive rather than needlessly entering waiting.
- Exact read refuses missing IDs, wrong area and waiting-as-productive capacity. Dockless target
  returns typed Missing. Private preparedness denies the foreign company; selected facts frozen.
- Native acquisition unit check against an actual engine-owned open job refuses a policy-invalid
  required bay before worker/slot/material mutation. Hash remains identical, worker stays idle,
  job stays Open with no substituted slot. This unit is not claimed as public-only acceptance.
- Actual live claim refuses another exact request. Prospective queries/hash/save remain pure.
- Physical approach, parallel earned work, original native claims/progress, player-assigned home
  intent, real departures and response restart. Materials consumed eight units once; never on
  mere selection/travel. Sole paused clock freezes progress. Travelling/partial saves continue
  identically with twins; completed state saves with no extra acquisition codec.
- With one service bay, no jointly feasible intention is fabricated. Actual second job waits;
  ordinary physical queue and home traffic complete deterministically through save/load.

## Freeze and diagnostics

Source/tests/content JSON frozen immediately after final green; remaining changes documentation-only.
The [source manifest](P12_FRONTIER_DOCK_ACQUISITION_SOURCE_MANIFEST.json) covers accumulated engine
plan paths plus current changes. Own manifest excluded from its hashes. Historical evidence is
preserved, not rewritten as current test results. Scoped counts recorded below after inspection.

Scoped591 hashed paths,115 Markdown documents,1704 local links,0 broken links. Actual
`git diff --check` exit0 with existing LF/CRLF warnings. Not whole-repository/runtime acceptance.

Unchanged sole GameClock SHA256:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

## Remaining gates

Joint resource-source choice acquisition, atomic teams, global recipe optimization, external/native
joint obligations, provider fault/global purity matrix, future departure/funding guarantees and
CPU/replay/stress/full acceptance remain open. Selected bay intention is not a joint reservation.
No Oil, Tycoon source/business policy, host, demo, Draw or platform changes. No publication yet.

[Stage](../../plans/enhancements/P12_FRONTIER_DOCK_ACQUISITION_DESIGN.md),
[master tracker](../../plans/enhancements/MASTER_TRACKER.md).
