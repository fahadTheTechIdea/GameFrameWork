# P4-G10 — Open fleet and activity-facility policy catalogs

Status: scoped verified107 checks; full P1–P12 remains open. [Evidence](../../docs/migration/P4_SERVICE_SELECTION_CONTENT_CONTRACTS_VERIFICATION.md).

## Specification and goals

Before this slice, MachineFleetService selected FleetPolicyDef despite its existing IFleetPolicy contract. RecurringPlanContracts.CheckFacilityPolicy and WorkBoard.ReadFacilities similarly selected ActivityFacilityPolicyDef. Games must author master-listed policies through actual content contracts rather than native base inheritance.

1. IFleetPolicy becomes an IContentDefinition and every native fleet catalog/admission/view/save lookup resolves that contract.
2. FleetPolicyDeclaration and ActivityFacilityPolicyDeclaration own the existing native JSON readers and stateless decisions; native and independent implementations share them.
3. Validate actual catalog ownership, dependencies, navigation metadata and facility node identities before Initialize. Bound service graphs still use the original machine-node checks.
4. Preserve sole membership, recurring intent, facility readiness/ranking, worker/dock/resource claims, physical execution, state codecs and GameClock.
5. One new bounded player-only proof loads independent fleet/facility definitions through explicit master declarations and exercises physical native hauling and service, preferences, refusals, saved twins and replay.

## Design and API

- Standard/MachineFleets.cs: IFleetPolicy extends IContentDefinition; FleetPolicyDeclaration exposes resolved roles/dependencies/classes and original validation/member/navigation decisions. AuthoredFleetPolicyDef delegates. MachineFleetService uses actual IFleetPolicy aliases and existing guarded admission.
- Standard/ActivityFacilities.cs: ActivityFacilityPolicyDeclaration reads required node/prefer_home and owns native preference. Composition validates all actual IActivityFacilityPolicy aliases and canonical node IDs.
- Standard/RecurringPlans.cs and WorkFacilities.cs: resolve IActivityFacilityPolicy; guarded Rank still sees detached safe facts only. Preference never overrides graph, machine, travel, dock or material admission.
- Existing fixture-authoring helpers accept explicit extra module/declaration/document inputs, without adding any runtime gameplay.
- ExtensionProof registers definitions derived directly from Def and implementing contracts, from a new master-listed service-selection-contracts.json. No scheduler modification or new membership/intent store.

## Errors and compatibility

Missing/duplicate/unknown roles, empty navigation lists, malformed node IDs, unknown/wrong profile bindings and missing typed dependencies refuse before Initialize. Runtime unknown profiles or incompatible sets use existing typed refusals and leave all admitted members unchanged. No defaults, compatibility shim or legacy resolver.

## Verification

Build Beep.SimEngine.slnx. Run only one new --bounded-exact case; repairs rerun that same case only. Point facility and native tile fleet authoring fixtures are inputs to that new case, not reruns of historical suites. Per-workflow waits are bounded with immutable diagnostics. Check source boundaries, current hashes, doc links and original acceptance records; preserve all original text/checkboxes.

## Risks and limits

Policy content may compose before runtime services: direct aliases are checked from the resolved catalog, not prematurely from another module's service binding. Facility policies identify a node relative to their bound workflow, not a universal globally existing node. Existing finite physical bays and routing rules remain authoritative.

## Out of scope

Oil, Tycoon, money/budget, platform/Draw/adapters, new transport media, broad/historical runtime regression, alternative clocks and publication before full completion.

## Execution order

1. Shared declarations, contract consumers and composition guards.
2. Independent master-listed definitions and reused explicit authoring hooks.
3. Bounded physical player proof, build and source review.
4. Evidence, master tracker and original completion inventory reconciliation.
