# P7-R3 complete local preparation observation

Current follow-up: M49/core2.710 extends the same preparation owner to other registered
entities, frame-pending entities and persistent providers through their actual codecs,
with verification after each local callback. Actual baseline150/88 becomes451/0 after
adding live shared-claim continuation and late prepared-binding probes. This exact
preparation case is selected again451/0 on current2.711; existing retention302/0 remains
M49 evidence. [All attempts, original causes, identities and limits](P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m50-authored-initial-port-bindings--2026-10-22).
Following2.614 measurements are historical, not the current runtime image.

Engine2.614.0/save159/replay1. No new state/codec/view schema; independent protocol
expectation and engine identity advance together.

## Reproduced defect

The old TryReplace captured only a version and stock vector before local preparation.
RestoreReplacementState could change source service payload without Touch, pause the
clock or alter selected space. R2's later admission boundary captured the already
mutated state and therefore could not detect those earlier changes. Silent callbacks
could commit replacement and relocation; mutate-then-throw could leave a healthy
saveable world. The old error path re-read source providers even after a known fault.

## Original owner implementation

Upgrades.TryReplace now captures original registered-source facts/whole Sync and
selected-space scopes BEFORE stock reads, carried-state serialization and candidate
creation. Its local VerifyPreparation verifies those same original scopes immediately
before and after detached admission, before the EntityStore registers the candidate.
The existing serialization, initialization and auxiliary-retention implementation is
moved inside that guarded interval, not copied into a new model or coordinator.

Candidate-local state is intentionally not compared with its pre-initialization
state: creation/retention is allowed to set that private state. R2 still separately
guards PURE admission against the candidate's own original codec. Resource movement,
reference ownership and retirement remain under the original owners.

On an exception, a previously latched fault is propagated without more provider
reads. If registration/commit already began, original replacement fault ownership
applies. Otherwise the original preparation scopes verify whether the failed local
candidate was harmless. An unchanged failure remains healthy. A mutation faults:
the first observer fault is retained where already recorded, otherwise the original
callback error is latched. The original callback exception is always rethrown to
the caller, not replaced by a secondary verification exception. No rollback or
compensating state is fabricated.

SingleResourcePort adds immutable negative retention modes over its existing source
service payload, clock and space. The probe adds no persisted field, balance, claim
book or codec. Candidate-only auxiliary mutation and pure retention failure are
positive contract probes, not new gameplay rules.

## Every attempt

Builds: affected test project, --no-restore -v minimal. Both exit0, zero warnings/errors,
reported5.19s before engine repair and6.42s after it. Runtime starts only after actual
terminal build success.

New retention literal baseline202passed/100failed,452ms; child29168 exit1/595ms.
Its5000char stdout WAS truncated. Captured tail/final counts show unlatched source/
clock/space changes, committed registration/stock movement and healthy saves after
mutating exceptions. The suite completed, not a timeout or restart.

| Exact case | Final checks | Suite ms | Child / outer ms |
| --- | --- | --- | --- |
| Replacement retention: source state clock space and local preparation failures preserve ownership | 302/0 | 469 | 13508 exit0/606 |
| Replacement preparation: original detached codecs and whole-vector resource relocation | 288/0 | 433 | 29996 exit0/565 |
| Resource replacement: native stock, real receipts, private history and saved identity | 78/0 | 285 | 6672 exit0/420 |
| Entity replacement references: physical homes independent owners refusals and saved continuation | 96/0 | 477 | 19884 exit0/612 |

Each used --bounded-exact, timeout60000ms, termination5000ms, final output4000chars.
All final stdout/stderr untruncated; every child terminal. Four focused finals764/0,
not a full/profile/family run. No older unrelated test reruns.

## Evidence and limits

Retention302 covers28new/restored mapless/point combinations. Twenty negative
combinations change source/clock/space, with and without an authored subsequent
exception. They fault before identity consumption, registration, resource movement
or success events, despite unchanged source versions. Save/hash refuse, later
checks do not retry callbacks, first fault stays stable, original callback exceptions
survive verification, and a separate loaded session stays unchanged and advances.

Eight positive combinations allow candidate-only auxiliary changes or harmless local
retention exceptions. Candidate resources remain exact, original source history is
unchanged, and normal save/load still works. Accounting78 separately calls Refusals,
including the existing pure target OnCreate initialization exception: it remains
healthy with original geometry/state/identity preserved. Prepared288 retains R2's
22admission combinations and real second-leg failure/no-success-prefix proof;
reference96 retains public timed replacement and physical rebound-home behavior.

Source inspection proves the known-fault error branch no longer calls ReadStock or
verification callbacks. This is not a claim that every possible mod callback has a
runtime retry-count probe. Nor is it a sandbox for unrelated global or unpersisted
CLR state: the scopes cover original source/entity/session facts and selected space.

P7.017 still includes incident/cargo disposition, full reference-owner rebinding
audit, conversion/spill/topology accounting and broader hostile/stress requirements.
The reference commit phase has legitimate reference-owner mutations and must be
audited separately; preparation purity scopes must not be extended blindly across
legitimate resource movement or reference updates. Original221/200recorded checked/
21tracked open remains unchanged, not a new audit of those200 checked items.

442core C# combined path/hash digest:
`b9f7c254bf40ce6eab98a049889b28b5e31a7afa3382a68401e444854b10c053`.
Focused diff-whitespace check exit0, only normal LF/CRLF conversion warning. No
Oil/Tycoon/host/platform/Draw edits, full/profile/family runtimes or Git publication.
Full goal remains active.
