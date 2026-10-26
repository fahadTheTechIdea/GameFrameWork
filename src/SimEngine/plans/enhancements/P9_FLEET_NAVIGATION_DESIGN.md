# P9-C10b: Engine-owned fleet navigation selection — Design Document

Status: scoped implementation verified,2026-10-08. Build2.260.0/save140/mover4/MotionView11;
persistent owner payloads unchanged. TWO clean --no-restore builds5.63s/3.47s,0 warnings/errors.
TWO runs of ONE exact NEW bounded case: first2 passed1 failed1 aborted150ms (explicit startup
home dependency missing); native JSON corrected only; final82 checks0 failures/aborts17341ms.
Engine source unchanged after first build.124 older build identities compile-only; no old/family/
full/platform/stale suites. Source/tests/JSON frozen after green; docs-only afterward.
[Evidence](../../docs/migration/P9_FLEET_NAVIGATION_VERIFICATION.md). Full goal/plan ACTIVE;
publication only after FULL verified completion.

## 0. Problem Statement

ConfigureMotionNavigationCommand changes one vehicle. A host implementing multi-vehicle policy by
looping separate commands can change a prefix when a later member refuses. The engine must own
complete admission and deterministic target ordering, without another fleet/route/job state store.

## 1. Goals

1. One normal selection command preflights every explicit owned target before any policy mutation.
2. Stable entity ordering, per-member typed private blockers and immutable current selection facts.
3. Same actual motion providers commit; no new group store, route, cargo, reservation or clock.
4. Original command wire/checkpoint records the selection, independent of input target order.
5. One new bounded native case covers refused sets and saved loaded travel through physical docks.

## 2. Design

Extend Standard/MotionNavigation.cs, the existing navigation-policy authority, with immutable
MotionNavigationSelectionEntry/Preview projections and PreviewGroup/ConfigureGroup. Copy and
canonically sort caller IDs, refuse empty/invalid/duplicate sets without inventing members. For a
valid set, inspect every member through existing individual Read/CheckConfigure; aggregate the
first canonical refusal. Foreign management entries conceal existence, normal commands retain
NotOwned. Never expose a foreign selection snapshot. No persistent fleet membership is introduced.

ConfigureGroup freshly preflights the whole set, then invokes existing exact Configure in stable
order. Ordinary admission refusal changes no prefix. A provider violating commit after preflight
faults the original session; no fake transaction rollback or healthy partial success. This is not a
hostile-mod atomicity guarantee. Existing physical movement and haul gates remain authoritative.

Add ConfigureMotionNavigationGroupCommand on existing MotionModule. Explicit entity IDs/profile
are the wire payload; sort a copy when writing, never mutate caller arrays. Null/empty are explicit
invalid requests, not a default fleet. Existing command copying owns input immutability. MotionView
adds PreviewNavigationGroup with detached per-member current policy/blocker facts. Normal command
checks and management preview use the same evaluator, with privacy-only refusal projection.

The native case composes existing JSON sensor/time/world/startup/navigation and a new master-selected
fleet document defining actual native docks, handling, cargo capacity and additional starts. No
test-authored gameplay provider. Normal route/work-policy/navigation commands cause acquisition,
physical queueing/loading/travel/unloading. Selected classes may change while cargo is loaded or a
carrier waits: actual job/cargo/receipt/queue owners stay unchanged and only physical Update advances.

## 3. API surface

MotionNavigationSelectionEntry/Preview; MotionNavigationContracts.PreviewGroup/ConfigureGroup;
ConfigureMotionNavigationGroupCommand; MotionView.PreviewNavigationGroup. Result Arg0 is admitted
member count, Arg1 is actual changed selection count. No group entity or fabricated fleet ID.

## 4. Files to change

Existing navigation contracts/commands/module/view/build identity; one new test/registration;
one new native fleet JSON document/resource inclusion; older strict build metadata compile-only;
stage, parent/master tracker, evidence and scoped manifest. Save140/mover4 remain unchanged;
MotionView11/build2.260.0 describe the new command/private projection.

## 5. Error handling

Malformed sets return InvalidArgument and no member prefix. Valid missing/foreign/unsupported/
unallowed members return stable per-entry typed reasons; the whole command refuses before commit.
Apply rechecks current state rather than trusting a preview. Provider commit violations fail-stop.
No class/vehicle/default substitution, receipt release, teleport or duplicate material transfer.

## 6. Backward compatibility

Development only; direct current protocol replacement. No shim or alternate reader.

## 7. Verification

Build completed slice; run ONLY one exact NEW bounded case. Repair/rerun ONLY observed failures.
Older identity/schema/content/API migrations compile-only. No old/family/full/platform/stale suites.
Record every invocation; freeze source/tests/JSON after final green, then documentation only.

## 8. Risks

Fleet selection is an explicit command target set, not persistent membership or route reservation.
Allowed profiles must match game-authored vehicle capabilities. Current preview is not a future
reachability/fuel/traffic guarantee. No global provider purity/rollback/CPU budget claim.

## 9. Out of scope

Named persistent fleets/formations/corridors, physical vessel/hull/3D behavior, technology rules,
hostile providers, loaded fuel/reserve matrix, endpoint destruction and full replay/stress/P9/P1–P12.
No Oil/Tycoon source/business, host/demo/Draw/platform. Publication only after FULL verified finish.

## 10. Sub-task execution order

1. Whole-set engine evaluator/commit and command/private view, with no extra state owner.
2. Master-selected native dock/cargo authoring and one normal-player physical/save proof.
3. Build and ONE exact new case; observed repairs only; source freeze after green.
4. Evidence, parent/master tracker and scoped hash/local-link inventory; retain open requirements.
