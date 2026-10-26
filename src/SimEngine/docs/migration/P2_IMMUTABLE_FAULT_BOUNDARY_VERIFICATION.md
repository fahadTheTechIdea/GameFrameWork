# P2-F1 immutable first-failure verification — 2026-10-06

Source build `beep-simengine-2.146.0`; save format76, existing codec/view layouts unchanged.
No previous-build reader or field compatibility API. All64 current source/test build identity files
were updated through apply_patch; unrelated suites were compiled, not executed.

## Final gates

`dotnet build Beep.SimEngine.slnx --no-restore`: succeeded,0 warnings,0 errors; MSBuild2.29s.

| Exact suite | Passed | Failed | Suite runtime |
| --- | ---: | ---: | ---: |
| Lifecycle: entity state faults latch before snapshot publication | 78 | 0 | 78ms |
| Resource replacement: timed geometry, auxiliary state, refusals and fault checkpoints | 112 | 0 | 388ms |

Total190 checks, two suites, no final aborts. Each ran through
`dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "<exact suite name>"` after a clean build.
No third/family/full test run. Preceding P10-A9 242 and other historical counts were not rerun/additive.

## Covered behavior

- The existing WorldFault has no public fields, setters or constructors; no second snapshot/model.
- Actual submitted command calls World.Spawn and an IEntityInitialization probe fails. Escaping,
  wrapped and swallowed exceptions preserve the original entity.initialize phase/message/stack.
- Earlier successful command retains its actual pending spawned identity and command result.
  Failed/later commands produce no success result; pending flush/entity state phase do not continue.
- Fault is visible at Updated publication, the previous healthy snapshot stays immutable, later
  replacement fault attempts retain the identical first record, and another session remains unchanged.
- The sole clock does not advance on partial failure; accumulated pacing backlog is halted. Pause,
  resume and diagnostic update batches cannot restart the terminal session. Saves/hashes refuse.
- Existing nested timed placement and replacement tests exercise auxiliary state, actual stock and
  receipts, geometry/refusals, failed resource providers, saved normal continuation and fault checkpoints.

First run:67 lifecycle checks passed with3 incorrect pending-list count assertions and one stale
fault-hash assertion causing an abort. Replacement passed24 before an undeclared supply resource
in its default point handling fixture aborted admission. Test corrections use actual pending identity
semantics, current faulted checkpoint refusal, and explicit alpha handling content. No engine stock,
clock, admission defaults or fallback were added to make those fixtures pass. Final gates supersede
that run; the same two exact suites were repaired/rerun only.

## Source fingerprints (SHA256)

| File | SHA256 |
| --- | --- |
| Core/Fault.cs | F5FF35806F564B34C962144F6E77E5B07DA2AB19EDA5BC86FF7814C7511A5EE2 |
| Kernel/World.cs | DE6D00DAB98EC71E3AADF7FCC4E0728DE94486192234F004DB3816C54ED6F9C8 |
| Tests/FaultBoundaryTests.cs | 0732DD8B437948DC980CABFE08DCE7804B454BC48EDCE4ACE9D917CF19C038C6 |
| Tests/LifecycleTests.cs | F529EFAB5B3286D706DFB0173E9D2B4B725578BCF759F8C557D1FD82086DC513 |
| Tests/ResourceReplacementTests.cs | 0DADD58BB4F1B5770AC489899EDC18B2871D44946247CF1E0109E8F4ADFF0441 |
| Hosting/GameClock.cs (unchanged) | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |

World constructs the only GameClock once; Tick remains its read-only StepIndex projection. Fault Tick
is a diagnostic coordinate, not another clock. World has one WorldFault construction in LatchFault.

## Limits

This is F1 proof only. Callbacks cannot be forcibly preempted; checks stop subsequent work after
control returns to the engine. Event-handler fanout/internal provider work is not separately preempted.
No complete shared-content freeze, protocol argument validation, arbitrary callback exception hardening,
full lifecycle/replay/stress/allocations, oil parity or P1–P12 completion claim. No Oil/Tycoon/host/demo/
platform/Draw changes. Fault details are developer diagnostics, not safe player-localized strings.
