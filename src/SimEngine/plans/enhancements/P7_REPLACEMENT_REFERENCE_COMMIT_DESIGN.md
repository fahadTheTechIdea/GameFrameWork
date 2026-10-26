# P7-R4: Reference-only replacement commit boundary

Status: implemented/focused-verified on2.615.0/save159/replay1. Five exact finals972/0.
[All attempts, source mapping and limitations](../../docs/migration/P7_REPLACEMENT_REFERENCE_COMMIT_VERIFICATION.md).

## Problem and acceptance

Relocate checks stock/claims and Tick only after all reference owners run. A callback
can change pause/speed/random state or presence geometry without changing that Tick;
a bad owner can also allow later owners to execute before its stock/claim violation
is found. Reference updates and their legitimate entity version changes must remain
allowed, not frozen by a whole-entity comparison across commit.

## Design

Reuse original EntityObservation scalar scopes with version changes permitted for
the two live participants; retain owner/state/identity/clock/random guarantees. Observe
selected space and detached physical snapshots around each registered owner callback.
Extract existing entity spatial capture and symmetric snapshot comparison from
PlayerView/EntityView into EntityObservation, then call that same implementation
from views and replacement. No host/projection gameplay rule or copied shape validator.

Extract the existing relocated-stock/claim check at Upgrades into one live observed
helper and run it initially and after each owner. Preserve the same replacement,
resource, reference and fault owners. No rollback or new reference registry/codec.

## Focused gate

New exact direct-contract probe uses ordinary public reference-binding commands,
then malformed reference commits for clock/speed/random/space/presence/stock/claims.
A second owner must remain uncalled after the first violation. New/restored sessions,
last-healthy publications, actual prior relocation and independent-session isolation.
Adjacent ordinary physical component/system/network reference cases and the directly
affected snapshot-delta case only. No full/profile/family execution or publication.

## Stages

- [x] R4.1 Reproduce forbidden state changes and late detection across reference owners.
- [x] R4.2 Reuse original spatial observation/comparison and guard per-owner commit.
- [x] R4.3 Focused verification and evidence/master update; broader P7.017 stays open.
