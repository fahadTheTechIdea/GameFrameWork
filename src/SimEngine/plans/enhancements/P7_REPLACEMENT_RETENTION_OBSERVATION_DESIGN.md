# P7-R3: Observe the complete local replacement preparation interval

Status: extended at2.710; last selected participant qualification2.711/save161/replay2. Existing-source retention case302/0 is M49 evidence; participant preparation case451/0 is M50 evidence, not rerun at M51/core2.712.
[All attempts, original-error semantics and limits](../../docs/migration/P7_REPLACEMENT_RETENTION_OBSERVATION_VERIFICATION.md).

## Problem and acceptance

R1 guards initial preflight; R2 guards detached admission and whole resource movement.
Between those boundaries, original source stock/codec reads, target construction,
OnCreate and RestoreReplacementState execute without whole-source/clock/space scopes.
Version/native-stock comparisons do not detect silent auxiliary source mutation or
clock/space changes. The old catch also re-reads providers after a latched fault.

## Design

Within the existing Upgrades.TryReplace owner, capture original registered-source
facts/whole Sync, every other live registered entity (including pending registrations),
persistent-service codecs and selected-space scopes before serialization/preparation.
EntityStore verifies after configure and every OnCreate; Upgrades verifies each
retention/carried-state callback and detached admission before registration.
Candidate-local changes remain allowed. No target codec mirror, new coordinator,
persistence or compensation. Legitimate stock relocation/reference commits are outside
this preparation interval. [Current participant/live-claim evidence](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#earlier-m49-original-preparation-participants-and-live-claims--2026-10-22).

If local creation/retention throws, preserve its original exception. A verified
unmutated local failure remains healthy; an authoritative mutation faults the original
session. Once faulted, do not invoke more resource callbacks while handling the error.
Existing whole-vector commit/reference/retirement behavior remains unchanged.

## Verification

Use the existing SingleResourcePort replacement callback and original service payload
for immutable authored source/clock/space mutations, including mutate-then-throw.
Prove new/restored mapless and point sessions, candidate-only auxiliary changes,
benign retention exceptions and existing pure OnCreate failure/accounting behavior.
Run only the new exact retention case and directly affected prepared/accounting/
physical-reference cases. No full/profile/family runs or publication.

## Stages

- [x] R3.1 Reproduce mutation across the earlier creation/retention interval.
- [x] R3.2 Guard original preparation interval and preserve exception/fault semantics.
- [x] R3.3 Focused verification, evidence and original tracker update.

P7.017 remains open for the other original disposition/reference/accounting/stress
requirements. This is not an arbitrary-mod/global CLR sandbox claim.
