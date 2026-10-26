# Narrative adventure layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Narrative adventure layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Build an authored journey with revisitable places, interactions, choices and multiple reachable outcomes.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. The kernel supplies conditions/endings and layer hooks; no adventure interactions, conversation checkpoints or retained-location rules are implemented.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| ADV-R01 | Build an authored journey with revisitable places, interactions, choices and multiple reachable outcomes. | ADV-A01 |
| ADV-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under AdventureLayer. | ADV-A04 |
| ADV-R03 | Required story progress has an affordable supported route. | ADV-A01 |
| ADV-R04 | A clue or item cannot be collected twice by reloading a scene. | ADV-A02 |
| ADV-R05 | Dialogue choice availability uses current state, not a stale menu. | ADV-A03 |
| ADV-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | ADV-A05 |
| ADV-R21 | Restore exact committed state/time/content identity, including pending owned work. | ADV-A06 |
| ADV-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | ADV-A07 |
| ADV-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | ADV-A08 |

## Invariants and quality boundaries

- Required story progress has an affordable supported route.
- A clue or item cannot be collected twice by reloading a scene.
- Dialogue choice availability uses current state, not a stale menu.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
