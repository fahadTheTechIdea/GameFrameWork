# P6-I7: Restore actual contended commitments and deferred controls

Status: implemented/focused-verified on unchanged2.605.0/save159/replay1,
parallel337/0 and control133/0 (470 total). [All attempts and limits](../../docs/migration/P6_CONTENDED_CONTROL_SAVES_VERIFICATION.md).
Continues original P6 durability requirements. Reuse the
existing ParallelAcquisitionTests and PlanetXControlResponseTests; do not introduce
another resource/automation test game, receipt model, scheduler or clock.

## Problem and acceptance

Current parallel baseline passes249 checks, but its saved contention proof can
more directly assert original receipt identities and the unacquired blocked node
before pending cancellation executes. Existing control scenario saves pending
shutdown/restart actions; explicitly inspect loaded due steps and target state,
then prove action application uses that exact simulation step without repetition.

## Changes and ownership

- Add a diagnostic wall-clock bound to existing512-update parallel waits, without
  extending the actual work bound or replacing physical requirements.
- Validate restored full worker/material/target-tool/worker-tool commitments with
  the existing Commitments reader, and compare the original native receipt structs.
- Save and inspect the actual blocked all/any node with no acquired worker/resources/
  choices/progress before cancellation; verify cancellation leaves parallel claims.
- Extend existing Rule/Enabled view helpers to accept the actual loaded session.
  Inspect pending action/due/target state after load, native LastAppliedAt after one
  update, and retained final applied timestamp through later physical play.
- No core implementation, content, protocol or native state-layout change.

## Stages and verification

- [x] I7.1 — Current baseline and direct restored contended-set/blocked-node assertions.
- [x] I7.2 — Loaded pending-control state, exact due-step application and non-repetition.
- [x] I7.3 — Focused final literals and coverage/remaining-gap reconciliation.

Targeted build before only the two affected exact cases; keep every attempt.
No full/profile/family execution. Existing broad replay identity/corruption/profile/
cross-provider gates are not automatically satisfied by these two physical cases.
No Oil/Tycoon, host/platform/Draw, compatibility or Git publication changes.
