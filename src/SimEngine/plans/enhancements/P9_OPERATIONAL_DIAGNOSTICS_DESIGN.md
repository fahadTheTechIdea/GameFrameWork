# P9-C9 — Engine-owned logistics diagnostics

## 0. Problem

Existing LogisticsView provides isolated requests/previews, but no consolidated immutable diagnostic
for target shortfall, existing inbound promises, queue refusals, real shipments, unassigned carriers
and measured incoming throughput. Hosts must not recreate logistics or classify transfer history.

## 1. Goals

1. One ILogisticsDiagnostics alias on the existing LogisticsService; no new stock/queue/history owner.
2. Derive unmet desired stock and actual unpromised dispatch gap from existing demand/inlet rules.
3. Detached current offers, shipments and company carriers expose exact blockers/native capacity.
4. Exact native-unit incoming haul throughput uses only retained committed endpoint history.
5. Private/pure reads, physical haul/pause/conservation and saved continuation through one new case.

## 2. Design

Add Standard/LogisticsDiagnostics.cs as another partial of the existing LogisticsService.
ReadDiagnostics(company,target,resource,out snapshot) validates owner/resource/inlet before any facts.
The SAME BuildQueue, DemandOf, DeliveryGap, guarded cargo binding and disposition owners supply
the result. Resource-specific capacity stays per carrier; never sum incompatible native units or
count ready requests as reservations. Unassigned does not promise eligibility; existing previews
and gates remain authoritative. Return-blocked cargo reports actual current selector refusal.

ReadIncomingThroughput(company,target,resource,fromStep,throughStep,out snapshot) delegates to the
optional IResourceFlowJournal.ReadWindow. Count actual sim.haul transfer facts whose destination
is the target's current endpoint; loading into other cargo endpoints is not double-counted.
The half-open window is explicit. BigInteger native raw quantity and reduced rational per-game-second
rate use the sole authored simulation quantum. No float/clamp, cache or new history/codec.
Absent/expired history refuses explicitly, never reports fabricated zero throughput.

LogisticsView only checks viewer ownership and delegates. Bind the new contract to the SAME service.
Advance view/build identities; persistent payloads unchanged. Reuse master-selected point/flow JSON.

## 3. API surface

ILogisticsDiagnostics; immutable LogisticsOperationalSnapshot, LogisticsCarrierDiagnostic,
LogisticsShipmentDiagnostic and LogisticsThroughputSnapshot. Existing native IDs/units/previews only.

## 4. Files

Existing LogisticsService declaration, LogisticsModule alias/view version, LogisticsView delegation,
Job's canonical reservation-scope constant, new partial diagnostic file and one new bounded proof.
Strict old identity/view assertions migrate and compile only. Stage/tracker/evidence updated.

## 5. Errors

Unknown/unowned target/resource/unsupported inlet refuse with no partial output. Invalid window,
absent/expired history are explicit. Current queue/provider exceptions propagate with existing guards.
No future availability, fabricated destination, aggregate cargo ledger or publication.

## 6. Compatibility

Development direct contract revision; no legacy overload, duplicate path or compatibility reader.

## 7. Verification

One completed-slice build and one distinct NEW bounded case:
`Logistics diagnostics: private pipeline, physical incoming throughput and saved continuation`.
Repair/rerun only observed failures. No older/family/full/platform/stale gate. Freeze source/tests/
JSON after final green; subsequent documentation only. Record every invocation.

## 8. Risks

Diagnostic reads enumerate existing companies' entities/jobs/requests. No CPU/allocation budget
claim. Current unassigned capacity is not route-compatible dispatch capacity; conditional travel
previews exclude queues/handling and are not guaranteed arrival deadlines. Historical throughput
is incoming to this exact endpoint, not end-to-end network profit or whole-session accounting.

## 9. Non-goals

No business gameplay/Tycoon source, Oil, host/demo gameplay, Draw, platforms, new scheduler or clock.
Full P1-P12 goal and publication remain pending full verified completion.

## 10. Execution

Implement same-owner derived contracts and delegation; author/register one bounded point-space
player-command proof; build/run it; freeze source and record evidence and remaining gates.

Implemented2026-10-08. Build2.256.0/save139/logistics view29; owner payloads unchanged.
ONE clean build8.41s,0 warnings/errors. ONE exact NEW bounded case48 checks,0 failures/aborts478ms,
first runtime attempt. Older121 strict identities compile-only. Source/tests/JSON frozen after green.
[Evidence](../../docs/migration/P9_OPERATIONAL_DIAGNOSTICS_VERIFICATION.md);
[master tracker](MASTER_TRACKER.md). Broader P9/full P1-P12 gates remain open.
