# P6-I8: Replay refusal and active-save gate reconciliation

Status: implemented/focused-verified on unchanged2.605.0/save159/replay1.
I8 five exact finals417/0 plus unchanged I7 parallel/control evidence470/0;
original P6.acceptance.009 now checked. [Mapping/attempts/limits](../../docs/migration/P6_DURABILITY_GATE_RECONCILIATION_VERIFICATION.md).
Audit original P6.acceptance.009 without reducing its scope
to the previous successful scenario or confusing a full profile with focused gates.

## Requirements and approach

Use existing canonical replay, callback failure, native protocol and codec-drift
cases for current identity/corruption/old-schema refusal evidence. Inspect actual
assertions, not just case names. Combine with I7's current parallel reservation/
paused pending-control proof and I6's genuinely advancing lockstep/save sessions.
Verify core source identity remains unchanged across these checkpoints.

Coverage review identifies a useful explicit gap: I6 saves partial loading but not
an endpoint already partly delivered. Extend that same transport proof: wait for
actual received stock between0 and8 with residual carried cargo, save both peers,
load/reattach another pair, and finish the existing haul without resubmission.
Verify a zero-new-command replay from that nonzero-step partial-delivery save.
Retain all current physical build/incident, sequence and isolation assertions.

No new runtime owners, content, protocol, compatibility or host gameplay. Existing
test transport only paces updates/forwards native packets; engine modules own work.

## Stages

- [x] I8.1 — Current exact identity/corruption/provider/native-format/codec-drift evidence.
- [x] I8.2 — Partial delivered endpoint/cargo checkpoint and actual restored continuation.
- [x] I8.3 — Map every named original durability stage to inspected source/runtime evidence.
- [x] I8.4 — Only P6.acceptance.009 checked; all other original gates preserved.

Focused literals only: four existing replay/protocol cases and the changed networked
mod case. No full/profile/family execution, Oil/Tycoon rule change, platform/Draw
or Git publication. Broad robustness, all-optional-module isolation and full goal
remain separate requirements even if this durability gate is satisfied.
