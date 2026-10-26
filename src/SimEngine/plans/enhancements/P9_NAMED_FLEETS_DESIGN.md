# P9-C11: Contract-owned named machine fleets — Design Document

Current M99/native2.735/save161/replay2: original fleet service rejects terminal reads and
all named control previews before empty/private/missing shortcuts; FleetView clears lists
before its viewer shortcut. Existing service1/view7/state/clock owners remain, no new model.
SAME affected exact case final851/0/0 after preserved239/612 and850/1 attempts. Existing
physical driver now verifies its full0-to126/23command/126checkpoint replayCA21B84E8FB978E4,
saved real hauling and four fresh/restored empty/configured terminal contexts. Three clean
target builds, one metadata-only1067/1047included/20excluded check; no full suite or original
gate closure. [Current evidence/self-review/handoff](../../docs/migration/P9_NAMED_FLEETS_VERIFICATION.md#current-m99-terminal-fleet-observation-and-complete-physical-replay--2026-10-22).

Earlier scoped implementation verified,2026-10-08. Build2.265.0/save140/new fleet codec/view1.
THREE builds: failed6.58s0 warnings/2 errors NEW public proof internal ContractIdentity;
failed5.69s0 warnings/1 error NEW test Maps import; clean3.55s0 warnings/errors. ONE exact NEW
bounded case104 checks0 failures/aborts3404ms FIRST runtime attempt.129 older strict identities/
shared factory module-document assembly compile-only. No older/family/full/platform/stale gate.
Source/public proof/tests/JSON frozen after green; documentation only thereafter.
[Evidence](../../docs/migration/P9_NAMED_FLEETS_VERIFICATION.md). Full P1–P12 remains active.

## 0. Problem Statement

Standard/MotionNavigation.cs owns whole temporary selection admission, but a host must currently
retain its own membership to address that selection again. Named company fleets belong to the
engine, not hosts. They must not become another motion, cargo, job, route or worker-policy owner.

## 1. Goals

1. Optional master-selected module owns stable company/fleet IDs and canonical membership once.
2. JSON fleet policies declare compatible roles and selectable navigation identities; external
   policy definitions use the same contract without coordinator type switches.
3. Membership replacement preflights the whole set. Members belong to at most one named fleet.
4. Named navigation commands resolve current membership and invoke original whole-selection
   admission/commit. No retained preview or host-provided member list authorizes execution.
5. Private immutable policy/member diagnostics, normal previews, pending command saves, original
   ledger hashing and bounded physical saved continuation prove this slice.

## 2. Design

One MachineFleetService implements IMachineFleets/IPersistentService/ICompositionValidator.
Its records contain only company, authored fleet ID and FleetSettings(profile, canonical members).
No reverse membership authority: exclusivity is derived from these records. Empty explicit fleets
are permitted; navigation returns fleet.empty. Removing/editing a fleet never cancels actual work.
Missing/removed members remain explicit blocked IDs until a normal membership edit; never substitute
or silently prune. Saved IDs must have been allocated, but live ownership/eligibility are current
diagnostics, not invented save-time repairs.

FleetPolicyDef implements IFleetPolicy with pure Validate/CheckMember/CheckNavigation plus declared
content dependencies. Native AuthoredFleetPolicyDef requires explicit nonempty unique allowed_roles
and navigation_classes. Roles resolve through the existing WorkerRoleDef catalog. Actual live
IWorker and IMotionNavigation contracts provide eligibility; no concrete WorkerProfile/Mover lookup.
The policy does not force navigation every Update or copy its value: each mover remains authoritative.
Direct individual navigation remains valid; addressing the fleet changes its current members only.

ConfigureFleetCommand replaces one complete membership; RemoveFleetCommand removes metadata only;
ConfigureFleetNavigationCommand carries only fleet ID/profile and delegates to
MotionNavigationContracts.PreviewGroup/ConfigureGroup. Company is original stamped command sender.
FleetView is selected module's immutable company-private projection, not a host gameplay registry.
Provider pure checks use existing scoped observation guards; violations fail-stop, no rollback.

## 3. API surface

IFleetPolicy/FleetPolicyDef/AuthoredFleetPolicyDef; FleetSettings; FleetMemberSnapshot/FleetSnapshot;
IMachineFleets; MachineFleetsModule; three normal commands; FleetView configure/remove/navigation
previews and current fleet/policy reads. No Draw, platform SDK or second game clock.

## 4. Files to change

New Standard/MachineFleets.cs, Standard/MachineFleetsModule.cs, Commands/FleetCommands.cs,
Views/FleetView.cs; build identity; one new bounded test/registration; public engine-side extension
policy proof and master-listed JSON. Existing FleetNavigationTests factory gains document/module
assembly only. Old strict identities compile-only. Stage/parent/master/README/AGENTS/evidence.

## 5. Error handling matrix

Unknown/hidden policy → UnknownDef; unknown fleet/member → NotFound; foreign command member →
NotOwned (private view conceals existence); another fleet membership → fleet.member-conflict;
incompatible role → fleet.member-not-allowed; empty operation → fleet.empty; disallowed class →
motion.navigation-not-allowed. One refusal changes no valid membership/navigation prefix.

## 6. Backward compatibility

Development only. No old save reader, compatibility aliases or duplicated execution path.

## 7. Verification

Build the completed slice, then ONLY ONE exact NEW bounded case. Repair/rerun observed failures
only. Record all attempts. Native physical hauling/loaded route changes and saved twins; independent
public policy exercises the same coordinator. Freeze source/proof/tests/JSON after final green.
Older/family/full/platform/stale gates are not authorized and remain unclaimed.

## 8. Risks

Membership is not reservation or a simultaneous multi-provider rollback guarantee. Saved removed
members retain blockers. The public extension owns gameplay in its declared engine-side module,
not the harness. Scoped guards do not sandbox arbitrary hostile unrelated-world mutations.

## 9. Out of scope

Automatic formation movement, fleet-wide work/home/fuel controls, shared route/cargo stores,
preferred corridors, cross-provider fault matrix, replay/lockstep/stress/CPU/full acceptance.

Subsequent [C12 work-policy controls](P9_FLEET_WORK_POLICIES_DESIGN.md) scoped-verified at2.267.0:
same WorkPolicyRules and current membership, no fleet overlay. Work-policy controls no longer remain
unimplemented; home/fuel/service/load, formation/corridors and full acceptance remain open.

## 10. Sub-task execution order

1. Add contracts/strict native JSON and sole membership service.
2. Compose optional module and ordinary commands/private view.
3. Add public policy extension/master JSON and one bounded native physical proof.
4. Build/exact proof; repair observed errors only; freeze and record source evidence/tracker.
