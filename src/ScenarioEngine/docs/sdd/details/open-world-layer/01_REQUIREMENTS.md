# Open-world sandbox layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Open-world sandbox layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Maintain an ongoing world whose exploration and local activities persist independently of native region residency.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. The current global zone seam lacks region-local revisions, inactive-world policy and ongoing-world activity closure.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| OPEN-R01 | Maintain an ongoing world whose exploration and local activities persist independently of native region residency. | OPEN-A01 |
| OPEN-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under OpenWorldLayer. | OPEN-A04 |
| OPEN-R03 | Unloading is not deletion of logical entities or state. | OPEN-A01 |
| OPEN-R04 | A finished side mission does not set the ongoing world to Ended. | OPEN-A02 |
| OPEN-R05 | Region-local changes do not invalidate every world order. | OPEN-A03 |
| OPEN-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | OPEN-A05 |
| OPEN-R21 | Restore exact committed state/time/content identity, including pending owned work. | OPEN-A06 |
| OPEN-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | OPEN-A07 |
| OPEN-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | OPEN-A08 |

## Invariants and quality boundaries

- Unloading is not deletion of logical entities or state.
- A finished side mission does not set the ongoing world to Ended.
- Region-local changes do not invalidate every world order.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
