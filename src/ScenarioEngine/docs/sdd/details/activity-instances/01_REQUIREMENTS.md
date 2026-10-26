# Repeatable jobs, quests and incidents: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Repeatable jobs, quests and incidents SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Represent repeatable work as independent instances with deadlines, reservations, cancellation and terminal results.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. The static objective graph is not a repeatable job queue; terminal objective states cannot be reused safely as job instances.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| ACT-R01 | Represent repeatable work as independent instances with deadlines, reservations, cancellation and terminal results. | ACT-A01 |
| ACT-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under ActivityOwner. | ACT-A04 |
| ACT-R03 | A terminal static objective is never reset to repeat a recipe. | ACT-A01 |
| ACT-R04 | Completion and cancellation cannot both consume/release the same reservation. | ACT-A02 |
| ACT-R05 | Due-time processing is deterministic and stored in logical units. | ACT-A03 |
| ACT-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | ACT-A05 |
| ACT-R21 | Restore exact committed state/time/content identity, including pending owned work. | ACT-A06 |
| ACT-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | ACT-A07 |
| ACT-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | ACT-A08 |

## Invariants and quality boundaries

- A terminal static objective is never reset to repeat a recipe.
- Completion and cancellation cannot both consume/release the same reservation.
- Due-time processing is deterministic and stored in logical units.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
