# Controller views and pure action previews: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Controller views and pure action previews SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Give UI and future network clients only permitted information and explain why actions are available.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. Snapshot exposes full trusted authoritative state; there is no reusable filtered query or action-preview interface.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| QUERY-R01 | Give UI and future network clients only permitted information and explain why actions are available. | QUERY-A01 |
| QUERY-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under GameQueryOwner. | QUERY-A04 |
| QUERY-R03 | Preview cannot change state, RNG or resource reservations. | QUERY-A01 |
| QUERY-R04 | Hidden entities and future random outcomes never appear in a public view. | QUERY-A02 |
| QUERY-R05 | A changed revision invalidates or recomputes the preview according to policy. | QUERY-A03 |
| QUERY-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | QUERY-A05 |
| QUERY-R21 | Restore exact committed state/time/content identity, including pending owned work. | QUERY-A06 |
| QUERY-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | QUERY-A07 |
| QUERY-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | QUERY-A08 |

## Invariants and quality boundaries

- Preview cannot change state, RNG or resource reservations.
- Hidden entities and future random outcomes never appear in a public view.
- A changed revision invalidates or recomputes the preview according to policy.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
