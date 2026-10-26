# Party RPG layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Party RPG layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Support a persistent party with configurable formation, progression, equipment, abilities and story interactions.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. No character/ability/equipment rules or party encounter runtime exists; generic facts are only an extension point.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| PARTY-R01 | Support a persistent party with configurable formation, progression, equipment, abilities and story interactions. | PARTY-A01 |
| PARTY-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under PartyRpgLayer. | PARTY-A04 |
| PARTY-R03 | A unique item occupies one container or equipment slot. | PARTY-A01 |
| PARTY-R04 | Unavailable/departed characters cannot be selected. | PARTY-A02 |
| PARTY-R05 | Ability/status order is declared, bounded and independent of animation timing. | PARTY-A03 |
| PARTY-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | PARTY-A05 |
| PARTY-R21 | Restore exact committed state/time/content identity, including pending owned work. | PARTY-A06 |
| PARTY-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | PARTY-A07 |
| PARTY-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | PARTY-A08 |

## Invariants and quality boundaries

- A unique item occupies one container or equipment slot.
- Unavailable/departed characters cannot be selected.
- Ability/status order is declared, bounded and independent of animation timing.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
