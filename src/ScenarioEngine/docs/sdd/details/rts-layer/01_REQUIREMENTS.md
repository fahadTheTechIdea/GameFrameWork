# RTS and base-defence layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [RTS and base-defence layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Run strategic group orders, resource production, construction and combat under a measured fixed-time profile.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. Command admission exists, but unit groups, production, fog of war, building placement and native unit scale do not.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| RTS-R01 | Run strategic group orders, resource production, construction and combat under a measured fixed-time profile. | RTS-A01 |
| RTS-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under RtsLayer. | RTS-A04 |
| RTS-R03 | Group commands cannot bypass per-actor ownership or cost checks. | RTS-A01 |
| RTS-R04 | A navigation target is not evidence that a body reached it. | RTS-A02 |
| RTS-R05 | Construction updates collision/navigation through native facilities before usable readiness. | RTS-A03 |
| RTS-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | RTS-A05 |
| RTS-R21 | Restore exact committed state/time/content identity, including pending owned work. | RTS-A06 |
| RTS-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | RTS-A07 |
| RTS-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | RTS-A08 |

## Invariants and quality boundaries

- Group commands cannot bypass per-actor ownership or cost checks.
- A navigation target is not evidence that a body reached it.
- Construction updates collision/navigation through native facilities before usable readiness.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
