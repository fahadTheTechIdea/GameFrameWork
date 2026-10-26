# Logical time and advancement policies: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Logical time and advancement policies SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Separate committed step order from elapsed game time for fixed-time, action-driven and calendar-based games.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. ScenarioEngine currently constructs every LayerTickContext with a 50 ms FixedStep.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| TIME-R01 | Separate committed step order from elapsed game time for fixed-time, action-driven and calendar-based games. | TIME-A01 |
| TIME-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under SimulationTimeOwner. | TIME-A04 |
| TIME-R03 | No DateTime.Now or elapsed render frame count controls rule time. | TIME-A01 |
| TIME-R04 | A failed commit cannot consume game time. | TIME-A02 |
| TIME-R05 | Fast-forward never skips an unresolved observable deadline. | TIME-A03 |
| TIME-R06 | Native body movement remains native continuous physics work. | TIME-A01 |
| TIME-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | TIME-A05 |
| TIME-R21 | Restore exact committed state/time/content identity, including pending owned work. | TIME-A06 |
| TIME-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | TIME-A07 |
| TIME-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | TIME-A08 |

## Invariants and quality boundaries

- No DateTime.Now or elapsed render frame count controls rule time.
- A failed commit cannot consume game time.
- Fast-forward never skips an unresolved observable deadline.
- Native body movement remains native continuous physics work.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
