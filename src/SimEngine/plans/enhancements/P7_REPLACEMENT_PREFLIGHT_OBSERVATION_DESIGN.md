# P7-R1: Authoritative replacement preflight observation

Status: implemented/focused-verified on2.612.0/save159/replay1. Three exact finals376/0.
[Every attempt and coverage limits](../../docs/migration/P7_REPLACEMENT_PREFLIGHT_OBSERVATION_VERIFICATION.md).

## Problem

Upgrades.Check delegates to CheckCurrent and compares only the old entity Version.
Raw IReplacementGuard/removal/stock/spatial callbacks can mutate persisted payload,
clock or space without touching that version, or throw without latching the session.
Replacement reference owners already have individual scopes; the whole replacement
preflight needs its original owner boundary as well.

## Design and acceptance

Use EntityObservation.ReadProvider around the sole CheckCurrent, observing the actual
whole source entity, selected space and clock. Publish captured placement only after
successful verification. Keep normal typed refusals, existing postfault NotAllowed
behavior and original placement/resource/reference owners. No duplicate serializer,
copy of source state or new replacement coordinator.

Extend existing non-friend ReplacementProbe with immutable JSON negative check modes:
companion persisted payload without Touch, payload plus typed denial, clock, space and
exception. Reuse the companion port's original codec; add no probe state/codec.
Prove direct Check and TryReplace refuse mutation before identity/stock/reference
commit, preserve first fault, withhold target, reject saves/hashes and isolate loaded
sessions in mapless/point compositions. Normal accounting and physical reference
replacement remain the positive adjacent acceptance.

## Stages and focused gate

- [x] R1.1 Reproduce hidden mutation and missing fault ownership in exact new case.
- [x] R1.2 Guard original whole preflight and advance engine identity, no save layout change.
- [x] R1.3 Verify new negative case plus existing resource-accounting and physical-reference literals.
- [x] R1.4 Evidence/master update; retain broader P7.017 disposition/stress gate open.

## Remaining source-path audit

This stage guards registered-source CheckCurrent only. CheckPreparedCurrent still
has separate version-only checks around detached-target resource admission, and
Relocate still invokes single-leg Evaluate/Commit in loops rather than the existing
whole-set transfer API. Audit and migrate these original paths without another
replacement coordinator, private transaction algorithm or compensation model.
Incident/cargo-disposition, all reference-owner coverage and broader stress remain
under the existing original open gate; do not infer closure from this fix.

No full/profile/family run, Oil/Tycoon/host/platform/Draw edits or Git publication.
