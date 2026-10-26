# P12-D6d — Editable recurring facility policies

Status: focused verified, 2026-10-06; build2.158.0/save85/board27/work20/view49.
Prerequisite: [D6c](P12_SERVICE_FACILITY_SELECTION_DESIGN.md).
Parents: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md), [P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md).

## 0. Problem statement

Before this slice, Standard/RecurringPlans.cs kept facility_policy in immutable RecurringPlanBinding and
resolved definition metadata. ConfigureRecurringPlanCommand can change cadence/priority/enabled state,
but a player cannot change facility preference. This blocks the plan's controllable fleet policy layer.

## 1. Goals

1. RecurringPlanSettings alone owns current facility policy identity; JSON supplies Initial.
2. The existing configuration command, preview, codec, acknowledgement and private view carry it.
3. Validate actual catalog profiles against source target, selected machine probe node and graph.
4. Changing profile affects future observations/postings only; active target/history/cadence stay intact.
5. Native and public source/policy/worker providers execute the same physical workflow and saves.
6. Exactly one GameClock and existing WorkBoard/WorkSystem remain unchanged authorities.

## 2. Design

Add explicit Code FacilityPolicy to immutable RecurringPlanSettings constructor/read/Sync/Matches.
Delete immutable policy identity from RecurringPlanBinding and resolved FacilityPolicy member from
IRecurringPlanBindingDefinition/native metadata. Binding owns authored target and worker-node bindings
only. No optional old constructor/duplicate policy override/fallback reader survives.

RecurringPlanContracts.CheckFacilityPolicy validates the actual definition and selected catalog
ActivityFacilityPolicyDef. Missing/unsupported/non-facility profiles and unknown/incompatible probe
nodes refuse with typed blockers. Startup checks Initial; current schedule validates on read/restore;
configuration checks requested settings. The existing exact Configure acknowledgement includes profile.
ActivityFacilitySelectionView captures the actual observed profile without another mutable value.
WorkBoard.ReadFacilities resolves only current schedule.Settings.FacilityPolicy through that contract.
Profile changes never rewrite actual PlanEntry intent/origin, jobs, reservations or source last posting.

Public ProofRecurringSource grows binding metadata and explicit JSON target/worker_nodes/group; its
own state/Configure/Sync remain independent. Add ignored-policy fault for exact acknowledgement proof.
Native/public existing policies rank actual safe candidates; no new selection algorithm or registry.

## 3. API surface

RecurringPlanSettings(enabled, priority, interval, facilityPolicy). Existing configure command and
private PreviewRecurringPlan/ReadRecurringPlan carry the same settings. No new command or scheduler.
Strict facility_policy:null remains explicit for self/home/public sources; facility sources need identity.

## 4. Files

Modify RecurringPlans/WorkRecurringPlans/WorkFacilities/WorkModule/Work/SaveGame, public recurring proof,
existing fixture callers/content, Program, and new EditableFacilityPolicyTests. Docs/master/parents align.

## 5. Errors

Unknown profile: work.facility-policy-unknown. Incompatible node: work.facility-policy-incompatible.
Missing/extra/wrong-target profile: work.facility-policy-not-selected. Bad structural identity refuses
construction/decode. Startup errors are ContentException; invalid restored schedules are corrupt state.
Dishonest provider acknowledgement faults before resumable state, never substitutes authored Initial.

## 6. Compatibility

Clean constructor/metadata/protocol replacement. No legacy overload, copied policy owner or save translator.

## 7. Verification

Clean build; only these two exact suites (same-name repair reruns allowed):

- Facility policies: editable settings, private preview and protocol guards
- Facility policies: active physical service, changed preferences and saved continuation

Strict content/settings, constructor/codec, unknown/incompatible targets, preview/command parity,
private/frozen/pure views, ignored edits, corrupt and pending saves, native/public sources/policies,
active preference edits, cancel/repost, physical stock/tools/travel/repair, saved twins/session/clock pause.
Other suites compile only. No third/family/full runtime gate.

Final build: zero warnings/errors, MSBuild2.44s. Contracts175 passed/0 failed/513ms;
physical184 passed/0 failed/714ms:359 checks/two exact suites, zero aborted suites.
[Evidence](../../docs/migration/P12_EDITABLE_FACILITY_POLICIES_VERIFICATION.md).

## 8. Risks

Changing a probe node is valid only when it is an actual positive selected machine node in the same
graph. Current facility readiness still probes one node; it does not reserve resources for future nodes.
Historical plans validate origin rather than today's preference. Exact acknowledgement includes all settings.

## 9. Out of scope

Fleet standby/preparedness/fuel/traffic, mutable target/binding graph, global optimization, science,
incident breadth, global callback purity, provider breadth/replay/stress/performance/full framework gates.
No Oil/Tycoon/host/demo/platform/Draw or second clock/update loop.

## 10. Execution order

- [x] Read actual owners and define clean single settings authority.
- [x] Replace settings/binding metadata; integrate validation/query and migrate explicit content/callers.
- [x] Public source proof plus bounded focused contracts/physical scenarios.
- [x] Clean build/two exact gates, ownership review and current tracker/evidence.
