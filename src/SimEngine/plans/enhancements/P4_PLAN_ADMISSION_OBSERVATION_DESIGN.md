# P4-R3: Observed plan argument admission

Status: implemented/focused-verified on2.608.0/save159/replay1/work-view107.
Four literal finals1322/0. [All attempts and limits](../../docs/migration/P4_PLAN_ADMISSION_OBSERVATION_VERIFICATION.md).

## Problem and acceptance

Plan identity, submission and activation callbacks are declared pure but several
consumers invoke them directly or through narrower guards. Extend the existing
negative independent activity proof to reproduce target/clock/space writes and
exceptions in these three callbacks. Malformed user arguments must still produce
typed InvalidArgument, without poisoning a healthy session; mutation followed by
ContentException must NOT hide a purity failure.

## Single ownership design

Introduce internal `PlanObservation` as the sole plan admission observation facade
over EXISTING EntityObservation scopes. Move R2's observed intent invocation there
(delete its former helper, no shim). For argument callbacks, observe actual plan,
clock/random/identity facts, selected space and whole existing entities in its
declared reference set. Reference parsing is observed for provider/global/spatial
facts; it is a declared pure contract, not arbitrary CLR object-graph isolation.

Keep expected ContentException separate: verify all captured scopes before
rethrowing it for existing typed-argument refusal handlers. Unexpected errors and
verification failures latch the original World fault. Never swallow a callback
mutation by classifying it as ordinary malformed input.

Migrate consumer identity/submission/activation reads across direct commands,
plans/groups/templates, recurrence, replacement, construction and saved admission
checks. Native declaration forwarding stays its ordinary implementation, not a
second execution path. Reuse one validated detached reference reader instead of
duplicating WorkBoard's existing parser/validator. No second scheduler, board,
reservation, persistence or exception store.

## Files and contracts

New Standard/PlanObservation.cs; remove ActivityDefinitionContracts.ObserveIntent
and migrate its three callers. Migrate actual plan callback consumers in existing
files. Existing independent proof methods become virtual so its negative subclass
can reuse the same declaration/parser. Explicit immutable JSON stage/fault keys
select probes; no mutable shared content callbacks. Build2.608.0/work-view107,
save159/replay1 unchanged. No legacy/compatibility support.

## Verification and limits

Use only literal work-observation, graph-disclosure, process-staffing and grouped
plan physical cases, plus a targeted project build. Record all attempts. No full/
profile/family tests, Oil/Tycoon/platform/host/rendering changes or Git publication.
P4.014 remains open until its full evaluator/private projection audit is complete;
arbitrary CLR purity, reference-parser target mutation, other callback protocols
and global performance are not claimed by this stage.

## Execution

- [x] R3.1 Reproduce all three callback bypasses and expected-error distinction.
- [x] R3.2 One observed owner and in-place consumer migration, no raw alternate path.
- [x] R3.3 Focused physical/durability/privacy checks and all-attempt tracker record.
