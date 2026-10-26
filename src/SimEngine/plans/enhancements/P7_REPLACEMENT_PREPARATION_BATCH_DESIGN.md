# P7-R2: Detached replacement admission and whole-vector relocation

Status: implemented/focused-verified on2.613.0/save159/replay1. Four exact finals664/0.
[All attempts, source mapping and limits](../../docs/migration/P7_REPLACEMENT_PREPARATION_BATCH_VERIFICATION.md).

## Problem and acceptance

Upgrades.CheckPreparedCurrent observes detached source/destination resource admission
with versions only. Its target is not registered, so a registered-entity observation
scope cannot be used for that target. Relocate still evaluates/commits individual
resource legs, allowing earlier success facts to queue before a later leg faults.

## Design

Keep Upgrades as the sole replacement owner. Wrap prepared admission with original
registered-source/entity/clock/space scopes and the detached target's original explicit
Sync codec; verify target liveness/registration too. No copy serializer or mirror state.
Use existing IResourceTransaction.EvaluateMovement/CommitMovement once for the whole
relocation Bill. Its existing movement owner already acquires the whole source/
destination claim set and delegates to CommitCapturedTransfers; remove raw reservation
and single-leg loops rather than duplicating their receipt mapping. Preserve actual
receipts, native stock, reference-owner ordering,
normal refusal, and fail-stop/no-compensation semantics.

Extend existing external probes only: SingleResourcePort's existing replacement
retention callback arms its existing InboundCheckProbe; AtomicInventory delegates
real receives and can fail its second receive. New settings are immutable JSON
negative metadata; no extra stock/book/codec or provider gameplay implementation.

## Focused verification

New exact preparation/batch case: target/source payload without Touch, typed denial,
clock/space/throw, new/restored mapless/point sessions, healthy independent mixed
provider relocation and later-leg failure with no queued earlier success fact.
Adjacent existing accounting and physical reference cases, plus original replacement
preflight negative case if directly needed. No full/profile/family runtime.

## Stages

- [x] R2.1 Reproduce detached admission and later-leg success-prefix defects.
- [x] R2.2 Guard original detached codec/scopes and migrate whole-vector commit.
- [x] R2.3 Focused verification, source review and tracker/evidence update.

P7.017 remains broader: this does not prove every incident/cargo/reference/retention
callback, full conversion/spill/topology accounting or arbitrary-provider stress.
No Oil/Tycoon/host/platform/Draw changes or Git publication.

The earlier OnCreate/RestoreReplacementState audit is implemented by the existing
R3 owner and extended at M49 to original registered/pending entities and persistent
providers, with per-callback checks and original exception preservation. M50 also
validates actual initial/retained selector state during detached admission before
registration. [Current evidence and broader limits](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m50-authored-initial-port-bindings--2026-10-22).
Arbitrary-provider isolation/disposition/stress and full original P7 acceptance remain open.
