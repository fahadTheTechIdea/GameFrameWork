# Reproducible generation and measured scale profiles: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Reproducible generation and measured scale profiles SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Generate regions/encounters reproducibly and qualify explicit workload limits with actual measurements.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. Seeded random streams exist; no generator manifest, world generator or native scale qualification exists.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| GEN-R01 | Generate regions/encounters reproducibly and qualify explicit workload limits with actual measurements. | GEN-A01 |
| GEN-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under GenerationOwner. | GEN-A04 |
| GEN-R03 | String.GetHashCode and dictionary order do not define seeds. | GEN-A01 |
| GEN-R04 | An upgrade cannot silently regenerate an existing world differently. | GEN-A02 |
| GEN-R05 | Actor catalog size and simultaneously active native actors are separate budgets. | GEN-A03 |
| GEN-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | GEN-A05 |
| GEN-R21 | Restore exact committed state/time/content identity, including pending owned work. | GEN-A06 |
| GEN-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | GEN-A07 |
| GEN-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | GEN-A08 |

## Invariants and quality boundaries

- String.GetHashCode and dictionary order do not define seeds.
- An upgrade cannot silently regenerate an existing world differently.
- Actor catalog size and simultaneously active native actors are separate budgets.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
