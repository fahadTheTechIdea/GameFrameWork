# P9-B11.3c2a — Authored service triggers and operation readiness

Status: c2a focused verified, 2026-10-07. B11.3c2 and the full roadmap remain open.
Build2.192.0/save104/board36/work34/work view67; existing owner payloads unchanged.
[Evidence](../../docs/migration/P9_SERVICE_TRIGGER_VERIFICATION.md).

## 0. Problem

RecurringPlanContracts.Read checks cadence before the native source's maintenance threshold.
Damage before a service deadline cannot request early maintenance, and linked policies implicitly
exclude healthy periodic inspection. These decisions must be explicit authored engine rules.

## 1. Goals

1. Every recurring definition explicitly selects schedule, maintenance or schedule_or_maintenance.
2. Maintenance modes require an actual same-entity operation policy; missing links fail composition.
3. The shared recurring protocol reads the existing policy's maintenance band, never copies thresholds.
4. Early condition demand cannot bypass disabled state, outstanding work, pending acknowledgement,
   failed-attempt retry spacing, physical worker readiness, supplies, docks or safety shutdown.
5. Native/public sources and policy providers share private previews, commands and saved continuation.

## 2. Design

Add RecurringTriggerMode to IRecurringPlanDefinition. Native/public definitions read explicit trigger
and operation_policy through the same parser. Existing source CheckTrigger retains provider-specific
filters only; shared RecurringPlanContracts handles schedule/maintenance composition. The policy
still owns settings, condition bindings, shutdown latch and restart. RecurringPlanView exposes
TriggerMode and MaintenanceDue as immutable current facts; DueGameTime remains scheduled cadence.
Successful completion continues to use actual board history. A failed/cancelled latest attempt
must wait for its existing retry interval even if condition is still low. No new state owner or clock.

## 3. API

RecurringTriggerMode; IRecurringPlanDefinition.TriggerMode; RecurringPlanView.TriggerMode/MaintenanceDue.
Public proof source implements the existing IRecurringOperationPolicyDefinition metadata link.
No new player command, scheduling class, fleet model, condition threshold, deadline or codec.

## 4. Files

RecurringPlans/WorkRecurringPlans and WorkSystem/view semantic identity; native/public JSON fixtures
and definition readers; public service proof registrations; CompletionServiceTests and authored
completion-service fixture; stage evidence, master tracker, README and offline help.

## 5. Errors

Missing/unknown trigger and maintenance mode without a valid policy fail before gameplay. Healthy
condition-only sources report MaintenanceNotDue. Disabled/outstanding/pending completion and retry
blockers remain authoritative. Invalid policy providers fault through the existing observation guard.

## 6. Compatibility

Direct authored content/API/build migration. Existing owner payloads stay unchanged. No defaults,
legacy parser, compatibility facade, alternate scheduler or fallback repair.

## 7. Verification

Build plus only the two exact completion-service suites, extended with trigger admission and
physical readiness scenarios. Verify early service, healthy scheduled service, policy edits,
cancellation retry spacing, shutdown/restart, resource shortages, pure/private views and saved twins.
Unrelated suites compile only; no full/family/third gate.

## 8. Risks

Condition-triggered maintenance does not guarantee that a stranded machine can move. Existing
operation and physical capability gates remain in force; independent response/recovery is still
required when propulsion fails. Broader trip/fleet/congestion/provider stress remains open.

## 9. Out of scope

Oil/Tycoon/hosts/demos/renderers/platforms/Draw, instant repair, independent timers or copied history.

## 10. Stages

- [x] Explicit trigger contracts, shared operation-policy admission and content migration.
- [x] Focused actual machine/service/condition/operation/save proof.
- [x] Current build, scoped evidence, tracker and fingerprints: clean build;332 assertions/two
  exact suites (138 admission;194 physical), zero final failures/aborts. Separate suites compile only.
- [ ] B11.3c2b broader fleet/trip readiness and B11.4 acceptance.
