# 4X strategy layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [4X strategy layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Support faction turns, territory, research, economy and agreements with multiple declared victory paths.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. Conditions/endings are reusable, but no faction knowledge, territory, diplomacy or 4X turn runtime exists.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| FOURX-R01 | Support faction turns, territory, research, economy and agreements with multiple declared victory paths. | FOURX-A01 |
| FOURX-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under FourXLayer. | FOURX-A04 |
| FOURX-R03 | Another faction's private plans/resources are not exposed by previews. | FOURX-A01 |
| FOURX-R04 | Diplomatic proposals apply nothing until valid acceptance. | FOURX-A02 |
| FOURX-R05 | Victory ties and mutually exclusive results have explicit policy. | FOURX-A03 |
| FOURX-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | FOURX-A05 |
| FOURX-R21 | Restore exact committed state/time/content identity, including pending owned work. | FOURX-A06 |
| FOURX-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | FOURX-A07 |
| FOURX-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | FOURX-A08 |

## Invariants and quality boundaries

- Another faction's private plans/resources are not exposed by previews.
- Diplomatic proposals apply nothing until valid acceptance.
- Victory ties and mutually exclusive results have explicit policy.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
