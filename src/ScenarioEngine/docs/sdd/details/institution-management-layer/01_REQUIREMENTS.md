# Institution and life-management layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Institution and life-management layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Coordinate people, facilities, schedules, service queues and calendar outcomes within a persistent institution.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. No timetable, facility service, relationship or calendar aggregate exists; these are domain rules over proposed time/jobs/world capabilities.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| LIFE-R01 | Coordinate people, facilities, schedules, service queues and calendar outcomes within a persistent institution. | LIFE-A01 |
| LIFE-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under InstitutionLayer. | LIFE-A04 |
| LIFE-R03 | One person cannot occupy mutually exclusive scheduled jobs. | LIFE-A01 |
| LIFE-R04 | Capacity/stock changes and assignments commit together. | LIFE-A02 |
| LIFE-R05 | Unavailable staff/service is not represented as a successful empty result. | LIFE-A03 |
| LIFE-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | LIFE-A05 |
| LIFE-R21 | Restore exact committed state/time/content identity, including pending owned work. | LIFE-A06 |
| LIFE-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | LIFE-A07 |
| LIFE-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | LIFE-A08 |

## Invariants and quality boundaries

- One person cannot occupy mutually exclusive scheduled jobs.
- Capacity/stock changes and assignments commit together.
- Unavailable staff/service is not represented as a successful empty result.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
