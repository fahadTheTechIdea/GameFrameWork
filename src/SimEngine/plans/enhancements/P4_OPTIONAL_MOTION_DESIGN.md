# P4-C1b1 — Independent motion and tile navigation

Status: focused-verified, 62 checks / two exact suites; build 2.25.0 / save 11.
See [source-specific evidence](../../docs/migration/P4_OPTIONAL_MOTION_VERIFICATION.md).

## Problem and scope

Before this slice, PhysicalWorkModule registered MovementSystem and tile-specific MoverDef/NavService.
A simple moving entity consequently selected work, logistics, construction and economy.
This slice transfers those registrations to independent module owners, without another update loop,
route store, position model, compatibility bundle or default gameplay provider.

MotionModule (`sim.motion`) owns only the existing MovementSystem at order 410. It depends on no
space, inventory, work or money capability. It advances actual IMotion providers; Entity.Update
remains the only state-transition owner after systems. TileNavigationModule (`sim.tile-navigation`)
depends on motion, owns MoverDef/MoveVehicleCommand/NavService and explicitly requires tile space.
NavService still requires authored movement classes before Initialize and owns its existing router.

PhysicalWorkModule requires motion for existing physical jobs. It no longer owns mover/nav/update/
move command registrations. Selected infrastructure requires a real NavService; point-space work
selects no tile-navigation capability. The remaining physical work/construction/logistics/network/
response group and its blanket economy requirement remain separate unfinished C1b items.

## Implementation and migration

- Create Standard/MotionModule.cs and Standard/TileNavigationModule.cs; transfer registrations.
- Update PhysicalWorkModule and every affected standalone registration/master manifest in place.
  ColonyModule explicitly requires tile navigation; point/mapless declarations do not select it.
- Keep MovementSystem and Mover codecs, command identity, state resolution and fixed timing intact.
  Saves carry changed module/system owners and build identity; no old module/schema compatibility.
- Add a public-contract proof module with JSON-authored route cost, starting entities/poses and time.
  Content uses ContentSet and registered definitions, never a second loader or host decisions.
- A player drives MoveVehicleCommand; movement, collision, timing, state and save continuation are
  computed by the existing engine. Tests assert views and instrumentation, not a new game algorithm.

## Exact acceptance gates

Two named suites: standalone tile movement/refusals without work/economy, and
coordinate-neutral physical work/home travel continuation. Include saved/interleaved twins, state changes,
non-instant physical travel, absent tile/motion providers and missing class refusal before Initialize.
Run the solution build. Compile older fixture migrations but do not run broad/full suites.

No renderer, Draw, platform adapter, oil/Tycoon or host/demo gameplay changes. This slice does not
close all C1b, P3 traffic stress or later DAG/resource/process stages.
