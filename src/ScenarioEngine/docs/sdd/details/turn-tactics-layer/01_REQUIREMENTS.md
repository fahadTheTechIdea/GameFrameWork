# Turn-based tactics layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Turn-based tactics layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Resolve tactical missions through explicit turns/phases, legal previews, action budgets and saved encounter state.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. A layer can encode a turn counter, but the shared current time context is fixed 50 ms and no turn query/encounter protocol exists.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| TACT-R01 | Resolve tactical missions through explicit turns/phases, legal previews, action budgets and saved encounter state. | TACT-A01 |
| TACT-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under TurnTacticsLayer. | TACT-A04 |
| TACT-R03 | Waiting for player input consumes no game time or RNG. | TACT-A01 |
| TACT-R04 | An invalid action spends no action points. | TACT-A02 |
| TACT-R05 | Animation completion cannot grant a second action or decide damage. | TACT-A03 |
| TACT-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | TACT-A05 |
| TACT-R21 | Restore exact committed state/time/content identity, including pending owned work. | TACT-A06 |
| TACT-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | TACT-A07 |
| TACT-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | TACT-A08 |

## Invariants and quality boundaries

- Waiting for player input consumes no game time or RNG.
- An invalid action spends no action points.
- Animation completion cannot grant a second action or decide damage.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
