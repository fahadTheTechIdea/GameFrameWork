# Roguelite run and meta-progression layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Roguelite run and meta-progression layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Run seeded encounters and temporary builds with explicitly limited persistent unlocks between attempts.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. Randomness and endings exist, but reset/carryover policy, run generation and durable profile updates do not.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| ROGUE-R01 | Run seeded encounters and temporary builds with explicitly limited persistent unlocks between attempts. | ROGUE-A01 |
| ROGUE-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under RogueliteLayer. | ROGUE-A04 |
| ROGUE-R03 | Reloading cannot reroll an already selected encounter/reward. | ROGUE-A01 |
| ROGUE-R04 | Run failure does not erase valid profile progress. | ROGUE-A02 |
| ROGUE-R05 | A duplicate run result cannot unlock/grant twice. | ROGUE-A03 |
| ROGUE-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | ROGUE-A05 |
| ROGUE-R21 | Restore exact committed state/time/content identity, including pending owned work. | ROGUE-A06 |
| ROGUE-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | ROGUE-A07 |
| ROGUE-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | ROGUE-A08 |

## Invariants and quality boundaries

- Reloading cannot reroll an already selected encounter/reward.
- Run failure does not erase valid profile progress.
- A duplicate run result cannot unlock/grant twice.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
