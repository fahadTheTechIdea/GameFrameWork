# P3-NT1 — Native traffic progress

## 0. Problem and specification

P5-MF1 exposed native trucks oscillating beside a narrow approach for multiple
game days. `Standard/Movement.cs` clears a route whenever `TrySegment` encounters
a blocked cell; `Standard/Navigation.cs` searches current occupancy only. The
next action is a small, generic reconstruction before choosing the correction.
This supports the original physical-logistics gates, not a replacement for them.

## 1. Goals

1. Reproduce opposing slow movers using native authored geometry and ordinary
   movement commands, without Oil classes or host steering.
2. Make the existing native movement owner resolve the reproduced interference
   without overlap, teleportation, altered speed, ignored corners or new clocks.
3. Prove deterministic saved continuation and cancellation using the same route
   owner, with relevant focused regression evidence.

## 2. Design

First use the existing `LiveSensorTests.Factory` composition and master-list an
explicit `native-movement-traffic.json` fixture. Four generic static rectangles
and two native vehicles reconstruct the competing approaches at a smaller scale.
`NativeTrafficTests` submits normal `MoveVehicleCommand` inputs and observes
actual positions and route state for a bounded240Updates. No test-side coordinator.

The baseline reproduces the cycle:244 assertions passed/1 failed at240Updates.
The selected correction adds `ITileMotionClearance : IMotion` in Map/TilePresence.cs.
`ITileMover` includes that capability. Original Mover.ReadMotionClearance derives
the adjacent segment's1x1-to2x2 swept rectangle from its existing PathIndex,
RouteOrigin, path and actual physical position. Idle, unstarted and ended segments
have no clearance. No saved claim, new timing policy or reservation store.

Original NavGrid.EntryCost includes guarded clearance only for dynamic entry.
At most nine existing occupancy cells can contain an adjacent segment's owner;
each actual provider/owner/space/full-entity codec is observed through the existing
EntityObservation boundary. Nonmoving, oversized, off-map or unrelated geometry
refuses. Static/structural probes deliberately ignore this transient clearance.
Diagonal clearance includes both corner cells, matching existing corner safety.

A second focused baseline proves a blocked choke could prematurely terminate
the trailing vehicle as Unreachable (25/1). Original Mover.Advance now leaves
the same goal/origin pending when dynamic search fails but the original structural
probe succeeds or exhausts its search budget. Actual structural failure still
finishes Unreachable. Existing motion operating gates and costs remain unchanged.

This is a physical safety invariant, not a fabricated gameplay default. Speeds,
navigation costs, topology, states and the single clock remain authored by the
existing definitions. No delay counter, second scheduler, Oil lookup or maneuver
command is introduced. Build2.647.0 deliberately changes simulation identity;
save160/replay1/mover4/movement6 payload schemas remain unchanged.

## 3. API surface

Pure module capability `ITileMotionClearance.ReadMotionClearance(): TileRect?`.
No host, renderer, platform or business API and no compatibility fallback.

## 4. Files

- New `tests/Beep.SimEngine.Tests/NativeTrafficTests.cs`.
- New master-listed `tests/ExtensionProof/content/native-movement-traffic.json`.
- Existing proof embedded resource and runner registration.
- Independent public-contract fault probe and master-listed probe JSON.
- Existing `Standard/Movement.cs` / `Standard/Navigation.cs`, if baseline confirms
  the defect there. Existing engine/protocol identity only if behavior changes.
- This stage, evidence, master tracker, README and AGENTS.

## 5. Errors

Invalid content remains a startup failure. Missing routes, static obstruction,
area refusal and provider faults retain their current explicit semantics. A
timeout is a failed assertion, never permission to grant arrival or materials.

## 6. Compatibility

Development only; no legacy loader/header rewrite. Existing2.646 native Gulf
checkpoints remain preserved as historical evidence if build identity changes.

## 7. Verification

Targeted Release build and literal exact case above. Add saved/twin/stop and
directly affected route checks after reproduction. No broad/family/full suite.
Later canonical Oil fleet acceptance remains separate and incomplete.

## 8. Risks

Reservation overreach can deadlock traffic or hide a structurally valid route;
test current versus future occupancy and partial-segment cancellation. A passing
two-vehicle fixture is not proof for every fleet, congestion layout or provider.

## 9. Out of scope

Oil-specific fixes, host maneuvers, second dispatcher, infinite wait budgets,
unearned cargo, cost/duration reductions, business rules, rendering and Git
publication. The full original221-requirement scope remains unchanged.

## 10. Execution order

1. [x] Reproduce actual generic interference; retain baseline diagnostics.
2. [x] Correct the existing movement owner and explicit identity as necessary.
3. [x] Verify targeted physical, durability and refusal cases; update evidence.

This supporting correction does not close any of the original eleven open gates.

[Evidence](../../docs/migration/P3_NATIVE_TRAFFIC_PROGRESS_VERIFICATION.md).
