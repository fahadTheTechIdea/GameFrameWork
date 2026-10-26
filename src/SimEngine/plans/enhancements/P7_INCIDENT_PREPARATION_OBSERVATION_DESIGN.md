# P7-IP1: Complete incident preparation observation

Status: implemented and focused verified2.637.0/save159/replay1, three exact cases1058/0.
[All attempts, identities and limits](../../docs/migration/P7_INCIDENT_PREPARATION_OBSERVATION_VERIFICATION.md).
Original221/208 recorded checked/13 open; broad gates and full plan remain open.

## 0. Problem and specification

`IncidentPropagationSystem.Advance` observes the source's original entity codec during
`IIncidentExposureEffect.PrepareExposure`, but the target has only scalar observation facts.
An independent effect can mutate a target sibling's persisted state without touching its
entity revision. Eligibility already observes both complete entities; preparation must do so
too. `TryRead` also lacks the healthy-session entry gate used by `ReadEligibility`.

## 1. Goals

1. Preparation observes the actual target's complete original codec as well as the source,
   space and clock, without another observer or state owner.
2. Reject source/target payload mutation, clock changes, missing preparation and throwing
   providers before raising a target episode or publishing prepared terrain changes.
3. All propagation-state reads refuse a faulted session before missing/private early returns.
4. Native and independent rule metadata retain real delayed exposure, original terrain
   publication, physically supplied recovery, saves, replay and session isolation.

## 2. Design

Set `observeTargetState:true` on the existing preparation ReadProvider interval; keep its
source codec observation and original-error handling. Require healthy state at TryRead entry.
No changed spread rule, timing, callback order, terrain/resource transaction or recovery logic.

Extend the existing tile-propagation proof owner with one exact case. A master-listed JSON
document authors a native terrain effect and an independent delegating exposure effect.
Positive behavior reuses the actual terrain effect; negative instrumentation reuses the
existing ExposurePrivateProbe component instead of creating another saved counter.

## 3. API surface

No public contract changes. Engine identity advances2.637.0; save159/replay1 and layouts stay.

## 4. Files

IncidentPropagationSystem/EngineInfo/protocol expectation, existing tile-propagation proof
factory and module, new effect probe/document/exact case, stage verification and tracker.

## 5. Errors

Pure-preparation failures latch the original first fault; no successful target episode or
terrain write prefix. Faulted worlds cannot save, hash or return owned/missing/private backlog
facts. No rollback or fabricated successful/no-op fallback.

## 6. Compatibility

No legacy/compatibility path or persisted state changes.

## 7. Verification

Demonstrate target-private preparation escape on current core with the new exact case first.
After repair run that exact case plus the affected original tile-propagation and native
incident-process exact cases. No full/profile/family tests; record every attempt and bound.

## 8. Risks

Full original target codecs add observation cost but cannot be replaced by revisions alone.
Negative callbacks must be armed only for their actual session, not shared positive content
state. No sandbox claim for hidden CLR state outside declared codecs.

## 9. Out of scope

Oil/business/platform/Draw source, new incident/recovery/terrain schedulers, clocks, broad gate
closure, staging/commit/push before entire goal/plan completion.

## 10. Execution order

1. Author independent delegating probe and one master-listed document in the original fixture.
2. Demonstrate the current preparation escape and terminal-read gap with a bounded exact run.
3. Strengthen the original two intervals; build and run the three focused exact cases.
4. Record evidence and retain the original broad P7/P11 requirements until fully proven.
