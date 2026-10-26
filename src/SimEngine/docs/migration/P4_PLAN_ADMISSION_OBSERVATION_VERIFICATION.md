# P4-R3 plan admission observation evidence

2026-10-15. Engine2.608.0/save159/replay1/work-view107. Four literal finals1322/0.

## Reproduction and implementation

Identity, submission and activation callbacks had unobserved consumers after the
previous CheckIntent repair. Existing negative independent content now selects an
explicit immutable JSON callback stage and fault. It reuses the SAME declaration/
argument parser; shared definitions never acquire mutable runtime callback state.

Before production changes, the work-observation literal reproduced774/27: target,
clock and space writes returned normally, original exceptions lacked an engine
fault, and target mutation followed by ContentException could become an ordinary
InvalidArgument refusal. The baseline includes15new stage/fault combinations
(three stages × target/clock/space/throw/target-content) and unchanged earlier
intent/visibility probes. Pure ContentException and genuinely malformed reference
arguments remained healthy typed refusals; those assertions must stay passing.

New internal `PlanObservation` owns pure plan admission invocation, not gameplay
state. It uses existing EntityObservation scopes for the actual provider, original
clock/random/entity identity facts, selected space and whole existing entities
from the provider's declared reference set. References are validated, canonical,
detached and never inferred from JSON numeric fields. Missing live entities remain
the actual provider's typed admission decision, not fabricated targets.

The original R2 intent guard MOVES into this class; its former
ActivityDefinitionContracts.ObserveIntent method is deleted. WorkBoard's separate
reference validator now delegates to PlanObservation.References. No old guard,
duplicate parser, compatibility wrapper, scheduler, state book or codec remains.

Argument callbacks distinguish expected errors deliberately: ContentException is
re-thrown to existing typed-refusal handlers ONLY AFTER every observed scope
verifies. If the callback wrote state first, verification faults instead; the
argument-error catch cannot conceal mutation. Unexpected exceptions latch through
the original World first-fault owner. Healthy callbacks return their own results;
there are no substituted defaults.

## Consumer migration audit

Inspected call sites, not just search counts:

- ActivityDefinitionContracts: submission check and pre-activation admission.
- WorkView and player/service activity admission: moved original intent guard.
- WorkPlans: proposed identity/submission, duplicate detection, pending promotion,
  native execution identity comparison and saved-plan identity validation.
- Standard PostOrder handler and construction removal preview: activation check.
- WorkPlanGroups: group duplicate identity reads.
- RecurringPlans/WorkRecurringPlans: authored request/recurrence identity reads;
  existing outer source observation remains where applicable.
- WorkPlanTemplates: readiness activation inside existing template-owner guard.
- WorkPlanReferenceReplacement: canonical references, projected identities and
  rewritten submission checks.

Remaining engine `.IntentIdentity/.CheckSubmission/.CheckActivation` invocations
are the three central PlanObservation callbacks or declaration forwarding in
native Construction/Networks/ActivityDef implementations. Those forward to their
ordinary parser/rules; they are not unobserved external consumer paths. This is
source audit evidence, not a claim of a new compiled whole-assembly audit.

Build2.607.0→2.608.0, work-view106→107. Authored protocol expectation changes
engine identity only; save159/replay1/state schemas unchanged. No old-build reader.

## Every build/runtime attempt

Build command each time:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

- Test/probe build exit0/8.06s reported, zero warnings/errors.
- Pre-fix reproduction774/27,1473ms, PID8172 exit1/1641ms. All27failures retained;
  no test assertion removed or bounds enlarged to hide them.
- Production build exit0/9.39s reported, zero warnings/errors. Session95878 was
  polled to terminal exit0 before runtime, despite already printing success.
- FINAL work924/0,1020ms, PID29644 exit0/1176ms.
- FINAL graph246/0,947ms, PID32400 exit0/1084ms.
- FINAL staffing102/0,7139ms, PID22184 exit0/7284ms.
- FINAL groups50/0,601ms, PID25692 exit0/744ms.

Only these four literal selectors ran:

1. `Work observations: physical execution and terminal candidate plan query boundaries`
2. `Activity graph disclosure: nested requirement and node final effects stay private while earned work continues`
3. `Process staffing: automatic exact queue teams, retained duty, sibling contention and saved departure`
4. `Plan groups: physical pause, cancellation, repeat and saved commands`

Each uses `--bounded-exact`,60second timeout,5second termination deadline and bounded
output. All children terminal, no timeout/abort. Final sum1322/0 excludes baseline.

The work case proves guarded failures, no result prefix or earned time, original
fault retention, no callback retry/healthy save/hash, preserved private snapshot,
independent-session identity and expected-error distinction. Its original physical
work/haul/save/replay paths remain. Other unchanged literals exercise real graph
discovery/forgetting, physical queue-team service and grouped construction/work
pause/cancel/repeat with saved command continuation; all on this new build.

Core442 C# combined SHA256:
`c38b65153d3da175e4e074d618ae7f96c7f97c11c968180d1efe2abad2be003e`.

## Limits and remaining scope

The declared reference parser is itself observed for provider/global/spatial facts,
but these scoped guards do not prove arbitrary entity writes while discovering
previously unknown references or writes to undeclared global authorities. This is
not a CLR security sandbox or universal purity proof. No global allocation/soak
or all-provider result is claimed. ExecutionIdentity/BindExecution/reference rewrite
and other callback protocols retain their separate existing admission boundaries;
this stage does not silently claim their entire fault matrix.

Original P4.acceptance.014 remains OPEN. No original checkbox changes;221/198recorded
checked/23tracked open unchanged, full objective active. Earlier build evidence is
historical unless explicitly rerun above. No full/profile/family runtime, Oil/
Tycoon/platform/host/rendering changes or Git staging/commit/push.
