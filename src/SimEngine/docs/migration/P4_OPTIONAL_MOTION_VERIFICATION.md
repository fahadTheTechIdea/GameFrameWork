# P4-C1b1 independent motion/navigation verification

Date: 2026-10-04. Working-tree build `beep-simengine-2.25.0`; persisted layout 11 unchanged.

## Transferred responsibilities

- MotionModule (`sim.motion`) alone registers the existing MovementSystem at order 410. It updates
  actual IMotion providers without requiring space, inventory, work, logistics, construction or economy.
- TileNavigationModule (`sim.tile-navigation`) explicitly requires motion and selected tile space;
  it alone declares MoverDef and binds NavService/registers the existing tile-move command. The same
  Mover position/progress codec and NavService sparse router remain, not a parallel manager or facade.
- PhysicalWorkModule no longer registers mover/navigation/motion/update/move-command ownership.
  It explicitly requires generic motion for physical jobs. Its active tile-line content declares a real
  NavService requirement; a non-tile work scenario does not select tile navigation implicitly.
- ColonyModule explicitly requires tile navigation. PointModule still uses generic motion through its
  captured work dependency, without selecting tile navigation. Affected standalone registrations and
  literal master manifests capture the new exact dependency/owner identities and compile.
- NavigationProofModule is independently compiled against public engine contracts. JSON authors time,
  space, route costs, entity states/definitions and starting poses, through the existing master loader.
  Its initialization counter is test instrumentation, not a gameplay state store or global scheduler.

## Exact commands and results

Run from `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Optional motion: independent tile movement, refusals and saved sessions"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Provider motion: non-tile work, home travel and saved lifecycle"
```

Final build: zero warnings/errors. Independent tile motion: **30 passed / 0 failed**, 109 ms.
Non-tile work/home travel/lifecycle: **32 passed / 0 failed**, 143 ms. Combined **62 checks / two exact
named suites**. Only those two suites ran/re-ran. The second suite's final run preceded addition of two
manifest-refusal assertions to the first; no runtime or second-suite source changed after that run.
Previous optional-process/selected-content/strict-content results remain historical source evidence.

## Proven behaviors

- A player moves an ordinary entity (not vehicle-kind-only) via the existing public tile command.
  No work board, inventory, logistics, construction, economy or content-access provider is registered.
  The authored rate produces real travel over game time, not an immediate coordinate change.
- Occupied destinations refuse in preview. Actual travel goes around a blocking entity, reaches the
  exact target centre and returns to its authored idle state; previews do not change the session hash.
- Save/load preserves active route and actual/interpolated pose. Interleaved saved sessions finish
  identically, with provider/module identity recorded by the existing save mechanism.
- Missing selected tile space, movement class and declared motion dependency refuse before any
  initialization callback. Navigation declarations remain frozen during gameplay.
- Existing non-tile work travels, completes work and returns home through PointMotion/PointSpace
  contracts. Active/saved continuation hashes remain identical and foreign endpoints are refused.
  The scenario selects MotionModule and not TileNavigationModule; the unselected tile command
  returns the ordinary UnknownCommand refusal, without invoking a missing-service path.
- The same second suite starts normal tile-colony movement, verifies its component contract aliases,
  one TicksMoving increment per update, and entity-owned authored movement state after systems.

## Test expectation corrections

The initial independent test expected StateSince=1 on the first completed update. Existing engine
semantics stamp the executing tick index (0 before the first completion), not the subsequently
completed tick count. The assertion now uses game.Tick-1; lifecycle/time logic was not modified.
The older point-space assertion expected a registered tile handler's NotMovable refusal. With tile
navigation unselected, the correct refusal is UnknownCommand. No compatibility registration was added.

## Remaining boundaries

This is C1b1, not completion of C1b or the overall framework plan. Physical work/construction/
logistics/network/response are still grouped and still have a blanket economy requirement. Further
decomposition needs contract-based work-supply/cancellation ownership and separate management views.
Resource atomic transactions, authored construction/salvage, DAG/executor migration, process contracts,
traffic queues/deadlocks, controls/incidents and pending full stage gates remain open.

All older affected fixtures compile, not necessarily pass. No full/broad suite or oil-integration claim.
Only standalone SimEngine source/proofs/docs changed; no oil, Tycoon, host/demo gameplay, renderer,
Unity/Godot or adapter edits. The lifecycle skill guided staged single ownership/evidence; repository
and user instructions override its generic full-regression recommendation.
