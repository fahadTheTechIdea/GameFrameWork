# Colony and emergent-story layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Colony and emergent-story layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Coordinate inhabitants, work, needs, relationships and authored incident policies in a persistent settlement.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. The kernel can commit layer state, but it has no colonist job allocator, incident director or population-scale evidence.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| COLONY-R01 | Coordinate inhabitants, work, needs, relationships and authored incident policies in a persistent settlement. | COLONY-A01 |
| COLONY-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under ColonyLayer. | COLONY-A04 |
| COLONY-R03 | Incident policies cannot mutate state outside the candidate transaction. | COLONY-A01 |
| COLONY-R04 | Assignment never duplicates an exclusive reservation. | COLONY-A02 |
| COLONY-R05 | Offscreen abstract simulation reconciles with the same saved inhabitants. | COLONY-A03 |
| COLONY-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | COLONY-A05 |
| COLONY-R21 | Restore exact committed state/time/content identity, including pending owned work. | COLONY-A06 |
| COLONY-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | COLONY-A07 |
| COLONY-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | COLONY-A08 |

## Invariants and quality boundaries

- Incident policies cannot mutate state outside the candidate transaction.
- Assignment never duplicates an exclusive reservation.
- Offscreen abstract simulation reconciles with the same saved inhabitants.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
