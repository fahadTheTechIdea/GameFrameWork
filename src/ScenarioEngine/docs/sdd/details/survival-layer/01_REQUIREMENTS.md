# Survival and crafting layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Survival and crafting layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Make gathering, exposure, needs, shelter and crafting operate as coherent resource/time decisions.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. Current engine provides transactions and integer state, but no survival needs, recipe, build or interruption policy.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| SURV-R01 | Make gathering, exposure, needs, shelter and crafting operate as coherent resource/time decisions. | SURV-A01 |
| SURV-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under SurvivalLayer. | SURV-A04 |
| SURV-R03 | Sources do not regenerate on scene reload unless an explicit replenishment rule says so. | SURV-A01 |
| SURV-R04 | Rest cannot skip an attack or lethal threshold. | SURV-A02 |
| SURV-R05 | Failed placement cannot consume materials or create an invisible shelter. | SURV-A03 |
| SURV-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | SURV-A05 |
| SURV-R21 | Restore exact committed state/time/content identity, including pending owned work. | SURV-A06 |
| SURV-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | SURV-A07 |
| SURV-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | SURV-A08 |

## Invariants and quality boundaries

- Sources do not regenerate on scene reload unless an explicit replenishment rule says so.
- Rest cannot skip an attack or lethal threshold.
- Failed placement cannot consume materials or create an invisible shelter.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
