# P3-S38 — Spatial notification invariants

## 0. Problem

Spatial publication catches thrown callbacks, but a notification could silently pause/change the sole clock or touch unrelated entity state and still return successfully. That violates storage-owner isolation even when the requested samples publish atomically.

## 1. Goals

1. Reject clock/random/identity and existing-entity fact changes during sample publication using original engine guards.
2. Preserve legitimate sample/cursor/resource endpoint publication and first-fault behavior.
3. Prove throwing, clock-mutating and entity-mutating native/independent notification paths, with scoped normal-path verification.

## 2. Design

SpatialMutation.Commit captures the original EntityObservation world facts and each actual entity's existing scope before publication. It verifies them after callbacks return, while allowing the provider's intended layer, cursor and resource-cell book changes. Verification failure uses the original World fault owner. No second clock/entity/fault/store or full-world hash is added.

The existing negative notification probe gains clock-pause and entity-touch modes; all resource/codec behavior remains delegated to the actual guard. The same native/independent cases verify terminal save/hash/query/update refusal and session isolation.

## 3. API/state

No new public signature or payload; build2.588.0/save157/replay1. Existing stateless publication boundary now verifies original scoped facts.

## 4. Files

Kernel/SpatialMutation.cs; existing SpatialPublicationFaultTests.cs; engine/protocol identity.

## 5. Errors

Mutation after publication is terminal, not rolled back or retried. Original thrown exceptions remain first; silent scoped fact mutation becomes the existing observation invariant exception. Preflight refusals remain unchanged.

## 6. Compatibility

Existing boundary strengthened directly, no alternate guarded path or legacy fallback.

## 7. Verification

Targeted compile clean7.75s. Four exact cases pass790 checks: expanded publication modes96, atomic refusal56 and native/independent physical corridor319 each. [Evidence](../../docs/migration/P3_SPATIAL_NOTIFICATION_INVARIANTS_VERIFICATION.md). No full/profile/family tests.

## 8. Risks

This observes existing entity scalar facts, clock, random stream and next identity—not arbitrary private component memory, other service ledgers or reflection bypasses. Whole-entity scope capture cost is not an allocation-budget proof. Pure resource guards and broader physical recovery/utility/global gates remain open.

## 9. Exclusions

Rollback, full world cloning/hashing, host/platform/Oil/Tycoon changes, full regression and Git publication.

## 10. Status

Publication scoped-fact safeguard and stated focused verification complete. Pure guard/private component/service mutation and wider physical recovery/utility/global gates remain open; full goal active, no original broad checkbox closes.
