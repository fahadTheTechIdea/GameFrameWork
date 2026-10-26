# P12-B4: Physical network interruption and renewed exposure — Design Document

## 0. Problem Statement

P12-B3 proves connected delayed propagation; P10-A6 proves physical cut/rebuild. Neither demonstrates that the original exposure owner rejects a queued delivery after a real cut, then schedules a fresh delay after hauled-material reconstruction.

## 1. Goals

1. Ordinary timed machine work cuts a bridge before an already scheduled exposure becomes due.
2. Actual split topology prevents stale delivery without ending the source incident.
3. Real material hauling and authored segment work rebuild the bridge; propagation schedules a fresh delay, not instant damage.
4. Partial cut, disconnected pending, reconstruction and renewed pending saves continue identically.
5. Actual water delivery and authored response work resolve source and target episodes exactly once; no free repair.
6. Native/direct JSON contracts share original schedulers, topology, work, resource and clock owners.

## 2. Design

Reuse NetworkIncidentPropagationTests.Create and existing physical-line-removal.json through explicit additional master document declarations. A new network-interruption.json defines a named network propagation rule with sixty-minute delay and a one-water target resource response requirement. The original response activity references that requirement. Explicit starting source steel is eight units; it is not a grant or global default. Original machinery, physical bridge, resource transaction and stock owners execute all changes. Helpers only select authored content, issue ordinary commands and inspect immutable views. No harness-side incident delivery, time calculation, topology edit or repair algorithm.

The focused driver captures the actual queued deadline, observes a physical cut, waits beyond that deadline and verifies the target remains inactive while the source remains active and supplied topology identities differ. It reconstructs with one hauled steel, observes a newly scheduled exposure, verifies a fresh authored delay and then supplies both actual response targets with water. Recovery must consume each unit and complete timed work. If this reveals an engine defect, repair the existing responsible owner, not a second subsystem.

## 3. API surface

No new engine public API or state codec is planned. Reuse PostOrderCommand, LayLineCommand, HaulResourcesCommand, RaiseIncidentCommand and original incident/network/work/resource views, Save/Load and replay. Thin fixture composition accepts additional explicitly declared modules/documents.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | NetworkIncidentPropagationTests.cs fixture visibility/composition | 30 | low |
| New | NetworkPhysicalInterruptionTests.cs | 170 | low |
| New | network-interruption.json | 15 | low |
| Modify | proof resource/project and case registration | 2 | low |
| Modify | master/phase/evidence/audit | metadata | low |

## 5. Error handling matrix

| Condition | Expected |
|---|---|
| Queued exposure reaches physically disconnected target | Original due-time eligibility refuses; no raise |
| Source lacks authored response water | Ordinary response blocker; incident remains active |
| Rebuilt connection | Fresh scan and delay; no instant damage or stale deadline reuse |
| Actual response supplied | Physical work consumes real material and resolves bound episode once |
| Stage stalls | Bounded diagnostic includes real clock, topology, pending exposures and response blockers |
| Save/load | Original codecs only; twin hashes agree at each Update |

## 6. Backward compatibility

Development only; no shim/legacy or alternate saved state. Build2.401.0/save154/replay1 unchanged unless an actual engine repair is required.

## 7. Verification

Completed narrowly: [evidence](../../docs/migration/P12_PHYSICAL_NETWORK_INTERRUPTION_VERIFICATION.md), FIRST/FINAL109/0/0 in3007ms; one clean build5.59s. No production engine repair required; original owners execute the integrated cut/rebuild/delay/supply/repair flow.

Build completed fixture/driver and run ONE NEW exact case only; outer30000ms/termination5000ms/output30000chars,256 Updates per bounded stage. Repair/rerun only this case. No historical/family/full/Oil/Tycoon/platform runtime. Broad P1-P12 gates stay OPEN.

## 8. Risks

Emergency response must remain blocked by real missing authored material, not a test-owned pause or worker exclusion. A sixty-minute authored delay makes this a deterministic opportunity for physical intervention, not a global balance default. Existing finite inventories must retain conservation through topology and repair.

## 9. Out of scope

Weather/chemical propagation, custom diagnostics/provider fault breadth, cross-company networks, arbitrary corridor removal plans and full acceptance.

## 10. Sub-task execution order

1. Add explicit master-listed integration rule/response requirement and reuse existing fixture/physical work.
2. Execute ordinary cut/rebuild/supply scenario with four partial-state saves, pause, privacy, isolation and replay.
3. Repair only actual existing-owner defects revealed; record exact bounded attempts.
4. Update tracker/source hashes without changing original compound requirements or publishing.
