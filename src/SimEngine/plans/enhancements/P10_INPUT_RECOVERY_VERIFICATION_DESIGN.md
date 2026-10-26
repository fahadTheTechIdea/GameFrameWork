# P10-IR1 — Recovery execution and physical fixture reconciliation

Status: implemented2.626.0/save159; five final focused cases2722/0. Specific
P10-AP1 follow-up resolved; broad automation bundle not rerun. [Every attempt and
limits](../../docs/migration/P10_INPUT_RECOVERY_VERIFICATION.md). P10.022/full goal open.

## 0. Problem Statement

P10-AP1 observed two failures inside the bundled AutomationTests.Processes case:
InputRecoveryTests.Execution counts pure codec/view validation callbacks as actual
retry decisions, and NetworkSignalTests.Factory uses raw recovery content containing
an inactive supplier. These block current proof of native input recovery.

## 1. Goals

1. Prove exactly one recovery decision per actual update without constraining
   legitimate pure validation calls or accepting an arbitrary observed count.
2. Prove repeated reads change neither authoritative state nor execution decisions.
3. Reuse PhysicalRecoveryTestContent's sole physical base authoring, startup and
   navigation without introducing business or removing network accounting checks.
4. Give existing recovery bodies direct exact registrations instead of a hidden
   broad bundle; preserve every body and original assertion obligation.

## 2. Design

Extend the existing shared fixture to accept explicit additional document/module
authoring and a scenario-content edit, following the existing Colony.Factory API.
No additional document means no fabricated empty document. Merge module lists
without a second world, content loader or gameplay function. NetworkSignalTests
uses that fixture with its same explicit network/arbitration/signal/recovery files.

The independent recovery policy's existing transient hook counts actual decisions
only while the original World.Stepping is true. Outside the update, actual hash and
view calls are allowed to validate; assert that they do not advance decisions/time
or alter saved state. Repeat one real unavailable update and verify one decision.

Move existing input-recovery execution and physical-network bodies to individual
TestRunner.Add registrations, removing their old embedding calls. Existing broad
process coverage remains; no expectation is deleted to obtain a green check.

## 3. API surface

No public engine API changes. Shared test content authoring plus internal original
observation/recovery admission, as extended below.

### Provider boundary extension after inspection

The physical and execution checks passed142/0, but source inspection found that
AutomationSystem.ReadRecovery retains the same version-only/finally observation
gap as P10-AP1. Add a focused reproduction for source/target silent payload changes
and original thrown errors. Move P10-AP1's bound-participant observation into
EntityObservation.ReadParticipants and reuse it from both action dispatch and
recovery. Delete both local version-only/duplicated observation paths. This is
internal admission, not another scheduler/public contract. Build identity advances;
save/replay schemas remain unchanged. Production changes are limited to these
existing observation/recovery/action owners and protocol build identity.

## 4. Files to change

| Action | File | Risk |
|--------|------|------|
| Modify | PhysicalRecoveryTestContent.cs | Medium: shared fixture composition |
| Modify | NetworkSignalTests.cs | Low: existing fixture delegation |
| Modify | InputRecoveryTests.cs, AutomationTests.cs | Low: scoped instrumentation/registration |
| Modify | EntityObservation.cs, ControlActionPolicies.cs, AutomationSystem.cs | Medium: one original participant observation owner |
| Modify | SaveGame.cs/current-engine-protocol.json | Low: build identity only |
| New | verification report | Low: exact attempts and limits |
| Modify | tracker, P10 design, README, AGENTS | Low: current status |

## 5. Error handling matrix

| Input | Required result |
|-------|-----------------|
| Optional supplier absent | No business activation or fake money |
| Explicit extra document/module | Normal strict existing loader/composition validation |
| Unavailable actual signal | One execution decision, typed wait, no stale action |
| Repeated pure validation | No execution decision, time, stock or rule mutation |
| Recovered signal | Fresh dwell and normal next-update action |

## 6. Backward compatibility

Development-only. No legacy fixture, compatibility alias or scheduler fallback.

## 7. Verification

Build changed tests once as needed. Run exactly the direct recovery-execution and
physical-network cases. Use the existing physical-response literal once if needed
to validate shared default composition. No full/profile/family/broad process run.
Record actual failed attempts rather than silently reclassifying them as passes.

## 8. Risks

Transient callback counts are not persistent state and do not themselves prove
strategic game outcomes. Actual state/hash/commands/physical conservation remain
the acceptance evidence. Explicit network geometry protocol setup is not player
construction acceptance and must be labeled accordingly.

## 9. Out of scope

Unrelated production/Tycoon/Oil/platform/Draw/adapters, arbitrary-provider or broad P10 stress
closure, silently adjusting a count to5, removing failing tests, Git publication.

## 10. Sub-task execution order

1. Confirm current callbacks/fixture and direct test ownership.
2. Reconcile original shared physical authoring and targeted decision instrumentation.
3. Build and run the selected exact cases; fix actual relevant defects if exposed.
4. Update evidence/tracker with bounded scope and remaining requirements.
