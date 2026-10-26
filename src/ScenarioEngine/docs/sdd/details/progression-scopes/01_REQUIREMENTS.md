# Campaign, run and profile progression: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Campaign, run and profile progression SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Carry declared state between sessions while preserving run/campaign/profile reset boundaries and outcome idempotency.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. Ending OutcomeId exists, but no progression aggregate atomically applies campaign deltas.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| PROGRESS-R01 | Carry declared state between sessions while preserving run/campaign/profile reset boundaries and outcome idempotency. | PROGRESS-A01 |
| PROGRESS-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under GameProgressionOwner. | PROGRESS-A04 |
| PROGRESS-R03 | A repeated outcome cannot award resources or unlocks twice. | PROGRESS-A01 |
| PROGRESS-R04 | One save lineage cannot mutate another campaign. | PROGRESS-A02 |
| PROGRESS-R05 | Encounter completion and final campaign closure are different scopes. | PROGRESS-A03 |
| PROGRESS-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | PROGRESS-A05 |
| PROGRESS-R21 | Restore exact committed state/time/content identity, including pending owned work. | PROGRESS-A06 |
| PROGRESS-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | PROGRESS-A07 |
| PROGRESS-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | PROGRESS-A08 |

## Invariants and quality boundaries

- A repeated outcome cannot award resources or unlocks twice.
- One save lineage cannot mutate another campaign.
- Encounter completion and final campaign closure are different scopes.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
