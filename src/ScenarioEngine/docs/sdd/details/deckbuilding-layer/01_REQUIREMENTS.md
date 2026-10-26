# Deckbuilding and card encounter layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Deckbuilding and card encounter layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Resolve card choices, draw/discard state, energy and bounded effect chains across seeded encounters.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. No card zones, reaction/trigger ordering or turn-based energy runtime exists.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| CARD-R01 | Resolve card choices, draw/discard state, energy and bounded effect chains across seeded encounters. | CARD-A01 |
| CARD-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under DeckbuildingLayer. | CARD-A04 |
| CARD-R03 | A card instance exists in exactly one zone. | CARD-A01 |
| CARD-R04 | Rejected play spends no energy or card. | CARD-A02 |
| CARD-R05 | Triggered effects cannot recurse indefinitely or use presentation callbacks as rule execution. | CARD-A03 |
| CARD-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | CARD-A05 |
| CARD-R21 | Restore exact committed state/time/content identity, including pending owned work. | CARD-A06 |
| CARD-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | CARD-A07 |
| CARD-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | CARD-A08 |

## Invariants and quality boundaries

- A card instance exists in exactly one zone.
- Rejected play spends no energy or card.
- Triggered effects cannot recurse indefinitely or use presentation callbacks as rule execution.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
