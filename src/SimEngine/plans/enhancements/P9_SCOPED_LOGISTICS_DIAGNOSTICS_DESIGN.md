# P9-D5e: Actual-endpoint logistics diagnostics

Status: native implementation/focused verification complete; full P9/P1-P12 and publication OPEN.
Core2.320.0/save153/logistics-view34; final200 checks in1131ms, two clean builds final6.46s.
[Evidence](../../docs/migration/P9_SCOPED_LOGISTICS_DIAGNOSTICS_VERIFICATION.md).

## 0. Problem statement

The existing diagnostic contract still requires local IInletPort. Named-only facilities
therefore have no operational or throughput diagnostics, and entity-based shipment/offer
filters mix different actual stores belonging to the same site.

## 1. Goals

Extend the original diagnostic contract and private LogisticsView with an explicit port
and optional original endpoint. Observe stock, promises, desired demand and unpromised
delivery gap for that actual store. Filter offers, live shipments and committed incoming
history by actual endpoint. Preserve original acquired trip identities after selection changes.

## 2. Design and ownership

LogisticsService remains the sole selector/intent/diagnostic projection owner. PhysicalPort
and ResourcePortContracts remain the existing resolution path. WorkBoard owns jobs and
receipt links; endpoint books own promises; ResourceFlowJournal owns bounded history.
Snapshots contain detached facts only. No diagnostic registry, cache, balance or new clock.

One DeliveryGap calculation accepts an actual IResourceInlet; existing local/recovery and
Demand entry points delegate to it. Endpoint identity, company, capacity policy and version
are guarded across reads, along with the existing entity/session observation guard.

## 3. API surface

ILogisticsDiagnostics.ReadDiagnostics and ReadIncomingThroughput gain Code port and nullable
ResourceEndpointId original arguments. No alternate overload, reader or compatibility wrapper.
LogisticsOperationalSnapshot gains the resolved Port and Endpoint. Shipment facts expose the
original source/destination port and admitted endpoint IDs. LogisticsView keeps company privacy.

## 4. Files

Standard/LogisticsDiagnostics.cs, LogisticsStockPolicies.cs, HaulResourcePorts.cs;
Views/LogisticsView.cs, Standard/LogisticsModule.cs and the engine build identity.
One new thin player test uses the already master-listed scoped-policies/scoped-haul JSON.
No new authored scenario or duplicate content is needed for the same native facility capabilities.

## 5. Refusal and purity

Unknown/removed explicit originals or missing ports return typed NoStorage, never selected
fallback. Invalid resource/window/endpoint arguments refuse. Foreign company reads refuse
before publishing facts. A changed actual endpoint during observation violates the provider
contract; no fabricated diagnostics or compensating resource changes.

## 6. Protocol

Advance core to2.320.0 and Logistics view34. Native save153, replay1 and all service/component
layouts remain unchanged: no additional persisted state. Older runtime evidence is historical.

## 7. Focused verification

Build for compilation. Run only ONE NEW bounded exact case. Physically fund two stores through
normal hauling; compare aliases and independent endpoints; inspect competing demands and an
acquired trip before/after selection changes; save/load twins and replay. Check company privacy,
typed refusals, zero read mutations, original receipt facts and endpoint-specific native history.
Each wait has a1000-Update bound and snapshot diagnostics; parent timeout is15000ms.

## 8. Risks

One endpoint may be reachable by multiple site/port aliases. Actual endpoint grouping must not
hide its shared promises or inflate throughput. A snapshot may outlive current selection, so
explicit original reads must resolve the original native store, not the current one.

## 9. Open scope

Generic activity-DAG scoped supply, scoped recovery dispatch, same-site automatic matching,
provider/fault/removal/corruption matrices, docks/areas/emergency/congestion/stress, historical
and full regression, P1-P12 acceptance and goal remain open. No Oil/platform/Draw/Tycoon work.
No staging/commit/push before FULL goal AND plan genuinely finished and verified.

## 10. Execution

1. Extend the existing actual-endpoint resolution and derived facts.
2. Unify gap calculation without changing intent/claims or adding owners.
3. Add the single new public-command/view player case and repair only that case as needed.
4. Record actual evidence and refresh the master tracker and source/acceptance audit.
