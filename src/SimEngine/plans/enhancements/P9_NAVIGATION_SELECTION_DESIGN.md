# P9-C10: Player-owned navigation profile selection — Design Document

Status: scoped implementation verified,2026-10-08. Build2.259.0/save140/mover4/MotionView10.
ONE clean build5.94s/0 warnings/errors; ONE exact NEW bounded case68 checks0 failures/aborts595ms,
FIRST attempt.123 older build identities/120 save assertions/mover schema and21 JSON files with28
native mover definitions compiled only. No old/family/full/platform/stale suite. Source/tests/JSON
frozen after green. [Evidence](../../docs/migration/P9_NAVIGATION_SELECTION_VERIFICATION.md).
Full goal and plan remain active; publication only after FULL verified completion.

## 0. Problem Statement

Before this slice, native Mover always used MoverDef.MovementClass. Players could not choose between authored route
preferences during a job. A separate host routing policy or copied fleet state would violate ownership.

## 1. Goals

1. Explicit JSON lists allowed provider navigation identities; initial class must be listed.
2. Optional coordinate-neutral IMotionNavigation aliases the actual motion capability and owns selection.
3. Same Mover retains selected class, goal, actual position and motion age; its original path is replanned.
4. Normal command/private preview/view, original Sync/hash/replay and sole-clock Update govern changes.
5. One new bounded case proves native in-flight preference switching and physical work/save continuation.

## 2. Design

Add Standard/MotionNavigation.cs contracts, immutable selection snapshot and guarded Read/CheckConfigure/
Configure. IMotionNavigationDefinition declares InitialNavigation and NavigationClasses; the provider
checks actual compatible identities. These are provider navigation identities, not a second global
content registry. No kernel tile branch. Custom geometry can implement the same contract/definition.

MoverDef implements this optional definition and requires classes explicitly; migrate all native
JSON mover entries in place, including singleton lists. No inferred default, old reader or shim.
NavService validates every native allowed class is actually registered in the selected session.
Existing MovementSystem composition validation rejects unmatched definition/runtime declarations;
live validation verifies the actual same motion owner and reads selection through its original codec.
Mover owns one selected string, initialized only on creation and included once in original Sync.
Initial authored MovementClass remains creation metadata, not a second current selection.

Configure checks declared class and actual native grid. On a real change, clear the existing path,
rebase its pending origin to actual fractional position and preserve goal/motion age/work/cargo.
Unreachable retained movement may retry; idle/arrived machines do not receive a new destination.
Same-value selection is an idempotent no-op. Configuration is permission to select an authored class,
not a guarantee the current route is reachable, supplied or free of traffic. Normal Update applies
existing movement, area, operating-input, trip-budget and work rules; never teleport or grant work.

Register ConfigureMotionNavigationCommand on MotionModule, with entity/profile wire fields. The
handler checks ownership and delegates. MotionView adds private read and matching management preview.
Provider reads/checks use the existing scoped observation guard; commit verifies selection/status/
owner/identity/version/clock. This is scoped verification, not a hostile-mod sandbox.

## 3. API surface

IMotionNavigationDefinition/IMotionNavigation; MotionNavigationSnapshot; MotionNavigationContracts;
ConfigureMotionNavigationCommand; MotionView.ReadNavigation/PreviewNavigation. Existing actual
motion/cargo/worker execution owners remain unchanged. Selection is saved in native mover codec4;
MotionView10 and save140/build2.259.0 directly replace current identities, no legacy decode.

## 4. Files to change

New contract and command files; existing Movement/MotionModule/MotionView/Navigation; native mover
JSON entries and one earlier test author's class override; build/save/strict metadata expectations;
one new test/registration and stage/tracker/evidence/inventory. No new gameplay service or loop.

## 5. Error handling

Missing/empty/duplicate/invalid lists, unlisted initial class and unregistered allowed class fail
initialization. Missing/not-motion/foreign entity or unavailable selection returns typed refusal.
Unknown/unallowed profile never substitutes another. Corrupt saved selected class refuses. Pure
provider mutation faults via original guard; no fallback path/cargo repair or instant arrival.

## 6. Backward compatibility

Development only: direct native authoring/codec migration. No compatibility overload/default.

## 7. Verification

Build the completed slice, run ONLY one exact NEW bounded native case. Repair/rerun ONLY observed
failures. Older identity/layout/JSON migration compile-only; no old/family/full/platform/stale suite.
Freeze source/tests/JSON immediately after final green; docs-only afterward. Record every invocation.

## 8. Risks

Profile selection changes navigation preferences, not vehicle hull or transport medium safety. Allowed
classes must be game-authored appropriately. Provider-specific technology/permission gates can refuse
through CheckNavigation; this slice does not claim that wider matrix or external geometry acceptance.
Dynamic routes are not future traffic/fuel guarantees; no CPU/allocation budget claim.

## 9. Out of scope

Fleet-wide batching/corridors, physical vessels/hulls, unreachable retry breadth, hostile providers,
selection during retained dock/cargo service, technology gates, hydrology/3D/replay/stress/full P3/P9/
P1-P12. No Oil/Tycoon/host/demo/Draw/platform code. Commit/push after FULL verified goal/plan finish.

## 10. Sub-task execution order

1. Same motion-owner selection contracts/native state and explicit JSON/schema migration.
2. Normal command/private view and actual native in-flight preference/physical work/save case.
3. Build, ONE exact new case, observed repairs only, freeze source after green.
4. Record evidence/inventory, update master tracker and retain broader open requirements.
