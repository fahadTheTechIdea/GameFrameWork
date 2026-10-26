# P3-S8a focused verification — native motion authority and route checkpoints

Date: 2026-10-06. Workspace: `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`.
Runtime: **beep-simengine-2.118.0 / save67 / native mover3**.
Native map6/public point space4/WorkBoard23/movement system4/work system10/views remain unchanged.

## Outcome and evidence boundary

This native motion durability slice is implemented and focused-verified, not full P3 or P1–P12.
Exactly two complete suite names were executed; no preceding, family, third or full gate ran.
55 preceding current build/save assertion files migrated and compiled only. Existing WideTime
instrumentation now uses a private setter through test-only reflection; its suite did not run.
Previous physical extraction/provider/arrival/dock counts are historical, not current regression proof.

Final build: `dotnet build Beep.SimEngine.slnx --no-restore`: success, **0 warnings / 0 errors**,
6.55 seconds reported by MSBuild.

| Exact suite name | Passed | Failed | Suite duration |
|---|---:|---:|---:|
| Motion checkpoints: owned route state and staged corruption admission | 355 | 0 | 534 ms |
| Motion checkpoints: physical paths, interruption and saved continuation | 80 | 0 | 495 ms |
| Total | 435 | 0 | two suites |

Each invocation: `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "<exact suite name>"`.
Repair reruns selected these same names only. An early physical fixture chose occupied destinations
and assumed a submitted command had not yet run its engine movement phase; the fixture was corrected
to valid player destinations and an explicit pre-Update engine contract probe for pending state.
A preliminary fixture compile error was fixed before rerunning tests; no stale binary was executed.

## Actual implementation owners

Native Mover has private writable status, goal, path, index, age and route origin. Public native facts
are getter-only; the sole path is exposed through a read-only collection, not another route authority.
This engine-side collection is live read-only data, not a claim that retained host snapshots mutate;
hosts continue using existing private detached MotionView/snapshot contracts and cannot obtain World.

MoverDef aliases the actual immutable authored TileSpaceDef through existing content resolution and
component validation. Goal/reach requests check authored bounds using widened intermediates rather than
adding a platform coordinate/path/world-size cap. Foreign-session reach/advance requests refuse.

RouteOrigin is the real fixed-point position when that path is planned. It is route metadata, not a
second coordinate/presence owner. GoTo captures it, actual replanning refreshes it, and ended routes
clear it. Stop also removes the inactive goal without moving actual presence. Changed route costs,
roads/access and occupancy are deliberately not structural checkpoint errors; actual pathfinding
continues to own interruption and deterministic recovery.

One Mover.Sync stages the entire candidate payload before local publication. Raw status integers are
validated before byte-enum conversion, so negative/wrapped encodings cannot silently become Idle.
Native phase/index/age/goal/origin/step bounds, adjacency, nonduplicate consecutive steps and terminal
goal membership validate through one local function. Idle has no stale goal; ended phases have no
active path/origin. There is no legacy reader or missing-origin synthesized default.

The existing IEntityCheckpointValidation hook checks actual placed/movable/same-space presence,
pending origin, current segment bounds and arrived goal membership after every entity component has
decoded. Component declaration order cannot cause it to validate initialized placeholder geometry.
Regular Advance checks current physical presence without rescanning the full historical path every
tick. Wide age and fixed-point tick budget preflight before route/age/presence mutation.

No new queue, scheduler, coordinate store, random/time source, renderer, platform or game-loop path
was added. IMotion/IMobility, MovementSystem, actual navigation, private views and entity-owned state
arbitration remain the same engine owners.

## Focused coverage

Admission proves actual mover codec3 ownership/catalog aliases, private setters and read-only route
mutation refusal. Invalid/out-of-bounds/overflow-prone goal requests and foreign execution/reach
sessions retain actual native state. Full malformed payloads cover raw status encodings, negative
age, index/phase conflicts, empty/duplicate/disconnected/off-map routes, invalid origin/goals,
terminal route state and stale Idle goals. Every truncated prefix of an actual native route payload
is rejected before publication and preserves the original full world hash.

Valid local decode and whole saves roundtrip. Actual geometry moved outside its current segment
refuses through the checkpoint hook; bad live native phase also refuses writer/hash. Foreign player
motion diagnostics remain private. These direct mutations/reflection calls are test-only fault
instrumentation, not runtime player commands or simulation decisions.

Physical coverage uses ordinary player commands and real engine Update for straight/diagonal
fractional movement, saved continuation and arrival. A pre-Update engine execution seam proves pending
route saves without a fabricated plan. Reordered JSON decodes the mover before tile presence and
still validates only against completed actual geometry. Saved twins continue identically.

Changing a current route tile to water remains a legal checkpoint. Actual next movement reopens
the same provider search from current physical position; reopening access completes normally and
saved hashes converge. Stop clears only intent, with no teleport, and saves. Native elapsed age
crosses int.MaxValue without narrowing, survives saves and refuses long.MaxValue increment before
movement. An unrepresentable tick budget likewise preserves route/presence/age. Physical waits use
a 500-update bound with tick/status/index/path/age diagnostics.

## Remaining work and limits

Segment admission uses exact axis-aligned bounds, not an invented collinearity epsilon. It proves
bounded consistency with the current segment, not cryptographic save authenticity, precise historical
trajectory or arbitrary-provider correctness. Full world decode is not transactional: a refused
session is not returned as playable; local staging does not promise universal rollback.

Independent-provider checkpoint breadth, whole-route/vehicle-envelope operating areas, fleet service,
hydrology/replenishment and broad replay/lockstep/shuffled-arrival/fault-injection/fuzz/soak/allocation
and P1–P12 acceptance remain open. No Oil/Tycoon/host/demo/platform/Draw changes or arbitrary world cap.
Generic complete staging/geometry validation for every engine component is not claimed by this slice.

## Verified source fingerprints

SHA-256 captured after final build/two named suites, before documentation-only updates. These identify
working files, not a clean committed HEAD.

| File | SHA-256 |
|---|---|
| src/Beep.SimEngine/Standard/Movement.cs | 57838B227A3D977725EB1FE522C2B2AEE1F5A7900F50CB21A43776339FB868FA |
| src/Beep.SimEngine/Standard/TileNavigationModule.cs | F6D06FCC588D6684414E729D04774AD670B608B0397F402F1FA7406C6BABFEE6 |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 20A346A60369CEE357A7E7E8662EDA3C0726B19E3EEF2F04E046E112C7571671 |
| tests/Beep.SimEngine.Tests/MotionCheckpointTests.cs | D438010310846C75AAB666743562F7ED82A3036A797EC942E560189C60C0531F |
| tests/Beep.SimEngine.Tests/WideTimeTests.cs | B210275044814F370519A11B53BF349F18498B3AB9167A117761B1A16819EE64 |
| tests/Beep.SimEngine.Tests/Program.cs | CB695D015B3AD7C8B67BFCA4A52AED76315165A61406B9CAE2785E7F9C10E93F |

