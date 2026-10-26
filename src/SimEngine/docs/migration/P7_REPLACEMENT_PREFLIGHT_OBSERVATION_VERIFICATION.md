# P7-R1 replacement preflight verification

Engine2.612.0/save159/replay1; state layouts and view schemas unchanged. Only
Upgrades.Check's existing preflight boundary changes, with engine identity and
independently authored protocol expectation advanced together.

## Reproduced defect and fix

The original Check compared old.Version after CheckCurrent. A raw IReplacementGuard
could alter its companion port's persisted service payload, pause the actual game
clock or alter selected-space state without advancing that version. Typed denial
did not make the mutation safe. A thrown guard exception propagated without a
latched world fault. In reproduced TryReplace paths, replacement could commit after
such a supposedly pure preflight.

The sole CheckCurrent now runs inside EntityObservation.ReadProvider with the
actual source entity's original whole Sync codec, its scalar facts, selected space
and clock. Placement is held locally and returned only after scope verification.
No new codec, state snapshot owner, replacement service or serializer was added.
Healthy typed refusals remain typed; existing postfault NotAllowed admission remains
an early refusal, not a second callback attempt. The first authored exception is
preserved by the original observation fault owner.

The existing non-friend ReplacementProbe gains immutable JSON check_fault metadata
only. Payload probes call the companion SingleResourcePort.FaultServicePayload
without Touch; the companion's original codec remains sole payload authority.
Clock/space/exception modes are negative extension instrumentation. The probe adds
no persisted field or codec, and its existing initialization/guard behavior remains.

## Every build and runtime attempt

All builds used the affected test project, `--no-restore -v minimal`.

1. New reproduction/probe build exit0, zero warnings/errors,4.99s.
2. New literal baseline40passed/36failed,306ms; child23188 exit1/450ms.
   Bounded stdout WAS truncated to4000chars. The captured tail/final counts prove
   hidden mutation/healthy save/hash/committed replacement and missing fault ownership.
   The baseline stopped during the repeated throw/check path, before the point-space
   matrix. It is not evidence that all18 combinations ran before the repair.
3. Engine correction build exit0, zero warnings/errors,6.43s.
4. New literal90passed/1failed,285ms; child6260 exit1/424ms. Mapless paths passed;
   point fixture failed because it requested2seats from the original1-seat scenario.
   Explicitly authored2seats in the test composition; engine seat validation unchanged.
5. Existing accounting literal78passed/0failed,261ms; child22156 exit0/400ms.
6. Existing physical-reference literal96passed/0failed,461ms; child31916 exit0/599ms.
7. Fixture correction build exit0, zero warnings/errors,5.40s.
8. New literal FINAL202passed/0failed,363ms; child34592 exit0/505ms.

Exact names:

- `Replacement preflight: hidden payload clock space and callback failure stop before identity commit`
- `Resource replacement: native stock, real receipts, private history and saved identity`
- `Entity replacement references: physical homes independent owners refusals and saved continuation`

Each used `--bounded-exact`, timeout60000ms, termination5000ms, output4000chars.
All children terminal. Only the initial failing baseline stdout was truncated;
all later stdout/stderr untruncated. No running handles were abandoned or restarted.
Final scoped sum376/0, no full/profile/family run. The unchanged positive literals
were not rerun after the new negative fixture's seat-only correction.

## Evidence matrix

The new case covers18combinations: mapless payload/payload-deny/clock/throw and point
payload/payload-deny/clock/space/throw, each through direct Check and TryReplace.
It proves actual hidden companion mutation while source Version remains unchanged,
fault before source retirement or target/identity commitment, null replacement output,
retained published facts, healthy-save/hash refusal and first-exception preservation.
An independently loaded session retains its hash and still updates normally.

Accounting78 verifies real mixed native stock relocation, original receipt cleanup,
private exact transfer history, saved identity and no copied initialization grant.
Physical-reference96 exercises ordinary player-authored timed replacement, homes,
independent reference-owner commitments/refusals, saved physical continuation and
bad reference callbacks. These are adjacent acceptance, not a global audit of every
reference-owner implementation or all resource disposition.

## Limits and next source paths

Original P7.017 and subordinate incident/cargo/reference/full-accounting/stress gates
remain open. Current inspection found two additional original replacement paths to
audit: CheckPreparedCurrent's detached-target/version-only resource admission, and
Relocate's per-leg Evaluate/Commit loops despite the existing whole-set transaction
API. This stage deliberately does not claim those paths were migrated or verified.
Do not copy the registered-source observation blindly onto an unregistered target;
it needs its original explicit codec and the registered source's actual scopes.

Persisted source/entity and selected-space/clock mutation are observed; arbitrary
undeclared global/unpersisted CLR changes are not sandboxed. Original221/200recorded
checked/21tracked open is unchanged bookkeeping, not a new proof of the other200.

442core C# combined path/hash digest:
`54d463d3b4b31f7b2deafe641904944440907fe890c6dde9fd97edad00e2d9a5`.
Focused diff-whitespace check exit0. No Oil/Tycoon/host/platform/Draw changes,
full/profile/family runtime, staging, commit or push. Full goal stays active.
