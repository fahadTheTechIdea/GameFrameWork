# P3-S8a — Sole motion state and validated physical route checkpoints

Focused-verified, 2026-10-06: build2.118.0/save67/native mover3. Clean build;435 checks in the two
exact suites below (355 admission;80 physical).55 preceding identity assertion files migrated and
compiled only, not executed. [Evidence](../../docs/migration/P3_MOTION_CHECKPOINT_VERIFICATION.md).
This closes this native durability slice, not full P3 or P1–P12.

## 0. Problem statement

Standard/Movement.cs exposes mutable Status/Goal/Path/PathIndex/TicksMoving and mutates restored
fields before late decode failures. Its saved path has no retained first-segment origin and no
cross-capability check against actual tile presence. This undermines engine-owned entity state and
save/load durability. Existing IMotion, IEntityCheckpointValidation and native Mover remain owners.

## 1. Goals

1. Native Mover owns writable route state; public route facts are read-only and cannot edit it.
2. GoTo validates actual authored space/goal before replacing intent; no arbitrary map-size limit.
3. One staged Sync decodes/validates complete local payload before publication, including route origin.
4. Existing post-component checkpoint hook validates actual placement/current segment/arrival independent
   of JSON component order; changed roads remain legal checkpoints and trigger existing replanning.
5. Exactly two focused suites prove malformed decode retention, real commands/travel/route change,
   mid-segment saves, wide age and saved deterministic continuation. Full roadmap stays open.

## 2. Design

MoverDef resolves its existing immutable catalog and validates a TileSpaceDef alias through actual
ITilePresenceDefinition, not a second bounds store. Native Mover exposes getter-only facts and a
read-only collection over its sole private path. IMotion/IMobility remain unchanged.

RouteOrigin is the actual physical position when the current path is planned. It belongs to Mover
and is saved with that same route, not a second presence model. Pending route creation captures actual
position. Cleared routes clear origin. Local payload admission validates status, nonnegative wide age,
in-bounds goals/origin/tiles, pending/active/terminal path/index shape, adjacent unique steps and final
goal membership. No world-area or path-length cap.

IEntityCheckpointValidation checks actual placed/movable same-space presence; pending motion has
actual origin and arrived motion is physically inside its goal. For a planned active path, current
position lies within the current segment's axis-aligned bounds. This deliberately is bounded segment
consistency, not proof of exact historical floating-point/collinearity or cryptographic save authorship.
Use exact fixed-point values; do not invent an epsilon. Structural route validation ignores changed
cost/access/occupancy: normal provider replanning, not checkpoint refusal, owns those changes.

Native Sync stages all fields and invokes one local validator before replacing them. Full cross-part
validation runs after all payloads decode through the existing entity hook. The overall world restore
is not claimed transactional; invalid session loads are refused rather than returned as playable.

## 3. API surface

Getter-only native route facts and unchanged IMotion/IMobility engine execution methods. Existing
player commands, private MotionView and entity Update remain authoritative. No client game logic.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | Standard/Movement.cs; Standard/TileNavigationModule.cs | high |
| Modify | Kernel/SaveGame.cs and current identity assertions | low |
| Modify | existing WideTimeTests instrumentation and Program registration | low |
| New | tests/Beep.SimEngine.Tests/MotionCheckpointTests.cs | medium |
| New | docs/migration/P3_MOTION_CHECKPOINT_VERIFICATION.md | low |
| Modify | master/P3 parents/AGENTS/README/help | low |

## 5. Errors

Invalid execution request/goal refuses before intent mutation. Invalid status/index/route geometry/
time/candidate stream raises CorruptStateException before native staged publication. Cross-component
mismatch refuses checkpoint/load through the existing pure entity hook. Changed roads remain resumable.
Checked route age exhaustion refuses without earning further travel. No correction/default route.

## 6. Compatibility

No legacy reader/shim. New native mover schema3/save67/build2.118.0 describe the route-origin payload.
No separate route store, scheduler, coordinate owner, random/time source or rendering phase.

## 7. Verification

Build then only these exact complete names, including same-name repair reruns:

- Motion checkpoints: owned route state and staged corruption admission
- Motion checkpoints: physical paths, interruption and saved continuation

Previous/family/third/full suites compile only; no broad gate authorization.

## 8. Risks

Cross-capability checks run after complete decode, not while presence may still hold initialization
values. Partial fixed-point diagonal positions need exact bounding-box admission, not invented
collinearity tolerance. Route costs can change legitimately between saves/updates. Any-provider
state validation, full-world rollback, fuzz/soak/replay/lockstep/allocation remain open.

## 9. Out of scope

Whole-route operating areas, fleet maintenance, broader solver/content requirements, Oil/Tycoon/
hosts/platforms/Draw, global rollback and full framework acceptance.

## 10. Execution order

- [x] Read-only native route authority and authored goal admission.
- [x] Shared native payload validator and staged codec3 with actual route origin.
- [x] Existing cross-capability hook and actual execution guards.
- [x] Two focused suites/build; compile-only current assertion migration.
- [x] Evidence, stage/master/parent/help alignment.

