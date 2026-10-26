# Factory and economic simulation layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Factory and economic simulation layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Simulate integer production/storage flows, research and infrastructure over long sessions.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. No production network or market rules exist; full-state hashing and retained history need measured long-session qualification.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| FACTORY-R01 | Simulate integer production/storage flows, research and infrastructure over long sessions. | FACTORY-A01 |
| FACTORY-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under FactoryEconomyLayer. | FACTORY-A04 |
| FACTORY-R03 | Total resource changes equal declared inputs/outputs/consumption. | FACTORY-A01 |
| FACTORY-R04 | A blocked output never erases produced value. | FACTORY-A02 |
| FACTORY-R05 | Cycles use explicit bounded transfer rules; no unbounded recursive propagation. | FACTORY-A03 |
| FACTORY-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | FACTORY-A05 |
| FACTORY-R21 | Restore exact committed state/time/content identity, including pending owned work. | FACTORY-A06 |
| FACTORY-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | FACTORY-A07 |
| FACTORY-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | FACTORY-A08 |

## Invariants and quality boundaries

- Total resource changes equal declared inputs/outputs/consumption.
- A blocked output never erases produced value.
- Cycles use explicit bounded transfer rules; no unbounded recursive propagation.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
