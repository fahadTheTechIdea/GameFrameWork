# P12-A10: authoritative owner invariants — Design Document

## 0. Problem Statement

The original P12 acceptance requirement names single condition, incident and resource owners and universal outlet/inlet contracts. Native declarations already enforce one singular inventory and distinct named condition/incident identities; named ports alias the original store. However, EquipmentConditionView and IncidentsView can return empty/missing/private facts after a first world fault. Incident ID callbacks lack scoped observations, and phase lists can publish an incomplete prefix. A callback can silently mutate a neighboring persisted capability on the same entity.

## 1. Goals

1. Keep native condition, episode, inventory, reservations, work and clock owners unchanged; no alternative state or scheduler.
2. Observe whole target entity codecs as well as actual selected providers before publishing condition/incident facts.
3. Reject faulted queries before missing/private/empty branches; publish complete detached lists only after successful observations.
4. Verify distinct named conditions/incidents coexist, duplicate owners refuse before Initialize, and named outlets/inlets resolve the same inventory during physical supplied repair.
5. Prove saved continuation, pause, once-only consumption, replay, privacy, session isolation and negative callback faults through ONE NEW bounded exact case.

## 2. Design

Extend the existing EquipmentConditionView and IncidentsView in place. Their private observation helpers use EntityObservation.ReadProvider and CaptureProvider with the entity's original Sync, never a second snapshot or fault owner. Existing condition/response contracts still calculate all facts and decisions. Lists remain local until every callback succeeds. First callback exceptions remain original; no finally verification masks them.

The master-listed owner-invariants.json adds two explicitly named conditions, timed damage and repair effects, and physical water consumption to the existing network incident fixture. Native and independently authored metadata bind Storage, EquipmentCondition and Incident. OwnerObservationProof registers existing independent metadata and a shared-declaration incident metadata probe; it implements no normal gameplay. Arm methods inject negative faults only into actual selected callbacks. The driver submits ordinary commands, calls Update and reads public detached views.

## 3. API surface

No new gameplay API, state codec or module. Existing view methods now refuse faulted sessions and withhold incomplete facts. Engine build identity advances to2.428.0; save154/replay1 remain unchanged.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| Modify | ../../src/Beep.SimEngine/Views/EquipmentConditionView.cs | 35 | medium |
| Modify | ../../src/Beep.SimEngine/Views/IncidentsView.cs | 45 | medium |
| Modify | ../../src/Beep.SimEngine/Kernel/SaveGame.cs | 1 | low |
| New | ../../tests/ExtensionProof/OwnerObservationProof.cs | 100 | medium |
| New | ../../tests/ExtensionProof/content/owner-invariants.json | 30 | low |
| New | ../../tests/Beep.SimEngine.Tests/OwnerInvariantTests.cs | 250 | medium |
| Modify | test registration, embedded resource registration and protocol fixture | 3 | low |

## 5. Error handling matrix

| Failure | Behavior |
|---|---|
| Missing/private entity or capability | Healthy ordinary refusal, no fabricated facts |
| Already faulted world | Original World cutoff throws before refusal branch |
| Callback throws/changes clock/neighbor codec | Original fault latches; no output prefix or resumable save |
| Duplicate inventory or named identity | Original definition admission refuses before Initialize |

## 6. Backward compatibility

No compatibility shim, legacy path or save migration. Exact current build required.

## 7. Verification

One solution compile, terminal observed before ONE NEW exact bounded case. Same-case repairs only. Case: `Owner invariants: physical condition incident and bidirectional stock share authoritative state`. Physical waits256 Updates; outer60000ms, owned termination5000ms, output30000 characters. Source review covers native owners/declaration guards and universal compiled port contracts. Full old/family/global regression remains OPEN and requires human authorization.

## 8. Risks

Whole entity hashing adds observation cost; allocation/stress budgets remain unproven. Provider-private CLR state outside existing codecs is not sandboxed. Multiple named conditions/incidents are intentional, not duplicate owners. Normal physical geometry and resource rules must remain unchanged.

## 9. Out of scope

OilFieldDays, Tycoon, renderers/platforms, legacy, scientific atmosphere, broad regressions, commit/push before full goal AND plan. Protected G19 permission remains pending.

## 10. Sub-task execution order

1. [x] Review actual single-owner contracts and view gaps.
2. [x] Extend the original view boundaries in place.
3. [x] Implement authored fixture and one focused proof.
4. [x] Compile once and observe bounded exact verification; retain every repair attempt.
5. [x] Update tracker/audit/evidence with literal requirements and scoped results.

Completed scoped evidence: FINAL204/0/0 in1529ms, PID33336 Exited0/1673ms observed; all five attempts and one solution/four targeted compiles retained in the [verification report](../../docs/migration/P12_OWNER_INVARIANTS_VERIFICATION.md). Only original P12.008 checked, all221 literal requirements unchanged, original171checked/50open/additional147. Actual1353 source anchors/13 quickstart headers/670 frozen executable inputs,2607 paths/902 Markdown/7926 local links/18 master rows validated without drift, broken links, driver boundary matches or staged files. Native clock/protected Tycoon hashes unchanged. Full normative/substage P12/P1-P12/publication remain OPEN; no executable changes or runtime after FINAL.
