# P9-C10 player-owned navigation selection — verification

Date2026-10-08. Canonical engine only. Full goal and P1–P12 plan ACTIVE; commit/push only after FULL
verified completion. This focused evidence is not full-engine acceptance.

## Ownership and identity

Build2.259.0/save140/mover4/MotionView10 directly replace current identities, no legacy decode.
[Contracts](../../src/Beep.SimEngine/Standard/MotionNavigation.cs) declare optional navigation
selection as an alias of actual IMotion, with explicit definition metadata and detached immutable
profile/allowed IDs. Same [Mover](../../src/Beep.SimEngine/Standard/Movement.cs) owns selected class,
route, intent, age and original Sync. Initial class is creation metadata, not a second current owner.
Changing class clears the original path, rebases to actual fractional position, preserves goal/age
and permits original retained Unreachable intent to retry. Idle/arrived selection grants no goal.
Same-value configuration is a no-op. Profile permission is not a guarantee of reachable travel.

Native JSON must explicitly list classes, including initial class. Original
[NavService](../../src/Beep.SimEngine/Standard/Navigation.cs) verifies selected-session registrations;
existing [motion contracts](../../src/Beep.SimEngine/Standard/MotionExecution.cs) definition/live
validation checks capability metadata and actual owner. IDs belong to navigation providers; no new
global catalog, route/planner/service/scheduler/claim/time owner or host routing policy.

[Normal command](../../src/Beep.SimEngine/Commands/MotionNavigationCommands.cs) checks ownership and
delegates the same pure admission/exact commit contracts; entity/profile use existing command wire.
[MotionView](../../src/Beep.SimEngine/Views/MotionView.cs) hides foreign policy through private
read/preview, preserving normal command refusal semantics. Existing provider codec guard verifies
reads/checks; commit checks selection/status/version/owner/clock/identity. This is scoped protection,
not a global malicious-mod sandbox. Original Update still owns travel, gates, resources and work.

## Every build and runtime invocation

Canonical working directory C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine:

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Navigation selection: private controls, retained travel and saved physical work"
```

ONE build exit0,5.94s,0 warnings/errors. ONE exact NEW bounded case exit0,68 checks,0 failures,
0 aborted suites595ms,FIRST attempt. No repair build/run needed. Source/tests/JSON frozen afterward.
123 older strict build identities/120 save assertions/one mover schema expectation and21 existing
native JSON files/28 mover definitions migrated/compiled only. An earlier layered-routing fixture's
explicit initial-class override also explicitly updates its singleton allowed list; not rerun.
No older/family/full/platform/stale suites. Prior layer-routing/sensor/logistics/clock checkpoints
are historical evidence, NOT revalidated on2.259.0. Unrelated older schema assertions remain older
gate work, not falsely claimed green.

## Focused proof

[One new case](../../tests/Beep.SimEngine.Tests/MotionNavigationSelectionTests.cs) uses the existing
native master/startup/work composition and explicitly selects authored
[routing rules](../../tests/ExtensionProof/content/native-layered-navigation.json) and
[public terrain/depth](../../tests/ExtensionProof/content/native-layered-navigation-world.json).
The fixture explicitly permits survey.route and terrain.route on the existing native machine.
No test-owned provider, cost/search/route/work/gameplay system. Players issue normal hold, release,
activity and navigation commands; subsequent time advances through normal Game.Update.

- Actual motion and navigation alias the same native mover; physical control needs no Tycoon.
- Explicit initial class and canonical immutable allowed IDs. Foreign reads/management previews
  hide state; foreign commands refuse ownership. Missing/non-motion/unlisted/null/empty profiles
  return typed refusals without substitution. Actual registered vessel class is deliberately unallowed.
- Management and command previews agree; hash remains unchanged through observation/refusal checks.
- Idle configuration creates no destination; same-value selection does not touch entity version.
- Actual uniform path starts physical work travel. Normal in-flight selection replans the same
  mover onto a preferred road, preserving goal/job and accumulated age plus the normal clock step.
  Physical position advances only its actual budget. Travel/selection grant no work/material use.
- Retained snapshots stay frozen; fresh private snapshots read selected state. Original save/load
  retains selected profile rather than initial metadata and continues identical fractional travel.
- Sole-clock pause stops actual position/work; saved twins physically use the road, reach work,
  consume exactly eight materials and earn knowledge only after completing actual productive work.
  Partial-work saved twins finish identically. Independent session keeps its own initial profile.
- Seven invalid authoring cases: absent/empty/null/duplicate classes, initial not listed, invalid
  identity and a declared but unregistered class. They fail initialization; no defaults.
- Each stage is bounded to500 normal updates; timeout includes step/position/profile/motion/orders.

## Boundary and remaining work

Selected state has exactly one owner; extension requires definition/runtime contracts and provider
code/content, not another scheduler branch. Native costs/occupancy/area/input/trip/work rules stay
authoritative. No Oil, Tycoon source/business logic, host/demo, Draw/platform, size cap or time change.
Physical ship/hull behavior, non-tile selection, provider faults/global purity, technology/profile
permissions, loaded cargo/dock-service switching, broader unreachable recovery, fleet batching/
corridors/congestion, CPU/allocation/replay/stress and full P3/P9/P1–P12 remain OPEN.

## Freeze and source inspection

Source/tests/JSON frozen after the final green case. Documentation and accumulated scoped inventory
only afterward. [Source manifest](P9_NAVIGATION_SELECTION_SOURCE_MANIFEST.json) records actual
hashes/local document targets, not whole-repository or full runtime acceptance. Exact inspection
counts:629 paths/127 Markdown/2033 local links/0 broken/0 duplicate paths. Actual git diff --check
exit0 with existing LF/CRLF warnings. Final hash recheck is recorded in the manifest; no broader
runtime gate. Sole GameClock SHA256 remains
339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31.
No commit or push yet; full goal and plan remain active.
