# P7-IP1 — Complete incident preparation observation verification

2026-10-16. Engine2.637.0/save159/replay1.
[Design](../../plans/enhancements/P7_INCIDENT_PREPARATION_OBSERVATION_DESIGN.md).
Focused verification only; the original full plan and broad resource/incident gates remain open.

## Demonstrated gaps and authoritative repair

The original delayed exposure preparation captured the source entity codec but only scalar
target facts. The independent delegating effect changes an existing private probe on the
actual target without Entity.Touch, then returns the native terrain preparation. On2.636,
the engine accepts it and raises target episode1: actual runtime evidence of a mutation
escape, not an inference from a search.

An additional baseline uses an original throwing preparation. The world faults correctly,
but retained propagation views still return owned, missing and unnamed backlog facts instead
of throwing. ReadEligibility already has the proper entry guard; TryRead did not.

Production changes are confined to the original IncidentPropagationSystem:

- The existing preparation ReadProvider now uses observeTargetState:true. Original source
  codec, actual selected space, provider observation and clock checks remain intact.
- TryRead requires a healthy update before looking up an entity or returning for missing,
  private or unnamed requests. No second view/provider/fault owner.

Engine build identity advances; save/replay/component layouts, scheduling, delay, eligibility,
incident Raise, terrain publication, resource transactions and physical response rules do not
change. Original callback errors remain the first cause, including mutate-then-throw.

## Mod proof and real physical behavior

The original tile-propagation proof owner is extended as one partial class; factory and
registration remain authoritative. Its existing master explicitly lists exposure-preparation.json
under proof.terrain-incidents, with native terrain metadata and an independent delegating
IIncidentExposureEffect. The wrapper delegates all positive checks/preparation to the actual
TileTerrainWorkEffectDef. No copied terrain effect, hazard loop, stock counter or scheduler.

Negative injection reuses ExposurePrivateProbe and its existing selected registration
network_signal_private_probe. No new saved probe component is introduced. A read-only Value
diagnostic proves actual private mutation happened; the source/target entity revisions do
not change. Hooks are armed only for their actual World, not other sessions sharing content.

Two positive native/independent metadata trajectories execute the original60-minute delayed
exposure, saved pending receipt, exactly one target episode and native atomic terrain write
over both actual footprint cells[13,28]/[14,28]. Both cells become2. The source and receiver
then recover through normal supplied work: the real hauler delivers two total water units,
each is consumed once, depot10 becomes8 and no cargo/site water remains. Saved twins,
completed roundtrip, untouched interleaved session and original replay verification agree.

Sixteen negative variants cover native/independent rule metadata with target-private,
source-private, clock, throw, target-throw, source-throw, target-mutate-and-refuse and null
preparations. No target episode or prepared terrain prefix publishes. Raw negative diagnostic
state proves mutation is detected, not rolled back or silently ignored. Healthy hash/save
certification and captured owned/private/missing/unnamed propagation views all refuse after
the first fault. Views are captured before injection so the reader itself is tested, not
an unrelated feature-lookup guard. Last healthy published snapshot is retained.

## Current final exact results

| Exact case | Checks | Child PID | Suite / bounded process ms |
|---|---:|---:|---:|
| Incident preparation: complete target state guards delayed native terrain publication and physical saved response | 354/0 | 30708 | 5628/5755 |
| Tile incidents: authored footprint conditions reobserve physical changes and supplied response | 272/0 | 35460 | 9479/9623 |
| Incident pressure: native finite pulses recovery phase stop and episode reset | 432/0 | 19476 | 7657/7812 |

**1058 passed, zero failed across three distinct exact cases.** All use outer60000ms and
termination5000ms, no truncation/timeout/abort. The two existing cases briefly overlap in
wall time; timings are local observations, not controlled benchmarks. Session59538 is
polled to terminal exit0; no matching engine/Oil test or build process remains live.

The new case initially passes322 checks after repair, then gains32 assertions proving exact
source/target private payload mutation (rather than only callback entry). Only this new case
is rerun after that assertion-only edit. Production core, proof providers, original tile
and native pressure case bodies are unchanged; their current-core results are not needlessly
rerun after a test-only assertion addition.

## All runtime attempts

| Attempt | Core | Result | PID | Suite / process ms | Outcome |
|---|---|---:|---:|---:|---|
| 1 new case | 2.636 | 2/1 | 25988 | 54/186 | Duplicate existing probe CLR definition rejected during composition |
| 2 new case | 2.636 | 2/1 | 12680 | 76/206 | Wrong component alias rejected by loader |
| 3 new case | 2.636 | 5/2 | 25972 | 1020/1141 | Actual target-private escape raises episode1 |
| 4 new case | 2.636 | 20/5 | 30076 | 1260/1380 | Owned/missing/unnamed terminal reads escape, then target-private escape |
| 5 new case | 2.637 | 322/0 | 31916 | 5324/5451 | Initial repaired proof |
| 6 native pressure | 2.637 | 432/0 | 19476 | 7657/7812 | Existing affected physical case |
| 7 tile propagation | 2.637 | 272/0 | 35460 | 9479/9623 | Existing shared factory/physical case |
| 8 new case | 2.637 | 354/0 | 30708 | 5628/5755 | Final stronger exact mutation assertions |

All attempts are bounded exact invocations. The first two are test-authoring failures, not
engine regressions; composition's duplicate-definition refusal is preserved, not weakened.
Baseline foreign-view lookup refused indirectly; final tests explicitly retain that view
before fault so all four TryRead paths are covered directly. Repeated new-case attempts are
not added to the distinct1058-check final total.

## Builds and final identities

Six targeted Release builds of tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj with
--no-restore -v minimal. All clean/zero warnings/errors:5.98s,5.47s,4.53s,4.69s,6.67s,4.51s.
No full/profile/family run or consumer build.

- Core443 sorted C# path/content SHA256:
  `84e2f8950d902fd35a584f7cebc29e68685f4abcef2a9c67127b4f30ea8b561c`.
- Release engine DLL:
  `F535F06693FB6C4C3C83F9706B8DFA3F2DC922A5E6052674FDB155D1399D61C3`.
- Final Release test DLL after assertion strengthening:
  `62AD1ECC1CE8B72AD7E7533549022F24463E407E82FD7BFB58D1ECC13EC5EC3D`.
- Release extension DLL, unchanged by final assertions:
  `93A6696CA9514F2175E711DEC43B6C27B0710E05435FB19E0F47CE09A5A765BA`.
- Earlier test DLL used for322/new,272/tile and432/native:
  `6554227236886D141E8510ACF5DB56DB4294095F17FF2A07971DDD287AED44BF`.

Original audit221 literal IDs/texts still map to208 recorded checked/13 open, no original
wording/checkbox edits. Arbitrary hidden CLR purity, broad P7.017/P11.026/.029/.035, stress,
soak and every optional module remain open. No Oil/Tycoon/platform/Draw source, new clock,
legacy/compatibility path, staging/commit/push.

Final hygiene: diff-check exit0,35 existing line-ending warnings/no other diagnostics;
explicit six changed/new slice-file whitespace scan clean. One new exact registration and
one embedded document, explicitly master-listed by the shared fixture. Staged paths0;
no matching test/build process remains live. Documentation-only changes follow verification.
