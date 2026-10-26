# P7-ER5: System reference-owner participation

## 0. Problem

Upgrades.ReferenceOwners visits service aliases and entity components, but not engine systems. A mod system implementing IEntityReplacementReferenceOwner is silently skipped unless also bound as a service.

## 1. Goals

Include actual systems in deterministic engine order; deduplicate service/system aliases by identity; preserve pure admission, typed refusal and first-fault semantics; prove timed replacement and saved continuation for a non-friend system-only mod.

## 2. Design

Extend the existing Upgrades enumerator: services, declared systems, live entity components, prepared destination components. One instance participates once. All callbacks continue through the original observation/replacement protocol; no second registry or event-based fix.

An independently compiled JSON-configured proof module declares two system-only owners and one service/system alias in scrambled registration order. Their existing system Sync owns reference/counter facts. Public commands/views and staffed upgrades demonstrate exact callback membership/order, pure refusal/mutation rejection and commit faults.

## 3. API/state

Existing IEntityReplacementReferenceOwner contract now explicitly covers systems. Build2.562.0/save157/replay1;no new core payload. Proof systems declare their own codec1.

## 4. Files

Upgrades, Entities contract documentation, SaveGame/current protocol; independent proof module/settings/tests and master/evidence.

## 5. Errors

Typed system refusal leaves source/reference state intact. Mutating admission or throwing commit faults the existing boundary with no healthy save. Alias enrollment never doubles commits.

## 6. Compatibility

No old API/save facade, duplicate reference registry or host repair.

## 7. Verification

One new exact system-owner case and affected existing reference case;solution build. Native physical timing, saved pending twins, once-only order, private facts and session isolation. No full regression.

## 8. Risks

Service aliases retain their established first position; remaining systems follow actual declared order. Enumeration membership is not proof that every native/domain system has migrated its references; those owner migrations remain open.

## 9. Out of scope

Unrelated native system migrations, Oil/business/clock/platform/host changes, full tests and Git writes. Full plan unchanged.

## 10. Execution

Extend sole enumerator;author independent system proof;verify physical/refusal/fault behavior;record scoped evidence/master audit.

Status: implemented and scoped verified. New system-owner72 and affected entity-owner96 checks:168/0;one clean solution12.68s. No runtime repair/rerun or executable changes after finals. This closes the missing enumeration path, not every native/domain reference migration. Original221/194recorded checked/27open unchanged;full goal open. See [evidence](../../docs/migration/P7_SYSTEM_REFERENCE_OWNERS_VERIFICATION.md).
